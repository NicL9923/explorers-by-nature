"""Blender: blender -b ArtSource/Reference/raw/{id}/{id}_2k.blend -P .agents/tools/import-reference-trees.py
Preserves source UVs/materials. Uses authored card LOD2, not individual needle LOD0.
"""
import bpy, bmesh, pathlib,json,math,runpy
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2]
id=pathlib.Path(bpy.data.filepath).parent.name
name='Pine' if id.startswith('pine') else 'Fir'
source=bpy.data.objects[id+'_a_LOD2']; mesh=source.data.copy();matrix=source.matrix_world.copy();matrix.translation=Vector((0,0,0));mesh.transform(matrix)
# Poly Haven stores UVMap as a float3 CORNER attribute, not a Blender UV layer.
# FBX silently drops that attribute unless converted to a float2 loop UV map.
attribute=mesh.attributes.get('UVMap')
if attribute is not None and not mesh.uv_layers:
 coordinates=[tuple(d.vector)[:2] for d in attribute.data]
 mesh.attributes.remove(attribute)
 uv=mesh.uv_layers.new(name='UVMap')
 for polygon in mesh.polygons:
  material=mesh.materials[polygon.material_index]
  mapping=next((n for n in material.node_tree.nodes if n.type=='MAPPING'),None)
  scale=mapping.inputs['Scale'].default_value if mapping else (1,1,1)
  for i in polygon.loop_indices:uv.data[i].uv=(coordinates[i][0]*scale[0],coordinates[i][1]*scale[1])
minz=min(v.co.z for v in mesh.vertices)
for v in mesh.vertices:v.co.z-=minz
# Only the selected authored tree; source includes construction components and other variants.
for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
out=root/'Assets/Resources/ReferenceTrees'
mesh=runpy.run_path(str(root/'.agents/tools/bake-reference-trunks.py'))['bake_trunk'](mesh,name,out)
for m in mesh.materials:
 if m:
  suffix=m.name.removeprefix(id+'_');m.name=name+'_'+suffix
  m.use_nodes=False;m.diffuse_color=(1,1,1,1)
result=[];out=root/'Assets/Resources/ReferenceTrees'
for level,budget in enumerate([80000,25000,8000]):
 o=bpy.data.objects.new(name+'_LOD'+str(level),mesh.copy());bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True)
 # Dissolve coplanar interior subdivisions without crossing atlas seams.
 mod=o.modifiers.new('Preserve card outlines','DECIMATE');mod.decimate_type='DISSOLVE';mod.angle_limit=math.radians(4);mod.delimit={'UV','MATERIAL','NORMAL'}
 bpy.ops.object.modifier_apply(modifier=mod.name);o.data.calc_loop_triangles();before=len(o.data.loop_triangles)
 if before>budget:
  mod=o.modifiers.new('Budget','DECIMATE');mod.ratio=budget/before;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.calc_loop_triangles();count=len(o.data.loop_triangles)
 bpy.ops.export_scene.fbx(filepath=str(out/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,use_mesh_modifiers=True,path_mode='STRIP')
 result.append({'lod':level,'triangles':count,'dimensions_m':list(o.dimensions),'source':id+'_a_LOD2','after_planar_dissolve':before});print('RESULT',result[-1],flush=True)
 bpy.data.objects.remove(o,do_unlink=True)
(root/'ArtSource/Reference'/(name+'-export-report.json')).write_text(json.dumps(result,indent=2)+'\n')
