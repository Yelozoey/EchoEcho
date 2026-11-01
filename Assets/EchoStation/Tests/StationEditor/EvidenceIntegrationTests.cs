using System.Collections;
using System.Linq;
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
 public sealed class EvidenceIntegrationTests:StationPlayFixture
 {
  [UnityTest]public IEnumerator P01_B01_ActualReadThenJournalButtonConfirmsOnceAndNeverOpensDoor()
  {
   yield return new EnterPlayMode();yield return Boot();var panel=station.GetComponent<StationEvidencePanel>();var journal=station.GetComponent<JournalPanel>();journal.SetOpen(true);yield return EchoStation.Tests.Fixtures.R04ShadowProbe.Frame();Assert.That(panel.TimelineButton.interactable,Is.False);Assert.That(panel.Confirm("pz01"),Is.EqualTo(PuzzleResult.MissingClues));journal.SetOpen(false);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);
   foreach(var c in StationClues.All.Where(c=>c.ZoneId=="valley")){WorldTarget.Active.TryGet(c.Id,out var target);Move(new Vector3(target.transform.position.x,.05f,target.transform.position.z-1.8f));Look(target.transform.position);yield return Press(Key.E);Assert.That(station.Progress.Has("pz01"),Is.False);}
   journal.SetOpen(true);yield return EchoStation.Tests.Fixtures.R04ShadowProbe.Frame();Assert.That(panel.TimelineButton.interactable,Is.True);panel.TimelineButton.onClick.Invoke();Assert.That(station.Progress.Has("pz01"),Is.True);Assert.That(station.Memories.Store.TryGetEvent("progress_pz01",out _),Is.True);Assert.That(panel.Confirm("pz01"),Is.EqualTo(PuzzleResult.AlreadyComplete));Assert.That(station.Memories.Store.Events.Count(e=>e.Id=="progress_pz01"),Is.EqualTo(1));WorldTarget.Active.TryGet("cabin_door_01",out var door);Assert.That(door.GetComponent<StationGate>().IsOpen,Is.False);Assert.That(panel.CauseButton.interactable,Is.False);
  }
  [UnityTest]public IEnumerator P01_B02_MemoryOnlyFactsAndFullEventCapacityCannotCreatePartialGameProgress()
  {
   yield return new EnterPlayMode();yield return Boot();foreach(var c in StationClues.All.Where(c=>c.ZoneId=="valley"))station.Memories.Store.AddClue(new ClueMemory(c.Id,c.Title,c.Summary,c.Kind));var journal=station.GetComponent<JournalPanel>();journal.SetOpen(true);var panel=station.GetComponent<StationEvidencePanel>();Assert.That(panel.Confirm("pz01"),Is.EqualTo(PuzzleResult.MissingClues));Assert.That(station.Progress.Capture(),Is.Empty);
   while(station.Memories.Store.Events.Count<512){var n=station.Memories.Store.Events.Count;station.Memories.Store.AddEvent("fixture_event_"+n,0,"测试容量。");}Assert.That(panel.Confirm("pz01"),Is.EqualTo(PuzzleResult.Invalid));Assert.That(station.Progress.Has("pz01"),Is.False);Assert.That(station.Memories.Store.TryGetEvent("progress_pz01",out _),Is.False);
  }
 }
}
