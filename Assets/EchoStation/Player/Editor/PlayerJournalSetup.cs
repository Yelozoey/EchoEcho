using EchoStation.Rendering.Editor;
using UnityEditor;

namespace EchoStation.Player.Editor
{
    public static class PlayerJournalSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/P05/Configure Player Journal")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var root=PrefabUtility.LoadPrefabContents(PlayerPrefabSetup.PrefabPath);
            try{if(!root.GetComponent<PlayerJournal>())root.AddComponent<PlayerJournal>();PrefabUtility.SaveAsPrefabAsset(root,PlayerPrefabSetup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
