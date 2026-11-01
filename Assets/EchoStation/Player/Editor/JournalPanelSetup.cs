using EchoStation.Rendering.Editor;
using UnityEditor;

namespace EchoStation.Player.Editor
{
    public static class JournalPanelSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/P05/Configure Journal Panel")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var root=PrefabUtility.LoadPrefabContents(PlayerPrefabSetup.PrefabPath);
            try{if(!root.GetComponent<JournalPanel>())root.AddComponent<JournalPanel>();PrefabUtility.SaveAsPrefabAsset(root,PlayerPrefabSetup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
