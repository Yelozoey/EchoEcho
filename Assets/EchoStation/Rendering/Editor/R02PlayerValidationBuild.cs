using System;
using System.IO;
using System.Linq;
using EchoStation.Tests.Fixtures;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class R02PlayerValidationBuild
    {
        [MenuItem("Tools/EchoStation/Stage 1/R02/Build Validation Player")]
        public static void ScheduleBuild() => EditorApplication.update += Build;

        private static void Build()
        {
            EditorApplication.update -= Build;
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before building.");
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ExplorationVolumeSetup.ScenePath);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ExplorationVolumeSetup.ScenePath, OpenSceneMode.Additive);
            if (scene.isDirty) throw new InvalidOperationException("R02 scene has unsaved changes; not saving or discarding them.");
            try
            {
                var camera = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>()).Single();
                if (!camera.GetComponent<R02PlayerProbe>()) camera.gameObject.AddComponent<R02PlayerProbe>();
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            var output = "Builds/ModuleTests/S1-R02/EchoStation_R02.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            // Enable GPU frame timestamps in this diagnostic build, then restore
            // the project's setting even if BuildPlayer throws.
            var originalTiming = PlayerSettings.enableFrameTimingStats;
            BuildReport report;
            try
            {
                PlayerSettings.enableFrameTimingStats = true;
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ExplorationVolumeSetup.ScenePath },
                    target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.Development });
            }
            finally { PlayerSettings.enableFrameTimingStats = originalTiming; AssetDatabase.SaveAssets(); }
            var messages = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Warning || m.type == LogType.Error)
                .Select(m => m.content).ToArray();
            File.WriteAllText(Path.Combine(ExplorationVolumeSetup.EvidencePath, "PlayerBuild.json"),
                JsonUtility.ToJson(new BuildResult { result = report.summary.result.ToString(),
                    errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                    messages = messages, diagnosticFrameTimingStats = true, restoredProjectFrameTimingStats = PlayerSettings.enableFrameTimingStats,
                    output = Path.GetFullPath(output), durationSeconds = report.summary.totalTime.TotalSeconds }, true));
        }

        [Serializable] private class BuildResult
        {
            public string result, output;
            public string[] messages;
            public bool diagnosticFrameTimingStats, restoredProjectFrameTimingStats;
            public int errors, warnings;
            public double durationSeconds;
        }
    }
}
