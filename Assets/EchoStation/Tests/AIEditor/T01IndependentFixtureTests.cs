using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using EchoStation.Story;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 internal static class T01IndependentFixture
 {
  public const string Path="Tools/Stage3/Fixtures/T01_Game100_D.json";
  public const string Sha256="6b87a58ee02dc690a8426e5af49efa5defd4345b1a99c9519430ded5d52f75d5";
  public static IReadOnlyList<T01Case> Load()
  {var bytes=File.ReadAllBytes(Path);using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=Sha256)throw new ArgumentException("Frozen D fixture changed");return T01QualityFixture.Parse(new UTF8Encoding(false,true).GetString(bytes),"T01_D_");}
 }
 public sealed class T01IndependentFixtureTests
 {
  private QwenByteBpeTokenizer tokenizer;
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));}
  private PreparedPrompt Prepare(string input,string setup)=>new Qwen3PromptPreparation(tokenizer,Qwen3PromptProfile.V7).Build(new ContextAssembler(new MemoryContextBuilder(StationScript.CreateSearch()),tokenizer).Build(T01ContextSnapshots.Create(input,setup)));
  [Test]public void FrozenShaClosedRowsAndDistributionRemainExact()
  {var rows=T01IndependentFixture.Load();Assert.That(rows.Count,Is.EqualTo(100));var kinds=new[]{LlmIntentKind.Follow,LlmIntentKind.Wait,LlmIntentKind.Investigate,LlmIntentKind.Warn,LlmIntentKind.Answer,LlmIntentKind.Refuse};var counts=new[]{15,15,20,10,25,15};for(var i=0;i<kinds.Length;i++)Assert.That(rows.Count(r=>r.Expected==kinds[i]),Is.EqualTo(counts[i]));}
  private static string Normalize(string value)=>Regex.Replace(value.Normalize(NormalizationForm.FormKC),@"[^\w\u4e00-\u9fff]","").ToLowerInvariant();
  [Test]public void AllInputsAreNewComparedWithBothPriorSuitesEvenWithoutPunctuation()
  {var previous=T01QualityFixture.Load().Select(r=>Normalize(r.Input)).Concat(JObject.Parse(File.ReadAllText("Tools/Stage3/Fixtures/Q3_Validation100_B.json"))["cases"].Select(r=>Normalize(r.Value<string>("input")))).ToHashSet();var current=T01IndependentFixture.Load().Select(r=>Normalize(r.Input)).ToArray();Assert.That(current.Distinct().Count(),Is.EqualTo(100));Assert.That(current.Any(previous.Contains),Is.False);}
  [Test]public void EveryNewCaseFitsRealV7BudgetsAndRetainsItsRequestedTarget()
  {foreach(var row in T01IndependentFixture.Load()){var result=Prepare(row.Input,row.Setup);Assert.That(result.InputIds.Count,Is.LessThanOrEqualTo(1536),row.Id);Assert.That(result.InputIds.Count+256,Is.LessThanOrEqualTo(2048));Assert.That(result.MemoryEntries,Is.LessThanOrEqualTo(8));Assert.That(result.MemoryTokens,Is.LessThanOrEqualTo(512));Assert.That(result.MemoryUtf8Bytes,Is.LessThanOrEqualTo(1200));Assert.That(result.EffectiveTargets.Count,Is.LessThanOrEqualTo(8));Assert.That(result.Body,Does.EndWith(row.Input+"\n").And.Not.Contain("7316"));if(row.Target!=null)Assert.That(result.EffectiveTargets.Any(t=>t.Id==row.Target),Is.True,row.Id);}}
  [Test]public void ScoringLabelAndExpectedTargetNeverChangeSnapshotBodyOrInputIds()
  {foreach(var row in T01IndependentFixture.Load().Where((r,i)=>i%9==0)){var changed=new T01Case(row.Id,row.Setup,row.Input,LlmIntentKind.Refuse,"expected_label_sentinel");var a=Prepare(row.Input,row.Setup);var b=Prepare(changed.Input,changed.Setup);Assert.That(b.Body,Is.EqualTo(a.Body).And.Not.Contain("expected_label_sentinel"));CollectionAssert.AreEqual(a.InputIds,b.InputIds);CollectionAssert.AreEqual(a.EffectiveTargets.Select(t=>t.Id),b.EffectiveTargets.Select(t=>t.Id));}}
  [Test]public void AFixtureCannotSilentlyMasqueradeAsTheOtherPrefix()
  {var text=File.ReadAllText(T01IndependentFixture.Path);Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(text));Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(text,"arbitrary_prefix_"));Assert.That(T01QualityFixture.Parse(text,"T01_D_").Count,Is.EqualTo(100));}
 }
}
