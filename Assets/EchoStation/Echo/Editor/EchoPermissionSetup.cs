using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.World;
using EchoStation.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace EchoStation.Echo.Editor
{
    public static class EchoPermissionSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/E04/Configure Cabin Echo Permissions")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                foreach(var target in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<WorldTarget>()))
                {var permissions=target.GetComponent<EchoTargetPermissions>();if(!permissions)permissions=target.gameObject.AddComponent<EchoTargetPermissions>();permissions.Configure(true,target.Id=="power_panel_01");}
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
