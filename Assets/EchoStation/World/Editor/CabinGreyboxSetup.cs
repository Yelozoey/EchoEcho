using System;
using System.IO;
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
    public static class CabinGreyboxSetup
    {
        public const string ScenePath="Assets/EchoStation/World/Scenes/ES_Cabin_Greybox.unity";
        public const string Folder="Assets/EchoStation/World/Greybox";
        public const string EvidencePath="Docs/Tests/Stage1/Evidence/S1-W01";
        public static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));
        }
        [MenuItem("Tools/EchoStation/Stage 1/W01/Create Cabin Greybox")]
        public static void Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))return;
            EnsureFolder(Folder);EnsureFolder("Assets/EchoStation/World/Scenes");Directory.CreateDirectory(EvidencePath);
            var paths=ForwardHdrBaselineSetup.TemplatePaths.Concat(new[]{ForwardHdrBaselineSetup.PipelinePath,ForwardHdrBaselineSetup.RendererPath,
                ForwardHdrBaselineSetup.ScenePath,ExplorationVolumeSetup.ScenePath,ExplorationVolumeSetup.BaseProfilePath,ExplorationVolumeSetup.ExplorationProfilePath,
                BakedLightingSetup.ScenePath,BakedLightingSetup.SettingsPath,RealtimeShadowFixtureSetup.ScenePath,OptimizationFixtureSetup.ScenePath});
            File.WriteAllText(Path.Combine(EvidencePath,"ProtectedFiles.json"),JsonUtility.ToJson(new ForwardHdrBaselineSetup.SetupRecord{
                capturedUtc=DateTime.UtcNow.ToString("O"),templates=paths.Select(p=>new ForwardHdrBaselineSetup.TemplateRecord{path=p,sha256=ForwardHdrBaselineSetup.Hash(p)}).ToArray()},true));
            var wall=MakeMaterial("ES_Cabin_WallGray",new Color(.47f,.52f,.59f));
            var floor=MakeMaterial("ES_Cabin_FloorBlueGray",new Color(.25f,.3f,.36f));var prop=MakeMaterial("ES_Cabin_PropGray",new Color(.32f,.36f,.4f));
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.08f,.1f,.14f);
                RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=0;RenderSettings.fog=false;
                var root=new GameObject("ES_Cabin_Greybox").transform;
                var main=Child("Cabin_Room_Main",root);var store=Child("Cabin_Room_Storage",root);var foyer=Child("Cabin_Room_Foyer",root);
                Box("Main_Floor",V(0,-.1f,0),V(10.2f,.2f,8.2f),main,floor);Box("Main_Ceiling",V(0,3.3f,0),V(10.2f,.2f,8.2f),main,floor);
                Box("Main_WallWest",V(-5.1f,1.6f,0),V(.2f,3.2f,8.4f),main,wall);
                Box("Main_WallNorth",V(0,1.6f,4.1f),V(10.2f,3.2f,.2f),main,wall);
                Box("Main_WallSouthLeft",V(-2.9f,1.6f,-4.1f),V(4.2f,3.2f,.2f),main,wall);
                Box("Main_WallSouthRight",V(2.9f,1.6f,-4.1f),V(4.2f,3.2f,.2f),main,wall);
                Box("Main_DoorHeaderSouth",V(0,2.8f,-4.1f),V(1.6f,.8f,.2f),main,wall);
                Box("Main_WallEastLower",V(5.1f,1.6f,-1.9f),V(.2f,3.2f,4.2f),main,wall);
                Box("Main_WallEastUpper",V(5.1f,1.6f,2.9f),V(.2f,3.2f,2.2f),main,wall);
                Box("Main_DoorHeaderEast",V(5.1f,2.8f,1),V(.2f,.8f,1.6f),main,wall);
                Box("Storage_Floor",V(7.1f,-.1f,1),V(4,.2f,6.2f),store,floor);Box("Storage_Ceiling",V(7.1f,3.3f,1),V(4,.2f,6.2f),store,floor);
                Box("Storage_WallEast",V(9.1f,1.6f,1),V(.2f,3.2f,6.4f),store,wall);
                Box("Storage_WallNorth",V(7.1f,1.6f,4.1f),V(4,3.2f,.2f),store,wall);
                Box("Storage_WallSouth",V(7.1f,1.6f,-2.1f),V(4,3.2f,.2f),store,wall);
                Box("Foyer_Floor",V(0,-.1f,-5.6f),V(3.2f,.2f,3),foyer,floor);Box("Foyer_Ceiling",V(0,2.9f,-5.6f),V(3.2f,.2f,3),foyer,floor);
                Box("Foyer_WallLeft",V(-1.6f,1.4f,-5.6f),V(.2f,2.8f,3),foyer,wall);
                Box("Foyer_WallRight",V(1.6f,1.4f,-5.6f),V(.2f,2.8f,3),foyer,wall);
                Box("Foyer_WallSouth",V(0,1.4f,-7.1f),V(3.4f,2.8f,.2f),foyer,wall);
                Box("Main_DeskTop",V(-2.5f,.85f,2.4f),V(3,.15f,1),main,prop,false);
                foreach(var x in new[]{-3.7f,-1.3f})foreach(var z in new[]{2.05f,2.75f})Box($"Main_DeskLeg_{x}_{z}",V(x,.4f,z),V(.15f,.8f,.15f),main,prop,false);
                Box("Main_TerminalProxy",V(-2.1f,1.15f,2.5f),V(.6f,.45f,.35f),main,prop,false);
                Box("Main_EquipmentCabinet",V(-3.8f,1.05f,0),V(1,2.1f,1.2f),main,prop,false);
                Box("Main_PowerPanelProxy",V(4.85f,1.4f,-1.5f),V(.2f,.8f,1),main,prop,false);
                Box("Storage_Crate",V(7,.6f,-.8f),V(1.4f,1.2f,1),store,prop,false);
                Box("Storage_ShelfProxy",V(7,1,3.4f),V(3,2,.6f),store,prop,false);
                var anchors=Child("Cabin_Anchors",root);
                Anchor("Spawn_Player",V(0,.05f,-6),anchors);Anchor("Spawn_Echo",V(1.5f,0,-2),anchors);
                Anchor("Anchor_NoteMaintenance",V(-2.8f,.95f,2.3f),anchors);Anchor("Anchor_NoteRoster",V(7.8f,1,3.05f),anchors);
                Anchor("Anchor_TerminalData",V(-2.1f,1.2f,2.25f),anchors);Anchor("Anchor_HiddenScanMark",V(8.97f,1.4f,2.8f),anchors);
                var camera=new GameObject("Cabin_ValidationCamera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";
                camera.transform.position=V(2.5f,1.65f,-2.5f);camera.transform.LookAt(V(-2,1.1f,2));camera.fieldOfView=70;camera.nearClipPlane=.05f;camera.farClipPlane=60;
                camera.allowHDR=true;camera.allowMSAA=false;camera.aspect=16f/9f;var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(0);data.allowHDROutput=false;data.renderPostProcessing=false;
                EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        private static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        private static Transform Child(string name,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        private static void Anchor(string name,Vector3 point,Transform parent){var t=Child(name,parent);t.position=point;}
        private static Material MakeMaterial(string name,Color color)
        {
            var path=$"{Folder}/{name}.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(material,path);return material;
        }
        private static void Box(string name,Vector3 center,Vector3 size,Transform parent,Material material,bool occluder=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=center;
            var mesh=UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);mesh.name=name;
            mesh.vertices=mesh.vertices.Select(v=>Vector3.Scale(v,size)).ToArray();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,$"{Folder}/{name}.asset");go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<BoxCollider>().size=size;
            go.GetComponent<Renderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.OccludeeStatic|(occluder?StaticEditorFlags.OccluderStatic:0));
        }
    }
}
