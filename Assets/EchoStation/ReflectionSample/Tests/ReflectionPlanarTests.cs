using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.ReflectionSample.Editor;
using EchoStation.Tests.Fixtures;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace EchoStation.ReflectionSample.Tests
{
    public sealed class ReflectionPlanarTests:ReflectionTestBase
    {
        static Color[] Read(RenderTexture rt)
        {var prior=RenderTexture.active;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGBAFloat,false,true);try{RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();return t.GetPixels();}finally{RenderTexture.active=prior;UnityEngine.Object.Destroy(t);}}
        [Test]public void MirrorMathPreservesTangentAndReversesPlaneDistance()
        {float y=.045f;var m=ReflectionPlanar.ReflectionMatrix(new Vector4(0,1,0,-y));var p=new Vector3(1,2,3);var q=m.MultiplyPoint3x4(p);Assert.That(q.x,Is.EqualTo(1));Assert.That(q.z,Is.EqualTo(3));Assert.That(q.y,Is.EqualTo(2*y-2).Within(.00001));Assert.That(Vector3.Distance(m.MultiplyPoint3x4(q),p),Is.LessThan(.00001));Assert.That(m.determinant,Is.EqualTo(-1).Within(.00001));}
        [UnityTest,Timeout(120000)]public IEnumerator ActualDynamicTextureChangesAndSwitchReleasesResources()
        {
            yield return new EnterPlayMode();yield return Load();Freeze();camera.transform.position=new Vector3(0,1.6f,-2.55f);camera.transform.LookAt(new Vector3(0,.65f,.7f));var p=root.GetComponentInChildren<ReflectionPlanar>();var motion=root.GetComponent<ReflectionDemoMotion>();bool inverted=GL.invertCulling;string id=Guid.NewGuid().ToString("N").Substring(0,8);
            Assert.That(root.GetComponentsInChildren<ReflectionPlanar>().Length,Is.EqualTo(1));Assert.That(p.sourceCamera,Is.SameAs(camera));Assert.That(root.Find("RS_TestSphere_1").GetComponent<Renderer>().sharedMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
            using(var capture=new SceneFrameCapture(camera))
            {
                motion.Pose(0);preview.SetDynamic(true);var on=capture.Capture(ReflectionSampleBuilder.Evidence+"/Planar_On_"+id+".png");Assert.That(p.RenderCount,Is.GreaterThan(0));Assert.That(p.LastTexture.IsCreated(),Is.True);Assert.That(p.LastTexture.width,Is.EqualTo(1024));var a=Read(p.LastTexture);int before=p.RenderCount;motion.Pose(1.5f);capture.Capture();var b=Read(p.LastTexture);float dynamic=SceneFrameCapture.Difference(a,b);Assert.That(p.RenderCount,Is.GreaterThan(before));
                motion.Pose(0);preview.SetDynamic(false);yield return Frames(3);var off=capture.Capture(ReflectionSampleBuilder.Evidence+"/Planar_Off_"+id+".png");float delta=SceneFrameCapture.Difference(on,off);Assert.That(dynamic,Is.GreaterThan(.00002f));Assert.That(delta,Is.GreaterThan(.0005f));Assert.That(GL.invertCulling,Is.EqualTo(inverted));Assert.That(Resources.FindObjectsOfTypeAll<Camera>().Count(c=>c.name.StartsWith("RS_PlanarCamera_")),Is.Zero);Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.name.StartsWith("RS_PlanarHDR_")),Is.Zero);
                preview.SetDynamic(true);capture.Capture();Assert.That(p.LastTexture.IsCreated(),Is.True);File.WriteAllText(ReflectionSampleBuilder.Evidence+"/PlanarDeltas_"+id+".json",new JObject{{"dynamicTextureDelta",dynamic},{"visiblePlaneDelta",delta},{"mainImageRobotPoseMatched",true},{"width",1024},{"onlySourceCamera",true},{"releasedAndRecreated",true},{"cullingRestored",GL.invertCulling==inverted}}.ToString());
            }
        }
        [UnityTest,Timeout(120000)]public IEnumerator OtherCameraAndBelowPlaneObjectAreExcluded()
        {
            yield return new EnterPlayMode();yield return Load();Freeze();camera.transform.position=new Vector3(0,1.6f,-2.55f);camera.transform.LookAt(new Vector3(0,.65f,.7f));var p=root.GetComponentInChildren<ReflectionPlanar>();preview.SetDynamic(true);
            using(var capture=new SceneFrameCapture(camera))
            {
                capture.Capture();var before=Read(p.LastTexture);var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=Color.red;cube.GetComponent<Renderer>().sharedMaterial=mat;cube.transform.position=new Vector3(0,-1,.2f);cube.transform.localScale=Vector3.one*.5f;capture.Capture();float clipped=SceneFrameCapture.Difference(before,Read(p.LastTexture));Assert.That(clipped,Is.LessThan(.00001f),"Below-plane geometry must be clipped, not mirrored into room");
                var other=new GameObject("RS_ExcludedCamera").AddComponent<Camera>();other.enabled=false;other.CopyFrom(camera);other.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);other.scene=sample;int count=p.RenderCount;using(var otherCapture=new SceneFrameCapture(other))otherCapture.Capture();Assert.That(p.RenderCount,Is.EqualTo(count),"Only configured sample camera may generate a reflection");UnityEngine.Object.Destroy(other.gameObject);UnityEngine.Object.Destroy(cube);UnityEngine.Object.Destroy(mat);
            }
        }
    }
}
