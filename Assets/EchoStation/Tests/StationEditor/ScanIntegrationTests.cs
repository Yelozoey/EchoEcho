using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Station;
using EchoStation.Story;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.AI;
namespace EchoStation.Tests.Station
{
 public sealed class ScanIntegrationTests:StationPlayFixture
 {
  private IEnumerator Scan(string id){var cabin=id=="scan_mark_01";Move(cabin?new Vector3(7.7f,.05f,1):new Vector3(1,-3.95f,30));Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(cabin?new Vector3(1.5f,0,-2):new Vector3(3.5f,-4,30)),Is.True);Assert.That(station.Echo.TrySubmit(ScriptCommands.Investigate(id),out _),Is.True);yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(id),18,"Actual scan failed: "+id);}
  [UnityTest]public IEnumerator P04_B01_HiddenArrowCannotBeReadAndMemoryOrRawRecordsCannotForgeLedger()
  {
   yield return new EnterPlayMode();yield return Boot();WorldTarget.Active.TryGet("scan_mark_01",out var target);Assert.That(AssetDatabase.GetAssetPath(target.GetComponent<MeshFilter>().sharedMesh),Does.EndWith("ES_ScanArrow.asset"));Assert.That(target.GetComponent<MeshRenderer>().enabled,Is.False);Move(new Vector3(7.7f,.05f,1));Look(target.transform.position);yield return Press(Key.E);Assert.That(station.Memories.Store.TryGetClue("scan_mark_01",out _),Is.False);
   foreach(var c in StationClues.All.Where(c=>c.Kind==EchoStation.Memory.MemoryClueKind.Scan))station.Journal.Store.Add(new JournalEntry(c.Id,c.Title,c.FullText,c.Summary,ClueKind.Scan));Assert.That(station.GetComponent<StationScanProgress>().Synchronize(),Is.EqualTo(PuzzleResult.MissingCredential));Assert.That(station.Progress.Has("pz04"),Is.False);Assert.That(target.GetComponent<ClueSource>().IsRevealed,Is.False);
  }
  [UnityTest]public IEnumerator P04_B02_ActualCabinThenLabCreatesCredentialOnceAndNeverOperatesDoorsOrPower()
  {
   yield return new EnterPlayMode();yield return Boot();yield return Scan("scan_mark_01");Assert.That(station.Progress.Has("pz04"),Is.False);Assert.That(station.Echo.LastSpeech,Does.Contain("地下A区校准架"));yield return Scan("scan_lab_calibration_01");Assert.That(station.Progress.Has("pz04"),Is.True);Assert.That(station.Memories.Store.TryGetEvent("progress_pz04",out _),Is.True);Assert.That(station.Echo.LastSpeech,Does.Contain("两处校准记录"));Assert.That(station.Echo.LastSpeech,Does.Not.Contain("0428"));
   Assert.That(station.GetComponent<StationScanProgress>().Synchronize(true),Is.EqualTo(PuzzleResult.AlreadyComplete));Assert.That(station.Memories.Store.Events.Count(e=>e.Id=="progress_pz04"),Is.EqualTo(1));foreach(var door in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>()))Assert.That(door.IsOpen,Is.False);Assert.That(station.GetComponent<StationPowerPanel>().Cycle.Powered,Is.False);
  }
  [UnityTest]public IEnumerator P04_B03_ActualReverseOrderAlsoRequiresBothAndProducesSameFacts()
  {
   yield return new EnterPlayMode();yield return Boot();yield return Scan("scan_lab_calibration_01");Assert.That(station.Progress.Has("pz04"),Is.False);Assert.That(station.Echo.LastSpeech,Does.Contain("还需回小屋储藏室"));Assert.That(station.Memories.Store.TryGetClue("scan_lab_calibration_01",out var fact),Is.True);Assert.That(fact.Password,Is.EqualTo("0428"));yield return Scan("scan_mark_01");Assert.That(station.Progress.Has("pz04"),Is.True);WorldTarget.Active.TryGet("scan_mark_01",out var target);Assert.That(target.GetComponent<MeshRenderer>().enabled,Is.True);Assert.That(target.GetComponent<MeshFilter>().sharedMesh.triangles.Length,Is.EqualTo(9));Assert.That(station.Progress.Has("pz05"),Is.False);
  }
  [UnityTest]public IEnumerator P04_B04_BlockedRouteDoesNotDiscoverAndRecoveredReachableScanCanRetry()
  {
   yield return new EnterPlayMode();yield return Boot();for(var i=0;i<4;i++)yield return EchoStation.Tests.Fixtures.R04ShadowProbe.Frame();var accepted=station.Echo.TrySubmit(ScriptCommands.Investigate("scan_lab_calibration_01"),out _);WorldTarget.Active.TryGet("cabin_door_01",out var door);WorldTarget.Active.TryGet("scan_lab_calibration_01",out var target);var obstacle=door.GetComponent<NavMeshObstacle>();var path=new NavMeshPath();var navigation=station.Echo.GetComponent<EchoNavigation>();navigation.Agent.CalculatePath(target.GetComponent<EchoTargetPermissions>().ApproachPoint.position,path);File.WriteAllText("Docs/Tests/Stage2/Evidence/S2-P04/BlockedPathInitial.json",JsonUtility.ToJson(new PathProbe{accepted=accepted,frame=Time.frameCount,carving=obstacle.carving,enabled=obstacle.enabled,center=obstacle.transform.TransformPoint(obstacle.center),size=Vector3.Scale(obstacle.size,obstacle.transform.lossyScale),status=path.status.ToString(),corners=path.corners},true));
   if(accepted)yield return Until(()=>station.Echo.CurrentMode==IntentKind.Wait,5,"Accepted route did not settle to blocked failure");Assert.That(station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered("scan_lab_calibration_01"),Is.False);Assert.That(station.Progress.Has("pz04"),Is.False);yield return Scan("scan_lab_calibration_01");Assert.That(station.Memories.Store.TryGetClue("scan_lab_calibration_01",out _),Is.True);Assert.That(station.Progress.Has("pz04"),Is.False);
  }
  [System.Serializable]private class PathProbe{public bool accepted,carving,enabled;public int frame;public Vector3 center,size;public string status;public Vector3[] corners;}
 }
}
