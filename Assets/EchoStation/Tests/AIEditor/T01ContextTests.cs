using System;
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
 public sealed class T01ContextTests
 {
  private QwenByteBpeTokenizer tokenizer;
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));}
  private PreparedPrompt Prepare(string input,string setup)
  {var snapshot=T01ContextSnapshots.Create(input,setup);return new Qwen3PromptPreparation(tokenizer).Build(new ContextAssembler(new MemoryContextBuilder(StationScript.CreateSearch()),tokenizer).Build(snapshot));}
  [Test]public void All100ProductionPreparedInputsObeyBudgetsAndProvideExpectedTargetsWithoutUsingLabels()
  {
   foreach(var row in T01QualityFixture.Load())
   {
    var prepared=Prepare(row.Input,row.Setup);Assert.That(prepared.InputIds.Count,Is.LessThanOrEqualTo(1536),row.Id);Assert.That(prepared.InputIds.Count+256,Is.LessThanOrEqualTo(2048));Assert.That(prepared.MemoryEntries,Is.LessThanOrEqualTo(8));Assert.That(prepared.MemoryTokens,Is.LessThanOrEqualTo(512));Assert.That(prepared.MemoryUtf8Bytes,Is.LessThanOrEqualTo(1200));Assert.That(prepared.EffectiveTargets.Count,Is.LessThanOrEqualTo(8));Assert.That(prepared.Body,Does.EndWith(row.Input+"\n").And.Not.Contain("7316"));
    if(row.Target!=null)Assert.That(prepared.EffectiveTargets.Any(t=>t.Id==row.Target),Is.True,row.Id+" expected action must be possible in this controlled context");
   }
  }
  [Test]public void CounterfactualScoringLabelsAndTargetsCannotChangeContextBodyOrTokens()
  {
   foreach(var row in T01QualityFixture.Load().Where((r,i)=>i%17==0))
   {
    var alternate=new T01Case(row.Id,row.Setup,row.Input,row.Expected==LlmIntentKind.Follow?LlmIntentKind.Refuse:LlmIntentKind.Follow,"not_a_real_target");var original=Prepare(row.Input,row.Setup);var changed=Prepare(alternate.Input,alternate.Setup);Assert.That(changed.Body,Is.EqualTo(original.Body));CollectionAssert.AreEqual(original.InputIds,changed.InputIds);CollectionAssert.AreEqual(original.EffectiveTargets.Select(t=>t.Id),changed.EffectiveTargets.Select(t=>t.Id));
   }
  }
  [TestCase("valley_empty",0,"valley")][TestCase("cabin_known",1,"cabin")][TestCase("lab_known",3,"lab_a")]
  public void OnlyDeclaredKnownSummariesAndCorrectWorldZonesEnterSnapshot(string setup,int count,string zone)
  {
   var snapshot=T01ContextSnapshots.Create("已知线索有哪些？",setup);Assert.That(snapshot.Zone,Is.EqualTo(zone));Assert.That(snapshot.Known.Clues.Count,Is.EqualTo(count));foreach(var clue in snapshot.Known.Clues){Assert.That(StationClues.TryGet(clue.Id,out var source),Is.True);Assert.That(clue.Summary,Is.EqualTo(source.Summary));Assert.That(clue.Summary,Is.Not.EqualTo(source.FullText));}
   var prepared=Prepare(snapshot.Input,setup);foreach(var clue in StationClues.All)Assert.That(prepared.Body,Does.Not.Contain(clue.FullText));
  }
  [Test]public void SnapshotFactoryRejectsUnknownSetupAndDoesNotAcceptAScoringCaseObject()
  {Assert.Throws<ArgumentException>(()=>T01ContextSnapshots.Create("跟随我","secret_world"));Assert.That(typeof(T01ContextSnapshots).GetMethod("Create").GetParameters().Select(p=>p.ParameterType),Is.EqualTo(new[]{typeof(string),typeof(string)}));}
 }
}
