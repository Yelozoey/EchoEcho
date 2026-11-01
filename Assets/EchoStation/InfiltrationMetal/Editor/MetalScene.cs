using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationLevel.Editor;
using EchoStation.InfiltrationRefinement.Editor;
using EchoStation.Player;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.InfiltrationMetal.Editor
{
    public static class MetalScene
    {
        public const string Root="Assets/EchoStation/InfiltrationMetal",Generated=Root+"/Generated",ScenePath=Root+"/Scenes/ES_Infiltration_Metal.unity",Evidence="Docs/Tests/InfiltrationPrototype/Evidence/A01M";
        public const string PipelinePath=Generated+"/MM_ForwardHDR.asset",RendererPath=Generated+"/MM_ForwardRenderer.asset",VolumePath=Generated+"/MM_Volume.asset",SettingsPath=Generated+"/MM_Lighting.asset";
        public static void Save(Object o){string path=AssetDatabase.GetAssetPath(o);if(!path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("Owned metal asset only: "+path);EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);}
        public static Transform World(Scene s)=>LevelBuilder.World(s);
        public static void Edit(Action<Transform> action)
        {SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(s);action(World(s));if(!EditorSceneManager.SaveScene(s))throw new IOException("Save metal scene");}finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(s,true);}}
        static void Copy(string from,string to){if(File.Exists(to)||!AssetDatabase.CopyAsset(from,to))throw new IOException("Preserve asset "+to);}
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();ArtSampleAssets.Folder(Generated);ArtSampleAssets.Folder(Root+"/Scenes");Copy(RefinementScene.ScenePath,ScenePath);Copy(RefinementLighting.PipelinePath,PipelinePath);Copy(RefinementLighting.RendererPath,RendererPath);Copy(RefinementLighting.VolumePath,VolumePath);Copy(RefinementLighting.SettingsPath,SettingsPath);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);var ps=new SerializedObject(pipeline);ps.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);ps.ApplyModifiedPropertiesWithoutUndo();Save(pipeline);
            Edit(root=>
            {
                MetalMaterials.Apply(root);var window=root.Find("ObservationWindow_Glass").GetComponent<MeshFilter>();string meshPath=Generated+"/MM_Window.asset";Copy(AssetDatabase.GetAssetPath(window.sharedMesh),meshPath);window.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);root.GetComponentInChildren<Volume>().sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);Lightmapping.lightingSettings=AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
                var preview=root.GetComponentInChildren<LevelPreview>();preview.Configure(pipeline,preview.GetComponent<PlayerInputReader>(),ArtSampleScene.ResolveFont());
            });
        }
    }
}
