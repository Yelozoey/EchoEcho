using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EchoStation.Echo;
using EchoStation.Memory;

namespace EchoStation.Dialogue.Commands
{
    public enum ScriptParseKind { Intent, Question, Clarify, Unknown }
    public enum ScriptQuestionKind { None, Hint, Summary, Password, Fact }

    public sealed class ScriptTarget
    {
        public string Id { get; }
        public string Name { get; }
        public bool CanInvestigate { get; }
        public bool IsDanger { get; }
        public IReadOnlyList<string> Aliases { get; }
        public ScriptTarget(string id, string name, bool investigate, bool danger, params string[] aliases)
        {
            if (!MemoryLimits.IsValidId(id) || string.IsNullOrWhiteSpace(name) || name.Length > 80 || aliases == null || aliases.Length > 16 || aliases.Any(a => string.IsNullOrWhiteSpace(a) || a.Length > 80)) throw new ArgumentException("Invalid bounded script target.");
            Id = id; Name = name; CanInvestigate = investigate; IsDanger = danger;
            Aliases = Array.AsReadOnly(aliases.Select(ScriptCommandParser.Normalize).Distinct(StringComparer.Ordinal).ToArray());
        }
        internal bool Exact(string value) => Id == value || ScriptCommandParser.Normalize(Name) == value || Aliases.Contains(value);
        internal bool Partial(string value)
        {
            foreach (var term in Aliases.Concat(new[] { ScriptCommandParser.Normalize(Name) }))
                if (term.Length >= 2 && term.Any(c => c > 127) && value.Contains(term)) return true;
            return false;
        }
    }

    public sealed class ScriptParseResult
    {
        public ScriptParseKind Kind { get; }
        public IntentRequest Intent { get; }
        public ScriptQuestionKind QuestionKind { get; }
        public string Input { get; }
        public string Message { get; }
        public IntentKind? PendingAction { get; }
        public IReadOnlyList<ScriptTarget> Candidates { get; }
        internal ScriptParseResult(ScriptParseKind kind, string input, IntentRequest intent = null, ScriptQuestionKind question = ScriptQuestionKind.None, string message = "", IntentKind? pending = null, ScriptTarget[] candidates = null)
        { Kind = kind; Input = input; Intent = intent; QuestionKind = question; Message = message; PendingAction = pending; Candidates = Array.AsReadOnly(candidates ?? Array.Empty<ScriptTarget>()); }
    }

