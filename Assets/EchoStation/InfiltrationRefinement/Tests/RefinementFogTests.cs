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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.InfiltrationRefinement.Tests
{
    public sealed class RefinementFogTests
    {
        Scene previous,sample;RefinementTestScope scope;
        Transform Load(){previous=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(RefinementScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);return RefinementScene.World(sample);}
        [Test]public void GenericFieldHasThreeLocalVolumesAndOnlyItsCameraOptIn()
        {
            var root=Load();var field=root.GetComponentInChildren<LevelFogField>();Assert.That(field,Is.Not.Null);Assert.That(LevelFogField.Steps,Is.EqualTo(16));Assert.That(field.Box(0).size,Is.EqualTo(new Vector3(10,5.4f,10)));Assert.That(field.Box(1).size.x,Is.EqualTo(5));Assert.That(root.GetComponentInChildren<LevelFogView>().Source,Is.EqualTo(field));
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RefinementLighting.RendererPath);Assert.That(renderer.rendererFeatures.OfType<LevelFogFeature>().Count(f=>f.isActive),Is.EqualTo(1));Assert.That(field.SampleShadows&&!field.ScatterOnly,Is.True);
        }
        [Test]public void FieldSkipsDisabledZeroDensityDistantAndBackwardViews()
        {
            var root=Load();var field=root.GetComponentInChildren<LevelFogField>();var c=root.GetComponentInChildren<Camera>();c.transform.position=new Vector3(0,2,-5);c.transform.LookAt(new Vector3(0,2,1));Assert.That(field.ShouldRender(c),Is.True);field.DensityScale=0;Assert.That(field.ShouldRender(c),Is.False);field.DensityScale=1;field.enabled=false;Assert.That(field.ShouldRender(c),Is.False);field.enabled=true;c.transform.position=new Vector3(0,2,-100);Assert.That(field.ShouldRender(c),Is.False);c.transform.position=new Vector3(0,2,-14);c.transform.rotation=Quaternion.Euler(0,180,0);Assert.That(field.ShouldRender(c),Is.False);Assert.Throws<ArgumentOutOfRangeException>(()=>field.DensityScale=float.NaN);Assert.Throws<ArgumentOutOfRangeException>(()=>field.DensityScale=3);
        }
        [UnityTest,Timeout(180000)]public IEnumerator ActualFogIsHalfResolutionAndWallsAttenuateDirectionalScattering()
        {
            yield return new EnterPlayMode();scope=new RefinementTestScope();previous=SceneManager.GetActiveScene();sample=EditorSceneManager.LoadSceneInPlayMode(RefinementScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!sample.isLoaded)yield return null;SceneManager.SetActiveScene(sample);var root=RefinementScene.World(sample);root.GetComponentInChildren<SalvageMotor>().enabled=false;root.GetComponentInChildren<EchoStation.Player.PlayerInputReader>().enabled=false;foreach(var a in root.GetComponentsInChildren<Animator>())a.enabled=false;
            var c=root.GetComponentInChildren<Camera>();c.scene=sample;c.transform.position=new Vector3(0,1.6f,-5);c.transform.LookAt(new Vector3(1,3,0));root.GetComponentInChildren<UnityEngine.Rendering.Volume>().profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;
            var field=root.GetComponentInChildren<LevelFogField>();string id=Guid.NewGuid().ToString("N").Substring(0,8);Color[] Capture(string name){using(var capture=new SceneFrameCapture(c))return capture.Capture(RefinementScene.Evidence+"/Fog_"+name+"_"+id+".png");}
            for(int f=0;f<5;f++)yield return null;var on=Capture("On");int width=field.LastWidth,height=field.LastHeight;bool dependency=field.LastShadowDependency;field.enabled=false;var off=Capture("Off");field.enabled=true;field.ScatterOnly=true;field.SampleShadows=true;var shadowed=Capture("ScatterShadowed");field.SampleShadows=false;var unshadowed=Capture("ScatterUnshadowed");field.SampleShadows=true;field.ScatterOnly=false;
            float delta=SceneFrameCapture.Difference(on,off),shadowDelta=SceneFrameCapture.Difference(shadowed,unshadowed);
            File.WriteAllText(RefinementScene.Evidence+"/FogDeltas_"+id+".json",new JObject{{"fogDelta",delta},{"shadowDelta",shadowDelta},{"width",width},{"height",height},{"steps",16},{"mainShadowResource",dependency},{"onMean",on.Average(p=>.2126*p.r+.7152*p.g+.0722*p.b)},{"offMean",off.Average(p=>.2126*p.r+.7152*p.g+.0722*p.b)},{"shadowedScatter",shadowed.Average(p=>(p.r+p.g+p.b)/3)},{"unshadowedScatter",unshadowed.Average(p=>(p.r+p.g+p.b)/3)}}.ToString());
            Assert.That(width,Is.EqualTo(960));Assert.That(height,Is.EqualTo(540));Assert.That(dependency,Is.True);Assert.That(delta,Is.GreaterThan(.00001));Assert.That(shadowDelta,Is.GreaterThan(.00001));Assert.That(shadowed.Average(p=>(p.r+p.g+p.b)/3),Is.LessThan(unshadowed.Average(p=>(p.r+p.g+p.b)/3)),"Main shadow must reduce scattering behind geometry");
        }
        [TearDown]public void CloseEdit(){if(Application.isPlaying)return;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);}
        [UnityTearDown]public IEnumerator ClosePlay(){if(!Application.isPlaying)yield break;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var u=SceneManager.UnloadSceneAsync(sample);while(!u.isDone)yield return null;}scope?.Dispose();scope=null;yield return new ExitPlayMode();}
    }
}
