using System;
using System.Collections;
using System.IO;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EchoStation.Tests.Rendering
{
    public class RealtimeShadowPolicyTests
    {
        private GameObject host, cube, cameraObject;
        private Renderer target;
        private DistanceShadowCaster policy;
        private Camera camera;
        private void Create()
        {
            host = new GameObject("R04_PolicyTest");
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube); target = cube.GetComponent<Renderer>();
            cameraObject = new GameObject("R04_CameraTest", typeof(Camera)); camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            policy = host.AddComponent<DistanceShadowCaster>(); policy.Configure(camera, new[] { target });
        }

        [TearDown] public void Cleanup()
        {
            if (host) Object.DestroyImmediate(host);
            if (cube) Object.DestroyImmediate(cube);
            if (cameraObject) Object.DestroyImmediate(cameraObject);
        }

        [Test] public void A01_PipelineUsesFourCascadesAndExplicitShadowDistances()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ForwardHdrBaselineSetup.PipelinePath);
            Assert.That(asset.shadowCascadeCount, Is.EqualTo(4)); Assert.That(asset.mainLightShadowmapResolution, Is.EqualTo(2048));
            Assert.That(asset.shadowDistance, Is.EqualTo(40)); Assert.That(asset.cascade4Split, Is.EqualTo(new Vector3(.1f, .25f, .5f)));
            Assert.That(asset.cascadeBorder, Is.EqualTo(.1f));
        }

        [Test] public void A02_BoundsDistanceAndHysteresisKeepVisibilityAndReceivingShadows()
        {
            Create(); var receives = target.receiveShadows;
            // Cube extends to x=.5: these are distances from its bounds, not pivot.
            policy.Evaluate(new Vector3(30.4f, 0, 0)); Assert.That(target.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
            policy.Evaluate(new Vector3(30.5f, 0, 0)); Assert.That(target.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            policy.Evaluate(new Vector3(29.5f, 0, 0)); Assert.That(target.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(target.enabled, Is.True); Assert.That(target.receiveShadows, Is.EqualTo(receives));
            policy.Evaluate(new Vector3(28.5f, 0, 0)); Assert.That(target.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
        }

        [UnityTest] public IEnumerator A03_OriginalModeSurvivesFarNearDisableAndDestroy()
        {
            yield return new EnterPlayMode();
            foreach (var mode in new[] { ShadowCastingMode.TwoSided, ShadowCastingMode.ShadowsOnly, ShadowCastingMode.Off })
            {
                Create(); policy.enabled = false; target.shadowCastingMode = mode; policy.enabled = true;
                policy.Evaluate(Vector3.one * 100); Assert.That(target.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off), mode.ToString());
                policy.Evaluate(Vector3.zero); Assert.That(target.shadowCastingMode, Is.EqualTo(mode));
                policy.Evaluate(Vector3.one * 100); policy.enabled = false; Assert.That(target.shadowCastingMode, Is.EqualTo(mode));
                policy.enabled = true; policy.Evaluate(Vector3.one * 100); Object.DestroyImmediate(policy);
                Assert.That(target.shadowCastingMode, Is.EqualTo(mode)); Cleanup();
            }
        }

        [Test] public void A04_NullAndDestroyedTargetsDoNotAffectAnotherRenderer()
        {
            Create(); var other = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                other.transform.position = Vector3.one * 100; var untouched = other.GetComponent<Renderer>();
                policy.Configure(null, new[] { target, null }); policy.Evaluate(Vector3.one * 100);
                Assert.That(untouched.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
                Object.DestroyImmediate(cube); Assert.DoesNotThrow(() => policy.Evaluate(Vector3.zero));
                policy.Configure(null, null); Assert.DoesNotThrow(() => policy.Evaluate(Vector3.zero));
            }
            finally { Object.DestroyImmediate(other); }
        }

        [TestCase(0, 0, .1f)] [TestCase(float.NaN, 2, .1f)] [TestCase(float.PositiveInfinity, 2, .1f)]
        [TestCase(30, -1, .1f)] [TestCase(30, 30, .1f)] [TestCase(30, 2, 0)] [TestCase(30, 2, float.NaN)]
        public void A05_InvalidParametersAreRejectedWithoutChangingRenderer(float distance, float margin, float seconds)
        {
            Create(); Assert.Throws<ArgumentOutOfRangeException>(() => policy.Configure(camera, new[] { target }, distance, margin, seconds));
            Assert.That(target.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
        }

        [Test] public void A06_PreviousAssetsAreUnchanged()
        {
            var record = JsonUtility.FromJson<ForwardHdrBaselineSetup.SetupRecord>(File.ReadAllText(Path.Combine(RealtimeShadowSetup.EvidencePath, "ProtectedFiles.json")));
            foreach (var item in record.templates) Assert.That(ForwardHdrBaselineSetup.Hash(item.path), Is.EqualTo(item.sha256), item.path);
        }

        [UnityTest] public IEnumerator A07_RuntimeUpdateFollowsAssignedCameraAndRestoresOnDisable()
        {
            yield return new EnterPlayMode(); var background = Application.runInBackground; Application.runInBackground = true;
            try
            {
                Create(); camera.transform.position = Vector3.one * 100;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(target.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                camera.transform.position = Vector3.zero; yield return new WaitForSecondsRealtime(.15f);
                Assert.That(target.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
                camera.transform.position = Vector3.one * 100; yield return new WaitForSecondsRealtime(.15f);
                policy.enabled = false; Assert.That(target.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
            }
            finally { Cleanup(); Application.runInBackground = background; }
        }
        [UnityTearDown] public IEnumerator LeaveRuntime() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
