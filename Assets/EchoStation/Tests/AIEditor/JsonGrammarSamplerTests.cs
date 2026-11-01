using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class JsonGrammarSamplerTests
 {
  private QwenByteBpeTokenizer tokenizer;
  // Deliberately uniform fixture with illegally early EOS pressure. No actual model/native calls.
  private sealed class EosPressure:ITokenModel
  {
   private readonly float[] values=new float[151936];public int Steps,Resets;
   public EosPressure(){values[151645]=100;values[151643]=90;}
   public ReadOnlySpan<float> Logits=>values;public void Prefill(IReadOnlyList<int> input){}public void Step(int token){Steps++;}public void ResetCache(){Resets++;}
  }
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.Parse(File.ReadAllText("Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/tokenizer.json")));}
  private PreparedPrompt Prepared(bool targets)
  {
   var items=targets?new[]{new TargetCandidate("panel_01","供电面板","cabin",true,true,true,true,1)}:Array.Empty<TargetCandidate>();var snapshot=new InferenceSnapshot(new MemoryStore(),"跟随我",0,"cabin",items);
   return new PromptPreparation(tokenizer).Build(new ContextAssembler(new MemoryContextBuilder(new MemorySearch()),tokenizer).Build(snapshot));
  }
  [TestCase(false)][TestCase(true)]public void SamplerMasksEarlyEosAndReserveEndsValidJsonWithin256Tokens(bool targets)
  {
   var prepared=Prepared(targets);var grammar=new JsonIntentConstraintProvider().Create(prepared);var model=new EosPressure();var generated=new TokenGenerator(model,tokenizer).Generate(prepared.InputIds,constraint:grammar);
   Assert.That(generated.Complete,Is.True);Assert.That(generated.Mode,Is.EqualTo("json-intent-v1-reserve16"));Assert.That(generated.TokenIds.Count,Is.InRange(240,256));Assert.That(IntentJsonParser.Parse(generated.Text).Success,Is.True,generated.Text);Assert.That(model.Resets,Is.EqualTo(1));Assert.That(generated.TokenIds.Last(),Is.EqualTo(151645));Assert.That(grammar.CanEnd,Is.True);
  }
  [Test]public void EveryRequestGetsFreshGrammarAndCannotReusePriorFinishedState()
  {var provider=new JsonIntentConstraintProvider();var a=provider.Create(Prepared(false));a.Accept(0,System.Text.Encoding.UTF8.GetBytes("{\"intent\":\"follow\"}"));Assert.That(a.CanEnd,Is.True);var b=provider.Create(Prepared(false));Assert.That(b.CanEnd,Is.False);Assert.That(ReferenceEquals(a,b),Is.False);Assert.Throws<ArgumentNullException>(()=>provider.Create(null));}
  [Test]public void FiniteTargetChoicesDistinguishPrefixIdsExactly()
  {
   var candidates=new[]{new TargetCandidate("a","短","cabin",true,true,false,true,1),new TargetCandidate("aa","长","cabin",true,true,false,true,2)};var targets=new InferenceSnapshot(new MemoryStore(),"调查",0,"cabin",candidates).Targets;
   foreach(var id in new[]{"a","aa"}){var g=new JsonIntentConstraint(targets);var raw="{\"intent\":\"investigate\",\"targetName\":\""+id+"\",\"content\":\"扫描。\"}";Assert.That(g.Allows(0,System.Text.Encoding.UTF8.GetBytes(raw)),Is.True);}
   Assert.That(new JsonIntentConstraint(targets).Allows(0,System.Text.Encoding.UTF8.GetBytes("{\"intent\":\"investigate\",\"targetName\":\"aaa\"")),Is.False);
  }
 }
}
