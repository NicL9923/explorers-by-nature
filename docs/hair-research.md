# Hair and fur research

Research checked September 13, 2026. This pass implements original Unity code and geometry. It imports no third-party hair assets or package code.

## What the research changes

Hair needs a direction-dependent highlight. A shiny surface normal alone cannot describe a fiber. Marschner and colleagues measured and modeled scattering from individual hairs; their later work explains why repeated scattering also changes the apparent color of pale hair. The game's new shader carries the actual curved strand tangent into lighting. It uses two shifted anisotropic highlight lobes and a colored backlight term. These are practical approximations, not an implementation of the full measured scattering model. [Cornell hair research](https://www.cs.cornell.edu/~srm/research/hair.html).

AMD's TressFX remains a reference for real-time simulation and rendering. Its solver work specifically addresses stretching under rapid root acceleration and the importance of shape constraints. The game's guides now have six segments, world-space inertia, wind forces, damped shape memory, pinned roots, and exact segment-length projection. A local skin plane prevents guides from folding into their attachment surface. Teleports clear motion history. [TressFX](https://gpuopen.com/tressfx/), [simulation changes](https://gpuopen.com/news/tressfx-4-simulation-changes/).

Unity's Demo Team hair package offers GPU strand simulation and rendering with authoring and import support. It is a stronger foundation for a future full groom pipeline than hand-writing every production feature. This pass keeps the existing URP materials and imported animal weights, so it does not add that package or claim equivalent capabilities. [Unity hair repository](https://github.com/Unity-Technologies/com.unity.demoteam.hair).

Recent research also explores thicker strand clusters that preserve scattering across levels of detail. That is relevant when this game later optimizes dense grooms. The current High preset instead adds three follower clumps per guide, while Low keeps one clump on every third guide. It does not implement elliptical scattering or continuous groom LOD. [Real-time Level-of-Detail Strand-based Hair Rendering, 2024](https://arxiv.org/abs/2405.10565).

Quaffure, published at CVPR 2025, predicts plausible hair drape with a trained neural model. That is useful research for many avatars, but its quasi-static output does not directly replace the wind and motion response wanted here. There is no neural inference dependency in this implementation. [Quaffure](https://arxiv.org/abs/2412.10061).

## Changes in this project

- Deer and cow High coats have 5,400 clumps each, up from 1,800. Rabbit and fox High coats have 3,600. Each clump has two crossing, six-segment tapered ribbons with seven procedural fibers across their width.
- All simulated coats use curved guide tangents for highlights, root darkening, backlighting, additional forward lights, and alpha-clipped shadow silhouettes. High coats remain visible to 35 meters.
- The horse gains 190 moving mane guides and 180 long tail guides, attached to its animated neck and tail pivots. Mane ribbons follow authored neck arcs, with simulated tip displacement limited to 35 mm and smoothly distributed along each arc. This is guide-driven groom deformation, not segment-by-segment body collision. A transported ribbon frame prevents twisting between rows.
- Each explorer gains 110 loose hairs around the temples and rear hairline. The scout and homesteader have longer hanging hairs. Existing solid braids, hairstyles and mane locks still supply their underlying volume.
- The mounted local player's hidden head also hides its loose hairs. The wardrobe can render them through its dedicated preview camera.

## What remains limited

This is a CPU guide solver and ribbon renderer. It does not simulate individual microscopic fibers, strand-to-strand collisions, scalp or body signed-distance fields, multiple scattering through a dense volume, or order-independent transparency. The local collision plane protects the root surface, but long hairs can still intersect other body parts. Existing braid meshes remain rigid. These limitations need authored grooms and a broader character collision setup to remove.

The new tests check length preservation under strong inward wind, pinned roots, teleport reset, and comparable settled shapes across frame rates. Passing tests do not establish attractive hair. Final acceptance also requires rendered horse, animal and wardrobe close-ups plus motion evidence. The shared validation ledger records the checks that actually ran.
