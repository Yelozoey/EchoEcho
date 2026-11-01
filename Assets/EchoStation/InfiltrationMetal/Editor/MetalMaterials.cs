using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace EchoStation.InfiltrationMetal.Editor
{
    public static class MetalMaterials
    {
        static readonly string[] RoughRoles={"Wall","WallCap","DoorFrame","Column","Door","EquipmentCase","Toolbox","Terminal","Locker","Shelf","Barrel","Vent","Fan","Cable","Computer","Guard","Echo","CeilingLight","Desk"};
        public static bool Protected(Material m)
        {string n=m.name;return m.shader.name!="Universal Render Pipeline/Lit"||new[]{"Screen","Glass","Sign","Label","Decal","Emit","_Light","Calm","_Black"}.Any(k=>n.IndexOf(k,StringComparison.OrdinalIgnoreCase)>=0);}
        public static MetalFinish Choose(Renderer renderer,Material material)
        {
            if(Protected(material))return MetalFinish.Preserved;var art=renderer.GetComponentInParent<ArtSampleModel>();if(!art)return MetalFinish.Preserved;
            if(art.Role=="Terminal"&&material.name.Contains("Trim01"))return MetalFinish.Polished;
            if(art.Role=="Floor"){bool roof=false;for(var t=renderer.transform;t;t=t.parent)if(t.name.StartsWith("Ceiling_",StringComparison.Ordinal))roof=true;return roof?MetalFinish.Rough:MetalFinish.Preserved;}
            if((art.Role=="Chair"||art.Role=="Seat")&&!material.name.Contains("Dark"))return MetalFinish.Rough;
            return RoughRoles.Contains(art.Role)?MetalFinish.Rough:MetalFinish.Preserved;
        }
        static Texture2D Mask(Texture texture,MetalFinish finish)
        {
            string source=AssetDatabase.GetAssetPath(texture);if(!source.EndsWith(".png",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Expected authored packed PNG: "+source);
            string path=MetalScene.Generated+"/MM_"+finish+"_"+AssetDatabase.AssetPathToGUID(source).Substring(0,10)+".png";var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(existing)return existing;
            Texture2D input=null,output=null;
            try
            {
                input=new Texture2D(2,2,TextureFormat.RGBA32,false,true);if(!input.LoadImage(File.ReadAllBytes(source)))throw new InvalidDataException(source);
                int width=Mathf.Min(1024,input.width),height=Mathf.Min(1024,input.height),sx=input.width/width,sy=input.height/height;if(sx*width!=input.width||sy*height!=input.height)throw new InvalidDataException("Expected integer mask resampling");var original=input.GetPixels32();var pixels=new Color32[width*height];
                for(int y=0;y<height;y++)for(int x=0;x<width;x++){int g=0,b=0,a=0,n=sx*sy;for(int dy=0;dy<sy;dy++)for(int dx=0;dx<sx;dx++){var p=original[(y*sy+dy)*input.width+x*sx+dx];g+=p.g;b+=p.b;a+=p.a;}pixels[y*width+x]=new Color32(finish==MetalFinish.Polished?(byte)255:(byte)232,(byte)Mathf.RoundToInt((float)g/n),(byte)Mathf.RoundToInt((float)b/n),finish==MetalFinish.Polished?(byte)220:(byte)Mathf.Clamp(Mathf.RoundToInt((float)a/n*.6f+45),90,140));}
                output=new Texture2D(width,height,TextureFormat.RGBA32,false,true);output.SetPixels32(pixels);output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.maxTextureSize=1024;importer.isReadable=false;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            finally{if(input)Object.DestroyImmediate(input);if(output)Object.DestroyImmediate(output);}
        }
        public static Material Variant(Material source,MetalFinish finish)
        {
            if(!source)throw new InvalidDataException("Material missing");string sourcePath=AssetDatabase.GetAssetPath(source);string guid=AssetDatabase.AssetPathToGUID(sourcePath);if(guid.Length<8)throw new InvalidOperationException("Persisted source required");
            string name="MM_"+finish+"_"+source.name.Replace(' ','_').Replace('.','_')+"_"+guid.Substring(0,8);string path=MetalScene.Generated+"/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;
            var own=Object.Instantiate(source);own.name=name;
            if(finish!=MetalFinish.Preserved)
            {
                own.SetFloat("_Metallic",finish==MetalFinish.Polished?1:.91f);var texture=source.GetTexture("_MetallicGlossMap");
                if(texture){own.SetTexture("_MetallicGlossMap",Mask(texture,finish));own.EnableKeyword("_METALLICSPECGLOSSMAP");own.SetFloat("_Smoothness",1);}
                else{own.DisableKeyword("_METALLICSPECGLOSSMAP");own.SetFloat("_Smoothness",finish==MetalFinish.Polished?.86f:.46f);}
                own.SetFloat("_EnvironmentReflections",1);own.SetFloat("_SpecularHighlights",1);own.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");own.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            }
            AssetDatabase.CreateAsset(own,path);MetalScene.Save(own);return own;
        }
        public static void Apply(Transform root)
        {
            var rows=new JArray();foreach(var r in root.GetComponentsInChildren<Renderer>())
            {
                var source=r.sharedMaterials;var variants=source.Select(m=>Variant(m,Choose(r,m))).ToArray();r.sharedMaterials=variants;var art=r.GetComponentInParent<ArtSampleModel>();
                for(int i=0;i<source.Length;i++)rows.Add(new JObject{{"renderer",r.name},{"role",art?art.Role:""},{"source",AssetDatabase.GetAssetPath(source[i])},{"finish",Choose(r,source[i]).ToString()},{"material",AssetDatabase.GetAssetPath(variants[i])}});
            }
            File.WriteAllText(MetalScene.Evidence+"/MaterialAssignments_A.json",rows.ToString());
        }
    }
}
