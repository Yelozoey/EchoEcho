using EchoStation.Rendering.Editor;
using UnityEditor;

namespace EchoStation.Echo.Editor
{
    public static class EchoBrainSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/E10/Configure Script NPC Brain")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var root=PrefabUtility.LoadPrefabContents(EchoModelSetup.PrefabPath);
            try{if(!root.GetComponent<EchoBrain>())root.AddComponent<EchoBrain>();PrefabUtility.SaveAsPrefabAsset(root,EchoModelSetup.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
