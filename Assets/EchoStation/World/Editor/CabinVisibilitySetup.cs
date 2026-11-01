using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.World.Editor
{
    public static class CabinVisibilitySetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/W01/Configure Cabin Visibility")]
        public static void Configure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||Lightmapping.isRunning||StaticOcclusionCulling.isRunning)throw new InvalidOperationException("Stop Play/bake first.");
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);
            var opened=!scene.IsValid()||!scene.isLoaded;if(!opened)throw new InvalidOperationException("Close the saved cabin scene before its visibility setup; user data not saved/closed.");
            var text=File.ReadAllText(CabinGreyboxSetup.ScenePath);
            text=System.Text.RegularExpressions.Regex.Replace(text,@"smallestOccluder: [^\r\n]+","smallestOccluder: 2");
            File.WriteAllText(CabinGreyboxSetup.ScenePath,text);AssetDatabase.ImportAsset(CabinGreyboxSetup.ScenePath,ImportAssetOptions.ForceUpdate);
            scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");var camera=scene.GetRootGameObjects().Single(o=>o.name=="Cabin_ValidationCamera").GetComponent<Camera>();
                if(!root.GetComponentInChildren<OcclusionArea>())
                {
                    var area=new GameObject("Cabin_OcclusionVolume",typeof(OcclusionArea)).GetComponent<OcclusionArea>();area.transform.SetParent(root.transform,false);
                    area.center=new Vector3(2,1.6f,-1.5f);area.size=new Vector3(13.6f,2.8f,10.6f);
                }
                var budget=root.GetComponent<DistanceShadowCaster>();if(!budget)budget=root.AddComponent<DistanceShadowCaster>();
                budget.Configure(camera,root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).Cast<Renderer>().ToArray());camera.useOcclusionCulling=true;
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
