using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class ForwardHdrBaselineSetup
    {
        public const string PipelinePath = "Assets/EchoStation/Rendering/Settings/ES_PC_ForwardHDR.asset";
        public const string RendererPath = "Assets/EchoStation/Rendering/Settings/ES_PC_ForwardRenderer.asset";
        public const string ScenePath = "Assets/EchoStation/Tests/Scenes/S1_R01_ForwardHDR.unity";
        public const string MaterialPath = "Assets/EchoStation/Tests/Fixtures/R01_HDRProbe.mat";
        public const string EvidencePath = "Docs/Tests/Stage1/Evidence/S1-R01";
        public static readonly string[] TemplatePaths = {
            "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/PC_Renderer.asset", "Assets/Scenes/SampleScene.unity"
        };

        [Serializable] public class TemplateRecord { public string path; public string sha256; }
        [Serializable] public class SetupRecord { public string capturedUtc; public TemplateRecord[] templates; }

        public static string Hash(string path)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        [MenuItem("Tools/EchoStation/Stage 1/R01/Create Baseline")]
        public static void CreateBaseline()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Create the rendering baseline outside Play Mode.");
            if (QualitySettings.names[QualitySettings.GetQualityLevel()] != "PC")
                throw new InvalidOperationException("Expected the existing PC quality level; refusing to alter another level.");

            Directory.CreateDirectory(EvidencePath);
            var snapshotPath = Path.Combine(EvidencePath, "TemplateSnapshot.json");
            if (!File.Exists(snapshotPath))
            {
                var record = new SetupRecord { capturedUtc = DateTime.UtcNow.ToString("O"),
                    templates = TemplatePaths.Select(p => new TemplateRecord { path = p, sha256 = Hash(p) }).ToArray() };
                File.WriteAllText(snapshotPath, JsonUtility.ToJson(record, true));
                foreach (var path in TemplatePaths.Take(2))
                    File.Copy(path, Path.Combine(EvidencePath, Path.GetFileName(path) + ".before.txt"), false);
            }

            EnsureFolder("Assets/EchoStation/Rendering/Settings");
            EnsureFolder("Assets/EchoStation/Tests/Scenes");
            EnsureFolder("Assets/EchoStation/Tests/Fixtures");
            CopyOnce(TemplatePaths[0], PipelinePath);
            CopyOnce(TemplatePaths[1], RendererPath);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            renderer.renderingMode = RenderingMode.Forward;
            EditorUtility.SetDirty(renderer);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var serialized = new SerializedObject(pipeline);
            serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.FindProperty("m_DefaultRendererIndex").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            pipeline.supportsHDR = true;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.allowHDRDisplaySupport = false;
            PlayerSettings.useHDRDisplay = false;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            AssetDatabase.SaveAssets();

            CreateFixtureSceneOnce();
            Debug.Log("[EchoStation R01] Created Forward/internal-HDR/SDR/D3D11 baseline. Inherited settings are not yet accepted.");
        }

        public static string[] Validate(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer, Camera camera)
        {
            var issues = new System.Collections.Generic.List<string>();
            if (!pipeline || !pipeline.supportsHDR) issues.Add("Pipeline HDR must be enabled.");
            if (!renderer || renderer.renderingMode != RenderingMode.Forward) issues.Add("Renderer must use ordinary Forward.");
            if (!camera || !camera.allowHDR) issues.Add("Camera HDR must be enabled.");
            return issues.ToArray();
        }

        [MenuItem("Tools/EchoStation/Stage 1/R01/Restart D3D11 If Scenes Clean")]
        public static void RestartD3D11IfScenesClean()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Unsaved scene detected; restart cancelled without saving or discarding changes.");
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.scene.isDirty)
                throw new InvalidOperationException("Unsaved prefab stage detected; restart cancelled.");
            File.WriteAllText(Path.Combine(EvidencePath, "EditorRestart.txt"),
                $"{DateTime.UtcNow:O}\nUser-authorized clean-scene restart. Previous API: {SystemInfo.graphicsDeviceType}\n-force-d3d11\n");
            EditorApplication.Exit(0);
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void CopyOnce(string source, string destination)
        {
            if (AssetDatabase.LoadMainAssetAtPath(destination)) return;
            if (!AssetDatabase.CopyAsset(source, destination))
                throw new IOException($"Failed to copy {source} to {destination}");
        }

        private static void CreateFixtureSceneOnce()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) return;
            var shader = Shader.Find("Hidden/EchoStation/Tests/HDRProbe");
            if (!shader) throw new InvalidOperationException("HDR test shader has not been imported.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material)
            {
                material = new Material(shader) { name = "R01_HDRProbe" };
                material.SetColor("_ProbeColor", new Color(4f, 2f, 1f, 1f));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera = new GameObject("R01_ValidationCamera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.aspect = 16f / 9f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 20f;
                var data = camera.GetUniversalAdditionalCameraData();
                data.SetRenderer(0);
                data.allowHDROutput = false;
                data.renderPostProcessing = false;

                var chart = GameObject.CreatePrimitive(PrimitiveType.Quad);
                chart.name = "R01_LinearHDRChart_4_2_1";
                chart.transform.position = new Vector3(0f, 0f, 3f);
                chart.transform.localScale = new Vector3(4f, 2f, 1f);
                chart.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(chart.GetComponent<Collider>());
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (originalScene.IsValid()) SceneManager.SetActiveScene(originalScene);
                EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
