using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Rendering.Editor
{
    public static class RenderBindingValidation
    {
        [Serializable]private class Result{public string beforeDefault,beforeQuality,afterDefault,afterQuality,qualityName;public bool passed;}
        [MenuItem("Tools/EchoStation/Checks/Restore Confirmed Render Bindings")]
        public static void Restore()
        {
            SceneSafetyGuard.RequireClean();var result=new Result{beforeDefault=Name(GraphicsSettings.defaultRenderPipeline),beforeQuality=Name(QualitySettings.renderPipeline),qualityName=QualitySettings.names[QualitySettings.GetQualityLevel()]};if(result.qualityName!="PC")throw new InvalidOperationException("Unexpected active quality level; not modifying it.");var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ForwardHdrBaselineSetup.PipelinePath);if(!pipeline)throw new InvalidOperationException("Confirmed pipeline missing.");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;AssetDatabase.SaveAssets();result.afterDefault=Name(GraphicsSettings.defaultRenderPipeline);result.afterQuality=Name(QualitySettings.renderPipeline);result.passed=GraphicsSettings.defaultRenderPipeline==pipeline&&QualitySettings.renderPipeline==pipeline;Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-P03");File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P03/RenderBindingRestore.json",JsonUtility.ToJson(result,true));
        }
        private static string Name(RenderPipelineAsset asset)=>asset?asset.name:"null";
        [Serializable]private class Safety{public bool allowed,hdr,displayHdrAllowed,ordinaryForward;public string utc,qualityName,defaultPipeline,qualityPipeline,effectivePipeline,api,unity;}
        [MenuItem("Tools/EchoStation/Checks/Export Render Safety")]
        public static void Export()
        {
            var pipeline=QualitySettings.renderPipeline as UniversalRenderPipelineAsset;UniversalRendererData renderer=null;
            if(pipeline){var serialized=new SerializedObject(pipeline);var list=serialized.FindProperty("m_RendererDataList");var index=serialized.FindProperty("m_DefaultRendererIndex").intValue;if(index>=0&&index<list.arraySize)renderer=list.GetArrayElementAtIndex(index).objectReferenceValue as UniversalRendererData;}
            var data=new Safety{utc=DateTime.UtcNow.ToString("O"),qualityName=QualitySettings.names[QualitySettings.GetQualityLevel()],defaultPipeline=Name(GraphicsSettings.defaultRenderPipeline),qualityPipeline=Name(QualitySettings.renderPipeline),effectivePipeline=Name(GraphicsSettings.currentRenderPipeline),hdr=pipeline&&pipeline.supportsHDR,ordinaryForward=renderer&&renderer.renderingMode==RenderingMode.Forward,displayHdrAllowed=PlayerSettings.allowHDRDisplaySupport,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion};
            var path=pipeline?AssetDatabase.GetAssetPath(pipeline):"";data.allowed=pipeline&&GraphicsSettings.defaultRenderPipeline==pipeline&&GraphicsSettings.currentRenderPipeline==pipeline&&data.hdr&&data.ordinaryForward&&!data.displayHdrAllowed&&data.qualityName=="PC"&&data.api=="Direct3D11"&&data.unity=="6000.6.7f1"&&(path==ForwardHdrBaselineSetup.PipelinePath||path==ScanWaveSetup.PipelinePath||path=="Assets/EchoStation/Station/Generated/ES_S2_ForwardHDR.asset");Directory.CreateDirectory("Library/EchoStation");File.WriteAllText("Library/EchoStation/RenderSafety.json",JsonUtility.ToJson(data,true));
        }
    }
}
