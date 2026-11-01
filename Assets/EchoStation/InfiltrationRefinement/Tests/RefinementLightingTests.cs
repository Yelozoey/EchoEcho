using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationRefinement.Editor;
using EchoStation.Salvage;
using EchoStation.Tests.Fixtures;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.InfiltrationRefinement.Tests
{
    public sealed class RefinementLightingTests
    {
        Scene previous,sample;Transform root;RefinementTestScope scope;
        void Edit(){previous=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(RefinementScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);root=RefinementScene.World(sample);}
        [Test]public void OwnedPipelineIsOrdinaryForwardHdrWithActualSsaoAndAces()
        {
            Edit();var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RefinementLighting.PipelinePath);var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RefinementLighting.RendererPath);
            Assert.That(new SerializedObject(renderer).FindProperty("m_RenderingMode").intValue,Is.EqualTo(0));Assert.That(pipeline.supportsHDR&&pipeline.useSRPBatcher,Is.True);Assert.That(RefinementLighting.Ao(renderer).isActive,Is.True);Assert.That(pipeline.shadowCascadeCount,Is.EqualTo(2));
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(RefinementLighting.VolumePath);Assert.That(profile.TryGet<Tonemapping>(out var tone)&&tone.mode.value==TonemappingMode.ACES,Is.True);var camera=root.GetComponentInChildren<Camera>();Assert.That(camera.allowHDR&&!camera.GetUniversalAdditionalCameraData().allowHDROutput,Is.True);
        }
        [Test]public void ActualBakeHasMapsBoundToStaticGeometryAndProbesForAllFourRobots()
        {
            Edit();Assert.That(LightmapSettings.lightmaps.Length,Is.GreaterThan(0));Assert.That(LightmapSettings.lightmaps.All(l=>l.lightmapColor&&l.lightmapDir),Is.True);
            Assert.That(root.GetComponentsInChildren<LevelFloor>().SelectMany(f=>f.GetComponentsInChildren<MeshRenderer>()).Count(r=>r.lightmapIndex>=0&&r.lightmapIndex<LightmapSettings.lightmaps.Length),Is.EqualTo(72));Assert.That(LightmapSettings.lightProbes.count,Is.GreaterThanOrEqualTo(216));
            Assert.That(Lightmapping.lightingSettings.maxBounces,Is.GreaterThanOrEqualTo(3));Assert.That(Lightmapping.lightingSettings.indirectScale,Is.EqualTo(1));foreach(var post in root.GetComponentsInChildren<LevelPost>())Assert.That(post.GetComponentsInChildren<Renderer>().All(r=>r.lightProbeUsage==LightProbeUsage.BlendProbes),Is.True);
            Assert.That(root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponentInParent<LevelFloor>()).All(r=>r.shadowCastingMode==ShadowCastingMode.TwoSided),Is.True,"Thin ceiling/floor must block lights from both sides");
        }
        [Test]public void EightOfflineReflectionBakesArePersistedAsLocalCustomCubemaps()
        {
            Edit();var probes=root.GetComponentsInChildren<ReflectionProbe>();Assert.That(probes.Length,Is.EqualTo(8));foreach(var p in probes){Assert.That(p.mode,Is.EqualTo(ReflectionProbeMode.Custom));Assert.That(p.customBakedTexture,Is.Not.Null);string path=AssetDatabase.GetAssetPath(p.customBakedTexture);Assert.That(path.StartsWith(RefinementScene.Generated+"/"),Is.True);Assert.That(new FileInfo(path).Length,Is.GreaterThan(1000));Assert.That(p.boxProjection&&p.hdr,Is.True);}
            string file=Directory.GetFiles(RefinementScene.Evidence,"ReflectionBakes_*.json").OrderBy(File.GetLastWriteTimeUtc).Last();var receipt=JArray.Parse(File.ReadAllText(file));Assert.That(receipt.Count,Is.EqualTo(8));Assert.That(receipt.All(r=>(bool)r["baked"]),Is.True);
        }
        [Test]public void LightBudgetAndCompletedBakeReceiptMatchActualScene()
        {
            Edit();var lights=root.GetComponentsInChildren<Light>();Assert.That(lights.Count(l=>l.lightmapBakeType==LightmapBakeType.Baked),Is.EqualTo(42));Assert.That(lights.Count(l=>l.lightmapBakeType==LightmapBakeType.Mixed),Is.EqualTo(1));Assert.That(lights.Count(l=>l.lightmapBakeType==LightmapBakeType.Realtime),Is.EqualTo(2));
            string file=Directory.GetFiles(RefinementScene.Evidence,"BakeJob_*.json").Where(p=>!p.EndsWith(".scope.json")).OrderBy(File.GetLastWriteTimeUtc).Last();var data=JObject.Parse(File.ReadAllText(file));Assert.That((bool)data["passed"]&&(bool)data["layoutRestored"],Is.True,data.ToString());
        }
        static double Difference(Color[] a,Color[] b)=>a.Select((c,i)=>(Math.Abs(c.r-b[i].r)+Math.Abs(c.g-b[i].g)+Math.Abs(c.b-b[i].b))/3).Average();
        [UnityTest,Timeout(180000)]public IEnumerator BakedLightingAoAndReflectionsChangeActualRenderedPixels()
        {
            yield return new EnterPlayMode();scope=new RefinementTestScope();previous=SceneManager.GetActiveScene();sample=EditorSceneManager.LoadSceneInPlayMode(RefinementScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!sample.isLoaded)yield return null;SceneManager.SetActiveScene(sample);root=RefinementScene.World(sample);root.GetComponentInChildren<SalvageMotor>().enabled=false;root.GetComponentInChildren<EchoStation.Player.PlayerInputReader>().enabled=false;foreach(var a in root.GetComponentsInChildren<Animator>())a.enabled=false;
            var camera=root.GetComponentInChildren<Camera>();camera.scene=sample;camera.transform.position=new Vector3(12,1.6f,-7);camera.transform.LookAt(new Vector3(12,1.1f,1));
            var profile=root.GetComponentInChildren<Volume>().profile;profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RefinementLighting.RendererPath);var ao=RefinementLighting.Ao(renderer);bool original=ao.isActive;var probes=root.GetComponentsInChildren<ReflectionProbe>();var intensities=probes.Select(p=>p.intensity).ToArray();float environment=RenderSettings.reflectionIntensity;
            var lights=root.GetComponentsInChildren<Light>();var enabled=lights.Select(l=>l.enabled).ToArray();var meshes=root.GetComponentsInChildren<MeshRenderer>();var indices=meshes.Select(r=>r.lightmapIndex).ToArray();var usage=meshes.Select(r=>r.lightProbeUsage).ToArray();var ambient=RenderSettings.ambientProbe;var mode=RenderSettings.ambientMode;var ambientColor=RenderSettings.ambientLight;
            string version=Guid.NewGuid().ToString("N").Substring(0,8);Color[] Capture(string name){using(var c=new SceneFrameCapture(camera))return c.Capture(RefinementScene.Evidence+"/Lighting_"+name+"_"+version+".png");}
            try
            {
                for(int i=0;i<6;i++)yield return null;var baseline=Capture("Baseline");ao.SetActive(false);renderer.SetDirty();for(int i=0;i<6;i++)yield return null;var noAo=Capture("NoAO");double aoDelta=Difference(baseline,noAo);ao.SetActive(true);renderer.SetDirty();for(int i=0;i<6;i++)yield return null;
                foreach(var p in probes)p.intensity=0;RenderSettings.reflectionIntensity=0;for(int i=0;i<5;i++)yield return null;var noReflection=Capture("NoReflection");double reflectionDelta=Difference(baseline,noReflection);
                foreach(var l in lights)l.enabled=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.ambientProbe=new SphericalHarmonicsL2();foreach(var r in meshes)r.lightProbeUsage=LightProbeUsage.Off;for(int i=0;i<6;i++)yield return null;var baked=Capture("BakedOnly");foreach(var r in meshes)r.lightmapIndex=-1;for(int i=0;i<6;i++)yield return null;var noBaked=Capture("NoBakedLighting");double bakedDelta=Difference(baked,noBaked);
                File.WriteAllText(RefinementScene.Evidence+"/LightingDeltas_"+version+".json",new JObject{{"aoMeanAbsolute",aoDelta},{"reflectionMeanAbsolute",reflectionDelta},{"bakedLightingMeanAbsolute",bakedDelta},{"width",1920},{"height",1080},{"animationsAndGrainDisabled",true},{"bakedTestIsTotalBakedContributionNotIsolatedIndirect",true}}.ToString());
                Assert.That(aoDelta,Is.GreaterThan(.00002),"AO must visibly contribute");Assert.That(reflectionDelta,Is.GreaterThan(.00002),"Offline probes must visibly contribute");Assert.That(bakedDelta,Is.GreaterThan(.001),"Actual baked light must visibly contribute");
            }
            finally
            {
                ao.SetActive(original);renderer.SetDirty();for(int i=0;i<probes.Length;i++)if(probes[i])probes[i].intensity=intensities[i];RenderSettings.reflectionIntensity=environment;for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=enabled[i];for(int i=0;i<meshes.Length;i++)if(meshes[i]){meshes[i].lightmapIndex=indices[i];meshes[i].lightProbeUsage=usage[i];}RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambientColor;RenderSettings.ambientProbe=ambient;
            }
        }
        [TearDown]public void CloseEdit(){if(Application.isPlaying)return;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);}
        [UnityTearDown]public IEnumerator ClosePlay(){if(!Application.isPlaying)yield break;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var u=SceneManager.UnloadSceneAsync(sample);while(!u.isDone)yield return null;}scope?.Dispose();scope=null;yield return new ExitPlayMode();}
    }
}
