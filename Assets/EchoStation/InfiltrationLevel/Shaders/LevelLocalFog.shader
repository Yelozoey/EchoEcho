Shader "Hidden/EchoStation/InfiltrationLevel/LocalFog"
{
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" }
  ZWrite Off ZTest Always Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D_X(_ILFogTexture);
  float4 _ILFogTexture_TexelSize,_ILFogSettings,_ILFogOptions;
  float4 _ILFogMin[3],_ILFogMax[3];
  bool Interval(float3 o,float3 d,float3 lo,float3 hi,float maximum,out float first,out float last)
  {
   float3 inv=sign(d+1e-7)/max(abs(d),1e-5),a=(lo-o)*inv,b=(hi-o)*inv;
   float3 n=min(a,b),f=max(a,b);first=max(0,max(n.x,max(n.y,n.z)));last=min(maximum,min(f.x,min(f.y,f.z)));return last>first;
  }
  half4 Integrate(Varyings input):SV_Target
  {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
   float2 uv=input.texcoord;float depth=SampleSceneDepth(uv);
   #if !UNITY_REVERSED_Z
    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
   #endif
   float3 end=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP),o=_WorldSpaceCameraPos,d=normalize(end-o);
   float maximum=min(distance(o,end),60),first=maximum,last=0;
   [unroll]for(int b=0;b<3;b++){float n,f;if(Interval(o,d,_ILFogMin[b].xyz,_ILFogMax[b].xyz,maximum,n,f)){first=min(first,n);last=max(last,f);}}
   if(last<=first)return half4(0,0,0,1);
   float stepSize=(last-first)/16,transmission=1;float3 scatter=0;
   [unroll]for(int s=0;s<16;s++)
   {
    float3 p=o+d*(first+(s+.5)*stepSize);float density=0;
    [unroll]for(int b=0;b<3;b++)if(all(p>=_ILFogMin[b].xyz)&&all(p<=_ILFogMax[b].xyz))density+=_ILFogSettings[b];
    Light main=GetMainLight(TransformWorldToShadowCoord(p));
    float shadow=lerp(1,main.shadowAttenuation,_ILFogOptions.y);
    float phase=.65+.35*pow(saturate(dot(d,main.direction)),4);
    float3 illumination=float3(.035,.05,.07)+_ILFogSettings.w*main.color*shadow*phase;
    float attenuation=exp(-density*stepSize);scatter+=transmission*(1-attenuation)*illumination;transmission*=attenuation;
   }
   return half4(scatter,transmission);
  }
  half4 Composite(Varyings input):SV_Target
  {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);float2 uv=input.texcoord;
   half4 color=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,uv,0);
   float center=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams),weights=0;float4 sum=0;
   [unroll]for(int i=0;i<4;i++){float2 offset=float2((i&1)?.5:-.5,(i&2)?.5:-.5)*_ILFogTexture_TexelSize.xy;float2 at=uv+offset;float z=LinearEyeDepth(SampleSceneDepth(at),_ZBufferParams);float w=1/(1+abs(z-center)*32);sum+=SAMPLE_TEXTURE2D_X_LOD(_ILFogTexture,sampler_LinearClamp,at,0)*w;weights+=w;}
   float4 fog=sum/max(weights,1e-5);return half4(lerp(color.rgb*fog.a+fog.rgb,fog.rgb,_ILFogOptions.x),color.a);
  }
  ENDHLSL
  Pass
  {
   Name "IL Local Fog Integration"
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex Vert
   #pragma fragment Integrate
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   ENDHLSL
  }
  Pass
  {
   Name "IL Local Fog Composite"
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex Vert
   #pragma fragment Composite
   ENDHLSL
  }
 }
}
