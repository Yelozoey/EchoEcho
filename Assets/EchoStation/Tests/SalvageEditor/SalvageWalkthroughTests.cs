using System.Collections;
using EchoStation.Salvage.Editor;
using EchoStation.Salvage.Validation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Salvage.Tests
{
    public class SalvageWalkthroughTests
    {
        Scene old,scene;bool background,backgroundCaptured;
        IEnumerator Run(string mode,string folder=null)
        {
            Assert.That(Application.isPlaying,Is.True,"EnterPlayMode must be yielded by the top-level UnityTest.");if(!backgroundCaptured){background=Application.runInBackground;backgroundCaptured=true;}Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timeout=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timeout.Elapsed.TotalSeconds>8)Assert.Fail("Owned scene load timed out.");yield return null;}SceneManager.SetActiveScene(scene);
            folder=folder??"Docs/Tests/SalvagePrototype/Evidence/R1-V01/EditorRegression_"+System.Guid.NewGuid().ToString("N");var go=new GameObject("OwnedWalkthrough");SceneManager.MoveGameObjectToScene(go,scene);var driver=go.AddComponent<SalvageWalkthrough>();yield return driver.Run(mode,folder);
        }
        [UnityTest]public IEnumerator MainRouteCancelDropCompleteRestart(){yield return new EnterPlayMode();yield return Run("route");}
        [UnityTest]public IEnumerator AlternateRouteViaShortcutCompletes(){yield return new EnterPlayMode();yield return Run("alternate");}
        [UnityTest]public IEnumerator MixedStateSaveAndFreshSceneLoad()
        {
            yield return new EnterPlayMode();var folder="Docs/Tests/SalvagePrototype/Evidence/R1-V01/EditorCross_"+System.Guid.NewGuid().ToString("N");
            yield return Run("write",folder);SceneManager.SetActiveScene(old);var op=SceneManager.UnloadSceneAsync(scene);var timeout=System.Diagnostics.Stopwatch.StartNew();while(op!=null&&!op.isDone){if(timeout.Elapsed.TotalSeconds>8)Assert.Fail("Unload timeout");yield return null;}scene=default;
            yield return Run("read",folder);TestContext.Out.WriteLine("Fresh scene save/load evidence: "+folder);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}if(backgroundCaptured)Application.runInBackground=background;backgroundCaptured=false;yield return new ExitPlayMode();}
    }
}
