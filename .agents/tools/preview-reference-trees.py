"""Blender: rebuild compact editable tree scene and /tmp/reference-trees-preview.png."""
import bpy,pathlib,math
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2];bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for name in ['Pine','Fir']:
 bpy.ops.import_scene.fbx(filepath=str(root/'Assets/Resources/ReferenceTrees'/(name+'_LOD0.fbx')))
 for o in bpy.context.selected_objects:
  if o.type!='MESH':continue
  o.location.x+=-5 if name=='Pine' else 5
  for m in o.data.materials:
   m.use_nodes=True;n=m.node_tree.nodes;n.clear();out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled');m.node_tree.links.new(bs.outputs['BSDF'],out.inputs[0]);bs.inputs['Roughness'].default_value=.85
   key=m.name.split('.')[0].replace('_dead_branches','_bark');path=root/'Assets/Resources/ReferenceTrees'/(key+'_BaseColor.png')
   if path.exists():
    tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path),check_existing=True);m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if key.endswith('_twig'):m.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha'])
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource/Reference/ReferenceTrees.blend'),compress=True)
bpy.ops.object.camera_add(location=(27,-40,18));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,10))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=28;bpy.context.scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(-10,-12,24));bpy.context.object.data.energy=4500;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=15
bpy.context.scene.world.color=(.35,.35,.35);scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.filepath='/tmp/reference-trees-preview.png';bpy.ops.render.render(write_still=True)
