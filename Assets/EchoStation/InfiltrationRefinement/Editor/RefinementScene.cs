using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationLevel.Editor;
using EchoStation.Player;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.InfiltrationRefinement.Editor
{
    public static class RefinementScene
    {
        public const string Root="Assets/EchoStation/InfiltrationRefinement",Generated=Root+"/Generated",ScenePath=Root+"/Scenes/ES_Infiltration_Dressed.unity",Evidence="Docs/Tests/InfiltrationPrototype/Evidence/A01D";
        public static void Save(Object obj){string path=AssetDatabase.GetAssetPath(obj);if(!path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("Save owned refinement only: "+path);EditorUtility.SetDirty(obj);AssetDatabase.SaveAssetIfDirty(obj);}
        public static Transform World(Scene scene)=>LevelBuilder.World(scene);
        public static void Edit(Action<Transform> action)
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);action(World(scene));if(!EditorSceneManager.SaveScene(scene))throw new IOException("Owned scene save failed");}
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        public static Material MaterialFor(Material source)
        {
            if(!source)throw new InvalidOperationException("Source material required");
            string path=Generated+"/DR_"+source.name.Replace(' ','_').Replace('.','_')+".mat";var result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!result){result=Object.Instantiate(source);result.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(result,path);Save(result);}return result;
        }
        public static void BuildNav(Transform root,string version)
        {
            var player=root.GetComponentInChildren<SalvageMotor>();bool active=player.gameObject.activeSelf;player.gameObject.SetActive(false);var surface=root.GetComponent<NavMeshSurface>();surface.RemoveData();surface.navMeshData=null;
            surface.collectObjects=CollectObjects.Volume;surface.center=new Vector3(0,1.1f,0);surface.size=new Vector3(36,2.8f,32);surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.1f;
            try{surface.BuildNavMesh();if(!surface.navMeshData)throw new InvalidOperationException("Navigation output missing");string path=Generated+"/DR_Nav_"+version+".asset";if(File.Exists(path))throw new InvalidOperationException("Preserve nav version");AssetDatabase.CreateAsset(surface.navMeshData,path);Save(surface.navMeshData);}
            finally{player.gameObject.SetActive(active);}
        }
        static void Copy(string from,string to){if(File.Exists(to)||!AssetDatabase.CopyAsset(from,to))throw new InvalidOperationException("Copy owned asset refused: "+to);}
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();ArtSampleAssets.Folder(Generated);ArtSampleAssets.Folder(Root+"/Scenes");ArtSampleAssets.Folder(Root+"/Prefabs");
            Copy(LevelBuilder.ScenePath,ScenePath);Copy(LevelLighting.RendererPath,RefinementLighting.RendererPath);Copy(LevelLighting.PipelinePath,RefinementLighting.PipelinePath);Copy(LevelLighting.VolumePath,RefinementLighting.VolumePath);Copy(LevelLighting.SettingsPath,RefinementLighting.SettingsPath);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RefinementLighting.PipelinePath);var so=new SerializedObject(pipeline);so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RefinementLighting.RendererPath);so.ApplyModifiedPropertiesWithoutUndo();Save(pipeline);
            // Keep the inherited A01 bake read-only for D01 geometry review. D02 detaches it before any bake.
            Edit(root=>
            {
                foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(MaterialFor).ToArray();
                var window=root.Find("ObservationWindow_Glass").GetComponent<MeshFilter>();string meshPath=Generated+"/DR_WindowPane.asset";Copy(AssetDatabase.GetAssetPath(window.sharedMesh),meshPath);window.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                root.GetComponentInChildren<Volume>().sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(RefinementLighting.VolumePath);
                Lightmapping.lightingSettings=AssetDatabase.LoadAssetAtPath<LightingSettings>(RefinementLighting.SettingsPath);
                var preview=root.GetComponentInChildren<LevelPreview>();preview.Configure(pipeline,preview.GetComponent<PlayerInputReader>(),ArtSampleScene.ResolveFont());
            });
        }
    }
}
