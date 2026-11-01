using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class RealSchedulerTests
 {
  [UnityTest]public IEnumerator ActualRequestsRespectCompletionPlus300Milliseconds()
  {
   var report=new JObject();var ids=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-G01/PrefillFixture.json"))["inputIds"].ToObject<int[]>();
   using(var owner=new InferenceOwner(new LocalQwenFactory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName)),Thread.CurrentThread.ManagedThreadId))
   {
    var schedule=new InferenceScheduler(owner);var timer=Stopwatch.StartNew();owner.Start();while(owner.State==OwnerState.Loading&&timer.Elapsed.TotalSeconds<30)yield return null;Assert.That(owner.State,Is.EqualTo(OwnerState.Ready),owner.StartupError);
    Assert.That(schedule.TrySubmit(new TokenRequest(1,ids)),Is.EqualTo(Admission.Accepted));Assert.That(schedule.TrySubmit(new TokenRequest(2,ids)),Is.EqualTo(Admission.Busy));timer.Restart();while(owner.State==OwnerState.Running&&timer.Elapsed.TotalSeconds<25)yield return null;
    Assert.That(schedule.TryTakeCompletion(out var first),Is.True);Assert.That(first.Outcome,Is.EqualTo(OwnerOutcome.Succeeded),first.Error);var observed=Stopwatch.GetTimestamp();var elapsedMs=(observed-first.ActualCompletedTick)*1000d/Stopwatch.Frequency;
    report["firstCompletedTick"]=first.ActualCompletedTick;report["observedAfterMs"]=elapsedMs;
    if(elapsedMs<300)Assert.That(schedule.TrySubmit(new TokenRequest(3,ids)),Is.EqualTo(Admission.CoolingDown));
    var notBefore=first.ActualCompletedTick+(Stopwatch.Frequency*3+9)/10;while(Stopwatch.GetTimestamp()<notBefore)yield return null;
    var admitted=Stopwatch.GetTimestamp();Assert.That(schedule.TrySubmit(new TokenRequest(4,ids)),Is.EqualTo(Admission.Accepted));var gap=(admitted-first.ActualCompletedTick)*1000d/Stopwatch.Frequency;Assert.That(gap,Is.GreaterThanOrEqualTo(300));
    report["secondAdmittedTick"]=admitted;report["actualGapMs"]=gap;timer.Restart();while(owner.State==OwnerState.Running&&timer.Elapsed.TotalSeconds<25)yield return null;
    Assert.That(schedule.TryTakeCompletion(out var second),Is.True);Assert.That(second.RequestId,Is.EqualTo(4));Assert.That(second.Outcome,Is.EqualTo(OwnerOutcome.Succeeded),second.Error);report["nativeCalls"]=owner.Activity.Calls;report["ownerThread"]=owner.OwnerThreadId;
    owner.StopAsync();timer.Restart();while(!owner.CleanupComplete&&timer.Elapsed.TotalSeconds<15)yield return null;Assert.That(owner.CleanupComplete,Is.True);schedule.ObserveCompletion();report["cleanupCompletionTick"]=owner.LastActualCompletionTick;report["passed"]=true;
   }
   File.WriteAllText("Docs/Tests/Stage3/Evidence/S3-I02/RealScheduler_"+DateTime.UtcNow.Ticks+".json",report.ToString());
  }
 }
}
