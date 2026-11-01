using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.Memory
{
    public sealed class MemorySearchTests
    {
        private static ClueMemory Clue(string id, string summary = "供电回路异常。") => new ClueMemory(id, "维护记录", summary, MemoryClueKind.Note);
        [Test] public void M05_T01_ChineseSynonymsRequireBothQuestionAndKnownRecordTopic()
        {
            var store = new MemoryStore(); store.AddClue(Clue("power_note")); store.AddClue(Clue("other_note", "气象数据正常。"));
            var search = new MemorySearch(new[] { new MemoryKeywordGroup("供电", "电源", "电路") });
            Assert.That(search.Search(store.CaptureSnapshot(), "电源在哪里异常？").Single().Record.Id, Is.EqualTo("power_note"));
        }
        [Test] public void M05_T02_ExactIdsCaseFoldAndAsciiBoundariesDoNotMatchLongerIds()
        {
            var store = new MemoryStore(); store.AddClue(Clue("power_panel_01")); var search = new MemorySearch();
            Assert.That(search.Search(store.CaptureSnapshot(), "请查看 POWER_PANEL_01。 ").Single().Relevance, Is.EqualTo(100));
            Assert.That(search.Search(store.CaptureSnapshot(), "power_panel_01_extra"), Is.Empty);
            Assert.That(search.Search(store.CaptureSnapshot(), "xpower_panel_01"), Is.Empty);
        }
        [Test] public void M05_T03_TargetAliasCannotRevealUnknownRecords()
        {
            var store = new MemoryStore(); var search = new MemorySearch(targetAliases: new[] { new MemoryAlias(MemoryCategory.Clue, "scan_mark_01", "储藏室可疑点"), new MemoryAlias(MemoryCategory.Clue, "secret_marker", "地下秘密") });
            Assert.That(search.Search(store.CaptureSnapshot(), "储藏室可疑点"), Is.Empty);
            store.AddClue(Clue("scan_mark_01", "A区校准架位置。"));
            Assert.That(search.Search(store.CaptureSnapshot(), "储藏室可疑点").Single().Record.Id, Is.EqualTo("scan_mark_01"));
            Assert.That(search.Search(store.CaptureSnapshot(), "地下秘密"), Is.Empty);
        }
        [Test] public void M05_T04_SynonymsWithinOneGroupDoNotMultiplyScore()
        {
            var store = new MemoryStore(); store.AddClue(Clue("power_note"));
            var search = new MemorySearch(new[] { new MemoryKeywordGroup("供电", "电源") });
            Assert.That(search.Search(store.CaptureSnapshot(), "电源和供电").Single().Relevance, Is.EqualTo(10));
        }
        [Test] public void M05_T05_TiedRankingIsDeterministicRegardlessOfAcquisitionOrder()
        {
            var store = new MemoryStore(); store.AddClue(Clue("z_note")); store.AddClue(Clue("a_note")); store.AddEvent("event_note", 0, "供电异常。");
            var search = new MemorySearch(new[] { new MemoryKeywordGroup("供电") });
            var expected = new[] { "event_note", "a_note", "z_note" };
            CollectionAssert.AreEqual(expected, search.Search(store.CaptureSnapshot(), "供电").Select(r => r.Record.Id));
            CollectionAssert.AreEqual(expected, search.Search(store.CaptureSnapshot(), "供电").Select(r => r.Record.Id));
        }
        [Test] public void M05_T06_InvalidInputAndMalformedCatalogRejectExplicitly()
        {
            var empty = new MemoryStore().CaptureSnapshot(); var search = new MemorySearch();
            Assert.That(search.Search(empty, null), Is.Empty); Assert.That(search.Search(empty, new string('问', 200)), Is.Empty);
            Assert.Throws<ArgumentException>(() => search.Search(empty, new string('问', 201))); Assert.Throws<ArgumentNullException>(() => search.Search(null, "问题"));
            Assert.Throws<ArgumentException>(() => new MemoryKeywordGroup("的")); Assert.Throws<ArgumentException>(() => new MemoryKeywordGroup("供电", "供电"));
            Assert.Throws<ArgumentException>(() => new MemoryAlias((MemoryCategory)9, "valid_id", "关键词"));
            Assert.Throws<ArgumentException>(() => new MemorySearch(new MemoryKeywordGroup[] { null }));
            Assert.Throws<ArgumentException>(() => new MemorySearch(targetAliases: new[] { new MemoryAlias(MemoryCategory.Clue, "same_id", "关键词"), new MemoryAlias(MemoryCategory.Clue, "same_id", "其他词") }));
        }
        [Test] public void M05_T07_ExternalCatalogArraysAndLaterWritesCannotMutateCapturedSearch()
        {
            var terms = new[] { "供电", "电源" }; var group = new MemoryKeywordGroup(terms); terms[0] = "其他词";
            var groups = new[] { group }; var search = new MemorySearch(groups); groups[0] = new MemoryKeywordGroup("其他词");
            var store = new MemoryStore(); store.AddClue(Clue("first_note")); var snapshot = store.CaptureSnapshot(); store.AddClue(Clue("second_note"));
            var result = search.Search(snapshot, "供电"); Assert.That(result.Single().Record.Id, Is.EqualTo("first_note"));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)group.Terms)[0] = "外部改写");
            Assert.Throws<NotSupportedException>(() => ((IList<MemoryMatch>)result).Clear());
        }
        [Test] public void M05_T08_AllThreeCategoriesAndKnownDangerTargetIdsAreSearchable()
        {
            var store = new MemoryStore(); store.AddEvent("vent_event", 1, "通风异常。"); store.AddClue(Clue("vent_note", "通风说明。"));
            store.AddCharacter(new CharacterMemory("known_danger", CharacterMemoryKind.KnownDanger, "通风设备危险。", "power_panel_01"));
            var search = new MemorySearch(new[] { new MemoryKeywordGroup("排风", "通风") });
            Assert.That(search.Search(store.CaptureSnapshot(), "排风").Count, Is.EqualTo(3));
            Assert.That(search.Search(store.CaptureSnapshot(), "power_panel_01").Single().Category, Is.EqualTo(MemoryCategory.Character));
        }
        [Test] public void M05_T09_UnrelatedCriticalEventsNeverForceHistoryIntoResults()
        {
            var store = new MemoryStore(); store.AddEvent("critical_event", 0, "供电危险。", MemoryPriority.Critical);
            var search = new MemorySearch(new[] { new MemoryKeywordGroup("供电", "电源") });
            Assert.That(search.Search(store.CaptureSnapshot(), "今天天气怎么样"), Is.Empty); Assert.That(search.Search(store.CaptureSnapshot(), "的"), Is.Empty);
        }
        [Test] public void M05_T10_ExplicitKnownPasswordsAndMarkersMatchWithoutOriginalText()
        {
            var store = new MemoryStore(); store.AddClue(new ClueMemory("known_code", "记录", "已知摘要", MemoryClueKind.Note, "7316", "A区校准架"));
            var search = new MemorySearch(); Assert.That(search.Search(store.CaptureSnapshot(), "7316是什么").Single().Record.Id, Is.EqualTo("known_code"));
            Assert.That(search.Search(store.CaptureSnapshot(), "A区校准架在哪里").Single().Record.Id, Is.EqualTo("known_code"));
            Assert.That(search.Search(store.CaptureSnapshot(), "未收集的0428"), Is.Empty);
            Assert.That(typeof(MemoryMatch).GetProperty("FullText"), Is.Null);
        }
    }
}
