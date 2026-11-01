using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Dialogue;
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
    public class R2SaveLifecycleTests
    {
        Scene old,scene;bool background;string folder;SalvageMotor motor;SalvageCommandHub hub;SalvageEchoCoordinator brain;SalvageEcho echo;SalvagePersistence save;
        IEnumerator Boot(){Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Save load timeout");yield return null;}SceneManager.SetActiveScene(scene);yield return null;motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.GetComponent<PlayerInputReader>().SetApplicationFocus(true);motor.GetComponent<PlayerInputReader>().SetPointerCaptured(true);hub=motor.GetComponent<SalvageCommandHub>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);brain.AutomaticWarnings=false;save=motor.GetComponent<SalvagePersistence>();folder=Path.GetFullPath("Library/EchoStation/R2SceneSaveTests/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);save.ConfigureTestStore(folder);}
        [UnityTest]public IEnumerator RestoreKeepsDiscoveryBaseModeAndFiftyMessagesButCancelsScanAndDraft()
        {
            yield return new EnterPlayMode();yield return Boot();motor.RestorePose(new Vector3(7,.1f,19),90,35);Assert.That(echo.Navigation.TryBind(new Vector3(9,0,17.5f)),Is.True);Physics.SyncTransforms();hub.Known.CommitValidatedSensor("cargo_b");for(int i=0;i<60;i++)Assert.That(hub.Submit("电量"),Is.True);Assert.That(hub.History.Messages.Count,Is.EqualTo(100));brain.ResetBase(IntentKind.Wait);var data=save.Capture();Assert.That(data.Collaboration.Messages.Length,Is.EqualTo(50));Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);var timer=System.Diagnostics.Stopwatch.StartNew();while(!echo.GetComponent<SalvageScanner>().Busy){if(timer.Elapsed.TotalSeconds>6)Assert.Fail("No active scan");yield return null;brain.Advance(Mathf.Min(Time.deltaTime,.1f),false);}var panel=hub.GetComponent<SalvageCommandPanel>();Assert.That(panel.Open(true),Is.True);panel.InputField.text="未提交草稿";ulong generation=brain.Generation;
            Assert.That(save.TryRestore(data,out var error),Is.True,error);Assert.That(brain.Generation,Is.GreaterThan(generation));Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(brain.CurrentTargetId,Is.Null);Assert.That(echo.GetComponent<SalvageScanner>().Busy,Is.False);Assert.That(panel.IsOpen,Is.False);Assert.That(panel.InputField.text,Is.Empty);Assert.That(hub.Known.Snapshot(),Is.EqualTo(new[]{"cargo_b"}));Assert.That(hub.History.Messages.Count,Is.EqualTo(50));for(int i=0;i<15;i++){yield return null;brain.Advance(.1f,false);}Assert.That(hub.Known.Count,Is.EqualTo(1));Assert.That(echo.GetComponent<SalvageScanner>().PaidDiscoveries,Is.Zero);
        }
        [UnityTest]public IEnumerator InvalidRestoreDoesNotCancelAndRealSubscriberFailureRollsBackAllHundredMessages()
        {
            yield return new EnterPlayMode();yield return Boot();brain.ResetBase(IntentKind.Wait);hub.Known.CommitValidatedSensor("cargo_a");for(int i=0;i<60;i++)hub.Submit("电量");var original=hub.History;var before=SalvageSaveStore.Encode(save.Capture());var invalid=SalvageSaveStore.Decode(before);invalid.Collaboration.Discovered=new[]{"cargo_heavy"};ulong generation=brain.Generation;Assert.That(save.TryRestore(invalid,out _),Is.False);Assert.That(brain.Generation,Is.EqualTo(generation));Assert.That(hub.History,Is.SameAs(original));Assert.That(SalvageSaveStore.Encode(save.Capture()),Is.EqualTo(before));
            var valid=SalvageSaveStore.Decode(before);valid.Collaboration.Discovered=Array.Empty<string>();valid.Collaboration.Messages=Array.Empty<SavedDialogue>();bool first=true;Action failOnce=()=>{if(first){first=false;throw new InvalidOperationException("Controlled downstream UI subscriber failure");}};hub.Changed+=failOnce;try{Assert.That(save.TryRestore(valid,out var error),Is.False);Assert.That(error,Does.Contain("已回滚"));}finally{hub.Changed-=failOnce;}Assert.That(hub.History,Is.SameAs(original));Assert.That(hub.History.Messages.Count,Is.EqualTo(100));Assert.That(SalvageSaveStore.Encode(save.Capture()),Is.EqualTo(before));
        }
        [UnityTest]public IEnumerator ActualR1MixedSlotMigratesInGameAndOriginalBytesSurvive()
        {
            yield return new EnterPlayMode();yield return Boot();var bytes=File.ReadAllBytes("Docs/Tests/SalvagePrototype/Fixtures/R1_v1_Mixed.json");File.WriteAllBytes(save.Store.Primary,bytes);Assert.That(save.Load(),Is.True,hub.GetComponent<SalvageMission>().Notice);Assert.That(File.ReadAllBytes(save.Store.LegacyBackup),Is.EqualTo(bytes));Assert.That(save.Store.TryRead(out var data,out var error),Is.True,error);Assert.That(save.Store.LastReadVersion,Is.EqualTo(2));Assert.That(hub.GetComponent<SalvageCarry>().HeldId,Is.EqualTo("cargo_b"));Assert.That(hub.GetComponent<SalvageDelivery>().Count,Is.EqualTo(1));Assert.That(echo.Battery.Charge,Is.EqualTo(37).Within(.001));Assert.That(hub.Known.Count,Is.Zero);Assert.That(hub.History.Messages,Is.Empty);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(save.Save(),Is.True);Assert.That(File.ReadAllBytes(save.Store.LegacyBackup),Is.EqualTo(bytes));
        }
        [UnityTest]public IEnumerator CurrentPlayerCarvingDoesNotInvalidateValidStoredEchoPose()
        {
            yield return new EnterPlayMode();yield return Boot();brain.ResetBase(IntentKind.Wait);var data=save.Capture();Assert.That(echo.Navigation.TryBind(new Vector3(8,0,8)),Is.True);motor.RestorePose(data.Echo.Position(),0,0);Physics.SyncTransforms();float end=Time.time+.5f;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.time<end){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Obstacle settle timeout");yield return null;}Assert.That(save.TryRestore(data,out var error),Is.True,error);Assert.That(Vector3.Distance(echo.transform.position,data.Echo.Position()),Is.LessThan(.1f));Assert.That(Vector3.Distance(motor.transform.position,data.Player.Position()),Is.LessThan(.1f));
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;if(folder!=null&&Directory.Exists(folder)&&folder.StartsWith(Path.GetFullPath("Library/EchoStation/R2SceneSaveTests")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(folder,true);yield return new ExitPlayMode();}
    }
}
