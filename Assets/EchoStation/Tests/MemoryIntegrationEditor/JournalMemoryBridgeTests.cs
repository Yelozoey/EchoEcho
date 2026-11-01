using System;
using System.Reflection;
using EchoStation.Memory;
using EchoStation.Memory.Integration;
using EchoStation.Player;
using EchoStation.World;
using NUnit.Framework;

namespace EchoStation.Tests.MemoryIntegration
{
    public sealed class JournalMemoryBridgeTests
    {
        private static JournalEntry Entry(string id = "note_known", ClueKind kind = ClueKind.Note)
            => new JournalEntry(id, "已读记录", "ORIGINAL_FULL_TEXT_ONLY 不得传入记忆的原始全文。", "只供记忆使用的独立摘要。", kind);
        private static void Replace(JournalStore journal, params JournalEntry[] entries)
            => typeof(JournalStore).GetMethod("Replace", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(journal, new object[] { entries });

        [Test] public void M04_A01_OnlyKnownSummariesReachFactoryAndMemoryNeverReceivesFullText()
        {
            var journal = new JournalStore(); var memory = new MemoryStore(); var calls = 0;
            using (var bridge = new JournalMemoryBridge(journal, memory, summary =>
            { calls++; return new ClueMemory(summary.id, summary.title, summary.summary, (MemoryClueKind)(int)summary.kind, "7316", "A区校准架"); }))
            {
                Assert.That(calls, Is.Zero); Assert.That(bridge.TryGetKnownClue("note_known", out _), Is.False);
                journal.Add(Entry()); Assert.That(calls, Is.EqualTo(1)); Assert.That(bridge.TryGetKnownClue("note_known", out var clue), Is.True);
                Assert.That(clue.Password, Is.EqualTo("7316")); Assert.That(clue.Summary, Is.EqualTo(Entry().Summary));
                Assert.That(MemoryJson.Encode(memory.CaptureSnapshot()), Does.Not.Contain("ORIGINAL_FULL_TEXT_ONLY"));
            }
        }
        [Test] public void M04_A02_ReplayAndRepeatedCollectionDoNotDuplicateMemoryNotifications()
        {
            var journal = new JournalStore(); journal.Add(Entry()); journal.Add(Entry("scan_known", ClueKind.Scan));
            var memory = new MemoryStore(); var changes = 0; memory.Changed += _ => changes++;
            using (var bridge = new JournalMemoryBridge(journal, memory))
            {
                Assert.That(bridge.IsReady, Is.True); Assert.That(changes, Is.EqualTo(2));
                journal.Add(Entry()); Assert.That(bridge.Synchronize(memory), Is.True); Assert.That(changes, Is.EqualTo(2));
                Assert.That(memory.Clues[0].Id, Is.EqualTo("note_known")); Assert.That(memory.Clues[1].Kind, Is.EqualTo(MemoryClueKind.Scan));
            }
        }
        [Test] public void M04_A03_FactoryCannotSmuggleDifferentIdsOrRewriteKnownSummaries()
        {
            foreach (var mismatch in new[] { "id", "title", "summary", "kind", "null" })
            {
                var journal = new JournalStore(); journal.Add(Entry()); var memory = new MemoryStore();
                using (var bridge = new JournalMemoryBridge(journal, memory, s => mismatch == "null" ? null : new ClueMemory(mismatch == "id" ? "unknown_secret" : s.id, mismatch == "title" ? "改写标题" : s.title, mismatch == "summary" ? "泄露未知事实" : s.summary, mismatch == "kind" ? MemoryClueKind.Scan : (MemoryClueKind)(int)s.kind)))
                { Assert.That(bridge.IsReady, Is.False); Assert.That(memory.Clues, Is.Empty); Assert.That(bridge.LastError, Is.Not.Empty); }
            }
        }
        [Test] public void M04_A04_DisposeUnsubscribesAndIsIdempotent()
        {
            var journal = new JournalStore(); var memory = new MemoryStore(); var bridge = new JournalMemoryBridge(journal, memory);
            bridge.Dispose(); bridge.Dispose(); journal.Add(Entry()); Assert.That(memory.Clues, Is.Empty); Assert.That(bridge.TryGetKnownClue("note_known", out _), Is.False);
            Assert.Throws<ObjectDisposedException>(() => bridge.Synchronize(memory));
        }
        [Test] public void M04_A05_JournalReplaceBlocksOldKnowledgeUntilMatchingResynchronization()
        {
            var journal = new JournalStore(); var memory = new MemoryStore();
            using (var bridge = new JournalMemoryBridge(journal, memory))
            {
                journal.Add(Entry()); Assert.That(bridge.TryGetKnownClue("note_known", out _), Is.True);
                Replace(journal); Assert.That(bridge.IsReady, Is.False); Assert.That(bridge.TryGetKnownClue("note_known", out _), Is.False);
                Assert.That(bridge.Synchronize(memory), Is.False);
                var fresh = new MemoryStore(); Assert.That(bridge.Synchronize(fresh), Is.True);
                journal.Add(Entry("note_new")); Assert.That(fresh.Clues.Count, Is.EqualTo(1)); Assert.That(memory.Clues[0].Id, Is.EqualTo("note_known"));
            }
        }
        [Test] public void M04_A06_ExtraUnknownOrConflictingMemoryRejectsWithoutPartialReplay()
        {
            var journal = new JournalStore(); journal.Add(Entry()); journal.Add(Entry("note_second"));
            var memory = new MemoryStore(); memory.AddClue(new ClueMemory("note_second", "冲突标题", "冲突摘要", MemoryClueKind.Note));
            using (var bridge = new JournalMemoryBridge(journal, memory)) { Assert.That(bridge.IsReady, Is.False); Assert.That(memory.Clues.Count, Is.EqualTo(1)); Assert.That(memory.TryGetClue("note_known", out _), Is.False); }
            var unknown = new MemoryStore(); unknown.AddClue(new ClueMemory("uncollected_secret", "秘密", "尚未收集", MemoryClueKind.Note));
            using (var bridge = new JournalMemoryBridge(journal, unknown)) { Assert.That(bridge.IsReady, Is.False); Assert.That(unknown.Clues.Count, Is.EqualTo(1)); }
        }
    }
}
