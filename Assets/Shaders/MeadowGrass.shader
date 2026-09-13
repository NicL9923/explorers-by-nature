Shader "Explorers/MeadowGrass"
{
    Properties { _WindStrength("Wind strength", Float)=.1 _SurfaceLighting("Surface lighting",Float)=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "NatureWind.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float _WindStrength; float _SurfaceLighting;
        CBUFFER_END
        struct A { float4 positionOS:POSITION; float3 normal:NORMAL; half4 color:COLOR; float2 uv:TEXCOORD0; };
        struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normal:TEXCOORD1; half4 color:COLOR; float2 uv:TEXCOORD2; half fog:TEXCOORD3; };
        V vert(A i)
        {
            V o;
            float3 p=TransformObjectToWorld(i.positionOS.xyz);
            p.xz+=NatureWindOffset(p,_WindStrength*.22*i.uv.y*i.uv.y);
            o.positionCS=TransformWorldToHClip(p); o.positionWS=p;
            o.normal=TransformObjectToWorldNormal(i.normal); o.color=i.color; o.uv=i.uv;
            o.fog=ComputeFogFactor(o.positionCS.z);return o;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags { "LightMode"="UniversalForward" } Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            half4 frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                // Bend the shading normal around each ribbon, avoiding flat strips of uniform light.
                half3 n=normalize(i.normal)*IS_FRONT_VFACE(face,1,-1);
                half3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                half3 across=normalize(cross(n,half3(.02,1,.01)));
                n=normalize(n+across*(i.uv.x-.5)*.65);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half rootOcclusion=lerp(.48,1,saturate(i.uv.y*1.8));
                half diffuse=saturate(dot(n,sun.direction)*.65+.35);
                half transmission=pow(saturate(dot(view,-sun.direction)),5)*.75;
                half3 ambient=SampleSH(normalize(n+half3(0,.65,0)))*rootOcclusion;
                half3 halfDirection=SafeNormalize(view+sun.direction);
                half sheen=pow(saturate(dot(n,halfDirection)),32)*.15;
                half vein=1-.09*pow(saturate(1-abs(i.uv.x*2-1)),8);
                half3 color=i.color.rgb*vein*(ambient+sun.color*sun.shadowAttenuation*
                    (diffuse+transmission*half3(.72,1,.40)))+sun.color*sheen*sun.shadowAttenuation;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor ao=GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                color*=ao.indirectAmbientOcclusion;
                #endif
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags { "LightMode"="ShadowCaster" } Cull Off ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment depthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection; float3 _LightPosition;
            V shadowVert(A i)
            {
                V o=vert(i);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 direction=normalize(_LightPosition-o.positionWS);
                #else
                float3 direction=_LightDirection;
                #endif
                o.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normal,direction)));return o;
            }
            half4 depthFrag(V i):SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode"="DepthOnly" } Cull Off ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment depthFrag
            half depthFrag(V i):SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags { "LightMode"="DepthNormals" } Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment normalFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 normalFrag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                float3 n=normalize(i.normal)*IS_FRONT_VFACE(face,1,-1);
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=PackNormalOctQuadEncode(n);return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                return half4(n,0);
                #endif
            }
            ENDHLSL
        }
    }
}
