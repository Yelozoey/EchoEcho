using System.Linq;
using EchoStation.InfiltrationArt;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationRefinement;
using EchoStation.InfiltrationRefinement.Editor;
using EchoStation.InfiltrationMetal.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.InfiltrationMetal.Tests
{
    public sealed class MetalArtTests
    {
        Scene previous,sample;Transform world;
        [SetUp]public void Load(){previous=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(MetalScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);world=MetalScene.World(sample);Physics.SyncTransforms();}
        [Test]public void FourRealRobotsHaveTwoStaticAndTwoFuturePatrolPostsWithoutAiComponents()
        {
            var posts=world.GetComponentsInChildren<LevelPost>();Assert.That(posts.Length,Is.EqualTo(4));Assert.That(posts.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(4));Assert.That(posts.Count(p=>p.FuturePatrol),Is.EqualTo(2));
            foreach(var p in posts){Assert.That(p.GetComponent<ArtSampleModel>().Role,Is.EqualTo("Guard"));Assert.That(p.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Any(c=>c.isLooping),Is.True);Assert.That(p.GetComponent<UnityEngine.AI.NavMeshAgent>(),Is.Null);}
            Assert.That(world.GetComponentsInChildren<MonoBehaviour>().Where(m=>m).Any(m=>m.GetType().Namespace!=null&&m.GetType().Namespace.StartsWith("EchoStation.AI")),Is.False);
        }
        [Test]public void FiveRoutePreviewsAndLegibleWorldSignsExistAsClearlyLabeledArt()
        {
            var routes=world.GetComponentsInChildren<LevelRoutePreview>();Assert.That(routes.Length,Is.EqualTo(5));Assert.That(routes.Count(r=>r.Id.StartsWith("Patrol_")),Is.EqualTo(2));foreach(var r in routes){Assert.That(r.Points.Length,Is.GreaterThan(3));Assert.That(r.GetComponent<LineRenderer>().positionCount,Is.EqualTo(r.Points.Length));}
            var labels=world.GetComponentsInChildren<TextMesh>();Assert.That(labels.Any(t=>t.text.Contains("机制待接入")),Is.True);Assert.That(labels.Count(t=>t.text.Contains("AI未启用")),Is.EqualTo(4));Assert.That(labels.All(t=>t.GetComponent<Renderer>().sharedMaterial.shader.name=="EchoStation/InfiltrationLevel/WorldLabel"),Is.True);
        }
        [Test]public void AdministrativeAndCargoScreensActuallyOccludeStandingSightlines()
        {
            Assert.That(Physics.Linecast(new Vector3(-16,1.6f,-4),new Vector3(-16,1.6f,0),out var office)&&office.collider.name=="Admin_PrivacyScreen",Is.True);
            Assert.That(Physics.Linecast(new Vector3(12,1.6f,-5.3f),new Vector3(17,1.6f,-5.3f),out var cargo)&&cargo.collider.name=="Cargo_TallCover",Is.True);
        }
        [Test]public void CoreObservationWindowIsTransparentAndHasActualSafeCollision()
        {
            var window=world.Find("ObservationWindow_Glass");Assert.That(window,Is.Not.Null);Assert.That(window.GetComponent<MeshFilter>().sharedMesh.vertexCount,Is.EqualTo(4));Assert.That(AssetDatabase.GetAssetPath(window.GetComponent<MeshFilter>().sharedMesh).StartsWith(MetalScene.Root+"/"),Is.True);
            Assert.That(window.GetComponent<Renderer>().sharedMaterial.GetFloat("_Surface"),Is.EqualTo(1));Assert.That(Physics.Linecast(new Vector3(0,1.6f,14),new Vector3(0,1.6f,10),out var hit)&&hit.collider.transform==window,Is.True);
        }
        [Test]public void ActualMajorObjectsHaveSourceMeshesAndOwnedPbrMaterials()
        {
            var models=world.GetComponentsInChildren<ArtSampleModel>();Assert.That(models,Is.Not.Empty);foreach(string role in new[]{"Desk","Chair","EquipmentCase","Terminal","Guard","Echo","Toolbox"})Assert.That(models.Any(m=>m.Role==role),Is.True,role);
            foreach(var renderer in models.SelectMany(m=>m.GetComponentsInChildren<Renderer>()))foreach(var material in renderer.sharedMaterials){Assert.That(material&&material.shader.name=="Universal Render Pipeline/Lit",Is.True);Assert.That(AssetDatabase.GetAssetPath(material).StartsWith(MetalScene.Generated+"/"),Is.True);}
            Assert.That(LevelLayout.ConservativeNetArea(world),Is.GreaterThanOrEqualTo(864));
        }
        [TearDown]public void Close(){if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);}
    }
}