    public sealed class ScriptCommandParser
    {
        public const string UnknownLine = "抱歉，我没能理解你的指令，请换一种说法。";
        private readonly ScriptTarget[] targets;
        public ScriptCommandParser(IEnumerable<ScriptTarget> availableTargets)
        {
            if (availableTargets == null) throw new ArgumentNullException(nameof(availableTargets));
            targets = availableTargets.ToArray();
            if (targets.Length > 128 || targets.Any(t => t == null) || targets.Select(t => t.Id).Distinct(StringComparer.Ordinal).Count() != targets.Length) throw new ArgumentException("Invalid available target catalog.");
        }
        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Trim();
        private static bool OneOf(string value, params string[] options) => Array.IndexOf(options, value) >= 0;
        private static bool MentionsPassword(string value) => value.Contains("密码") || value.Contains("服务码") || value.Contains("维护码");
        private static bool MachineRequest(string value)
        {
            var delegatedOperation = new[] { "帮我", "替我", "你来" }.Any(value.Contains) && new[] { "开门", "打开", "解锁", "操作", "输入密码", "输入服务码", "输入维护码", "回路", "开关", "启动通风", "送电" }.Any(value.Contains);
            if (delegatedOperation || new[]{"输入密码","输入服务码","输入维护码"}.Any(v=>value.StartsWith(v,StringComparison.Ordinal))) return true;
            if (MentionsPassword(value) && (value.Contains("是多少") || value.Contains("是什么"))) return false;
            var imperative = new[] { "开门", "打开门", "解锁门", "输入密码", "输入服务码", "输入维护码", "操作终端", "操作设备", "调整回路", "切换供电", "启动通风", "开启通风", "关闭通风", "拨动开关", "替我解谜" };
            return imperative.Any(v => value.StartsWith(v, StringComparison.Ordinal));
        }
        private static bool MultipleActions(string value)
        {
            if (!new[] { "并", "然后", "同时", "再", ";", "和" }.Any(value.Contains)) return false;
            var groups = new[] { new[] { "跟随", "跟着", "follow" }, new[] { "等在", "待命", "停下", "wait" }, new[] { "调查", "扫描", "investigate" }, new[] { "警告", "warn" } };
            return groups.Count(g => g.Any(value.Contains)) > 1;
        }
        public ScriptParseResult Parse(string input, IntentKind? pendingAction = null)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length > 200) return Unknown(input);
            string text;
            try { text = Normalize(input).TrimEnd('。', '.', '!', '?'); }
            catch (ArgumentException) { return Unknown(input); }
            if (text.StartsWith("echo", StringComparison.Ordinal)) text = text.Substring(4).TrimStart(' ', ',', ':', '：');
            if (text.StartsWith("请", StringComparison.Ordinal)) text = text.Substring(1).Trim();
            if (MachineRequest(text)) return Request(input, ScriptCommands.Refuse("我不能代替你操作门或设备。我可以扫描并提供线索。"));
            if (MultipleActions(text)) return new ScriptParseResult(ScriptParseKind.Clarify, input, message: "请一次指定一个行动。");
            if (OneOf(text, "跟随我", "跟着我", "跟上我", "跟上", "跟随", "follow")) return Request(input, ScriptCommands.Follow());
            if (OneOf(text, "等待", "等在这里", "原地待命", "待命", "停下", "wait")) return Request(input, ScriptCommands.Wait());
            if (OneOf(text, "给我提示", "给我一点提示", "提示", "下一步", "下一步做什么")) return Question(input, ScriptQuestionKind.Hint);
            if (OneOf(text, "整理线索", "我们知道什么", "总结线索", "总结记录")) return Question(input, ScriptQuestionKind.Summary);
            if (MentionsPassword(text)) return Question(input, ScriptQuestionKind.Password);
            if (new[] { "怎么", "如何", "为什么", "能否", "是否", "哪里", "什么", "告诉我" }.Any(v => text.StartsWith(v, StringComparison.Ordinal))) return Question(input, ScriptQuestionKind.Fact);
            foreach (var prefix in new[] { "去调查", "去扫描", "investigate ", "调查", "扫描" })
                if (text.StartsWith(prefix, StringComparison.Ordinal)) return Resolve(input, text.Substring(prefix.Length).Trim(), IntentKind.Investigate);
            if (text == "investigate") return Resolve(input, "", IntentKind.Investigate);
            if (text == "附近有危险吗" || text == "检查危险" || text == "warn" || text == "警告") return Resolve(input, "", IntentKind.Warn);
            foreach (var prefix in new[] { "warn ", "警告" })
                if (text.StartsWith(prefix, StringComparison.Ordinal)) return Resolve(input, text.Substring(prefix.Length).Trim(), IntentKind.Warn);
            if (pendingAction == IntentKind.Investigate || pendingAction == IntentKind.Warn) return Resolve(input, text, pendingAction.Value);
            if (input.Contains("?") || input.Contains("？") || text.Contains("是什么") || text.Contains("在哪里")) return Question(input, ScriptQuestionKind.Fact);
            return Unknown(input);
        }
        private ScriptParseResult Resolve(string input, string name, IntentKind kind)
        {
            if (name.EndsWith("一下", StringComparison.Ordinal)) name = name.Substring(0, name.Length - 2).Trim();
            var eligible = targets.Where(t => kind == IntentKind.Investigate ? t.CanInvestigate : t.IsDanger).ToArray();
            if (name.Length == 0)
            {
                if (kind == IntentKind.Warn && eligible.Length == 1) return Selected(input, eligible[0], kind);
                if (kind == IntentKind.Warn && eligible.Length == 0) return Request(input, ScriptCommands.Answer("我目前没有确认附近的危险。"));
                return Clarify(input, kind, eligible);
            }
            var candidates = targets.Where(t => t.Exact(name)).ToArray();
            if (candidates.Length == 0) candidates = targets.Where(t => t.Partial(name)).ToArray();
            if (candidates.Length == 0) return Request(input, ScriptCommands.Refuse("我找不到这个目标。请使用已知名称或唯一ID。"));
            if (candidates.Any(t => kind == IntentKind.Investigate ? !t.CanInvestigate : !t.IsDanger)) return Request(input, ScriptCommands.Refuse("该目标不支持这一行动。我可以提供已有记录。"));
            return candidates.Length == 1 ? Selected(input, candidates[0], kind) : Clarify(input, kind, candidates);
        }
        private static ScriptParseResult Selected(string input, ScriptTarget target, IntentKind kind)
            => Request(input, kind == IntentKind.Investigate ? ScriptCommands.Investigate(target.Id) : ScriptCommands.Warn(target.Id));
        private static ScriptParseResult Clarify(string input, IntentKind kind, ScriptTarget[] candidates)
        {
            var names = string.Join(" / ", candidates.Take(3).Select(t => t.Name));
            var message = candidates.Length == 0 ? "请指定目标的名称或唯一ID。" : "请明确目标：" + names + (candidates.Length > 3 ? "。其余目标请用唯一ID指定。" : "。");
            return new ScriptParseResult(ScriptParseKind.Clarify, input, message: message, pending: kind, candidates: candidates);
        }
        private static ScriptParseResult Request(string input, IntentRequest intent) => new ScriptParseResult(ScriptParseKind.Intent, input, intent);
        private static ScriptParseResult Question(string input, ScriptQuestionKind kind) => new ScriptParseResult(ScriptParseKind.Question, input, question: kind);
        private static ScriptParseResult Unknown(string input) => new ScriptParseResult(ScriptParseKind.Unknown, input, message: UnknownLine);
    }
}
