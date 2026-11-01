using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class ExplorationVolumeSetup
    {
        public const string BaseProfilePath = "Assets/EchoStation/Rendering/Settings/ES_Volume_Defaults.asset";
        public const string ExplorationProfilePath = "Assets/EchoStation/Rendering/Settings/ES_Volume_Exploration.asset";
        public const string PrefabPath = "Assets/EchoStation/Rendering/Prefabs/ES_GlobalVolume.prefab";
        public const string ScenePath = "Assets/EchoStation/Tests/Scenes/S1_R02_Volume.unity";
        public const string MaterialPath = "Assets/EchoStation/Tests/Fixtures/R02_VolumeChart.mat";
        public const string EvidencePath = "Docs/Tests/Stage1/Evidence/S1-R02";
        public const string LayerName = "PostProcessing";

        [MenuItem("Tools/EchoStation/Stage 1/R02/Create Volume Template")]
        public static void CreateTemplate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var chartShader = Shader.Find("Hidden/EchoStation/Tests/VolumeChart");
            if (!chartShader || ShaderUtil.ShaderHasError(chartShader))
                throw new InvalidOperationException("Fix the R02 test shader before creating or updating the template.");
            Directory.CreateDirectory(EvidencePath);
            var snapshotPath = Path.Combine(EvidencePath, "ProtectedFiles.json");
            if (!File.Exists(snapshotPath))
            {
                var paths = ForwardHdrBaselineSetup.TemplatePaths.Concat(new[] { ForwardHdrBaselineSetup.ScenePath }).ToArray();
                var record = new ForwardHdrBaselineSetup.SetupRecord { capturedUtc = DateTime.UtcNow.ToString("O"),
                    templates = paths.Select(p => new ForwardHdrBaselineSetup.TemplateRecord { path = p, sha256 = ForwardHdrBaselineSetup.Hash(p) }).ToArray() };
                File.WriteAllText(snapshotPath, JsonUtility.ToJson(record, true));
                File.Copy(ForwardHdrBaselineSetup.PipelinePath, Path.Combine(EvidencePath, "Pipeline.before.txt"));
                File.Copy("ProjectSettings/TagManager.asset", Path.Combine(EvidencePath, "TagManager.before.txt"));
            }
            ForwardHdrBaselineSetup.EnsureFolder("Assets/EchoStation/Rendering/Prefabs");
            var layer = EnsureLayer();
            var defaults = CreateDefaultsOnce();
            var effects = CreateEffectsOnce();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ForwardHdrBaselineSetup.PipelinePath);
            pipeline.colorGradingMode = ColorGradingMode.HighDynamicRange;
            pipeline.volumeProfile = defaults;
            EditorUtility.SetDirty(pipeline);
            VolumeManager.instance.SetQualityDefaultProfile(defaults);
            CreatePrefabOnce(effects, layer);
            CreateSceneOnce(layer);
            AssetDatabase.SaveAssets();
            Debug.Log("[EchoStation R02] Approved Volume template created. ACES / Thin1 0.10,0.80 / CA 0.02.");
        }

        private static int EnsureLayer()
        {
            var existing = LayerMask.NameToLayer(LayerName);
            if (existing >= 0) return existing;
            var data = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = data.FindProperty("layers");
            for (var i = 8; i < layers.arraySize; i++)
            {
                if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
                layers.GetArrayElementAtIndex(i).stringValue = LayerName;
                data.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            throw new InvalidOperationException("No free user Layer for the approved PostProcessing layer.");
        }

        private static VolumeProfile CreateDefaultsOnce()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(BaseProfilePath);
            if (profile) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, BaseProfilePath);
            // Reset post effects only. AO, GI, lights and shadows remain outside R02.
            var types = new[] { typeof(Tonemapping), typeof(FilmGrain), typeof(ChromaticAberration), typeof(Bloom),
                typeof(Vignette), typeof(MotionBlur), typeof(DepthOfField), typeof(LensDistortion), typeof(PaniniProjection),
                typeof(ColorAdjustments), typeof(WhiteBalance), typeof(ColorLookup), typeof(ColorCurves), typeof(ChannelMixer),
                typeof(LiftGammaGain), typeof(ShadowsMidtonesHighlights), typeof(SplitToning), typeof(ScreenSpaceLensFlare) };
            foreach (var type in types)
            {
                var component = profile.Add(type, true);
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            profile.TryGet<Tonemapping>(out var tone);
            tone.SetAllOverridesTo(false);
            tone.mode.Override(TonemappingMode.None);
            profile.TryGet<FilmGrain>(out var grain);
            grain.texture.overrideState = false;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static VolumeProfile CreateEffectsOnce()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ExplorationProfilePath);
            if (profile) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ExplorationProfilePath);
            var tone = profile.Add<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);
            var grain = profile.Add<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.10f);
            grain.response.Override(0.80f);
            var ca = profile.Add<ChromaticAberration>();
            ca.intensity.Override(0.02f);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void CreatePrefabOnce(VolumeProfile effects, int layer)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
            var previous = SceneManager.GetActiveScene();
            var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(temporary);
                var go = new GameObject("ES_GlobalVolume");
                go.layer = layer;
                var volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.weight = 1f;
                volume.priority = 0f;
                volume.sharedProfile = effects;
                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(temporary, true);
            }
        }

        private static void CreateSceneOnce(int layer)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) return;
            var shader = Shader.Find("Hidden/EchoStation/Tests/VolumeChart");
            if (!shader) throw new InvalidOperationException("R02 test chart shader is not imported.");
            var material = new Material(shader) { name = "R02_VolumeChart" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera = new GameObject("R02_ValidationCamera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.orthographicSize = 4.5f;
                camera.aspect = 16f / 9f;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                var data = camera.GetUniversalAdditionalCameraData();
                data.SetRenderer(0);
                data.renderPostProcessing = true;
                data.allowHDROutput = false;
                data.volumeLayerMask = 1 << layer;
                data.volumeTrigger = camera.transform;
                var chart = GameObject.CreatePrimitive(PrimitiveType.Quad);
                chart.name = "R02_HDR_Grayscale_Color_Lines_Glyphs";
                chart.transform.position = new Vector3(0f, 0f, 3f);
                chart.transform.localScale = new Vector3(16f, 9f, 1f);
                chart.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(chart.GetComponent<Collider>());
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
