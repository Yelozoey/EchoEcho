using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.Echo;
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
    public sealed class NextLightingTests
    {
        Scene old,scene;bool background;Volume noiseVolume;VolumeProfile noiseProfile;
        [Test]public void PaintedMaskOnlyChangesMetalChannelAndKeepsSourceAoAndSmoothness()
        {
            var rows=JArray.Parse(File.ReadAllText(NextLighting.Evidence+"/PaintedMasks_A.json"));Assert.That(rows.Count,Is.GreaterThan(0));
            foreach(var row in rows){var source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);var derived=new Texture2D(2,2,TextureFormat.RGBA32,false,true);try{Assert.That(source.LoadImage(File.ReadAllBytes(row["source"].Value<string>())),Is.True);Assert.That(derived.LoadImage(File.ReadAllBytes(row["derived"].Value<string>())),Is.True);var a=source.GetPixels32();var b=derived.GetPixels32();Assert.That(b.Length,Is.EqualTo(a.Length));for(int i=0;i<a.Length;i++)if(b[i].r!=(byte)Mathf.RoundToInt(a[i].r*.15f)||b[i].g!=a[i].g||b[i].b!=a[i].b||b[i].a!=a[i].a)Assert.Fail("Painted packed texture changed unexpected channel at "+i);}finally{Object.DestroyImmediate(source);Object.DestroyImmediate(derived);}}
        }
        static void FinitePixels(Texture source)
        {
            var target=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);var previous=RenderTexture.active;var texture=new Texture2D(source.width,source.height,TextureFormat.RGBAFloat,false,true);
            try{Graphics.Blit(source,target);RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,source.width,source.height),0,0);texture.Apply();foreach(var p in texture.GetPixels())if(!float.IsFinite(p.r)||!float.IsFinite(p.g)||!float.IsFinite(p.b))Assert.Fail("Non-finite baked texture texel "+source.name);}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(texture);}
        }
        [Test]public void ReloadedStaticMapsAllReceiversAndPersistentDistinctReflectionProbes()
        {
            old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var settings=Lightmapping.lightingSettings;Assert.That(AssetDatabase.GetAssetPath(settings),Is.EqualTo(NextLighting.SettingsPath));Assert.That(settings.bakedGI&&!settings.realtimeGI&&!settings.autoGenerate,Is.True);Assert.That(settings.lightmapMaxSize,Is.EqualTo(512));var maps=LightmapSettings.lightmaps;Assert.That(maps.Length,Is.GreaterThan(0));
                foreach(var map in maps){Assert.That(map.lightmapColor&&map.lightmapDir,Is.True);Assert.That(AssetDatabase.GetAssetPath(map.lightmapColor),Does.StartWith(FacilityAssets.Root));FinitePixels(map.lightmapColor);FinitePixels(map.lightmapDir);}
                var receivers=root.GetComponentsInChildren<RoomVisual>().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).ToArray();foreach(var renderer in receivers){var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;Assert.That(mesh.uv2.Length,Is.EqualTo(mesh.vertexCount),renderer.name);Assert.That(renderer.lightmapIndex,Is.InRange(0,maps.Length-1),renderer.name);Assert.That(renderer.lightmapScaleOffset.x*renderer.lightmapScaleOffset.y,Is.GreaterThan(0));}
                Assert.That(LightmapSettings.lightProbes.count,Is.GreaterThanOrEqualTo(180));Assert.That(LightmapSettings.lightProbes.bakedProbes.Any(p=>Mathf.Abs(p[0,0])+Mathf.Abs(p[1,0])+Mathf.Abs(p[2,0])>.001f),Is.True);
                var reflections=root.GetComponentsInChildren<ReflectionProbe>();Assert.That(reflections.Length,Is.EqualTo(3));var hashes=reflections.Select(probe=>{Assert.That(probe.mode,Is.EqualTo(ReflectionProbeMode.Custom));Assert.That(probe.customBakedTexture,Is.Not.Null);using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(probe.customBakedTexture))));}).ToArray();Assert.That(hashes.Distinct().Count(),Is.EqualTo(3));
                var result=new JObject{{"atlases",maps.Length},{"receiversMapped",receivers.Length},{"probes",LightmapSettings.lightProbes.count},{"distinctReflections",3}};Directory.CreateDirectory(NextLighting.Evidence);File.WriteAllText(NextLighting.Evidence+"/Reload_"+Guid.NewGuid().ToString("N")+".json",result.ToString());
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);scene=default;}
        }
        IEnumerator Boot()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");root.GetComponentInChildren<SalvageMotor>().enabled=false;var echo=root.GetComponentInChildren<SalvageEcho>();var brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);brain.AutomaticWarnings=false;brain.ResetBase(IntentKind.Wait);
            var focus=root.GetComponentInChildren<NextFocus>();if(focus)focus.enabled=false;
            var fog=root.GetComponent<NextFogField>();if(fog)fog.enabled=false;
            noiseVolume=root.GetComponentsInChildren<Volume>().First(v=>v.isGlobal);noiseProfile=noiseVolume.profile;if(noiseProfile.TryGet<FilmGrain>(out var grain))grain.intensity.value=0;if(noiseProfile.TryGet<ChromaticAberration>(out var chromatic))chromatic.intensity.value=0;
            var camera=root.GetComponentInChildren<Camera>();string beforeScene=camera.scene.path;camera.scene=scene;File.WriteAllText(NextLighting.Evidence+"/CameraScene_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"beforePath",beforeScene},{"afterPath",camera.scene.path},{"gameObjectScene",camera.gameObject.scene.path}}.ToString());
            SphericalHarmonicsL2 sample;LightProbes.GetInterpolatedProbe(new Vector3(5,.6f,10),null,out sample);File.WriteAllText(NextLighting.Evidence+"/RuntimeState_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"maps",LightmapSettings.lightmaps.Length},{"probes",LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0},{"mappedReceivers",root.GetComponentsInChildren<RoomVisual>().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).Count(r=>r.lightmapIndex>=0&&r.lightmapIndex<LightmapSettings.lightmaps.Length)},{"probeL0",new JArray(sample[0,0],sample[1,0],sample[2,0])},{"pipeline",GraphicsSettings.currentRenderPipeline.name},{"mode",LightmapSettings.lightmapsMode.ToString()}}.ToString());
        }
        static IEnumerator Frame(){int initial=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount==initial){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}}
        [UnityTest]public IEnumerator StaticBakeRemainsWithBakedLightsDisabledAndMapRemovalChangesActualImage()
        {
            yield return new EnterPlayMode();yield return Boot();var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var lights=root.GetComponentsInChildren<Light>(true);var flags=lights.Select(l=>l.enabled).ToArray();var maps=LightmapSettings.lightmaps;var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath);var ao=renderer.rendererFeatures.Single(f=>f.name.Contains("AmbientOcclusion"));bool active=ao.isActive;var camera=root.GetComponentInChildren<Camera>();camera.transform.position=new Vector3(7,1.6f,8);camera.transform.LookAt(new Vector3(9,.3f,19));var id=Guid.NewGuid().ToString("N");Color[] on,off,noMaps;
            try
            {
                ao.SetActive(false);foreach(var light in lights)if(light.lightmapBakeType!=LightmapBakeType.Baked)light.enabled=false;yield return Frame();using(var capture=new SalvageFrameCapture(camera))on=capture.Capture(NextLighting.Evidence+"/BakedOn_"+id+".png");foreach(var light in lights)light.enabled=false;yield return Frame();using(var capture=new SalvageFrameCapture(camera))off=capture.Capture(NextLighting.Evidence+"/BakedLightsOff_"+id+".png");
                float staticDifference=SalvageFrameCapture.Difference(on,off);LightmapSettings.lightmaps=Array.Empty<LightmapData>();yield return Frame();using(var capture=new SalvageFrameCapture(camera))noMaps=capture.Capture(NextLighting.Evidence+"/NoMaps_"+id+".png");float contribution=SalvageFrameCapture.Difference(off,noMaps);File.WriteAllText(NextLighting.Evidence+"/StaticAB_"+id+".json",new JObject{{"bakedLightDisableDifference",staticDifference},{"lightmapRemovalDifference",contribution},{"SSAOIsolated",true}}.ToString());Assert.That(staticDifference,Is.LessThan(.0001f));Assert.That(contribution,Is.GreaterThan(.001f));
            }
            finally{LightmapSettings.lightmaps=maps;for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=flags[i];ao.SetActive(active);}
        }
        [UnityTest]public IEnumerator WestNorthAndBothPartitionFacesHaveActualBakedIrradiance()
        {
            yield return new EnterPlayMode();yield return Boot();var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var lights=root.GetComponentsInChildren<Light>(true);var flags=lights.Select(l=>l.enabled).ToArray();var camera=root.GetComponentInChildren<Camera>();var positions=new[]{new Vector3(3,1.6f,12),new Vector3(6,1.6f,21),new Vector3(10,1.6f,12),new Vector3(14,1.6f,12)};var targets=new[]{new Vector3(0,1.5f,12),new Vector3(6,1.5f,24),new Vector3(12,1.5f,12),new Vector3(12,1.5f,12)};var values=new JArray();var id=Guid.NewGuid().ToString("N");
            try
            {
                foreach(var light in lights)light.enabled=false;
                for(int i=0;i<positions.Length;i++){camera.transform.position=positions[i];camera.transform.LookAt(targets[i]);yield return Frame();Color[] pixels;using(var capture=new SalvageFrameCapture(camera))pixels=capture.Capture(NextLighting.Evidence+"/Wall_"+i+"_"+id+".png");double sum=0;foreach(int x in new[]{948,960,972})foreach(int y in new[]{528,540,552}){var p=pixels[y*1920+x];sum+=(p.r+p.g+p.b)/3;}values.Add(new JObject{{"position",positions[i].ToString()},{"target",targets[i].ToString()},{"meanCenterRgb",sum/9}});}
                File.WriteAllText(NextLighting.Evidence+"/WallFaces_"+id+".json",values.ToString());foreach(var value in values)Assert.That(value["meanCenterRgb"].Value<double>(),Is.GreaterThan(.001),value.ToString());
            }
            finally{for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=flags[i];}
        }
        [UnityTest]public IEnumerator ActualDynamicProbeIrradianceIncreasesAcrossZeroOneTwoMultipliers()
        {
            yield return new EnterPlayMode();yield return Boot();var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var lights=root.GetComponentsInChildren<Light>(true);var flags=lights.Select(l=>l.enabled).ToArray();var probes=LightmapSettings.lightProbes;var original=probes.bakedProbes;var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath);var ao=rendererData.rendererFeatures.Single(f=>f.name.Contains("AmbientOcclusion"));bool active=ao.isActive;var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="NextDynamicProbeWitness";sphere.transform.position=new Vector3(5,.6f,10);sphere.transform.localScale=Vector3.one*.8f;Object.Destroy(sphere.GetComponent<Collider>());var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.5f,.5f,.5f));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.1f);material.SetFloat("_EnvironmentReflections",0);material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");var witness=sphere.GetComponent<MeshRenderer>();witness.sharedMaterial=material;witness.receiveGI=ReceiveGI.LightProbes;witness.lightProbeUsage=LightProbeUsage.BlendProbes;witness.reflectionProbeUsage=ReflectionProbeUsage.Off;var camera=root.GetComponentInChildren<Camera>();camera.transform.position=new Vector3(5,1.1f,8);camera.transform.LookAt(sphere.transform.position);var values=new JArray();double[] energy=new double[3];var id=Guid.NewGuid().ToString("N");
            try
            {
                foreach(var light in lights)light.enabled=false;ao.SetActive(false);
                for(int scale=0;scale<3;scale++)
                {
                    var data=(SphericalHarmonicsL2[])original.Clone();for(int i=0;i<data.Length;i++)for(int c=0;c<3;c++)for(int n=0;n<9;n++)data[i][c,n]*=scale;probes.bakedProbes=data;yield return Frame();Color[] pixels;using(var capture=new SalvageFrameCapture(camera))pixels=capture.Capture(NextLighting.Evidence+"/Probe_"+scale+"_"+id+".png");double sum=0;foreach(int x in new[]{948,960,972})foreach(int y in new[]{528,540,552}){var p=pixels[y*1920+x];sum+=(p.r+p.g+p.b)/3;}energy[scale]=sum/9;values.Add(new JObject{{"scale",scale},{"meanCenterRgb",energy[scale]},{"frame",Time.frameCount}});
                }
                File.WriteAllText(NextLighting.Evidence+"/DynamicAB_"+id+".json",new JObject{{"samples",values},{"monotonic",energy[1]>energy[0]&&energy[2]>energy[1]},{"reflectionIsolated",true}}.ToString());Assert.That(energy[1]-energy[0],Is.GreaterThan(.00001));Assert.That(energy[2]-energy[1],Is.GreaterThan(.00001));
            }
            finally{probes.bakedProbes=original;for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=flags[i];ao.SetActive(active);Object.Destroy(sphere);Object.Destroy(material);}
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(noiseVolume)noiseVolume.profile=null;if(noiseProfile){foreach(var component in noiseProfile.components)if(component)Object.Destroy(component);Object.Destroy(noiseProfile);noiseProfile=null;}if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
