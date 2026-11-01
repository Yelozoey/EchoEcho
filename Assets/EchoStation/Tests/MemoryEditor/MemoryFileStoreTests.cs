using System;
using System.IO;
using EchoStation.Memory;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.Tests.Memory
{
    public sealed class MemoryFileStoreTests
    {
        private string testRoot, directory;
        private MemoryFileStore files;
        private static MemorySnapshot Snapshot(string id = "event_first")
        { var store = new MemoryStore(); store.AddEvent(id, 0, "中文记忆摘要。"); return store.CaptureSnapshot(); }
        [SetUp] public void SetUp()
        {
            testRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/EchoStation/S2-M03"));
            directory = Path.Combine(testRoot, Guid.NewGuid().ToString("N"));
            files = new MemoryFileStore(directory);
        }
        [TearDown] public void TearDown()
        {
            var resolved = Path.GetFullPath(directory);
            Assert.That(resolved.StartsWith(testRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(resolved).Length, Is.EqualTo(32));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
        private void Save(string id = "event_first") { Assert.That(files.TryWrite(Snapshot(id), out var error), Is.True, error); }
        [Test] public void M03_T01_MissingReadCreatesNothingAndFirstWriteRoundTripsOnDisk()
        {
            Assert.That(files.Read().Status, Is.EqualTo(MemoryReadStatus.Missing)); Assert.That(Directory.Exists(directory), Is.False);
            Save(); var read = new MemoryFileStore(directory).Read();
            Assert.That(read.Success, Is.True, read.Detail); Assert.That(read.Store.Events[0].Summary, Is.EqualTo("中文记忆摘要。"));
            Assert.That(File.Exists(files.PrimaryPath), Is.True); Assert.That(File.Exists(files.BackupPath), Is.False); Assert.That(File.Exists(files.TemporaryPath), Is.False);
        }
        [Test] public void M03_T02_SecondAtomicWriteKeepsExactlyPreviousValidVersion()
        {
            Save(); var first = File.ReadAllText(files.PrimaryPath); Save("event_second");
            Assert.That(files.Read().Store.Events[0].Id, Is.EqualTo("event_second")); Assert.That(File.ReadAllText(files.BackupPath), Is.EqualTo(first));
            Assert.That(MemoryJson.Decode(File.ReadAllText(files.BackupPath)).Store.Events[0].Id, Is.EqualTo("event_first"));
        }
        [Test] public void M03_T03_CorruptPrimaryFallsBackWithoutChangingFiles()
        {
            Save(); Save("event_second"); File.WriteAllText(files.PrimaryPath, "broken"); var backup = File.ReadAllBytes(files.BackupPath);
            var read = files.Read(); Assert.That(read.Status, Is.EqualTo(MemoryReadStatus.Backup)); Assert.That(read.Store.Events[0].Id, Is.EqualTo("event_first"));
            Assert.That(File.ReadAllText(files.PrimaryPath), Is.EqualTo("broken")); CollectionAssert.AreEqual(backup, File.ReadAllBytes(files.BackupPath));
        }
        [Test] public void M03_T04_RecoverySavePreservesRejectedPrimaryAndGoodBackup()
        {
            Save(); Save("event_second"); var backup = File.ReadAllText(files.BackupPath); File.WriteAllText(files.PrimaryPath, "broken"); Save("event_third");
            Assert.That(File.ReadAllText(files.BackupPath), Is.EqualTo(backup));
            var rejected = Directory.GetFiles(directory, "memory.json.rejected-*"); Assert.That(rejected.Length, Is.EqualTo(1)); Assert.That(File.ReadAllText(rejected[0]), Is.EqualTo("broken"));
            Assert.That(files.Read().Store.Events[0].Id, Is.EqualTo("event_third"));
        }
        [Test] public void M03_T05_DoubleCorruptionCannotBeOverwritten()
        {
            Save(); File.WriteAllText(files.PrimaryPath, "bad primary"); File.WriteAllText(files.BackupPath, "bad backup");
            Assert.That(files.Read().Status, Is.EqualTo(MemoryReadStatus.Invalid)); Assert.That(files.TryWrite(Snapshot(), out _), Is.False);
            Assert.That(File.ReadAllText(files.PrimaryPath), Is.EqualTo("bad primary")); Assert.That(File.ReadAllText(files.BackupPath), Is.EqualTo("bad backup"));
        }
        [Test] public void M03_T06_FuturePrimaryNeverFallsBackOrGetsOverwritten()
        {
            Save(); Save("event_second"); var future = File.ReadAllText(files.PrimaryPath).Replace("\"version\":1", "\"version\":2,\"future\":true"); File.WriteAllText(files.PrimaryPath, future);
            Assert.That(files.Read().Status, Is.EqualTo(MemoryReadStatus.UnsupportedVersion)); Assert.That(files.TryWrite(Snapshot(), out _), Is.False);
            Assert.That(File.ReadAllText(files.PrimaryPath), Is.EqualTo(future));
        }
        [Test] public void M03_T07_MissingPrimaryStillProtectsFutureOrCorruptBackup()
        {
            Save(); var valid = File.ReadAllText(files.PrimaryPath); File.Delete(files.PrimaryPath);
            File.WriteAllText(files.BackupPath, valid.Replace("\"version\":1", "\"version\":2"));
            Assert.That(files.Read().Status, Is.EqualTo(MemoryReadStatus.UnsupportedVersion)); Assert.That(files.TryWrite(Snapshot(), out _), Is.False); Assert.That(File.Exists(files.PrimaryPath), Is.False);
            File.WriteAllText(files.BackupPath, "bad"); Assert.That(files.TryWrite(Snapshot(), out _), Is.False); Assert.That(File.ReadAllText(files.BackupPath), Is.EqualTo("bad"));
            File.WriteAllText(files.BackupPath, valid); Assert.That(files.Read().Status, Is.EqualTo(MemoryReadStatus.Backup)); Save("event_new"); Assert.That(File.ReadAllText(files.BackupPath), Is.EqualTo(valid));
        }
        [Test] public void M03_T08_LockedPrimaryFailsCommitWithoutDestroyingOldData()
        {
            Save(); var before = File.ReadAllBytes(files.PrimaryPath);
            using (var held = new FileStream(files.PrimaryPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            { Assert.That(files.TryWrite(Snapshot("event_new"), out var error), Is.False); Assert.That(error, Is.Not.Empty); }
            CollectionAssert.AreEqual(before, File.ReadAllBytes(files.PrimaryPath)); Assert.That(File.Exists(files.TemporaryPath), Is.False); Save("event_new");
        }
        [Test] public void M03_T09_ResidualTemporaryFileIsNotACommittedSave()
        {
            Directory.CreateDirectory(directory); File.WriteAllText(files.TemporaryPath, MemoryJson.Encode(Snapshot("event_uncommitted")));
            Assert.That(files.Read().Status, Is.EqualTo(MemoryReadStatus.Missing)); Save(); Assert.That(File.Exists(files.TemporaryPath), Is.False);
            Assert.That(files.Read().Store.Events[0].Id, Is.EqualTo("event_first"));
        }
        [Test] public void M03_T10_InvalidUtf8AndOversizedFileArePreserved()
        {
            Directory.CreateDirectory(directory); var invalid = new byte[] { 0xc3, 0x28 }; File.WriteAllBytes(files.PrimaryPath, invalid);
            Assert.That(files.Read().Status, Is.EqualTo(MemoryReadStatus.Invalid)); Assert.That(files.TryWrite(Snapshot(), out _), Is.False); CollectionAssert.AreEqual(invalid, File.ReadAllBytes(files.PrimaryPath));
            using (var stream = File.Create(files.PrimaryPath)) stream.SetLength(MemoryJson.MaximumBytes + 1L);
            Assert.That(files.Read().Status, Is.EqualTo(MemoryReadStatus.Invalid)); Assert.That(files.TryWrite(Snapshot(), out _), Is.False); Assert.That(new FileInfo(files.PrimaryPath).Length, Is.EqualTo(MemoryJson.MaximumBytes + 1L));
        }
        [Test] public void M03_T11_InvalidRequestCannotChangeValidFiles()
        {
            Save(); var before = File.ReadAllText(files.PrimaryPath); Assert.That(files.TryWrite(null, out _), Is.False); Assert.That(File.ReadAllText(files.PrimaryPath), Is.EqualTo(before));
            Assert.Throws<ArgumentException>(() => new MemoryFileStore(" "));
        }
        [Test] public void M03_T12_PathOccupiedByDirectoryReturnsIoErrorAndKeepsDirectory()
        {
            Directory.CreateDirectory(files.PrimaryPath); Assert.That(files.Read().Status, Is.EqualTo(MemoryReadStatus.IoError));
            Assert.That(files.TryWrite(Snapshot(), out _), Is.False); Assert.That(Directory.Exists(files.PrimaryPath), Is.True); Assert.That(File.Exists(files.TemporaryPath), Is.False);
        }
    }
}
