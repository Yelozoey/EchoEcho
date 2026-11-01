using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.Echo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace EchoStation.Salvage.Tests
{
    public class R2SaveContractTests
    {
        string folder;const string Fixture="Docs/Tests/SalvagePrototype/Fixtures/R1_v1_Mixed.json";
        [SetUp]public void Before(){folder=Path.GetFullPath("Library/EchoStation/R2SaveTests/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);}
        [TearDown]public void After(){var parent=Path.GetFullPath("Library/EchoStation/R2SaveTests")+Path.DirectorySeparatorChar;if(Directory.Exists(folder)&&folder.StartsWith(parent,StringComparison.OrdinalIgnoreCase))Directory.Delete(folder,true);}
        static SalvageSnapshot Legacy()=>SalvageSaveStore.Decode(File.ReadAllText(Fixture));
        [Test]public void ActualR1FixtureDecodesWithEmptyCollaborationAndV2RoundTrips()
        {var s=Legacy();Assert.That(s.Collaboration.Discovered,Is.Empty);Assert.That(s.Collaboration.Messages,Is.Empty);s.Collaboration.BaseMode=(int)IntentKind.Wait;s.Collaboration.Discovered=new[]{"cargo_a","danger_power_box"};s.Collaboration.Messages=new[]{new SavedDialogue{Number=9,Speaker=0,Text="电量"},new SavedDialogue{Number=10,Speaker=1,Text="当前电量37%。"}};var json=SalvageSaveStore.Encode(s);Assert.That(JObject.Parse(json)["version"].Value<int>(),Is.EqualTo(2));Assert.That(SalvageSaveStore.Encode(SalvageSaveStore.Decode(json)),Is.EqualTo(json));}
        [Test]public void MigrationPreservesActualOriginalBytesBeforeAtomicV2Write()
        {var bytes=File.ReadAllBytes(Fixture);var store=new SalvageSaveStore(folder);File.WriteAllBytes(store.Primary,bytes);Assert.That(store.TryRead(out var s,out var error),Is.True,error);Assert.That(store.LastReadVersion,Is.EqualTo(1));Assert.That(store.TryWrite(s,out error),Is.True,error);Assert.That(File.ReadAllBytes(store.LegacyBackup),Is.EqualTo(bytes));Assert.That(JObject.Parse(File.ReadAllText(store.Primary))["version"].Value<int>(),Is.EqualTo(2));Assert.That(store.TryWrite(s,out error),Is.True,error);Assert.That(File.ReadAllBytes(store.LegacyBackup),Is.EqualTo(bytes));Assert.That(File.Exists(store.Primary+".tmp"),Is.False);}
        [Test]public void ConflictingLegacyBackupAndFutureFilesArePreserved()
        {var store=new SalvageSaveStore(folder);File.Copy(Fixture,store.Primary);File.WriteAllText(store.LegacyBackup,"different");var before=File.ReadAllBytes(store.Primary);Assert.That(store.TryWrite(Legacy(),out _),Is.False);Assert.That(File.ReadAllBytes(store.Primary),Is.EqualTo(before));var future=SalvageSaveStore.Encode(Legacy()).Replace("\"version\":2","\"version\":3");File.WriteAllText(store.Primary,future);Assert.That(store.TryRead(out _,out _),Is.False);Assert.That(store.TryWrite(Legacy(),out _),Is.False);Assert.That(File.ReadAllText(store.Primary),Is.EqualTo(future));}
        [TestCase("unknown")][TestCase("duplicate")][TestCase("mode")][TestCase("history")][TestCase("extra")][TestCase("type")]
        public void ValidChecksumCannotAuthorizeBadCollaboration(string mutation)
        {
            var root=JObject.Parse(SalvageSaveStore.Encode(Legacy()));var p=(JObject)root["payload"];var c=(JObject)p["Collaboration"];
            switch(mutation){case "unknown":c["Discovered"]=new JArray("cargo_heavy");break;case "duplicate":c["Discovered"]=new JArray("cargo_a","cargo_a");break;case "mode":c["BaseMode"]=(int)IntentKind.Investigate;break;case "history":c["Messages"]=new JArray(new JObject{["Number"]=0,["Speaker"]=0,["Text"]="坏编号",["Refusal"]=false});break;case "extra":c["Execute"]="open_door";break;case "type":c["BaseMode"]="follow";break;}
            using(var sha=SHA256.Create())root["sha256"]=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(p.ToString(Formatting.None)))).Replace("-","").ToLowerInvariant();Assert.Catch(()=>SalvageSaveStore.Decode(root.ToString(Formatting.None)));
        }
    }
}
