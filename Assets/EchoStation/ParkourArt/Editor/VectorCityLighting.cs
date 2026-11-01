using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.ParkourArt.Editor
{
    public static class VectorCityLighting
    {
        public const string PipelinePath=VectorCityBuilder.Root+"/Settings/VC_ForwardHDR.asset";
        public const string RendererPath=VectorCityBuilder.Root+"/Settings/VC_ForwardRenderer.asset";
        public const string DefaultProfilePath=VectorCityBuilder.Root+"/Settings/VC_DefaultVolume.asset";
        [MenuItem("Tools/EchoStation/Parkour Art/03 Configure Daylight and Shadows")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var scene=SceneManager.GetActiveScene();
            if(scene.path!=VectorCityBuilder.ScenePath)throw new InvalidOperationException("Open the owned parkour scene first.");
            if(!AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath))AssetDatabase.CopyAsset("Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardRenderer.asset",RendererPath);
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);renderer.rendererFeatures.Clear();
            var r=new SerializedObject(renderer);r.FindProperty("m_RenderingMode").intValue=0;
            r.FindProperty("m_RendererFeatureMap").arraySize=0;r.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(renderer);
            if(!AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath))AssetDatabase.CopyAsset("Assets/EchoStation/Station/Generated/ES_S2_ForwardHDR.asset",PipelinePath);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var p=new SerializedObject(pipeline);p.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;p.ApplyModifiedPropertiesWithoutUndo();
            pipeline.supportsHDR=true;pipeline.msaaSampleCount=4;pipeline.renderScale=1;
            pipeline.supportsCameraDepthTexture=true;pipeline.supportsCameraOpaqueTexture=false;pipeline.useSRPBatcher=true;
            pipeline.mainLightShadowmapResolution=8192;pipeline.shadowCascadeCount=4;pipeline.shadowDistance=110;
            pipeline.cascade4Split=new Vector3(.12f,.32f,.60f);pipeline.cascadeBorder=.12f;
            pipeline.shadowDepthBias=.15f;pipeline.shadowNormalBias=.4f;
            p.Update();p.FindProperty("m_SoftShadowsSupported").boolValue=true;
            p.FindProperty("m_ReflectionProbeBlending").boolValue=true;p.FindProperty("m_ReflectionProbeBoxProjection").boolValue=true;p.ApplyModifiedPropertiesWithoutUndo();
            var defaults=AssetDatabase.LoadAssetAtPath<VolumeProfile>(DefaultProfilePath);
            if(!defaults){defaults=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(defaults,DefaultProfilePath);}
            pipeline.volumeProfile=defaults;EditorUtility.SetDirty(pipeline);
            var skyPath=VectorCityBuilder.Root+"/Settings/VC_CleanSky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(!sky){sky=new Material(Shader.Find("EchoStation/Parkour/Clean Sky"));AssetDatabase.CreateAsset(sky,skyPath);}
            sky.SetColor("_Horizon",new Color(.69f,.84f,.9f));sky.SetColor("_Zenith",new Color(.32f,.6f,.75f));EditorUtility.SetDirty(sky);
            RenderSettings.skybox=sky;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.72f,.81f,.87f);RenderSettings.ambientEquatorColor=new Color(.57f,.66f,.72f);RenderSettings.ambientGroundColor=new Color(.44f,.51f,.57f);RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=1;RenderSettings.fog=false;
            var sun=GameObject.Find("Sun_Daylight").GetComponent<Light>();sun.intensity=1.4f;sun.color=new Color(1,.965f,.92f);
            sun.shadowBias=.15f;sun.shadowNormalBias=.4f;sun.shadowStrength=.72f;sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(48,-32,0);RenderSettings.sun=sun;
            var camera=GameObject.Find("Camera_Hero").GetComponent<Camera>();camera.clearFlags=CameraClearFlags.Skybox;camera.allowHDR=true;camera.allowMSAA=true;
            var ownedRoot=scene.GetRootGameObjects().Single(g=>g.hideFlags==HideFlags.None);
            var scope=ownedRoot.GetComponent<VectorCityPipelineScope>();
            if(!scope)scope=ownedRoot.AddComponent<VectorCityPipelineScope>();
            scope.Initialize(pipeline,GraphicsSettings.defaultRenderPipeline,QualitySettings.renderPipeline);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
}
