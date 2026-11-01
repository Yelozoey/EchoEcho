Shader "Hidden/EchoStation/ScanWave"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "Echo Scan Wave"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            float4 _WaveOriginRadius;
            float4 _WaveSettings;
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                half4 color=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,uv,0);
                float depth=SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                if(depth<0.000001)return color;
                #else
                if(depth>0.999999)return color;
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 world=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float shell=1-smoothstep(0,_WaveSettings.x,abs(distance(world,_WaveOriginRadius.xyz)-_WaveOriginRadius.w));
                float fade=1-.5*saturate(_WaveOriginRadius.w/_WaveSettings.z);
                color.rgb+=half3(.08,.4,1)*shell*fade*_WaveSettings.y;
                return color;
            }
            ENDHLSL
        }
    }
}
