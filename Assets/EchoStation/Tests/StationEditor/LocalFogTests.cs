using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Rendering;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class LocalFogTests:StationPlayFixture
 {
  [Serializable]private class Evidence{public int width,height,steps;public float fogDifference,lightDifference,recoveryDifference,occludedRoiDifference,unoccludedDifference;}
  private static float Difference(Color[] a,Color[] b,int x0,int y0,int w,int h){double s=0;for(var y=y0;y<y0+h;y++)for(var x=x0;x<x0+w;x++){var i=y*1920+x;s+=(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b))/3;}return(float)(s/(w*h));}
  [Test]public void P03_B01_S2PipelineOwnsOrdinaryForwardHdrRendererAndValidFogShader()
  {
   var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(StationLayout.DataFolder+"/ES_S2_ForwardHDR.asset");var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(StationLayout.DataFolder+"/ES_S2_ForwardRenderer.asset");Assert.That(pipeline,Is.Not.Null);Assert.That(pipeline.supportsHDR,Is.True);Assert.That(renderer.renderingMode,Is.EqualTo(RenderingMode.Forward));Assert.That(renderer.rendererFeatures.OfType<LocalFogFeature>().Count(),Is.EqualTo(1));Assert.That(ShaderUtil.ShaderHasError(Shader.Find("Hidden/EchoStation/LocalFog")),Is.False);Assert.That(PlayerSettings.allowHDRDisplaySupport,Is.False);Assert.That(QualitySettings.renderPipeline,Is.SameAs(pipeline));Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(pipeline));
  }
  [Test]public void P03_B02_BoundedFieldAndRecoveryValidationDoNotCreateGlobalFog()
  {
   var go=new GameObject("FogDataFixture");try{var f=go.AddComponent<LocalFogField>();Assert.That(f.ShouldRender(new Vector3(0,1,-25)),Is.False);Assert.That(f.ShouldRender(new Vector3(0,-2,30)),Is.True);Assert.That(f.LabBounds.Contains(new Vector3(0,-2,30)),Is.True);Assert.That(f.VentBounds.Contains(new Vector3(0,-2,48)),Is.True);Assert.That(f.LabDensity,Is.EqualTo(.025f));f.SetRecovery(1);Assert.That(f.LabDensity,Is.EqualTo(.008f).Within(1e-6));Assert.That(f.VentDensity,Is.EqualTo(.015f).Within(1e-6));Assert.Throws<ArgumentOutOfRangeException>(()=>f.SetRecovery(float.NaN));Assert.That(f.Recovery,Is.EqualTo(1));Assert.Throws<ArgumentOutOfRangeException>(()=>f.SetDensityScale(-1,1));Assert.That(LocalFogFeature.Steps,Is.EqualTo(16));}finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [UnityTest]public IEnumerator P03_B03_ActualHalfResolutionVolumeLightsRecoveryAndDepthOcclusion()
  {
   yield return new EnterPlayMode();yield return Boot();station.Player.enabled=false;var camera=station.Player.View.GetComponent<Camera>();camera.scene=scene;camera.aspect=16f/9f;camera.transform.position=new Vector3(0,-2.35f,26);camera.transform.LookAt(new Vector3(0,-2.4f,38));var data=camera.GetUniversalAdditionalCameraData();var pp=data.renderPostProcessing;data.renderPostProcessing=false;
   var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(StationLayout.DataFolder+"/ES_S2_ForwardRenderer.asset");var fog=renderer.rendererFeatures.OfType<LocalFogFeature>().Single();var features=renderer.rendererFeatures.Where(f=>f&&f.GetType().Name=="ScreenSpaceAmbientOcclusion").ToArray();var enabled=features.Select(f=>f.isActive).ToArray();foreach(var f in features)f.SetActive(false);
   var lights=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>()).Where(l=>l.lightmapBakeType==LightmapBakeType.Realtime).ToArray();var oldLights=lights.Select(l=>l.enabled).ToArray();foreach(var l in lights)l.enabled=false;
   var field=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LocalFogField>()).Single();var folder="Docs/Tests/Stage2/Evidence/S2-P03/B-Images";Directory.CreateDirectory(folder);GameObject cube=null;Material material=null;var result=new Evidence{steps=LocalFogFeature.Steps};
   try{using(var frames=new SceneFrameCapture(camera)){
    fog.SetActive(false);yield return R04ShadowProbe.Frame();var off=frames.Capture(folder+"/Off.png");fog.SetActive(true);yield return R04ShadowProbe.Frame();var on=frames.Capture(folder+"/On.png");result.fogDifference=SceneFrameCapture.Difference(off,on);Assert.That(result.fogDifference,Is.GreaterThan(.0001f));result.width=field.LastWidth;result.height=field.LastHeight;Assert.That(result.width,Is.EqualTo(960));Assert.That(result.height,Is.EqualTo(540));
    field.LightStrength=0;yield return R04ShadowProbe.Frame();result.lightDifference=SceneFrameCapture.Difference(on,frames.Capture(folder+"/LightsOff.png"));Assert.That(result.lightDifference,Is.GreaterThan(.0001f));field.LightStrength=1;field.SetRecovery(1);yield return R04ShadowProbe.Frame();result.recoveryDifference=SceneFrameCapture.Difference(on,frames.Capture(folder+"/Recovered.png"));Assert.That(result.recoveryDifference,Is.GreaterThan(.0001f));field.SetRecovery(0);
    field.SetDensityScale(0,1);var isolation=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>()).Single(t=>t.name=="Vent_Isolation");isolation.gameObject.SetActive(false);cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(field.transform,false);cube.transform.position=new Vector3(0,-2.2f,35);cube.transform.localScale=new Vector3(3,3,.3f);material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",Color.gray);cube.GetComponent<Renderer>().sharedMaterial=material;
    fog.SetActive(false);yield return R04ShadowProbe.Frame();var opaqueOff=frames.Capture(folder+"/DepthOff.png");fog.SetActive(true);yield return R04ShadowProbe.Frame();var opaqueOn=frames.Capture(folder+"/DepthOn.png");result.occludedRoiDifference=Difference(opaqueOff,opaqueOn,864,486,192,108);result.unoccludedDifference=SceneFrameCapture.Difference(opaqueOff,opaqueOn);Assert.That(result.occludedRoiDifference,Is.LessThan(1e-6f));Assert.That(result.unoccludedDifference,Is.GreaterThan(.0001f));isolation.gameObject.SetActive(true);
   }}finally{fog.SetActive(true);field.SetDensityScale(1,1);field.LightStrength=1;field.SetRecovery(0);for(var i=0;i<features.Length;i++)features[i].SetActive(enabled[i]);for(var i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=oldLights[i];data.renderPostProcessing=pp;if(cube)UnityEngine.Object.Destroy(cube);if(material)UnityEngine.Object.Destroy(material);}
   File.WriteAllText(folder+"/VolumeEvidence.json",JsonUtility.ToJson(result,true));
  }
 }
}
