using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EchoStation.ParkourArt.Editor;

namespace EchoStation.ParkourArt.Tests
{
    public sealed class VectorCityGeometryTests
    {
        [SetUp] public void OpenOwnedScene() { EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single); }
        [TearDown] public void CloseOwnedScene() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        [Test] public void PK01_GeometryUsesUnityPrimitivesAndAllRoofsHaveCollision()
        {
            var scene=SceneManager.GetActiveScene();Assert.That(scene.path,Is.EqualTo(VectorCityBuilder.ScenePath));
            Assert.That(scene.isDirty,Is.False);
            var root=scene.GetRootGameObjects().Single(g=>g.hideFlags==HideFlags.None);
            var meshes=root.GetComponentsInChildren<MeshFilter>();Assert.That(meshes.Length,Is.GreaterThan(180));
            foreach(var mesh in meshes){Assert.That(mesh.sharedMesh,Is.Not.Null);Assert.That(new[]{"Cube","Plane","Cylinder"},Does.Contain(mesh.sharedMesh.name));Assert.That(AssetDatabase.GetAssetPath(mesh.sharedMesh),Does.Not.StartWith("Assets/"));}
            foreach(Transform t in root.transform)if(t.name.StartsWith("0")&&t.name.Contains(" / ")&&t.name[1]>='1'&&t.name[1]<='5')
                Assert.That(t.Find("Roof_Cap").GetComponent<BoxCollider>(),Is.Not.Null);
            var cameras=root.GetComponentsInChildren<Camera>().Where(c=>c.gameObject.hideFlags==HideFlags.None).ToArray();
            Assert.That(cameras.Length,Is.EqualTo(2));Assert.That(cameras.Count(c=>c.enabled),Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<Light>().Single().type,Is.EqualTo(LightType.Directional));
        }
        [Test] public void PK01_RouteHasClearLandingPointsAndLimitedJumpGaps()
        {
            Physics.SyncTransforms();var route=GameObject.Find("07 — Route / Jump landing markers").transform;
            Assert.That(route.childCount,Is.EqualTo(16));
            foreach(Transform node in route){Assert.That(Physics.Raycast(node.position+Vector3.up*.3f,Vector3.down,out var hit,1),Is.True,node.name);Assert.That(hit.point.y,Is.EqualTo(node.position.y).Within(.15f),node.name);}
            foreach(var indices in new[]{new[]{1,2},new[]{5,6},new[]{9,10},new[]{13,14}}){var a=route.GetChild(indices[0]).position;var b=route.GetChild(indices[1]).position;Assert.That(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z)),Is.LessThanOrEqualTo(3.4f));Assert.That(b.y-a.y,Is.LessThanOrEqualTo(1.1f));}
        }
    }
}
