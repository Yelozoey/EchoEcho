using System;
using System.Collections.Generic;
using EchoStation.Player;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.Tests.Player
{
    public class JournalStoreTests
    {
        private static JournalEntry Entry(string id="journal_note",ClueKind kind=ClueKind.Note,string brief="稳定摘要。")=>new JournalEntry(id,"日志标题","ORIGINAL_FULL_PARAGRAPH_ONLY：这是给玩家阅读的完整原文。",brief,kind);
        [Serializable]private class Export{public JournalSummary[] entries;}
        [Test]public void P05_A01_ThreeKindsPreserveAcquisitionOrderAndEmitOneEventEach()
        {
            var store=new JournalStore();var records=new List<JournalEntry>();var summaries=new List<JournalSummary>();store.Added+=records.Add;store.SummaryAdded+=summaries.Add;foreach(var entry in new[]{Entry("journal_scan",ClueKind.Scan),Entry("journal_note"),Entry("journal_data",ClueKind.Data)})Assert.That(store.Add(entry),Is.EqualTo(JournalAddResult.Added));Assert.That(store.Entries.Count,Is.EqualTo(3));Assert.That(store.Entries[0].Id,Is.EqualTo("journal_scan"));Assert.That(store.Entries[1].Kind,Is.EqualTo(ClueKind.Note));Assert.That(store.Entries[2].Kind,Is.EqualTo(ClueKind.Data));Assert.That(records.Count,Is.EqualTo(3));Assert.That(summaries.Count,Is.EqualTo(3));
        }
        [Test]public void P05_A02_RepeatedIdIsDeduplicatedAndConflictCannotOverwriteKnownText()
        {
            var store=new JournalStore();var count=0;store.Added+=_=>count++;Assert.That(store.Add(Entry()),Is.EqualTo(JournalAddResult.Added));Assert.That(store.Add(Entry()),Is.EqualTo(JournalAddResult.AlreadyKnown));Assert.That(store.Add(Entry(brief:"冲突摘要。")),Is.EqualTo(JournalAddResult.Conflict));Assert.That(count,Is.EqualTo(1));Assert.That(store.Entries.Count,Is.EqualTo(1));Assert.That(store.TryGet("journal_note",out var known),Is.True);Assert.That(known.Summary,Is.EqualTo("稳定摘要。"));
        }
        [Test]public void P05_A03_SummaryExportHasNoFullTextAndCopiesCannotMutateStore()
        {
            var store=new JournalStore();store.Add(Entry());var exported=store.ExportSummaries();var json=JsonUtility.ToJson(new Export{entries=exported});Assert.That(json,Does.Contain("稳定摘要"));Assert.That(json,Does.Not.Contain("ORIGINAL_FULL_PARAGRAPH_ONLY"));Assert.That(json,Does.Not.Contain("FullText"));exported[0].summary="外部改写";Assert.That(store.Entries[0].Summary,Is.EqualTo("稳定摘要。"));Assert.Throws<NotSupportedException>(()=>((IList<JournalEntry>)store.Entries).Add(Entry("journal_other")));
        }
        [Test]public void P05_A04_InvalidMetadataAndIdenticalOrOversizedSummaryAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>new JournalEntry("Bad ID","标题","原文","摘要",ClueKind.Note));Assert.Throws<ArgumentException>(()=>new JournalEntry("valid_id","","原文","摘要",ClueKind.Note));Assert.Throws<ArgumentException>(()=>new JournalEntry("valid_id","标题","原文","原文",ClueKind.Note));Assert.Throws<ArgumentException>(()=>new JournalEntry("valid_id","标题","原文",new string('摘',121),ClueKind.Note));Assert.Throws<ArgumentException>(()=>new JournalEntry("valid_id","标题",new string('文',4001),"摘要",ClueKind.Note));Assert.Throws<ArgumentException>(()=>new JournalEntry("valid_id","标题","原文","摘要",(ClueKind)9));Assert.Throws<ArgumentNullException>(()=>JournalEntry.FromClue(null));Assert.That(new JournalStore().Add(null),Is.EqualTo(JournalAddResult.Invalid));
        }
        [Test]public void P05_A05_CapacityIsBoundedAndKnownEntryCanStillBeReadAtCapacity()
        {
            var store=new JournalStore();for(var i=0;i<128;i++)Assert.That(store.Add(Entry("journal_item_"+i)),Is.EqualTo(JournalAddResult.Added));Assert.That(store.Add(Entry("journal_overflow")),Is.EqualTo(JournalAddResult.CapacityReached));Assert.That(store.Add(Entry("journal_item_0")),Is.EqualTo(JournalAddResult.AlreadyKnown));Assert.That(store.Entries.Count,Is.EqualTo(128));Assert.That(store.TryGet(null,out _),Is.False);Assert.That(store.TryGet("missing_id",out _),Is.False);
        }
    }
}
