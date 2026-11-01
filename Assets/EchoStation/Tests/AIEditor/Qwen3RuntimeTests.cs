using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
 public sealed class Qwen3RuntimeTests
 {
  private sealed class Work{public readonly CancellationTokenSource Cancel=new CancellationTokenSource();public int Done,Ready,Owner,Strict,Semantic;public bool Clean;public string Error;}
  private Work active;
  [TearDown]public void CancelUnfinishedOwner(){var work=active;if(work!=null&&Volatile.Read(ref work.Done)==0)work.Cancel.Cancel();}
  [Test]public void FactoryRequiresDirectoryAndConstraintProvider()
  {Assert.Throws<ArgumentNullException>(()=>new LocalQwen3Factory("unused",null));Assert.Throws<ArgumentException>(()=>new LocalQwen3Factory("",new JsonIntentConstraintProvider()));}
  [Test]public void MainCannotInitializeFormalRuntime()
  {Assert.Throws<InvalidOperationException>(()=>new LocalQwen3Factory("unused",new JsonIntentConstraintProvider()).Create(Thread.CurrentThread.ManagedThreadId,CancellationToken.None));}
  [UnityTest,Timeout(600000)]public IEnumerator Actual18InputsUseFormalCSharpPreparationAndCompleteJson()
  {
   var work=active=new Work();var main=Thread.CurrentThread.ManagedThreadId;var output=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-Q3-R01/Runtime18_"+DateTime.UtcNow.Ticks+".json");
   new Thread(()=>Run(work,main,output)){IsBackground=true,Name="Q3-Formal-Runtime-Owner",Priority=ThreadPriority.BelowNormal}.Start();var clock=Stopwatch.StartNew();
   try
   {
    while(Volatile.Read(ref work.Done)==0&&clock.Elapsed.TotalSeconds<580){if(Volatile.Read(ref work.Ready)==0&&clock.Elapsed.TotalSeconds>=30||clock.Elapsed.TotalSeconds>=560)work.Cancel.Cancel();yield return null;}
    Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1),"Owner draining: "+output);Assert.That(work.Error,Is.Null,work.Error+"; "+output);Assert.That(work.Clean,Is.True,output);Assert.That(work.Strict,Is.EqualTo(18));Assert.That(work.Semantic,Is.GreaterThanOrEqualTo(17),"Formal runtime smoke gate: "+output);
   }
   finally{work.Cancel.Cancel();if(Volatile.Read(ref work.Done)==1)work.Cancel.Dispose();}
  }
  private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
  private static void Run(Work work,int main,string output)
  {
   work.Owner=Thread.CurrentThread.ManagedThreadId;ILocalModelRuntime runtime=null;var rows=new JArray();var clock=Stopwatch.StartNew();
   var report=new JObject{{"scope","formal C# snapshot/retrieval/tokenizer/prompt/backend JSON smoke; no game dispatch or factual acceptance"},{"mainThread",main},{"ownerThread",work.Owner},{"cpuThreads",4},{"newOrdinaryOutputMarkersEnabled",true},{"promptSha256",Qwen3PromptTemplate.Sha256},{"requests",rows},{"completed",false},{"passed",false}};
   Action save=()=>File.WriteAllText(output,report.ToString(),new UTF8Encoding(false));
   try
   {
    Directory.CreateDirectory(Path.GetDirectoryName(output));using(var unique=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){};
    var source=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/CandidateCases.json"));
    var baseline=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/Tuning/V5_V2Contrast_A/Cases.json"));
    var search=new MemorySearch(new[]{new MemoryKeywordGroup("设备记录","供电面板","危险设备","power_panel_01","hazard_01")});
    runtime=new LocalQwen3Factory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName),new JsonIntentConstraintProvider(),search).Create(main,work.Cancel.Token);
    Need(clock.Elapsed.TotalSeconds<30,"Formal cold load exceeded30s");work.Cancel.Token.ThrowIfCancellationRequested();Volatile.Write(ref work.Ready,1);report["coldSeconds"]=clock.Elapsed.TotalSeconds;
    var diagnostics=(ILocalRuntimeDiagnostics)runtime;var beforeRaw=diagnostics.Activity.Calls;
    try{runtime.Generate(new TokenRequest(1,new[]{0}),work.Cancel.Token);throw new InvalidDataException("Raw ID bypass accepted");}
    catch(GenerationException error){Need(error.Code==GenerationFault.InvalidContext,"Wrong raw bypass failure");}
    Need(diagnostics.Activity.Calls==beforeRaw,"Raw bypass reached native");report["rawIdBypassRejectedBeforeNative"]=true;
    long previous=0;var index=0;
    foreach(var item in source["cases"])
    {
     work.Cancel.Token.ThrowIfCancellationRequested();
     if(previous!=0)while((Stopwatch.GetTimestamp()-previous)/(double)Stopwatch.Frequency<.3){if(work.Cancel.Token.WaitHandle.WaitOne(1))work.Cancel.Token.ThrowIfCancellationRequested();}
     var store=new MemoryStore();Need(store.AddClue(new ClueMemory("note_devices","设备记录","已发现供电面板，Echo可以扫描但不能操作；危险设备应保持距离。",MemoryClueKind.Note))==MemoryWriteResult.Added,"Fixture clue failed");
     var input=item["input"].Value<string>();var snapshot=new InferenceSnapshot(store,input,index,"cabin",new[]{new TargetCandidate("power_panel_01","供电面板","cabin",true,true,false,true,1),new TargetCandidate("hazard_01","危险设备","cabin",true,false,true,true,2)});
     var row=new JObject{{"index",index},{"input",input},{"expected",item["expected"]},{"expectedTarget",item["target"]},{"strictJson",false},{"semanticCorrect",false}};rows.Add(row);save();var timer=Stopwatch.StartNew();
     try
     {
      var generated=runtime.Generate(new TokenRequest(index+2,snapshot),work.Cancel.Token);var prepared=generated.Prepared;Need(prepared!=null&&generated.PreparationThreadId==work.Owner,"Preparation bypassed Owner");
      row["body"]=prepared.Body;row["inputIds"]=new JArray(prepared.InputIds);row["inputTokens"]=prepared.InputIds.Count;row["effectiveTargets"]=new JArray(prepared.EffectiveTargets.Select(target=>target.Id));row["usedMemory"]=prepared.UsedMemory;
      row["sameInputIdsAsFixedHfDiagnostic"]=prepared.InputIds.SequenceEqual(baseline["cases"][index]["ids"].Values<int>());row["raw"]=generated.Text;row["outputIds"]=new JArray(generated.TokenIds);row["seconds"]=generated.Seconds+generated.PreparationSeconds;row["completeEos"]=generated.Complete;row["mode"]=generated.Mode;
      var parsed=IntentJsonParser.Parse(generated.Text);var valid=generated.Complete&&parsed.Success&&generated.Mode=="json-intent-v1-reserve16";row["strictJson"]=valid;if(valid)work.Strict++;
      if(parsed.Success)
      {
       var kind=parsed.Intent.Kind;row["actual"]=kind.ToString();row["actualTarget"]=parsed.Intent.TargetName??parsed.Intent.DangerTarget;
       var correct=valid&&kind.ToString()==item["expected"].Value<string>()&&(kind!=LlmIntentKind.Investigate||parsed.Intent.TargetName==item["target"]?.Value<string>())&&(kind!=LlmIntentKind.Warn||parsed.Intent.DangerTarget==item["target"]?.Value<string>());row["semanticCorrect"]=correct;if(correct)work.Semantic++;
      }
     }
     catch(Exception error){row["error"]=error.ToString();}
     finally{previous=Stopwatch.GetTimestamp();row["wallSeconds"]=timer.Elapsed.TotalSeconds;row["nativeCallsTotal"]=diagnostics.Activity.Calls;report["strictJsonCorrect"]=work.Strict;report["semanticCorrect"]=work.Semantic;save();}
     index++;
    }
    report["nativeRunThread"]=diagnostics.Activity.ThreadId;report["nativeCalls"]=diagnostics.Activity.Calls;
   }
   catch(Exception error){work.Error=error.ToString();report["error"]=work.Error;}
   finally
   {
    try{runtime?.Dispose();work.Clean=runtime==null||!((ILocalRuntimeDiagnostics)runtime).Activity.Active;report["disposeThread"]=Thread.CurrentThread.ManagedThreadId;}
    catch(Exception error){work.Error=(work.Error??"")+"\nCleanup: "+error;work.Clean=false;report["cleanupError"]=error.ToString();}
    report["ownerCleanupComplete"]=work.Clean;report["completed"]=true;report["passed"]=work.Error==null&&work.Clean&&work.Strict==18&&work.Semantic>=17;report["seconds"]=clock.Elapsed.TotalSeconds;
    try{save();}catch(Exception error){work.Error=(work.Error??"")+"\nEvidence: "+error;}Volatile.Write(ref work.Done,1);
   }
  }
 }
}
