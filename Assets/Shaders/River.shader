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
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; };
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.world = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.world);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(Varyings input):SV_Target
            {
                float2 p = input.world.xz;
                float wave = sin(p.x * 1.8 + p.y * .8 - _Time.y * 1.2) * .05;
                float3 normal = normalize(float3(wave, 1, cos(p.y * 2 - _Time.y) * .08));
                float3 view = GetWorldSpaceNormalizeViewDir(input.world);
                Light sun = GetMainLight();
                float fresnel = pow(1 - saturate(dot(normal, view)), 4);
                float glint = pow(saturate(dot(normal, normalize(sun.direction + view))), 120);
                half3 color = lerp(_BaseColor.rgb, half3(.57,.72,.76), fresnel * .7) + glint * sun.color * .65 + wave * .2;
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
