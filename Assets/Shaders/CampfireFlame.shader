Shader "Explorers/CampfireFlame"
{
 Properties { _Color("Color",Color)=(1,.45,.05,1) }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {Blend SrcAlpha One ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(float4 p:POSITION,float2 uv:TEXCOORD0){V o;o.p=TransformObjectToHClip(p.xyz);o.uv=uv;return o;}
 half4 frag(V i):SV_Target {
 float y=i.uv.y,t=_Time.y;float x=(i.uv.x-.5)*2+sin(y*9-t*5)*.10+sin(y*17-t*3)*.04;
 float width=max(.02,(1-y)*(.75+.18*sin(t*6+y*11)));
 float body=saturate(1-abs(x)/width);float alpha=smoothstep(.05,.55,body)*smoothstep(0,.12,y)*(1-smoothstep(.75,1,y));
 half3 color=lerp(half3(1,.13,.008),half3(1,.85,.23),body*body*(1-y));
 return half4(color,alpha*.8);
 }
 ENDHLSL
 } }
}
