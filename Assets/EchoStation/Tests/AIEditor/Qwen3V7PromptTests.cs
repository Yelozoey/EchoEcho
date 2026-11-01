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
 internal static class T01Diagnostic30
 {
  public static readonly int[] Numbers={1,4,6,10,11,16,18,19,23,29,31,36,41,48,49,51,53,55,57,58,61,66,71,76,81,89,90,92,93,98};
  public static T01Case[] Cases()=>Numbers.Select(n=>T01QualityFixture.Load()[n-1]).ToArray();
 }
 public sealed class Qwen3V7PromptTests
 {
  [Test]public void CandidateBytesShaAndDefaultIsolationAreExact()
  {Assert.That(Qwen3PromptProfile.V7.Text,Is.EqualTo(File.ReadAllText("Docs/Stage3/Prompts/Q3_V7_ShortBoundaries.txt")));Assert.That(Qwen3PromptProfile.V7.Sha256,Is.EqualTo(Qwen3V7Prompt.Sha256));Assert.That(Qwen3PromptProfile.Selected,Is.SameAs(Qwen3PromptProfile.V5));}
  [Test]public void DiagnosticSubsetIsFixedBalancedAndExplicitlySeen()
  {var rows=T01Diagnostic30.Cases();Assert.That(rows.Length,Is.EqualTo(30));Assert.That(rows.Select(r=>r.Id).Distinct().Count(),Is.EqualTo(30));foreach(LlmIntentKind kind in System.Enum.GetValues(typeof(LlmIntentKind)))Assert.That(rows.Count(r=>r.Expected==kind),Is.EqualTo(5));}
  [Test]public void All100ContextsFitWithAllRequestedTargetsAndNoSecret()
  {
   var tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));var assembler=new ContextAssembler(new MemoryContextBuilder(StationScript.CreateSearch()),tokenizer);var prepare=new Qwen3PromptPreparation(tokenizer,Qwen3PromptProfile.V7);
   foreach(var row in T01QualityFixture.Load()){var result=prepare.Build(assembler.Build(T01ContextSnapshots.Create(row.Input,row.Setup)));Assert.That(result.InputIds.Count,Is.LessThanOrEqualTo(1536),row.Id);Assert.That(result.Body,Does.EndWith(row.Input+"\n").And.Not.Contain("7316"));if(row.Target!=null)Assert.That(result.EffectiveTargets.Any(t=>t.Id==row.Target),Is.True,row.Id);}
  }
 }
}
