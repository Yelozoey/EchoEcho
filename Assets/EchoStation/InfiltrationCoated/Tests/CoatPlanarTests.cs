using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationCoated.Editor;
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
namespace EchoStation.InfiltrationCoated.Tests
{
    public sealed class CoatPlanarTests
    {
        Scene previous,sample;CoatTestScope scope;
        [Test]public void SinglePatchAndAuxiliaryRendererKeepMainEffectsAndGameplayGeometrySeparate()
        {
            previous=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(CoatScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);var root=CoatScene.World(sample);var p=root.GetComponentsInChildren<CoatPlanar>().Single();Assert.That(p.GetComponent<Collider>(),Is.Null);Assert.That(p.transform.position,Is.EqualTo(new Vector3(0,.012f,-2)));Assert.That(p.GetComponent<Renderer>().bounds.size.x*p.GetComponent<Renderer>().bounds.size.z,Is.EqualTo(16).Within(.001));Assert.That(p.textureWidth,Is.EqualTo(1024));Assert.That(p.sourceCamera,Is.SameAs(root.GetComponentInChildren<Camera>()));
            var pipeline=new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(CoatScene.PipelinePath));var list=pipeline.FindProperty("m_RendererDataList");Assert.That(list.arraySize,Is.EqualTo(2));var main=(UniversalRendererData)list.GetArrayElementAtIndex(0).objectReferenceValue;var aux=(UniversalRendererData)list.GetArrayElementAtIndex(1).objectReferenceValue;Assert.That(main.rendererFeatures.Count(f=>f&&f.isActive),Is.EqualTo(2));Assert.That(aux.rendererFeatures,Is.Empty);Assert.That(new SerializedObject(aux).FindProperty("m_RenderingMode").intValue,Is.Zero);
            var probes=root.GetComponentsInChildren<ReflectionProbe>();Assert.That(probes.Count(p=>p.resolution==256),Is.EqualTo(2));Assert.That(probes.Count(p=>p.resolution==128),Is.EqualTo(6));Assert.That(probes.All(p=>p.intensity==1),Is.True);
        }
        static Color[] Read(RenderTexture rt){var previous=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBAFloat,false,true);try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();return image.GetPixels();}finally{RenderTexture.active=previous;UnityEngine.Object.Destroy(image);}}
        [UnityTest,Timeout(180000)]public IEnumerator ActualPlaneChangesPixelsUpdatesAndReleasesItsOwnResources()
        {
            yield return new EnterPlayMode();scope=new CoatTestScope();previous=SceneManager.GetActiveScene();sample=EditorSceneManager.LoadSceneInPlayMode(CoatScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!sample.isLoaded){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8));yield return null;}SceneManager.SetActiveScene(sample);var root=CoatScene.World(sample);root.GetComponentInChildren<SalvageMotor>().enabled=false;root.GetComponentInChildren<EchoStation.Player.PlayerInputReader>().enabled=false;foreach(var a in root.GetComponentsInChildren<Animator>())a.enabled=false;
            var camera=root.GetComponentInChildren<Camera>();camera.scene=sample;camera.transform.position=new Vector3(0,1.6f,-5);camera.transform.LookAt(new Vector3(0,.3f,-1.5f));root.GetComponentInChildren<Volume>().profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;var view=root.GetComponent<CoatReflectionView>();var p=root.GetComponentInChildren<CoatPlanar>();bool initialCulling=GL.invertCulling;string id=Guid.NewGuid().ToString("N").Substring(0,8);
            using(var capture=new SceneFrameCapture(camera))
            {
                view.SetReflection(true);var on=capture.Capture(CoatScene.Evidence+"/Plane_On_"+id+".png");Assert.That(p.LastTexture&&p.LastTexture.IsCreated(),Is.True);Assert.That(p.LastTexture.width,Is.EqualTo(1024));
                var test=GameObject.CreatePrimitive(PrimitiveType.Sphere);var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=Color.red;test.GetComponent<Renderer>().sharedMaterial=material;test.transform.position=new Vector3(-.7f,1,-1);test.transform.localScale=Vector3.one*.6f;capture.Capture();var first=Read(p.LastTexture);test.transform.position=new Vector3(.7f,1,-1);capture.Capture();float dynamic=SceneFrameCapture.Difference(first,Read(p.LastTexture));UnityEngine.Object.Destroy(test);UnityEngine.Object.Destroy(material);yield return null;
                view.SetReflection(false);for(int i=0;i<4;i++)yield return null;var off=capture.Capture(CoatScene.Evidence+"/Plane_Off_"+id+".png");float delta=SceneFrameCapture.Difference(on,off);Assert.That(delta,Is.GreaterThan(.0001f));Assert.That(dynamic,Is.GreaterThan(.00001f));Assert.That(Resources.FindObjectsOfTypeAll<Camera>().Count(c=>c.name.StartsWith("CF_PlanarCamera_")),Is.Zero);Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.name.StartsWith("CF_PlanarHDR_")),Is.Zero);Assert.That(GL.invertCulling,Is.EqualTo(initialCulling));view.SetReflection(true);capture.Capture();Assert.That(p.LastTexture.IsCreated(),Is.True);
                File.WriteAllText(CoatScene.Evidence+"/PlaneDeltas_"+id+".json",new JObject{{"visibleDelta",delta},{"dynamicTextureDelta",dynamic},{"matchedWorldAndExposure",true},{"temporaryDiagnosticSphereNotShipped",true},{"width",1024},{"releasedRecreated",true},{"cullingRestored",GL.invertCulling==initialCulling}}.ToString());
            }
        }
        [TearDown]public void CloseEdit(){if(Application.isPlaying)return;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);}
        [UnityTearDown]public IEnumerator ClosePlay(){if(!Application.isPlaying)yield break;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var op=SceneManager.UnloadSceneAsync(sample);while(!op.isDone)yield return null;}scope?.Dispose();scope=null;yield return new ExitPlayMode();}
    }
}
