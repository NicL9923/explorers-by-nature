Shader "Explorers/Foliage"
{
 Properties {
 _BaseMap("Leaves",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
 _Cutoff("Cutout",Range(0,1))=.35 _Cull("Cull",Float)=0 _AlphaClip("Clip",Float)=1
 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
 Pass {
 Tags {"LightMode"="UniversalForward"} Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST; half4 _BaseColor; float _Cutoff;
 CBUFFER_END
 struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
 V vert(A i){UNITY_SETUP_INSTANCE_ID(i);V o;float3 p=TransformObjectToWorld(i.p.xyz);p.xz+=float2(sin(p.x*.7+p.z*.4+_Time.y*1.2),cos(p.x*.4+_Time.y*.8))*.045*saturate(i.p.y*.3);o.p=TransformWorldToHClip(p);o.w=p;o.n=TransformObjectToWorldNormal(i.n);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
 half4 leaf=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(leaf.a-_Cutoff);
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
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}
