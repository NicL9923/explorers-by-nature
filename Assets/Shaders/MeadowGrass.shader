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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _WindStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; half fog:TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 p = TransformObjectToWorld(input.positionOS.xyz);
                p.x += sin(_Time.y * 1.5 + p.x * .5 + p.z * .35) * _WindStrength;
                output.positionCS = TransformWorldToHClip(p);
                output.color = input.color;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                Light sun = GetMainLight();
                half3 color = input.color.rgb * (half3(.3,.34,.3) + sun.color * .7);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
