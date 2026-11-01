using System;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.Memory
{
    public sealed class MemorySessionTests
    {
        private static MemorySession Sample()
        {
            var session = new MemorySession(); session.Store.AddEvent("event_first", 10, "已读记录。");
            session.Store.AddClue(new ClueMemory("clue_note", "维护记录", "已知摘要", MemoryClueKind.Note, "7316"));
            session.Store.AddCharacter(new CharacterMemory("state_wait", CharacterMemoryKind.Agreement, "在入口等待。")); return session;
        }
        [Test] public void M07_T01_ValidRestoreReplacesWholeStoreAndEmitsOneReplacementOnly()
        {
            var source = Sample(); var target = new MemorySession(); var old = target.Store; var additions = 0; var replacements = 0;
            old.Changed += _ => additions++; target.Replaced += () => replacements++;
            Assert.That(target.TryRestore(source.CaptureJson(), out var error), Is.True, error);
            Assert.That(target.Version, Is.EqualTo(2)); Assert.That(replacements, Is.EqualTo(1)); Assert.That(additions, Is.Zero);
            Assert.That(target.CaptureJson(), Is.EqualTo(source.CaptureJson())); Assert.That(target.Store, Is.Not.SameAs(old));
        }
        [Test] public void M07_T02_InvalidAndFutureJsonCannotPartiallyChangeCurrentStateOrVersion()
        {
            var session = Sample(); var before = session.Store; var json = session.CaptureJson(); var notifications = 0; session.Replaced += () => notifications++;
            foreach (var bad in new[] { "broken", json.Replace("7316", "9999"), json.Replace("\"version\":1", "\"version\":2") })
            { Assert.That(session.TryRestore(bad, out _), Is.False); Assert.That(session.Store, Is.SameAs(before)); Assert.That(session.Version, Is.EqualTo(1)); Assert.That(session.CaptureJson(), Is.EqualTo(json)); }
            Assert.That(notifications, Is.Zero); Assert.That(session.LastRestoreStatus, Is.EqualTo(MemoryReadStatus.UnsupportedVersion));
        }
        [Test] public void M07_T03_ConsistencyPredicateRejectsBeforeCommitIncludingValidationErrors()
        {
            var session = Sample(); var old = session.Store; var input = new MemorySession().CaptureJson();
            Assert.That(session.TryRestore(input, out _, _ => false), Is.False); Assert.That(session.Store, Is.SameAs(old));
            Assert.That(session.TryRestore(input, out _, _ => throw new ArgumentException("canonical mismatch")), Is.False);
            Assert.That(session.Version, Is.EqualTo(1)); Assert.That(session.CaptureJson(), Does.Contain("7316"));
        }
        [Test] public void M07_T04_AdoptClonesCollectionsSoExternalReadResultCannotMutateLiveState()
        {
            var prepared = MemoryJson.Decode(Sample().CaptureJson()); var session = new MemorySession();
            Assert.That(session.TryAdopt(prepared, out _), Is.True); var snapshot = session.Store.CaptureSnapshot();
            prepared.Store.AddEvent("external_event", 20, "外部后续写入。"); prepared.Store.UpdateCharacter(new CharacterMemory("state_wait", CharacterMemoryKind.Agreement, "外部改写。"));
            Assert.That(session.Store.Events.Count, Is.EqualTo(1)); Assert.That(session.Store.Characters[0].Summary, Is.EqualTo("在入口等待。")); Assert.That(snapshot.Events.Count, Is.EqualTo(1));
        }
        [Test] public void M07_T05_ResetClearsAllThreeCategoriesAndStartsFreshEventNumbers()
        {
            var session = Sample(); var oldSnapshot = session.Store.CaptureSnapshot(); var replacements = 0; session.Replaced += () => replacements++;
            session.Reset(); Assert.That(session.Store.Events, Is.Empty); Assert.That(session.Store.Clues, Is.Empty); Assert.That(session.Store.Characters, Is.Empty);
            Assert.That(session.Version, Is.EqualTo(2)); Assert.That(replacements, Is.EqualTo(1));
            session.Store.AddEvent("event_new", 0, "新局"); Assert.That(session.Store.Events[0].Number, Is.EqualTo(1)); Assert.That(oldSnapshot.Clues[0].Password, Is.EqualTo("7316"));
        }
        [Test] public void M07_T06_BackupCanBeAdoptedButFailureAndMissingDataCannot()
        {
            var prepared = Sample().Store; var session = new MemorySession();
            Assert.That(session.TryAdopt(new MemoryReadResult(MemoryReadStatus.Backup, prepared), out _), Is.True); Assert.That(session.LastRestoreStatus, Is.EqualTo(MemoryReadStatus.Backup)); var old = session.Store; var version = session.Version;
            foreach (var read in new[] { null, new MemoryReadResult(MemoryReadStatus.Loaded), new MemoryReadResult(MemoryReadStatus.Invalid), new MemoryReadResult(MemoryReadStatus.Missing), new MemoryReadResult(MemoryReadStatus.IoError) })
            { Assert.That(session.TryAdopt(read, out _), Is.False); Assert.That(session.Store, Is.SameAs(old)); Assert.That(session.Version, Is.EqualTo(version)); }
        }
    }
}
