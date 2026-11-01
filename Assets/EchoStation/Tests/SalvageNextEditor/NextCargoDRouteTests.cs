using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextCargoDRouteTests
    {
        Scene old,main;RoomSceneOwner owner;bool background;
        static IEnumerator Wait(Func<bool> condition,string stage){var watch=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(watch.Elapsed.TotalSeconds,Is.LessThan(20),stage);yield return null;}}
        [UnityTest]public IEnumerator ActualWorldCenterAimingAndUnoccupiedApproachAllowDScan(){yield return new EnterPlayMode();yield return Run(false);}
        [UnityTest]public IEnumerator RestoredWorldStillAllowsDScanFromSameClearStandpoint(){yield return new EnterPlayMode();yield return Run(true);}
        IEnumerator Run(bool restore)
        {
            Assert.That(Application.isPlaying,Is.True,"F048/F199: EnterPlayMode must be yielded by the test root.");background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();main=EditorSceneManager.LoadSceneInPlayMode(NextRoomPartition.Main,new LoadSceneParameters(LoadSceneMode.Additive));yield return Wait(()=>main.isLoaded,"Scene load");SceneManager.SetActiveScene(main);var stream=main.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NextRoomStreaming>()).Single();owner=stream.Owner;var motor=stream.GetComponent<SalvageMotor>();motor.enabled=false;var session=stream.GetComponent<NextSession>();var input=stream.GetComponent<PlayerInputReader>();var hub=stream.GetComponent<SalvageCommandHub>();var echo=main.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();var brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;brain.AutomaticWarnings=false;Assert.That(brain.Initialize(),Is.True);brain.ResetBase(IntentKind.Wait);yield return Wait(()=>!stream.Blocking,"Initial");Assert.That(echo.Navigation.Agent.Warp(new Vector3(16,0,18)),Is.True);
            if(restore){var seed=session.Capture();seed.World.Collaboration.Discovered=new[]{"cargo_a","cargo_b","cargo_c"};foreach(var cargo in seed.World.Cargo.Where(c=>c.Id=="cargo_a"||c.Id=="cargo_b"))cargo.State=2;Assert.That(session.TryRestore(seed,out var error),Is.True,error);}
            var target=hub.Targets.Single(t=>t.Id=="cargo_d");var actor=target.GetComponent<CargoActor>();var position=new Vector3(25,.03f,11.908f);motor.RestorePose(position,90,0);var delta=actor.Body.bounds.center-motor.View.position;motor.RestorePose(position,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);Physics.SyncTransforms();stream.EvaluateNow();yield return Wait(()=>!stream.Blocking,"D room ready");input.SetApplicationFocus(true);input.SetPointerCaptured(true);
            var approach=target.GetComponent<EchoTargetPermissions>().ApproachPoint.position;Assert.That(Vector3.Distance(position,approach),Is.GreaterThan(2));Assert.That(hub.PointedId(),Is.EqualTo("cargo_d"));Assert.That(hub.Select(IntentKind.Investigate,"cargo_d"),Is.True);motor.RestorePose(new Vector3(26,.03f,14),90,0);Physics.SyncTransforms();brain.enabled=true;var watch=System.Diagnostics.Stopwatch.StartNew();
            while(watch.Elapsed.TotalSeconds<20&&brain.LastFailure==null&&!(hub.Known.Contains("cargo_d")&&brain.CurrentMode!=IntentKind.Investigate))yield return null;
            {string diagFolder="Docs/Tests/SalvagePrototype/Evidence/R4-P01c-1";Directory.CreateDirectory(diagFolder);var scanner=echo.GetComponent<SalvageScanner>();var rig=echo.GetComponent<EchoRig>();File.WriteAllText(diagFolder+"/Diagnostic_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"restored",restore},{"seconds",watch.Elapsed.TotalSeconds},{"mode",brain.CurrentMode.ToString()},{"target",brain.CurrentTargetId},{"lastFailure",brain.LastFailure},{"scannerError",scanner.LastError},{"phase",scanner.Engine?.Phase.ToString()},{"scanFailure",scanner.Engine?.Failure.ToString()},{"requestFailure",scanner.Engine?.LastRequestFailure.ToString()},{"timeScale",Time.timeScale},{"delta",Time.deltaTime},{"externalControl",echo.ExternalControl},{"initialized",echo.Initialized},{"echoEnabled",echo.enabled},{"brainEnabled",brain.enabled},{"inputEnabled",input.enabled},{"focus",input.HasApplicationFocus},{"uiBlocked",input.UiBlocked},{"blocking",stream.Blocking},{"navState",echo.Navigation.State.ToString()},{"robot",echo.transform.position.ToString("F4")},{"goal",echo.Navigation.Goal.ToString("F4")},{"scanOrigin",rig.ScanOrigin.position.ToString("F4")},{"scanForward",rig.ScanOrigin.forward.ToString("F4")},{"approach",approach.ToString("F4")},{"paid",scanner.PaidDiscoveries},{"known",hub.Known.Contains("cargo_d")}}.ToString());}
            Assert.That(hub.Known.Contains("cargo_d"),Is.True,"D scan failed: "+brain.LastFailure);Assert.That(brain.LastFailure,Is.Null);Assert.That(echo.GetComponent<SalvageScanner>().PaidDiscoveries,Is.EqualTo(1));Assert.That(stream.GetComponent<SalvageCarry>().Registry.Get("cargo_d").State,Is.EqualTo(CargoState.World));
            string folder="Docs/Tests/SalvagePrototype/Evidence/R4-P01c-1";Directory.CreateDirectory(folder);File.WriteAllText(folder+"/D_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"restored",restore},{"seconds",watch.Elapsed.TotalSeconds},{"actualTarget","cargo_d"},{"scanPaid",1},{"approach",approach.ToString("F4")},{"player",position.ToString("F4")},{"bodyBounds",actor.Body.bounds.size.ToString("F4")},{"worldUnchanged",true}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(main.IsValid()&&main.isLoaded){var op=SceneManager.UnloadSceneAsync(main);while(!op.isDone)yield return null;}if(owner!=null)yield return Wait(()=>owner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Cleanup");Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
