using System;
using System.IO;
using System.Linq;
using EchoStation.Tests.Fixtures;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class R05PlayerValidationBuild
    {
        [MenuItem("Tools/EchoStation/Stage 1/R05/Build Validation Player")]
        public static void Schedule() => EditorApplication.update += Build;
        private static void Build()
        {
            EditorApplication.update -= Build;
            if (EditorApplication.isPlayingOrWillChangePlaymode || Lightmapping.isRunning) throw new InvalidOperationException("Exit Play Mode/baking first.");
            var previous = SceneManager.GetActiveScene(); var scene = SceneManager.GetSceneByPath(OptimizationFixtureSetup.ScenePath);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(OptimizationFixtureSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("R05 has unsaved changes; refusing to save them.");
                var camera = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>()).Single();
                if (!camera.GetComponent<R05PlayerProbe>()) camera.gameObject.AddComponent<R05PlayerProbe>();
                camera.GetComponent<R05PlayerProbe>().validationRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            var output = "Builds/ModuleTests/S1-R05/EchoStation_R05.exe"; Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { OptimizationFixtureSetup.ScenePath },
                locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            File.WriteAllText(Path.Combine(OptimizationFixtureSetup.EvidencePath, "PlayerBuild.json"), JsonUtility.ToJson(new Result {
                result = report.summary.result.ToString(), errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                seconds = report.summary.totalTime.TotalSeconds, output = Path.GetFullPath(output),
                messages = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Warning || m.type == LogType.Error).Select(m => m.content).ToArray() }, true));
        }
        [Serializable] private class Result { public string result, output; public int errors, warnings; public double seconds; public string[] messages; }
    }
}
