using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Echo;
using EchoStation.SalvageExpansion;
using EchoStation.SalvageExpansion.Tests;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace EchoStation.L01.Tests
{
    public sealed class ExpansionDialogueTests
    {
        ExpansionWorld world;ExpansionEcho echo;ExpansionDialoguePanel panel;ExpansionTools tools;ExpansionMission mission;ExpansionPersistence persistence;WorldTarget[] targets;NavMeshData mesh;NavMeshDataInstance meshInstance;
        IEnumerator Boot()
        {
            yield return HazardTests.CreateFixture();world=InventoryWorldTests.FixtureWorld;
            var source=new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,size=new Vector3(36,.2f,24),transform=Matrix4x4.TRS(new Vector3(18,-.1f,12),Quaternion.identity,Vector3.one),area=0};mesh=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),new List<NavMeshBuildSource>{source},new Bounds(new Vector3(18,0,12),new Vector3(40,10,28)),Vector3.zero,Quaternion.identity);Assert.That(mesh,Is.Not.Null);meshInstance=NavMesh.AddNavMeshData(mesh);
            targets=FacilityTests.FixtureDevices.Select(d=>{var go=d.gameObject;go.SetActive(false);var t=go.AddComponent<WorldTarget>();t.Configure(d.Id,d.DisplayName);go.AddComponent<EchoTargetPermissions>().Configure(true,false);go.SetActive(true);return t;}).ToArray();
            var robot=new GameObject("L01_TestEcho");robot.SetActive(false);robot.transform.SetParent(InventoryWorldTests.FixtureRoot.transform);robot.transform.position=new Vector3(14.8f,0,6);var agent=robot.AddComponent<NavMeshAgent>();agent.radius=.3f;agent.height=.6f;agent.stoppingDistance=.2f;var nav=robot.AddComponent<EchoNavigation>();robot.SetActive(true);
            echo=world.gameObject.AddComponent<ExpansionEcho>();echo.Configure(nav,targets);echo.Initialize();tools=world.gameObject.AddComponent<ExpansionTools>();tools.Initialize();mission=world.gameObject.AddComponent<ExpansionMission>();mission.Initialize();persistence=world.GetComponent<ExpansionPersistence>();panel=world.gameObject.AddComponent<ExpansionDialoguePanel>();panel.Initialize();
            world.Input.SetApplicationFocus(true);world.Input.SetPointerCaptured(true);Assert.That(Keyboard.current.native,Is.False);yield return R04ShadowProbe.Frame();
        }
        IEnumerator Press(params Key[] keys)
        {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return R04ShadowProbe.Frame();}
        IEnumerator Focus()
        {for(int i=0;i<6&&!panel.InputField.isFocused;i++)yield return R04ShadowProbe.Frame();Assert.That(panel.InputField.isFocused,Is.True);}
        void DiscoverValve(){FacilityTests.Aim(FacilityTests.FixtureDevices[3]);echo.Discover(targets[3]);}
        [UnityTest]public IEnumerator ActualTEntryPausesAndBlocksExplorationAndOtherHotkeys()
        {
            yield return new EnterPlayMode();yield return Boot();yield return Press(Key.T);yield return Focus();Assert.That(panel.IsOpen,Is.True);Assert.That(panel.ModeCaption,Does.Contain("LLM未接入"));Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(Time.timeScale,Is.Zero);Assert.That(world.Pause.Paused,Is.True);Assert.That(echo.Navigation.Agent.isStopped,Is.True);
            var before=world.transform.position;yield return Press(Key.W,Key.I,Key.R,Key.Tab,Key.E,Key.G);Assert.That(world.transform.position,Is.EqualTo(before));Assert.That(world.Backpack.IsOpen,Is.False);Assert.That(echo.MenuOpen,Is.False);Assert.That(mission.BoardOpen,Is.False);Assert.That(tools.Active,Is.False);Assert.That(panel.IsOpen,Is.True);
            panel.Close();Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(world.Input.CanPlay,Is.True);
        }
        [UnityTest]public IEnumerator ActualCompositionKeysDoNotCloseOrSubmitThenBatteryQueryStaysOpen()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(panel.Open(),Is.True);yield return Focus();var evt=IMECompositionEvent.Create(Keyboard.current.deviceId,"dian",InputState.currentTime);InputSystem.QueueEvent(ref evt);yield return R04ShadowProbe.Frame();yield return Press(Key.Enter,Key.Escape);Assert.That(panel.IsOpen,Is.True);Assert.That(panel.MessageCount,Is.Zero);
            evt=IMECompositionEvent.Create(Keyboard.current.deviceId,"",InputState.currentTime);InputSystem.QueueEvent(ref evt);InputSystem.QueueTextEvent(Keyboard.current,'电');InputSystem.QueueTextEvent(Keyboard.current,'量');yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();yield return Press(Key.Enter);
            Assert.That(panel.MessageCount,Is.EqualTo(2));Assert.That(panel.LastReply,Does.Contain("当前电量"));Assert.That(panel.IsOpen,Is.True);Assert.That(panel.InputField.text,Is.Empty);Assert.That(FacilityTests.FixtureFacility.ValveClosed,Is.False);
        }
        [UnityTest]public IEnumerator EditorNativeKeyboardUsesEngineImeAndCloseRestoresPreviousMode()
        {
            yield return new EnterPlayMode();yield return Boot();var synthetic=Keyboard.current;var native=InputSystem.devices.OfType<Keyboard>().FirstOrDefault(k=>k.native);if(native==null)Assert.Inconclusive("No real editor keyboard available for the engine IME path");var previousMode=UnityEngine.Input.imeCompositionMode;
            try{Assert.That(panel.Open(),Is.True);yield return Focus();Assert.That(panel.InputField.NativeImeBridgeActive,Is.False);native.MakeCurrent();yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(panel.InputField.NativeImeBridgeActive,Is.True);Assert.That(UnityEngine.Input.imeCompositionMode,Is.EqualTo(IMECompositionMode.On));panel.Close();Assert.That(UnityEngine.Input.imeCompositionMode,Is.EqualTo(previousMode));Assert.That(panel.InputField.NativeImeBridgeActive,Is.False);}
            finally{panel.Close();synthetic.MakeCurrent();UnityEngine.Input.imeCompositionMode=previousMode;}
        }
        [UnityTest]public IEnumerator BehaviorTextReleasesOnlyItsLeaseAndDispatchesWaitFollowInvestigationAndCancel()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(panel.Open(),Is.True);Assert.That(panel.SubmitText("原地待命"),Is.True);Assert.That(panel.IsOpen,Is.False);Assert.That(echo.Mode,Is.EqualTo(IntentKind.Wait));Assert.That(panel.LastReply,Does.StartWith("已收到待命"));Assert.That(world.Pause.Blocked,Is.False);
            Assert.That(panel.Open(),Is.True);panel.SubmitText("跟随我");Assert.That(echo.Mode,Is.EqualTo(IntentKind.Follow));Assert.That(panel.LastReply,Does.StartWith("已收到跟随"));DiscoverValve();Assert.That(panel.Open(),Is.True);panel.SubmitText("调查这个");Assert.That(panel.IsOpen,Is.False);Assert.That(echo.CurrentTarget,Is.EqualTo(targets[3].Id));Assert.That(panel.LastReply,Does.StartWith("准备调查"));Assert.That(FacilityTests.FixtureFacility.ValveClosed,Is.False);
            Assert.That(panel.Open(),Is.True);panel.SubmitText("取消");Assert.That(echo.CurrentTarget,Is.Null);Assert.That(echo.Mode,Is.EqualTo(IntentKind.Follow));Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator UnknownTextDoesNotExecuteAndTargetChoiceIsExplicit()
        {
            yield return new EnterPlayMode();yield return Boot();DiscoverValve();FacilityTests.Aim(FacilityTests.FixtureDevices[0]);echo.Discover(targets[0]);ulong generation=echo.Generation;Assert.That(panel.Open(),Is.True);panel.SubmitText("调查不存在的设备");Assert.That(panel.AwaitingTarget,Is.True);Assert.That(echo.Generation,Is.EqualTo(generation));Assert.That(panel.ChooseTarget(targets[3].Id),Is.True);Assert.That(echo.CurrentTarget,Is.EqualTo(targets[3].Id));Assert.That(panel.IsOpen,Is.False);
        }
        [UnityTest]public IEnumerator ReportsRequireActualScanAndKeepStaleWarningWithoutDeviceWrites()
        {
            yield return new EnterPlayMode();yield return Boot();DiscoverValve();
            Assert.That(panel.Open(),Is.True,"Open before unscanned query");panel.SubmitText("报告这个");Assert.That(panel.LastResult.Kind,Is.EqualTo(EchoTextKind.Reject));panel.Close();
            Assert.That(echo.Command(ScriptCommands.Investigate(targets[3].Id)),Is.True,"Scan dispatch: "+world.Message+" blocked="+world.Pause.Blocked+" focus="+world.Input.HasApplicationFocus);
            echo.Advance(.5f,false);echo.Advance(.5f,false);
            Assert.That(echo.TryReadKnownReport(targets[3].Id,out _,out _),Is.True,"Actual scan commits report; "+echo.LastSpeech);
            Assert.That(panel.Open(),Is.True,"Open after scan: "+world.Message+" blocked="+world.Pause.Blocked+" focus="+world.Input.HasApplicationFocus);
            panel.SubmitText("报告这个");Assert.That(panel.LastReply,Does.Contain("扳手"));panel.Close();
            FacilityTests.Equip(ExpansionCatalog.Wrench);FacilityTests.Aim(FacilityTests.FixtureDevices[3]);Assert.That(FacilityTests.FixtureFacility.TryCloseValve(FacilityTests.FixtureDevices[3]),Is.True);Assert.That(panel.Open(),Is.True);panel.SubmitText("报告这个");Assert.That(panel.LastReply,Does.StartWith("（报告已过时"));Assert.That(FacilityTests.FixtureFacility.ValveClosed,Is.True);
        }
        [UnityTest]public IEnumerator RToTextHasOnePauseAndBackpackValveAndBoardExcludeText()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(echo.OpenMenu(),Is.True);var button=world.GetComponentsInChildren<Button>(true).Single(b=>b.name=="文字通信 [T]");button.onClick.Invoke();Assert.That(echo.MenuOpen,Is.False);Assert.That(panel.IsOpen,Is.True);Assert.That(Time.timeScale,Is.Zero);Assert.That(world.Backpack.Open(),Is.False);Assert.That(mission.OpenBoard(),Is.False);panel.Close();Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(world.Backpack.Open(),Is.True);Assert.That(panel.Open(),Is.False);world.Backpack.Close();FacilityTests.Equip(ExpansionCatalog.Wrench);FacilityTests.Aim(FacilityTests.FixtureDevices[3]);Assert.That(tools.Request(FacilityTests.FixtureDevices[3]),Is.True);Assert.That(tools.Active,Is.True);Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(panel.Open(),Is.False);tools.Cancel();Assert.That(mission.OpenBoard(),Is.True);Assert.That(panel.Open(),Is.False);mission.CloseBoard();Assert.That(panel.Open(),Is.True);
        }
        [UnityTest]public IEnumerator HistoryCapsAt100ReusesRowsAndDoesNotEnterCanonicalEchoOrSaveSections()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(panel.Open(),Is.True);string echoBefore=echo.Capture().ToString();var sections=persistence.Capture().Sections.Properties().Select(p=>p.Name).ToArray();for(int i=0;i<56;i++)Assert.That(panel.SubmitText("电量"),Is.True);Assert.That(panel.MessageCount,Is.EqualTo(100));Assert.That(panel.AllocatedRows,Is.EqualTo(100));Assert.That(echo.Capture().ToString(),Is.EqualTo(echoBefore));Assert.That(persistence.Capture().Sections.Properties().Select(p=>p.Name),Is.EqualTo(sections));panel.Close();Assert.That(panel.Open(),Is.True);Assert.That(panel.MessageCount,Is.EqualTo(100));
        }
        [UnityTest]public IEnumerator FailedRestoreKeepsDraftAndSuccessfulRestoreClosesPendingWithoutAddingSaveSection()
        {
            yield return new EnterPlayMode();yield return Boot();var snapshot=persistence.Capture();Assert.That(panel.Open(),Is.True);panel.SubmitText("调查未知对象");panel.InputField.text="未提交草稿";Assert.That(panel.AwaitingTarget,Is.True);Assert.That(persistence.TryRestore(null,out _),Is.False);Assert.That(panel.IsOpen,Is.True);Assert.That(panel.InputField.text,Is.EqualTo("未提交草稿"));Assert.That(panel.AwaitingTarget,Is.True);
            Assert.That(persistence.TryRestore(snapshot,out var error),Is.True,error);Assert.That(panel.IsOpen,Is.False);Assert.That(panel.InputField.text,Is.Empty);Assert.That(panel.AwaitingTarget,Is.False);Assert.That(panel.MessageCount,Is.EqualTo(2));Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator ActualMouseSendSubmitsOnceAndFocusLossReleasesAllOwnedUiState()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(panel.Open(),Is.True);yield return Focus();InputSystem.QueueTextEvent(Keyboard.current,'电');InputSystem.QueueTextEvent(Keyboard.current,'量');yield return R04ShadowProbe.Frame();var point=RectTransformUtility.WorldToScreenPoint(null,panel.SendButton.transform.position);InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point}.WithButton(MouseButton.Left));yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return R04ShadowProbe.Frame();Assert.That(panel.MessageCount,Is.EqualTo(2));Assert.That(panel.IsOpen,Is.True);
            world.Input.SetApplicationFocus(false);yield return R04ShadowProbe.Frame();Assert.That(panel.IsOpen,Is.False);Assert.That(world.Pause.Paused,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));world.Input.SetApplicationFocus(true);
        }
        [UnityTest]public IEnumerator ActualHazardClockStopsDuringTextAndRenderedTextFitsAllocatedRects()
        {
            yield return new EnterPlayMode();yield return Boot();world.Motor.RestorePose(new Vector3(22,.15f,18),90,0);Physics.SyncTransforms();HazardTests.FixtureHazards.enabled=true;Assert.That(panel.Open(),Is.True);float pressure=HazardTests.FixtureHazards.Pressure,phase=HazardTests.FixtureHazards.Phase;yield return new WaitForSecondsRealtime(.15f);Assert.That(HazardTests.FixtureHazards.Pressure,Is.EqualTo(pressure));Assert.That(HazardTests.FixtureHazards.Phase,Is.EqualTo(phase));panel.SubmitText("电量");yield return R04ShadowProbe.Frame();
            foreach(var text in panel.Canvas.GetComponentsInChildren<Text>().Where(t=>t.isActiveAndEnabled&&!string.IsNullOrEmpty(t.text)))Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name+" clips");panel.Close();yield return new WaitForSecondsRealtime(.1f);Assert.That(HazardTests.FixtureHazards.Pressure,Is.GreaterThan(pressure));
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {if(!Application.isPlaying)yield break;if(panel)panel.Close();if(tools)tools.Cancel();if(mission)mission.CloseBoard();yield return InventoryWorldTests.DestroyFixture();if(meshInstance.valid)meshInstance.Remove();if(mesh)Object.Destroy(mesh);yield return new ExitPlayMode();}
    }
}
