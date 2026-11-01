using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class RealtimeShadowFixtureSetup
    {
        public const string ScenePath = "Assets/EchoStation/Tests/Scenes/S1_R04_RealtimeShadows.unity";
        public const string ReceiverPath = "Assets/EchoStation/Tests/Fixtures/R04_ShadowReceiver.mat";
        [MenuItem("Tools/EchoStation/Stage 1/R04/Create Shadow Fixture")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) return;
            var shader = Shader.Find("EchoStation/Tests/ShadowReceiver");
            if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Shadow receiver shader unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(ReceiverPath);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, ReceiverPath); }
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black; RenderSettings.ambientIntensity = 0; RenderSettings.reflectionIntensity = 0;
                RenderSettings.fog = false;
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "R04_Receiver";
                ground.transform.position = new Vector3(0, 0, 25); ground.transform.localScale = new Vector3(12, 1, 12);
                ground.GetComponent<Renderer>().sharedMaterial = material; ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var caster = GameObject.CreatePrimitive(PrimitiveType.Cube); caster.name = "R04_Caster";
                caster.transform.position = new Vector3(0, 1, 7); caster.transform.localScale = new Vector3(1.5f, 2, 1.5f);
                caster.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(BakedLightingSetup.MaterialPath);
                var main = new GameObject("R04_Main", typeof(Light)).GetComponent<Light>();
                main.type = LightType.Directional; main.lightmapBakeType = LightmapBakeType.Realtime;
                main.transform.rotation = Quaternion.Euler(50, -35, 0); main.color = new Color(.65f, .8f, 1); main.intensity = 1;
                main.shadows = LightShadows.Soft; main.shadowStrength = 1;
                var spot = new GameObject("R04_Spot", typeof(Light)).GetComponent<Light>();
                spot.type = LightType.Spot; spot.lightmapBakeType = LightmapBakeType.Realtime;
                spot.transform.position = new Vector3(-3, 5, 3); spot.transform.LookAt(caster.transform.position);
                spot.color = new Color(1, .45f, .15f); spot.intensity = 6; spot.range = 20; spot.spotAngle = 65;
                spot.shadows = LightShadows.Soft; spot.shadowStrength = 1;
                // URP guards this property's public setter outside Play Mode.
                var lightData = new SerializedObject(spot.GetUniversalAdditionalLightData());
                lightData.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = 2; lightData.ApplyModifiedPropertiesWithoutUndo();
                var camera = new GameObject("R04_ValidationCamera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 3, -3);
                camera.transform.rotation = Quaternion.Euler(12, 0, 0); camera.fieldOfView = 60; camera.nearClipPlane = .1f; camera.farClipPlane = 90;
                camera.aspect = 16f/9f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.allowHDR = true; camera.allowMSAA = false;
                var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = false; data.allowHDROutput = false; data.SetRenderer(0);
                var budget = caster.AddComponent<DistanceShadowCaster>(); budget.Configure(camera, new[] { caster.GetComponent<Renderer>() });
                EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
