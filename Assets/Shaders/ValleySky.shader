Shader "Explorers/ValleySky"
{
 Properties { _Tint("Tint", Color) = (0.2,0.45,0.7,1) }
 SubShader {
 Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
 Cull Off ZWrite Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct V { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
 V vert(float4 p:POSITION) { V o; o.pos=TransformObjectToHClip(p.xyz);o.dir=p.xyz;return o; }
 float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
 float noise(float2 p) { float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y); }
 half4 frag(V i):SV_Target {
 float3 d=normalize(i.dir);float h=saturate(d.y);
 float3 col=lerp(float3(.69,.81,.84),float3(.15,.39,.66),pow(h,.45));
 float2 p=d.xz/max(.08,d.y)*2.1+float2(_Time.y*.0015,0);
 float n=noise(p)*.55+noise(p*2.03)*.27+noise(p*4.1)*.13+noise(p*8.3)*.05;
 float cloud=smoothstep(.52,.7,n)*smoothstep(.02,.16,d.y);
 col=lerp(col,lerp(float3(.72,.79,.8),float3(1,.97,.87),saturate((n-.5)*4)),cloud);
 return half4(col,1);
 }
 ENDHLSL
 }
 }
}
