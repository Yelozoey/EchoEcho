using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EchoStation.Memory;
using EchoStation.World;
using NUnit.Framework;

namespace EchoStation.Tests.Memory
{
    public sealed class MemoryContractTests
    {
        private static ClueMemory Clue(string id = "clue_note", string summary = "已发现的简短摘要。")
            => new ClueMemory(id, "维护记录", summary, MemoryClueKind.Note);
        private static CharacterMemory Character(string id = "state_danger", string summary = "供电面板存在危险。")
            => new CharacterMemory(id, CharacterMemoryKind.KnownDanger, summary, "power_panel_01");

        [Test] public void M01_T01_ThreeCategoriesAndAllSubtypesPreserveDataAndOrder()
        {
            var store = new MemoryStore();
            Assert.That(store.AddEvent("event_enter", 0, "进入观测小屋。"), Is.EqualTo(MemoryWriteResult.Added));
            foreach (MemoryClueKind kind in Enum.GetValues(typeof(MemoryClueKind)))
                Assert.That(store.AddClue(new ClueMemory("clue_" + (int)kind, "标题", "摘要", kind)), Is.EqualTo(MemoryWriteResult.Added));
            foreach (CharacterMemoryKind kind in Enum.GetValues(typeof(CharacterMemoryKind)))
                Assert.That(store.AddCharacter(new CharacterMemory("state_" + (int)kind, kind, "角色记忆")), Is.EqualTo(MemoryWriteResult.Added));
            CollectionAssert.AreEqual(new[] { MemoryClueKind.Note, MemoryClueKind.Data, MemoryClueKind.Scan }, store.Clues.Select(c => c.Kind));
            CollectionAssert.AreEqual(new[] { CharacterMemoryKind.KnownDanger, CharacterMemoryKind.Agreement, CharacterMemoryKind.PlayerKnowledge }, store.Characters.Select(c => c.Kind));
            Assert.That(store.Events.Single().ElapsedGameSeconds, Is.Zero);
        }

        [Test] public void M01_T02_EventNumbersAreStableAndDuplicatesDoNotConsumeNumbers()
        {
            var store = new MemoryStore();
            store.AddEvent("event_a", 0, "进入小屋。");
            store.AddEvent("event_b", 12.5, "阅读维护记录。", MemoryPriority.Critical);
            Assert.That(store.AddEvent("event_a", 0, "进入小屋。"), Is.EqualTo(MemoryWriteResult.AlreadyKnown));
            Assert.That(store.AddEvent("event_a", 99, "进入小屋。"), Is.EqualTo(MemoryWriteResult.Conflict));
            store.AddEvent("event_c", 13, "完成扫描。", MemoryPriority.Low);
            CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, store.Events.Select(e => e.Number));
            Assert.That(store.Events[1].DisplayText, Is.EqualTo("[2] 阅读维护记录。"));
        }

        [Test] public void M01_T03_IdGrammarMatchesWorldRegistryIncludingBoundaries()
        {
            var ids = new[] { null, "", " ", "Bad_id", "1bad", "a-b", "a/b", "a\n", "ä", new string('a', 65), "a", "target_01", "a" + new string('_', 63) };
            foreach (var id in ids)
            {
                var expected = TargetRegistry.IsValidId(id);
                Assert.That(MemoryLimits.IsValidId(id), Is.EqualTo(expected), id);
                var store = new MemoryStore();
                Assert.That(store.AddEvent(id, 0, "摘要"), Is.EqualTo(expected ? MemoryWriteResult.Added : MemoryWriteResult.Invalid));
                if (!expected)
                {
                    Assert.Throws<ArgumentException>(() => Clue(id));
                    Assert.Throws<ArgumentException>(() => Character(id));
                    Assert.That(store.Events.Count, Is.Zero);
                }
            }
        }

        [Test] public void M01_T04_SummaryAndTitleLimitsAreEnforcedWithoutTruncation()
        {
            foreach (var text in new[] { "摘", new string('摘', 120) })
            {
                var store = new MemoryStore();
                Assert.That(store.AddEvent("event_valid", 0, text), Is.EqualTo(MemoryWriteResult.Added));
                Assert.That(Clue(summary: text).Summary, Is.EqualTo(text));
                Assert.That(Character(summary: text).Summary, Is.EqualTo(text));
            }
            foreach (var text in new[] { null, "", " \t", new string('摘', 121) })
            {
                Assert.That(new MemoryStore().AddEvent("event_invalid", 0, text), Is.EqualTo(MemoryWriteResult.Invalid));
                Assert.Throws<ArgumentException>(() => Clue(summary: text));
                Assert.Throws<ArgumentException>(() => Character(summary: text));
            }
            foreach (var length in new[] { 1, 80 }) Assert.That(new ClueMemory("clue_valid", new string('题', length), "摘要", MemoryClueKind.Note).Title.Length, Is.EqualTo(length));
            foreach (var text in new[] { null, "", " ", new string('题', 81) }) Assert.Throws<ArgumentException>(() => new ClueMemory("clue_invalid", text, "摘要", MemoryClueKind.Note));
        }

