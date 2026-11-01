using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class OptimizationOcclusionBake
    {
        [Serializable] private class Result {public bool passed,originalLayoutRestored;public double seconds;public int bytes,serializedPvsBytes;public long assetFileBytes;public string error,dataPath;}
        private static SceneSetup[] previous;
        private static DateTime started;
        [MenuItem("Tools/EchoStation/Stage 1/R05/Bake Isolated Occlusion")]
        public static void StartBake()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || StaticOcclusionCulling.isRunning || Lightmapping.isRunning || previous!=null)
                throw new InvalidOperationException("Another play/bake operation is active.");
            for(var i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene: preserving it.");
            if(PrefabStageUtility.GetCurrentPrefabStage()!=null)throw new InvalidOperationException("Close Prefab Stage before isolated bake.");
            previous=EditorSceneManager.GetSceneManagerSetup();started=DateTime.UtcNow;
            try
            {
                EditorSceneManager.OpenScene(OptimizationFixtureSetup.ScenePath,OpenSceneMode.Single);
                if(!StaticOcclusionCulling.GenerateInBackground())throw new InvalidOperationException("Occlusion bake did not start.");
                EditorApplication.update+=Tick;
            }
            catch(Exception e){Finish(e.ToString());}
        }
        private static void Tick()
        {
            if((DateTime.UtcNow-started).TotalSeconds>180){StaticOcclusionCulling.Cancel();Finish("Occlusion bake timed out after180s.");return;}
            if(!StaticOcclusionCulling.isRunning)Finish(null);
        }
        private static void Finish(string error)
        {
            EditorApplication.update-=Tick;var result=new Result{error=error,seconds=(DateTime.UtcNow-started).TotalSeconds};
            try
            {
                var scene=SceneManager.GetActiveScene();if(SceneManager.sceneCount!=1 || scene.path!=OptimizationFixtureSetup.ScenePath)
                    throw new InvalidOperationException("Scene layout changed; not saving/closing user's new state.");
                if(string.IsNullOrEmpty(error))
                {
                    result.bytes=StaticOcclusionCulling.umbraDataSize;EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                    var reference=Regex.Match(File.ReadAllText(scene.path),@"m_OcclusionCullingData: \{fileID: 36300000, guid: ([a-f0-9]+)");
                    if(reference.Success)
                    {
                        result.dataPath=AssetDatabase.GUIDToAssetPath(reference.Groups[1].Value);
                        if(File.Exists(result.dataPath))
                        {
                            result.assetFileBytes=new FileInfo(result.dataPath).Length;
                            var pvs=Regex.Match(File.ReadAllText(result.dataPath),@"m_PVSData: ([a-fA-F0-9]+)");
                            result.serializedPvsBytes=pvs.Success?pvs.Groups[1].Length/2:0;
                        }
                    }
                    // Native statistics may remain zero before the Occlusion UI loads it.
                    // Actual saved PVS + subsequent rendering contrast are the acceptance gates.
                    result.passed=result.assetFileBytes>128 && result.serializedPvsBytes>128;
                }
                EditorSceneManager.RestoreSceneManagerSetup(previous);result.originalLayoutRestored=true;
            }
            catch(Exception e){result.error=e.ToString();result.passed=false;}
            finally{previous=null;File.WriteAllText(Path.Combine(OptimizationFixtureSetup.EvidencePath,"OcclusionBake.json"),JsonUtility.ToJson(result,true));}
        }
    }
}
