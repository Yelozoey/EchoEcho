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
    public static class R01PlayerValidationBuild
    {
        [MenuItem("Tools/EchoStation/Stage 1/R01/Build Validation Player")]
        public static void ScheduleBuild() => EditorApplication.update += BuildOnNextUpdate;

        private static void BuildOnNextUpdate()
        {
            EditorApplication.update -= BuildOnNextUpdate;
            Build();
        }

        private static void Build()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ForwardHdrBaselineSetup.ScenePath);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ForwardHdrBaselineSetup.ScenePath, OpenSceneMode.Additive);
            if (scene.isDirty) throw new InvalidOperationException("Test scene has unsaved changes; refusing to overwrite it.");
            try
            {
                var camera = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>()).Single();
                if (!camera.GetComponent<R01PlayerProbe>()) camera.gameObject.AddComponent<R01PlayerProbe>();
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            var output = "Builds/ModuleTests/S1-R01/EchoStation_R01.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ForwardHdrBaselineSetup.ScenePath },
                target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.Development });
            File.WriteAllText(Path.Combine(ForwardHdrBaselineSetup.EvidencePath, "PlayerBuild.json"),
                JsonUtility.ToJson(new BuildResult { result = report.summary.result.ToString(),
                    errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                    output = Path.GetFullPath(output), durationSeconds = report.summary.totalTime.TotalSeconds }, true));
        }

        [Serializable] private class BuildResult
        {
            public string result, output;
            public int errors, warnings;
            public double durationSeconds;
        }
    }
}
