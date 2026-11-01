using System.Collections;
using EchoStation.Dialogue.Commands;
using EchoStation.Dialogue.Integration;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class TerminalIntegrationTests:StationPlayFixture
 {
  private IEnumerator Open(string id,Vector3 point){Move(point);WorldTarget.Active.TryGet(id,out var t);Look(t.transform.position);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);Assert.That(station.GetComponent<StationTerminalPanel>().IsOpen,Is.True);yield return Until(()=>station.GetComponent<StationTerminalPanel>().CodeInput.isFocused,5,"Terminal input not focused");}
  private IEnumerator Type(string text){foreach(var c in text)InputSystem.QueueTextEvent(inputs.Keyboard,c);yield return R04ShadowProbe.Frame();}
  [UnityTest]public IEnumerator P02_B01_ActualGuessUnlocksAndArchivesButOnlySeparatePlayerEOpensDoor()
  {
   yield return new EnterPlayMode();yield return Boot();yield return Open("cabin_terminal_01",new Vector3(-2.1f,.05f,.6f));var ui=station.GetComponent<StationTerminalPanel>();yield return Type("0000");yield return Press(Key.Enter);Assert.That(station.Progress.Has("pz02"),Is.False);yield return Type("7316");yield return Press(Key.Enter);Assert.That(station.Progress.Has("pz02"),Is.True);Assert.That(station.Memories.Store.TryGetClue("terminal_data_01",out _),Is.True);Assert.That(station.Memories.Store.TryGetClue("note_maintenance_01",out _),Is.False);Assert.That(station.Memories.CaptureJson(),Does.Not.Contain("7316"));Assert.That(ui.SubmitCode(),Is.EqualTo(PuzzleResult.AlreadyComplete));
   var controller=station.GetComponent<ScriptDialogueController>();Assert.That(controller.AnswerProvider(new ScriptCommandParser(station.AvailableTargets()).Parse("小屋终端的密码是多少")).Content,Is.EqualTo(ScriptKnowledgeResponder.NoRecords));WorldTarget.Active.TryGet("cabin_door_01",out var door);Assert.That(door.GetComponent<StationGate>().IsOpen,Is.False,"Unlock must not open cabin door");yield return Press(Key.Escape);Assert.That(ui.IsOpen,Is.False,"Success modal must close on Escape");
   Move(new Vector3(0,.05f,3.3f));Look(door.transform.position);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);Assert.That(door.GetComponent<StationGate>().IsOpen,Is.True,"Separate player E must open authorized door");Assert.That(door.GetComponent<Collider>().enabled,Is.False);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));
  }
  [UnityTest]public IEnumerator P02_B02_VentGuessAndProgressFlagCannotReplaceActualLedgerAndCancelRestoresModal()
  {
   yield return new EnterPlayMode();yield return Boot();station.Progress.TryRestore(new[]{"pz02","pz03","pz04"});yield return Open("vent_terminal_01",new Vector3(-1.5f,-3.95f,51));var ui=station.GetComponent<StationTerminalPanel>();yield return Type("0428");Assert.That(ui.SubmitCode(),Is.EqualTo(PuzzleResult.MissingCredential));Assert.That(station.Progress.Has("pz05"),Is.False);Assert.That(WorldTarget.Active.TryGet("terminal_vent_data_01",out _),Is.False);
   var before=station.transform.position;yield return Hold(.3f,Key.W);Assert.That(Vector3.Distance(before,station.transform.position),Is.LessThan(.02f));yield return Press(Key.T,Key.J,Key.Tab);Assert.That(station.Dialogue.IsOpen,Is.False);Assert.That(station.GetComponent<JournalPanel>().IsOpen,Is.False);Assert.That(station.GetComponent<StationControls>().IsOpen,Is.False);yield return Press(Key.Escape);Assert.That(ui.IsOpen,Is.False);Assert.That(station.GetComponent<PlayerInputReader>().UiBlocked,Is.False);
   yield return Open("vent_terminal_01",new Vector3(-1.5f,-3.95f,51));station.GetComponent<PlayerInputReader>().SetApplicationFocus(false);yield return R04ShadowProbe.Frame();Assert.That(ui.IsOpen,Is.False);Assert.That(station.GetComponent<PlayerInputReader>().UiBlocked,Is.False);
  }
  [UnityTest]public IEnumerator P02_B03_TwoActualScansThenVentSubmitArchivesReceiptAndRequiresManualCoreDoor()
  {
   yield return new EnterPlayMode();yield return Boot();Move(new Vector3(7.7f,.05f,1));Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(1.5f,0,-2)),Is.True);Assert.That(station.Echo.TrySubmit(ScriptCommands.Investigate("scan_mark_01"),out _),Is.True);var ledger=station.Echo.GetComponent<EchoScanController>().Scanner.Ledger;yield return Until(()=>ledger.HasDiscovered("scan_mark_01"),18,"First real scan failed");
   Move(new Vector3(1,-3.95f,30));Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(3.5f,-4,30)),Is.True);Assert.That(station.Echo.TrySubmit(ScriptCommands.Investigate("scan_lab_calibration_01"),out _),Is.True);yield return Until(()=>ledger.HasDiscovered("scan_lab_calibration_01"),10,"Second real scan failed");station.Progress.TryRestore(new[]{"pz02","pz03","pz04"});
   yield return Open("vent_terminal_01",new Vector3(-1.5f,-3.95f,51));yield return Type("0428");yield return Press(Key.Enter);Assert.That(station.Progress.Has("pz05"),Is.True,"Actual scan credential + typed code must authorize vent");Assert.That(station.Memories.Store.TryGetClue("terminal_vent_data_01",out _),Is.True);WorldTarget.Active.TryGet("core_door_01",out var door);Assert.That(door.GetComponent<StationGate>().IsOpen,Is.False);yield return Press(Key.Escape);Assert.That(station.GetComponent<StationTerminalPanel>().IsOpen,Is.False,"Vent success modal must close");Move(new Vector3(0,-3.95f,56.2f));Look(door.transform.position);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);Assert.That(door.GetComponent<StationGate>().IsOpen,Is.True,"Separate player E must open authorized door");
  }
 }
}
