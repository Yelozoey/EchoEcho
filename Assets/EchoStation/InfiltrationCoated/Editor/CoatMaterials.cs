using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace EchoStation.InfiltrationCoated.Editor
{
    public static class CoatMaterials
    {
        public static bool Painted(Renderer renderer,Material m)
        {
            if(!m||m.shader.name!="Universal Render Pipeline/Lit")return false;
            string n=m.name;if(new[]{"Screen","Glass","Sign","Label","Decal","Emit","_Light","Calm","Black","Dark"}.Any(k=>n.IndexOf(k,StringComparison.OrdinalIgnoreCase)>=0))return false;
            var art=renderer.GetComponentInParent<ArtSampleModel>();if(!art)return false;
            switch(art.Role)
            {
                case "Wall":case "Column":case "RoofStrut":case "EquipmentCase":case "Barrel":return n.Contains("Trim02");
                case "WallCap":case "Desk":case "Chair":case "Seat":return n.Contains("Trim03");
                case "Locker":return true;
                case "Terminal":case "Computer":return n.Contains("Trim01");
                case "Toolbox":return true;
                case "Door":return !n.Contains("Trim01");
                case "Floor":for(var t=renderer.transform;t;t=t.parent)if(t.name.StartsWith("Ceiling_",StringComparison.Ordinal))return true;return false;
                default:return false;
            }
        }
        static Texture2D CoatingAtlas(Texture source)
        {
            string from=AssetDatabase.GetAssetPath(source),path=CoatScene.Generated+"/CF_Coating_"+AssetDatabase.AssetPathToGUID(from).Substring(0,10)+".png";
            var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(existing)return existing;
            Texture2D input=null,output=null;
            try
            {
                input=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!input.LoadImage(File.ReadAllBytes(from)))throw new IOException("Read author atlas "+from);
                int w=Mathf.Min(512,input.width),h=Mathf.Min(512,input.height),sx=input.width/w,sy=input.height/h;if(sx*w!=input.width||sy*h!=input.height)throw new InvalidOperationException("Integer atlas resampling required");var data=input.GetPixels32();var pixels=new Color32[w*h];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++){float l=0;for(int dy=0;dy<sy;dy++)for(int dx=0;dx<sx;dx++){var c=data[(y*sy+dy)*input.width+x*sx+dx];l+=.2126f*c.r+.7152f*c.g+.0722f*c.b;}float v=.82f+(l/(sx*sy*255)-.5f)*.14f;pixels[y*w+x]=new Color32((byte)Mathf.RoundToInt(v*.97f*255),(byte)Mathf.RoundToInt(v*.985f*255),(byte)Mathf.RoundToInt(v*255),255);}
                output=new Texture2D(w,h,TextureFormat.RGBA32,false);output.SetPixels32(pixels);output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=true;importer.isReadable=false;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            finally{if(input)UnityEngine.Object.DestroyImmediate(input);if(output)UnityEngine.Object.DestroyImmediate(output);}
        }
        static Material Variant(Material source,bool coated)
        {
            string path=CoatScene.Generated+"/CF_"+(coated?"Coated_":"Original_")+source.name+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)).Substring(0,8)+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;var material=UnityEngine.Object.Instantiate(source);material.name=Path.GetFileNameWithoutExtension(path);
            if(coated){material.SetTexture("_BaseMap",CoatingAtlas(source.GetTexture("_BaseMap")));material.SetColor("_BaseColor",Color.white);material.SetFloat("_WorkflowMode",1);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.42f);material.SetTexture("_MetallicGlossMap",null);material.DisableKeyword("_METALLICSPECGLOSSMAP");material.DisableKeyword("_SPECULAR_SETUP");}
            AssetDatabase.CreateAsset(material,path);CoatScene.Save(material);return material;
        }
        public static void Apply(Transform root)
        {
            var rows=new JArray();foreach(var r in root.GetComponentsInChildren<Renderer>())
            {
                var source=r.sharedMaterials;var variants=source.Select(m=>Variant(m,Painted(r,m))).ToArray();r.sharedMaterials=variants;var art=r.GetComponentInParent<ArtSampleModel>();
                for(int i=0;i<source.Length;i++)rows.Add(new JObject{{"renderer",r.name},{"role",art?art.Role:""},{"slot",i},{"source",AssetDatabase.GetAssetPath(source[i])},{"finish",Painted(r,source[i])?"Coated":"Original"},{"material",AssetDatabase.GetAssetPath(variants[i])}});
            }
            File.WriteAllText(CoatScene.Evidence+"/MaterialAssignments_A.json",rows.ToString());
        }
    }
}
