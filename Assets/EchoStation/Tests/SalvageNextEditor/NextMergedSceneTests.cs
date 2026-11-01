using System;
using System.IO;
using System.Linq;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextMergedSceneTests
    {
        [Test]public void SavedRoomMeshesReopenWithAtlasBindingsAndAllTriangleCounts()
        {
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);var evidence=JArray.Parse(File.ReadAllText(NextMeshMerge.Evidence+"/Geometry_A.json"));foreach(var room in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RoomVisual>())){var renderers=room.GetComponentsInChildren<MeshRenderer>();var row=evidence.Single(r=>r["room"].Value<int>()==room.Index);Assert.That(renderers.Length,Is.EqualTo(row["afterRenderers"].Value<int>()));Assert.That(room.GetComponentsInChildren<MeshFilter>().Sum(f=>(long)f.sharedMesh.GetIndexCount(0)),Is.EqualTo(row["triangleCorners"].Value<long>()));foreach(var renderer in renderers){var binding=renderer.GetComponent<MergedRoomMesh>();Assert.That(binding,Is.Not.Null);binding.Apply();Assert.That(renderer.lightmapIndex,Is.EqualTo(binding.Atlas));Assert.That(renderer.lightmapIndex,Is.InRange(0,LightmapSettings.lightmaps.Length-1));Assert.That(renderer.lightmapScaleOffset,Is.EqualTo(new Vector4(1,1,0,0)));Assert.That(AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh),Does.StartWith(FacilityAssets.Generated+"/ES_Next_Room"));}}}finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
