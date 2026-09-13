# Adult white-tailed doe study

Original procedural Blender geometry and original baked 2048px coat. This is a separate anatomy study for the focused trail scene, not a replacement of the seven prototype animals.

Photographic references inspected on 2026-09-12:

- [NPS Big Thicket, female white-tailed deer](https://www.nps.gov/bith/learn/nature/white-tailed-deer.htm), photograph credited to NPS / Soren George-Nichol. [Direct photograph](https://www.nps.gov/bith/learn/nature/images/IMGP0434_crop.jpg). Used for adult barrel, withers, tucked flank, stifle/hock arrangement, forearm/cannon ratio and ear/head proportions.
- Same page, [NPS trail-camera photograph](https://www.nps.gov/bith/learn/nature/images/Kirby-Deer-4_crop.jpg). Used for the long tapering face, nose leather and lateral eyes. This photograph depicts a buck; antlers and heavy male neck were not carried into the doe.

Photographs are external visual references, not redistributed files or texture sources. All distributed mesh and texture data is generated originally in this repository. NPS [copyright guidance](https://www.nps.gov/media/photo/gallery-item.htm?gid=E171841D-4928-44B6-831A-6461189B97A9&id=9514103e-4383-42ca-b3fb-038ae69b5699) identifies NPS-credited photographs without a copyright symbol as public domain.

Regenerate from repository root:

```bash
blender -b -t 6 --python .agents/tools/make-reference-deer.py
```

`Deer.fbx` has `Deer_LOD0`, `Deer_LOD1` and the existing 18-bone `MotionRoot`, `Neck`, `Head`, ear, tail and limb naming contract. The default stance is planted. Large grazing or walking poses require further deformation review; the focused scene should use quiet idle motion.

Visual limitations: the long-legged adult silhouette is a study, not finished realistic wildlife art. The face, ear interiors and coat remain visibly procedural at close range. Do not use export success or the triangle budget as evidence that the photographic reference quality has been met.

To generate a candidate without touching imported Unity assets, set `REFERENCE_DEER_OUTPUT` to an absolute directory under `ArtSource/ReferenceDeer/Candidate`. The 2026-09-12 second study was inspected in this isolated output and then copied into Assets/Resources/ReferenceDeer. It includes smaller hoof geometry, broad muscle-junction smoothing and stronger baked directional coat grain. Deer.blend matches this shipped mesh. The isolated Candidate directory is ignored to avoid duplicate binaries.

Final inspected export hashes:

- `Deer.fbx`: `eb09602e3c7a81fb843d37b2338152bb42dc40d481d1ec5262a0ec28ed6c4502`
- `DeerCoat.png`: `4a47e54a836c0e1f12cbf44a5e51502bb4e837c265de034cd5835be0021af88c`
