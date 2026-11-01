using System;
using System.IO;
using System.Linq;
using EchoStation.ParkourArt.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.ParkourArt.Tests
{
    public sealed class VectorCityLightingTests
    {
        [Test] public void PK03_SavedCustomBindingBootStillRestoresTheOriginalAssets()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var baseline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/EchoStation/Station/Generated/ES_S2_ForwardHDR.asset");
            var art=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(VectorCityLighting.PipelinePath);
            try{GraphicsSettings.defaultRenderPipeline=art;QualitySettings.renderPipeline=art;EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single);}
            finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
            Assert.That(GraphicsSettings.defaultRenderPipeline,Is.EqualTo(baseline));Assert.That(QualitySettings.renderPipeline,Is.EqualTo(baseline));
        }
        [Test] public void PK03_ForwardHDRShadowSettingsAndNoSsaoAreActuallyBound()
        {
            EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single);
            try{
                var pipeline=QualitySettings.renderPipeline as UniversalRenderPipelineAsset;Assert.That(AssetDatabase.GetAssetPath(pipeline),Is.EqualTo(VectorCityLighting.PipelinePath));
                Assert.That(GraphicsSettings.defaultRenderPipeline,Is.EqualTo(pipeline));Assert.That(pipeline.supportsHDR,Is.True);Assert.That(PlayerSettings.allowHDRDisplaySupport,Is.False);Assert.That(SystemInfo.graphicsDeviceType,Is.EqualTo(GraphicsDeviceType.Direct3D11));
                Assert.That(pipeline.msaaSampleCount,Is.EqualTo(4));Assert.That(pipeline.mainLightShadowmapResolution,Is.EqualTo(8192));Assert.That(pipeline.shadowCascadeCount,Is.EqualTo(4));Assert.That(pipeline.shadowDistance,Is.EqualTo(110));
                var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(VectorCityLighting.RendererPath);Assert.That(new SerializedObject(renderer).FindProperty("m_RenderingMode").intValue,Is.EqualTo(0));Assert.That(renderer.rendererFeatures.Count,Is.EqualTo(0));
                Assert.That(RenderSettings.ambientSkyColor.maxColorComponent,Is.GreaterThan(.6f));Assert.That(RenderSettings.fog,Is.False);
            }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        }
        [Test] public void PK03_DaylightShadowImageAndNonNullOriginalBindingsRestore()
        {
            var baselineDefault=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/EchoStation/Station/Generated/ES_S2_ForwardHDR.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);GraphicsSettings.defaultRenderPipeline=baselineDefault;QualitySettings.renderPipeline=baselineDefault;
            EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single);
            var path="Docs/Tests/ParkourArt/Evidence/PK03";Directory.CreateDirectory(path);
            try{
                var camera=GameObject.Find("Camera_Hero").GetComponent<Camera>();camera.transform.position=new Vector3(-30,16,-30);camera.transform.LookAt(new Vector3(-19,12,-14));
                var sun=GameObject.Find("Sun_Daylight").GetComponent<Light>();using(var frames=new ParkourFrameCapture(camera)){
                    var on=frames.Capture(path+"/Shadows_On.png");var original=sun.shadows;
                    try{sun.shadows=LightShadows.None;var off=frames.Capture(path+"/Shadows_Off.png");float delta=ParkourFrameCapture.Difference(on,off);Assert.That(delta,Is.GreaterThan(.0001f));File.WriteAllText(path+"/ShadowDifference.json","{\"difference\":"+delta.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");}
                    finally{sun.shadows=original;}
                    Assert.That(on.Average(c=>(c.r+c.g+c.b)/3),Is.GreaterThan(.08));
                }
            }finally{
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                Assert.That(GraphicsSettings.defaultRenderPipeline,Is.EqualTo(baselineDefault));Assert.That(QualitySettings.renderPipeline,Is.EqualTo(baselineDefault));
            }
            File.WriteAllText(path+"/BindingsRestored.json","{\"default\":\""+AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline)+"\",\"PC\":\""+AssetDatabase.GetAssetPath(QualitySettings.renderPipeline)+"\",\"passed\":true}");
        }
    }
}
