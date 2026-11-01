using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
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
    public sealed class RefinementLightSurfaceTests
    {
        Scene previous,sample;RefinementTestScope scope;
        [Test]public void ActualLampFacesAndWallWashesHaveOwnedGeometryAndDetachedLightingData()
        {
            previous=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(RefinementScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);var root=RefinementScene.World(sample);var faces=root.GetComponentsInChildren<RefinementLightSurface>();Assert.That(faces.Length,Is.EqualTo(50));Assert.That(faces.Count(f=>f.Kind=="Ceiling"),Is.EqualTo(36));Assert.That(faces.Count(f=>f.Kind=="Wall"),Is.EqualTo(6));Assert.That(faces.Count(f=>f.Kind=="Gate"),Is.EqualTo(8));
            foreach(var face in faces){var r=face.GetComponent<MeshRenderer>();Assert.That(r.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.True);Assert.That(r.sharedMaterial.GetColor("_EmissionColor").maxColorComponent,Is.GreaterThan(1));Assert.That(AssetDatabase.GetAssetPath(face.GetComponent<MeshFilter>().sharedMesh).StartsWith(RefinementScene.Generated+"/"),Is.True);}
            Assert.That(root.GetComponentsInChildren<ArtSampleModel>().Count(m=>m.Role=="RoofStrut"),Is.EqualTo(4));Assert.That(AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset).StartsWith(RefinementScene.Root+"/"),Is.True,"Must not keep the old scene's bake as final");Assert.That(LightmapSettings.lightmaps.All(m=>AssetDatabase.GetAssetPath(m.lightmapColor).StartsWith(RefinementScene.Root+"/")),Is.True);
            var lights=root.GetComponentsInChildren<Light>();Assert.That(lights.Count(l=>l.lightmapBakeType==LightmapBakeType.Realtime),Is.EqualTo(2));Assert.That(lights.Count(l=>l.lightmapBakeType==LightmapBakeType.Realtime&&l.shadows!=LightShadows.None),Is.EqualTo(1));
            var p=AssetDatabase.LoadAssetAtPath<VolumeProfile>(RefinementLighting.VolumePath);p.TryGet<ColorAdjustments>(out var color);Assert.That(color.postExposure.value,Is.EqualTo(.55f));
        }
        [UnityTest,Timeout(180000)]public IEnumerator EmissiveFacesChangeActualVisiblePixelsWithSameExposureAndBake()
        {
            yield return new EnterPlayMode();scope=new RefinementTestScope();previous=SceneManager.GetActiveScene();sample=EditorSceneManager.LoadSceneInPlayMode(RefinementScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!sample.isLoaded)yield return null;SceneManager.SetActiveScene(sample);var root=RefinementScene.World(sample);root.GetComponentInChildren<SalvageMotor>().enabled=false;root.GetComponentInChildren<EchoStation.Player.PlayerInputReader>().enabled=false;foreach(var a in root.GetComponentsInChildren<Animator>())a.enabled=false;
            var camera=root.GetComponentInChildren<Camera>();camera.scene=sample;camera.transform.position=new Vector3(0,1.6f,-5);camera.transform.LookAt(new Vector3(0,5.7f,-6));root.GetComponentInChildren<Volume>().profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;var faces=root.GetComponentsInChildren<RefinementLightSurface>().Select(f=>f.GetComponent<Renderer>()).ToArray();string id=Guid.NewGuid().ToString("N").Substring(0,8);Color[] Capture(string name){using(var capture=new SceneFrameCapture(camera))return capture.Capture(RefinementScene.Evidence+"/Emission_"+name+"_"+id+".png");}
            try{for(int i=0;i<5;i++)yield return null;var on=Capture("On");foreach(var r in faces)r.enabled=false;var off=Capture("Off");float delta=SceneFrameCapture.Difference(on,off);File.WriteAllText(RefinementScene.Evidence+"/EmissionDeltas_"+id+".json",new JObject{{"visibleEmissionMeanAbsolute",delta},{"sameBakeAndExposure",true},{"onlySurfaceVisibilityChanged",true},{"width",1920},{"height",1080}}.ToString());Assert.That(delta,Is.GreaterThan(.0005),"Actual glowing geometry must be visible");}
            finally{foreach(var r in faces)if(r)r.enabled=true;}
        }
        [TearDown]public void CloseEdit(){if(Application.isPlaying)return;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);}
        [UnityTearDown]public IEnumerator ClosePlay(){if(!Application.isPlaying)yield break;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var u=SceneManager.UnloadSceneAsync(sample);while(!u.isDone)yield return null;}scope?.Dispose();scope=null;yield return new ExitPlayMode();}
    }
}
