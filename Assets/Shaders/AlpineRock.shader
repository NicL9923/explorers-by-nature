Shader "Explorers/AlpineRock"
{
    Properties { _SnowLine("Snow line", Float)=290 _RockMap("Granite", 2D)="gray" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+1" }
        HLSLINCLUDE
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
            void Coverage(Varyings input)
            {
                float2 uv=float2(input.positionWS.x+input.positionWS.z*.43,input.positionWS.y-input.positionWS.z*.19);
                clip(input.coverage-lerp(.025,.975,noise(uv*.029+float2(13,87))));
            }
        ENDHLSL
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
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
                float shelf=smoothstep(.38,.82,n.y+(fine-.5)*.18);
                float sheltered=smoothstep(.42,.72,fractured);
                float snow=elevation*shelf*lerp(.38,1,sheltered);
                half3 albedo=lerp(rock,half3(.58,.66,.72),snow);
                // Derivative bump follows the same irregular fracture field, without bands.
                // Relief includes mineral-scale texture luminance, but fades subpixel detail.
                float micro=dot(scan,half3(.2126,.7152,.0722));
                float footprint=max(length(ddx(p)),length(ddy(p)));
                float height=fractured*2.2+fine*.42+micro*.23*saturate(1-footprint*.7);
                float3 dpdx=ddx(p),dpdy=ddy(p);
                float3 r1=cross(dpdy,n),r2=cross(n,dpdx);
                float determinant=dot(dpdx,r1);
                float3 gradient=(ddx(height)*r1+ddy(height)*r2)*sign(determinant);
                half3 geologicalNormal=normalize(abs(determinant)*n-gradient*.24*(1-snow*.92)+n*.00001);
                InputData lighting=(InputData)0;
                lighting.positionWS=p;lighting.normalWS=geologicalNormal;
                lighting.viewDirectionWS=GetWorldSpaceNormalizeViewDir(p);
                lighting.shadowCoord=TransformWorldToShadowCoord(p);
                lighting.bakedGI=SampleSH(geologicalNormal);
                lighting.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=albedo;surface.alpha=1;surface.normalTS=half3(0,0,1);
                surface.metallic=0;surface.smoothness=lerp(.13,.42,snow);
                surface.occlusion=lerp(.74,1,smoothstep(.15,.8,fractured));
                half4 color=UniversalFragmentPBR(lighting,surface);
                return half4(MixFog(color.rgb,input.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags { "LightMode"="ShadowCaster" } Cull Back ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment depthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;float3 _LightPosition;
            Varyings shadowVert(Attributes i)
            {
                Varyings o=vert(i);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 direction=normalize(_LightPosition-o.positionWS);
                #else
                float3 direction=_LightDirection;
                #endif
                o.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,direction)));return o;
            }
            half4 depthFrag(Varyings i):SV_Target { Coverage(i);return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode"="DepthOnly" } Cull Back ZWrite On ColorMask R
            Offset -8,-8
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment depthFrag
            half depthFrag(Varyings i):SV_Target { Coverage(i);return i.positionCS.z; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags { "LightMode"="DepthNormals" } Cull Back ZWrite On
            Offset -8,-8
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment normalFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 normalFrag(Varyings i):SV_Target
            {
                Coverage(i);float3 n=normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=PackNormalOctQuadEncode(n);return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                return half4(n,0);
                #endif
            }
            ENDHLSL
        }
    }
}
