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
    public sealed class NextSessionTests
    {
        Scene old,scene;bool background;NextSession session;SalvageMotor motor;SalvageCommandHub hub;SalvageEchoCoordinator brain;SalvageEcho echo;
        void RestoreKnownB(){var seed=session.Capture();seed.World.Collaboration.Discovered=new[]{"cargo_b"};Assert.That(session.TryRestore(seed,out var error),Is.True,error);}
        IEnumerator Boot()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.GetComponent<PlayerInputReader>().SetApplicationFocus(true);motor.GetComponent<PlayerInputReader>().SetPointerCaptured(true);hub=motor.GetComponent<SalvageCommandHub>();session=motor.GetComponent<NextSession>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);brain.AutomaticWarnings=false;brain.ResetBase(IntentKind.Wait);session.ConfigureStoreForTests(Path.GetFullPath("Library/EchoStation/SalvageNext/Tests/Session_"+Guid.NewGuid().ToString("N")));
        }
        [UnityTest]public IEnumerator AtomicNewSlotRestoresPreferencesKnowledgeHistoryAndCancelsActiveTask()
        {
            yield return new EnterPlayMode();yield return Boot();RestoreKnownB();for(int i=0;i<55;i++)hub.Submit("电量");session.SetPreference(RecoveryPreference.HighScore);session.SetQuality(RecoveryQuality.Low);var snapshot=session.Capture();Assert.That(snapshot.World.Collaboration.Messages.Length,Is.EqualTo(50));Assert.That(session.Save(),Is.True);
            session.SetPreference(RecoveryPreference.Lightweight);session.SetQuality(RecoveryQuality.High);Assert.That(brain.Command(ScriptCommands.Investigate("cargo_b")),Is.True);var generation=brain.Generation;long epoch=session.Epoch;Assert.That(session.Load(),Is.True,motor.GetComponent<SalvageMission>().Notice);
            Assert.That(session.Epoch,Is.GreaterThan(epoch));Assert.That(brain.Generation,Is.GreaterThan(generation));Assert.That(brain.CurrentTargetId,Is.Null);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(session.Preference,Is.EqualTo(RecoveryPreference.HighScore));Assert.That(session.Quality,Is.EqualTo(RecoveryQuality.Low));Assert.That(hub.Known.Snapshot(),Is.EqualTo(new[]{"cargo_b"}));Assert.That(hub.History.Messages.Count,Is.EqualTo(50));Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(motor.GetComponent<SalvagePersistence>().enabled,Is.False);
            Directory.CreateDirectory("Docs/Tests/SalvagePrototype/Evidence/R4-A01f-2");File.WriteAllText("Docs/Tests/SalvagePrototype/Evidence/R4-A01f-2/Restore_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"newSlot",session.Store.Primary},{"restoredPreference",session.Preference.ToString()},{"restoredQuality",session.Quality.ToString()},{"messages",50},{"pendingTaskCancelled",true},{"UseLLM",false}}.ToString());
        }
        [UnityTest]public IEnumerator InvalidGeometryAndUnreadableFileLeaveTaskSettingsAndFullHistoryUntouched()
        {
            yield return new EnterPlayMode();yield return Boot();RestoreKnownB();for(int i=0;i<55;i++)hub.Submit("电量");Assert.That(brain.Command(ScriptCommands.Investigate("cargo_b")),Is.True);var history=hub.History;var generation=brain.Generation;long epoch=session.Epoch;var before=NextSaveStore.Encode(session.Capture());var invalid=NextSaveStore.Decode(before);invalid.World.Player.X=12;invalid.World.Player.Z=12;
            Assert.That(session.TryRestore(invalid,out _),Is.False);Assert.That(brain.Generation,Is.EqualTo(generation));Assert.That(session.Epoch,Is.EqualTo(epoch));Assert.That(hub.History,Is.SameAs(history));Assert.That(hub.History.Messages.Count,Is.EqualTo(100));Assert.That(NextSaveStore.Encode(session.Capture()),Is.EqualTo(before));
            Directory.CreateDirectory(Path.GetDirectoryName(session.Store.Primary));File.WriteAllText(session.Store.Primary,"broken");Assert.That(session.Load(),Is.False);Assert.That(File.ReadAllText(session.Store.Primary),Is.EqualTo("broken"));Assert.That(brain.Generation,Is.EqualTo(generation));Assert.That(session.Preference,Is.EqualTo(RecoveryPreference.Nearby));
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
