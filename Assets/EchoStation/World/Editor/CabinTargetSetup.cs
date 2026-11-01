using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.World.Editor
{
    public static class CabinTargetSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/W03/Configure Cabin Target Identities")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox").transform;
                Add(root.Find("Cabin_Anchors/Anchor_NoteMaintenance"),"note_maintenance_01","维护记录");
                Add(root.Find("Cabin_Anchors/Anchor_NoteRoster"),"note_roster_01","值班记录");
                Add(root.Find("Cabin_Anchors/Anchor_TerminalData"),"terminal_data_01","观测数据终端");
                Add(root.Find("Cabin_Anchors/Anchor_HiddenScanMark"),"scan_mark_01","隐藏校准标记");
                Add(root.Find("Cabin_Room_Main/Main_PowerPanelProxy"),"power_panel_01","供电面板");
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        private static void Add(Transform transform,string id,string label)
        {
            if(transform.GetComponent<WorldTarget>())return;var active=transform.gameObject.activeSelf;transform.gameObject.SetActive(false);
            transform.gameObject.AddComponent<WorldTarget>().Configure(id,label);transform.gameObject.SetActive(active);
        }
    }
}
