using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextArtV2Lighting
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/Feedback02/V03",Settings=NextArtV2Layout.Generated+"/Lighting.asset",Probes=NextArtV2Layout.Generated+"/Probes_B.asset",Merged=NextArtV2Layout.Root+"/ES_ArtV2_Merged_B.unity";
        static SceneSetup[] previous;static NextSdkSaveScope saveScope;static DateTime started;static bool completed,cancelled;static readonly List<string> errors=new List<string>();public static string Receipt{get;private set;}
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();if(File.Exists(Settings))throw new InvalidOperationException("Keep ArtV2 lighting settings");Directory.CreateDirectory(Evidence);var settings=Object.Instantiate(AssetDatabase.LoadAssetAtPath<LightingSettings>(NextLighting.SettingsPath));settings.name="ES_ArtV2_Lighting";settings.lightmapMaxSize=1024;settings.lightmapResolution=12;settings.autoGenerate=false;AssetDatabase.CreateAsset(settings,Settings);AssetDatabase.SaveAssetIfDirty(settings);var old=SceneManager.GetActiveScene();
            using(NextSdkSaveScope.Begin(Evidence+"/ConfigureScope_A.json"))
            {
                var scene=EditorSceneManager.OpenScene(NextArtV2Layout.Authoring,OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);Lightmapping.lightingSettings=settings;Lightmapping.lightingDataAsset=null;var world=scene.GetRootGameObjects().Single();var area=world.GetComponentsInChildren<Light>().Where(l=>l.name.StartsWith("Area_",StringComparison.Ordinal)).ToArray();if(area.Length!=18)throw new InvalidOperationException("Expected eighteen inherited lamps");foreach(var l in area)l.intensity=new[]{160,150,170}[int.Parse(l.name.Split('_')[1])];
                    for(int i=0;i<3;i++)for(int z=0;z<2;z++)
                    {
                        var l=new GameObject("ArtV2_BakedWallWash_"+i+"_"+z).AddComponent<Light>();l.transform.SetParent(world.transform,false);l.transform.position=new Vector3(6+12*i,2.7f,z==0?1:23);l.transform.rotation=Quaternion.Euler(-90,0,0);l.type=LightType.Rectangle;l.areaSize=new Vector2(5,.4f);l.intensity=25;l.range=8;l.color=new Color(.78f,.87f,1);l.lightmapBakeType=LightmapBakeType.Baked;
                    }
                    var probes=world.GetComponentInChildren<LightProbeGroup>();var positions=probes.probePositions.ToList();for(int x=0;x<9;x++)for(int z=0;z<6;z++)positions.Add(new Vector3(2+4*x,3.7f,2+4*z));probes.probePositions=positions.ToArray();EditorSceneManager.SaveScene(scene);
                    File.WriteAllText(Evidence+"/Parameters_A.json",new JObject{{"areaLights",24},{"realtimeLightsAdded",0},{"primaryIntensity",new JArray(160,150,170)},{"upwardWashIntensity",25},{"lightmapResolution",12},{"maxAtlasSize",1024},{"probes",positions.Count},{"exposure",1},{"internalHDR",true},{"SDROutput",true}}.ToString());
                }
                finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
            }
        }
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)lock(errors)errors.Add(text);}
        public static void StartBake()
        {
            SceneSafetyGuard.RequireClean();if(previous!=null||Lightmapping.isRunning||StaticOcclusionCulling.isRunning)throw new InvalidOperationException("Bake busy");Directory.CreateDirectory(Evidence);Receipt=Evidence+"/Bake_"+Guid.NewGuid().ToString("N")+".json";previous=EditorSceneManager.GetSceneManagerSetup();started=DateTime.UtcNow;completed=cancelled=false;lock(errors)errors.Clear();saveScope=NextSdkSaveScope.Begin(Receipt+".scope.json");Application.logMessageReceivedThreaded+=Log;
            try{EditorSceneManager.OpenScene(NextArtV2Layout.Authoring,OpenSceneMode.Single);Lightmapping.bakeCompleted+=Complete;Lightmapping.bakeCancelled+=Cancel;EditorApplication.update+=Tick;if(!Lightmapping.BakeAsync())Finish("BakeAsync rejected");}catch(Exception e){Finish(e.ToString());}
        }
        static void Complete()=>completed=true;static void Cancel()=>cancelled=true;
        static void Tick(){if((DateTime.UtcNow-started).TotalSeconds>600){Lightmapping.Cancel();Finish("Bake exceeded600 seconds");}else if(!Lightmapping.isRunning&&(completed||cancelled))Finish(cancelled?"Cancelled":null);}
        static void Finish(string error)
        {
            Lightmapping.bakeCompleted-=Complete;Lightmapping.bakeCancelled-=Cancel;EditorApplication.update-=Tick;var row=new JObject{{"passed",false},{"error",error},{"seconds",(DateTime.UtcNow-started).TotalSeconds}};bool owned=SceneManager.sceneCount==1&&SceneManager.GetActiveScene().path==NextArtV2Layout.Authoring;
            try
            {
                if(!owned)throw new InvalidOperationException("Layout changed during bake, preserve current state");if(error!=null)throw new InvalidOperationException(error);var scene=SceneManager.GetActiveScene();var world=scene.GetRootGameObjects().Single();var maps=LightmapSettings.lightmaps;if(maps.Length<1||maps.Length>2)throw new InvalidOperationException("Atlas count outside current two-slot contract: "+maps.Length);
                var meshes=world.GetComponentsInChildren<RoomVisual>().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).ToArray();if(meshes.Any(r=>r.lightmapIndex<0||r.lightmapIndex>=maps.Length))throw new InvalidOperationException("Unmapped static geometry");var realtime=world.GetComponentsInChildren<Light>(true).Where(l=>l.lightmapBakeType==LightmapBakeType.Realtime).ToArray();var enabled=realtime.Select(l=>l.enabled).ToArray();
                try{foreach(var l in realtime)l.enabled=false;foreach(var room in world.GetComponentsInChildren<RoomVisual>()){var p=room.GetComponentInChildren<ReflectionProbe>();string path=NextArtV2Layout.Generated+"/Reflection_"+room.Index+".exr";if(!Lightmapping.BakeReflectionProbe(p,path))throw new InvalidOperationException("Reflection bake failed");AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);p.mode=ReflectionProbeMode.Custom;p.customBakedTexture=AssetDatabase.LoadAssetAtPath<Texture>(path);}}
                finally{for(int i=0;i<realtime.Length;i++)if(realtime[i])realtime[i].enabled=enabled[i];}
                if(File.Exists(Probes))throw new InvalidOperationException("Keep independent probe asset");var probes=Object.Instantiate(LightmapSettings.lightProbes);probes.hideFlags=HideFlags.None;probes.name="ES_ArtV2_Probes";AssetDatabase.CreateAsset(probes,Probes);AssetDatabase.SaveAssetIfDirty(probes);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssetIfDirty(Lightmapping.lightingDataAsset);
                row["atlases"]=maps.Length;row["maps"]=new JArray(maps.Select(m=>new JObject{{"color",AssetDatabase.GetAssetPath(m.lightmapColor)},{"direction",AssetDatabase.GetAssetPath(m.lightmapDir)}}));row["mappedRenderers"]=meshes.Length;row["probes"]=probes.count;lock(errors){row["errors"]=new JArray(errors);row["passed"]=errors.Count==0&&probes.count>=234;}
            }
            catch(Exception e){row["error"]=e.ToString();row["passed"]=false;}
            finally{Application.logMessageReceivedThreaded-=Log;try{if(owned){EditorSceneManager.RestoreSceneManagerSetup(previous);row["layoutRestored"]=true;}saveScope?.Dispose();}catch(Exception e){row["restoreError"]=e.ToString();row["passed"]=false;}finally{previous=null;saveScope=null;File.WriteAllText(Receipt,row.ToString());}}
        }
        public static void CreateMerged()
        {
            SceneSafetyGuard.RequireClean();if(File.Exists(Merged))throw new InvalidOperationException("Keep independent merged scene");if(!AssetDatabase.CopyAsset(NextArtV2Layout.Authoring,Merged))throw new IOException("Merged scene copy failed");FacilityAssets.Folder(NextArtV2Layout.Generated+"/Meshes_B");var old=SceneManager.GetActiveScene();using(NextSdkSaveScope.Begin(Evidence+"/MergeScope_B.json"))
            {
                var scene=EditorSceneManager.OpenScene(Merged,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);var rows=new JArray();foreach(var room in scene.GetRootGameObjects().Single().GetComponentsInChildren<RoomVisual>()){int before=room.GetComponentsInChildren<MeshRenderer>().Length;long corners=room.GetComponentsInChildren<MeshFilter>().Sum(f=>(long)f.sharedMesh.triangles.Length);var merged=NextMeshMerge.Merge(room,true,NextArtV2Layout.Generated+"/Meshes_B","ES_V2B");long after=merged.Sum(r=>(long)r.GetComponent<MeshFilter>().sharedMesh.triangles.Length);if(corners!=after)throw new InvalidOperationException("Merge changed triangle corners");rows.Add(new JObject{{"room",room.Index},{"before",before},{"after",merged.Length},{"triangleCorners",corners}});}EditorSceneManager.SaveScene(scene);File.WriteAllText(Evidence+"/Merge_B.json",rows.ToString());}
                finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
            }
        }
    }
}
