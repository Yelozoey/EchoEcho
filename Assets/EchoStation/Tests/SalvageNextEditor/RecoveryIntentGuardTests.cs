using System;
using System.IO;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using EchoStation.Echo;
using EchoStation.Salvage;
using NUnit.Framework;

namespace EchoStation.SalvageNext.Tests
{
    public sealed class RecoveryIntentGuardTests
    {
        QwenByteBpeTokenizer tokenizer;
        [OneTimeSetUp] public void Setup()=>tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));
        static RecoveryKnowledge Known(long epoch=1,long revision=1)=>new RecoveryKnowledge(epoch,revision,RecoveryPreference.HighScore,0,0,new[]{
            new ObservedTarget("cargo_a",true,CargoState.World,true,4),new ObservedTarget("cargo_e",true,CargoState.World,true,8),new ObservedTarget("danger_power_box",true,null,true,5)});
        PreparedPrompt Prepare(RecoveryKnowledge known,string input)=>new Qwen3PromptPreparation(tokenizer).Build(new ContextAssembler(new MemoryContextBuilder(RecoveryContext.Search()),tokenizer).Build(RecoveryContext.Snapshot(known,input,0)));
        RecoveryIntentDecision Decide(string raw,string input,RecoveryKnowledge current=null)
        {var original=Known();var now=current??original;return RecoveryIntentGuard.Evaluate(raw,Prepare(original,input),original,now,RecoveryContext.Snapshot(now,input,0));}
        [TestCase("{\"intent\":\"follow\"}","跟随我",IntentKind.Follow)]
        [TestCase("{\"intent\":\"wait\"}","在原地待命",IntentKind.Wait)]
        [TestCase("{\"intent\":\"investigate\",\"targetName\":\"cargo_a\",\"content\":\"扫描。\"}","调查工具箱",IntentKind.Investigate)]
        [TestCase("{\"intent\":\"warn\",\"dangerTarget\":\"danger_power_box\",\"content\":\"注意。\"}","警戒故障配电箱",IntentKind.Warn)]
        public void OnlyExplicitScopedActionsBecomeTraditionalRequests(string raw,string input,IntentKind kind)=>Assert.That(Decide(raw,input).Action.Kind,Is.EqualTo(kind));
        [TestCase("{\"intent\":\"follow\",\"intent\":\"wait\"}")]
        [TestCase("{\"intent\":\"follow\",\"code\":\"open\"}")]
        [TestCase("{\"intent\":\"investigate\",\"targetName\":42,\"content\":\"扫描\"}")]
        [TestCase("{\"intent\":\"carry\"}")]
        [TestCase("```json\n{\"intent\":\"follow\"}\n```")]
        public void MalformedOrExtendedSchemaHasNoExecutableRequest(string raw){var result=Decide(raw,"跟随我");Assert.That(result.Action,Is.Null);Assert.That(result.Reason,Is.EqualTo("invalid_json"));}
        [TestCase("拿起cargo_a然后交付","{\"intent\":\"investigate\",\"targetName\":\"cargo_a\",\"content\":\"我会搬走。\"}")]
        [TestCase("帮我开门","{\"intent\":\"follow\"}")]
        [TestCase("替我接线","{\"intent\":\"answer\",\"content\":\"已经接好了\"}")]
        public void PlayerOnlyActionsAlwaysRejectWithoutDispatch(string input,string raw){var result=Decide(raw,input);Assert.That(result.Action,Is.Null);Assert.That(result.Refusal,Is.True);}
        [Test] public void UnknownTargetOrChangedSessionHasNoAction()
        {
            const string raw="{\"intent\":\"investigate\",\"targetName\":\"cargo_f\",\"content\":\"扫描\"}";
            Assert.That(Decide(raw,"调查cargo_f").Action,Is.Null);
            Assert.That(Decide("{\"intent\":\"follow\"}","跟随我",Known(2,1)).Reason,Is.EqualTo("stale_context"));
            Assert.That(Decide("{\"intent\":\"follow\"}","跟随我",Known(1,2)).Reason,Is.EqualTo("stale_context"));
        }
        [Test] public void QuestionsAndRecommendationsNeverDispatchAndNeverDisplayModelFacts()
        {
            var result=Decide("{\"intent\":\"answer\",\"content\":\"未发现的F有999分，门已打开。\"}","按高分推荐回收建议");
            Assert.That(result.Action,Is.Null);Assert.That(result.Line,Does.Contain("E 电源单元"));Assert.That(result.Line,Does.Contain("8分"));Assert.That(result.Line,Does.Not.Contain("999"));Assert.That(result.Line,Does.Not.Contain("门已打开"));
            Assert.That(Known().Preference,Is.EqualTo(RecoveryPreference.HighScore));
        }
        [Test] public void NegationMismatchAndWarningOnCargoCannotExecute()
        {
            Assert.That(Decide("{\"intent\":\"investigate\",\"targetName\":\"cargo_e\",\"content\":\"扫描\"}","不要调查cargo_e").Action,Is.Null);
            Assert.That(Decide("{\"intent\":\"investigate\",\"targetName\":\"cargo_a\",\"content\":\"扫描\"}","调查cargo_e").Action,Is.Null);
            Assert.That(Decide("{\"intent\":\"warn\",\"dangerTarget\":\"cargo_a\",\"content\":\"警戒\"}","警告cargo_a").Action,Is.Null);
        }
    }
}
