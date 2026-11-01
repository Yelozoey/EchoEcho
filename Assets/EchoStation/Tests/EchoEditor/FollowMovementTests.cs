using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class FollowMovementTests
    {
        private Scene scene,previous;private bool background;private IntentBehaviorTree tree;private EchoNavigation motor;private Transform target;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-E06";
        [Serializable]private class Result{public bool passed,cancelledHeld;public float minimumDistance,maximumPenetration,holdingDrift,cancelledDrift;public int checkedFrames;public Vector3[] settledPositions;public string mode;}
        [UnityTest]public IEnumerator E06_B01_ActualFollowCrossesRoomsHoldsDistanceAndStopsAfterCancel()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var loading=System.Diagnostics.Stopwatch.StartNew();
            while(!scene.isLoaded){if(loading.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timed out.");yield return null;}SceneManager.SetActiveScene(scene);
            motor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath)).GetComponent<EchoNavigation>();Assert.That(motor.TryBind(new Vector3(0,0,-6)),Is.True);
            target=new GameObject("FollowTargetFixture").transform;target.position=new Vector3(2,.05f,-1);var player=target.gameObject.AddComponent<CapsuleCollider>();player.radius=.3f;player.height=1.75f;player.center=new Vector3(0,.875f,0);
            tree=new IntentBehaviorTree(motor,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>new FollowNode(()=>target?target.position:(Vector3?)null)}});
            var dispatcher=new IntentDispatcher(WorldTarget.Active,tree);Assert.That(dispatcher.TryDispatch(ScriptCommands.Follow(),out _),Is.True);
            var environment=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox").GetComponentsInChildren<BoxCollider>().Where(c=>!c.isTrigger).Cast<Collider>().Concat(new[]{player}).ToArray();var box=motor.GetComponent<BoxCollider>();
            var report=new Result{minimumDistance=float.MaxValue};var settled=new List<Vector3>();Directory.CreateDirectory(Evidence);
            foreach(var destination in new[]{new Vector3(2,.05f,-1),new Vector3(7.8f,.05f,2.2f)})
            {
                target.position=destination;var timer=System.Diagnostics.Stopwatch.StartNew();
                while(true)
                {
                    if(timer.Elapsed.TotalSeconds>15){report.mode=motor.State.ToString();File.WriteAllText(Evidence+"/ActualFollow.json",JsonUtility.ToJson(report,true));Assert.Fail("Actual follow failed to settle near target.");}
                    tree.Tick(Time.deltaTime);yield return R04ShadowProbe.Frame();report.checkedFrames++;var delta=target.position-motor.Position;delta.y=0;report.minimumDistance=Mathf.Min(report.minimumDistance,delta.magnitude);
                    foreach(var solid in environment)if(Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,solid,solid.transform.position,solid.transform.rotation,out _,out var depth))report.maximumPenetration=Mathf.Max(report.maximumPenetration,depth);
                    if(motor.State!=NavigationState.Moving&&delta.magnitude<=2.2f){settled.Add(motor.Position);break;}
                }
                var held=motor.Position;var started=Time.time;timer.Restart();
                while(Time.time-started<.5f){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Follow holding interval timed out.");tree.Tick(Time.deltaTime);yield return R04ShadowProbe.Frame();report.holdingDrift=Mathf.Max(report.holdingDrift,Vector3.Distance(held,motor.Position));}
                Assert.That(tree.CurrentMode,Is.EqualTo(IntentKind.Follow));
            }
            Assert.That(motor.Position.x,Is.GreaterThan(5.5f));tree.Cancel();var afterCancel=motor.Position;target.position=new Vector3(0,.05f,-6);var cancelledAt=Time.time;var cancelledTimer=System.Diagnostics.Stopwatch.StartNew();
            while(Time.time-cancelledAt<.5f){if(cancelledTimer.Elapsed.TotalSeconds>5)Assert.Fail("Cancelled observation timed out.");tree.Tick(Time.deltaTime);yield return R04ShadowProbe.Frame();report.cancelledDrift=Mathf.Max(report.cancelledDrift,Vector3.Distance(afterCancel,motor.Position));}
            report.cancelledHeld=tree.Status==BehaviorTreeStatus.Cancelled&&motor.State==NavigationState.Idle;report.settledPositions=settled.ToArray();report.mode=tree.Status.ToString();
            report.passed=report.cancelledHeld&&report.maximumPenetration<.015f&&report.minimumDistance>=1.2f&&report.holdingDrift<.02f&&report.cancelledDrift<.02f;File.WriteAllText(Evidence+"/ActualFollow.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
        }
        [UnityTearDown]public IEnumerator After()
        {
            tree?.Dispose();tree=null;
            if(Application.isPlaying)
            {
                if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();
                while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Follow fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();
            }
        }
    }
}
