using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.ParkourArt.Editor;
using SceneFrameCapture=EchoStation.ParkourArt.Editor.ParkourFrameCapture;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.ParkourArt.Tests
{
    public sealed class VectorCityMaterialTests
    {
        [Test] public void PK02_ActualShaderCompilesAndAllMaterialsAreTextureFree()
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(VectorCityBuilder.Root+"/Shaders/VectorCityPBR.shader");
            Assert.That(shader,Is.Not.Null);Assert.That(shader.isSupported,Is.True);Assert.That(ShaderUtil.ShaderHasError(shader),Is.False);
            var paths=AssetDatabase.FindAssets("t:Material",new[]{VectorCityBuilder.Root+"/Materials"});Assert.That(paths.Length,Is.EqualTo(6));
            foreach(var id in paths){var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));Assert.That(m.shader,Is.EqualTo(shader));Assert.That(m.GetFloat("_Roughness"),Is.InRange(.04f,1f));Assert.That(m.GetTexturePropertyNames().All(p=>m.GetTexture(p)==null),Is.True,m.name);Assert.That(m.FindPass("ForwardLit"),Is.GreaterThanOrEqualTo(0));Assert.That(m.FindPass("ShadowCaster"),Is.GreaterThanOrEqualTo(0));Assert.That(m.FindPass("DepthOnly"),Is.GreaterThanOrEqualTo(0));}
        }
        [Test] public void PK02_ActualImageIsNonblackAndRoughnessChangesSpecularResponse()
        {
            var scene=EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single);var camera=Object.FindAnyObjectByType<Camera>();
            var path="Docs/Tests/ParkourArt/Evidence/PK02";Directory.CreateDirectory(path);
            try{using(var frames=new SceneFrameCapture(camera)){
                var first=frames.Capture(path+"/PBR_Hero.png");Assert.That(first.Average(c=>(c.r+c.g+c.b)/3),Is.GreaterThan(.03));
                Assert.That(ShaderUtil.ShaderHasError(Shader.Find("EchoStation/Parkour/Vector City PBR")),Is.False);
                // Change roughness on the actual visible terrace, then restore in finally.
                var surface=GameObject.Find("Planar_Atrium_Floor").GetComponent<Renderer>();var original=surface.sharedMaterial;var test=new Material(original);surface.sharedMaterial=test;
                var pos=camera.transform.position;var rot=camera.transform.rotation;
                try{camera.transform.position=new Vector3(-7,17,-3);camera.transform.LookAt(new Vector3(4,15.1f,5));test.SetFloat("_Metallic",1);test.SetFloat("_Roughness",.06f);var smooth=frames.Capture(path+"/Roughness006.png");test.SetFloat("_Roughness",.85f);var rough=frames.Capture(path+"/Roughness085.png");var delta=SceneFrameCapture.Difference(smooth,rough);Assert.That(delta,Is.GreaterThan(.00001f));File.WriteAllText(path+"/RoughnessActual.json","{\"imageMeanAbsoluteDifference\":"+delta.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");}
                finally{surface.sharedMaterial=original;Object.DestroyImmediate(test);camera.transform.SetPositionAndRotation(pos,rot);}
            }}finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        }
    }
}
