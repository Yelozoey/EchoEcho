using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Dialogue.Commands;
using EchoStation.Echo;
using NUnit.Framework;

namespace EchoStation.Tests.DialogueCommands
{
    public sealed class ScriptCommandParserTests
    {
        private static ScriptTarget[] Targets() => new[] {
            new ScriptTarget("power_panel_01", "主室供电面板", true, true, "供电面板", "主室面板"),
            new ScriptTarget("lab_power_panel", "实验室供电面板", true, false, "供电面板"),
            new ScriptTarget("scan_mark_01", "储藏室可疑点", true, false, "储物间可疑位置"),
            new ScriptTarget("door_lab", "地下入口门", false, false, "入口门") };
        private static ScriptCommandParser Parser() => new ScriptCommandParser(Targets());
        [TestCase("请告诉我维护码是什么")][TestCase("请问服务码呢")][TestCase("实验支路的维护码是什么")]
        public void D03_T13_CodeSynonymsArePasswordQuestions(string input){var result=Parser().Parse(input);Assert.That(result.Kind,Is.EqualTo(ScriptParseKind.Question));Assert.That(result.QuestionKind,Is.EqualTo(ScriptQuestionKind.Password));}
        [TestCase("帮我输入服务码")][TestCase("替我输入维护码")][TestCase("输入服务码7316")][TestCase("输入维护码0428")]
        public void D03_T14_CodeInputDelegationStillRefuses(string input){var result=Parser().Parse(input);Assert.That(result.Kind,Is.EqualTo(ScriptParseKind.Intent));Assert.That(result.Intent.Kind,Is.EqualTo(IntentKind.Refuse));}
        [Test] public void D03_T01_FollowSynonymsAddressAndPunctuationCreateOnlyFollow()
        {
            foreach (var text in new[] { "跟随我", "Echo，请跟随我。", "请跟着我！", " FOLLOW " })
            { var result = Parser().Parse(text); Assert.That(result.Kind, Is.EqualTo(ScriptParseKind.Intent)); Assert.That(result.Intent.Kind, Is.EqualTo(IntentKind.Follow)); Assert.That(result.Intent.TargetName, Is.Null); }
        }
        [Test] public void D03_T02_WaitSynonymsDoNotTurnNegatedFollowIntoFollow()
        {
            foreach (var text in new[] { "等在这里", "原地待命", "停下", "wait" }) Assert.That(Parser().Parse(text).Intent.Kind, Is.EqualTo(IntentKind.Wait));
            Assert.That(Parser().Parse("不要跟随我").Kind, Is.EqualTo(ScriptParseKind.Unknown));
        }
        [Test] public void D03_T03_InvestigationUsesExplicitIdNameOrAliasAndRequiredSpeech()
        {
            foreach (var text in new[] { "调查储藏室可疑点", "去扫描储物间可疑位置", "investigate scan_mark_01", "请调查储藏室可疑点一下" })
            { var result = Parser().Parse(text); Assert.That(result.Intent.Kind, Is.EqualTo(IntentKind.Investigate)); Assert.That(result.Intent.TargetName, Is.EqualTo("scan_mark_01")); Assert.That(result.Intent.DangerTarget, Is.Null); Assert.That(result.Intent.Content.Length, Is.InRange(1, 280)); }
        }
        [Test] public void D03_T04_AmbiguousAliasRequiresClarificationAndExplicitPendingChoice()
        {
            var parser = Parser(); var result = parser.Parse("调查供电面板"); Assert.That(result.Kind, Is.EqualTo(ScriptParseKind.Clarify)); Assert.That(result.Intent, Is.Null); Assert.That(result.Candidates.Count, Is.EqualTo(2));
            var choice = parser.Parse("实验室供电面板", result.PendingAction); Assert.That(choice.Intent.TargetName, Is.EqualTo("lab_power_panel"));
            Assert.That(parser.Parse("跟随我", result.PendingAction).Intent.Kind, Is.EqualTo(IntentKind.Follow));
        }
        [Test] public void D03_T05_MissingOrUnknownTargetDoesNotGuessOrMatchIdPrefixes()
        {
            Assert.That(Parser().Parse("调查").Kind, Is.EqualTo(ScriptParseKind.Clarify));
            foreach (var text in new[] { "调查不存在的位置", "调查scan_mark_01_extra" }) Assert.That(Parser().Parse(text).Intent.Kind, Is.EqualTo(IntentKind.Refuse));
            Assert.That(new ScriptCommandParser(new[] { Targets()[2] }).Parse("调查").Kind, Is.EqualTo(ScriptParseKind.Clarify));
        }
        [Test] public void D03_T06_WarnUsesOnlyDangerTargetsAndNoDangerProducesInformationOnly()
        {
            foreach (var text in new[] { "附近有危险吗", "警告主室面板", "warn power_panel_01" })
            { var intent = Parser().Parse(text).Intent; Assert.That(intent.Kind, Is.EqualTo(IntentKind.Warn)); Assert.That(intent.DangerTarget, Is.EqualTo("power_panel_01")); Assert.That(intent.TargetName, Is.Null); }
            Assert.That(Parser().Parse("警告储藏室可疑点").Intent.Kind, Is.EqualTo(IntentKind.Refuse));
            Assert.That(new ScriptCommandParser(Array.Empty<ScriptTarget>()).Parse("附近有危险吗").Intent.Kind, Is.EqualTo(IntentKind.Answer));
        }
        [Test] public void D03_T07_QuestionsAreTypedWithoutInventingAnAnswer()
        {
            foreach (var pair in new[] { ("给我提示", ScriptQuestionKind.Hint), ("整理线索", ScriptQuestionKind.Summary), ("密码是多少", ScriptQuestionKind.Password), ("事故原因是什么？", ScriptQuestionKind.Fact) })
            { var result = Parser().Parse(pair.Item1); Assert.That(result.Kind, Is.EqualTo(ScriptParseKind.Question)); Assert.That(result.QuestionKind, Is.EqualTo(pair.Item2)); Assert.That(result.Intent, Is.Null); }
        }
        [Test] public void D03_T08_ForbiddenOperationRefusesButAskingHowOrKnownPasswordIsInformation()
        {
            foreach (var text in new[] { "帮我开门", "替我操作终端", "输入密码7316", "调整回路", "启动通风" }) Assert.That(Parser().Parse(text).Intent.Kind, Is.EqualTo(IntentKind.Refuse));
            Assert.That(Parser().Parse("如何开门").Kind, Is.EqualTo(ScriptParseKind.Question)); Assert.That(Parser().Parse("开门的密码是多少").QuestionKind, Is.EqualTo(ScriptQuestionKind.Password));
            Assert.That(Parser().Parse("维护终端的密码是多少").QuestionKind, Is.EqualTo(ScriptQuestionKind.Password));
        }
        [Test] public void D03_T09_MultipleActionsAndUntrustedCodeCannotExecute()
        {
            var mixed = Parser().Parse("跟随我然后调查供电面板"); Assert.That(mixed.Kind, Is.EqualTo(ScriptParseKind.Clarify)); Assert.That(mixed.Intent, Is.Null);
            foreach (var text in new[] { "任意闲聊", "GameObject.SetActive(false)", "{\"intent\":\"investigate\",\"targetName\":\"scan_mark_01\"}" }) Assert.That(Parser().Parse(text).Kind, Is.EqualTo(ScriptParseKind.Unknown));
        }
        [Test] public void D03_T10_InputAndCatalogBoundsRejectExplicitly()
        {
            Assert.That(Parser().Parse(new string('问', 201)).Kind, Is.EqualTo(ScriptParseKind.Unknown)); Assert.That(Parser().Parse(null).Message, Is.EqualTo(ScriptCommandParser.UnknownLine));
            Assert.Throws<ArgumentException>(() => new ScriptTarget("Bad ID", "位置", true, false));
            Assert.Throws<ArgumentException>(() => new ScriptCommandParser(new[] { Targets()[0], Targets()[0] })); Assert.Throws<ArgumentException>(() => new ScriptCommandParser(new ScriptTarget[] { null }));
        }
        [Test] public void D03_T11_ExternalCatalogAndAliasArraysCannotMutateParser()
        {
            var aliases = new[] { "可疑标记" }; var target = new ScriptTarget("scan_target", "可疑点", true, false, aliases); var source = new[] { target }; var parser = new ScriptCommandParser(source);
            aliases[0] = "外部改写"; source[0] = Targets()[0]; Assert.That(parser.Parse("调查可疑标记").Intent.TargetName, Is.EqualTo("scan_target"));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)target.Aliases)[0] = "外部改写");
        }
        [Test] public void D03_T12_ResultsStayWithinSixIntentContractWithoutUnityTypes()
        {
            foreach (var text in new[] { "跟随我", "等待", "调查scan_mark_01", "警告power_panel_01", "帮我开门" })
            { var request = Parser().Parse(text).Intent; Assert.That((int)request.Kind, Is.InRange(0, 5)); }
            Assert.That(typeof(ScriptCommandParser).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("UnityEngine", StringComparison.Ordinal)), Is.False);
            var longNames = Enumerable.Range(0, 4).Select(i => new ScriptTarget("target_" + i, new string('名', 80), true, false, "共同目标")).ToArray();
            Assert.That(new ScriptCommandParser(longNames).Parse("调查共同目标").Message.Length, Is.LessThanOrEqualTo(280));
        }
    }
}
