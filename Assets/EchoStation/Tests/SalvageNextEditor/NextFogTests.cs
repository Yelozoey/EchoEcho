using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Rendering;
using EchoStation.Salvage;
using EchoStation.Salvage.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextFogTests
    {
        Scene old,scene;bool background;Volume volume;VolumeProfile instance;
        static RenderTexture[] FogTextures()=>Resources.FindObjectsOfTypeAll<RenderTexture>().Where(t=>t&&t.name.StartsWith("Next Local Fog",StringComparison.Ordinal)).ToArray();
        [Test]public void ShaderAndOwnedRendererUseThreeBoundedRegionsAndNoLegacyFog()
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(NextFogSetup.ShaderPath);Assert.That(shader,Is.Not.Null);Assert.That(ShaderUtil.ShaderHasError(shader),Is.False);var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath);Assert.That(renderer.rendererFeatures.Count(f=>f is NextFogFeature),Is.EqualTo(1));Assert.That(renderer.rendererFeatures.Any(f=>f is LocalFogFeature),Is.False);Assert.That(AssetDatabase.GetAssetPath(renderer.rendererFeatures.Single(f=>f is NextFogFeature)),Is.EqualTo(NextRendering.RendererPath));
            old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);try{var field=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NextFogField>()).Single();Assert.That(NextFogField.Steps,Is.EqualTo(16));Assert.That(field.Box(0).center,Is.EqualTo(new Vector3(12,1.3f,6)));Assert.That(field.Box(1).center,Is.EqualTo(new Vector3(12,1.3f,18)));Assert.That(field.Box(2).center,Is.EqualTo(new Vector3(24,1.3f,12)));for(int i=0;i<3;i++)Assert.That(field.Box(i).size,Is.EqualTo(new Vector3(3,2.6f,3)));Assert.That(field.GetComponent<ParticleSystem>(),Is.Null);Assert.Throws<ArgumentOutOfRangeException>(()=>field.DensityScale=float.NaN);Assert.Throws<ArgumentOutOfRangeException>(()=>field.LightStrength=5);}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);scene=default;}
        }
        static IEnumerator Frame(){int frame=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount==frame){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}}
        [UnityTest,Timeout(180000)]public IEnumerator RealHalfResolutionFogAndLiveSpotScatterSkipLowFarAndReleaseOnExit()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;int before=FogTextures().Length;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var motor=root.GetComponentInChildren<SalvageMotor>();motor.enabled=false;motor.RestorePose(new Vector3(9.5f,.1f,6),90,0);var echo=root.GetComponentInChildren<SalvageEcho>();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;motor.GetComponent<NextFocus>().enabled=false;var session=motor.GetComponent<NextSession>();var field=root.GetComponent<NextFogField>();var camera=motor.View.GetComponent<Camera>();camera.scene=scene;volume=root.GetComponentsInChildren<Volume>().Single(v=>v.name=="ES_Next_GlobalVolume");instance=volume.profile;instance.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;instance.TryGet<ChromaticAberration>(out var chromatic);chromatic.intensity.value=0;
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath);var ao=renderer.rendererFeatures.Single(f=>f.name.Contains("AmbientOcclusion"));bool aoActive=ao.isActive;var torch=motor.GetComponent<PlayerFlashlight>();bool wasOn=torch.IsOn;var lights=root.GetComponentsInChildren<Light>(true).Where(l=>l.lightmapBakeType==LightmapBakeType.Realtime).ToArray();var lightFlags=lights.Select(l=>l.enabled).ToArray();var flickers=root.GetComponentsInChildren<FaultLightFlicker>();var flickerFlags=flickers.Select(f=>f.enabled).ToArray();var id=Guid.NewGuid().ToString("N");Directory.CreateDirectory(NextFogSetup.Evidence);float fogDifference=0,scatterDifference=0;int during=0;
            try
            {
                ao.SetActive(false);foreach(var flicker in flickers)flicker.enabled=false;foreach(var light in lights)light.enabled=false;torch.SetOn(true);field.enabled=false;yield return Frame();Color[] off,ambient,lit;using(var capture=new SalvageFrameCapture(camera))off=capture.Capture(NextFogSetup.Evidence+"/FogOff_"+id+".png");field.enabled=true;field.LightStrength=0;Assert.That(field.ShouldRender(camera),Is.True);yield return Frame();using(var capture=new SalvageFrameCapture(camera))ambient=capture.Capture(NextFogSetup.Evidence+"/FogAmbient_"+id+".png");Assert.That(field.LastWidth,Is.EqualTo(960));Assert.That(field.LastHeight,Is.EqualTo(540));fogDifference=SalvageFrameCapture.Difference(off,ambient);
                field.LightStrength=1;yield return Frame();using(var capture=new SalvageFrameCapture(camera))lit=capture.Capture(NextFogSetup.Evidence+"/FogSpot_"+id+".png");scatterDifference=SalvageFrameCapture.Difference(ambient,lit);during=FogTextures().Length;File.WriteAllText(NextFogSetup.Evidence+"/FogAB_"+id+".json",new JObject{{"fogDifference",fogDifference},{"liveSpotScatterDifference",scatterDifference},{"halfWidth",field.LastWidth},{"halfHeight",field.LastHeight},{"steps",16},{"fogRenderTexturesDuring",during},{"textureNames",new JArray(FogTextures().Select(t=>t.name))}}.ToString());Assert.That(fogDifference,Is.GreaterThan(.0001f));Assert.That(scatterDifference,Is.GreaterThan(.00001f));Assert.That(during,Is.GreaterThan(before));
                int recorded=field.LastRecordedFrame;session.SetQuality(RecoveryQuality.Low);Assert.That(field.ShouldRender(camera),Is.False);yield return Frame();using(var capture=new SalvageFrameCapture(camera))capture.Capture(NextFogSetup.Evidence+"/Low_"+id+".png");Assert.That(field.LastRecordedFrame,Is.EqualTo(recorded));session.SetQuality(RecoveryQuality.Medium);camera.transform.position=new Vector3(-10,2,-10);camera.transform.LookAt(new Vector3(12,1,12));Assert.That(field.ShouldRender(camera),Is.False);yield return Frame();using(var capture=new SalvageFrameCapture(camera))capture.Capture(NextFogSetup.Evidence+"/Far_"+id+".png");Assert.That(field.LastRecordedFrame,Is.EqualTo(recorded));
            }
            finally{field.DensityScale=1;field.LightStrength=1;field.enabled=true;session.SetQuality(RecoveryQuality.Medium);ao.SetActive(aoActive);for(int i=0;i<flickers.Length;i++)if(flickers[i])flickers[i].enabled=flickerFlags[i];torch.SetOn(wasOn);for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=lightFlags[i];}
            if(volume)volume.profile=null;if(instance){foreach(var component in instance.components)if(component)Object.Destroy(component);Object.Destroy(instance);instance=null;}SceneManager.SetActiveScene(old);var unload=SceneManager.UnloadSceneAsync(scene);while(unload!=null&&!unload.isDone)yield return null;scene=default;yield return Frame();yield return Frame();var unused=Resources.UnloadUnusedAssets();while(!unused.isDone)yield return null;int after=FogTextures().Length;File.WriteAllText(NextFogSetup.Evidence+"/Release_"+id+".json",new JObject{{"before",before},{"during",during},{"after",after},{"fieldDestroyed",field==null},{"viewDestroyed",camera==null}}.ToString());Assert.That(field==null&&camera==null,Is.True);Assert.That(after,Is.LessThanOrEqualTo(before));
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(volume)volume.profile=null;if(instance){foreach(var component in instance.components)if(component)Object.Destroy(component);Object.Destroy(instance);instance=null;}if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
