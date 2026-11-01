using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.ParkourArt
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class VectorCityPipelineScope : MonoBehaviour
    {
        public UniversalRenderPipelineAsset pipeline;
        [SerializeField] RenderPipelineAsset fallbackDefault;
        [SerializeField] RenderPipelineAsset fallbackQuality;
        [SerializeField] int fallbackAntiAliasing;
        [NonSerialized] RenderPipelineAsset previousDefault,previousQuality;
        [NonSerialized] bool bound;
        [NonSerialized] int qualityIndex;
        [NonSerialized] int previousAntiAliasing;
        public void Initialize(UniversalRenderPipelineAsset asset,RenderPipelineAsset originalDefault,RenderPipelineAsset originalQuality)
        {
            pipeline=asset;
            if(originalDefault&&originalDefault!=asset)fallbackDefault=originalDefault;
            if(originalQuality&&originalQuality!=asset){fallbackQuality=originalQuality;fallbackAntiAliasing=QualitySettings.antiAliasing;}
            Bind();
        }
        void OnEnable()=>Bind();
        public void Bind()
        {
            if(bound||!pipeline)return;
            previousDefault=GraphicsSettings.defaultRenderPipeline==pipeline?fallbackDefault:GraphicsSettings.defaultRenderPipeline;
            previousQuality=QualitySettings.renderPipeline==pipeline?fallbackQuality:QualitySettings.renderPipeline;
            previousAntiAliasing=QualitySettings.renderPipeline==pipeline?fallbackAntiAliasing:QualitySettings.antiAliasing;
            // Never silently restore a null baseline (F061).
            if(!previousDefault||!previousQuality)return;
            qualityIndex=QualitySettings.GetQualityLevel();bound=true;
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        }
        void OnDisable()
        {
            if(!pipeline)return;
            var restoreDefault=bound&&previousDefault?previousDefault:fallbackDefault;
            var restoreQuality=bound&&QualitySettings.GetQualityLevel()==qualityIndex&&previousQuality?previousQuality:fallbackQuality;
            if(GraphicsSettings.defaultRenderPipeline==pipeline&&restoreDefault)GraphicsSettings.defaultRenderPipeline=restoreDefault;
            if(QualitySettings.renderPipeline==pipeline&&restoreQuality){QualitySettings.renderPipeline=restoreQuality;QualitySettings.antiAliasing=bound?previousAntiAliasing:fallbackAntiAliasing;}
            bound=false;
        }
    }
}
