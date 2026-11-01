Shader "EchoStation/Tests/ShadowReceiver"
{
    Properties { _DebugCascade("Debug Cascade", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _DebugCascade;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                if (_DebugCascade > .5)
                {
                    half index = ComputeCascadeIndex(input.positionWS);
                    return half4((index + 1) * .2, 0, 0, 1);
                }
                Light main = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 light = main.color * saturate(main.direction.y) * main.shadowAttenuation;
                #ifdef _ADDITIONAL_LIGHTS
                uint count = GetAdditionalLightsCount();
                for (uint i = 0u; i < count; i++)
                {
                    Light extra = GetAdditionalLight(i, input.positionWS, half4(1,1,1,1));
                    light += extra.color * saturate(extra.direction.y) * extra.distanceAttenuation * extra.shadowAttenuation;
                }
                #endif
                return half4(light, 1);
            }
            ENDHLSL
        }
    }
}
