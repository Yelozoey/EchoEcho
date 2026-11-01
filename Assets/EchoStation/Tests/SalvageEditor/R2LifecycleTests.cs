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
    public class R2LifecycleTests
    {
        Scene old,scene;bool background;SalvageMotor motor;SalvageCommandHub hub;SalvageEchoCoordinator brain;SalvageEcho echo;
        IEnumerator Boot(){Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Lifecycle load timeout");yield return null;}SceneManager.SetActiveScene(scene);yield return null;motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.GetComponent<PlayerInputReader>().SetApplicationFocus(true);motor.GetComponent<PlayerInputReader>().SetPointerCaptured(true);hub=motor.GetComponent<SalvageCommandHub>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);brain.ResetBase(IntentKind.Wait);motor.RestorePose(new Vector3(20,.1f,15),0,0);Assert.That(echo.Navigation.TryBind(new Vector3(20,0,18)),Is.True);echo.transform.rotation=Quaternion.Euler(0,90,0);Physics.SyncTransforms();}
        void Tick(float dt=.1f)=>brain.Advance(dt,false);
        [UnityTest]public IEnumerator DangerPreemptsAndOnlyResumesStillValidInvestigation()
        {
            yield return new EnterPlayMode();yield return Boot();hub.Known.CommitValidatedSensor("cargo_b");Assert.That(brain.Command(ScriptCommands.Investigate("cargo_b")),Is.True);Tick();Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Warn));Assert.That(echo.GetComponent<EchoPresentation>().Mood,Is.EqualTo(EchoMood.Alert));Assert.That(brain.WarningCount,Is.EqualTo(1));Assert.That(hub.Known.Count,Is.EqualTo(1),"Warning is not a scanned discovery");for(int i=0;i<16;i++){yield return null;Tick();}Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Investigate));Assert.That(brain.CurrentTargetId,Is.EqualTo("cargo_b"));Assert.That(brain.BaseMode,Is.EqualTo(IntentKind.Wait));
        }
        [UnityTest]public IEnumerator NewPlayerModeOverridesWarningWithoutOldTaskResurrection()
        {
            yield return new EnterPlayMode();yield return Boot();hub.Known.CommitValidatedSensor("cargo_b");Assert.That(brain.Command(ScriptCommands.Investigate("cargo_b")),Is.True);Tick();Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Warn));ulong generation=brain.Generation;Assert.That(brain.Command(ScriptCommands.Follow()),Is.True);for(int i=0;i<20;i++){yield return null;Tick();}Assert.That(brain.Generation,Is.GreaterThan(generation));Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(brain.CurrentTargetId,Is.Null);Assert.That(brain.WarningCount,Is.EqualTo(1));Assert.That(hub.Known.Count,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator TakenSuspendedTargetIsNotResumedAndNewRunClearsKnowledgeAndText()
        {
            yield return new EnterPlayMode();yield return Boot();hub.Known.CommitValidatedSensor("cargo_b");Assert.That(brain.Command(ScriptCommands.Investigate("cargo_b")),Is.True);Tick();Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Warn));motor.RestorePose(new Vector3(14,.1f,18),90,35);Physics.SyncTransforms();Assert.That(hub.GetComponent<SalvageCarry>().TryPickFromView(),Is.True);for(int i=0;i<17;i++){yield return null;Tick();}Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(brain.CurrentTargetId,Is.Null);
            Assert.That(hub.Submit("电量"),Is.True);var mission=hub.GetComponent<SalvageMission>();Assert.That(mission.OpenBoard()&&mission.RequestRestart()&&mission.ConfirmRestart(),Is.True);Assert.That(hub.Known.Count,Is.Zero);Assert.That(hub.History.Messages,Is.Empty);Assert.That(brain.BaseMode,Is.EqualTo(IntentKind.Follow));Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(brain.WarningCount,Is.Zero);Assert.That(hub.GetComponent<SalvageCarry>().HeldId,Is.Null);Assert.That(echo.Battery.Charge,Is.EqualTo(100));Assert.That(echo.GetComponent<SalvageScanner>().Busy,Is.False);
        }
        [UnityTest]public IEnumerator DangerBehindWallOrOutsideViewDoesNotWarn()
        {
            yield return new EnterPlayMode();yield return Boot();echo.transform.rotation=Quaternion.Euler(0,270,0);Tick();Assert.That(brain.WarningCount,Is.Zero);echo.transform.rotation=Quaternion.Euler(0,90,0);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(wall,scene);wall.transform.position=new Vector3(21,1,18);wall.transform.localScale=new Vector3(.2f,2,2);Physics.SyncTransforms();for(int i=0;i<5;i++){yield return null;Tick();}Assert.That(brain.WarningCount,Is.Zero);Assert.That(hub.Known.Count,Is.Zero);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
