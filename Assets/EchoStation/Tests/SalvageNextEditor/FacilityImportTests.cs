using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Salvage.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.SalvageNext.Tests
{
    public sealed class FacilityImportTests
    {
        [Test] public void CopiedFilesStillMatchVerifiedStandardBytes()
        {
            var manifest=JObject.Parse(File.ReadAllText(FacilityAssets.Evidence+"/Extraction.json"));
            foreach(var record in manifest["files"]){var raw=File.ReadAllBytes(record["path"].Value<string>());Assert.That(raw.LongLength,Is.EqualTo(record["bytes"].Value<long>()));Assert.That(ExperimentArtifacts.Hash(raw),Is.EqualTo(record["sha256"].Value<string>()));}
            Assert.That(manifest["models"].Value<int>(),Is.EqualTo(26));
        }
        [Test] public void AllTwentySixModelsHaveGeometryNormalsUvsAndOwnedUrpMaterials()
        {
            var models=FacilityAssets.Models();Assert.That(models.Length,Is.EqualTo(26));var rows=new JArray();
            foreach(var path in models)
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);Assert.That(importer.importAnimation||importer.importCameras||importer.importLights||importer.addCollider,Is.False,path);
                Assert.That(importer.globalScale,Is.EqualTo(1));Assert.That(importer.isReadable,Is.True);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FacilityAssets.Prefab(path));Assert.That(prefab,Is.Not.Null,path);
                var renderers=prefab.GetComponentsInChildren<Renderer>();var total=renderers[0].bounds;foreach(var item in renderers.Skip(1))total.Encapsulate(item.bounds);
                Assert.That(Mathf.Max(total.size.x,total.size.y,total.size.z),Is.LessThanOrEqualTo(10),"Source meter scale, not a 100x baked skin: "+path);
                var filters=prefab.GetComponentsInChildren<MeshFilter>();Assert.That(filters.Length,Is.GreaterThan(0),path);long vertices=0,triangles=0;
                foreach(var filter in filters)
                {
                    var mesh=filter.sharedMesh;Assert.That(mesh,Is.Not.Null,path);Assert.That(mesh.vertexCount,Is.GreaterThan(0),path+"/"+filter.name);vertices+=mesh.vertexCount;triangles+=mesh.triangles.Length/3;
                    Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount),path);Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount),path);
                    if(FacilityAssets.PrimaryStatic(path))Assert.That(mesh.uv2.Length,Is.EqualTo(mesh.vertexCount),path);
                    Assert.That(float.IsFinite(mesh.bounds.size.sqrMagnitude)&&mesh.bounds.size.sqrMagnitude>.0000001f,Is.True,path);
                }
                foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                {
                    Assert.That(renderer.sharedMaterials.Length,Is.EqualTo(renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount));
                    foreach(var material in renderer.sharedMaterials){Assert.That(material&&material.shader.name=="Universal Render Pipeline/Lit",Is.True,path);Assert.That(AssetDatabase.GetAssetPath(material),Does.StartWith(FacilityAssets.Generated+"/"));Assert.That(material.GetTexture("_BaseMap"),Is.Not.Null);Assert.That(material.GetTexture("_BumpMap"),Is.Not.Null);}
                }
                Assert.That(prefab.GetComponentsInChildren<Collider>(),Is.Empty,"Visual modules must not put a box across door holes; persistent scene collision is tested later.");
                Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(),Is.Empty,"Static props are baked to a pose, without CPU skeletal animation.");
                Assert.That(prefab.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterials.Any(m=>m&&m.name=="ES_LightFade")),Is.False,"Cosmetic source light cones are not the project's volumetric lighting.");
                Assert.That(prefab.GetComponentsInChildren<Camera>(),Is.Empty);rows.Add(new JObject{{"path",path},{"vertices",vertices},{"triangles",triangles},{"meshes",filters.Length},{"staticUv2",FacilityAssets.PrimaryStatic(path)},{"sizeX",total.size.x},{"sizeY",total.size.y},{"sizeZ",total.size.z}});
            }
            File.WriteAllText(FacilityAssets.Evidence+"/Geometry_"+Guid.NewGuid().ToString("N")+".json",rows.ToString());
        }
        [TestCase("01")][TestCase("02")][TestCase("03")][TestCase("Padded")]
        public void PackedMasksKeepExactOrmChannelMeaning(string number)
        {
            Texture2D source=null,packed=null;
            try
            {
                source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);packed=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                Assert.That(source.LoadImage(File.ReadAllBytes(FacilityAssets.Raw+"/Textures/"+(number=="Padded"?"T_PaddedWall":"T_Trim_"+number)+"_ORM.png")),Is.True);
                var path=FacilityAssets.Generated+"/ES_"+(number=="Padded"?"Padded":"Trim"+number)+"_Mask.png";Assert.That(packed.LoadImage(File.ReadAllBytes(path)),Is.True);
                var a=source.GetPixels32();var b=packed.GetPixels32();Assert.That(b.Length,Is.EqualTo(a.Length));
                for(int i=0;i<a.Length;i++)if(b[i].r!=a[i].b||b[i].g!=a[i].r||b[i].a!=255-a[i].g)Assert.Fail("Mask channel mismatch at pixel "+i);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.sRGBTexture,Is.False);Assert.That(importer.isReadable,Is.False);Assert.That(importer.maxTextureSize,Is.EqualTo(1024));Assert.That(importer.mipmapEnabled,Is.True);
            }
            finally{if(source)Object.DestroyImmediate(source);if(packed)Object.DestroyImmediate(packed);}
        }
        [UnityTest] public IEnumerator RealHdrBenchHasVisibleGeometryAndRestoresSceneAndCameraState()
        {
            var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(FacilityAssets.Bench,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);yield return null;yield return null;
                var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();Assert.That(camera.allowHDR,Is.True);
                var planes=GeometryUtility.CalculateFrustumPlanes(camera);var gallery=scene.GetRootGameObjects().Where(r=>r.name.StartsWith("ES_",StringComparison.Ordinal)).ToArray();Assert.That(gallery.Length,Is.EqualTo(7));
                foreach(var model in gallery){var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var item in renderers.Skip(1))bounds.Encapsulate(item.bounds);Assert.That(bounds.Contains(camera.transform.position),Is.False,"Camera is inside a giant asset: "+model.name);Assert.That(GeometryUtility.TestPlanesAABB(planes,bounds),Is.True,"Unframed asset: "+model.name);}
                var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Assert.That(pipeline&&pipeline.supportsHDR,Is.True);Assert.That(UnityEditor.PlayerSettings.allowHDRDisplaySupport,Is.False);Assert.That(SystemInfo.graphicsDeviceType,Is.EqualTo(GraphicsDeviceType.Direct3D11));
                float originalAspect=camera.aspect;var target=camera.targetTexture;var oldType=camera.cameraType;var file=FacilityAssets.Evidence+"/Bench_"+Guid.NewGuid().ToString("N")+".png";
                Color[] colors;using(var capture=new SalvageFrameCapture(camera))colors=capture.Capture(file);
                Assert.That(camera.targetTexture,Is.SameAs(target));Assert.That(camera.cameraType,Is.EqualTo(oldType));Assert.That(camera.aspect,Is.EqualTo(originalAspect).Within(.001));
                Assert.That(colors.Length,Is.EqualTo(1920*1080));Assert.That(colors.Count(c=>c.grayscale>.15f),Is.GreaterThan(1000));
                double mean=colors.Average(c=>c.grayscale),variance=colors.Average(c=>Math.Pow(c.grayscale-mean,2));Assert.That(variance,Is.GreaterThan(.0005),"A flat empty/black frame is not compatibility evidence.");
                Assert.That(colors.Count(c=>c.r>.8f&&c.g<.05f&&c.b>.8f),Is.LessThan(colors.Length/1000),"Missing-shader magenta");
                File.WriteAllText(Path.ChangeExtension(file,".json"),new JObject{{"image",file},{"width",1920},{"height",1080},{"mean",mean},{"variance",variance},{"pipeline",AssetDatabase.GetAssetPath(pipeline)},{"internalHdr",true},{"displayHdr",false},{"api",SystemInfo.graphicsDeviceType.ToString()},{"scope","asset compatibility studio, not gameplay lighting or FPS"}}.ToString());
            }
            finally{SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(original.path));Assert.That(original.isDirty,Is.False);
        }
    }
}
