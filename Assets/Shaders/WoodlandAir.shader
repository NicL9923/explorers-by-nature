Shader "Explorers/WoodlandAir"
{
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent"}
 Pass {
 Cull Front ZWrite Off ZTest Always Blend One OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 float4 _BoxMin,_BoxMax;float _WeatherWetness;
 struct V {float4 pos:SV_POSITION;float3 world:TEXCOORD0;};
 V vert(float4 p:POSITION){V o;o.world=TransformObjectToWorld(p.xyz);o.pos=TransformWorldToHClip(o.world);return o;}
 half4 frag(V i):SV_Target {
 float3 origin=_WorldSpaceCameraPos,ray=normalize(i.world-origin);
 float3 inv=1/(ray+float3(.000001,.000001,.000001));float3 a=(_BoxMin.xyz-origin)*inv,b=(_BoxMax.xyz-origin)*inv;
 float3 nearv=min(a,b),farv=max(a,b);float start=max(0,max(nearv.x,max(nearv.y,nearv.z)));float end=min(farv.x,min(farv.y,farv.z));
 float2 uv=GetNormalizedScreenSpaceUV(i.pos);float depth=SampleSceneDepth(uv);
 #if !UNITY_REVERSED_Z
 depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
 #endif
 float3 surface=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);end=min(end,length(surface-origin));
 if(end<=start)return 0;
 float stride=(end-start)/48;float jitter=frac(sin(dot(i.pos.xy,float2(12.9898,78.233)))*43758.5453);
 half3 sum=0;float opacity=0;
 [loop] for(int k=0;k<48;k++){
 float3 p=origin+ray*(start+(k+jitter)*stride);
 float2 edge=saturate(min(p.xz-_BoxMin.xz,_BoxMax.xz-p.xz)/12);
 float density=exp(-max(0,p.y-_BoxMin.y)*.10)*edge.x*edge.y*.006*(1+_WeatherWetness*.35);
 Light sun=GetMainLight(TransformWorldToShadowCoord(p));
 float mu=dot(ray,sun.direction),g=.62;
 float phase=.16+(1-g*g)/pow(max(.02,1+g*g-2*g*mu),1.5)*.16;
 density*=.7+.3*sin(p.x*.11+p.z*.08+_Time.y*.13)*sin(p.y*.35+p.z*.035);
 float alpha=1-exp(-density*stride);
 half3 light=SampleSH(half3(0,1,0))*.40+sun.color*sun.shadowAttenuation*phase;
 sum+=(1-opacity)*alpha*light;opacity+=(1-opacity)*alpha;
 }
 return half4(sum,opacity);
 }
 ENDHLSL
 }
 }
}
