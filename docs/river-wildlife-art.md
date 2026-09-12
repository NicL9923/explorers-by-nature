# River wildlife art

The mallard drake and North American beaver are original procedural Blender assets authored for Explorers by Nature. They use no third-party geometry, images, or textures. Their editable source and generator may be redistributed under the repository's license.

Regenerate from the repository root:

```sh
blender -b -t 4 --python .agents/tools/make-river-wildlife.py
```

The script exports `Assets/Resources/RiverWildlife/Duck.fbx` and `Beaver.fbx`, saves `ArtSource/RiverWildlife/RiverWildlife.blend`, renders two studio portraits, and records geometry counts and dimensions in `ArtSource/RiverWildlife/mesh-stats.json`. It handles the Fedora Blender/OCIO profile mismatch using a temporary compatible configuration.

| Model | LOD0 triangles | LOD1 triangles | Width × height × length, meters |
| --- | ---: | ---: | --- |
| Mallard | 11,442 | 2,350 | 0.326 × 0.434 × 0.740 |
| Beaver | 13,800 | 2,350 | 0.538 × 0.519 × 1.239 |

Each FBX has named `Duck_LOD0`/`Duck_LOD1` or `Beaver_LOD0`/`Beaver_LOD1` meshes. Origins are at underside height, with identity exported object transforms. Blender -Y maps to Unity +Z with the existing FBX import convention. Place a swimming duck's root 0.075 meters below water height to hide its feet and lower hull. The beaver rests on dry ground at root height.

Mallard anatomy uses a continuous hull, chest, neck and head sculpt, a flattened bill with nostrils, eyes with highlights, layered folded feathers, blue speculum patches and a drake's curled tail. Beaver anatomy merges a pear-shaped torso, skull, hips and forearms; separate detail geometry adds ears, cheeks, incisors, whiskers, fingers and a broad paddle tail with scale ridges.

Materials store matching diffuse and Principled base colors. The Unity material remapper must preserve these colors when replacing imported materials with URP Lit. `MallardPlumage_BaseColor.png` is an original baked plumage map stored beside the FBX. Bind it to the `MallardPlumage` material, whose base tint is white. This keeps the collar and feather color boundaries smooth across both LODs. Wings and limbs are static within each LOD mesh; locomotion is supplied by the scene's existing whole-model behavior.

Validation: Blender generation and FBX export completed; both studio renders were visually inspected. Initial hard duck breast intersections and beaver faceting were corrected in the generator. Triangle budgets meet the 14,000/2,500 limits. Unity import and gameplay validation belong to the scene integration pass; these asset checks do not establish runtime performance.
