using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Tests.Rendering
{
    public class ForwardHdrBaselineTests
    {
        private Scene preview;
        private Camera camera;
        private UniversalRenderPipelineAsset pipeline;
        private UniversalRendererData renderer;

        [SetUp]
        public void LoadIsolatedFixture()
        {
            pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ForwardHdrBaselineSetup.PipelinePath);
            renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath);
            preview = EditorSceneManager.OpenPreviewScene(ForwardHdrBaselineSetup.ScenePath);
            camera = preview.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>()).Single();
            camera.scene = preview;
            camera.enabled = false;
        }

        [TearDown]
        public void CloseIsolatedFixture()
        {
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
        }

        [Test]
        public void T01_ExactEngineAndRequiredAssetsExist()
        {
            Assert.That(Application.unityVersion, Is.EqualTo("6000.6.7f1"));
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(Shader.Find("Hidden/EchoStation/Tests/HDRProbe").isSupported, Is.True);
        }

        [Test]
        public void T02_ActualPipelineAndCameraRendererResolveToProjectAssets()
        {
            var active=QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            var gameplay=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ScanWaveSetup.PipelinePath);
            Assert.That(active==pipeline||active==gameplay,Is.True,"Only verified project pipelines may bind after P09 integration.");
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(active));
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(active));
            Assert.That(QualitySettings.names[QualitySettings.GetQualityLevel()], Is.EqualTo("PC"));
            var serialized = new SerializedObject(pipeline);
            Assert.That(serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(renderer));
            Assert.That(camera.GetUniversalAdditionalCameraData().scriptableRenderer, Is.SameAs(active.GetRenderer(0)));
        }

        [Test]
        public void T03_OrdinaryForwardAndBothHdrSwitchesAreEnabled()
        {
            Assert.That(renderer.renderingMode, Is.EqualTo(RenderingMode.Forward));
            Assert.That(ForwardHdrBaselineSetup.Validate(pipeline, renderer, camera), Is.Empty);
        }

        [Test]
        public void T04_InternalHdrIsIndependentOfDisabledDisplayHdr()
        {
            Assert.That(pipeline.supportsHDR && camera.allowHDR, Is.True);
            Assert.That(PlayerSettings.allowHDRDisplaySupport, Is.False);
            Assert.That(PlayerSettings.useHDRDisplay, Is.False);
            Assert.That(camera.GetUniversalAdditionalCameraData().allowHDROutput, Is.False);
        }

        [Test]
        public void T05_Windows64D3D11And1080pAreTheBaseline()
        {
            Assert.That(EditorUserBuildSettings.activeBuildTarget, Is.EqualTo(BuildTarget.StandaloneWindows64));
            Assert.That(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64), Is.False);
            Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64), Is.EqualTo(new[] { GraphicsDeviceType.Direct3D11 }));
            Assert.That(SystemInfo.graphicsDeviceType, Is.EqualTo(GraphicsDeviceType.Direct3D11));
            Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(1920));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(1080));
        }

        [TestCase("forward_plus")]
        [TestCase("pipeline_hdr_off")]
        [TestCase("camera_hdr_off")]
        public void T06_ValidatorRejectsInvalidConfigurationWithoutEditingProjectAssets(string fault)
        {
            var pipelineCopy = UnityEngine.Object.Instantiate(pipeline);
            var rendererCopy = UnityEngine.Object.Instantiate(renderer);
            try
            {
                if (fault == "forward_plus") rendererCopy.renderingMode = RenderingMode.ForwardPlus;
                if (fault == "pipeline_hdr_off") pipelineCopy.supportsHDR = false;
                if (fault == "camera_hdr_off") camera.allowHDR = false;
                Assert.That(ForwardHdrBaselineSetup.Validate(pipelineCopy, rendererCopy, camera), Has.Length.EqualTo(1));
                Assert.That(ForwardHdrBaselineSetup.Validate(pipeline, renderer, LoadFreshCameraHdr()), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pipelineCopy);
                UnityEngine.Object.DestroyImmediate(rendererCopy);
            }
        }

        private Camera LoadFreshCameraHdr() { camera.allowHDR = true; return camera; }

        [Test]
        public void T07_OriginalTemplateFilesAreUnchanged()
        {
            var record = JsonUtility.FromJson<ForwardHdrBaselineSetup.SetupRecord>(File.ReadAllText(
                Path.Combine(ForwardHdrBaselineSetup.EvidencePath, "TemplateSnapshot.json")));
            foreach (var item in record.templates)
                Assert.That(ForwardHdrBaselineSetup.Hash(item.path), Is.EqualTo(item.sha256), item.path);
        }

        [Test]
        public void T08_OnlyApprovedEffectiveSettingsDifferFromTheTemplates()
        {
            AssertMatchingProperties(AssetDatabase.LoadMainAssetAtPath(ForwardHdrBaselineSetup.TemplatePaths[0]), pipeline,
                "m_Name", "m_RendererDataList", "m_ColorGradingMode", "m_VolumeProfile",
                "m_ShadowDistance", "m_Cascade4Split", "m_CascadeBorder");
            // v0.3: R04's three inputs have explicit assertions rather than blanket exclusions.
            Assert.That(pipeline.shadowDistance, Is.EqualTo(40));
            Assert.That(pipeline.cascade4Split, Is.EqualTo(new Vector3(.1f, .25f, .5f)));
            Assert.That(pipeline.cascadeBorder, Is.EqualTo(.1f));
            Assert.That(pipeline.colorGradingMode, Is.EqualTo(ColorGradingMode.HighDynamicRange));
            Assert.That(AssetDatabase.GetAssetPath(pipeline.volumeProfile), Is.EqualTo(ExplorationVolumeSetup.BaseProfilePath));
            var originalRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.TemplatePaths[1]);
            AssertMatchingProperties(originalRenderer, renderer,
                "m_Name", "m_RenderingMode", "m_RendererFeatures", "m_RendererFeatureMap");
            Assert.That(renderer.rendererFeatures.Count, Is.EqualTo(originalRenderer.rendererFeatures.Count));
            for (var i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                Assert.That(renderer.rendererFeatures[i].GetType(), Is.EqualTo(originalRenderer.rendererFeatures[i].GetType()));
                var originalSettings = new SerializedObject(originalRenderer.rendererFeatures[i]).FindProperty("m_Settings");
                var copiedSettings = new SerializedObject(renderer.rendererFeatures[i]).FindProperty("m_Settings");
                Assert.That(SerializedProperty.DataEquals(originalSettings, copiedSettings), Is.True, "Inherited renderer feature settings changed.");
            }
        }

        private static void AssertMatchingProperties(UnityEngine.Object original, UnityEngine.Object copy, params string[] exclusions)
        {
            var originalData = new SerializedObject(original);
            var copiedData = new SerializedObject(copy);
            var iterator = originalData.GetIterator();
            while (iterator.Next(true))
            {
                var path = iterator.propertyPath;
                // URP's ShaderBuildPreprocessor rewrites these private, derived
                // variant-filter fields before each build. They are recorded in
                // the inventory, not treated as user-authored rendering inputs.
                if (path.StartsWith("m_Prefilter", StringComparison.Ordinal) ||
                    path == "m_Light2DPrefilteringMode" || path.StartsWith("m_Light2DKeptVariantCombos", StringComparison.Ordinal)) continue;
                if (iterator.propertyType == SerializedPropertyType.Generic ||
                    exclusions.Any(e => path == e || path.StartsWith(e + ".", StringComparison.Ordinal))) continue;
                var counterpart = copiedData.FindProperty(path);
                Assert.That(counterpart, Is.Not.Null, path);
                Assert.That(SerializedProperty.DataEquals(iterator, counterpart), Is.True, path);
            }
        }

        [Test]
        public void T11_InheritedRenderingParameterDriftIsStillDetected()
        {
            var changed = UnityEngine.Object.Instantiate(pipeline);
            try
            {
                changed.renderScale = 0.5f;
                var error = Assert.Throws<AssertionException>(() => AssertMatchingProperties(pipeline, changed, "m_Name"));
                StringAssert.Contains("m_RenderScale", error.Message);
            }
            finally { UnityEngine.Object.DestroyImmediate(changed); }
        }

        [Test]
        public void T09_SetupCanBeRepeatedWithoutDuplicateAssets()
        {
            var paths = new[] { ForwardHdrBaselineSetup.PipelinePath, ForwardHdrBaselineSetup.RendererPath,
                ForwardHdrBaselineSetup.ScenePath, ForwardHdrBaselineSetup.MaterialPath };
            var before = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            var oldDefaultPath=AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
            var oldQualityPath=AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);
            Assert.That(oldDefaultPath,Is.Not.Empty);Assert.That(oldQualityPath,Is.Not.Empty);
            try
            {
                ForwardHdrBaselineSetup.CreateBaseline();
                Assert.That(paths.Select(AssetDatabase.AssetPathToGUID).ToArray(), Is.EqualTo(before));
            }
            finally
            {
                var restoreDefault=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldDefaultPath);
                var restoreQuality=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldQualityPath);
                Assert.That(restoreDefault,Is.Not.Null);Assert.That(restoreQuality,Is.Not.Null);
                GraphicsSettings.defaultRenderPipeline=restoreDefault;QualitySettings.renderPipeline=restoreQuality;AssetDatabase.SaveAssets();
            }
        }

        [Test]
        public void T10_ActualUrpRenderPreservesKnownHdrValues()
        {
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var pixel = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var oldActive = RenderTexture.active;
            try
            {
                target.Create();
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                Assert.That(RenderPipeline.SupportsRenderRequest(camera, request), Is.True);
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                pixel.ReadPixels(new Rect(960, 540, 1, 1), 0, 0);
                pixel.Apply();
                var value = pixel.GetPixel(0, 0);
                File.WriteAllText(Path.Combine(ForwardHdrBaselineSetup.EvidencePath, "HdrPixel.json"),
                    $"{{\"width\":1920,\"height\":1080,\"r\":{value.r.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"g\":{value.g.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"b\":{value.b.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"graphicsFormat\":\"{target.graphicsFormat}\"}}");
                Assert.That(value.r, Is.EqualTo(4f).Within(0.02f));
                Assert.That(value.g, Is.EqualTo(2f).Within(0.02f));
                Assert.That(value.b, Is.EqualTo(1f).Within(0.02f));
                Assert.That(value.r, Is.GreaterThan(1f));
            }
            finally
            {
                RenderTexture.active = oldActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixel);
            }
        }
    }
}
