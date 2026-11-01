using System.Collections;
using System.Linq;
using EchoStation.Memory;
using EchoStation.Player;
using EchoStation.Story;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class ValleyTests:StationPlayFixture
 {
  [UnityTest]public IEnumerator W02_T01_TwoActualEReadsArchiveDistinctFullTextAndOnlySummariesInMemory()
  {
   yield return new EnterPlayMode();yield return Boot();foreach(var c in StationClues.All.Where(c=>c.ZoneId=="valley")){
    Assert.That(WorldTarget.Active.TryGet(c.Id,out var target),Is.True);Move(new Vector3(target.transform.position.x,.05f,target.transform.position.z-1.8f));Look(target.transform.position);yield return Press(Key.E);
    Assert.That(station.Journal.Store.ExportSummaries().Any(s=>s.id==c.Id),Is.True,c.Id);Assert.That(station.Memories.Store.TryGetClue(c.Id,out var known),Is.True);Assert.That(known.Summary,Is.EqualTo(c.Summary));
    Assert.That(station.GetComponent<JournalPanel>().SetOpen(true),Is.True);station.GetComponent<JournalPanel>().SelectEntry(c.Id);Assert.That(station.GetComponent<JournalPanel>().DisplayedFullText,Is.EqualTo(c.FullText));station.GetComponent<JournalPanel>().SetOpen(false);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);
   }
   Assert.That(station.Memories.Store.Clues.Count,Is.EqualTo(2));Assert.That(station.Memories.Store.Events.Any(e=>e.Id=="progress_pz01"),Is.False);foreach(var c in StationClues.All)Assert.That(station.Memories.CaptureJson(),Does.Not.Contain(c.FullText));
  }
  [UnityTest]public IEnumerator W02_T02_ActualMovementCannotCrossValleyWallsAndCabinExitRemainsOpen()
  {
   yield return new EnterPlayMode();yield return Boot();Move(new Vector3(7,.05f,-25));station.Player.SetLookAngles(0,0);yield return Hold(.6f,Key.D);Assert.That(station.transform.position.x,Is.InRange(7.1f,7.55f));
   Move(new Vector3(0,.05f,-39));yield return Hold(.6f,Key.S);Assert.That(station.transform.position.z,Is.GreaterThan(-39.55f));
   Move(new Vector3(0,.05f,-8));yield return Hold(.6f,Key.W);Assert.That(station.transform.position.z,Is.GreaterThan(-7));Assert.That(station.transform.position.y,Is.GreaterThan(-.1f));
  }
 }
}
