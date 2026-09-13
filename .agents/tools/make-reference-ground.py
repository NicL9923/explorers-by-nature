"""Convert downloaded CC0 Poly Haven ground scans to meter-scale Unity LOD props.
Run blender -b --python .agents/tools/make-reference-ground.py.
Originals and exact download hashes: docs/reference-ground-assets.json.
"""
import bpy,json,pathlib
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
SOURCE=ROOT/'ArtSource/ReferenceGround'; OUT=ROOT/'Assets/Resources/ReferenceGround'
stats=[]
for asset,material in [('fern_02','Fern'),('tree_stump_01','Stump'),('rock_moss_set_01','Rock')]:
 bpy.ops.wm.open_mainfile(filepath=str(SOURCE/asset/(asset+'_2k.blend')))
 original=sorted([o for o in bpy.data.objects if o.type=='MESH'],key=lambda o:o.name)
 for idx,source in enumerate(original):
  name=('Fern'+('' if idx==1 else 'ACD'[idx if idx<1 else idx-1])) if material=='Fern' else ('Rock'+chr(65+idx) if material=='Rock' else 'Stump')
  for lod,budget in enumerate(([8000,1100,350] if material=='Fern' else [18000,4500,800])):
   bpy.ops.object.select_all(action='DESELECT');o=source.copy();o.data=source.data.copy();bpy.context.collection.objects.link(o);o.hide_set(False);o.select_set(True);bpy.context.view_layer.objects.active=o
   bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
   verts=o.data.vertices
   center=Vector(((min(v.co.x for v in verts)+max(v.co.x for v in verts))/2,(min(v.co.y for v in verts)+max(v.co.y for v in verts))/2,min(v.co.z for v in verts)))
   for v in verts:v.co-=center
   o.location=(0,0,0);o.name=name+'_LOD'+str(lod)
   o.data.calc_loop_triangles();tris=len(o.data.loop_triangles)
   if tris>budget:
    mod=o.modifiers.new('Budget','DECIMATE');mod.ratio=budget/tris;bpy.ops.object.modifier_apply(modifier=mod.name)
   o.data.materials.clear();mat=bpy.data.materials.get(material) or bpy.data.materials.new(material);o.data.materials.append(mat)
   o.data.calc_loop_triangles();bpy.context.view_layer.update()
   stats.append({'name':o.name,'triangles':len(o.data.loop_triangles),'dimensions_blender_xyz_m':list(o.dimensions),'material':material})
   bpy.ops.export_scene.fbx(filepath=str(OUT/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,path_mode='STRIP',use_mesh_modifiers=True)
   bpy.data.objects.remove(o,do_unlink=True)
(SOURCE/'mesh-stats.json').write_text(json.dumps(stats,indent=2)+'\n')
print(json.dumps(stats,indent=2))
