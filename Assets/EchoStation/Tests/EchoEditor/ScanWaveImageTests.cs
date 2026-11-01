using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class ScanWaveImageTests
    {
        private Scene scene,previous;private bool background,pipelineOverridden;private RenderPipelineAsset oldQuality,oldDefault;private Material surface;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-E12";
        [Serializable]private class Result{public bool passed;public float imageDifference,radiusDifference,nearBlueDelta,farBlueDelta,skyDelta,occludedBlueDelta;public int activePasses,idlePasses;}
        private static Color Sample(Color[] pixels,Camera camera,Vector3 world)
        {
            var v=camera.WorldToViewportPoint(world);var x=Mathf.RoundToInt(v.x*1920);var y=Mathf.RoundToInt(v.y*1080);Color color=Color.clear;for(var dy=-2;dy<=2;dy++)for(var dx=-2;dx<=2;dx++)color+=pixels[(y+dy)*1920+x+dx];return color/25;
        }
        [UnityTest]public IEnumerator E12_B01_ActualDepthShellChangesOnlyMatchingSurfacesAndUsesOneFullscreenPass()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("ScanWaveImageFixture");SceneManager.SetActiveScene(scene);Directory.CreateDirectory(Evidence);
            oldDefault=GraphicsSettings.defaultRenderPipeline;oldQuality=QualitySettings.renderPipeline;var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ScanWaveSetup.PipelinePath);Assert.That(pipeline,Is.Not.Null);pipelineOverridden=true;GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            var camera=new GameObject("WaveCamera").AddComponent<Camera>();camera.scene=scene;camera.transform.position=new Vector3(0,3,-6);camera.transform.LookAt(new Vector3(0,.45f,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.005f,.008f,.012f);camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().allowHDROutput=false;camera.fieldOfView=60;camera.aspect=1920f/1080;camera.nearClipPlane=.05f;camera.farClipPlane=40;camera.GetUniversalAdditionalCameraData().SetRenderer(0);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            surface=new Material(Shader.Find("Universal Render Pipeline/Unlit"));surface.SetColor("_BaseColor",new Color(.05f,.06f,.07f));var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="DepthFloor";floor.transform.position=new Vector3(0,-.1f,0);floor.transform.localScale=new Vector3(10,.2f,10);floor.GetComponent<Renderer>().sharedMaterial=surface;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="DepthWall";wall.transform.position=new Vector3(0,1,3);wall.transform.localScale=new Vector3(8,2,.2f);wall.GetComponent<Renderer>().sharedMaterial=surface;
            var emitter=new GameObject("SingleWaveEmitter").AddComponent<ScanWaveEmitter>();emitter.enabled=false;var view=camera.gameObject.AddComponent<ScanWaveView>();view.Source=emitter;
            // Sample immediately before explicit render requests so game-clock Update cannot advance the radius between controls.
            emitter.enabled=true;yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();
            using(var frames=new SceneFrameCapture(camera))
            {
                emitter.State.Cancel();var baseline=frames.Capture(Evidence+"/WaveOff.png");emitter.State.Begin(new Vector3(0,.8f,0));emitter.State.Tick(.3f);var on=frames.Capture(Evidence+"/WaveRadius1_5.png");
                var near=new Vector3(Mathf.Sqrt(1.5f*1.5f-.8f*.8f),0,0);var far=new Vector3(-3,0,0);var result=new Result{imageDifference=SceneFrameCapture.Difference(baseline,on),nearBlueDelta=Sample(on,camera,near).b-Sample(baseline,camera,near).b,farBlueDelta=Sample(on,camera,far).b-Sample(baseline,camera,far).b,skyDelta=Mathf.Abs(on[1070*1920+960].b-baseline[1070*1920+960].b)};
                emitter.State.Begin(new Vector3(0,.8f,0));emitter.State.Tick(.6f);var expanded=frames.Capture(Evidence+"/WaveRadius3.png");result.radiusDifference=SceneFrameCapture.Difference(on,expanded);
                FrameDebuggerEvidence.CaptureResult active=null,idle=null;
                yield return FrameDebuggerEvidence.Capture(()=>{emitter.State.Begin(new Vector3(0,.8f,0));emitter.State.Tick(.3f);frames.RenderAndSynchronize();},Evidence+"/WaveActiveFrameDebugger.json",r=>active=r);
                yield return FrameDebuggerEvidence.Capture(()=>{emitter.State.Cancel();frames.RenderAndSynchronize();},Evidence+"/WaveIdleFrameDebugger.json",r=>idle=r);
                result.activePasses=active.events.Count(e=>e.label.Contains(ScanWaveFeature.PassName)&&e.type=="DrawProcedural");result.idlePasses=idle.events.Count(e=>e.label.Contains(ScanWaveFeature.PassName)&&e.type=="DrawProcedural");
                var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="OpaqueDepthOccluder";blocker.transform.position=new Vector3(.85f,1,-2);blocker.transform.localScale=new Vector3(1,1,.4f);blocker.GetComponent<Renderer>().sharedMaterial=surface;emitter.State.Cancel();var blockedOff=frames.Capture();emitter.State.Begin(new Vector3(0,.8f,0));emitter.State.Tick(.3f);var blockedOn=frames.Capture(Evidence+"/WaveOccluded.png");result.occludedBlueDelta=Sample(blockedOn,camera,near).b-Sample(blockedOff,camera,near).b;
                result.passed=result.imageDifference>.001f&&result.radiusDifference>.001f&&result.nearBlueDelta>.15f&&Mathf.Abs(result.farBlueDelta)<.002f&&result.skyDelta<.002f&&Mathf.Abs(result.occludedBlueDelta)<.002f&&result.activePasses==1&&result.idlePasses==0;File.WriteAllText(Evidence+"/WaveImages.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));Assert.That(scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ParticleSystem>(true)),Is.Empty);
            }
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying){if(pipelineOverridden){GraphicsSettings.defaultRenderPipeline=oldDefault;QualitySettings.renderPipeline=oldQuality;pipelineOverridden=false;}if(surface)Object.Destroy(surface);if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Wave fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
