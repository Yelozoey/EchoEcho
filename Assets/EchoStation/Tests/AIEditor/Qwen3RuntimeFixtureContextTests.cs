using System;
using System.IO;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3RuntimeFixtureContextTests
 {
  private static JArray Cases()=>(JArray)JObject.Parse(File.ReadAllText("Tools/Stage3/Fixtures/Q3_Validation100_B.json"))["cases"];
  [Test]public void EverySnapshotRetainsFiveTargetsAndOneKnownSummary()
  {foreach(var row in Cases()){var snapshot=Qwen3RuntimeFixtureContext.Create(row,0);Assert.That(snapshot.Targets.Count,Is.EqualTo(5));Assert.That(snapshot.Known.Clues.Single().Summary,Is.EqualTo(row["summary"].Value<string>()));Assert.That(snapshot.Targets.Count(t=>t.CanInvestigate),Is.EqualTo(3));Assert.That(snapshot.Targets.Count(t=>t.IsDanger),Is.EqualTo(2));}}
  [Test]public void AnswerLabelChangesCannotAlterActualPreparedPrompt()
  {
   var tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));
   var assembler=new ContextAssembler(new MemoryContextBuilder(Qwen3RuntimeFixtureContext.Search()),tokenizer);var prepare=new Qwen3PromptPreparation(tokenizer);var row=Cases()[30];var changed=row.DeepClone();changed["expected"]="Refuse";changed["target"]="expected_only_sentinel";
   var before=prepare.Build(assembler.Build(Qwen3RuntimeFixtureContext.Create(row,0)));var after=prepare.Build(assembler.Build(Qwen3RuntimeFixtureContext.Create(changed,0)));
   Assert.That(after.Body,Is.EqualTo(before.Body));CollectionAssert.AreEqual(before.InputIds,after.InputIds);Assert.That(after.Body,Does.Not.Contain("expected_only_sentinel"));Assert.That(after.EffectiveTargets.Count,Is.EqualTo(5));
  }
  [Test]public void MissingOrOversizedKnownSummaryRejected()
  {var row=Cases()[0].DeepClone();((JObject)row).Remove("summary");Assert.Throws<InvalidDataException>(()=>Qwen3RuntimeFixtureContext.Create(row,0));row["summary"]=new string('中',121);Assert.Throws<ArgumentException>(()=>Qwen3RuntimeFixtureContext.Create(row,0));}
  [Test]public void BoundedDeviceKeywordsRetrieveKnownSummary()
  {var snapshot=Qwen3RuntimeFixtureContext.Create(Cases()[30],0);var slice=new MemoryContextBuilder(Qwen3RuntimeFixtureContext.Search()).Build(snapshot.Known,snapshot.Input,snapshot.ElapsedGameSeconds);Assert.That(slice.Entries.Count,Is.EqualTo(1));}
 }
}
