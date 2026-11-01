using System.IO;
using System.Linq;
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
 public sealed class Qwen3V11PromptTests
 {
  private QwenByteBpeTokenizer tokenizer;
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));}
  private PreparedPrompt Prepare(string input,string setup)=>new Qwen3PromptPreparation(tokenizer,Qwen3PromptProfile.V11).Build(new ContextAssembler(new MemoryContextBuilder(StationScript.CreateSearch()),tokenizer).Build(T01ContextSnapshots.Create(input,setup)));
  [Test]public void ExactNewExamplesKeepV10RulesSixSchemasAndTheExistingDefault()
  {var p=Qwen3PromptProfile.V11;Assert.That(p.Text,Is.EqualTo(Qwen3PromptProfile.V10.Text));Assert.That(p.Sha256,Is.EqualTo(Qwen3PromptProfile.V10.Sha256));Assert.That(p.ExamplesSha256,Is.EqualTo(Qwen3V11Prompt.ExamplesSha256).And.Not.EqualTo(Qwen3PromptProfile.V10.ExamplesSha256));Assert.That(Qwen3PromptProfile.Selected,Is.SameAs(Qwen3PromptProfile.V5));var rows=JArray.Parse(File.ReadAllText("Docs/Stage3/Prompts/Q3_V11_BoundaryExamples.json"));Assert.That(rows.Count,Is.EqualTo(8));for(var i=0;i<8;i++){Assert.That(p.Examples[i].User,Is.EqualTo(rows[i].Value<string>("user")));Assert.That(p.Examples[i].Assistant,Is.EqualTo(rows[i].Value<string>("assistant")));Assert.That(IntentJsonParser.Parse(p.Examples[i].Assistant).Success,Is.True);}Assert.That(p.Examples.Select(e=>IntentJsonParser.Parse(e.Assistant).Intent.Kind).Distinct().Count(),Is.EqualTo(6));}
  [Test]public void All200ContextsFitAndRetainRequestedTargetsWithoutSecrets()
  {foreach(var row in T01QualityFixture.Load().Concat(T01IndependentFixture.Load())){var p=Prepare(row.Input,row.Setup);Assert.That(p.InputIds.Count,Is.LessThanOrEqualTo(1536),row.Id);Assert.That(p.InputIds.Count+256,Is.LessThanOrEqualTo(2048));Assert.That(p.MemoryTokens,Is.LessThanOrEqualTo(512));Assert.That(p.MemoryUtf8Bytes,Is.LessThanOrEqualTo(1200));Assert.That(p.Body,Does.EndWith(row.Input+"\n").And.Not.Contain("7316"));if(row.Target!=null)Assert.That(p.EffectiveTargets.Any(t=>t.Id==row.Target),Is.True,row.Id);}}
  [Test]public void BoundaryExamplesDoNotCopyAnySeenWholePlayerInput()
  {var seen=T01QualityFixture.Load().Concat(T01IndependentFixture.Load()).Select(r=>r.Input).ToHashSet();Assert.That(Qwen3PromptProfile.V11.Examples.Any(e=>seen.Contains(e.User)),Is.False);foreach(var row in T01Diagnostic60.Load().Where((r,i)=>i%17==0)){var a=Prepare(row.Input,row.Setup);var changed=new T01Case(row.Id,row.Setup,row.Input,LlmIntentKind.Refuse,"label_only_sentinel");var b=Prepare(changed.Input,changed.Setup);Assert.That(b.Body,Is.EqualTo(a.Body).And.Not.Contain("label_only_sentinel"));CollectionAssert.AreEqual(a.InputIds,b.InputIds);}}
 }
}
