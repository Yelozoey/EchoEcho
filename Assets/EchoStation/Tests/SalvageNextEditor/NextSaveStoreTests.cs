using System;
using System.IO;
using System.Linq;
using EchoStation.Salvage;
using EchoStation.Salvage.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextSaveStoreTests
    {
        string directory;NextSaveStore store;
        internal static NextSnapshot Fresh(RecoveryPreference preference=RecoveryPreference.Nearby,RecoveryQuality quality=RecoveryQuality.Medium)=>new NextSnapshot(new SalvageSnapshot{Player=new SalvagePose{X=5,Y=.15f,Z=12,Yaw=90},Echo=new SalvagePose{X=4,Y=.02f,Z=10},Charge=100,Cargo=SalvageCatalog.Items.Select((d,i)=>new SavedCargo{Id=d.Id,State=0,X=SalvageCargoSetup.Positions[i].x,Y=0,Z=SalvageCargoSetup.Positions[i].z}).ToArray()},preference,quality);
        [SetUp]public void Setup(){directory="Library/EchoStation/SalvageNext/Tests/Store_"+Guid.NewGuid().ToString("N");store=new NextSaveStore(directory);}
        [Test]public void RoundTripExplicitSettingsAndWorldAreOneChecksummedPayload(){var original=Fresh(RecoveryPreference.Lightweight,RecoveryQuality.High);var text=NextSaveStore.Encode(original);var copy=NextSaveStore.Decode(text);Assert.That(copy.Preference,Is.EqualTo(original.Preference));Assert.That(copy.Quality,Is.EqualTo(original.Quality));Assert.That(SalvageSaveStore.Encode(copy.World),Is.EqualTo(SalvageSaveStore.Encode(original.World)));Assert.That(text,Does.Not.Contain("UseLLM"));}
        [Test]public void SnapshotCopiesMutableWorld(){var copy=Fresh();var snapshot=new NextSnapshot(copy.World,RecoveryPreference.HighScore,RecoveryQuality.Low);copy.World.Charge=0;Assert.That(snapshot.World.Charge,Is.EqualTo(100));}
        [TestCase("duplicate")][TestCase("extra")][TestCase("stringPreference")][TestCase("checksum")][TestCase("nan")][TestCase("trailing")]
        public void StrictMalformedSaveRejected(string kind)
        {var text=NextSaveStore.Encode(Fresh());switch(kind){case "duplicate":text=text.Replace("\"version\":1","\"version\":1,\"version\":1");break;case "extra":text=text.Insert(1,"\"surprise\":0,");break;case "stringPreference":text=text.Replace("\"preference\":0","\"preference\":\"0\"");break;case "checksum":text=text.Replace("\"quality\":1","\"quality\":2");break;case "nan":text=text.Replace("\"quality\":1","\"quality\":NaN");break;default:text+="{}";break;}Assert.That(()=>NextSaveStore.Decode(text),Throws.Exception);}
        [Test]public void AtomicReplacementKeepsExactPreviousGenerationAndNoTemp(){Assert.That(store.TryWrite(Fresh(),out var error),Is.True,error);var first=File.ReadAllBytes(store.Primary);Assert.That(store.TryWrite(Fresh(RecoveryPreference.HighScore),out error),Is.True,error);Assert.That(File.ReadAllBytes(store.Backup),Is.EqualTo(first));Assert.That(File.Exists(store.Primary+".tmp"),Is.False);Assert.That(store.TryRead(out var value,out error),Is.True,error);Assert.That(value.Preference,Is.EqualTo(RecoveryPreference.HighScore));}
        [Test]public void CorruptPrimaryFallsBackAndRecoveryWritePreservesOriginalBytes(){store.TryWrite(Fresh(),out _);store.TryWrite(Fresh(RecoveryPreference.HighScore),out _);File.WriteAllText(store.Primary,"broken-primary");Assert.That(store.TryRead(out var value,out var error),Is.True,error);Assert.That(value.Preference,Is.EqualTo(RecoveryPreference.Nearby));Assert.That(store.TryWrite(Fresh(RecoveryPreference.Lightweight),out error),Is.True,error);Assert.That(File.ReadAllText(Directory.GetFiles(directory,"slot1.json.rejected-*").Single()),Is.EqualTo("broken-primary"));}
        [Test]public void DualCorruptionAndTemporaryFilesAreNeverOverwritten(){Directory.CreateDirectory(directory);File.WriteAllText(store.Primary,"bad");File.WriteAllText(store.Backup,"bad-backup");Assert.That(store.TryWrite(Fresh(),out _),Is.False);Assert.That(File.ReadAllText(store.Primary),Is.EqualTo("bad"));File.WriteAllText(store.Primary,NextSaveStore.Encode(Fresh()));File.WriteAllText(store.Backup,NextSaveStore.Encode(Fresh()));File.WriteAllText(store.Primary+".tmp","unfinished");Assert.That(store.TryWrite(Fresh(),out _),Is.False);Assert.That(File.ReadAllText(store.Primary+".tmp"),Is.EqualTo("unfinished"));}
        [Test]public void FuturePrimaryRefusesFallbackAndWrite(){Directory.CreateDirectory(directory);var root=JObject.Parse(NextSaveStore.Encode(Fresh()));root["version"]=2;var future=root.ToString();File.WriteAllText(store.Primary,future);File.WriteAllText(store.Backup,NextSaveStore.Encode(Fresh()));Assert.That(store.TryRead(out _,out _),Is.False);Assert.That(store.TryWrite(Fresh(),out _),Is.False);Assert.That(File.ReadAllText(store.Primary),Is.EqualTo(future));}
        [Test]public void InvalidEnumsAndOversizeRejected(){Assert.Throws<ArgumentException>(()=>new NextSnapshot(Fresh().World,(RecoveryPreference)99,RecoveryQuality.Medium));Assert.Throws<ArgumentException>(()=>new NextSnapshot(Fresh().World,RecoveryPreference.Nearby,(RecoveryQuality)9));Assert.That(()=>NextSaveStore.Decode(new string(' ',98305)),Throws.Exception);}
    }
}
