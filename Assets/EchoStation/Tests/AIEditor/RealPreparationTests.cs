using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EchoStation.AI.Context;
using EchoStation.AI.Inference;
using EchoStation.AI.Models;
using EchoStation.Memory;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class RealPreparationTests
 {
  private static IEnumerator Until(InferenceSessionHost host,Func<bool> ready,double limit=30)
  {var clock=Stopwatch.StartNew();while(!ready()&&clock.Elapsed.TotalSeconds<limit){host.Poll();yield return null;}Assert.That(ready(),Is.True,"actual preparation transition: "+host.Phase);}
  [UnityTest]public IEnumerator SnapshotPreparationIsActuallyOnOwnerAndBadContextCannotRunNative()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var search=new MemorySearch(new[]{new MemoryKeywordGroup("测试","记录")});var host=new InferenceSessionHost(new LocalQwenFactory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName),search),main);var report=new JObject{{"mainThread",main},{"passed",false}};var path="Docs/Tests/Stage3/Evidence/S3-C02/RealPreparation_"+DateTime.UtcNow.Ticks+".json";
   try
   {
    host.SetEnabled(true);yield return Until(host,()=>host.Phase==RequestPhase.Ready);report["ownerThread"]=host.OwnerThreadId;
    var store=new MemoryStore();store.AddClue(new ClueMemory("note_test","测试笔记","测试服务码7316已归档",MemoryClueKind.Note,"7316"));var targets=new[]{new TargetCandidate("power_panel_01","供电面板","cabin",true,true,false,true,1)};var snapshot=new InferenceSnapshot(store,"测试记录是什么？",1,"cabin",targets);store.AddClue(new ClueMemory("later_note","后来资料","之后发现的内容不准进入旧快照",MemoryClueKind.Note));
    Assert.That(host.TrySubmit(snapshot,out var first),Is.EqualTo(Admission.Accepted));yield return Until(host,()=>host.Phase==RequestPhase.Ready);Assert.That(host.TryTakeCompletion(out var delivery),Is.True);var result=delivery.Completion;Assert.That(result.Outcome,Is.EqualTo(RequestOutcome.Succeeded),result.Error);var generated=result.Generation;Assert.That(generated.Complete,Is.True);Assert.That(generated.Prepared,Is.Not.Null);Assert.That(generated.PreparationThreadId,Is.EqualTo(host.OwnerThreadId));Assert.That(generated.PreparationThreadId,Is.Not.EqualTo(main));Assert.That(generated.Prepared.Body,Does.Not.Contain("7316"));Assert.That(generated.Prepared.Body,Does.Not.Contain("之后发现"));Assert.That(generated.Prepared.UsedMemory,Does.Contain("测试"));Assert.That(generated.Prepared.InputIds.Count,Is.LessThanOrEqualTo(1536));Assert.That(generated.Prepared.EffectiveTargets.Count,Is.EqualTo(1));Assert.That(host.TryClaim(first),Is.True);report["firstRaw"]=generated.Text;report["inputTokens"]=generated.Prepared.InputIds.Count;report["preparationSeconds"]=generated.PreparationSeconds;report["preparationThread"]=generated.PreparationThreadId;report["generationSeconds"]=generated.Seconds;
    yield return Until(host,()=>host.Cooldown.Ready(Stopwatch.GetTimestamp()),5);var before=host.Activity.Calls;var badStore=new MemoryStore();badStore.AddClue(new ClueMemory("note_bad","测试资料","测试\ud800",MemoryClueKind.Note));var bad=new InferenceSnapshot(badStore,"测试记录",2,"cabin",Array.Empty<TargetCandidate>());Assert.That(host.TrySubmit(bad,out var badStamp),Is.EqualTo(Admission.Accepted));yield return Until(host,()=>host.Phase==RequestPhase.Ready);Assert.That(host.TryTakeCompletion(out var rejected),Is.True);Assert.That(rejected.Completion.Outcome,Is.EqualTo(RequestOutcome.GenerationFailed));Assert.That(rejected.Completion.Generation,Is.Null);Assert.That(host.Activity.Calls,Is.EqualTo(before));Assert.That(host.TryClaim(badStamp),Is.False);report["badContextNativeCallDelta"]=host.Activity.Calls-before;
    yield return Until(host,()=>host.Cooldown.Ready(Stopwatch.GetTimestamp()),5);var fresh=new InferenceSnapshot(store,"跟随我",3,"cabin",targets);Assert.That(host.TrySubmit(fresh,out _),Is.EqualTo(Admission.Accepted));yield return Until(host,()=>host.Phase==RequestPhase.Ready);Assert.That(host.TryTakeCompletion(out var recovered),Is.True);Assert.That(recovered.Completion.Outcome,Is.EqualTo(RequestOutcome.Succeeded),recovered.Completion.Error);Assert.That(recovered.Completion.Generation.Complete,Is.True);report["recoveredRaw"]=recovered.Completion.Generation.Text;report["nativeCalls"]=host.Activity.Calls;
   }
   finally{host.Dispose();File.WriteAllText(path,report.ToString());}
   yield return Until(host,()=>host.CleanupComplete);report["passed"]=true;File.WriteAllText(path,report.ToString());
  }
 }
}
