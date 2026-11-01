using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class OptimizationDynamicSetup
    {
        public const string ScenePath="Assets/EchoStation/Tests/Scenes/S1_R05_DynamicBatcher.unity";
        [MenuItem("Tools/EchoStation/Stage 1/R05/Create Isolated Dynamic Fixture")]
        public static void CreateIsolated()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))return;
            var scene=SceneManager.GetSceneByPath(OptimizationFixtureSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            var previous=SceneManager.GetActiveScene();if(opened)scene=EditorSceneManager.OpenScene(OptimizationFixtureSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty)throw new InvalidOperationException("Unsaved R05 source fixture: no changes saved.");
                EditorSceneManager.SaveScene(scene,ScenePath,true);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
            // The dynamic-only test has no baked PVS dependency or registrations.
            var text=File.ReadAllText(ScenePath);text=Regex.Replace(text,@"  m_OcclusionCullingData: \{[^\r\n]+\}","  m_OcclusionCullingData: {fileID: 0}");
            text=Regex.Replace(text,@"  m_SceneGUID: [a-f0-9]+","  m_SceneGUID: 00000000000000000000000000000000");
            File.WriteAllText(ScenePath,text);AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceUpdate);
        }
        [MenuItem("Tools/EchoStation/Stage 1/R05/Add Dynamic Batcher Proxies")]
        public static void Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(OptimizationFixtureSetup.ScenePath);
            var opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(OptimizationFixtureSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty)throw new InvalidOperationException("Unsaved R05 scene: no changes saved.");
                if(scene.GetRootGameObjects().Any(o=>o.name=="R05_DynamicProxies"))return;
                SceneManager.SetActiveScene(scene);var materialPath=OptimizationFixtureSetup.Folder+"/R05_Dynamic.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material)
                {
                    material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="R05_Dynamic",enableInstancing=false};
                    material.SetColor("_BaseColor",new Color(.3f,.5f,.7f));material.SetColor("_EmissionColor",new Color(.1f,.22f,.35f));material.EnableKeyword("_EMISSION");
                    AssetDatabase.CreateAsset(material,materialPath);
                }
                var group=new GameObject("R05_DynamicProxies");
                for(var i=0;i<3;i++)
                {
                    var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="R05_Dynamic_"+i;go.transform.SetParent(group.transform);
                    go.transform.position=new Vector3(-2+i*2,.6f,-1);go.transform.localScale=Vector3.one*.6f;
                    go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                    GameObjectUtility.SetStaticEditorFlags(go,0);
                }
                group.SetActive(false);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
