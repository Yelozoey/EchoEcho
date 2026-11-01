using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;

namespace EchoStation.Player.Editor
{
    public static class PlayerInteractionSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/P04/Configure Player Interaction")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var root=PrefabUtility.LoadPrefabContents(PlayerPrefabSetup.PrefabPath);
            try{var interaction=root.GetComponent<PlayerInteraction>();if(!interaction)interaction=root.AddComponent<PlayerInteraction>();interaction.Configure(root.GetComponent<FirstPersonMotor>().View.GetComponent<Camera>());PrefabUtility.SaveAsPrefabAsset(root,PlayerPrefabSetup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
