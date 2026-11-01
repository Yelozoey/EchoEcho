using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Station;
using EchoStation.Story;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class EndingIntegrationTests:StationPlayFixture
 {
  private IEnumerator Run(string id)
  {
   foreach(var target in new[]{"scan_mark_01","scan_lab_calibration_01"}){var cabin=target=="scan_mark_01";Move(cabin?new Vector3(7.7f,.05f,1):new Vector3(1,-3.95f,30));Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(cabin?new Vector3(1.5f,0,-2):new Vector3(3.5f,-4,30)),Is.True);station.Echo.TrySubmit(ScriptCommands.Investigate(target),out _);yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(target),18,"Ending fixture actual scan failed");}
   foreach(var c in StationClues.All)station.Journal.Store.Add(new JournalEntry(c.Id,c.Title,c.FullText,c.Summary,(ClueKind)(int)c.Kind));var spec=StationScript.Endings.Single(e=>e.Id==id);Assert.That(station.Progress.TryRestore(spec.Required),Is.True);foreach(var p in StationScript.Puzzles.Where(p=>spec.Required.Contains(p.Id)))if(!station.Memories.Store.TryGetEvent(p.CompletionId,out _))station.Memories.Store.AddEvent(p.CompletionId,station.Elapsed,"玩家完成"+p.Title+"。",MemoryPriority.Critical);station.GetComponent<StationPowerPanel>().RestoreCycle(true,3);
   foreach(var door in new[]{"cabin_door_01","core_door_01"}){WorldTarget.Active.TryGet(door,out var t);Move(door=="cabin_door_01"?new Vector3(0,.05f,3.3f):new Vector3(0,-3.95f,56.2f));Look(t.transform.position);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);Assert.That(t.GetComponent<StationGate>().IsOpen,Is.True);}
   Move(new Vector3(0,-3.95f,67));station.RefreshZone();Assert.That(ProgressAudit.Validate(station.CaptureProgressState()),Is.Null);WorldTarget.Active.TryGet("core_terminal_01",out var terminal);Move(terminal.transform.position+new Vector3(0,-1.1f,-1.7f));Look(terminal.transform.position);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);var ui=station.GetComponent<StationEndingPanel>();Assert.That(ui.IsOpen,Is.True);var index=StationScript.Endings.ToList().FindIndex(e=>e.Id==id);ui.ChoiceButton(index).onClick.Invoke();Assert.That(ui.Choice.PendingId,Is.EqualTo(id));Assert.That(ui.HasEnded,Is.False);yield return Press(Key.Escape);Assert.That(ui.IsOpen,Is.False);Assert.That(ui.HasEnded,Is.False);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);ui.ChoiceButton(index).onClick.Invoke();ui.ConfirmButton.onClick.Invoke();Assert.That(ui.Choice.EndingId,Is.EqualTo(id));Assert.That(ui.HasEnded,Is.True);Assert.That(station.GetComponent<PlayerInputReader>().UiBlocked,Is.True);Assert.That(station.Echo.CurrentMode,Is.EqualTo(spec.EchoStays?IntentKind.Wait:IntentKind.Follow));Assert.That(station.Memories.Store.Events.Count(e=>e.Id.StartsWith("ending_")),Is.EqualTo(1));Assert.That(ui.ArchivedEvidenceIds.Count,Is.EqualTo(id=="archive_exit"?14:0));Assert.That(ui.Confirm(),Is.EqualTo(EndingResult.AlreadyEnded));yield return Press(Key.Escape);Assert.That(ui.IsOpen,Is.True);
  }
  [UnityTest]public IEnumerator F02_B01_SealExitActualCoreChoice(){yield return new EnterPlayMode();yield return Boot();yield return Run("seal_exit");}
  [UnityTest]public IEnumerator F02_B02_MonitorStayActualCoreChoice(){yield return new EnterPlayMode();yield return Boot();yield return Run("monitor_stay");}
  [UnityTest]public IEnumerator F02_B03_ArchiveExitActualCoreChoice(){yield return new EnterPlayMode();yield return Boot();yield return Run("archive_exit");}
  [UnityTest]public IEnumerator F02_B04_MissingConditionsNeverSelectOrEnd(){yield return new EnterPlayMode();yield return Boot();WorldTarget.Active.TryGet("core_terminal_01",out var t);Move(t.transform.position+new Vector3(0,-1.1f,-1.7f));Look(t.transform.position);yield return Press(Key.E);var ui=station.GetComponent<StationEndingPanel>();Assert.That(ui.IsOpen,Is.True);for(var i=0;i<3;i++)Assert.That(ui.ChoiceButton(i).interactable,Is.False);Assert.That(ui.Select("archive_exit"),Is.EqualTo(EndingResult.Locked));Assert.That(ui.Confirm(),Is.EqualTo(EndingResult.NoSelection));Assert.That(ui.HasEnded,Is.False);}
 }
}
