using System;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace EchoStation.Salvage.Editor
{
    public static class SalvageFeedbackSetup
    {
        [MenuItem("Tools/EchoStation/Salvage/R2-P01/Add Scan Feedback")]
        public static void Add(){SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);var echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();if(echo.GetComponent<SalvageScanFeedback>())throw new InvalidOperationException("Feedback already exists.");var hub=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageCommandHub>()).Single();echo.GetComponent<EchoScanWaveBinding>().enabled=false;echo.gameObject.AddComponent<SalvageScanFeedback>();hub.gameObject.AddComponent<SalvageDiscoveryMarkers>();var camera=hub.GetComponent<SalvageMotor>().View.gameObject;var waveView=camera.GetComponent<ScanWaveView>();if(!waveView)waveView=camera.AddComponent<ScanWaveView>();waveView.Source=echo.GetComponent<ScanWaveEmitter>();EditorSceneManager.SaveScene(scene);}finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
    }
}
