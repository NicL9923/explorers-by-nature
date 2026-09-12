Shader "Explorers/GentleRain"
{
 Properties { _Color("Color", Color) = (1,1,1,1) }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
 Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A { float4 p:POSITION; half4 c:COLOR; float2 uv:TEXCOORD0; };
 struct V { float4 p:SV_POSITION; half4 c:COLOR; float2 uv:TEXCOORD0; };
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.c=i.c;o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target {return half4(i.c.rgb,i.c.a*saturate(1-abs(i.uv.x*2-1))*sin(i.uv.y*3.14159));}
 ENDHLSL
 } }
}
