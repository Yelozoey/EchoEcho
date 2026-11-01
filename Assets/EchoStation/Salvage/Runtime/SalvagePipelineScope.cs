using UnityEngine;
using UnityEngine.Rendering;

namespace EchoStation.Salvage
{
    [ExecuteAlways]
    public sealed class SalvagePipelineScope:MonoBehaviour
    {
        [SerializeField]RenderPipelineAsset pipeline;
        RenderPipelineAsset previousDefault,previousQuality;int previousAA;bool bound;
        public void Configure(RenderPipelineAsset value){pipeline=value;Bind();}
        void OnEnable()=>Bind();
        void Bind(){if(bound||!pipeline)return;previousDefault=GraphicsSettings.defaultRenderPipeline;previousQuality=QualitySettings.renderPipeline;previousAA=QualitySettings.antiAliasing;GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;bound=true;}
        void OnDisable(){if(!bound)return;if(GraphicsSettings.defaultRenderPipeline==pipeline)GraphicsSettings.defaultRenderPipeline=previousDefault;if(QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=previousQuality;QualitySettings.antiAliasing=previousAA;bound=false;}
    }
}
