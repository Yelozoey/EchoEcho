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
    public static class NextOcclusion
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R02b";
        static SceneSetup[] previous;static DateTime started;static string receipt;
        public static string Receipt=>receipt;
        public static void StartBake()
        {
            SceneSafetyGuard.RequireClean();if(previous!=null||StaticOcclusionCulling.isRunning||Lightmapping.isRunning)throw new InvalidOperationException("Bake busy.");Directory.CreateDirectory(Evidence);if(!File.Exists(Evidence+"/BeforeOcclusion.unity"))File.Copy(NextLayout.ScenePath,Evidence+"/BeforeOcclusion.unity");previous=EditorSceneManager.GetSceneManagerSetup();started=DateTime.UtcNow;receipt=Evidence+"/Bake_"+Guid.NewGuid().ToString("N")+".json";
            try{var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Single);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");if(!root.GetComponentInChildren<OcclusionArea>()){var area=new GameObject("NextViewVolume").AddComponent<OcclusionArea>();area.transform.SetParent(root.transform,false);area.center=new Vector3(18,1.5f,12);area.size=new Vector3(35.5f,2.7f,23.5f);}StaticOcclusionCulling.smallestOccluder=1;StaticOcclusionCulling.smallestHole=.25f;StaticOcclusionCulling.backfaceThreshold=100;root.GetComponentInChildren<Camera>().useOcclusionCulling=true;EditorSceneManager.SaveScene(scene);if(!StaticOcclusionCulling.GenerateInBackground())throw new InvalidOperationException("Occlusion did not start.");EditorApplication.update+=Tick;}
            catch(Exception e){Finish(e.ToString());}
        }
        static void Tick(){if((DateTime.UtcNow-started).TotalSeconds>240){StaticOcclusionCulling.Cancel();Finish("Occlusion exceeded240s.");}else if(!StaticOcclusionCulling.isRunning)Finish(null);}
        static void Finish(string error)
        {
            EditorApplication.update-=Tick;var row=new JObject{{"seconds",(DateTime.UtcNow-started).TotalSeconds},{"error",error},{"passed",false},{"layoutRestored",false}};bool own=false;
            try{var scene=SceneManager.GetActiveScene();own=SceneManager.sceneCount==1&&scene.path==NextLayout.ScenePath;if(!own)throw new InvalidOperationException("Layout changed; preserve new user state.");if(error==null){EditorSceneManager.SaveScene(scene);var match=Regex.Match(File.ReadAllText(NextLayout.ScenePath),@"m_OcclusionCullingData: \{fileID: 36300000, guid: ([a-f0-9]+)");if(!match.Success)throw new InvalidOperationException("Missing saved PVS reference.");string path=AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);if(!path.StartsWith(FacilityAssets.Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("Foreign PVS asset.");string data=File.ReadAllText(path);var pvs=Regex.Match(data,@"m_PVSData: ([a-fA-F0-9]+)");row["path"]=path;row["assetBytes"]=new FileInfo(path).Length;row["pvsBytes"]=pvs.Success?pvs.Groups[1].Length/2:0;row["passed"]=row["pvsBytes"].Value<int>()>128;}}
            catch(Exception e){row["error"]=e.ToString();row["passed"]=false;}
            finally{if(own){EditorSceneManager.RestoreSceneManagerSetup(previous);row["layoutRestored"]=true;}previous=null;File.WriteAllText(receipt,row.ToString());}
        }
    }
}
