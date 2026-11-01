using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.World.Editor
{
    public static class CabinStaticOptimization
    {
        public const string Prefix="ES_Combined_";
        [MenuItem("Tools/EchoStation/Stage 1/W01/Combine Rooms And Repack UV2")]
        public static void Combine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);
            var opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty)throw new InvalidOperationException("Unsaved cabin scene: not saving user changes.");
                var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");
                if(root.GetComponentsInChildren<Renderer>().Any(r=>r.name.StartsWith(Prefix)))return;
                File.Copy(CabinGreyboxSetup.ScenePath,Path.Combine(CabinGreyboxSetup.EvidencePath,"Greybox-BeforeCombine.unity.txt"),false);
                foreach(Transform room in root.transform)
                {
                    if(!room.name.StartsWith("Cabin_Room_"))continue;
                    var groups=room.GetComponentsInChildren<MeshRenderer>().GroupBy(r=>r.sharedMaterial).ToArray();
                    foreach(var group in groups)
                    {
                        var sources=group.ToArray();if(sources.Length<2)continue;
                        var name=Prefix+room.name+"_"+group.Key.name;
                        var renderer=StaticRoomMeshCombiner.Combine(room,sources,$"{CabinGreyboxSetup.Folder}/{name}.asset");renderer.name=name;
                        var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;UnwrapParam.SetDefaults(out var settings);settings.packMargin=4f/1024;
                        string error=null;
                        if(!Unwrapping.GenerateSecondaryUVSet(mesh,settings)||!LightmapUvValidator.IsValidNonOverlapping(mesh,out error))
                            throw new InvalidOperationException("Cabin combined UV2 invalid: "+name+" / "+error);
                        EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
                    }
                }
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
