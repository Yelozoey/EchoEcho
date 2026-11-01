using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextSessionSetup
    {
        public static void AddPanel()
        {
            SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NextSession>()).Single();if(player.GetComponent<NextCommandPanel>())throw new InvalidOperationException("New panel exists.");var legacy=player.GetComponent<SalvageCommandPanel>();if(legacy)UnityEngine.Object.DestroyImmediate(legacy);player.gameObject.AddComponent<NextAdvice>();player.gameObject.AddComponent<NextCommandPanel>();if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Panel save failed.");}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void Add()
        {
            SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvagePersistence>()).Single();if(player.GetComponent<NextSession>())throw new InvalidOperationException("New session already exists.");player.enabled=false;player.gameObject.AddComponent<NextSession>();if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Save failed.");}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
