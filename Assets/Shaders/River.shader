Shader "Explorers/River"
{
    Properties { _BaseColor("Water color", Color) = (0.08,0.27,0.3,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; float2 uv:TEXCOORD2; };
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.world = TransformObjectToWorld(input.positionOS.xyz);
                o.uv=input.uv;
                o.positionCS = TransformWorldToHClip(o.world);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(Varyings input):SV_Target
            {
                float2 p = input.world.xz;
                float t=_Time.y;
                float warp=sin(p.x*.27+p.y*.18)*1.8;
                float a=p.x*.7+p.y*.45-t*.7+warp;
                float b=p.y*1.35-p.x*.32-t*.95;
                float3 normal=normalize(float3(sin(a)*.022+cos(b)*.014,1,cos(a*.83)*.018+sin(b)*.012));
                float3 view=GetWorldSpaceNormalizeViewDir(input.world);
                Light sun=GetMainLight();
                float fresnel=pow(1-saturate(dot(normal,view)),3);
                float glint=pow(saturate(dot(normal,normalize(sun.direction+view))),180);
                float edge=min(input.uv.x,1-input.uv.x);
                float shallow=1-smoothstep(0,.18,edge);
                half3 deep=lerp(_BaseColor.rgb,half3(.15,.29,.23),shallow*.6);
                half3 color=lerp(deep,half3(.43,.64,.73),fresnel*.65)+glint*sun.color*.35;
                float lace=(sin(p.y*3.7+p.x*2.1-t)+sin(p.y*1.9-p.x*3.4+t*.6))*.25+.5;
                color=lerp(color,half3(.64,.72,.64),(1-smoothstep(.005,.03,edge))*smoothstep(.4,.75,lace)*.45);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
