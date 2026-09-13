# Frontier tool art

Original procedural geometry and original wood, steel and leather textures authored for this repository by `.agents/tools/make-frontier-tools.py`. No third-party assets are included. Redistribution follows the repository license. Run with Python to regenerate the editable `FrontierTools.blend`, contact sheet and Unity FBX resources.

The muzzleloader is an illustrative frontier percussion long gun with walnut stock, brass furniture, metal barrel, sights, trigger guard and a ramrod. It is a game prop, not an engineering drawing or historical replica. `Hammer` and `Ramrod` are independently movable pivots. The grip is at the origin, the barrel points Unity +Z, and its muzzle is at approximately `(0, .087, .878)` metres.

The axe and pickaxe each have one rigid mesh, a shaped wooden handle and steel head. Their grip is at the origin; the shaft rises along Unity +Y with its head approximately .6 metres above the grip. Their cutting direction is Unity +Z.

`HandL` and `HandR` are separate relaxed gripping hands with canvas cuffs. Their wrists are at the origin, fingers extend Unity +Z, and their curled fingers point toward Unity -Y. The `FrontierSkin` material can be recolored to match the selected explorer. These are rigid first-person hand meshes, not finger rigs.

The contact sheet is a Blender studio check. Unity appearance and animation require separate in-game validation. Mesh triangle counts are recorded in `Assets/Resources/Tools/mesh-budget.json`.
