using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Rendering
{
    public sealed class ScanWaveFeature:ScriptableRendererFeature
    {
        public const string PassName="Echo Scan Wave";
        [SerializeField]private Shader shader;
        private Material material;private WavePass pass;
        public void Configure(Shader value){shader=value;Create();}
        public override void Create()
        {
            CoreUtils.Destroy(material);material=shader?CoreUtils.CreateEngineMaterial(shader):null;pass=new WavePass{renderPassEvent=RenderPassEvent.BeforeRenderingPostProcessing};
        }
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData renderingData)
        {
            var camera=renderingData.cameraData.camera;if(!material||renderingData.cameraData.renderType!=CameraRenderType.Base||camera.cameraType!=CameraType.Game)return;
            var view=camera.GetComponent<ScanWaveView>();if(!view||!view.enabled||!view.Source||!view.Source.isActiveAndEnabled||!view.Source.State.Active)return;
            pass.Setup(material,view.Source.State);renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing){CoreUtils.Destroy(material);material=null;}
        private sealed class WavePass:ScriptableRenderPass
        {
            private Material material;private Vector4 originRadius;private float width,intensity;
            private sealed class Data{public Material material;public TextureHandle source;public Vector4 originRadius;public float width,intensity;}
            private readonly ProfilingSampler sampler=new ProfilingSampler(PassName);
            public void Setup(Material value,ScanWaveState wave)
            {
                material=value;originRadius=new Vector4(wave.Origin.x,wave.Origin.y,wave.Origin.z,wave.Radius);width=ScanWaveState.Width;intensity=ScanWaveState.Intensity;requiresIntermediateTexture=true;ConfigureInput(ScriptableRenderPassInput.Depth);
            }
            public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();if(resources.isActiveTargetBackBuffer)return;
                var source=resources.activeColorTexture;var desc=graph.GetTextureDesc(source);desc.name=PassName+" Color";desc.clearBuffer=false;var destination=graph.CreateTexture(desc);
                using(var builder=graph.AddRasterRenderPass<Data>(PassName,out var data,sampler))
                {
                    data.material=material;data.source=source;data.originRadius=originRadius;data.width=width;data.intensity=intensity;
                    builder.UseTexture(source,AccessFlags.Read);builder.UseTexture(resources.cameraDepthTexture,AccessFlags.Read);builder.SetRenderAttachment(destination,0,AccessFlags.Write);
                    builder.SetRenderFunc((Data d,RasterGraphContext context)=>
                    {
                        d.material.SetVector("_WaveOriginRadius",d.originRadius);d.material.SetVector("_WaveSettings",new Vector4(d.width,d.intensity,ScanWaveState.MaximumRadius,0));
                        Blitter.BlitTexture(context.cmd,d.source,new Vector4(1,1,0,0),d.material,0);
                    });
                }
                resources.cameraColor=destination;
            }
        }
    }
}
