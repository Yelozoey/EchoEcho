using System;
using System.Linq;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextRendering
    {
        public const string RendererPath=FacilityAssets.Generated+"/ES_Next_ForwardRenderer.asset";
        public static string PipelinePath(int quality)=>FacilityAssets.Generated+"/ES_Next_"+((RecoveryQuality)quality)+"_ForwardHDR.asset";
        public static void TuneFlashlight()
        {
            SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>(true)).Single(l=>l.name=="Flashlight").intensity=20;EditorSceneManager.SaveScene(scene);}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(RendererPath)||Enumerable.Range(0,3).Any(i=>AssetDatabase.LoadMainAssetAtPath(PipelinePath(i))))throw new InvalidOperationException("Keep existing own rendering assets.");
            if(!AssetDatabase.CopyAsset("Assets/EchoStation/Station/Generated/ES_S2_ForwardRenderer.asset",RendererPath))throw new InvalidOperationException("Renderer clone failed.");var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);var rso=new SerializedObject(renderer);rso.FindProperty("m_RenderingMode").intValue=0;rso.ApplyModifiedPropertiesWithoutUndo();Save(renderer);
            for(int i=0;i<3;i++)
            {
                if(!AssetDatabase.CopyAsset("Assets/EchoStation/Station/Generated/ES_S2_ForwardHDR.asset",PipelinePath(i)))throw new InvalidOperationException("Pipeline clone failed.");var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath(i));
                pipeline.supportsHDR=true;pipeline.msaaSampleCount=new[]{1,2,4}[i];pipeline.renderScale=i==0?.8f:1;pipeline.mainLightShadowmapResolution=new[]{1024,2048,4096}[i];pipeline.additionalLightsShadowmapResolution=i==0?1024:2048;pipeline.shadowDistance=new[]{20,32,48}[i];pipeline.shadowCascadeCount=new[]{1,2,4}[i];pipeline.cascade2Split=.3f;pipeline.cascade4Split=new Vector3(.1f,.25f,.5f);pipeline.cascadeBorder=.1f;pipeline.useSRPBatcher=true;pipeline.maxAdditionalLightsCount=4;
                var so=new SerializedObject(pipeline);so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;so.FindProperty("m_MainLightShadowsSupported").boolValue=true;so.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;so.FindProperty("m_AdditionalLightsRenderingMode").intValue=1;so.ApplyModifiedPropertiesWithoutUndo();Save(pipeline);
            }
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().Single();var old=world.GetComponent<SalvagePipelineScope>();if(old)Object.DestroyImmediate(old);var session=world.GetComponentInChildren<NextSession>();var view=world.GetComponentInChildren<Camera>();view.allowHDR=true;view.GetUniversalAdditionalCameraData().allowHDROutput=false;
                var scope=world.AddComponent<NextRenderScope>();scope.Configure(Enumerable.Range(0,3).Select(i=>(RenderPipelineAsset)AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath(i))).ToArray(),session);
                foreach(var room in world.GetComponentsInChildren<RoomVisual>())room.gameObject.AddComponent<DistanceShadowCaster>().Configure(view,room.GetComponentsInChildren<MeshRenderer>(),30,2,.1f);
                foreach(var actor in world.GetComponentsInChildren<CargoActor>())actor.gameObject.AddComponent<DistanceShadowCaster>().Configure(view,actor.GetComponentInChildren<CargoVisualModel>().GetComponentsInChildren<MeshRenderer>(),30,2,.1f);
                var rover=world.GetComponentInChildren<RoverModelParts>();rover.gameObject.AddComponent<DistanceShadowCaster>().Configure(view,rover.GetComponentsInChildren<MeshRenderer>(),30,2,.1f);
                var lights=world.GetComponentsInChildren<Light>(true).Where(l=>l.type!=LightType.Directional).Select(l=>l.GetUniversalAdditionalLightData()).ToArray();world.AddComponent<LocalShadowBudget>().Configure(view,lights);foreach(var light in world.GetComponentsInChildren<Light>(true)){light.shadows=LightShadows.Soft;if(light.type!=LightType.Directional)light.lightmapBakeType=LightmapBakeType.Realtime;}
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Rendering scene save failed.");
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        static void Save(Object asset){AssetDatabase.SetLabels(asset,new[]{"EchoStation-SalvageNext-Generated"});EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
    }
}
