using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Station;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class StationCabinTests:StationPlayFixture
 {
  [UnityTest]public IEnumerator W03_T01_ActualCabinReadsAndBothEntryDirectionsPreserveS1RoomGeometry()
  {
   yield return new EnterPlayMode();yield return Boot();Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>()).Any(t=>t.name=="ES_Cabin_S2_Copy"),Is.True);
   foreach(var pair in new[]{("note_maintenance_01",new Vector3(-2.8f,.05f,.8f)),("note_roster_01",new Vector3(7.7f,.05f,1))}){WorldTarget.Active.TryGet(pair.Item1,out var t);Move(pair.Item2);Look(t.transform.position);yield return Press(Key.E);Assert.That(station.Memories.Store.TryGetClue(t.Id,out _),Is.True,t.Id);}
   Move(new Vector3(0,.05f,-6.5f));station.Player.SetLookAngles(0,0);yield return Hold(.5f,Key.S);Assert.That(station.transform.position.z,Is.LessThan(-7));
   Move(new Vector3(0,.05f,3.4f));station.Player.SetLookAngles(0,0);yield return Hold(.7f,Key.W);Assert.That(station.transform.position.z,Is.InRange(4,4.85f));
   WorldTarget.Active.TryGet("cabin_door_01",out var door);Assert.That(door.GetComponent<StationGate>().IsOpen,Is.False);Assert.That(station.Memories.Store.TryGetClue("terminal_data_01",out _),Is.False);
  }
  [UnityTest]public IEnumerator W03_T02_ActualLockedEquipmentScanCannotExposeInactiveTerminalData()
  {
   yield return new EnterPlayMode();yield return Boot();Move(new Vector3(0,.05f,-2));WorldTarget.Active.TryGet("cabin_terminal_01",out var terminal);Assert.That(WorldTarget.Active.TryGet("terminal_data_01",out _),Is.False);
   Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(-2.1f,0,1.15f)),Is.True);Assert.That(station.Echo.TrySubmit(ScriptCommands.Investigate(terminal.Id),out _),Is.True);
   yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(terminal.Id),8,"Locked terminal scan failed");
   Assert.That(station.Memories.Store.TryGetClue(terminal.Id,out var fact),Is.True);Assert.That(fact.Summary,Does.Contain("不提供锁定数据"));Assert.That(station.Memories.Store.TryGetClue("terminal_data_01",out _),Is.False);Assert.That(station.Memories.CaptureJson(),Does.Not.Contain("7316"));
  }
  [UnityTest]public IEnumerator W03_T03_ActualHiddenScanRevealsOnlyDirectionAndKeepsDoorClosed()
  {
   yield return new EnterPlayMode();yield return Boot();Move(new Vector3(7.7f,.05f,1));WorldTarget.Active.TryGet("scan_mark_01",out var target);Assert.That(target.GetComponent<ClueSource>().IsReadable,Is.False);Assert.That(station.Memories.Store.TryGetClue(target.Id,out _),Is.False);
   Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(1.5f,0,-2)),Is.True);Assert.That(station.Echo.TrySubmit(ScriptCommands.Investigate(target.Id),out _),Is.True);
   yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(target.Id),18,"Storage mark scan failed");
   Assert.That(target.GetComponent<ClueSource>().IsRevealed,Is.True);Assert.That(station.Memories.Store.TryGetClue(target.Id,out var known),Is.True);Assert.That(known.KeyMarker,Is.EqualTo("scan_lab_calibration_01"));Assert.That(known.Summary,Does.Contain("地下A区校准架"));
   WorldTarget.Active.TryGet("cabin_door_01",out var door);Assert.That(door.GetComponent<StationGate>().IsOpen,Is.False);Assert.That(station.Memories.Store.TryGetClue("scan_lab_calibration_01",out _),Is.False);
  }
 }
}
