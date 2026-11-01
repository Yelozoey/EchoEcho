using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EchoStation.Dialogue.Commands;
using EchoStation.Echo;
using EchoStation.Memory;

namespace EchoStation.Dialogue.Knowledge
{
    public sealed class ScriptKnowledgeResponder
    {
        public const string NoRecords = "我目前没有相关记录。";
        public const string CurrentZoneMemoryId = "player_current_zone";
        private readonly MemorySession memories;
        private readonly MemoryContextBuilder context;
        private readonly MemorySearch retrieval;
        private readonly HintPlan[] hints;
        private readonly string[] protectedAnswers;
        private readonly Func<double> clock;
        private readonly Dictionary<string, int> hintLevels = new Dictionary<string, int>(StringComparer.Ordinal);
        private ulong memoryVersion, dialogueVersion;
        public MemoryContextSlice LastContext { get; private set; }

        public ScriptKnowledgeResponder(MemorySession session, MemorySearch retrieval, IEnumerable<HintPlan> hintPlans = null, IEnumerable<string> secretAnswers = null, Func<double> gameClock = null)
        {
            memories = session ?? throw new ArgumentNullException(nameof(session)); this.retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval)); context = new MemoryContextBuilder(retrieval);
            hints = hintPlans == null ? Array.Empty<HintPlan>() : hintPlans.ToArray(); protectedAnswers = secretAnswers == null ? Array.Empty<string>() : secretAnswers.ToArray(); clock = gameClock ?? (() => 0);
            if (hints.Length > 64 || hints.Any(h => h == null) || hints.Select(h => h.Id).Distinct(StringComparer.Ordinal).Count() != hints.Length || protectedAnswers.Length > 16 || protectedAnswers.Any(p => string.IsNullOrWhiteSpace(p) || p.Length > 120)) throw new ArgumentException("Invalid knowledge catalog.");
        }
        public IntentRequest Answer(ScriptParseResult question, ulong conversationVersion = 1)
        {
            if (question == null || question.Kind != ScriptParseKind.Question) return ScriptCommands.Refuse("请先明确需要查询的信息。");
            return AnswerKnown(question.Input, question.QuestionKind, conversationVersion);
        }
        // Semantic callers may select a question category, never an executable action.
        public IntentRequest AnswerKnown(string input, ScriptQuestionKind kind, ulong conversationVersion = 1)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length > 200 || kind < ScriptQuestionKind.Hint || kind > ScriptQuestionKind.Fact)
                return ScriptCommands.Refuse("请先明确需要查询的信息。");
            try { new UTF8Encoding(false, true).GetByteCount(input); }
            catch (EncoderFallbackException) { return ScriptCommands.Refuse("请先明确需要查询的信息。"); }
            if (memoryVersion != memories.Version || dialogueVersion != conversationVersion) { hintLevels.Clear(); memoryVersion = memories.Version; dialogueVersion = conversationVersion; }
            var known = memories.Store.CaptureSnapshot();
            try
            {
                if (kind == ScriptQuestionKind.Hint) return Hint(known);
                if (kind == ScriptQuestionKind.Password) return Password(input, known);
                var query = kind == ScriptQuestionKind.Summary ? "线索 记录 资料" : input;
                LastContext = context.Build(known, query, clock());
                var lines = LastContext.Entries.Select(e => e.Match.Record is ClueMemory c ? c.Title + "：" + c.Summary : e.Match.Record.Summary);
                var selected = new List<string>(); var length = 0;
                foreach (var line in lines)
                {
                    var safe = Mask(line, known); if (safe.Length > 280 || length + safe.Length + (selected.Count == 0 ? 0 : 1) > 280) continue;
                    selected.Add(safe); length += safe.Length + (selected.Count == 1 ? 0 : 1); if (selected.Count == 3) break;
                }
                return ScriptCommands.Answer(selected.Count == 0 ? NoRecords : string.Join("\n", selected));
            }
            catch (ArgumentException) { return ScriptCommands.Refuse("当前记录不可用，请稍后再问。"); }
        }
        private IntentRequest Password(string input, MemorySnapshot known)
        {
            var scope = retrieval.ExplicitClueScope(input);
            var generic = Regex.Replace(input ?? "", @"[\s?？。！!，,]", "");
            var genericRequest = Regex.IsMatch(generic, @"\A(?:请|请问|麻烦)?(?:告诉我|复述|再说|提醒我)?(?:一下|一遍)?(?:已知的|已归档的|已知|已归档)?(?:终端的|终端)?(?:密码|服务码|维护码)(?:是|为)?(?:多少|什么|啥|哪个|哪些|有哪些|呢)?(?:吗)?\z", RegexOptions.CultureInvariant);
            // Named unknown devices must not fall back to a password group from another device.
            var query = scope.Count > 0 ? string.Join(" ", scope) : genericRequest ? input : "";
            LastContext = context.Build(known, query, clock());
            if (query.Length == 0) return ScriptCommands.Answer(NoRecords);
            var candidates = LastContext.Entries.Where(e => e.Match.Record is ClueMemory c && c.Password != null && (scope.Count == 0 || scope.Contains(c.Id))).ToArray();
            if (candidates.Length == 0) return ScriptCommands.Answer(NoRecords);
            var maximum = candidates.Max(c => c.Match.Relevance); var best = candidates.Where(c => c.Match.Relevance == maximum).Select(c => (ClueMemory)c.Match.Record).ToArray();
            if (best.Select(c => c.Password).Distinct(StringComparer.Ordinal).Count() != 1) return ScriptCommands.Answer("已有多条终端维护码。请明确终端或线索名称。");
            return ScriptCommands.Answer("已归档的" + best[0].Title + "维护码是" + best[0].Password + "。请由你输入。");
        }
        private IntentRequest Hint(MemorySnapshot known)
        {
            var zone = known.Characters.FirstOrDefault(c => c.Id == CurrentZoneMemoryId)?.TargetId;
            var plan = hints.FirstOrDefault(h => (h.ZoneId == null || h.ZoneId == zone) && h.Required.All(r => r.Present(known)) && (h.CompletedEventId == null || !known.Events.Any(e => e.Id == h.CompletedEventId)));
            if (plan == null)
            {
                LastContext = context.Build(known, "提示 线索", clock());
                return ScriptCommands.Answer("我目前没有足够资料。请先检查周围可读取的记录。");
            }
            LastContext = context.Build(known, plan.RetrievalQuery, clock());
            var level = hintLevels.TryGetValue(plan.Id, out var current) ? Math.Min(current + 1, 3) : 1; hintLevels[plan.Id] = level;
            var text = Mask(plan.Levels[level - 1], known);
            return ScriptCommands.Answer(text.Length <= 280 ? text : "这条记录较长，请在日志中复核。我可以根据具体问题给出提示。");
        }
        private string Mask(string text, MemorySnapshot known)
        {
            foreach (var value in protectedAnswers.Concat(known.Clues.Where(c => c.Password != null).Select(c => c.Password)).Distinct(StringComparer.Ordinal)) text = text.Replace(value, "已归档的维护码");
            return text;
        }
    }
}
