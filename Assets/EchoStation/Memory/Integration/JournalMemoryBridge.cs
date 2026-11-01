using System;
using System.Collections.Generic;
using EchoStation.Player;

namespace EchoStation.Memory.Integration
{
    public sealed class JournalMemoryBridge : IDisposable
    {
        private readonly JournalStore journal;
        private readonly Func<JournalSummary, ClueMemory> knownFactFactory;
        private MemoryStore memory;
        private bool disposed;
        public bool IsReady { get; private set; }
        public string LastError { get; private set; } = "";

        public JournalMemoryBridge(JournalStore source, MemoryStore destination, Func<JournalSummary, ClueMemory> factFactory = null)
        {
            journal = source ?? throw new ArgumentNullException(nameof(source));
            knownFactFactory = factFactory;
            Synchronize(destination ?? throw new ArgumentNullException(nameof(destination)));
            journal.SummaryAdded += OnSummary;
            journal.Replaced += OnReplaced;
        }

        public bool TryGetKnownClue(string id, out ClueMemory clue)
        { clue = null; return IsReady && !disposed && memory.TryGetClue(id, out clue); }

        public bool Synchronize(MemoryStore replacement)
        {
            if (disposed) throw new ObjectDisposedException(nameof(JournalMemoryBridge));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            IsReady = false;
            var summaries = journal.ExportSummaries(); var facts = new List<ClueMemory>(summaries.Length);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach (var summary in summaries) { ids.Add(summary.id); facts.Add(Build(summary)); }
                foreach (var known in replacement.Clues)
                    if (!ids.Contains(known.Id)) return Fail("记忆包含当前日志没有的线索，等待完整状态同步。");
                foreach (var fact in facts)
                    if (replacement.TryGetClue(fact.Id, out var known) && !Same(known, fact)) return Fail("日志与已有线索摘要冲突。");
                if (facts.Count > MemoryLimits.MaximumClues) return Fail("线索记忆容量不足。");
            }
            catch (ArgumentException error) { return Fail(error.Message); }
            // Validated in full before adding any missing facts. No original text crosses this boundary.
            foreach (var fact in facts) replacement.AddClue(fact);
            memory = replacement; IsReady = true; LastError = ""; return true;
        }

        private ClueMemory Build(JournalSummary summary)
        {
            var id = summary.id; var title = summary.title; var brief = summary.summary; var kind = (MemoryClueKind)(int)summary.kind;
            var fact = knownFactFactory == null ? new ClueMemory(id, title, brief, kind) : knownFactFactory(summary);
            if (fact == null || fact.Id != id || fact.Title != title || fact.Summary != brief || fact.Kind != kind)
                throw new ArgumentException("Known-fact metadata cannot change the authorized journal summary.");
            return fact;
        }

        private void OnSummary(JournalSummary summary)
        {
            if (!IsReady || disposed) return;
            try
            {
                var result = memory.AddClue(Build(summary));
                if (result != MemoryWriteResult.Added && result != MemoryWriteResult.AlreadyKnown) Fail("线索记忆入库失败：" + result);
            }
            catch (ArgumentException error) { Fail(error.Message); }
        }

        private static bool Same(ClueMemory a, ClueMemory b)
            => a.Id == b.Id && a.Title == b.Title && a.Summary == b.Summary && a.Kind == b.Kind && a.Password == b.Password && a.KeyMarker == b.KeyMarker;
        private bool Fail(string message) { IsReady = false; LastError = message; return false; }
        private void OnReplaced() => Fail("日志已替换，等待完整记忆同步。");
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; IsReady = false;
            journal.SummaryAdded -= OnSummary; journal.Replaced -= OnReplaced;
        }
    }
}
