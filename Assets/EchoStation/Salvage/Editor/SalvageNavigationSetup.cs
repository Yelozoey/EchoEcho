using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace EchoStation.Salvage.Editor
{
    public static class SalvageNavigationSetup
    {
        [MenuItem("Tools/EchoStation/Salvage/R2-N01/Add Behavior Navigation")]
        public static void Add(){SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);var echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();if(echo.GetComponent<SalvageEchoCoordinator>())throw new InvalidOperationException("Coordinator already exists.");echo.gameObject.AddComponent<SalvageEchoCoordinator>();EditorSceneManager.SaveScene(scene);}finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
    }
}
