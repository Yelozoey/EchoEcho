using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.Player;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.ReflectionSample.Editor
{
    public static class ReflectionSampleBuilder
    {
        public const string Root="Assets/EchoStation/ReflectionSample",Generated=Root+"/Generated",ScenePath=Root+"/Scenes/ES_ReflectionSample.unity",Evidence="Docs/Tests/InfiltrationPrototype/Evidence/A01R";
        public const string PipelinePath=Generated+"/RS_ForwardHDR.asset",RendererPath=Generated+"/RS_Renderer.asset",VolumePath=Generated+"/RS_Volume.asset";
        public static readonly Vector3 Start=new(0,.08f,-2.55f);
        public static Transform World(Scene s)=>s.GetRootGameObjects().Single(g=>g.name=="RS_World").transform;
        public static void Save(Object o){if(!AssetDatabase.GetAssetPath(o).StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("Owned sample save only");EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);}
        static void Copy(string source,string target){if(File.Exists(target)||!AssetDatabase.CopyAsset(source,target))throw new IOException("Preserve asset: "+target);}
        static Material Own(Material m)
        {
            var path=Generated+"/RS_"+m.name.Replace(' ','_')+".mat";var own=AssetDatabase.LoadAssetAtPath<Material>(path);if(!own){own=Object.Instantiate(m);AssetDatabase.CreateAsset(own,path);Save(own);}return own;
        }
        static Material Lit(string name,Color color,float metal,float smooth)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,Generated+"/"+name+".mat");Save(m);return m;
        }
        static GameObject Model(Transform root,string role,string name,Vector3 point,Vector3 scale,float yaw=0,bool collider=false)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor(role)),root.gameObject.scene);go.name=name;go.transform.SetParent(root,false);go.transform.position=point;go.transform.localScale=scale;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(Own).ToArray();r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;}
            if(collider){var b=ArtSampleAssets.LocalBoundsOf(go);var c=go.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;}
            foreach(var t in go.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.ReflectionProbeStatic);return go;
        }
        static GameObject Primitive(Transform root,PrimitiveType type,string name,Vector3 position,Vector3 scale,Material m,bool collide=false)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=m;if(!collide)Object.DestroyImmediate(go.GetComponent<Collider>());GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ReflectionProbeStatic);return go;}
        static void Label(Transform root,string text,Vector3 point,Material material)
        {var go=new GameObject("RS_Label_"+text);go.transform.SetParent(root,false);go.transform.position=point;var t=go.AddComponent<TextMesh>();t.text=text;t.font=ArtSampleScene.ResolveFont();t.fontSize=72;t.characterSize=.018f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.8f,.91f,1);t.GetComponent<Renderer>().sharedMaterial=material;}
        public static void Edit(Action<Transform> edit)
        {SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);edit(World(scene));if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save sample");}finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}}
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();if(File.Exists(ScenePath))throw new InvalidOperationException("Preserve sample");ArtSampleAssets.Folder(Generated);ArtSampleAssets.Folder(Root+"/Scenes");Directory.CreateDirectory(Evidence);
            const string source="Assets/EchoStation/InfiltrationRefinement/Generated/";
            Copy(source+"DR_ForwardHDR.asset",PipelinePath);Copy("Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardRenderer.asset",RendererPath);Copy(source+"DR_Volume.asset",VolumePath);
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);renderer.rendererFeatures.Clear();var rs=new SerializedObject(renderer);rs.FindProperty("m_RendererFeatureMap").arraySize=0;rs.FindProperty("m_RenderingMode").intValue=0;rs.ApplyModifiedPropertiesWithoutUndo();renderer.SetDirty();Save(renderer);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);var ps=new SerializedObject(pipeline);ps.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;ps.ApplyModifiedPropertiesWithoutUndo();pipeline.supportsHDR=true;pipeline.msaaSampleCount=1;pipeline.renderScale=1;Save(pipeline);
            var volume=AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);volume.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;volume.TryGet<ColorAdjustments>(out var color);color.postExposure.value=.25f;volume.TryGet<Vignette>(out var vignette);vignette.intensity.value=0;foreach(var c in volume.components)Save(c);Save(volume);
            var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=new GameObject("RS_World").transform;
                Model(root,"Floor","RS_RoomFloor",Vector3.zero,new Vector3(1.5f,1,1.5f));var floor=root.Find("RS_RoomFloor").gameObject;var fc=floor.AddComponent<BoxCollider>();fc.center=Vector3.down*.08f;fc.size=new Vector3(4,.16f,4);
                Model(root,"Floor","RS_RoomCeiling",new Vector3(0,4.5f,0),new Vector3(1.5f,1,1.5f)).transform.rotation=Quaternion.Euler(180,0,0);
                for(int i=0;i<4;i++){float a=i*90;var p=Quaternion.Euler(0,a,0)*new Vector3(-3,0,0);Model(root,"Wall","RS_Boundary_"+i,p,new Vector3(1,1.5f,1.5f),a+180,true);}
                var rough=Lit("RS_RoughMetal",new Color(.68f,.71f,.76f),1,.22f);var polished=Lit("RS_PolishedMetal",new Color(.68f,.71f,.76f),1,.97f);
                var painted=Own(AssetDatabase.LoadAssetAtPath<Material>("Assets/EchoStation/InfiltrationRefinement/Generated/DR_IL_IA_ES_Trim03.mat"));
                var baseMat=Lit("RS_Stand",new Color(.08f,.11f,.15f),.1f,.3f);var mirror=Lit("RS_ProbeFloor",new Color(.65f,.69f,.74f),1,.94f);
                Primitive(root,PrimitiveType.Cube,"RS_MirrorSurface",new Vector3(0,.025f,0),new Vector3(5,.04f,3.6f),mirror,true);
                var label=new Material(Shader.Find("EchoStation/InfiltrationLevel/WorldLabel")){name="RS_LabelMaterial"};label.mainTexture=ArtSampleScene.ResolveFont().material.mainTexture;AssetDatabase.CreateAsset(label,Generated+"/RS_Label.mat");Save(label);
                for(int i=0;i<3;i++){float x=(i-1)*1.65f;Primitive(root,PrimitiveType.Cylinder,"RS_DiagnosticStand_"+i,new Vector3(x,.39f,2.1f),new Vector3(.85f,.39f,.85f),baseMat);Primitive(root,PrimitiveType.Sphere,"RS_TestSphere_"+i,new Vector3(x,1.22f,2.1f),Vector3.one*.85f,i==0?rough:i==1?polished:painted);Label(root,i==0?"粗糙金属":i==1?"抛光金属":"原涂装材质",new Vector3(x,2.05f,2.45f),label);}
                var robot=Model(root,"Guard","RS_RobotDemo",new Vector3(0,.045f,.35f),Vector3.one*.7f,180);foreach(var t in robot.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
                var red=Lit("RS_Red",new Color(.65f,.055f,.02f),0,.55f);var blue=Lit("RS_Blue",new Color(.025f,.24f,.65f),0,.55f);
                Model(root,"EquipmentCase","RS_RedCase",new Vector3(-2,.045f,.6f),Vector3.one*.3f,180);Model(root,"EquipmentCase","RS_BlueCase",new Vector3(2,.045f,.6f),Vector3.one*.3f,180);
                foreach(var r in root.Find("RS_RedCase").GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>red).ToArray();foreach(var r in root.Find("RS_BlueCase").GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>blue).ToArray();
                for(int i=0;i<2;i++)
                {
                    var fixture=Model(root,"CeilingLight","RS_Lamp_"+i,new Vector3(i==0?-1.6f:1.6f,4.48f,0),new Vector3(.65f,1,1));fixture.transform.rotation=Quaternion.Euler(180,0,0);
                    var e=Lit("RS_Emission_"+i,i==0?new Color(1,.42f,.12f):new Color(.12f,.62f,1),0,.4f);e.SetColor("_EmissionColor",(i==0?new Color(1,.3f,.04f):new Color(.04f,.5f,1))*4);e.EnableKeyword("_EMISSION");Save(e);Primitive(root,PrimitiveType.Cube,"RS_ColorLight_"+i,new Vector3(i==0?-2.65f:2.65f,2.5f,1),new Vector3(.08f,2,.12f),e);
                    var l=new GameObject("RS_Key_"+i).AddComponent<Light>();l.transform.SetParent(root,false);l.type=LightType.Spot;l.transform.position=new Vector3(i==0?-1.7f:1.7f,3.7f,-1);l.transform.LookAt(new Vector3(0,.7f,1.2f));l.range=9;l.spotAngle=95;l.intensity=7;l.color=i==0?new Color(1,.87f,.68f):new Color(.63f,.82f,1);l.shadows=i==0?LightShadows.Soft:LightShadows.None;
                }
                var sun=new GameObject("RS_MainLight").AddComponent<Light>();sun.transform.SetParent(root,false);sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(48,-28,0);sun.intensity=.55f;sun.shadows=LightShadows.None;
                var probe=new GameObject("RS_Probe").AddComponent<ReflectionProbe>();probe.transform.SetParent(root,false);probe.transform.position=new Vector3(0,1.7f,0);probe.size=new Vector3(6,4.5f,6);probe.center=new Vector3(0,.55f,0);probe.mode=ReflectionProbeMode.Baked;probe.resolution=512;probe.hdr=true;probe.boxProjection=true;probe.intensity=1;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.2f,.25f,.32f);probe.farClipPlane=15;
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.28f,.32f,.4f);RenderSettings.fog=false;RenderSettings.reflectionIntensity=1;
                var v=new GameObject("RS_Volume").AddComponent<Volume>();v.transform.SetParent(root,false);v.isGlobal=true;v.sharedProfile=volume;
                var player=new GameObject("RS_Player");player.SetActive(false);player.transform.SetParent(root,false);player.transform.position=Start;
                var cc=player.AddComponent<CharacterController>();cc.height=1.75f;cc.radius=.25f;cc.center=Vector3.up*.875f;cc.minMoveDistance=0;cc.skinWidth=.03f;
                var camera=new GameObject("RS_Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(player.transform,false);camera.transform.localPosition=Vector3.up*1.6f;camera.transform.localRotation=Quaternion.Euler(12,0,0);camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.farClipPlane=25;camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.12f,.18f);camera.gameObject.AddComponent<AudioListener>();
                var data=camera.GetUniversalAdditionalCameraData();data.allowHDROutput=false;data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                var input=player.AddComponent<PlayerInputReader>();player.AddComponent<SalvageMotor>().Configure(camera.transform);root.gameObject.AddComponent<ReflectionPreview>().Configure(pipeline,ArtSampleScene.ResolveFont(),input);player.SetActive(true);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Save R01");
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void BakeProbe(string version)
        {
            Edit(root=>
            {
                var d=GraphicsSettings.defaultRenderPipeline;var q=QualitySettings.renderPipeline;int aa=QualitySettings.antiAliasing;
                try{GraphicsSettings.defaultRenderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);QualitySettings.renderPipeline=GraphicsSettings.defaultRenderPipeline;var p=root.GetComponentInChildren<ReflectionProbe>();string path=Generated+"/RS_Probe_"+version+".exr";if(File.Exists(path))throw new IOException("Preserve bake");if(!Lightmapping.BakeReflectionProbe(p,path))throw new InvalidOperationException("Bake failed");AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var texture=AssetDatabase.LoadAssetAtPath<Texture>(path);if(!texture)throw new IOException("Actual Cubemap missing");p.mode=ReflectionProbeMode.Custom;p.customBakedTexture=texture;RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=texture;File.WriteAllText(Evidence+"/ProbeBake_"+version+".json",new JObject{{"path",path},{"width",texture.width},{"bytes",new FileInfo(path).Length},{"actual",true}}.ToString());}
                finally{GraphicsSettings.defaultRenderPipeline=d;QualitySettings.renderPipeline=q;QualitySettings.antiAliasing=aa;}
            });
        }
    }
}
