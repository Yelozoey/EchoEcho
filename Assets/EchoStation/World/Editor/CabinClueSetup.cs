using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EchoStation.World.Editor
{
    public static class CabinClueSetup
    {
        public const string Folder="Assets/EchoStation/Data/Clues";
        [MenuItem("Tools/EchoStation/Stage 1/W04/Configure Cabin Clue Data And Presentation")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();CabinGreyboxSetup.EnsureFolder(Folder);var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var targets=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<WorldTarget>()).ToDictionary(t=>t.Id);
                Add(targets["note_maintenance_01"],ClueKind.Note,"维护记录","通风测试结束后，A区传感器出现相同偏移。供电面板随后切换到隔离状态。维修员要求将这些数据与值班记录交叉核对。","通风测试后出现传感器偏移，供电面板随后隔离。",new Vector3(.32f,.012f,.24f));
                Add(targets["note_roster_01"],ClueKind.Note,"值班记录","02:10，值班员断开通风回路。交接记录指出，异常读数在断电之前已经出现。储物室墙面留有设备校准标记，需由扫描设备辨认。","异常早于断电；储物室留有需扫描辨认的校准标记。",new Vector3(.25f,.32f,.012f));
                Add(targets["terminal_data_01"],ClueKind.Data,"观测数据片段","采样序列07：温度稳定，空气微粒浓度持续缓慢上升。当前记录不足以支持单一供电故障的解释。需要继续核对其他观察资料。","温度稳定而微粒浓度上升，单一供电故障解释不足。",new Vector3(.35f,.2f,.015f));
                Add(targets["scan_mark_01"],ClueKind.Scan,"隐藏校准标记","扫描结果：此处为地下A区支路的校准定位点。标记用于确认设备记录的位置，不提供终端密码，也不会直接改变供电状态。","发现地下A区支路校准定位点，仅提供位置线索。",new Vector3(.012f,.3f,.3f));
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        private static void Add(WorldTarget target,ClueKind kind,string title,string full,string brief,Vector3 size)
        {
            if(target.GetComponent<ClueSource>())return;var active=target.gameObject.activeSelf;target.gameObject.SetActive(false);
            var path=$"{Folder}/ES_{target.Id}.asset";var data=AssetDatabase.LoadAssetAtPath<ClueDefinition>(path);
            if(!data){data=ScriptableObject.CreateInstance<ClueDefinition>();data.Configure(target.Id,title,full,brief,kind);AssetDatabase.CreateAsset(data,path);}
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(primitive);
            target.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=target.gameObject.AddComponent<MeshRenderer>();var matPath=$"{Folder}/ES_Clue_{kind}.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",kind==ClueKind.Note?new Color(.7f,.72f,.68f):new Color(.1f,.25f,.4f));material.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(material,matPath);}
            renderer.sharedMaterial=material;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.receiveGI=ReceiveGI.LightProbes;renderer.shadowCastingMode=ShadowCastingMode.Off;
            target.transform.localScale=size;var collider=target.gameObject.AddComponent<BoxCollider>();collider.isTrigger=true;GameObjectUtility.SetStaticEditorFlags(target.gameObject,0);
            target.gameObject.AddComponent<ClueSource>().Configure(data,renderer);target.gameObject.SetActive(active);
        }
    }
}
