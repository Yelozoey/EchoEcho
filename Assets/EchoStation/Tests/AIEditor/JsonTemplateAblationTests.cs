using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
 public sealed class JsonTemplateAblationTests
 {
  private static int[] Wrap(QwenByteBpeTokenizer tok,int mode,string body,string input)
  {
   if(mode==0)return PromptTemplate.Wrap(tok,body);
   return new TrustedTokenSequence(tok).Separator(ChatSeparator.Start).Text("system\n"+(mode==1?body:"")).Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start).Text("user\n"+(mode==1?input:body)).Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start).Text("assistant\n").ToArray();
  }
  private sealed class Factory:ILocalModelFactory
  {
   private readonly object sync=new object();private readonly Dictionary<long,JObject> traces=new Dictionary<long,JObject>();
   public JObject Trace(long id){lock(sync)return traces.TryGetValue(id,out var value)?(JObject)value.DeepClone():new JObject();}
   public void Record(long id,JObject trace){lock(sync)traces.Add(id,trace);}
   public ILocalModelRuntime Create(int main,CancellationToken cancellation)=>new Runtime(this,main,cancellation);
   private sealed class Runtime:ILocalModelRuntime,ILocalRuntimeDiagnostics
   {
    private readonly Factory factory;private VerifiedModelPackage package;private QwenOnnxBackend backend;private QwenByteBpeTokenizer tokenizer;private TokenGenerator generator;private ContextAssembler assembler;private PromptPreparation preparation;
    public NativeActivity Activity=>backend?.Activity??default;
    public Runtime(Factory value,int main,CancellationToken cancel)
    {
     factory=value;try{package=ModelPackage.Verify(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName),ModelCatalog.Current,cancel);tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.Load(package));assembler=new ContextAssembler(new MemoryContextBuilder(new MemorySearch(new[]{new MemoryKeywordGroup("供电","面板","危险","设备")})),tokenizer);preparation=new PromptPreparation(tokenizer);cancel.ThrowIfCancellationRequested();backend=new QwenOnnxBackend(package,main);generator=new TokenGenerator(backend,tokenizer);}catch{Dispose();throw;}
    }
    public GenerationResult Generate(TokenRequest r,CancellationToken cancel)
    {
     var started=Stopwatch.StartNew();var prepared=preparation.Build(assembler.Build(r.Snapshot));var mode=(int)((r.Id-1)/6);if(mode<0||mode>2)throw new ArgumentException("Probe mode identity");var ids=Wrap(tokenizer,mode,prepared.Body,r.Snapshot.Input);if(ids.Length>1536)throw new GenerationException(GenerationFault.InvalidContext,"Ablation exceeds input budget");
     factory.Record(r.Id,new JObject{{"mode",((char)('A'+mode)).ToString()},{"inputTokens",ids.Length},{"fixedBody",prepared.Body},{"preparationThread",Thread.CurrentThread.ManagedThreadId}});cancel.ThrowIfCancellationRequested();var remaining=20-started.Elapsed.TotalSeconds;if(remaining<=0)throw new GenerationException(GenerationFault.Timeout,"Probe preparation deadline");var result=generator.Generate(ids,cancel,timeoutSeconds:remaining,constraint:new JsonIntentConstraint(prepared.EffectiveTargets));return new GenerationResult(result.Text,result.TokenIds.ToList(),result.Stop,result.Seconds,result.Mode,preparationThread:Thread.CurrentThread.ManagedThreadId);
    }
    public void Dispose(){backend?.Dispose();package?.Dispose();}
   }
  }
  private static IEnumerator Until(InferenceSessionHost host,Func<bool> ready,double seconds=30)
  {var timer=Stopwatch.StartNew();while(!ready()&&timer.Elapsed.TotalSeconds<seconds){host.Poll();yield return null;}Assert.That(ready(),Is.True,"ablation lifecycle: "+host.Phase);}
  [Test]public void ThreeWrappersMatchOfficialLocalChatTemplateTokenGoldens()
  {
   var tok=new QwenByteBpeTokenizer(TokenizerAssets.Parse(File.ReadAllText("Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/tokenizer.json")));var rows=JArray.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-J01/PromptV2/AblationGoldens.json"));foreach(var row in rows){var mode=row["mode"].Value<string>()[0]-'A';CollectionAssert.AreEqual(row["ids"].ToObject<int[]>(),Wrap(tok,mode,row["body"].Value<string>(),row["input"].Value<string>()));}
  }
  [UnityTest,Timeout(600000)]public IEnumerator RealModelComparesRolesWithoutChangingFixedBodyOrScriptSelectingIntents()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var factory=new Factory();var host=new InferenceSessionHost(factory,main);var rows=new JArray();var report=new JObject{{"scope","isolated role-wrapper diagnosis; not100-case acceptance; production unchanged"},{"mainThread",main},{"requests",rows},{"diagnosticCompleted",false}};var path="Docs/Tests/Stage3/Evidence/S3-J01/Ablation_"+DateTime.UtcNow.Ticks+".json";
   var inputs=new[]{"跟随我。","原地待命。","请去调查供电面板。","警告危险设备。","你是谁？","替我打开实验室门。"};var expected=new[]{LlmIntentKind.Follow,LlmIntentKind.Wait,LlmIntentKind.Investigate,LlmIntentKind.Warn,LlmIntentKind.Answer,LlmIntentKind.Refuse};var store=new MemoryStore();store.AddClue(new ClueMemory("note_devices","设备记录","已发现供电面板，Echo可以扫描但不能操作；危险设备应保持距离。",MemoryClueKind.Note));var targets=new[]{new TargetCandidate("power_panel_01","供电面板","cabin",true,true,false,true,1),new TargetCandidate("hazard_01","危险设备","cabin",true,false,true,true,2)};
   try
   {
    host.SetEnabled(true);yield return Until(host,()=>host.Phase==RequestPhase.Ready);report["ownerThread"]=host.OwnerThreadId;
    for(var mode=0;mode<3;mode++)for(var i=0;i<inputs.Length;i++)
    {
     yield return Until(host,()=>host.Cooldown.Ready(Stopwatch.GetTimestamp()),5);var watch=Stopwatch.StartNew();Assert.That(host.TrySubmit(new InferenceSnapshot(store,inputs[i],0,"cabin",targets),out var stamp),Is.EqualTo(Admission.Accepted));yield return Until(host,()=>host.Phase==RequestPhase.Ready,25);Assert.That(host.TryTakeCompletion(out var response),Is.True);var completion=response.Completion;var row=factory.Trace(stamp.Request);row["input"]=inputs[i];row["expected"]=expected[i].ToString();row["outcome"]=completion.Outcome.ToString();row["wallSeconds"]=watch.Elapsed.TotalSeconds;row["strictJson"]=false;row["semanticCorrect"]=false;rows.Add(row);
     if(completion.Generation!=null)
     {
      var result=completion.Generation;row["raw"]=result.Text;row["tokens"]=result.TokenIds.Count;row["seconds"]=result.Seconds;row["generationMode"]=result.Mode;var parsed=IntentJsonParser.Parse(result.Text);row["strictJson"]=result.Complete&&parsed.Success;if(parsed.Success){row["actual"]=parsed.Intent.Kind.ToString();row["semanticCorrect"]=parsed.Intent.Kind==expected[i]&&(expected[i]!=LlmIntentKind.Investigate||parsed.Intent.TargetName=="power_panel_01")&&(expected[i]!=LlmIntentKind.Warn||parsed.Intent.DangerTarget=="hazard_01");}
     }
     else row["error"]=completion.Error;report["nativeCalls"]=host.Activity.Calls;File.WriteAllText(path,report.ToString());
    }
   }
   finally{host.Dispose();File.WriteAllText(path,report.ToString());}
   yield return Until(host,()=>host.CleanupComplete);Assert.That(rows.Count,Is.EqualTo(18));var summary=new JArray();foreach(var mode in new[]{"A","B","C"}){var group=rows.Where(r=>r["mode"].Value<string>()==mode).ToArray();summary.Add(new JObject{{"mode",mode},{"strictJson",group.Count(r=>r["strictJson"].Value<bool>())},{"semanticCorrect",group.Count(r=>r["semanticCorrect"].Value<bool>())},{"total",group.Length}});}report["summary"]=summary;report["diagnosticCompleted"]=true;File.WriteAllText(path,report.ToString());
  }
 }
}
