using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class ToolTests
    {
        static ExpansionTools tools;static ExpansionWorld world;static FacilityController facility;
        static IEnumerator Boot(){yield return HazardTests.CreateFixture();world=InventoryWorldTests.FixtureWorld;facility=FacilityTests.FixtureFacility;tools=world.gameObject.AddComponent<ExpansionTools>();tools.Initialize();}
        static void Valve(){FacilityTests.Equip(ExpansionCatalog.Wrench);FacilityTests.Aim(FacilityTests.FixtureDevices[3]);Assert.That(tools.Request(FacilityTests.FixtureDevices[3]),Is.True);}
        [UnityTest]public IEnumerator ValveRequiresWrenchAndClockwiseArcRatherThanClickOrHold()
        {
            yield return new EnterPlayMode();yield return Boot();FacilityTests.Aim(FacilityTests.FixtureDevices[3]);Assert.That(tools.Request(FacilityTests.FixtureDevices[3]),Is.False);Valve();Assert.That(world.Pause.Blocked,Is.True);Assert.That(world.Pause.Paused,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));var c=tools.Center;Assert.That(tools.BeginStroke(c),Is.False);Assert.That(tools.BeginStroke(c+Vector2.right*100),Is.True);Assert.That(tools.DragStroke(c+Vector2.right*100),Is.True);Assert.That(facility.ValveClosed,Is.False);tools.DragStroke(c+new Vector2(70.71f,-70.71f));Assert.That(tools.Degrees,Is.EqualTo(45).Within(.1));Assert.That(facility.ValveClosed,Is.False);tools.DragStroke(c+Vector2.down*100);Assert.That(facility.ValveClosed,Is.True);Assert.That(tools.Active,Is.False);Assert.That(world.Pause.Blocked,Is.False);
        }
        [UnityTest]public IEnumerator ReleaseRegrabAndCancelNeverPartiallyCommitAndHazardsContinue()
        {
            yield return new EnterPlayMode();yield return Boot();Valve();var c=tools.Center;tools.BeginStroke(c+Vector2.right*100);tools.DragStroke(c+new Vector2(70.71f,-70.71f));tools.CancelStroke();Assert.That(tools.DragStroke(c+Vector2.down*100),Is.False);Assert.That(tools.Degrees,Is.EqualTo(45).Within(.1));float phase=HazardTests.FixtureHazards.Phase;HazardTests.FixtureHazards.Advance(1,world.Pause.Paused);Assert.That(HazardTests.FixtureHazards.Phase,Is.EqualTo(phase+1));tools.Cancel();Assert.That(facility.ValveClosed,Is.False);Assert.That(tools.Degrees,Is.Zero);Assert.That(world.Pause.Blocked,Is.False);
        }
        [UnityTest]public IEnumerator FocusLossCancelsAndSuccessfulLoadCancelsButInvalidLoadDoesNot()
        {
            yield return new EnterPlayMode();yield return Boot();var persistence=world.GetComponent<ExpansionPersistence>();var saved=persistence.Capture();Valve();Assert.That(persistence.TryRestore(saved,out var error),Is.True,error);Assert.That(tools.Active,Is.False);Assert.That(world.Pause.Blocked,Is.False);Valve();Assert.That(persistence.TryRestore(null,out _),Is.False);Assert.That(tools.Active,Is.True);world.Input.SetApplicationFocus(false);yield return null;yield return null;Assert.That(tools.Active,Is.False);Assert.That(world.Pause.Blocked,Is.False);Assert.That(facility.ValveClosed,Is.False);
        }
        [UnityTest]public IEnumerator InsulationAndClampsRequireIndividualAimCorrectToolAndPowerOff()
        {
            yield return new EnterPlayMode();yield return Boot();FacilityTests.Equip(ExpansionCatalog.Wrench);FacilityTests.Aim(FacilityTests.FixtureDevices[4]);Assert.That(tools.Request(FacilityTests.FixtureDevices[4]),Is.False);FacilityTests.Aim(FacilityTests.FixtureDevices[2]);Assert.That(tools.Request(FacilityTests.FixtureDevices[2]),Is.False);FacilityTests.Equip(ExpansionCatalog.Insulator);Assert.That(tools.Request(FacilityTests.FixtureDevices[2]),Is.True);Assert.That(facility.Clamps,Is.Zero);FacilityTests.Equip(ExpansionCatalog.Wrench);for(int i=4;i<7;i++){FacilityTests.Aim(FacilityTests.FixtureDevices[i]);Assert.That(tools.Request(FacilityTests.FixtureDevices[i]),Is.True);Assert.That(facility.Clamps,Is.EqualTo((1<<(i-3))-1));}Assert.That(facility.CanRemoveCore,Is.True);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;yield return InventoryWorldTests.DestroyFixture();yield return new ExitPlayMode();}
    }
}
