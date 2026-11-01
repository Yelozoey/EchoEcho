using System.IO;
using System.Linq;
using EchoStation.Station;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.Tests.Station
{
 public sealed class StationLightmapTests
 {
  [Test]public void W06_B201_AllStaticReceiversReferenceActualOwnedDirectionalLightmaps()
  {
   var old=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(StationLayout.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(s);var maps=LightmapSettings.lightmaps;Assert.That(maps.Length,Is.GreaterThan(0));foreach(var map in maps){Assert.That(map.lightmapColor,Is.Not.Null);Assert.That(map.lightmapDir,Is.Not.Null);foreach(var tex in new[]{map.lightmapColor,map.lightmapDir}){var path=AssetDatabase.GetAssetPath(tex);Assert.That(path,Does.StartWith("Assets/EchoStation/Station/"));Assert.That(File.Exists(path),Is.True);Assert.That(tex.width,Is.LessThanOrEqualTo(1024));}}
    var root=s.GetRootGameObjects().Single(r=>r.name=="ES_Station_Greybox");foreach(var r in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject,StaticEditorFlags.ContributeGI))){Assert.That(r.lightmapIndex,Is.InRange(0,maps.Length-1),r.name);Assert.That(r.lightmapScaleOffset.x,Is.GreaterThan(0));Assert.That(r.lightmapScaleOffset.y,Is.GreaterThan(0));}
   }finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(s,true);}
  }
  [Test]public void W06_B202_ActualBakedProbeFieldProvidesNonzeroIrradianceAcrossFiveZones()
  {
   var old=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(StationLayout.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(s);Assert.That(LightmapSettings.lightProbes,Is.Not.Null);Assert.That(LightmapSettings.lightProbes.count,Is.GreaterThan(200));LightProbes.Tetrahedralize();foreach(var p in new[]{new Vector3(0,1,-30),new Vector3(0,1,0),new Vector3(0,-3,30),new Vector3(0,-3,48),new Vector3(0,-3,68)}){LightProbes.GetInterpolatedProbe(p,null,out var sh);Assert.That(sh[0,0]+sh[1,0]+sh[2,0],Is.GreaterThan(.0001f),p.ToString());}}
   finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(s,true);}
  }
 }
}
