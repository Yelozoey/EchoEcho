using EchoStation.InfiltrationLevel.Editor;
using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.InfiltrationLevel;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace EchoStation.InfiltrationRefinement.Editor
{
    public static class RefinementLighting
    {
        public const string PipelinePath=RefinementScene.Generated+"/DR_ForwardHDR.asset",RendererPath=RefinementScene.Generated+"/DR_ForwardRenderer.asset",VolumePath=RefinementScene.Generated+"/DR_Volume.asset",SettingsPath=RefinementScene.Generated+"/DR_Lighting.asset";
        public static ScriptableRendererFeature Ao(UniversalRendererData renderer)=>LevelLighting.Ao(renderer);
        static Mesh face;
        static Color Tint(int zone)=>zone==2?new Color(1,.80f,.59f):zone==3?new Color(.65f,.82f,1):zone==0?new Color(1,.91f,.77f):new Color(.77f,.88f,1);
        static Material Emission(string name,Color color)
        {
            string path=RefinementScene.Generated+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true,globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive};m.SetColor("_BaseColor",color);m.SetColor("_EmissionColor",color*2.5f);m.EnableKeyword("_EMISSION");m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(m,path);RefinementScene.Save(m);return m;
        }
        static void Static(GameObject go)
        {foreach(var t in go.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic|StaticEditorFlags.OccludeeStatic);}
        static GameObject Face(Transform parent,string name,string kind,int zone,Vector3 point,Quaternion rotation,Vector2 size,Color? color=null)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=point;go.transform.rotation=rotation;go.transform.localScale=new Vector3(size.x,1,size.y);go.AddComponent<MeshFilter>().sharedMesh=face;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=Emission("DR_Emit_"+(color.HasValue?"Accent"+zone:zone.ToString()),color??Tint(zone));r.shadowCastingMode=ShadowCastingMode.Off;r.receiveGI=ReceiveGI.Lightmaps;r.scaleInLightmap=.1f;var marker=go.AddComponent<RefinementLightSurface>();marker.Kind=kind;marker.Zone=zone;Static(go);return go;
        }
        static void Fixture(Transform root,int zone,Vector3 point,Vector3 target)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor("CeilingLight"));var go=(GameObject)PrefabUtility.InstantiatePrefab(source,root.gameObject.scene);go.name="DR_WallWashFixture_"+zone;go.transform.SetParent(root,false);go.transform.position=point;var direction=target-point;direction.y=0;go.transform.rotation=Quaternion.LookRotation(Vector3.up,-direction.normalized);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(RefinementScene.MaterialFor).ToArray();Static(go);
            Face(root,"DR_WallEmission_"+zone,"Wall",zone,point+direction.normalized*.004f,go.transform.rotation,new Vector2(1.9f,.23f));
            var l=new GameObject("DR_BakedWallWash_"+zone).AddComponent<Light>();l.transform.SetParent(root,false);l.transform.position=point+direction.normalized*.06f;l.transform.LookAt(target);l.type=LightType.Rectangle;l.lightmapBakeType=LightmapBakeType.Baked;l.areaSize=new Vector2(1.9f,.3f);l.intensity=10;l.color=Tint(zone);l.shadows=LightShadows.Soft;
        }
        static void Strut(Transform root,int index,float x)
        {
            string source=ArtSampleAssets.PathFor("Column");var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),root.gameObject.scene);go.name="DR_RoofStrut_"+index;go.transform.SetParent(root,false);var b=ArtSampleAssets.LocalBoundsOf(go);go.transform.localScale=new Vector3(.14f/b.size.x,4.05f/b.size.y,.14f/b.size.z);go.transform.rotation=Quaternion.Euler(90,0,0);go.transform.position+=new Vector3(x,6.015f,-2)-ArtSampleAssets.BoundsOf(go).center;
            go.GetComponent<ArtSampleModel>().Configure("RoofStrut",source);foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterials=r.sharedMaterials.Select(RefinementScene.MaterialFor).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;r.receiveGI=ReceiveGI.Lightmaps;}Static(go);
        }
        public static void Polish()
        {
            string receipt=RefinementScene.Evidence+"/LightingPolished_A.json";if(File.Exists(receipt))throw new InvalidOperationException("Preserve polish receipt");
            face=new Mesh{name="DR_LightFace"};face.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};face.triangles=new[]{0,1,2,0,2,3};face.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};face.RecalculateNormals();face.RecalculateBounds();Unwrapping.GenerateSecondaryUVSet(face);AssetDatabase.CreateAsset(face,RefinementScene.Generated+"/DR_LightFace.asset");RefinementScene.Save(face);
            RefinementScene.Edit(root=>
            {
                string inherited=AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);if(!inherited.StartsWith(LevelBuilder.Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("Unexpected inherited lighting data");
                Lightmapping.lightingDataAsset=null;LightmapSettings.lightmaps=Array.Empty<LightmapData>();foreach(var r in root.GetComponentsInChildren<MeshRenderer>())r.lightmapIndex=-1;
                Lightmapping.lightingSettings=AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
                foreach(var lamp in root.GetComponentsInChildren<ArtSampleModel>().Where(m=>m.name.StartsWith("CeilingLamp_")))
                {var b=ArtSampleAssets.BoundsOf(lamp.gameObject);int x=Mathf.Clamp(Mathf.FloorToInt((b.center.x+18)/4),0,8),z=Mathf.Clamp(Mathf.FloorToInt((b.center.z+16)/4),0,7);Face(root,"DR_CeilingEmission_"+lamp.name,"Ceiling",LevelLayout.Zone(x,z),new Vector3(b.center.x,b.min.y-.004f,b.center.z),Quaternion.identity,new Vector2(b.size.x*.72f,b.size.z*.5f));}
                Fixture(root,0,new Vector3(0,3.3f,-15.72f),new Vector3(0,1.4f,-9));Fixture(root,1,new Vector3(5.72f,4.4f,-4),new Vector3(0,2,-2));Fixture(root,2,new Vector3(-17.72f,3,-2),new Vector3(-12,1.5f,-2));Fixture(root,3,new Vector3(17.72f,3,-2),new Vector3(12,1.5f,-2));Fixture(root,4,new Vector3(0,3.3f,15.72f),new Vector3(0,1.5f,12));Fixture(root,5,new Vector3(3.8f,3.6f,11.72f),new Vector3(0,1.5f,8));
                for(int i=0;i<4;i++)Strut(root,i,-1.5f+i);
                foreach(var entry in new[]{new Vector3(0,0,-8.28f),new Vector3(-12,2,-8.28f),new Vector3(12,3,-8.28f),new Vector3(0,5,3.72f)})foreach(float dx in new[]{-1.82f,1.82f})
                {int zone=(int)entry.y;Face(root,"DR_GateAccent_"+zone+"_"+dx,"Gate",zone,new Vector3(entry.x+dx,2.65f,entry.z),Quaternion.Euler(90,0,0),new Vector2(.045f,.75f),zone==2?new Color(1,.52f,.19f):zone==3?new Color(.2f,.85f,.65f):zone==5?new Color(1,.42f,.15f):new Color(.2f,.6f,1));}
                var sun=root.GetComponentsInChildren<Light>().Single(l=>l.type==LightType.Directional);sun.transform.rotation=Quaternion.Euler(52,-35,0);sun.intensity=1.9f;
                var accents=root.GetComponentsInChildren<Light>().Where(l=>l.lightmapBakeType==LightmapBakeType.Realtime).OrderBy(l=>l.name).ToArray();accents[0].transform.position=new Vector3(-1,3.6f,-11);accents[0].transform.LookAt(new Vector3(-3.1f,1.2f,-9));accents[0].intensity=4;accents[1].transform.position=new Vector3(-2,3.5f,5.5f);accents[1].transform.LookAt(new Vector3(1.5f,1.2f,5.7f));accents[1].intensity=2.2f;
                float[] smooth={.3f,.32f,.22f,.4f,.2f,.34f};foreach(var floor in root.GetComponentsInChildren<LevelFloor>())foreach(var r in floor.GetComponentsInChildren<MeshRenderer>())
                {r.sharedMaterials=r.sharedMaterials.Select(m=>{string path=RefinementScene.Generated+"/DR_Floor_"+floor.Zone+"_"+m.name+".mat";var own=AssetDatabase.LoadAssetAtPath<Material>(path);if(!own){own=UnityEngine.Object.Instantiate(m);own.name=System.IO.Path.GetFileNameWithoutExtension(path);own.SetFloat("_Smoothness",smooth[floor.Zone]);AssetDatabase.CreateAsset(own,path);RefinementScene.Save(own);}return own;}).ToArray();}
                var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);profile.TryGet<ColorAdjustments>(out var color);if(Mathf.Abs(color.postExposure.value-.55f)>.001)throw new InvalidOperationException("Preserve comparison exposure");profile.TryGet<Bloom>(out var bloom);bloom.intensity.value=.16f;RefinementScene.Save(profile);
                File.WriteAllText(receipt,new JObject{{"inheritedDataDetached",inherited},{"newLightingDataBeforeBake",AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset)},{"exposure",.55},{"emissiveFaces",50},{"bakedLights",42},{"mixedMainLights",1},{"realtimeSpots",2},{"newRealtimeShadowLights",0},{"roofStruts",4},{"rebakeRequired",true},{"oldA01Modified",false}}.ToString());
            });
        }
        public static void BakeProbes(string version)
        {
            var root=RefinementScene.World(UnityEngine.SceneManagement.SceneManager.GetActiveScene());var previousDefault=GraphicsSettings.defaultRenderPipeline;var previousQuality=QualitySettings.renderPipeline;int aa=QualitySettings.antiAliasing;var rows=new JArray();
            try{var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;foreach(var p in root.GetComponentsInChildren<ReflectionProbe>()){string path=RefinementScene.Generated+"/"+p.name+"_"+version+".exr";if(File.Exists(path))throw new InvalidOperationException("Preserve reflection version");bool ok=Lightmapping.BakeReflectionProbe(p,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var texture=AssetDatabase.LoadAssetAtPath<Texture>(path);if(!ok||!texture)throw new InvalidOperationException("Reflection output missing");p.mode=ReflectionProbeMode.Custom;p.customBakedTexture=texture;rows.Add(new JObject{{"probe",p.name},{"path",path},{"bytes",new FileInfo(path).Length},{"baked",true}});}}
            finally{GraphicsSettings.defaultRenderPipeline=previousDefault;QualitySettings.renderPipeline=previousQuality;QualitySettings.antiAliasing=aa;}
            File.WriteAllText(RefinementScene.Evidence+"/ReflectionBakes_"+version+".json",rows.ToString());
        }
    }
}
