using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextStreamingSetup
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03b-4d";
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Evidence);if(File.Exists(Evidence+"/BeforeStreaming.unity"))throw new InvalidOperationException("Streaming setup already attempted.");File.Copy(NextRoomPartition.Main,Evidence+"/BeforeStreaming.unity");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextRoomPartition.Main,OpenSceneMode.Additive);
            try{var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var player=root.GetComponentInChildren<NextSession>();if(player.GetComponent<NextRoomStreaming>())throw new InvalidOperationException("Streaming already attached.");player.gameObject.AddComponent<NextRoomStreaming>().Configure(root.GetComponentInChildren<SalvageEcho>());if(!EditorSceneManager.SaveScene(scene))throw new IOException("Streamed entry save failed.");}
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
