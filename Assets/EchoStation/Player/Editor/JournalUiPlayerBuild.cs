using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.PlayerFixtures;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EchoStation.Player.Editor
{
    public static class JournalUiPlayerBuild
    {
        public const string ScenePath="Assets/EchoStation/Tests/Scenes/S1_P05_JournalUI.unity";
        [MenuItem("Tools/EchoStation/Stage 1/P05/Build Journal UI Player")]
        public static void Schedule(){SceneSafetyGuard.RequireClean();EditorApplication.update+=Build;}
        private static void Build()
        {
            EditorApplication.update-=Build;SceneSafetyGuard.RequireClean();if(!GraphicsSettings.currentRenderPipeline||GraphicsSettings.currentRenderPipeline.name!="ES_PC_ForwardHDR")throw new InvalidOperationException("Journal UI fixture requires confirmed baseline.");if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)&&!AssetDatabase.CopyAsset(InteractionUiPlayerBuild.ScenePath,ScenePath))throw new InvalidOperationException("UI scene copy failed.");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty)throw new InvalidOperationException("Journal fixture dirty; not saving preexisting changes.");SceneManager.SetActiveScene(scene);foreach(var old in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<P04UiWindowProbe>()).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);var panel=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<JournalPanel>()).Single();var existing=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<P05JournalWindowProbe>()).SingleOrDefault();if(!existing)existing=new GameObject("JournalUIValidationDriver").AddComponent<P05JournalWindowProbe>();existing.panel=panel;EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            var output="Builds/ModuleTests/S1-P05/EchoStation_P05.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});var notices=Path.Combine(Path.GetDirectoryName(output),"ThirdPartyNotices","Noto");Directory.CreateDirectory(notices);foreach(var file in new[]{"LICENSE.txt","COPYRIGHT.txt"})File.Copy("Assets/EchoStation/ThirdParty/Noto/"+file,Path.Combine(notices,file),true);Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-P05");File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P05/PlayerBuild.json",JsonUtility.ToJson(new Result{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,output=Path.GetFullPath(output),messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray()},true));
        }
        [Serializable]private class Result{public string result,output;public int errors,warnings;public double seconds;public string[] messages;}
    }
}
