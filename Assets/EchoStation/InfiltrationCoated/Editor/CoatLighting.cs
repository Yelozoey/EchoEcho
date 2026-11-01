using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.InfiltrationLevel.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace EchoStation.InfiltrationCoated.Editor
{
    public static class CoatLighting
    {
        public const string PipelinePath=CoatScene.PipelinePath,RendererPath=CoatScene.RendererPath,VolumePath=CoatScene.VolumePath,SettingsPath=CoatScene.SettingsPath;
        public static ScriptableRendererFeature Ao(UniversalRendererData r)=>LevelLighting.Ao(r);
        public static void Prepare()
        {
            string receipt=CoatScene.Evidence+"/LightingPrepared_A.json";if(File.Exists(receipt))throw new InvalidOperationException("Preserve setup");
            string auxPath=CoatScene.Generated+"/CF_ReflectionRenderer.asset";if(File.Exists(auxPath)||!AssetDatabase.CopyAsset(RendererPath,auxPath))throw new IOException("Preserve auxiliary Renderer");
            var aux=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(auxPath);aux.rendererFeatures.Clear();var fields=new SerializedObject(aux);fields.FindProperty("m_RendererFeatureMap").arraySize=0;fields.ApplyModifiedPropertiesWithoutUndo();aux.SetDirty();CoatScene.Save(aux);
            var pipe=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);var settings=new SerializedObject(pipe);var list=settings.FindProperty("m_RendererDataList");list.arraySize=2;list.GetArrayElementAtIndex(0).objectReferenceValue=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);list.GetArrayElementAtIndex(1).objectReferenceValue=aux;settings.ApplyModifiedPropertiesWithoutUndo();CoatScene.Save(pipe);
            CoatScene.Edit(root=>
            {
                string inherited=AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);if(!inherited.StartsWith("Assets/EchoStation/InfiltrationMetal/",StringComparison.Ordinal))throw new InvalidOperationException("Unexpected inherited bake");
                Lightmapping.lightingDataAsset=null;LightmapSettings.lightmaps=Array.Empty<LightmapData>();foreach(var r in root.GetComponentsInChildren<MeshRenderer>())r.lightmapIndex=-1;Lightmapping.lightingSettings=AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
                var probes=root.GetComponentsInChildren<ReflectionProbe>().OrderBy(p=>p.name).ToArray();for(int i=0;i<probes.Length;i++){probes[i].resolution=i==1||i==2?256:128;probes[i].intensity=1;}
                var plane=root.GetComponentInChildren<CoatPlanar>();if(!plane||root.GetComponentsInChildren<CoatPlanar>().Length!=1)throw new InvalidOperationException("One inherited plane required");
                File.WriteAllText(receipt,new JObject{{"oldDataDetached",inherited},{"newDataBeforeBake",AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset)},{"planes",1},{"patchArea",16},{"probe256",2},{"probe128",6},{"mainRendererFeaturesPreserved",true},{"reflectionRendererFeatureCount",0},{"extraRealtimeLights",0},{"exposure",.55}}.ToString());
            });
        }
        public static void BakeProbes(string version)
        {
            var root=CoatScene.World(UnityEngine.SceneManagement.SceneManager.GetActiveScene());var d=GraphicsSettings.defaultRenderPipeline;var q=QualitySettings.renderPipeline;int aa=QualitySettings.antiAliasing;var rows=new JArray();
            try
            {
                GraphicsSettings.defaultRenderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);QualitySettings.renderPipeline=GraphicsSettings.defaultRenderPipeline;
                foreach(var p in root.GetComponentsInChildren<ReflectionProbe>())
                {
                    string path=CoatScene.Generated+"/CF_"+p.name+"_"+version+".exr";if(File.Exists(path))throw new IOException("Preserve reflection version");if(!Lightmapping.BakeReflectionProbe(p,path))throw new InvalidOperationException("Reflection failed");AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var texture=AssetDatabase.LoadAssetAtPath<Texture>(path);if(!texture)throw new IOException("Reflection image missing");p.mode=ReflectionProbeMode.Custom;p.customBakedTexture=texture;rows.Add(new JObject{{"path",path},{"resolution",p.resolution},{"bytes",new FileInfo(path).Length},{"baked",true}});
                }
                File.WriteAllText(CoatScene.Evidence+"/ReflectionBakes_"+version+".json",rows.ToString());
            }
            finally{GraphicsSettings.defaultRenderPipeline=d;QualitySettings.renderPipeline=q;QualitySettings.antiAliasing=aa;}
        }
    }
}
