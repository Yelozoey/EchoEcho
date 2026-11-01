using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.InfiltrationMetal.Editor;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.InfiltrationWallPreview.Editor
{
    public static class WallPreviewScene
    {
        public const string Root="Assets/EchoStation/InfiltrationWallPreview",ScenePath=Root+"/Scenes/ES_WallBrightness_Preview.unity",MaterialPath=Root+"/Generated/WP_WallBrighter.mat",Evidence="Docs/Tests/InfiltrationPrototype/Evidence/A01W";
        public const string CoatingPath=Root+"/Generated/WP_WallCoated.mat",CoatingTexturePath=Root+"/Generated/WP_CoatingBase.png";
        public static void ApplyCoating()
        {
            SceneSafetyGuard.RequireClean();
            if(File.Exists(CoatingPath)||File.Exists(CoatingTexturePath))throw new InvalidOperationException("Preserve existing coating assets");
            if(!File.Exists(Evidence+"/BareMetalPreview_Archive.zip"))throw new InvalidOperationException("Archive initial preview first");
            var previous=SceneManager.GetActiveScene();var sample=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            Texture2D input=null,output=null;
            try
            {
                SceneManager.SetActiveScene(sample);var preview=MetalScene.World(sample).GetComponent<WallBrightnessPreview>();if(preview.Coated)throw new InvalidOperationException("Coating already configured");
                var original=preview.original;string source=AssetDatabase.GetAssetPath(original.GetTexture("_BaseMap"));
                input=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!input.LoadImage(File.ReadAllBytes(source)))throw new IOException("Read author base atlas failed");
                int w=Mathf.Min(512,input.width),h=Mathf.Min(512,input.height),sx=input.width/w,sy=input.height/h;if(sx*w!=input.width||sy*h!=input.height)throw new InvalidOperationException("Expected integer resampling");
                var pixels=input.GetPixels32();var result=new Color32[w*h];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++){float luma=0;for(int dy=0;dy<sy;dy++)for(int dx=0;dx<sx;dx++){var c=pixels[(y*sy+dy)*input.width+x*sx+dx];luma+=.2126f*c.r+.7152f*c.g+.0722f*c.b;}float value=.82f+(luma/(sx*sy*255)-.5f)*.14f;result[y*w+x]=new Color32((byte)Mathf.RoundToInt(value*.97f*255),(byte)Mathf.RoundToInt(value*.985f*255),(byte)Mathf.RoundToInt(value*255),255);}
                output=new Texture2D(w,h,TextureFormat.RGBA32,false);output.SetPixels32(result);output.Apply();File.WriteAllBytes(CoatingTexturePath,output.EncodeToPNG());AssetDatabase.ImportAsset(CoatingTexturePath,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(CoatingTexturePath);importer.sRGBTexture=true;importer.isReadable=false;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                var coating=UnityEngine.Object.Instantiate(original);coating.name="WP_WallCoated";coating.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(CoatingTexturePath));coating.SetColor("_BaseColor",Color.white);coating.SetFloat("_Metallic",0);coating.SetFloat("_Smoothness",.42f);coating.DisableKeyword("_METALLICSPECGLOSSMAP");coating.SetTexture("_MetallicGlossMap",null);AssetDatabase.CreateAsset(coating,CoatingPath);AssetDatabase.SaveAssetIfDirty(coating);
                preview.brighter=coating;preview.Coated=true;preview.grazing.intensity=2;preview.SetMode(1);
                if(!EditorSceneManager.SaveScene(sample))throw new IOException("Save coating preview failed");
            }
            finally{if(input)UnityEngine.Object.DestroyImmediate(input);if(output)UnityEngine.Object.DestroyImmediate(output);SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(sample,true);}
        }
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();ArtSampleAssets.Folder(Root+"/Scenes");ArtSampleAssets.Folder(Root+"/Generated");
            if(File.Exists(MaterialPath))throw new InvalidOperationException("Preserve preview already created");
            if(File.Exists(ScenePath)){if(!File.ReadAllBytes(ScenePath).SequenceEqual(File.ReadAllBytes(MetalScene.ScenePath)))throw new InvalidOperationException("Preserve modified preview scene");}
            else if(!AssetDatabase.CopyAsset(MetalScene.ScenePath,ScenePath))throw new IOException("Copy independent scene failed");
            var previous=SceneManager.GetActiveScene();var sample=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(sample);var world=MetalScene.World(sample);
                var targets=world.GetComponentsInChildren<Transform>().Where(t=>t.name=="South_4"||t.name=="South_4_Reverse").SelectMany(t=>t.GetComponentsInChildren<Renderer>()).ToArray();
                if(targets.Length!=2||targets.Any(r=>r.sharedMaterials.Length!=3))throw new InvalidOperationException("Unexpected wall geometry");
                var original=targets[0].sharedMaterial;if(targets.Any(r=>r.sharedMaterial!=original))throw new InvalidOperationException("Wall atlas mismatch");
                var brighter=UnityEngine.Object.Instantiate(original);brighter.name="WP_WallBrighter";brighter.SetColor("_BaseColor",new Color(.87f,.91f,.94f,1));AssetDatabase.CreateAsset(brighter,MaterialPath);AssetDatabase.SaveAssetIfDirty(brighter);
                var go=new GameObject("WP_LocalGrazingPreview");go.transform.SetParent(world,false);go.transform.position=new Vector3(1.6f,4,-14.9f);go.transform.LookAt(new Vector3(-.5f,1.9f,-15.88f));
                var light=go.AddComponent<Light>();light.type=LightType.Spot;light.lightmapBakeType=LightmapBakeType.Realtime;light.color=new Color(.83f,.91f,1);light.intensity=12;light.range=6;light.spotAngle=115;light.innerSpotAngle=85;light.shadows=LightShadows.None;light.bounceIntensity=0;
                var preview=world.gameObject.AddComponent<WallBrightnessPreview>();preview.surfaces=targets;preview.original=original;preview.brighter=brighter;preview.grazing=light;preview.font=ArtSampleScene.ResolveFont();preview.SetMode(2);
                var motor=world.GetComponentInChildren<SalvageMotor>();motor.transform.position=new Vector3(0,.05f,-11);motor.transform.rotation=Quaternion.Euler(0,180,0);motor.View.localRotation=Quaternion.Euler(-3,0,0);
                if(!EditorSceneManager.SaveScene(sample))throw new IOException("Save owned preview failed");
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(sample,true);}
        }
    }
}
