using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
namespace EchoStation.Rendering
{
 public sealed class LocalFogFeature:ScriptableRendererFeature
 {
  public const int Steps=16;
  [SerializeField]private Shader shader;private Material material;private FogPass pass;
  public void Configure(Shader value){shader=value;Create();}
  public override void Create(){CoreUtils.Destroy(material);material=shader?CoreUtils.CreateEngineMaterial(shader):null;pass=new FogPass{renderPassEvent=RenderPassEvent.BeforeRenderingPostProcessing};}
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data){var camera=data.cameraData.camera;if(!material||data.cameraData.renderType!=CameraRenderType.Base||camera.cameraType!=CameraType.Game)return;var view=camera.GetComponent<LocalFogView>();if(!view||!view.isActiveAndEnabled||!view.Source||!view.Source.ShouldRender(camera.transform.position))return;pass.Setup(material,view.Source);renderer.EnqueuePass(pass);}
  protected override void Dispose(bool disposing){CoreUtils.Destroy(material);material=null;}
  private sealed class FogPass:ScriptableRenderPass
  {
   private static readonly int FogTexture=Shader.PropertyToID("_StationFogTexture");private Material material;private LocalFogField field;
   private readonly ProfilingSampler integrate=new ProfilingSampler("Station Local Fog Integration");private readonly ProfilingSampler composite=new ProfilingSampler("Station Local Fog Composite");
   private sealed class Data{public Material material;public TextureHandle source;public Vector4 density,texel;}
   public void Setup(Material value,LocalFogField source){material=value;field=source;requiresIntermediateTexture=true;ConfigureInput(ScriptableRenderPassInput.Depth);}
   public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
   {
    var resources=frameData.Get<UniversalResourceData>();if(resources.isActiveTargetBackBuffer)return;var color=resources.activeColorTexture;var desc=graph.GetTextureDesc(color);desc.name="Station Local Fog Half";desc.width=Mathf.Max(1,desc.width/2);desc.height=Mathf.Max(1,desc.height/2);desc.msaaSamples=MSAASamples.None;desc.colorFormat=GraphicsFormat.R16G16B16A16_SFloat;desc.clearBuffer=false;desc.filterMode=FilterMode.Bilinear;var fog=graph.CreateTexture(desc);field.LastWidth=desc.width;field.LastHeight=desc.height;field.LastRecordedFrame=Time.frameCount;
    using(var builder=graph.AddRasterRenderPass<Data>("Station Local Fog Integration",out var data,integrate)){
     data.material=material;data.source=color;data.density=new Vector4(field.LabDensity,field.VentDensity,.12f,Mathf.Clamp(field.LightStrength,0,4));builder.UseTexture(color,AccessFlags.Read);builder.UseTexture(resources.cameraDepthTexture,AccessFlags.Read);builder.SetRenderAttachment(fog,0,AccessFlags.Write);builder.SetGlobalTextureAfterPass(fog,FogTexture);
     builder.SetRenderFunc((Data d,RasterGraphContext context)=>{d.material.SetVector("_StationFogSettings",d.density);Blitter.BlitTexture(context.cmd,d.source,new Vector4(1,1,0,0),d.material,0);});
    }
    var full=graph.GetTextureDesc(color);full.name="Station Local Fog Color";full.clearBuffer=false;var destination=graph.CreateTexture(full);
    using(var builder=graph.AddRasterRenderPass<Data>("Station Local Fog Composite",out var data,composite)){
     data.material=material;data.source=color;data.texel=new Vector4(1f/desc.width,1f/desc.height,desc.width,desc.height);builder.UseTexture(color,AccessFlags.Read);builder.UseGlobalTexture(FogTexture,AccessFlags.Read);builder.UseTexture(resources.cameraDepthTexture,AccessFlags.Read);builder.SetRenderAttachment(destination,0,AccessFlags.Write);builder.SetRenderFunc((Data d,RasterGraphContext context)=>{d.material.SetVector("_StationFogTexture_TexelSize",d.texel);Blitter.BlitTexture(context.cmd,d.source,new Vector4(1,1,0,0),d.material,1);});
    }
    resources.cameraColor=destination;
   }
  }
 }
}
