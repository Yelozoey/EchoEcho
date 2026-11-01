using System;
using System.Linq;
using EchoStation.Player;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace EchoStation.Station.Editor
{
 public static class StationFogSetup
 {
  public const string PipelinePath=StationLayout.DataFolder+"/ES_S2_ForwardHDR.asset";
  public const string RendererPath=StationLayout.DataFolder+"/ES_S2_ForwardRenderer.asset";
  [MenuItem("Tools/EchoStation/Stage 2/P03/Configure Local Volumes")]
  public static void Configure()
  {
   SceneSafetyGuard.RequireClean();var shader=Shader.Find("Hidden/EchoStation/LocalFog");if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Fix local volume shader before binding S2 pipeline.");
   if(!AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath)&&!AssetDatabase.CopyAsset(ScanWaveSetup.PipelinePath,PipelinePath))throw new InvalidOperationException("Cannot copy S2 pipeline.");
   if(!AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath)&&!AssetDatabase.CopyAsset(ScanWaveSetup.RendererPath,RendererPath))throw new InvalidOperationException("Cannot copy S2 renderer.");
   var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);var feature=renderer.rendererFeatures.OfType<LocalFogFeature>().SingleOrDefault();if(!feature){feature=ScriptableObject.CreateInstance<LocalFogFeature>();feature.name="ES_Station_LocalFog";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}feature.Configure(shader);feature.SetActive(true);EditorUtility.SetDirty(feature);EditorUtility.SetDirty(renderer);
   var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();
   StationWorldSetup.Edit((scene,root)=>{var field=root.GetComponent<LocalFogField>();if(!field)field=root.AddComponent<LocalFogField>();var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<FirstPersonMotor>()).Single().View.GetComponent<Camera>();var view=camera.GetComponent<LocalFogView>();if(!view)view=camera.gameObject.AddComponent<LocalFogView>();view.Source=field;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;});
   GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;AssetDatabase.SaveAssets();
  }
 }
}
