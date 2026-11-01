using EchoStation.Echo;
using EchoStation.Echo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.Echo
{
    public class EchoPresentationTests
    {
        private UnityEngine.SceneManagement.Scene scene;private GameObject actor;private EchoRig rig;private EchoPresentation presentation;
        [SetUp]public void Before()
        {
            scene=EditorSceneManager.NewPreviewScene();actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath),scene);rig=actor.GetComponent<EchoRig>();presentation=actor.GetComponent<EchoPresentation>();presentation.enabled=false;
        }
        [Test]public void E11_A01_ThreeMoodsChooseSharedMaterialsAndThinkingBlinks()
        {
            presentation.ApplyFrame(0,EchoMood.Calm,.1f,.1f,false);Assert.That(rig.Status.sharedMaterial,Is.SameAs(rig.Calm));Assert.That(rig.Status.enabled,Is.True);
            presentation.ApplyFrame(0,EchoMood.Thinking,.1f,.1f,false);Assert.That(rig.Status.sharedMaterial,Is.SameAs(rig.Thinking));Assert.That(rig.Status.enabled,Is.True);
            presentation.ApplyFrame(0,EchoMood.Thinking,.1f,.4f,false);Assert.That(rig.Status.enabled,Is.False);presentation.ApplyFrame(0,EchoMood.Alert,.1f,.4f,false);Assert.That(rig.Status.sharedMaterial,Is.SameAs(rig.Alert));Assert.That(rig.Status.enabled,Is.True);
        }
        [Test]public void E11_A02_ForwardAndReverseWheelAnglesUsePhysicalRadiusWithoutMovingBody()
        {
            var root=actor.transform.position;var body=rig.Body.position;var initial=rig.Wheels[0].localRotation;presentation.ApplyFrame(2.2f,EchoMood.Calm,.1f,0,false);var expected=initial*Quaternion.AngleAxis(2.2f/.14f*Mathf.Rad2Deg*.1f,Vector3.right);
            foreach(var wheel in rig.Wheels)Assert.That(Quaternion.Angle(wheel.localRotation,expected),Is.LessThan(.01f));presentation.ApplyFrame(-2.2f,EchoMood.Calm,.1f,0,false);foreach(var wheel in rig.Wheels)Assert.That(Quaternion.Angle(wheel.localRotation,initial),Is.LessThan(.01f));Assert.That(actor.transform.position,Is.EqualTo(root));Assert.That(rig.Body.position,Is.EqualTo(body));
        }
        [Test]public void E11_A03_ThinkingMovesProbeAndScannerOwnershipPreventsPoseOverwrite()
        {
            var root=actor.transform.rotation;var body=rig.Body.position;presentation.ApplyFrame(0,EchoMood.Thinking,.2f,.2f,false);Assert.That(Quaternion.Angle(Quaternion.identity,rig.ProbePitch.localRotation),Is.GreaterThan(1));
            var scan=Quaternion.Euler(-20,35,0);rig.ProbePitch.localRotation=scan;presentation.ApplyFrame(0,EchoMood.Thinking,.2f,.1f,true);Assert.That(Quaternion.Angle(scan,rig.ProbePitch.localRotation),Is.LessThan(.001f));Assert.That(presentation.ScanLight.enabled,Is.True);Assert.That(actor.transform.rotation,Is.EqualTo(root));Assert.That(rig.Body.position,Is.EqualTo(body));
            presentation.ApplyFrame(0,EchoMood.Calm,1,.1f,false);Assert.That(Quaternion.Angle(Quaternion.identity,rig.ProbePitch.localRotation),Is.LessThan(.1f));Assert.That(presentation.ScanLight.enabled,Is.False);
        }
        [Test]public void E11_A04_RealtimeScanLightMatchesValidatedTemplateAndDynamicRenderContract()
        {
            var light=presentation.ScanLight;Assert.That(light.transform.parent,Is.SameAs(rig.ScanOrigin));Assert.That(light.type,Is.EqualTo(LightType.Spot));Assert.That(light.lightmapBakeType,Is.EqualTo(LightmapBakeType.Realtime));Assert.That(light.intensity,Is.EqualTo(3));Assert.That(light.range,Is.EqualTo(8));Assert.That(light.spotAngle,Is.EqualTo(70));Assert.That(light.shadows,Is.EqualTo(LightShadows.Soft));
            Assert.That(new SerializedObject(light.GetUniversalAdditionalLightData()).FindProperty("m_AdditionalLightsShadowResolutionTier").intValue,Is.EqualTo(1));
            foreach(var renderer in actor.GetComponentsInChildren<MeshRenderer>()){Assert.That(renderer.HasPropertyBlock(),Is.False);Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(renderer.lightProbeUsage,Is.EqualTo(LightProbeUsage.BlendProbes));Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject),Is.EqualTo((StaticEditorFlags)0));}
            Assert.That(actor.GetComponentsInChildren<Animator>().Length+actor.GetComponentsInChildren<SkinnedMeshRenderer>().Length+actor.GetComponentsInChildren<ParticleSystem>().Length,Is.Zero);
        }
        [TearDown]public void After(){if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);}
    }
}
