Shader "Hidden/EchoStation/Tests/VolumeChart"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; return o;
            }
            uint Glyph(uint index, int row)
            {
                row=clamp(row,0,6);
                bool endRow=row==0 || row==6;
                uint bits=0;
                if(index==0)bits=endRow?31:(row==3?30:16);
                if(index==1)bits=endRow?15:16;
                if(index==2)bits=row==3?31:17;
                if(index==3)bits=endRow?14:17;
                if(index==5)bits=endRow?14:(row==2?19:(row==3?21:(row==4?25:17)));
                if(index==6)bits=row==1?12:(row==6?14:4);
                if(index==7)bits=row==0?14:(row==1?17:(row==2?1:(row==3?2:(row==4?4:(row==5?8:31)))));
                if(index==8)bits=endRow?30:(row==3?14:1);
                return bits;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=i.uv;
                float3 color;
                if(uv.y>0.68) color=floor(saturate(uv.x)*12.0)/2.0;
                else if(uv.y>0.36)
                {
                    float checker=fmod(floor(uv.x*96)+floor(uv.y*54),2);
                    color=checker.xxx;
                    if(uv.x>0.30 && uv.x<0.70) color=0.18;
                    if(uv.x<0.12)color=float3(0.05,0.35,2.0);
                    if(uv.x>0.88)color=float3(3.0,0.25,0.04);
                }
                else
                {
                    float2 p=uv*float2(1920,1080);
                    int glyphLine=(int)floor((p.y-40)/64);
                    float size=glyphLine==0?4:(glyphLine==1?3:2);
                    float2 grid=float2((p.x-48)/size,((p.y-40)-glyphLine*64)/size);
                    uint index=(uint)max(0.0,floor(grid.x/6))%10u;
                    int2 pixel=int2((int)floor(fmod(grid.x,6)),(int)floor(grid.y));
                    bool inside=p.x>=48 && glyphLine>=0 && glyphLine<3 && pixel.x>=0 && pixel.x<5 && pixel.y>=0 && pixel.y<7;
                    uint bits=inside?Glyph(index,6-pixel.y):0;
                    color=(inside && ((bits>>(4-pixel.x))&1))?0.9:0.04;
                }
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
