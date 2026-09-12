# Red fox art

The fox is an original procedural Blender sculpt created for Explorers by Nature. Its body, muzzle, legs and curved tail are voxel-unified and smoothed. Separate meshes provide inset ear interiors, eyes, nose and a fine mouth crease before joining each LOD. No downloaded models, external textures or generative image assets were used.

Regenerate from the repository root with:

```sh
blender -b -t 4 --python .agents/tools/make-fox-art.py
```

The script includes the Fedora Blender OCIO compatibility retry used by the wildlife generator. It writes editable `ArtSource/Fox/Fox.blend`, two studio previews and `mesh-stats.json`. `Assets/Resources/Fox/Fox.fbx` contains `Fox_LOD0` and `Fox_LOD1`; `FoxCoat_BaseColor.png` is the original 2048-pixel baked coat. The blend also packs that texture.

Coordinates use meters, a ground-level origin and Unity +Z facing through the FBX axis conversion. The sculpt is approximately 0.40 m wide, 0.83 m tall and 1.64 m long including the tail. Exact triangle counts and dimensions are in `ArtSource/Fox/mesh-stats.json`; the generator caps high detail at 17,900 triangles and targets 2,850 triangles for distance detail.

The studio floor, camera and lights are excluded from FBX export. Material diffuse colors and Principled base colors match; the coat multiplies its texture by white. Unity should use `FoxCoat_BaseColor.png` with the `FoxCoat` material and preserve the imported colors for the ear, eye and nose materials.

Validation covers successful Blender generation, FBX export and visual inspection of the portrait and side renders. Unity import and gameplay validation belong to the integration pass. The fox is a static mesh without a skeleton or animation clips.
