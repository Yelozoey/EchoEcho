using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
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
 public sealed class RealJsonGenerationTests
 {
  private static IEnumerator Until(InferenceSessionHost host,Func<bool> ready,double seconds=30)
  {var clock=Stopwatch.StartNew();while(!ready()&&clock.Elapsed.TotalSeconds<seconds){host.Poll();yield return null;}Assert.That(ready(),Is.True,"real JSON transition "+host.Phase);}
  [UnityTest,Timeout(600000)]public IEnumerator RealLogitsProduceLockedExpandedObjectsAndReportSemanticsSeparately()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var search=new MemorySearch(new[]{new MemoryKeywordGroup("供电","面板","危险","设备")});var host=new InferenceSessionHost(new LocalQwenFactory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName),search,new JsonIntentConstraintProvider()),main);var rows=new JArray();var report=new JObject{{"mainThread",main},{"requests",rows},{"passed",false},{"scope","JSON structure only; locked 18-case preliminary semantics are not T01 acceptance"}};var path="Docs/Tests/Stage3/Evidence/S3-J01/PromptV2/Expanded_"+DateTime.UtcNow.Ticks+".json";
   var caseBytes=File.ReadAllBytes("Docs/Stage3/J01_V2_ExpandedCases.json");var cases=(JArray)JObject.Parse(Encoding.UTF8.GetString(caseBytes))["cases"];Assert.That(cases.Count,Is.EqualTo(18));using(var sha=SHA256.Create()){report["promptSha256"]=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(PromptTemplate.Text))).Replace("-","").ToLowerInvariant();report["caseSha256"]=BitConverter.ToString(sha.ComputeHash(caseBytes)).Replace("-","").ToLowerInvariant();}
   var store=new MemoryStore();store.AddClue(new ClueMemory("note_devices","设备记录","已发现供电面板，Echo可以扫描但不能操作；危险设备应保持距离。",MemoryClueKind.Note));var targets=new[]{new TargetCandidate("power_panel_01","供电面板","cabin",true,true,false,true,1),new TargetCandidate("hazard_01","危险设备","cabin",true,false,true,true,2)};var semantic=0;
   try
   {
    host.SetEnabled(true);yield return Until(host,()=>host.Phase==RequestPhase.Ready);report["ownerThread"]=host.OwnerThreadId;
    for(var i=0;i<cases.Count;i++)
    {
     var input=cases[i]["input"].Value<string>();var expected=(LlmIntentKind)Enum.Parse(typeof(LlmIntentKind),cases[i]["expected"].Value<string>());var target=cases[i]["target"]?.Value<string>();yield return Until(host,()=>host.Cooldown.Ready(Stopwatch.GetTimestamp()),5);var started=Stopwatch.StartNew();Assert.That(host.TrySubmit(new InferenceSnapshot(store,input,i,"cabin",targets),out _),Is.EqualTo(Admission.Accepted));yield return Until(host,()=>host.Phase==RequestPhase.Ready,25);Assert.That(host.TryTakeCompletion(out var delivery),Is.True);var completion=delivery.Completion;
     var row=new JObject{{"input",input},{"expected",expected.ToString()},{"outcome",completion.Outcome.ToString()},{"wallSeconds",started.Elapsed.TotalSeconds}};rows.Add(row);File.WriteAllText(path,report.ToString());Assert.That(completion.Outcome,Is.EqualTo(RequestOutcome.Succeeded),completion.Error);var generated=completion.Generation;row["raw"]=generated.Text;row["tokens"]=generated.TokenIds.Count;row["seconds"]=generated.Seconds;row["mode"]=generated.Mode;row["inputTokens"]=generated.Prepared.InputIds.Count;row["preparationThread"]=generated.PreparationThreadId;
     Assert.That(generated.Complete,Is.True);Assert.That(generated.Mode,Is.EqualTo("json-intent-v1-reserve16"));Assert.That(generated.PreparationThreadId,Is.EqualTo(host.OwnerThreadId));Assert.That(host.Activity.ThreadId,Is.Not.EqualTo(main));var parsed=IntentJsonParser.Parse(generated.Text);row["strictJson"]=parsed.Success;Assert.That(parsed.Success,Is.True,generated.Text);var okay=parsed.Intent.Kind==expected&&(expected!=LlmIntentKind.Investigate||parsed.Intent.TargetName==target)&&(expected!=LlmIntentKind.Warn||parsed.Intent.DangerTarget==target);if(okay)semantic++;row["actual"]=parsed.Intent.Kind.ToString();row["semanticCorrect"]=okay;report["preliminarySemanticCorrect"]=semantic;report["nativeCalls"]=host.Activity.Calls;File.WriteAllText(path,report.ToString());
    }
   }
   finally{host.Dispose();File.WriteAllText(path,report.ToString());}
   yield return Until(host,()=>host.CleanupComplete);report["strictJsonPassed"]=cases.Count;report["preliminarySemanticTotal"]=cases.Count;report["passed"]=true;File.WriteAllText(path,report.ToString());
  }
 }
}
