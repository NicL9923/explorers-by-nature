Shader "Explorers/AlpineRock"
{
    Properties { _SnowLine("Snow line", Float)=290 _RockMap("Granite", 2D)="gray" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            // Slope bias keeps the full-resolution face above Terrain distance tessellation.
            Offset -8,-8
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _SnowLine;
            CBUFFER_END
            TEXTURE2D(_RockMap); SAMPLER(sampler_RockMap);
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half fog:TEXCOORD2; half coverage:TEXCOORD3; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.fog=ComputeFogFactor(output.positionCS.z);
                output.coverage=input.color.a;
                return output;
            }
            float hash(float2 p) { p=frac(p*float2(.1031,.11369));p+=dot(p,p.yx+19.19);return frac(p.x*p.y); }
            float noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            half3 RockSample(float2 uv)
            {
                // Blend differently oriented scales so the photographed patch does not
                // repeat as a visible grid across a several-hundred-metre rock face.
                float blend=noise(uv*.19+float2(37,91));
                float2 warped=uv+float2(noise(uv*.13+7),noise(uv*.13+53))*.45;
                float2 rotated=mul(float2x2(.819,-.574,.574,.819),warped)*.731+float2(17.3,43.8);
                half3 a=SAMPLE_TEXTURE2D(_RockMap,sampler_RockMap,warped).rgb;
                half3 b=SAMPLE_TEXTURE2D(_RockMap,sampler_RockMap,rotated).rgb;
                return lerp(a,b,.25+blend*.5);
            }
            half4 frag(Varyings input):SV_Target
            {
                float3 p=input.positionWS;
                half3 n=normalize(input.normalWS);
                float2 geologicalUV=float2(p.x+p.z*.43,p.y-p.z*.19);
                float broad=noise(geologicalUV*.029+float2(13,87));
                float fractured=noise(geologicalUV*.083+float2(broad*3.1,17));
                float fine=noise(geologicalUV*.37+float2(fractured*1.7,91));
                // Irregular patches conceal the edge of the conforming surface.
                clip(input.coverage-lerp(.025,.975,broad));
                half3 weights=pow(abs(n),4);weights/=max(.001,weights.x+weights.y+weights.z);
                half3 scan=RockSample(p.zy*.12)*weights.x
                    +RockSample(p.xz*.12)*weights.y
                    +RockSample(p.xy*.12)*weights.z;
                // Broad color variation stays subordinate to the scan; thresholded noise
                // made artificial contour lines visible across entire mountain faces.
                float crag=lerp(.91,1.08,broad)*lerp(.97,1.025,fine);
                // Scan samples arrive in linear space. Keep granite reflectance dark enough
                // to retain mineral contrast under direct sun rather than producing beige icing.
                half3 rock=scan*half3(.31,.335,.365)*crag;
                float elevation=smoothstep(_SnowLine,_SnowLine+100,p.y+(broad-.5)*32);
                float shelf=smoothstep(.78,.96,n.y+(fine-.5)*.12);
                float sheltered=smoothstep(.42,.72,fractured);
                float snow=elevation*shelf*sheltered;
                half3 albedo=lerp(rock,half3(.58,.66,.72),snow);
                // Derivative bump follows the same irregular fracture field, without bands.
                float height=(fractured*.8+fine*.2)*1.4;
                float3 dpdx=ddx(p),dpdy=ddy(p);
                float3 r1=cross(dpdy,n),r2=cross(n,dpdx);
                float determinant=dot(dpdx,r1);
                float3 gradient=(ddx(height)*r1+ddy(height)*r2)*sign(determinant);
                half3 geologicalNormal=normalize(abs(determinant)*n-gradient*.045*(1-snow)+n*.00001);
                Light sun=GetMainLight();
                half diffuse=saturate(dot(geologicalNormal,sun.direction));
                half3 ambient=SampleSH(geologicalNormal);
                half3 color=albedo*(ambient*.84+sun.color*diffuse);
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
