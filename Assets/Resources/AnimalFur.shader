Shader "Explorers/AnimalFur"
{
 Properties { _BaseMap("Coat",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Pass
  {
   Tags {"LightMode"="UniversalForward"} Cull Off ZWrite On AlphaToMask On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float2 card:TEXCOORD1;};
   struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;float2 card:TEXCOORD3;half fog:TEXCOORD4;};
   V vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.uv=i.uv;o.card=i.card;o.fog=ComputeFogFactor(o.p.z);return o;}
   half4 frag(V i):SV_Target
   {
    // Five tapering fibers per narrow clump, with soft MSAA edge coverage.
    float strand=abs(frac(i.card.x*5+.12*sin(i.card.y*4))-0.5)*2;
    float width=(1-i.card.y*.85)*.62;
    half alpha=saturate((width-strand)/max(fwidth(strand),.03)+.5);
    clip(alpha-.25);
    half3 coat=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
    half3 n=normalize(i.n);Light sun=GetMainLight(TransformWorldToShadowCoord(i.w));
    half diffuse=saturate(dot(n,sun.direction));
    half scatter=pow(saturate(dot(normalize(_WorldSpaceCameraPos-i.w),-sun.direction)),5)*.12;
    half3 col=coat*(SampleSH(n)+sun.color*(diffuse+scatter)*sun.shadowAttenuation);
    col*=lerp(.88,1,i.card.y);
    return half4(MixFog(col,i.fog),alpha);
   }
   ENDHLSL
  }
 }
}
