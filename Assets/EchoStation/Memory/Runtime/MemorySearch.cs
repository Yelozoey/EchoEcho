using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EchoStation.Memory
{
    public sealed class MemoryKeywordGroup
    {
        public IReadOnlyList<string> Terms { get; }
        public MemoryKeywordGroup(params string[] terms)
        {
            if (terms == null || terms.Length == 0 || terms.Length > 16) throw new ArgumentException("Keyword group requires 1–16 terms.");
            var copy = new string[terms.Length]; var unique = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < terms.Length; i++)
            {
                var term = MemorySearch.Normalize(terms[i]);
                if (term.Length < 2 || term.Length > 32 || !unique.Add(term)) throw new ArgumentException("Invalid or repeated bounded keyword.");
                copy[i] = term;
            }
            Terms = Array.AsReadOnly(copy);
        }
    }

    public sealed class MemoryAlias
    {
        public MemoryCategory Category { get; }
        public string Id { get; }
        public MemoryKeywordGroup Keywords { get; }
        public MemoryAlias(MemoryCategory category, string id, params string[] terms)
        {
            if ((uint)category > (uint)MemoryCategory.Character) throw new ArgumentException("Invalid memory category.");
            Category = category; Id = MemoryLimits.Id(id, nameof(id)); Keywords = new MemoryKeywordGroup(terms);
        }
    }

    public sealed class MemoryMatch
    {
        public MemoryCategory Category { get; }
        public MemoryRecord Record { get; }
        public int Relevance { get; }
        internal MemoryMatch(MemoryCategory category, MemoryRecord record, int relevance)
        { Category = category; Record = record; Relevance = relevance; }
    }

    public sealed class MemorySearch
    {
        public const int MaximumQueryCharacters = 200;
        private readonly MemoryKeywordGroup[] groups;
        private readonly MemoryAlias[] aliases;
        public MemorySearch(IEnumerable<MemoryKeywordGroup> keywordGroups = null, IEnumerable<MemoryAlias> targetAliases = null)
        {
            groups = keywordGroups == null ? Array.Empty<MemoryKeywordGroup>() : keywordGroups.ToArray();
            aliases = targetAliases == null ? Array.Empty<MemoryAlias>() : targetAliases.ToArray();
            if (groups.Length > 64 || groups.Any(g => g == null) || aliases.Length > 1024 || aliases.Any(a => a == null)) throw new ArgumentException("Invalid bounded search catalog.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var alias in aliases) if (!ids.Add((int)alias.Category + ":" + alias.Id)) throw new ArgumentException("Duplicate alias identity.");
        }

        public IReadOnlyList<MemoryMatch> Search(MemorySnapshot known, string question)
        {
            if (known == null) throw new ArgumentNullException(nameof(known));
            if (question != null && question.Length > MaximumQueryCharacters) throw new ArgumentException("Question exceeds character budget.");
            var query = Normalize(question); var result = new List<MemoryMatch>();
            if (query.Length == 0) return Array.AsReadOnly(result.ToArray());
            foreach (var item in known.Events) Add(result, MemoryCategory.Event, item, query, item.Id + " " + item.Summary);
            foreach (var item in known.Clues) Add(result, MemoryCategory.Clue, item, query, item.Id + " " + item.Title + " " + item.Summary + " " + item.Password + " " + item.KeyMarker);
            foreach (var item in known.Characters) Add(result, MemoryCategory.Character, item, query, item.Id + " " + item.Summary + " " + item.TargetId);
            result.Sort((a, b) => { var score = b.Relevance.CompareTo(a.Relevance); if (score != 0) return score; var category = a.Category.CompareTo(b.Category); return category != 0 ? category : string.CompareOrdinal(a.Record.Id, b.Record.Id); });
            return Array.AsReadOnly(result.ToArray());
        }

        // The catalog may name an undiscovered target. Scope carries identity only,
        // never its text or value; callers must still select from known records.
        public IReadOnlyList<string> ExplicitClueScope(string question)
        {
            if (question != null && question.Length > MaximumQueryCharacters) throw new ArgumentException("Question exceeds character budget.");
            var query = Normalize(question);
            return Array.AsReadOnly(aliases.Where(a => a.Category == MemoryCategory.Clue &&
                (Contains(query, a.Id) || Matches(query, a.Keywords))).Select(a => a.Id).ToArray());
        }

        private void Add(List<MemoryMatch> result, MemoryCategory category, MemoryRecord item, string query, string text)
        {
            var body = Normalize(text); var score = Contains(query, item.Id) ? 100 : 0;
            if (item is CharacterMemory state && state.TargetId != null && Contains(query, state.TargetId)) score += 100;
            if (item is ClueMemory clue)
            {
                if (Normalize(clue.Title).Length >= 2 && Contains(query, Normalize(clue.Title))) score += 30;
                if (clue.Password != null && Contains(query, Normalize(clue.Password))) score += 30;
                if (clue.KeyMarker != null && Normalize(clue.KeyMarker).Length >= 2 && Contains(query, Normalize(clue.KeyMarker))) score += 30;
            }
            foreach (var group in groups) if (Matches(query, group) && Matches(body, group)) score += 10;
            foreach (var alias in aliases) if (alias.Category == category && alias.Id == item.Id && Matches(query, alias.Keywords)) score += 20;
            if (score > 0) result.Add(new MemoryMatch(category, item, score));
        }

        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Trim();
        private static bool Matches(string text, MemoryKeywordGroup group)
        { foreach (var term in group.Terms) if (Contains(text, term)) return true; return false; }
        private static bool AsciiWord(char c) => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_';
        private static bool Contains(string text, string term)
        {
            if (string.IsNullOrEmpty(term)) return false;
            var start = 0;
            while (start <= text.Length - term.Length)
            {
                var index = text.IndexOf(term, start, StringComparison.Ordinal); if (index < 0) return false;
                var end = index + term.Length;
                if ((!AsciiWord(term[0]) || index == 0 || !AsciiWord(text[index - 1])) && (!AsciiWord(term[term.Length - 1]) || end == text.Length || !AsciiWord(text[end]))) return true;
                start = index + 1;
            }
            return false;
        }
    }
}
