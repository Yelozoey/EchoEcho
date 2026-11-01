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
    public static class RoverAssets
    {
        public const string Raw=FacilityAssets.Root+"/ThirdParty/MarsRover",PrefabPath=FacilityAssets.Root+"/Prefabs/Echo/ES_Echo_Rover_Visual.prefab";
        public const string Bench=FacilityAssets.Root+"/Scenes/ES_Next_RoverBench.unity",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-A01c-1";
        const string Owner="EchoStation-SalvageNext-Generated";
        static void Save(Object asset){AssetDatabase.SetLabels(asset,new[]{Owner});EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
        static void Owned(Object asset){if(asset&&!AssetDatabase.GetLabels(asset).Contains(Owner))throw new InvalidOperationException("Preserving non-owned Rover asset.");}
        public static Material Palette(string name)=>AssetDatabase.LoadAssetAtPath<Material>(FacilityAssets.Generated+"/ES_Rover_"+name+".mat");
        static Material MakeMaterial(string name,Texture baseMap,Color color,float metallic,float smoothness,Color emission)
        {
            var path=FacilityAssets.Generated+"/ES_Rover_"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);Owned(material);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",baseMap);material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smoothness);
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(FacilityAssets.Generated+"/ES_FlatNormal.png"));material.SetFloat("_BumpScale",0);material.enableInstancing=true;
            if(emission.maxColorComponent>0){material.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(FacilityAssets.Generated+"/ES_FlatWhite.png"));material.SetColor("_EmissionColor",emission);material.EnableKeyword("_EMISSION");}
            Save(material);return material;
        }
        [MenuItem("Tools/EchoStation/SalvageNext/R4-A01c/1 Prepare Rover Visual")]
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();FacilityAssets.Folder(Path.GetDirectoryName(PrefabPath).Replace('\\','/'));Directory.CreateDirectory(Evidence);
            var source=JObject.Parse(File.ReadAllText(Raw+"/RoverParts.json"));
            var texture=AssetImporter.GetAtPath(Raw+"/Original/rover_diffuse.png") as TextureImporter;if(!texture)throw new InvalidOperationException("Rover source not imported.");
            texture.textureType=TextureImporterType.Default;texture.sRGBTexture=true;texture.mipmapEnabled=true;texture.isReadable=false;texture.maxTextureSize=256;texture.anisoLevel=2;texture.textureCompression=TextureImporterCompression.CompressedHQ;texture.SaveAndReimport();
            foreach(var path in Directory.GetFiles(Raw,"*.obj",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')))
            {
                var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(!importer)throw new InvalidOperationException(path);
                importer.globalScale=1;importer.useFileScale=true;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.generateSecondaryUV=false;importer.isReadable=true;
                importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importNormals=ModelImporterNormals.Calculate;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.normalSmoothingAngle=60;importer.SaveAndReimport();
            }
            var material=MakeMaterial("Surface",AssetDatabase.LoadAssetAtPath<Texture2D>(Raw+"/Original/rover_diffuse.png"),new Color(.78f,.87f,.97f),.15f,.28f,Color.black);
            var white=AssetDatabase.LoadAssetAtPath<Texture2D>(FacilityAssets.Generated+"/ES_FlatWhite.png");
            var calm=MakeMaterial("Calm",white,new Color(.03f,.25f,.8f),0,.3f,new Color(.02f,.35f,2.2f));
            var thinking=MakeMaterial("Thinking",white,new Color(.9f,.7f,.03f),0,.3f,new Color(2.2f,1.25f,.02f));
            var alert=MakeMaterial("Alert",white,new Color(.9f,.03f,.01f),0,.3f,new Color(2.2f,.03f,.01f));
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=new GameObject("ES_Echo_Rover_Visual");Transform body=null,head=null;var wheels=new System.Collections.Generic.List<Transform>();var radii=new System.Collections.Generic.List<float>();
                foreach(var row in source["parts"])
                {
                    string name=row["name"].Value<string>();var pivot=new GameObject(name+"_Pivot").transform;pivot.SetParent(root.transform,false);var p=row["unityPosition"].Values<float>().ToArray();pivot.localPosition=new Vector3(p[0],p[1],p[2]);
                    var asset=AssetDatabase.LoadAssetAtPath<GameObject>(row["path"].Value<string>());if(!asset)throw new InvalidOperationException("Missing derived Rover part "+name);
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);instance.transform.SetParent(pivot,false);
                    foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>()){renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;}
                    if(name=="Body")body=pivot;else if(name=="Head")head=pivot;else if(name.StartsWith("Wheel_",StringComparison.Ordinal)){wheels.Add(pivot);radii.Add(row["radius"].Value<float>());}
                }
                if(!head)throw new InvalidOperationException("No Rover scan head.");
                var scan=new GameObject("RoverScanOrigin").transform;scan.SetParent(head,false);scan.localPosition=new Vector3(0,0,.07f);
                var lamp=GameObject.CreatePrimitive(PrimitiveType.Cube);lamp.name="RoverStatusIndicator";lamp.transform.SetParent(head,false);lamp.transform.localPosition=new Vector3(.045f,.02f,.01f);lamp.transform.localScale=new Vector3(.022f,.012f,.018f);Object.DestroyImmediate(lamp.GetComponent<Collider>());
                root.AddComponent<RoverModelParts>().Configure(body,head,scan,wheels.ToArray(),radii.ToArray(),lamp.GetComponent<MeshRenderer>(),calm,thinking,alert);
                Owned(AssetDatabase.LoadMainAssetAtPath(PrefabPath));var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);if(!prefab)throw new IOException("Rover prefab save failed.");Save(prefab);
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            File.WriteAllText(Evidence+"/VisualPrepared.json",new JObject{{"utc",DateTime.UtcNow.ToString("O")},{"prefab",PrefabPath},{"parts",8},{"wheels",6},{"sourceFaces",704},{"animationsVerified",false}}.ToString());
        }
        static Cubemap Studio()
        {
            var path=FacilityAssets.Generated+"/ES_Next_StudioReflection.asset";var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);Owned(cube);if(cube)return cube;
            cube=new Cubemap(16,TextureFormat.RGBAHalf,true);for(int f=0;f<6;f++)cube.SetPixels(Enumerable.Repeat(new Color(.4f,.45f,.52f)*(f==2?2:f==3?.15f:.8f),256).ToArray(),(CubemapFace)f);cube.Apply(true,false);AssetDatabase.CreateAsset(cube,path);Save(cube);return cube;
        }
        [MenuItem("Tools/EchoStation/SalvageNext/R4-A01c/2 Rover Bench")]
        public static void CreateBench()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(Bench))throw new InvalidOperationException("Preserve existing Rover bench.");var cube=Studio();
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
                var floorModel=FacilityAssets.Models().Single(p=>Path.GetFileNameWithoutExtension(p)=="Platform_Simple");var floor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FacilityAssets.Prefab(floorModel)),scene);floor.transform.localScale=Vector3.one*.5f;
                var camera=new GameObject("RoverBenchCamera").AddComponent<Camera>();camera.allowHDR=true;camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.fieldOfView=38;camera.backgroundColor=new Color(.04f,.055f,.075f);camera.clearFlags=CameraClearFlags.SolidColor;camera.transform.position=new Vector3(.95f,.7f,1.2f);camera.transform.LookAt(new Vector3(0,.27f,0));camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                var key=new GameObject("RoverBenchKey").AddComponent<Light>();key.type=LightType.Directional;key.color=new Color(1,.93f,.86f);key.intensity=2.5f;key.transform.rotation=Quaternion.Euler(45,-35,0);key.shadows=LightShadows.Soft;
                var fill=new GameObject("RoverBenchFill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=1.5f;fill.color=new Color(.5f,.7f,1);fill.transform.rotation=Quaternion.Euler(25,140,0);
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.4f,.48f);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflection=cube;RenderSettings.reflectionIntensity=1;RenderSettings.fog=false;
                var volume=new GameObject("RoverBenchVolume").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(FacilityAssets.Generated+"/ES_FacilityBench_Volume.asset");
                if(!EditorSceneManager.SaveScene(scene,Bench))throw new IOException("Rover bench save failed.");
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
