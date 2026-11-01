using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextWorldLabelSetup
    {
        public const string ShaderPath=FacilityAssets.Root+"/Shaders/NextWorldText.shader",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-P01a-1";
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Evidence);if(File.Exists(Evidence+"/BeforeLabels.unity"))throw new InvalidOperationException("Label setup already attempted.");File.Copy(NextRoomPartition.Main,Evidence+"/BeforeLabels.unity");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextRoomPartition.Main,OpenSceneMode.Additive);
            try{var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var hub=root.GetComponentInChildren<SalvageCommandHub>();var texts=root.GetComponentsInChildren<TextMesh>(true);if(texts.Length!=13||hub.GetComponent<NextWorldLabels>())throw new InvalidOperationException("Expected thirteen original labels.");var legacy=hub.GetComponent<SalvageDiscoveryMarkers>();if(!legacy)throw new InvalidOperationException("Expected legacy marker adapter.");UnityEngine.Object.DestroyImmediate(legacy);hub.gameObject.AddComponent<NextWorldLabels>().Configure(root.GetComponentInChildren<Camera>(),AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath),texts,hub);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Label entry save failed.");}
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
