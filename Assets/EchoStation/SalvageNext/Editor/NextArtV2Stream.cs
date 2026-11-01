using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextArtV2Stream
    {
        public const string Main=NextArtV2Layout.Root+"/ES_ArtV2_Streamed.unity",Nav=NextArtV2Layout.Generated+"/Nav.asset",Evidence="Docs/Tests/SalvagePrototype/Evidence/Feedback02/V04",Prefix="es-v2-room-";
        public static string RoomPath(int room)=>NextArtV2Layout.Root+"/Rooms/ES_V2_Room_"+room+".unity";
        static Texture2D Map(Texture2D source,int room,int atlas,string channel)
        {string target=NextArtV2Layout.Generated+"/Maps/Room"+room+"_Atlas"+atlas+"_"+channel+Path.GetExtension(AssetDatabase.GetAssetPath(source));if(File.Exists(target)||!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source),target))throw new IOException("Keep/create unique map: "+target);return AssetDatabase.LoadAssetAtPath<Texture2D>(target);}
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(File.Exists(Main))throw new InvalidOperationException("Keep new streamed scene");Directory.CreateDirectory(Evidence);FacilityAssets.Folder(NextArtV2Layout.Root+"/Rooms");FacilityAssets.Folder(NextArtV2Layout.Generated+"/Maps");var old=SceneManager.GetActiveScene();var created=new List<Scene>();
            using(NextSdkSaveScope.Begin(Evidence+"/PartitionScope_A.json"))
            {
                var scene=EditorSceneManager.OpenScene(NextArtV2Lighting.Merged,OpenSceneMode.Additive);created.Add(scene);
                try
                {
                    SceneManager.SetActiveScene(scene);var maps=LightmapSettings.lightmaps;if(maps.Length!=2)throw new InvalidOperationException("Two final directional atlases required");var source=scene.GetRootGameObjects().Single();var rooms=source.GetComponentsInChildren<RoomVisual>().OrderBy(r=>r.Index).ToArray();var rows=new JArray();
                    foreach(var room in rooms)
                    {
                        int index=room.Index;var colors=new Texture2D[2];var dirs=new Texture2D[2];for(int m=0;m<2;m++){colors[m]=Map(maps[m].lightmapColor,index,m,"Color");dirs[m]=Map(maps[m].lightmapDir,index,m,"Direction");}
                        var renderers=room.GetComponentsInChildren<MeshRenderer>();room.gameObject.AddComponent<RoomPackage>().Configure(index,renderers,renderers.Select(r=>r.GetComponent<MergedRoomMesh>().Atlas).ToArray(),colors,dirs);room.GetComponent<DistanceShadowCaster>().Configure(null,renderers,30,2,.1f);
                        var legacy=EditorSceneManager.OpenScene(NextRoomPartition.RoomPath(index),OpenSceneMode.Additive);
                        try{var decor=room.gameObject.AddComponent<InstancedRoomDecor>();EditorUtility.CopySerialized(legacy.GetRootGameObjects().Single().GetComponent<InstancedRoomDecor>(),decor);decor.BindView(null);}
                        finally{SceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(legacy,true);}
                        var positions=Enumerable.Range(0,2).SelectMany(x=>Enumerable.Range(0,3).Select(z=>new Vector3(index*12+3+x*6,NextArtV2Layout.Height(index)-.09f,4+z*8))).ToArray();room.gameObject.AddComponent<InstancedRoomLamps>().Configure(AssetDatabase.LoadAssetAtPath<Mesh>(NextLampAssets.MeshPath),new[]{AssetDatabase.LoadAssetAtPath<Material>(NextLampAssets.BodyPath),AssetDatabase.LoadAssetAtPath<Material>(NextLampAssets.LightPath)},positions);
                        room.transform.SetParent(null,true);var target=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);created.Add(target);SceneManager.MoveGameObjectToScene(room.gameObject,target);room.gameObject.SetActive(false);if(!EditorSceneManager.SaveScene(target,RoomPath(index)))throw new IOException("Room save failed");rows.Add(new JObject{{"room",index},{"renderers",renderers.Length},{"height",NextArtV2Layout.Height(index)},{"lamps",positions.Length},{"path",RoomPath(index)}});
                    }
                    SceneManager.SetActiveScene(old);foreach(var s in created.AsEnumerable().Reverse())EditorSceneManager.CloseScene(s,true);created.Clear();
                    if(!AssetDatabase.CopyAsset(NextRoomPartition.Main,Main))throw new IOException("Latest gameplay scene copy failed");string text=Regex.Replace(File.ReadAllText(Main),@"m_OcclusionCullingData: \{[^\r\n]*\}","m_OcclusionCullingData: {fileID: 0}");File.WriteAllText(Main,text);AssetDatabase.ImportAsset(Main,ImportAssetOptions.ForceSynchronousImport);var main=EditorSceneManager.OpenScene(Main,OpenSceneMode.Additive);created.Add(main);SceneManager.SetActiveScene(main);var world=main.GetRootGameObjects().Single();int before=world.GetComponentsInChildren<Collider>(true).Length;NextArtV2Layout.RaiseCollision(world);world.GetComponent<NextProbeScope>().Configure(AssetDatabase.LoadAssetAtPath<LightProbes>(NextArtV2Lighting.Probes));world.GetComponentInChildren<NextRoomStreaming>().Configure(world.GetComponentInChildren<SalvageEcho>(),Prefix);
                    foreach(var light in world.GetComponentsInChildren<Light>())if(light.name.StartsWith("Area_",StringComparison.Ordinal)){int i=int.Parse(light.name.Split('_')[1]);light.transform.position+=Vector3.up*(NextArtV2Layout.Height(i)-3);}
                    var area=world.GetComponentInChildren<OcclusionArea>();area.center=new Vector3(18,2.5f,12);area.size=new Vector3(35.5f,4.7f,23.5f);
                    var surface=world.GetComponent<NavMeshSurface>();surface.RemoveData();surface.navMeshData=null;surface.BuildNavMesh();if(!surface.navMeshData)throw new InvalidOperationException("Independent navigation bake failed");AssetDatabase.CreateAsset(surface.navMeshData,Nav);AssetDatabase.SaveAssetIfDirty(surface.navMeshData);if(!EditorSceneManager.SaveScene(main))throw new IOException("Main scene save failed");
                    File.WriteAllText(Evidence+"/PartitionCreated_A.json",new JObject{{"main",Main},{"rooms",rows},{"collidersBefore",before},{"collidersAfter",world.GetComponentsInChildren<Collider>(true).Length},{"nav",Nav},{"prefix",Prefix},{"originalScenesPreserved",true}}.ToString());
                }
                finally{SceneManager.SetActiveScene(old);foreach(var s in created.AsEnumerable().Reverse())if(s.IsValid()&&s.isLoaded)EditorSceneManager.CloseScene(s,true);}
            }
        }
        public static void RegisterBundles()
        {
            SceneSafetyGuard.RequireClean();var settings=NextAddressables.Settings;if(settings.groups.Any(g=>g&&g.Name=="ES_V2_Rooms"))throw new InvalidOperationException("Keep ArtV2 groups");using(NextSdkSaveScope.Begin(Evidence+"/GroupsScope_A.json"))
            {
                var group=settings.CreateGroup("ES_V2_Rooms",false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));NextAddressables.ConfigureLocal(group);group.GetSchema<BundledAssetGroupSchema>().BundleMode=BundledAssetGroupSchema.BundlePackingMode.PackSeparately;var shared=settings.CreateGroup("ES_V2_Shared",false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));NextAddressables.ConfigureLocal(shared);var counts=new Dictionary<string,int>();
                for(int i=0;i<3;i++){string path=RoomPath(i);settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),group).address=Prefix+i;foreach(string dependency in AssetDatabase.GetDependencies(path,true).Distinct()){var type=AssetDatabase.GetMainAssetTypeAtPath(dependency);if(type!=typeof(Material)&&type!=typeof(Texture2D)&&type!=typeof(Mesh))continue;counts.TryGetValue(dependency,out int n);counts[dependency]=n+1;}}
                var added=new JArray();foreach(var pair in counts.Where(p=>p.Value>1)){string guid=AssetDatabase.AssetPathToGUID(pair.Key);if(settings.FindAssetEntry(guid)!=null)continue;settings.CreateOrMoveEntry(guid,shared).address="es-v2-shared-"+guid;added.Add(pair.Key);}EditorUtility.SetDirty(settings);NextAddressables.SaveOwned();File.WriteAllText(Evidence+"/Groups_A.json",new JObject{{"newRoomAddresses",new JArray(Enumerable.Range(0,3).Select(i=>Prefix+i))},{"newSharedAssets",added},{"existingEntriesPreserved",true}}.ToString());
            }
        }
    }
}
