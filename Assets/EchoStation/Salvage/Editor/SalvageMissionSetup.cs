using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace EchoStation.Salvage.Editor
{
    public static class SalvageMissionSetup
    {
        [MenuItem("Tools/EchoStation/Salvage/R1-M01/Add Mission Board")]
        public static void Add(){SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);var carry=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageCarry>()).Single();if(!carry.GetComponent<SalvageMission>())carry.gameObject.AddComponent<SalvageMission>();EditorSceneManager.SaveScene(scene);}finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}}
    }
}
