using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Station;
using EchoStation.Story;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class LaboratoryTests:StationPlayFixture
 {
  [UnityTest]public IEnumerator W04_T01_ActualLabReadsAndPassageCollisionHaveCorrectElevation()
  {
   yield return new EnterPlayMode();yield return Boot();var gates=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>()).ToArray();Assert.That(gates.Length,Is.EqualTo(3));Assert.That(gates.Select(g=>g.GetComponent<WorldTarget>()?g.GetComponent<WorldTarget>().Id:g.name),Is.EquivalentTo(new[]{"cabin_door_01","Vent_Isolation","core_door_01"}));foreach(var gate in gates)Assert.That(UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(gate)),Does.EndWith("StationGate.cs"));foreach(var c in StationClues.All.Where(c=>c.ZoneId=="lab_a"&&c.Kind!=EchoStation.Memory.MemoryClueKind.Scan)){
    WorldTarget.Active.TryGet(c.Id,out var target);Move(new Vector3(target.transform.position.x,-3.95f,target.transform.position.z-1.8f));Look(target.transform.position);yield return Press(Key.E);Assert.That(station.Memories.Store.TryGetClue(c.Id,out _),Is.True,c.Id);
   }
   Move(new Vector3(0,-3.95f,37));station.Player.SetLookAngles(0,0);yield return Hold(.6f,Key.W);Assert.That(station.transform.position.z,Is.LessThan(37.8f));Assert.That(station.transform.position.y,Is.InRange(-4.1f,-3.8f));
   Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>()).Single(g=>g.name=="Vent_Isolation").IsOpen,Is.False);Assert.That(RenderSettings.fog,Is.False);
  }
  [UnityTest]public IEnumerator W04_T02_ActualRackScanStoresCodeOnlyAfterSuccessAndNeverPowersVent()
  {
   yield return new EnterPlayMode();yield return Boot();Move(new Vector3(1,-3.95f,30));WorldTarget.Active.TryGet("scan_lab_calibration_01",out var mark);Assert.That(mark.GetComponent<ClueSource>().IsRevealed,Is.False);Assert.That(station.Memories.CaptureJson(),Does.Not.Contain("0428"));
   Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(3.5f,-4,30)),Is.True);Assert.That(station.Echo.TrySubmit(ScriptCommands.Investigate(mark.Id),out _),Is.True);
   yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(mark.Id),10,"Lab calibration scan failed");Assert.That(station.Memories.Store.TryGetClue(mark.Id,out var known),Is.True);Assert.That(known.Password,Is.EqualTo("0428"));Assert.That(known.KeyMarker,Is.EqualTo("vent_calibration_valid"));
   Assert.That(station.Memories.Store.TryGetClue("scan_mark_01",out _),Is.False);Assert.That(station.Memories.Store.TryGetEvent("progress_pz04",out _),Is.False);Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>()).Single(g=>g.name=="Vent_Isolation").IsOpen,Is.False);
  }
 }
}
