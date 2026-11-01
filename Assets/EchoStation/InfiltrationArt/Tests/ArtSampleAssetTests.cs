using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EchoStation.InfiltrationArt.Tests
{
    public sealed class ArtSampleAssetTests
    {
        [Test]
        public void EveryRequiredRoleHasTraceableRealGeometry()
        {
            foreach (string role in ArtSampleAssets.Roles)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor(role));
                Assert.That(prefab, Is.Not.Null, role);
                var marker = prefab.GetComponent<ArtSampleModel>();
                Assert.That(marker.Role, Is.EqualTo(role));
                Assert.That(AssetDatabase.LoadMainAssetAtPath(marker.SourceAsset), Is.Not.Null);
                var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
                foreach(var indicator in filters.Where(f=>f.name=="RoverStatusIndicator"))
                    Assert.That(indicator.GetComponent<Renderer>().bounds.size.magnitude,Is.LessThan(.08f),"Only the tiny designed status indicator may use simple geometry");
                var meshes = filters.Where(f=>f.name!="RoverStatusIndicator").Select(f => f.sharedMesh)
                    .Concat(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.sharedMesh)).ToArray();
                Assert.That(meshes, Is.Not.Empty, role);
                Assert.That(meshes.All(m => m && m.vertexCount >= 3), Is.True, role);
                Assert.That(meshes.All(m => AssetDatabase.GetAssetPath(m).StartsWith("Assets/")), Is.True, "Built-in primitive is not a source model: " + role);
            }
        }
        [Test]
        public void MaterialsAreOwnedUrpAssetsAndSourceNotMutated()
        {
            foreach (string role in ArtSampleAssets.Roles)
            foreach (var renderer in AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor(role)).GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                Assert.That(material, Is.Not.Null, role);
                Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), role);
                Assert.That(AssetDatabase.GetAssetPath(material).StartsWith(ArtSampleAssets.Generated + "/"), Is.True, role);
            }
        }
        [Test]
        public void GuardHasActualSourceIdleAndNoGameplayScripts()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor("Guard"));
            var animator = prefab.GetComponentInChildren<Animator>();
            Assert.That(animator && animator.runtimeAnimatorController, Is.True);
            Assert.That(animator.runtimeAnimatorController.animationClips.Any(c => c.length > .1f && c.name.ToLowerInvariant().Contains("idle")), Is.True);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true).All(c => c is ArtSampleModel), Is.True);
        }
        [TestCase("Guard",1.65f,1.95f)]
        [TestCase("Chair",.9f,1.3f)]
        [TestCase("Door",3f,3.4f)]
        public void NormalizedNearObjectsHavePlausibleHeight(string role,float min,float max)
        {
            var bounds=ArtSampleAssets.BoundsOf(AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor(role)));
            Assert.That(bounds.size.y,Is.InRange(min,max));
            Assert.That(bounds.min.y,Is.EqualTo(0).Within(.02f));
        }
        [Test]
        public void FurnitureAndDoorHaveActualTextureAtlases()
        {
            foreach(var role in new[]{"Desk","Chair","Door"})
            {
                var p=AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor(role));
                Assert.That(p.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Any(m=>m.GetTexture("_BaseMap")),Is.True,role+" cannot be an untextured import fallback");
            }
        }
        [Test]
        public void ToolAndTerminalCanBeRecognizedAsDifferentSourceModels()
        {
            var tool=AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor("Toolbox"));
            var terminal=AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor("Terminal"));
            Assert.That(tool.GetComponent<ArtSampleModel>().SourceAsset,Is.Not.EqualTo(terminal.GetComponent<ArtSampleModel>().SourceAsset));
            Assert.That(ArtSampleAssets.BoundsOf(tool).size.x,Is.InRange(.35f,.8f));
            Assert.That(ArtSampleAssets.BoundsOf(terminal).size.y,Is.InRange(1.2f,1.9f));
        }
    }
}
