using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.World;
using EchoStation.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Echo.Editor
{
    public static class EchoScanSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/E08/Move Maintenance Note To Visible Desk Front")]
        public static void FixMaintenancePlacement()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                var note=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<WorldTarget>()).Single(t=>t.Id=="note_maintenance_01");
                note.transform.position=new Vector3(-2.8f,.8f,1.885f);note.transform.rotation=Quaternion.Euler(90,0,0);EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        [MenuItem("Tools/EchoStation/Stage 1/E08/Configure Echo Scanner And Equipment Fact")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var prefab=PrefabUtility.LoadPrefabContents(EchoModelSetup.PrefabPath);
            try{if(!prefab.GetComponent<EchoScanController>())prefab.AddComponent<EchoScanController>();PrefabUtility.SaveAsPrefabAsset(prefab,EchoModelSetup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                var panel=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<WorldTarget>()).Single(t=>t.Id=="power_panel_01");
                panel.GetComponent<EchoTargetPermissions>().SetScanFact("检测到供电面板异常信号。建议保持距离。");EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
