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
    public static class EchoInvestigationSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/E07/Configure Cabin Investigation Points")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var cabin=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox").transform;var points=cabin.Find("Cabin_InvestigationPoints");if(!points){points=new GameObject("Cabin_InvestigationPoints").transform;points.SetParent(cabin,false);}
                foreach(var target in cabin.GetComponentsInChildren<WorldTarget>())
                {
                    var point=points.Find("Approach_"+target.Id);if(!point){point=new GameObject("Approach_"+target.Id).transform;point.SetParent(points,false);}
                    point.position=target.Id switch{"note_maintenance_01"=>new Vector3(-2.8f,0,1.15f),"terminal_data_01"=>new Vector3(-2.1f,0,1.15f),"note_roster_01"=>new Vector3(7.7f,0,2.1f),"scan_mark_01"=>new Vector3(8.1f,0,2.1f),"power_panel_01"=>new Vector3(3.8f,0,-1.5f),_=>throw new System.InvalidOperationException("Unapproved investigation target: "+target.Id)};
                    target.GetComponent<EchoTargetPermissions>().SetApproachPoint(point);
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
