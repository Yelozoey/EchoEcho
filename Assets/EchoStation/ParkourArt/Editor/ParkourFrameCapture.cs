using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace EchoStation.ParkourArt.Editor
{
    // Local capture owns its temporary GPU resources in both EditMode and PlayMode.
    public sealed class ParkourFrameCapture : IDisposable
    {
        readonly Camera camera;readonly RenderTexture target;readonly Texture2D pixels;readonly float originalAspect;
        public ParkourFrameCapture(Camera camera,int width=1920,int height=1080)
        {
            this.camera=camera;originalAspect=camera.aspect;camera.aspect=(float)width/height;
            target=new RenderTexture(width,height,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);target.Create();
            pixels=new Texture2D(width,height,TextureFormat.RGBAFloat,false,true);
        }
        public Color[] Capture(string path=null)
        {
            var previous=RenderTexture.active;var previousTarget=camera.targetTexture;var previousType=camera.cameraType;Color[] data;
            try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false);pixels.Apply(false);data=pixels.GetPixels();}
            finally{camera.targetTexture=previousTarget;camera.cameraType=previousType;RenderTexture.active=previous;}
            if(path!=null){var png=new Texture2D(target.width,target.height,TextureFormat.RGB24,false,true);try{png.SetPixels32(data.Select(c=>(Color32)c.gamma).ToArray());png.Apply(false);File.WriteAllBytes(path,png.EncodeToPNG());}finally{Release(png);}}
            return data;
        }
        public static float Difference(Color[] a,Color[] b){double sum=0;for(int i=0;i<a.Length;i++)sum+=(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b))/3;return(float)(sum/a.Length);}
        static void Release(Object value){if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
        public void Dispose(){if(camera)camera.aspect=originalAspect;target.Release();Release(target);Release(pixels);}
    }
}
