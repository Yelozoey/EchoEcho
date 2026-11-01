using System;
using System.Collections;
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
 public sealed class RealOwnerTests
 {
  [UnityTest]public IEnumerator RealOwnerCompletesThenCancelsWhileNativeCallIsActive()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var directory=Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName);var folder="Docs/Tests/Stage3/Evidence/S3-I01";
   var ids=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-G01/PrefillFixture.json"))["inputIds"].ToObject<int[]>();var report=new JObject{{"mainThread",main}};var requests=new JArray();report["requests"]=requests;
   using(var owner=new InferenceOwner(new LocalQwenFactory(directory),main))
   {
    Assert.That(owner.Activity.Calls,Is.Zero);var start=System.Diagnostics.Stopwatch.StartNew();owner.Start();Assert.That(start.Elapsed.TotalMilliseconds,Is.LessThan(100),"Start must not load synchronously");
    while(owner.State==OwnerState.Loading&&start.Elapsed.TotalSeconds<30)yield return null;
    Assert.That(owner.State,Is.EqualTo(OwnerState.Ready),owner.StartupError);Assert.That(owner.OwnerThreadId,Is.Not.EqualTo(main));report["ownerThread"]=owner.OwnerThreadId;report["coldSeconds"]=owner.ColdSeconds;
    for(var request=1;request<=2;request++)
    {
     Assert.That(owner.TrySubmit(new TokenRequest(request,ids)),Is.True);var wait=System.Diagnostics.Stopwatch.StartNew();
     while(owner.State==OwnerState.Running&&wait.Elapsed.TotalSeconds<25)yield return null;
     Assert.That(owner.TryTakeCompletion(out var result),Is.True,owner.StartupError);Assert.That(result.Outcome,Is.EqualTo(OwnerOutcome.Succeeded),result.Error);Assert.That(result.Generation.Complete,Is.True);Assert.That(result.OwnerThreadId,Is.EqualTo(owner.OwnerThreadId));
     requests.Add(new JObject{{"id",result.RequestId},{"raw",result.Generation.Text},{"tokens",result.Generation.TokenIds.Count},{"seconds",result.Generation.Seconds},{"completedTick",result.ActualCompletedTick}});
    }
    Assert.That(requests[0]["raw"].Value<string>(),Is.EqualTo(requests[1]["raw"].Value<string>()));
    Assert.That(owner.TrySubmit(new TokenRequest(3,Enumerable.Range(0,1536).Select(i=>ids[i%ids.Length]).ToArray())),Is.True);
    var deadline=System.Diagnostics.Stopwatch.StartNew();while(!owner.Activity.Active&&owner.State==OwnerState.Running&&deadline.Elapsed.TotalSeconds<5)yield return null;
    var active=owner.Activity;Assert.That(active.Active,Is.True,"Must observe actual native Run, not just queued state");Assert.That(active.ThreadId,Is.EqualTo(owner.OwnerThreadId));report["activeAtCancel"]=true;report["nativeCallsBeforeCancel"]=active.Calls;
    var stop=System.Diagnostics.Stopwatch.StartNew();owner.StopAsync();report["stopCallMs"]=stop.Elapsed.TotalMilliseconds;Assert.That(stop.Elapsed.TotalMilliseconds,Is.LessThan(100));
    while(!owner.CleanupComplete&&deadline.Elapsed.TotalSeconds<30)yield return null;
    Assert.That(owner.CleanupComplete,Is.True);Assert.That(owner.State,Is.EqualTo(OwnerState.Stopped),owner.StartupError);Assert.That(owner.TryTakeCompletion(out var cancelled),Is.True);Assert.That(cancelled.Outcome,Is.EqualTo(OwnerOutcome.Cancelled));Assert.That(cancelled.Generation,Is.Null);
    report["cancelOutcome"]=cancelled.Outcome.ToString();report["releasedOnOwner"]=cancelled.OwnerThreadId;report["passed"]=true;
   }
   File.WriteAllText(folder+"/RealOwner_"+DateTime.UtcNow.Ticks+".json",report.ToString());
  }
 }
}
