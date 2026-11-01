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
    public sealed class R05OptimizationProbe : IDisposable
    {
        [Serializable] public class Result {public bool passed;public float occlusionImageDifference,farRoomVisibilityDifference;}
        public Camera Camera {get;}
        public Renderer[] FarObjects {get;}
        public Result Report {get;}=new Result();
        private readonly RenderTexture target;
        private readonly Texture2D pixels;
        private readonly ScriptableRendererFeature[] features;
        private readonly bool[] featureStates;
        private readonly Vector3 originalPosition;
        private readonly Quaternion originalRotation;
        public R05OptimizationProbe(Scene scene,UniversalRendererData rendererData)
        {
            var roots=scene.GetRootGameObjects();Camera=roots.Single(o=>o.name=="R05_ValidationCamera").GetComponent<Camera>();Camera.scene=scene;Camera.aspect=16f/9f;
            FarObjects=roots.Single(o=>o.name=="R05_FarRoom").GetComponentsInChildren<Renderer>();
            originalPosition=Camera.transform.position;originalRotation=Camera.transform.rotation;
            features=rendererData.rendererFeatures.Where(f=>f && f.GetType().Name=="ScreenSpaceAmbientOcclusion").ToArray();featureStates=features.Select(f=>f.isActive).ToArray();
            foreach(var feature in features)feature.SetActive(false);
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);target.Create();
            pixels=new Texture2D(1920,1080,TextureFormat.RGBAFloat,false,true);
        }
        public void RenderAndSynchronize()=>Capture();
        public Color[] Capture(string path=null)
        {
            RenderPipeline.SubmitRenderRequest(Camera,new RenderPipeline.StandardRequest{destination=target});var old=RenderTexture.active;Color[] colors;
            try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0,false);pixels.Apply(false);colors=pixels.GetPixels();}
            finally{RenderTexture.active=old;}
            if(path!=null)
            {
                var png=new Texture2D(1920,1080,TextureFormat.RGB24,false,true);
                try{png.SetPixels32(colors.Select(c=>(Color32)c.gamma).ToArray());png.Apply(false);File.WriteAllBytes(path,png.EncodeToPNG());}
                finally{Object.Destroy(png);}
            }
            return colors;
        }
        private static float Difference(Color[] a,Color[] b)
        {double sum=0;for(var i=0;i<a.Length;i++)sum+=(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b))/3;return(float)(sum/a.Length);}
        public void NearView(bool occlusion)
        {Camera.transform.SetPositionAndRotation(new Vector3(0,1.6f,-4),Quaternion.identity);Camera.useOcclusionCulling=occlusion;}
        public void FarView(bool occlusion)
        {Camera.transform.SetPositionAndRotation(new Vector3(0,1.6f,5.5f),Quaternion.identity);Camera.useOcclusionCulling=occlusion;}
        public IEnumerator RunOcclusionImages(string directory)
        {
            Directory.CreateDirectory(directory);NearView(false);yield return R04ShadowProbe.Frame();var off=Capture(Path.Combine(directory,"01_OcclusionOff.png"));
            NearView(true);yield return R04ShadowProbe.Frame();Report.occlusionImageDifference=Difference(off,Capture(Path.Combine(directory,"02_OcclusionOn.png")));
            FarView(true);yield return R04ShadowProbe.Frame();var inside=Capture(Path.Combine(directory,"03_InsideFarRoom.png"));
            foreach(var r in FarObjects)r.enabled=false;yield return R04ShadowProbe.Frame();
            Report.farRoomVisibilityDifference=Difference(inside,Capture(Path.Combine(directory,"04_FarObjectsHidden.png")));
            foreach(var r in FarObjects)r.enabled=true;
            Report.passed=Report.occlusionImageDifference<1e-6f && Report.farRoomVisibilityDifference>.001f;
        }
        public void Dispose()
        {
            foreach(var r in FarObjects)if(r)r.enabled=true;
            for(var i=0;i<features.Length;i++)if(features[i])features[i].SetActive(featureStates[i]);
            Camera.transform.SetPositionAndRotation(originalPosition,originalRotation);Camera.useOcclusionCulling=true;
            target.Release();Object.Destroy(target);Object.Destroy(pixels);
        }
    }
}
