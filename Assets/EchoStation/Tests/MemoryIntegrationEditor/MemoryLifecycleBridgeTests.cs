using System.Reflection;
using EchoStation.Memory;
using EchoStation.Memory.Integration;
using EchoStation.Player;
using EchoStation.World;
using NUnit.Framework;

namespace EchoStation.Tests.MemoryIntegration
{
    public sealed class MemoryLifecycleBridgeTests
    {
        private static JournalEntry Entry(string id) => new JournalEntry(id, "已读记录", "只给玩家的原始全文。", "独立摘要。", ClueKind.Note);
        [Test] public void M07_I01_JournalRollbackAndMatchingMemoryRestoreDoNotRediscoverOldClues()
        {
            var journal = new JournalStore(); var session = new MemorySession();
            using (var bridge = new JournalMemoryBridge(journal, session.Store))
            {
                journal.Add(Entry("note_first")); var saved = session.CaptureJson(); journal.Add(Entry("note_later"));
                typeof(JournalStore).GetMethod("Replace", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(journal, new object[] { new[] { Entry("note_first") } });
                Assert.That(bridge.IsReady, Is.False); var changes = 0; var replacements = 0;
                session.Replaced += () => { replacements++; session.Store.Changed += _ => changes++; Assert.That(bridge.Synchronize(session.Store), Is.True); };
                Assert.That(session.TryRestore(saved, out var error), Is.True, error);
                Assert.That(bridge.TryGetKnownClue("note_later", out _), Is.False); Assert.That(bridge.TryGetKnownClue("note_first", out _), Is.True);
                Assert.That(changes, Is.Zero); Assert.That(replacements, Is.EqualTo(1)); Assert.That(session.Store.Clues.Count, Is.EqualTo(1));
            }
        }
        [Test] public void M07_I02_PreparedJournalConsistencyCanRejectMemoryWithoutClosingOldKnowledge()
        {
            var journal = new JournalStore(); var session = new MemorySession();
            using (var bridge = new JournalMemoryBridge(journal, session.Store))
            {
                journal.Add(Entry("note_first")); var old = session.Store;
                Assert.That(session.TryRestore(new MemorySession().CaptureJson(), out _, snapshot => snapshot.Clues.Count == journal.Entries.Count), Is.False);
                Assert.That(session.Store, Is.SameAs(old)); Assert.That(bridge.TryGetKnownClue("note_first", out _), Is.True);
            }
        }
    }
}
