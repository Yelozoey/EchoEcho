using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.AI.Tokenization;
using EchoStation.L01;
using EchoStation.L02;
using EchoStation.Memory;
using NUnit.Framework;
namespace EchoStation.L02Q01.Tests
{
    public class TrustedRetrievalTests
    {
        QwenByteBpeTokenizer tokenizer;
        [OneTimeSetUp]public void Load(){tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/Qwen3_06B_Q4/tokenizer.json")));}
        ContextAssembler Builder()=>new ContextAssembler(new MemoryContextBuilder(new MemorySearch()),tokenizer);
        static TargetCandidate[] Targets()=>new[]{new TargetCandidate("selected","蒸汽阀门","salvage",true,true,false,true,0,true)};
        static MemoryStore Store(){var s=new MemoryStore();s.AddClue(new ClueMemory("selected","蒸汽阀门","扳手可关闭阀门；操作期间危险仍运行。",MemoryClueKind.Scan));s.AddClue(new ClueMemory("other","其他已知资料","不应灌入的历史摘要",MemoryClueKind.Note));return s;}
        [Test]public void OriginalConstructorStillExistsAndDefaultQueryIsOriginalInput()
        {
            Assert.That(typeof(InferenceSnapshot).GetConstructor(new[]{typeof(MemoryStore),typeof(string),typeof(double),typeof(string),typeof(IEnumerable<TargetCandidate>)}),Is.Not.Null);
            var a=new InferenceSnapshot(Store(),"蒸汽阀门",0,"salvage",Targets());var b=new InferenceSnapshot(Store(),"蒸汽阀门",0,"salvage",Targets(),"蒸汽阀门");Assert.That(a.RetrievalQuery,Is.EqualTo(a.Input));Assert.That(Builder().Build(a).MemoryText,Is.EqualTo(Builder().Build(b).MemoryText));Assert.That(Builder().Build(a).TargetsJson,Is.EqualTo(Builder().Build(b).TargetsJson));
        }
        [Test]public void ExplicitIdRetrievesOnlySelectedReportWithoutRewritingQuestion()
        {
            const string question="这个应该怎么处理？";var s=new InferenceSnapshot(Store(),question,0,"salvage",Targets(),"selected");var r=Builder().Build(s);Assert.That(s.Input,Is.EqualTo(question));Assert.That(r.Entries.Count,Is.EqualTo(1));Assert.That(r.MemoryText,Does.Contain("蒸汽阀门"));Assert.That(r.MemoryText,Does.Contain("扳手"));Assert.That(r.MemoryText,Does.Not.Contain(question));Assert.That(r.MemoryText,Does.Not.Contain("不应灌入"));
        }
        [Test]public void DefaultUnrelatedQuestionStillDoesNotDumpMemory(){Assert.That(Builder().Build(new InferenceSnapshot(Store(),"不存在的资料",0,"salvage",Targets())).Entries,Is.Empty);}
        [TestCase(null)] [TestCase("")] [TestCase(" ")]
        public void InvalidRetrievalKeyRejected(string key){Assert.Catch<ArgumentException>(()=>new InferenceSnapshot(Store(),"问题",0,"salvage",Targets(),key));}
        [Test]public void InvalidUnicodeConstructedInsideTestIsRejected(){var invalid=new string((char)0xd800,1);Assert.Catch<ArgumentException>(()=>new InferenceSnapshot(Store(),"问题",0,"salvage",Targets(),invalid));}
        [Test]public void RetrievalKeyHasIndependent200CharacterBudget(){Assert.Throws<ArgumentException>(()=>new InferenceSnapshot(Store(),"问题",0,"salvage",Targets(),new string('x',201)));Assert.That(new InferenceSnapshot(Store(),"问题",0,"salvage",Targets(),new string('x',200)).Input,Is.EqualTo("问题"));}
        [Test]public void QueryKeyCannotGrantTargetAuthority(){var hidden=new TargetCandidate("hidden","隐藏设备","other_zone",true,true,false,false,5);var s=new InferenceSnapshot(new MemoryStore(),"读取资料",0,"salvage",new[]{hidden},"hidden");Assert.That(s.Targets,Is.Empty);}
        [Test]public void SceneCaptureUsesRealTitleAndKeepsAll200InputCharacters()
        {
            string question="  "+new string('字',195)+"   ";var known=new EchoTextContext(new[]{new EchoTextTarget("selected","蒸汽阀门",true,"扳手可关闭阀门；操作期间危险仍运行。")},"selected",90);var capture=SceneQuestion.Select(question,known,"selected","v1").Capture;Assert.That(capture,Is.Not.Null);Assert.That(capture.Snapshot.Input,Is.EqualTo(question));Assert.That(capture.Snapshot.RetrievalQuery,Is.EqualTo("selected"));Assert.That(capture.Snapshot.Known.Clues[0].Title,Is.EqualTo("蒸汽阀门"));
        }
        [Test]public void ActualQwen3TokenBudgetsKeepSelectedFactAndOriginalQuestion()
        {
            var s=new InferenceSnapshot(Store(),new string('问',200),0,"salvage",Targets(),"selected");var context=Builder().Build(s);var prepared=new Qwen3PromptPreparation(tokenizer,SceneQuestionPrompt.Profile).Build(context);Assert.That(context.MemoryUtf8Bytes,Is.LessThanOrEqualTo(1200));Assert.That(context.MemoryTokens,Is.LessThanOrEqualTo(512));Assert.That(prepared.InputIds.Count,Is.LessThanOrEqualTo(1536));Assert.That(prepared.InputIds.Count+256,Is.LessThanOrEqualTo(2048));Assert.That(prepared.UsedMemory,Does.Contain("操作期间危险仍运行"));Assert.That(prepared.Body,Does.Contain(s.Input));Assert.That(Qwen3PromptProfile.Selected,Is.SameAs(Qwen3PromptProfile.V5));
        }
        [Test]public void SnapshotStillOwnsFrozenMemory(){var store=Store();var snapshot=new InferenceSnapshot(store,"问题",0,"salvage",Targets(),"selected");store.AddClue(new ClueMemory("later","后来资料","后来资料摘要",MemoryClueKind.Note));Assert.That(snapshot.Known.Clues.Count,Is.EqualTo(2));Assert.That(snapshot.RetrievalQuery,Is.EqualTo("selected"));}
    }
}
