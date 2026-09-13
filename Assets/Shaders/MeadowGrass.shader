Shader "Explorers/MeadowGrass"
{
    Properties { _WindStrength("Wind strength", Float) = 0.1 _SurfaceLighting("Surface lighting", Float) = 0 }
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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "NatureWind.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _WindStrength;
                float _SurfaceLighting;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normal:NORMAL; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; half fog:TEXCOORD0; float3 positionWS:TEXCOORD1; half3 normal:TEXCOORD2; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 p = TransformObjectToWorld(input.positionOS.xyz);
                p.xz += NatureWindOffset(p, _WindStrength * .22 * input.uv.y * input.uv.y);
                output.positionCS = TransformWorldToHClip(p);
                output.color = input.color;
                output.normal = TransformObjectToWorldNormal(input.normal);
                output.positionWS = p;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 n=normalize(input.normal);
                half diffuse=lerp(.55,saturate(abs(dot(n,sun.direction))*.7+.15),_SurfaceLighting);
                half transmission=pow(saturate(dot(normalize(_WorldSpaceCameraPos-input.positionWS),-sun.direction)),4)*.32;
                half3 color = input.color.rgb * (SampleSH(half3(0,1,0)) * .7 + sun.color * lerp(.06,1,sun.shadowAttenuation) * (diffuse+transmission));
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
