using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EchoStation.AI.GameIntegration;
using EchoStation.Memory;
using EchoStation.Station;
using EchoStation.Story;
using EchoStation.Tests.Station;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class StationSnapshotSourceTests:StationPlayFixture
 {
  [UnityTest]public IEnumerator ActualFrustumAndWallOcclusionLimitUnknownTargets()
  {
   yield return new EnterPlayMode();yield return Boot();Assert.That(WorldTarget.Active.TryGet("note_entry_inspection_01",out var target),Is.True);Move(new Vector3(-4,.05f,-31.8f));Look(target.transform.position);station.RefreshZone();Physics.SyncTransforms();scene.GetPhysicsScene().Simulate(.02f);var source=new StationInferenceSnapshotSource(station,Thread.CurrentThread.ManagedThreadId);
   Assert.That(source.Capture("扫描入口巡检记录").Snapshot.Targets.Any(t=>t.Id==target.Id),Is.True);
   Look(station.Player.View.position+Vector3.back*10);Assert.That(source.Capture("扫描入口巡检记录").Snapshot.Targets.Any(t=>t.Id==target.Id),Is.False);Look(target.transform.position);
   var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="AI_Fixture_Occluder";blocker.transform.position=(station.Player.View.position+target.transform.position)*.5f;blocker.transform.localScale=new Vector3(1,.8f,.12f);Physics.SyncTransforms();scene.GetPhysicsScene().Simulate(.02f);
   try{Assert.That(source.Capture("扫描入口巡检记录").Snapshot.Targets.Any(t=>t.Id==target.Id),Is.False);}finally{blocker.SetActive(false);UnityEngine.Object.Destroy(blocker);}
   Physics.SyncTransforms();scene.GetPhysicsScene().Simulate(.02f);Assert.That(source.Capture("扫描入口巡检记录").Snapshot.Targets.Any(t=>t.Id==target.Id),Is.True);
  }
  [UnityTest]public IEnumerator OnlyExistingKnownMaintenanceRecordGrantsItsExplicitStorageReference()
  {
   yield return new EnterPlayMode();yield return Boot();var source=new StationInferenceSnapshotSource(station,Thread.CurrentThread.ManagedThreadId);Assert.That(source.Capture("调查储藏室可疑位置").Snapshot.Targets.Any(t=>t.Id=="scan_mark_01"),Is.False);
   station.Memories.Store.AddClue(new ClueMemory("fixture_note","普通记录","提及scan_mark_01和储藏室可疑位置，不是可信指向关系。",MemoryClueKind.Note));Assert.That(source.Capture("调查储藏室可疑位置").Snapshot.Targets.Any(t=>t.Id=="scan_mark_01"),Is.False);
   StationClues.TryGet("note_maintenance_01",out var record);station.Memories.Store.AddClue(new ClueMemory(record.Id,record.Title,record.Summary,record.Kind,record.Password,record.KeyMarker));var capture=source.Capture("调查储藏室可疑位置");Assert.That(capture.Snapshot.Targets.Any(t=>t.Id=="scan_mark_01"),Is.True);Assert.That(capture.Snapshot.Targets.Any(t=>t.Id=="scan_lab_calibration_01"),Is.False);Assert.That(capture.Catalog.Single(t=>t.Id=="scan_mark_01").Aliases,Does.Contain("储藏室可疑位置"));
   Assert.That(capture.Snapshot.Targets.Count,Is.LessThanOrEqualTo(8));Assert.That(capture.Catalog.Count,Is.LessThanOrEqualTo(128));
  }
  [UnityTest]public IEnumerator FreshCaptureDropsRemovedTargetButEarlierSnapshotStaysImmutable()
  {
   yield return new EnterPlayMode();yield return Boot();StationClues.TryGet("note_maintenance_01",out var record);station.Memories.Store.AddClue(new ClueMemory(record.Id,record.Title,record.Summary,record.Kind));var source=new StationInferenceSnapshotSource(station,Thread.CurrentThread.ManagedThreadId);var before=source.Capture("调查储藏室可疑位置");Assert.That(before.Snapshot.Targets.Any(t=>t.Id=="scan_mark_01"),Is.True);WorldTarget.Active.TryGet("scan_mark_01",out var target);target.gameObject.SetActive(false);Assert.That(source.Capture("调查储藏室可疑位置").Snapshot.Targets.Any(t=>t.Id==target.Id),Is.False);Assert.That(before.Snapshot.Targets.Any(t=>t.Id==target.Id),Is.True);
  }
  [UnityTest]public IEnumerator WorkerIsRejectedBeforeAnyUnityRead()
  {
   yield return new EnterPlayMode();yield return Boot();var source=new StationInferenceSnapshotSource(station,Thread.CurrentThread.ManagedThreadId);Exception error=null;var task=Task.Run(()=>{try{source.Capture("跟随我");}catch(Exception failure){error=failure;}});yield return Until(()=>task.IsCompleted,5,"Worker snapshot rejection");Assert.That(error,Is.TypeOf<InvalidOperationException>());Assert.That(error.Message,Does.Contain("No Unity objects"));Assert.That(source.Capture("跟随我").Snapshot.Input,Is.EqualTo("跟随我"));
  }
 }
}
