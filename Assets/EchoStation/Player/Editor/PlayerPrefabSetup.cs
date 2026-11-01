using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Player.Editor
{
    public static class PlayerPrefabSetup
    {
        public const string PrefabPath="Assets/EchoStation/Player/Prefabs/ES_Player.prefab";
        [MenuItem("Tools/EchoStation/Stage 1/P01/Create Player Prefab")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();if(!AssetDatabase.IsValidFolder("Assets/EchoStation/Player/Prefabs"))AssetDatabase.CreateFolder("Assets/EchoStation/Player","Prefabs");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=new GameObject("ES_Player");var controller=root.AddComponent<CharacterController>();controller.height=1.75f;controller.radius=.3f;controller.center=new Vector3(0,.875f,0);controller.skinWidth=.03f;controller.stepOffset=.25f;controller.slopeLimit=45;controller.minMoveDistance=0;root.AddComponent<PlayerInputReader>();var cameraObject=new GameObject("ViewCamera",typeof(Camera));cameraObject.transform.SetParent(root.transform,false);cameraObject.transform.localPosition=new Vector3(0,1.6f,0);cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();camera.fieldOfView=70;camera.nearClipPlane=.05f;camera.farClipPlane=60;camera.allowHDR=true;camera.allowMSAA=true;var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(0);data.allowHDROutput=false;data.renderPostProcessing=true;data.volumeLayerMask=1<<8;root.AddComponent<FirstPersonMotor>().Configure(cameraObject.transform);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
