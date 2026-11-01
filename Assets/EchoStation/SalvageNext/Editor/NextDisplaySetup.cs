using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextDisplaySetup
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-P01a-3c";
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Evidence);string backup=Evidence+"/Main_Before_A.unity";if(File.Exists(backup))throw new InvalidOperationException("Display preparation already started.");File.Copy(NextRoomPartition.Main,backup);var old=SceneManager.GetActiveScene();
            using(NextSdkSaveScope.Begin(Evidence+"/PrepareScope_A.json"))
            {
                var scene=EditorSceneManager.OpenScene(NextRoomPartition.Main,OpenSceneMode.Additive);
                try{SceneManager.SetActiveScene(scene);var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>(true)).Single();if(player.GetComponent<NextDisplayPanel>())throw new InvalidOperationException("Display component already present.");player.gameObject.AddComponent<NextDisplayPanel>();if(!EditorSceneManager.SaveScene(scene))throw new IOException("Display scene save failed.");}
                finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
            }
        }
    }
}
