using System;
using System.IO;
using System.Linq;
using EchoStation.Tests.Fixtures;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class ScanWavePlayerBuild
    {
        public const string ScenePath="Assets/EchoStation/Tests/Scenes/S1_E12_ScanWave.unity";
        [MenuItem("Tools/EchoStation/Stage 1/E12/Build Timing Player")]
        public static void Schedule(){SceneSafetyGuard.RequireClean();EditorApplication.update+=Build;}
        private static void Build()
        {
            EditorApplication.update-=Build;SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var originalTiming=PlayerSettings.enableFrameTimingStats;var oldQuality=QualitySettings.renderPipeline;var oldDefault=GraphicsSettings.defaultRenderPipeline;
            try
            {
                SceneManager.SetActiveScene(scene);var camera=new GameObject("WaveTimingCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,3,-6);camera.transform.LookAt(new Vector3(0,.45f,0));camera.fieldOfView=60;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.005f,.008f,.012f);camera.nearClipPlane=.05f;camera.farClipPlane=40;var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(0);data.allowHDROutput=false;data.renderPostProcessing=false;
                var emitter=new GameObject("SingleWaveEmitter").AddComponent<ScanWaveEmitter>();camera.gameObject.AddComponent<ScanWaveView>().Source=emitter;var probe=camera.gameObject.AddComponent<ScanWaveTimingProbe>();probe.pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ScanWaveSetup.PipelinePath);
                const string materialPath="Assets/EchoStation/Tests/Scenes/ES_E12_ProbeSurface.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",new Color(.05f,.06f,.07f));AssetDatabase.CreateAsset(mat,materialPath);}
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.1f,0);floor.transform.localScale=new Vector3(10,.2f,10);floor.GetComponent<Renderer>().sharedMaterial=mat;var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1,3);wall.transform.localScale=new Vector3(8,2,.2f);wall.GetComponent<Renderer>().sharedMaterial=mat;
                EditorSceneManager.SaveScene(scene,ScenePath);SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);scene=default;
                PlayerSettings.enableFrameTimingStats=true;GraphicsSettings.defaultRenderPipeline=probePipeline();QualitySettings.renderPipeline=probePipeline();AssetDatabase.SaveAssets();
                var output="Builds/ModuleTests/S1-E12/EchoStation_E12.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-E12");
                File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-E12/PlayerBuild.json",JsonUtility.ToJson(new Result{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,output=Path.GetFullPath(output),messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray()},true));
            }
            finally{GraphicsSettings.defaultRenderPipeline=oldDefault;QualitySettings.renderPipeline=oldQuality;PlayerSettings.enableFrameTimingStats=originalTiming;AssetDatabase.SaveAssets();if(previous.IsValid())SceneManager.SetActiveScene(previous);if(scene.IsValid())EditorSceneManager.CloseScene(scene,true);}
        }
        private static UniversalRenderPipelineAsset probePipeline()=>AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ScanWaveSetup.PipelinePath);
        [Serializable]private class Result{public string result,output;public int errors,warnings;public double seconds;public string[] messages;}
    }
}
