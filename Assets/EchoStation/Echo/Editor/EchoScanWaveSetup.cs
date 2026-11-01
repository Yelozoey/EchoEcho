using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using UnityEditor;

namespace EchoStation.Echo.Editor
{
    public static class EchoScanWaveSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/E12/Bind Echo Scan Wave")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var root=PrefabUtility.LoadPrefabContents(EchoModelSetup.PrefabPath);
            try{if(!root.GetComponent<ScanWaveEmitter>())root.AddComponent<ScanWaveEmitter>();if(!root.GetComponent<EchoScanWaveBinding>())root.AddComponent<EchoScanWaveBinding>();PrefabUtility.SaveAsPrefabAsset(root,EchoModelSetup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
