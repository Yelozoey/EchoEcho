using System;
using System.Linq;
using EchoStation.Dialogue.Commands;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.DialogueKnowledge
{
    public sealed class ScriptKnowledgeTests
    {
        private static ScriptParseResult Question(string text) => new ScriptCommandParser(Array.Empty<ScriptTarget>()).Parse(text);
        private static MemorySearch Search() => new MemorySearch(new[] { new MemoryKeywordGroup("密码", "服务码", "维护码", "终端"), new MemoryKeywordGroup("供电", "回路", "排风"), new MemoryKeywordGroup("线索", "记录", "资料") }, new[] {
            new MemoryAlias(MemoryCategory.Clue, "cabin_note", "小屋终端", "线索", "提示"),
            new MemoryAlias(MemoryCategory.Clue, "vent_note", "通道终端", "线索", "提示") });
        private static void Zone(MemorySession session, string id)
        {
            var record = new CharacterMemory(ScriptKnowledgeResponder.CurrentZoneMemoryId, CharacterMemoryKind.PlayerKnowledge, "玩家当前区域。", id);
            if (session.Store.TryGetCharacter(record.Id, out _)) session.Store.UpdateCharacter(record); else session.Store.AddCharacter(record);
        }
        private static HintPlan Plan(string[] lines = null) => new HintPlan("hint_power", "lab_a", "progress_pz03", "供电 回路", new[] { new MemoryFactRequirement(MemoryCategory.Clue, "circuit_note") }, lines ?? new[] { "线索支持独立排风。", "区分排风与内循环。", "按已知记录核对三条支路，再由你确认送电。" });
        private static ScriptKnowledgeResponder Responder(MemorySession session, params HintPlan[] plans) => new ScriptKnowledgeResponder(session, Search(), plans, new[] { "7316", "0428" });
        [Test] public void D05_T01_UnknownFactsAndPasswordsAreNeverInvented()
        {
            var session = new MemorySession(); var responder = Responder(session);
            foreach (var input in new[] { "密码是多少", "供电记录是什么？", "整理线索" }) Assert.That(responder.Answer(Question(input)).Content, Is.EqualTo(ScriptKnowledgeResponder.NoRecords));
            Assert.That(responder.Answer(Question("给我提示")).Content, Does.Not.Contain("7316")); Assert.That(responder.LastContext.Entries, Is.Empty);
        }
        [Test] public void D05_T02_OnlyExplicitKnownMetadataAnswersPasswordQuestion()
        {
            var session = new MemorySession(); session.Store.AddClue(new ClueMemory("cabin_note", "小屋维护记录", "终端服务码已归档。", MemoryClueKind.Note, "7316")); var responder = Responder(session);
            Assert.That(responder.Answer(Question("密码是多少")).Content, Does.Contain("7316"));
            var other = new MemorySession(); other.Store.AddClue(new ClueMemory("cabin_note", "小屋维护记录", "终端文字中有7316，但没有密码元数据。", MemoryClueKind.Note));
            Assert.That(Responder(other).Answer(Question("密码是多少")).Content, Is.EqualTo(ScriptKnowledgeResponder.NoRecords));
        }
        [Test] public void D05_T03_SummariesAndFactRepliesMaskKnownPasswordsUnlessExplicitlyAsked()
        {
            var session = new MemorySession(); session.Store.AddClue(new ClueMemory("cabin_note", "小屋记录", "供电终端服务码7316已记录。", MemoryClueKind.Note, "7316")); var responder = Responder(session);
            Assert.That(responder.Answer(Question("整理线索")).Content, Does.Not.Contain("7316")); Assert.That(responder.Answer(Question("供电记录是什么？")).Content, Does.Not.Contain("7316"));
            Assert.That(responder.Answer(Question("密码是多少")).Content, Does.Contain("7316"));
        }
        [Test] public void D05_T04_MultipleKnownCodesClarifyAndSpecificAliasSelectsOnlyRelevantCode()
        {
            var session = new MemorySession(); session.Store.AddClue(new ClueMemory("cabin_note", "小屋记录", "终端服务码", MemoryClueKind.Note, "7316")); session.Store.AddClue(new ClueMemory("vent_note", "通道记录", "终端维护码", MemoryClueKind.Note, "0428")); var responder = Responder(session);
            var ambiguous = responder.Answer(Question("密码是多少")).Content; Assert.That(ambiguous, Does.Contain("明确")); Assert.That(ambiguous, Does.Not.Contain("7316")); Assert.That(ambiguous, Does.Not.Contain("0428"));
            var specific = responder.Answer(Question("小屋终端的密码是多少")).Content; Assert.That(specific, Does.Contain("7316")); Assert.That(specific, Does.Not.Contain("0428"));
        }
        [Test] public void D05_T05_HintsRequireKnownFactsAndCurrentZoneThenAdvanceThroughThreeLevels()
        {
            var session = new MemorySession(); var plan = Plan(); var responder = Responder(session, plan);
            Assert.That(responder.Answer(Question("给我提示")).Content, Does.Contain("没有足够资料"));
            session.Store.AddClue(new ClueMemory("circuit_note", "回路记录", "供电要求独立排风。", MemoryClueKind.Note)); Zone(session, "cabin");
            Assert.That(responder.Answer(Question("给我提示")).Content, Does.Contain("没有足够资料")); Zone(session, "lab_a");
            foreach (var expected in new[] { plan.Levels[0], plan.Levels[1], plan.Levels[2], plan.Levels[2] }) Assert.That(responder.Answer(Question("给我提示")).Content, Is.EqualTo(expected));
        }
        [Test] public void D05_T06_AllRequirementsAndCompletionGateHintsWithoutChangingProgress()
        {
            var session = new MemorySession(); Zone(session, "lab_a");
            var plan = new HintPlan("hint_power", "lab_a", "progress_pz03", "供电 回路", new[] { new MemoryFactRequirement(MemoryCategory.Clue, "circuit_note"), new MemoryFactRequirement(MemoryCategory.Event, "known_lab_route") }, "线索支持独立排风。", "区分排风与内循环。", "按已知记录核对后由你送电。");
            var responder = Responder(session, plan);
            session.Store.AddClue(new ClueMemory("circuit_note", "回路记录", "供电要求独立排风。", MemoryClueKind.Note));
            Assert.That(responder.Answer(Question("给我提示")).Content, Does.Contain("没有足够资料"));
            session.Store.AddEvent("known_lab_route", 0, "已知实验室路线。");
            var before = session.CaptureJson(); responder.Answer(Question("给我提示")); Assert.That(session.CaptureJson(), Is.EqualTo(before));
            session.Store.AddEvent("progress_pz03", 0, "玩家已确认供电。"); Assert.That(responder.Answer(Question("给我提示")).Content, Does.Contain("没有足够资料"));
        }
        [Test] public void D05_T07_MemoryAndConversationVersionsResetHintLevel()
        {
            var session = new MemorySession(); Zone(session, "lab_a"); session.Store.AddClue(new ClueMemory("circuit_note", "回路记录", "供电要求独立排风。", MemoryClueKind.Note)); var responder = Responder(session, Plan()); var question = Question("给我提示");
            responder.Answer(question, 1); Assert.That(responder.Answer(question, 1).Content, Is.EqualTo(Plan().Levels[1])); Assert.That(responder.Answer(question, 2).Content, Is.EqualTo(Plan().Levels[0]));
            var saved = session.CaptureJson(); Assert.That(session.TryRestore(saved, out _), Is.True); Assert.That(responder.Answer(question, 2).Content, Is.EqualTo(Plan().Levels[0]));
        }
        [Test] public void D05_T08_ContextAndReplyBudgetsKeepWholeFacts()
        {
            var session = new MemorySession(); for (var i = 0; i < 12; i++) session.Store.AddClue(new ClueMemory("record_" + i, new string('题', 80), "供电" + new string('摘', 118), MemoryClueKind.Note)); var responder = Responder(session);
            var answer = responder.Answer(Question("供电记录是什么？")); Assert.That(answer.Content.Length, Is.LessThanOrEqualTo(280)); Assert.That(responder.LastContext.Entries.Count, Is.LessThanOrEqualTo(8)); Assert.That(responder.LastContext.Utf8Bytes, Is.LessThanOrEqualTo(1200)); Assert.That(answer.Content, Does.EndWith(new string('摘', 118)));
        }
        [Test] public void D05_T09_CatalogCopiesArraysAndConfiguredSecretsCannotLeakThroughHintTemplate()
        {
            var levels = new[] { "已知记录提到了0428。", "核对回路。", "由你确认。" }; var plan = Plan(levels); levels[0] = "外部改写";
            var session = new MemorySession(); Zone(session, "lab_a"); session.Store.AddClue(new ClueMemory("circuit_note", "回路记录", "供电说明。", MemoryClueKind.Note));
            var reply = Responder(session, plan).Answer(Question("给我提示")).Content; Assert.That(reply, Does.Not.Contain("0428")); Assert.That(reply, Does.Not.Contain("外部改写"));
        }
        [Test] public void D05_T10_AnswerNeverBecomesAnActionAndHasNoDirectEngineDependency()
        {
            var responder = Responder(new MemorySession()); Assert.That(responder.Answer(Question("跟随我")).Kind, Is.EqualTo(IntentKind.Refuse));
            foreach (var input in new[] { "给我提示", "整理线索", "密码是多少", "供电记录是什么？" }) Assert.That(responder.Answer(Question(input)).Kind, Is.EqualTo(IntentKind.Answer));
            Assert.That(typeof(ScriptKnowledgeResponder).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("UnityEngine", StringComparison.Ordinal)), Is.False);
        }
        [Test] public void D05_T11_InvalidHintAndKnowledgeCatalogsReject()
        {
            Assert.Throws<ArgumentException>(() => new HintPlan("hint_bad", "lab_a", null, "供电", Array.Empty<MemoryFactRequirement>(), "只有一级"));
            Assert.Throws<ArgumentException>(() => new MemoryFactRequirement((MemoryCategory)9, "valid_id"));
            Assert.Throws<ArgumentException>(() => new ScriptKnowledgeResponder(new MemorySession(), Search(), new[] { Plan(), Plan() }));
            Assert.Throws<ArgumentException>(() => new ScriptKnowledgeResponder(new MemorySession(), Search(), secretAnswers: new[] { " " }));
        }
    }
}
