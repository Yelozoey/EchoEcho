using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EchoStation.AI.Inference;
using EchoStation.Echo;
using EchoStation.L01;
using EchoStation.SalvageExpansion;
using EchoStation.SalvageExpansion.Tests;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace EchoStation.L02.Tests
{
    public class SceneQuestionPanelTests
    {
        ExpansionWorld world;ExpansionEcho echo;ExpansionDialoguePanel panel;SceneQuestionBridge bridge;WorldTarget[] targets;QuestionTestFactory factory;NavMeshData mesh;NavMeshDataInstance meshInstance;
        IEnumerator Wait(Func<bool> ready){float end=Time.realtimeSinceStartup+5;while(!ready()){Assert.That(Time.realtimeSinceStartup,Is.LessThan(end));yield return R04ShadowProbe.Frame();}}
        IEnumerator Boot(bool scan=true)
        {
            yield return HazardTests.CreateFixture();world=InventoryWorldTests.FixtureWorld;
            var s=new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,size=new Vector3(36,.2f,24),transform=Matrix4x4.TRS(new Vector3(18,-.1f,12),Quaternion.identity,Vector3.one),area=0};mesh=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),new List<NavMeshBuildSource>{s},new Bounds(new Vector3(18,0,12),new Vector3(40,10,28)),Vector3.zero,Quaternion.identity);meshInstance=NavMesh.AddNavMeshData(mesh);
            targets=FacilityTests.FixtureDevices.Select(d=>{var go=d.gameObject;go.SetActive(false);var t=go.AddComponent<WorldTarget>();t.Configure(d.Id,d.DisplayName);go.AddComponent<EchoTargetPermissions>().Configure(true,false);go.SetActive(true);return t;}).ToArray();
            var robot=new GameObject("L02_TestEcho");robot.SetActive(false);robot.transform.SetParent(InventoryWorldTests.FixtureRoot.transform);robot.transform.position=new Vector3(14.8f,0,6);var agent=robot.AddComponent<NavMeshAgent>();agent.radius=.3f;agent.height=.6f;agent.stoppingDistance=.2f;var nav=robot.AddComponent<EchoNavigation>();robot.SetActive(true);echo=world.gameObject.AddComponent<ExpansionEcho>();echo.Configure(nav,targets);echo.Initialize();panel=world.gameObject.AddComponent<ExpansionDialoguePanel>();panel.Initialize();factory=new QuestionTestFactory{Hold=true};bridge=world.gameObject.AddComponent<SceneQuestionBridge>();bridge.ConfigureTests(factory);bridge.Initialize();world.Input.SetApplicationFocus(true);world.Input.SetPointerCaptured(true);FacilityTests.Aim(FacilityTests.FixtureDevices[3]);Assert.That(echo.Discover(targets[3]),Is.True);
            if(scan){Assert.That(echo.Command(ScriptCommands.Investigate(targets[3].Id)),Is.True);echo.Advance(.5f,false);echo.Advance(.5f,false);Assert.That(echo.TryReadKnownReport(targets[3].Id,out _,out _),Is.True);}
            Assert.That(panel.Open(),Is.True);Assert.That(bridge.AIEnabled,Is.False);Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(bridge.SetAIEnabled(true),Is.True);yield return Wait(()=>bridge.Phase==RequestPhase.Ready);
        }
        [UnityTest]public IEnumerator QuestionUsesVisibleSourceWhilePausedAndNeverChangesDeviceOrInventory()
        {
            yield return new EnterPlayMode();yield return Boot();long rev=world.Inventory.Revision;var sections=world.GetComponent<ExpansionPersistence>().Capture().Sections.Properties().Select(p=>p.Name).ToArray();Assert.That(panel.SubmitText("这个应该如何处理？"),Is.True);yield return Wait(()=>factory.Entered);Assert.That(bridge.Busy,Is.True);Assert.That(Time.timeScale,Is.Zero);Assert.That(bridge.SourceText,Does.Contain("扳手"));Assert.That(panel.ModeCaption,Does.Contain("质量未达标"));factory.Hold=false;yield return Wait(()=>bridge.LastReply!=null);Assert.That(bridge.LastReply.Success,Is.True,bridge.LastReply.Error);Assert.That(panel.LastReply,Is.EqualTo("用扳手处理。"));Assert.That(world.Inventory.Revision,Is.EqualTo(rev));Assert.That(FacilityTests.FixtureFacility.ValveClosed,Is.False);Assert.That(world.GetComponent<ExpansionPersistence>().Capture().Sections.Properties().Select(p=>p.Name),Is.EqualTo(sections));foreach(var t in panel.Canvas.GetComponentsInChildren<Text>().Where(t=>t.isActiveAndEnabled&&!string.IsNullOrEmpty(t.text)))Assert.That(t.preferredHeight,Is.LessThanOrEqualTo(t.rectTransform.rect.height+1),t.name+" clips");
        }
        [UnityTest]public IEnumerator UnscannedBlocksModelAndSideTargetSelectionDoesNotStartInvestigation()
        {
            yield return new EnterPlayMode();yield return Boot(false);ulong generation=echo.Generation;var target=panel.Canvas.GetComponentsInChildren<Button>().Single(b=>b.name=="Target_"+targets[3].Id);target.onClick.Invoke();Assert.That(echo.Generation,Is.EqualTo(generation));panel.SubmitText("这里应该怎么处理？");yield return R04ShadowProbe.Frame();Assert.That(factory.Entered,Is.False);Assert.That(panel.LastReply,Does.Contain("尚无扫描报告"));Assert.That(bridge.SourceText,Does.Contain("尚未扫描"));Assert.That(panel.IsOpen,Is.True);
        }
        [UnityTest]public IEnumerator ScriptBehaviorClosesPanelAndCancelsLateModelReply()
        {
            yield return new EnterPlayMode();yield return Boot();panel.SubmitText("这个应该怎么处理？");yield return Wait(()=>factory.Entered);panel.SubmitText("原地待命");Assert.That(panel.IsOpen,Is.False);Assert.That(echo.Mode,Is.EqualTo(IntentKind.Wait));Assert.That(Time.timeScale,Is.EqualTo(1));factory.Hold=false;yield return Wait(()=>bridge.Phase==RequestPhase.Ready);Assert.That(bridge.LastReply,Is.Null);Assert.That(panel.LastReply,Does.StartWith("已收到待命"));
        }
        [UnityTest]public IEnumerator LoadCancelsRequestAndChangedReportRequiresNewScan()
        {
            yield return new EnterPlayMode();yield return Boot();var persistence=world.GetComponent<ExpansionPersistence>();var snapshot=persistence.Capture();panel.SubmitText("这个应该如何处理？");yield return Wait(()=>factory.Entered);Assert.That(persistence.TryRestore(snapshot,out var error),Is.True,error);Assert.That(panel.IsOpen,Is.False);factory.Hold=false;yield return Wait(()=>bridge.Phase==RequestPhase.Ready);Assert.That(bridge.LastReply,Is.Null);FacilityTests.Equip(ExpansionCatalog.Wrench);FacilityTests.Aim(FacilityTests.FixtureDevices[3]);Assert.That(FacilityTests.FixtureFacility.TryCloseValve(FacilityTests.FixtureDevices[3]),Is.True);factory.Entered=false;Assert.That(panel.Open(),Is.True);panel.SubmitText("这个应该如何处理？");Assert.That(panel.LastReply,Does.Contain("过时"));yield return R04ShadowProbe.Frame();Assert.That(factory.Entered,Is.False);
        }
        [UnityTest]public IEnumerator CancelAndGlobalOffRestoreScriptUiAndReleaseWorker()
        {
            yield return new EnterPlayMode();yield return Boot();panel.SubmitText("这个应该怎么处理？");yield return Wait(()=>factory.Entered);bridge.CancelButton.onClick.Invoke();Assert.That(bridge.Busy,Is.False);factory.Hold=false;yield return Wait(()=>bridge.Phase==RequestPhase.Ready);Assert.That(bridge.LastReply,Is.Null);GameRuntimeOptions.UseLLM=false;yield return R04ShadowProbe.Frame();Assert.That(bridge.AIEnabled,Is.False);Assert.That(panel.ModeCaption,Does.Contain("LLM未接入"));yield return Wait(()=>bridge.CleanupComplete);panel.SubmitText("电量");Assert.That(panel.LastReply,Does.Contain("当前电量"));Assert.That(panel.IsOpen,Is.True);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(factory!=null)factory.Hold=false;if(bridge){bridge.SetAIEnabled(false);yield return Wait(()=>bridge.CleanupComplete);}if(panel)panel.Close();yield return InventoryWorldTests.DestroyFixture();if(meshInstance.valid)meshInstance.Remove();if(mesh)Object.Destroy(mesh);yield return new ExitPlayMode();}
    }
}
