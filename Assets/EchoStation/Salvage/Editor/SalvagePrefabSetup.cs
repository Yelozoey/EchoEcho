using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace EchoStation.Salvage.Editor
{
    public static class SalvagePrefabSetup
    {
        public const string Folder=SalvageAssetImport.Root+"/Prefabs";
        public static string Path(int i)=>Folder+(i==0?"/ES_Salvage_Platform.prefab":"/ES_Salvage_Toolbox.prefab");
        [MenuItem("Tools/EchoStation/Salvage/R0-A02/Create Prefabs")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();SalvageAssetImport.EnsureFolder(Folder);
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                for(int i=0;i<2;i++)
                {
                    var root=new GameObject(i==0?"ES_Salvage_Platform":"ES_Salvage_Toolbox");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
                    try
                    {
                        var visual=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SalvageAssetImport.Model(i)),root.transform);
                        visual.name="Visual";
                        foreach(var r in visual.GetComponentsInChildren<Renderer>())
                        {
                            var mats=r.sharedMaterials;for(int n=0;n<mats.Length;n++)mats[n]=AssetDatabase.LoadAssetAtPath<Material>(SalvageAssetImport.MaterialPath(i));r.sharedMaterials=mats;
                            r.lightProbeUsage=i==0?LightProbeUsage.Off:LightProbeUsage.BlendProbes;
                        }
                        var b=SalvageAssetImport.BoundsOf(root);var box=root.AddComponent<BoxCollider>();
                        box.center=i==0?new Vector3(b.center.x,b.max.y-.05f,b.center.z):b.center;
                        box.size=i==0?new Vector3(b.size.x,.1f,b.size.z):b.size;
                        if(i==0)foreach(var t in root.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
                        PrefabUtility.SaveAsPrefabAsset(root,Path(i));
                    }
                    finally{Object.DestroyImmediate(root);}
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
        }
    }
}
