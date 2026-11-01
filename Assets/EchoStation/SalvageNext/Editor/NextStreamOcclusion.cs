using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextStreamOcclusion
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03b-4b",Group="ES_Next_Visibility";
        static SceneSetup[] previous;static Scene main;static Scene[] rooms;static DateTime started;static NextSdkSaveScope saveScope;public static string Receipt{get;private set;}
        public static string DataPath=>NextRoomPartition.Main.Replace(".unity","/OcclusionCullingData.asset");
        static string Reference(string path){var match=Regex.Match(File.ReadAllText(path),@"m_OcclusionCullingData: \{fileID: 36300000, guid: ([a-f0-9]+)");return match.Success?match.Groups[1].Value:null;}
        public static void StartBake()
        {
            SceneSafetyGuard.RequireClean();if(previous!=null||StaticOcclusionCulling.isRunning||Lightmapping.isRunning)throw new InvalidOperationException("Bake busy.");Directory.CreateDirectory(Evidence);string id=Guid.NewGuid().ToString("N");Receipt=Evidence+"/Bake_"+id+".json";previous=EditorSceneManager.GetSceneManagerSetup();started=DateTime.UtcNow;rooms=new Scene[3];
            foreach(var path in Enumerable.Range(0,3).Select(NextRoomPartition.RoomPath).Prepend(NextRoomPartition.Main))File.Copy(path,Evidence+"/"+Path.GetFileNameWithoutExtension(path)+"_Before_"+id+".unity");
            try
            {
                saveScope=NextSdkSaveScope.Begin(Receipt+".save-scope.json");
                main=EditorSceneManager.OpenScene(NextRoomPartition.Main,OpenSceneMode.Single);for(int i=0;i<3;i++){rooms[i]=EditorSceneManager.OpenScene(NextRoomPartition.RoomPath(i),OpenSceneMode.Additive);var root=rooms[i].GetRootGameObjects().Single();if(root.activeSelf)throw new InvalidOperationException("Room should start inactive.");root.SetActive(true);}
                SceneManager.SetActiveScene(main);var world=main.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var area=world.GetComponentInChildren<OcclusionArea>();if(!area)throw new InvalidOperationException("Owned view volume missing.");StaticOcclusionCulling.smallestOccluder=1;StaticOcclusionCulling.smallestHole=.25f;StaticOcclusionCulling.backfaceThreshold=100;world.GetComponentInChildren<Camera>().useOcclusionCulling=true;foreach(var scene in rooms.Prepend(main))if(!EditorSceneManager.SaveScene(scene))throw new IOException("Prepare bake scene save failed.");if(!StaticOcclusionCulling.GenerateInBackground())throw new InvalidOperationException("Multi-scene PVS did not start.");EditorApplication.update+=Tick;
            }
            catch(Exception error){Finish(error.ToString());}
        }
        static void Tick(){if((DateTime.UtcNow-started).TotalSeconds>240){StaticOcclusionCulling.Cancel();Finish("Multi-scene occlusion exceeded240s.");}else if(!StaticOcclusionCulling.isRunning)Finish(null);}
        static void Finish(string error)
        {
            EditorApplication.update-=Tick;var row=new JObject{{"passed",false},{"seconds",(DateTime.UtcNow-started).TotalSeconds},{"error",error},{"layoutRestored",false}};var allowed=Enumerable.Range(0,3).Select(NextRoomPartition.RoomPath).Prepend(NextRoomPartition.Main).ToArray();bool own=main.IsValid()&&main.isLoaded&&Enumerable.Range(0,SceneManager.sceneCount).All(i=>allowed.Contains(SceneManager.GetSceneAt(i).path));
            try
            {
                if(!own)throw new InvalidOperationException("User changed layout; preserve it.");SceneManager.SetActiveScene(main);
                foreach(var scene in rooms.Where(s=>s.IsValid()&&s.isLoaded)){scene.GetRootGameObjects().Single().SetActive(false);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Inactive room save failed.");}if(!EditorSceneManager.SaveScene(main))throw new IOException("Main PVS save failed.");if(error!=null)throw new InvalidOperationException(error);
                string guid=Reference(NextRoomPartition.Main);if(guid==null||AssetDatabase.GUIDToAssetPath(guid)!=DataPath)throw new InvalidOperationException("Main needs its own new PVS asset.");var rows=new JArray();foreach(var scene in rooms.Prepend(main)){string value=Reference(scene.path);rows.Add(new JObject{{"path",scene.path},{"guid",value}});if(value!=guid)throw new InvalidOperationException("Scenes did not share baked visibility data: "+scene.path);}
                var data=Regex.Match(File.ReadAllText(DataPath),@"m_PVSData: ([a-fA-F0-9]+)");row["data"]=DataPath;row["assetBytes"]=new FileInfo(DataPath).Length;row["pvsBytes"]=data.Success?data.Groups[1].Length/2:0;row["scenes"]=rows;row["roomsSavedInactive"]=true;row["passed"]=row["pvsBytes"].Value<int>()>128;
            }
            catch(Exception failure){row["error"]=failure.ToString();row["passed"]=false;}
            finally{try{if(own){EditorSceneManager.RestoreSceneManagerSetup(previous);row["layoutRestored"]=true;}saveScope?.Dispose();}catch(Exception restore){row["error"]=restore.ToString();row["passed"]=false;}finally{saveScope=null;previous=null;File.WriteAllText(Receipt,row.ToString());}}
        }
        public static void RegisterBundle()
        {
            SceneSafetyGuard.RequireClean();if(!AssetDatabase.LoadMainAssetAtPath(DataPath))throw new InvalidOperationException("Bake data missing.");if(NextAddressables.Settings.groups.Any(g=>g&&g.Name==Group))throw new InvalidOperationException("Visibility bundle already registered.");
            using(NextSdkSaveScope.Begin(Evidence+"/RegisterSaveScope_A.json")){var settings=NextAddressables.Settings;var group=settings.CreateGroup(Group,false,false,false,null,typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));NextAddressables.ConfigureLocal(group);settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(DataPath),group).address="es-next-visibility";EditorUtility.SetDirty(settings);NextAddressables.SaveOwned();}
        }
    }
}
