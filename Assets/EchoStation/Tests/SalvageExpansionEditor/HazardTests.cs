using System;
using System.Collections;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class HazardTests
    {
        public static EnvironmentHazards FixtureHazards;static ExpansionWorld world;static ExpansionPersistence persistence;
        public static IEnumerator CreateFixture()
        {
            yield return FacilityTests.CreateFixture();world=InventoryWorldTests.FixtureWorld;persistence=world.GetComponent<ExpansionPersistence>();
            var zones=new HazardZone[2];for(int i=0;i<2;i++){var go=new GameObject("Hazard_"+i);go.transform.SetParent(InventoryWorldTests.FixtureRoot.transform);go.transform.position=new Vector3(12+i*10,1,18);var box=go.AddComponent<BoxCollider>();box.size=new Vector3(3,2,3);zones[i]=go.AddComponent<HazardZone>();zones[i].Configure((EnvironmentHazardKind)i);}
            FixtureHazards=world.gameObject.AddComponent<EnvironmentHazards>();FixtureHazards.Configure(zones);FixtureHazards.Initialize();FixtureHazards.enabled=false;Physics.SyncTransforms();
        }
        static void Place(Vector3 p){world.Motor.RestorePose(p,90,0);Physics.SyncTransforms();}
        static void Expose(){Place(new Vector3(22,.15f,18));FixtureHazards.Advance(1,false);FixtureHazards.Advance(1,false);}
        [UnityTest]public IEnumerator SignalsCycleAndActualDeviceStateSuppressesBothHazards()
        {
            yield return new EnterPlayMode();yield return CreateFixture();var h=FixtureHazards;var zones=h.Zones;Assert.That(h.Signal(zones[0]),Is.EqualTo(HazardSignal.Warning));h.Advance(1,false);h.Advance(1,false);Assert.That(h.Signal(zones[0]),Is.EqualTo(HazardSignal.Active));h.Advance(1,false);h.Advance(1,false);Assert.That(h.Signal(zones[0]),Is.EqualTo(HazardSignal.Safe));for(int i=0;i<4;i++)h.Advance(1,false);Assert.That(h.Signal(zones[0]),Is.EqualTo(HazardSignal.Warning));
            FacilityTests.Equip(ExpansionCatalog.Battery);FacilityTests.Aim(FacilityTests.FixtureDevices[0]);Assert.That(FacilityTests.FixtureFacility.Socket(FacilityTests.FixtureDevices[0]),Is.True);Assert.That(h.Signal(zones[0]),Is.EqualTo(HazardSignal.Safe));Assert.That(h.Signal(zones[1]),Is.EqualTo(HazardSignal.Active));FacilityTests.Equip(ExpansionCatalog.Insulator);FacilityTests.Aim(FacilityTests.FixtureDevices[2]);Assert.That(FacilityTests.FixtureFacility.TryIsolate(FacilityTests.FixtureDevices[2]),Is.True);Assert.That(h.Signal(zones[1]),Is.EqualTo(HazardSignal.Safe));
        }
        [UnityTest]public IEnumerator ExposureRecoveryGraceAndIncidentsUseRealVolumes()
        {
            yield return new EnterPlayMode();yield return CreateFixture();var h=FixtureHazards;Place(new Vector3(22,.15f,18));h.Advance(1,false);Assert.That(h.Pressure,Is.EqualTo(55));Assert.That(h.Incidents,Is.EqualTo(1));Place(new Vector3(5,.15f,12));h.Advance(1,false);Assert.That(h.Pressure,Is.EqualTo(55));h.Advance(.5f,false);Assert.That(h.Pressure,Is.EqualTo(49));Place(new Vector3(22,.15f,18));h.Advance(.2f,false);Assert.That(h.Incidents,Is.EqualTo(2));Assert.That(h.Pressure,Is.EqualTo(60).Within(.001));Assert.That(world.SafeDrop(new Vector3(22,.15f,18)),Is.False);Assert.That(world.SafeDrop(new Vector3(5,.15f,12)),Is.True);
        }
        [UnityTest]public IEnumerator BackpackPauseFreezesPhasePressureFacilityAndRestoresClock()
        {
            yield return new EnterPlayMode();yield return CreateFixture();var h=FixtureHazards;FacilityTests.Equip(ExpansionCatalog.Battery);FacilityTests.Aim(FacilityTests.FixtureDevices[0]);FacilityTests.FixtureFacility.Socket(FacilityTests.FixtureDevices[0]);float original=Time.timeScale;world.Backpack.Open();Assert.That(world.Pause.Paused,Is.True);h.Advance(1,world.Pause.Paused);FacilityTests.FixtureFacility.Advance(1,world.Pause.Paused);Assert.That(h.Phase,Is.Zero);Assert.That(h.Pressure,Is.Zero);Assert.That(FacilityTests.FixtureFacility.Charge,Is.EqualTo(90));world.Backpack.Close();Assert.That(Time.timeScale,Is.EqualTo(original));h.Advance(1,false);Assert.That(h.Phase,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator FailureFreezesActionsAndValidLoadRestoresWithoutRewardOrTeleport()
        {
            yield return new EnterPlayMode();yield return CreateFixture();Assert.That(persistence.Save(),Is.True,world.Message);Expose();var h=FixtureHazards;Assert.That(h.Pressure,Is.EqualTo(100));Assert.That(h.Failed,Is.True);Assert.That(world.ActionsAllowed,Is.False);Assert.That(world.Pause.Paused,Is.True);Assert.That(world.transform.position.x,Is.EqualTo(22));Assert.That(world.Inventory.Records.Count(r=>r.Location==InventoryItemLocation.Delivered),Is.Zero);Assert.That(persistence.Save(),Is.False);Assert.That(world.BeginDelivery(),Is.False);float phase=h.Phase;h.Advance(1,false);Assert.That(h.Phase,Is.EqualTo(phase));Assert.That(persistence.Load(),Is.True,world.Message);Assert.That(h.Failed,Is.False);Assert.That(h.Pressure,Is.Zero);Assert.That(world.ActionsAllowed,Is.True);Assert.That(world.Pause.Paused,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(world.transform.position.x,Is.EqualTo(5));
        }
        [UnityTest]public IEnumerator SavedExposureRestoresAndInconsistentFailureCannotMutateLiveState()
        {
            yield return new EnterPlayMode();yield return CreateFixture();Place(new Vector3(22,.15f,18));FixtureHazards.Advance(.5f,false);Place(new Vector3(5,.15f,12));var saved=persistence.Capture();Assert.That(FixtureHazards.Pressure,Is.EqualTo(27.5f));FixtureHazards.Advance(1,false);FixtureHazards.Advance(1,false);Assert.That(persistence.TryRestore(saved,out var error),Is.True,error);Assert.That(FixtureHazards.Pressure,Is.EqualTo(27.5f));var sections=saved.Sections;sections["hazards"]["failed"]=true;var bad=new ExpansionSnapshot(saved.Inventory,saved.Positions,saved.Player,saved.Yaw,saved.Pitch,sections);var inventory=world.Inventory;Assert.That(persistence.TryRestore(bad,out _),Is.False);Assert.That(world.Inventory,Is.SameAs(inventory));Assert.That(world.ActionsAllowed,Is.True);Assert.That(FixtureHazards.Failed,Is.False);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;yield return InventoryWorldTests.DestroyFixture();yield return new ExitPlayMode();}
    }
}
