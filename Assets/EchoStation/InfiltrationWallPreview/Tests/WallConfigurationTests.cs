using System.Linq;
using EchoStation.InfiltrationMetal.Editor;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationWallPreview.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace EchoStation.InfiltrationWallPreview.Tests
{
    public sealed class WallConfigurationTests
    {
        Scene previous,sample;Transform root;WallBrightnessPreview preview;
        [SetUp]public void Open(){previous=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(WallPreviewScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);root=MetalScene.World(sample);preview=root.GetComponent<WallBrightnessPreview>();}
        [TearDown]public void Close(){if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);}
        [Test]public void IndependentSceneChangesOnlyTwoWallMaterialSlots()
        {
            Assert.That(sample.path,Is.Not.EqualTo(MetalScene.ScenePath));Assert.That(root.GetComponentsInChildren<LevelFloor>().Length,Is.EqualTo(72));Assert.That(LevelLayout.ConservativeNetArea(root),Is.EqualTo(1007.70074f).Within(.01));Assert.That(root.GetComponentsInChildren<LevelPost>().Length,Is.EqualTo(4));Assert.That(preview.surfaces.Length,Is.EqualTo(2));
            string own=preview.Coated?WallPreviewScene.CoatingPath:WallPreviewScene.MaterialPath;var owned=root.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterials.Any(m=>AssetDatabase.GetAssetPath(m)==own)).ToArray();Assert.That(owned,Is.EquivalentTo(preview.surfaces));Assert.That(preview.surfaces.All(r=>r.GetComponentsInParent<Transform>().Any(t=>t.name=="South_4"||t.name=="South_4_Reverse")),Is.True);
            Assert.That(root.GetComponentsInChildren<Collider>().All(c=>!c.name.StartsWith("WP_")),Is.True);
        }
        [Test]public void WallKeepsDetailMapsAndItsConfirmedFinish()
        {
            var a=preview.original;var b=preview.brighter;Assert.That(b.shader,Is.SameAs(a.shader));
            if(preview.Coated)
            {
                Assert.That(AssetDatabase.GetAssetPath(b),Is.EqualTo(WallPreviewScene.CoatingPath));Assert.That(AssetDatabase.GetAssetPath(b.GetTexture("_BaseMap")),Is.EqualTo(WallPreviewScene.CoatingTexturePath));Assert.That(b.GetTexture("_BaseMap").width,Is.LessThanOrEqualTo(512));Assert.That(b.GetFloat("_Metallic"),Is.Zero);Assert.That(b.GetFloat("_Smoothness"),Is.EqualTo(.42f));Assert.That(b.IsKeywordEnabled("_METALLICSPECGLOSSMAP"),Is.False);
                foreach(string t in new[]{"_BumpMap","_OcclusionMap","_EmissionMap"})Assert.That(b.GetTexture(t),Is.SameAs(a.GetTexture(t)),t);
                var input=new Texture2D(2,2,TextureFormat.RGBA32,false);try{Assert.That(input.LoadImage(System.IO.File.ReadAllBytes(WallPreviewScene.CoatingTexturePath)),Is.True);var values=input.GetPixels32();Assert.That(values.All(p=>p.r>=180&&p.b<=235),Is.True);Assert.That(values.Select(p=>p.b).Distinct().Count(),Is.GreaterThan(8),"Preserve low-contrast atlas variation");}finally{UnityEngine.Object.DestroyImmediate(input);}return;
            }
            Assert.That(AssetDatabase.GetAssetPath(b),Is.EqualTo(WallPreviewScene.MaterialPath));
            foreach(string t in new[]{"_BaseMap","_BumpMap","_OcclusionMap","_MetallicGlossMap","_EmissionMap"})Assert.That(b.GetTexture(t),Is.SameAs(a.GetTexture(t)),t);
            foreach(string f in new[]{"_Metallic","_Smoothness","_EnvironmentReflections","_SpecularHighlights"})Assert.That(b.GetFloat(f),Is.EqualTo(a.GetFloat(f)),f);
            Assert.That(b.shaderKeywords,Is.EquivalentTo(a.shaderKeywords));var old=a.GetColor("_BaseColor");var next=b.GetColor("_BaseColor");Assert.That(next.r>old.r&&next.g>old.g&&next.b>old.b,Is.True);Assert.That(next.maxColorComponent,Is.LessThanOrEqualTo(1));
        }
        [Test]public void CoatingDefaultNeedsNoExtraLightAndMetalTrimStaysMetal()
        {
            Assert.That(preview.Coated,Is.True);Assert.That(preview.Mode,Is.EqualTo(1));Assert.That(preview.grazing.enabled,Is.False);Assert.That(preview.grazing.intensity,Is.EqualTo(2));foreach(var r in preview.surfaces){Assert.That(r.sharedMaterials.Length,Is.EqualTo(3));Assert.That(r.sharedMaterials[1].name,Does.StartWith("MM_Rough_"));Assert.That(r.sharedMaterials[2].name,Does.StartWith("MM_Preserved_"));}
        }
        [Test]public void ThreeStatesRestoreOnlyTheirOwnedWallAndLamp()
        {
            var protectedSlots=preview.surfaces.Select(r=>r.sharedMaterials.Skip(1).ToArray()).ToArray();
            for(int i=0;i<3;i++){preview.SetMode(i);Assert.That(preview.Mode,Is.EqualTo(i));Assert.That(preview.surfaces.All(r=>r.sharedMaterial==(i==0?preview.original:preview.brighter)),Is.True);Assert.That(preview.grazing.enabled,Is.EqualTo(i==2));for(int n=0;n<preview.surfaces.Length;n++)Assert.That(preview.surfaces[n].sharedMaterials.Skip(1),Is.EqualTo(protectedSlots[n]));}preview.SetMode(0);Assert.That(preview.grazing.enabled,Is.False);
        }
        [Test]public void FixedExposureAndLocalShadowlessPreviewKeepExistingLighting()
        {
            var volume=root.GetComponentInChildren<Volume>().sharedProfile;volume.TryGet<ColorAdjustments>(out var color);Assert.That(color.postExposure.value,Is.EqualTo(.55f));var l=preview.grazing;Assert.That(l.type,Is.EqualTo(LightType.Spot));Assert.That(l.lightmapBakeType,Is.EqualTo(LightmapBakeType.Realtime));Assert.That(l.shadows,Is.EqualTo(LightShadows.None));Assert.That(l.range,Is.EqualTo(6));Assert.That(l.bounceIntensity,Is.Zero);Assert.That(root.GetComponentsInChildren<Light>().Count(x=>x.name.StartsWith("WP_")),Is.EqualTo(1));Assert.That(root.GetComponentsInChildren<Light>().Count(x=>x.lightmapBakeType==LightmapBakeType.Baked),Is.EqualTo(42));Assert.That(LightmapSettings.lightmaps.Length,Is.EqualTo(2));Assert.That(root.GetComponentsInChildren<ReflectionProbe>().Length,Is.EqualTo(8));
        }
    }
}
