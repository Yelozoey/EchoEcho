using System;
using System.Collections;
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
    public class EchoPresentationRuntimeTests
    {
        private Scene scene,previous;private bool background;private EchoBrain brain;
        [Serializable]private class Result{public bool passed,thinkingLight,redWarning,calmAfter;public float movingWheelDegrees,stoppedWheelDegrees;}
        [UnityTest]public IEnumerator E11_C01_ActualNavigationScanAndWarningDrivePresentation()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));var timer=System.Diagnostics.Stopwatch.StartNew();
            while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timeout.");yield return null;}SceneManager.SetActiveScene(scene);
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));var motor=actor.GetComponent<EchoNavigation>();var rig=actor.GetComponent<EchoRig>();var presentation=actor.GetComponent<EchoPresentation>();var scanner=actor.GetComponent<EchoScanController>().Scanner;
            Assert.That(motor.TryBind(new Vector3(0,0,-6)),Is.True);var player=new GameObject("PresentationPlayerFixture").transform;player.position=new Vector3(2,.05f,-1);brain=actor.GetComponent<EchoBrain>();brain.AutomaticWarningsEnabled=false;scene.GetPhysicsScene().Simulate(.02f);Assert.That(brain.Initialize(player,IntentKind.Follow),Is.True);
            var start=motor.Position;var wheel=rig.Wheels[0].localRotation;float turn=0;timer.Restart();
            while(Vector3.Distance(start,motor.Position)<.5f){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Follow did not move.");yield return R04ShadowProbe.Frame();turn=Mathf.Max(turn,Quaternion.Angle(wheel,rig.Wheels[0].localRotation));}
            Assert.That(brain.TrySubmit(ScriptCommands.Wait(),out _),Is.True);yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();wheel=rig.Wheels[0].localRotation;var started=Time.time;timer.Restart();float drift=0;
            while(Time.time-started<.5f){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Wait observation stalled.");yield return R04ShadowProbe.Frame();drift=Mathf.Max(drift,Quaternion.Angle(wheel,rig.Wheels[0].localRotation));}
            Assert.That(brain.TrySubmit(ScriptCommands.Investigate("scan_mark_01"),out _),Is.True);timer.Restart();bool thinking=false;
            while(brain.CurrentMode!=IntentKind.Wait)
            {
                if(timer.Elapsed.TotalSeconds>15)Assert.Fail("Actual investigation scan timed out.");scene.GetPhysicsScene().Simulate(.02f);yield return R04ShadowProbe.Frame();
                if(scanner.IsBusy&&presentation.Mood==EchoMood.Thinking&&presentation.ScanLight.enabled){thinking=true;Assert.That(rig.Status.sharedMaterial,Is.SameAs(rig.Thinking));}
            }
            yield return R04ShadowProbe.Frame();Assert.That(scanner.Ledger.HasDiscovered("scan_mark_01"),Is.True);Assert.That(presentation.ScanLight.enabled,Is.False);
            Assert.That(motor.TryBind(new Vector3(3.3f,0,-1.5f)),Is.True);actor.transform.rotation=Quaternion.Euler(0,90,0);motor.Agent.updateRotation=false;scene.GetPhysicsScene().Simulate(.02f);
            Assert.That(brain.TrySubmit(ScriptCommands.Warn("power_panel_01"),out _),Is.True);yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();var red=presentation.Mood==EchoMood.Alert&&rig.Status.sharedMaterial==rig.Alert&&rig.Status.enabled;timer.Restart();
            while(brain.CurrentMode!=IntentKind.Wait){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Warning did not finish.");yield return R04ShadowProbe.Frame();}yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();
            var result=new Result{movingWheelDegrees=turn,stoppedWheelDegrees=drift,thinkingLight=thinking,redWarning=red,calmAfter=presentation.Mood==EchoMood.Calm&&!presentation.ScanLight.enabled};result.passed=turn>10&&drift<.01f&&thinking&&red&&result.calmAfter;Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-E11");File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-E11/ActualPresentation.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(brain)brain.Shutdown();if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Presentation fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
