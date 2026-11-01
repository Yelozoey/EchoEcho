using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Fixtures
{
    public sealed class SceneFrameCapture : IDisposable
    {
        private readonly Camera camera;private readonly RenderTexture target;private readonly Texture2D pixels;
        public SceneFrameCapture(Camera camera)
        {
            this.camera=camera;target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);target.Create();
            pixels=new Texture2D(1920,1080,TextureFormat.RGBAFloat,false,true);
        }
        public void RenderAndSynchronize()=>Capture();
        public Color[] Capture(string path=null)
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
        public static float Difference(Color[] a,Color[] b){double sum=0;for(var i=0;i<a.Length;i++)sum+=(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b))/3;return(float)(sum/a.Length);}
        public void Dispose(){target.Release();Object.Destroy(target);Object.Destroy(pixels);}
    }
}
