using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextRoomPartition
    {
        public const string Main=FacilityAssets.Root+"/Scenes/ES_Salvage_Streamed.unity",RoomFolder=FacilityAssets.Root+"/Scenes/Rooms",Maps=FacilityAssets.Generated+"/StreamedLightmaps",Nav=FacilityAssets.Generated+"/ES_Next_Streamed_Nav.asset",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03b-1b";
        public static string RoomPath(int index)=>RoomFolder+"/ES_Next_Room_"+index+".unity";
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static Texture2D CopyMap(Texture2D source,int room,int atlas,string channel)
        {
            string path=Maps+"/Room"+room+"_Atlas"+atlas+"_"+channel+Path.GetExtension(AssetDatabase.GetAssetPath(source));if(AssetDatabase.LoadMainAssetAtPath(path)||!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source),path))throw new InvalidOperationException("Lightmap copy failed/preserve existing: "+path);return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static string ReplaceOne(string source,string pattern,string replacement)
        {if(Regex.Matches(source,pattern).Count!=1)throw new InvalidOperationException("Expected exactly one scene field: "+pattern);return Regex.Replace(source,pattern,replacement);}
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(File.Exists(Main)||AssetDatabase.LoadMainAssetAtPath(Nav))throw new InvalidOperationException("Streamed partition already exists.");Directory.CreateDirectory(Evidence);var baseline=new JObject{{NextLayout.ScenePath,Hash(NextLayout.ScenePath)},{NextLayout.NavPath,Hash(NextLayout.NavPath)}};File.WriteAllText(Evidence+"/ReferenceBefore_A.json",baseline.ToString());FacilityAssets.Folder(RoomFolder);FacilityAssets.Folder(Maps);var previous=SceneManager.GetActiveScene();var sourceScene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);var colors=new Texture2D[3][];var directions=new Texture2D[3][];int colliders;
            try
            {
                SceneManager.SetActiveScene(sourceScene);var root=sourceScene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");colliders=root.GetComponentsInChildren<Collider>(true).Length;var data=LightmapSettings.lightmaps;if(data.Length!=2)throw new InvalidOperationException("Expected two reference atlases.");for(int room=0;room<3;room++){colors[room]=new Texture2D[data.Length];directions[room]=new Texture2D[data.Length];for(int atlas=0;atlas<data.Length;atlas++){colors[room][atlas]=CopyMap(data[atlas].lightmapColor,room,atlas,"Color");directions[room][atlas]=CopyMap(data[atlas].lightmapDir,room,atlas,"Direction");}}
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(sourceScene,true);}
            if(!AssetDatabase.CopyAsset(NextLayout.NavPath,Nav)||!AssetDatabase.CopyAsset(NextLayout.ScenePath,Main))throw new IOException("Own scene/nav copy failed.");string text=File.ReadAllText(Main);text=ReplaceOne(text,@"m_LightingDataAsset: \{[^\r\n]*\}","m_LightingDataAsset: {fileID: 0}");text=ReplaceOne(text,@"m_OcclusionCullingData: \{[^\r\n]*\}","m_OcclusionCullingData: {fileID: 0}");text=ReplaceOne(text,AssetDatabase.AssetPathToGUID(NextLayout.NavPath),AssetDatabase.AssetPathToGUID(Nav));File.WriteAllText(Main,text);AssetDatabase.ImportAsset(Main,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            using(NextSdkSaveScope.Begin(Evidence+"/PartitionSaveScope_A.json"))
            {
                var main=EditorSceneManager.OpenScene(Main,OpenSceneMode.Additive);var created=new System.Collections.Generic.List<Scene>();
                try
                {
                    SceneManager.SetActiveScene(main);var root=main.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");foreach(var group in root.GetComponentsInChildren<LightProbeGroup>(true))UnityEngine.Object.DestroyImmediate(group);root.AddComponent<NextProbeScope>().Configure(AssetDatabase.LoadAssetAtPath<LightProbes>(NextProbeAssets.Path));var rooms=root.GetComponentsInChildren<RoomVisual>().OrderBy(r=>r.Index).ToArray();var rows=new JArray();
                    foreach(var room in rooms)
                    {
                        int index=room.Index;var renderers=room.GetComponentsInChildren<MeshRenderer>(true);var atlas=renderers.Select(r=>r.GetComponent<MergedRoomMesh>().Atlas).ToArray();room.gameObject.AddComponent<RoomPackage>().Configure(index,renderers,atlas,colors[index],directions[index]);var decor=room.GetComponent<InstancedRoomDecor>();var so=new SerializedObject(decor);so.FindProperty("view").objectReferenceValue=null;so.ApplyModifiedPropertiesWithoutUndo();room.GetComponent<DistanceShadowCaster>().Configure(null,renderers,30,2,.1f);room.transform.SetParent(null,true);var target=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);created.Add(target);SceneManager.MoveGameObjectToScene(room.gameObject,target);room.gameObject.SetActive(false);if(!EditorSceneManager.SaveScene(target,RoomPath(index)))throw new IOException("Room save failed.");rows.Add(new JObject{{"room",index},{"path",RoomPath(index)},{"renderers",renderers.Length},{"maps",colors[index].Length},{"initiallyInactive",true}});
                    }
                    SceneManager.SetActiveScene(main);if(root.GetComponentsInChildren<Collider>(true).Length!=colliders)throw new InvalidOperationException("Persistent collision changed.");if(root.GetComponent<NavMeshSurface>().navMeshData!=AssetDatabase.LoadAssetAtPath<NavMeshData>(Nav))throw new InvalidOperationException("Independent streamed nav binding missing.");if(!EditorSceneManager.SaveScene(main))throw new IOException("Main save failed.");File.WriteAllText(Evidence+"/Prepared_A.json",new JObject{{"main",Main},{"rooms",rows},{"persistentColliders",colliders},{"uniqueLightmapTextures",12},{"referenceHashesUnchanged",baseline.Properties().All(p=>Hash(p.Name)==p.Value.Value<string>())}}.ToString());
                }
                finally{SceneManager.SetActiveScene(previous);foreach(var room in created)if(room.IsValid())EditorSceneManager.CloseScene(room,true);EditorSceneManager.CloseScene(main,true);}
            }
        }
    }
}
