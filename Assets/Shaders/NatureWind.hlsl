#ifndef EXPLORERS_NATURE_WIND_INCLUDED
#define EXPLORERS_NATURE_WIND_INCLUDED
// Shared world-space velocity and gust envelope set by WindWeather.
float4 _NatureWind;
float _NatureWindTime;

float2 NatureWindOffset(float3 world, float compliance)
{
    float speed = min(length(_NatureWind.xz), 12.0);
    float2 direction = _NatureWind.xz / max(length(_NatureWind.xz), .001);
    float phase = dot(world.xz, float2(.083, .061)) - _NatureWindTime * 1.1;
    float wave = .62 + .25 * sin(phase) + .13 * sin(phase * 1.73 + 1.6);
    return direction * speed * min(max(_NatureWind.w, 0.0), 1.8) * wave * compliance;
}

// Object coordinates are meters in the imported trees and ground plants.
// Every LOD samples the same phase at its root, and every pass calls this function.
void NatureVegetation(inout float3 positionOS, inout float3 normalOS)
{
    float3 root = TransformObjectToWorld(float3(0,0,0));
    float3 original = positionOS;
    float y = max(original.y, 0.0);
#if defined(_NATURE_FERN)
    const float height = .45;
    const float flexibility = .019;
#else
    const float height = 20.0;
    const float flexibility = .0045;
#endif
    float h = saturate(y / height);
    float2 offset = NatureWindOffset(root, flexibility * height);
    float3 bendOS = TransformWorldToObjectDir(float3(offset.x, 0, offset.y), false);
    // Scaling with the object retains smaller sapling excursions. Quadratic compliance pins roots.
    float worldScale = max(length(TransformObjectToWorldDir(float3(0,1,0), false)), .001);
    bendOS *= worldScale;
    positionOS += bendOS * h * h;
    float3 slope = bendOS * (2.0 * h / height);
    normalOS = normalize(float3(normalOS.x, normalOS.y - dot(normalOS, slope), normalOS.z));
    float phase = dot(root.xz, float2(.23, .19)) + _NatureWindTime * 2.3;
    float radial = saturate(length(original.xz) / max(height * .2, .12));
    float branch = sin(phase + y * .7) * radial * h;
    positionOS += bendOS * branch * .28;
#if defined(_NATURE_NEEDLES) || defined(_NATURE_FERN)
    // Fine leaf flutter is deliberately much smaller than coherent stem/branch motion.
    float flutter = sin(_NatureWindTime * 5.4 + dot(original, float3(2.7, 1.8, 2.3)) + phase);
    positionOS += bendOS * flutter * h * .055;
#endif
}
#endif
