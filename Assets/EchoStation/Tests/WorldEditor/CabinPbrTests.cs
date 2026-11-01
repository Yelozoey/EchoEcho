using System.IO;
using System.Linq;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoStation.Tests.World
{
    public class CabinPbrTests
    {
        [Test]public void W02_01_ArmConversionPreservesCorrectUnityChannelsAndUvUsesMeters()
        {
            Assert.That(CabinPbrSetup.PackMask(new Color32(37,81,123,255)),Is.EqualTo(new Color32(123,37,0,174)));
            Assert.That(CabinPbrSetup.PackMask(new Color32(255,0,0,255)),Is.EqualTo(new Color32(0,255,0,255)));
            Assert.That(CabinPbrSetup.MeterUv(new Vector3(4,6,8),Vector3.up),Is.EqualTo(new Vector2(2,4)));
            Assert.That(CabinPbrSetup.MeterUv(new Vector3(4,6,8),Vector3.forward),Is.EqualTo(new Vector2(2,3)));
        }
        [Test]public void W02_02_AllImportedPbrMapsHaveCorrectColorSpaceMipCompressionAndActualMaterialReferences()
        {
            foreach(var id in CabinPbrSetup.Assets)
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>($"{CabinPbrSetup.Folder}/ES_{id}.mat");Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(material.shader.isSupported,Is.True);
                foreach(var kind in new[]{"BaseColor","Normal","Mask"})
                {
                    var path=$"{CabinPbrSetup.Folder}/ES_{id}_{kind}.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    Assert.That(importer.sRGBTexture,Is.EqualTo(kind=="BaseColor"));Assert.That(importer.textureType,Is.EqualTo(kind=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default));
                    Assert.That(importer.mipmapEnabled&&!importer.isReadable,Is.True);Assert.That(importer.maxTextureSize,Is.EqualTo(1024));Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.CompressedHQ));
                    Assert.That(texture.width,Is.EqualTo(1024));Assert.That(material.GetTexture(kind=="BaseColor"?"_BaseMap":kind=="Normal"?"_BumpMap":"_MetallicGlossMap"),Is.SameAs(texture));
                }
                Assert.That(material.IsKeywordEnabled("_NORMALMAP")&&material.IsKeywordEnabled("_METALLICSPECGLOSSMAP")&&material.IsKeywordEnabled("_OCCLUSIONMAP"),Is.True);
            }
        }
        [Test]public void W02_03_RoomPrefabsAreIndependentAndDoNotCarrySceneCamerasOrLightmapIndices()
        {
            foreach(var name in new[]{"Main","Storage","Foyer"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"{CabinPbrSetup.Prefabs}/ES_Cabin_Room_{name}.prefab");Assert.That(prefab,Is.Not.Null);
                Assert.That(prefab.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(prefab.GetComponentsInChildren<BoxCollider>().Length,Is.GreaterThan(0));
                Assert.That(prefab.GetComponentsInChildren<Camera>().Length+prefab.GetComponentsInChildren<Light>().Length,Is.Zero);
                foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>()){Assert.That(renderer.sharedMaterial,Is.Not.Null);Assert.That(renderer.lightmapIndex,Is.EqualTo(-1));}
            }
        }
        [Test]public void W02_04_SceneActuallyUsesBothPbrSetsAndKeepsCombinedUv2AndCollisions()
        {
            var scene=EditorSceneManager.OpenPreviewScene(CabinGreyboxSetup.ScenePath);
            try
            {
                var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");var enabled=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&!r.GetComponent<EchoStation.World.ClueSource>()).ToArray();
                Assert.That(enabled.Length,Is.EqualTo(8));Assert.That(root.GetComponentsInChildren<BoxCollider>().Count(c=>!c.GetComponent<EchoStation.World.ClueSource>()),Is.EqualTo(30));
                foreach(var id in CabinPbrSetup.Assets)Assert.That(enabled.Any(r=>r.sharedMaterial.name=="ES_"+id),Is.True);
                foreach(var renderer in enabled)Assert.That(LightmapUvValidator.IsValidNonOverlapping(renderer.GetComponent<MeshFilter>().sharedMesh,out _),Is.True);
                Assert.That(File.Exists("Docs/Assets/Sources/PolyHaven/Manifest.json")&&File.Exists("Docs/Assets/ThirdPartyLicenses.md"),Is.True);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]public void W02_05_GeneratedFilesMatchActualSourceAoRoughnessMetallicPixels()
        {
            foreach(var id in CabinPbrSetup.Assets)
            {
                var source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);var mask=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                try
                {
                    Assert.That(source.LoadImage(File.ReadAllBytes($"Docs/Assets/Sources/PolyHaven/{id}_ARM.png")),Is.True);
                    Assert.That(mask.LoadImage(File.ReadAllBytes($"{CabinPbrSetup.Folder}/ES_{id}_Mask.png")),Is.True);
                    Assert.That(mask.width,Is.EqualTo(source.width));Assert.That(mask.height,Is.EqualTo(source.height));var a=source.GetPixels32();var b=mask.GetPixels32();
                    for(var i=0;i<a.Length;i++)if(b[i].r!=a[i].b||b[i].g!=a[i].r||b[i].b!=0||b[i].a!=255-a[i].g)Assert.Fail("Incorrect mask channel at actual source pixel "+id+"/"+i);
                }
                finally{Object.DestroyImmediate(source);Object.DestroyImmediate(mask);}
            }
        }
    }
}
