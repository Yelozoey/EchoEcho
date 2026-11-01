using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace EchoStation.Salvage.Editor
{
    public static class SalvagePersistenceSetup
    {
        [MenuItem("Tools/EchoStation/Salvage/R1-S01/Add Persistence")]
        public static void Add(){SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);var carry=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageCarry>()).Single();if(!carry.GetComponent<SalvagePersistence>())carry.gameObject.AddComponent<SalvagePersistence>();EditorSceneManager.SaveScene(scene);}finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}}
    }
}
