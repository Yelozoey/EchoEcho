using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    // The MCP runner auto-saves dirty scenes. Export a read-only preflight first.
    public static class SceneSafetyGuard
    {
        public static void RequireClean()
        {
            var prefab=PrefabStageUtility.GetCurrentPrefabStage();
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||
                (prefab!=null&&prefab.scene.isDirty)||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Unsaved scene/Prefab or busy Editor: no automatic save/discard is allowed.");
        }
        [Serializable]private class Entry{public string path;public bool dirty;}
        [Serializable]private class State{public string utc;public bool allowed,playing,compiling,prefabDirty;public Entry[] scenes;}
        [MenuItem("Tools/EchoStation/Checks/Export Scene Safety")]
        public static void Export()
        {
            var prefab=PrefabStageUtility.GetCurrentPrefabStage();
            var data=new State{utc=DateTime.UtcNow.ToString("O"),playing=EditorApplication.isPlayingOrWillChangePlaymode,compiling=EditorApplication.isCompiling,
                prefabDirty=prefab!=null&&prefab.scene.isDirty,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{
                    var scene=SceneManager.GetSceneAt(i);return new Entry{path=scene.path,dirty=scene.isDirty};}).ToArray()};
            data.allowed=!data.playing&&!data.compiling&&!data.prefabDirty&&data.scenes.All(s=>!s.dirty);
            Directory.CreateDirectory("Library/EchoStation");File.WriteAllText("Library/EchoStation/SceneSafety.json",JsonUtility.ToJson(data,true));
        }
    }
}
