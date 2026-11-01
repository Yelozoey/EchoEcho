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
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using EchoStation.Story;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3ConstraintAblationTests
 {
  private sealed class Work{public readonly CancellationTokenSource Cancel=new CancellationTokenSource();public int Done,Ready,Attempts,Errors;public bool Clean;public string Error;}
  private Work active;
  [UnityTest,Timeout(1200000)]public IEnumerator SameSixInputsCompareCompactGrammarWithUnconstrainedGreedyOnlyInIsolation()
  {
   var work=active=new Work();var main=Thread.CurrentThread.ManagedThreadId;var output=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-T01g1/Ablation_"+DateTime.UtcNow.Ticks+".json");new Thread(()=>Run(work,main,output)){IsBackground=true,Name="Q3-Constraint-Ablation",Priority=ThreadPriority.BelowNormal}.Start();var timer=Stopwatch.StartNew();
   while(Volatile.Read(ref work.Done)==0&&timer.Elapsed.TotalSeconds<1190){if(Volatile.Read(ref work.Ready)==0&&timer.Elapsed.TotalSeconds>=30||timer.Elapsed.TotalSeconds>=1170)work.Cancel.Cancel();yield return null;}
   Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1),output);Assert.That(work.Error,Is.Null,work.Error);Assert.That(work.Clean,Is.True);Assert.That(work.Attempts,Is.EqualTo(12));Assert.That(work.Errors,Is.Zero,"Diagnostic generation faults: "+output);
  }
  private static void Run(Work work,int main,string output)
  {
   var owner=Thread.CurrentThread.ManagedThreadId;VerifiedModelPackage package=null;Qwen3OnnxBackend backend=null;var timer=Stopwatch.StartNew();var rows=new JArray();var report=new JObject{{"scope","isolated same-input compact grammar ablation; never game dispatch or semantic acceptance"},{"mainThread",main},{"ownerThread",owner},{"profileId",Qwen3PromptProfile.V9.Id},{"promptSha256",Qwen3PromptProfile.V9.Sha256},{"examplesSha256",Qwen3PromptProfile.V9.ExamplesSha256},{"requests",rows},{"completed",false},{"ownerCleanupComplete",false}};
   Action save=()=>{var tmp=output+".tmp";File.WriteAllText(tmp,report.ToString(),new UTF8Encoding(false));if(File.Exists(output))File.Replace(tmp,output,null);else File.Move(tmp,output);};
   try
   {
    Directory.CreateDirectory(Path.GetDirectoryName(output));if(File.Exists(output))throw new IOException("Fresh diagnostic evidence required");
    var source=T01QualityFixture.Load().Concat(T01IndependentFixture.Load()).ToDictionary(c=>c.Id);var ids=new[]{"T01_C_001","T01_C_018","T01_C_048","T01_C_057","T01_C_061","T01_D_098"};var previous=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-T01v9s/Game60_639286831772464050.json"))["requests"].ToDictionary(r=>r.Value<string>("id"));
    package=ModelPackage.Verify(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName),Qwen3Catalog.Current,work.Cancel.Token);var tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.LoadQwen3(package));var assembler=new ContextAssembler(new MemoryContextBuilder(StationScript.CreateSearch()),tokenizer);var preparation=new Qwen3PromptPreparation(tokenizer,Qwen3PromptProfile.V9);backend=new Qwen3OnnxBackend(package,main,4);var generator=new TokenGenerator(backend,tokenizer);report["coldSeconds"]=timer.Elapsed.TotalSeconds;if(timer.Elapsed.TotalSeconds>=30)throw new TimeoutException("Cold deadline exceeded");Volatile.Write(ref work.Ready,1);save();long finished=0;
    foreach(var id in ids)
    {
     var item=source[id];var prepared=preparation.Build(assembler.Build(T01ContextSnapshots.Create(item.Input,item.Setup)));if(!prepared.InputIds.SequenceEqual(previous[id]["inputIds"].Values<int>()))throw new InvalidDataException("Ablation input differs from formal v9: "+id);
     foreach(var constrained in new[]{true,false})
     {
      work.Cancel.Token.ThrowIfCancellationRequested();if(finished!=0)while((Stopwatch.GetTimestamp()-finished)/(double)Stopwatch.Frequency<.3){if(work.Cancel.Token.WaitHandle.WaitOne(1))work.Cancel.Token.ThrowIfCancellationRequested();}
      var row=new JObject{{"id",id},{"input",item.Input},{"expected",item.Expected.ToString()},{"expectedTarget",item.Target},{"compactGrammar",constrained},{"inputIds",new JArray(prepared.InputIds)},{"sameFormalInputIds",true},{"strictJson",false},{"semanticCorrect",false}};rows.Add(row);save();var wall=Stopwatch.StartNew();
      try
      {
       if(backend.ConsumedTokens!=0)throw new InvalidOperationException("Cross-request KV found");var result=generator.Generate(prepared.InputIds,work.Cancel.Token,constraint:constrained?new JsonIntentConstraint(prepared.EffectiveTargets):null);row["raw"]=result.Text;row["outputIds"]=new JArray(result.TokenIds);row["mode"]=result.Mode;row["completeEos"]=result.Complete;row["seconds"]=result.Seconds;var parsed=IntentJsonParser.Parse(result.Text);var strict=result.Complete&&parsed.Success;row["strictJson"]=strict;if(parsed.Success){var target=parsed.Intent.TargetName??parsed.Intent.DangerTarget;row["actual"]=parsed.Intent.Kind.ToString();row["actualTarget"]=target;row["semanticCorrect"]=strict&&parsed.Intent.Kind==item.Expected&&target==item.Target;}if(constrained)row["samePreviousConstrainedTokens"]=result.TokenIds.SequenceEqual(previous[id]["outputIds"].Values<int>());
      }
      catch(Exception error){work.Errors++;row["error"]=error.ToString();if(work.Cancel.IsCancellationRequested)throw;}
      finally{finished=Stopwatch.GetTimestamp();work.Attempts++;row["wallSeconds"]=wall.Elapsed.TotalSeconds;row["cacheAfter"]=backend.ConsumedTokens;row["nativeThread"]=backend.Activity.ThreadId;save();}
     }
    }
    report["mainNativeCalls"]=backend.MainThreadNativeCalls;report["nativeCalls"]=backend.TotalNativeCalls;
   }
   catch(Exception error){work.Error=error.ToString();report["error"]=work.Error;}
   finally
   {
    try{backend?.Dispose();package?.Dispose();work.Clean=(backend==null||backend.IsDisposed&&!backend.Activity.Active&&backend.EmptyPastCreated==backend.EmptyPastDisposed&&backend.CollectionsCreated==backend.CollectionsDisposed&&backend.MainThreadNativeCalls==0)&&(package==null||package.IsDisposed);report["disposeThread"]=Thread.CurrentThread.ManagedThreadId;}catch(Exception error){work.Error=(work.Error??"")+error;report["cleanupError"]=error.ToString();}
    report["completed"]=work.Attempts==12;report["ownerCleanupComplete"]=work.Clean;report["attempts"]=work.Attempts;report["generationErrors"]=work.Errors;report["seconds"]=timer.Elapsed.TotalSeconds;try{save();}catch(Exception error){work.Error=(work.Error??"")+error;}Volatile.Write(ref work.Done,1);
   }
  }
  [UnityTearDown]public IEnumerator Drain(){var work=active;if(work==null)yield break;work.Cancel.Cancel();var timer=Stopwatch.StartNew();while(Volatile.Read(ref work.Done)==0&&timer.Elapsed.TotalSeconds<40)yield return null;Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1));Assert.That(work.Clean,Is.True);work.Cancel.Dispose();active=null;}
 }
}
