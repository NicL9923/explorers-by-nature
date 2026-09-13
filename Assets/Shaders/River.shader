Shader "Explorers/River"
{
    Properties { _BaseColor("Water color", Color) = (0.035,0.16,0.14,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; float2 uv:TEXCOORD2; half depth:TEXCOORD3; };
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.world = TransformObjectToWorld(input.positionOS.xyz);
                o.uv = input.uv;
                o.depth = input.color.r;
                o.positionCS = TransformWorldToHClip(o.world);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float Hash(float2 p)
            {
                float3 q = frac(float3(p.xyx) * .1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }
            float Noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * f * (f * (f * 6 - 15) + 10);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1,0)), f.x),
                    lerp(Hash(cell + float2(0,1)), Hash(cell + 1), f.x), f.y);
            }

            half4 frag(Varyings input):SV_Target
            {
                float2 p = input.world.xz;
                float t = _Time.y;
                // River tangent follows ValleyShape.RiverX. Waves and foam travel downstream.
                float2 flow = normalize(float2(.264 * cos(p.y * .012), 1));
                float2 across = float2(flow.y, -flow.x);
                float2 river = float2(dot(p, across), p.y);
                float footprint = max(length(ddx(p)), length(ddy(p)));
                float fine = 1 - smoothstep(.08, .75, footprint);
                // Advected, rotated noise prevents long regular sine-wave interference
                // bands. Calm broad motion stays subtle; only nearby riffles carry detail.
                float2 moving = p - flow * t * .32;
                float2 rotated = float2(moving.x * .8 - moving.y * .6, moving.x * .6 + moving.y * .8);
                float warp = Noise(rotated * .085) * 3;
                float broadX = Noise(rotated * .34 + float2(warp, -warp));
                float broadZ = Noise(rotated * .39 + float2(17.3, 51.7) - warp);
                float2 slope = (float2(broadX, broadZ) - .5) * .075;
                float c = river.x * 5.8 + river.y * 3.2 - t * 3.6 + broadX * 12;
                slope += float2(sin(c), cos(c * .79 + broadZ * 9)) * (.006 * fine);
                float3 normal = normalize(float3(slope.x, 1, slope.y));
                float3 view = GetWorldSpaceNormalizeViewDir(input.world);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.world));
                half3 ambient = max(SampleSH(float3(0,1,0)), half3(.008,.012,.018));
                float depth = saturate(input.depth) * 4;
                float shallow = exp2(-depth * 1.35);

                // An optically attenuated, refracted bed costs no scene-color copy. A filtered
                // irregular pebble pattern replaces the former regular checkerboard surface.
                float2 bedUV = p + slope * (.7 + depth * .6);
                float sediment = Noise(bedUV * .42);
                float stones = Noise(bedUV * 5.4);
                float pebble = smoothstep(.24, .72, stones);
                float joints = 1 - smoothstep(.15, .29, stones);
                float bedDetail = 1 - smoothstep(.06, .45, footprint);
                half3 bed = lerp(half3(.12,.145,.085), half3(.31,.29,.19), sediment);
                bed *= lerp(1, .7 + pebble * .45 - joints * .3, bedDetail);
                float caustic = pow(saturate(sin(bedUV.x * 2.9 + sin(bedUV.y * 2.1 - t))
                    * sin(bedUV.y * 3.7 + sin(bedUV.x * 1.8 + t * .7))), 5);
                bed += caustic * shallow * fine * sun.color * sun.shadowAttenuation * .10;
                half3 transmitted = lerp(_BaseColor.rgb * half3(.45,.66,.58), bed, shallow);
                transmitted *= ambient * .65 + sun.color * sun.shadowAttenuation * .65;

                // Water's low normal-incidence reflectance preserves the green channel.
                // Only grazing views become predominantly sky. Ambient and sun keep the
                // approximation in step with weather without a planar reflection camera.
                float facing = saturate(dot(normal, view));
                float fresnel = .02 + .98 * pow(1 - facing, 5);
                float3 reflected = reflect(-view, normal);
                float skyHeight = saturate(reflected.y);
                half3 sky = ambient * lerp(half3(1.32,1.5,1.56), half3(.63,1.04,1.48), skyHeight);
                sky += sun.color * pow(saturate(dot(reflected, sun.direction)), 24) * .12;
                half3 color = lerp(transmitted, sky, fresnel);

                float3 halfDirection = SafeNormalize(sun.direction + view);
                float specularPower = lerp(70, 260, fine);
                float specular = pow(saturate(dot(normal, halfDirection)), specularPower);
                color += specular * sun.color * sun.shadowAttenuation * .7;

                // Broken riffles gather over shallow shelves, with no continuous white bank.
                float foamNoise = Noise(float2(river.x * 1.8, river.y * .65 - t * .65));
                float crest = sin(river.x * 9 + sin(river.y * 1.7 - t * 1.8) * 2);
                float crestWidth = max(fwidth(crest), .045);
                float foam = smoothstep(.88 - crestWidth, .96 + crestWidth, crest)
                    * smoothstep(.64, .86, foamNoise) * (1 - smoothstep(.25, 1.65, depth))
                    * smoothstep(.02, .18, depth) * fine;
                half3 foamLight = ambient * .65 + sun.color * sun.shadowAttenuation * .7;
                color = lerp(color, foamLight * half3(.65,.72,.67), foam * .65);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
