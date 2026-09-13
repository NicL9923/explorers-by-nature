Shader "Explorers/Foliage"
{
Properties {
_BaseMap("Leaves",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
_Cutoff("Cutout",Range(0,1))=.35 _Cull("Cull",Float)=0 _AlphaClip("Clip",Float)=1
}
SubShader {
Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
HLSLINCLUDE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "NatureWind.hlsl"
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST; half4 _BaseColor; float _Cutoff;
CBUFFER_END
struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
float3 FoliagePosition(A i) {
 float3 p=TransformObjectToWorld(i.p.xyz);
 p.xz+=NatureWindOffset(p,.02*saturate(i.p.y*.3));
 return p;
}
V vert(A i) {
 UNITY_SETUP_INSTANCE_ID(i);V o;
 float3 p=FoliagePosition(i);o.p=TransformWorldToHClip(p);o.w=p;
 o.n=TransformObjectToWorldNormal(i.n);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.fog=ComputeFogFactor(o.p.z);return o;
}
void ClipLeafAlpha(half alpha) {
 #if defined(_ALPHATEST_ON)
 clip(alpha-_Cutoff);
 #endif
}
void LeafClip(V i) {ClipLeafAlpha(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a*_BaseColor.a);}
ENDHLSL
Pass {
Name "ForwardLit" Tags {"LightMode"="UniversalForward"} Cull Off AlphaToMask On
HLSLPROGRAM
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#pragma multi_compile_instancing
 half4 frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
 half4 leaf=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;ClipLeafAlpha(leaf.a);
 half3 n=normalize(i.n)*IS_FRONT_VFACE(face,1,-1);Light sun=GetMainLight(TransformWorldToShadowCoord(i.w));
 half diffuse=saturate(dot(n,sun.direction)*.7+.3);
 half back=pow(saturate(dot(normalize(_WorldSpaceCameraPos-i.w),-sun.direction)),3)*.65;
 half shade=lerp(.10,1,sun.shadowAttenuation);
 half3 ambient=SampleSH(n)*.85+SampleSH(half3(0,1,0))*.13;
 half3 col=leaf.rgb*(ambient+sun.color*shade*(diffuse*.85+back*half3(.75,1,.32)));
 return half4(MixFog(col,i.fog),1);
 }

ENDHLSL
}
Pass {
Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} Cull Off ZWrite On ColorMask 0
HLSLPROGRAM
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma vertex shadowVert
#pragma fragment depthFrag
#pragma multi_compile_instancing
#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
float3 _LightDirection; float3 _LightPosition;
V shadowVert(A i) {
 V o=vert(i);
 #if _CASTING_PUNCTUAL_LIGHT_SHADOW
 float3 lightDirection=normalize(_LightPosition-o.w);
 #else
 float3 lightDirection=_LightDirection;
 #endif
 o.p=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(o.w,o.n,lightDirection)));return o;
}
half4 depthFrag(V i):SV_Target {LeafClip(i);return 0;}
ENDHLSL
}
Pass {
Name "DepthOnly" Tags {"LightMode"="DepthOnly"} Cull Off ZWrite On ColorMask R
HLSLPROGRAM
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma vertex vert
#pragma fragment depthFrag
#pragma multi_compile_instancing
half depthFrag(V i):SV_Target {LeafClip(i);return i.p.z;}
ENDHLSL
}
Pass {
Name "DepthNormals" Tags {"LightMode"="DepthNormals"} Cull Off ZWrite On
HLSLPROGRAM
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma vertex vert
#pragma fragment normalFrag
#pragma multi_compile_instancing
#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
half4 normalFrag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
 LeafClip(i);float3 n=normalize(i.n)*IS_FRONT_VFACE(face,1,-1);
 #if defined(_GBUFFER_NORMALS_OCT)
 float2 oct=PackNormalOctQuadEncode(n); return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
 #else
 return half4(n,0);
 #endif
}
ENDHLSL
}
}
}
