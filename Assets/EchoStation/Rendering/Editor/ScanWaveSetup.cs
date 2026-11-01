using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Rendering.Editor
{
    public static class ScanWaveSetup
    {
        public const string PipelinePath="Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardHDR.asset";
        public const string RendererPath="Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardRenderer.asset";
        [MenuItem("Tools/EchoStation/Stage 1/E12/Create Gameplay Scan Renderer")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();if(!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PipelinePath)&&!AssetDatabase.CopyAsset(ForwardHdrBaselineSetup.PipelinePath,PipelinePath))throw new InvalidOperationException("Pipeline copy failed.");
            if(!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RendererPath)&&!AssetDatabase.CopyAsset(ForwardHdrBaselineSetup.RendererPath,RendererPath))throw new InvalidOperationException("Renderer copy failed.");
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);ScanWaveFeature feature=null;
            foreach(var f in renderer.rendererFeatures)if(f is ScanWaveFeature found)feature=found;
            if(!feature){feature=ScriptableObject.CreateInstance<ScanWaveFeature>();feature.name="ES_ScanWave";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
            var shader=Shader.Find("Hidden/EchoStation/ScanWave");if(!shader||!shader.isSupported)throw new InvalidOperationException("Scan shader unavailable.");feature.Configure(shader);EditorUtility.SetDirty(feature);renderer.SetDirty();EditorUtility.SetDirty(renderer);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();
        }
    }
}
