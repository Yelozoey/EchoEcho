using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Dialogue.Integration;
using EchoStation.Echo;
using EchoStation.Station;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3NewGameTests:S3PendingSaveFixture
 {
  [UnityTest]public IEnumerator ConfirmedNewGameCancelsOldRequestStartsScriptAndKeepsManualSlot()
  {
   yield return new EnterPlayMode();yield return BootS3();BindTemporarySlot();Assert.That(persistence.TrySave(),Is.True,persistence.LastMessage);var bytes=File.ReadAllBytes(persistence.Files.PrimaryPath);yield return UseControlledOwner();yield return StartPendingFollow();station.Dialogue.SetOpen(false);var controls=station.GetComponent<StationControls>();Assert.That(controls.SetOpen(true),Is.True);controls.Canvas.GetComponentsInChildren<Button>(true).Single(b=>b.name=="New").onClick.Invoke();var lifecycle=station.GetComponent<StationLifecycle>();Assert.That(lifecycle.IsOpen,Is.True);Assert.That(GameRuntimeOptions.UseLLM,Is.False);lifecycle.NewButton.onClick.Invoke();Assert.That(lifecycle.ConfirmNewButton.gameObject.activeSelf,Is.True);lifecycle.ConfirmNewButton.onClick.Invoke();yield return Until(()=>!lifecycle.IsChangingSession&&!persistence.Busy,6,"S3 new session completes");
   Assert.That(persistence.LastSucceeded,Is.True,persistence.LastMessage);Assert.That(station.Progress.Capture(),Is.Empty);Assert.That(station.Journal.Store.Entries,Is.Empty);Assert.That(station.Memories.Store.Clues,Is.Empty);Assert.That(station.Dialogue.Session.Messages.Where(m=>m.Speaker==DialogueSpeaker.Player),Is.Empty);Assert.That(station.transform.position.z,Is.EqualTo(StationLayout.PlayerSpawn.z).Within(.02f));Assert.That(File.ReadAllBytes(persistence.Files.PrimaryPath),Is.EqualTo(bytes));Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(factory.Disposed,Is.Zero);
   station.GetComponent<ScriptDialogueController>().SubmitPlayerIntent(ScriptCommands.Wait());factory.Work.Set();yield return Until(()=>driver.Session.CleanupComplete,5,"Old new-game kernel release");Assert.That(driver.Session.DeliveredGenerationCount,Is.Zero);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(factory.Disposed,Is.EqualTo(1));
  }
 }
}
