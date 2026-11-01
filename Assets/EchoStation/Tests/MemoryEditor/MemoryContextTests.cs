using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.Memory
{
    public sealed class MemoryContextTests
    {
        private static MemoryContextBuilder Builder() => new MemoryContextBuilder(new MemorySearch(new[] { new MemoryKeywordGroup("供电", "电源") }));
        [Test] public void M06_T01_UnrelatedHistoryStaysEmptyEvenWhenCritical()
        {
            var store = new MemoryStore(); store.AddEvent("event_danger", 0, "供电危险", MemoryPriority.Critical);
            var context = Builder().Build(store.CaptureSnapshot(), "天气怎么样", 1000);
            Assert.That(context.Entries, Is.Empty); Assert.That(context.Text, Is.Empty); Assert.That(context.Utf8Bytes, Is.Zero);
        }
        [Test] public void M06_T02_OldLowPriorityEventsRankBelowRecentRelevantEvents()
        {
            var store = new MemoryStore(); store.AddEvent("a_old", 0, "供电记录", MemoryPriority.Low); store.AddEvent("z_recent", 1700, "供电记录", MemoryPriority.Low);
            var context = Builder().Build(store.CaptureSnapshot(), "电源", 1800);
            Assert.That(context.Entries[0].Match.Record.Id, Is.EqualTo("z_recent")); Assert.That(context.Entries[1].Score, Is.EqualTo(1));
        }
        [Test] public void M06_T03_CriticalRelevantFactsDoNotDecayWithTime()
        {
            var store = new MemoryStore(); store.AddEvent("critical_old", 0, "供电危险", MemoryPriority.Critical); store.AddEvent("recent_low", 10000, "供电记录", MemoryPriority.Low);
            var context = Builder().Build(store.CaptureSnapshot(), "电源", 10000);
            Assert.That(context.Entries[0].Match.Record.Id, Is.EqualTo("critical_old")); Assert.That(context.Entries[0].Score, Is.EqualTo(30));
        }
        [Test] public void M06_T04_ExactRequestedIdOutranksBroadCriticalAlias()
        {
            var store = new MemoryStore(); store.AddEvent("old_event", 0, "旧记录", MemoryPriority.Low); store.AddEvent("critical_event", 0, "其他记录", MemoryPriority.Critical);
            var builder = new MemoryContextBuilder(new MemorySearch(targetAliases: new[] { new MemoryAlias(MemoryCategory.Event, "critical_event", "old_event") }));
            var context = builder.Build(store.CaptureSnapshot(), "old_event", 10000);
            Assert.That(context.Entries[0].Match.Record.Id, Is.EqualTo("old_event")); Assert.That(context.Entries[0].Score, Is.EqualTo(85));
        }
        [Test] public void M06_T05_NeverSelectsMoreThanEightCompleteRelevantEntries()
        {
            var store = new MemoryStore(); for (var i = 0; i < 20; i++) store.AddEvent("event_" + i.ToString("D2"), 0, "供电记录");
            var context = Builder().Build(store.CaptureSnapshot(), "供电", 0);
            Assert.That(context.Entries.Count, Is.EqualTo(8)); Assert.That(context.Text, Does.Contain("event_07")); Assert.That(context.Text, Does.Not.Contain("event_08"));
        }
        [Test] public void M06_T06_ChineseUtf8BytesIncludingSeparatorsAreBoundedWithoutTruncation()
        {
            var store = new MemoryStore(); for (var i = 0; i < 4; i++) store.AddClue(new ClueMemory("clue_" + i, new string('题', 80), "供电" + new string('摘', 118), MemoryClueKind.Note));
            var context = Builder().Build(store.CaptureSnapshot(), "供电", 0);
            Assert.That(context.Utf8Bytes, Is.EqualTo(Encoding.UTF8.GetByteCount(context.Text))); Assert.That(context.Utf8Bytes, Is.LessThanOrEqualTo(1200));
            Assert.That(context.Entries.Count, Is.EqualTo(1)); Assert.That(context.Text, Does.EndWith(new string('摘', 118)));
            Assert.That(context.Utf8Bytes, Is.GreaterThan(context.Text.Length));
        }
        [Test] public void M06_T07_OversizedWholeFactIsSkippedAndSmallerLaterFactStillFits()
        {
            var store = new MemoryStore(); store.AddClue(new ClueMemory("large_fact", new string('题', 80), "供电" + new string('摘', 118), MemoryClueKind.Note, new string('密', 120), new string('标', 120)));
            store.AddClue(new ClueMemory("small_fact", "记录", "供电已稳定", MemoryClueKind.Note));
            var context = Builder().Build(store.CaptureSnapshot(), "供电", 0);
            Assert.That(context.OversizedEntriesSkipped, Is.EqualTo(1)); Assert.That(context.Entries.Single().Match.Record.Id, Is.EqualTo("small_fact")); Assert.That(context.Text, Does.Not.Contain("large_fact"));
        }
        [Test] public void M06_T08_ResultIsFixedReadonlyAndDeterministic()
        {
            var store = new MemoryStore(); store.AddEvent("event_first", 0, "供电记录"); var snapshot = store.CaptureSnapshot();
            var builder = Builder(); var a = builder.Build(snapshot, "电源", 1); store.AddEvent("event_new", 1, "供电记录"); var b = builder.Build(snapshot, "电源", 1);
            Assert.That(a.Text, Is.EqualTo(b.Text)); Assert.That(a.Entries.Count, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((IList<RankedMemory>)a.Entries).Clear());
        }
        [Test] public void M06_T09_InvalidClockRejectsAndFutureDatedEventHasNoNegativeAgeBonus()
        {
            var snapshot = new MemoryStore().CaptureSnapshot();
            foreach (var time in new[] { -1d, double.NaN, double.PositiveInfinity }) Assert.Throws<ArgumentException>(() => Builder().Build(snapshot, "供电", time));
            var store = new MemoryStore(); store.AddEvent("event_future", 1000, "供电记录", MemoryPriority.Normal);
            Assert.That(Builder().Build(store.CaptureSnapshot(), "供电", 0).Entries.Single().Score, Is.EqualTo(15));
        }
        [Test] public void M06_T10_ThreeCategoryTextContainsOnlyKnownFactsAndExplicitMetadata()
        {
            var store = new MemoryStore(); store.AddEvent("event_read", 0, "供电记录已读");
            store.AddClue(new ClueMemory("clue_code", "维护记录", "供电服务码", MemoryClueKind.Note, "7316", "A区校准架"));
            store.AddCharacter(new CharacterMemory("state_danger", CharacterMemoryKind.KnownDanger, "供电面板危险", "power_panel_01"));
            var context = Builder().Build(store.CaptureSnapshot(), "电源", 0);
            Assert.That(context.Entries.Count, Is.EqualTo(3)); Assert.That(context.Text, Does.Contain("[1] 供电记录已读")); Assert.That(context.Text, Does.Contain("密码：7316")); Assert.That(context.Text, Does.Contain("标记：A区校准架")); Assert.That(context.Text, Does.Contain("目标：power_panel_01"));
            Assert.That(context.Text, Does.Not.Contain("FullText")); Assert.That(context.Text, Does.Not.Contain("0428"));
        }
    }
}
