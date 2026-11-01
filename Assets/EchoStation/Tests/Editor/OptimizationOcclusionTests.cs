using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.Rendering
{
    public class OptimizationOcclusionTests
    {
        private AsyncOperation pendingUnload;
        private const string ReloadBackup="EchoStation.R05.Occlusion.ReloadOptionBackup";
        private const string OptionsBackup="EchoStation.R05.Occlusion.ReloadFlagsBackup";
        [Serializable] private class Result {public bool passed;public int opaqueBatchesOff,opaqueBatchesOn,opaqueBatchesReverse,opaqueBatchesInside;public R05OptimizationProbe.Result images;}
        private static int Opaques(FrameDebuggerEvidence.CaptureResult capture)=>capture.events.Count(e=>
            new[]{"SRPBatch","Mesh","StaticBatch","InstancedMesh","DynamicBatch"}.Contains(e.type) && e.label.Contains("DrawOpaqueObjects"));
        private static IEnumerator SettleCamera(){for(var i=0;i<10;i++)yield return R04ShadowProbe.Frame();}

        [Test] public void B01_SavedSceneActuallyReferencesNonemptyOcclusionPvs()
        {
            var path=OptimizationFixtureSetup.ScenePath;var sceneText=File.ReadAllText(path);
            Assert.That(sceneText,Does.Contain("smallestOccluder: 2"));Assert.That(sceneText,Does.Contain("smallestHole: 0.25"));Assert.That(sceneText,Does.Contain("backfaceThreshold: 100"));
            Assert.That(sceneText,Does.Contain("m_OcclusionCullingData: {fileID: 36300000, guid:"));
            var data=File.ReadAllText(path.Replace(".unity","/OcclusionCullingData.asset"));Assert.That(data,Does.Contain("m_PVSData:"));Assert.That(data.Length,Is.GreaterThan(1000));
        }
        [UnityTest] public IEnumerator B02_ActualOcclusionAndFrustumReduceOpaqueSubmissionsWithoutImageLoss()
        {
            if(SessionState.GetInt(ReloadBackup,-1)<0)SessionState.SetInt(ReloadBackup,EditorSettings.enterPlayModeOptionsEnabled?1:0);
            if(SessionState.GetInt(OptionsBackup,-1)<0)SessionState.SetInt(OptionsBackup,(int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled=false;
            yield return new EnterPlayMode();var background=Application.runInBackground;Application.runInBackground=true;
            var previous=SceneManager.GetActiveScene();
            var existing=SceneManager.GetSceneByPath(OptimizationFixtureSetup.ScenePath);
            if(existing.IsValid()&&existing.isLoaded)throw new InvalidOperationException("R05 fixture already loaded; not unloading live user scene data.");
            var scene=EditorSceneManager.LoadSceneInPlayMode(OptimizationFixtureSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);R05OptimizationProbe probe=null;
            try
            {
                probe=new R05OptimizationProbe(scene,AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath));
                var directory=Path.Combine(OptimizationFixtureSetup.EvidencePath,"Editor");yield return probe.RunOcclusionImages(directory);
                var report=new Result{images=probe.Report};FrameDebuggerEvidence.CaptureResult capture=null;
                probe.NearView(false);yield return SettleCamera();yield return FrameDebuggerEvidence.Capture(probe.RenderAndSynchronize,Path.Combine(directory,"FD-OcclusionOff.json"),r=>capture=r);
                report.opaqueBatchesOff=Opaques(capture);
                probe.NearView(true);yield return SettleCamera();yield return FrameDebuggerEvidence.Capture(probe.RenderAndSynchronize,Path.Combine(directory,"FD-OcclusionOn.json"),r=>capture=r);
                report.opaqueBatchesOn=Opaques(capture);
                probe.NearView(false);probe.Camera.transform.rotation=Quaternion.Euler(0,180,0);yield return SettleCamera();
                yield return FrameDebuggerEvidence.Capture(probe.RenderAndSynchronize,Path.Combine(directory,"FD-FrustumReverse.json"),r=>capture=r);
                report.opaqueBatchesReverse=Opaques(capture);
                probe.FarView(true);yield return SettleCamera();yield return FrameDebuggerEvidence.Capture(probe.RenderAndSynchronize,Path.Combine(directory,"FD-InsideFar.json"),r=>capture=r);
                report.opaqueBatchesInside=Opaques(capture);
                report.passed=report.images.passed && report.opaqueBatchesOff>report.opaqueBatchesOn && report.opaqueBatchesOn>0 &&
                    report.opaqueBatchesReverse<report.opaqueBatchesOff && report.opaqueBatchesInside>report.opaqueBatchesOn;
                File.WriteAllText(Path.Combine(directory,"OcclusionReport.json"),JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
            }
            finally
            {
                probe?.Dispose();if(previous.IsValid())SceneManager.SetActiveScene(previous);pendingUnload=SceneManager.UnloadSceneAsync(scene);Application.runInBackground=background;
            }
        }
        [UnityTearDown] public IEnumerator LeaveRuntime()
        {
            var background=Application.runInBackground;Application.runInBackground=true;
            try
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();
                while(pendingUnload!=null&&!pendingUnload.isDone)
                {if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("R05 scene unload did not complete.");yield return null;}
                if(Application.isPlaying)yield return new ExitPlayMode();
            }
            finally
            {
                pendingUnload=null;Application.runInBackground=background;
                var options=SessionState.GetInt(OptionsBackup,-1);
                if(options>=0){EditorSettings.enterPlayModeOptions=(EnterPlayModeOptions)options;SessionState.EraseInt(OptionsBackup);}
                var saved=SessionState.GetInt(ReloadBackup,-1);
                if(saved>=0){EditorSettings.enterPlayModeOptionsEnabled=saved==1;SessionState.EraseInt(ReloadBackup);}
            }
        }
    }
}
