# Reference trees

`ReferenceTrees.blend` is a compact editable scene containing the optimized near pine and fir. The source tree meshes and 2K image URLs, source hashes, redistribution license, final asset hashes, and triangle counts are recorded in `docs/reference-trees.json`. Original downloads remain outside Git in `raw/`.

Rebuild from the repository root:

```bash
python .agents/tools/fetch-reference-trees.py
python .agents/tools/rebuild-reference-trees.py
python .agents/tools/prepare-reference-tree-textures.py
blender -b -P .agents/tools/preview-reference-trees.py
```

Pine is 20.4m tall with an approximately 8m open crown; fir is 19m tall with an approximately 6.6m denser crown. Unity assets use meters and a ground-level origin. Authored source card geometry and branch placement supply irregular silhouettes. Near/middle/far meshes retain about 80k/25k/8.5k pine triangles and 69k/25k/10.5k fir triangles. Distant card islands impose a practical simplification floor; these are budgets for this focused scene, not proven integrated-GPU performance.

The fir source's `UVMap` is a float3 corner attribute, which FBX normally drops. The importer explicitly converts it to a loop UV layer and bakes source bark texture scaling. Needle transparency is packed into color alpha; Unity imports preserve coverage in mipmaps. Bark and twig normals are OpenGL tangent-space maps. The near tree preview was inspected for silhouette and texture coordinates, but Blender on this machine reports an OCIO version mismatch, so its preview is not color-validation evidence. Unity scene appearance and performance remain separate checks.

Pine twig color uses the official JPEG albedo with the standalone opacity map. The PNG has invalid white RGB beneath embedded transparency, which is unsafe to expose when replacing its alpha. The runtime applies a restrained green tint only to pine needles because the native pale albedo reads chalky with direct URP Lit shading. Source geometry and source texture colors remain available unchanged in `raw/`.

Trunk materials require a bake. Poly Haven blends the scanned base atlas with object-space triplanar bark using the `Col` vertex mask. `bake-reference-trunks.py` preserves this blend in a new 2K color and tangent normal atlas, copies baked coordinates into UV0, and preserves other material UVs. The texture preparation tool deliberately skips baked trunk files. The rebuild wrapper supplies a temporary compatible color configuration for Fedora Blender; it does not modify the machine installation.
