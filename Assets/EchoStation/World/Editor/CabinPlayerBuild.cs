using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.World.Editor
{
    public static class CabinPlayerBuild
    {
        [MenuItem("Tools/EchoStation/Stage 1/W01/Build Cabin Validation Player")]
        public static void Schedule()=>EditorApplication.update+=Build;
        private static void Build()
        {
            EditorApplication.update-=Build;SceneSafetyGuard.RequireClean();if(EditorApplication.isPlayingOrWillChangePlaymode||Lightmapping.isRunning)throw new InvalidOperationException("Exit Play/bake first.");
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty)throw new InvalidOperationException("Unsaved cabin: not saving user changes.");
                var camera=scene.GetRootGameObjects().Single(o=>o.name=="Cabin_ValidationCamera");var probe=camera.GetComponent<CabinPlayerProbe>();if(!probe)probe=camera.AddComponent<CabinPlayerProbe>();
                probe.validationRenderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath);EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
            var path="Builds/ModuleTests/S1-W01/EchoStation_W01.exe";Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{CabinGreyboxSetup.ScenePath},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Path.Combine(CabinGreyboxSetup.EvidencePath,"PlayerBuild.json"),JsonUtility.ToJson(new Result{result=report.summary.result.ToString(),errors=report.summary.totalErrors,
                warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,output=Path.GetFullPath(path),messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning||m.type==LogType.Error).Select(m=>m.content).ToArray()},true));
        }
        [Serializable]private class Result{public string result,output;public int errors,warnings;public double seconds;public string[] messages;}
    }
}
