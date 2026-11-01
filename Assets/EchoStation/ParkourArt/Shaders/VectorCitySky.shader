Shader "EchoStation/Parkour/Clean Sky"
{
    Properties { _Horizon("Horizon",Color)=(.72,.87,.93,1) _Zenith("Zenith",Color)=(.35,.65,.79,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            float4 _Horizon,_Zenith;
            struct V {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
            V Vert(float4 v:POSITION){V o;o.pos=UnityObjectToClipPos(v);o.dir=v.xyz;return o;}
            half4 Frag(V i):SV_Target{return half4(lerp(_Horizon.rgb,_Zenith.rgb,smoothstep(0,1,normalize(i.dir).y)),1);}
            ENDHLSL
        }
    }
}
