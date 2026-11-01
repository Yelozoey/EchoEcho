using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Rendering.Editor
{
    public static class RealtimeShadowSetup
    {
        public const string EvidencePath = "Docs/Tests/Stage1/Evidence/S1-R04";
        [MenuItem("Tools/EchoStation/Stage 1/R04/Configure Shadows")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            Directory.CreateDirectory(EvidencePath);
            var snapshot = Path.Combine(EvidencePath, "ProtectedFiles.json");
            if (!File.Exists(snapshot))
            {
                var paths = ForwardHdrBaselineSetup.TemplatePaths.Concat(new[] { ForwardHdrBaselineSetup.ScenePath,
                    ExplorationVolumeSetup.ScenePath, ExplorationVolumeSetup.BaseProfilePath, ExplorationVolumeSetup.ExplorationProfilePath,
                    ExplorationVolumeSetup.PrefabPath, ForwardHdrBaselineSetup.RendererPath, BakedLightingSetup.ScenePath,
                    BakedLightingSetup.SettingsPath, BakedLightingSetup.ScenePath.Replace(".unity", "/LightingData.asset") });
                File.WriteAllText(snapshot, JsonUtility.ToJson(new ForwardHdrBaselineSetup.SetupRecord {
                    capturedUtc = DateTime.UtcNow.ToString("O"), templates = paths.Select(p => new ForwardHdrBaselineSetup.TemplateRecord {
                        path = p, sha256 = ForwardHdrBaselineSetup.Hash(p) }).ToArray() }, true));
                File.Copy(ForwardHdrBaselineSetup.PipelinePath, Path.Combine(EvidencePath, "Pipeline.before.txt"));
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ForwardHdrBaselineSetup.PipelinePath);
            pipeline.shadowDistance = 40; pipeline.cascade4Split = new Vector3(.1f, .25f, .5f); pipeline.cascadeBorder = .1f;
            EditorUtility.SetDirty(pipeline); AssetDatabase.SaveAssetIfDirty(pipeline);
            Debug.Log("[EchoStation R04-A] Four-cascade 2048/40m shadow configuration; runtime tests still required.");
        }
    }
}
