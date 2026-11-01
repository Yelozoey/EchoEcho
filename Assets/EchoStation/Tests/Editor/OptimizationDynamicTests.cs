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
    public class OptimizationDynamicTests
    {
        private AsyncOperation pendingUnload;
        [Serializable]private class Result{public bool passed;public int absentSrpBatches,presentSrpBatches,movedSrpBatches;public float visibilityDifference,movementDifference;}
        private static int Count(FrameDebuggerEvidence.CaptureResult capture)=>capture.events.Count(e=>e.type=="SRPBatch" && e.label.Contains("DrawOpaqueObjects"));
        private static float Difference(Color[] a,Color[] b){double n=0;for(var i=0;i<a.Length;i++)n+=(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b))/3;return(float)(n/a.Length);}
        [Test]public void C01_DynamicProxiesAreNotStaticMergedInstancedOrPropertyBlocked()
        {
            Assert.That(File.ReadAllText(OptimizationDynamicSetup.ScenePath),Does.Contain("m_OcclusionCullingData: {fileID: 0}"));
            var scene=EditorSceneManager.OpenPreviewScene(OptimizationDynamicSetup.ScenePath);
            try
            {
                var group=scene.GetRootGameObjects().Single(o=>o.name=="R05_DynamicProxies");var items=group.GetComponentsInChildren<MeshRenderer>(true);Assert.That(items.Length,Is.EqualTo(3));
                foreach(var item in items)
                {
                    Assert.That(GameObjectUtility.GetStaticEditorFlags(item.gameObject),Is.EqualTo((StaticEditorFlags)0));Assert.That(item.HasPropertyBlock(),Is.False);
                    Assert.That(item.sharedMaterial.enableInstancing,Is.False);Assert.That(item.sharedMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [UnityTest]public IEnumerator C02_MovingDynamicMeshesActuallySubmitThroughSrpBatcher()
        {
            yield return new EnterPlayMode();var background=Application.runInBackground;Application.runInBackground=true;
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.LoadSceneInPlayMode(OptimizationDynamicSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);R05OptimizationProbe probe=null;
            try
            {
                var directory=Path.Combine(OptimizationFixtureSetup.EvidencePath,"Dynamic");Directory.CreateDirectory(directory);
                probe=new R05OptimizationProbe(scene,AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath));probe.NearView(true);
                var group=scene.GetRootGameObjects().Single(o=>o.name=="R05_DynamicProxies");group.SetActive(false);yield return R04ShadowProbe.Frame();var absent=probe.Capture();
                FrameDebuggerEvidence.CaptureResult capture=null;var result=new Result();
                yield return FrameDebuggerEvidence.Capture(probe.RenderAndSynchronize,Path.Combine(directory,"FD-Absent.json"),r=>capture=r);result.absentSrpBatches=Count(capture);
                group.SetActive(true);yield return R04ShadowProbe.Frame();var present=probe.Capture(Path.Combine(directory,"01_Present.png"));result.visibilityDifference=Difference(absent,present);
                yield return FrameDebuggerEvidence.Capture(probe.RenderAndSynchronize,Path.Combine(directory,"FD-Present.json"),r=>capture=r);result.presentSrpBatches=Count(capture);
                foreach(Transform item in group.transform)item.position+=new Vector3(.75f,0,.35f);
                yield return R04ShadowProbe.Frame();result.movementDifference=Difference(present,probe.Capture(Path.Combine(directory,"02_Moved.png")));
                yield return FrameDebuggerEvidence.Capture(probe.RenderAndSynchronize,Path.Combine(directory,"FD-Moved.json"),r=>capture=r);result.movedSrpBatches=Count(capture);
                result.passed=result.visibilityDifference>.001f && result.movementDifference>.001f && result.presentSrpBatches>result.absentSrpBatches && result.movedSrpBatches>=result.presentSrpBatches;
                File.WriteAllText(Path.Combine(directory,"BatcherReport.json"),JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
            }
            finally{probe?.Dispose();if(previous.IsValid())SceneManager.SetActiveScene(previous);pendingUnload=SceneManager.UnloadSceneAsync(scene);Application.runInBackground=background;}
        }
        [UnityTearDown]public IEnumerator LeaveRuntime()
        {
            var background=Application.runInBackground;Application.runInBackground=true;
            try
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();
                while(pendingUnload!=null&&!pendingUnload.isDone)
                {if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("R05 scene unload did not complete.");yield return null;}
            }
            finally{pendingUnload=null;Application.runInBackground=background;}
            if(Application.isPlaying)yield return new ExitPlayMode();
        }
    }
}
