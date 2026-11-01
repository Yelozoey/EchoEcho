using EchoStation.Rendering.Editor;
using EchoStation.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.Echo.Editor
{
    public static class EchoObstacleSetup
    {
        public const string PrefabPath="Assets/EchoStation/Echo/Navigation/ES_DynamicObstacle.prefab";
        [MenuItem("Tools/EchoStation/Stage 1/E03/Create Dynamic Obstacle Template")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))return;
            CabinGreyboxSetup.EnsureFolder("Assets/EchoStation/Echo/Navigation");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var root=new GameObject("DynamicCabinetObstacle");var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.65f,0);box.size=new Vector3(1.1f,1.3f,.8f);
                var obstacle=root.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=box.center;obstacle.size=box.size;obstacle.carving=true;obstacle.carveOnlyStationary=true;obstacle.carvingMoveThreshold=.05f;obstacle.carvingTimeToStationary=.2f;
                var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.name="CabinetVisual";visual.transform.SetParent(root.transform,false);visual.transform.localPosition=box.center;visual.transform.localScale=box.size;Object.DestroyImmediate(visual.GetComponent<Collider>());
                var path=EchoModelSetup.Folder+"/ES_DynamicCabinet.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.24f,.29f,.34f));material.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(material,path);}
                var renderer=visual.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.receiveGI=ReceiveGI.LightProbes;GameObjectUtility.SetStaticEditorFlags(visual,0);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
