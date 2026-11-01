using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Station;
using EchoStation.Story;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace EchoStation.Tests.Station
{
 public sealed class StationLifecycleTests:StationPlayFixture
 {
  private string directory;
  private StationPersistence Store(){var p=station.GetComponent<StationPersistence>();Assert.That(station.GetComponent<StationLifecycle>(),Is.Not.Null);directory=Path.GetFullPath(Path.Combine("Library","EchoStation","S2LifecycleTests",Guid.NewGuid().ToString("N")));p.ConfigureDirectory(directory);return p;}
  private IEnumerator PowerUi(){Move(new Vector3(-4.5f,-3.95f,26.8f));WorldTarget.Active.TryGet("power_panel_01",out var target);Look(target.transform.position);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);Assert.That(station.GetComponent<StationPowerPanel>().IsOpen,Is.True);}
  [UnityTest]public IEnumerator F03_D01_ActualF5AndF9UseWholeS2SlotWithoutImplicitOverwrite()
  {
   yield return new EnterPlayMode();yield return Boot();var p=Store();var life=station.GetComponent<StationLifecycle>();yield return Press(Key.F5);Assert.That(p.LastSucceeded,Is.True,p.LastMessage);Assert.That(File.Exists(p.Files.PrimaryPath),Is.True);var bytes=File.ReadAllBytes(p.Files.PrimaryPath);var saved=p.Files.Read().Data;Move(new Vector3(0,.05f,-30));station.GetComponent<PlayerFlashlight>().SetOn(true);yield return Press(Key.F9);yield return Until(()=>!life.IsChangingSession&&!p.Busy,6,"F9 did not finish");Assert.That(p.LastSucceeded,Is.True,p.LastMessage);Assert.That(station.transform.position.z,Is.EqualTo(saved.Player.Z).Within(.02));Assert.That(station.GetComponent<PlayerFlashlight>().IsOn,Is.False);Assert.That(File.ReadAllBytes(p.Files.PrimaryPath),Is.EqualTo(bytes));Assert.That(life.IsOpen,Is.False);Assert.That(station.GetComponent<PlayerInputReader>().UiBlocked,Is.False);
  }
  [UnityTest]public IEnumerator F03_D02_TitleNewRequiresConfirmationResetsSessionDraftAndCircuitButKeepsManualFile()
  {
   yield return new EnterPlayMode();yield return Boot();var p=Store();var life=station.GetComponent<StationLifecycle>();yield return PowerUi();var power=station.GetComponent<StationPowerPanel>();power.ToggleButton(0).onClick.Invoke();power.Close();Move(StationLayout.PlayerSpawn);station.RefreshZone();StationClues.TryGet("note_maintenance_01",out var note);station.Journal.Store.Add(new JournalEntry(note.Id,note.Title,note.FullText,note.Summary,(ClueKind)(int)note.Kind));station.Dialogue.Session.TryAdd(DialogueSpeaker.Player,"OLD_HISTORY_MARKER",false,out _);station.Dialogue.InputField.text="尚未提交的文字";Assert.That(p.TrySave(),Is.True,p.LastMessage);var bytes=File.ReadAllBytes(p.Files.PrimaryPath);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.Tab);var controls=scene.GetRootGameObjects().Single(r=>r.name=="ES_S2Controls");controls.GetComponentsInChildren<Button>(true).Single(b=>b.name=="New").onClick.Invoke();Assert.That(life.IsOpen,Is.True);Assert.That(station.GetComponent<StationControls>().IsOpen,Is.False);life.NewButton.onClick.Invoke();Assert.That(life.ConfirmNewButton.gameObject.activeSelf,Is.True);Assert.That(station.Journal.Store.Entries.Count,Is.EqualTo(1));life.CancelNewButton.onClick.Invoke();Assert.That(life.ConfirmNewButton.gameObject.activeSelf,Is.False);life.NewButton.onClick.Invoke();station.GetComponent<PlayerInputReader>().SetApplicationFocus(false);life.ConfirmNewButton.onClick.Invoke();Assert.That(life.IsChangingSession,Is.False);station.GetComponent<PlayerInputReader>().SetApplicationFocus(true);life.ConfirmNewButton.onClick.Invoke();yield return Until(()=>!life.IsChangingSession&&!p.Busy,6,"New session did not finish");Assert.That(p.LastSucceeded,Is.True,p.LastMessage);Assert.That(life.IsOpen,Is.False);Assert.That(station.Progress.Capture(),Is.Empty);Assert.That(station.Journal.Store.Entries,Is.Empty);Assert.That(station.Memories.Store.Clues,Is.Empty);Assert.That(station.Memories.Store.Events.Count,Is.EqualTo(1));Assert.That(station.Dialogue.Session.Messages.Any(m=>m.Text.Contains("OLD_HISTORY_MARKER")),Is.False);Assert.That(station.Dialogue.InputField.text,Is.Empty);Assert.That(station.transform.position.z,Is.EqualTo(StationLayout.PlayerSpawn.z).Within(.02));Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(File.ReadAllBytes(p.Files.PrimaryPath),Is.EqualTo(bytes));yield return PowerUi();Assert.That(power.ToggleButton(0).GetComponentInChildren<Text>().text,Does.Contain("OFF"));Assert.That(power.ToggleButton(1).GetComponentInChildren<Text>().text,Does.Contain("ON"));Assert.That(power.ToggleButton(2).GetComponentInChildren<Text>().text,Does.Contain("ON"));Assert.That(power.Cycle.Powered,Is.False);
  }
  [UnityTest]public IEnumerator F03_D03_EndedStateCannotOverwriteAndLoadReturnsToPrechoiceSession()
  {
   yield return new EnterPlayMode();yield return Boot();var p=Store();Assert.That(p.TrySave(),Is.True,p.LastMessage);var bytes=File.ReadAllBytes(p.Files.PrimaryPath);var ending=station.GetComponent<StationEndingPanel>();station.Progress.TryRestore(StationScript.Endings[0].Required);ending.Choice.Select(station.Progress,"seal_exit");ending.Choice.Confirm(station.Progress);Assert.That(ending.HasEnded,Is.True);Assert.That(p.TrySave(),Is.False);Assert.That(File.ReadAllBytes(p.Files.PrimaryPath),Is.EqualTo(bytes));station.GetComponent<StationControls>().RequestLoad();var life=station.GetComponent<StationLifecycle>();yield return Until(()=>!life.IsChangingSession&&!p.Busy,6,"Prechoice load did not finish");Assert.That(p.LastSucceeded,Is.True,p.LastMessage);Assert.That(ending.HasEnded,Is.False);Assert.That(station.Progress.Capture(),Is.Empty);Assert.That(station.GetComponent<PlayerInputReader>().UiBlocked,Is.False);Assert.That(File.ReadAllBytes(p.Files.PrimaryPath),Is.EqualTo(bytes));
  }
  [UnityTest]public IEnumerator F03_D04_FutureFileLoadKeepsTitleCurrentStateAndOriginalBytes()
  {
   yield return new EnterPlayMode();yield return Boot();station.Player.enabled=false;var p=Store();Assert.That(p.TrySave(),Is.True,p.LastMessage);var future=File.ReadAllText(p.Files.PrimaryPath).Replace("\"version\":1","\"version\":2");File.WriteAllText(p.Files.PrimaryPath,future);var life=station.GetComponent<StationLifecycle>();life.OpenTitle();var position=station.transform.position;var memory=station.Memories.CaptureJson();life.LoadButton.onClick.Invoke();yield return Until(()=>!life.IsChangingSession,6,"Future load did not finish");Assert.That(p.LastSucceeded,Is.False);Assert.That(life.IsOpen,Is.True);Assert.That(station.GetComponent<PlayerInputReader>().UiBlocked,Is.True);Assert.That(station.transform.position,Is.EqualTo(position));Assert.That(station.Memories.CaptureJson(),Is.EqualTo(memory));Assert.That(File.ReadAllText(p.Files.PrimaryPath),Is.EqualTo(future));life.ContinueButton.onClick.Invoke();Assert.That(life.IsOpen,Is.False);Assert.That(station.GetComponent<PlayerInputReader>().UiBlocked,Is.False);
  }
  [TearDown]public void CleanupFiles(){if(directory==null||!Directory.Exists(directory))return;var parent=Path.GetFullPath(Path.Combine("Library","EchoStation","S2LifecycleTests"))+Path.DirectorySeparatorChar;Assert.That(Path.GetFullPath(directory).StartsWith(parent,StringComparison.OrdinalIgnoreCase),Is.True);if(TestContext.CurrentContext.Result.Outcome.Status!=NUnit.Framework.Interfaces.TestStatus.Failed)Directory.Delete(directory,true);}
 }
}
