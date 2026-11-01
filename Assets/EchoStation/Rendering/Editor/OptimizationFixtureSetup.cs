using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class OptimizationFixtureSetup
    {
        public const string ScenePath="Assets/EchoStation/Tests/Scenes/S1_R05_Optimization.unity";
        public const string EvidencePath="Docs/Tests/Stage1/Evidence/S1-R05";
        public const string Folder="Assets/EchoStation/Tests/Fixtures/R05";
        [MenuItem("Tools/EchoStation/Stage 1/R05/Create Optimization Fixture")]
        public static void Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))return;
            Directory.CreateDirectory(EvidencePath);ForwardHdrBaselineSetup.EnsureFolder(Folder);
            var protectedPaths=ForwardHdrBaselineSetup.TemplatePaths.Concat(new[]{ForwardHdrBaselineSetup.PipelinePath,ForwardHdrBaselineSetup.RendererPath,
                ExplorationVolumeSetup.BaseProfilePath,ExplorationVolumeSetup.ExplorationProfilePath,BakedLightingSetup.ScenePath,RealtimeShadowFixtureSetup.ScenePath});
            File.WriteAllText(Path.Combine(EvidencePath,"ProtectedFiles.json"),JsonUtility.ToJson(new ForwardHdrBaselineSetup.SetupRecord{
                capturedUtc=DateTime.UtcNow.ToString("O"),templates=protectedPaths.Select(p=>new ForwardHdrBaselineSetup.TemplateRecord{path=p,sha256=ForwardHdrBaselineSetup.Hash(p)}).ToArray()},true));
            var wall=Material("R05_Wall",new Color(.25f,.3f,.36f));var near=Material("R05_Near",new Color(.5f,.45f,.35f));
            var far=Material("R05_Far",new Color(.25f,.5f,.8f),true);
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;
                RenderSettings.ambientLight=Color.black;RenderSettings.fog=false;
                var walls=new GameObject("R05_Walls").transform;
                Box("Floor",new Vector3(0,-.15f,4),new Vector3(12.6f,.3f,20.6f),walls,wall,true);
                Box("Ceiling",new Vector3(0,4.15f,4),new Vector3(12.6f,.3f,20.6f),walls,wall,true);
                Box("Left",new Vector3(-6.15f,2,4),new Vector3(.3f,4,20),walls,wall,true);
                Box("Right",new Vector3(6.15f,2,4),new Vector3(.3f,4,20),walls,wall,true);
                Box("Front",new Vector3(0,2,-6.15f),new Vector3(12,.3f+3.7f,.3f),walls,wall,true);
                Box("Back",new Vector3(0,2,14.15f),new Vector3(12,4,.3f),walls,wall,true);
                Box("Partition",new Vector3(0,2,4),new Vector3(12,4,.5f),walls,wall,true);
                var nearRoom=new GameObject("R05_NearRoom").transform;
                for(var z=0;z<2;z++)for(var x=0;x<4;x++)Box($"Near_{z}_{x}",new Vector3(-3.6f+x*2.4f,.5f,.2f+z*2),Vector3.one,nearRoom,near,false);
                var farRoom=new GameObject("R05_FarRoom").transform;
                for(var z=0;z<3;z++)for(var x=0;x<4;x++)Box($"Far_{z}_{x}",new Vector3(-3.6f+x*2.4f,.65f,7+z*2.3f),new Vector3(1.2f,1.3f,1.2f),farRoom,far,false);
                var merged=StaticRoomMeshCombiner.Combine(nearRoom,nearRoom.GetComponentsInChildren<MeshRenderer>(),Folder+"/R05_NearCombined.asset");
                merged.name="R05_NearCombined";merged.enabled=false;
                foreach(var r in nearRoom.GetComponentsInChildren<MeshRenderer>())if(r!=merged)r.enabled=true;
                var area=new GameObject("R05_ViewVolume",typeof(OcclusionArea)).GetComponent<OcclusionArea>();
                area.center=new Vector3(0,2,4);area.size=new Vector3(11.6f,3.6f,19.6f);
                var camera=new GameObject("R05_ValidationCamera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";
                camera.transform.position=new Vector3(0,1.6f,-4);camera.transform.rotation=Quaternion.identity;
                camera.nearClipPlane=.1f;camera.farClipPlane=45;camera.fieldOfView=70;camera.aspect=16f/9f;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=true;camera.allowMSAA=false;camera.useOcclusionCulling=true;
                var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(0);data.renderPostProcessing=false;data.allowHDROutput=false;
                EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            // Own new, closed fixture only: Unity's serialized scene setting, verified on reload.
            var text=File.ReadAllText(ScenePath);if(!text.Contains("smallestOccluder: 5"))throw new InvalidOperationException("Unexpected occlusion defaults; inspect before editing.");
            File.WriteAllText(ScenePath,text.Replace("smallestOccluder: 5","smallestOccluder: 2"));AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceUpdate);
        }
        private static Material Material(string name,Color color,bool clip=false)
        {
            var path=$"{Folder}/{name}.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name,enableInstancing=false};material.SetColor("_BaseColor",color);
            if(clip){material.SetFloat("_AlphaClip",1);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;}
            AssetDatabase.CreateAsset(material,path);return material;
        }
        private static void Box(string name,Vector3 position,Vector3 size,Transform parent,Material material,bool occluder)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.position=position;go.transform.localScale=size;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ContributeGI|(occluder?StaticEditorFlags.OccluderStatic:0));
        }
    }
}
