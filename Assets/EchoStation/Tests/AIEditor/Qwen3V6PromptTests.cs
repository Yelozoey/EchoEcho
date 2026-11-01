using System.IO;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using EchoStation.Story;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3V6PromptTests
 {
  private QwenByteBpeTokenizer tokenizer;
  [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));}
  [Test]public void CandidateIsExactCanonicalDocumentAndDoesNotSelectItself()
  {Assert.That(Qwen3PromptProfile.V6.Sha256,Is.EqualTo(Qwen3V6Prompt.Sha256));Assert.That(Qwen3PromptProfile.V6.Text,Is.EqualTo(File.ReadAllText("Docs/Stage3/Prompts/Q3_V6_ActionPriority.txt")));Assert.That(Qwen3PromptProfile.Selected,Is.SameAs(Qwen3PromptProfile.V5));}
  [Test]public void AllDiagnostic100FitAndRetainEveryRequestedActionTarget()
  {
   foreach(var row in T01QualityFixture.Load())
   {
    var snapshot=T01ContextSnapshots.Create(row.Input,row.Setup);var context=new ContextAssembler(new MemoryContextBuilder(StationScript.CreateSearch()),tokenizer).Build(snapshot);var prepared=new Qwen3PromptPreparation(tokenizer,Qwen3PromptProfile.V6).Build(context);
    Assert.That(prepared.InputIds.Count,Is.LessThanOrEqualTo(1536),row.Id);Assert.That(prepared.InputIds.Count+256,Is.LessThanOrEqualTo(2048));Assert.That(prepared.MemoryEntries,Is.LessThanOrEqualTo(8));Assert.That(prepared.MemoryTokens,Is.LessThanOrEqualTo(512));Assert.That(prepared.MemoryUtf8Bytes,Is.LessThanOrEqualTo(1200));Assert.That(prepared.Body,Does.EndWith(row.Input+"\n").And.Not.Contain("7316"));if(row.Target!=null)Assert.That(prepared.EffectiveTargets.Any(t=>t.Id==row.Target),Is.True,row.Id);
   }
  }
  [Test]public void RevisionAddsPriorityBoundariesWithoutGameAnswersOrNewSchema()
  {var text=Qwen3PromptProfile.V6.Text;Assert.That(text,Does.Contain("停止或解除跟随→wait").And.Contain("intent也必须是refuse").And.Contain("危险提醒→warn"));Assert.That(text,Does.Not.Contain("7316").And.Not.Contain("power_panel_01").And.Not.Contain("note_lab_circuit_01"));}
 }
}
