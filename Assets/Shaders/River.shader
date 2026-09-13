Shader "Explorers/River"
{
    Properties
    {
        _BaseColor("Deep water color", Color) = (0.035,0.16,0.14,1)
        [HideInInspector] _RiverWaves("Local waves", 2D) = "black" {}
        [HideInInspector] _RiverGrid("Local world rectangle", Vector) = (0,0,0,0)
        [HideInInspector] _RiverSceneColor("Scene refraction available", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_RiverWaves); SAMPLER(sampler_RiverWaves);
            float4 _NatureWind;
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _RiverGrid;
                float _RiverSceneColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; float2 uv:TEXCOORD2; half depth:TEXCOORD3; };
            float4 LocalWaves(float2 world)
            {
                float2 uv=(world-_RiverGrid.xy)/max(_RiverGrid.zw,float2(1,1));
                float valid=step(1,_RiverGrid.z)*step(0,uv.x)*step(0,uv.y)*step(uv.x,1)*step(uv.y,1);
                // Pixel centers correspond exactly to the simulation's world-space vertices.
                float2 textureUV=(uv*float2(64,96)+.5)/float2(65,97);
                return SAMPLE_TEXTURE2D_LOD(_RiverWaves,sampler_RiverWaves,textureUV,0)*valid;
            }
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.world = TransformObjectToWorld(input.positionOS.xyz);
                o.world.y += LocalWaves(o.world.xz).b * smoothstep(0,.07,input.color.r);
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

            // Analytic derivatives give the normal a height-surface interpretation.
            // Two rotated bands remove the axis-aligned troughs of a single noise lattice.
            float3 NoiseGradient(float2 p)
            {
                float2 cell=floor(p), f=frac(p);
                float2 u=f*f*f*(f*(f*6-15)+10);
                float2 du=30*f*f*(f*(f-2)+1);
                float a=Hash(cell),b=Hash(cell+float2(1,0));
                float c=Hash(cell+float2(0,1)),d=Hash(cell+1);
                return float3(lerp(lerp(a,b,u.x),lerp(c,d,u.x),u.y),
                    du.x*lerp(b-a,d-c,u.y),du.y*lerp(c-a,d-b,u.x));
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
                float depth = saturate(input.depth) * 4;
                // Advect each band at a uniform speed. Multiplying elapsed time by local
                // depth shears the coordinates indefinitely into parallel bank stripes.
                float2 moving = p - flow * t * .67;
                float2 rotated = float2(moving.x*.8-moving.y*.6,moving.x*.6+moving.y*.8);
                float3 broad = NoiseGradient(rotated*.43);
                float3 ripple = NoiseGradient((p-flow*t*.91)*1.9+float2(41.7,13.4));
                float2 smallUV=float2(p.x*.6+p.y*.8,-p.x*.8+p.y*.6)-float2(.42,.28)*t;
                float3 small = NoiseGradient(smallUV*4.8+17.2);
                float2 broadSlope=float2(broad.y*.8+broad.z*.6,-broad.y*.6+broad.z*.8);
                float2 smallSlope=float2(small.y*.6-small.z*.8,small.y*.8+small.z*.6);
                float middleDetail=1-smoothstep(.12,1.1,footprint);
                float2 slope=broadSlope*.012+ripple.yz*.045*middleDetail+smallSlope*.019*fine;
                float4 physical=LocalWaves(p);
                float windStrength=saturate(length(_NatureWind.xz)/5);
                slope *= .75+windStrength*.65;
                // Both the rendered normal and nearby surface vertices respond to the same field.
                slope -= physical.rg;
                float3 normal = normalize(float3(slope.x, 1, slope.y));
                float3 view = GetWorldSpaceNormalizeViewDir(input.world);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.world));
                half3 ambient = max(SampleSH(float3(0,1,0)), half3(.008,.012,.018));
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
                // Broken refracted-light cells, without regular crossed sine bands.
                float causticNoise=Noise(bedUV*2.3+float2(t*.19,-t*.27));
                float caustic=1-smoothstep(.015,.065,abs(causticNoise-.52));
                bed += caustic * shallow * fine * sun.color * sun.shadowAttenuation * .035;
                half3 transmitted = lerp(_BaseColor.rgb * half3(.45,.66,.58), bed, shallow);
                transmitted *= ambient * .65 + sun.color * sun.shadowAttenuation * .65;

                if(_RiverSceneColor>.5)
                {
                    float2 screenUV=GetNormalizedScreenSpaceUV(input.positionCS);
                    float surfaceEye=-TransformWorldToView(input.world).z;
                    float2 refractedUV=saturate(screenUV+slope*(.012+.006*depth)*smoothstep(0,.4,depth));
                    float behind=LinearEyeDepth(SampleSceneDepth(refractedUV),_ZBufferParams)-surfaceEye;
                    // Reject distortion across foreground rocks and the bank silhouette.
                    refractedUV=lerp(screenUV,refractedUV,smoothstep(0,.35,behind));
                    float opticalDepth=max(0,LinearEyeDepth(SampleSceneDepth(refractedUV),_ZBufferParams)-surfaceEye);
                    half3 sceneBed=SampleSceneColor(refractedUV);
                    half3 attenuation=exp(-min(opticalDepth,12)*half3(.29,.115,.15));
                    half3 waterScatter=_BaseColor.rgb*(ambient*.8+sun.color*sun.shadowAttenuation*.5);
                    transmitted=sceneBed*attenuation+waterScatter*(1-attenuation);
                }

                // Water's low normal-incidence reflectance preserves the green channel.
                // Only grazing views become predominantly sky. Ambient and sun keep the
                // approximation in step with weather without a planar reflection camera.
                float facing = saturate(dot(normal, view));
                float fresnel = .02 + .98 * pow(1 - facing, 5);
                float3 reflected = reflect(-view, normal);
                float skyHeight = saturate(reflected.y);
                half3 sky = ambient * lerp(half3(.82,1.04,1.12), half3(.48,.82,1.12), skyHeight);
                // A broad cloud reflection breaks the featureless silver grazing sheet.
                float2 skyUV=reflected.xz/max(.22,reflected.y);
                float cloud=Noise(skyUV*1.7+float2(t*.004,0));
                sky*=lerp(.84,1.08,smoothstep(.25,.78,cloud));
                sky += sun.color * pow(saturate(dot(reflected, sun.direction)), 24) * .12;
                half3 color = lerp(transmitted, sky, fresnel);

                float3 halfDirection = SafeNormalize(sun.direction + view);
                float specularPower = lerp(70, 260, fine);
                float specular = pow(saturate(dot(normal, halfDirection)), specularPower);
                color += specular * sun.color * sun.shadowAttenuation * .38;

                // Sparse cellular flecks gather in shallow riffles and on simulated
                // impact/wake slopes. No global sine crests or continuous white bank.
                float patch=Noise(moving*.58+float2(19.3,7.7));
                float cells=Noise(moving*5.6+float2(3.1,21.8));
                float filaments=1-smoothstep(.022,.09,abs(cells-.5));
                float shelf=(1-smoothstep(.3,1.3,depth))*smoothstep(.035,.22,depth);
                float riffle=smoothstep(.66,.86,patch)*shelf;
                float impact=smoothstep(.008,.095,physical.a)*smoothstep(.3,.66,patch);
                float foam=saturate((riffle*.5+impact*.7)*filaments)*fine;
                half3 foamLight = ambient * .65 + sun.color * sun.shadowAttenuation * .7;
                color = lerp(color, foamLight * half3(.65,.72,.67), foam * .65);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
