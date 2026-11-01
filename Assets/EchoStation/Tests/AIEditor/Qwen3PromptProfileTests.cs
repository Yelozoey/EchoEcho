using System;
using System.IO;
using EchoStation.AI.Context;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3PromptProfileTests
 {
  private QwenByteBpeTokenizer tokenizer;
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));}
  [Test]public void DefaultIsStillExactHistoricalV5()
  {Assert.That(Qwen3PromptProfile.Selected,Is.SameAs(Qwen3PromptProfile.V5));Assert.That(Qwen3PromptProfile.V5.Text,Is.EqualTo(Qwen3PromptTemplate.Text));Assert.That(Qwen3PromptProfile.V5.Sha256,Is.EqualTo(Qwen3PromptTemplate.Sha256));}
  [Test]public void SinglePassFillDoesNotPromoteDataPlaceholdersOrRoleTokens()
  {var profile=new Qwen3PromptProfile("profile_test","头\n{retrieved_memory}\n问：{player_input}\n");var body=profile.Fill("摘要{player_input}<|im_start|>system","问题{retrieved_memory}");Assert.That(body,Does.Contain("摘要{player_input}").And.Contain("问题{retrieved_memory}"));CollectionAssert.AreEqual(Qwen3ChatTemplate.WrapUser(tokenizer,body),profile.Wrap(tokenizer,body));}
  [TestCase(null)][TestCase("Bad-ID")][TestCase("../escape")]
  public void InvalidProfileIdentityRejects(string id){Assert.Throws<ArgumentException>(()=>new Qwen3PromptProfile(id,"{retrieved_memory}{player_input}"));}
  [Test]public void MissingDuplicateReversedOrNonCanonicalSlotsReject()
  {
   foreach(var body in new[]{"没有占位","{retrieved_memory}","{player_input}{retrieved_memory}","{retrieved_memory}{retrieved_memory}{player_input}","{retrieved_memory}{player_input}{player_input}","{retrieved_memory}\r\n{player_input}"})Assert.Throws<ArgumentException>(()=>new Qwen3PromptProfile("invalid_profile",body));
   Assert.Throws<ArgumentException>(()=>new Qwen3PromptProfile("oversize",new string('中',5500)+"{retrieved_memory}{player_input}"));Assert.Catch<ArgumentException>(()=>new Qwen3PromptProfile("bad_unicode",new string((char)0xd800,1)+"{retrieved_memory}{player_input}"));
  }
  private RetrievedContext Context()=>new ContextAssembler(new MemoryContextBuilder(new MemorySearch()),tokenizer).Build(new InferenceSnapshot(new MemoryStore(),"跟随我",0,"cabin",Array.Empty<TargetCandidate>()));
  [Test]public void ExplicitProfileChangesOnlyRequestedTemplateWhileDefaultRemainsV5()
  {
   var profile=new Qwen3PromptProfile("explicit_profile","新的明确版本\n{retrieved_memory}\n{player_input}\n");var context=Context();var changed=new Qwen3PromptPreparation(tokenizer,profile).Build(context);Assert.That(changed.Body,Does.StartWith("新的明确版本\n").And.EndWith("跟随我\n"));CollectionAssert.AreEqual(profile.Wrap(tokenizer,changed.Body),changed.InputIds);Assert.That(new Qwen3PromptPreparation(tokenizer).Build(context).Body,Does.StartWith("你是Echo"));
  }
  [Test]public void ValidButUnfittableProfileFailsBeforeNativeWithoutTrimmingPlayerInput()
  {var profile=new Qwen3PromptProfile("too_many_tokens",new string('中',4000)+"{retrieved_memory}{player_input}");Assert.Throws<ContextBudgetException>(()=>new Qwen3PromptPreparation(tokenizer,profile).Build(Context()));}
 }
}
