using System;
using System.IO;
using System.Linq;
using EchoStation.AI.GameIntegration;
using EchoStation.Rendering.Editor;
using EchoStation.Station;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace EchoStation.AI.SceneEditor
{
 public static class S3SceneSetup
 {
  [Serializable]private sealed class Receipt{public string scene,navigation,sourceScene,sourceNavigation,sourceNavGuid,newNavGuid;public bool saved,originalLayoutRestored;}
  [MenuItem("Tools/EchoStation/Stage 3/S01/Create Dedicated S3 Scene")]
  public static void Create()
  {
   SceneSafetyGuard.RequireClean();if(PrefabStageUtility.GetCurrentPrefabStage()!=null||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("S3 creation requires clean idle editor");
   if(AssetDatabase.LoadAssetAtPath<SceneAsset>(S3Bootstrap.ScenePath)||AssetDatabase.LoadAssetAtPath<NavMeshData>(S3Bootstrap.NavigationPath))throw new InvalidOperationException("S3 output already exists; refusing overwrite");
   var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>s.isLoaded&&string.IsNullOrEmpty(s.path)))throw new InvalidOperationException("Preserving unnamed user scene");
   Folder("Assets/EchoStation/AI/Scenes");Folder("Assets/EchoStation/AI/Generated");if(!AssetDatabase.CopyAsset(StationLayout.ScenePath,S3Bootstrap.ScenePath))throw new IOException("S3 scene copy failed");
   var receipt=new Receipt{scene=S3Bootstrap.ScenePath,navigation=S3Bootstrap.NavigationPath,sourceScene=StationLayout.ScenePath};var scene=default(Scene);
   try
   {
    scene=EditorSceneManager.OpenScene(S3Bootstrap.ScenePath,OpenSceneMode.Additive);var roots=scene.GetRootGameObjects();var player=roots.Single(r=>r.GetComponent<StationSession>());var surface=roots.SelectMany(r=>r.GetComponentsInChildren<NavMeshSurface>()).Single();
    if(!surface.navMeshData)throw new InvalidOperationException("Source station navigation missing");receipt.sourceNavigation=AssetDatabase.GetAssetPath(surface.navMeshData);receipt.sourceNavGuid=AssetDatabase.AssetPathToGUID(receipt.sourceNavigation);
    if(!receipt.sourceNavigation.StartsWith("Assets/EchoStation/Station/",StringComparison.Ordinal)||!AssetDatabase.CopyAsset(receipt.sourceNavigation,S3Bootstrap.NavigationPath))throw new IOException("S3 navigation copy failed");
    surface.RemoveData();surface.navMeshData=AssetDatabase.LoadAssetAtPath<NavMeshData>(S3Bootstrap.NavigationPath);surface.AddData();receipt.newNavGuid=AssetDatabase.AssetPathToGUID(S3Bootstrap.NavigationPath);if(receipt.newNavGuid==receipt.sourceNavGuid)throw new InvalidOperationException("Navigation must be independent");
    if(!player.GetComponent<S3Bootstrap>())player.AddComponent<S3Bootstrap>();if(!player.GetComponent<S3DialogueDriver>())player.AddComponent<S3DialogueDriver>();EditorSceneManager.MarkSceneDirty(scene);receipt.saved=EditorSceneManager.SaveScene(scene,S3Bootstrap.ScenePath);if(!receipt.saved)throw new IOException("Owned S3 scene save failed");
   }
   finally
   {
    if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);receipt.originalLayoutRestored=EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+":"+s.isLoaded+":"+s.isActive).SequenceEqual(setup.Select(s=>s.path+":"+s.isLoaded+":"+s.isActive));
    Directory.CreateDirectory("Library/EchoStation");File.WriteAllText("Library/EchoStation/S3SceneCreation.json",JsonUtility.ToJson(receipt,true));
   }
   if(!receipt.originalLayoutRestored)throw new InvalidOperationException("Original scene layout restoration mismatch");
  }
  private static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;var slash=path.LastIndexOf('/');if(!AssetDatabase.IsValidFolder(path.Substring(0,slash)))throw new InvalidOperationException("Expected AI folder parent missing");AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
 }
}
