using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Fixtures
{
    public sealed class CabinLightingProbe : IDisposable
    {
        [Serializable]public class Result
        {
            public bool passed,validLightmaps,hdr,displayHdrActive;public int atlases,probes;
            public float[] viewLuminance,probeMagnitude;public float baselineStableDifference,bakedLightsOffDifference,removedLightmapDifference,removedLightmapLuminance;
            public bool[] bakedOutputs;
        }
        private readonly Camera camera;private readonly UniversalAdditionalCameraData data;
        private readonly MeshRenderer[] dynamicPresentation;
        private readonly MeshRenderer[] geometry;private readonly Light[] baked;private readonly Light fault;
        private readonly int[] indices;private readonly Vector4[] offsets;private readonly ScriptableRendererFeature[] features;private readonly bool[] featureStates;
        private readonly RenderTexture target;private readonly Texture2D pixels;private readonly Vector3 originalPosition;private readonly Quaternion originalRotation;
        private readonly SphericalHarmonicsL2 originalAmbient;
        public Result Report{get;}=new Result();
        public CabinLightingProbe(Scene scene,UniversalRendererData rendererData)
        {
            var roots=scene.GetRootGameObjects();camera=roots.Single(o=>o.name=="Cabin_ValidationCamera").GetComponent<Camera>();camera.scene=scene;camera.aspect=16f/9f;
            data=camera.GetUniversalAdditionalCameraData();originalPosition=camera.transform.position;originalRotation=camera.transform.rotation;
            var root=roots.Single(o=>o.name=="ES_Cabin_Greybox");geometry=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.name.StartsWith("ES_Combined_")).ToArray();
            dynamicPresentation=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&!r.name.StartsWith("ES_Combined_")).ToArray();
            // Bake classification is Editor-only. The fixture owns an explicit baked-light group in both builds.
            baked=root.transform.Find("Cabin_Lighting").GetComponentsInChildren<Light>();fault=roots.Single(o=>o.name=="Cabin_Fault_Realtime").GetComponent<Light>();
            indices=geometry.Select(r=>r.lightmapIndex).ToArray();offsets=geometry.Select(r=>r.lightmapScaleOffset).ToArray();originalAmbient=RenderSettings.ambientProbe;
            features=rendererData.rendererFeatures.Where(f=>f&&f.GetType().Name=="ScreenSpaceAmbientOcclusion").ToArray();featureStates=features.Select(f=>f.isActive).ToArray();
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);target.Create();pixels=new Texture2D(1920,1080,TextureFormat.RGBAFloat,false,true);
            Report.atlases=LightmapSettings.lightmaps.Length;Report.probes=LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0;
            Report.validLightmaps=geometry.Length==8&&geometry.All(r=>r.lightmapIndex>=0&&r.lightmapIndex<Report.atlases);
            Report.hdr=camera.allowHDR&&((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsHDR;Report.displayHdrActive=HDROutputSettings.main.active;
        }
        private Color[] Capture(string path=null)
        {
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var old=RenderTexture.active;Color[] colors;
            try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0,false);pixels.Apply(false);colors=pixels.GetPixels();}finally{RenderTexture.active=old;}
            if(path!=null)
            {
                var png=new Texture2D(1920,1080,TextureFormat.RGB24,false,true);
                try{png.SetPixels32(colors.Select(c=>(Color32)c.gamma).ToArray());png.Apply(false);File.WriteAllBytes(path,png.EncodeToPNG());}finally{Object.Destroy(png);}
            }
            return colors;
        }
        private static float Mean(Color[] a){double v=0;foreach(var c in a)v+=.2126*c.r+.7152*c.g+.0722*c.b;return(float)(v/a.Length);}
        private static float Difference(Color[] a,Color[] b){double v=0;for(var i=0;i<a.Length;i++)v+=(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b))/3;return(float)(v/a.Length);}
        public IEnumerator Run(string directory)
        {
            Directory.CreateDirectory(directory);LightProbes.Tetrahedralize();Report.probeMagnitude=new float[3];
            var locations=new[]{new Vector3(1.5f,1.1f,-2),new Vector3(6.5f,1.1f,1),new Vector3(0,1.1f,-5.5f)};
            for(var i=0;i<locations.Length;i++)
            {
                LightProbes.GetInterpolatedProbe(locations[i],null,out var sh);float magnitude=0;
                for(var channel=0;channel<3;channel++)for(var coefficient=0;coefficient<9;coefficient++)magnitude+=Mathf.Abs(sh[channel,coefficient]);Report.probeMagnitude[i]=magnitude;
            }
            foreach(var feature in features)feature.SetActive(false);fault.enabled=false;data.renderPostProcessing=false;
            foreach(var renderer in dynamicPresentation)renderer.enabled=false;
            camera.transform.SetPositionAndRotation(originalPosition,originalRotation);
            for(var warm=0;warm<10;warm++)yield return R04ShadowProbe.Frame();
            Report.bakedOutputs=baked.Select(l=>l.bakingOutput.isBaked).ToArray();
            var preBaseline=Capture();yield return R04ShadowProbe.Frame();var baseline=Capture(Path.Combine(directory,"01_BakedControl.png"));
            Report.baselineStableDifference=Difference(preBaseline,baseline);
            foreach(var light in baked)light.enabled=false;yield return R04ShadowProbe.Frame();Report.bakedLightsOffDifference=Difference(baseline,Capture(Path.Combine(directory,"02_BakedLightsOff.png")));
            foreach(var renderer in geometry)renderer.lightmapIndex=-1;RenderSettings.ambientProbe=new SphericalHarmonicsL2();yield return R04ShadowProbe.Frame();
            var removed=Capture(Path.Combine(directory,"03_LightmapsRemoved.png"));Report.removedLightmapDifference=Difference(baseline,removed);Report.removedLightmapLuminance=Mean(removed);
            for(var i=0;i<geometry.Length;i++){geometry[i].lightmapIndex=indices[i];geometry[i].lightmapScaleOffset=offsets[i];}
            foreach(var light in baked)light.enabled=true;fault.enabled=true;data.renderPostProcessing=true;
            foreach(var renderer in dynamicPresentation)renderer.enabled=true;
            for(var i=0;i<features.Length;i++)features[i].SetActive(featureStates[i]);RenderSettings.ambientProbe=originalAmbient;
            var positions=new[]{originalPosition,new Vector3(5.7f,1.65f,-1.6f),new Vector3(0,1.6f,-6.2f)};
            var aims=new[]{new Vector3(-2,1.1f,2),new Vector3(8,1.1f,2.8f),new Vector3(0,1.4f,-3)};
            Report.viewLuminance=new float[3];
            for(var i=0;i<positions.Length;i++)
            {camera.transform.position=positions[i];camera.transform.LookAt(aims[i]);yield return R04ShadowProbe.Frame();Report.viewLuminance[i]=Mean(Capture(Path.Combine(directory,$"0{i+4}_RoomView.png")));}
            Report.passed=Report.validLightmaps&&Report.probes>=100&&Report.probeMagnitude.All(v=>v>1e-5f)&&Report.viewLuminance.All(v=>v>.01f)&&
                Report.baselineStableDifference<1e-6f&&Report.bakedOutputs.All(v=>v)&&Report.bakedLightsOffDifference<1e-6f&&Report.removedLightmapDifference>1e-4f&&Report.hdr&&!Report.displayHdrActive;
        }
        public void Dispose()
        {
            for(var i=0;i<geometry.Length;i++){geometry[i].lightmapIndex=indices[i];geometry[i].lightmapScaleOffset=offsets[i];}
            foreach(var light in baked)light.enabled=true;fault.enabled=true;data.renderPostProcessing=true;RenderSettings.ambientProbe=originalAmbient;
            foreach(var renderer in dynamicPresentation)if(renderer)renderer.enabled=true;
            for(var i=0;i<features.Length;i++)if(features[i])features[i].SetActive(featureStates[i]);camera.transform.SetPositionAndRotation(originalPosition,originalRotation);
            target.Release();Object.Destroy(target);Object.Destroy(pixels);
        }
    }
}
