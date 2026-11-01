using System;
using EchoStation.Rendering.Editor;
using EchoStation.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.Echo.Editor
{
    public static class EchoModelSetup
    {
        public const string Folder="Assets/EchoStation/Echo/Art";
        public const string PrefabPath="Assets/EchoStation/Echo/Prefabs/ES_Echo.prefab";
        [MenuItem("Tools/EchoStation/Stage 1/E01/Repair Indicator Emission Flags")]
        public static void RepairIndicators()
        {
            SceneSafetyGuard.RequireClean();foreach(var name in new[]{"ES_Echo_StatusBlue","ES_Echo_StatusYellow","ES_Echo_StatusRed"})
            {var material=AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{name}.mat");if(!material)throw new InvalidOperationException("Create Echo first.");material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);}
        }
        [MenuItem("Tools/EchoStation/Stage 1/E01/Create Wheeled Echo Rig")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))return;
            CabinGreyboxSetup.EnsureFolder(Folder);CabinGreyboxSetup.EnsureFolder("Assets/EchoStation/Echo/Prefabs");
            var shell=Material("ES_Echo_Shell",new Color(.36f,.41f,.46f));var dark=Material("ES_Echo_Dark",new Color(.055f,.065f,.08f));
            var blue=Material("ES_Echo_StatusBlue",new Color(.08f,.4f,1),true);var yellow=Material("ES_Echo_StatusYellow",new Color(1,.7f,.03f),true);var red=Material("ES_Echo_StatusRed",new Color(1,.04f,.02f),true);
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var root=new GameObject("Echo");root.SetActive(false);
            try
            {
                var body=Part("Body",PrimitiveType.Cube,root.transform,new Vector3(0,.35f,0),new Vector3(.52f,.22f,.6f),shell).transform;
                Part("LowerChassis",PrimitiveType.Cube,root.transform,new Vector3(0,.22f,0),new Vector3(.48f,.12f,.52f),dark);
                Part("ProbeMast",PrimitiveType.Cylinder,root.transform,new Vector3(0,.59f,.1f),new Vector3(.07f,.13f,.07f),shell);
                var probe=new GameObject("ProbePitch").transform;probe.SetParent(root.transform,false);probe.localPosition=new Vector3(0,.74f,.1f);
                Part("ProbeHousing",PrimitiveType.Sphere,probe,Vector3.zero,new Vector3(.24f,.18f,.25f),shell);
                var lens=Part("ScannerAperture",PrimitiveType.Cylinder,probe,new Vector3(0,0,.13f),new Vector3(.12f,.01f,.12f),dark);lens.transform.localRotation=Quaternion.Euler(90,0,0);
                var scan=new GameObject("ScanOrigin").transform;scan.SetParent(probe,false);scan.localPosition=new Vector3(0,0,.155f);
                var indicator=Part("Status",PrimitiveType.Cube,root.transform,new Vector3(0,.477f,-.25f),new Vector3(.24f,.035f,.04f),blue).GetComponent<MeshRenderer>();
                var wheels=new Transform[4];var index=0;
                foreach(var z in new[]{.22f,-.22f})foreach(var x in new[]{-.315f,.315f})
                {
                    var pivot=new GameObject("Wheel_"+(z>0?"F":"R")+(x<0?"L":"R")).transform;pivot.SetParent(root.transform,false);pivot.localPosition=new Vector3(x,.14f,z);wheels[index++]=pivot;
                    var tire=Part("Tire",PrimitiveType.Cylinder,pivot,Vector3.zero,new Vector3(.28f,.045f,.28f),dark);tire.transform.localRotation=Quaternion.Euler(0,0,90);
                    Part("HubStripe",PrimitiveType.Cube,pivot,new Vector3(Mathf.Sign(x)*.047f,0,0),new Vector3(.012f,.16f,.035f),shell);
                }
                var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.43f,0);collider.size=new Vector3(.75f,.86f,.75f);
                root.AddComponent<EchoRig>().Configure(body,probe,scan,wheels,indicator,blue,yellow,red);root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
            }
            finally{Object.DestroyImmediate(root);if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        private static Material Material(string name,Color color,bool emission=false)
        {
            var path=$"{Folder}/{name}.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.25f);
            if(emission){material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*1.4f);}
            AssetDatabase.CreateAsset(material,path);return material;
        }
        private static GameObject Part(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.receiveGI=ReceiveGI.LightProbes;GameObjectUtility.SetStaticEditorFlags(go,0);return go;
        }
    }
}
