using System;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Salvage.Tests
{
    public class SalvageSaveTests
    {
        string folder;
        [SetUp]public void Before(){folder=Path.GetFullPath("Library/EchoStation/SalvageSaveTests/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);}
        [TearDown]public void After(){if(Directory.Exists(folder)&&folder.StartsWith(Path.GetFullPath("Library/EchoStation/SalvageSaveTests")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(folder,true);}
        static SalvageSnapshot Fresh()=>new SalvageSnapshot{Player=new SalvagePose{X=5,Y=.15f,Z=12,Yaw=90},Echo=new SalvagePose{X=4,Y=.02f,Z=10},Charge=100,Cargo=SalvageCatalog.Items.Select((d,i)=>new SavedCargo{Id=d.Id,State=0,X=SalvageCargoSetup.Positions[i].x,Y=0,Z=SalvageCargoSetup.Positions[i].z}).ToArray()};
        [Test]public void FullSnapshotRoundTripsAllStatesAndNumbers()
        {var s=Fresh();s.Cargo[0].State=1;s.Cargo[1].State=2;s.Cargo[2].Z=8;s.Charge=20;s.Warned=true;s.Elapsed=123.25;s.Flashlight=true;var json=SalvageSaveStore.Encode(s);var copy=SalvageSaveStore.Decode(json);Assert.That(SalvageSaveStore.Encode(copy),Is.EqualTo(json));Assert.That(copy.Cargo[0].State,Is.EqualTo(1));}
        [Test]public void DuplicateUnknownMultipleHeldAndInvalidNumbersReject()
        {var s=Fresh();s.Cargo[1].Id=s.Cargo[0].Id;Assert.Throws<ArgumentException>(()=>SalvageSaveStore.Encode(s));s=Fresh();s.Cargo[0].Id="bad";Assert.Throws<ArgumentException>(()=>SalvageSaveStore.Encode(s));s=Fresh();s.Cargo[0].State=s.Cargo[1].State=1;Assert.Throws<ArgumentException>(()=>SalvageSaveStore.Encode(s));s=Fresh();s.Charge=float.NaN;Assert.Throws<ArgumentException>(()=>SalvageSaveStore.Encode(s));s=Fresh();s.Completed=true;Assert.Throws<ArgumentException>(()=>SalvageSaveStore.Encode(s));}
        [Test]public void ChecksumTrailingObjectAndDuplicateFieldsReject()
        {var json=SalvageSaveStore.Encode(Fresh());Assert.Throws<ArgumentException>(()=>SalvageSaveStore.Decode(json.Replace("\"Charge\":100.0","\"Charge\":99.0")));Assert.Catch(()=>SalvageSaveStore.Decode(json+"{}"));Assert.Catch(()=>SalvageSaveStore.Decode(json.Replace("\"version\":2","\"version\":2,\"version\":2")));}
        [Test]public void AtomicWriteAndValidBackupRecovery()
        {var store=new SalvageSaveStore(folder);var s=Fresh();s.Elapsed=10;Assert.That(store.TryWrite(s,out var error),Is.True,error);s.Elapsed=20;Assert.That(store.TryWrite(s,out error),Is.True,error);Assert.That(File.Exists(store.Primary+".tmp"),Is.False);File.WriteAllText(store.Primary,"broken");Assert.That(store.TryRead(out var recovered,out error),Is.True);Assert.That(recovered.Elapsed,Is.EqualTo(10));Assert.That(store.TryWrite(s,out error),Is.True,error);Assert.That(Directory.GetFiles(folder,"*.rejected-*").Length,Is.EqualTo(1));Assert.That(File.ReadAllText(store.Backup),Does.Contain("10.0"));}
        [Test]public void FutureAndUnrecoverableFilesAreNeverOverwritten()
        {var store=new SalvageSaveStore(folder);var future=SalvageSaveStore.Encode(Fresh()).Replace("\"version\":2","\"version\":3");File.WriteAllText(store.Primary,future);Assert.That(store.TryRead(out _,out _),Is.False);Assert.That(store.TryWrite(Fresh(),out _),Is.False);Assert.That(File.ReadAllText(store.Primary),Is.EqualTo(future));File.WriteAllText(store.Primary,"damaged");Assert.That(store.TryWrite(Fresh(),out _),Is.False);Assert.That(File.ReadAllText(store.Primary),Is.EqualTo("damaged"));}
        [Test]public void SceneRestoreIsWholeAndInvalidGeometryDoesNotMutate()
        {
            var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var save=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvagePersistence>()).Single();save.ConfigureTestStore(folder);var s=save.Capture();s.Cargo.Single(c=>c.Id=="cargo_a").State=1;s.Cargo.Single(c=>c.Id=="cargo_b").State=2;s.Cargo.Single(c=>c.Id=="cargo_c").Z=8;s.Charge=20;s.Warned=true;s.Elapsed=123.25;s.Flashlight=true;
                Physics.SyncTransforms();Assert.That(save.TryRestore(s,out var error),Is.True,error);var carry=save.GetComponent<SalvageCarry>();Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));Assert.That(carry.Held.Body.enabled,Is.False);Assert.That(save.GetComponent<SalvageMotor>().Speed,Is.EqualTo(3.3f).Within(.001));Assert.That(save.GetComponent<SalvageDelivery>().Count,Is.EqualTo(1));
                var before=SalvageSaveStore.Encode(save.Capture());var invalid=SalvageSaveStore.Decode(before);invalid.Player.X=12;invalid.Player.Z=12;Assert.That(save.TryRestore(invalid,out error),Is.False);Assert.That(SalvageSaveStore.Encode(save.Capture()),Is.EqualTo(before));Assert.That(save.Save(),Is.True);carry.ResetToInitial();Assert.That(save.Load(),Is.True);Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));Assert.That(save.GetComponent<PlayerFlashlight>().IsOn,Is.True);
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
