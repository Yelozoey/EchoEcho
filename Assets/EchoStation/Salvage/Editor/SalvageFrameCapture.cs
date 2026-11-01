using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace EchoStation.Salvage.Editor
{
    public sealed class SalvageFrameCapture:IDisposable
    {
        readonly Camera camera;readonly RenderTexture target;readonly Texture2D pixels;readonly float originalAspect;
        public SalvageFrameCapture(Camera camera)
        {this.camera=camera;originalAspect=camera.aspect;camera.aspect=1920f/1080;target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);target.Create();pixels=new Texture2D(1920,1080,TextureFormat.RGBAFloat,false,true);}
        public Color[] Capture(string path)
        {
            var active=RenderTexture.active;var previous=camera.targetTexture;var type=camera.cameraType;Color[] result;
            try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0,false);pixels.Apply(false);result=pixels.GetPixels();}
            finally{camera.targetTexture=previous;camera.cameraType=type;RenderTexture.active=active;}
            var png=new Texture2D(1920,1080,TextureFormat.RGB24,false,true);
            try{png.SetPixels32(result.Select(c=>(Color32)c.gamma).ToArray());png.Apply(false);File.WriteAllBytes(path,png.EncodeToPNG());}
            finally{Object.DestroyImmediate(png);}return result;
        }
        public static float Difference(Color[] a,Color[] b){double sum=0;for(int i=0;i<a.Length;i++)sum+=(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b))/3;return(float)(sum/a.Length);}
        public void Dispose(){if(camera)camera.aspect=originalAspect;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);}
    }
}
