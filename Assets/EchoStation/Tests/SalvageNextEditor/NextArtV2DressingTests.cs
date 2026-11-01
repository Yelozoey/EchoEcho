using System;
using System.IO;
using System.Linq;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextArtV2DressingTests
    {
        [Test]public void LandmarksAreDistinctAndLeavePlayerClearanceAndPhysicsUntouched()
        {
            var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextArtV2Layout.Authoring,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var rooms=scene.GetRootGameObjects().Single().GetComponentsInChildren<RoomVisual>();var rows=new JArray();foreach(var room in rooms)
                {
                    var landmark=room.transform.Find("ArtV2_Landmark_"+room.Index);Assert.That(landmark,Is.Not.Null);Assert.That(landmark.GetComponentsInChildren<Collider>().Length,Is.Zero);Assert.That(landmark.Find("ArtV2_ZoneSign_"+room.Index),Is.Not.Null);
                    foreach(Transform child in landmark){var b=NextLayout.BoundsOf(child.gameObject);Assert.That(b.max.y,Is.LessThanOrEqualTo(NextArtV2Layout.Height(room.Index)+.01));if(child.name.StartsWith("Dock_",StringComparison.Ordinal)&&child.name.EndsWith("Guide",StringComparison.Ordinal))Assert.That(b.max.y,Is.LessThan(.11));else Assert.That(b.min.y,Is.GreaterThanOrEqualTo(2.1f),child.name);}
                    string expected=room.Index==0?"Dock_HeaderRail":room.Index==1?"Storage_WallShelf_0":"Maintenance_Extractor";Assert.That(landmark.Find(expected),Is.Not.Null);foreach(var f in landmark.GetComponentsInChildren<MeshFilter>()){Assert.That(f.sharedMesh,Is.Not.Null);Assert.That(f.sharedMesh.uv2.Length,Is.EqualTo(f.sharedMesh.vertexCount));}
                    rows.Add(new JObject{{"room",room.Index},{"focus",expected},{"overheadClearanceAtLeast",2.1},{"newPhysicsObjects",0},{"renderers",landmark.GetComponentsInChildren<MeshRenderer>().Length}});
                }
                File.WriteAllText(NextArtV2Dressing.Evidence+"/Clearance_"+Guid.NewGuid().ToString("N")+".json",rows.ToString());
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
