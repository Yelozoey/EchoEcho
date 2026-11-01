using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class EchoDynamicObstacleTests
    {
        private Scene scene,previous;private bool background;private EchoNavigation motor;private BoxCollider[] environment;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-E03";
        [Serializable]private class Report{public bool passed,carved,recovered;public float oldClearance,newClearance,maximumPenetration;public int checkedFrames;public Vector3[] pathBefore,pathAfter;}
        [Test]public void E03_01_ObstacleTemplateHasMatchingGeometryAndCarvingBudget()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(EchoObstacleSetup.PrefabPath);var box=prefab.GetComponent<BoxCollider>();var obstacle=prefab.GetComponent<NavMeshObstacle>();
            Assert.That(prefab.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(obstacle.shape,Is.EqualTo(NavMeshObstacleShape.Box));Assert.That(obstacle.size,Is.EqualTo(box.size));Assert.That(obstacle.center,Is.EqualTo(box.center));
            Assert.That(obstacle.carving&&obstacle.carveOnlyStationary,Is.True);Assert.That(obstacle.carvingMoveThreshold,Is.EqualTo(.05f));Assert.That(obstacle.carvingTimeToStationary,Is.EqualTo(.2f));
            var renderer=prefab.GetComponentInChildren<MeshRenderer>();Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(renderer.HasPropertyBlock(),Is.False);Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject),Is.EqualTo((StaticEditorFlags)0));
        }
        private IEnumerator Setup()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("EnterPlayMode must be yielded directly by the UnityTest before Setup.");
            background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();
            while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Cabin load did not complete.");yield return null;}SceneManager.SetActiveScene(scene);
            motor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath)).GetComponent<EchoNavigation>();environment=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox").GetComponentsInChildren<BoxCollider>().Where(c=>!c.isTrigger).ToArray();Directory.CreateDirectory(Evidence);
        }
        private NavMeshQueryFilter Filter=>new NavMeshQueryFilter{agentTypeID=motor.Agent.agentTypeID,areaMask=NavMesh.AllAreas};
        private static IEnumerator GameSeconds(float seconds)
        {
            var start=Time.time;var timer=System.Diagnostics.Stopwatch.StartNew();
            while(Time.time-start<seconds){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("No game-time advance for carving.");yield return R04ShadowProbe.Frame();}
        }
        private static float Clearance(NavMeshPath path,Vector3 center)
        {
            var best=float.PositiveInfinity;var points=path.corners;
            for(var i=1;i<points.Length;i++)
            {var a=new Vector2(points[i-1].x,points[i-1].z);var b=new Vector2(points[i].x,points[i].z);var c=new Vector2(center.x,center.z);var delta=b-a;var t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(c-a,delta)/delta.sqrMagnitude):0;best=Mathf.Min(best,Vector2.Distance(c,a+delta*t));}
            return best;
        }
        [UnityTest]public IEnumerator E03_02_NewObstacleActuallyCarvesAndRobotReplansAroundIt()
        {
            yield return new EnterPlayMode();yield return Setup();var center=new Vector3(2,0,-1.4f);var from=new Vector3(1,0,-2.5f);var to=new Vector3(4,0,0);var before=new NavMeshPath();
            Assert.That(NavMesh.SamplePosition(center,out _,.1f,Filter),Is.True);Assert.That(NavMesh.CalculatePath(from,to,Filter,before)&&before.status==NavMeshPathStatus.PathComplete,Is.True);
            var beforeCorners=before.corners;Assert.That(beforeCorners.Length,Is.GreaterThanOrEqualTo(2));var oldClearance=Clearance(before,center);Assert.That(float.IsFinite(oldClearance),Is.True);
            Assert.That(motor.TryBind(new Vector3(0,0,-6))&&motor.TryMove(new Vector3(7.8f,0,2.2f)),Is.True);yield return GameSeconds(.25f);
            var obstacle=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoObstacleSetup.PrefabPath),center,Quaternion.identity);yield return GameSeconds(.5f);
            var after=new NavMeshPath();var report=new Report{pathBefore=beforeCorners,oldClearance=oldClearance,carved=!NavMesh.SamplePosition(center,out _,.05f,Filter)};
            Assert.That(NavMesh.CalculatePath(from,to,Filter,after)&&after.status==NavMeshPathStatus.PathComplete,Is.True);report.pathAfter=after.corners;report.newClearance=Clearance(after,center);
            var box=motor.GetComponent<BoxCollider>();var solids=environment.Concat(new[]{obstacle.GetComponent<BoxCollider>()}).ToArray();var timer=System.Diagnostics.Stopwatch.StartNew();
            while(motor.State==NavigationState.Moving)
            {
                if(timer.Elapsed.TotalSeconds>15)Assert.Fail("Dynamic detour timed out.");yield return R04ShadowProbe.Frame();report.checkedFrames++;
                foreach(var solid in solids)if(Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,solid,solid.transform.position,solid.transform.rotation,out _,out var depth))report.maximumPenetration=Mathf.Max(report.maximumPenetration,depth);
            }
            report.passed=report.carved&&report.newClearance>report.oldClearance+.25f&&report.maximumPenetration<.015f&&motor.State==NavigationState.Arrived;
            File.WriteAllText(Evidence+"/Detour.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
        }
        [UnityTest]public IEnumerator E03_03_BlockedDoorStopsAndRemovalAllowsExplicitNewCommand()
        {
            yield return new EnterPlayMode();yield return Setup();Assert.That(motor.TryBind(new Vector3(0,0,-6)),Is.True);var destination=new Vector3(2,0,-1);
            var obstacle=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoObstacleSetup.PrefabPath),new Vector3(0,0,-4.1f),Quaternion.identity);yield return GameSeconds(.5f);
            var blocked=new NavMeshPath();motor.Agent.CalculatePath(destination,blocked);Assert.That(blocked.status,Is.Not.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(motor.TryMove(destination),Is.False);Assert.That(motor.State,Is.EqualTo(NavigationState.Unreachable));var stopped=motor.transform.position;yield return GameSeconds(.4f);Assert.That(Vector3.Distance(stopped,motor.transform.position),Is.LessThan(.01f));
            Object.Destroy(obstacle);yield return GameSeconds(.5f);Assert.That(motor.State,Is.EqualTo(NavigationState.Unreachable),"Removing an obstacle must not revive a cancelled goal.");Assert.That(motor.TryMove(destination),Is.True);
            var timer=System.Diagnostics.Stopwatch.StartNew();while(motor.State==NavigationState.Moving){if(timer.Elapsed.TotalSeconds>10)Assert.Fail("Restored doorway timed out.");yield return R04ShadowProbe.Frame();}
            var report=new Report{passed=motor.State==NavigationState.Arrived,recovered=motor.State==NavigationState.Arrived};File.WriteAllText(Evidence+"/DoorRecovery.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True);
        }
        [UnityTearDown]public IEnumerator TearDown()
        {
            if(Application.isPlaying)
            {
                if(motor)motor.Cancel();if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();
                while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Dynamic obstacle fixture unload timed out.");yield return null;}
                Application.runInBackground=background;yield return new ExitPlayMode();
            }
        }
    }
}
