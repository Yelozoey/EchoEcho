using System;
using System.Linq;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextFogSetup
    {
        public const string ShaderPath=FacilityAssets.Root+"/Shaders/NextLocalFog.shader",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R01c-3";
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();var shader=AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Resolve new local fog shader first.");var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath);if(renderer.rendererFeatures.Any(f=>f is NextFogFeature))throw new InvalidOperationException("Keep installed new fog feature.");var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single();if(root.GetComponent<NextFogField>())throw new InvalidOperationException("New fog field already exists.");var session=root.GetComponentInChildren<NextSession>();var camera=root.GetComponentInChildren<Camera>();var legacy=root.GetComponentsInChildren<Light>(true).Where(l=>l.name=="ReadOnlyWarningLight").ToArray();if(legacy.Length!=0&&legacy.Length!=2)throw new InvalidOperationException("Unexpected legacy warning lights.");foreach(var duplicate in legacy){var data=duplicate.GetComponent<UniversalAdditionalLightData>();if(data)Object.DestroyImmediate(data);Object.DestroyImmediate(duplicate);}
                var lights=root.GetComponentsInChildren<Light>(true).Where(l=>l.type!=LightType.Directional&&l.lightmapBakeType==LightmapBakeType.Realtime).ToArray();if(lights.Length!=4)throw new InvalidOperationException("Expected four budgeted realtime local lights.");root.GetComponent<LocalShadowBudget>().Configure(camera,lights.Select(l=>l.GetUniversalAdditionalLightData()).ToArray());var field=root.AddComponent<NextFogField>();field.Configure(session,lights);camera.gameObject.AddComponent<NextFogView>().Source=field;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
                var old=renderer.rendererFeatures.Single(f=>f is LocalFogFeature);if(AssetDatabase.GetAssetPath(old)!=NextRendering.RendererPath)throw new InvalidOperationException("Preserve foreign fog feature.");renderer.rendererFeatures.Remove(old);Object.DestroyImmediate(old,true);var feature=ScriptableObject.CreateInstance<NextFogFeature>();feature.name="ES_Next_LocalFog";feature.Configure(shader);AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);renderer.SetDirty();EditorUtility.SetDirty(renderer);AssetDatabase.SaveAssetIfDirty(renderer);EditorSceneManager.SaveScene(scene);System.IO.Directory.CreateDirectory(Evidence);System.IO.File.WriteAllText(Evidence+"/Prepared_A.json",new Newtonsoft.Json.Linq.JObject{{"renderer",NextRendering.RendererPath},{"boxes",3},{"liveLocalLights",lights.Length},{"steps",16},{"particleSystems",root.GetComponentsInChildren<ParticleSystem>(true).Length},{"oldS2SourceChanged",false}}.ToString());
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
