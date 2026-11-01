using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class ExpansionSaveTests
    {
        sealed class Section:IExpansionSaveSection
        {
            public string Key{get;}public int Value;public bool Throw;
            public Section(string key){Key=key;}
            public JToken Capture()=>new JValue(Value);
            public bool TryPrepare(ExpansionSnapshot snapshot,JToken token,out Action apply,out Action rollback,out string error)
            {
                apply=rollback=null;error="";if(token==null||token.Type!=JTokenType.Integer||token.Value<int>()<0||token.Value<int>()>10){error="bad section";return false;}
                int before=Value,target=token.Value<int>();apply=()=>{Value=target;if(Throw)throw new InvalidOperationException("Injected section failure");};rollback=()=>Value=before;return true;
            }
        }
        static Dictionary<string,Vector3> Positions()=>ExpansionCatalog.Items.Select((d,i)=>new{id=d.Id,p=new Vector3(3+i*3,.3f,5)}).ToDictionary(x=>x.id,x=>x.p);
        static ExpansionSnapshot Snapshot()=>new ExpansionSnapshot(ExpansionCatalog.NewInventory().Records,Positions(),new Vector3(5,.15f,12),90,0,new JObject());
        static ExpansionSaveStore FileStore()=>new ExpansionSaveStore(Path.GetFullPath("Library/EchoStation/SalvageExpansion/Tests/File_"+Guid.NewGuid().ToString("N")));
        static ExpansionPersistence persistence;static ExpansionWorld world;
        static IEnumerator Boot()
        {
            yield return InventoryWorldTests.CreateFixture();world=InventoryWorldTests.FixtureWorld;persistence=world.gameObject.AddComponent<ExpansionPersistence>();persistence.ConfigureTests(Path.GetFullPath("Library/EchoStation/SalvageExpansion/Tests/Runtime_"+Guid.NewGuid().ToString("N")));
        }
        [Test]public void StrictCodecRoundTripsGridPositionsAndRejectsTamperDuplicateFieldsAndFutureVersion()
        {
            var snapshot=Snapshot();string json=ExpansionSaveStore.Encode(snapshot);var decoded=ExpansionSaveStore.Decode(json);Assert.That(ExpansionSaveStore.Encode(decoded),Is.EqualTo(json));
            var tampered=JObject.Parse(json);tampered["payload"]["player"]["yaw"]=91;Assert.Throws<ArgumentException>(()=>ExpansionSaveStore.Decode(tampered.ToString(Newtonsoft.Json.Formatting.None)));
            Assert.Throws<Newtonsoft.Json.JsonReaderException>(()=>ExpansionSaveStore.Decode(json.Replace("\"version\":1","\"version\":1,\"version\":1")));
            Assert.Throws<NotSupportedException>(()=>ExpansionSaveStore.Decode(json.Replace("\"version\":1","\"version\":2")));
            Assert.Throws<ArgumentException>(()=>ExpansionSaveStore.Decode(new string('x',131073)));
        }
        [Test]public void SnapshotCopiesPositionsSectionsAndRejectsBadGeometryState()
        {
            var positions=Positions();var sections=new JObject{{"a",1}};var snapshot=new ExpansionSnapshot(ExpansionCatalog.NewInventory().Records,positions,new Vector3(5,.15f,12),90,0,sections);positions[ExpansionCatalog.Core]=new Vector3(float.NaN,0,0);sections["a"]=9;
            Assert.That(snapshot.Positions[ExpansionCatalog.Core].x,Is.EqualTo(12));Assert.That(snapshot.Sections["a"].Value<int>(),Is.EqualTo(1));var copy=snapshot.Sections;copy["a"]=8;Assert.That(snapshot.Sections["a"].Value<int>(),Is.EqualTo(1));
            Assert.Throws<ArgumentException>(()=>new ExpansionSnapshot(snapshot.Inventory,positions,snapshot.Player,90,0,new JObject()));
            Assert.Throws<ArgumentException>(()=>new ExpansionSnapshot(snapshot.Inventory,snapshot.Positions,new Vector3(37,0,12),90,0,new JObject()));
        }
        [Test]public void ActualAtomicWriteKeepsPreviousBackupAndFallsBackWithoutOverwritingBadPrimary()
        {
            var store=FileStore();Assert.That(store.TryWrite(Snapshot(),out var error),Is.True,error);string first=File.ReadAllText(store.Primary);var next=new ExpansionSnapshot(Snapshot().Inventory,Snapshot().Positions,new Vector3(6,.15f,12),90,0,new JObject());Assert.That(store.TryWrite(next,out error),Is.True,error);Assert.That(File.ReadAllText(store.Backup),Is.EqualTo(first));
            File.WriteAllText(store.Primary,"broken");Assert.That(store.TryRead(out var read,out var message),Is.True,message);Assert.That(read.Player.x,Is.EqualTo(5));Assert.That(message,Does.Contain("备份"));Assert.That(File.ReadAllText(store.Primary),Is.EqualTo("broken"));
        }
        [Test]public void InvalidFilesAndExistingTemporaryArePreservedOnWriteRefusal()
        {
            var store=FileStore();Directory.CreateDirectory(Path.GetDirectoryName(store.Primary));File.WriteAllText(store.Primary,"broken");Assert.That(store.TryWrite(Snapshot(),out _),Is.False);Assert.That(File.ReadAllText(store.Primary),Is.EqualTo("broken"));
            var second=FileStore();Directory.CreateDirectory(Path.GetDirectoryName(second.Primary));File.WriteAllText(second.Primary+".tmp","owned stale evidence");Assert.That(second.TryWrite(Snapshot(),out _),Is.False);Assert.That(File.ReadAllText(second.Primary+".tmp"),Is.EqualTo("owned stale evidence"));Assert.That(File.Exists(second.Primary),Is.False);
        }
        [Test]public void FuturePrimaryIsNotSilentlyReplacedByOlderBackup()
        {
            var store=FileStore();store.TryWrite(Snapshot(),out _);store.TryWrite(Snapshot(),out _);string future=File.ReadAllText(store.Primary).Replace("\"version\":1","\"version\":2");File.WriteAllText(store.Primary,future);
            Assert.That(store.TryRead(out _,out _),Is.False);Assert.That(store.TryWrite(Snapshot(),out _),Is.False);Assert.That(File.ReadAllText(store.Primary),Is.EqualTo(future));
        }
        [UnityTest]public IEnumerator ActualPlayerInventoryHeldAndWorldPositionsRestoreAndCancelPreview()
        {
            yield return new EnterPlayMode();yield return Boot();world.Inventory.TryStow(ExpansionCatalog.Core,3,0,1,out _);world.Inventory.TryEquip(ExpansionCatalog.Wrench,out _);world.SyncViews();Assert.That(persistence.Save(),Is.True,world.Message);var saved=persistence.Capture();
            world.Inventory.TryUnequip(out _);world.Motor.RestorePose(new Vector3(10,.15f,12),30,10);world.Backpack.Open();world.Backpack.BeginDrag(0,0);long epoch=persistence.Epoch;
            Assert.That(persistence.Load(),Is.True,world.Message);Assert.That(persistence.Epoch,Is.GreaterThan(epoch));Assert.That(world.Backpack.IsOpen,Is.False);Assert.That(world.Inventory.HeldId,Is.EqualTo(ExpansionCatalog.Wrench));Assert.That(world.Inventory.Get(ExpansionCatalog.Core).Rotation,Is.EqualTo(1));Assert.That(world.transform.position,Is.EqualTo(saved.Player));Assert.That(world.Actors[ExpansionCatalog.Wrench].Body.enabled,Is.False);
        }
        [UnityTest]public IEnumerator InvalidScenePoseAndSectionsLeaveActiveDragAndStateUntouched()
        {
            yield return new EnterPlayMode();yield return Boot();world.Backpack.Open();world.Backpack.BeginDrag(0,0);var before=persistence.Capture();var inventory=world.Inventory;long revision=inventory.Revision;long epoch=persistence.Epoch;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(InventoryWorldTests.FixtureRoot.transform);wall.transform.position=new Vector3(10,1,12);Physics.SyncTransforms();var invalid=new ExpansionSnapshot(before.Inventory,before.Positions,new Vector3(10,.15f,12),90,0,before.Sections);
            Assert.That(persistence.TryRestore(invalid,out _),Is.False);Assert.That(world.Inventory,Is.SameAs(inventory));Assert.That(world.Inventory.Revision,Is.EqualTo(revision));Assert.That(persistence.Epoch,Is.EqualTo(epoch));Assert.That(world.Backpack.IsOpen,Is.True);Assert.That(world.Backpack.Drag.Active,Is.True);
            var badSections=new ExpansionSnapshot(before.Inventory,before.Positions,before.Player,before.Yaw,before.Pitch,new JObject{{"unknown",1}});Assert.That(persistence.TryRestore(badSections,out _),Is.False);Assert.That(world.Backpack.Drag.Active,Is.True);
        }
        [UnityTest]public IEnumerator ParticipantFailureRollsBackEarlierParticipantsWithoutReplacingInventory()
        {
            yield return new EnterPlayMode();yield return Boot();var first=new Section("a");var second=new Section("b"){Throw=true};persistence.Register(first);persistence.Register(second);world.Backpack.Open();world.Backpack.BeginDrag(0,0);var before=persistence.Capture();var inventory=world.Inventory;
            var snapshot=new ExpansionSnapshot(before.Inventory,before.Positions,before.Player,before.Yaw,before.Pitch,new JObject{{"a",2},{"b",3}});
            Assert.That(persistence.TryRestore(snapshot,out _),Is.False);Assert.That(first.Value,Is.Zero);Assert.That(second.Value,Is.Zero);Assert.That(world.Inventory,Is.SameAs(inventory));Assert.That(world.Backpack.IsOpen,Is.True);Assert.That(world.Backpack.Drag.Active,Is.True);Assert.That(persistence.Epoch,Is.Zero);
        }
        [UnityTest]public IEnumerator SuccessfulParticipantRestoreAndFailedRunCannotOverwriteValidSave()
        {
            yield return new EnterPlayMode();yield return Boot();var section=new Section("device"){Value=4};persistence.Register(section);Assert.That(persistence.Save(),Is.True,world.Message);string saved=File.ReadAllText(persistence.Store.Primary);section.Value=1;Assert.That(persistence.Load(),Is.True,world.Message);Assert.That(section.Value,Is.EqualTo(4));world.ActionsAllowed=false;Assert.That(persistence.Save(),Is.False);Assert.That(File.ReadAllText(persistence.Store.Primary),Is.EqualTo(saved));
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;yield return InventoryWorldTests.DestroyFixture();yield return new ExitPlayMode();}
    }
}
