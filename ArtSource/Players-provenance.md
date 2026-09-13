# Frontier player art

Four original models authored procedurally for Explorers by Nature: Ranch Hand, Trail Scout, Homesteader and Frontiersman. Geometry, clothing details, palette and fabric/leather textures come entirely from `.agents/tools/make-player-art.py`. No third-party meshes, images, scans or reference artwork are embedded. Redistribution follows this repository's license.

Run `python3 .agents/tools/make-player-art.py` from the repository to regenerate `Players.blend`, `Players-contact-sheet.png`, the four FBX files and eight original material color maps. The wrapper supplies a temporary compatible color configuration for Fedora's Blender/OpenColorIO package mismatch without changing system files.

Characters have adult proportions and a stylized finish. They face Unity +Z, stand with their soles at ground level and reach approximately 1.84 metres including hats. Named rigid pivots support simple walking: `Hips`, `Torso`, `Head`, `ArmL`, `ArmR`, `LegL`, `LegR`, `KneeL`, `KneeR`. They are not skinned humanoid rigs; elbows and fingers do not articulate individually. The Homesteader uses `StandingSkirt` under the torso while walking. Mounted play hides that long skirt and shows `RidingSkirtL` and `RidingSkirtR`, knee-length split drapes parented to the thighs. The studio source defaults to the standing appearance; runtime explicitly toggles the three variant parents.

The editable source keeps four characters side by side for inspection. Exports place each character independently at the origin. `Assets/Resources/Players/mesh-budget.json` records exported triangle counts. There are nine mesh renderers per model, plus three wardrobe variant renderers for the Homesteader, with multiple material slots and no separately authored distance LODs.
