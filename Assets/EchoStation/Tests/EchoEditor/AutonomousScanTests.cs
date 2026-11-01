using System;
using System.Collections;
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
    public class AutonomousScanTests
    {
        private Scene scene,previous;private bool background;private EchoNavigation motor;private EchoScanController controller;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-E08";
        [Serializable]private class Report{public bool passed,hiddenVisible;public int discoveries;public float positionDrift;public string[] ids;}
        private IEnumerator Setup()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timed out.");yield return null;}SceneManager.SetActiveScene(scene);
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));motor=actor.GetComponent<EchoNavigation>();Assert.That(motor.TryBind(new Vector3(8.1f,0,2.1f)),Is.True);controller=actor.GetComponent<EchoScanController>();scene.GetPhysicsScene().Simulate(.02f);
        }
        private static IEnumerator Observe(float seconds)
        {
            var began=Time.time;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.time-began<seconds){if(timer.Elapsed.TotalSeconds>seconds+5)Assert.Fail("Observation game-time did not advance.");yield return R04ShadowProbe.Frame();}
        }
        [UnityTest]public IEnumerator E08_C03_FollowAtRestAutonomouslyScansNearbyCluesWithoutMovementOrRepeatedEvents()
        {
            yield return new EnterPlayMode();yield return Setup();Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var hidden),Is.True);Assert.That(hidden.GetComponent<ClueSource>().IsRevealed,Is.False);
            yield return Observe(.6f);Assert.That(controller.Scanner.Ledger.GetDiscoveredIds().Length,Is.Zero,"No autonomous source is enabled until explicitly bound.");
            var report=new Report();controller.Scanner.Ledger.Discovered+=receipt=>report.discoveries++;var original=motor.Position;controller.BindAutonomousMode(()=>IntentKind.Follow);var timer=System.Diagnostics.Stopwatch.StartNew();
            while(!hidden.GetComponent<ClueSource>().IsRevealed)
            {if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Automatic scan did not discover hidden marker: "+controller.Scanner.LastRequestFailure+" / "+controller.Scanner.LastOccluderName);yield return R04ShadowProbe.Frame();}
            var events=report.discoveries;yield return Observe(1.5f);Assert.That(report.discoveries,Is.EqualTo(events));report.hiddenVisible=hidden.GetComponent<MeshRenderer>().enabled;report.positionDrift=Vector3.Distance(original,motor.Position);report.ids=controller.Scanner.Ledger.GetDiscoveredIds();
            report.passed=report.hiddenVisible&&controller.Scanner.Ledger.HasDiscovered("scan_mark_01")&&report.positionDrift<.01f;Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/AutonomousScan.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True);
        }
        [UnityTest]public IEnumerator E08_C04_LeavingFollowCancelsOwnedAutomaticScanBeforeDiscovery()
        {
            yield return new EnterPlayMode();yield return Setup();IntentKind? mode=IntentKind.Follow;controller.BindAutonomousMode(()=>mode);var timer=System.Diagnostics.Stopwatch.StartNew();
            while(!controller.Scanner.IsBusy){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Automatic scanner did not start.");yield return R04ShadowProbe.Frame();}
            var id=controller.Scanner.CurrentTarget.Id;mode=IntentKind.Wait;yield return Observe(.8f);Assert.That(controller.Scanner.Phase,Is.EqualTo(ScanPhase.Cancelled));Assert.That(controller.Scanner.Ledger.HasDiscovered(id),Is.False);Assert.That(motor.State,Is.EqualTo(NavigationState.Idle));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying)
            {
                if(controller)controller.BindAutonomousMode(null);if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Automatic scan fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();
            }
        }
    }
}
