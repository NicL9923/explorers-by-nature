"""Round-trip the frontier FBX assets. blender -b --python-exit-code 1 --python this-file.py."""
import bpy,pathlib,json,hashlib
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2];results={}
for folder,names in [('Players',['RanchHand','TrailScout','Homesteader','Frontiersman']),('Tools',['Muzzleloader','Axe','Pickaxe','HandL','HandR'])]:
 for name in names:
  path=ROOT/'Assets/Resources'/folder/(name+'.fbx');assert path.exists(),path
  bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(path));bpy.context.view_layer.update()
  objects=list(bpy.context.scene.objects);meshes=[o for o in objects if o.type=='MESH'];object_names={o.name for o in objects}
  if folder=='Players':
   required={'Hips','Torso','Head','ArmL','ArmR','LegL','LegR','KneeL','KneeR'};assert required<=object_names,(name,object_names);assert len(meshes)==(12 if name=='Homesteader' else 9)
   for side in ['L','R']:assert bpy.data.objects['Knee'+side].parent.name=='Leg'+side
  if name=='Homesteader':
   assert bpy.data.objects['StandingSkirt'].parent.name=='Torso'
   for side in ['L','R']:assert bpy.data.objects['RidingSkirt'+side].parent.name=='Leg'+side
  if name=='Muzzleloader':assert {'Hammer','Ramrod'}<=object_names
  points=[o.matrix_world@Vector(corner) for o in meshes for corner in o.bound_box];lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)];size=[b-a for a,b in zip(lo,hi)]
  if folder=='Players':assert 1.80<size[2]<1.9 and .6<size[0]<.8,(name,size)
  elif name.startswith('Hand'):assert .14<size[1]<.22,(name,size)
  elif name=='Muzzleloader':assert 1.2<size[1]<1.4,(name,size)
  else:assert .75<size[2]<1,(name,size)
  assert all(o.data.uv_layers for o in meshes),(name,'Missing UVs')
  results[name]={'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'triangles':sum(len(o.data.polygons) for o in meshes),'meshes':len(meshes),'blender_bounds_min':lo,'blender_bounds_max':hi,'uv_complete':True}
(ROOT/'ArtSource/frontier-export-validation.json').write_text(json.dumps(results,indent=2)+'\n');print('Validated all nine frontier FBX assets.')
