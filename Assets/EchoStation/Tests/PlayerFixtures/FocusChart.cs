using System;
using UnityEngine;

namespace EchoStation.Tests.PlayerFixtures
{
    public sealed class FocusChart:IDisposable
    {
        private readonly Material material;private readonly Texture2D texture;private readonly GameObject near,far;
        public FocusChart(Material source=null)
        {
            texture=new Texture2D(512,512,TextureFormat.RGB24,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};var colors=new Color32[512*512];for(var y=0;y<512;y++)for(var x=0;x<512;x++)colors[y*512+x]=((x/4+y/4)%2)==0?new Color32(220,220,220,255):new Color32(20,20,20,255);texture.SetPixels32(colors);texture.Apply();material=source?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetTexture("_BaseMap",texture);material.SetFloat("_Cull",0);far=Quad("FocusChartFar",new Vector3(0,0,8),new Vector3(16,10,1));near=Quad("FocusChartNear",new Vector3(-.3f,0,1),new Vector3(.5f,.6f,1));
        }
        private GameObject Quad(string name,Vector3 position,Vector3 scale){var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;go.layer=30;go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.Destroy(go.GetComponent<Collider>());return go;}
        public static float Contrast(Color[] pixels,int width,int x0,int y0,int x1,int y1)
        {double sum=0;var count=0;for(var y=y0;y<y1;y++)for(var x=x0;x<x1;x++){sum+=Math.Abs(pixels[y*width+x].r-pixels[y*width+x+1].r);count++;}return(float)(sum/count);}
        public void Dispose(){UnityEngine.Object.Destroy(near);UnityEngine.Object.Destroy(far);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(texture);}
    }
}
