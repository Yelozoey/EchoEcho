using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class RealSessionHostTests
 {
  private sealed class Factory:ILocalModelFactory
  {
   private readonly LocalQwenFactory inner;private readonly object sync=new object();private readonly JArray events=new JArray();public int Active,Peak,Disposals;
   public Factory(string path){inner=new LocalQwenFactory(path);}
   public JArray Events{get{lock(sync)return (JArray)events.DeepClone();}}
   private void Record(string name){lock(sync)events.Add(new JObject{{"event",name},{"tick",Stopwatch.GetTimestamp()},{"thread",Thread.CurrentThread.ManagedThreadId}});}
   public ILocalModelRuntime Create(int main,CancellationToken cancellation)
   {
    var active=Interlocked.Increment(ref Active);Peak=Math.Max(Peak,active);Record("create-enter");if(active!=1){Interlocked.Decrement(ref Active);throw new InvalidOperationException("Actual models must not overlap");}
    try{return new Runtime(this,inner.Create(main,cancellation));}catch{Interlocked.Decrement(ref Active);throw;}
   }
   private sealed class Runtime:ILocalModelRuntime,ILocalRuntimeDiagnostics
   {
    private readonly Factory f;private readonly ILocalModelRuntime runtime;public Runtime(Factory owner,ILocalModelRuntime value){f=owner;runtime=value;}
    public NativeActivity Activity=>(runtime as ILocalRuntimeDiagnostics)?.Activity??default;
    public GenerationResult Generate(TokenRequest request,CancellationToken c)=>runtime.Generate(request,c);
    public void Dispose(){try{runtime.Dispose();}finally{Interlocked.Decrement(ref f.Active);Interlocked.Increment(ref f.Disposals);f.Record("dispose-complete");}}
   }
  }
  private static IEnumerator Until(InferenceSessionHost host,Func<bool> predicate,double seconds=30)
  {var w=Stopwatch.StartNew();while(!predicate()&&w.Elapsed.TotalSeconds<seconds){host.Poll();yield return null;}Assert.That(predicate(),Is.True,"real-host transition timeout: "+host.Phase);}
  [UnityTest]public IEnumerator RealModelLateResultsAreStaleAndReplacementFollowsFinalDispose()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var f=new Factory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName));var host=new InferenceSessionHost(f,main);var report=new JObject{{"mainThread",main},{"passed",false}};var reportPath="Docs/Tests/Stage3/Evidence/S3-I04/RealHost_"+DateTime.UtcNow.Ticks+".json";
   var ids=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-G01/PrefillFixture.json"))["inputIds"].ToObject<int[]>();
   try
   {
    host.SetEnabled(true);yield return Until(host,()=>host.Phase==RequestPhase.Ready);report["firstOwner"]=host.OwnerThreadId;Assert.That(host.OwnerThreadId,Is.Not.EqualTo(main));
    Assert.That(host.TrySubmit(Enumerable.Range(0,1536).Select(i=>ids[i%ids.Length]).ToArray(),out var old),Is.EqualTo(Admission.Accepted));yield return Until(host,()=>host.Activity.Active,5);report["nativeActiveAtLoad"]=true;
    var gc0=GC.CollectionCount(0);var started=Stopwatch.GetTimestamp();host.Invalidate(LifecycleChange.LoadGame);var invalidateMs=(Stopwatch.GetTimestamp()-started)*1000d/Stopwatch.Frequency;var collections=GC.CollectionCount(0)-gc0;var recording=Stopwatch.GetTimestamp();report["invalidateMs"]=invalidateMs;report["firstDoubleRecordingMs"]=(Stopwatch.GetTimestamp()-recording)*1000d/Stopwatch.Frequency;report["gc0DuringInvalidate"]=collections;Assert.That(invalidateMs,Is.LessThan(100));Assert.That(host.IsCurrent(old),Is.False);yield return Until(host,()=>host.Phase==RequestPhase.Ready);Assert.That(host.TryTakeCompletion(out _),Is.False);yield return Until(host,()=>host.Cooldown.Ready(Stopwatch.GetTimestamp()),5);
    Assert.That(host.TrySubmit(ids,out var beforeManual),Is.EqualTo(Admission.Accepted));yield return Until(host,()=>host.Phase==RequestPhase.Ready);Assert.That(host.TryTakeCompletion(out var actual),Is.True);Assert.That(actual.Completion.Outcome,Is.EqualTo(RequestOutcome.Succeeded));Assert.That(host.IsCurrent(beforeManual),Is.True);host.Invalidate(LifecycleChange.ManualOverride);Assert.That(host.TryClaim(beforeManual),Is.False);report["oldNativeCalls"]=host.Activity.Calls;
    host.SetEnabled(false);host.SetEnabled(true);yield return Until(host,()=>host.Phase==RequestPhase.Ready);report["secondOwner"]=host.OwnerThreadId;Assert.That(host.OwnerThreadId,Is.Not.EqualTo(main));Assert.That(f.Peak,Is.EqualTo(1));
    Assert.That(host.TrySubmit(ids,out var fresh),Is.EqualTo(Admission.Accepted));yield return Until(host,()=>host.Phase==RequestPhase.Ready);Assert.That(host.TryTakeCompletion(out var reply),Is.True);Assert.That(reply.Completion.Outcome,Is.EqualTo(RequestOutcome.Succeeded));Assert.That(host.TryClaim(fresh),Is.True);Assert.That(host.TryClaim(fresh),Is.False);Assert.That(host.TryClaim(old),Is.False);report["newNativeCalls"]=host.Activity.Calls;
   }
   finally{host.Dispose();report["events"]=f.Events;report["activeAtFinally"]=f.Active;File.WriteAllText(reportPath,report.ToString());}
   yield return Until(host,()=>host.CleanupComplete);Assert.That(f.Active,Is.Zero);Assert.That(f.Disposals,Is.EqualTo(2));var events=f.Events;Assert.That(events.Count,Is.EqualTo(4));Assert.That(events[0]["event"].Value<string>(),Is.EqualTo("create-enter"));Assert.That(events[1]["event"].Value<string>(),Is.EqualTo("dispose-complete"));Assert.That(events[2]["event"].Value<string>(),Is.EqualTo("create-enter"));Assert.That(events[2]["tick"].Value<long>(),Is.GreaterThan(events[1]["tick"].Value<long>()));report["events"]=events;report["activeAtFinally"]=f.Active;report["peakModels"]=f.Peak;report["passed"]=true;File.WriteAllText(reportPath,report.ToString());
  }
 }
}
