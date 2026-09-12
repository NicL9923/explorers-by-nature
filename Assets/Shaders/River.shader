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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; float2 uv:TEXCOORD2; half depth:TEXCOORD3; };
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.world = TransformObjectToWorld(input.positionOS.xyz);
                o.uv=input.uv; o.depth=input.color.r;
                o.positionCS = TransformWorldToHClip(o.world);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(Varyings input):SV_Target
            {
                float2 p = input.world.xz;
                float t=_Time.y;
                float warp=sin(p.x*.27+p.y*.18)*1.8;
                float a=p.x*.7+p.y*.45+t*.7+warp;
                float b=p.y*1.35-p.x*.32+t*.95;
                float riffle=sin(p.x*2.8+sin(p.y*.7)*1.5)*sin(p.y*3.2+t*2.2);
                float3 normal=normalize(float3(sin(a)*.028+cos(b)*.018,1,cos(a*.83)*.021+sin(b)*.016+riffle*.012));
                float3 view=GetWorldSpaceNormalizeViewDir(input.world);
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.world));
                float fresnel=pow(1-saturate(dot(normal,view)),3);
                float glint=pow(saturate(dot(normal,normalize(sun.direction+view))),150);
                float shallow=1-smoothstep(.03,.65,input.depth);
                float bed=sin(p.x*8.5+sin(p.y*5.1))*sin(p.y*9.3+sin(p.x*4.2))*.5+.5;
                half3 sand=lerp(half3(.19,.26,.18),half3(.34,.38,.23),bed);
                half3 deep=lerp(_BaseColor.rgb,sand,shallow*.85);
                half3 reflectedSky=half3(.43,.63,.72)*(.28+sun.color*.55);
                half3 color=lerp(deep,reflectedSky,fresnel*.72);
                color*=.68+sun.color*sun.shadowAttenuation*.32;
                color+=glint*sun.color*sun.shadowAttenuation*.38;
                // Broken narrow current lines follow the river, never a solid white bank outline.
                float current=pow(saturate(sin(p.x*3.4+sin(p.y*.22+t*.18)*1.3)),28);
                float broken=smoothstep(.72,.97,sin(p.y*.9+t*.8+sin(p.x*2))*.5+.5);
                color+=current*broken*.055*sun.color*smoothstep(.03,.4,input.depth);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
