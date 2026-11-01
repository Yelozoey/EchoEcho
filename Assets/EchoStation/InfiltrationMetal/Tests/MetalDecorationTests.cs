using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationRefinement;
using EchoStation.InfiltrationRefinement.Editor;
using EchoStation.InfiltrationMetal.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace EchoStation.InfiltrationMetal.Tests
{
    public sealed class MetalDecorationTests
    {
        Scene previous,sample;Transform root;
        [SetUp]public void Load(){previous=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(MetalScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);root=MetalScene.World(sample);Physics.SyncTransforms();}
        [Test]public void SeventyEightRealDetailsCoverAllSixZonesWithTenKinds()
        {
            var items=root.GetComponentsInChildren<RefinementDecoration>();Assert.That(items.Length,Is.EqualTo(78));Assert.That(items.Select(i=>i.Kind).Distinct().Count(),Is.EqualTo(10));Assert.That(items.Select(i=>i.Zone).Distinct().OrderBy(z=>z),Is.EqualTo(Enumerable.Range(0,6)));
            foreach(var item in items){var art=item.GetComponent<ArtSampleModel>();Assert.That(art.SourceAsset,Is.Not.Empty);Assert.That(item.GetComponentsInChildren<MeshFilter>().All(f=>f.sharedMesh&&f.sharedMesh.vertexCount>10&&!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(f.sharedMesh))),Is.True);Assert.That(item.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>AssetDatabase.GetAssetPath(m).StartsWith(MetalScene.Generated+"/"))),Is.True);}
        }
        [Test]public void AdaptedPropsHaveReadableScaleAtlasesUvAndActualObstacleContracts()
        {
            foreach(string kind in RefinementAssets.Kinds)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(RefinementAssets.Prefab(kind));Assert.That(prefab,Is.Not.Null);var b=ArtSampleAssets.LocalBoundsOf(prefab);Assert.That(b.size.magnitude,Is.InRange(.1f,4.5f),kind);
                if(kind=="Mug"){Assert.That(b.size.y,Is.EqualTo(.14f).Within(.003));Assert.That(prefab.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m.GetTexture("_BaseMap"))),Is.True,"Mug must use actual author atlas");}
                if(kind=="Locker")Assert.That(b.size.y,Is.InRange(1.8f,2.2f));
                foreach(var f in prefab.GetComponentsInChildren<MeshFilter>())Assert.That(f.sharedMesh.uv2.Length,Is.GreaterThan(0),kind+" needs authored/generated lightmap UVs");
            }
            foreach(var item in root.GetComponentsInChildren<RefinementDecoration>())
            {
                Assert.That(item.GetComponents<Collider>().Length,Is.EqualTo(item.FloorObstacle?1:0));
                if(item.Kind=="Mug"||item.Kind=="Computer"){bool hit=Physics.Raycast(item.transform.position+Vector3.up*.01f,Vector3.down,out var support,.04f);var art=hit?support.collider.GetComponentInParent<ArtSampleModel>():null;Assert.That(art&&art.Role=="Desk",Is.True,item.name+" must rest on an actual desk");}
                if(item.Kind=="Fan"||item.Kind=="Vent"||item.Kind=="Cable")Assert.That(ArtSampleAssets.BoundsOf(item.gameObject).min.y,Is.GreaterThan(2.1f),item.name+" overhead clearance");
            }
        }
        [Test]public void OldSceneAndPrivateRendererRemainSeparateWithoutNewGameplay()
        {
            Assert.That(MetalScene.ScenePath,Is.Not.EqualTo(EchoStation.InfiltrationLevel.Editor.LevelBuilder.ScenePath));Assert.That(File.Exists(EchoStation.InfiltrationLevel.Editor.LevelBuilder.ScenePath),Is.True);Assert.That(LevelLayout.ConservativeNetArea(root),Is.GreaterThanOrEqualTo(864));
            var preview=root.GetComponentInChildren<LevelPreview>();var p=new SerializedObject(preview).FindProperty("pipeline").objectReferenceValue;Assert.That(AssetDatabase.GetAssetPath(p),Is.EqualTo(MetalScene.PipelinePath));
            Assert.That(root.GetComponentsInChildren<RefinementDecoration>().Any(i=>i.GetComponent<UnityEngine.AI.NavMeshAgent>()),Is.False);Assert.That(preview.AiEnabled,Is.False);
        }
        [TearDown]public void Close(){if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);}
    }
}
