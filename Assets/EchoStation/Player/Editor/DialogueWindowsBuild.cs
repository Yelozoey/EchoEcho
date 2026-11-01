using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoStation.Player.Editor
{
    public static class DialogueWindowsBuild
    {
        private static GlobalObjectId[] previousSelection = Array.Empty<GlobalObjectId>();
        private static int warmups;
        [MenuItem("Tools/EchoStation/Stage 2/D02/Build Windows UI Test")]
        public static void Schedule()
        {
            SceneSafetyGuard.RequireClean(); previousSelection = Selection.objects.Select(GlobalObjectId.GetGlobalObjectIdSlow).ToArray();
            Selection.objects = Array.Empty<UnityEngine.Object>(); ActiveEditorTracker.sharedTracker.ForceRebuild(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            warmups = 2; EditorApplication.update += Queue;
        }
        private static void Queue() { if (--warmups > 0) return; EditorApplication.update -= Queue; EditorApplication.update += Build; }
        private static void Build()
        {
            EditorApplication.update -= Build;
            try
            {
                SceneSafetyGuard.RequireClean(); if (!GraphicsSettings.currentRenderPipeline || GraphicsSettings.currentRenderPipeline.name != "ES_Gameplay_ForwardHDR") throw new InvalidOperationException("Gameplay Forward pipeline required.");
                const string output = "Builds/Tests/S2-D02/EchoStationDialogue.exe"; Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { CabinDemoSetup.ScenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                var notices = Path.Combine(Path.GetDirectoryName(output), "ThirdPartyNotices", "Noto"); Directory.CreateDirectory(notices);
                foreach (var file in new[] { "LICENSE.txt", "COPYRIGHT.txt" }) File.Copy("Assets/EchoStation/ThirdParty/Noto/" + file, Path.Combine(notices, file), true);
                const string evidence = "Docs/Tests/Stage2/Evidence/S2-D02/PlayerBuild.json";
                File.WriteAllText(evidence, JsonUtility.ToJson(new Result { result = report.summary.result.ToString(), errors = report.summary.totalErrors, warnings = report.summary.totalWarnings, seconds = report.summary.totalTime.TotalSeconds, output = Path.GetFullPath(output), messages = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Warning).Select(m => m.content).ToArray() }, true));
            }
            finally
            {
                Selection.objects = previousSelection.Select(GlobalObjectId.GlobalObjectIdentifierToObjectSlow).Where(o => o).ToArray(); previousSelection = Array.Empty<GlobalObjectId>(); ActiveEditorTracker.sharedTracker.ForceRebuild();
            }
        }
        [Serializable] private sealed class Result { public string result, output; public int errors, warnings; public double seconds; public string[] messages; }
    }
}
