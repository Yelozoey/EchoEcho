Shader "EchoStation/ReflectionSample/Planar PBR"
{
    Properties
    {
        _BaseColor ("Pure albedo", Color) = (0.85,0.88,0.9,1)
        _Roughness ("Perceptual roughness (constant)", Range(0.04,1)) = 0.4
        _Metallic ("Metallic", Range(0,1)) = 0
        _PlanarStrength ("Planar reflection blend", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Roughness;
            half _Metallic;
            half _PlanarStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_PlanarTexture); SAMPLER(sampler_PlanarTexture);
            float4x4 _PlanarVP;
            float4 _PlanarNormal;
            float _PlanarValid;
            struct CityAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct CityVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CityVaryings Vert(CityAttributes v)
            {
                CityVaryings o=(CityVaryings)0;
                UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 Frag(CityVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                InputData data=(InputData)0;
                data.positionWS=i.positionWS;
                data.normalWS=NormalizeNormalPerPixel(i.normalWS);
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                data.bakedGI=SampleSH(data.normalWS);
                data.vertexLighting=VertexLighting(i.positionWS,data.normalWS);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                data.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=_BaseColor.rgb;
                surface.alpha=1;
                surface.metallic=_Metallic;
                surface.smoothness=1-_Roughness;
                surface.normalTS=half3(0,0,1);
                surface.occlusion=1;
                half4 color=UniversalFragmentPBR(data,surface);
                // Replace only the indirect specular radiance, retaining URP's BRDF/Fresnel.
                // No surface texture, normal, roughness or noise texture is sampled.
                if(_PlanarValid>.5 && _PlanarStrength>0 && dot(data.normalWS,_PlanarNormal.xyz)>.95)
                {
                    float4 projected=mul(_PlanarVP,float4(i.positionWS,1));
                    float2 uv=projected.xy/projected.w*.5+.5;
                    #if UNITY_UV_STARTS_AT_TOP
                    uv.y=1-uv.y;
                    #endif
                    if(projected.w>0 && all(uv>=0) && all(uv<=1))
                    {
                        BRDFData brdf;InitializeBRDFData(surface,brdf);
                        half fresnel=Pow4(1-saturate(dot(data.normalWS,data.viewDirectionWS)));
                        half3 direction=reflect(-data.viewDirectionWS,data.normalWS);
                        half3 probe=GlossyEnvironmentReflection(direction,i.positionWS,brdf.perceptualRoughness,1,data.normalizedScreenSpaceUV);
                        half3 planar=SAMPLE_TEXTURE2D_LOD(_PlanarTexture,sampler_PlanarTexture,uv,_Roughness*5).rgb;
                        color.rgb+=(planar-probe)*EnvironmentBRDFSpecular(brdf,fresnel)*_PlanarStrength;
                    }
                }
                return half4(max(color.rgb,0),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            struct DepthAttributes { float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DepthVaryings { float4 positionCS:SV_POSITION;UNITY_VERTEX_OUTPUT_STEREO };
            DepthVaryings DepthVertex(DepthAttributes v) {DepthVaryings o=(DepthVaryings)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(v.positionOS.xyz);return o;}
            half DepthFragment(DepthVaryings v):SV_Target {return v.positionCS.z;}
            ENDHLSL
        }
    }
    FallBack Off
}
