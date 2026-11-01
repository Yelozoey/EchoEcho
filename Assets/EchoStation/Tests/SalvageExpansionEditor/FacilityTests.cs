using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class FacilityTests
    {
        static ExpansionWorld world;static FacilityController facility;static FacilityDevice[] devices;static ExpansionPersistence persistence;
        public static FacilityController FixtureFacility=>facility;
        public static FacilityDevice[] FixtureDevices=>devices;
        public static IEnumerator CreateFixture()
        {
            yield return InventoryWorldTests.CreateFixture();world=InventoryWorldTests.FixtureWorld;var parent=InventoryWorldTests.FixtureRoot.transform;
            var positions=new[]{new Vector3(10,.8f,4),new Vector3(15,.8f,4),new Vector3(27.5f,.8f,10),new Vector3(16,.8f,6),new Vector3(29.5f,.3f,12),new Vector3(30,.3f,11.5f),new Vector3(30,.3f,12.5f)};
            devices=new FacilityDevice[7];for(int i=0;i<7;i++){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Device_"+i;go.transform.SetParent(parent);go.transform.position=positions[i];go.transform.localScale=Vector3.one*.2f;devices[i]=go.AddComponent<FacilityDevice>();devices[i].Configure((FacilityDeviceKind)i);}
            facility=world.gameObject.AddComponent<FacilityController>();facility.Configure(devices);facility.Initialize();facility.enabled=false;persistence=world.GetComponent<ExpansionPersistence>();persistence.ConfigureTests(Path.GetFullPath("Library/EchoStation/SalvageExpansion/Tests/Facility_"+Guid.NewGuid().ToString("N")));Physics.SyncTransforms();
        }
        public static void Aim(FacilityDevice target)
        {
            var position=new Vector3(target.transform.position.x-2,.15f,target.transform.position.z);var delta=target.transform.position-(position+Vector3.up*1.6f);world.Motor.RestorePose(position,90,-Mathf.Asin(delta.normalized.y)*Mathf.Rad2Deg);world.Input.SetApplicationFocus(true);world.Input.SetPointerCaptured(true);Physics.SyncTransforms();
        }
        public static void Equip(string id){if(world.Inventory.HeldId!=null)Assert.That(world.Inventory.TryUnequip(out _),Is.True);Assert.That(world.Inventory.TryEquip(id,out _),Is.True);world.SyncViews();}
        public static void ReleaseCore()
        {
            Equip(ExpansionCatalog.Insulator);Aim(devices[2]);Assert.That(facility.TryIsolate(devices[2]),Is.True);Equip(ExpansionCatalog.Wrench);
            foreach(var device in devices.Skip(4)){Aim(device);Assert.That(facility.TryReleaseClamp(device),Is.True);}
        }
        [UnityTest]public IEnumerator BatteryRequiresEquippedItemAndOwnedReachableSocket()
        {
            yield return new EnterPlayMode();yield return CreateFixture();Aim(devices[0]);long revision=world.Inventory.Revision;Assert.That(facility.Socket(devices[0]),Is.False);Assert.That(world.Inventory.Revision,Is.EqualTo(revision));
            Equip(ExpansionCatalog.Battery);Aim(devices[0]);Assert.That(facility.Socket(devices[0]),Is.True);Assert.That(facility.FanPowered,Is.True);Assert.That(facility.DataPowered,Is.False);Assert.That(world.Inventory.Get(ExpansionCatalog.Battery).Location,Is.EqualTo(InventoryItemLocation.Installed));Assert.That(world.Actors[ExpansionCatalog.Battery].gameObject.activeSelf,Is.True);Assert.That(world.Actors[ExpansionCatalog.Battery].Body.enabled,Is.False);
            Aim(devices[1]);revision=world.Inventory.Revision;Assert.That(facility.Socket(devices[1]),Is.False);Assert.That(world.Inventory.Revision,Is.EqualTo(revision));Assert.That(facility.BatterySlot,Is.EqualTo("fan_socket"));
        }
        [UnityTest]public IEnumerator FanAndDataShareOneFiniteBatteryAndPauseStopsDrain()
        {
            yield return new EnterPlayMode();yield return CreateFixture();Equip(ExpansionCatalog.Battery);Aim(devices[0]);facility.Socket(devices[0]);facility.Advance(1,false);Assert.That(facility.Charge,Is.EqualTo(89));Assert.That(facility.DataSeconds,Is.Zero);facility.Advance(1,true);Assert.That(facility.Charge,Is.EqualTo(89));
            Aim(devices[0]);Assert.That(facility.Socket(devices[0]),Is.True);facility.Advance(1,false);Assert.That(facility.Charge,Is.EqualTo(89));Equip(ExpansionCatalog.Battery);Aim(devices[1]);Assert.That(facility.Socket(devices[1]),Is.True);for(int i=0;i<8;i++)facility.Advance(1,false);Assert.That(facility.DataReady,Is.True);Assert.That(facility.Charge,Is.EqualTo(81));Assert.That(facility.FanPowered,Is.False);
            for(int i=0;i<100;i++)facility.Advance(1,false);Assert.That(facility.Charge,Is.Zero);Assert.That(facility.DataPowered,Is.False);Assert.That(facility.DataSeconds,Is.EqualTo(8));
        }
        [UnityTest]public IEnumerator IsolationAndThreeIndividualClampsGateActualCorePickup()
        {
            yield return new EnterPlayMode();yield return CreateFixture();Aim(devices[2]);Assert.That(facility.TryIsolate(devices[2]),Is.False);Equip(ExpansionCatalog.Wrench);Aim(devices[4]);Assert.That(facility.TryReleaseClamp(devices[4]),Is.False);
            ReleaseCore();Assert.That(facility.Clamps,Is.EqualTo(7));Assert.That(facility.CanRemoveCore,Is.True);long before=facility.Revision;Aim(devices[6]);Assert.That(facility.TryReleaseClamp(devices[6]),Is.False);Assert.That(facility.Revision,Is.EqualTo(before));
            var actor=world.Actors[ExpansionCatalog.Core];var position=new Vector3(28,.15f,12);var delta=actor.transform.position-(position+Vector3.up*1.6f);world.Motor.RestorePose(position,90,-Mathf.Asin(delta.normalized.y)*Mathf.Rad2Deg);Physics.SyncTransforms();Assert.That(world.TryPickupFromView(),Is.True,world.Message);facility.Advance(0,false);Assert.That(facility.CoreRemoved,Is.True);Assert.That(facility.Archived,Is.False);
        }
        [UnityTest]public IEnumerator FullBagRecoveryLeavesBatteryInstalledAndPowerStateUnchanged()
        {
            yield return new EnterPlayMode();yield return CreateFixture();Equip(ExpansionCatalog.Battery);Aim(devices[0]);facility.Socket(devices[0]);ReleaseCore();
            var rows=world.Inventory.Records.Select(r=>r.Id==ExpansionCatalog.Wrench?new InventoryItemState(r.Id,InventoryItemLocation.Backpack,0,0,1):r.Id==ExpansionCatalog.Insulator?new InventoryItemState(r.Id,InventoryItemLocation.Backpack,3,0,1):r.Id==ExpansionCatalog.Core?new InventoryItemState(r.Id,InventoryItemLocation.Backpack,0,1):r.Id==ExpansionCatalog.Instrument?new InventoryItemState(r.Id,InventoryItemLocation.Backpack,4,1):r.Id==ExpansionCatalog.Records?new InventoryItemState(r.Id,InventoryItemLocation.Backpack,2,1):r).ToArray();
            Assert.That(world.Inventory.TryRestore(rows,out var error),Is.True,error);world.SyncViews();facility.Advance(0,false);Aim(devices[0]);long revision=world.Inventory.Revision;Assert.That(facility.Socket(devices[0]),Is.False);Assert.That(world.Inventory.Revision,Is.EqualTo(revision));Assert.That(facility.FanPowered,Is.True);Assert.That(facility.BatterySlot,Is.EqualTo("fan_socket"));Assert.That(world.Message,Does.Contain("空间不足"));
        }
        [UnityTest]public IEnumerator DeviceSectionRestoresChargeProgressAndSocketAndRejectsUnknownSocket()
        {
            yield return new EnterPlayMode();yield return CreateFixture();Equip(ExpansionCatalog.Battery);Aim(devices[1]);facility.Socket(devices[1]);for(int i=0;i<4;i++)facility.Advance(1,false);Assert.That(persistence.Save(),Is.True,world.Message);var saved=persistence.Capture();for(int i=0;i<5;i++)facility.Advance(1,false);Assert.That(persistence.Load(),Is.True,world.Message);Assert.That(facility.Charge,Is.EqualTo(86));Assert.That(facility.DataSeconds,Is.EqualTo(4));Assert.That(facility.BatterySlot,Is.EqualTo("data_socket"));
            var invalid=new ExpansionSnapshot(saved.Inventory.Select(r=>r.Id==ExpansionCatalog.Battery?new InventoryItemState(r.Id,InventoryItemLocation.Installed,slot:"unknown"):r),saved.Positions,saved.Player,saved.Yaw,saved.Pitch,saved.Sections);var inventory=world.Inventory;long revision=facility.Revision;Assert.That(persistence.TryRestore(invalid,out _),Is.False);Assert.That(world.Inventory,Is.SameAs(inventory));Assert.That(facility.Revision,Is.EqualTo(revision));
        }
        [UnityTest]public IEnumerator ValveRequiresWrenchAndAimAndInstalledSnapshotCannotForgeCoreExtraction()
        {
            yield return new EnterPlayMode();yield return CreateFixture();Aim(devices[3]);Assert.That(facility.TryCloseValve(devices[3]),Is.False);Equip(ExpansionCatalog.Wrench);Aim(devices[3]);Assert.That(facility.TryCloseValve(devices[3]),Is.True);Assert.That(facility.SteamSuppressed,Is.True);
            var saved=persistence.Capture();var invalid=new ExpansionSnapshot(saved.Inventory.Select(r=>r.Id==ExpansionCatalog.Core?new InventoryItemState(r.Id,InventoryItemLocation.Backpack,3,0,1):r),saved.Positions,saved.Player,saved.Yaw,saved.Pitch,saved.Sections);Assert.That(persistence.TryRestore(invalid,out _),Is.False);Assert.That(world.Inventory.Get(ExpansionCatalog.Core).Location,Is.EqualTo(InventoryItemLocation.World));
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;yield return InventoryWorldTests.DestroyFixture();yield return new ExitPlayMode();}
    }
}
