# Articulated animal motion

All seven original animals now use Blender-authored skin weights and compact skeletons. Both imported LOD meshes share the same skeleton, use the detailed color and normal atlases, and retain named material slots. The Unity controller moves bones; it does not rebuild mesh vertices every frame.

- Deer and foxes walk with alternating stance and swing phases, with two-joint leg solving and level hooves/paws. Gait phase follows measured travel distance. Deer stop long enough to lower their necks and browse.
- Rabbits alternate short travel bursts with still observation periods, with paired fore/hind leg motion and small hops. Their ears turn independently while resting.
- Mallards paddle their webbed feet beneath the water and turn their heads. Beavers move their forepaws toward their faces during quiet grooming intervals.
- Clover lowers her neck, moves her head, flicks her ears and swishes her tail while her hooves remain planted. The hen has articulated neck/head pecking with stationary feet.
- Swallows have swept wing silhouettes with shoulder and wingtip hinges, alternating flapping and gliding.

`AnimalMotion` attaches through `ModelArt` for the seven resource names. Local scenery controllers still choose animal positions; none of these poses changes shared ranch authority or animal production. Animation runs after locomotion, skips bone updates when no renderer is visible, allocates its bone/limb lookup once, and uses at most 18 bones per animal. Those choices are budget controls, not a measured hardware performance claim.

## Regeneration

Run the relevant original art generator first when changing anatomy, then bake the detailed surfaces and regenerate every rig:

```bash
blender -b -t 4 --python .agents/tools/detail-animal-art.py
blender -b -t 4 --python .agents/tools/rig-animals.py
blender -b -t 4 --python .agents/tools/check-animal-rigs.py
```

The detail command preserves named material slots and bakes a full-surface 2048px color atlas and 1024px tangent normal atlas per animal. Both LODs share the atlas. The hen retains its authored feather-sheet distance mesh with a reserved quarter of its shared atlas; the other distance meshes use bounded decimation. It saves isolated, editable studios in `ArtSource/Detail`; the original generator studios remain available. All texture patterns are original procedural work.

The rig command reads the detailed studios when present, otherwise the original editable sculpts, applies anatomical region weights, exports the seven FBXs, and saves compressed rigged studios under `ArtSource/Animation`. It caps and normalizes each vertex to four influences. Horns and antlers inherit the head rigidly. FBX armatures retain the export coordinate transform because Blender's baked-space conversion does not support armatures.

The final command checks every source vertex's weight sum and renders head/neck/ear/tail poses. [Deer pose](../ArtSource/Animation/Deer-pose.png), [cow pose](../ArtSource/Animation/Clover-pose.png), and [rabbit pose](../ArtSource/Animation/Rabbit-pose.png) demonstrate deformation independently of game captures. They do not verify runtime walking or frame time. Assets are original repository work with the same provenance as the source sculpts.

## Validation and limits

Blender generation and normalized-weight checks passed for all seven animals. Pose inspection caught and corrected antler stretching and inconsistent muzzle weights. The Unity tests cover imported head/leg skinning on both LODs, constant-speed stance compensation, swing clearance and smooth idle envelopes. The integration task owns running Unity tests, actual game captures and the exact-state validation ledger in [first milestone](first-milestone.md).

These are procedural prototype performances, not motion-captured animation. Feet use a compact two-joint solve against each animal's local ground plane; uneven terrain beneath individual feet is not sampled. Foot locking is approximate during acceleration and turns. Rabbit jumps and duck float height remain controlled by their scenery actors. Keep future anatomy changes synchronized with the explicit joint coordinates and masks in the rig generator.
