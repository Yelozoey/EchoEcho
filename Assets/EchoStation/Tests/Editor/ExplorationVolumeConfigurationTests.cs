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
    public class ExplorationVolumeConfigurationTests
    {
        private Scene preview;
        private Camera camera;
        private Volume volume;
        private VolumeStack stack;

        [SetUp]
        public void OpenIsolatedScene()
        {
            preview = EditorSceneManager.OpenPreviewScene(ExplorationVolumeSetup.ScenePath);
            camera = preview.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>()).Single();
            camera.enabled = false;
            camera.scene = preview;
            volume = preview.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Volume>()).Single();
            // EditMode may start without any Scene/Game view rendering. Let URP
            // initialize its own VolumeManager through a real render request.
            var warmup = new RenderTexture(16, 16, 24);
            try
            {
                warmup.Create();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = warmup });
            }
            finally { warmup.Release(); Object.DestroyImmediate(warmup); }
            stack = VolumeManager.instance.CreateStack();
        }

        [TearDown]
        public void CloseIsolatedScene()
        {
            if (stack != null) VolumeManager.instance.DestroyStack(stack);
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
        }

        private void UpdateStack() => VolumeManager.instance.Update(stack, camera.transform, camera.GetUniversalAdditionalCameraData().volumeLayerMask);

        [Test]
        public void T01_ApprovedParametersAndOverrideStates()
        {
            var profile = volume.sharedProfile;
            Assert.That(profile.components.Count, Is.EqualTo(3));
            Assert.That(profile.TryGet<Tonemapping>(out var tone), Is.True);
            Assert.That(tone.mode.value, Is.EqualTo(TonemappingMode.ACES));
            Assert.That(tone.mode.overrideState, Is.True);
            Assert.That(profile.TryGet<FilmGrain>(out var grain), Is.True);
            Assert.That(grain.type.value, Is.EqualTo(FilmGrainLookup.Thin1));
            Assert.That(grain.intensity.value, Is.EqualTo(0.10f));
            Assert.That(grain.response.value, Is.EqualTo(0.80f));
            Assert.That(grain.type.overrideState && grain.intensity.overrideState && grain.response.overrideState, Is.True);
            Assert.That(grain.texture.overrideState, Is.False);
            Assert.That(profile.TryGet<ChromaticAberration>(out var ca), Is.True);
            Assert.That(ca.intensity.value, Is.EqualTo(0.02f));
            Assert.That(ca.intensity.overrideState, Is.True);
            Assert.That(ShaderUtil.ShaderHasError(Shader.Find("Hidden/EchoStation/Tests/VolumeChart")), Is.False);
        }

        [Test]
        public void T02_PipelineCameraAndPrefabAreBound()
        {
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            Assert.That(pipeline.colorGradingMode, Is.EqualTo(ColorGradingMode.HighDynamicRange));
            Assert.That(pipeline.colorGradingLutSize, Is.EqualTo(32));
            Assert.That(AssetDatabase.GetAssetPath(pipeline.volumeProfile), Is.EqualTo(ExplorationVolumeSetup.BaseProfilePath));
            var data = camera.GetUniversalAdditionalCameraData();
            Assert.That(data.renderPostProcessing, Is.True);
            Assert.That(data.allowHDROutput, Is.False);
            Assert.That(data.volumeLayerMask.value, Is.EqualTo(1 << LayerMask.NameToLayer("PostProcessing")));
            Assert.That(volume.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("PostProcessing")));
            Assert.That(volume.isGlobal, Is.True);
            Assert.That(volume.weight, Is.EqualTo(1f));
            Assert.That(volume.priority, Is.EqualTo(0f));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(volume.gameObject), Is.Not.Null);
        }

        [Test]
        public void T03_EngineStackUsesTheApprovedEffectsAndNeutralBackground()
        {
            UpdateStack();
            Assert.That(stack.GetComponent<Tonemapping>().mode.value, Is.EqualTo(TonemappingMode.ACES));
            Assert.That(stack.GetComponent<FilmGrain>().intensity.value, Is.EqualTo(0.10f));
            Assert.That(stack.GetComponent<FilmGrain>().response.value, Is.EqualTo(0.80f));
            Assert.That(stack.GetComponent<ChromaticAberration>().intensity.value, Is.EqualTo(0.02f));
            AssertNeutralUnrelatedEffects();
        }

        [Test]
        public void T04_ZeroWeightRestoresNeutralDefaults()
        {
            volume.weight = 0f;
            UpdateStack();
            Assert.That(stack.GetComponent<Tonemapping>().mode.value, Is.EqualTo(TonemappingMode.None));
            Assert.That(stack.GetComponent<FilmGrain>().intensity.value, Is.Zero);
            Assert.That(stack.GetComponent<ChromaticAberration>().intensity.value, Is.Zero);
            AssertNeutralUnrelatedEffects();
        }

        private void AssertNeutralUnrelatedEffects()
        {
            Assert.That(stack.GetComponent<Bloom>().intensity.value, Is.Zero);
            Assert.That(stack.GetComponent<Vignette>().intensity.value, Is.Zero);
            Assert.That(stack.GetComponent<MotionBlur>().intensity.value, Is.Zero);
            Assert.That(stack.GetComponent<DepthOfField>().mode.value, Is.EqualTo(DepthOfFieldMode.Off));
            Assert.That(stack.GetComponent<LensDistortion>().intensity.value, Is.Zero);
            Assert.That(stack.GetComponent<PaniniProjection>().distance.value, Is.Zero);
            var color = stack.GetComponent<ColorAdjustments>();
            Assert.That(color.postExposure.value, Is.Zero);
            Assert.That(color.contrast.value, Is.Zero);
            Assert.That(color.saturation.value, Is.Zero);
            Assert.That(color.colorFilter.value, Is.EqualTo(Color.white));
            Assert.That(stack.GetComponent<WhiteBalance>().temperature.value, Is.Zero);
            Assert.That(stack.GetComponent<WhiteBalance>().tint.value, Is.Zero);
        }

        [Test]
        public void T05_ForeignLayerVolumeCannotAffectTheStack()
        {
            var go = new GameObject("ForeignVolume_TestOnly");
            SceneManager.MoveGameObjectToScene(go, preview);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            try
            {
                var foreign = go.AddComponent<Volume>();
                foreign.isGlobal = true;
                foreign.priority = 1000;
                profile.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
                profile.Add<Bloom>().intensity.Override(10f);
                foreign.sharedProfile = profile;
                UpdateStack();
                Assert.That(stack.GetComponent<Tonemapping>().mode.value, Is.EqualTo(TonemappingMode.ACES));
                Assert.That(stack.GetComponent<Bloom>().intensity.value, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(go);
                foreach (var component in profile.components) Object.DestroyImmediate(component);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void T06_ProtectedAssetsAndR01SceneAreUnchanged()
        {
            var record = JsonUtility.FromJson<ForwardHdrBaselineSetup.SetupRecord>(File.ReadAllText(
                Path.Combine(ExplorationVolumeSetup.EvidencePath, "ProtectedFiles.json")));
            foreach (var item in record.templates)
                Assert.That(ForwardHdrBaselineSetup.Hash(item.path), Is.EqualTo(item.sha256), item.path);
        }

        [Test]
        public void T07_RepeatSetupKeepsGuidsAndOriginalSceneDirtyState()
        {
            var paths = new[] { ExplorationVolumeSetup.BaseProfilePath, ExplorationVolumeSetup.ExplorationProfilePath,
                ExplorationVolumeSetup.PrefabPath, ExplorationVolumeSetup.ScenePath };
            var guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            var active = SceneManager.GetActiveScene();
            var wasDirty = active.isDirty;
            ExplorationVolumeSetup.CreateTemplate();
            Assert.That(paths.Select(AssetDatabase.AssetPathToGUID).ToArray(), Is.EqualTo(guids));
            Assert.That(active.isDirty, Is.EqualTo(wasDirty));
        }
    }
}
