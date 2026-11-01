using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.World.Editor
{
    public static class CabinLightingSetup
    {
        public const string SettingsPath="Assets/EchoStation/World/Lighting/ES_Cabin_Lighting.asset";
        [MenuItem("Tools/EchoStation/Stage 1/W01/Tune Cabin Lighting Readability")]
        public static void TuneReadability()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||Lightmapping.isRunning)throw new InvalidOperationException("Exit Play/bake first.");
            var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;var previous=SceneManager.GetActiveScene();
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty)throw new InvalidOperationException("Unsaved cabin: preserving user changes.");
                var lights=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light>()).Where(l=>l.lightmapBakeType==LightmapBakeType.Baked);
                foreach(var light in lights)
                {
                    light.intensity=light.name switch{"Cabin_MainCoolA"=>9,"Cabin_MainCoolB"=>6,"Cabin_StorageCool"=>7.5f,"Cabin_FoyerCool"=>4.5f,"Cabin_DangerWarm"=>2.5f,_=>light.intensity};
                    if(light.name!="Cabin_DangerWarm")light.color=new Color(.78f,.86f,1);
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        [MenuItem("Tools/EchoStation/Stage 1/W01/Configure Cabin Lighting And Volume")]
        public static void Configure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||Lightmapping.isRunning)throw new InvalidOperationException("Exit Play/bake first.");
            CabinGreyboxSetup.EnsureFolder("Assets/EchoStation/World/Lighting");
            if(!AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath)&&!AssetDatabase.CopyAsset(BakedLightingSetup.SettingsPath,SettingsPath))
                throw new InvalidOperationException("Could not copy the tested lighting settings.");
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);
            var opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty)throw new InvalidOperationException("Unsaved cabin: not saving user changes.");
                var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");if(root.transform.Find("Cabin_Lighting"))return;
                SceneManager.SetActiveScene(scene);Lightmapping.lightingSettings=AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
                RenderSettings.ambientLight=Color.black;RenderSettings.ambientIntensity=0;RenderSettings.reflectionIntensity=0;RenderSettings.ambientProbe=new SphericalHarmonicsL2();
                var lighting=new GameObject("Cabin_Lighting");lighting.transform.SetParent(root.transform,false);
                AddLight("Cabin_MainCoolA",new Vector3(-1.8f,2.7f,.5f),new Color(.78f,.86f,1),9,8,lighting.transform);
                AddLight("Cabin_MainCoolB",new Vector3(3,2.7f,2),new Color(.78f,.86f,1),6,7,lighting.transform);
                AddLight("Cabin_StorageCool",new Vector3(7.1f,2.7f,.5f),new Color(.78f,.86f,1),7.5f,6,lighting.transform);
                AddLight("Cabin_FoyerCool",new Vector3(0,2.4f,-5.6f),new Color(.78f,.86f,1),4.5f,5,lighting.transform);
                AddLight("Cabin_DangerWarm",new Vector3(4.4f,2.5f,-1.5f),new Color(1,.4f,.15f),2.5f,4,lighting.transform);
                var probes=new GameObject("Cabin_LightProbes",typeof(LightProbeGroup)).GetComponent<LightProbeGroup>();probes.transform.SetParent(lighting.transform,false);
                var positions=new System.Collections.Generic.List<Vector3>();
                foreach(var x in new[]{-4f,-2f,0,2,4})foreach(var z in new[]{-3f,-1,1,3})foreach(var y in new[]{.35f,1.1f,2.2f,2.9f})positions.Add(new Vector3(x,y,z));
                foreach(var x in new[]{5.7f,7.1f,8.5f})foreach(var z in new[]{-1f,1,2.6f})foreach(var y in new[]{.35f,1.1f,2.2f,2.9f})positions.Add(new Vector3(x,y,z));
                foreach(var x in new[]{-.9f,0,.9f})foreach(var z in new[]{-6.4f,-5.2f,-4.3f})foreach(var y in new[]{.35f,1.1f,2.1f})positions.Add(new Vector3(x,y,z));
                // Combined renderer bounds enclose empty rooms: exclude solids with source colliders instead.
                var solids=root.GetComponentsInChildren<BoxCollider>().Select(c=>new Bounds(c.transform.TransformPoint(c.center),Vector3.Scale(c.size,c.transform.lossyScale))).ToArray();
                probes.probePositions=positions.Where(p=>!solids.Any(b=>b.Contains(p))).ToArray();
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled))
                {renderer.receiveGI=ReceiveGI.Lightmaps;renderer.lightProbeUsage=LightProbeUsage.Off;}
                var camera=scene.GetRootGameObjects().Single(o=>o.name=="Cabin_ValidationCamera").GetComponent<Camera>();var data=camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing=true;data.volumeLayerMask=1<<LayerMask.NameToLayer(ExplorationVolumeSetup.LayerName);data.volumeTrigger=camera.transform;
                var volume=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ExplorationVolumeSetup.PrefabPath),scene);volume.name="ES_Cabin_GlobalVolume";
                var fault=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RealtimeLightTemplates.Folder+"/ES_FaultLight.prefab"),scene);
                fault.name="Cabin_Fault_Realtime";fault.transform.position=new Vector3(4.5f,2.3f,-1.5f);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        private static void AddLight(string name,Vector3 point,Color color,float intensity,float range,Transform parent)
        {
            var go=new GameObject(name,typeof(Light));go.transform.SetParent(parent);go.transform.position=point;var light=go.GetComponent<Light>();
            light.type=LightType.Point;light.lightmapBakeType=LightmapBakeType.Baked;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.Soft;
        }
    }
}
