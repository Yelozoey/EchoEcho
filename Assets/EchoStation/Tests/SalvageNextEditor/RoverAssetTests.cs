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

namespace EchoStation.SalvageNext.Tests
{
    public sealed class RoverAssetTests
    {
        static RoverModelParts Parts()=>AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssets.PrefabPath).GetComponent<RoverModelParts>();
        [Test] public void DerivedSourceFilesRetainFrozenFaceUvConversionAndMeasuredAxes()
        {
            var source=JObject.Parse(File.ReadAllText(RoverAssets.Raw+"/RoverParts.json"));Assert.That(source["license"].Value<string>(),Is.EqualTo("CC0-1.0"));Assert.That(source["sourceFaces"].Value<int>(),Is.EqualTo(704));
            Assert.That(source["parts"].Sum(p=>p["faces"].Value<int>()),Is.EqualTo(704));
            foreach(var part in source["parts"])Assert.That(ExperimentArtifacts.Hash(File.ReadAllBytes(part["path"].Value<string>())),Is.EqualTo(part["sha256"].Value<string>()));
            var probe=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EchoStation/Tests/SalvageNextFixtures/Geometry/ObjAxisProbe.obj").GetComponentInChildren<MeshRenderer>().bounds;
            Assert.That(Vector3.Distance(probe.center,new Vector3(-1.5f,3,5)),Is.LessThan(.0001f));
        }
        [Test] public void EightPartUnityGeometryMatchesOriginalWholeModelInMeters()
        {
            var parts=Parts();Assert.That(parts.WheelCount,Is.EqualTo(6));Assert.That(parts.Head&&parts.Body&&parts.ScanOrigin.IsChildOf(parts.Head),Is.True);
            var renderers=parts.GetComponentsInChildren<MeshRenderer>().Where(r=>r!=parts.Indicator).ToArray();Assert.That(renderers.Length,Is.EqualTo(8));
            var bounds=renderers[0].bounds;long triangles=0;
            foreach(var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;Assert.That(mesh.vertexCount,Is.GreaterThan(0));Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));triangles+=mesh.triangles.Length/3;
                Assert.That(renderer.sharedMaterials.All(m=>m==RoverAssets.Palette("Surface")),Is.True);
            }
            var raw=AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssets.Raw+"/Original/rover_mesh.obj");var expected=raw.GetComponentInChildren<MeshRenderer>().bounds.size*.25f;
            Assert.That(Vector3.Distance(bounds.size,expected),Is.LessThan(.001f));Assert.That(Mathf.Abs(bounds.min.y),Is.LessThan(.0001f));Assert.That(Mathf.Abs(bounds.center.x)+Mathf.Abs(bounds.center.z),Is.LessThan(.001f));
            Assert.That(triangles,Is.EqualTo(raw.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)));
            Assert.That(bounds.size.x,Is.InRange(.57f,.59f));Assert.That(bounds.size.y,Is.InRange(.53f,.55f));Assert.That(bounds.size.z,Is.InRange(.76f,.79f));
            for(int i=0;i<6;i++){Assert.That(parts.Wheel(i).IsChildOf(parts.transform),Is.True);Assert.That(parts.Radius(i),Is.InRange(.04f,.07f));}
            Assert.That(parts.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(parts.GetComponentsInChildren<Animator>(),Is.Empty);Assert.That(parts.GetComponentsInChildren<SkinnedMeshRenderer>(),Is.Empty);
            File.WriteAllText(RoverAssets.Evidence+"/Geometry_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"parts",8},{"wheels",6},{"triangles",triangles},{"sizeX",bounds.size.x},{"sizeY",bounds.size.y},{"sizeZ",bounds.size.z},{"scope","visual geometry only; traditional behavior integration is c2"}}.ToString());
        }
        [Test] public void SourceTextureAndThreeHdrStatusColorsPersist()
        {
            var source=JObject.Parse(File.ReadAllText(RoverAssets.Raw+"/RoverParts.json"));Assert.That(ExperimentArtifacts.Hash(File.ReadAllBytes(RoverAssets.Raw+"/Original/rover_diffuse.png")),Is.EqualTo(source["textureSha256"].Value<string>()));
            var body=RoverAssets.Palette("Surface");Assert.That(body.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(AssetDatabase.GetAssetPath(body.GetTexture("_BaseMap")),Is.EqualTo(RoverAssets.Raw+"/Original/rover_diffuse.png"));
            var importer=(TextureImporter)AssetImporter.GetAtPath(RoverAssets.Raw+"/Original/rover_diffuse.png");Assert.That(importer.maxTextureSize,Is.EqualTo(256));Assert.That(importer.sRGBTexture,Is.True);Assert.That(importer.isReadable,Is.False);
            var parts=Parts();Assert.That(parts.Indicator.sharedMaterial,Is.SameAs(parts.Calm));
            var blue=parts.Calm.GetColor("_EmissionColor");var yellow=parts.Thinking.GetColor("_EmissionColor");var red=parts.Alert.GetColor("_EmissionColor");
            Assert.That(blue.b,Is.GreaterThan(blue.r+blue.g));Assert.That(yellow.r,Is.GreaterThan(1));Assert.That(yellow.g,Is.GreaterThan(1));Assert.That(yellow.b,Is.LessThan(.1f));Assert.That(red.r,Is.GreaterThan(red.g+red.b));
            foreach(var material in new[]{parts.Calm,parts.Thinking,parts.Alert})Assert.That(material.GetTexture("_BaseMap"),Is.Not.Null);
        }
        [UnityTest] public IEnumerator ActualRoverStudioShowsModelWithHdrAndRestoresScope()
        {
            var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(RoverAssets.Bench,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);yield return null;yield return null;var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();
                var parts=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RoverModelParts>()).Single();Assert.That(parts.WheelCount,Is.EqualTo(6));Assert.That(camera.allowHDR,Is.True);Assert.That(((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsHDR,Is.True);Assert.That(PlayerSettings.allowHDRDisplaySupport,Is.False);Assert.That(SystemInfo.graphicsDeviceType,Is.EqualTo(GraphicsDeviceType.Direct3D11));
                var path=RoverAssets.Evidence+"/Rover_"+Guid.NewGuid().ToString("N")+".png";var target=camera.targetTexture;var type=camera.cameraType;float aspect=camera.aspect;Color[] pixels;
                using(var capture=new SalvageFrameCapture(camera))pixels=capture.Capture(path);
                Assert.That(camera.targetTexture,Is.SameAs(target));Assert.That(camera.cameraType,Is.EqualTo(type));Assert.That(camera.aspect,Is.EqualTo(aspect).Within(.001f));
                Assert.That(pixels.Count(p=>p.grayscale>.15f),Is.GreaterThan(1000));Assert.That(pixels.Count(p=>p.r>.8f&&p.g<.05f&&p.b>.8f),Is.LessThan(pixels.Length/1000));
                File.WriteAllText(Path.ChangeExtension(path,".json"),new JObject{{"image",path},{"width",1920},{"height",1080},{"api",SystemInfo.graphicsDeviceType.ToString()},{"internalHdr",true},{"displayHdr",false},{"scope","static asset studio; movement/light state binding not yet tested"}}.ToString());
            }
            finally{SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
            Assert.That(original.isDirty,Is.False);
        }
    }
}
