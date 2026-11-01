using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace EchoStation.Salvage.Editor
{
    // Asset preparation only; never alters project-wide rendering or gameplay.
    public static class SalvageAssetImport
    {
        public const string Root="Assets/EchoStation/Salvage", Generated=Root+"/Generated";
        public const string FloorModel=Root+"/ThirdParty/Quaternius/FBX/Platforms/Platform_Simple.fbx";
        public const string BoxModel=Root+"/ThirdParty/PolyHaven/MetalToolbox/metal_toolbox_1k.fbx";
        const string FloorTex=Root+"/ThirdParty/Quaternius/Textures/T_Trim_03_";
        const string BoxTex=Root+"/ThirdParty/PolyHaven/MetalToolbox/textures/metal_toolbox_";
        public static string Model(int i)=>i==0?FloorModel:BoxModel;
        public static string BaseMap(int i)=>i==0?FloorTex+"BaseColor.png":BoxTex+"diff_1k.jpg";
        public static string Normal(int i)=>i==0?FloorTex+"Normal.png":BoxTex+"nor_gl_1k.exr";
        public static string SourceMask(int i)=>i==0?FloorTex+"ORM.png":BoxTex+"arm_1k.png";
        public static string Mask(int i)=>Generated+(i==0?"/ES_Platform_Mask.png":"/ES_Toolbox_Mask.png");
        public static string MaterialPath(int i)=>Generated+(i==0?"/ES_Platform_URP.mat":"/ES_Toolbox_URP.mat");

        [MenuItem("Tools/EchoStation/Salvage/R0-A02/Import Materials")]
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean(); EnsureFolder(Generated);
            for(int i=0;i<2;i++)
            {
                var m=AssetImporter.GetAtPath(Model(i)) as ModelImporter;
                if(m==null)throw new InvalidOperationException("Missing model: "+Model(i));
                m.globalScale=1; m.useFileScale=true; m.importCameras=false; m.importLights=false;
                m.importAnimation=false; m.materialImportMode=ModelImporterMaterialImportMode.None;
                // Unity 6000.6 automatic UV2 removes three small toolbox submeshes.
                // Portable objects use probes; only the static floor needs lightmap UVs.
                m.addCollider=false; m.generateSecondaryUV=i==0; m.isReadable=false;
                m.importNormals=ModelImporterNormals.Import; m.importTangents=ModelImporterTangents.CalculateMikk;
                m.SaveAndReimport();
                ConfigureTexture(BaseMap(i),true,false); ConfigureTexture(Normal(i),false,true);
                ConfigureTexture(SourceMask(i),false,false); WriteMask(SourceMask(i),Mask(i));
                AssetDatabase.ImportAsset(Mask(i),ImportAssetOptions.ForceSynchronousImport);
                ConfigureTexture(Mask(i),false,false);
                var mat=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(i));
                if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,MaterialPath(i));}
                mat.SetTexture("_BaseMap",LoadTexture(BaseMap(i))); mat.SetTexture("_BumpMap",LoadTexture(Normal(i)));
                mat.SetTexture("_MetallicGlossMap",LoadTexture(Mask(i))); mat.SetTexture("_OcclusionMap",LoadTexture(Mask(i)));
                mat.SetColor("_BaseColor",Color.white);
                foreach(var key in new[]{"_Smoothness","_BumpScale","_OcclusionStrength","_Metallic"})mat.SetFloat(key,1);
                mat.SetFloat("_Cull",i==0?(float)CullMode.Off:(float)CullMode.Back);
                foreach(var key in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP"})mat.EnableKeyword(key);
                EditorUtility.SetDirty(mat); AssetDatabase.SaveAssetIfDirty(mat);
            }
        }
        static Texture2D LoadTexture(string path)=>AssetDatabase.LoadAssetAtPath<Texture2D>(path)?AssetDatabase.LoadAssetAtPath<Texture2D>(path):throw new InvalidOperationException(path);
        static void ConfigureTexture(string path,bool srgb,bool normal)
        {
            var t=AssetImporter.GetAtPath(path) as TextureImporter;
            if(t==null)throw new InvalidOperationException(path);
            t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            t.sRGBTexture=srgb; t.isReadable=false; t.mipmapEnabled=true; t.maxTextureSize=1024;
            t.textureCompression=TextureImporterCompression.CompressedHQ; t.wrapMode=TextureWrapMode.Repeat; t.anisoLevel=4;t.SaveAndReimport();
        }
        static void WriteMask(string source,string target)
        {
            Texture2D input=null,output=null;
            try
            {
                input=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                if(!input.LoadImage(File.ReadAllBytes(source)))throw new InvalidDataException(source);
                var pixels=input.GetPixels32();
                for(int n=0;n<pixels.Length;n++){var p=pixels[n];pixels[n]=new Color32(p.b,p.r,0,(byte)(255-p.g));}
                output=new Texture2D(input.width,input.height,TextureFormat.RGBA32,false,true);
                output.SetPixels32(pixels);output.Apply();File.WriteAllBytes(target,output.EncodeToPNG());
            }
            finally{if(input)Object.DestroyImmediate(input);if(output)Object.DestroyImmediate(output);}
        }
        public static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            var parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        public static Bounds BoundsOf(GameObject instance)
        {
            var rs=instance.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new InvalidDataException("Model has no renderers.");
            var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
        }
    }
}
