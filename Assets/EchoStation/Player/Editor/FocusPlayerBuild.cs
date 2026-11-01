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
    public static class FocusPlayerBuild
    {
        public const string ScenePath="Assets/EchoStation/Tests/Scenes/S1_P06_Focus.unity";
        [MenuItem("Tools/EchoStation/Stage 1/P06/Build Focus Player")]
        public static void Schedule(){SceneSafetyGuard.RequireClean();EditorApplication.update+=Build;}
        private static void Build()
        {
            EditorApplication.update-=Build;SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var oldDefault=AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);var oldQuality=AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);if(string.IsNullOrEmpty(oldDefault)||string.IsNullOrEmpty(oldQuality))throw new InvalidOperationException("Cannot restore missing bindings.");var timing=PlayerSettings.enableFrameTimingStats;
            try{SceneManager.SetActiveScene(scene);var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));var driver=new GameObject("FocusValidation").AddComponent<P06FocusWindowProbe>();driver.player=actor;const string matPath="Assets/EchoStation/Tests/Scenes/ES_P06_Chart.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,matPath);}driver.chartMaterial=mat;EditorSceneManager.SaveScene(scene,ScenePath);SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);scene=default;var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(ScanWaveSetup.PipelinePath);GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;PlayerSettings.enableFrameTimingStats=true;AssetDatabase.SaveAssets();var output="Builds/ModuleTests/S1-P06/EchoStation_P06.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-P06");File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P06/PlayerBuild.json",JsonUtility.ToJson(new Result{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray()},true));}
            finally{GraphicsSettings.defaultRenderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldDefault);QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldQuality);PlayerSettings.enableFrameTimingStats=timing;AssetDatabase.SaveAssets();if(previous.IsValid())SceneManager.SetActiveScene(previous);if(scene.IsValid())EditorSceneManager.CloseScene(scene,true);}
        }
        [Serializable]private class Result{public string result;public int errors,warnings;public double seconds;public string[] messages;}
    }
}
