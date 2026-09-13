Shader "Explorers/VegetationLit"
{
Properties {
_BaseMap("Albedo",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
_BumpMap("Normal",2D)="bump" {} _BumpScale("Normal strength",Float)=1
_MetallicGlossMap("Metallic/smoothness",2D)="white" {} _Metallic("Metallic",Float)=0
_Smoothness("Smoothness",Float)=.16 _SpecColor("Specular",Color)=(.2,.2,.2,1)
_Cutoff("Cutoff",Range(0,1))=.35 _Cull("Cull",Float)=2 _AlphaClip("Clip",Float)=0
_AlphaToMask("Alpha to coverage",Float)=0 _Surface("Surface",Float)=0
_OcclusionMap("Occlusion",2D)="white" {} _OcclusionStrength("Occlusion strength",Float)=1
_EmissionMap("Emission",2D)="white" {} _EmissionColor("Emission color",Color)=(0,0,0,0)
}
SubShader {
Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "UniversalMaterialType"="Lit"}
Pass {
Name "ForwardLit"
Tags {"LightMode"="UniversalForward"}
Cull [_Cull]
ZWrite On
AlphaToMask [_AlphaToMask]
HLSLPROGRAM
#pragma target 3.0
#pragma vertex NatureVertex
#pragma fragment LitPassFragment
#pragma multi_compile_instancing
#pragma shader_feature_local _ _NATURE_FERN _NATURE_NEEDLES
#pragma shader_feature_local _NORMALMAP
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
#pragma multi_compile_fog
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "NatureWind.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
Varyings NatureVertex(Attributes input) { UNITY_SETUP_INSTANCE_ID(input);
NatureVegetation(input.positionOS.xyz,input.normalOS);
return LitPassVertex(input); }
ENDHLSL
}
Pass {
Name "ShadowCaster"
Tags {"LightMode"="ShadowCaster"}
Cull [_Cull]
ZWrite On
ColorMask 0
HLSLPROGRAM
#pragma target 3.0
#pragma vertex NatureVertex
#pragma fragment ShadowPassFragment
#pragma multi_compile_instancing
#pragma shader_feature_local _ _NATURE_FERN _NATURE_NEEDLES
#pragma shader_feature_local _NORMALMAP
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "NatureWind.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
Varyings NatureVertex(Attributes input) { UNITY_SETUP_INSTANCE_ID(input);
NatureVegetation(input.positionOS.xyz,input.normalOS);
return ShadowPassVertex(input); }
ENDHLSL
}
Pass {
Name "DepthOnly"
Tags {"LightMode"="DepthOnly"}
Cull [_Cull]
ZWrite On
ColorMask R
HLSLPROGRAM
#pragma target 3.0
#pragma vertex NatureVertex
#pragma fragment DepthOnlyFragment
#pragma multi_compile_instancing
#pragma shader_feature_local _ _NATURE_FERN _NATURE_NEEDLES
#pragma shader_feature_local _NORMALMAP
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "NatureWind.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
Varyings NatureVertex(Attributes input) { UNITY_SETUP_INSTANCE_ID(input);
float3 unusedNormal=float3(0,1,0); NatureVegetation(input.position.xyz,unusedNormal);
return DepthOnlyVertex(input); }
ENDHLSL
}
Pass {
Name "DepthNormals"
Tags {"LightMode"="DepthNormals"}
Cull [_Cull]
ZWrite On
HLSLPROGRAM
#pragma target 3.0
#pragma vertex NatureVertex
#pragma fragment DepthNormalsFragment
#pragma multi_compile_instancing
#pragma shader_feature_local _ _NATURE_FERN _NATURE_NEEDLES
#pragma shader_feature_local _NORMALMAP
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "NatureWind.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
Varyings NatureVertex(Attributes input) { UNITY_SETUP_INSTANCE_ID(input);
NatureVegetation(input.positionOS.xyz,input.normal);
return DepthNormalsVertex(input); }
ENDHLSL
}
}
}
