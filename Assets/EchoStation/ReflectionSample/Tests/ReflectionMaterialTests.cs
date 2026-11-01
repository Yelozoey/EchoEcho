using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using EchoStation.ReflectionSample.Editor;
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
namespace EchoStation.ReflectionSample.Tests
{
    public sealed class ReflectionMaterialTests:ReflectionTestBase
    {
        [Test]public void OwnedRoomHasRealSourcesAndThreeDistinctSurfaceModels()
        {
            var old=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(ReflectionSampleBuilder.ScenePath,OpenSceneMode.Additive);
            try
            {
                var w=ReflectionSampleBuilder.World(s);Assert.That(w.GetComponentsInChildren<ArtSampleModel>().Length,Is.GreaterThanOrEqualTo(9));
                var rough=w.Find("RS_TestSphere_0").GetComponent<Renderer>().sharedMaterial;var polished=w.Find("RS_TestSphere_1").GetComponent<Renderer>().sharedMaterial;var painted=w.Find("RS_TestSphere_2").GetComponent<Renderer>().sharedMaterial;
                Assert.That(rough.GetFloat("_Metallic"),Is.EqualTo(1));Assert.That(polished.GetFloat("_Metallic"),Is.EqualTo(1));Assert.That(polished.GetFloat("_Smoothness")-rough.GetFloat("_Smoothness"),Is.GreaterThan(.7));Assert.That(painted.GetTexture("_MetallicGlossMap"),Is.Not.Null);
                Assert.That(new[]{rough,polished,painted}.All(m=>AssetDatabase.GetAssetPath(m).StartsWith(ReflectionSampleBuilder.Root+"/")),Is.True);
                var c=w.GetComponentInChildren<Camera>();Assert.That(c.allowHDR&&!c.GetUniversalAdditionalCameraData().allowHDROutput,Is.True);Assert.That(w.GetComponentInChildren<EchoStation.Salvage.SalvageMotor>(),Is.Not.Null);
                var p=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ReflectionSampleBuilder.PipelinePath);Assert.That(p.supportsHDR&&p.useSRPBatcher,Is.True);var r=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ReflectionSampleBuilder.RendererPath);Assert.That(new SerializedObject(r).FindProperty("m_RenderingMode").intValue,Is.Zero);
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(s,true);}
        }
        [Test]public void Actual512CubemapIsPersistedAndReferencedInOwnedScene()
        {
            var old=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(ReflectionSampleBuilder.ScenePath,OpenSceneMode.Additive);
            try{var p=ReflectionSampleBuilder.World(s).GetComponentsInChildren<ReflectionProbe>().Single();Assert.That(p.mode,Is.EqualTo(ReflectionProbeMode.Custom));Assert.That(p.customBakedTexture,Is.Not.Null);Assert.That(p.customBakedTexture.dimension,Is.EqualTo(TextureDimension.Cube));Assert.That(p.customBakedTexture.width,Is.EqualTo(512));var path=AssetDatabase.GetAssetPath(p.customBakedTexture);Assert.That(path.StartsWith(ReflectionSampleBuilder.Generated+"/"),Is.True);Assert.That(new FileInfo(path).Length,Is.GreaterThan(50000));Assert.That(p.boxProjection,Is.True);}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(s,true);}
        }
        [UnityTest,Timeout(120000)]public IEnumerator ActualEnvironmentAndDirectSpecularEachChangePixelsAtFixedExposure()
        {
            yield return new EnterPlayMode();yield return Load();Freeze();camera.transform.position=new Vector3(0,2.4f,-1.5f);camera.transform.LookAt(new Vector3(0,1.2f,2.1f));string id=Guid.NewGuid().ToString("N").Substring(0,8);
            using(var capture=new SceneFrameCapture(camera))
            {
                preview.SetDynamic(false);preview.SetEnvironment(true);preview.SetHighlights(true);var on=capture.Capture(ReflectionSampleBuilder.Evidence+"/Material_On_"+id+".png");preview.SetEnvironment(false);var noEnvironment=capture.Capture(ReflectionSampleBuilder.Evidence+"/Material_NoEnvironment_"+id+".png");preview.SetHighlights(false);var noBoth=capture.Capture();
                float environment=SceneFrameCapture.Difference(on,noEnvironment),highlight=SceneFrameCapture.Difference(noEnvironment,noBoth);File.WriteAllText(ReflectionSampleBuilder.Evidence+"/MaterialDeltas_"+id+".json",new JObject{{"environment",environment},{"directSpecular",highlight},{"sameExposure",true},{"width",1920},{"height",1080},{"dynamicReflectionOff",true}}.ToString());
                Assert.That(on.Average(c=>(c.r+c.g+c.b)/3),Is.GreaterThan(.01));Assert.That(environment,Is.GreaterThan(.0002f));Assert.That(highlight,Is.GreaterThan(.00001f));preview.SetEnvironment(true);preview.SetHighlights(true);
            }
        }
    }
}
