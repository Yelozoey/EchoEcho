using System;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Context;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 // Protocol/lifecycle fixtures only. They are never model-quality samples.
 public sealed class OptInSamplingGenerationTests
 {
  private QwenByteBpeTokenizer tokenizer;
  private sealed class Sequence:ITokenModel
  {
   private readonly int[] output;private readonly float[] logits=new float[151936];private int index;public int Calls,Resets;public Action AfterRun;public bool EarlyEos;public int? FirstAlternative;
   public Sequence(params int[] ids){output=ids;}public ReadOnlySpan<float> Logits=>logits;
   private void Set(){for(var i=0;i<logits.Length;i++)logits[i]=float.NegativeInfinity;logits[output[Math.Min(index,output.Length-1)]]=10;if(index==0&&FirstAlternative.HasValue)logits[FirstAlternative.Value]=10;if(EarlyEos)logits[151645]=100;Calls++;AfterRun?.Invoke();}
   public void Prefill(System.Collections.Generic.IReadOnlyList<int> input){index=0;Set();}public void Step(int token){index++;Set();}public void ResetCache(){Resets++;}
  }
  private sealed class Deny:ITokenConstraint
  {public string Name=>"deny";public bool CanEnd=>false;public bool Allows(int id,ReadOnlySpan<byte> bytes)=>false;public void Accept(int id,ReadOnlySpan<byte> bytes)=>throw new InvalidOperationException();}
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));}
  private int[] Reply(string text)=>tokenizer.EncodePlain(text).Concat(new[]{151645}).ToArray();
  [Test]public void DefaultGreedyKeepsExactTokensEosMetadataAndIndependentCaches()
  {var ids=Reply("Echo已就绪。");var model=new Sequence(ids);var generator=new TokenGenerator(model,tokenizer);var a=generator.Generate(new[]{1});var b=generator.Generate(new[]{2});CollectionAssert.AreEqual(ids,a.TokenIds);CollectionAssert.AreEqual(a.TokenIds,b.TokenIds);Assert.That(a.Text,Is.EqualTo("Echo已就绪。"));Assert.That(a.Complete,Is.True);Assert.That(a.Mode,Is.EqualTo("greedy"));Assert.That(a.SamplingMode,Is.EqualTo("greedy"));Assert.That(a.SamplingOptions,Is.Null);Assert.That(model.Resets,Is.EqualTo(2));}
  [Test]public void ExplicitSamplingCarriesItsOptionsAndDoesNotChangeAFullyPredictedSequence()
  {var ids=Reply("我在这里待命。");var model=new Sequence(ids);var options=new TokenSamplingOptions();var result=new TokenGenerator(model,tokenizer).Generate(new[]{1},sampling:options);CollectionAssert.AreEqual(ids,result.TokenIds);Assert.That(result.SamplingOptions,Is.SameAs(options));Assert.That(result.SamplingMode,Is.EqualTo(TopKTokenSampler.Name));Assert.That(result.Complete,Is.True);Assert.That(model.Resets,Is.EqualTo(1));}
  [Test]public void RequestLocalSamplingStateDoesNotLeakAcrossRequests()
  {var model=new Sequence(Reply("a")){FirstAlternative=tokenizer.EncodePlain("b").Single()};var generator=new TokenGenerator(model,tokenizer);var options=new TokenSamplingOptions(1,2,1);var first=generator.Generate(new[]{1},sampling:options);for(var i=0;i<5;i++)CollectionAssert.AreEqual(first.TokenIds,generator.Generate(new[]{1},sampling:options).TokenIds);Assert.That(model.Resets,Is.EqualTo(6));}
  [Test]public void SamplingCannotBypassGrammarWithAnEarlyHighScoringEos()
  {const string raw="{\"intent\":\"refuse\",\"content\":\"我不能代为操作设备。\"}";var ids=Reply(raw);var model=new Sequence(ids){EarlyEos=true};var grammar=new JsonIntentConstraint(Array.Empty<AllowedTarget>());var result=new TokenGenerator(model,tokenizer).Generate(new[]{1},constraint:grammar,sampling:new TokenSamplingOptions());Assert.That(result.Complete,Is.True);Assert.That(result.Text,Is.EqualTo(raw));Assert.That(result.Mode,Is.EqualTo("json-intent-v1-reserve16"));Assert.That(IntentJsonParser.Parse(result.Text).Success,Is.True);Assert.That(model.Resets,Is.EqualTo(1));}
  [Test]public void CancelledBeforeAndAfterModelCallReturnsNoReplyAndAlwaysResets()
  {
   using(var cancel=new CancellationTokenSource()){cancel.Cancel();var model=new Sequence(151645);Assert.Throws<OperationCanceledException>(()=>new TokenGenerator(model,tokenizer).Generate(new[]{1},cancel.Token,sampling:new TokenSamplingOptions()));Assert.That(model.Calls,Is.Zero);Assert.That(model.Resets,Is.EqualTo(1));}
   using(var cancel=new CancellationTokenSource()){var model=new Sequence(151645){AfterRun=()=>cancel.Cancel()};Assert.Throws<OperationCanceledException>(()=>new TokenGenerator(model,tokenizer).Generate(new[]{1},cancel.Token,sampling:new TokenSamplingOptions()));Assert.That(model.Calls,Is.EqualTo(1));Assert.That(model.Resets,Is.EqualTo(1));}
  }
  [Test]public void TwentySecondLogicalTimeoutStillResetsWithoutReply()
  {double now=0;var model=new Sequence(151645){AfterRun=()=>now=20};var error=Assert.Throws<GenerationException>(()=>new TokenGenerator(model,tokenizer,()=>now).Generate(new[]{1},sampling:new TokenSamplingOptions()));Assert.That(error.Code,Is.EqualTo(GenerationFault.Timeout));Assert.That(model.Resets,Is.EqualTo(1));}
  [Test]public void NoAllowedCandidateIsAFaultWithNoRepair()
  {var model=new Sequence(151645);var error=Assert.Throws<GenerationException>(()=>new TokenGenerator(model,tokenizer).Generate(new[]{1},constraint:new Deny(),sampling:new TokenSamplingOptions()));Assert.That(error.Code,Is.EqualTo(GenerationFault.NoAllowedToken));Assert.That(model.Resets,Is.EqualTo(1));}
  [Test]public void SampledTokenLimitRemainsExplicitlyIncomplete()
  {var model=new Sequence(tokenizer.EncodePlain("a").Single());var result=new TokenGenerator(model,tokenizer).Generate(new[]{1},maximumNewTokens:3,sampling:new TokenSamplingOptions());Assert.That(result.Text,Is.EqualTo("aaa"));Assert.That(result.Stop,Is.EqualTo(GenerationStop.TokenLimit));Assert.That(result.Complete,Is.False);Assert.That(model.Resets,Is.EqualTo(1));}
  [Test]public void InvalidBudgetsAreRejectedBeforeAnyModelCall()
  {var model=new Sequence(151645);var generator=new TokenGenerator(model,tokenizer);Assert.Throws<ArgumentOutOfRangeException>(()=>generator.Generate(new[]{1},maximumNewTokens:257,sampling:new TokenSamplingOptions()));Assert.Throws<ArgumentOutOfRangeException>(()=>generator.Generate(new[]{1},timeoutSeconds:double.NaN,sampling:new TokenSamplingOptions()));Assert.That(model.Calls,Is.Zero);}
 }
}
