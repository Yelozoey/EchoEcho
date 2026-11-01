using System;
using EchoStation.Player;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Salvage.Editor
{
    public static class SalvageWorldSetup
    {
        public const string ScenePath=SalvageAssetImport.Root+"/Scenes/ES_Salvage_Demo.unity";
        public const string Pipeline="Assets/EchoStation/Station/Generated/ES_S2_ForwardHDR.asset";
        [MenuItem("Tools/EchoStation/Salvage/R1-G01/Create Greybox")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))throw new InvalidOperationException("Greybox exists; edit owned scene in later modules instead of replacing it.");
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=new GameObject("ES_Salvage_World");root.AddComponent<SalvagePipelineScope>().Configure(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(Pipeline));
                var floorRoot=new GameObject("Floor_36x24");floorRoot.transform.SetParent(root.transform);
                for(int x=0;x<9;x++)for(int z=0;z<6;z++){var tile=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SalvagePrefabSetup.Path(0)),scene);tile.transform.SetParent(floorRoot.transform);tile.transform.position=new Vector3(2+4*x,0,2+4*z);}
                var wall=Material("ES_Greybox_Wall",new Color(.14f,.21f,.27f));
                Block(root,"West",new Vector3(-.15f,1.5f,12),new Vector3(.3f,3,24.6f),wall);Block(root,"East",new Vector3(36.15f,1.5f,12),new Vector3(.3f,3,24.6f),wall);
                Block(root,"South",new Vector3(18,1.5f,-.15f),new Vector3(36,3,.3f),wall);Block(root,"North",new Vector3(18,1.5f,24.15f),new Vector3(36,3,.3f),wall);
                // x12: main entrance z18, shortcut z6; x24: workshop entrance z12.
                Segment(root,12,0,4.5f,wall);Segment(root,12,7.5f,16.5f,wall);Segment(root,12,19.5f,24,wall);
                Segment(root,24,0,10.5f,wall);Segment(root,24,13.5f,24,wall);
                var marker=Material("ES_Recovery_Blue",new Color(.08f,.48f,.7f));Block(root,"RecoveryPoint",new Vector3(5,.04f,12),new Vector3(3,.08f,3),marker);
                Label(root,"回收泊位",new Vector3(2,2.5f,13));Label(root,"储藏区",new Vector3(14,2.5f,13));Label(root,"检修间",new Vector3(26,2.5f,13));Label(root,"返回短路",new Vector3(11,2.5f,6));
                var player=new GameObject("SalvagePlayer");player.SetActive(false);player.transform.SetParent(root.transform);player.transform.position=new Vector3(5,.15f,12);player.transform.rotation=Quaternion.Euler(0,90,0);
                var cc=player.AddComponent<CharacterController>();cc.height=1.75f;cc.center=new Vector3(0,.875f,0);cc.radius=.3f;cc.skinWidth=.03f;cc.stepOffset=.25f;cc.minMoveDistance=0;
                player.AddComponent<PlayerInputReader>();var camera=new GameObject("ViewCamera").AddComponent<Camera>();camera.transform.SetParent(player.transform,false);camera.transform.localPosition=new Vector3(0,1.6f,0);camera.tag="MainCamera";camera.allowHDR=true;camera.fieldOfView=70;camera.nearClipPlane=.05f;camera.farClipPlane=80;camera.backgroundColor=new Color(.035f,.055f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;
                var data=camera.GetUniversalAdditionalCameraData();data.allowHDROutput=false;data.renderPostProcessing=true;
                player.AddComponent<SalvageMotor>().Configure(camera.transform);
                var torch=new GameObject("Flashlight").AddComponent<Light>();torch.transform.SetParent(camera.transform,false);torch.transform.localPosition=new Vector3(.15f,-.1f,0);torch.type=LightType.Spot;torch.range=16;torch.spotAngle=48;torch.innerSpotAngle=28;torch.intensity=5;torch.shadows=LightShadows.Soft;player.AddComponent<PlayerFlashlight>().Configure(torch);player.SetActive(true);
                var light=new GameObject("Greybox_Key").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=1.2f;light.color=new Color(.65f,.8f,1);light.transform.rotation=Quaternion.Euler(55,-25,0);light.shadows=LightShadows.Soft;
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.25f,.3f,.4f);RenderSettings.fog=false;RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflection=AssetDatabase.LoadAssetAtPath<Cubemap>(SalvageAssetImport.Generated+"/ES_StudioReflection.asset");
                var volume=new GameObject("GreyboxVolume").AddComponent<Volume>();volume.transform.SetParent(root.transform);volume.isGlobal=true;volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(SalvageAssetImport.Generated+"/ES_ArtBench_Volume.asset");
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new InvalidOperationException("Greybox save failed.");
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        static void Segment(GameObject root,float x,float lo,float hi,Material mat)=>Block(root,"Partition",new Vector3(x,1.5f,(lo+hi)/2),new Vector3(.3f,3,hi-lo),mat);
        public static GameObject Block(GameObject root,string name,Vector3 position,Vector3 size,Material mat)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(root.transform);o.transform.position=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
        public static Material Material(string name,Color color)
        {var path=SalvageAssetImport.Generated+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(mat,path);}return mat;}
        static void Label(GameObject root,string text,Vector3 position)
        {var label=new GameObject(text).AddComponent<TextMesh>();label.transform.SetParent(root.transform);label.transform.position=position;label.transform.rotation=Quaternion.Euler(0,90,0);label.text=text;label.font=RuntimeUiFont.Get();label.fontSize=48;label.characterSize=.045f;label.GetComponent<Renderer>().sharedMaterial=label.font.material;label.color=new Color(.6f,.85f,1);}
    }
}
