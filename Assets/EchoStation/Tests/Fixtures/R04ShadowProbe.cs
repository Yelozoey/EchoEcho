using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EchoStation.Tests.Fixtures
{
    public sealed class R04ShadowProbe : IDisposable
    {
        [Serializable] public class DistanceSample
        {
            public float distance, imageDifference, litMean, actualVisibilityDifference;
            public int frame; public bool castsShadow, visible;
        }
        [Serializable] public class Result
        {
            public bool passed, hdr, displayHdrActive;
            public string unity, api, cpu, gpu, os;
            public int ramMb, vramMb;
            public float mainShadowDifference, mainLightDifference, spotLightDifference, spotShadowDifference;
            public int[] cascadePixelCounts;
            public float[] cascadePixelsPerMeter;
            public Vector4 cascadeSphereRadii;
            public DistanceSample[] distances;
        }
        private readonly Camera camera;
        private readonly Renderer caster, receiver;
        private readonly Light main, spot;
        private readonly DistanceShadowCaster policy;
        private readonly Material material;
        private readonly RenderTexture target;
        private readonly Texture2D pixels;
        private readonly ScriptableRendererFeature[] features;
        private readonly bool[] featureStates;
        private readonly Vector3 cameraPosition;
        private readonly Quaternion cameraRotation;
        public Camera Camera => camera;
        public Renderer Caster => caster;
        public Result Report { get; } = new Result();

        public R04ShadowProbe(Scene scene, UniversalRendererData rendererData)
        {
            var roots = scene.GetRootGameObjects();
            camera = roots.Single(o => o.name == "R04_ValidationCamera").GetComponent<Camera>(); camera.scene = scene; camera.aspect = 16f/9f;
            cameraPosition = camera.transform.position; cameraRotation = camera.transform.rotation;
            caster = roots.Single(o => o.name == "R04_Caster").GetComponent<Renderer>();
            receiver = roots.Single(o => o.name == "R04_Receiver").GetComponent<Renderer>();
            main = roots.Single(o => o.name == "R04_Main").GetComponent<Light>();
            spot = roots.Single(o => o.name == "R04_Spot").GetComponent<Light>();
            policy = caster.GetComponent<DistanceShadowCaster>(); policy.enabled = false;
            material = receiver.material;
            features = rendererData.rendererFeatures.Where(f => f && f.GetType().Name == "ScreenSpaceAmbientOcclusion").ToArray();
            featureStates = features.Select(f => f.isActive).ToArray(); foreach (var f in features) f.SetActive(false);
            target = new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear); target.Create();
            pixels = new Texture2D(1920,1080,TextureFormat.RGBAFloat,false,true);
            Report.unity = Application.unityVersion; Report.api = SystemInfo.graphicsDeviceType.ToString(); Report.cpu = SystemInfo.processorType;
            Report.gpu = SystemInfo.graphicsDeviceName; Report.os = SystemInfo.operatingSystem;
            Report.ramMb = SystemInfo.systemMemorySize; Report.vramMb = SystemInfo.graphicsMemorySize;
            Report.hdr = camera.allowHDR && ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsHDR;
            Report.displayHdrActive = HDROutputSettings.main.active;
        }

        public void Render() => RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
        public void RenderAndSynchronize() => Capture();
        private Color[] Capture(string path = null)
        {
            Render(); var previous = RenderTexture.active;
            Color[] colors;
            try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0,0,1920,1080),0,0,false); pixels.Apply(false); colors = pixels.GetPixels(); }
            finally { RenderTexture.active = previous; }
            if (path != null)
            {
                var png = new Texture2D(1920,1080,TextureFormat.RGB24,false,true);
                try { png.SetPixels32(colors.Select(c => (Color32)c.gamma).ToArray()); png.Apply(false); File.WriteAllBytes(path,png.EncodeToPNG()); }
                finally { Object.Destroy(png); }
            }
            return colors;
        }
        private static float Difference(Color[] a, Color[] b)
        {
            double sum=0; for(var i=0;i<a.Length;i++) sum += (Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b))/3;
            return (float)(sum/a.Length);
        }
        private static float Mean(Color[] a) { double sum=0; foreach(var c in a) sum += .2126*c.r+.7152*c.g+.0722*c.b; return (float)(sum/a.Length); }
        public static IEnumerator Frame()
        {
            var start=Time.frameCount; var timer=System.Diagnostics.Stopwatch.StartNew();
            while(Time.frameCount==start) { if(timer.Elapsed.TotalSeconds>5) throw new TimeoutException("No real game frame advanced."); yield return null; }
        }

        public IEnumerator Run(string directory)
        {
            Directory.CreateDirectory(directory); spot.enabled=false; main.enabled=true; caster.shadowCastingMode=ShadowCastingMode.On;
            yield return Frame(); var shadowed=Capture(Path.Combine(directory,"01_MainShadow.png"));
            caster.shadowCastingMode=ShadowCastingMode.Off; yield return Frame(); var unshadowed=Capture(Path.Combine(directory,"02_MainNoShadow.png"));
            Report.mainShadowDifference=Difference(shadowed,unshadowed); main.enabled=false; yield return Frame();
            Report.mainLightDifference=Difference(unshadowed,Capture(Path.Combine(directory,"03_LightsOff.png")));
            spot.enabled=true; caster.shadowCastingMode=ShadowCastingMode.On; yield return Frame(); var spotShadow=Capture(Path.Combine(directory,"04_SpotShadow.png"));
            caster.shadowCastingMode=ShadowCastingMode.Off; yield return Frame(); var spotNoShadow=Capture(Path.Combine(directory,"05_SpotNoShadow.png"));
            Report.spotShadowDifference=Difference(spotShadow,spotNoShadow); spot.enabled=false; yield return Frame();
            Report.spotLightDifference=Difference(spotNoShadow,Capture());
            main.enabled=true; caster.shadowCastingMode=ShadowCastingMode.On;
            camera.transform.position=new Vector3(0,.7f,0); camera.transform.rotation=Quaternion.Euler(12,0,0);
            material.SetFloat("_DebugCascade",1); yield return Frame(); var debug=Capture(Path.Combine(directory,"06_CascadeIDs.png"));
            Report.cascadePixelCounts=new int[5];
            foreach(var color in debug)
            {
                if(color.g>.00001f || color.b>.00001f || color.r<.1f) continue;
                var index=Mathf.RoundToInt(color.r*5)-1;
                if(index>=0 && index<5 && Mathf.Abs(color.r-(index+1)*.2f)<.003f) Report.cascadePixelCounts[index]++;
            }
            var matrices=Shader.GetGlobalMatrixArray("_MainLightWorldToShadow");
            Report.cascadePixelsPerMeter=matrices.Take(4).Select(m=>new Vector3(m.m00,m.m01,m.m02).magnitude*2048).ToArray();
            Report.cascadeSphereRadii=Shader.GetGlobalVector("_CascadeShadowSplitSphereRadii");
            material.SetFloat("_DebugCascade",0);
            var samples=new List<DistanceSample>();
            foreach(var distance in new[]{3f,3.9f,4.1f,9.9f,10.1f,19.9f,20.1f,29f,31f,39f,41f})
            {
                camera.transform.position=new Vector3(0,1.4f,7-distance); camera.transform.LookAt(new Vector3(0,0,8));
                policy.enabled=true; policy.Evaluate(camera.transform.position); yield return Frame();
                var enabledMode=caster.shadowCastingMode;
                var actual=Capture(distance==31 ? Path.Combine(directory,"07_FarStillVisible.png"):null);
                var row=new DistanceSample{distance=distance,frame=Time.frameCount,castsShadow=enabledMode!=ShadowCastingMode.Off,
                    visible=caster.enabled,litMean=Mean(actual)};
                // Keep camera constant and compare actual shadow budget with explicit Off.
                caster.shadowCastingMode=ShadowCastingMode.Off; yield return Frame(); row.imageDifference=Difference(actual,Capture());
                if(distance>=31)
                {
                    caster.enabled=false; yield return Frame(); row.actualVisibilityDifference=Difference(actual,Capture());
                    caster.enabled=true; yield return Frame();
                }
                caster.shadowCastingMode=enabledMode; samples.Add(row); policy.enabled=false;
            }
            Report.distances=samples.ToArray();
            Report.passed=Report.mainShadowDifference>1e-5f && Report.mainLightDifference>.01f && Report.spotLightDifference>1e-4f &&
                Report.spotShadowDifference>1e-5f && Report.cascadePixelCounts.Take(4).All(n=>n>100) &&
                Report.cascadePixelsPerMeter.Length==4 && Report.cascadePixelsPerMeter.All(v=>v>0) &&
                Enumerable.Range(0,3).All(i=>Report.cascadePixelsPerMeter[i]>Report.cascadePixelsPerMeter[i+1]) &&
                samples.Where(s=>s.distance<=20.1f).All(s=>s.castsShadow && s.imageDifference>1e-6f && s.litMean>.01f) &&
                samples.Where(s=>s.distance>=31).All(s=>!s.castsShadow && s.visible && s.imageDifference<1e-6f && s.actualVisibilityDifference>1e-6f) && Report.hdr && !Report.displayHdrActive;
            camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation); caster.shadowCastingMode=ShadowCastingMode.On;
            main.enabled=true; spot.enabled=false; yield return Frame(); Capture(Path.Combine(directory,"08_Final.png"));
        }
        public void Dispose()
        {
            for(var i=0;i<features.Length;i++) if(features[i]) features[i].SetActive(featureStates[i]);
            camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation); policy.enabled=true; main.enabled=true; spot.enabled=true;
            Object.Destroy(material); target.Release(); Object.Destroy(target); Object.Destroy(pixels);
        }
    }
}
