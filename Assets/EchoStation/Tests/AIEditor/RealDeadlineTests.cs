using System;
using System.Collections;
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
 public sealed class RealDeadlineTests
 {
  private sealed class AdvancingClock:IMonotonicClock
  {private long offset;public long Now=>Stopwatch.GetTimestamp()+Interlocked.Read(ref offset);public long Frequency=>Stopwatch.Frequency;public void Advance20Seconds()=>Interlocked.Add(ref offset,20*Frequency);}
  [UnityTest]public IEnumerator RealNativeCancellationTimeoutDrainAndRuntimeReuse()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var clock=new AdvancingClock();var owner=new InferenceOwner(new LocalQwenFactory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName)),main,clock);var s=new RequestSupervisor(owner);var report=new JObject{{"mainThread",main},{"deadlineTest","real native Run with advanced monotonic clock; production20s unchanged"}};
   var ids=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-G01/PrefillFixture.json"))["inputIds"].ToObject<int[]>();var longIds=Enumerable.Range(0,1536).Select(i=>ids[i%ids.Length]).ToArray();
   try
   {
    s.Start();var watch=Stopwatch.StartNew();while(owner.State==OwnerState.Loading&&watch.Elapsed.TotalSeconds<30){s.Poll();yield return null;}Assert.That(owner.State,Is.EqualTo(OwnerState.Ready),owner.StartupError);Assert.That(owner.OwnerThreadId,Is.Not.EqualTo(main));report["ownerThread"]=owner.OwnerThreadId;report["coldSeconds"]=owner.ColdSeconds;
    Assert.That(s.TrySubmit(new TokenRequest(1,longIds)),Is.EqualTo(Admission.Accepted));watch.Restart();while(!owner.Activity.Active&&watch.Elapsed.TotalSeconds<5)yield return null;Assert.That(owner.Activity.Active,Is.True);report["nativeActiveAtCancel"]=true;
    watch.Restart();Assert.That(s.CancelCurrent(),Is.True);report["cancelCallMs"]=watch.Elapsed.TotalMilliseconds;Assert.That(watch.Elapsed.TotalMilliseconds,Is.LessThan(100));Assert.That(s.TryTakeCompletion(out var cancelled),Is.True);Assert.That(cancelled.Outcome,Is.EqualTo(RequestOutcome.Cancelled));Assert.That(cancelled.Generation,Is.Null);Assert.That(s.TrySubmit(new TokenRequest(2,ids)),Is.EqualTo(Admission.Busy));
    watch.Restart();while(owner.State==OwnerState.Running&&watch.Elapsed.TotalSeconds<25){s.Poll();yield return null;}s.Poll();Assert.That(owner.State,Is.EqualTo(OwnerState.Ready));Assert.That(s.TryTakeCompletion(out _),Is.False);report["cancelDrainSeconds"]=watch.Elapsed.TotalSeconds;while(!s.Cooldown.Ready(clock.Now))yield return null;
    Assert.That(s.TrySubmit(new TokenRequest(3,longIds)),Is.EqualTo(Admission.Accepted));watch.Restart();while(!owner.Activity.Active&&watch.Elapsed.TotalSeconds<5)yield return null;Assert.That(owner.Activity.Active,Is.True);report["nativeActiveAtTimeout"]=true;
    clock.Advance20Seconds();watch.Restart();s.Poll();report["timeoutPollMs"]=watch.Elapsed.TotalMilliseconds;Assert.That(watch.Elapsed.TotalMilliseconds,Is.LessThan(100));Assert.That(s.TryTakeCompletion(out var timeout),Is.True);Assert.That(timeout.Outcome,Is.EqualTo(RequestOutcome.RequestTimeout));Assert.That(timeout.Generation,Is.Null);Assert.That(s.TrySubmit(new TokenRequest(4,ids)),Is.EqualTo(Admission.Busy));
    watch.Restart();while(owner.State==OwnerState.Running&&watch.Elapsed.TotalSeconds<25){s.Poll();yield return null;}s.Poll();Assert.That(owner.State,Is.EqualTo(OwnerState.Ready));Assert.That(s.TryTakeCompletion(out _),Is.False);report["timeoutDrainSeconds"]=watch.Elapsed.TotalSeconds;while(!s.Cooldown.Ready(clock.Now))yield return null;
    Assert.That(s.TrySubmit(new TokenRequest(5,ids)),Is.EqualTo(Admission.Accepted));watch.Restart();while(owner.State==OwnerState.Running&&watch.Elapsed.TotalSeconds<25){s.Poll();yield return null;}
    Assert.That(s.TryTakeCompletion(out var recovered),Is.True);Assert.That(recovered.Outcome,Is.EqualTo(RequestOutcome.Succeeded),recovered.Error);Assert.That(recovered.Generation.Complete,Is.True);report["recoveredRequest"]=recovered.RequestId;report["recoveredSeconds"]=recovered.Generation.Seconds;report["nativeCalls"]=owner.Activity.Calls;
   }
   finally{s.StopAsync();}
   var cleanup=Stopwatch.StartNew();while(!owner.CleanupComplete&&cleanup.Elapsed.TotalSeconds<15)yield return null;Assert.That(owner.CleanupComplete,Is.True);Assert.That(owner.State,Is.EqualTo(OwnerState.Stopped),owner.StartupError);report["passed"]=true;File.WriteAllText("Docs/Tests/Stage3/Evidence/S3-I03/RealDeadline_"+DateTime.UtcNow.Ticks+".json",report.ToString());
  }
 }
}
