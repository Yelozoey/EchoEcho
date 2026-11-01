using EchoStation.Rendering.Editor;
using UnityEditor;

namespace EchoStation.Player.Editor
{
    public static class InteractionPromptSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/P04/Configure Interaction Overlay")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var root=PrefabUtility.LoadPrefabContents(PlayerPrefabSetup.PrefabPath);
            try{if(!root.GetComponent<InteractionPrompt>())root.AddComponent<InteractionPrompt>();PrefabUtility.SaveAsPrefabAsset(root,PlayerPrefabSetup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
