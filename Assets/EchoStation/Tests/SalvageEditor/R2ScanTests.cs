using System;
using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.Salvage.Tests
{
    public class R2ScanTests
    {
        Scene old,scene;bool background;SalvageMotor motor;SalvageCommandHub hub;SalvageEchoCoordinator brain;SalvageEcho echo;SalvageScanner scanner;
        IEnumerator Boot(){Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Scan load timeout");yield return null;}SceneManager.SetActiveScene(scene);yield return null;motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.GetComponent<PlayerInputReader>().SetApplicationFocus(true);motor.GetComponent<PlayerInputReader>().SetPointerCaptured(true);hub=motor.GetComponent<SalvageCommandHub>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);Assert.That(echo.Initialized,Is.True);scanner=echo.GetComponent<SalvageScanner>();scanner.Initialize();brain.ResetBase(IntentKind.Wait);motor.RestorePose(new Vector3(7,.1f,19),90,35);Assert.That(echo.Navigation.TryBind(new Vector3(9,0,17.5f)),Is.True);Physics.SyncTransforms();}
        IEnumerator Until(Func<bool> done,float max=8){var timer=System.Diagnostics.Stopwatch.StartNew();while(!done()){if(timer.Elapsed.TotalSeconds>max)Assert.Fail("Scan timeout phase="+scanner.Engine.Phase+" error="+scanner.LastError+" position="+echo.transform.position);yield return null;brain.Advance(Mathf.Min(Time.deltaTime,.1f),false);}}
        [UnityTest]public IEnumerator RealSensorCommitsOnceAndOnlyFirstDiscoveryPaysEight()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(hub.PointedId(),Is.EqualTo("cargo_a"));float before=echo.Battery.Charge;Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);yield return Until(()=>hub.Known.Contains("cargo_a"));Assert.That(scanner.PaidDiscoveries,Is.EqualTo(1));Assert.That(before-echo.Battery.Charge,Is.InRange(8f,8.5f));Assert.That(hub.Known.Summary("cargo_a"),Is.EqualTo(SalvageTargetCatalog.Items[0].Summary));Assert.That(hub.GetComponent<SalvageDelivery>().Count,Is.Zero);Assert.That(hub.GetComponent<SalvageCarry>().Registry.Get("cargo_a").State,Is.EqualTo(CargoState.World));
            yield return Until(()=>brain.CurrentMode==IntentKind.Wait);float repeat=echo.Battery.Charge;Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);yield return Until(()=>brain.CurrentMode==IntentKind.Wait);Assert.That(scanner.PaidDiscoveries,Is.EqualTo(1));Assert.That(repeat-echo.Battery.Charge,Is.LessThan(.5f));
        }
        [UnityTest]public IEnumerator CancelAndInsufficientChargeNeverCommitStaleResult()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);yield return Until(()=>scanner.Busy);float before=echo.Battery.Charge;brain.CancelCurrent();for(int i=0;i<15;i++){yield return null;brain.Advance(.1f,false);}Assert.That(hub.Known.Count,Is.Zero);Assert.That(scanner.PaidDiscoveries,Is.Zero);Assert.That(before-echo.Battery.Charge,Is.LessThan(.2f));
            echo.Battery.Restore(8.01f,true);Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);yield return Until(()=>brain.CurrentMode==IntentKind.Wait);Assert.That(hub.Known.Count,Is.Zero);Assert.That(echo.Battery.Charge,Is.GreaterThanOrEqualTo(0));Assert.That(scanner.PaidDiscoveries,Is.Zero);
        }
        [UnityTest]public IEnumerator OcclusionAndTargetPickupDuringScanDoNotPublish()
        {
            yield return new EnterPlayMode();yield return Boot();var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(wall,scene);wall.transform.position=new Vector3(9,.6f,18.4f);wall.transform.localScale=new Vector3(1.3f,1.2f,.15f);Physics.SyncTransforms();Assert.That(scanner.Engine.Eligibility(hub.Targets.Single(t=>t.Id=="cargo_a").World),Is.EqualTo(ScanFailure.Obstructed));Assert.That(scanner.Engine.TryBegin(hub.Targets.Single(t=>t.Id=="cargo_a").World),Is.False);Assert.That(hub.Known.Count,Is.Zero);UnityEngine.Object.Destroy(wall);yield return null;
            Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);yield return Until(()=>scanner.Busy);motor.RestorePose(new Vector3(7,.1f,19),90,35);Physics.SyncTransforms();Assert.That(motor.GetComponent<SalvageCarry>().TryPickFromView(),Is.True);brain.Advance(.1f,false);Assert.That(hub.Known.Count,Is.Zero);Assert.That(scanner.PaidDiscoveries,Is.Zero);Assert.That(scanner.Busy,Is.False);
        }
        [UnityTest]public IEnumerator SensorRejectsDistanceAndExtremePitch()
        {
            yield return new EnterPlayMode();yield return Boot();var target=hub.Targets.Single(t=>t.Id=="cargo_b").World;Assert.That(scanner.Engine.Eligibility(target),Is.EqualTo(ScanFailure.OutOfRange));var a=hub.Targets.Single(t=>t.Id=="cargo_a").World;var p=a.transform.position;a.transform.position=echo.GetComponent<EchoRig>().ScanOrigin.position+Vector3.up*1.5f;Physics.SyncTransforms();Assert.That(scanner.Engine.Eligibility(a),Is.EqualTo(ScanFailure.OutOfView));a.transform.position=p;Assert.That(hub.Known.Count,Is.Zero);Assert.That(scanner.PaidDiscoveries,Is.Zero);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
