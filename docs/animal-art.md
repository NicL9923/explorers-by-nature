# Ranch animal art

Clover is a warm fawn Jersey cow with a continuous sculpted body, neck, face and legs. Separate details provide cupped ears, small curved horns, amber eyes with eyelids, a dark nose pad, nostrils, a pale muzzle surround, cloven hooves, udder and tail switch. The hen has a continuous breast, neck and head, overlapping wing and neck feathers, a dark tail fan, comb, wattles, beak, scaled shanks and clawed toes.

The [contact sheet](../ArtSource/animal-contact-sheet.jpg) shows each near mesh from three angles and its distance mesh.

These are original stylized models generated for this repository. They use no downloaded meshes, textures or other third-party art. The coat atlas is baked from an original spatial color function. Redistribution follows the repository's license.

| Resource | Near triangles | Distance triangles | Width × length × height |
| --- | ---: | ---: | --- |
| Clover | 23,500 | 4,400 | 1.060 × 2.421 × 2.000 m |
| Hen | 11,500 | 2,496 | 0.480 × 0.914 × 0.923 m |

Both FBX files contain two meshes named `<animal>_LOD0` and `<animal>_LOD1`. Each mesh retains multiple material slots. Show only the selected LOD. The distance hen removes most tiny feather layers, scales and eye accents, uses simple feather silhouettes, and simplifies each anatomical part separately to preserve its body. Both animals now have shared LOD skeletons, with planted feet, articulated neck/head idles, and cow ear/tail motion; see [animal animation](animal-animation.md). The feet sit at the model origin's ground plane. Blender Z-up and head toward -Y export to Unity Y-up and forward.

Clover uses `JerseyCoat` with `Assets/Resources/Animals/JerseyCoat_BaseColor.png`, a 1024px sRGB atlas with mipmaps. Use white base tint when assigning that texture. Its imported fawn diffuse color is a fallback for a missing texture. Other material slots retain their imported colors. Use double-sided rendering for the hen feather materials `HenBuff`, `HenGold`, `HenUmber` and `HenTail`; LOD1 uses thin feather sheets. Eye materials have low roughness; hide and feathers have high roughness. The exact palette is in the generator, and exported slot names and triangle counts are in [animal-model-stats.json](../ArtSource/animal-model-stats.json).

Run from the repository root:

```bash
blender -b -t 4 --python .agents/tools/make-ranch-art.py
```

Append `-- Clover` or `-- Hen` to regenerate one animal. Then run `blender -b -t 4 --python .agents/tools/rig-animals.py` to restore the animation rigs on all animal exports.

The script replaces the FBXs, packed editable [Clover.blend](../ArtSource/Clover.blend) and [Hen.blend](../ArtSource/Hen.blend), coat atlas, budget report and four studio views per animal. The blends retain both LOD meshes and a lit portrait studio; cameras, lights and floor are excluded from FBX. LOD1 is hidden in each saved studio.

Fedora's current Blender package ships an OCIO 2.5 configuration with a 2.4 library. If this mismatch disables color management, the generator restarts Blender using a temporary copy with the compatible version declaration and absolute lookup-table paths. It leaves installed files untouched. Correct color management is required for the coat's sRGB bake.

Validation for this asset revision includes complete Blender generation, recorded triangle counts, and visual inspection of portrait, profile, rear and distance views. Blender FBX re-import also verified mesh names, material slots and UV data. Importers can discard a few degenerate triangles, so imported counts may be slightly below these source budgets. Unity import, runtime appearance and device performance are tracked in the project's main validation ledger by the integration task. The studio renders do not establish game performance.
