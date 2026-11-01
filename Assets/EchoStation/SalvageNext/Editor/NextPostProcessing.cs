using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextPostProcessing
    {
        public const string ProfilePath=FacilityAssets.Generated+"/ES_Next_ExplorationVolume.asset",NeutralPath=FacilityAssets.Raw+"/Textures/T_Trim_02_BaseColor.png",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R01c-1";
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(ProfilePath))throw new InvalidOperationException("Preserve existing post-processing profile.");var importer=(TextureImporter)AssetImporter.GetAtPath(NeutralPath);if(!importer)throw new InvalidOperationException("Verified neutral source missing.");importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();var neutral=AssetDatabase.LoadAssetAtPath<Texture2D>(NeutralPath);AssetDatabase.SetLabels(neutral,new[]{"EchoStation-SalvageNext-Generated"});
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;var grain=profile.Add<FilmGrain>(true);grain.type.value=FilmGrainLookup.Thin1;grain.intensity.value=.06f;grain.response.value=.8f;profile.Add<ChromaticAberration>(true).intensity.value=.012f;var color=profile.Add<ColorAdjustments>(true);color.postExposure.value=1f;color.contrast.value=2;color.saturation.value=-30;foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);AssetDatabase.SetLabels(profile,new[]{"EchoStation-SalvageNext-Generated"});EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single();var materials=root.GetComponentsInChildren<RoomVisual>().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).SelectMany(r=>r.sharedMaterials).Distinct();int changed=0;
                foreach(var material in materials)if(AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap"))==FacilityAssets.Raw+"/Textures/T_Trim_02_BaseColor_Blue.png")
                {if(!AssetDatabase.GetAssetPath(material).StartsWith(FacilityAssets.Generated+"/ES_NextLit_",StringComparison.Ordinal))throw new InvalidOperationException("Keep non-static material.");material.SetTexture("_BaseMap",neutral);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);changed++;}
                if(changed==0)throw new InvalidOperationException("No authored blue static surfaces found.");var volume=root.GetComponentsInChildren<Volume>().Single();volume.name="ES_Next_GlobalVolume";volume.sharedProfile=profile;volume.isGlobal=true;volume.priority=10;volume.weight=1;var camera=root.GetComponentInChildren<Camera>();camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;EditorSceneManager.SaveScene(scene);System.IO.File.WriteAllText(Evidence+"/Prepared_A.json",new Newtonsoft.Json.Linq.JObject{{"neutralStaticMaterialBindings",changed},{"profile",ProfilePath},{"copiedOldTemplate",false},{"DOF",false},{"fog",false}}.ToString());
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
