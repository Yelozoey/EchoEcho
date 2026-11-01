using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class StationRenderingImageTests:StationPlayFixture
 {
  [Serializable]private class Result{public float[] means;public float stableDifference,occlusionDifference,bakedLightsDifference;public int offOpaqueBatches,onOpaqueBatches;}
  private static float Mean(Color[] c){double s=0;foreach(var p in c)s+=.2126*p.r+.7152*p.g+.0722*p.b;return(float)(s/c.Length);}
  private static int Opaque(FrameDebuggerEvidence.CaptureResult r)=>r.events.Count(e=>e.type=="SRPBatch"&&e.label.Contains("DrawOpaqueObjects"));
  [UnityTest]public IEnumerator W06_C01_ActualFiveZoneRenderRequestsHaveNonblackBakedLighting()
  {
   yield return new EnterPlayMode();yield return Boot();var motor=station.Player;motor.enabled=false;var camera=motor.View.GetComponent<Camera>();camera.scene=scene;camera.aspect=16f/9f;var path="Docs/Tests/Stage2/Evidence/S2-W06/C-Images";Directory.CreateDirectory(path);var result=new Result{means=new float[5]};
   var views=new[]{(new Vector3(0,1.65f,-34),new Vector3(0,1,-14)),(new Vector3(0,1.65f,-2.5f),new Vector3(-2.5f,1.15f,2.3f)),(new Vector3(0,-2.35f,26),new Vector3(0,-2.6f,34.5f)),(new Vector3(0,-2.35f,40.5f),new Vector3(0,-2.7f,53)),(new Vector3(0,-2.35f,61),new Vector3(0,-2.7f,72))};
   using(var frames=new SceneFrameCapture(camera)){for(var i=0;i<views.Length;i++){camera.transform.position=views[i].Item1;camera.transform.LookAt(views[i].Item2);for(var j=0;j<3;j++)yield return R04ShadowProbe.Frame();result.means[i]=Mean(frames.Capture(path+"/Zone_"+i+".png"));Assert.That(result.means[i],Is.GreaterThan(.0001f),"Zone"+i);}}
   File.WriteAllText(path+"/LightingImages.json",JsonUtility.ToJson(result,true));
  }
  [UnityTest]public IEnumerator W06_C02_ActualPvsPreservesControlledImageAndBakedLightsRemainAfterSourceDisabled()
  {
   yield return new EnterPlayMode();yield return Boot();var motor=station.Player;motor.enabled=false;var camera=motor.View.GetComponent<Camera>();camera.scene=scene;camera.aspect=16f/9f;var data=camera.GetUniversalAdditionalCameraData();var pp=data.renderPostProcessing;data.renderPostProcessing=false;
   Assert.That(AssetDatabase.GetAssetPath(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline),Is.EqualTo(EchoStation.Station.StationLayout.DataFolder+"/ES_S2_ForwardHDR.asset"));var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(EchoStation.Station.StationLayout.DataFolder+"/ES_S2_ForwardRenderer.asset");Assert.That(renderer,Is.Not.Null);var features=renderer.rendererFeatures.Where(f=>f&&f.GetType().Name=="ScreenSpaceAmbientOcclusion").ToArray();var active=features.Select(f=>f.isActive).ToArray();foreach(var f in features)f.SetActive(false);
   var lights=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>()).Where(l=>l.lightmapBakeType==LightmapBakeType.Realtime).ToArray();var lightStates=lights.Select(l=>l.enabled).ToArray();foreach(var l in lights)l.enabled=false;station.Echo.GetComponent<EchoPresentation>().enabled=false;
   var shadows=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DistanceShadowCaster>()).ToArray();foreach(var s in shadows)s.enabled=false;
   var folder="Docs/Tests/Stage2/Evidence/S2-W06/C-Controlled";Directory.CreateDirectory(folder);var result=new Result();
   try{using(var frames=new SceneFrameCapture(camera)){
    camera.transform.SetPositionAndRotation(new Vector3(0,1.6f,-5.5f),Quaternion.Euler(0,90,0));camera.useOcclusionCulling=false;for(var i=0;i<5;i++)yield return R04ShadowProbe.Frame();var off=frames.Capture(folder+"/PvsOff.png");yield return R04ShadowProbe.Frame();result.stableDifference=SceneFrameCapture.Difference(off,frames.Capture());Assert.That(Mean(off),Is.GreaterThan(.0001f));Assert.That(result.stableDifference,Is.LessThan(1e-6f));
    FrameDebuggerEvidence.CaptureResult capture=null;yield return FrameDebuggerEvidence.Capture(frames.RenderAndSynchronize,folder+"/FD-Off.json",r=>capture=r);result.offOpaqueBatches=Opaque(capture);
    camera.useOcclusionCulling=true;for(var i=0;i<5;i++)yield return R04ShadowProbe.Frame();result.occlusionDifference=SceneFrameCapture.Difference(off,frames.Capture(folder+"/PvsOn.png"));yield return FrameDebuggerEvidence.Capture(frames.RenderAndSynchronize,folder+"/FD-On.json",r=>capture=r);result.onOpaqueBatches=Opaque(capture);Assert.That(result.occlusionDifference,Is.LessThan(1e-6f));Assert.That(result.onOpaqueBatches,Is.GreaterThan(0));Assert.That(result.offOpaqueBatches,Is.GreaterThanOrEqualTo(result.onOpaqueBatches));
    camera.transform.position=new Vector3(0,-2.35f,26);camera.transform.LookAt(new Vector3(0,-2.6f,34.5f));for(var i=0;i<3;i++)yield return R04ShadowProbe.Frame();var baked=frames.Capture(folder+"/BakedOn.png");var bakedSources=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>()).Where(l=>l.lightmapBakeType==LightmapBakeType.Baked).ToArray();var states=bakedSources.Select(l=>l.enabled).ToArray();try{foreach(var l in bakedSources)l.enabled=false;yield return R04ShadowProbe.Frame();var disabled=frames.Capture(folder+"/BakedSourcesOff.png");result.bakedLightsDifference=SceneFrameCapture.Difference(baked,disabled);Assert.That(Mean(disabled),Is.GreaterThan(.0001f));Assert.That(result.bakedLightsDifference,Is.LessThan(1e-6f));}finally{for(var i=0;i<bakedSources.Length;i++)bakedSources[i].enabled=states[i];}
   }}finally{for(var i=0;i<features.Length;i++)features[i].SetActive(active[i]);for(var i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=lightStates[i];foreach(var s in shadows)if(s)s.enabled=true;data.renderPostProcessing=pp;camera.useOcclusionCulling=true;}
   File.WriteAllText(folder+"/Controlled.json",JsonUtility.ToJson(result,true));
  }
 }
}
