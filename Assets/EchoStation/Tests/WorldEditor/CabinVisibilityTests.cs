using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.World
{
    public class CabinVisibilityTests
    {
        private const string Key="EchoStation.Cabin.VisibilityReload";private AsyncOperation pending;
        [Serializable]private class Result{public bool passed;public int offBatches,onBatches,insideBatches;public float imageDifference;}
        private static int Batches(FrameDebuggerEvidence.CaptureResult result)=>result.events.Count(e=>e.type=="SRPBatch"&&e.label.Contains("DrawOpaqueObjects"));
        private static IEnumerator Settle(){for(var i=0;i<10;i++)yield return R04ShadowProbe.Frame();}
        [Test]public void B301_CabinReferencesSavedPvsData()
        {
            Assert.That(File.ReadAllText(CabinGreyboxSetup.ScenePath),Does.Contain("m_OcclusionCullingData: {fileID: 36300000, guid:"));
            Assert.That(new FileInfo(CabinGreyboxSetup.ScenePath.Replace(".unity","/OcclusionCullingData.asset")).Length,Is.GreaterThan(1000));
        }
        [UnityTest]public IEnumerator B302_ActualCabinOcclusionPreservesImageAndRecordsOpaquePath()
        {
            if(SessionState.GetInt(Key+"Enabled",-1)<0)SessionState.SetInt(Key+"Enabled",EditorSettings.enterPlayModeOptionsEnabled?1:0);
            if(SessionState.GetInt(Key+"Flags",-1)<0)SessionState.SetInt(Key+"Flags",(int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled=false;yield return new EnterPlayMode();var background=Application.runInBackground;Application.runInBackground=true;
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);CabinVisibilityProbe probe=null;
            try
            {
                var directory=Path.Combine(CabinGreyboxSetup.EvidencePath,"EditorVisibility");Directory.CreateDirectory(directory);
                probe=new CabinVisibilityProbe(scene,AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath));
                var result=new Result();FrameDebuggerEvidence.CaptureResult capture=null;
                probe.HiddenRoomView(false);yield return Settle();var off=probe.Frames.Capture(Path.Combine(directory,"01_Off.png"));
                yield return FrameDebuggerEvidence.Capture(probe.Frames.RenderAndSynchronize,Path.Combine(directory,"FD-Off.json"),r=>capture=r);result.offBatches=Batches(capture);
                probe.HiddenRoomView(true);yield return Settle();result.imageDifference=SceneFrameCapture.Difference(off,probe.Frames.Capture(Path.Combine(directory,"02_On.png")));
                yield return FrameDebuggerEvidence.Capture(probe.Frames.RenderAndSynchronize,Path.Combine(directory,"FD-On.json"),r=>capture=r);result.onBatches=Batches(capture);
                probe.InsideStorageView();yield return Settle();probe.Frames.Capture(Path.Combine(directory,"03_Storage.png"));
                yield return FrameDebuggerEvidence.Capture(probe.Frames.RenderAndSynchronize,Path.Combine(directory,"FD-Storage.json"),r=>capture=r);result.insideBatches=Batches(capture);
                // A single SRP batch can contain all the room draws. Exact draw reduction
                // is verified by the separate Windows normal-frame recorder, not this count.
                result.passed=result.offBatches>=result.onBatches&&result.onBatches>0&&result.insideBatches>0&&result.imageDifference<1e-6f;
                File.WriteAllText(Path.Combine(directory,"VisibilityReport.json"),JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
            }
            finally{probe?.Dispose();if(previous.IsValid())SceneManager.SetActiveScene(previous);pending=SceneManager.UnloadSceneAsync(scene);Application.runInBackground=background;}
        }
        [UnityTearDown]public IEnumerator LeaveRuntime()
        {
            var background=Application.runInBackground;Application.runInBackground=true;
            try
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();while(pending!=null&&!pending.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Cabin unload timed out.");yield return null;}
                if(Application.isPlaying)yield return new ExitPlayMode();
            }
            finally
            {
                pending=null;Application.runInBackground=background;var flags=SessionState.GetInt(Key+"Flags",-1);if(flags>=0){EditorSettings.enterPlayModeOptions=(EnterPlayModeOptions)flags;SessionState.EraseInt(Key+"Flags");}
                var enabled=SessionState.GetInt(Key+"Enabled",-1);if(enabled>=0){EditorSettings.enterPlayModeOptionsEnabled=enabled==1;SessionState.EraseInt(Key+"Enabled");}
            }
        }
    }
}
