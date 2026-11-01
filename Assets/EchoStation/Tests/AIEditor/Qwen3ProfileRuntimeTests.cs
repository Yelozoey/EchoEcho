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
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3ProfileRuntimeTests
 {
  private sealed class Work{public readonly CancellationTokenSource Cancel=new CancellationTokenSource();public int Done,Ready;public bool Clean,Passed;public string Error;}
  private Work active;
  [UnityTest,Timeout(120000)]public IEnumerator ExplicitProfileReachesFormalBackgroundPreparationAndNativeGeneration()
  {
   var work=active=new Work();var main=Thread.CurrentThread.ManagedThreadId;var output=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-T01p2/Profile_"+DateTime.UtcNow.Ticks+".json");
   new Thread(()=>Run(work,main,output)){IsBackground=true,Name="Q3-Profile-Owner",Priority=ThreadPriority.BelowNormal}.Start();var timer=Stopwatch.StartNew();
   while(Volatile.Read(ref work.Done)==0&&timer.Elapsed.TotalSeconds<100){if(Volatile.Read(ref work.Ready)==0&&timer.Elapsed.TotalSeconds>=30||timer.Elapsed.TotalSeconds>=75)work.Cancel.Cancel();yield return null;}
   Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1),output);Assert.That(work.Error,Is.Null,work.Error);Assert.That(work.Clean&&work.Passed,Is.True,output);
  }
  private static void Run(Work work,int main,string output)
  {
   var owner=Thread.CurrentThread.ManagedThreadId;ILocalModelRuntime runtime=null;var timer=Stopwatch.StartNew();var profile=new Qwen3PromptProfile("profile_runtime_test","版本校验标记：PROFILE_TEST_01\n"+Qwen3PromptProfile.V5.Text);var report=new JObject{{"scope","explicit profile transport, one real Qwen3 request; not quality acceptance"},{"mainThread",main},{"ownerThread",owner},{"profileId",profile.Id},{"profileSha256",profile.Sha256},{"completed",false}};
   try
   {
    Directory.CreateDirectory(Path.GetDirectoryName(output));if(File.Exists(output))throw new IOException("Fresh evidence required");
    runtime=new LocalQwen3Factory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName),new JsonIntentConstraintProvider(),promptProfile:profile).Create(main,work.Cancel.Token);report["coldSeconds"]=timer.Elapsed.TotalSeconds;if(timer.Elapsed.TotalSeconds>=30)throw new TimeoutException("Cold deadline exceeded");Volatile.Write(ref work.Ready,1);
    var snapshot=new InferenceSnapshot(new MemoryStore(),"请跟随我。",0,"cabin",Array.Empty<TargetCandidate>());var result=runtime.Generate(new TokenRequest(1,snapshot),work.Cancel.Token);var prepared=result.Prepared;
    var tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));
    var context="相关记忆（摘要）：\n\n允许目标（ID/名称/允许行为）：\n[]";var body=profile.Fill(context,snapshot.Input);var sameBody=prepared.Body==body;var sameIds=prepared.InputIds.SequenceEqual(profile.Wrap(tokenizer,body));var parsed=IntentJsonParser.Parse(result.Text);var diagnostics=(ILocalRuntimeDiagnostics)runtime;
    report["body"]=prepared.Body;report["inputIds"]=new JArray(prepared.InputIds);report["sameExplicitBody"]=sameBody;report["sameExplicitInputIds"]=sameIds;report["raw"]=result.Text;report["nativeThread"]=diagnostics.Activity.ThreadId;report["nativeCalls"]=diagnostics.Activity.Calls;report["preparationThread"]=result.PreparationThreadId;report["requestSeconds"]=result.Seconds+result.PreparationSeconds;
    work.Passed=owner!=main&&sameBody&&sameIds&&result.PreparationThreadId==owner&&diagnostics.Activity.ThreadId==owner&&diagnostics.Activity.Calls>0&&result.Complete&&parsed.Success&&parsed.Intent.Kind==LlmIntentKind.Follow&&Qwen3PromptProfile.Selected==Qwen3PromptProfile.V5;
   }
   catch(Exception error){work.Error=error.ToString();report["error"]=work.Error;}
   finally
   {
    try{runtime?.Dispose();work.Clean=runtime==null||!((ILocalRuntimeDiagnostics)runtime).Activity.Active;report["disposeThread"]=Thread.CurrentThread.ManagedThreadId;}catch(Exception error){work.Error=(work.Error??"")+error;report["cleanupError"]=error.ToString();}
    report["ownerCleanupComplete"]=work.Clean;report["completed"]=true;report["passed"]=work.Passed&&work.Clean&&work.Error==null;report["seconds"]=timer.Elapsed.TotalSeconds;
    try{File.WriteAllText(output,report.ToString(),new UTF8Encoding(false));}catch(Exception error){work.Error=(work.Error??"")+error;}Volatile.Write(ref work.Done,1);
   }
  }
  [UnityTearDown]public IEnumerator Drain()
  {var work=active;if(work==null)yield break;work.Cancel.Cancel();var timer=Stopwatch.StartNew();while(Volatile.Read(ref work.Done)==0&&timer.Elapsed.TotalSeconds<40)yield return null;Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1));Assert.That(work.Clean,Is.True);work.Cancel.Dispose();active=null;}
 }
}
