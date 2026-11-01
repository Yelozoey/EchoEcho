using System;
using System.Collections;
using System.IO;
using System.Reflection;
using EchoStation.AI.Inference;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Station;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3RestoreRollbackTests:S3PendingSaveFixture
 {
  [UnityTest]public IEnumerator CommitFailureRestoresOldWorldAndStillInvalidatesPendingModelResult()
  {
   yield return new EnterPlayMode();yield return BootS3();BindTemporarySlot();var target=persistence.Capture();Assert.That(persistence.TrySave(),Is.True,persistence.LastMessage);var bytes=File.ReadAllBytes(persistence.Files.PrimaryPath);Move(new Vector3(0,.05f,-30));station.GetComponent<PlayerFlashlight>().SetOn(true);yield return UseControlledOwner();yield return StartPendingFollow();var position=station.transform.position;var memory=station.Memories.CaptureJson();var injection=typeof(StationPersistence).GetField("BeforeCommitForTest",BindingFlags.Instance|BindingFlags.NonPublic);Assert.That(injection,Is.Not.Null);var invoked=0;
   injection.SetValue(persistence,new Action(()=>{invoked++;throw new InvalidOperationException("fixture S3 commit failure");}));yield return persistence.RestoreData(target);
   Assert.That(invoked,Is.EqualTo(1));Assert.That(persistence.LastSucceeded,Is.False);Assert.That(persistence.LastMessage,Does.Contain("已恢复原状态"));Assert.That(Vector3.Distance(position,station.transform.position),Is.LessThan(.02f));Assert.That(station.GetComponent<PlayerFlashlight>().IsOn,Is.True);Assert.That(station.Memories.CaptureJson(),Is.EqualTo(memory));Assert.That(station.Knowledge.IsReady,Is.True);Assert.That(File.ReadAllBytes(persistence.Files.PrimaryPath),Is.EqualTo(bytes));Assert.That(factory.Disposed,Is.Zero);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));
   factory.Work.Set();yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Rollback request drains");Assert.That(driver.Session.DeliveredGenerationCount,Is.Zero);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(driver.Session.ConsecutiveFaults,Is.Zero);
  }
 }
}
