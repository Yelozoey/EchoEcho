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
 internal static class T01IndependentEFixture
 {
  public const string Path="Tools/Stage3/Fixtures/T01_Game100_E.json";
  public const string Sha256="2d9134de3ad413560f4b89f2fb0fabc501224fef20c4db5b0a76b5663cd5cfa2";
  public static IReadOnlyList<T01Case> Load()
  {var bytes=File.ReadAllBytes(Path);using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=Sha256)throw new ArgumentException("Frozen E fixture changed");return T01QualityFixture.Parse(new UTF8Encoding(false,true).GetString(bytes),"T01_E_");}
 }
 public sealed class T01IndependentEFixtureTests
 {
  private QwenByteBpeTokenizer tokenizer;
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));}
  private PreparedPrompt Prepare(string input,string setup)=>new Qwen3PromptPreparation(tokenizer,Qwen3PromptProfile.V11).Build(new ContextAssembler(new MemoryContextBuilder(StationScript.CreateSearch()),tokenizer).Build(T01ContextSnapshots.Create(input,setup)));
  private static string Normal(string text)=>Regex.Replace(text.Normalize(NormalizationForm.FormKC),@"[^\w\u4e00-\u9fff]","").ToLowerInvariant();
  [Test]public void FrozenEHasExactClosedDistributionAndAllThreeDeclaredContexts()
  {var rows=T01IndependentEFixture.Load();Assert.That(rows.Count,Is.EqualTo(100));var kinds=new[]{LlmIntentKind.Follow,LlmIntentKind.Wait,LlmIntentKind.Investigate,LlmIntentKind.Warn,LlmIntentKind.Answer,LlmIntentKind.Refuse};var counts=new[]{15,15,20,10,25,15};for(var i=0;i<kinds.Length;i++)Assert.That(rows.Count(r=>r.Expected==kinds[i]),Is.EqualTo(counts[i]));Assert.That(rows.Count(r=>r.Setup=="valley_empty"),Is.EqualTo(19));Assert.That(rows.Count(r=>r.Setup=="cabin_known"),Is.EqualTo(37));Assert.That(rows.Count(r=>r.Setup=="lab_known"),Is.EqualTo(44));}
  [Test]public void EDoesNotRepeatBOrCOrDOrAnyV11ExampleAfterNormalization()
  {var previous=T01QualityFixture.Load().Concat(T01IndependentFixture.Load()).Select(r=>Normal(r.Input)).Concat(JObject.Parse(File.ReadAllText("Tools/Stage3/Fixtures/Q3_Validation100_B.json"))["cases"].Select(r=>Normal(r.Value<string>("input")))).Concat(Qwen3PromptProfile.V11.Examples.Select(e=>Normal(e.User))).ToHashSet();var current=T01IndependentEFixture.Load().Select(r=>Normal(r.Input)).ToArray();Assert.That(current.Distinct().Count(),Is.EqualTo(100));Assert.That(current.Any(previous.Contains),Is.False);}
  [Test]public void All100FitExactV11BudgetsAndSupplyEveryRequestedActionTarget()
  {foreach(var row in T01IndependentEFixture.Load()){var p=Prepare(row.Input,row.Setup);Assert.That(p.InputIds.Count,Is.LessThanOrEqualTo(1536),row.Id);Assert.That(p.InputIds.Count+256,Is.LessThanOrEqualTo(2048));Assert.That(p.MemoryEntries,Is.LessThanOrEqualTo(8));Assert.That(p.MemoryTokens,Is.LessThanOrEqualTo(512));Assert.That(p.MemoryUtf8Bytes,Is.LessThanOrEqualTo(1200));Assert.That(p.EffectiveTargets.Count,Is.LessThanOrEqualTo(8));Assert.That(p.Body,Does.EndWith(row.Input+"\n").And.Not.Contain("7316"));if(row.Target!=null)Assert.That(p.EffectiveTargets.Any(t=>t.Id==row.Target),Is.True,row.Id);}}
  [Test]public void NeitherExpectedIntentNorTargetCanAlterActualEInputIds()
  {foreach(var row in T01IndependentEFixture.Load().Where((r,i)=>i%11==0)){var alternative=new T01Case(row.Id,row.Setup,row.Input,LlmIntentKind.Refuse,"expected_only_sentinel");var a=Prepare(row.Input,row.Setup);var b=Prepare(alternative.Input,alternative.Setup);Assert.That(b.Body,Is.EqualTo(a.Body).And.Not.Contain("expected_only_sentinel"));CollectionAssert.AreEqual(a.InputIds,b.InputIds);}}
  [Test]public void EIdentifiersCannotMasqueradeAsPriorOrArbitrarySuites()
  {var text=File.ReadAllText(T01IndependentEFixture.Path);Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(text));Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(text,"T01_D_"));Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(text,"anything_"));}
 }
}
