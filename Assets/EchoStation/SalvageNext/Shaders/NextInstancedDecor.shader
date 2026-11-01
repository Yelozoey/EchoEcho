Shader "EchoStation/Next/InstancedDecor"
{
 Properties { _BaseMap("Base",2D)="white"{} _BaseColor("Tint",Color)=(0.7,0.8,0.9,1) _Wind("Soft strip wind",Float)=0 _Cutoff("Cutoff",Range(0,1))=0.4 _Cull("Cull",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
  Pass
  {
   Name "Forward" Tags{"LightMode"="UniversalForward"} Cull Off
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor;float _Wind,_Cutoff,_Cull;
   CBUFFER_END
   struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
   Varyings Vert(Attributes input)
   {
    Varyings o;UNITY_SETUP_INSTANCE_ID(input);UNITY_TRANSFER_INSTANCE_ID(input,o);
    float3 p=input.positionOS.xyz;p.x+=sin(_Time.y*1.4+TransformObjectToWorld(float3(0,0,0)).z)*.02*_Wind*(1-input.uv.y);
    o.positionWS=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.uv=TRANSFORM_TEX(input.uv,_BaseMap);return o;
   }
   half4 Frag(Varyings i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);half4 sample=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(sample.a-_Cutoff);
    InputData data=(InputData)0;data.positionWS=i.positionWS;data.normalWS=normalize(i.normalWS);data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);data.bakedGI=SampleSH(data.normalWS);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=half4(1,1,1,1);
    SurfaceData surface=(SurfaceData)0;surface.albedo=sample.rgb;surface.metallic=.15;surface.smoothness=.4;surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;return UniversalFragmentPBR(data,surface);
   }
   ENDHLSL
  }
 }
}
