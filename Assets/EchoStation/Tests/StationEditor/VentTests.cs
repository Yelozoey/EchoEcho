using System.Collections;
using EchoStation.Echo;
using EchoStation.Station;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class VentTests:StationPlayFixture
 {
  [UnityTest]public IEnumerator W05_T01_ActualRouteNoteAndCorridorWallsAndCoreDoorCollision()
  {
   yield return new EnterPlayMode();yield return Boot();WorldTarget.Active.TryGet("note_vent_route_01",out var note);Move(new Vector3(1.65f,-3.95f,42.2f));Look(note.transform.position);yield return Press(Key.E);Assert.That(station.Memories.Store.TryGetClue(note.Id,out _),Is.True);
   Move(new Vector3(0,-3.95f,56.8f));station.Player.SetLookAngles(0,0);yield return Hold(.6f,Key.W);Assert.That(station.transform.position.z,Is.LessThan(57.7f));WorldTarget.Active.TryGet("core_door_01",out var door);Assert.That(door.GetComponent<StationGate>().IsOpen,Is.False);
   Move(new Vector3(1.8f,-3.95f,47));yield return Hold(.6f,Key.D);Assert.That(station.transform.position.x,Is.InRange(1.8f,2.15f));
  }
  [UnityTest]public IEnumerator W05_T02_ActualTerminalScanNeverReadsLockedReceiptOrOpensCore()
  {
   yield return new EnterPlayMode();yield return Boot();Move(new Vector3(.5f,-3.95f,50));WorldTarget.Active.TryGet("vent_terminal_01",out var terminal);Assert.That(WorldTarget.Active.TryGet("terminal_vent_data_01",out _),Is.False);
   Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(-1.5f,-4,51.3f)),Is.True);Assert.That(station.Echo.TrySubmit(ScriptCommands.Investigate(terminal.Id),out _),Is.True);yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(terminal.Id),8,"Vent terminal scan failed");
   Assert.That(station.Memories.Store.TryGetClue(terminal.Id,out _),Is.True);Assert.That(station.Memories.Store.TryGetClue("terminal_vent_data_01",out _),Is.False);WorldTarget.Active.TryGet("core_door_01",out var door);Assert.That(door.GetComponent<StationGate>().IsOpen,Is.False);
  }
 }
}
