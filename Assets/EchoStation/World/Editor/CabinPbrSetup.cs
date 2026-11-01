using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.World.Editor
{
    public static class CabinPbrSetup
    {
        public const string Folder="Assets/EchoStation/ThirdParty/PolyHaven";
        public const string Evidence="Docs/Tests/Stage1/Evidence/S1-W02";
        public const string Prefabs="Assets/EchoStation/World/Prefabs";
        public static readonly string[] Assets={"concrete_floor_02","painted_plaster_wall"};
        [MenuItem("Tools/EchoStation/Stage 1/W02/Bake Cabin PBR Lighting")]
        public static void Bake()=>CabinLightingBake.StartBakeTo(Evidence);
        public static Color32 PackMask(Color32 arm)=>new Color32(arm.b,arm.r,0,(byte)(255-arm.g));
        public static Vector2 MeterUv(Vector3 point,Vector3 normal)
        {
            var n=new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z));
            if(n.y>=n.x&&n.y>=n.z)return new Vector2(point.x,point.z)*.5f;
            return(n.x>=n.z?new Vector2(-point.z,point.y):new Vector2(point.x,point.y))*.5f;
        }
        [MenuItem("Tools/EchoStation/Stage 1/W02/Import Cabin PBR And Room Prefabs")]
        public static void Import()
        {
            SceneSafetyGuard.RequireClean();if(Lightmapping.isRunning)throw new InvalidOperationException("Wait for bake.");
            Directory.CreateDirectory(Evidence);CabinGreyboxSetup.EnsureFolder(Folder);CabinGreyboxSetup.EnsureFolder(Prefabs);
            if(!File.Exists(Evidence+"/Cabin-BeforePBR.unity.txt"))File.Copy(CabinGreyboxSetup.ScenePath,Evidence+"/Cabin-BeforePBR.unity.txt");
            foreach(var id in Assets)ImportSet(id);
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty)throw new InvalidOperationException("Unsaved cabin: preserving changes.");SceneManager.SetActiveScene(scene);
                var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");
                var meshes=root.GetComponentsInChildren<MeshFilter>();
                foreach(var mf in meshes)
                {
                    var material=mf.GetComponent<MeshRenderer>().sharedMaterial;
                    var id=material.name.Contains("Wall")||material.name.Contains("plaster")?Assets[1]:material.name.Contains("Floor")||material.name.Contains("concrete")?Assets[0]:null;
                    if(id==null)continue;
                    mf.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/ES_{id}.mat");
                    var mesh=mf.sharedMesh;var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                    for(var i=0;i<uv.Length;i++)uv[i]=MeterUv(mf.transform.TransformPoint(vertices[i]),mf.transform.TransformDirection(normals[i]));
                    mesh.uv=uv;mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
                }
                foreach(Transform room in root.transform)
                {
                    if(!room.name.StartsWith("Cabin_Room_"))continue;var path=$"{Prefabs}/ES_{room.name}.prefab";
                    if(AssetDatabase.LoadAssetAtPath<GameObject>(path))continue;
                    var copy=Object.Instantiate(room.gameObject);copy.name=room.name;
                    try
                    {
                        foreach(var distance in copy.GetComponentsInChildren<DistanceShadowCaster>())Object.DestroyImmediate(distance);
                        foreach(var renderer in copy.GetComponentsInChildren<MeshRenderer>()){renderer.lightmapIndex=-1;renderer.lightmapScaleOffset=new Vector4(1,1,0,0);}
                        PrefabUtility.SaveAsPrefabAsset(copy,path);
                    }
                    finally{Object.DestroyImmediate(copy);}
                }
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        private static void ImportSet(string id)
        {
            var source="Docs/Assets/Sources/PolyHaven/"+id;
            foreach(var kind in new[]{"BaseColor","Normal"})
            {
                var path=$"{Folder}/ES_{id}_{kind}.png";if(!File.Exists(path))File.Copy(source+"_"+kind+".png",path);
                AssetDatabase.ImportAsset(path);ConfigureTexture(path,kind=="BaseColor",kind=="Normal");
            }
            var maskPath=$"{Folder}/ES_{id}_Mask.png";
            if(!File.Exists(maskPath))
            {
                var raw=new Texture2D(2,2,TextureFormat.RGBA32,false,true);Texture2D mask=null;
                try
                {
                    if(!raw.LoadImage(File.ReadAllBytes(source+"_ARM.png")))throw new InvalidOperationException("Invalid source ARM.");
                    mask=new Texture2D(raw.width,raw.height,TextureFormat.RGBA32,false,true);mask.SetPixels32(raw.GetPixels32().Select(PackMask).ToArray());mask.Apply();File.WriteAllBytes(maskPath,mask.EncodeToPNG());
                }
                finally{Object.DestroyImmediate(raw);if(mask)Object.DestroyImmediate(mask);}
            }
            AssetDatabase.ImportAsset(maskPath);ConfigureTexture(maskPath,false,false);
            var materialPath=$"{Folder}/ES_{id}.mat";if(AssetDatabase.LoadAssetAtPath<Material>(materialPath))return;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="ES_"+id};
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/ES_{id}_BaseColor.png"));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/ES_{id}_Normal.png"));
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));material.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));
            material.SetColor("_BaseColor",id==Assets[1]?new Color(.75f,.8f,.85f):new Color(.5f,.57f,.65f));
            material.SetFloat("_BumpScale",.35f);material.SetFloat("_Smoothness",.5f);material.SetFloat("_OcclusionStrength",.5f);
            material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_METALLICSPECGLOSSMAP");material.EnableKeyword("_OCCLUSIONMAP");AssetDatabase.CreateAsset(material,materialPath);
        }
        private static void ConfigureTexture(string path,bool srgb,bool normal)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=srgb;importer.isReadable=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
            importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.anisoLevel=4;importer.SaveAndReimport();
        }
    }
}
