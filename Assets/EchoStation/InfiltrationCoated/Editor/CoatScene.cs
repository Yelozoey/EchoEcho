using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationLevel.Editor;
using EchoStation.Player;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace EchoStation.InfiltrationCoated.Editor
{
    public static class CoatScene
    {
        public const string Root="Assets/EchoStation/InfiltrationCoated",Generated=Root+"/Generated",ScenePath=Root+"/Scenes/ES_Infiltration_Coated.unity",Evidence="Docs/Tests/InfiltrationPrototype/Evidence/A01F";
        public const string PipelinePath=Generated+"/CF_ForwardHDR.asset",RendererPath=Generated+"/CF_ForwardRenderer.asset",VolumePath=Generated+"/CF_Volume.asset",SettingsPath=Generated+"/CF_Lighting.asset";
        public static Transform World(Scene s)=>LevelBuilder.World(s);
        public static void Save(UnityEngine.Object o){string path=AssetDatabase.GetAssetPath(o);if(!path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("Owned coating assets only");EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);}
        public static void Edit(Action<Transform> action)
        {SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(s);action(World(s));if(!EditorSceneManager.SaveScene(s))throw new IOException("Save coating scene");}finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(s,true);}}
        static void Copy(string source,string target){if(File.Exists(target)||!AssetDatabase.CopyAsset(source,target))throw new IOException("Preserve asset "+target);}
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();ArtSampleAssets.Folder(Generated);ArtSampleAssets.Folder(Root+"/Scenes");
            Copy(EchoStation.InfiltrationMetal.Editor.MetalScene.ScenePath,ScenePath);Copy(EchoStation.InfiltrationMetal.Editor.MetalScene.PipelinePath,PipelinePath);Copy(EchoStation.InfiltrationMetal.Editor.MetalScene.RendererPath,RendererPath);Copy(EchoStation.InfiltrationMetal.Editor.MetalScene.VolumePath,VolumePath);Copy(EchoStation.InfiltrationMetal.Editor.MetalScene.SettingsPath,SettingsPath);
            var p=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);var serialized=new SerializedObject(p);serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);serialized.ApplyModifiedPropertiesWithoutUndo();Save(p);
            Edit(root=>
            {
                CoatMaterials.Apply(root);
                var window=root.Find("ObservationWindow_Glass").GetComponent<MeshFilter>();Copy(AssetDatabase.GetAssetPath(window.sharedMesh),Generated+"/CF_Window.asset");window.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/CF_Window.asset");
                var old=root.GetComponentInChildren<EchoStation.InfiltrationMetal.MetalPlanar>();var plane=old.gameObject.AddComponent<CoatPlanar>();plane.sourceCamera=old.sourceCamera;plane.localPlanePoint=old.localPlanePoint;plane.localPlaneNormal=old.localPlaneNormal;plane.textureWidth=old.textureWidth;plane.clipOffset=old.clipOffset;UnityEngine.Object.DestroyImmediate(root.GetComponent<EchoStation.InfiltrationMetal.MetalReflectionView>());UnityEngine.Object.DestroyImmediate(old);
                var mesh=plane.GetComponent<MeshFilter>();Copy(AssetDatabase.GetAssetPath(mesh.sharedMesh),Generated+"/CF_ReflectivePatch.asset");mesh.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/CF_ReflectivePatch.asset");plane.name="CF_ReflectivePatch";root.gameObject.AddComponent<CoatReflectionView>().Configure(plane,ArtSampleScene.ResolveFont());
                root.GetComponentInChildren<Volume>().sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);Lightmapping.lightingSettings=AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
                var view=root.GetComponentInChildren<LevelPreview>();view.Configure(p,view.GetComponent<PlayerInputReader>(),ArtSampleScene.ResolveFont());
            });
        }
    }
}
