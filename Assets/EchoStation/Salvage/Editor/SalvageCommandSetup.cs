using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace EchoStation.Salvage.Editor
{
    public static class SalvageCommandSetup
    {
        [MenuItem("Tools/EchoStation/Salvage/R2-U01/Add Command UI")]
        public static void Add()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var carry=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageCarry>()).Single();if(carry.GetComponent<SalvageCommandPanel>())throw new InvalidOperationException("Command UI already exists.");carry.gameObject.AddComponent<SalvageCommandHub>();carry.gameObject.AddComponent<SalvageCommandPanel>();EditorSceneManager.SaveScene(scene);}
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
