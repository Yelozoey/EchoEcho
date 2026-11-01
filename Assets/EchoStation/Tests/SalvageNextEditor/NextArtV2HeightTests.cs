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
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextArtV2HeightTests
    {
        [Test]public void ActualRoomsHaveRaisedCeilingsAndContinuousUpperWallsWithoutScalingGameplay()
        {
            var previous=SceneManager.GetActiveScene();var baseline=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);var scene=EditorSceneManager.OpenScene(NextArtV2Layout.Authoring,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().Single();var original=baseline.GetRootGameObjects().Single();var rows=new JArray();
                foreach(var room in world.GetComponentsInChildren<RoomVisual>())
                {
                    float h=NextArtV2Layout.Height(room.Index);Assert.That(room.Region.max.y,Is.EqualTo(h));var tiles=room.transform.Cast<Transform>().Where(t=>t.name.StartsWith("ES_Platform_Simple",StringComparison.Ordinal)).ToArray();Assert.That(tiles.Length,Is.EqualTo(18));
                    foreach(var tile in tiles){var b=NextLayout.BoundsOf(tile.gameObject);Assert.That(b.center.y,Is.EqualTo(h).Within(.001));Assert.That(b.size.x,Is.EqualTo(4).Within(.001));Assert.That(b.size.z,Is.EqualTo(4).Within(.001));}
                    var upper=room.transform.Cast<Transform>().Where(t=>t.name.StartsWith("ArtV2_UpperWall_",StringComparison.Ordinal)||t.name.StartsWith("ArtV2_DoorLintel_",StringComparison.Ordinal)).ToArray();Assert.That(upper.Length,Is.GreaterThanOrEqualTo(5));foreach(var wall in upper){var b=NextLayout.BoundsOf(wall.gameObject);Assert.That(b.min.y,Is.EqualTo(3).Within(.001));Assert.That(b.max.y,Is.EqualTo(h).Within(.001));}
                    foreach(var frame in room.transform.Cast<Transform>().Where(t=>t.name.StartsWith("ES_Door_Frame",StringComparison.Ordinal)))Assert.That(NextLayout.BoundsOf(frame.gameObject).size.y,Is.EqualTo(3).Within(.001));
                    var ceiling=world.transform.Find("ArtV2_CeilingCollision_"+room.Index).GetComponent<BoxCollider>();Assert.That(ceiling.bounds.min.y,Is.EqualTo(h).Within(.001));rows.Add(new JObject{{"room",room.Index},{"clearHeight",h},{"roofTiles",tiles.Length},{"upperPanels",upper.Length}});
                }
                foreach(var actor in world.GetComponentsInChildren<CargoActor>()){var prior=original.GetComponentsInChildren<CargoActor>().Single(a=>a.Id==actor.Id);Assert.That(actor.transform.position,Is.EqualTo(prior.transform.position));Assert.That(actor.transform.localScale,Is.EqualTo(prior.transform.localScale));}
                var cam=world.GetComponentInChildren<Camera>();var oldCam=original.GetComponentInChildren<Camera>();Assert.That(cam.fieldOfView,Is.EqualTo(oldCam.fieldOfView));Assert.That(cam.transform.localPosition,Is.EqualTo(oldCam.transform.localPosition));Assert.That(world.GetComponentInChildren<SalvageEcho>().transform.localScale,Is.EqualTo(original.GetComponentInChildren<SalvageEcho>().transform.localScale));
                File.WriteAllText(NextArtV2Layout.Evidence+"/Geometry_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"rooms",rows},{"cameraFov",cam.fieldOfView},{"cameraLocalHeight",cam.transform.localPosition.y},{"gameplayTransformsUnchanged",true},{"bakeNotYetAccepted",true}}.ToString());
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);EditorSceneManager.CloseScene(baseline,true);}
        }
    }
}
