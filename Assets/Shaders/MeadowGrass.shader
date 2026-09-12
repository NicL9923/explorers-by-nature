Shader "Explorers/MeadowGrass"
{
    Properties { _WindStrength("Wind strength", Float) = 0.1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _WindStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; half fog:TEXCOORD0; float3 positionWS:TEXCOORD1; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 p = TransformObjectToWorld(input.positionOS.xyz);
                p.x += sin(_Time.y * 1.5 + p.x * .5 + p.z * .35) * _WindStrength * input.uv.y * input.uv.y;
                output.positionCS = TransformWorldToHClip(p);
                output.color = input.color;
                output.positionWS = p;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 color = input.color.rgb * (SampleSH(half3(0,1,0)) * .7 + sun.color * sun.shadowAttenuation * .55);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
