using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.InfiltrationLevel.Editor
{
    public static class LevelLighting
    {
        public const string PipelinePath=LevelBuilder.Generated+"/IL_ForwardHDR.asset",RendererPath=LevelBuilder.Generated+"/IL_ForwardRenderer.asset",VolumePath=LevelBuilder.Generated+"/IL_Volume.asset",SettingsPath=LevelBuilder.Generated+"/IL_Lighting.asset";
        public static readonly Vector3[] ProbePositions={new Vector3(0,2,-12),new Vector3(0,3,-2),new Vector3(-12,2,-2),new Vector3(12,2,-2),new Vector3(-12,2,8),new Vector3(12,2,8),new Vector3(0,2,8),new Vector3(0,2,14)};
        public static ScriptableRendererFeature Ao(UniversalRendererData renderer)=>renderer.rendererFeatures.Single(f=>f.GetType().Name=="ScreenSpaceAmbientOcclusion");
        static void PipelineAssets()
        {
            if(File.Exists(PipelinePath)||File.Exists(RendererPath))throw new InvalidOperationException("Preserve own render assets");
            if(!AssetDatabase.CopyAsset("Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardRenderer.asset",RendererPath)||!AssetDatabase.CopyAsset(LevelBuilder.Pipeline,PipelinePath))throw new IOException("Copy pipeline assets");
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);renderer.rendererFeatures.Clear();var rs=new SerializedObject(renderer);rs.FindProperty("m_RenderingMode").intValue=0;rs.FindProperty("m_RendererFeatureMap").arraySize=0;rs.FindProperty("m_IntermediateTextureMode").intValue=1;rs.ApplyModifiedPropertiesWithoutUndo();
            var type=typeof(UniversalRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion",true);var ao=(ScriptableRendererFeature)ScriptableObject.CreateInstance(type);ao.name="IL_SSAO";AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);
            var so=new SerializedObject(ao);so.FindProperty("m_Settings.Downsample").boolValue=true;so.FindProperty("m_Settings.Intensity").floatValue=.65f;so.FindProperty("m_Settings.Radius").floatValue=.22f;so.FindProperty("m_Settings.Source").enumValueIndex=1;so.FindProperty("m_Settings.Samples").enumValueIndex=1;so.FindProperty("m_Settings.BlurQuality").enumValueIndex=1;so.FindProperty("m_Settings.DirectLightingStrength").floatValue=.15f;so.FindProperty("m_Settings.Falloff").floatValue=40;so.ApplyModifiedPropertiesWithoutUndo();ao.SetActive(true);renderer.SetDirty();LevelBuilder.Save(ao);LevelBuilder.Save(renderer);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);var ps=new SerializedObject(pipeline);ps.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;ps.FindProperty("m_ReflectionProbeBlending").boolValue=true;ps.FindProperty("m_ReflectionProbeBoxProjection").boolValue=true;ps.FindProperty("m_SoftShadowsSupported").boolValue=true;ps.ApplyModifiedPropertiesWithoutUndo();pipeline.supportsHDR=true;pipeline.msaaSampleCount=1;pipeline.renderScale=1;pipeline.useSRPBatcher=true;pipeline.supportsCameraDepthTexture=true;pipeline.supportsCameraOpaqueTexture=false;pipeline.mainLightShadowmapResolution=2048;pipeline.shadowCascadeCount=2;pipeline.cascade2Split=.25f;pipeline.shadowDistance=40;pipeline.shadowDepthBias=.15f;pipeline.shadowNormalBias=.35f;
            var defaults=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(defaults,LevelBuilder.Generated+"/IL_EmptyDefaults.asset");pipeline.volumeProfile=defaults;LevelBuilder.Save(defaults);LevelBuilder.Save(pipeline);
        }
        static VolumeProfile Volume()
        {
            var p=ScriptableObject.CreateInstance<VolumeProfile>();p.name="IL_Volume";AssetDatabase.CreateAsset(p,VolumePath);
            var tone=p.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;
            var color=p.Add<ColorAdjustments>(true);color.postExposure.value=.4f;color.contrast.value=5;color.saturation.value=-6;
            var white=p.Add<WhiteBalance>(true);white.temperature.value=-4;
            var bloom=p.Add<Bloom>(true);bloom.intensity.value=.12f;bloom.threshold.value=1.5f;
            var grain=p.Add<FilmGrain>(true);grain.intensity.value=.045f;grain.response.value=.8f;
            var edge=p.Add<Vignette>(true);edge.intensity.value=.1f;edge.smoothness.value=.4f;
            foreach(var c in p.components)AssetDatabase.AddObjectToAsset(c,p);LevelBuilder.Save(p);return p;
        }
        static LightingSettings LightingSettings()
        {
            var s=new LightingSettings{name="IL_Lighting",lightmapper=UnityEngine.LightingSettings.Lightmapper.UnityComputeGPU,bakedGI=true,realtimeGI=false,lightmapResolution=10,lightmapMaxSize=1024,lightmapPadding=4,directionalityMode=LightmapsMode.CombinedDirectional,lightmapCompression=LightmapCompression.HighQuality,directSampleCount=32,indirectSampleCount=128,environmentSampleCount=64,minBounces=2,maxBounces=3,indirectScale=1,albedoBoost=1,mixedBakeMode=MixedLightingMode.IndirectOnly,lightProbeSampleCountMultiplier=2,ao=false,filteringMode=UnityEngine.LightingSettings.FilterMode.Advanced,denoiserTypeDirect=UnityEngine.LightingSettings.DenoiserType.None,denoiserTypeIndirect=UnityEngine.LightingSettings.DenoiserType.None,filterTypeDirect=UnityEngine.LightingSettings.FilterType.Gaussian,filterTypeIndirect=UnityEngine.LightingSettings.FilterType.Gaussian,filteringGaussianRadiusDirect=1,filteringGaussianRadiusIndirect=2};AssetDatabase.CreateAsset(s,SettingsPath);LevelBuilder.Save(s);return s;
        }
        public static void Prepare()
        {
            if(File.Exists(LevelBuilder.Evidence+"/LightingPrepared_A.json"))throw new InvalidOperationException("Preserve lighting setup");
            PipelineAssets();var volume=Volume();var settings=LightingSettings();
            LevelBuilder.Edit(root=>
            {
                Lightmapping.lightingSettings=settings;
                foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
                {
                    bool dynamic=r.GetComponentInParent<LevelPost>()||r.GetComponentsInParent<ArtSampleModel>().Any(m=>m.Role=="Echo");
                    var flags=GameObjectUtility.GetStaticEditorFlags(r.gameObject);if(dynamic){GameObjectUtility.SetStaticEditorFlags(r.gameObject,0);continue;}
                    if((flags&StaticEditorFlags.ContributeGI)!=0){GameObjectUtility.SetStaticEditorFlags(r.gameObject,flags|StaticEditorFlags.ReflectionProbeStatic);r.receiveGI=ReceiveGI.Lightmaps;r.scaleInLightmap=1;}
                    var model=r.GetComponentInParent<ArtSampleModel>();if(model&&(model.Role=="Floor"||model.Role=="Wall"||model.Role=="WallCap"))r.shadowCastingMode=ShadowCastingMode.TwoSided;
                }
                foreach(string path in AssetDatabase.FindAssets("t:Material",new[]{LevelBuilder.Generated}).Select(AssetDatabase.GUIDToAssetPath)){var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m.shader.name=="Universal Render Pipeline/Lit"&&m.name!="IL_Glass"){m.doubleSidedGI=true;if(m.name.Contains("Trim03")){m.SetFloat("_Metallic",.08f);m.SetFloat("_Smoothness",.35f);}if(m.name.Contains("ES_Light")){m.SetColor("_EmissionColor",new Color(.8f,.9f,1)*1.5f);}LevelBuilder.Save(m);}}
                var sun=root.GetComponentsInChildren<Light>().Single(l=>l.type==LightType.Directional);sun.transform.rotation=Quaternion.Euler(68,-25,0);sun.intensity=1.5f;sun.color=new Color(1,.96f,.88f);sun.lightmapBakeType=LightmapBakeType.Mixed;sun.shadows=LightShadows.Soft;sun.shadowBias=.04f;sun.shadowNormalBias=.25f;RenderSettings.sun=sun;
                for(int z=0;z<8;z++)for(int x=0;x<9;x++)if((x+z)%2==0)
                {
                    int zone=LevelLayout.Zone(x,z);float h=zone==1?6:4.5f;var p=LevelLayout.Cell(x,z);
                    LevelBuilder.Spawn("CeilingLight","CeilingLamp_"+x+"_"+z,p+Vector3.up*(h-.2f),Vector3.one,collide:false);
                    var l=new GameObject("BakedArea_"+x+"_"+z).AddComponent<Light>();l.transform.SetParent(root,false);l.type=LightType.Rectangle;l.lightmapBakeType=LightmapBakeType.Baked;l.transform.position=p+Vector3.up*(h-.24f);l.transform.rotation=Quaternion.Euler(90,0,0);l.areaSize=new Vector2(2.1f,.6f);l.intensity=zone==1?60:35;l.color=zone==2?new Color(1,.78f,.56f):zone==0?new Color(1,.9f,.78f):zone==3?new Color(.68f,.82f,1):new Color(.8f,.9f,1);l.shadows=LightShadows.Soft;
                }
                for(int i=0;i<2;i++){var l=new GameObject("RealtimeAccent_"+i).AddComponent<Light>();l.transform.SetParent(root,false);l.type=LightType.Spot;l.lightmapBakeType=LightmapBakeType.Realtime;l.transform.position=i==0?new Vector3(1.5f,4,-10):new Vector3(14,4,0);l.transform.LookAt(i==0?new Vector3(-3.1f,1.2f,-9):new Vector3(14,0,-2));l.color=i==0?new Color(1,.83f,.64f):new Color(1,.57f,.26f);l.range=8;l.spotAngle=65;l.intensity=i==0?5:3;l.shadows=i==0?LightShadows.Soft:LightShadows.None;}
                var probes=new GameObject("IL_LightProbes").AddComponent<LightProbeGroup>();probes.transform.SetParent(root,false);probes.probePositions=(from z in Enumerable.Range(0,8) from x in Enumerable.Range(0,9) from h in new[]{.4f,1.8f,3.3f} select LevelLayout.Cell(x,z)+Vector3.up*h).ToArray();
                for(int i=0;i<ProbePositions.Length;i++){var p=new GameObject("IL_Reflection_"+i).AddComponent<ReflectionProbe>();p.transform.SetParent(root,false);p.transform.position=ProbePositions[i];p.mode=ReflectionProbeMode.Baked;p.resolution=128;p.hdr=true;p.boxProjection=true;p.blendDistance=1.5f;p.size=i==0?new Vector3(36,5,8):i==7?new Vector3(36,5,4):new Vector3(12,6,12);p.intensity=.7f;p.clearFlags=ReflectionProbeClearFlags.SolidColor;p.backgroundColor=new Color(.08f,.12f,.18f);p.nearClipPlane=.1f;p.farClipPlane=60;}
                var v=new GameObject("IL_GlobalVolume").AddComponent<Volume>();v.transform.SetParent(root,false);v.isGlobal=true;v.sharedProfile=volume;
                var preview=root.GetComponentInChildren<LevelPreview>();preview.Configure(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath),preview.GetComponent<EchoStation.Player.PlayerInputReader>(),EchoStation.InfiltrationArt.Editor.ArtSampleScene.ResolveFont());
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.22f,.30f,.4f);RenderSettings.ambientEquatorColor=new Color(.09f,.13f,.17f);RenderSettings.ambientGroundColor=new Color(.035f,.05f,.065f);RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.6f;RenderSettings.fog=false;
                File.WriteAllText(LevelBuilder.Evidence+"/LightingPrepared_A.json",new JObject{{"bakedAreaLights",36},{"mixedMainLights",1},{"realtimeSpots",2},{"lightProbes",216},{"reflectionProbes",8},{"ssao",true},{"indirectBounces",3},{"ssr",false},{"sss",false}}.ToString());
            });
        }
        public static void Polish()
        {
            string backup=LevelBuilder.Evidence+"/LightingDraft_A";if(Directory.Exists(backup))throw new InvalidOperationException("Preserve lighting draft");Directory.CreateDirectory(backup);
            string sceneFolder=Path.GetDirectoryName(LevelBuilder.ScenePath);foreach(string file in Directory.GetFiles(sceneFolder,"*",SearchOption.AllDirectories)){string target=Path.Combine(backup,Path.GetRelativePath(sceneFolder,file));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(file,target,false);}
            LevelBuilder.Edit(root=>
            {
                foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("CeilingLamp_"))){float height=t.position.y+.2f;t.position=new Vector3(t.position.x,height-.02f,t.position.z);t.rotation=Quaternion.Euler(180,0,0);}
                var glass=AssetDatabase.LoadAssetAtPath<Material>(LevelBuilder.Generated+"/IL_Glass.mat");glass.SetFloat("_EnvironmentReflections",0);glass.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");glass.SetFloat("_SpecularHighlights",0);glass.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");LevelBuilder.Save(glass);
                var volume=AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);volume.TryGet<ColorAdjustments>(out var color);color.postExposure.value=.55f;LevelBuilder.Save(volume);
                File.WriteAllText(LevelBuilder.Evidence+"/LightingPolish_B.json",new JObject{{"ceilingEmissiveFaceTurnedDown",true},{"windowLowReflectionTransmissionApproximation",true},{"exposure",.55},{"rebakeRequired",true},{"oldMapsCopiedTo",backup}}.ToString());
            });
        }
        public static void BakeProbes(string version)
        {
            var root=LevelBuilder.World(UnityEngine.SceneManagement.SceneManager.GetActiveScene());var originalDefault=GraphicsSettings.defaultRenderPipeline;var originalQuality=QualitySettings.renderPipeline;int aa=QualitySettings.antiAliasing;
            var rows=new JArray();try
            {
                var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                foreach(var p in root.GetComponentsInChildren<ReflectionProbe>()){string path=LevelBuilder.Generated+"/"+p.name+"_"+version+".exr";if(File.Exists(path))throw new InvalidOperationException("Preserve probe output");bool ok=Lightmapping.BakeReflectionProbe(p,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var texture=AssetDatabase.LoadAssetAtPath<Texture>(path);if(!ok||!texture)throw new InvalidOperationException("Reflection output missing");p.mode=ReflectionProbeMode.Custom;p.customBakedTexture=texture;rows.Add(new JObject{{"probe",p.name},{"path",path},{"bytes",new FileInfo(path).Length},{"baked",true}});}
            }
            finally{GraphicsSettings.defaultRenderPipeline=originalDefault;QualitySettings.renderPipeline=originalQuality;QualitySettings.antiAliasing=aa;}
            File.WriteAllText(LevelBuilder.Evidence+"/ReflectionBakes_"+version+".json",rows.ToString());
        }
    }
}
