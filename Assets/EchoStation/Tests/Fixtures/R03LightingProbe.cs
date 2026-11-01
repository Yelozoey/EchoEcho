using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EchoStation.Tests.Fixtures
{
    // Actual lighting A/B checks, shared by Editor PlayMode and Windows Player.
    public sealed class R03LightingProbe : IDisposable
    {
        [Serializable] public class ProbeSample { public float x, r, g, b, luminance; }
        [Serializable] public class ImageSample
        {
            public float x, viewportX, viewportY, withProbes, withoutProbes, zeroBakedProbe, doubleBakedProbe, zeroControlSHMagnitude, controlAmbientL0;
            public int withFrame, offFrame, zeroFrame, doubleFrame;
        }
        [Serializable] public class Result
        {
            public bool passed, hdr, displayHdrActive, allStaticBindingsValid;
            public bool stochasticOcclusionIsolated;
            public string utc, unity, api, cpu, gpu, os, error;
            public int ramMb, vramMb, atlasCount, probeCount, width = 1920, height = 1080;
            public long lightmapRuntimeBytes;
            public float reflectionIntensity;
            public float staticMeanLuminance, lightsDisabledDifference, lightmapsRemovedLuminance, lightmapsRemovedDifference;
            public float maxNormalizedProbeStep, endpointProbeColorDistance;
            public ProbeSample[] interpolated;
            public ImageSample[] dynamicImages;
        }
        private readonly Camera camera;
        private readonly MeshRenderer proxy;
        private readonly MeshRenderer[] room;
        private readonly Light[] lights;
        private readonly Volume volume;
        private readonly VolumeProfile profile;
        private readonly Vector3 originalPosition;
        private readonly bool[] originalEnabled, originalLights;
        private readonly int[] indices;
        private readonly Vector4[] offsets;
        private readonly RenderTexture target;
        private readonly Texture2D pixels;
        private readonly LightProbeUsage originalProbeUsage;
        private readonly SphericalHarmonicsL2 originalAmbient;
        private readonly float originalAspect;
        private readonly ScriptableRendererFeature[] occlusionFeatures;
        private readonly bool[] occlusionEnabled;
        private readonly SphericalHarmonicsL2[] originalBakedProbes;
        public Result Report { get; } = new Result();

        public R03LightingProbe(Scene scene, UniversalRendererData rendererData)
        {
            if (!rendererData) throw new ArgumentNullException(nameof(rendererData));
            occlusionFeatures = rendererData.rendererFeatures.Where(f => f && f.GetType().Name == "ScreenSpaceAmbientOcclusion").ToArray();
            occlusionEnabled = occlusionFeatures.Select(f => f.isActive).ToArray();
            foreach (var feature in occlusionFeatures) feature.SetActive(false);
            var roots = scene.GetRootGameObjects();
            camera = roots.Single(o => o.name == "R03_ValidationCamera").GetComponent<Camera>();
            camera.scene = scene;
            originalAspect = camera.aspect; camera.aspect = 16f / 9f;
            proxy = roots.Single(o => o.name == "R03_DynamicProxy").GetComponent<MeshRenderer>();
            room = roots.Single(o => o.name == "R03_StaticRoom").GetComponentsInChildren<MeshRenderer>();
            lights = roots.SelectMany(o => o.GetComponentsInChildren<Light>()).ToArray();
            volume = roots.SelectMany(o => o.GetComponentsInChildren<Volume>()).Single();
            profile = volume.profile;
            profile.TryGet<FilmGrain>(out var grain); grain.intensity.value = 0;
            profile.TryGet<ChromaticAberration>(out var ca); ca.intensity.value = 0;
            originalPosition = proxy.transform.position; originalProbeUsage = proxy.lightProbeUsage;
            originalBakedProbes = LightmapSettings.lightProbes.bakedProbes;
            originalEnabled = room.Select(r => r.enabled).ToArray(); originalLights = lights.Select(l => l.enabled).ToArray();
            indices = room.Select(r => r.lightmapIndex).ToArray(); offsets = room.Select(r => r.lightmapScaleOffset).ToArray();
            originalAmbient = RenderSettings.ambientProbe; RenderSettings.ambientProbe = new SphericalHarmonicsL2();
            target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            target.Create(); pixels = new Texture2D(1920, 1080, TextureFormat.RGBAFloat, false, true);
        }

        private Color[] Capture(string path = null)
        {
            // LightingData can refresh the scene ambient probe on game frames;
            // reassert the neutral environment for each controlled measurement.
            RenderSettings.ambientProbe = new SphericalHarmonicsL2();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            var old = RenderTexture.active;
            Color[] colors;
            try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0, false); pixels.Apply(false); colors = pixels.GetPixels(); }
            finally { RenderTexture.active = old; }
            if (path != null)
            {
                var png = new Texture2D(1920, 1080, TextureFormat.RGB24, false, true);
                try
                {
                    var encoded = new Color32[colors.Length];
                    for (var i = 0; i < colors.Length; i++) encoded[i] = colors[i].gamma;
                    png.SetPixels32(encoded); png.Apply(false); File.WriteAllBytes(path, png.EncodeToPNG());
                }
                finally { Destroy(png); }
            }
            return colors;
        }

        private static float Luma(Color color) => .2126f * color.r + .7152f * color.g + .0722f * color.b;
        private static float Mean(Color[] colors)
        {
            double sum = 0; foreach (var color in colors) sum += Luma(color); return (float)(sum / colors.Length);
        }
        private static float Difference(Color[] a, Color[] b)
        {
            double sum = 0;
            for (var i = 0; i < a.Length; i++) sum += (Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b)) / 3;
            return (float)(sum / a.Length);
        }
        private float ProxyPatch(Color[] colors)
        {
            var center = camera.WorldToViewportPoint(proxy.bounds.center);
            var x = Mathf.Clamp((int)(center.x * 1920), 12, 1907); var y = Mathf.Clamp((int)(center.y * 1080), 12, 1067);
            double sum = 0;
            for (var iy = y - 8; iy < y + 8; iy++) for (var ix = x - 8; ix < x + 8; ix++) sum += Luma(colors[iy * 1920 + ix]);
            return (float)(sum / 256);
        }

        private static IEnumerator NextGameFrame()
        {
            var frame = Time.frameCount; var watch = System.Diagnostics.Stopwatch.StartNew();
            while (Time.frameCount == frame)
            {
                if (watch.Elapsed.TotalSeconds > 5) throw new TimeoutException("R03 game frame did not advance.");
                yield return null;
            }
        }

        public IEnumerator Run(string directory)
        {
            Directory.CreateDirectory(directory); var r = Report;
            r.utc = DateTime.UtcNow.ToString("O"); r.unity = Application.unityVersion; r.api = SystemInfo.graphicsDeviceType.ToString();
            r.cpu = SystemInfo.processorType; r.gpu = SystemInfo.graphicsDeviceName; r.os = SystemInfo.operatingSystem;
            r.ramMb = SystemInfo.systemMemorySize; r.vramMb = SystemInfo.graphicsMemorySize;
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            r.hdr = pipeline.supportsHDR && camera.allowHDR;
            r.displayHdrActive = HDROutputSettings.main != null && HDROutputSettings.main.active;
            var maps = LightmapSettings.lightmaps;
            r.reflectionIntensity = RenderSettings.reflectionIntensity;
            r.stochasticOcclusionIsolated = occlusionFeatures.All(f => !f.isActive);
            r.atlasCount = maps.Length; r.probeCount = LightmapSettings.lightProbes ? LightmapSettings.lightProbes.count : 0;
            r.lightmapRuntimeBytes = maps.Sum(m => (m.lightmapColor ? Profiler.GetRuntimeMemorySizeLong(m.lightmapColor) : 0) +
                (m.lightmapDir ? Profiler.GetRuntimeMemorySizeLong(m.lightmapDir) : 0));
            r.allStaticBindingsValid = room.All(o => o.lightmapIndex >= 0 && o.lightmapIndex < maps.Length &&
                o.lightmapScaleOffset.x > 0 && o.lightmapScaleOffset.y > 0);
            proxy.enabled = false;
            var original = Capture(Path.Combine(directory, "01_Baked_LightsEnabled.png"));
            r.staticMeanLuminance = Mean(original);
            foreach (var light in lights) light.enabled = false;
            var disabled = Capture(Path.Combine(directory, "02_Baked_AllLightsDisabled.png"));
            r.lightsDisabledDifference = Difference(original, disabled);
            foreach (var renderer in room) renderer.lightmapIndex = -1;
            var removed = Capture(Path.Combine(directory, "03_WithoutLightmaps.png"));
            r.lightmapsRemovedLuminance = Mean(removed); r.lightmapsRemovedDifference = Difference(disabled, removed);
            for (var i = 0; i < room.Length; i++) { room[i].lightmapIndex = indices[i]; room[i].lightmapScaleOffset = offsets[i]; }
            original = null; disabled = null; removed = null;

            // 0.1m steps across the full path; interpolate genuine baked SH.
            LightProbes.Tetrahedralize();
            var samples = new List<ProbeSample>();
            var normals = new[] { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            var evaluated = new Color[6];
            for (var i = 0; i <= 48; i++)
            {
                var x = -2.4f + i * .1f;
                LightProbes.GetInterpolatedProbe(new Vector3(x, .9f, -.8f), null, out var sh); sh.Evaluate(normals, evaluated);
                var average = Color.black;
                foreach (var c in evaluated) average += new Color(Mathf.Max(0, c.r), Mathf.Max(0, c.g), Mathf.Max(0, c.b), 0) / 6;
                samples.Add(new ProbeSample { x = x, r = average.r, g = average.g, b = average.b, luminance = Luma(average) });
            }
            r.interpolated = samples.ToArray();
            var peak = samples.Max(s => s.luminance);
            for (var i = 1; i < samples.Count; i++) r.maxNormalizedProbeStep = Mathf.Max(r.maxNormalizedProbeStep,
                Mathf.Abs(samples[i].luminance - samples[i - 1].luminance) / Mathf.Max(.000001f, peak));
            var first = samples[0]; var last = samples[samples.Count - 1];
            r.endpointProbeColorDistance = Vector3.Distance(new Vector3(first.r, first.g, first.b), new Vector3(last.r, last.g, last.b));

            foreach (var renderer in room) renderer.enabled = false;
            proxy.enabled = true;
            var images = new List<ImageSample>();
            var zeroProbes = new SphericalHarmonicsL2[originalBakedProbes.Length];
            var doubleProbes = (SphericalHarmonicsL2[])originalBakedProbes.Clone();
            for (var p = 0; p < doubleProbes.Length; p++) for (var c = 0; c < 3; c++) for (var k = 0; k < 9; k++)
                doubleProbes[p][c, k] *= 2;
            for (var i = 0; i < 9; i++)
            {
                var x = -2.4f + i * .6f; proxy.transform.position = new Vector3(x, .9f, -.8f);
                proxy.lightProbeUsage = LightProbeUsage.BlendProbes;
                LightmapSettings.lightProbes.bakedProbes = originalBakedProbes;
                yield return NextGameFrame();
                var with = Capture(i == 0 || i == 8 ? Path.Combine(directory, $"04_Proxy_Probes_{i}.png") : null);
                var withLuma = ProxyPatch(with); var withFrame = Time.frameCount;
                proxy.lightProbeUsage = LightProbeUsage.Off;
                yield return NextGameFrame();
                var without = Capture();
                var fallbackLuma = ProxyPatch(without); var offFrame = Time.frameCount;
                proxy.lightProbeUsage = LightProbeUsage.BlendProbes;
                LightmapSettings.lightProbes.bakedProbes = zeroProbes;
                yield return NextGameFrame();
                LightProbes.GetInterpolatedProbe(proxy.transform.position, null, out var zeroSh);
                float zeroMagnitude = 0;
                for (var channel = 0; channel < 3; channel++) for (var coefficient = 0; coefficient < 9; coefficient++)
                    zeroMagnitude = Mathf.Max(zeroMagnitude, Mathf.Abs(zeroSh[channel, coefficient]));
                var zero = Capture(i == 0 ? Path.Combine(directory, "05_Proxy_ZeroBakedProbe.png") : null);
                var viewport = camera.WorldToViewportPoint(proxy.bounds.center);
                var zeroLuma = ProxyPatch(zero);
                var zeroFrame = Time.frameCount;
                var ambient = Mathf.Abs(RenderSettings.ambientProbe[0, 0]) + Mathf.Abs(RenderSettings.ambientProbe[1, 0]) + Mathf.Abs(RenderSettings.ambientProbe[2, 0]);
                LightmapSettings.lightProbes.bakedProbes = doubleProbes;
                yield return NextGameFrame();
                var doubled = Capture(i == 0 ? Path.Combine(directory, "07_Proxy_DoubleProbe.png") : null);
                images.Add(new ImageSample { x = x, viewportX = viewport.x, viewportY = viewport.y,
                    withProbes = withLuma, withoutProbes = fallbackLuma, zeroBakedProbe = zeroLuma, doubleBakedProbe = ProxyPatch(doubled),
                    zeroControlSHMagnitude = zeroMagnitude, controlAmbientL0 = ambient,
                    withFrame = withFrame, offFrame = offFrame, zeroFrame = zeroFrame, doubleFrame = Time.frameCount });
                LightmapSettings.lightProbes.bakedProbes = originalBakedProbes;
                yield return NextGameFrame();
            }
            r.dynamicImages = images.ToArray();
            proxy.lightProbeUsage = originalProbeUsage; proxy.transform.position = originalPosition;
            for (var i = 0; i < room.Length; i++) room[i].enabled = originalEnabled[i];
            profile.TryGet<FilmGrain>(out var grain); grain.intensity.value = .1f;
            profile.TryGet<ChromaticAberration>(out var ca); ca.intensity.value = .02f;
            RestoreOcclusion();
            yield return NextGameFrame();
            Capture(Path.Combine(directory, "06_Room_ApprovedVolume.png"));
            r.passed = r.unity == "6000.6.7f1" && r.api == "Direct3D11" && r.hdr && !r.displayHdrActive &&
                !camera.GetUniversalAdditionalCameraData().allowHDROutput && r.atlasCount > 0 && r.probeCount > 0 && r.allStaticBindingsValid &&
                r.staticMeanLuminance > .001f && r.lightsDisabledDifference < .000001f && r.lightmapsRemovedDifference > .001f &&
                r.lightmapsRemovedLuminance < r.staticMeanLuminance * .1f && samples.All(s => s.luminance > .0001f) &&
                r.maxNormalizedProbeStep < .25f && r.endpointProbeColorDistance > .001f &&
                images.All(s => s.viewportX > .01f && s.viewportX < .99f && s.viewportY > .01f && s.viewportY < .99f &&
                    s.withProbes > .0001f && s.zeroControlSHMagnitude < .000001f &&
                    s.withProbes > s.zeroBakedProbe + .0001f && s.doubleBakedProbe > s.withProbes + .0001f &&
                    s.withFrame < s.offFrame && s.offFrame < s.zeroFrame && s.zeroFrame < s.doubleFrame);
        }

        public void Dispose()
        {
            proxy.enabled = true; proxy.transform.position = originalPosition; proxy.lightProbeUsage = originalProbeUsage;
            LightmapSettings.lightProbes.bakedProbes = originalBakedProbes; camera.aspect = originalAspect;
            RestoreOcclusion();
            for (var i = 0; i < room.Length; i++) { room[i].enabled = originalEnabled[i]; room[i].lightmapIndex = indices[i]; room[i].lightmapScaleOffset = offsets[i]; }
            for (var i = 0; i < lights.Length; i++) lights[i].enabled = originalLights[i];
            RenderSettings.ambientProbe = originalAmbient; volume.profile = null;
            foreach (var component in profile.components) Destroy(component); Destroy(profile);
            target.Release(); Destroy(target); Destroy(pixels);
        }
        private void RestoreOcclusion()
        {
            for (var i = 0; i < occlusionFeatures.Length; i++) occlusionFeatures[i].SetActive(occlusionEnabled[i]);
        }
        private static void Destroy(Object obj) { if (Application.isPlaying) Object.Destroy(obj); else Object.DestroyImmediate(obj); }
    }
}
