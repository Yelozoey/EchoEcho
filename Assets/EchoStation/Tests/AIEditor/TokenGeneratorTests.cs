using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.Tokenization;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class TokenGeneratorTests
 {
  private QwenByteBpeTokenizer tokenizer;
  [OneTimeSetUp]public void Setup()=>tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.Parse(File.ReadAllText("Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/tokenizer.json")));
  // A deterministic protocol fixture, never used as game inference or counted as true LLM output.
  private sealed class SequenceModel:ITokenModel
  {
   private readonly int[] ids;private readonly float[] logits=new float[151936];private int step;public int Calls,Resets;public Action AfterRun;public int? ExtraTie;
   public SequenceModel(params int[] sequence){ids=sequence;}
   public ReadOnlySpan<float> Logits=>logits;
   private void Set(){for(var i=0;i<logits.Length;i++)logits[i]=-100;logits[ids[Math.Min(step,ids.Length-1)]]=10;if(ExtraTie.HasValue)logits[ExtraTie.Value]=10;Calls++;AfterRun?.Invoke();}
   public void Prefill(IReadOnlyList<int> input){step=0;Set();}public void Step(int token){step++;Set();}public void ResetCache(){Resets++;}
  }
  [Test]public void EosProducesExactTextAndResetsEveryRequest()
  {
   var ids=tokenizer.EncodePlain("Echo已就绪。").Concat(new[]{151645}).ToArray();var model=new SequenceModel(ids);var engine=new TokenGenerator(model,tokenizer);
   var first=engine.Generate(new[]{1});var second=engine.Generate(new[]{2});Assert.That(first.Text,Is.EqualTo("Echo已就绪。"));Assert.That(first.Complete,Is.True);Assert.That(first.TokenIds.Last(),Is.EqualTo(151645));Assert.That(second.Text,Is.EqualTo(first.Text));Assert.That(model.Resets,Is.EqualTo(2));
  }
  [Test]public void TokenLimitIsExplicitlyIncomplete()
  {var id=tokenizer.EncodePlain("a")[0];var model=new SequenceModel(id);var result=new TokenGenerator(model,tokenizer).Generate(new[]{1},maximumNewTokens:3);Assert.That(result.Text,Is.EqualTo("aaa"));Assert.That(result.Stop,Is.EqualTo(GenerationStop.TokenLimit));Assert.That(result.Complete,Is.False);Assert.That(model.Calls,Is.EqualTo(3));Assert.That(model.Resets,Is.EqualTo(1));}
  [Test]public void EqualLogitsPickLowestDefinedId()
  {var a=tokenizer.EncodePlain("a")[0];var b=tokenizer.EncodePlain("b")[0];var model=new SequenceModel(b){ExtraTie=a};var result=new TokenGenerator(model,tokenizer).Generate(new[]{1},maximumNewTokens:1);Assert.That(result.TokenIds[0],Is.EqualTo(Math.Min(a,b)));}
  [Test]public void PreCancelledAndCancelledAfterNativePhaseDoNotReturnText()
  {
   using(var source=new CancellationTokenSource())
   {source.Cancel();var model=new SequenceModel(151645);Assert.Throws<OperationCanceledException>(()=>new TokenGenerator(model,tokenizer).Generate(new[]{1},source.Token));Assert.That(model.Calls,Is.Zero);Assert.That(model.Resets,Is.EqualTo(1));}
   using(var source=new CancellationTokenSource())
   {var model=new SequenceModel(151645){AfterRun=()=>source.Cancel()};Assert.Throws<OperationCanceledException>(()=>new TokenGenerator(model,tokenizer).Generate(new[]{1},source.Token));Assert.That(model.Calls,Is.EqualTo(1));Assert.That(model.Resets,Is.EqualTo(1));}
  }
  [Test]public void ExactlyTwentySecondsTimesOutAndResets()
  {double now=0;var model=new SequenceModel(151645){AfterRun=()=>now=20};var error=Assert.Throws<GenerationException>(()=>new TokenGenerator(model,tokenizer,()=>now).Generate(new[]{1}));Assert.That(error.Code,Is.EqualTo(GenerationFault.Timeout));Assert.That(model.Resets,Is.EqualTo(1));}
  [Test]public void IncompleteUtf8CannotBecomeSuccessfulReply()
  {var encoded=ByteAlphabet.ForByte(0xe4).ToString();var data=TokenizerAssets.Parse(File.ReadAllText("Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/tokenizer.json"));var model=new SequenceModel(data.Vocabulary[encoded],151645);var error=Assert.Throws<GenerationException>(()=>new TokenGenerator(model,tokenizer).Generate(new[]{1}));Assert.That(error.Code,Is.EqualTo(GenerationFault.InvalidUtf8));Assert.That(model.Resets,Is.EqualTo(1));}
  [Test]public void FourKiBOutputBudgetIsEnforcedBeforeReturn()
  {var id=tokenizer.EncodePlain(new string(' ',128)).Single();var model=new SequenceModel(id);var error=Assert.Throws<GenerationException>(()=>new TokenGenerator(model,tokenizer).Generate(new[]{1}));Assert.That(error.Code,Is.EqualTo(GenerationFault.OutputBudget));Assert.That(model.Resets,Is.EqualTo(1));}
  private sealed class DenyAll:ITokenConstraint
  {public string Name=>"test-deny";public bool CanEnd=>false;public bool Allows(int id,ReadOnlySpan<byte> bytes)=>false;public void Accept(int id,ReadOnlySpan<byte> bytes)=>throw new InvalidOperationException();}
  [Test]public void NoLegalTokenIsFaultRatherThanRepairedOutput()
  {var model=new SequenceModel(151645);var error=Assert.Throws<GenerationException>(()=>new TokenGenerator(model,tokenizer).Generate(new[]{1},constraint:new DenyAll()));Assert.That(error.Code,Is.EqualTo(GenerationFault.NoAllowedToken));Assert.That(model.Resets,Is.EqualTo(1));}
  [Test]public void InvalidBudgetsNeverReachModel()
  {var model=new SequenceModel(151645);var engine=new TokenGenerator(model,tokenizer);Assert.Throws<ArgumentOutOfRangeException>(()=>engine.Generate(new[]{1},maximumNewTokens:257));Assert.Throws<ArgumentOutOfRangeException>(()=>engine.Generate(new[]{1},timeoutSeconds:double.NaN));Assert.That(model.Calls,Is.Zero);}
 }
}
