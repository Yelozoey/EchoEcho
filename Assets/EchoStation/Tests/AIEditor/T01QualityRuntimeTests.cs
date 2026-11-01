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
using EchoStation.Story;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class T01QualityRuntimeTests
 {
  private sealed class Work{public readonly CancellationTokenSource Cancel=new CancellationTokenSource();public int Done,Ready,Strict,Semantic;public bool Clean,Complete;public string Error;}
  private Work active;
  [UnityTest,Timeout(2400000)]public IEnumerator NewFrozenGame100UseRealQwen3AndScoringLabelsNeverReachGeneration()
   =>Execute(Qwen3PromptProfile.V5,"S3-T01c","frozen C100 v5 first validation; no game dispatch, guard correction or script scoring");
  [UnityTest,Timeout(2400000)]public IEnumerator V6SeenC100DiagnosticUsesExplicitCandidateWithoutSelectingIt()
   =>Execute(Qwen3PromptProfile.V6,"S3-T01v6c","seen C100 v6 diagnostic, not independent acceptance; no game dispatch, guard correction or script scoring");
  [UnityTest,Timeout(2400000)]public IEnumerator V7BalancedSeen30IsOnlyAnIterationDiagnostic()
   =>Execute(Qwen3PromptProfile.V7,"S3-T01v7s","seen balanced C30 v7 diagnostic, never independent acceptance; no guard or script scoring",true);
  [UnityTest,Timeout(2400000)]public IEnumerator V7CompleteSeenC100IsNotIndependentAcceptance()
   =>Execute(Qwen3PromptProfile.V7,"S3-T01v7c","seen complete C100 v7 diagnostic, not independent acceptance; no guard or script scoring");
  [UnityTest,Timeout(2400000)]public IEnumerator V7FrozenD100FirstIndependentAcceptance()
   =>Execute(Qwen3PromptProfile.V7,"S3-T01d2","frozen D100 first independent v7 acceptance; no guard, dispatch or script scoring",independent:true);
  [UnityTest,Timeout(2400000)]public IEnumerator V8OfficialSystemRulesUsesSeenCombined60Diagnostic()
   =>Execute(Qwen3PromptProfile.V8,"S3-T01v8s","seen C/D60 v8 system/user diagnostic, never independent acceptance; no guard or script scoring",combined:true);
  [UnityTest,Timeout(2400000)]public IEnumerator V9ShortClassifierWithRealExamplesUsesSeenCombined60Diagnostic()
   =>Execute(Qwen3PromptProfile.V9,"S3-T01v9s","seen C/D60 v9 short task and real message examples diagnostic, never independent acceptance; no guard or script scoring",combined:true);
  [UnityTest,Timeout(2400000)]public IEnumerator V10EnglishLabelDefinitionsUseSeenCombined60Diagnostic()
   =>Execute(Qwen3PromptProfile.V10,"S3-T01v10s","seen C/D60 v10 English label definitions diagnostic, never independent acceptance; no guard or script scoring",combined:true);
  [UnityTest,Timeout(2400000)]public IEnumerator V10OfficialNonThinkingSamplingSeed42UsesSeen60Diagnostic()
   =>Execute(Qwen3PromptProfile.V10,"S3-T01s3","seen C/D60 v10 official non-thinking sampling seed42 diagnostic, never independent acceptance; no guard or script scoring",combined:true,sampling:new TokenSamplingOptions());
  [UnityTest,Timeout(2400000)]public IEnumerator V11GenericBoundaryExamplesUseSeen60GreedyDiagnostic()
   =>Execute(Qwen3PromptProfile.V11,"S3-T01v11s","seen C/D60 v11 generic boundary examples greedy diagnostic, never independent acceptance; no guard or script scoring",combined:true);
  [UnityTest,Timeout(2400000)]public IEnumerator V11FrozenE100FirstIndependentAcceptance()
   =>Execute(Qwen3PromptProfile.V11,"S3-T01e2","frozen E100 first independent v11 greedy acceptance; no guard, dispatch or script scoring",independentE:true);
  private IEnumerator Execute(Qwen3PromptProfile profile,string directory,string scope,bool subset=false,bool independent=false,bool combined=false,TokenSamplingOptions sampling=null,bool independentE=false)
  {
   var count=combined?60:subset?30:100;var work=active=new Work();var main=Thread.CurrentThread.ManagedThreadId;var output=Path.GetFullPath("Docs/Tests/Stage3/Evidence/"+directory+"/Game"+count+"_"+DateTime.UtcNow.Ticks+".json");new Thread(()=>Run(work,main,output,profile,scope,subset,independent,combined,sampling,independentE)){IsBackground=true,Name="S3-T01-Qwen3-Owner",Priority=ThreadPriority.BelowNormal}.Start();var timer=Stopwatch.StartNew();
   try
   {
    while(Volatile.Read(ref work.Done)==0&&timer.Elapsed.TotalSeconds<2390){if(Volatile.Read(ref work.Ready)==0&&timer.Elapsed.TotalSeconds>=30||timer.Elapsed.TotalSeconds>=2370)work.Cancel.Cancel();yield return null;}
    Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1),"Owner still draining: "+output);Assert.That(work.Error,Is.Null,work.Error+"; "+output);Assert.That(work.Clean,Is.True);Assert.That(work.Complete,Is.True);Assert.That(work.Strict,Is.GreaterThanOrEqualTo((int)Math.Ceiling(count*.95)),"Raw JSON threshold: "+output);Assert.That(work.Semantic,Is.GreaterThanOrEqualTo((int)Math.Ceiling(count*.9)),"Raw intent/target threshold: "+output);
   }
   finally{work.Cancel.Cancel();}
  }
  private static void Run(Work work,int main,string output,Qwen3PromptProfile profile,string scope,bool subset,bool independent,bool combined,TokenSamplingOptions sampling,bool independentE)
  {
   var count=combined?60:subset?30:100;var owner=Thread.CurrentThread.ManagedThreadId;ILocalModelRuntime runtime=null;var rows=new JArray();var timer=Stopwatch.StartNew();var report=new JObject{{"scope",scope},{"mainThread",main},{"ownerThread",owner},{"cpuThreads",4},{"suiteSha256",independentE?T01IndependentEFixture.Sha256:combined?T01Diagnostic60.Sha256:independent?T01IndependentFixture.Sha256:T01QualityFixture.Sha256},{"profileId",profile.Id},{"profileLayout",profile.Layout.ToString()},{"promptSha256",profile.Sha256},{"examplesSha256",profile.ExamplesSha256},{"requests",rows},{"finished",false},{"completed",false},{"ownerCleanupComplete",false}};
   report["samplingMode"]=sampling==null?"greedy":TopKTokenSampler.Name;report["temperature"]=sampling?.Temperature??0;report["topK"]=sampling?.TopK??0;report["topP"]=sampling?.TopP??0;report["seed"]=sampling?.Seed??0;
   Action save=()=>{var temporary=output+".tmp";File.WriteAllText(temporary,report.ToString(),new UTF8Encoding(false));if(File.Exists(output))File.Replace(temporary,output,null);else File.Move(temporary,output);};
   try
   {
    Directory.CreateDirectory(Path.GetDirectoryName(output));if(File.Exists(output))throw new IOException("Fresh quality evidence path required");if((subset?1:0)+(independent?1:0)+(combined?1:0)+(independentE?1:0)>1)throw new ArgumentException("Only one explicit suite allowed");var cases=independentE?T01IndependentEFixture.Load().ToArray():combined?T01Diagnostic60.Load().ToArray():independent?T01IndependentFixture.Load().ToArray():subset?T01Diagnostic30.Cases():T01QualityFixture.Load().ToArray();if(cases.Length!=count)throw new ArgumentException("Actual suite count differs");report["expectedAttempts"]=cases.Length;runtime=new LocalQwen3Factory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName),new JsonIntentConstraintProvider(),StationScript.CreateSearch(),profile,sampling).Create(main,work.Cancel.Token);if(owner==main||timer.Elapsed.TotalSeconds>=30)throw new InvalidOperationException("Background/cold deadline contract failed");Volatile.Write(ref work.Ready,1);report["coldSeconds"]=timer.Elapsed.TotalSeconds;var diagnostics=(ILocalRuntimeDiagnostics)runtime;save();long previous=0;
    foreach(var item in cases)
    {
     work.Cancel.Token.ThrowIfCancellationRequested();if(previous!=0)while((Stopwatch.GetTimestamp()-previous)/(double)Stopwatch.Frequency<.3){if(work.Cancel.Token.WaitHandle.WaitOne(1))work.Cancel.Token.ThrowIfCancellationRequested();}
     // This function accepts no expected intent/target; only immutable player input and setup.
     var snapshot=T01ContextSnapshots.Create(item.Input,item.Setup);var row=new JObject{{"id",item.Id},{"input",item.Input},{"setup",item.Setup},{"expected",item.Expected.ToString()},{"expectedTarget",item.Target},{"strictJson",false},{"semanticCorrect",false}};rows.Add(row);save();var request=Stopwatch.StartNew();
     try
     {
      var generated=runtime.Generate(new TokenRequest(rows.Count,snapshot),work.Cancel.Token);var prepared=generated.Prepared;if(prepared==null||generated.PreparationThreadId!=owner)throw new InvalidOperationException("Formal preparation did not stay on Owner");
      if(!ReferenceEquals(generated.SamplingOptions,sampling))throw new InvalidOperationException("Formal runtime did not preserve explicit sampling policy");row["samplingMode"]=generated.SamplingMode;
      var roles=prepared.InputIds.Count(i=>i==151644);if(roles!=(profile.Layout==Qwen3PromptLayout.SingleUser?2:3+2*profile.Examples.Count))throw new InvalidOperationException("Prepared role count does not match explicit profile");row["trustedRoleStarts"]=roles;
      row["body"]=prepared.Body;row["inputIds"]=new JArray(prepared.InputIds);row["inputTokens"]=prepared.InputIds.Count;row["effectiveTargets"]=new JArray(prepared.EffectiveTargets.Select(t=>t.Id));row["usedMemory"]=prepared.UsedMemory;row["raw"]=generated.Text;row["outputIds"]=new JArray(generated.TokenIds);row["seconds"]=generated.Seconds+generated.PreparationSeconds;row["completeEos"]=generated.Complete;row["mode"]=generated.Mode;
      var parsed=IntentJsonParser.Parse(generated.Text);var valid=generated.Complete&&parsed.Success&&generated.Mode=="json-intent-v1-reserve16";row["strictJson"]=valid;if(valid)work.Strict++;
      if(parsed.Success){var kind=parsed.Intent.Kind;var target=parsed.Intent.TargetName??parsed.Intent.DangerTarget;row["actual"]=kind.ToString();row["actualTarget"]=target;var correct=valid&&kind==item.Expected&&target==item.Target;row["semanticCorrect"]=correct;if(correct)work.Semantic++;}
     }
     catch(Exception error){row["error"]=error.ToString();if(work.Cancel.IsCancellationRequested)throw;}
     finally{previous=Stopwatch.GetTimestamp();row["wallSeconds"]=request.Elapsed.TotalSeconds;row["nativeCallsTotal"]=diagnostics.Activity.Calls;report["strictJsonCorrect"]=work.Strict;report["semanticCorrect"]=work.Semantic;save();}
    }
    work.Complete=rows.Count==cases.Length;report["nativeRunThread"]=diagnostics.Activity.ThreadId;report["nativeCalls"]=diagnostics.Activity.Calls;if(diagnostics.Activity.ThreadId!=owner)throw new InvalidOperationException("Native calls were not owned by worker");
   }
   catch(Exception error){work.Error=error.ToString();report["error"]=work.Error;}
   finally
   {
    try{runtime?.Dispose();work.Clean=runtime==null||!((ILocalRuntimeDiagnostics)runtime).Activity.Active;report["disposeThread"]=Thread.CurrentThread.ManagedThreadId;}
    catch(Exception error){work.Error=(work.Error??"")+"\nCleanup:"+error;report["cleanupError"]=error.ToString();work.Clean=false;}
    report["finished"]=true;report["completed"]=work.Complete;report["ownerCleanupComplete"]=work.Clean;report["strictJsonCorrect"]=work.Strict;report["semanticCorrect"]=work.Semantic;report["cancelled"]=work.Cancel.IsCancellationRequested;report["passed"]=work.Error==null&&work.Complete&&work.Clean&&work.Strict>=(int)Math.Ceiling(count*.95)&&work.Semantic>=(int)Math.Ceiling(count*.9);report["seconds"]=timer.Elapsed.TotalSeconds;
    try{save();}catch(Exception error){work.Error=(work.Error??"")+"\nEvidence:"+error;}Volatile.Write(ref work.Done,1);
   }
  }
  [UnityTearDown]public IEnumerator DrainOwner()
  {
   var work=active;if(work==null)yield break;work.Cancel.Cancel();var timer=Stopwatch.StartNew();while(Volatile.Read(ref work.Done)==0&&timer.Elapsed.TotalSeconds<40)yield return null;Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1),"Never change fixtures/scenes while Owner still runs");Assert.That(work.Clean,Is.True);work.Cancel.Dispose();active=null;
  }
 }
}
