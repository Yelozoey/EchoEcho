using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace EchoStation.Salvage.Tests
{
    public class SalvageImportTests
    {
        [Serializable] class SourceFile { public string path; public string sha256; }
        [Serializable] class SourceManifest { public SourceFile[] files; }
        [Test] public void CopiedSourceBytesMatchIntake()
        {
            var manifest=JsonUtility.FromJson<SourceManifest>(File.ReadAllText("Docs/Tests/SalvagePrototype/Evidence/R0-A02/ImportedSourceManifest_A.json"));
            Assert.That(manifest.files.Length,Is.EqualTo(12));
            using(var sha=SHA256.Create())foreach(var f in manifest.files)
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f.path))).Replace("-","").ToLowerInvariant(),Is.EqualTo(f.sha256),f.path);
        }
        [TestCase(0)][TestCase(1)] public void ModelImporterAndMeshAreUsable(int i)
        {
            var m=(ModelImporter)AssetImporter.GetAtPath(SalvageAssetImport.Model(i));
            Assert.That(m.globalScale,Is.EqualTo(1));Assert.That(m.useFileScale,Is.True);Assert.That(m.generateSecondaryUV,Is.EqualTo(i==0));
            Assert.That(m.isReadable||m.importAnimation||m.importLights||m.importCameras||m.addCollider,Is.False);
            Assert.That(m.materialImportMode,Is.EqualTo(ModelImporterMaterialImportMode.None));
            var meshes=AssetDatabase.LoadAllAssetsAtPath(m.assetPath).OfType<Mesh>().ToArray();Assert.That(meshes,Is.Not.Empty);
            foreach(var mesh in meshes)
            {
                Assert.That(mesh.vertexCount,Is.GreaterThan(2));
                foreach(var a in new[]{VertexAttribute.Normal,VertexAttribute.Tangent,VertexAttribute.TexCoord0})Assert.That(mesh.HasVertexAttribute(a),Is.True,mesh.name+":"+a);
                if(i==0)Assert.That(mesh.HasVertexAttribute(VertexAttribute.TexCoord1),Is.True);
            }
        }
        [TestCase(0)][TestCase(1)] public void ActualFbxDimensionsArePlausible(int i)
        {
            var scene=EditorSceneManager.NewPreviewScene();GameObject instance=null;
            try
            {
                instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SalvageAssetImport.Model(i)));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance,scene);
                var b=SalvageAssetImport.BoundsOf(instance);
                if(i==0){Assert.That(b.size.x,Is.EqualTo(4).Within(.002));Assert.That(b.size.z,Is.EqualTo(4).Within(.002));Assert.That(b.max.y,Is.EqualTo(0).Within(.002));}
                else foreach(var d in new[]{b.size.x,b.size.y,b.size.z})Assert.That(d,Is.InRange(.05f,2f));
                TestContext.Out.WriteLine("Asset="+i+" bounds="+b.ToString("F5"));
                foreach(var mesh in instance.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh))TestContext.Out.WriteLine(mesh.name+" vertices="+mesh.vertexCount+" indices="+Enumerable.Range(0,mesh.subMeshCount).Sum(s=>(long)mesh.GetIndexCount(s)));
            }
            finally{if(instance)Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [TestCase(0)][TestCase(1)] public void TextureImportAndMaskChannelsAreCorrect(int i)
        {
            var paths=new[]{SalvageAssetImport.BaseMap(i),SalvageAssetImport.Normal(i),SalvageAssetImport.SourceMask(i),SalvageAssetImport.Mask(i)};
            for(int n=0;n<paths.Length;n++)
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(paths[n]);Assert.That(t.sRGBTexture,Is.EqualTo(n==0),paths[n]);
                Assert.That(t.textureType,Is.EqualTo(n==1?TextureImporterType.NormalMap:TextureImporterType.Default));
                Assert.That(t.maxTextureSize,Is.EqualTo(1024));Assert.That(t.textureCompression,Is.EqualTo(TextureImporterCompression.CompressedHQ));Assert.That(t.mipmapEnabled&&!t.isReadable,Is.True);
            }
            Texture2D source=null,packed=null;
            try
            {
                source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);packed=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                Assert.That(source.LoadImage(File.ReadAllBytes(paths[2]))&&packed.LoadImage(File.ReadAllBytes(paths[3])),Is.True);
                Assert.That(new Vector2Int(packed.width,packed.height),Is.EqualTo(new Vector2Int(source.width,source.height)));
                var input=source.GetPixels32();var output=packed.GetPixels32();Assert.That(input.Select(p=>p.g).Distinct().Count(),Is.GreaterThan(8));
                for(int n=0;n<input.Length;n++)if(input[n].b!=output[n].r||input[n].r!=output[n].g||output[n].b!=0||255-input[n].g!=output[n].a)Assert.Fail("Mask channel mismatch at pixel "+n);
            }
            finally{if(source)Object.DestroyImmediate(source);if(packed)Object.DestroyImmediate(packed);}
        }
        [TestCase(0)][TestCase(1)] public void MaterialsUseUrpAndCorrectMaps(int i)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(SalvageAssetImport.MaterialPath(i));Assert.That(m,Is.Not.Null);
            Assert.That(m.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(ShaderUtil.ShaderHasError(m.shader),Is.False);
            Assert.That(AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")),Is.EqualTo(SalvageAssetImport.BaseMap(i)));
            Assert.That(AssetDatabase.GetAssetPath(m.GetTexture("_BumpMap")),Is.EqualTo(SalvageAssetImport.Normal(i)));
            foreach(var p in new[]{"_MetallicGlossMap","_OcclusionMap"})Assert.That(AssetDatabase.GetAssetPath(m.GetTexture(p)),Is.EqualTo(SalvageAssetImport.Mask(i)));
            foreach(var k in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP"})Assert.That(m.IsKeywordEnabled(k),Is.True);
            Assert.That(m.GetColor("_BaseColor"),Is.EqualTo(Color.white));foreach(var p in new[]{"_Smoothness","_BumpScale","_OcclusionStrength"})Assert.That(m.GetFloat(p),Is.EqualTo(1));
        }
    }
}
