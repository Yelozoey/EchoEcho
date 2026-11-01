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
    public class R2NavigationTests
    {
        Scene old,scene;bool background;SalvageMotor motor;SalvageCommandHub hub;SalvageEchoCoordinator brain;SalvageEcho echo;
        IEnumerator Boot(){Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Navigation load timeout");yield return null;}SceneManager.SetActiveScene(scene);yield return null;motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.GetComponent<PlayerInputReader>().SetApplicationFocus(true);motor.GetComponent<PlayerInputReader>().SetPointerCaptured(true);hub=motor.GetComponent<SalvageCommandHub>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);Assert.That(echo.Initialized,Is.True);}
        IEnumerator AdvanceUntil(Func<bool> done,float limit){var timer=System.Diagnostics.Stopwatch.StartNew();while(!done()){if(timer.Elapsed.TotalSeconds>limit)Assert.Fail("Navigation timeout position="+echo.transform.position+" goal="+echo.Navigation.Goal+" state="+echo.Navigation.State+" failure="+brain.LastFailure);yield return null;brain.Advance(Mathf.Min(Time.deltaTime,.1f),false);}}
        [UnityTest]public IEnumerator ActualInvestigationCrossesDoorAndArrivesBeforeCallback()
        {
            yield return new EnterPlayMode();yield return Boot();hub.Known.CommitValidatedSensor("cargo_b");brain.ResetBase(IntentKind.Wait);Assert.That(echo.Navigation.TryBind(new Vector3(8,0,18)),Is.True);var start=echo.transform.position;bool callback=false;brain.SensorStep=t=>{Assert.That(Vector3.Distance(Vector3.ProjectOnPlane(echo.transform.position-t.GetComponent<EchoTargetPermissions>().ApproachPoint.position,Vector3.up),Vector3.zero),Is.LessThan(.25f));callback=true;return NodeStatus.Success;};
            Assert.That(brain.Command(ScriptCommands.Investigate("cargo_b")),Is.True);Assert.That(callback,Is.False);yield return AdvanceUntil(()=>callback,14);Assert.That(echo.transform.position.x,Is.GreaterThan(12.5f));Assert.That(Vector3.Distance(start,echo.transform.position),Is.GreaterThan(6));Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(hub.Known.Count,Is.EqualTo(1));Assert.That(hub.GetComponent<SalvageDelivery>().Count,Is.Zero);
        }
        [UnityTest]public IEnumerator InvalidApproachFailsAndNewCommandCancelsWithoutArrival()
        {
            yield return new EnterPlayMode();yield return Boot();hub.Known.CommitValidatedSensor("cargo_a");brain.ResetBase(IntentKind.Wait);var target=hub.Targets.Single(t=>t.Id=="cargo_a");var approach=target.GetComponent<EchoTargetPermissions>().ApproachPoint;var initial=approach.position;approach.position=new Vector3(-20,0,-20);Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);brain.Advance(.1f,false);Assert.That(brain.LastFailure,Is.Not.Null);Assert.That(echo.Navigation.State,Is.EqualTo(NavigationState.Idle));Assert.That(brain.LastArrivedId,Is.Null);approach.position=initial;
            bool callback=false;brain.SensorStep=t=>{callback=true;return NodeStatus.Success;};Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);brain.Advance(.1f,false);Assert.That(brain.Command(ScriptCommands.Wait()),Is.True);for(int i=0;i<10;i++){yield return null;brain.Advance(.1f,false);}Assert.That(callback,Is.False);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(hub.GetComponent<SalvageCarry>().Registry.Get("cargo_a").State,Is.EqualTo(CargoState.World));
        }
        [UnityTest]public IEnumerator PickupInvalidatesInvestigationAndPauseDoesNotConsumeOrTeleport()
        {
            yield return new EnterPlayMode();yield return Boot();motor.RestorePose(new Vector3(9,.1f,17),0,35);Physics.SyncTransforms();Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);float charge=echo.Battery.Charge;var position=echo.transform.position;brain.Advance(1,true);Assert.That(echo.Battery.Charge,Is.EqualTo(charge));Assert.That(echo.transform.position,Is.EqualTo(position));Assert.That(motor.GetComponent<SalvageCarry>().TryPickFromView(),Is.True);brain.Advance(.1f,false);Assert.That(brain.LastFailure,Is.Not.Null);Assert.That(brain.LastArrivedId,Is.Null);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(hub.GetComponent<SalvageCarry>().HeldId,Is.EqualTo("cargo_a"));
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
