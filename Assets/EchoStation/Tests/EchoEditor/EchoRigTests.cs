using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.Echo
{
    public class EchoRigTests
    {
        [Test]public void E01_01_IndependentRigFitsDoorwayAndGroundCollider()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath);var rig=prefab.GetComponent<EchoRig>();Assert.That(rig,Is.Not.Null);
            Assert.That(rig.Wheels.Count,Is.EqualTo(4));Assert.That(rig.Wheels.Distinct().Count(),Is.EqualTo(4));Assert.That(rig.ScanOrigin.IsChildOf(rig.ProbePitch),Is.True);
            Assert.That(prefab.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(prefab.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));
            var box=prefab.GetComponent<BoxCollider>();Assert.That(box.size.x,Is.LessThan(1.6f));Assert.That(box.size.y,Is.LessThan(2.4f));Assert.That(box.center.y-box.size.y*.5f,Is.EqualTo(0).Within(.0001f));
            foreach(var wheel in rig.Wheels)Assert.That(wheel.localPosition.y,Is.EqualTo(.14f));
            Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Length+prefab.GetComponentsInChildren<Animator>().Length+prefab.GetComponentsInChildren<ParticleSystem>().Length,Is.Zero);
        }
        [Test]public void E01_02_RotatingWheelsOrProbeDoesNotDisplaceChassis()
        {
            var scene=EditorSceneManager.NewPreviewScene();var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath),scene);
            try
            {
                var rig=instance.GetComponent<EchoRig>();var body=rig.Body.position;var root=instance.transform.position;var scan=rig.ScanOrigin.position;
                foreach(var wheel in rig.Wheels)wheel.Rotate(Vector3.right,60,Space.Self);rig.ProbePitch.localRotation=Quaternion.Euler(25,0,0);
                Assert.That(rig.Body.position,Is.EqualTo(body));Assert.That(instance.transform.position,Is.EqualTo(root));Assert.That(Vector3.Distance(scan,rig.ScanOrigin.position),Is.GreaterThan(.02f));
                Assert.That(Vector3.Angle(rig.ScanOrigin.forward,instance.transform.forward),Is.EqualTo(25).Within(.01f));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]public void E01_03_DynamicSrpMaterialsAndThreeIndicatorsAreConfigured()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath);var rig=prefab.GetComponent<EchoRig>();
            foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
            {Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(renderer.HasPropertyBlock(),Is.False);Assert.That(renderer.lightProbeUsage,Is.EqualTo(LightProbeUsage.BlendProbes));Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject),Is.EqualTo((StaticEditorFlags)0));}
            foreach(var material in new[]{rig.Calm,rig.Thinking,rig.Alert}){Assert.That(material.IsKeywordEnabled("_EMISSION"),Is.True,material.name);Assert.That(material.globalIlluminationFlags,Is.EqualTo(MaterialGlobalIlluminationFlags.BakedEmissive));}
            Assert.That(rig.Calm.GetColor("_BaseColor").b,Is.GreaterThan(.9f));Assert.That(rig.Thinking.GetColor("_BaseColor").g,Is.GreaterThan(.6f));Assert.That(rig.Alert.GetColor("_BaseColor").r,Is.GreaterThan(.9f));Assert.That(rig.Status.sharedMaterial,Is.SameAs(rig.Calm));
        }
        [UnityTest]public IEnumerator E01_04_ActualFrontAndRearRenderContainRobotWithoutPinkShader()
        {
            yield return new EnterPlayMode();var background=Application.runInBackground;Application.runInBackground=true;var previous=SceneManager.GetActiveScene();var scene=SceneManager.CreateScene("EchoModelFixture");SceneManager.SetActiveScene(scene);
            try
            {
                var rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath)).GetComponent<EchoRig>();
                var camera=new GameObject("RigCamera",typeof(Camera)).GetComponent<Camera>();camera.scene=scene;camera.allowHDR=true;camera.fieldOfView=38;camera.nearClipPlane=.05f;camera.farClipPlane=10;camera.aspect=16f/9f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(0);data.allowHDROutput=false;data.renderPostProcessing=false;
                var light=new GameObject("RigLight",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-30,0);
                var directory="Docs/Tests/Stage1/Evidence/S1-E01/Images";Directory.CreateDirectory(directory);using var frames=new SceneFrameCapture(camera);
                for(var i=0;i<2;i++)
                {
                    camera.transform.position=new Vector3(i==0?1.6f:-1.6f,1.1f,i==0?1.6f:-1.6f);camera.transform.LookAt(new Vector3(0,.42f,0));yield return R04ShadowProbe.Frame();
                    var colors=frames.Capture(Path.Combine(directory,i==0?"Front.png":"Rear.png"));Assert.That(colors.Count(c=>c.maxColorComponent>.03f),Is.GreaterThan(10000));Assert.That(colors.Count(c=>c.r>.3f&&c.b>.3f&&c.g<.05f),Is.LessThan(1000));
                }
                foreach(var renderer in rig.GetComponentsInChildren<MeshRenderer>())if(renderer!=rig.Status)renderer.enabled=false;light.enabled=false;
                camera.transform.position=rig.Status.transform.position+new Vector3(0,.15f,-.8f);camera.transform.LookAt(rig.Status.transform);yield return R04ShadowProbe.Frame();
                var emission=frames.Capture(Path.Combine(directory,"StatusEmissionDark.png"));Assert.That(emission.Count(c=>c.b>1f),Is.GreaterThan(30),"Indicator must emit actual HDR pixels with all lights disabled.");
            }
            finally{SceneManager.SetActiveScene(previous);unload=SceneManager.UnloadSceneAsync(scene);Application.runInBackground=background;}
        }
        private AsyncOperation unload;
        [UnityTearDown]public IEnumerator LeaveRuntime()
        {
            var background=Application.runInBackground;Application.runInBackground=true;var timer=System.Diagnostics.Stopwatch.StartNew();
            try{while(unload!=null&&!unload.isDone){if(timer.Elapsed.TotalSeconds>5)throw new System.TimeoutException("Rig fixture unload timed out.");yield return null;}}
            finally{unload=null;Application.runInBackground=background;}if(Application.isPlaying)yield return new ExitPlayMode();
        }
    }
}
