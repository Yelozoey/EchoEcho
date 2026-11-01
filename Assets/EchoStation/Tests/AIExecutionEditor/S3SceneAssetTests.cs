using System.Linq;
using EchoStation.AI.GameIntegration;
using EchoStation.Station;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3SceneAssetTests
 {
  [Test]public void DedicatedSceneHasOneBootstrapDriverAndIndependentNavigationWithAllFiveZones()
  {
   Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(S3Bootstrap.ScenePath),Is.Not.Null);var navigation=AssetDatabase.LoadAssetAtPath<NavMeshData>(S3Bootstrap.NavigationPath);Assert.That(navigation,Is.Not.Null);
   var scene=EditorSceneManager.OpenPreviewScene(S3Bootstrap.ScenePath);
   try
   {
    var roots=scene.GetRootGameObjects();var player=roots.Single(r=>r.GetComponent<StationSession>());Assert.That(player.GetComponents<S3Bootstrap>().Length,Is.EqualTo(1));Assert.That(player.GetComponents<S3DialogueDriver>().Length,Is.EqualTo(1));
    var surface=roots.SelectMany(r=>r.GetComponentsInChildren<NavMeshSurface>()).Single();Assert.That(surface.navMeshData,Is.SameAs(navigation));Assert.That(AssetDatabase.AssetPathToGUID(S3Bootstrap.NavigationPath),Is.Not.EqualTo(AssetDatabase.AssetPathToGUID("Assets/EchoStation/Station/Generated/StationNavMesh.asset")));
    foreach(var zone in StationLayout.Zones)Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<Transform>()).Any(t=>t.name=="Zone_"+zone.Id),Is.True,zone.Id);Assert.That(scene.isDirty,Is.False);
   }
   finally{EditorSceneManager.ClosePreviewScene(scene);}
  }
 }
}
