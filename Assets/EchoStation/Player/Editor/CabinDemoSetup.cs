using System;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.World.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EchoStation.Player.Editor
{
    public static class CabinDemoSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/P09/Bake Demo Lighting")]public static void BakeLighting(){SceneSafetyGuard.RequireClean();CabinLightingBake.StartBakeFor(ScenePath,"Docs/Tests/Stage1/Evidence/S1-P09");}
        [MenuItem("Tools/EchoStation/Stage 1/P09/Bake Demo Occlusion")]public static void BakeOcclusion(){SceneSafetyGuard.RequireClean();CabinOcclusionBake.StartBakeFor(ScenePath,"Docs/Tests/Stage1/Evidence/S1-P09");}
        public const string ScenePath="Assets/EchoStation/World/Scenes/ES_Cabin_Demo.unity";
        public const string DataFolder="Assets/EchoStation/World/Scenes/ES_Cabin_Demo";
        [MenuItem("Tools/EchoStation/Stage 1/P09/Configure Cabin Demo")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();if(SceneManager.GetSceneByPath(ScenePath).isLoaded)throw new InvalidOperationException("Close owned saved Demo scene before setup.");if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
            {
                if(!AssetDatabase.CopyAsset(CabinGreyboxSetup.ScenePath,ScenePath)||!AssetDatabase.CopyAsset("Assets/EchoStation/World/Scenes/ES_Cabin_Greybox",DataFolder))throw new InvalidOperationException("Owned cabin copy failed.");var text=File.ReadAllText(ScenePath);foreach(var file in new[]{"LightingData.asset","OcclusionCullingData.asset"})text=text.Replace(AssetDatabase.AssetPathToGUID("Assets/EchoStation/World/Scenes/ES_Cabin_Greybox/"+file),AssetDatabase.AssetPathToGUID(DataFolder+"/"+file));File.WriteAllText(ScenePath,text);AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceUpdate);
            }
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var roots=scene.GetRootGameObjects();foreach(var camera in roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)))if(!camera.GetComponentInParent<FirstPersonMotor>())camera.gameObject.SetActive(false);var player=roots.SelectMany(r=>r.GetComponentsInChildren<FirstPersonMotor>()).SingleOrDefault();if(!player){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));go.transform.position=new Vector3(0,.05f,-6);player=go.GetComponent<FirstPersonMotor>();}var brain=roots.SelectMany(r=>r.GetComponentsInChildren<EchoBrain>()).SingleOrDefault();if(!brain){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));go.transform.position=new Vector3(1.5f,0,-2);brain=go.GetComponent<EchoBrain>();}var demo=player.GetComponent<CabinDemo>();if(!demo)demo=player.gameObject.AddComponent<CabinDemo>();var serialized=new SerializedObject(demo);serialized.FindProperty("echo").objectReferenceValue=brain;serialized.ApplyModifiedPropertiesWithoutUndo();var cameraView=player.View.GetComponent<Camera>();if(!cameraView.GetComponent<AudioListener>())cameraView.gameObject.AddComponent<AudioListener>();var view=cameraView.GetComponent<ScanWaveView>();if(!view)view=cameraView.gameObject.AddComponent<ScanWaveView>();view.Source=brain.GetComponent<ScanWaveEmitter>();var obstacle=player.GetComponent<NavMeshObstacle>();if(!obstacle)obstacle=player.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Capsule;obstacle.center=new Vector3(0,.875f,0);obstacle.height=1.75f;obstacle.radius=.32f;obstacle.carving=true;obstacle.carveOnlyStationary=true;obstacle.carvingMoveThreshold=.1f;obstacle.carvingTimeToStationary=.2f;var world=roots.Single(r=>r.name=="ES_Cabin_Greybox");var navPath=DataFolder+"/NavMesh.asset";if(!AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath)&&!AssetDatabase.CopyAsset(EchoNavigationSetup.DataPath,navPath))throw new InvalidOperationException("Owned navigation copy failed.");var surface=world.GetComponent<NavMeshSurface>();surface.RemoveData();surface.navMeshData=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);surface.AddData();world.GetComponent<DistanceShadowCaster>().Configure(cameraView,world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).Cast<Renderer>().ToArray());EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(ScanWaveSetup.PipelinePath);if(!pipeline)throw new InvalidOperationException("Approved gameplay pipeline missing.");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;AssetDatabase.SaveAssets();
        }
    }
}
