using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class ExpansionEchoTests
    {
        static NavMeshData mesh;static NavMeshDataInstance instance;static ExpansionEcho echo;static ExpansionWorld world;static WorldTarget[] targets;
        static IEnumerator Boot()
        {
            yield return HazardTests.CreateFixture();world=InventoryWorldTests.FixtureWorld;var source=new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,size=new Vector3(36,.2f,24),transform=Matrix4x4.TRS(new Vector3(18,-.1f,12),Quaternion.identity,Vector3.one),area=0};mesh=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),new System.Collections.Generic.List<NavMeshBuildSource>{source},new Bounds(new Vector3(18,0,12),new Vector3(40,10,28)),Vector3.zero,Quaternion.identity);Assert.That(mesh,Is.Not.Null);instance=NavMesh.AddNavMeshData(mesh);
            targets=FacilityTests.FixtureDevices.Select(d=>{var go=d.gameObject;go.SetActive(false);var target=go.AddComponent<WorldTarget>();target.Configure(d.Id,d.DisplayName);go.AddComponent<EchoTargetPermissions>().Configure(true,false);go.SetActive(true);return target;}).ToArray();
            var robot=new GameObject("Echo_Module");robot.SetActive(false);robot.transform.SetParent(InventoryWorldTests.FixtureRoot.transform);robot.transform.position=new Vector3(14.8f,0,6);var agent=robot.AddComponent<NavMeshAgent>();agent.radius=.3f;agent.height=.6f;agent.stoppingDistance=.2f;var nav=robot.AddComponent<EchoNavigation>();robot.SetActive(true);echo=world.gameObject.AddComponent<ExpansionEcho>();echo.Configure(nav,targets);echo.Initialize();
        }
        static void ScanValve(){FacilityTests.Aim(FacilityTests.FixtureDevices[3]);echo.Discover(targets[3]);var delta=targets[3].transform.position-world.Motor.View.position;string ray=string.Join(",",Physics.RaycastAll(world.Motor.View.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider.name));Assert.That(echo.Command(ScriptCommands.Investigate(targets[3].Id)),Is.True,world.Message+" discovered="+echo.DiscoveredCount+" target="+targets[3].Registered+" distance="+delta.magnitude+" angle="+Vector3.Angle(world.Motor.View.forward,delta)+" hits="+ray);echo.Advance(.5f,false);echo.Advance(.5f,false);}
        [UnityTest]public IEnumerator UnknownTargetsRefusedAndScanReadsStateWithoutDeviceMutation()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(echo.Command(ScriptCommands.Investigate(targets[3].Id)),Is.False);ScanValve();Assert.That(echo.Report(targets[3].Id),Does.Contain("扳手"));Assert.That(FacilityTests.FixtureFacility.ValveClosed,Is.False);Assert.That(echo.Battery.Charge,Is.LessThan(93));Assert.That(echo.Mode,Is.EqualTo(IntentKind.Follow));Assert.That(echo.Command(ScriptCommands.Investigate("not_owned")),Is.False);
        }
        [UnityTest]public IEnumerator NewDeviceStateInvalidatesOldReportAndRepeatedScanIsFree()
        {
            yield return new EnterPlayMode();yield return Boot();ScanValve();float before=echo.Battery.Charge;FacilityTests.Equip(ExpansionCatalog.Wrench);FacilityTests.Aim(FacilityTests.FixtureDevices[3]);FacilityTests.FixtureFacility.TryCloseValve(FacilityTests.FixtureDevices[3]);Assert.That(echo.Report(targets[3].Id),Does.Contain("已过时"));Assert.That(echo.Command(ScriptCommands.Investigate(targets[3].Id)),Is.True);echo.Advance(.5f,false);echo.Advance(.5f,false);Assert.That(echo.Report(targets[3].Id),Does.Contain("已关闭"));Assert.That(echo.Report(targets[3].Id),Does.Not.Contain("已过时"));Assert.That(before-echo.Battery.Charge,Is.LessThan(.3f));
        }
        [UnityTest]public IEnumerator CancellationAndReplacementCannotCommitOldScanOrSpendBattery()
        {
            yield return new EnterPlayMode();yield return Boot();FacilityTests.Aim(FacilityTests.FixtureDevices[3]);echo.Discover(targets[3]);echo.Command(ScriptCommands.Investigate(targets[3].Id));echo.Advance(.5f,false);ulong generation=echo.Generation;echo.Command(ScriptCommands.Wait());echo.Advance(.6f,false);Assert.That(echo.Generation,Is.GreaterThan(generation));Assert.That(echo.Report(targets[3].Id),Does.Contain("尚无"));Assert.That(echo.Battery.Charge,Is.GreaterThan(99));Assert.That(echo.Mode,Is.EqualTo(IntentKind.Wait));echo.CancelTask();Assert.That(echo.Mode,Is.EqualTo(IntentKind.Wait));
        }
        [UnityTest]public IEnumerator SaveLoadKeepsKnowledgeBaseModeAndCancelsRunningTaskAndMenu()
        {
            yield return new EnterPlayMode();yield return Boot();ScanValve();echo.Command(ScriptCommands.Wait());var persistence=world.GetComponent<ExpansionPersistence>();var saved=persistence.Capture();echo.Command(ScriptCommands.Investigate(targets[3].Id));Assert.That(echo.OpenMenu(),Is.True);Assert.That(persistence.TryRestore(saved,out var error),Is.True,error);Assert.That(echo.MenuOpen,Is.False);Assert.That(echo.Mode,Is.EqualTo(IntentKind.Wait));Assert.That(echo.CurrentTarget,Is.Null);Assert.That(echo.Report(targets[3].Id),Does.Contain("扳手"));Assert.That(world.Pause.Blocked,Is.False);
        }
        [UnityTest]public IEnumerator EmptyBatteryRefusesFirstScanButFollowAndDockChargingRemainAvailable()
        {
            yield return new EnterPlayMode();yield return Boot();echo.Battery.Restore(0,true);ScanValve();Assert.That(echo.Report(targets[3].Id),Does.Contain("尚无"));Assert.That(echo.Battery.FollowSpeed,Is.EqualTo(1.5f));Assert.That(FacilityTests.FixtureFacility.ValveClosed,Is.False);Assert.That(echo.Navigation.TryBind(new Vector3(5,0,12)),Is.True);echo.Advance(.5f,false);Assert.That(echo.Battery.Charge,Is.GreaterThan(8));
        }
        [UnityTest]public IEnumerator RealWallStillOccludesDiscoveryAfterIgnoringOwnPlayerCollider()
        {
            yield return new EnterPlayMode();yield return Boot();FacilityTests.Aim(FacilityTests.FixtureDevices[3]);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(InventoryWorldTests.FixtureRoot.transform);wall.transform.position=world.Motor.View.position+world.Motor.View.forward;Physics.SyncTransforms();Assert.That(echo.Discover(targets[3]),Is.False);Assert.That(echo.Command(ScriptCommands.Investigate(targets[3].Id)),Is.False);Object.Destroy(wall);yield return null;FacilityTests.Aim(FacilityTests.FixtureDevices[3]);echo.Discover(targets[3]);Assert.That(echo.Command(ScriptCommands.Investigate(targets[3].Id)),Is.True,world.Message);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;yield return InventoryWorldTests.DestroyFixture();if(instance.valid)instance.Remove();if(mesh)Object.Destroy(mesh);yield return new ExitPlayMode();}
    }
}
