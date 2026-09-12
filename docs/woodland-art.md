# Woodland art

Original procedural pine, aspen, and oxeye-daisy clump assets replace the tree cones, sphere canopies, and sphere flowers. Their editable source is `ArtSource/Woodland/Woodland.blend`. No third-party geometry or images are used. The generator creates the needle, leaf, and bark textures from mathematical shapes and seeded noise.

Regenerate from the repository root with:

```bash
blender --background --factory-startup --python .agents/tools/make-woodland-art.py
```

The script writes nine FBX files and five 512 × 512 PNG textures to `Assets/Resources/Woodland`. It also writes measured mesh counts and three preview images to `ArtSource/Woodland`. The source scene includes studio lighting and a camera for the high-detail trees. Preview colors use Blender's fallback color configuration on this machine; judge final colors in Unity.

| Asset | LOD0 triangles | LOD1 triangles | LOD2 triangles | Source height |
| --- | ---: | ---: | ---: | ---: |
| Pine | 5,774 | 3,182 | 612 | 10.32 m |
| Aspen | 5,920 | 2,428 | 620 | 9.00 m |
| Wildflower | 693 | 574 | 455 | 0.65 m |

Each FBX contains one mesh, named exactly like its file stem, such as `Pine_LOD0`. Tree origins are at ground level. Unity uses Y-up after FBX export. Each tree has two material slots; the flower has three. LOD foliage placement and silhouette differ slightly, so transition distances need visual verification in the game.

Pines have tapered bent trunks, irregular branch whorls, and crossed needle spray cards. Aspens have slender marked trunks, ascending limbs and branchlets, and individually oriented leaf cards. Daisies have bent stems, narrow leaves, shaped petals and a raised central disk. Leaf and petal backs must remain visible.

## Unity material contract

- `PineBark` uses `PineBark.png`; `AspenBark` uses `AspenBark.png`. Opaque URP Lit, rough surface.
- `PineNeedles` uses `PineNeedles.png`; the low pine uses `PineNeedlesDistant.png` with material `PineNeedlesDistant` to retain coverage; `AspenLeaves` uses `AspenLeaves.png`. URP Lit, alpha clipping at 0.35, both faces rendered. Preserve texture alpha and enable mipmaps with alpha-coverage preservation where available. Do not use transparent blending for forest foliage.
- `WildflowerStem`, `WildflowerPetals`, and `WildflowerCenter` use the original FBX diffuse colors and no textures. Render stem leaves and petals on both faces. The petal color may be tinted for planted varieties.
- Reuse shared materials across instances. Texture color should not be multiplied by an unrelated foliage tint. Meshes have UV0 coordinates; no custom shader is required. Runtime wind would require separate shader work.

## Validation

Blender 5.2.1 generated and exported every mesh, saved the editable scene, and rendered previews successfully. Mesh triangle totals are recorded in `ArtSource/Woodland/mesh-stats.json`. The high-detail tree and flower renders were inspected, then pine branch regularity, the exposed leader, and aspen bark markings were improved. The all-LOD render exposed thin distant pine crowns, so medium sprays were enlarged and crossed; the distant pine received thicker alpha silhouettes and crossed sprays. `woodland-lod-sheet.png` places pine LOD0/1/2 followed by aspen LOD0/1/2 from left to right.

A Unity inspection caught the flower studio offset being exported. Flower exports now occur at the ground origin before studio placement; the generator asserts a zero export origin. All three corrected flower FBXs were reimported in Blender and verified at `[0, 0, 0]` with a 0.645 m height.

These Blender checks do not establish Unity import correctness or integrated-GPU frame rates. The main validation ledger records engine import, player builds, and hardware measurements separately.
