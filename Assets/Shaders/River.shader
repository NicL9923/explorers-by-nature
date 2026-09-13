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
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
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

            // Camera-depth ray tracing supplies actual visible banks, trees and mountains.
            // Missing/offscreen geometry fades to the environment probe, never a clamped edge.
            bool ProjectRay(float3 world, out float2 uv, out float eye)
            {
                float4 clip=TransformWorldToHClip(world);
                float4 screen=ComputeScreenPos(clip);
                uv=screen.xy/max(screen.w,.0001);
                eye=-TransformWorldToView(world).z;
                return clip.w>.05 && all(uv>.002) && all(uv<.998);
            }
            half4 RiverReflection(float3 origin,float3 direction,float roughness)
            {
                float previous=.12, distance=.18, previousGap=-1;
                [loop] for(int i=0;i<40;i++)
                {
                    if(distance>100)break;
                    float2 uv; float eye;
                    if(!ProjectRay(origin+direction*distance,uv,eye))break;
                    float sceneEye=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
                    float gap=eye-sceneEye;
                    // Refine every crossing BEFORE testing thickness. Testing the coarse step
                    // rejected alternating neighboring rays and punched bright holes in banks.
                    if(previousGap<0 && gap>0 && sceneEye<_ProjectionParams.z*.98)
                    {
                        float lo=previous,hi=distance;
                        [unroll] for(int j=0;j<5;j++)
                        {
                            float mid=(lo+hi)*.5;float2 refineUV;float refineEye;
                            ProjectRay(origin+direction*mid,refineUV,refineEye);
                            float delta=refineEye-LinearEyeDepth(SampleSceneDepth(refineUV),_ZBufferParams);
                            if(delta>0)hi=mid;else lo=mid;
                        }
                        ProjectRay(origin+direction*hi,uv,eye);
                        float edge=min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));
                        float hitDepth=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
                        float residual=max(0,eye-hitDepth);
                        float thickness=.12+hi*.009;
                        float confidence=smoothstep(.025,.14,edge)*(1-smoothstep(65,100,hi));
                        confidence*=1-smoothstep(thickness,thickness*3,residual);
                        confidence*=smoothstep(.15,.65,hi);
                        // A small deterministic cone footprint approximates rough reflection.
                        // Bilateral weights prevent the sky bleeding over bank silhouettes.
                        float2 texel=1/_ScaledScreenParams.xy;
                        float radius=clamp(roughness*hi*.55,1.25,5);
                        half3 filtered=SampleSceneColor(uv)*2;
                        float total=2;
                        float support=0;
                        [unroll] for(int tap=0;tap<4;tap++)
                        {
                            float2 offset=tap==0?float2(1,0):tap==1?float2(-1,0):tap==2?float2(0,1):float2(0,-1);
                            float2 tapUV=saturate(uv+offset*texel*radius);
                            float tapDepth=LinearEyeDepth(SampleSceneDepth(tapUV),_ZBufferParams);
                            float weight=exp2(-abs(tapDepth-hitDepth)/max(.3,hitDepth*.025));
                            filtered+=SampleSceneColor(tapUV)*weight;
                            total+=weight;
                            support+=weight;
                        }
                        // Isolated depth samples are disocclusions rather than trustworthy
                        // reflected detail. Keep a probe contribution at all confidence levels.
                        confidence*=lerp(.25,.88,smoothstep(.5,3,support));
                        return half4(filtered/total,confidence);
                    }
                    previousGap=gap;previous=distance;distance+=.15+distance*.16;
                }
                return 0;
            }
            // Isotropic GGX with Smith visibility and water-air Schlick reflectance.
            float WaterSun(float3 normal,float3 view,float3 light,float roughness)
            {
                float3 h=SafeNormalize(view+light);
                float nv=max(.015,saturate(dot(normal,view))),nl=saturate(dot(normal,light));
                float nh=saturate(dot(normal,h)),vh=saturate(dot(view,h));
                float a2=roughness*roughness*roughness*roughness;
                float denom=nh*nh*(a2-1)+1;
                float distribution=a2/max(3.14159265*denom*denom,.0000001);
                float gv=nl*sqrt(nv*nv*(1-a2)+a2);
                float gl=nv*sqrt(nl*nl*(1-a2)+a2);
                float visibility=.5/max(gv+gl,.0001);
                float fresnel=.02037+.97963*pow(1-vh,5);
                return min(20,distribution*visibility*fresnel*nl);
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
                float2 slope=broadSlope*.018+ripple.yz*.018*middleDetail+smallSlope*.006*fine;
                float4 physical=LocalWaves(p);
                float windStrength=saturate(length(_NatureWind.xz)/5);
                slope *= .75+windStrength*.65;
                // Both the rendered normal and nearby surface vertices respond to the same field.
                slope -= physical.rg;
                float3 normal = normalize(float3(slope.x, 1, slope.y));
                // Micro-ripples broaden the BRDF instead of flipping an entire bank reflection
                // between adjacent pixels. Keep the simulated impact/wake field in this normal.
                float2 reflectionSlope=(broadSlope*.018+ripple.yz*.009*middleDetail)*(.75+windStrength*.65)-physical.rg;
                float3 reflectionNormal=normalize(float3(reflectionSlope.x,1,reflectionSlope.y));
                float3 view = GetWorldSpaceNormalizeViewDir(input.world);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.world));
                half3 ambient = max(SampleSH(float3(0,1,0)), half3(.008,.012,.018));
                float shallow = exp2(-depth * 1.35);
                float contactDepth=depth;
                float2 screenUV=GetNormalizedScreenSpaceUV(input.positionCS);

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
                    contactDepth=max(0,LinearEyeDepth(SampleSceneDepth(screenUV),_ZBufferParams)+TransformWorldToView(input.world).z);
                    float surfaceEye=-TransformWorldToView(input.world).z;
                    float2 distortion=TransformWorldToViewDir(normal).xy;
                    float2 refractedUV=saturate(screenUV+distortion*(.012+.006*depth)*smoothstep(0,.4,depth));
                    float behind=LinearEyeDepth(SampleSceneDepth(refractedUV),_ZBufferParams)-surfaceEye;
                    // Reject distortion across foreground rocks and the bank silhouette.
                    refractedUV=lerp(screenUV,refractedUV,smoothstep(0,.35,behind));
                    float opticalDepth=max(0,LinearEyeDepth(SampleSceneDepth(refractedUV),_ZBufferParams)-surfaceEye);
                    half3 sceneBed=SampleSceneColor(refractedUV);
                    // Anchor refracted light to the actual bed, not the moving water surface.
                    float rawDepth=SampleSceneDepth(refractedUV);
                    #if !UNITY_REVERSED_Z
                        rawDepth=lerp(UNITY_NEAR_CLIP_VALUE,1,rawDepth);
                    #endif
                    float3 bedWorld=ComputeWorldSpacePosition(refractedUV,rawDepth,UNITY_MATRIX_I_VP);
                    float submerged=max(0,input.world.y-bedWorld.y);
                    float causticA=Noise(bedWorld.xz*2.1+float2(t*.27,-t*.21));
                    float causticB=Noise(float2(bedWorld.x*.8-bedWorld.z*.6,bedWorld.x*.6+bedWorld.z*.8)*2.7+float2(-t*.19,t*.17));
                    float caustics=pow(saturate(1-abs(causticA-causticB)*5),7);
                    sceneBed+=caustics*.09*sun.color*sun.shadowAttenuation*exp(-submerged*.6)*fine;
                    half3 attenuation=exp(-min(opticalDepth,12)*half3(.29,.115,.15));
                    half3 waterScatter=_BaseColor.rgb*(ambient*.8+sun.color*sun.shadowAttenuation*.5);
                    transmitted=sceneBed*attenuation+waterScatter*(1-attenuation);
                }

                float facing=saturate(dot(normal,view));
                float fresnel=.02037+.97963*pow(1-facing,5);
                float3 reflected=reflect(-view,reflectionNormal);
                // Increase roughness with the pixel footprint to filter distant sun glitter.
                float normalVariance=dot(ddx(normal),ddx(normal))+dot(ddy(normal),ddy(normal));
                float roughness=sqrt(lerp(.24*.24,.18*.18,fine)+windStrength*.012+min(.12,normalVariance*.6));
                half3 reflection=GlossyEnvironmentReflection(reflected,input.world,roughness,1,screenUV);
                if(_RiverSceneColor>.5 && reflected.y>-.05)
                {
                    half4 traced=RiverReflection(input.world+float3(0,.035,0),reflected,roughness);
                    reflection=lerp(reflection,traced.rgb,traced.a);
                }
                half3 color=lerp(transmitted,reflection,fresnel);
                color+=WaterSun(normal,view,sun.direction,roughness)*sun.color*sun.shadowAttenuation;

                // Sparse cellular flecks gather in shallow riffles and on simulated
                // impact/wake slopes. No global sine crests or continuous white bank.
                float patch=Noise(moving*.58+float2(19.3,7.7));
                float cells=Noise(moving*5.6+float2(3.1,21.8));
                float filaments=1-smoothstep(.022,.09,abs(cells-.5));
                float shelf=(1-smoothstep(.3,1.3,depth))*smoothstep(.035,.22,depth);
                float riffle=smoothstep(.66,.86,patch)*shelf;
                float impact=smoothstep(.008,.095,physical.a)*smoothstep(.3,.66,patch);
                // Real scene depth adds broken foam around protruding stones and timber.
                float contact=(1-smoothstep(.04,.42,contactDepth))*smoothstep(.002,.06,contactDepth);
                float foam=saturate((riffle*.5+impact*.7+contact*.8)*filaments)*fine;
                half3 foamLight = ambient * .65 + sun.color * sun.shadowAttenuation * .7;
                color = lerp(color, foamLight * half3(.65,.72,.67), foam * .65);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
