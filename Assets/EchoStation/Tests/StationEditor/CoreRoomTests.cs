using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Station;
using EchoStation.Story;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class CoreRoomTests:StationPlayFixture
 {
  [UnityTest]public IEnumerator W06_A01_ActualCoreReadsAndRoomEndCollision()
  {
   yield return new EnterPlayMode();yield return Boot();foreach(var c in StationClues.All.Where(c=>c.ZoneId=="core")){WorldTarget.Active.TryGet(c.Id,out var target);Move(new Vector3(target.transform.position.x,-3.95f,target.transform.position.z-1.8f));Look(target.transform.position);yield return Press(Key.E);Assert.That(station.Memories.Store.TryGetClue(c.Id,out _),Is.True,c.Id);}
   Move(new Vector3(3,-3.95f,74.8f));station.Player.SetLookAngles(0,0);yield return Hold(.6f,Key.W);Assert.That(station.transform.position.z,Is.LessThan(75.7f));Assert.That(station.Memories.Store.Events.Any(e=>e.Id.StartsWith("progress_")),Is.False);
  }
  [UnityTest]public IEnumerator W06_A02_ActualCoreTerminalScanIsInformationOnly()
  {
   yield return new EnterPlayMode();yield return Boot();Move(new Vector3(2,-3.95f,70));WorldTarget.Active.TryGet("core_terminal_01",out var target);Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(0,-4,70.5f)),Is.True);Assert.That(station.Echo.TrySubmit(ScriptCommands.Investigate(target.Id),out _),Is.True);yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(target.Id),8,"Core terminal scan failed");Assert.That(station.Memories.Store.TryGetClue(target.Id,out _),Is.True);Assert.That(station.Memories.Store.Events.Any(e=>e.Id.StartsWith("ending_")),Is.False);Assert.That(station.Memories.Store.TryGetClue("data_core_sensor_01",out _),Is.False);
  }
 }
}
