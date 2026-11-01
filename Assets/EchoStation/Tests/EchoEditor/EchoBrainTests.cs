using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class EchoBrainTests
    {
        private Scene scene,previous;private bool background;private EchoBrain brain;private EchoNavigation motor;private Transform player;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-E10";
        [Serializable]private class Report{public bool passed;public int automaticWarnings;public string finalMode,lastWarning;public float positionDrift;}
        [Serializable]private class CooldownReport{public bool passed;public float warningInterval;public int automaticWarnings;}
        private IEnumerator Setup(bool automatic=true,IntentKind start=IntentKind.Follow)
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));var timer=System.Diagnostics.Stopwatch.StartNew();
            while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timed out.");yield return null;}SceneManager.SetActiveScene(scene);var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));motor=actor.GetComponent<EchoNavigation>();Assert.That(motor.TryBind(new Vector3(3.3f,0,-1.5f)),Is.True);actor.transform.rotation=Quaternion.Euler(0,90,0);motor.Agent.updateRotation=false;
            player=new GameObject("BrainPlayerFixture").transform;player.position=new Vector3(4.5f,.05f,-1.5f);brain=actor.GetComponent<EchoBrain>();brain.AutomaticWarningsEnabled=automatic;scene.GetPhysicsScene().Simulate(.02f);Assert.That(brain.Initialize(player,start),Is.True);Directory.CreateDirectory(Evidence);
        }
        private static IEnumerator Observe(float seconds)
        {var start=Time.time;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.time-start<seconds){if(timer.Elapsed.TotalSeconds>seconds+5)Assert.Fail("Game time stalled.");yield return R04ShadowProbe.Frame();}}
        [UnityTest]public IEnumerator E10_C01_AutomaticWarningInterruptsThenResumesFollowWithoutRepeatWithinCooldown()
        {
            yield return new EnterPlayMode();yield return Setup();var original=motor.Position;var timer=System.Diagnostics.Stopwatch.StartNew();
            while(brain.CurrentMode!=IntentKind.Warn){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Visible hazard did not trigger warning.");yield return R04ShadowProbe.Frame();}
            Assert.That(brain.LastWarningTargetId,Is.EqualTo("power_panel_01"));Assert.That(motor.State,Is.EqualTo(NavigationState.Idle));timer.Restart();
            while(brain.CurrentMode!=IntentKind.Follow){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Automatic warning did not restore Follow.");yield return R04ShadowProbe.Frame();}
            yield return Observe(2);var report=new Report{automaticWarnings=brain.AutomaticWarningCount,finalMode=brain.CurrentMode.ToString(),lastWarning=brain.LastWarningTargetId,positionDrift=Vector3.Distance(original,motor.Position)};
            report.passed=report.automaticWarnings==1&&brain.CurrentMode==IntentKind.Follow&&report.positionDrift<.01f;File.WriteAllText(Evidence+"/AutomaticWarning.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True);
        }
        [UnityTest]public IEnumerator E10_C02_NewUserWaitDuringWarningPreventsOldFollowFromResuming()
        {
            yield return new EnterPlayMode();yield return Setup();var timer=System.Diagnostics.Stopwatch.StartNew();while(brain.CurrentMode!=IntentKind.Warn){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("No initial automatic warning.");yield return R04ShadowProbe.Frame();}
            Assert.That(brain.TrySubmit(ScriptCommands.Wait(),out _),Is.True);var held=motor.Position;player.position=new Vector3(0,.05f,-6);yield return Observe(2);
            var report=new Report{automaticWarnings=brain.AutomaticWarningCount,finalMode=brain.CurrentMode.ToString(),positionDrift=Vector3.Distance(held,motor.Position)};report.passed=brain.CurrentMode==IntentKind.Wait&&report.positionDrift<.01f&&brain.AutomaticWarningCount==1;File.WriteAllText(Evidence+"/UserWaitOverridesResume.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True);
        }
        [UnityTest]public IEnumerator E10_C03_ScriptBrainWiresSixIntentsFourModesAndInvestigationScanWithLlmOff()
        {
            yield return new EnterPlayMode();yield return Setup(false,IntentKind.Wait);Assert.That(GameRuntimeOptions.UseLLM,Is.False);var messages=new List<string>();brain.Speech+=(text,refusal)=>messages.Add(text);
            Assert.That(brain.TrySubmit(ScriptCommands.Follow(),out _),Is.True);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Follow));brain.TrySubmit(ScriptCommands.Answer("记录已核对。"),out _);brain.TrySubmit(ScriptCommands.Refuse("我不能替你操作机关。"),out _);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Follow));brain.TrySubmit(ScriptCommands.Wait(),out _);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Wait));
            Assert.That(brain.TrySubmit(ScriptCommands.Investigate("missing_target"),out _),Is.False);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(brain.TrySubmit(ScriptCommands.Investigate("scan_mark_01"),out _),Is.True);var timer=System.Diagnostics.Stopwatch.StartNew();
            while(brain.CurrentMode!=IntentKind.Wait){if(timer.Elapsed.TotalSeconds>15)Assert.Fail("Brain investigation scan did not complete.");yield return R04ShadowProbe.Frame();scene.GetPhysicsScene().Simulate(.02f);}
            Assert.That(brain.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered("scan_mark_01"),Is.True);Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var hidden)&&hidden.GetComponent<ClueSource>().IsRevealed,Is.True);
            Assert.That(motor.TryBind(new Vector3(3.3f,0,-1.5f)),Is.True);motor.transform.rotation=Quaternion.Euler(0,90,0);motor.Agent.updateRotation=false;scene.GetPhysicsScene().Simulate(.02f);Assert.That(brain.TrySubmit(ScriptCommands.Warn("power_panel_01"),out _),Is.True);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Warn));timer.Restart();
            while(brain.CurrentMode!=IntentKind.Wait){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Manual warning did not end in Wait.");yield return R04ShadowProbe.Frame();}
            Assert.That(messages,Does.Contain("记录已核对。"));Assert.That(messages,Does.Contain("我不能替你操作机关。"));Assert.That(brain.AutomaticWarningCount,Is.Zero);
        }
        [UnityTest]public IEnumerator E10_C04_AutomaticWarningCanRepeatOnlyAfterEightSecondCooldown()
        {
            yield return new EnterPlayMode();yield return Setup();var timer=System.Diagnostics.Stopwatch.StartNew();while(brain.AutomaticWarningCount<1){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("No first warning.");yield return R04ShadowProbe.Frame();}
            var first=Time.time;timer.Restart();while(brain.AutomaticWarningCount<2){if(timer.Elapsed.TotalSeconds>12)Assert.Fail("Warning did not become eligible again after cooldown.");if(Time.time-first<7.99f)Assert.That(brain.AutomaticWarningCount,Is.EqualTo(1));yield return R04ShadowProbe.Frame();}
            var report=new CooldownReport{warningInterval=Time.time-first,automaticWarnings=brain.AutomaticWarningCount};report.passed=report.warningInterval>=7.99f&&report.warningInterval<8.5f&&report.automaticWarnings==2;File.WriteAllText(Evidence+"/WarningCooldown.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
        }
        [UnityTest]public IEnumerator E10_C05_ManualWarningDelaysAutomaticEchoButExplicitManualRepeatIsAllowed()
        {
            yield return new EnterPlayMode();yield return Setup(false,IntentKind.Wait);Assert.That(brain.TrySubmit(ScriptCommands.Warn("power_panel_01"),out _),Is.True);brain.AutomaticWarningsEnabled=true;yield return Observe(2.1f);
            var report=new Report{automaticWarnings=brain.AutomaticWarningCount,finalMode=brain.CurrentMode.ToString(),passed=brain.AutomaticWarningCount==0&&brain.CurrentMode==IntentKind.Wait};File.WriteAllText(Evidence+"/ManualWarningDedup.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
            Assert.That(brain.TrySubmit(ScriptCommands.Warn("power_panel_01"),out _),Is.True);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Warn));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(brain)brain.Shutdown();if(Application.isPlaying)
            {if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Brain fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
