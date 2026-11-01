using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class BakedLightingBake
    {
        [Serializable] public class Atlas
        {
            public string color, direction, colorFormat, directionFormat;
            public int width, height;
            public long runtimeBytes;
        }
        [Serializable] public class BakeResult
        {
            public bool passed, originalLayoutRestored;
            public string startedUtc, finishedUtc, error, lightingDataAsset;
            public double seconds;
            public int probeCount, staticRendererCount;
            public Atlas[] atlases;
        }
        private static SceneSetup[] previous;
        private static DateTime started;
        private static double nextProgress;
        private static bool complete, cancelled;

        [MenuItem("Tools/EchoStation/Stage 1/R03/Bake Isolated Lighting")]
        public static void StartBake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Lightmapping.isRunning || previous != null)
                throw new InvalidOperationException("Another play/bake operation is active.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene detected. No scene is saved or closed.");
            var prefab = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefab != null) throw new InvalidOperationException("Close the Prefab Stage before the isolated bake; no prefab changes are saved.");
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(BakedLightingSetup.ScenePath)) throw new InvalidOperationException("Create and test R03-A first.");
            previous = EditorSceneManager.GetSceneManagerSetup();
            File.WriteAllText(Path.Combine(BakedLightingSetup.EvidencePath, "SceneLayout.before.json"),
                JsonUtility.ToJson(new Layout { scenes = previous.Select(s => new Entry { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray() }, true));
            try
            {
                EditorSceneManager.OpenScene(BakedLightingSetup.ScenePath, OpenSceneMode.Single);
                started = DateTime.UtcNow; complete = false; cancelled = false;
                Lightmapping.bakeCompleted += Completed; Lightmapping.bakeCancelled += Cancelled;
                EditorApplication.update += Tick;
                if (!Lightmapping.BakeAsync()) Finish("Lightmapping.BakeAsync refused to start.");
                else Debug.Log("[EchoStation R03-B] CPU bake started with only R03 scene loaded.");
            }
            catch (Exception exception) { Finish(exception.ToString()); }
        }

        private static void Completed() => complete = true;
        private static void Cancelled() => cancelled = true;
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup >= nextProgress)
            {
                nextProgress = EditorApplication.timeSinceStartup + 2;
                File.WriteAllText(Path.Combine(BakedLightingSetup.EvidencePath, "BakeProgress.json"),
                    JsonUtility.ToJson(new Progress { running = Lightmapping.isRunning, progress = Lightmapping.buildProgress,
                        seconds = (DateTime.UtcNow - started).TotalSeconds }, true));
            }
            if (!Lightmapping.isRunning && (complete || cancelled)) Finish(cancelled ? "Bake cancelled." : null);
        }

        private static void Finish(string error)
        {
            Lightmapping.bakeCompleted -= Completed; Lightmapping.bakeCancelled -= Cancelled; EditorApplication.update -= Tick;
            var result = new BakeResult { startedUtc = started.ToString("O"), finishedUtc = DateTime.UtcNow.ToString("O"),
                seconds = (DateTime.UtcNow - started).TotalSeconds, error = error };
            try
            {
                var scene = SceneManager.GetActiveScene();
                if (SceneManager.sceneCount != 1 || scene.path != BakedLightingSetup.ScenePath)
                    throw new InvalidOperationException("Loaded scenes changed during bake; not saving/closing the user's new scene layout.");
                if (string.IsNullOrEmpty(error))
                {
                    EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                    result.lightingDataAsset = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                    result.atlases = LightmapSettings.lightmaps.Select(map => new Atlas {
                        color = AssetDatabase.GetAssetPath(map.lightmapColor), direction = AssetDatabase.GetAssetPath(map.lightmapDir),
                        width = map.lightmapColor ? map.lightmapColor.width : 0, height = map.lightmapColor ? map.lightmapColor.height : 0,
                        colorFormat = map.lightmapColor ? map.lightmapColor.graphicsFormat.ToString() : "missing",
                        directionFormat = map.lightmapDir ? map.lightmapDir.graphicsFormat.ToString() : "missing",
                        runtimeBytes = (map.lightmapColor ? Profiler.GetRuntimeMemorySizeLong(map.lightmapColor) : 0) +
                            (map.lightmapDir ? Profiler.GetRuntimeMemorySizeLong(map.lightmapDir) : 0) }).ToArray();
                    result.probeCount = LightmapSettings.lightProbes ? LightmapSettings.lightProbes.count : 0;
                    var renderers = scene.GetRootGameObjects().Single(o => o.name == "R03_StaticRoom").GetComponentsInChildren<MeshRenderer>();
                    result.staticRendererCount = renderers.Length;
                    result.passed = !string.IsNullOrEmpty(result.lightingDataAsset) && result.atlases.Length > 0 &&
                        result.atlases.All(a => a.width > 0 && !string.IsNullOrEmpty(a.color) && !string.IsNullOrEmpty(a.direction)) &&
                        result.probeCount > 0 && renderers.All(r => r.lightmapIndex >= 0 && r.lightmapIndex < result.atlases.Length);
                }
                if (previous != null) { EditorSceneManager.RestoreSceneManagerSetup(previous); result.originalLayoutRestored = true; }
            }
            catch (Exception exception) { result.error = exception.ToString(); result.passed = false; }
            finally
            {
                previous = null;
                File.WriteAllText(Path.Combine(BakedLightingSetup.EvidencePath, "BakeResult.json"), JsonUtility.ToJson(result, true));
                Debug.Log($"[EchoStation R03-B] Bake finished. dataPassed={result.passed}, originalLayoutRestored={result.originalLayoutRestored}");
            }
        }

        [Serializable] private class Progress { public bool running; public float progress; public double seconds; }
        [Serializable] private class Entry { public string path; public bool loaded, active; }
        [Serializable] private class Layout { public Entry[] scenes; }
    }
}
