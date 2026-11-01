using System;
using System.IO;
using System.Linq;
using EchoStation.Salvage;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextArtV2PartitionTests
    {
        [Test]public void PersistentWorldAndVisualRoomsAreIndependentAndNavigationConnectsAllCargo()
        {
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextArtV2Stream.Main,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().Single();Assert.That(world.GetComponentsInChildren<RoomVisual>(true).Length,Is.Zero);Assert.That(world.GetComponentsInChildren<Collider>(true).Length,Is.EqualTo(120));Assert.That(world.GetComponent<NextProbeScope>().Probes.count,Is.EqualTo(234));Assert.That(AssetDatabase.GetAssetPath(world.GetComponent<Unity.AI.Navigation.NavMeshSurface>().navMeshData),Is.EqualTo(NextArtV2Stream.Nav));
                var stream=world.GetComponentInChildren<NextRoomStreaming>();Assert.That(new SerializedObject(stream).FindProperty("addressPrefix").stringValue,Is.EqualTo(NextArtV2Stream.Prefix));var rows=new JArray();
                foreach(var target in world.GetComponentsInChildren<CargoActor>())
                {
                    Assert.That(NavMesh.SamplePosition(target.transform.position,out var hit,1,NavMesh.AllAreas),Is.True);var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(new Vector3(5,0,12),hit.position,NavMesh.AllAreas,path),Is.True);Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),target.Id);rows.Add(new JObject{{"target",target.Id},{"completePath",true}});
                }
                for(int i=0;i<3;i++)
                {
                    var roomScene=EditorSceneManager.OpenScene(NextArtV2Stream.RoomPath(i),OpenSceneMode.Additive);
                    try{var root=roomScene.GetRootGameObjects().Single();Assert.That(root.activeSelf,Is.False);Assert.That(root.GetComponentsInChildren<Collider>(true).Length,Is.Zero);var room=root.GetComponent<RoomPackage>();Assert.That(room.Index,Is.EqualTo(i));Assert.That(room.MapCount,Is.EqualTo(2));Assert.That(room.RendererCount,Is.EqualTo(new[]{19,16,17}[i]));Assert.That(root.GetComponent<RoomVisual>().Region.max.y,Is.EqualTo(NextArtV2Layout.Height(i)));Assert.That(root.GetComponent<InstancedRoomLamps>().InstanceCount,Is.EqualTo(6));for(int a=0;a<2;a++)Assert.That(AssetDatabase.GetAssetPath(room.ColorMap(a)),Does.StartWith(NextArtV2Layout.Generated+"/Maps/Room"+i));}
                    finally{SceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(roomScene,true);}
                }
                File.WriteAllText(NextArtV2Stream.Evidence+"/PartitionValidation_A.json",new JObject{{"cargoPaths",rows},{"mainColliders",120},{"roomRenderers",new JArray(19,16,17)},{"uniqueRoomAtlases",true},{"newPrefix",NextArtV2Stream.Prefix}}.ToString());
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
