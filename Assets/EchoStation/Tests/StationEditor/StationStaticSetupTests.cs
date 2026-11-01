using System.Linq;
using EchoStation.Player;
using EchoStation.Station;
using EchoStation.World;
using EchoStation.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace EchoStation.Tests.Station
{
 public sealed class StationStaticSetupTests
 {
  [Test]public void W06_B101_StaticMeshesHaveOwnedNonOverlappingUVAndRoomLimitedCombines()
  {
   var old=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(StationLayout.ScenePath,OpenSceneMode.Additive);
   try{var root=s.GetRootGameObjects().Single(r=>r.name=="ES_Station_Greybox");var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject,StaticEditorFlags.ContributeGI)).ToArray();Assert.That(renderers.Length,Is.InRange(8,24));
    foreach(var r in renderers){var mesh=r.GetComponent<MeshFilter>().sharedMesh;Assert.That(AssetDatabase.GetAssetPath(mesh),Does.StartWith(StationLayout.DataFolder+"/Meshes/"));Assert.That(EchoStation.World.Editor.LightmapUvValidator.IsValidNonOverlapping(mesh,out var error),Is.True,error);Assert.That(r.receiveGI,Is.EqualTo(ReceiveGI.Lightmaps));Assert.That(r.HasPropertyBlock(),Is.False);}
    Assert.That(root.GetComponentsInChildren<MeshRenderer>().Count(r=>!r.enabled&&r.name.StartsWith("Main_")),Is.GreaterThan(10));
    foreach(var device in root.GetComponentsInChildren<StationDevice>())Assert.That(device.name,Does.Not.StartWith("S2_Combined_"));
   }finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(s,true);}
  }
  [Test]public void W06_B102_LightingTemplateAndProbeAndDynamicSourcesMatchRecordedParameters()
  {
   var old=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(StationLayout.ScenePath,OpenSceneMode.Additive);
   try{SceneManager.SetActiveScene(s);var settings=Lightmapping.lightingSettings;Assert.That(AssetDatabase.GetAssetPath(settings),Does.StartWith(StationLayout.DataFolder));Assert.That(settings.bakedGI,Is.True);Assert.That(settings.realtimeGI,Is.False);Assert.That(settings.lightmapResolution,Is.EqualTo(12));Assert.That(settings.lightmapMaxSize,Is.EqualTo(1024));Assert.That(settings.indirectSampleCount,Is.EqualTo(128));Assert.That(settings.mixedBakeMode,Is.EqualTo(MixedLightingMode.Shadowmask));
    var roots=s.GetRootGameObjects();Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<LightProbeGroup>()).Single().probePositions.Length,Is.GreaterThan(200));Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<Light>()).Count(l=>l.lightmapBakeType==LightmapBakeType.Baked),Is.GreaterThanOrEqualTo(13));
    Assert.That(roots.Single(r=>r.name=="Station_LabFault").GetComponent<Light>().lightmapBakeType,Is.EqualTo(LightmapBakeType.Realtime));Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single(c=>c.CompareTag("MainCamera")).useOcclusionCulling,Is.True);Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<DistanceShadowCaster>()).Single().DisableDistance,Is.EqualTo(30));Assert.That(RenderSettings.fog,Is.False);
   }finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(s,true);}
  }
 }
}
