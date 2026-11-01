using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EchoStation.Rendering.Editor
{
    public static class BakedLightingSetup
    {
        public const string ScenePath = "Assets/EchoStation/Tests/Scenes/S1_R03_BakedLighting.unity";
        public const string SettingsPath = "Assets/EchoStation/Rendering/Lighting/ES_Interior_Prototype.asset";
        public const string MaterialPath = "Assets/EchoStation/Tests/Fixtures/R03_LitGray.mat";
        public const string ProbeMaterialPath = "Assets/EchoStation/Tests/Fixtures/R03_ProbeDiffuse.mat";
        public const string MeshFolder = "Assets/EchoStation/Tests/Fixtures/R03Meshes";
        public const string EvidencePath = "Docs/Tests/Stage1/Evidence/S1-R03";

        [MenuItem("Tools/EchoStation/Stage 1/R03/Create Lighting Template")]
        public static void CreateTemplate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Lightmapping.isRunning)
                throw new InvalidOperationException("Stop Play Mode and other baking jobs first.");
            Directory.CreateDirectory(EvidencePath);
            var snapshot = Path.Combine(EvidencePath, "ProtectedFiles.json");
            if (!File.Exists(snapshot))
            {
                var paths = ForwardHdrBaselineSetup.TemplatePaths.Concat(new[] { ForwardHdrBaselineSetup.ScenePath,
                    ExplorationVolumeSetup.ScenePath, ExplorationVolumeSetup.BaseProfilePath, ExplorationVolumeSetup.ExplorationProfilePath,
                    ExplorationVolumeSetup.PrefabPath, ForwardHdrBaselineSetup.RendererPath }).ToArray();
                var record = new ForwardHdrBaselineSetup.SetupRecord { capturedUtc = DateTime.UtcNow.ToString("O"),
                    templates = paths.Select(p => new ForwardHdrBaselineSetup.TemplateRecord { path = p, sha256 = ForwardHdrBaselineSetup.Hash(p) }).ToArray() };
                File.WriteAllText(snapshot, JsonUtility.ToJson(record, true));
                File.Copy(ForwardHdrBaselineSetup.PipelinePath, Path.Combine(EvidencePath, "Pipeline.before.txt"));
            }
            ForwardHdrBaselineSetup.EnsureFolder("Assets/EchoStation/Rendering/Lighting");
            ForwardHdrBaselineSetup.EnsureFolder(MeshFolder);
            var settings = CreateSettingsOnce();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("URP Lit shader is not available.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material)
            {
                material = new Material(shader) { name = "R03_LitGray" };
                material.SetColor("_BaseColor", new Color(.55f, .55f, .55f, 1));
                material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", .15f);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                Lightmapping.lightingSettings = settings;
                RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black; RenderSettings.ambientIntensity = 0;
                RenderSettings.ambientProbe = new SphericalHarmonicsL2();
                RenderSettings.reflectionIntensity = 0; RenderSettings.fog = false;
                var geometry = new GameObject("R03_StaticRoom");
                Box("Floor", new Vector3(0, -.125f, 0), new Vector3(8.5f, .25f, 6.5f), geometry.transform, material);
                Box("Ceiling", new Vector3(0, 4.125f, 0), new Vector3(8.5f, .25f, 6.5f), geometry.transform, material);
                Box("WallLeft", new Vector3(-4.125f, 2, 0), new Vector3(.25f, 4, 6.5f), geometry.transform, material);
                Box("WallRight", new Vector3(4.125f, 2, 0), new Vector3(.25f, 4, 6.5f), geometry.transform, material);
                Box("WallBack", new Vector3(0, 2, 3.125f), new Vector3(8, 4, .25f), geometry.transform, material);
                Box("WallFront", new Vector3(0, 2, -3.125f), new Vector3(8, 4, .25f), geometry.transform, material);
                Box("Divider", new Vector3(0, 1.6f, 1.7f), new Vector3(.25f, 3.2f, 2.6f), geometry.transform, material);
                Box("Occluder", new Vector3(-2, .45f, .7f), new Vector3(1.2f, .9f, 1.2f), geometry.transform, material);
                AddBakedLight("R03_Cool_Baked", new Vector3(-2.1f, 3.2f, .3f), new Color(.55f, .75f, 1f));
                AddBakedLight("R03_Warm_Baked", new Vector3(2.1f, 3.2f, .3f), new Color(1f, .48f, .2f));
                var group = new GameObject("R03_LightProbes", typeof(LightProbeGroup)).GetComponent<LightProbeGroup>();
                var bounds = geometry.GetComponentsInChildren<Renderer>().Select(r => r.bounds).ToArray();
                var positions = new List<Vector3>();
                foreach (var y in new[] { .35f, 1.1f, 2.1f, 3.4f })
                    for (var ix = 0; ix < 5; ix++) for (var iz = 0; iz < 5; iz++)
                    {
                        var p = new Vector3(-3.25f + ix * 1.625f, y, -2.5f + iz * 1.25f);
                        if (!bounds.Any(b => b.Contains(p))) positions.Add(p);
                    }
                group.probePositions = positions.ToArray();
                var dynamic = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                dynamic.name = "R03_DynamicProxy"; dynamic.transform.position = new Vector3(-2.4f, .9f, -.8f);
                dynamic.transform.localScale = Vector3.one * .6f;
                Object.DestroyImmediate(dynamic.GetComponent<Collider>());
                var dynamicRenderer = dynamic.GetComponent<MeshRenderer>();
                dynamicRenderer.sharedMaterial = CreateProbeMaterial(); dynamicRenderer.receiveGI = ReceiveGI.LightProbes;
                dynamicRenderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                dynamicRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                dynamicRenderer.probeAnchor = dynamic.transform;
                GameObjectUtility.SetStaticEditorFlags(dynamic, 0);
                var camera = new GameObject("R03_ValidationCamera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 1.8f, -2.7f);
                camera.transform.LookAt(new Vector3(0, 1.2f, 1.8f)); camera.fieldOfView = 75; camera.nearClipPlane = .05f;
                camera.aspect = 16f / 9f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.allowHDR = true; camera.allowMSAA = false;
                var data = camera.GetUniversalAdditionalCameraData(); data.SetRenderer(0);
                data.renderPostProcessing = true; data.allowHDROutput = false;
                data.volumeLayerMask = 1 << LayerMask.NameToLayer(ExplorationVolumeSetup.LayerName); data.volumeTrigger = camera.transform;
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ExplorationVolumeSetup.PrefabPath), scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[EchoStation R03-A] Template created: 8 static meshes, {positions.Count} probes. Not baked yet.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static LightingSettings CreateSettingsOnce()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (settings) return settings;
            settings = new LightingSettings { name = "ES_Interior_Prototype", lightmapper = LightingSettings.Lightmapper.ProgressiveCPU,
                bakedGI = true, realtimeGI = false, autoGenerate = false, lightmapResolution = 20, lightmapMaxSize = 1024,
                lightmapPadding = 4, directionalityMode = LightmapsMode.CombinedDirectional, lightmapCompression = LightmapCompression.HighQuality,
                directSampleCount = 64, indirectSampleCount = 256, environmentSampleCount = 64,
                minBounces = 2, maxBounces = 2, indirectScale = 1, albedoBoost = 1, lightProbeSampleCountMultiplier = 2,
                ao = false, filteringMode = LightingSettings.FilterMode.Advanced,
                denoiserTypeDirect = LightingSettings.DenoiserType.None, denoiserTypeIndirect = LightingSettings.DenoiserType.None,
                filterTypeDirect = LightingSettings.FilterType.Gaussian, filterTypeIndirect = LightingSettings.FilterType.Gaussian,
                filteringGaussianRadiusDirect = 1, filteringGaussianRadiusIndirect = 2, prioritizeView = false };
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static Material CreateProbeMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ProbeMaterialPath);
            if (material) return material;
            material = new Material(AssetDatabase.LoadAssetAtPath<Material>(MaterialPath)) { name = "R03_ProbeDiffuse" };
            // Isolate diffuse probe irradiance from the default reflection cube.
            material.SetFloat("_EnvironmentReflections", 0);
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            AssetDatabase.CreateAsset(material, ProbeMaterialPath);
            return material;
        }

        [MenuItem("Tools/EchoStation/Stage 1/R03/Repair Probe Fixture Reflection Isolation")]
        public static void RepairProbeMaterial()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Lightmapping.isRunning) throw new InvalidOperationException("Exit Play Mode/baking first.");
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath); var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("R03 scene has unsaved changes; refusing to save them.");
                scene.GetRootGameObjects().Single(o => o.name == "R03_DynamicProxy").GetComponent<MeshRenderer>().sharedMaterial = CreateProbeMaterial();
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Box(string name, Vector3 center, Vector3 size, Transform parent, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent); go.transform.position = center;
            go.GetComponent<BoxCollider>().size = size;
            var path = $"{MeshFolder}/R03_{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh)
            {
                mesh = Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh); mesh.name = $"R03_{name}";
                mesh.vertices = mesh.vertices.Select(v => Vector3.Scale(v, size)).ToArray(); mesh.RecalculateBounds();
                UnwrapParam.SetDefaults(out var unwrap); unwrap.packMargin = 4f / 1024;
                if (!Unwrapping.GenerateSecondaryUVSet(mesh, unwrap)) throw new InvalidOperationException($"UV2 generation failed: {name}");
                AssetDatabase.CreateAsset(mesh, path);
            }
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.receiveGI = ReceiveGI.Lightmaps; renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);
        }

        private static void AddBakedLight(string name, Vector3 position, Color color)
        {
            var light = new GameObject(name, typeof(Light)).GetComponent<Light>(); light.transform.position = position;
            light.type = LightType.Point; light.lightmapBakeType = LightmapBakeType.Baked;
            light.color = color; light.intensity = 3; light.range = 8; light.bounceIntensity = 1; light.shadows = LightShadows.Soft;
        }
    }
}
