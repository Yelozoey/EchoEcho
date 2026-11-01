using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Context;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.AI.Models;
using EchoStation.Memory;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3RealOwnerTests
 {
  private InferenceOwner owner;private JObject report;private string output;
  private static IEnumerator Until(Func<bool> predicate,double seconds=35)
  {var timer=Stopwatch.StartNew();while(!predicate()&&timer.Elapsed.TotalSeconds<seconds)yield return null;Assert.That(predicate(),Is.True,"Real Qwen3 owner transition timed out");}
  private static InferenceSnapshot Small()=>new InferenceSnapshot(new MemoryStore(),"跟随我。",0,"cabin",new[]{new TargetCandidate("fixture_panel","测试面板","cabin",true,true,false,true,1)});
  private static InferenceSnapshot Long()
  {
   var memory=new MemoryStore();for(var i=0;i<8;i++)memory.AddEvent("event_"+i,0,"测试 "+new string('7',117));var rare=string.Concat(Enumerable.Repeat("\U00030000",40));
   var targets=Enumerable.Range(0,8).Select(i=>new TargetCandidate("t"+new string('7',60)+i,rare,"cabin",true,true,true,true,i));
   return new InferenceSnapshot(memory,"测试"+new string('中',198),100,"cabin",targets);
  }
  [UnityTearDown]public IEnumerator Cleanup()
  {
   if(owner==null)yield break;owner.StopAsync();yield return Until(()=>owner.CleanupComplete,40);
   if(report!=null){report["cleanupComplete"]=owner.CleanupComplete;report["finalState"]=owner.State.ToString();File.WriteAllText(output,report.ToString());}owner.Dispose();
  }
  [UnityTest,Timeout(180000)]public IEnumerator RealNativeCancellationRecoversAndActiveStopReleasesOnOwner()
  {
   var main=Thread.CurrentThread.ManagedThreadId;output="Docs/Tests/Stage3/Evidence/S3-Q3-R03b/Owner_"+DateTime.UtcNow.Ticks+".json";report=new JObject{{"mainThread",main},{"scope","real Qwen3 Owner/Scheduler lifecycle only, no game dispatch"},{"passed",false}};
   owner=new InferenceOwner(new LocalQwen3Factory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName),new JsonIntentConstraintProvider(),new MemorySearch(new[]{new MemoryKeywordGroup("测试","记录")})),main);
   var schedule=new InferenceScheduler(owner);var timer=Stopwatch.StartNew();owner.Start();Assert.That(timer.Elapsed.TotalMilliseconds,Is.LessThan(100));yield return Until(()=>owner.State==OwnerState.Ready||owner.CleanupComplete);
   Assert.That(owner.State,Is.EqualTo(OwnerState.Ready),owner.StartupError);Assert.That(owner.OwnerThreadId,Is.Not.EqualTo(main));report["ownerThread"]=owner.OwnerThreadId;report["coldSeconds"]=owner.ColdSeconds;var normals=new JArray();report["normalRequests"]=normals;
   for(var id=1;id<=2;id++)
   {
    while(!schedule.Cooldown.Ready(Stopwatch.GetTimestamp()))yield return null;
    Assert.That(schedule.TrySubmit(new TokenRequest(id,Small())),Is.EqualTo(Admission.Accepted));Assert.That(schedule.TrySubmit(new TokenRequest(999,Small())),Is.EqualTo(Admission.Busy));
    yield return Until(()=>owner.State==OwnerState.Ready||owner.CleanupComplete);Assert.That(schedule.TrySubmit(new TokenRequest(999,Small())),Is.EqualTo(Admission.Busy));Assert.That(schedule.TryTakeCompletion(out var result),Is.True);
    Assert.That(result.Outcome,Is.EqualTo(OwnerOutcome.Succeeded),result.Error);Assert.That(result.Generation.Complete,Is.True);Assert.That(IntentJsonParser.Parse(result.Generation.Text).Success,Is.True);Assert.That(result.Generation.PreparationThreadId,Is.EqualTo(owner.OwnerThreadId));
    normals.Add(new JObject{{"id",result.RequestId},{"raw",result.Generation.Text},{"completedTick",result.ActualCompletedTick},{"owner",result.OwnerThreadId}});
    Assert.That(schedule.Cooldown.Ready(result.ActualCompletedTick+Stopwatch.Frequency*299/1000),Is.False);Assert.That(schedule.Cooldown.Ready(result.ActualCompletedTick+(Stopwatch.Frequency*3+9)/10),Is.True);
   }
   Assert.That(normals[0]["raw"].Value<string>(),Is.EqualTo(normals[1]["raw"].Value<string>()));
   while(!schedule.Cooldown.Ready(Stopwatch.GetTimestamp()))yield return null;
   Assert.That(schedule.TrySubmit(new TokenRequest(3,Long())),Is.EqualTo(Admission.Accepted));yield return Until(()=>owner.Activity.Active,8);var active=owner.Activity;Assert.That(active.ThreadId,Is.EqualTo(owner.OwnerThreadId));report["nativeActiveAtCancel"]=true;report["callsAtCancel"]=active.Calls;
   timer.Restart();Assert.That(owner.CancelCurrent(),Is.True);report["cancelCallMs"]=timer.Elapsed.TotalMilliseconds;Assert.That(timer.Elapsed.TotalMilliseconds,Is.LessThan(100));
   yield return Until(()=>owner.State==OwnerState.Ready||owner.CleanupComplete);Assert.That(schedule.TryTakeCompletion(out var cancelled),Is.True);Assert.That(cancelled.Outcome,Is.EqualTo(OwnerOutcome.Cancelled));Assert.That(cancelled.Generation,Is.Null);report["lateCancelledResultDiscarded"]=true;
   while(!schedule.Cooldown.Ready(Stopwatch.GetTimestamp()))yield return null;
   Assert.That(schedule.TrySubmit(new TokenRequest(4,Small())),Is.EqualTo(Admission.Accepted));yield return Until(()=>owner.State==OwnerState.Ready||owner.CleanupComplete);Assert.That(schedule.TryTakeCompletion(out var recovered),Is.True);Assert.That(recovered.Outcome,Is.EqualTo(OwnerOutcome.Succeeded),recovered.Error);Assert.That(recovered.Generation.Text,Is.EqualTo(normals[0]["raw"].Value<string>()));report["recoveredAfterNativeCancellation"]=true;
   while(!schedule.Cooldown.Ready(Stopwatch.GetTimestamp()))yield return null;
   Assert.That(schedule.TrySubmit(new TokenRequest(5,Long())),Is.EqualTo(Admission.Accepted));yield return Until(()=>owner.Activity.Active,8);report["nativeActiveAtStop"]=true;
   timer.Restart();owner.StopAsync();report["stopCallMs"]=timer.Elapsed.TotalMilliseconds;Assert.That(timer.Elapsed.TotalMilliseconds,Is.LessThan(100));yield return Until(()=>owner.CleanupComplete,35);
   Assert.That(owner.State,Is.EqualTo(OwnerState.Stopped),owner.StartupError);Assert.That(owner.TryTakeCompletion(out var stopped),Is.True);Assert.That(stopped.Outcome,Is.EqualTo(OwnerOutcome.Cancelled));Assert.That(stopped.Generation,Is.Null);Assert.That(stopped.OwnerThreadId,Is.EqualTo(owner.OwnerThreadId));report["releaseOwner"]=stopped.OwnerThreadId;report["passed"]=true;
  }
 }
}
