using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Salvage.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextLampRenderTests
    {
        const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-P01a-2b2";
        GameObject context;Camera camera;InstancedRoomLamps lamps;bool background;JArray events;string id;
        void Begin()
        {
            background=Application.runInBackground;Application.runInBackground=true;Directory.CreateDirectory(Evidence);id=Guid.NewGuid().ToString("N");context=new GameObject("LampRenderTest");camera=new GameObject("View").AddComponent<Camera>();camera.transform.SetParent(context.transform);camera.transform.position=new Vector3(6,1.4f,12);camera.transform.LookAt(new Vector3(6,2.91f,12),Vector3.forward);camera.fieldOfView=140;camera.aspect=1920f/1080;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=true;camera.scene=context.scene;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.allowHDROutput=false;var sun=new GameObject("Sun").AddComponent<Light>();sun.transform.SetParent(context.transform);sun.type=LightType.Directional;sun.intensity=.25f;lamps=context.AddComponent<InstancedRoomLamps>();lamps.Configure(AssetDatabase.LoadAssetAtPath<Mesh>(NextLampAssets.MeshPath),new[]{AssetDatabase.LoadAssetAtPath<Material>(NextLampAssets.BodyPath),AssetDatabase.LoadAssetAtPath<Material>(NextLampAssets.LightPath)},new[]{new Vector3(3,2.91f,4),new Vector3(3,2.91f,12),new Vector3(3,2.91f,20),new Vector3(9,2.91f,4),new Vector3(9,2.91f,12),new Vector3(9,2.91f,20)});
        }
        void Render(){lamps.Submit(camera);using(var image=new SalvageFrameCapture(camera))image.Capture(Evidence+"/FDImage_"+id+".png");}
        void Received(JArray value){events=value;}
        [UnityTest,Timeout(180000)]public IEnumerator RealHdrEmitterUsesInstancedNativeDrawsAndFrustumDistanceCulling()
        {
            yield return new EnterPlayMode();Begin();yield return null;Color[] off,on;using(var image=new SalvageFrameCapture(camera))off=image.Capture(Evidence+"/Off_"+id+".png");lamps.Submit(camera);using(var image=new SalvageFrameCapture(camera))on=image.Capture(Evidence+"/On_"+id+".png");float difference=SalvageFrameCapture.Difference(off,on);float peak=on.Max(p=>Mathf.Max(p.r,Mathf.Max(p.g,p.b)));Assert.That(difference,Is.GreaterThan(.0001f));Assert.That(peak,Is.GreaterThan(1),"Actual internal HDR self-emission required");Assert.That(lamps.LastBatches,Is.EqualTo(2));Assert.That(lamps.LastInstances,Is.InRange(2,6));yield return NextFrameEvidence.Capture(Render,Evidence+"/FD_"+id+".json",Received);var batches=events.Where(e=>e["type"].Value<string>()=="InstancedMesh"&&e["instances"].Value<int>()>=2&&e["shader"].Value<string>()=="Universal Render Pipeline/Lit").ToArray();Assert.That(batches.Length,Is.GreaterThanOrEqualTo(2),events.ToString());Assert.That(batches.All(e=>e["draws"].Value<int>()==1),Is.True);camera.transform.position=new Vector3(-50,1.4f,12);Assert.That(lamps.Evaluate(camera),Is.Zero);camera.transform.position=new Vector3(6,1.4f,12);camera.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);Assert.That(lamps.Evaluate(camera),Is.Zero);File.WriteAllText(Evidence+"/Render_"+id+".json",new JObject{{"onOffDifference",difference},{"hdrPeak",peak},{"nativeInstancedEvents",batches.Length},{"instances",new JArray(batches.Select(e=>e["instances"].Value<int>()))},{"distanceAndFrustumCulling",true},{"newRealtimeLights",0}}.ToString());
        }
        [UnityTest]public IEnumerator SteadyMatrixEvaluationHasNoManagedAllocationAndNoPhysicsOrCpuAnimation()
        {
            yield return new EnterPlayMode();Begin();for(int i=0;i<10;i++)lamps.Evaluate(camera);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)lamps.Evaluate(camera);long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(bytes,Is.Zero);Assert.That(lamps.InstanceCount,Is.EqualTo(6));Assert.That(context.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(context.GetComponentsInChildren<Animator>(),Is.Empty);Assert.That(context.GetComponentsInChildren<ParticleSystem>(),Is.Empty);lamps.enabled=false;Assert.That(lamps.Evaluate(camera),Is.Zero);Assert.That(lamps.LastBatches,Is.Zero);File.WriteAllText(Evidence+"/Budget_"+id+".json",new JObject{{"evaluations",1000},{"managedBytes",bytes},{"instances",6},{"newPhysicsOrAnimation",false}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(context)Object.Destroy(context);Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
