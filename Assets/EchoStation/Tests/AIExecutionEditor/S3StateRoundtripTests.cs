using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.AI.GameIntegration;
using EchoStation.Dialogue;
using EchoStation.Dialogue.Integration;
using EchoStation.Echo;
using EchoStation.Persistence;
using EchoStation.Player;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3StateRoundtripTests:S3SaveFixture
 {
  [UnityTest]public IEnumerator RealReadAndScanRoundtripUsesOnlyS3AtomicSlotAndNeverSerializesInferenceState()
  {
   yield return new EnterPlayMode();yield return BootS3();BindTemporarySlot();foreach(var name in new[]{"S1","S2"}){Directory.CreateDirectory(Path.Combine(directory,name));File.WriteAllText(Path.Combine(directory,name,"slot1.json"),"DO_NOT_TOUCH_"+name);}var fresh=persistence.Capture();
   Assert.That(WorldTarget.Active.TryGet("note_maintenance_01",out var note),Is.True);Move(note.GetComponent<EchoTargetPermissions>().ApproachPoint.position+Vector3.up*.05f);Look(note.transform.position);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);Assert.That(station.Journal.Store.TryGet(note.Id,out _),Is.True);
   Move(new Vector3(7.7f,.05f,1));Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(1.5f,0,-2)),Is.True);Assert.That(station.GetComponent<ScriptDialogueController>().SubmitPlayerIntent(ScriptCommands.Investigate("scan_mark_01")),Is.True);yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered("scan_mark_01"),18,"S3 actual scan");
   Move(new Vector3(0,.05f,-2));station.Player.SetLookAngles(215,12);station.GetComponent<PlayerFlashlight>().SetOn(true);for(var i=0;i<60;i++)station.Dialogue.Session.TryAdd(DialogueSpeaker.Player,"S3_HISTORY_"+i,false,out _);var captured=persistence.Capture();Assert.That(persistence.TrySave(),Is.True,persistence.LastMessage);Assert.That(persistence.TrySave(),Is.True,persistence.LastMessage);Assert.That(persistence.LastMessage,Does.StartWith("S3"));
   Assert.That(File.Exists(persistence.Files.PrimaryPath),Is.True);Assert.That(File.Exists(persistence.Files.BackupPath),Is.True);Assert.That(File.Exists(persistence.Files.TemporaryPath),Is.False);var disk=persistence.Files.Read();Assert.That(disk.Success,Is.True);var backup=StationSaveJson.Decode(File.ReadAllText(persistence.Files.BackupPath));Assert.That(backup.Success,Is.True);Assert.That(backup.Data.MemoryJson,Is.EqualTo(disk.Data.MemoryJson));
   var json=File.ReadAllText(persistence.Files.PrimaryPath);Assert.That(json,Does.Not.Contain("UseLLM").And.Not.Contain("TokenIds").And.Not.Contain("Prepared").And.Not.Contain("requestId").And.Not.Contain("past_key_values"));
   yield return persistence.RestoreData(fresh);Assert.That(persistence.LastSucceeded,Is.True,persistence.LastMessage);Assert.That(station.Memories.Store.Clues,Is.Empty);yield return persistence.Load();Assert.That(persistence.LastSucceeded,Is.True,persistence.LastMessage);Assert.That(persistence.LastMessage,Does.StartWith("S3"));
   Assert.That(station.Memories.CaptureJson(),Is.EqualTo(captured.MemoryJson));Assert.That(station.Journal.Store.Entries.Count,Is.EqualTo(2));Assert.That(station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered("scan_mark_01"),Is.True);Assert.That(station.Dialogue.Session.Messages.Count,Is.EqualTo(50));Assert.That(station.Dialogue.Session.Messages.Last().Text,Is.EqualTo("S3_HISTORY_59"));Assert.That(station.Player.Yaw,Is.EqualTo(215).Within(.01));Assert.That(station.Player.Pitch,Is.EqualTo(12).Within(.01));Assert.That(station.GetComponent<PlayerFlashlight>().IsOn,Is.True);Assert.That(station.transform.position.z,Is.EqualTo(captured.Player.Z).Within(.02));
   foreach(var name in new[]{"S1","S2"})Assert.That(File.ReadAllText(Path.Combine(directory,name,"slot1.json")),Is.EqualTo("DO_NOT_TOUCH_"+name));Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(station.GetComponent<S3DialogueDriver>().Session.OwnerThreadId,Is.Zero);
  }
 }
}
