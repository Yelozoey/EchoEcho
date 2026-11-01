using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;

namespace EchoStation.Echo.Editor
{
    public static class EchoPresentationSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/E11/Configure Echo Presentation And Scan Light")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var root=PrefabUtility.LoadPrefabContents(EchoModelSetup.PrefabPath);
            try
            {
                var rig=root.GetComponent<EchoRig>();var anchor=rig.ScanOrigin.Find("ScanRealtimeLight");
                if(!anchor)
                {
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(RealtimeLightTemplates.Folder+"/ES_ScanLight.prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(source,root.scene);go.name="ScanRealtimeLight";anchor=go.transform;anchor.SetParent(rig.ScanOrigin,false);anchor.localPosition=Vector3.zero;anchor.localRotation=Quaternion.identity;anchor.localScale=Vector3.one;
                }
                var light=anchor.GetComponent<Light>();light.enabled=false;var presentation=root.GetComponent<EchoPresentation>();if(!presentation)presentation=root.AddComponent<EchoPresentation>();presentation.ConfigureScanLight(light);PrefabUtility.SaveAsPrefabAsset(root,EchoModelSetup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
