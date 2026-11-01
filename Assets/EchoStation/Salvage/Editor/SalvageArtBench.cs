using System.IO;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Salvage.Editor
{
    public static class SalvageArtBench
    {
        public const string ScenePath=SalvageAssetImport.Root+"/Scenes/ES_Salvage_ArtBench.unity";
        [MenuItem("Tools/EchoStation/Salvage/R0-A02/Create Art Bench")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();SalvageAssetImport.EnsureFolder(SalvageAssetImport.Root+"/Scenes");
            var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SalvagePrefabSetup.Path(0)),scene);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SalvagePrefabSetup.Path(1)),scene);
                var camera=new GameObject("ArtBenchCamera").AddComponent<Camera>();camera.fieldOfView=50;camera.nearClipPlane=.05f;camera.farClipPlane=50;
                camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.04f,.065f);Wide(camera);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                var key=new GameObject("StudioKey").AddComponent<Light>();key.type=LightType.Directional;key.intensity=2.5f;key.color=new Color(1,.92f,.82f);key.transform.rotation=Quaternion.Euler(45,-30,0);key.shadows=LightShadows.Soft;
                var fill=new GameObject("StudioFill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=1.2f;fill.color=new Color(.52f,.7f,1);fill.transform.rotation=Quaternion.Euler(30,140,0);
                var profilePath=SalvageAssetImport.Generated+"/ES_ArtBench_Volume.asset";
                var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
                if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;AssetDatabase.AddObjectToAsset(tone,profile);AssetDatabase.SaveAssetIfDirty(profile);}
                var volume=new GameObject("ArtBenchVolume").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.4f,.48f);RenderSettings.fog=false;
                var cubePath=SalvageAssetImport.Generated+"/ES_StudioReflection.asset";var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(cubePath);
                if(!cube)
                {
                    cube=new Cubemap(16,TextureFormat.RGBAHalf,true);
                    for(int face=0;face<6;face++){var colors=new Color[256];for(int p=0;p<256;p++)colors[p]=new Color(.4f,.45f,.52f)*(face==2?2:face==3?.15f:.8f);cube.SetPixels(colors,(CubemapFace)face);}
                    cube.Apply(true,false);AssetDatabase.CreateAsset(cube,cubePath);
                }
                RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflection=cube;RenderSettings.reflectionIntensity=1;
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Cannot save owned bench scene.");
            }
            finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void Wide(Camera camera){camera.transform.position=new Vector3(3,2.4f,-3.4f);camera.transform.LookAt(new Vector3(0,.1f,0));}
        public static void Close(Camera camera){camera.transform.position=new Vector3(.65f,.5f,-.8f);camera.transform.LookAt(new Vector3(0,.16f,0));}
    }
}
