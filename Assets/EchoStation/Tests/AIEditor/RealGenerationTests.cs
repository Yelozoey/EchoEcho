using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class RealGenerationTests
 {
  [UnityTest]public IEnumerator ActualLocalModelCompletesRepliesWithFreshRequestCache()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var folder=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-A04");var output=Path.Combine(folder,"RealGeneration_"+DateTime.UtcNow.Ticks+".json");var packagePath=Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName);
   var fixture=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-G01/PrefillFixture.json"));var original=fixture["prompt"].Value<string>();
   var thread=new Thread(()=>
   {
    var report=new JObject{{"mainThread",main},{"ownerThread",Thread.CurrentThread.ManagedThreadId},{"scriptFallbackUsed",false}};var requests=new JArray();report["requests"]=requests;
    try
    {
     var cold=System.Diagnostics.Stopwatch.StartNew();
     using(var package=ModelPackage.Verify(packagePath,ModelCatalog.Current))
     {
      var vocabulary=new QwenByteBpeTokenizer(TokenizerAssets.Load(package));
      using(var backend=new QwenOnnxBackend(package,main))
      {
       report["coldSeconds"]=cold.Elapsed.TotalSeconds;var generator=new TokenGenerator(backend,vocabulary);
       foreach(var question in new[]{"请去调查供电面板。","请跟随我。","请去调查供电面板。"})
       {
        var prompt=original.Replace("请去调查供电面板。",question);var ids=new TrustedTokenSequence(vocabulary).Separator(ChatSeparator.Start).Text("system\nYou are Qwen, created by Alibaba Cloud. You are a helpful assistant.").Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start).Text("user\n"+prompt).Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start).Text("assistant\n").ToArray();
        var before=backend.NativeCalls;var result=generator.Generate(ids);var native=backend.NativeCalls-before;
        if(!result.Complete||native!=result.TokenIds.Count||backend.ConsumedTokens!=0)throw new InvalidDataException("Actual generation EOS/calls/reset mismatch");
        requests.Add(new JObject{{"question",question},{"raw",result.Text},{"tokens",new JArray(result.TokenIds)},{"inputTokens",ids.Length},{"nativeCalls",native},{"seconds",result.Seconds},{"stop",result.Stop.ToString()},{"mode",result.Mode},{"cacheAfter",backend.ConsumedTokens}});
       }
       if(requests[0]["raw"].Value<string>()!=requests[2]["raw"].Value<string>())throw new InvalidDataException("Greedy fresh-cache repeat differed");
      }
     }
     report["passed"]=true;
    }
    catch(Exception error){report["passed"]=false;report["error"]=error.ToString();}
    finally{report["releasedOnOwner"]=Thread.CurrentThread.ManagedThreadId;File.WriteAllText(output,report.ToString());}
   }){IsBackground=true,Name="Echo-A04-Real-Generator"};
   thread.Start();var timer=System.Diagnostics.Stopwatch.StartNew();while(thread.IsAlive&&timer.Elapsed.TotalSeconds<100)yield return null;
   Assert.That(thread.IsAlive,Is.False,"Real generation owner did not release");var data=JObject.Parse(File.ReadAllText(output));Assert.That((bool)data["passed"],Is.True,data["error"]?.ToString());Assert.That(data["requests"].Count(),Is.EqualTo(3));
  }
 }
}
