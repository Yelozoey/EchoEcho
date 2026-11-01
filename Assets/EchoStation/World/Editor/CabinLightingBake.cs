using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.World.Editor
{
    public static class CabinLightingBake
    {
        [Serializable]private class Result{public bool passed,originalLayoutRestored;public string error;public double seconds;public int atlases,probes,validRenderers;public string[] colorPaths,directionPaths;}
        private static SceneSetup[] previous;private static DateTime started;private static bool complete,cancelled;private static string evidence,targetScene;
        [MenuItem("Tools/EchoStation/Stage 1/W01/Bake Isolated Cabin Lighting")]
        public static void StartBake()
            =>StartBakeTo(CabinGreyboxSetup.EvidencePath);
        public static void StartBakeTo(string outputEvidence)=>StartBakeFor(CabinGreyboxSetup.ScenePath,outputEvidence);
        public static void StartBakeFor(string scenePath,string outputEvidence)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||Lightmapping.isRunning||previous!=null)throw new InvalidOperationException("Another play/bake operation is active.");
            for(var i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene: not saving/closing it.");
            if(PrefabStageUtility.GetCurrentPrefabStage()!=null)throw new InvalidOperationException("Close Prefab Stage before isolated bake.");
            targetScene=scenePath;Directory.CreateDirectory(outputEvidence);evidence=outputEvidence;
            previous=EditorSceneManager.GetSceneManagerSetup();started=DateTime.UtcNow;complete=false;cancelled=false;
            try
            {
                EditorSceneManager.OpenScene(targetScene,OpenSceneMode.Single);Lightmapping.bakeCompleted+=Completed;Lightmapping.bakeCancelled+=Cancelled;EditorApplication.update+=Tick;
                if(!Lightmapping.BakeAsync())Finish("BakeAsync did not start.");
            }
            catch(Exception e){Finish(e.ToString());}
        }
        private static void Completed()=>complete=true;private static void Cancelled()=>cancelled=true;
        private static void Tick()
        {
            if((DateTime.UtcNow-started).TotalSeconds>600){Lightmapping.Cancel();Finish("Cabin bake exceeded 600s.");return;}
            if(!Lightmapping.isRunning&&(complete||cancelled))Finish(cancelled?"Bake cancelled.":null);
        }
        private static void Finish(string error)
        {
            Lightmapping.bakeCompleted-=Completed;Lightmapping.bakeCancelled-=Cancelled;EditorApplication.update-=Tick;
            var result=new Result{error=error,seconds=(DateTime.UtcNow-started).TotalSeconds};
            try
            {
                var scene=SceneManager.GetActiveScene();if(SceneManager.sceneCount!=1||scene.path!=targetScene)throw new InvalidOperationException("Scene layout changed: not saving/closing new user state.");
                if(string.IsNullOrEmpty(error))
                {
                    EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();var maps=LightmapSettings.lightmaps;
                    result.atlases=maps.Length;result.probes=LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0;
                    var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");
                    result.validRenderers=root.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled&&r.lightmapIndex>=0&&r.lightmapIndex<maps.Length);
                    result.colorPaths=maps.Select(m=>AssetDatabase.GetAssetPath(m.lightmapColor)).ToArray();result.directionPaths=maps.Select(m=>AssetDatabase.GetAssetPath(m.lightmapDir)).ToArray();
                    result.passed=result.atlases>0&&result.probes>=100&&result.validRenderers==8&&result.colorPaths.All(p=>!string.IsNullOrEmpty(p))&&result.directionPaths.All(p=>!string.IsNullOrEmpty(p));
                }
                EditorSceneManager.RestoreSceneManagerSetup(previous);result.originalLayoutRestored=true;
            }
            catch(Exception e){result.error=e.ToString();result.passed=false;}
            finally{previous=null;File.WriteAllText(Path.Combine(evidence,"LightingBake.json"),JsonUtility.ToJson(result,true));}
        }
    }
}