        [Test] public void M01_T05_InvalidTimesAndEnumsAreRejectedForEveryCategory()
        {
            foreach (var seconds in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                Assert.That(new MemoryStore().AddEvent("event_invalid", seconds, "摘要"), Is.EqualTo(MemoryWriteResult.Invalid));
            foreach (var value in new[] { -1, 3, int.MaxValue })
            {
                Assert.That(new MemoryStore().AddEvent("event_invalid", 0, "摘要", (MemoryPriority)value), Is.EqualTo(MemoryWriteResult.Invalid));
                Assert.Throws<ArgumentException>(() => new ClueMemory("clue_invalid", "标题", "摘要", (MemoryClueKind)value));
                Assert.Throws<ArgumentException>(() => new CharacterMemory("state_invalid", (CharacterMemoryKind)value, "摘要"));
            }
            foreach (MemoryPriority priority in Enum.GetValues(typeof(MemoryPriority)))
                Assert.That(new MemoryStore().AddEvent("event_valid", 0, "摘要", priority), Is.EqualTo(MemoryWriteResult.Added));
        }

        [Test] public void M01_T06_IdenticalRecordsAreIdempotentAndNotifyOnce()
        {
            var store = new MemoryStore(); var changes = new List<MemoryChange>(); store.Changed += changes.Add;
            for (var i = 0; i < 3; i++)
            {
                var expected = i == 0 ? MemoryWriteResult.Added : MemoryWriteResult.AlreadyKnown;
                Assert.That(store.AddEvent("event_known", 10, "已知事件"), Is.EqualTo(expected));
                Assert.That(store.AddClue(Clue()), Is.EqualTo(expected));
                Assert.That(store.AddCharacter(Character()), Is.EqualTo(expected));
            }
            CollectionAssert.AreEqual(new[] { MemoryCategory.Event, MemoryCategory.Clue, MemoryCategory.Character }, changes.Select(c => c.Category));
            Assert.That(changes.All(c => c.Result == MemoryWriteResult.Added), Is.True);
        }

        [Test] public void M01_T07_ConflictsCannotOverwriteAnyExistingField()
        {
            var store = new MemoryStore(); store.AddEvent("event_known", 1, "原摘要"); store.AddClue(Clue()); store.AddCharacter(Character());
            Assert.That(store.AddEvent("event_known", 1, "不同摘要"), Is.EqualTo(MemoryWriteResult.Conflict));
            Assert.That(store.AddEvent("event_known", 2, "原摘要"), Is.EqualTo(MemoryWriteResult.Conflict));
            Assert.That(store.AddEvent("event_known", 1, "原摘要", MemoryPriority.Critical), Is.EqualTo(MemoryWriteResult.Conflict));
            foreach (var clue in new[] { Clue(summary: "不同摘要"), new ClueMemory("clue_note", "不同标题", Clue().Summary, MemoryClueKind.Note), new ClueMemory("clue_note", Clue().Title, Clue().Summary, MemoryClueKind.Data), new ClueMemory("clue_note", Clue().Title, Clue().Summary, MemoryClueKind.Note, "1234"), new ClueMemory("clue_note", Clue().Title, Clue().Summary, MemoryClueKind.Note, keyMarker: "新标记") })
                Assert.That(store.AddClue(clue), Is.EqualTo(MemoryWriteResult.Conflict));
            foreach (var state in new[] { Character(summary: "不同摘要"), new CharacterMemory("state_danger", CharacterMemoryKind.Agreement, Character().Summary, "power_panel_01"), new CharacterMemory("state_danger", CharacterMemoryKind.KnownDanger, Character().Summary, "other_panel") })
                Assert.That(store.AddCharacter(state), Is.EqualTo(MemoryWriteResult.Conflict));
            Assert.That(store.Events.Single().Summary, Is.EqualTo("原摘要"));
            Assert.That(store.Clues.Single().Password, Is.Null);
            Assert.That(store.Characters.Single().TargetId, Is.EqualTo("power_panel_01"));
        }

        [Test] public void M01_T08_AllCapacitiesRejectOverflowWithoutEviction()
        {
            var store = new MemoryStore();
            for (var i = 0; i < 512; i++) Assert.That(store.AddEvent("event_" + i, i, "事件摘要"), Is.EqualTo(MemoryWriteResult.Added));
            for (var i = 0; i < 128; i++) Assert.That(store.AddClue(Clue("clue_" + i)), Is.EqualTo(MemoryWriteResult.Added));
            for (var i = 0; i < 64; i++) Assert.That(store.AddCharacter(Character("state_" + i)), Is.EqualTo(MemoryWriteResult.Added));
            Assert.That(store.AddEvent("event_overflow", 999, "额外事件"), Is.EqualTo(MemoryWriteResult.CapacityReached));
            Assert.That(store.AddClue(Clue("clue_overflow")), Is.EqualTo(MemoryWriteResult.CapacityReached));
            Assert.That(store.AddCharacter(Character("state_overflow")), Is.EqualTo(MemoryWriteResult.CapacityReached));
            Assert.That(store.AddEvent("event_0", 0, "事件摘要"), Is.EqualTo(MemoryWriteResult.AlreadyKnown));
            Assert.That(store.AddClue(Clue("clue_0")), Is.EqualTo(MemoryWriteResult.AlreadyKnown));
            Assert.That(store.AddCharacter(Character("state_0")), Is.EqualTo(MemoryWriteResult.AlreadyKnown));
            CollectionAssert.AreEqual(Enumerable.Range(1, 512).Select(i => (long)i), store.Events.Select(e => e.Number));
            Assert.That(store.Clues.Count, Is.EqualTo(128)); Assert.That(store.Characters.Count, Is.EqualTo(64));
        }

        [Test] public void M01_T09_ViewsRejectMutationAndSnapshotsDoNotFollowLaterWrites()
        {
            var store = new MemoryStore(); store.AddEvent("event_a", 0, "事件"); store.AddClue(Clue()); store.AddCharacter(Character());
            var snapshot = store.CaptureSnapshot();
            Assert.Throws<NotSupportedException>(() => ((IList<EventMemory>)store.Events).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<ClueMemory>)snapshot.Clues)[0] = Clue("replacement"));
            Assert.Throws<NotSupportedException>(() => ((IList<CharacterMemory>)snapshot.Characters).Clear());
            store.AddEvent("event_b", 1, "新事件"); store.AddClue(Clue("clue_new")); store.UpdateCharacter(Character(summary: "更新后的危险事实"));
            Assert.That(snapshot.Events.Count, Is.EqualTo(1)); Assert.That(snapshot.Clues.Count, Is.EqualTo(1));
            Assert.That(snapshot.Characters[0].Summary, Is.EqualTo("供电面板存在危险。"));
            Assert.That(store.Characters[0].Summary, Is.EqualTo("更新后的危险事实"));
        }

        [Test] public void M01_T10_CharacterUpdateIsExplicitPreservesOrderAndNotifiesExactlyOnce()
        {
            var store = new MemoryStore(); store.AddCharacter(Character()); store.AddCharacter(Character("state_other"));
            var changes = new List<MemoryChange>(); store.Changed += changes.Add;
            var replacement = new CharacterMemory("state_danger", CharacterMemoryKind.KnownDanger, "已确认新的安全距离。", "power_panel_01");
            Assert.That(store.AddCharacter(replacement), Is.EqualTo(MemoryWriteResult.Conflict));
            Assert.That(store.UpdateCharacter(replacement), Is.EqualTo(MemoryWriteResult.Updated));
            Assert.That(store.UpdateCharacter(replacement), Is.EqualTo(MemoryWriteResult.AlreadyKnown));
            Assert.That(store.UpdateCharacter(Character("state_missing")), Is.EqualTo(MemoryWriteResult.NotFound));
            Assert.That(store.UpdateCharacter(null), Is.EqualTo(MemoryWriteResult.Invalid));
            Assert.That(store.Characters.Select(c => c.Id), Is.EqualTo(new[] { "state_danger", "state_other" }));
            Assert.That(changes.Single().Result, Is.EqualTo(MemoryWriteResult.Updated));
            Assert.That(changes[0].Id, Is.EqualTo("state_danger"));
            Assert.That(store.TryGetCharacter("state_danger", out var current), Is.True); Assert.That(current, Is.SameAs(replacement));
        }

        [Test] public void M01_T11_OptionalFactsAreExplicitBoundedAndDoNotInferPasswords()
        {
            var ordinary = Clue(summary: "这里写着1234，但没有授权的密码元数据。");
            Assert.That(ordinary.Password, Is.Null); Assert.That(ordinary.KeyMarker, Is.Null);
            var known = new ClueMemory("clue_known", "已读笔记", "已知密码与标记", MemoryClueKind.Scan, "1234", "A区支路");
            Assert.That(known.Password, Is.EqualTo("1234")); Assert.That(known.KeyMarker, Is.EqualTo("A区支路"));
            Assert.That(new ClueMemory("clue_empty", "标题", "摘要", MemoryClueKind.Note, "", "").Password, Is.Null);
            foreach (var value in new[] { " ", new string('值', 121) })
            {
                Assert.Throws<ArgumentException>(() => new ClueMemory("clue_bad", "标题", "摘要", MemoryClueKind.Note, value));
                Assert.Throws<ArgumentException>(() => new ClueMemory("clue_bad", "标题", "摘要", MemoryClueKind.Note, keyMarker: value));
            }
            Assert.That(new ClueMemory("clue_limit", "标题", "摘要", MemoryClueKind.Note, new string('1', 120)).Password.Length, Is.EqualTo(120));
            Assert.That(new CharacterMemory("state_no_target", CharacterMemoryKind.PlayerKnowledge, "已知事实", "").TargetId, Is.Null);
            Assert.Throws<ArgumentException>(() => new CharacterMemory("state_bad_target", CharacterMemoryKind.KnownDanger, "摘要", "Bad ID"));
        }

        [Test] public void M01_T12_AssemblyHasNoEngineDependencyOrFullTextAndRecordsAreImmutable()
        {
            var assembly = typeof(MemoryStore).Assembly;
            Assert.That(assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Unity", StringComparison.Ordinal)), Is.False);
            foreach (var type in new[] { typeof(EventMemory), typeof(ClueMemory), typeof(CharacterMemory) })
            {
                Assert.That(type.IsSealed, Is.True);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
                foreach (var property in type.GetProperties())
                {
                    Assert.That(property.CanWrite, Is.False, property.Name);
                    Assert.That(property.Name.IndexOf("FullText", StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1));
                    Assert.That(property.PropertyType.IsPrimitive || property.PropertyType.IsEnum || property.PropertyType == typeof(string), Is.True, property.Name);
                }
            }
            Assert.That(Clue().Summary, Does.Not.Contain("ORIGINAL_FULL_PARAGRAPH_ONLY"));
        }

        [Test] public void M01_T13_SeparateStoresHaveIndependentNumbersAndLookups()
        {
            var old = new MemoryStore(); old.AddEvent("event_old", 1, "旧事件"); old.AddClue(Clue()); old.AddCharacter(Character());
            var snapshot = old.CaptureSnapshot(); var fresh = new MemoryStore();
            fresh.AddEvent("event_new", 0, "新事件");
            Assert.That(fresh.Events.Single().Number, Is.EqualTo(1)); Assert.That(fresh.Clues, Is.Empty); Assert.That(fresh.Characters, Is.Empty);
            Assert.That(snapshot.Events.Single().Id, Is.EqualTo("event_old"));
            Assert.That(old.TryGetEvent("event_old", out var entry), Is.True); Assert.That(entry, Is.SameAs(old.Events[0]));
            Assert.That(old.TryGetClue("clue_note", out _), Is.True); Assert.That(old.TryGetCharacter("state_danger", out _), Is.True);
            Assert.That(fresh.TryGetEvent("event_old", out _), Is.False); Assert.That(old.TryGetEvent(null, out _), Is.False);
            Assert.That(old.TryGetClue(null, out _), Is.False); Assert.That(old.TryGetCharacter("missing_state", out _), Is.False);
        }

        [Test] public void M01_T14_RejectedWritesCannotNotifyOrPartiallyCommit()
        {
            var store = new MemoryStore(); store.AddEvent("event_known", 0, "原事件"); store.AddClue(Clue()); store.AddCharacter(Character());
            for (var i = 1; i < 64; i++) store.AddCharacter(Character("state_" + i));
            var changes = 0; store.Changed += _ => changes++;
            Assert.That(store.AddEvent("Bad", 0, "错误事件"), Is.EqualTo(MemoryWriteResult.Invalid));
            Assert.That(store.AddEvent("event_known", 0, "冲突"), Is.EqualTo(MemoryWriteResult.Conflict));
            Assert.That(store.AddClue(null), Is.EqualTo(MemoryWriteResult.Invalid)); Assert.That(store.AddCharacter(null), Is.EqualTo(MemoryWriteResult.Invalid));
            Assert.That(store.AddClue(Clue(summary: "冲突")), Is.EqualTo(MemoryWriteResult.Conflict));
            Assert.That(store.AddCharacter(Character("state_extra")), Is.EqualTo(MemoryWriteResult.CapacityReached));
            Assert.That(changes, Is.Zero); Assert.That(store.Events.Single().Summary, Is.EqualTo("原事件")); Assert.That(store.Clues.Single().Summary, Is.EqualTo(Clue().Summary));
            Assert.That(store.Characters.Count, Is.EqualTo(64));
        }
    }
}
