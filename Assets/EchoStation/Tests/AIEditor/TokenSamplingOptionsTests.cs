using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.AI.Inference;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class TokenSamplingOptionsTests
 {
  private QwenByteBpeTokenizer tokenizer;
  private sealed class Mask:ITokenConstraint
  {private readonly HashSet<int> ids;public Mask(bool canEnd,params int[] allowed){CanEnd=canEnd;ids=new HashSet<int>(allowed);}public string Name=>"test-allow-set";public bool CanEnd{get;}public bool Allows(int id,ReadOnlySpan<byte> bytes)=>ids.Contains(id);public void Accept(int id,ReadOnlySpan<byte> bytes)=>throw new NotSupportedException();}
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));}
  private static float[] Logits(params (int id,float score)[] values){var data=new float[QwenTensorContract.Vocabulary];for(var i=0;i<data.Length;i++)data[i]=float.NegativeInfinity;foreach(var item in values)data[item.id]=item.score;return data;}
  [Test]public void OfficialDefaultsAreBoundedImmutableAndExplicitlySeeded()
  {var p=new TokenSamplingOptions();Assert.That(p.Temperature,Is.EqualTo(.7));Assert.That(p.TopK,Is.EqualTo(20));Assert.That(p.TopP,Is.EqualTo(.8));Assert.That(p.Seed,Is.EqualTo(42u));Assert.That(typeof(TokenSamplingOptions).GetProperties().All(property=>!property.CanWrite),Is.True);}
  [Test]public void InvalidTemperaturesAndNucleusProbabilitiesReject()
  {foreach(var value in new[]{0d,-1,2.1,double.NaN,double.PositiveInfinity,double.NegativeInfinity})Assert.Throws<ArgumentOutOfRangeException>(()=>new TokenSamplingOptions(temperature:value));foreach(var value in new[]{0d,-1,1.01,double.NaN,double.PositiveInfinity})Assert.Throws<ArgumentOutOfRangeException>(()=>new TokenSamplingOptions(topP:value));}
  [Test]public void InvalidCandidateCountsAndZeroSeedReject()
  {Assert.Throws<ArgumentOutOfRangeException>(()=>new TokenSamplingOptions(topK:0));Assert.Throws<ArgumentOutOfRangeException>(()=>new TokenSamplingOptions(topK:21));Assert.Throws<ArgumentOutOfRangeException>(()=>new TokenSamplingOptions(seed:0));Assert.Throws<ArgumentNullException>(()=>new TopKTokenSampler(null));}
  [Test]public void SameSeedIsReproducibleAndAnotherSeedDoesNotShareState()
  {var data=Logits((1,0),(2,0),(3,0));var a=new TopKTokenSampler(new TokenSamplingOptions(1,3,1,42));var b=new TopKTokenSampler(new TokenSamplingOptions(1,3,1,42));var c=new TopKTokenSampler(new TokenSamplingOptions(1,3,1,1729));var first=Enumerable.Range(0,64).Select(_=>a.Select(data,tokenizer)).ToArray();var same=Enumerable.Range(0,64).Select(_=>b.Select(data,tokenizer)).ToArray();var other=Enumerable.Range(0,64).Select(_=>c.Select(data,tokenizer)).ToArray();CollectionAssert.AreEqual(first,same);Assert.That(first.SequenceEqual(other),Is.False);Assert.That(a.Draws,Is.EqualTo(64));}
  [Test]public void TopKOneSelectsOnlyHighestEligibleId()
  {var sampler=new TopKTokenSampler(new TokenSamplingOptions(1,1,1));Assert.That(sampler.Select(Logits((1,-9),(2,4),(3,1)),tokenizer),Is.EqualTo(2));}
  [Test]public void TopPTruncatesWithinTopKWithStableTieOrder()
  {var data=Logits((1,0),(2,0),(3,0));var sampler=new TopKTokenSampler(new TokenSamplingOptions(1,3,.5));var values=Enumerable.Range(0,64).Select(_=>sampler.Select(data,tokenizer)).ToArray();Assert.That(values.All(i=>i==1||i==2),Is.True);Assert.That(values.Distinct().Count(),Is.EqualTo(2));}
  [Test]public void TemperatureSoftmaxDistributionIsNotUniformOrAlwaysArgmax()
  {var data=Logits((1,(float)Math.Log(3)),(2,0));var sampler=new TopKTokenSampler(new TokenSamplingOptions(1,2,1));var count=0;for(var i=0;i<512;i++)if(sampler.Select(data,tokenizer)==1)count++;Assert.That(count/512.0,Is.InRange(.67,.83));}
  [Test]public void TinyTemperatureAndLargeLogitGapRemainFinite()
  {var sampler=new TopKTokenSampler(new TokenSamplingOptions(.01,2,1));Assert.That(sampler.Select(Logits((1,0),(2,-1000)),tokenizer),Is.EqualTo(1));}
  [Test]public void GrammarMaskCannotBeOverriddenByHigherLogits()
  {var sampler=new TopKTokenSampler(new TokenSamplingOptions(1,20,1));Assert.That(sampler.Select(Logits((1,100),(2,-10),(3,90)),tokenizer,new Mask(false,2)),Is.EqualTo(2));}
  [Test]public void EosRequiresCanEndAndOtherSpecialRolesStayForbidden()
  {var data=Logits((1,0),(151644,100),(151645,20));Assert.That(new TopKTokenSampler(new TokenSamplingOptions(1,1,1)).Select(data,tokenizer,new Mask(false,1)),Is.EqualTo(1));Assert.That(new TopKTokenSampler(new TokenSamplingOptions(1,1,1)).Select(data,tokenizer,new Mask(true,1)),Is.EqualTo(151645));}
  [Test]public void EmptyAllowedSetAndInvalidLogitsRejectWithoutFallbackToken()
  {var sampler=new TopKTokenSampler(new TokenSamplingOptions());var absent=Assert.Throws<GenerationException>(()=>sampler.Select(Logits((1,0)),tokenizer,new Mask(false,2)));Assert.That(absent.Code,Is.EqualTo(GenerationFault.NoAllowedToken));Assert.Throws<InvalidDataException>(()=>sampler.Select(Logits((1,float.NaN)),tokenizer));Assert.Throws<InvalidDataException>(()=>sampler.Select(Logits((1,float.PositiveInfinity)),tokenizer));Assert.Throws<InvalidDataException>(()=>sampler.Select(new float[4],tokenizer));Assert.Throws<ArgumentNullException>(()=>sampler.Select(Logits((1,0)),null));}
 }
}
