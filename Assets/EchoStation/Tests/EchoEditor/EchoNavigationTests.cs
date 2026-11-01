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
    public class EchoNavigationTests
    {
        private Scene scene,previous;private bool background;private EchoNavigation motor;private BoxCollider[] environment;
        [Serializable]private class Trace{public bool passed;public Vector3[] positions;public int checkedFrames;public float maximumPenetration;public string finalState;}
        [UnitySetUp]public IEnumerator Setup()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);
            motor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath)).GetComponent<EchoNavigation>();
            environment=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox").GetComponentsInChildren<BoxCollider>().Where(c=>!c.isTrigger).ToArray();
        }
        [UnityTest]public IEnumerator E02_B01_ActualRobotCrossesBothDoorwaysWithoutWallPenetration()
        {
            Assert.That(motor.TryBind(new Vector3(0,0,-6)),Is.True);Assert.That(motor.TryMove(new Vector3(7.8f,0,2.2f)),Is.True);
            var timer=System.Diagnostics.Stopwatch.StartNew();var positions=new List<Vector3>();float maximum=0;var checkedFrames=0;var collider=motor.GetComponent<BoxCollider>();
            while(motor.State==NavigationState.Moving)
            {
                if(timer.Elapsed.TotalSeconds>15)Assert.Fail("Echo movement exceeded 15 seconds.");yield return R04ShadowProbe.Frame();checkedFrames++;
                if(positions.Count==0||Vector3.Distance(positions[positions.Count-1],motor.transform.position)>.02f)positions.Add(motor.transform.position);
                foreach(var obstacle in environment)if(Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out var depth))maximum=Mathf.Max(maximum,depth);
            }
            positions.Add(motor.transform.position);var report=new Trace{positions=positions.ToArray(),checkedFrames=checkedFrames,maximumPenetration=maximum,finalState=motor.State.ToString(),passed=motor.State==NavigationState.Arrived&&maximum<.015f};
            Directory.CreateDirectory(EchoNavigationSetup.Evidence);File.WriteAllText(EchoNavigationSetup.Evidence+"/MovementTrace.json",JsonUtility.ToJson(report,true));
            Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));Assert.That(Vector3.Distance(motor.transform.position,motor.Goal),Is.LessThan(.2f));Assert.That(positions.Any(p=>p.x>5.5f),Is.True);
            var camera=scene.GetRootGameObjects().Single(o=>o.name=="Cabin_ValidationCamera").GetComponent<Camera>();camera.scene=scene;camera.aspect=16f/9f;camera.transform.position=new Vector3(5.7f,1.65f,-1.6f);camera.transform.LookAt(new Vector3(8,1.1f,2.8f));
            yield return R04ShadowProbe.Frame();using var frames=new SceneFrameCapture(camera);frames.Capture(EchoNavigationSetup.Evidence+"/StorageArrival.png");
        }
        [UnityTest]public IEnumerator E02_B02_InvalidAndOffSurfaceTargetsRejectWithoutTeleport()
        {
            Assert.That(motor.Agent.enabled,Is.False);Assert.That(motor.TryMove(Vector3.zero),Is.False);Assert.That(motor.Failure,Is.EqualTo(NavigationFailure.NotBound));
            Assert.That(motor.TryBind(new Vector3(30,0,0)),Is.False);Assert.That(motor.Agent.enabled,Is.False);Assert.That(motor.TryBind(new Vector3(1.5f,0,-2)),Is.True);var original=motor.transform.position;
            Assert.That(motor.TryMove(new Vector3(float.NaN,0,0)),Is.False);Assert.That(motor.Failure,Is.EqualTo(NavigationFailure.InvalidPosition));
            Assert.That(motor.TryMove(new Vector3(30,0,0)),Is.False);Assert.That(motor.Failure,Is.EqualTo(NavigationFailure.NoNearbySurface));yield return R04ShadowProbe.Frame();Assert.That(motor.transform.position,Is.EqualTo(original));
        }
        [UnityTest]public IEnumerator E02_B03_DisconnectedRoofIslandRejectsPartialPath()
        {
            Assert.That(motor.TryBind(new Vector3(1.5f,0,-2)),Is.True);var before=motor.transform.position;var filter=new NavMeshQueryFilter{agentTypeID=motor.Agent.agentTypeID,areaMask=NavMesh.AllAreas};
            Assert.That(NavMesh.SamplePosition(new Vector3(0,3.4f,0),out var roof,.3f,filter),Is.True,"Independent roof navigation island is required for this case.");
            var path=new NavMeshPath();motor.Agent.CalculatePath(roof.position,path);Assert.That(path.status,Is.Not.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(motor.TryMove(roof.position),Is.False);Assert.That(motor.Failure,Is.EqualTo(NavigationFailure.IncompletePath));Assert.That(motor.Agent.hasPath,Is.False);
            yield return R04ShadowProbe.Frame();Assert.That(motor.transform.position,Is.EqualTo(before));
        }
        [UnityTest]public IEnumerator E02_B04_CancelClearsPathAndStopsActualMovement()
        {
            Assert.That(motor.TryBind(new Vector3(0,0,-6))&&motor.TryMove(new Vector3(7.8f,0,2.2f)),Is.True);
            var start=motor.transform.position;var timer=System.Diagnostics.Stopwatch.StartNew();
            while(Vector3.Distance(start,motor.transform.position)<.4f){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Robot never began moving before cancel.");yield return R04ShadowProbe.Frame();}
            motor.Cancel();var position=motor.transform.position;var stoppedAt=Time.time;timer.Restart();
            while(Time.time-stoppedAt<.5f){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("No game-time advance during stopped observation.");yield return R04ShadowProbe.Frame();}
            Assert.That(motor.State,Is.EqualTo(NavigationState.Idle));Assert.That(motor.Agent.hasPath,Is.False);Assert.That(Vector3.Distance(position,motor.transform.position),Is.LessThan(.01f));
        }
        [UnityTearDown]public IEnumerator TearDown()
        {
            if(Application.isPlaying)
            {
                if(motor)motor.Cancel();if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();
                while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Navigation fixture unload timed out.");yield return null;}
                Application.runInBackground=background;yield return new ExitPlayMode();
            }
        }
    }
}
