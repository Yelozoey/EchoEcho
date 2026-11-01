using System;
using System.Collections;
using System.IO;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Rendering;
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
    public class ScanWaveBindingTests
    {
        private Scene scene,previous;private bool background;
        [Serializable]private class Result{public bool passed,cancelledImmediately,revealed;public uint waveGenerations;public float expandingRadius;}
        [UnityTest]public IEnumerator E12_C01_ActualScanStartsOneWaveCancelStopsAndSuccessfulRepeatCompletes()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timeout.");yield return null;}SceneManager.SetActiveScene(scene);
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));Assert.That(actor.GetComponent<EchoNavigation>().TryBind(new Vector3(8.1f,0,2.1f)),Is.True);var scanner=actor.GetComponent<EchoScanController>().Scanner;var wave=actor.GetComponent<ScanWaveEmitter>().State;scene.GetPhysicsScene().Simulate(.02f);Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var target),Is.True);
            var origin=actor.GetComponent<EchoRig>().ScanOrigin.position;Assert.That(scanner.TryBegin(target),Is.True);Assert.That(wave.Active,Is.True);Assert.That(wave.Origin,Is.EqualTo(origin));var start=Time.time;timer.Restart();while(Time.time-start<.2f){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Wave clock stalled.");yield return R04ShadowProbe.Frame();}
            var result=new Result{expandingRadius=wave.Radius};scanner.Cancel();result.cancelledImmediately=!wave.Active;Assert.That(target.GetComponent<ClueSource>().IsRevealed,Is.False);Assert.That(scanner.TryBegin(target),Is.True);timer.Restart();
            while(scanner.IsBusy||wave.Active){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Actual scan or wave did not complete.");scene.GetPhysicsScene().Simulate(.02f);yield return R04ShadowProbe.Frame();}
            result.waveGenerations=wave.Generation;result.revealed=target.GetComponent<ClueSource>().IsRevealed;result.passed=result.expandingRadius>.9f&&result.cancelledImmediately&&result.waveGenerations==2&&result.revealed&&scanner.Ledger.HasDiscovered("scan_mark_01");Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-E12");File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-E12/ActualWaveBinding.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Wave binding fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
