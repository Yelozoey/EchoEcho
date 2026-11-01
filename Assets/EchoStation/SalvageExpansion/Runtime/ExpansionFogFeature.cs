using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
namespace EchoStation.SalvageExpansion
{
    public sealed class ExpansionFogFeature:ScriptableRendererFeature
    {
        [SerializeField]Shader shader;Material material;FogPass pass;
        public void Configure(Shader value){shader=value;Create();}
        public override void Create(){CoreUtils.Destroy(material);material=shader?CoreUtils.CreateEngineMaterial(shader):null;pass=new FogPass{renderPassEvent=RenderPassEvent.BeforeRenderingPostProcessing};}
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
        {var camera=data.cameraData.camera;if(!material||data.cameraData.renderType!=CameraRenderType.Base||camera.cameraType!=CameraType.Game)return;var view=camera.GetComponent<ExpansionFogView>();if(!view||!view.isActiveAndEnabled||!view.Source||!view.Source.ShouldRender(camera))return;pass.Setup(material,view.Source);renderer.EnqueuePass(pass);}
        protected override void Dispose(bool disposing){CoreUtils.Destroy(material);material=null;}
        sealed class FogPass:ScriptableRenderPass
        {
            static readonly int FogTexture=Shader.PropertyToID("_NextFogTexture");Material material;ExpansionFogField field;
            readonly ProfilingSampler integrate=new ProfilingSampler("Expansion Local Fog Integration"),composite=new ProfilingSampler("Expansion Local Fog Composite");
            sealed class Data{public Material Material;public TextureHandle Source;public ExpansionFogField Field;public Vector4 Settings,Texel;}
            public void Setup(Material value,ExpansionFogField source){material=value;field=source;requiresIntermediateTexture=true;ConfigureInput(ScriptableRenderPassInput.Depth);}
            public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();if(resources.isActiveTargetBackBuffer)return;var color=resources.activeColorTexture;var desc=graph.GetTextureDesc(color);desc.name="Expansion Local Fog Half";desc.width=Mathf.Max(1,desc.width/2);desc.height=Mathf.Max(1,desc.height/2);desc.msaaSamples=MSAASamples.None;desc.colorFormat=GraphicsFormat.R16G16B16A16_SFloat;desc.clearBuffer=false;desc.filterMode=FilterMode.Bilinear;var fog=graph.CreateTexture(desc);field.LastWidth=desc.width;field.LastHeight=desc.height;field.LastRecordedFrame=Time.frameCount;
                using(var builder=graph.AddRasterRenderPass<Data>("Expansion Local Fog Integration",out var data,integrate))
                {
                    data.Material=material;data.Source=color;data.Field=field;data.Settings=field.Pack();builder.UseTexture(color,AccessFlags.Read);builder.UseTexture(resources.cameraDepthTexture,AccessFlags.Read);builder.SetRenderAttachment(fog,0,AccessFlags.Write);builder.SetGlobalTextureAfterPass(fog,FogTexture);
                    builder.SetRenderFunc((Data d,RasterGraphContext context)=>{d.Material.SetVector("_NextFogSettings",d.Settings);d.Material.SetVectorArray("_NextFogMin",d.Field.Minima);d.Material.SetVectorArray("_NextFogMax",d.Field.Maxima);d.Material.SetVectorArray("_NextFogLightPosition",d.Field.LightPositions);d.Material.SetVectorArray("_NextFogLightColor",d.Field.LightColors);d.Material.SetVectorArray("_NextFogLightDirection",d.Field.LightDirections);Blitter.BlitTexture(context.cmd,d.Source,new Vector4(1,1,0,0),d.Material,0);});
                }
                var full=graph.GetTextureDesc(color);full.name="Expansion Local Fog Color";full.clearBuffer=false;var destination=graph.CreateTexture(full);
                using(var builder=graph.AddRasterRenderPass<Data>("Expansion Local Fog Composite",out var data,composite))
                {data.Material=material;data.Source=color;data.Texel=new Vector4(1f/desc.width,1f/desc.height,desc.width,desc.height);builder.UseTexture(color,AccessFlags.Read);builder.UseGlobalTexture(FogTexture,AccessFlags.Read);builder.UseTexture(resources.cameraDepthTexture,AccessFlags.Read);builder.SetRenderAttachment(destination,0,AccessFlags.Write);builder.SetRenderFunc((Data d,RasterGraphContext context)=>{d.Material.SetVector("_NextFogTexture_TexelSize",d.Texel);Blitter.BlitTexture(context.cmd,d.Source,new Vector4(1,1,0,0),d.Material,1);});}
                resources.cameraColor=destination;
            }
        }
    }
}
