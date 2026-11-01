using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Rendering.Editor
{
    public static class RealtimeLightTemplates
    {
        public const string Folder="Assets/EchoStation/Rendering/Prefabs";
        [MenuItem("Tools/EchoStation/Stage 1/R04/Create Realtime Light Templates")]
        public static void Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var previous=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                Save("ES_Flashlight",LightType.Spot,Color.white,6,20,55,2,false);
                Save("ES_ScanLight",LightType.Spot,new Color(.2f,.55f,1),3,8,70,1,false);
                Save("ES_FaultLight",LightType.Point,new Color(1,.23f,.07f),2,5,0,1,true);
            }
            finally { if(previous.IsValid()) SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
        }
        private static void Save(string name,LightType type,Color color,float intensity,float range,float angle,int tier,bool flicker)
        {
            var path=$"{Folder}/{name}.prefab"; if(AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;
            var go=new GameObject(name,typeof(Light)); var light=go.GetComponent<Light>();
            light.type=type; light.color=color; light.intensity=intensity; light.range=range;
            if(type==LightType.Spot) light.spotAngle=angle;
            light.lightmapBakeType=LightmapBakeType.Realtime; light.shadows=LightShadows.Soft;
            var data=new SerializedObject(light.GetUniversalAdditionalLightData());
            data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=tier; data.ApplyModifiedPropertiesWithoutUndo();
            if(flicker) go.AddComponent<FaultLightFlicker>();
            PrefabUtility.SaveAsPrefabAsset(go,path); UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
