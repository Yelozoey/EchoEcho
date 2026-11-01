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
using Object=UnityEngine.Object;

namespace EchoStation.ParkourArt.Tests
{
    public sealed class VectorCityFinishingTests
    {
        [Test] public void PK05_OnlyColorEffectsAndNoForbiddenSurfaceInputs()
        {
            var scene=EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single);
            try{
                var volume=Object.FindAnyObjectByType<Volume>();Assert.That(volume,Is.Not.Null);Assert.That(volume.isGlobal&&volume.weight==1,Is.True);
                Assert.That(volume.sharedProfile.components.Select(c=>c.GetType().Name).OrderBy(n=>n).ToArray(),Is.EqualTo(new[]{"ColorAdjustments","WhiteBalance"}));
                var camera=GameObject.Find("Camera_Hero").GetComponent<Camera>();Assert.That(camera.GetUniversalAdditionalCameraData().renderPostProcessing,Is.True);
                var materials=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).Select(r=>r.sharedMaterial).Distinct().ToArray();Assert.That(materials.Length,Is.EqualTo(6));
                foreach(var m in materials){Assert.That(m.shader.name,Is.EqualTo("EchoStation/Parkour/Vector City PBR"));Assert.That(m.GetTexturePropertyNames().All(p=>m.GetTexture(p)==null),Is.True);Assert.That(m.shaderKeywords.Any(k=>k.Contains("NORMAL")||k.Contains("DETAIL")||k.Contains("OCCLUSION")),Is.False);}
                var shader=File.ReadAllText(VectorCityBuilder.Root+"/Shaders/VectorCityPBR.shader");Assert.That(shader.Contains("UniversalFragmentPBR(data,surface)"),Is.True);Assert.That(shader.Contains("_NormalMap")||shader.Contains("_BumpMap")||shader.Contains("floor(")||shader.Contains("Outline"),Is.False);
                Assert.That(Object.FindObjectsByType<ParticleSystem>().Length,Is.Zero);Assert.That(Object.FindObjectsByType<LineRenderer>().Length,Is.Zero);Assert.That(RenderSettings.fog,Is.False);
            }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        }
        [Test] public void PK05_ColorGradingActuallyChangesImageAndFinalViewsRender()
        {
            EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single);var path="Docs/Tests/ParkourArt/Evidence/PK05";Directory.CreateDirectory(path);
            try{
                var camera=GameObject.Find("Camera_Hero").GetComponent<Camera>();var data=camera.GetUniversalAdditionalCameraData();
                using(var frames=new ParkourFrameCapture(camera)){
                    var on=frames.Capture(path+"/Color_On.png");
                    try{data.renderPostProcessing=false;var off=frames.Capture(path+"/Color_Off.png");float difference=ParkourFrameCapture.Difference(on,off);Assert.That(difference,Is.GreaterThan(.0001f));File.WriteAllText(path+"/ActualColor.json","{\"imageDifference\":"+difference.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");}
                    finally{data.renderPostProcessing=true;}
                    Assert.That(on.Average(c=>(c.r+c.g+c.b)/3),Is.GreaterThan(.08));
                }
                VectorCityFinishing.ExportViews();
                Assert.That(ShaderUtil.ShaderHasError(Shader.Find("EchoStation/Parkour/Vector City PBR")),Is.False);
            }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        }
    }
}
