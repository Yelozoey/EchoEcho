using System.IO;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using EchoStation.Story;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3V10PromptTests
 {
  private QwenByteBpeTokenizer tokenizer;
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));}
  private PreparedPrompt Prepare(string input,string setup)=>new Qwen3PromptPreparation(tokenizer,Qwen3PromptProfile.V10).Build(new ContextAssembler(new MemoryContextBuilder(StationScript.CreateSearch()),tokenizer).Build(T01ContextSnapshots.Create(input,setup)));
  [Test]public void ExactEnglishTextReusesExactChineseExamplesAndKeepsDefault()
  {var p=Qwen3PromptProfile.V10;Assert.That(p.Text,Is.EqualTo(File.ReadAllText("Docs/Stage3/Prompts/Q3_V10_EnglishLabels.txt")));Assert.That(p.Sha256,Is.EqualTo(Qwen3V10Prompt.Sha256));Assert.That(p.ExamplesSha256,Is.EqualTo(Qwen3V9Prompt.ExamplesSha256));Assert.That(Qwen3PromptProfile.Selected,Is.SameAs(Qwen3PromptProfile.V5));Assert.That(p.Text,Does.Not.Contain("power_panel_01").And.Not.Contain("7316"));}
  [Test]public void All200ContextsStillFitAndRetainEveryRequestedTarget()
  {foreach(var row in T01QualityFixture.Load().Concat(T01IndependentFixture.Load())){var p=Prepare(row.Input,row.Setup);Assert.That(p.InputIds.Count,Is.LessThanOrEqualTo(1536),row.Id);Assert.That(p.InputIds.Count+256,Is.LessThanOrEqualTo(2048));Assert.That(p.MemoryTokens,Is.LessThanOrEqualTo(512));Assert.That(p.MemoryUtf8Bytes,Is.LessThanOrEqualTo(1200));Assert.That(p.Body,Does.EndWith(row.Input+"\n").And.Not.Contain("7316"));if(row.Target!=null)Assert.That(p.EffectiveTargets.Any(t=>t.Id==row.Target),Is.True,row.Id);Assert.That(p.InputIds.Count(i=>i==151644),Is.EqualTo(19));}}
  [Test]public void EnglishRulesCannotReceiveScoringLabelsThroughContextConstruction()
  {foreach(var row in T01Diagnostic60.Load().Where((r,i)=>i%13==0)){var alternate=new T01Case(row.Id,row.Setup,row.Input,LlmIntentKind.Refuse,"scoring_only_sentinel");var a=Prepare(row.Input,row.Setup);var b=Prepare(alternate.Input,alternate.Setup);Assert.That(b.Body,Is.EqualTo(a.Body).And.Not.Contain("scoring_only_sentinel"));CollectionAssert.AreEqual(a.InputIds,b.InputIds);}}
 }
}
