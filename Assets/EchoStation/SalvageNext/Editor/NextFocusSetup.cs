using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextFocusSetup
    {
        public const string ProfilePath=FacilityAssets.Generated+"/ES_Next_FocusVolume.asset",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R01c-2";
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(ProfilePath))throw new InvalidOperationException("Preserve existing focus profile.");var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);var depth=profile.Add<DepthOfField>(true);depth.mode.value=DepthOfFieldMode.Gaussian;depth.gaussianStart.value=2;depth.gaussianEnd.value=6;depth.gaussianMaxRadius.value=.85f;depth.highQualitySampling.value=false;AssetDatabase.AddObjectToAsset(depth,profile);AssetDatabase.SetLabels(profile,new[]{"EchoStation-SalvageNext-Generated"});EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single();var player=root.GetComponentInChildren<SalvageCarry>();if(player.GetComponent<NextFocus>())throw new InvalidOperationException("Focus controller exists.");var volume=new GameObject("ES_Next_FocusVolume").AddComponent<Volume>();volume.transform.SetParent(root.transform,false);volume.isGlobal=true;volume.priority=20;volume.weight=0;volume.sharedProfile=profile;player.gameObject.AddComponent<NextFocus>().Configure(volume);root.GetComponentInChildren<Camera>().GetUniversalAdditionalCameraData().requiresDepthTexture=true;EditorSceneManager.SaveScene(scene);}
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
