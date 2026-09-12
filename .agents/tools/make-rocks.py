"""Generate original river stones with Blender's CLI; export FBX for Unity.

Run from the repository root:
blender --background --factory-startup --python .agents/tools/make-rocks.py
"""
from pathlib import Path
import math
import random
import bpy

root = Path(__file__).resolve().parents[2]
source = root / 'ArtSource'
output = root / 'Assets/Models'
source.mkdir(exist_ok=True)
output.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

for index in range(3):
    rng = random.Random(470 + index)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=1)
    rock = bpy.context.object
    rock.name = f'RiverStone{index + 1}'
    phase = rng.random() * math.tau
    for vertex in rock.data.vertices:
        p = vertex.co
        deformation = 1 + .12 * math.sin(p.x * 4 + phase) * math.cos(p.y * 3) + .06 * math.sin(p.z * 7 + phase)
        p.x *= deformation
        p.y *= deformation * .8
        p.z = max(-.52, p.z * .65 * deformation)
    for face in rock.data.polygons:
        face.use_smooth = True
    # UV coordinates allow the CC0 rock texture to follow every stone.
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=.03)
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.export_scene.fbx(filepath=str(output / f'{rock.name}.fbx'), use_selection=True,
                             object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, bake_anim=False, add_leaf_bones=False)
    rock.location.x = index * 3
    rock.select_set(False)

bpy.ops.wm.save_as_mainfile(filepath=str(source / 'RiverStones.blend'))
print('RIVER_STONES_EXPORTED', output)
