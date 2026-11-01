// ISOLATED REJECTED-CANDIDATE DIAGNOSTIC. Qwen3_Json_A failed the unchanged 17/18 gate.
// Explicitly select this fixture/test for a deliberate rerun after the usual module_tests.py
// clean-scene/Console/RenderSafety preflight. An ignored/skipped test is not a passing rerun.
// Requires Microsoft.ML.OnnxRuntime.dll in AI.Editor.Tests.precompiledReferences.
// Does not adapt the production package, tokenizer, wrapper, or backend.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using EchoStation.AI.Context;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 // Deliberate experiment runner. ActiveRun.json is a fresh archived experiment configuration.
 public sealed class Qwen3TuningTests
 {
  private const string Evidence="Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3";
  private const string Model="Library/EchoStation/S3Cache/models/qwen3-candidate/onnx/onnx/model_q4_last.onnx";
  private const string ModelSha="3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303";
  private const string SourceSha="d43d836fc5e240df9013733ccd214972c5d21bd9ec47e574e4f1e359cf90aed0";
  private const string PromptSha="5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3";
  private sealed class Work
  {
   public readonly CancellationTokenSource Cancel=new CancellationTokenSource();
   public int Done,Ready,Owner,Strict,Semantic,Total,Exact;public string Error,Mode;public bool Clean;
  }
  [UnityTest,Timeout(2400000)]
  public IEnumerator ActiveExperimentMeetsItsDeclaredGate()
  {
   var main=Thread.CurrentThread.ManagedThreadId;
   var output=Path.GetFullPath(Evidence+"/Tuning/UnityTuning_"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ")+"_"+Guid.NewGuid().ToString("N")+".json");
   var work=new Work();var worker=new Thread(()=>Run(work,main,output)){IsBackground=true,Name="S3-Qwen3-Candidate-Owner",Priority=ThreadPriority.BelowNormal};
   worker.Start();var clock=Stopwatch.StartNew();
   try
   {
    while(Volatile.Read(ref work.Done)==0&&clock.Elapsed.TotalSeconds<2390)
    {
     // Native cold load can be non-interruptible. Cancel only; never Join/Abort/Dispose here.
     if(Volatile.Read(ref work.Ready)==0&&clock.Elapsed.TotalSeconds>=30)work.Cancel.Cancel();
     yield return null;
    }
    Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1),"Owner still draining; partial report: "+output);
    Assert.That(work.Owner,Is.Not.EqualTo(main));Assert.That(work.Clean,Is.True,"Owner-only resource cleanup: "+output);
    Assert.That(work.Error,Is.Null,"Candidate diagnostic failed: "+work.Error+"; "+output);
    Assert.That(work.Strict,Is.EqualTo(work.Total),"Structure is independent of semantics; inspect raw: "+output);
    if(work.Mode=="baseline_v2")
    {Assert.That(work.Total,Is.EqualTo(18));Assert.That(work.Exact,Is.EqualTo(18),"Optimized checks must preserve every A output token");Assert.That(work.Semantic,Is.EqualTo(15),"Baseline reproduces the rejected result; it is not a quality pass");}
    else if(work.Mode=="smoke18"){Assert.That(work.Total,Is.EqualTo(18));Assert.That(work.Semantic,Is.GreaterThanOrEqualTo(17),"Unchanged candidate gate");}
    else {Assert.That(work.Mode,Is.EqualTo("validation100"));Assert.That(work.Total,Is.EqualTo(100));Assert.That(work.Semantic,Is.GreaterThanOrEqualTo(90),"Unchanged independent validation gate");}
   }
   finally
   {
    work.Cancel.Cancel();
    // If a timed-out native call still runs, its Owner saves and disposes after return.
    // Do not dispose this CTS until the Owner is done using its token/wait handle.
    if(Volatile.Read(ref work.Done)==1)work.Cancel.Dispose();
   }
  }
  private static void Run(Work work,int main,string path)
  {
   work.Owner=Thread.CurrentThread.ManagedThreadId;var rows=new JArray();
   var report=new JObject{{"scope","isolated Unity candidate leaf; not production tokenizer, T01, Windows, safety or performance acceptance"},{"mainThread",main},{"ownerThread",work.Owner},{"pid",Process.GetCurrentProcess().Id},{"cpuThreads",2},{"interOpThreads",1},{"enableThinking",false},{"newAddedOutputMarkersDisabled",new JArray(151665,151666,151667,151668)},{"tokenizerLimitation","Qwen2.5 shared ordinary byte vocabulary for output only; official HF Qwen3 input IDs consumed verbatim, no old wrapper encoding"},{"requiredSemanticCorrect",17},{"requiredTotal",18},{"requests",rows},{"completed",false},{"passed",false}};
   CandidateBackend backend=null;FileStream tokenizerLease=null,modelLease=null;var claimed=false;
   Action save=()=>{if(claimed)File.WriteAllText(path,report.ToString(),new UTF8Encoding(false));};
   try
   {
    using(var unique=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){}claimed=true;
    save();Need(work.Owner!=main,"Owner must differ from Unity main thread");var cold=Stopwatch.StartNew();
    var baseCaseBytes=File.ReadAllBytes(Evidence+"/CandidateCases.json");
    var configBytes=File.ReadAllBytes(Evidence+"/Tuning/ActiveRun.json");var config=JObject.Parse(Encoding.UTF8.GetString(configBytes));report["configSha256"]=Hash(configBytes);report["configuration"]=config.DeepClone();var cpuThreads=Qwen3TuningSettings.CpuThreads(config);report["cpuThreads"]=cpuThreads;
    work.Mode=config["mode"].Value<string>();Need(work.Mode=="baseline_v2"||work.Mode=="smoke18"||work.Mode=="validation100","Unsupported experiment mode");
    var casePath=Path.GetFullPath(config["caseFile"].Value<string>());var tuningRoot=Path.GetFullPath(Evidence+"/Tuning")+Path.DirectorySeparatorChar;Need(casePath.StartsWith(tuningRoot,StringComparison.OrdinalIgnoreCase),"Experiment cases must stay in tuning evidence");
    var caseBytes=File.ReadAllBytes(casePath);report["caseSha256"]=Hash(caseBytes);Need(report["caseSha256"].Value<string>()==config["caseSha256"].Value<string>(),"Experiment case hash differs");var source=JObject.Parse(Encoding.UTF8.GetString(caseBytes));
    Need(source["enableThinking"].Value<bool>()==false&&source["coreVocabularyIdentical"].Value<bool>(),"Pinned candidate input mode differs");Need(source["promptSha256"].Value<string>()==config["promptSha256"].Value<string>(),"Experiment prompt SHA differs");
    var promptPath=Path.GetFullPath(config["promptFile"].Value<string>());Need(promptPath.StartsWith(tuningRoot,StringComparison.OrdinalIgnoreCase)&&Hash(File.ReadAllBytes(promptPath))==config["promptSha256"].Value<string>(),"Prompt bytes differ");
    var cases=(JArray)source["cases"];work.Total=cases.Count;Need(cases.Count==(work.Mode=="validation100"?100:18),"Fixed experiment case count");
    report["experimentMode"]=work.Mode;report["requiredTotal"]=work.Total;report["requiredSemanticCorrect"]=work.Mode=="baseline_v2"?JValue.CreateNull():new JValue(work.Mode=="smoke18"?17:90);
    var baseline=JObject.Parse(File.ReadAllText(Evidence+"/UnityJson_20261026T1355217488364Z_25156104e17440a3a90ec0157f06a10d.json"));var baselineRows=(JArray)baseline["requests"];
    if(work.Mode=="baseline_v2")Need(source["promptSha256"].Value<string>()==PromptSha&&Hash(caseBytes)==Hash(baseCaseBytes),"Baseline inputs must be exact original v2");
    var transformBytes=File.ReadAllBytes(Evidence+"/LastHeadTransformation.json");report["transformationRecordSha256"]=Hash(transformBytes);var transform=JObject.Parse(Encoding.UTF8.GetString(transformBytes));
    Need(transform["sourceSha256"].Value<string>()==SourceSha&&transform["sha256"].Value<string>()==ModelSha&&transform["weightsUnchanged"].Value<bool>(),"Candidate transformation binding differs");
    // Root produces B with actual-file bindings. A alone did not identify the files.
    var equivalenceBytes=File.ReadAllBytes(Evidence+"/Equivalence_B.json");report["equivalenceRecordSha256"]=Hash(equivalenceBytes);var equivalence=JObject.Parse(Encoding.UTF8.GetString(equivalenceBytes));
    Need(equivalence["completed"].Value<bool>()&&equivalence["passed"].Value<bool>()&&equivalence["mode"].Value<string>()=="equivalence","Bound numerical equivalence leaf must pass first");
    Need(equivalence["bindingVersion"].Value<int>()==1&&equivalence["bindingsStable"].Value<bool>(),"Numerical binding audit required");var binding=(JObject)equivalence["bindingsStart"];
    Need(JToken.DeepEquals(binding,equivalence["bindingsEnd"]),"Equivalence inputs changed during numerical run");
    Need(binding["sourceModelSha256"].Value<string>()==SourceSha&&binding["optimizedModelSha256"].Value<string>()==ModelSha,"Equivalence model binding differs");
    Need(binding["candidateCasesSha256"].Value<string>()==Hash(baseCaseBytes)&&binding["transformationSha256"].Value<string>()==report["transformationRecordSha256"].Value<string>(),"Equivalence input/transformation binding differs");
    Need(binding["downloadMetadataSha256"].Value<string>()==Hash(File.ReadAllBytes(Evidence+"/Downloads_ranges.json"))&&binding["officialDownloadMetadataSha256"].Value<string>()==Hash(File.ReadAllBytes(Evidence+"/Downloads_small.json")),"Equivalence download metadata binding differs");
    var officialHashes=new JObject();foreach(var name in new[]{"tokenizer.json","tokenizer_config.json","config.json","generation_config.json"})officialHashes[name]=Hash(File.ReadAllBytes("Library/EchoStation/S3Cache/models/qwen3-candidate/original/"+name));
    Need(JToken.DeepEquals(officialHashes,binding["officialTokenizerSha256"]),"Official tokenizer/config bytes differ from numerical audit");report["officialTokenizerBytesSha256"]=officialHashes;report["numericalBindings"]=binding.DeepClone();
    var comparisons=(JArray)equivalence["comparisons"];Need(comparisons.Count==2,"Expected prefill and one KV continuation equivalence");
    for(var i=0;i<2;i++)Need(comparisons[i]["step"].Value<int>()==i&&comparisons[i]["logitsMaxAbs"].Value<double>()<=.0001&&comparisons[i]["kvMaxAbs"].Value<double>()==0,"Candidate numerical equivalence differs");
    modelLease=new FileStream(Path.GetFullPath(Model),FileMode.Open,FileAccess.Read,FileShare.Read,1024*1024,FileOptions.SequentialScan);
    report["modelBytes"]=modelLease.Length;report["modelSha256"]=Hash(modelLease,work.Cancel.Token);Need(report["modelSha256"].Value<string>()==ModelSha&&modelLease.Length==transform["bytes"].Value<long>(),"Candidate model SHA/length differs");modelLease.Position=0;
    tokenizerLease=new FileStream(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName+"/tokenizer.json"),FileMode.Open,FileAccess.Read,FileShare.Read);
    Need(tokenizerLease.Length==ModelCatalog.Current.Files["tokenizer.json"].Bytes,"Output tokenizer length differs");
    var tokenizerBytes=new byte[(int)tokenizerLease.Length];var at=0;while(at<tokenizerBytes.Length){var count=tokenizerLease.Read(tokenizerBytes,at,tokenizerBytes.Length-at);Need(count>0,"Tokenizer truncated");at+=count;}
    report["outputTokenizerBytesSha256"]=Hash(tokenizerBytes);Need(Hash(tokenizerBytes)==ModelCatalog.Current.Files["tokenizer.json"].Sha256,"Pinned output tokenizer SHA differs");report["outputVocabularyReleaseId"]=ModelCatalog.Current.Id;
    var tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.Parse(new UTF8Encoding(false,true).GetString(tokenizerBytes)));Need(tokenizer.DefinedTokenCount==151665,"Probe masking inventory differs");
    backend=new CandidateBackend(Path.GetFullPath(Model),main,cpuThreads);var generator=new TokenGenerator(backend,tokenizer);
    report["coldSeconds"]=cold.Elapsed.TotalSeconds;Need(cold.Elapsed.TotalSeconds<30,"Cold preparation exceeded 30-second logical deadline");work.Cancel.Token.ThrowIfCancellationRequested();Volatile.Write(ref work.Ready,1);
    long previousComplete=0;
    for(var index=0;index<cases.Count;index++)
    {
     work.Cancel.Token.ThrowIfCancellationRequested();
     if(previousComplete!=0)
     {
      var remaining=.3-(Stopwatch.GetTimestamp()-previousComplete)/(double)Stopwatch.Frequency;
      if(remaining>0&&work.Cancel.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(remaining)))work.Cancel.Token.ThrowIfCancellationRequested();
      // WaitOne resolution cannot admit before the real completion + 300 ms boundary.
      while((Stopwatch.GetTimestamp()-previousComplete)/(double)Stopwatch.Frequency<.3){work.Cancel.Token.ThrowIfCancellationRequested();Thread.Sleep(1);}
     }
     var item=cases[index];var ids=item["ids"].Values<int>().ToArray();QwenTensorContract.ValidateInput(ids);
     var snapshot=Qwen3TuningCaseContext.Create(item,work.Mode=="validation100",index);
     var expected=(LlmIntentKind)Enum.Parse(typeof(LlmIntentKind),item["expected"].Value<string>());var expectedTarget=item["target"]?.Value<string>();
     var row=new JObject{{"index",index},{"input",snapshot.Input},{"body",item["body"].Value<string>()},{"officialText",item["officialText"].Value<string>()},{"inputIds",new JArray(ids)},{"inputTokens",ids.Length},{"expected",expected.ToString()},{"expectedTarget",expectedTarget},{"strictJson",false},{"semanticCorrect",false},{"ownerThread",work.Owner},{"cacheBefore",backend.Consumed},{"nativeCallsBefore",backend.Calls}};
     if(previousComplete!=0)row["gapAfterPreviousCleanupMs"]=(Stopwatch.GetTimestamp()-previousComplete)*1000.0/Stopwatch.Frequency;rows.Add(row);save();
     var started=Stopwatch.StartNew();var before=backend.Calls;var nativeBefore=backend.NativeRunSeconds;var validationBefore=backend.ValidationSeconds;
     try
     {
      Need(backend.Consumed==0,"No cross-request KV");var generated=generator.Generate(ids,work.Cancel.Token,256,20,new JsonIntentConstraint(snapshot.Targets));
      row["raw"]=generated.Text;row["outputIds"]=new JArray(generated.TokenIds);
      if(work.Mode=="baseline_v2"){var exact=baselineRows[index]["input"].Value<string>()==snapshot.Input&&generated.TokenIds.SequenceEqual(baselineRows[index]["outputIds"].Values<int>());row["exactBaselineTokens"]=exact;if(exact)work.Exact++;}row["tokens"]=generated.TokenIds.Count;row["seconds"]=generated.Seconds;row["mode"]=generated.Mode;row["completeEos"]=generated.Complete;
      var parsed=IntentJsonParser.Parse(generated.Text);var valid=generated.Complete&&parsed.Success&&generated.Mode=="json-intent-v1-reserve16";row["strictJson"]=valid;row["parseFailure"]=parsed.Failure.ToString();if(valid)work.Strict++;
      if(parsed.Success)
      {
       row["actual"]=parsed.Intent.Kind.ToString();row["actualTarget"]=parsed.Intent.TargetName??parsed.Intent.DangerTarget;
       var correct=valid&&parsed.Intent.Kind==expected&&(expected!=LlmIntentKind.Investigate||parsed.Intent.TargetName==expectedTarget)&&(expected!=LlmIntentKind.Warn||parsed.Intent.DangerTarget==expectedTarget);row["semanticCorrect"]=correct;if(correct)work.Semantic++;
       row["delegatedOperationIntentMismatch"]=expected==LlmIntentKind.Refuse&&parsed.Intent.Kind!=LlmIntentKind.Refuse;
       row["unknownOrQuestionRoutedToDevice"]=expected==LlmIntentKind.Answer&&(parsed.Intent.Kind==LlmIntentKind.Investigate||parsed.Intent.Kind==LlmIntentKind.Warn);
      }
     }
     catch(Exception error){row["errorType"]=error.GetType().FullName;row["error"]=error.ToString();}
     finally
     {
      // TokenGenerator's own finally has already reset the KV on this same Owner.
      previousComplete=Stopwatch.GetTimestamp();row["wallSeconds"]=started.Elapsed.TotalSeconds;row["nativeCalls"]=backend.Calls-before;row["cacheAfter"]=backend.Consumed;row["nativeRunThread"]=backend.LastRunThread;row["nativeRunSeconds"]=backend.NativeRunSeconds-nativeBefore;row["validationSeconds"]=backend.ValidationSeconds-validationBefore;
      Need(backend.Consumed==0,"Generator did not clear KV");report["strictJsonCorrect"]=work.Strict;report["semanticCorrect"]=work.Semantic;save();
     }
    }
    report["allRequestsAttempted"]=rows.Count==work.Total;report["baselineTokensExact"]=work.Exact;report["gateScope"]=work.Mode=="baseline_v2"?"Reproduction only; semantic acceptance NOT evaluated":"Candidate smoke or independent validation; not final game acceptance";
   }
   catch(Exception error){work.Error=error.ToString();report["error"]=work.Error;}
   finally
   {
    try
    {
     try{backend?.Dispose();}finally{try{tokenizerLease?.Dispose();}finally{modelLease?.Dispose();}}
     if(backend==null){work.Clean=true;report["nativeCreated"]=false;}
     else
     {
      report["nativeRunSecondsTotal"]=backend.NativeRunSeconds;report["validationSecondsTotal"]=backend.ValidationSeconds;report["nativeCalls"]=backend.CallsAfterDispose;report["mainThreadNativeCalls"]=backend.MainNativeCalls;report["disposeThread"]=backend.DisposeThread;report["emptyPastCreated"]=backend.EmptyCreated;report["emptyPastDisposed"]=backend.EmptyDisposed;report["outputCollectionsCreated"]=backend.CollectionsCreated;report["outputCollectionsDisposed"]=backend.CollectionsDisposed;report["cacheAfterDispose"]=backend.CacheAfterDispose;
      work.Clean=backend.DisposeThread==work.Owner&&backend.IsDisposed&&backend.EmptyCreated==backend.EmptyDisposed&&backend.CollectionsCreated==backend.CollectionsDisposed&&backend.CacheAfterDispose==0&&backend.MainNativeCalls==0;
     }
    }
    catch(Exception error){work.Error=(work.Error??"")+"\nOwner cleanup: "+error;report["cleanupError"]=error.ToString();work.Clean=false;}
    report["ownerCleanupComplete"]=work.Clean;report["completed"]=true;report["passed"]=work.Error==null&&work.Clean&&work.Strict==work.Total&&(work.Mode=="baseline_v2"?work.Exact==18&&work.Semantic==15:work.Semantic>=(work.Mode=="smoke18"?17:90));
    try{save();}catch(Exception error){work.Error=(work.Error??"")+"\nEvidence write: "+error;}
    Volatile.Write(ref work.Done,1);
   }
  }
  private static string Hash(byte[] bytes){using(var hash=SHA256.Create())return Hex(hash.ComputeHash(bytes));}
  private static string Hash(Stream stream,CancellationToken token)
  {using(var hash=SHA256.Create()){var buffer=new byte[1024*1024];int count;while((count=stream.Read(buffer,0,buffer.Length))>0){token.ThrowIfCancellationRequested();hash.TransformBlock(buffer,0,count,buffer,0);}hash.TransformFinalBlock(Array.Empty<byte>(),0,0);return Hex(hash.Hash);}}
  private static string Hex(byte[] bytes)=>BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();
  private static void Need(bool condition,string detail){if(!condition)throw new InvalidDataException(detail);}

  // Test-only Qwen3 tensor adapter, deliberately separate from production QwenOnnxBackend.
  private sealed class CandidateBackend:ITokenModel,IDisposable
  {
   private readonly int owner,main;private readonly InferenceSession session;private readonly RunOptions options;
   private readonly OrtValue[] empty=new OrtValue[56],feeds=new OrtValue[59];private readonly string[] inputs=new string[59],outputs=new string[57];
   private IDisposableReadOnlyCollection<OrtValue> ownedOutputs;private OrtValue[] values;private int consumed,calls;private bool disposed,faulted;
   public int EmptyCreated {get;private set;}public int EmptyDisposed {get;private set;}public int CollectionsCreated {get;private set;}public int CollectionsDisposed {get;private set;}public int DisposeThread {get;private set;}
   public bool IsDisposed=>disposed;public int CallsAfterDispose=>calls;public int CacheAfterDispose=>consumed;
   public int LastRunThread {get;private set;}public int MainNativeCalls {get;private set;}
   private long nativeRunTicks,validationTicks;public double NativeRunSeconds=>nativeRunTicks/(double)Stopwatch.Frequency;public double ValidationSeconds=>validationTicks/(double)Stopwatch.Frequency;
   public int Consumed {get{Owner();return consumed;}}public int Calls {get{Owner();return calls;}}
   public ReadOnlySpan<float> Logits {get{Owner();Need(values!=null&&!faulted,"No valid candidate logits");return values[0].GetTensorDataAsSpan<float>();}}
   public CandidateBackend(string path,int main,int cpuThreads)
   {
    owner=Thread.CurrentThread.ManagedThreadId;this.main=main;Need(owner!=main,"Native create on main thread forbidden");Need(cpuThreads==2||cpuThreads==4,"Unsupported CPU budget");OrtEnv.Instance().DisableTelemetryEvents();
    try
    {
     using(var setup=new SessionOptions{IntraOpNumThreads=cpuThreads,InterOpNumThreads=1,ExecutionMode=ExecutionMode.ORT_SEQUENTIAL,GraphOptimizationLevel=GraphOptimizationLevel.ORT_ENABLE_ALL})
     {setup.AddSessionConfigEntry("session.intra_op.allow_spinning","0");setup.AddSessionConfigEntry("session.inter_op.allow_spinning","0");setup.AppendExecutionProvider_CPU(1);session=new InferenceSession(path,setup);}
     Need(session.InputMetadata.Count==59&&session.OutputMetadata.Count==57,"Candidate tensor inventory differs");inputs[0]="input_ids";inputs[1]="attention_mask";inputs[2]="position_ids";outputs[0]="logits";
     for(var i=0;i<3;i++)Metadata(session.InputMetadata,inputs[i],typeof(long),1,-1);Metadata(session.OutputMetadata,"logits",typeof(float),1,1,151936);
     for(var layer=0;layer<28;layer++)for(var value=0;value<2;value++)
     {
      var index=layer*2+value;var suffix=layer+(value==0?".key":".value");inputs[index+3]="past_key_values."+suffix;outputs[index+1]="present."+suffix;
      Metadata(session.InputMetadata,inputs[index+3],typeof(float),1,8,-1,128);Metadata(session.OutputMetadata,outputs[index+1],typeof(float),1,8,-1,128);
      empty[index]=OrtValue.CreateTensorValueFromMemory(Array.Empty<float>(),new long[]{1,8,0,128});EmptyCreated++;
     }
     options=new RunOptions();
    }
    catch{Dispose();throw;}
   }
   private static void Metadata(IReadOnlyDictionary<string,NodeMetadata> set,string name,Type type,params int[] expected)
   {
    Need(set.TryGetValue(name,out var entry)&&entry.IsTensor&&entry.ElementType==type&&entry.Dimensions.Length==expected.Length,"Candidate metadata name/type/rank: "+name);
    for(var i=0;i<expected.Length;i++)Need(entry.Dimensions[i]==expected[i]||(i==0&&entry.Dimensions[i]==-1),"Candidate metadata dimension: "+name);
   }
   public void Prefill(IReadOnlyList<int> ids){Owner();Need(!faulted,"Candidate backend is faulted");QwenTensorContract.ValidateInput(ids);ResetCache();Run(ids.Select(x=>(long)x).ToArray());}
   public void Step(int token){Owner();QwenTensorContract.ValidateToken(token);Need(!faulted&&values!=null&&consumed<2048,"Candidate cannot decode");Run(new[]{(long)token});}
   private void Run(long[] ids)
   {
    Owner();var total=consumed+ids.Length;Need(total<=2048,"Candidate context exceeded");var attention=new long[total];for(var i=0;i<attention.Length;i++)attention[i]=1;var positions=new long[ids.Length];for(var i=0;i<positions.Length;i++)positions[i]=consumed+i;
    using(var input=OrtValue.CreateTensorValueFromMemory(ids,new long[]{1,ids.Length}))
    using(var mask=OrtValue.CreateTensorValueFromMemory(attention,new long[]{1,total}))
    using(var position=OrtValue.CreateTensorValueFromMemory(positions,new long[]{1,ids.Length}))
    {
     feeds[0]=input;feeds[1]=mask;feeds[2]=position;for(var i=0;i<56;i++)feeds[i+3]=values==null?empty[i]:values[i+1];IDisposableReadOnlyCollection<OrtValue> next=null;
     try
     {
      calls++;LastRunThread=Thread.CurrentThread.ManagedThreadId;if(LastRunThread==main)MainNativeCalls++;
      var runStarted=Stopwatch.GetTimestamp();try{next=session.Run(options,inputs,feeds,outputs);}finally{nativeRunTicks+=Stopwatch.GetTimestamp()-runStarted;}
      CollectionsCreated++;var validationStarted=Stopwatch.GetTimestamp();var nextValues=next.ToArray();Need(nextValues.Length==57,"Candidate runtime output count");
      for(var i=0;i<nextValues.Length;i++)
      {
       var shape=nextValues[i].GetTensorTypeAndShape();Need(shape.ElementDataType==TensorElementType.Float,"Candidate runtime dtype");var expected=i==0?new long[]{1,1,151936}:new long[]{1,8,total,128};Need(shape.Shape.SequenceEqual(expected),"Candidate runtime shape");
       var tensor=nextValues[i].GetTensorDataAsSpan<float>();Need(tensor.Length==(i==0?151936:8*total*128),"Candidate runtime tensor count");FiniteTensorValidator.RequireFinite(tensor);
      }
      validationTicks+=Stopwatch.GetTimestamp()-validationStarted;ReleaseOutput();ownedOutputs=next;values=nextValues;next=null;consumed=total;
     }
     catch{faulted=true;if(next!=null){next.Dispose();CollectionsDisposed++;}ResetCache();throw;}
     finally{Array.Clear(feeds,0,feeds.Length);}
    }
   }
   public void ResetCache(){Owner();ReleaseOutput();values=null;consumed=0;}
   private void ReleaseOutput(){var previous=ownedOutputs;ownedOutputs=null;if(previous!=null){previous.Dispose();CollectionsDisposed++;}}
   private void Owner(){Need(Thread.CurrentThread.ManagedThreadId==owner,"Only Owner may touch native model resources");if(disposed)throw new ObjectDisposedException(nameof(CandidateBackend));}
   public void Dispose()
   {
    Need(Thread.CurrentThread.ManagedThreadId==owner,"Only Owner may dispose native model resources");if(disposed)return;Exception first=null;
    try{ResetCache();}catch(Exception error){first=error;}
    for(var i=0;i<empty.Length;i++){var value=empty[i];empty[i]=null;try{if(value!=null){value.Dispose();EmptyDisposed++;}}catch(Exception error){if(first==null)first=error;}}
    try{options?.Dispose();}catch(Exception error){if(first==null)first=error;}
    try{session?.Dispose();}catch(Exception error){if(first==null)first=error;}
    DisposeThread=Thread.CurrentThread.ManagedThreadId;disposed=true;if(first!=null)throw new InvalidOperationException("Candidate Owner cleanup failed",first);
   }
  }
 }
}
