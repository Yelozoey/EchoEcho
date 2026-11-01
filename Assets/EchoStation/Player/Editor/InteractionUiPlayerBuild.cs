using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.PlayerFixtures;
using EchoStation.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EchoStation.Player.Editor
{
    public static class InteractionUiPlayerBuild
    {
        public const string ScenePath="Assets/EchoStation/Tests/Scenes/S1_P04_InteractionUI.unity";
        [MenuItem("Tools/EchoStation/Stage 1/P04/Build UI Validation Player")]
        public static void Schedule(){SceneSafetyGuard.RequireClean();EditorApplication.update+=Build;}
        private static void Build()
        {
            EditorApplication.update-=Build;SceneSafetyGuard.RequireClean();if(!GraphicsSettings.currentRenderPipeline||GraphicsSettings.currentRenderPipeline.name!="ES_PC_ForwardHDR")throw new InvalidOperationException("UI fixture requires the confirmed baseline pipeline.");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath),scene);actor.GetComponent<FirstPersonMotor>().enabled=false;actor.GetComponent<CharacterController>().enabled=false;var camera=actor.GetComponent<FirstPersonMotor>().View.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.012f,.02f,.035f);PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ExplorationVolumeSetup.PrefabPath),scene);
                const string dataPath="Assets/EchoStation/Tests/Scenes/ES_P04_ProbeClue.asset";var definition=AssetDatabase.LoadAssetAtPath<ClueDefinition>(dataPath);if(!definition){definition=ScriptableObject.CreateInstance<ClueDefinition>();AssetDatabase.CreateAsset(definition,dataPath);}definition.Configure("prompt_window_fixture","界面诊断笔记","该文本只用于独立界面测试，不属于正式剧情。","界面测试摘要。",ClueKind.Note);EditorUtility.SetDirty(definition);
                const string materialPath="Assets/EchoStation/Tests/Scenes/ES_P04_ProbeSurface.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.08f,.13f,.2f));AssetDatabase.CreateAsset(material,materialPath);}
                var clue=GameObject.CreatePrimitive(PrimitiveType.Cube);clue.name="UIClueFixture";clue.SetActive(false);clue.transform.position=new Vector3(0,1.6f,2);clue.transform.localScale=Vector3.one*.2f;clue.GetComponent<Collider>().isTrigger=true;clue.GetComponent<Renderer>().sharedMaterial=material;clue.AddComponent<WorldTarget>().Configure(definition.Id,"测试线索");clue.AddComponent<ClueSource>().Configure(definition,clue.GetComponent<Renderer>());clue.SetActive(true);var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="UIOcclusionFixture";blocker.transform.position=new Vector3(0,1.6f,1);blocker.transform.localScale=new Vector3(1,1,.1f);blocker.GetComponent<Renderer>().sharedMaterial=material;blocker.SetActive(false);var probe=new GameObject("UIValidationDriver").AddComponent<P04UiWindowProbe>();probe.interaction=actor.GetComponent<PlayerInteraction>();probe.blocker=blocker;AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            var output="Builds/ModuleTests/S1-P04/EchoStation_P04.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});var notices=Path.Combine(Path.GetDirectoryName(output),"ThirdPartyNotices","Noto");Directory.CreateDirectory(notices);foreach(var file in new[]{"LICENSE.txt","COPYRIGHT.txt"})File.Copy("Assets/EchoStation/ThirdParty/Noto/"+file,Path.Combine(notices,file),true);Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-P04");File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P04/PlayerBuild.json",JsonUtility.ToJson(new Result{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,output=Path.GetFullPath(output),messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray()},true));
        }
        [Serializable]private class Result{public string result,output;public int errors,warnings;public double seconds;public string[] messages;}
    }
}
