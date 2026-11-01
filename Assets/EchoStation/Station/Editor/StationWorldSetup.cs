using System;
using System.IO;
using System.Linq;
using EchoStation.Dialogue.Integration;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.World.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Station.Editor
{
    public static class StationWorldSetup
    {
        public const string NavPath=StationLayout.DataFolder+"/StationNavMesh.asset";
        [MenuItem("Tools/EchoStation/Stage 2/W01/Create Five Zone Foundation")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadAssetAtPath<SceneAsset>(StationLayout.ScenePath))return;
            CabinGreyboxSetup.EnsureFolder(StationLayout.DataFolder);CabinGreyboxSetup.EnsureFolder("Assets/EchoStation/Station/Scenes");
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(scene);RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.13f,.18f,.25f);RenderSettings.reflectionIntensity=.2f;RenderSettings.fog=false;
                var root=new GameObject("ES_Station_Greybox");
                foreach(var zone in StationLayout.Zones){var region=new GameObject("Zone_"+zone.Id);region.transform.SetParent(root.transform,false);
                    var center=zone.Center;var size=zone.Size;if(zone.Id=="lab_a"){center.z=30;size.z=16;}Box("Foundation_"+zone.Id,center-Vector3.up*.1f,size,region.transform);
                }
                var ramp=Box("Descent_Ramp",new Vector3(0,-2.1f,13),new Vector3(3.2f,.2f,Mathf.Sqrt(18*18+16)),root.transform);ramp.transform.rotation=Quaternion.Euler(Mathf.Atan2(4,18)*Mathf.Rad2Deg,0,0);
                var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));player.transform.position=StationLayout.PlayerSpawn;
                var robot=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));robot.transform.position=StationLayout.EchoSpawn;
                var motor=player.GetComponent<FirstPersonMotor>();var camera=motor.View.GetComponent<Camera>();camera.farClipPlane=90;camera.backgroundColor=new Color(.025f,.05f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;if(!camera.GetComponent<AudioListener>())camera.gameObject.AddComponent<AudioListener>();
                camera.gameObject.AddComponent<ScanWaveView>().Source=robot.GetComponent<ScanWaveEmitter>();
                player.AddComponent<DialoguePanel>();player.AddComponent<ScriptDialogueController>();player.AddComponent<StationSession>().Configure(robot.GetComponent<EchoBrain>());player.AddComponent<StationControls>();
                player.GetComponent<PlayerJournal>().BindEcho(robot.GetComponent<EchoScanController>());
                var obstacle=player.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Capsule;obstacle.center=new Vector3(0,.875f,0);obstacle.height=1.75f;obstacle.radius=.32f;obstacle.carving=true;obstacle.carveOnlyStationary=true;
                var sun=new GameObject("Station_DayFill",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.65f,.77f,1);sun.intensity=.7f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(42,-28,0);
                var volume=new GameObject("Station_ExplorationVolume",typeof(Volume)).GetComponent<Volume>();volume.gameObject.layer=8;volume.isGlobal=true;volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ExplorationVolumeSetup.ExplorationProfilePath);
                BakeNav(root);root.AddComponent<DistanceShadowCaster>().Configure(camera,root.GetComponentsInChildren<MeshRenderer>().Cast<Renderer>().ToArray());
                EditorSceneManager.SaveScene(scene,StationLayout.ScenePath);AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/Tests/Stage2/Evidence/S2-W01");File.WriteAllText("Docs/Tests/Stage2/Evidence/S2-W01/Create.json",JsonUtility.ToJson(new BuildRecord{scene=StationLayout.ScenePath,nav=NavPath,agent=EchoNavigationSetup.AgentTypeId,vertices=NavMesh.CalculateTriangulation().vertices.Length},true));
            } finally {if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        public static GameObject Box(string name,Vector3 center,Vector3 size,Transform parent,Material material=null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=center;go.transform.localScale=size;
            go.GetComponent<MeshRenderer>().sharedMaterial=material?material:Material("ES_Station_BlueGray",new Color(.29f,.37f,.43f));
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);return go;
        }
        public static Material Material(string name,Color color){var path=StationLayout.DataFolder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(m,path);return m;}
        public static void BakeNav(GameObject root)
        {
            var surface=root.GetComponent<NavMeshSurface>();if(!surface)surface=root.AddComponent<NavMeshSurface>();surface.agentTypeID=EchoNavigationSetup.AgentTypeId;surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.ignoreNavMeshAgent=true;surface.ignoreNavMeshObstacle=true;surface.overrideVoxelSize=true;surface.voxelSize=.08f;surface.overrideTileSize=true;surface.tileSize=128;surface.minRegionArea=.1f;
            surface.RemoveData();surface.BuildNavMesh();if(!surface.navMeshData)throw new InvalidOperationException("No owned Station navigation data.");var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath);
            if(previous){EditorUtility.CopySerialized(surface.navMeshData,previous);surface.RemoveData();surface.navMeshData=previous;surface.AddData();EditorUtility.SetDirty(previous);}else AssetDatabase.CreateAsset(surface.navMeshData,NavPath);
        }
        public static void Edit(Action<Scene,GameObject> configure)
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(StationLayout.ScenePath);var alreadyOpen=scene.isLoaded;if(!alreadyOpen)scene=EditorSceneManager.OpenScene(StationLayout.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_Station_Greybox");configure(scene,root);BakeNav(root);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(!alreadyOpen)EditorSceneManager.CloseScene(scene,true);}
        }
        [Serializable]private class BuildRecord {public string scene,nav;public int agent,vertices;}
    }
}
