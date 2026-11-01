using System;
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
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextArtV2LightingTests
    {
        static double Lum(Color c)=>.2126*c.r+.7152*c.g+.0722*c.b;
        static Color[] Picture(string path,int room,string output,bool post)
        {
            var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single();foreach(var l in root.GetComponentsInChildren<Light>(true))if(l.lightmapBakeType==LightmapBakeType.Realtime)l.enabled=false;
                var camera=root.GetComponentInChildren<Camera>();camera.scene=scene;camera.transform.position=new Vector3(6+12*room,1.65f,13);camera.transform.LookAt(new Vector3(6+12*room,2.8f,23.5f));camera.GetUniversalAdditionalCameraData().renderPostProcessing=post;
                using(var capture=new SalvageFrameCapture(camera)){capture.Capture(output);return capture.Capture(output);}
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
        [Test]public void BakedGeometryHasOwnedDirectionalMapsAndRealReadableFrames()
        {
            var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextArtV2Layout.Authoring,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);Assert.That(AssetDatabase.GetAssetPath(Lightmapping.lightingSettings),Is.EqualTo(NextArtV2Lighting.Settings));Assert.That(LightmapSettings.lightmaps.Length,Is.EqualTo(2));Assert.That(LightmapSettings.lightmapsMode,Is.EqualTo(LightmapsMode.CombinedDirectional));
                foreach(var map in LightmapSettings.lightmaps){Assert.That(map.lightmapColor,Is.Not.Null);Assert.That(map.lightmapDir,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(map.lightmapColor),Does.StartWith(NextArtV2Layout.Root+"/"));}
                var root=scene.GetRootGameObjects().Single();var meshes=root.GetComponentsInChildren<RoomVisual>().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).ToArray();Assert.That(meshes.Length,Is.EqualTo(200));foreach(var renderer in meshes){Assert.That(renderer.lightmapIndex,Is.InRange(0,1));Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh.uv2.Length,Is.EqualTo(renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount));}
                Assert.That(AssetDatabase.LoadAssetAtPath<LightProbes>(NextArtV2Lighting.Probes).count,Is.EqualTo(234));foreach(var p in root.GetComponentsInChildren<ReflectionProbe>()){Assert.That(p.customBakedTexture,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(p.customBakedTexture),Does.StartWith(NextArtV2Layout.Generated));}
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
            var rows=new JArray();string id=Guid.NewGuid().ToString("N");for(int room=0;room<3;room++){string output=NextArtV2Lighting.Evidence+"/LitB_Room"+room+"_"+id+".png";var p=Picture(NextArtV2Layout.Authoring,room,output,true);double mean=p.Average(Lum),clipped=p.Count(c=>c.r>=.99f&&c.g>=.99f&&c.b>=.99f)/(double)p.Length;Assert.That(mean,Is.GreaterThan(.01),"Actual scene must not be black");Assert.That(clipped,Is.LessThan(.05),"Widespread white clipping");rows.Add(new JObject{{"room",room},{"meanLinear",mean},{"whiteClippedFraction",clipped},{"image",output}});}File.WriteAllText(NextArtV2Lighting.Evidence+"/Images_"+id+".json",rows.ToString());
        }
        [Test]public void MergedMeshesPreserveGeometryAndBakedImage()
        {
            var rows=new JArray();for(int room=0;room<3;room++){var a=Picture(NextArtV2Layout.Authoring,room,NextArtV2Lighting.Evidence+"/MergeBefore_"+room+".png",false);var b=Picture(NextArtV2Lighting.Merged,room,NextArtV2Lighting.Evidence+"/MergeAfter_"+room+".png",false);float difference=SalvageFrameCapture.Difference(a,b);rows.Add(new JObject{{"room",room},{"linearMeanAbsoluteDifference",difference}});File.WriteAllText(NextArtV2Lighting.Evidence+"/MergeImages_A.json",rows.ToString());Assert.That(difference,Is.LessThan(.015),"Merged lighting differs: room "+room);}
        }
    }
}
