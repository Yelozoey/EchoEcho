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
    public class BakedLightingConfigurationTests
    {
        private Scene preview;
        private GameObject[] roots;
        [SetUp] public void Open()
        { preview = EditorSceneManager.OpenPreviewScene(BakedLightingSetup.ScenePath); roots = preview.GetRootGameObjects(); }
        [TearDown] public void Close() { if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview); }

        [Test] public void A01_LightingSettingsMatchRecommendedBaseline()
        {
            var s = AssetDatabase.LoadAssetAtPath<LightingSettings>(BakedLightingSetup.SettingsPath);
            Assert.That(s.lightmapper, Is.EqualTo(LightingSettings.Lightmapper.ProgressiveCPU));
            Assert.That(s.bakedGI && !s.realtimeGI && !s.autoGenerate, Is.True);
            Assert.That(s.lightmapResolution, Is.EqualTo(20)); Assert.That(s.lightmapMaxSize, Is.EqualTo(1024));
            Assert.That(s.lightmapPadding, Is.EqualTo(4)); Assert.That(s.directionalityMode, Is.EqualTo(LightmapsMode.CombinedDirectional));
            Assert.That(s.lightmapCompression, Is.EqualTo(LightmapCompression.HighQuality));
            Assert.That(s.directSampleCount, Is.EqualTo(64)); Assert.That(s.indirectSampleCount, Is.EqualTo(256));
            Assert.That(s.environmentSampleCount, Is.EqualTo(64)); Assert.That(s.minBounces, Is.EqualTo(2)); Assert.That(s.maxBounces, Is.EqualTo(2));
            Assert.That(s.indirectScale == 1 && s.albedoBoost == 1 && s.lightProbeSampleCountMultiplier == 2 && !s.ao, Is.True);
            Assert.That(s.filteringMode, Is.EqualTo(LightingSettings.FilterMode.Advanced));
            Assert.That(s.denoiserTypeDirect == LightingSettings.DenoiserType.None && s.denoiserTypeIndirect == LightingSettings.DenoiserType.None, Is.True);
            Assert.That(s.filterTypeDirect == LightingSettings.FilterType.Gaussian && s.filterTypeIndirect == LightingSettings.FilterType.Gaussian, Is.True);
            Assert.That(s.filteringGaussianRadiusDirect, Is.EqualTo(1)); Assert.That(s.filteringGaussianRadiusIndirect, Is.EqualTo(2));
            StringAssert.Contains(AssetDatabase.AssetPathToGUID(BakedLightingSetup.SettingsPath), File.ReadAllText(BakedLightingSetup.ScenePath));
        }

        [Test] public void A02_EightStaticSolidsHaveUsableUv2AndLightmapFlags()
        {
            var room = roots.Single(o => o.name == "R03_StaticRoom");
            var renderers = room.GetComponentsInChildren<MeshRenderer>(); Assert.That(renderers.Length, Is.EqualTo(8));
            foreach (var r in renderers)
            {
                Assert.That((GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0, Is.True);
                Assert.That(r.receiveGI, Is.EqualTo(ReceiveGI.Lightmaps)); Assert.That(r.lightProbeUsage, Is.EqualTo(LightProbeUsage.Off));
                var mesh = r.GetComponent<MeshFilter>().sharedMesh; var uv = mesh.uv2;
                Assert.That(uv.Length, Is.EqualTo(mesh.vertexCount));
                Assert.That(uv.All(p => !float.IsNaN(p.x) && !float.IsNaN(p.y) && p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1), Is.True);
                var triangles = mesh.triangles;
                for (var i = 0; i < triangles.Length; i += 3)
                {
                    var a = uv[triangles[i + 1]] - uv[triangles[i]]; var b = uv[triangles[i + 2]] - uv[triangles[i]];
                    Assert.That(Mathf.Abs(a.x * b.y - a.y * b.x), Is.GreaterThan(0.00000001f), r.name);
                }
                Assert.That(r.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            }
            Assert.That(room.transform.Find("Floor").GetComponent<Renderer>().bounds.max.y, Is.EqualTo(0));
            Assert.That(room.transform.Find("Ceiling").GetComponent<Renderer>().bounds.min.y, Is.EqualTo(4));
            Assert.That(room.transform.Find("WallFront").GetComponent<Renderer>().bounds.max.z, Is.EqualTo(-3));
            Assert.That(room.transform.Find("WallBack").GetComponent<Renderer>().bounds.min.z, Is.EqualTo(3));
        }

        [Test] public void A03_ProbeGridAvoidsSolidsAndCoversDynamicPath()
        {
            var group = roots.Single(o => o.name == "R03_LightProbes").GetComponent<LightProbeGroup>();
            var bounds = roots.Single(o => o.name == "R03_StaticRoom").GetComponentsInChildren<Renderer>().Select(r => r.bounds).ToArray();
            Assert.That(group.probePositions.Length, Is.GreaterThan(70));
            Assert.That(group.probePositions.All(p => !bounds.Any(b => b.Contains(p))), Is.True);
            Assert.That(group.probePositions.Min(p => p.x), Is.LessThan(-2.4f)); Assert.That(group.probePositions.Max(p => p.x), Is.GreaterThan(2.4f));
            Assert.That(group.probePositions.Min(p => p.y), Is.LessThan(.9f)); Assert.That(group.probePositions.Max(p => p.y), Is.GreaterThan(.9f));
        }

        [Test] public void A04_DynamicProxyCameraAndBakedOnlyLightsAreExplicit()
        {
            var proxy = roots.Single(o => o.name == "R03_DynamicProxy"); var renderer = proxy.GetComponent<MeshRenderer>();
            Assert.That(GameObjectUtility.GetStaticEditorFlags(proxy), Is.EqualTo((StaticEditorFlags)0));
            Assert.That(renderer.lightProbeUsage, Is.EqualTo(LightProbeUsage.BlendProbes)); Assert.That(renderer.receiveGI, Is.EqualTo(ReceiveGI.LightProbes));
            Assert.That(renderer.sharedMaterial.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF"), Is.True);
            var lights = roots.SelectMany(o => o.GetComponentsInChildren<Light>()).ToArray(); Assert.That(lights.Length, Is.EqualTo(2));
            Assert.That(lights.All(l => l.lightmapBakeType == LightmapBakeType.Baked && l.intensity == 3 && l.range == 8), Is.True);
            var camera = roots.Single(o => o.name == "R03_ValidationCamera").GetComponent<Camera>();
            Assert.That(camera.allowHDR, Is.True); var data = camera.GetUniversalAdditionalCameraData();
            Assert.That(data.renderPostProcessing && !data.allowHDROutput, Is.True);
            Assert.That(data.volumeLayerMask.value, Is.EqualTo(1 << LayerMask.NameToLayer("PostProcessing")));
        }

        [Test] public void A05_PreviousModuleAssetsAreProtected()
        {
            var record = JsonUtility.FromJson<ForwardHdrBaselineSetup.SetupRecord>(File.ReadAllText(Path.Combine(BakedLightingSetup.EvidencePath, "ProtectedFiles.json")));
            foreach (var item in record.templates) Assert.That(ForwardHdrBaselineSetup.Hash(item.path), Is.EqualTo(item.sha256), item.path);
        }

        [Test] public void A06_RepeatCreationDoesNotRewriteAssetsOrDirtyOriginalScene()
        {
            var paths = new[] { BakedLightingSetup.SettingsPath, BakedLightingSetup.ScenePath, BakedLightingSetup.MaterialPath };
            var hashes = paths.Select(ForwardHdrBaselineSetup.Hash).ToArray(); var active = SceneManager.GetActiveScene(); var dirty = active.isDirty;
            BakedLightingSetup.CreateTemplate();
            Assert.That(paths.Select(ForwardHdrBaselineSetup.Hash).ToArray(), Is.EqualTo(hashes)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
