using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EchoStation.Tests.Fixtures
{
    // Validation fixture only: no game camera or saved profile is modified.
    public sealed class R02ImageProbe : IDisposable
    {
        [Serializable] public class Result
        {
            public bool passed, hdr, displayHdrActive, cameraHdrOutputAllowed, effectiveStackPassed;
            public string utc, unity, os, cpu, gpu, api, pipeline, targetFormat, error;
            public int ramMb, vramMb, width = 1920, height = 1080;
            public float grainIntensity, grainResponse, caIntensity, bloom, exposure, whiteBalance;
            public string tonemapping;
            public float noToneBright, acesBright, toneMeanDifference, caMeanDifference, foreignVolumeDifference;
            public GrainSample[] grainSamples;
            public TimingSample[] submitAndReadback;
            public bool frameTimingEnabled;
            public string timingNote = "Explicit render-request CPU submission and synchronous GPU readback; neither is GPU frame time. PNG encoding excluded. Diagnostic fixture only, no target-hardware FPS acceptance.";
        }
        [Serializable] public class GrainSample
        {
            public int frame;
            public float response0BrightDifference, response08BrightDifference, response08MidDifference;
        }
        [Serializable] public class TimingSample
        {
            public bool effects;
            public double submitMs, readbackMs;
        }

        private readonly Camera camera;
        private readonly Volume volume;
        private readonly VolumeProfile profile;
        private readonly RenderTexture target;
        private readonly Texture2D pixels;
        private readonly Tonemapping tone;
        private readonly FilmGrain grain;
        private readonly ChromaticAberration ca;
        private readonly bool originalPost;
        private readonly VolumeStack stack;
        public Result Report { get; } = new Result();

        public R02ImageProbe(Camera camera, Volume volume)
        {
            this.camera = camera;
            this.volume = volume;
            var data = camera.GetUniversalAdditionalCameraData();
            originalPost = data.renderPostProcessing;
            // Volume.profile clones all components; sharedProfile stays untouched.
            profile = volume.profile;
            profile.TryGet(out tone); profile.TryGet(out grain); profile.TryGet(out ca);
            target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            target.Create();
            pixels = new Texture2D(1920, 1080, TextureFormat.RGBAFloat, false, true);
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            stack = VolumeManager.instance.CreateStack();
        }

        private void Mode(TonemappingMode mode, float noise, float response, float aberration, bool post = true)
        {
            tone.mode.value = mode; grain.intensity.value = noise; grain.response.value = response; ca.intensity.value = aberration;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = post;
        }

        private Color[] Capture(out TimingSample timing)
        {
            timing = new TimingSample { effects = camera.GetUniversalAdditionalCameraData().renderPostProcessing };
            var watch = Stopwatch.StartNew();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            timing.submitMs = watch.Elapsed.TotalMilliseconds;
            var previous = RenderTexture.active;
            try
            {
                watch.Restart();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0, false);
                pixels.Apply(false);
                timing.readbackMs = watch.Elapsed.TotalMilliseconds;
                return pixels.GetPixels();
            }
            finally { RenderTexture.active = previous; }
        }

        // Data is linear SDR after tonemapping. PNG uses one explicit sRGB encoding.
        private static void Save(Color[] colors, string path)
        {
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false, true);
            try
            {
                var encoded = new Color32[colors.Length];
                for (var i = 0; i < colors.Length; i++) encoded[i] = colors[i].gamma;
                texture.SetPixels32(encoded); texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { Destroy(texture); }
        }

        private static float Difference(Color[] a, Color[] b, int x0, int y0, int x1, int y1)
        {
            double sum = 0;
            for (var y = y0; y < y1; y++) for (var x = x0; x < x1; x++)
            {
                var i = y * 1920 + x;
                sum += (Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b)) / 3;
            }
            return (float)(sum / ((x1 - x0) * (y1 - y0)));
        }

        public IEnumerator Run(string directory)
        {
            Directory.CreateDirectory(directory);
            var r = Report;
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            r.utc = DateTime.UtcNow.ToString("O"); r.unity = Application.unityVersion; r.os = SystemInfo.operatingSystem;
            r.cpu = SystemInfo.processorType; r.gpu = SystemInfo.graphicsDeviceName; r.api = SystemInfo.graphicsDeviceType.ToString();
            r.ramMb = SystemInfo.systemMemorySize; r.vramMb = SystemInfo.graphicsMemorySize; r.pipeline = pipeline.name;
            r.hdr = pipeline.supportsHDR && camera.allowHDR; r.targetFormat = target.graphicsFormat.ToString();
            r.displayHdrActive = HDROutputSettings.main != null && HDROutputSettings.main.active;
            r.cameraHdrOutputAllowed = camera.GetUniversalAdditionalCameraData().allowHDROutput;
            r.frameTimingEnabled = FrameTimingManager.IsFeatureEnabled();
            Mode(TonemappingMode.None, 0, 0.8f, 0);
            var noTone = Capture(out _);
            Save(noTone, Path.Combine(directory, "01_NoTonemapping.png"));
            Mode(TonemappingMode.ACES, 0, 0.8f, 0);
            var aces = Capture(out _);
            Save(aces, Path.Combine(directory, "02_ACES_Only.png"));
            r.noToneBright = noTone[900 * 1920 + 1200].r;
            r.acesBright = aces[900 * 1920 + 1200].r;
            r.toneMeanDifference = Difference(noTone, aces, 200, 800, 1800, 1000);
            noTone = null;
            yield return null;
            var samples = new List<GrainSample>();
            for (var i = 0; i < 8; i++)
            {
                // Same-frame pair shares URP's frameCount-seeded grain offset.
                Mode(TonemappingMode.ACES, 0.1f, 0, 0);
                var zero = Capture(out _);
                Mode(TonemappingMode.ACES, 0.1f, 0.8f, 0);
                var approved = Capture(out _);
                samples.Add(new GrainSample { frame = Time.frameCount,
                    response0BrightDifference = Difference(zero, aces, 1000, 800, 1500, 1000),
                    response08BrightDifference = Difference(approved, aces, 1000, 800, 1500, 1000),
                    response08MidDifference = Difference(approved, aces, 650, 450, 1250, 650) });
                if (i == 0) Save(approved, Path.Combine(directory, "03_ACES_Grain.png"));
                // The Editor test runner may advance its enumerator while the
                // game loop is idle. Require a real frame, never duplicate a seed.
                var sampledFrame = Time.frameCount;
                var frameWait = Stopwatch.StartNew();
                while (Time.frameCount == sampledFrame)
                {
                    if (frameWait.Elapsed.TotalSeconds > 5) throw new TimeoutException("R02 game frame did not advance; check focus/background execution.");
                    yield return null;
                }
            }
            r.grainSamples = samples.ToArray();
            Mode(TonemappingMode.ACES, 0, 0.8f, 0.02f);
            var aberration = Capture(out _);
            r.caMeanDifference = Difference(aberration, aces, 240, 400, 560, 700);
            Save(aberration, Path.Combine(directory, "04_ACES_CA.png"));
            Mode(TonemappingMode.ACES, 0.1f, 0.8f, 0.02f);
            Save(Capture(out _), Path.Combine(directory, "05_Approved.png"));
            VolumeManager.instance.Update(stack, camera.transform, camera.GetUniversalAdditionalCameraData().volumeLayerMask);
            r.tonemapping = stack.GetComponent<Tonemapping>().mode.value.ToString();
            r.grainIntensity = stack.GetComponent<FilmGrain>().intensity.value;
            r.grainResponse = stack.GetComponent<FilmGrain>().response.value;
            r.caIntensity = stack.GetComponent<ChromaticAberration>().intensity.value;
            r.bloom = stack.GetComponent<Bloom>().intensity.value;
            r.exposure = stack.GetComponent<ColorAdjustments>().postExposure.value;
            r.whiteBalance = stack.GetComponent<WhiteBalance>().temperature.value;
            r.effectiveStackPassed = r.tonemapping == "ACES" && r.grainIntensity == .1f && r.grainResponse == .8f &&
                r.caIntensity == .02f && r.bloom == 0 && r.exposure == 0 && r.whiteBalance == 0;

            // Actual-image isolation check; no grain so random frames cannot mask leakage.
            Mode(TonemappingMode.ACES, 0, .8f, 0);
            var foreignGo = new GameObject("R02_ForeignLayerVolume");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(foreignGo, camera.gameObject.scene);
            var foreignProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            try
            {
                var foreign = foreignGo.AddComponent<Volume>(); foreign.isGlobal = true; foreign.priority = 1000;
                foreignProfile.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
                foreignProfile.Add<Bloom>().intensity.Override(10);
                foreign.sharedProfile = foreignProfile;
                r.foreignVolumeDifference = Difference(Capture(out _), aces, 0, 0, 1920, 1080);
            }
            finally
            {
                Destroy(foreignGo);
                foreach (var component in foreignProfile.components) Destroy(component);
                Destroy(foreignProfile);
            }
            aces = null;
            var timing = new List<TimingSample>();
            for (var i = 0; i < 20; i++)
            {
                var enabled = i % 2 != 0;
                Mode(TonemappingMode.ACES, .1f, .8f, .02f, enabled);
                Capture(out var sample);
                if (i >= 4) timing.Add(sample); // Four alternating warm-up requests excluded.
                yield return null;
            }
            r.submitAndReadback = timing.ToArray();
            r.passed = r.unity == "6000.6.7f1" && r.api == "Direct3D11" && r.hdr && !r.displayHdrActive &&
                !r.cameraHdrOutputAllowed && r.effectiveStackPassed && r.toneMeanDifference > .01f &&
                r.noToneBright > r.acesBright && r.caMeanDifference > .00001f && r.foreignVolumeDifference < .000001f &&
                samples.Select(s => s.frame).Distinct().Count() == 8 && samples.All(s => s.response08MidDifference > .00001f &&
                    s.response08BrightDifference < s.response0BrightDifference);
            Mode(TonemappingMode.ACES, .1f, .8f, .02f);
        }

        public void Dispose()
        {
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = originalPost;
            volume.profile = null;
            foreach (var component in profile.components) Destroy(component);
            Destroy(profile); target.Release(); Destroy(target); Destroy(pixels);
            VolumeManager.instance.DestroyStack(stack);
        }

        private static void Destroy(Object value)
        {
            if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
