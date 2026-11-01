using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.PlayerFixtures;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EchoStation.Player.Editor
{
    public static class CabinDemoPlayerBuild
    {
        private static GlobalObjectId[] previousSelection=Array.Empty<GlobalObjectId>();private static int warmups;
        [MenuItem("Tools/EchoStation/Stage 1/P09/Build Windows Demo")]
        public static void Schedule(){SceneSafetyGuard.RequireClean();previousSelection=Selection.objects.Select(GlobalObjectId.GetGlobalObjectIdSlow).ToArray();Selection.objects=Array.Empty<UnityEngine.Object>();ActiveEditorTracker.sharedTracker.ForceRebuild();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();warmups=2;EditorApplication.update+=QueueBuild;}
        private static void QueueBuild(){if(--warmups>0)return;EditorApplication.update-=QueueBuild;EditorApplication.update+=Build;}
        private static void Build()
        {
            EditorApplication.update-=Build;SceneSafetyGuard.RequireClean();if(!GraphicsSettings.currentRenderPipeline||GraphicsSettings.currentRenderPipeline.name!="ES_Gameplay_ForwardHDR")throw new InvalidOperationException("Demo requires gameplay Forward pipeline.");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(CabinDemoSetup.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var surfaces=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<NavMeshSurface>()).ToArray();if(surfaces.Length!=1||!surfaces[0].navMeshData||AssetDatabase.GetAssetPath(surfaces[0].navMeshData)!=CabinDemoSetup.DataFolder+"/NavMesh.asset")throw new InvalidOperationException("Demo owned navigation reference missing; refusing build.");var probe=scene.GetRootGameObjects().Select(o=>o.GetComponent<P09DemoWindowProbe>()).SingleOrDefault(p=>p);if(!probe)probe=new GameObject("S1_CLI_Diagnostics").AddComponent<P09DemoWindowProbe>();probe.validationRenderer=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(ScanWaveSetup.RendererPath);EditorSceneManager.SaveScene(scene);}
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}var oldTiming=PlayerSettings.enableFrameTimingStats;
            try{PlayerSettings.enableFrameTimingStats=true;var output="Builds/S1/EchoStation.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{CabinDemoSetup.ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});var notices=Path.Combine(Path.GetDirectoryName(output),"ThirdPartyNotices","Noto");Directory.CreateDirectory(notices);foreach(var file in new[]{"LICENSE.txt","COPYRIGHT.txt"})File.Copy("Assets/EchoStation/ThirdParty/Noto/"+file,Path.Combine(notices,file),true);File.Copy("Docs/Assets/ThirdPartyLicenses.md",Path.Combine(Path.GetDirectoryName(output),"ThirdPartyNotices","Resources.md"),true);Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-P09");File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P09/PlayerBuild.json",JsonUtility.ToJson(new Result{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,output=Path.GetFullPath(output),messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray()},true));}
            finally{PlayerSettings.enableFrameTimingStats=oldTiming;AssetDatabase.SaveAssets();Selection.objects=previousSelection.Select(GlobalObjectId.GlobalObjectIdentifierToObjectSlow).Where(o=>o).ToArray();previousSelection=Array.Empty<GlobalObjectId>();ActiveEditorTracker.sharedTracker.ForceRebuild();}
        }
        [Serializable]private class Result{public string result,output;public int errors,warnings;public double seconds;public string[] messages;}
    }
}
