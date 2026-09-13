Shader "Explorers/ValleySky"
{
 Properties { _Tint("Tint", Color)=(.2,.45,.7,1) _Daylight("Daylight",Float)=1 _CloudCover("Cloud cover",Float)=0 _SunDirection("Sun direction",Vector)=(0,1,0,0) }
 SubShader {
 Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
 Cull Off ZWrite Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float _Daylight,_CloudCover;float4 _SunDirection;
 struct V {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
 V vert(float4 p:POSITION){V o;o.pos=TransformObjectToHClip(p.xyz);o.dir=p.xyz;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float cloudNoise(float2 p){float a=noise(p);p=mul(float2x2(.8,-.6,.6,.8),p);return a*.52+noise(p*2.13)*.27+noise(p*4.41)*.14+noise(p*8.8)*.07;}
 half4 frag(V i):SV_Target {
 float3 d=normalize(i.dir),sun=normalize(_SunDirection.xyz);float h=saturate(d.y),facing=saturate(dot(d,sun));
 float3 zenith=lerp(float3(.025,.055,.13),float3(.095,.28,.53),_Daylight);
 float3 horizon=lerp(float3(.20,.24,.34),float3(.66,.78,.82),_Daylight);
 float warm=pow(facing,5)*pow(1-h,3)*saturate(1-_Daylight*.8);
 float3 col=lerp(horizon,zenith,pow(h,.43))+float3(.45,.18,.025)*warm;
 col+=pow(facing,32)*float3(.36,.24,.12)*(.3+_Daylight*.7);
 // Two independent wind layers: sculpted cumulus beneath thin high wisps.
 float2 p=d.xz/max(.12,d.y+.10)*1.8+float2(_Time.y*.0018,_Time.y*.0005);
 float broad=cloudNoise(p);float detail=noise(p*15.7);
 float density=smoothstep(lerp(.51,.30,_CloudCover),lerp(.67,.59,_CloudCover),broad+detail*.025)*smoothstep(.015,.14,d.y);
 float towardsSun=cloudNoise(p+sun.xz*.18);
 float shadow=saturate((towardsSun-broad)*6+.45);
 float silver=pow(saturate(1-abs(density-.35)*2),3)*pow(facing,9);
 float3 cloudShade=lerp(float3(.27,.35,.47),float3(.54,.63,.71),_Daylight);
 float3 cloudLight=lerp(float3(.53,.48,.48),float3(1.22,1.13,.94),_Daylight);
 float3 clouds=lerp(cloudLight,cloudShade,shadow*.85+_CloudCover*.12)+silver*float3(.5,.36,.16);
 col=lerp(col,clouds,density);
 float wisps=smoothstep(.66,.84,cloudNoise(p*.48+float2(17,5)))*smoothstep(.25,.7,d.y)*.18;
 col=lerp(col,cloudLight,wisps*(1-density));
 float disk=smoothstep(.9996,.99985,facing);
 col+=disk*lerp(float3(.55,.67,.95),float3(5,3.8,2.1),_Daylight)*(1-density*.97);
 return half4(col,1);
 }
 ENDHLSL
 }
 }
}
