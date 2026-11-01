using System;
using System.IO;
using EchoStation.Player;
using NUnit.Framework;

namespace EchoStation.Tests.Player
{
    public class LocalSaveStoreTests
    {
        private string directory;private LocalSaveStore store;
        [SetUp]public void Before(){directory=Path.GetFullPath(Path.Combine("Library","EchoStation","SaveTests",Guid.NewGuid().ToString("N")));store=new LocalSaveStore(directory);}
        [TearDown]public void After(){var allowed=Path.GetFullPath(Path.Combine("Library","EchoStation","SaveTests"))+Path.DirectorySeparatorChar;Assert.That(directory.StartsWith(allowed,StringComparison.OrdinalIgnoreCase),Is.True);if(Directory.Exists(directory))Directory.Delete(directory,true);}
        [Test]public void P07_B01_MissingFirstWriteAndSecondAtomicReplacementKeepPreviousBackup()
        {
            Assert.That(store.Read().Status,Is.EqualTo(SaveStatus.Missing));var a=SaveCodecTests.Fixture();Assert.That(store.TryWrite(a,out var error),Is.True,error);Assert.That(File.Exists(store.BackupPath),Is.False);var old=File.ReadAllText(store.PrimaryPath);a.playerYaw=210;Assert.That(store.TryWrite(a,out error),Is.True,error);Assert.That(store.Read().Data.playerYaw,Is.EqualTo(210));Assert.That(File.ReadAllText(store.BackupPath),Is.EqualTo(old));Assert.That(File.Exists(store.TemporaryPath),Is.False);
        }
        [Test]public void P07_B02_CorruptPrimaryFallsBackAndSavePreservesRejectedBytesAndGoodBackup()
        {
            var a=SaveCodecTests.Fixture();Assert.That(store.TryWrite(a,out _),Is.True);a.playerYaw=220;Assert.That(store.TryWrite(a,out _),Is.True);var backup=File.ReadAllText(store.BackupPath);File.WriteAllText(store.PrimaryPath,"{broken original");var recovered=store.Read();Assert.That(recovered.Status,Is.EqualTo(SaveStatus.Backup));Assert.That(recovered.Data.playerYaw,Is.EqualTo(120));a.playerYaw=310;Assert.That(store.TryWrite(a,out var error),Is.True,error);Assert.That(store.Read().Data.playerYaw,Is.EqualTo(310));Assert.That(File.ReadAllText(store.BackupPath),Is.EqualTo(backup));var rejected=Directory.GetFiles(directory,"*.rejected-*");Assert.That(rejected.Length,Is.EqualTo(1));Assert.That(File.ReadAllText(rejected[0]),Is.EqualTo("{broken original"));
        }
        [Test]public void P07_B03_DoubleCorruptionRefusesOverwriteAndKeepsBothOriginals()
        {
            Directory.CreateDirectory(directory);File.WriteAllText(store.PrimaryPath,"original A");File.WriteAllText(store.BackupPath,"original B");Assert.That(store.Read().Status,Is.EqualTo(SaveStatus.Invalid));Assert.That(store.TryWrite(SaveCodecTests.Fixture(),out _),Is.False);Assert.That(File.ReadAllText(store.PrimaryPath),Is.EqualTo("original A"));Assert.That(File.ReadAllText(store.BackupPath),Is.EqualTo("original B"));
        }
        [Test]public void P07_B04_FutureVersionCannotFallBackOrOverwrite()
        {
            var data=SaveCodecTests.Fixture();Assert.That(store.TryWrite(data,out _),Is.True);Assert.That(store.TryWrite(data,out _),Is.True);var future=File.ReadAllText(store.PrimaryPath).Replace("\"version\": 1","\"version\": 99");File.WriteAllText(store.PrimaryPath,future);var backup=File.ReadAllText(store.BackupPath);Assert.That(store.Read().Status,Is.EqualTo(SaveStatus.UnsupportedVersion));Assert.That(store.TryWrite(data,out _),Is.False);Assert.That(File.ReadAllText(store.PrimaryPath),Is.EqualTo(future));Assert.That(File.ReadAllText(store.BackupPath),Is.EqualTo(backup));
        }
        [Test]public void P07_B05_LockedPrimaryMakesCommitFailWithoutChangingPreviousState()
        {
            var data=SaveCodecTests.Fixture();Assert.That(store.TryWrite(data,out _),Is.True);var original=File.ReadAllBytes(store.PrimaryPath);using(var locked=new FileStream(store.PrimaryPath,FileMode.Open,FileAccess.Read,FileShare.Read)){data.playerYaw=222;Assert.That(store.TryWrite(data,out _),Is.False);}Assert.That(File.ReadAllBytes(store.PrimaryPath),Is.EqualTo(original));Assert.That(store.Read().Data.playerYaw,Is.EqualTo(120));Assert.That(File.Exists(store.TemporaryPath),Is.False);
        }
        [Test]public void P07_B06_OrphanTemporaryIsIgnoredAndNextSaveReplacesIt()
        {
            var data=SaveCodecTests.Fixture();Assert.That(store.TryWrite(data,out _),Is.True);File.WriteAllText(store.TemporaryPath,"half a write");Assert.That(store.Read().Data.playerYaw,Is.EqualTo(120));data.playerYaw=230;Assert.That(store.TryWrite(data,out _),Is.True);Assert.That(File.Exists(store.TemporaryPath),Is.False);Assert.That(store.Read().Data.playerYaw,Is.EqualTo(230));
        }
        [Test]public void P07_B07_OversizedInvalidUtf8AndEmptyFilesAreRejectedBeforeUse()
        {
            Directory.CreateDirectory(directory);foreach(var bytes in new[]{new byte[0],new byte[]{255,254,255},new byte[65537]}){File.WriteAllBytes(store.PrimaryPath,bytes);Assert.That(store.Read().Success,Is.False);Assert.That(store.TryWrite(SaveCodecTests.Fixture(),out _),Is.False);Assert.That(File.ReadAllBytes(store.PrimaryPath),Is.EqualTo(bytes));}
        }
    }
}
