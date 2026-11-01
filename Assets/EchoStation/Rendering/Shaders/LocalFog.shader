Shader "Hidden/EchoStation/LocalFog"
{
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" }
  ZWrite Off ZTest Always Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
  TEXTURE2D_X(_StationFogTexture);
  float4 _StationFogTexture_TexelSize;
  float4 _StationFogSettings;
  bool Interval(float3 origin,float3 direction,float3 lo,float3 hi,float maximum,out float first,out float last)
  {
   float3 inv=sign(direction+1e-7)/max(abs(direction),1e-5);
   float3 a=(lo-origin)*inv,b=(hi-origin)*inv;
   float3 nearHit=min(a,b),farHit=max(a,b);
   first=max(0,max(nearHit.x,max(nearHit.y,nearHit.z)));last=min(maximum,min(farHit.x,min(farHit.y,farHit.z)));
   return last>first;
  }
  bool Inside(float3 p,float3 lo,float3 hi){return all(p>=lo)&&all(p<=hi);}
  half4 Integrate(Varyings input):SV_Target
  {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
   float2 uv=input.texcoord;float depth=SampleSceneDepth(uv);
   #if !UNITY_REVERSED_Z
   depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
   #endif
   float3 end=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);float3 origin=_WorldSpaceCameraPos;
   float3 direction=normalize(end-origin);float maximum=min(distance(origin,end),80);
   float f0,l0,f1,l1;bool b0=Interval(origin,direction,float3(-6,-4,22),float3(6,0,38),maximum,f0,l0);bool b1=Interval(origin,direction,float3(-2.5,-4,38),float3(2.5,0,58),maximum,f1,l1);
   if(!b0&&!b1)return half4(0,0,0,1);
   float first=min(b0?f0:maximum,b1?f1:maximum);float last=max(b0?l0:0,b1?l1:0);float stepSize=(last-first)/16;
   float transmission=1;float3 scatter=0;
   [unroll]for(int i=0;i<16;i++){
    float3 p=origin+direction*(first+(i+.5)*stepSize);float density=0;
    if(Inside(p,float3(-6,-4,22),float3(6,0,38)))density+=_StationFogSettings.x;
    if(Inside(p,float3(-2.5,-4,38),float3(2.5,0,58)))density+=_StationFogSettings.y;
    density*=1+_StationFogSettings.z*sin(p.x*1.7+p.z*.9)*sin(p.y*2.3);
    float3 d0=p-float3(0,-.8,34),d1=p-float3(0,-.8,50);
    float illumination=5/(1+dot(d0,d0))+5/(1+dot(d1,d1));
    float3 light=float3(.18,.28,.38)+_StationFogSettings.w*illumination*float3(.35,.55,1);
    float attenuation=exp(-density*stepSize);scatter+=transmission*(1-attenuation)*light;transmission*=attenuation;
   }
   return half4(scatter,transmission);
  }
  half4 Composite(Varyings input):SV_Target
  {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);float2 uv=input.texcoord;
   half4 color=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,uv,0);
   float center=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);float4 sum=0;float weights=0;
   [unroll]for(int i=0;i<4;i++){
    float2 offset=float2((i&1)? .5:-.5,(i&2)? .5:-.5)*_StationFogTexture_TexelSize.xy;
    float2 sampleUv=uv+offset;float z=LinearEyeDepth(SampleSceneDepth(sampleUv),_ZBufferParams);float weight=1/(1+abs(z-center)*32);
    sum+=SAMPLE_TEXTURE2D_X_LOD(_StationFogTexture,sampler_LinearClamp,sampleUv,0)*weight;weights+=weight;
   }
   float4 fog=sum/max(weights,1e-5);return half4(color.rgb*fog.a+fog.rgb,color.a);
  }
  ENDHLSL
  Pass
  {
   Name "Station Local Fog Integration"
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Integrate
   ENDHLSL
  }
  Pass
  {
   Name "Station Local Fog Composite"
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Composite
   ENDHLSL
  }
 }
}
