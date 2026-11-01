using System;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Rendering.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace EchoStation.Salvage.Editor
{
    public static class SalvageEchoSetup
    {
        [MenuItem("Tools/EchoStation/Salvage/R1-E01/Add Echo Battery And Follow")]
        public static void Add()
        {
            SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_Salvage_World");if(root.GetComponentInChildren<SalvageEcho>())throw new InvalidOperationException("Echo exists; preserve it.");
                var player=root.GetComponentInChildren<SalvageMotor>();foreach(var a in root.GetComponentsInChildren<CargoActor>())a.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild=true;player.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
                int agent=-1;for(int i=0;i<NavMesh.GetSettingsCount();i++){var s=NavMesh.GetSettingsByIndex(i);if(NavMesh.GetSettingsNameFromID(s.agentTypeID)=="Echo")agent=s.agentTypeID;}if(agent==-1)throw new InvalidOperationException("Existing Echo agent missing; no project setting changed.");
                var surface=root.AddComponent<NavMeshSurface>();surface.agentTypeID=agent;surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.ignoreNavMeshAgent=true;surface.ignoreNavMeshObstacle=true;surface.overrideVoxelSize=true;surface.voxelSize=.08f;surface.overrideTileSize=true;surface.tileSize=128;surface.minRegionArea=.1f;surface.BuildNavMesh();
                if(!surface.navMeshData)throw new InvalidOperationException("Salvage nav bake failed.");AssetDatabase.CreateAsset(surface.navMeshData,SalvageAssetImport.Generated+"/ES_Salvage_Nav.asset");
                var robot=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EchoStation/Echo/Prefabs/ES_Echo.prefab"),scene);robot.name="SalvageEcho";robot.transform.SetParent(root.transform);robot.transform.position=new Vector3(4,0,10);
                robot.GetComponent<EchoBrain>().enabled=false;robot.GetComponent<EchoScanController>().enabled=false;robot.GetComponent<EchoPresentation>().enabled=false;
                robot.AddComponent<SalvageEcho>().Configure(player.transform);EditorSceneManager.SaveScene(scene);
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
