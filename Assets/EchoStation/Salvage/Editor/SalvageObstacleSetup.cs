using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace EchoStation.Salvage.Editor
{
    public static class SalvageObstacleSetup
    {
        public const string NavPath=SalvageAssetImport.Generated+"/ES_Salvage_R2_Nav.asset";
        [MenuItem("Tools/EchoStation/Salvage/R2-N01/Add Dynamic Obstacles")]
        public static void Add()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath))throw new InvalidOperationException("R2 navigation exists.");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_Salvage_World");var carry=root.GetComponentInChildren<SalvageCarry>();
                foreach(var actor in root.GetComponentsInChildren<CargoActor>())
                {var obstacle=actor.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=actor.Body.center;obstacle.size=actor.Body.size;Set(obstacle);actor.gameObject.AddComponent<SalvageCargoObstacle>().Configure(carry);}
                var player=carry.gameObject.AddComponent<NavMeshObstacle>();player.shape=NavMeshObstacleShape.Capsule;player.center=Vector3.up*.875f;player.radius=.33f;player.height=1.75f;Set(player);
                var surface=root.GetComponent<NavMeshSurface>();surface.BuildNavMesh();if(!surface.navMeshData)throw new InvalidOperationException("R2 bake missing.");AssetDatabase.CreateAsset(surface.navMeshData,NavPath);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        [MenuItem("Tools/EchoStation/Salvage/R2-B01/Use Stable Local Obstacles")]
        public static void Stabilize()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var obstacles=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NavMeshObstacle>()).ToArray();if(obstacles.Length!=8)throw new InvalidOperationException("Expected seven cargo and player obstacles.");foreach(var obstacle in obstacles)obstacle.carving=false;EditorSceneManager.SaveScene(scene);}
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        static void Set(NavMeshObstacle o){o.carving=false;o.carveOnlyStationary=true;o.carvingMoveThreshold=.1f;o.carvingTimeToStationary=.15f;}
    }
}
