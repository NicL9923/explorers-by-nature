# River rendering research

Research checked September 13, 2026 against Unity 6000.3 and the installed URP 17.3 shader source.

The river previously reflected a procedural sky approximation. This pass samples the real environment probe and traces reflection rays through camera depth, so visible banks and trees can appear in the water. Refraction uses the opaque scene with depth-based absorption. Sunlight uses a GGX microfacet highlight with water's low normal-incidence reflectance. Broken contact foam follows scene depth around rocks, and refracted-light patterns follow the reconstructed riverbed.

## Sources and decisions

Unity's current manual documents `GlossyEnvironmentReflection`, including the world position and screen UV overload. The installed URP `GlobalIllumination.hlsl` confirms the same API. The shader now calls it instead of inventing a sky color. Confidence in API compatibility is high. [Unity 6.3 indirect lighting](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-indirect-lighting.html), checked September 13, 2026.

Unity documents reconstruction from camera depth with `ComputeWorldSpacePosition` and the inverse view-projection matrix. The river uses that method to anchor caustic patterns to the submerged bed. It handles reversed and conventional depth conventions. [Unity 6.3 world-position reconstruction](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/writing-shaders-urp-reconstruct-world-position.html), checked September 13, 2026.

Unity's HDRP 17 documentation describes screen-space reflection using the depth and color buffers and its visibility limitations. This project remains URP. Its river shader implements a separate local ray march, with up to 40 samples and five refinement steps, rather than importing an HDRP override. Rays that leave the image, hit the sky, or exceed the search range fall back to the environment probe. [HDRP 17 screen-space reflections](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/Override-Screen-Space-Reflection.html), checked September 13, 2026.

The physical basis for surface normals, reflection and refraction is established work. Newer rendering systems still depend on it. The existing wave field supplies matching local vertex displacement and slopes; filtered procedural slope bands add smaller ripples. [NVIDIA, Effective water simulation from physical models](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-1-effective-water-simulation-physical-models), published 2004, checked September 13, 2026.

Caustics require concentrating refracted light on submerged surfaces. The shader uses a moving procedural approximation, softened by depth and sun shadow, rather than tracing photons. It adds this light before spectral attenuation. [NVIDIA, Rendering water caustics](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-2-rendering-water-caustics), published 2004, checked September 13, 2026.

The direct sun response uses GGX normal distribution and correlated Smith visibility rather than a fixed-power highlight. These models describe masking between microfacets and the distribution of reflected light. [Walter et al., Microfacet models for refraction through rough surfaces](https://www.cs.cornell.edu/~srm/publications/EGSR07-btdf.html), published 2007, and [Heitz, Understanding the masking-shadowing function in microfacet-based BRDFs](https://jcgt.org/published/0003/02/03/paper.pdf), published June 30, 2014; both checked September 13, 2026.

## Implementation and limits

`Assets/Shaders/River.shader` owns the rendering changes. The existing `RiverDynamics` component enables scene-color effects when both opaque and depth textures are available. High quality provides them; Low retains the procedural riverbed fallback. No extra cameras, render textures, packages or downloaded assets were added.

Screen-space reflections cannot show hidden or offscreen objects. Thin branches may disappear from reflections, and depth discontinuities can produce misses. Probe reflections fill those gaps, but their perspective differs from a planar camera. There is no temporal accumulation, hierarchical depth pyramid, ray tracing or planar reflection pass. The tracer's visual stability still requires rendered evaluation while moving beside the river.

The GGX highlight is bounded to avoid extreme HDR outliers, and its roughness increases with pixel footprint to reduce distant sparkle. This is an artistic stability choice. Absorption coefficients and foam density are art-directed, not measured from a particular river.

The local shallow-wave interaction still propagates pebble impacts and driftwood wakes. This pass does not add a three-dimensional fluid solver, changing river levels, erosion or splash volumes. Those would require different simulation work.

The orchestrator owns the validation ledger. Shader import, Linux/Windows shader builds and river-view captures must pass before treating this pass as validated. Research supports the implementation choices; it does not prove the rendered result.
