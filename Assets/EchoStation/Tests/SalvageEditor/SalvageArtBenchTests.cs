using System.IO;
using System.Linq;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace EchoStation.Salvage.Tests
{
    public class SalvageArtBenchTests
    {
        [Test]public void ActualBenchRendersAndMapsAffectPixels()
        {
            string evidence="Docs/Tests/SalvagePrototype/Evidence/R0-A02-V/Regression_"+System.Guid.NewGuid().ToString("N");TestContext.Out.WriteLine("Fresh art evidence: "+evidence);Directory.CreateDirectory(evidence);
            var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageArtBench.ScenePath,OpenSceneMode.Additive);
            Material copy=null;
            try
            {
                SceneManager.SetActiveScene(scene);var roots=scene.GetRootGameObjects();
                var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();
                var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;Assert.That(pipeline.supportsHDR&&camera.allowHDR,Is.True);Assert.That(PlayerSettings.allowHDRDisplaySupport,Is.False);
                foreach(var r in roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>()))foreach(var m in r.sharedMaterials)Assert.That(m&&m.shader.name=="Universal Render Pipeline/Lit",Is.True);
                var box=roots.Single(r=>r.name=="ES_Salvage_Toolbox");var renderers=box.GetComponentsInChildren<Renderer>();
                var source=AssetDatabase.LoadAssetAtPath<Material>(SalvageAssetImport.MaterialPath(1));copy=new Material(source);
                foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>copy).ToArray();
                using(var capture=new SalvageFrameCapture(camera))
                {
                    SalvageArtBench.Wide(camera);Validate(capture.Capture(evidence+"/Wide.png"));
                    SalvageArtBench.Close(camera);var normal= capture.Capture(evidence+"/Toolbox.png");Validate(normal);
                    copy.DisableKeyword("_NORMALMAP");var flat=capture.Capture(evidence+"/Toolbox_NoNormal.png");var normalDiff=SalvageFrameCapture.Difference(normal,flat);
                    copy.EnableKeyword("_NORMALMAP");copy.DisableKeyword("_METALLICSPECGLOSSMAP");copy.SetFloat("_Metallic",0);copy.SetFloat("_Smoothness",.05f);
                    var noMask=capture.Capture(evidence+"/Toolbox_NoMask.png");var maskDiff=SalvageFrameCapture.Difference(normal,noMask);
                    copy.CopyPropertiesFromMaterial(source);var restored=capture.Capture(evidence+"/Toolbox_Restored.png");var restoreDiff=SalvageFrameCapture.Difference(normal,restored);
                    File.WriteAllText(evidence+"/PixelStatistics.json",JsonUtility.ToJson(new Stats{normalDifference=normalDiff,maskDifference=maskDiff,restoredDifference=restoreDiff},true));
                    Assert.That(normalDiff,Is.GreaterThan(.00001));Assert.That(maskDiff,Is.GreaterThan(.0001));Assert.That(restoreDiff,Is.LessThan(.00001));
                }
            }
            finally{SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);if(copy)Object.DestroyImmediate(copy);}
        }
        [System.Serializable]class Stats{public float normalDifference,maskDifference,restoredDifference;}
        static void Validate(Color[] pixels)
        {
            Assert.That(pixels.Average(p=>(p.r+p.g+p.b)/3),Is.GreaterThan(.01));
            Assert.That(pixels.Count(p=>p.r>.8f&&p.b>.8f&&p.g<.05f),Is.LessThan(pixels.Length/100));
            Assert.That(pixels.Max(p=>p.maxColorComponent)-pixels.Min(p=>p.maxColorComponent),Is.GreaterThan(.1));
        }
    }
}
