using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.SalvageNext.Editor
{
    public static class FacilityAssets
    {
        public const string Root="Assets/EchoStation/SalvageNext",Raw=Root+"/ThirdParty/Quaternius",Generated=Root+"/Generated",Prefabs=Root+"/Prefabs/Facilities";
        public const string Bench=Root+"/Scenes/ES_Next_FacilityBench.unity",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-A01b";
        const string Owner="EchoStation-SalvageNext-Generated";
        public static string[] Models()=>AssetDatabase.FindAssets("t:Model",new[]{Raw+"/FBX"}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        public static string Prefab(string model)=>Prefabs+"/ES_"+Path.GetFileNameWithoutExtension(model)+".prefab";
        public static void Folder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;var parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        static void RequireOwned(Object obj){if(obj&&!AssetDatabase.GetLabels(obj).Contains(Owner))throw new InvalidOperationException("Preserving non-owned generated asset: "+AssetDatabase.GetAssetPath(obj));}
        static void Mark(Object obj){AssetDatabase.SetLabels(obj,new[]{Owner});EditorUtility.SetDirty(obj);AssetDatabase.SaveAssetIfDirty(obj);}
        static void Texture(string path,bool srgb,bool normal)
        {
            var t=AssetImporter.GetAtPath(path) as TextureImporter;if(t==null)throw new InvalidOperationException("Missing texture "+path);
            t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;t.sRGBTexture=srgb;t.maxTextureSize=1024;t.isReadable=false;t.mipmapEnabled=true;t.anisoLevel=4;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
        }
        public static Material MaterialFor(string name)
        {
            var normalized=name.ToLowerInvariant().Replace("_","").Replace(" ","");string variant;
            if(normalized.Contains("lightfade"))variant="LightFade";else if(normalized=="mblack")variant="Black";else if(normalized=="mlight")variant="Light";
            else if(normalized.Contains("decalscreen"))variant="Screen";else if(normalized.Contains("paddedwall"))variant="PaddedWall";else if(normalized.Contains("decal"))variant="Decal";
            else if(normalized.Contains("trim01"))variant="Trim01";else if(normalized.Contains("trim02"))variant="Trim02";
            else if(normalized.Contains("trim03"))variant=normalized.Contains("dark")?"Trim03Dark":normalized.Contains("cable")?"Trim03Cables":"Trim03";
            else throw new InvalidOperationException("Unrecognized source material: "+name);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/ES_"+variant+".mat");if(!material)throw new InvalidOperationException("Prepare material first: "+variant);return material;
        }
        [MenuItem("Tools/EchoStation/SalvageNext/R4-A01b/1 Materials")]
        public static void PrepareMaterials()
        {
            SceneSafetyGuard.RequireClean();Folder(Generated);Folder(Prefabs);Folder(Root+"/Scenes");Directory.CreateDirectory(Evidence);
            foreach(var path in Directory.GetFiles(Raw+"/Textures","*.png")){bool normal=path.Contains("Normal"),data=normal||path.Contains("ORM")||path.Contains("Mask");Texture(path.Replace('\\','/'),!data,normal);}
            foreach(var number in new[]{"01","02","03","Padded"})
            {
                var source=Raw+"/Textures/"+(number=="Padded"?"T_PaddedWall":"T_Trim_"+number)+"_ORM.png";var output=Generated+"/ES_"+(number=="Padded"?"Padded":"Trim"+number)+"_Mask.png";
                Texture2D input=null,packed=null;
                try
                {
                    input=new Texture2D(2,2,TextureFormat.RGBA32,false,true);if(!input.LoadImage(File.ReadAllBytes(source)))throw new InvalidDataException(source);
                    var pixels=input.GetPixels32();for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32(p.b,p.r,0,(byte)(255-p.g));}
                    packed=new Texture2D(input.width,input.height,TextureFormat.RGBA32,false,true);packed.SetPixels32(pixels);packed.Apply();
                    if(File.Exists(output)&&!AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(output)).Contains(Owner))throw new InvalidOperationException("Preserving foreign packed texture.");
                    File.WriteAllBytes(output,packed.EncodeToPNG());AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);Texture(output,false,false);Mark(AssetDatabase.LoadMainAssetAtPath(output));
                }
                finally{if(input)Object.DestroyImmediate(input);if(packed)Object.DestroyImmediate(packed);}
            }
            foreach(var variant in new[]{"Trim01","Trim02","Trim03","Trim03Dark","Trim03Cables","PaddedWall"})
            {
                bool padded=variant=="PaddedWall";var number=padded?"Padded":variant.Substring(4,2);var path=Generated+"/ES_"+variant+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);RequireOwned(material);
                if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
                var baseName=padded?"T_PaddedWall_BaseColor.png":number=="02"?"T_Trim_02_BaseColor_Blue.png":variant.EndsWith("Cables",StringComparison.Ordinal)?"T_Trim_03_Cables.png":"T_Trim_"+number+"_BaseColor.png";
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Raw+"/Textures/"+baseName));
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Raw+"/Textures/"+(padded?"T_PaddedWall":"T_Trim_"+number)+"_Normal.png"));
                var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(Generated+"/ES_"+(padded?"Padded":"Trim"+number)+"_Mask.png");material.SetTexture("_MetallicGlossMap",mask);material.SetTexture("_OcclusionMap",mask);
                material.SetColor("_BaseColor",variant.EndsWith("Dark",StringComparison.Ordinal)?new Color(.3f,.35f,.42f):new Color(.84f,.91f,1));
                material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",.72f);material.SetFloat("_BumpScale",1);material.SetFloat("_OcclusionStrength",1);
                material.SetFloat("_Cull",number=="03"?(float)CullMode.Off:(float)CullMode.Back);material.enableInstancing=true;
                foreach(var keyword in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP"})material.EnableKeyword(keyword);
                var emission=AssetDatabase.LoadAssetAtPath<Texture2D>(Raw+"/Textures/"+(padded?"T_PaddingWall":"T_Trim_"+number)+"_Emissive.png");
                if(emission){material.SetTexture("_EmissionMap",emission);material.SetColor("_EmissionColor",new Color(.12f,.42f,.8f)*1.5f);material.EnableKeyword("_EMISSION");}
                Mark(material);
            }
            var normalPath=Generated+"/ES_FlatNormal.png";
            if(!File.Exists(normalPath))
            {
                var flat=new Texture2D(2,2,TextureFormat.RGBA32,false,true);try{flat.SetPixels32(Enumerable.Repeat(new Color32(128,128,255,255),4).ToArray());flat.Apply();File.WriteAllBytes(normalPath,flat.EncodeToPNG());}finally{Object.DestroyImmediate(flat);}
                AssetDatabase.ImportAsset(normalPath,ImportAssetOptions.ForceSynchronousImport);Texture(normalPath,false,true);Mark(AssetDatabase.LoadMainAssetAtPath(normalPath));
            }
            var decalPath=Generated+"/ES_Decal.mat";var decal=AssetDatabase.LoadAssetAtPath<Material>(decalPath);RequireOwned(decal);
            if(!decal){decal=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(decal,decalPath);}
            decal.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Raw+"/Textures/T_Decals.png"));decal.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));decal.SetFloat("_BumpScale",0);decal.SetFloat("_Metallic",0);decal.SetFloat("_Smoothness",.2f);decal.SetFloat("_AlphaClip",1);decal.SetFloat("_Cutoff",.3f);decal.SetFloat("_Cull",(float)CullMode.Off);decal.EnableKeyword("_ALPHATEST_ON");decal.SetOverrideTag("RenderType","TransparentCutout");decal.renderQueue=(int)RenderQueue.AlphaTest;decal.enableInstancing=true;Mark(decal);
            var whitePath=Generated+"/ES_FlatWhite.png";
            if(!File.Exists(whitePath))
            {
                var flat=new Texture2D(2,2,TextureFormat.RGBA32,false,true);try{flat.SetPixels32(Enumerable.Repeat(new Color32(255,255,255,255),4).ToArray());flat.Apply();File.WriteAllBytes(whitePath,flat.EncodeToPNG());}finally{Object.DestroyImmediate(flat);}
                AssetDatabase.ImportAsset(whitePath,ImportAssetOptions.ForceSynchronousImport);Texture(whitePath,true,false);Mark(AssetDatabase.LoadMainAssetAtPath(whitePath));
            }
            foreach(var variant in new[]{"Black","Light","LightFade","Screen"})
            {
                var path=Generated+"/ES_"+variant+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);RequireOwned(material);
                if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
                material.SetTexture("_BaseMap",variant=="Screen"?decal.GetTexture("_BaseMap"):AssetDatabase.LoadAssetAtPath<Texture2D>(whitePath));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));material.SetFloat("_BumpScale",0);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.3f);
                material.SetColor("_BaseColor",variant=="Black"?new Color(.025f,.035f,.05f):variant=="LightFade"?Color.clear:new Color(.3f,.65f,1));material.enableInstancing=true;
                if(variant=="Light"||variant=="Screen"){material.SetTexture("_EmissionMap",material.GetTexture("_BaseMap"));material.SetColor("_EmissionColor",new Color(.15f,.5f,1)*(variant=="Light"?3:.75f));material.EnableKeyword("_EMISSION");}
                if(variant=="LightFade"){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.5f);material.EnableKeyword("_ALPHATEST_ON");material.SetShaderPassEnabled("ShadowCaster",false);}
                Mark(material);
            }
            File.WriteAllText(Evidence+"/MaterialsPrepared.json",new JObject{{"utc",DateTime.UtcNow.ToString("O")},{"variants",11},{"globalSettingsSaved",false},{"packing","R=metal G=occlusion B=0 A=1-roughness"},{"lightFade","source-only mapping; cosmetic beam triangles excluded from derived prefabs"}}.ToString());
        }
        public static bool PrimaryStatic(string path)=>!path.Contains("/Props/");
        public static void PrepareModel(string path)
        {
            SceneSafetyGuard.RequireClean();if(!path.StartsWith(Raw+"/FBX/",StringComparison.Ordinal))throw new ArgumentException("Only owned copied models.");
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(!importer)throw new InvalidOperationException(path);
            importer.globalScale=1;importer.useFileScale=true;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.isReadable=true;
            importer.generateSecondaryUV=PrimaryStatic(path);importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.materialLocation=ModelImporterMaterialLocation.InPrefab;importer.SaveAndReimport();
            var materials=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray();
            foreach(var source in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),MaterialFor(source.name));
            importer.SaveAndReimport();var sourceModel=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!sourceModel)throw new InvalidOperationException("No imported model.");
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var container=new GameObject("ES_"+Path.GetFileNameWithoutExtension(path));
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(sourceModel,scene);instance.transform.SetParent(container.transform,false);
                PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var animation in instance.GetComponentsInChildren<Animator>(true))Object.DestroyImmediate(animation);
                int part=0;
                foreach(var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh=new Mesh{name=Path.GetFileNameWithoutExtension(path)+"_StaticPose"};skin.BakeMesh(mesh,true);
                    var stored=StoreMesh(mesh,Generated+"/ES_"+Path.GetFileNameWithoutExtension(path)+"_Pose_"+(part++)+".asset");
                    var go=skin.gameObject;var materialsCopy=skin.sharedMaterials;Object.DestroyImmediate(skin);go.AddComponent<MeshFilter>().sharedMesh=stored;go.AddComponent<MeshRenderer>().sharedMaterials=materialsCopy;
                }
                part=0;
                foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var slots=renderer.sharedMaterials;var keep=Enumerable.Range(0,slots.Length).Where(i=>slots[i]&&slots[i].name!="ES_LightFade").ToArray();
                    if(keep.Length==slots.Length)continue;
                    if(keep.Length==0){Object.DestroyImmediate(renderer.gameObject);continue;}
                    var filter=renderer.GetComponent<MeshFilter>();var original=filter.sharedMesh;var mesh=Object.Instantiate(original);mesh.name=original.name+"_NoCosmeticBeam";
                    mesh.subMeshCount=keep.Length;for(int i=0;i<keep.Length;i++)mesh.SetTriangles(original.GetTriangles(keep[i]),i,false);mesh.RecalculateBounds();
                    filter.sharedMesh=StoreMesh(mesh,Generated+"/ES_"+Path.GetFileNameWithoutExtension(path)+"_NoBeam_"+(part++)+".asset");renderer.sharedMaterials=keep.Select(i=>slots[i]).ToArray();
                }
                // Remapped FBX slots are canonical. No automatic box across door openings.
                foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.sharedMaterials.Any(m=>!m||m.shader.name!="Universal Render Pipeline/Lit"))throw new InvalidOperationException("Unmapped material on "+path);
                var target=Prefab(path);RequireOwned(AssetDatabase.LoadMainAssetAtPath(target));var prefab=PrefabUtility.SaveAsPrefabAsset(container,target);if(!prefab)throw new InvalidOperationException("Prefab save failed.");Mark(prefab);
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        static Mesh StoreMesh(Mesh mesh,string path)
        {
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);RequireOwned(existing);
            if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);Mark(existing);return existing;}
            AssetDatabase.CreateAsset(mesh,path);Mark(mesh);return mesh;
        }
        [MenuItem("Tools/EchoStation/SalvageNext/R4-A01b/2 Models")]
        public static void PrepareModels()
        {
            SceneSafetyGuard.RequireClean();var models=Models();if(models.Length!=26)throw new InvalidOperationException("Expected copied selection of 26 models.");
            var rows=new JArray();foreach(var path in models){PrepareModel(path);rows.Add(new JObject{{"model",path},{"prefab",Prefab(path)}});File.WriteAllText(Evidence+"/ModelProgress.json",rows.ToString());}
        }
        [MenuItem("Tools/EchoStation/SalvageNext/R4-A01b/3 Bench")]
        public static void CreateBench()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Bench))throw new InvalidOperationException("Preserve existing bench.");
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var picks=new[]{"Platform_Simple","WallAstra_Straight","WallBand_Straight","Door_Frame_Square","Column_Simple","Prop_Computer","Prop_Chest"};
                for(int i=0;i<picks.Length;i++)
                {
                    var model=Models().Single(p=>Path.GetFileNameWithoutExtension(p)==picks[i]);var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(model)),scene);
                    instance.transform.position=new Vector3(i%3*4,0,i/3*5);
                }
                var camera=new GameObject("FacilityBenchCamera").AddComponent<Camera>();camera.allowHDR=true;camera.nearClipPlane=.05f;camera.farClipPlane=70;camera.fieldOfView=48;camera.transform.position=new Vector3(14,10,-14);camera.transform.LookAt(new Vector3(4,1,3));camera.backgroundColor=new Color(.025f,.04f,.065f);camera.clearFlags=CameraClearFlags.SolidColor;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                var key=new GameObject("FacilityBenchKey").AddComponent<Light>();key.type=LightType.Directional;key.intensity=2.5f;key.transform.rotation=Quaternion.Euler(45,-35,0);key.shadows=LightShadows.Soft;
                var fill=new GameObject("FacilityBenchFill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=1;fill.color=new Color(.5f,.7f,1);fill.transform.rotation=Quaternion.Euler(30,140,0);
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.3f,.36f,.44f);RenderSettings.fog=false;
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();var profilePath=Generated+"/ES_FacilityBench_Volume.asset";if(AssetDatabase.LoadMainAssetAtPath(profilePath))throw new InvalidOperationException("Preserve profile.");
                AssetDatabase.CreateAsset(profile,profilePath);var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;AssetDatabase.AddObjectToAsset(tone,profile);Mark(profile);
                var volume=new GameObject("FacilityBenchVolume").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
                if(!EditorSceneManager.SaveScene(scene,Bench))throw new IOException("Bench save failed.");
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
