from pathlib import Path
import bpy,math,json,os,tempfile
from mathutils import Vector
# Fedora's Blender/OCIO package versions currently differ; isolate compatibility
# to a temporary configuration, as the original art generators do.
try:bpy.context.scene.view_settings.view_transform='AgX'
except TypeError:
 config=Path('/usr/share/blender/5.2/datafiles/colormanagement/config.ocio')
 if config.exists() and not os.environ.get('MOTION_OCIO_RETRY'):
  text=config.read_text().replace('ocio_profile_version: 2.5','ocio_profile_version: 2.4').replace('search_path: "icc:luts:filmic"',f'search_path: "{config.parent}/icc:{config.parent}/luts:{config.parent}/filmic"')
  fd,path=tempfile.mkstemp(prefix='motion-ocio-',suffix='.ocio')
  with os.fdopen(fd,'w') as f:f.write(text)
  os.environ['OCIO']=path;os.environ['MOTION_OCIO_RETRY']='1'
  os.execv(bpy.app.binary_path,[bpy.app.binary_path,'-b','-t','4','--python',str(Path(__file__).resolve())])
 else:raise RuntimeError('Working color management is required for previews.')
root=Path(__file__).resolve().parents[2];report={}
for name in ['Deer','Clover','Fox','Beaver','Rabbit','Duck','Hen']:
 bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Animation'/f'{name}.blend'))
 arm=bpy.data.objects[name+'_MotionRig'];meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(name+'_LOD')]
 # Restrict preview to the rigged near mesh; source studios can contain another species.
 for o in bpy.context.scene.objects:
  if o.type=='MESH' and o.name.endswith(('_LOD0','_LOD1')):o.hide_render=o.name!=name+'_LOD0'
 for o in meshes:
  for v in o.data.vertices:assert abs(sum(g.weight for g in v.groups)-1)<1e-5
 p=arm.pose.bones['Neck'];p.rotation_mode='XYZ';p.rotation_euler.x=math.radians(75 if name=='Deer' else 60 if name=='Clover' else 12)
 arm.pose.bones['Head'].rotation_mode='XYZ';arm.pose.bones['Head'].rotation_euler.x=math.radians(18)
 if 'EarL' in arm.pose.bones:arm.pose.bones['EarL'].rotation_mode='XYZ';arm.pose.bones['EarL'].rotation_euler.y=.2
 arm.pose.bones['Tail'].rotation_mode='XYZ';arm.pose.bones['Tail'].rotation_euler.z=.17
 # Resolve original texture images after relocating the editable rig studio.
 for im in bpy.data.images:
  filename=Path(im.filepath).name if im.filepath else im.name+'.png'
  matches=list((root/'Assets/Resources').rglob(filename))
  if matches:im.source='FILE';im.filepath=str(matches[0]);im.reload()
 sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=8;sc.render.resolution_x=700;sc.render.resolution_y=650;sc.render.resolution_percentage=100
 try:sc.view_settings.view_transform='AgX'
 except:pass
 high=bpy.data.objects[name+'_LOD0'];bounds=[high.matrix_world@Vector(v) for v in high.bound_box];center=sum(bounds,Vector())/8;size=max(high.dimensions)
 cam=sc.camera;cam.location=center+Vector((size*1.8,-size*2,size*.9));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=size*1.25
 sc.render.filepath=str(root/'ArtSource/Animation'/f'{name}-pose.png');bpy.ops.render.render(write_still=True)
 report[name]={'normalized_skin_weights':True,'bones':len(arm.data.bones)}
(root/'ArtSource/Animation/verification.json').write_text(json.dumps(report,indent=2)+'\n')
