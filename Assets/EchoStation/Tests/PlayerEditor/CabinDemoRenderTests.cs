using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class CabinDemoRenderTests
    {
        private Scene scene,previous;private bool background;private Renderer[] foreign;private bool[] states;private Light[] lights;private bool[] lightStates;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-P09";
        [Serializable]private class Visibility{public bool passed;public int offSubmissions,onSubmissions;public float imageDifference;}
        [UnityTest]public IEnumerator P09_B01_NewSceneBakeActuallyLightsThreeRoomsAndPvsPreservesVisibleImage()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(CabinDemoSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Demo render load timeout.");yield return null;}SceneManager.SetActiveScene(scene);yield return R04ShadowProbe.Frame();var demo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CabinDemo>()).Single();demo.Echo.gameObject.SetActive(false);demo.gameObject.SetActive(false);foreign=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene!=scene).ToArray();states=foreign.Select(r=>r.enabled).ToArray();foreach(var renderer in foreign)renderer.enabled=false;lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.gameObject.scene!=scene).ToArray();lightStates=lights.Select(l=>l.enabled).ToArray();foreach(var light in lights)light.enabled=false;var camera=scene.GetRootGameObjects().Single(r=>r.name=="Cabin_ValidationCamera").GetComponent<Camera>();camera.gameObject.SetActive(true);camera.enabled=false;var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ScanWaveSetup.RendererPath);Directory.CreateDirectory(Evidence+"/Lighting");
            using(var probe=new CabinLightingProbe(scene,rendererData)){yield return probe.Run(Evidence+"/Lighting");File.WriteAllText(Evidence+"/Lighting/ActualLighting.json",JsonUtility.ToJson(probe.Report,true));Assert.That(probe.Report.passed,Is.True,JsonUtility.ToJson(probe.Report,true));}
            foreach(var map in LightmapSettings.lightmaps){Assert.That(AssetDatabase.GetAssetPath(map.lightmapColor),Does.StartWith(CabinDemoSetup.DataFolder+"/"));Assert.That(AssetDatabase.GetAssetPath(map.lightmapDir),Does.StartWith(CabinDemoSetup.DataFolder+"/"));}
            using(var visibility=new CabinVisibilityProbe(scene,rendererData))
            {
                visibility.HiddenRoomView(false);for(var i=0;i<10;i++)yield return R04ShadowProbe.Frame();var off=visibility.Frames.Capture(Evidence+"/PvsOff.png");FrameDebuggerEvidence.CaptureResult a=null,b=null;yield return FrameDebuggerEvidence.Capture(visibility.Frames.RenderAndSynchronize,Evidence+"/FD-PvsOff.json",r=>a=r);visibility.HiddenRoomView(true);for(var i=0;i<10;i++)yield return R04ShadowProbe.Frame();var on=visibility.Frames.Capture(Evidence+"/PvsOn.png");yield return FrameDebuggerEvidence.Capture(visibility.Frames.RenderAndSynchronize,Evidence+"/FD-PvsOn.json",r=>b=r);var result=new Visibility{offSubmissions=a.events.Count(e=>e.type=="SRPBatch"&&e.label.Contains("DrawOpaqueObjects")),onSubmissions=b.events.Count(e=>e.type=="SRPBatch"&&e.label.Contains("DrawOpaqueObjects")),imageDifference=SceneFrameCapture.Difference(off,on)};result.passed=result.offSubmissions>=result.onSubmissions&&result.onSubmissions>0&&result.imageDifference<1e-6f;File.WriteAllText(Evidence+"/ActualVisibility.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
            }
        }
        [UnityTearDown]public IEnumerator After(){if(Application.isPlaying){if(foreign!=null)for(var i=0;i<foreign.Length;i++)if(foreign[i])foreign[i].enabled=states[i];if(lights!=null)for(var i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=lightStates[i];if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Demo render unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}}
    }
}
