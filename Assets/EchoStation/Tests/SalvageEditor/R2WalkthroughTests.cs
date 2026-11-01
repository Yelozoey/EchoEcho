using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Salvage.Editor;
using EchoStation.Salvage.Validation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.Salvage.Tests
{
    public class R2WalkthroughTests
    {
        Scene old,scene;bool background,backgroundCaptured;SalvageR2Walkthrough driver;string folder;
        IEnumerator Boot(){Assert.That(Application.isPlaying,Is.True);if(!backgroundCaptured){background=Application.runInBackground;backgroundCaptured=true;}Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Route load timeout");yield return null;}SceneManager.SetActiveScene(scene);driver=new GameObject("ExplicitR2RouteFixture").AddComponent<SalvageR2Walkthrough>();folder=Path.GetFullPath("Library/EchoStation/R2Walkthrough/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);yield return null;}
        [UnityTest]public IEnumerator NormalRouteUsesActualRMouseScanECarryAndExplicitEnding(){yield return new EnterPlayMode();yield return Boot();yield return driver.Run("route",folder);var r=JsonUtility.FromJson<SalvageR2Walkthrough.Result>(File.ReadAllText(Path.Combine(folder,"route_Result.json")));Assert.That(r.passed&&r.inputRestored&&!r.useLLM,Is.True);Assert.That(r.scannedBeforeRestart,Is.EqualTo(3));Assert.That(r.actions,Does.Contain("cancel_investigation_verified").And.Contain("restart_verified"));}
        [UnityTest]public IEnumerator AlternateRouteUsesShortcutAndDifferentThreeItems(){yield return new EnterPlayMode();yield return Boot();yield return driver.Run("alternate",folder);var r=JsonUtility.FromJson<SalvageR2Walkthrough.Result>(File.ReadAllText(Path.Combine(folder,"alternate_Result.json")));Assert.That(r.passed&&r.inputRestored&&!r.useLLM,Is.True);Assert.That(r.scannedBeforeRestart,Is.EqualTo(3));Assert.That(r.actions,Does.Contain("scanned_cargo_c").And.Contain("scanned_cargo_f").And.Contain("scanned_cargo_b"));}
        [UnityTest]public IEnumerator MixedCollaborationSurvivesFreshSceneAndCompletes()
        {
            yield return new EnterPlayMode();yield return Boot();var shared=folder;yield return driver.Run("write",shared);driver.Cleanup();SceneManager.SetActiveScene(old);var op=SceneManager.UnloadSceneAsync(scene);var timer=System.Diagnostics.Stopwatch.StartNew();while(op!=null&&!op.isDone){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Fresh scene unload timeout");yield return null;}scene=default;yield return Boot();yield return driver.Run("read",shared);var r=JsonUtility.FromJson<SalvageR2Walkthrough.Result>(File.ReadAllText(Path.Combine(shared,"read_Result.json")));Assert.That(r.passed&&r.completed&&r.inputRestored&&!r.useLLM,Is.True);Assert.That(r.count,Is.EqualTo(3));Assert.That(r.points,Is.EqualTo(12));Assert.That(r.known,Is.EqualTo(4));Assert.That(r.actions,Does.Contain("cross_process_loaded_mixed_state"));TestContext.Out.WriteLine("Fresh scene write/read: "+shared);
        }
        [UnityTest]public IEnumerator DepletedEchoCannotScanButPlayerCanCompleteEntireRecovery()
        {
            yield return new EnterPlayMode();yield return Boot();driver.StartDepleted=true;yield return driver.Run("route",folder);var r=JsonUtility.FromJson<SalvageR2Walkthrough.Result>(File.ReadAllText(Path.Combine(folder,"route_Result.json")));Assert.That(r.passed&&r.inputRestored&&!r.useLLM,Is.True);Assert.That(r.scannedBeforeRestart,Is.Zero);Assert.That(r.actions,Does.Contain("unscanned_pickup_cargo_a").And.Contain("unscanned_pickup_cargo_b").And.Contain("unscanned_pickup_cargo_d").And.Contain("explicit_completion"));
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(driver)driver.Cleanup();if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}if(backgroundCaptured)Application.runInBackground=background;backgroundCaptured=false;yield return new ExitPlayMode();}
    }
}
