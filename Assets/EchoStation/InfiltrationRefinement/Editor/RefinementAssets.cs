using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using EchoStation.InfiltrationArt.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.InfiltrationRefinement.Editor
{
    public static class RefinementAssets
    {
        public static readonly string[] Kinds={"Seat","Mug","Locker","Shelf","TarpCrate","Barrel","Cable","Vent","Fan","Computer"};
        public static string Prefab(string kind)=>RefinementScene.Root+"/Prefabs/DR_"+kind+".prefab";
        static string Source(string kind)
        {
            const string old="Assets/EchoStation/SalvageNext/Prefabs/Facilities/ES_Prop_";
            switch(kind){case "Seat":return ArtSampleAssets.PathFor("Chair");case "Mug":return RefinementScene.Root+"/ThirdParty/Prop_Mug.fbx";case "Locker":return RefinementScene.Root+"/ThirdParty/Prop_Locker.fbx";case "Shelf":return RefinementScene.Root+"/ThirdParty/Prop_Shelves_WideTall.fbx";case "TarpCrate":return RefinementScene.Root+"/ThirdParty/Prop_Crate_Tarp.fbx";case "Vent":return RefinementScene.Root+"/ThirdParty/Prop_Vent_Small.fbx";case "Barrel":return old+"Barrel_Large.prefab";case "Cable":return "Assets/EchoStation/SalvageNext/Prefabs/Facilities/ES_TopCables_Straight.prefab";case "Fan":return old+"Fan_Small.prefab";case "Computer":return old+"Computer.prefab";default:throw new ArgumentException(kind);}
        }
        static float Height(string kind){switch(kind){case "Mug":return .14f;case "Locker":return 2.1f;case "Shelf":return 2.15f;case "TarpCrate":return .75f;case "Barrel":return .85f;case "Computer":return .52f;default:return 1;}}
        static Material Adapt(Material original)
        {
            var trim=System.Text.RegularExpressions.Regex.Match(original.name,@"MI_Trim_(0[123])");
            if(trim.Success)return RefinementScene.MaterialFor(AssetDatabase.LoadAssetAtPath<Material>(ArtSampleAssets.Generated+"/IA_ES_Trim"+trim.Groups[1].Value+".mat"));
            if(original.name=="MI_Props_Crates")return CrateMaterial();
            if(original.shader.name=="Universal Render Pipeline/Lit")return RefinementScene.MaterialFor(original);
            string name=original.name.Replace(' ','_').Replace('.','_');string path=RefinementScene.Generated+"/DR_Imported_"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="DR_Imported_"+name,enableInstancing=true,doubleSidedGI=true};m.SetColor("_BaseColor",Color.white);m.SetFloat("_Metallic",.18f);m.SetFloat("_Smoothness",.28f);m.SetFloat("_Cull",2);
            string batch=name.Contains("Batch2")?"Batch2":name.Contains("Batch1")?"Batch1":null;
            if(batch!=null){string texture="Assets/EchoStation/InfiltrationArt/ThirdParty/sci-fi-essentials-kit/T_Props_"+batch;m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture+"_BaseColor.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture+"_Normal.png"));if(!m.GetTexture("_BaseMap"))throw new InvalidOperationException("Missing existing atlas "+batch);m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.65f);}
            else
            {
                var match=AssetDatabase.FindAssets("t:Material",new[]{"Assets/EchoStation/SalvageNext"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).FirstOrDefault(v=>v&&v.name.IndexOf(name,StringComparison.OrdinalIgnoreCase)>=0&&v.shader.name=="Universal Render Pipeline/Lit");
                if(!match)throw new InvalidOperationException("Unmapped original material: "+original.name);Object.DestroyImmediate(m);return RefinementScene.MaterialFor(match);
            }
            AssetDatabase.CreateAsset(m,path);RefinementScene.Save(m);return m;
        }
        static Material CrateMaterial()
        {
            string path=RefinementScene.Generated+"/DR_AuthoredCrate.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;
            string maskPath=RefinementScene.Generated+"/DR_CrateMask.png";Texture2D input=null,packed=null;
            try
            {
                input=new Texture2D(2,2,TextureFormat.RGBA32,false,true);if(!input.LoadImage(File.ReadAllBytes(RefinementScene.Root+"/ThirdParty/T_Props_Crates_ORM.png")))throw new InvalidDataException("Crate ORM");var pixels=input.GetPixels32();for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32(p.b,p.r,0,(byte)(255-p.g));}
                packed=new Texture2D(input.width,input.height,TextureFormat.RGBA32,false,true);packed.SetPixels32(pixels);packed.Apply();File.WriteAllBytes(maskPath,packed.EncodeToPNG());AssetDatabase.ImportAsset(maskPath,ImportAssetOptions.ForceSynchronousImport);SetTextureImport(maskPath,false,false);
            }
            finally{if(input)Object.DestroyImmediate(input);if(packed)Object.DestroyImmediate(packed);}
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="DR_AuthoredCrate",enableInstancing=true,doubleSidedGI=true};string raw=RefinementScene.Root+"/ThirdParty/T_Props_Crates_";m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(raw+"BaseColor.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(raw+"Normal.png"));var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);m.SetTexture("_MetallicGlossMap",mask);m.SetTexture("_OcclusionMap",mask);m.SetColor("_BaseColor",Color.white);m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",.65f);m.SetFloat("_BumpScale",.65f);foreach(string key in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP"})m.EnableKeyword(key);AssetDatabase.CreateAsset(m,path);RefinementScene.Save(m);return m;
        }
        static void SetTextureImport(string path,bool srgb,bool normal)
        {var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;t.sRGBTexture=srgb;t.maxTextureSize=1024;t.isReadable=false;t.mipmapEnabled=true;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();}
        public static void Prepare(string version="A")
        {
            string receipt=RefinementScene.Evidence+"/PreparedProps_"+version+".json";if(File.Exists(receipt))throw new InvalidOperationException("Preserve props receipt");
            foreach(string file in Directory.GetFiles(RefinementScene.Root+"/ThirdParty","*.png")){string p=file.Replace('\\','/');SetTextureImport(p,!p.Contains("Normal")&&!p.Contains("ORM"),p.Contains("Normal"));}
            foreach(string file in Directory.GetFiles(RefinementScene.Root+"/ThirdParty","*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.addCollider=false;importer.generateSecondaryUV=true;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.materialLocation=ModelImporterMaterialLocation.InPrefab;importer.SaveAndReimport();
            }
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var rows=new JArray();
            try
            {
                SceneManager.SetActiveScene(scene);
                foreach(string kind in Kinds)
                {
                    string source=Source(kind);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(!prefab)throw new InvalidOperationException("Source unavailable "+source);var root=new GameObject("DR_"+kind);var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);visual.transform.SetParent(root.transform,false);PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    foreach(var s in visual.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(s);foreach(var c in visual.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);foreach(var a in visual.GetComponentsInChildren<Animator>(true))Object.DestroyImmediate(a);
                    foreach(var r in visual.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(Adapt).ToArray();r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;}
                    var b=ArtSampleAssets.LocalBoundsOf(root);bool mounted=kind=="Fan"||kind=="Vent"||kind=="Cable";float factor=mounted?(kind=="Cable"?2.5f:1.1f)/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z)):Height(kind)/b.size.y;factor=Mathf.Min(factor,2.6f/Mathf.Max(b.size.x,b.size.z));visual.transform.localScale*=factor;visual.transform.localPosition=visual.transform.localPosition*factor-(mounted?b.center:new Vector3(b.center.x,b.min.y,b.center.z))*factor;
                    root.AddComponent<ArtSampleModel>().Configure(kind,source);var bounds=ArtSampleAssets.LocalBoundsOf(root);rows.Add(new JObject{{"kind",kind},{"source",source},{"prefab",Prefab(kind)},{"size",new JArray(bounds.size.x,bounds.size.y,bounds.size.z)},{"centerMounted",mounted}});if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab(kind)))throw new IOException("Save prop "+kind);Object.DestroyImmediate(root);
                }
                File.WriteAllText(receipt,rows.ToString());
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
