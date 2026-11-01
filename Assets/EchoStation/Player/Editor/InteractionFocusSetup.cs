using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Player.Editor
{
    public static class InteractionFocusSetup
    {
        public const string ProfilePath="Assets/EchoStation/Rendering/Settings/ES_Volume_InteractionFocus.asset";
        public const string PrefabPath="Assets/EchoStation/Rendering/Prefabs/ES_InteractionFocusVolume.prefab";
        [MenuItem("Tools/EchoStation/Stage 1/P06/Configure Interaction Focus")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);var depth=profile.Add<DepthOfField>(true);depth.mode.Override(DepthOfFieldMode.Bokeh);depth.focusDistance.Override(1);depth.aperture.Override(5.6f);depth.focalLength.Override(50);depth.bladeCount.Override(5);depth.bladeCurvature.Override(1);depth.bladeRotation.Override(0);AssetDatabase.AddObjectToAsset(depth,profile);EditorUtility.SetDirty(profile);}
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)){var go=new GameObject("ES_InteractionFocusVolume");go.layer=8;var volume=go.AddComponent<Volume>();volume.isGlobal=true;volume.priority=20;volume.weight=0;volume.sharedProfile=profile;PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);Object.DestroyImmediate(go);}
            var root=PrefabUtility.LoadPrefabContents(PlayerPrefabSetup.PrefabPath);
            try{var focus=root.GetComponent<InteractionFocus>();if(!focus)focus=root.AddComponent<InteractionFocus>();var volume=root.GetComponentInChildren<Volume>();if(!volume){var child=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),root.transform);volume=child.GetComponent<Volume>();}var serialized=new SerializedObject(focus);serialized.FindProperty("focusVolume").objectReferenceValue=volume;serialized.ApplyModifiedPropertiesWithoutUndo();root.GetComponentInChildren<Camera>().GetUniversalAdditionalCameraData().requiresDepthTexture=true;PrefabUtility.SaveAsPrefabAsset(root,PlayerPrefabSetup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
        }
    }
}
