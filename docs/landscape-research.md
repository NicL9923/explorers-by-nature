# Landscape rendering research

Reviewed September 13, 2026 against Unity 6000.3 and the installed URP 17.3 shader source. This pass raises the visual ceiling of the existing valley. It does not change terrain collision, gathering locations or the shared world.

## What was holding the scene back

The alpine shader calculated a single diffuse light without shadow attenuation, specular response or screen-space occlusion. Its renderer explicitly disabled casting and receiving shadows. Grass used a mostly constant diffuse term, had only three triangles per blade, and could not cast shadows or contribute normals to ambient occlusion. Imported ferns and needles used ordinary solid-surface Lit shading. Those omissions made detailed assets look flatter than their geometry justified.

## Research and decisions

Unity 6.3 exposes main-light shadow attenuation, ambient occlusion and physically based lighting to custom URP shaders. The project already contains those implementations in `Lighting.hlsl`. We now use the installed `UniversalFragmentPBR` path for granite and broadleaf foliage, and preserve it for imported vegetation. [Unity 6.3 custom lighting](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-lighting.html) and [custom shadows](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-shadows.html), accessed September 13, 2026.

HDRP has useful capabilities beyond URP, including its dedicated physical sky and volumetric systems. Migrating would require replacing or porting this project's custom shaders and validating the rendering stack again. My recommendation for this pass is to fix the material and geometry defects within URP, then judge the result in captures. Confidence is high that the defects above were real, medium that URP remains the best long-term choice. A demonstrated requirement for HDRP's integrated lighting features would change that decision. [Unity 6.3 pipeline comparison](https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines-feature-comparison.html), accessed September 13, 2026.

Sucker Punch's work on Tsushima treats wind, grass and environmental composition as a coordinated art problem. That is useful here: increasing blade count alone would leave the flat lighting intact. Our grass now has six curved sections, slight cross-section twist, darkened roots, two-sided normals and sunlight transmission. The existing shared wind drives every rendering pass so shadows follow the same blades. This is an original CPU-generated mesh implementation, not a port of Tsushima's GPU grass system. [Sucker Punch on environmental effects](https://blog.playstation.com/2021/01/12/how-stunning-visual-effects-bring-ghost-of-tsushima-to-life/), January 12, 2021, accessed September 13, 2026.

Texture synthesis research shows why plain blending can wash out a repeated texture's contrast. The existing granite already blends rotated, warped samples. This pass retains that method and adds mineral-scale relief derived from the scan, with a pixel-footprint fade to reduce distant shimmer. It does not implement histogram-preserving synthesis or claim to reproduce its results. A full conversion would need prepared texture distributions and a separate comparison. [Deliot, Heitz and Neyret, texture synthesis with histogram-preserving blending](https://unity-grenoble.github.io/website/demo/2020/10/16/demo-histogram-preserving-blend-synthesis.html), October 16, 2020, accessed September 13, 2026.

Newer stochastic texture filtering research addresses the cost and quality of texture filtering, including representations that do not work naturally with hardware filtering. It is not a substitute for correct material lighting, and we have not added its renderer architecture here. [Pharr et al., Filtering After Shading With Stochastic Texture Filtering](https://research.nvidia.com/labs/rtr/publication/pharr2024stochtex/stochtex.pdf), 2024, accessed September 13, 2026.

## Implemented changes

- Granite receives and casts shadows. Its PBR material uses separate rock and snow roughness, scan-derived surface relief, and irregular snow accumulation on sloped shelves. Matching depth and normal passes let it participate in the renderer's occlusion.
- The distant range has a 561 by 205 vertex grid, with nested drainage detail in its visual height function. The playable heightfield remains unchanged.
- Grass has curved, twisting silhouettes and root-to-tip color variation. Its thin-sheet lighting responds to blade orientation, grazing highlights and backlit sun. Grass and understory now cast shadows; their wind deformation is shared across passes.
- Broadleaf foliage uses PBR reflection plus a shadow-aware approximation of light passing through leaves. Imported ferns and conifer needles receive the same transmission treatment and correct back-face normals.
- The high-detail understory attempts 76 plants per spatial cell instead of 42, preserving clear trails, river margins and the ranch footprint.

The transmission is an artist-controlled approximation, not measured leaf scattering. Mountain relief changes shading and distant visual geometry, not traversable rock collision. The new geometry and shadow passes cost more rendering work. No minimum-hardware claim or frame-rate improvement follows from these changes.

## Validation

The orchestrator owns the exact-state validation ledger in `docs/first-milestone.md`. Shader import, Unity tests and rendered high-quality inspection must pass before this work is called validated. This document records implementation and source decisions, not a substitute for that evidence.

High also extends curved grass and undergrowth onto the eastern riverbank and adds 32 candidate positions for riverbank pine groves using the existing CC0 reference trees. Water depth and slope checks keep growth on dry, accessible slopes. These are visual objects and do not change terrain collision.
