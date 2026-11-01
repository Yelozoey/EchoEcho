using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;

namespace EchoStation.Player.Editor
{
    public static class PlayerFlashlightSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/P03/Configure Player Flashlight")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var root=PrefabUtility.LoadPrefabContents(PlayerPrefabSetup.PrefabPath);
            try
            {
                var view=root.GetComponent<FirstPersonMotor>().View;var anchor=view.Find("PlayerFlashlight");if(!anchor){var template=AssetDatabase.LoadAssetAtPath<GameObject>(RealtimeLightTemplates.Folder+"/ES_Flashlight.prefab");anchor=((GameObject)PrefabUtility.InstantiatePrefab(template,root.scene)).transform;anchor.name="PlayerFlashlight";anchor.SetParent(view,false);}anchor.localPosition=new Vector3(.15f,-.1f,.15f);anchor.localRotation=Quaternion.identity;anchor.localScale=Vector3.one;var controller=root.GetComponent<PlayerFlashlight>();if(!controller)controller=root.AddComponent<PlayerFlashlight>();controller.Configure(anchor.GetComponent<Light>());PrefabUtility.SaveAsPrefabAsset(root,PlayerPrefabSetup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
