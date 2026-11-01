using System;
using System.Collections.Generic;
using System.Text;

namespace EchoStation.Memory
{
    public sealed class RankedMemory
    {
        public MemoryMatch Match { get; }
        public int Score { get; }
        public string Text { get; }
        internal RankedMemory(MemoryMatch match, int score, string text) { Match = match; Score = score; Text = text; }
    }

    public sealed class MemoryContextSlice
    {
        public IReadOnlyList<RankedMemory> Entries { get; }
        public string Text { get; }
        public int Utf8Bytes { get; }
        public int OversizedEntriesSkipped { get; }
        internal MemoryContextSlice(RankedMemory[] entries, string text, int bytes, int skipped)
        { Entries = Array.AsReadOnly(entries); Text = text; Utf8Bytes = bytes; OversizedEntriesSkipped = skipped; }
    }

    public sealed class MemoryContextBuilder
    {
        public const int MaximumEntries = 8;
        public const int MaximumUtf8Bytes = 1200;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly MemorySearch search;
        public MemoryContextBuilder(MemorySearch retrieval) { search = retrieval ?? throw new ArgumentNullException(nameof(retrieval)); }

        public MemoryContextSlice Build(MemorySnapshot known, string question, double elapsedGameSeconds)
        {
            MemoryLimits.Time(elapsedGameSeconds);
            var candidates = new List<RankedMemory>();
            foreach (var match in search.Search(known, question)) candidates.Add(new RankedMemory(match, Score(match, elapsedGameSeconds), Render(match.Record)));
            candidates.Sort((a, b) =>
            {
                var rank = b.Score.CompareTo(a.Score); if (rank != 0) return rank;
                var relevance = b.Match.Relevance.CompareTo(a.Match.Relevance); if (relevance != 0) return relevance;
                var category = a.Match.Category.CompareTo(b.Match.Category); return category != 0 ? category : string.CompareOrdinal(a.Match.Record.Id, b.Match.Record.Id);
            });
            var selected = new List<RankedMemory>(); var text = new StringBuilder(); var bytes = 0; var skipped = 0;
            foreach (var candidate in candidates)
            {
                if (selected.Count == MaximumEntries) break;
                var length = Utf8.GetByteCount(candidate.Text);
                if (length > MaximumUtf8Bytes) { skipped++; continue; }
                var separator = selected.Count == 0 ? 0 : 1;
                if (bytes + separator + length > MaximumUtf8Bytes) continue;
                if (separator != 0) text.Append('\n');
                text.Append(candidate.Text); bytes += separator + length; selected.Add(candidate);
            }
            return new MemoryContextSlice(selected.ToArray(), text.ToString(), bytes, skipped);
        }

        private static int Score(MemoryMatch match, double now)
        {
            var boost = 5; var penalty = 0;
            if (match.Record is EventMemory e)
            {
                var age = Math.Max(0, now - e.ElapsedGameSeconds);
                boost = e.Priority == MemoryPriority.Critical ? 20 : e.Priority == MemoryPriority.Normal ? 5 : 0;
                if (e.Priority == MemoryPriority.Low) penalty = (int)Math.Min(15, Math.Floor(age / 120));
                else if (e.Priority == MemoryPriority.Normal) penalty = (int)Math.Min(5, Math.Floor(age / 600));
            }
            else if (match.Record is ClueMemory c && (c.Password != null || c.KeyMarker != null) || match.Record is CharacterMemory state && state.Kind == CharacterMemoryKind.KnownDanger) boost = 20;
            return Math.Max(1, match.Relevance + boost - penalty);
        }

        private static string Render(MemoryRecord item)
        {
            if (item is EventMemory e) return "[事件 " + e.Id + "] " + e.DisplayText;
            if (item is ClueMemory c) return "[线索 " + c.Id + "] " + c.Title + "：" + c.Summary + (c.Password == null ? "" : "；密码：" + c.Password) + (c.KeyMarker == null ? "" : "；标记：" + c.KeyMarker);
            var state = (CharacterMemory)item;
            return "[角色 " + state.Id + "] " + state.Summary + (state.TargetId == null ? "" : "；目标：" + state.TargetId);
        }
    }
}
