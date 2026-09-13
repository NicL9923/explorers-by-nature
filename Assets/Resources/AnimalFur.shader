Shader "Explorers/AnimalFur"
{
 Properties
 {
  _BaseMap("Coat",2D)="white" {}
  _BaseColor("Tint",Color)=(1,1,1,1)
  _FiberCount("Fibers across ribbon",Range(1,7))=7
  _SpecularStrength("Fiber specular strength",Range(0,1))=.16
  _Roughness("Fiber roughness",Range(.15,1))=.48
  _Transmission("Backlit fibers",Range(0,1))=.32
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
  CBUFFER_START(UnityPerMaterial)
  half4 _BaseColor;half _Roughness;half _Transmission;half _FiberCount;half _SpecularStrength;
  CBUFFER_END
  half FiberCoverage(float2 card)
  {
   float phase=card.x*_FiberCount+.08*sin(card.y*7+card.x*4);
   float strand=abs(frac(phase)-.5)*2;
   float width=lerp(.76,.24,card.y);
   return saturate((width-strand)/max(fwidth(strand),.025)+.5);
  }
  ENDHLSL
  Pass
  {
   Tags {"LightMode"="UniversalForward"} Cull Off ZWrite On AlphaToMask On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _LIGHT_LAYERS
   struct A {float4 p:POSITION;float3 n:NORMAL;float4 tangent:TANGENT;float2 uv:TEXCOORD0;float2 card:TEXCOORD1;};
   struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;float2 card:TEXCOORD3;half fog:TEXCOORD4;half3 tangent:TEXCOORD5;};
   V vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.tangent=TransformObjectToWorldDir(i.tangent.xyz);o.uv=i.uv;o.card=i.card;o.fog=ComputeFogFactor(o.p.z);return o;}
   half Lobe(half3 tangent,half3 halfVector,half exponent)
   {
    half d=dot(tangent,halfVector);
    return pow(sqrt(saturate(1-d*d)),exponent);
   }
   half3 FiberLight(Light light,half3 coat,half3 n,half3 tangent,half3 view)
   {
    #if defined(_LIGHT_LAYERS)
    if(!IsMatchingLightLayer(light.layerMask,GetMeshRenderingLayer()))return 0;
    #endif
    half3 h=SafeNormalize(light.direction+view);
    half diffuse=sqrt(saturate(1-pow(dot(tangent,light.direction),2)))*.55+saturate(dot(n,light.direction))*.25;
    half primary=Lobe(normalize(tangent+n*.08),h,lerp(120,18,_Roughness));
    half secondary=Lobe(normalize(tangent-n*.18),h,lerp(48,8,_Roughness));
    half transmission=pow(saturate(dot(view,-light.direction)),5)*_Transmission;
    half3 radiance=light.color*light.distanceAttenuation*light.shadowAttenuation;
    return (coat*(diffuse+transmission)+primary*_SpecularStrength+sqrt(max(coat,0))*secondary*(_SpecularStrength*.75))*radiance;
   }
   half4 frag(V i):SV_Target
   {
    half alpha=FiberCoverage(i.card);clip(alpha-.25);
    half3 coat=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
    half3 n=normalize(i.n),tangent=normalize(i.tangent),view=SafeNormalize(_WorldSpaceCameraPos-i.w);
    half occlusion=lerp(.62,1,sqrt(saturate(i.card.y)));
    half3 col=coat*SampleSH(n)*occlusion;
    col+=FiberLight(GetMainLight(TransformWorldToShadowCoord(i.w)),coat,n,tangent,view)*occlusion;
    #if defined(_ADDITIONAL_LIGHTS)
    uint count=GetAdditionalLightsCount();
    for(uint index=0;index<count;index++)col+=FiberLight(GetAdditionalLight(index,i.w),coat,n,tangent,view)*occlusion;
    #endif
    return half4(MixFog(col,i.fog),alpha);
   }
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} Cull Off ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex shadowVert
   #pragma fragment shadowFrag
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   float3 _LightDirection;float3 _LightPosition;
   struct A {float4 p:POSITION;float3 n:NORMAL;float2 card:TEXCOORD1;};
   struct V {float4 p:SV_POSITION;float2 card:TEXCOORD0;};
   V shadowVert(A i)
   {
    V o;float3 world=TransformObjectToWorld(i.p.xyz);float3 normal=TransformObjectToWorldNormal(i.n);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 light=normalize(_LightPosition-world);
    #else
    float3 light=_LightDirection;
    #endif
    o.p=TransformWorldToHClip(ApplyShadowBias(world,normal,light));
    #if UNITY_REVERSED_Z
    o.p.z=min(o.p.z,UNITY_NEAR_CLIP_VALUE);
    #else
    o.p.z=max(o.p.z,UNITY_NEAR_CLIP_VALUE);
    #endif
    o.card=i.card;return o;
   }
   half4 shadowFrag(V i):SV_Target {clip(FiberCoverage(i.card)-.5);return 0;}
   ENDHLSL
  }
 }
}
