Shader "Hidden/EchoStation/SalvageNext/LocalFog"
{
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" }
  ZWrite Off ZTest Always Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
  TEXTURE2D_X(_NextFogTexture);
  float4 _NextFogTexture_TexelSize,_NextFogSettings;
  float4 _NextFogMin[3],_NextFogMax[3];
  float4 _NextFogLightPosition[4],_NextFogLightColor[4],_NextFogLightDirection[4];
  bool Interval(float3 origin,float3 direction,float3 lo,float3 hi,float maximum,out float first,out float last)
  {
   float3 inverse=sign(direction+1e-7)/max(abs(direction),1e-5);
   float3 a=(lo-origin)*inverse,b=(hi-origin)*inverse;
   float3 nearHit=min(a,b),farHit=max(a,b);
   first=max(0,max(nearHit.x,max(nearHit.y,nearHit.z)));last=min(maximum,min(farHit.x,min(farHit.y,farHit.z)));return last>first;
  }
  bool Inside(float3 p,int box){return all(p>=_NextFogMin[box].xyz)&&all(p<=_NextFogMax[box].xyz);}
  half4 Integrate(Varyings input):SV_Target
  {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
   float2 uv=input.texcoord;float depth=SampleSceneDepth(uv);
   #if !UNITY_REVERSED_Z
   depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
   #endif
   float3 end=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP),origin=_WorldSpaceCameraPos;
   float3 direction=normalize(end-origin);float maximum=min(distance(origin,end),80),first=maximum,last=0;
   [unroll]for(int box=0;box<3;box++){float nearHit,farHit;if(Interval(origin,direction,_NextFogMin[box].xyz,_NextFogMax[box].xyz,maximum,nearHit,farHit)){first=min(first,nearHit);last=max(last,farHit);}}
   if(last<=first)return half4(0,0,0,1);
   float stepSize=(last-first)/16,transmission=1;float3 scatter=0;
   [unroll]for(int sampleIndex=0;sampleIndex<16;sampleIndex++)
   {
    float3 p=origin+direction*(first+(sampleIndex+.5)*stepSize);float density=0;
    [unroll]for(int box=0;box<3;box++)if(Inside(p,box))density+=_NextFogSettings[box];
    density*=1+.12*sin(p.x*1.7+p.z*.9)*sin(p.y*2.3);
    float3 illumination=float3(.035,.055,.075);
    [unroll]for(int light=0;light<4;light++)
    {
     float range=_NextFogLightPosition[light].w;if(range<=0)continue;
     float3 fromLight=p-_NextFogLightPosition[light].xyz;float distanceSquared=max(dot(fromLight,fromLight),.1);
     float attenuation=pow(saturate(1-distanceSquared/(range*range)),2)/distanceSquared;
     if(_NextFogLightColor[light].w> -1)attenuation*=smoothstep(_NextFogLightColor[light].w,_NextFogLightDirection[light].w,dot(normalize(fromLight),_NextFogLightDirection[light].xyz));
     illumination+=_NextFogSettings.w*.15*attenuation*_NextFogLightColor[light].rgb;
    }
    float attenuation=exp(-density*stepSize);scatter+=transmission*(1-attenuation)*illumination;transmission*=attenuation;
   }
   return half4(scatter,transmission);
  }
  half4 Composite(Varyings input):SV_Target
  {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);float2 uv=input.texcoord;half4 color=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,uv,0);
   float center=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);float4 sum=0;float weights=0;
   [unroll]for(int i=0;i<4;i++){float2 offset=float2((i&1)?.5:-.5,(i&2)?.5:-.5)*_NextFogTexture_TexelSize.xy;float2 sampleUv=uv+offset;float z=LinearEyeDepth(SampleSceneDepth(sampleUv),_ZBufferParams);float weight=1/(1+abs(z-center)*32);sum+=SAMPLE_TEXTURE2D_X_LOD(_NextFogTexture,sampler_LinearClamp,sampleUv,0)*weight;weights+=weight;}
   float4 fog=sum/max(weights,1e-5);return half4(color.rgb*fog.a+fog.rgb,color.a);
  }
  ENDHLSL
  Pass
  {
   Name "Next Local Fog Integration"
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex Vert
   #pragma fragment Integrate
   ENDHLSL
  }
  Pass
  {
   Name "Next Local Fog Composite"
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex Vert
   #pragma fragment Composite
   ENDHLSL
  }
 }
}
