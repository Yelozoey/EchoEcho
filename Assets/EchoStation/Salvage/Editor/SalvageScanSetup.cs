using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace EchoStation.Salvage.Editor
{
    public static class SalvageScanSetup
    {
        [MenuItem("Tools/EchoStation/Salvage/R2-P01/Add Trusted Scanner")]
        public static void Add(){SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);var echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();if(echo.GetComponent<SalvageScanner>())throw new InvalidOperationException("Scanner already exists.");echo.gameObject.AddComponent<SalvageScanner>();EditorSceneManager.SaveScene(scene);}finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
    }
}
