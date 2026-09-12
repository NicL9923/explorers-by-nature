# Pinewatch timber kit

Original assets generated for Explorers by Nature. The geometry and wood grain were written for this repository, with no third-party models, textures, or references incorporated. They are original project work.

Regenerate with `blender -b -t 4 --python .agents/tools/make-homestead-art.py` from the repository root. The generator exports six FBX resources in `Assets/Resources/Homestead`, an editable `ArtSource/Homestead/PinewatchHomestead.blend`, two reference renders, and `mesh-budget.json`. Blender's installed OpenColorIO configuration currently needs a newer library, so the reference renders use the available Standard transform.

Each FBX contains two meshes named after its module with `_LOD0` and `_LOD1` suffixes. Both preserve the same construction openings. LOD1 removes bevels, joinery pegs and straw, and uses larger roof shingles. The runtime must select the active LOD and remap the named material slots to URP.

| Resource | LOD0 triangles | LOD1 triangles |
| --- | ---: | ---: |
| Homestead/Foundation | 4,732 | 1,092 |
| Homestead/Wall | 2,272 | 528 |
| Homestead/Door | 1,080 | 264 |
| Homestead/Roof | 9,108 | 1,140 |
| Homestead/Fence | 420 | 84 |
| Homestead/Coop | 5,204 | 1,344 |

The shared material names are `HomesteadTimber`, `HomesteadPlanks`, `HomesteadCedar`, `HomesteadIron`, `HomesteadStraw`, and `HomesteadStone`. The first three have matching `_BaseColor.png` images with original raster wood grain; the FBX files also embed those images. Wood UVs follow each board's long axis. Iron, straw and fieldstone use flat rough colors. `PinewatchWoodGrain.png` is the untinted source image.

The kit uses meters, with Blender Z exported to Unity Y and Blender -Y exported to Unity Z. Mesh origins remain at the placement root. The main foundation deck is exactly 3 by 3 meters at local Y 0.45. Its lower entry apron extends to a 4.6 by 4.6 meter footprint, with board tops at local Y 0.12, matching the runtime step colliders. Walls and doors center on local Z 1.45 with corner posts spanning local Y 0.45 through 2.85. The window aperture spans X -0.53 through 0.53 and Y 1.42 through 2.175, with a thin wooden mullion and transom. The doorway provides 1.19 meters of clear width and a lintel underside at Y 2.485. Roofs have a ridge near Y 3.64 and a roughly 3.45 by 3.42 meter footprint including trim. Fence tops reach Y 1.505. The coop stands on its own legs, with a ramp, rear nesting hatches and overhanging roof.

These are visual meshes. Placement and interaction colliders belong to runtime code. No Unity import, runtime rendering, hardware performance or collision validation is claimed by the Blender reference renders.

Validation completed for the final generated assets: Blender reimported all six FBX files, confirmed both expected LOD mesh names and zero-origin locations, and checked meter-scale world dimensions and triangle budgets. The generator passes Python compilation. Both reference renders were inspected; the final pass corrected coincident timber faces, doorway infill gaps and buried coop shingles.
