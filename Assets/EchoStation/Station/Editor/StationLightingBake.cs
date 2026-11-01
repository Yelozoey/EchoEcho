using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.Station.Editor
{
 public static class StationLightingBake
 {
  [Serializable]private class Report {public bool passed,layoutRestored;public string error;public double seconds;public int atlases,probes,receivers,mapped;public string[] colors,directions;}
  private static SceneSetup[] previous;private static DateTime started;private static bool complete,cancelled;private static string folder;
  public static string NextFolder(string prefix){for(var i=1;;i++){var f="Docs/Tests/Stage2/Evidence/S2-W06/"+prefix+"-"+i.ToString("00");if(Directory.Exists(f))continue;Directory.CreateDirectory(f);return f;}}
  [MenuItem("Tools/EchoStation/Stage 2/W06/Bake Station Lighting")]
  public static void Start()
  {
   SceneSafetyGuard.RequireClean();if(EditorApplication.isPlayingOrWillChangePlaymode||Lightmapping.isRunning||previous!=null||StaticOcclusionCulling.isRunning)throw new InvalidOperationException("Another play/bake operation is active.");
   if(PrefabStageUtility.GetCurrentPrefabStage()!=null)throw new InvalidOperationException("Preserving Prefab Stage.");
   previous=EditorSceneManager.GetSceneManagerSetup();started=DateTime.UtcNow;complete=cancelled=false;folder=NextFolder("B2");File.WriteAllText(folder+"/Started.json","{\"startedUtc\":\""+started.ToString("O")+"\"}");
   try{EditorSceneManager.OpenScene(StationLayout.ScenePath,OpenSceneMode.Single);Lightmapping.bakeCompleted+=Completed;Lightmapping.bakeCancelled+=Cancelled;EditorApplication.update+=Tick;if(!Lightmapping.BakeAsync())Finish("BakeAsync did not start.");}catch(Exception e){Finish(e.ToString());}
  }
  private static void Completed()=>complete=true;private static void Cancelled()=>cancelled=true;
  [MenuItem("Tools/EchoStation/Stage 2/W06/Bake Station Occlusion")]
  public static void Occlusion()
  {
   SceneSafetyGuard.RequireClean();if(EditorApplication.isPlayingOrWillChangePlaymode||Lightmapping.isRunning||StaticOcclusionCulling.isRunning)throw new InvalidOperationException("Another play/bake operation is active.");
   if(SceneManager.GetSceneByPath(StationLayout.ScenePath).isLoaded)throw new InvalidOperationException("Preserving open Station scene; close saved owned scene before bake setup.");
   var text=File.ReadAllText(StationLayout.ScenePath);text=System.Text.RegularExpressions.Regex.Replace(text,@"smallestOccluder: [^\r\n]+","smallestOccluder: 2");text=System.Text.RegularExpressions.Regex.Replace(text,@"smallestHole: [^\r\n]+","smallestHole: 0.25");File.WriteAllText(StationLayout.ScenePath,text);AssetDatabase.ImportAsset(StationLayout.ScenePath,ImportAssetOptions.ForceUpdate);
   EchoStation.World.Editor.CabinOcclusionBake.StartBakeFor(StationLayout.ScenePath,NextFolder("B3"));
  }
  private static void Tick(){if((DateTime.UtcNow-started).TotalSeconds>600){Lightmapping.Cancel();Finish("S2 lighting bake exceeded600s.");return;}if(!Lightmapping.isRunning&&(complete||cancelled))Finish(cancelled?"Bake cancelled.":null);}
  private static void Finish(string error)
  {
   Lightmapping.bakeCompleted-=Completed;Lightmapping.bakeCancelled-=Cancelled;EditorApplication.update-=Tick;var result=new Report{error=error,seconds=(DateTime.UtcNow-started).TotalSeconds};
   try{
    var scene=SceneManager.GetActiveScene();if(SceneManager.sceneCount!=1||scene.path!=StationLayout.ScenePath)throw new InvalidOperationException("Layout changed during bake; preserving current scene state.");
    if(error==null){EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();var maps=LightmapSettings.lightmaps;result.atlases=maps.Length;result.probes=LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0;var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_Station_Greybox");var receivers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject,StaticEditorFlags.ContributeGI)).ToArray();result.receivers=receivers.Length;result.mapped=receivers.Count(r=>r.lightmapIndex>=0&&r.lightmapIndex<maps.Length);result.colors=maps.Select(m=>AssetDatabase.GetAssetPath(m.lightmapColor)).ToArray();result.directions=maps.Select(m=>AssetDatabase.GetAssetPath(m.lightmapDir)).ToArray();result.passed=result.atlases>0&&result.probes>200&&result.receivers>0&&result.mapped==result.receivers&&result.colors.Concat(result.directions).All(p=>p.StartsWith("Assets/EchoStation/Station/",StringComparison.Ordinal)&&File.Exists(p));}
    EditorSceneManager.RestoreSceneManagerSetup(previous);result.layoutRestored=true;
   }catch(Exception e){result.passed=false;result.error=e.ToString();}
   finally{previous=null;File.WriteAllText(folder+"/LightingBake.json",JsonUtility.ToJson(result,true));}
  }
 }
}
