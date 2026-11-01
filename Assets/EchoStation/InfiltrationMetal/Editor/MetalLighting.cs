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
namespace EchoStation.InfiltrationMetal.Editor
{
    public static class MetalLighting
    {
        public const string PipelinePath=MetalScene.PipelinePath,RendererPath=MetalScene.RendererPath,VolumePath=MetalScene.VolumePath,SettingsPath=MetalScene.SettingsPath;
        public static ScriptableRendererFeature Ao(UniversalRendererData r)=>LevelLighting.Ao(r);
        public static void Prepare()
        {
            string receipt=MetalScene.Evidence+"/LightingPrepared_A.json";if(File.Exists(receipt))throw new InvalidOperationException("Preserve lighting setup");
            string auxPath=MetalScene.Generated+"/MM_ReflectionRenderer.asset";if(File.Exists(auxPath)||!AssetDatabase.CopyAsset(RendererPath,auxPath))throw new IOException("Preserve reflection Renderer");
            var aux=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(auxPath);aux.rendererFeatures.Clear();var settings=new SerializedObject(aux);settings.FindProperty("m_RendererFeatureMap").arraySize=0;settings.ApplyModifiedPropertiesWithoutUndo();aux.SetDirty();MetalScene.Save(aux);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);var serialized=new SerializedObject(pipeline);var list=serialized.FindProperty("m_RendererDataList");list.arraySize=2;list.GetArrayElementAtIndex(0).objectReferenceValue=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);list.GetArrayElementAtIndex(1).objectReferenceValue=aux;serialized.ApplyModifiedPropertiesWithoutUndo();MetalScene.Save(pipeline);
            MetalScene.Edit(root=>
            {
                string inherited=AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);if(!inherited.StartsWith("Assets/EchoStation/InfiltrationRefinement/",StringComparison.Ordinal))throw new InvalidOperationException("Unexpected source bake");Lightmapping.lightingDataAsset=null;LightmapSettings.lightmaps=Array.Empty<LightmapData>();foreach(var r in root.GetComponentsInChildren<MeshRenderer>())r.lightmapIndex=-1;Lightmapping.lightingSettings=AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
                var probes=root.GetComponentsInChildren<ReflectionProbe>().OrderBy(p=>p.name).ToArray();for(int i=0;i<probes.Length;i++){probes[i].resolution=i==1||i==2?256:128;probes[i].intensity=1;}
                var mesh=new Mesh{name="MM_ReflectivePatch"};mesh.vertices=new[]{new Vector3(-2,0,-2),new Vector3(-2,0,2),new Vector3(2,0,2),new Vector3(2,0,-2)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,MetalScene.Generated+"/MM_ReflectivePatch.asset");MetalScene.Save(mesh);
                var material=new Material(Shader.Find("EchoStation/ReflectionSample/Planar PBR")){name="MM_PlanarFloor"};material.SetColor("_BaseColor",new Color(.6f,.66f,.73f));material.SetFloat("_Metallic",1);material.SetFloat("_Roughness",.14f);material.SetFloat("_PlanarStrength",.85f);AssetDatabase.CreateAsset(material,MetalScene.Generated+"/MM_PlanarFloor.mat");MetalScene.Save(material);
                var go=new GameObject("MM_ReflectivePatch");go.transform.SetParent(root,false);go.transform.position=new Vector3(0,.012f,-2);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
                var planar=go.AddComponent<MetalPlanar>();planar.sourceCamera=root.GetComponentInChildren<Camera>();planar.textureWidth=1024;root.gameObject.AddComponent<MetalReflectionView>().Configure(planar,ArtSampleScene.ResolveFont());
                File.WriteAllText(receipt,new JObject{{"oldDataDetached",inherited},{"newDataBeforeBake",AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset)},{"planes",1},{"patchArea",16},{"probe256",2},{"probe128",6},{"mainRendererFeaturesPreserved",true},{"reflectionRendererFeatureCount",0},{"extraRealtimeLights",0},{"exposure",.55}}.ToString());
            });
        }
        public static void BakeProbes(string version)
        {
            var root=MetalScene.World(UnityEngine.SceneManagement.SceneManager.GetActiveScene());var d=GraphicsSettings.defaultRenderPipeline;var q=QualitySettings.renderPipeline;int aa=QualitySettings.antiAliasing;var rows=new JArray();
            try
            {
                GraphicsSettings.defaultRenderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);QualitySettings.renderPipeline=GraphicsSettings.defaultRenderPipeline;
                foreach(var p in root.GetComponentsInChildren<ReflectionProbe>())
                {
                    string path=MetalScene.Generated+"/MM_"+p.name+"_"+version+".exr";if(File.Exists(path))throw new IOException("Preserve reflection version");if(!Lightmapping.BakeReflectionProbe(p,path))throw new InvalidOperationException("Reflection failed");AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var texture=AssetDatabase.LoadAssetAtPath<Texture>(path);if(!texture)throw new IOException("Reflection image missing");p.mode=ReflectionProbeMode.Custom;p.customBakedTexture=texture;rows.Add(new JObject{{"path",path},{"resolution",p.resolution},{"bytes",new FileInfo(path).Length},{"baked",true}});
                }
                File.WriteAllText(MetalScene.Evidence+"/ReflectionBakes_"+version+".json",rows.ToString());
            }
            finally{GraphicsSettings.defaultRenderPipeline=d;QualitySettings.renderPipeline=q;QualitySettings.antiAliasing=aa;}
        }
    }
}
