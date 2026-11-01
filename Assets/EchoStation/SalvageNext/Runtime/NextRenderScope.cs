using System;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.SalvageNext
{
    [ExecuteAlways,DefaultExecutionOrder(-100)]
    public sealed class NextRenderScope:MonoBehaviour
    {
        [SerializeField]RenderPipelineAsset[] profiles=Array.Empty<RenderPipelineAsset>();
        [SerializeField]NextSession session;
        RenderPipelineAsset previousDefault,previousQuality;int previousAA;bool bound;
        public RenderPipelineAsset Profile(RecoveryQuality quality)=>profiles[(int)quality];
        public void Configure(RenderPipelineAsset[] values,NextSession owner)
        {if(values==null||values.Length!=3||!values[0]||!values[1]||!values[2]||!owner)throw new ArgumentException("Three render profiles and a session required.");Restore();profiles=(RenderPipelineAsset[])values.Clone();session=owner;Bind(Application.isPlaying?session.Quality:RecoveryQuality.Medium);}
        void OnEnable(){if(Application.isPlaying&&session)session.QualityChanged+=Bind;if(profiles.Length==3&&session)Bind(Application.isPlaying?session.Quality:RecoveryQuality.Medium);}
        void Bind(RecoveryQuality quality)
        {if(!bound){previousDefault=GraphicsSettings.defaultRenderPipeline;previousQuality=QualitySettings.renderPipeline;previousAA=QualitySettings.antiAliasing;bound=true;}GraphicsSettings.defaultRenderPipeline=Profile(quality);QualitySettings.renderPipeline=Profile(quality);}
        public void Restore()
        {if(!bound)return;foreach(var profile in profiles){if(GraphicsSettings.defaultRenderPipeline==profile)GraphicsSettings.defaultRenderPipeline=previousDefault;if(QualitySettings.renderPipeline==profile)QualitySettings.renderPipeline=previousQuality;}QualitySettings.antiAliasing=previousAA;bound=false;}
        void OnDisable(){if(session)session.QualityChanged-=Bind;Restore();}
    }
}
