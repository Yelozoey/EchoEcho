using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public class WaitNodeTests
    {
        private sealed class Port:IMovementPort
        {
            public Vector3 Position=>Vector3.zero;public NavigationState State{get;set;}=NavigationState.Moving;public int Moves,Stops;
            public bool TryMove(Vector3 destination){Moves++;State=NavigationState.Moving;return true;}public void Cancel(){Stops++;State=NavigationState.Idle;}
        }
        [Test]public void E09_01_EnteringWaitClearsMovementAndNeverSendsFollowRequests()
        {
            var port=new Port();using var root=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Wait,()=>new WaitNode()}});var dispatcher=new IntentDispatcher(new TargetRegistry(),root);
            Assert.That(dispatcher.TryDispatch(ScriptCommands.Wait(),out _),Is.True);Assert.That(port.State,Is.EqualTo(NavigationState.Idle));for(var i=0;i<100;i++)root.Tick(.1f);
            Assert.That(port.Moves,Is.Zero);Assert.That(port.Stops,Is.EqualTo(1));Assert.That(root.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(root.Status,Is.EqualTo(BehaviorTreeStatus.Running));
            port.State=NavigationState.Moving;root.Tick(.1f);Assert.That(port.State,Is.EqualTo(NavigationState.Idle));Assert.That(port.Stops,Is.EqualTo(2));
        }
        [Test]public void E09_02_LeavingWaitAllowsNewOwnerAndOldNodeCannotCancelIt()
        {
            var port=new Port();using var root=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Wait,()=>new WaitNode()},{IntentKind.Follow,()=>new FollowNode(()=>new Vector3(10,0,0))}});var dispatcher=new IntentDispatcher(new TargetRegistry(),root);
            dispatcher.TryDispatch(ScriptCommands.Wait(),out _);root.Tick(.1f);dispatcher.TryDispatch(ScriptCommands.Follow(),out _);root.Tick(.1f);var stopped=port.Stops;
            for(var i=0;i<10;i++)root.Tick(.1f);Assert.That(port.State,Is.EqualTo(NavigationState.Moving));Assert.That(port.Stops,Is.EqualTo(stopped));Assert.That(port.Moves,Is.EqualTo(1));
        }
        private Scene scene,previous;private bool background;private IntentBehaviorTree tree;private EchoNavigation motor;
        [Serializable]private class Report{public bool passed,pathCleared;public float movedBeforeWait,waitDrift,movedAfterNewCommand;public string heldMode;}
        [UnityTest]public IEnumerator E09_03_ActualMovingRobotHoldsWaitDespiteTargetMotionAndResumesOnlyOnCommand()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));var loading=System.Diagnostics.Stopwatch.StartNew();
            while(!scene.isLoaded){if(loading.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timed out.");yield return null;}SceneManager.SetActiveScene(scene);var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));motor=actor.GetComponent<EchoNavigation>();Assert.That(motor.TryBind(new Vector3(0,0,-6)),Is.True);scene.GetPhysicsScene().Simulate(.02f);
            var target=new Vector3(2,.05f,-1);tree=new IntentBehaviorTree(motor,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>new FollowNode(()=>target)},{IntentKind.Wait,()=>new WaitNode()}});actor.GetComponent<EchoScanController>().BindAutonomousMode(()=>tree?.CurrentMode);var dispatcher=new IntentDispatcher(WorldTarget.Active,tree);
            Assert.That(dispatcher.TryDispatch(ScriptCommands.Follow(),out _),Is.True);var start=motor.Position;var timer=System.Diagnostics.Stopwatch.StartNew();
            while(Vector3.Distance(start,motor.Position)<.5f){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Robot did not start following.");tree.Tick(Time.deltaTime);yield return R04ShadowProbe.Frame();}
            var report=new Report{movedBeforeWait=Vector3.Distance(start,motor.Position)};Assert.That(dispatcher.TryDispatch(ScriptCommands.Wait(),out _),Is.True);var held=motor.Position;var waitedAt=Time.time;timer.Restart();
            while(Time.time-waitedAt<.75f)
            {if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Wait observation timed out.");target=new Vector3(2+Mathf.Sin(Time.time),.05f,-1);tree.Tick(Time.deltaTime);yield return R04ShadowProbe.Frame();report.waitDrift=Mathf.Max(report.waitDrift,Vector3.Distance(held,motor.Position));}
            report.heldMode=tree.CurrentMode.ToString();report.pathCleared=!motor.Agent.hasPath&&motor.State==NavigationState.Idle;Assert.That(tree.CurrentMode,Is.EqualTo(IntentKind.Wait));
            target=new Vector3(7.8f,.05f,2.2f);Assert.That(dispatcher.TryDispatch(ScriptCommands.Follow(),out _),Is.True);timer.Restart();
            while(Vector3.Distance(held,motor.Position)<.3f){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("New Follow command did not resume movement.");tree.Tick(Time.deltaTime);yield return R04ShadowProbe.Frame();}
            report.movedAfterNewCommand=Vector3.Distance(held,motor.Position);report.passed=report.movedBeforeWait>=.5f&&report.waitDrift<.01f&&report.pathCleared&&report.movedAfterNewCommand>=.3f;
            var evidence="Docs/Tests/Stage1/Evidence/S1-E09";Directory.CreateDirectory(evidence);File.WriteAllText(evidence+"/ActualWait.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
        }
        [UnityTearDown]public IEnumerator After()
        {
            tree?.Dispose();tree=null;if(Application.isPlaying)
            {if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Wait fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
