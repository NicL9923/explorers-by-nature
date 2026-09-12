# Woodland wildlife art

`Deer.fbx` and `Rabbit.fbx` in `Assets/Resources/Wildlife` are original procedural sculptures. Deer has a narrow muzzle, pointed ears, branched antlers, long articulated leg shapes and split hooves. Rabbit has a crouched body, hind haunches, long feet, cupped ears, whiskers and a pale cottontail. Bodies are unified voxel-remeshed surfaces, relaxed and decimated before export. No third-party meshes, textures, brushes or images are used.

Regenerate with:

```bash
blender -b -t 4 --python .agents/tools/make-wildlife-art.py
```

The script works around this Fedora Blender package's OCIO version mismatch by making a temporary compatible configuration with absolute LUT paths and relaunching Blender. It does not change installed files.

Editable source, portrait renders and measured triangle counts are in `ArtSource/Wildlife`. `Wildlife.blend` includes both animals, their distance meshes and a lit portrait studio. The rabbit is hidden for the saved deer portrait view.

| Asset | Near triangles | Distant triangles |
| --- | ---: | ---: |
| Deer | 14,108 | 2,600 |
| Rabbit | 7,500 | 1,600 |

## Integration

Each FBX contains exactly two joined meshes, `Deer_LOD0`/`Deer_LOD1` or `Rabbit_LOD0`/`Rabbit_LOD1`. Both occupy the same origin and must be assigned to separate LOD levels, not rendered together. Root origins sit at the ground. Blender forward is -Y, with -Z forward/Y up FBX export. Deer shoulder height is approximately 1.2 m, its head reaches 1.75 m, and its antlers reach 2.13 m. Rabbit ear height is approximately 0.69 m. Runtime can normalize their overall bounds to match existing roaming placements.

`DeerCoat` and `RabbitCoat` use corresponding 1024 × 1024 PNGs. The original coat colors are baked to UV0, with pale undersides and fine texture grain. Use the texture at a white material tint. `WildlifeDark`, `WildlifeEar`, `WildlifeCream`, `DeerHoof`, and `DeerAntler` have untextured FBX diffuse colors. All surfaces are opaque; the thickened ear meshes require no alpha clipping. The dark eye/nose material can use greater smoothness than the coat. Materials are shared within each export.

These are static body poses. The runtime supplies roaming motion; articulated gait animation is outside this asset pass. Antlers are part of the joined deer mesh.

## Validation

Blender generated and exported both FBX files, saved the editable source, baked the coat textures and rendered both portraits. Initial portrait inspection caught twisted lower-leg tube frames and excessive coat mottling. Stable tube frames, smoother sculpt inputs and texture-scale coat grain replaced those defects; updated portraits were inspected. A final hoof correction replaced round shapes with tapered cloven hooves, flat soles at ground level, rough horn material and tops overlapping the shins. The final deer portrait was inspected again. Measured counts are in `mesh-stats.json`.

Unity import, runtime orientation, LOD transitions and hardware performance require engine validation in the main ledger. Blender renders do not establish in-game visual or frame-time results.
