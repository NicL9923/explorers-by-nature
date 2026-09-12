"""Original ranch animals, exported reproducibly through Blender's background CLI."""
import bpy, math, pathlib
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
colors={'Ivory':(.83,.76,.59,1),'Patch':(.075,.045,.023,1),'Hoof':(.04,.03,.023,1),'Nose':(.57,.28,.24,1),'Comb':(.65,.045,.025,1),'Beak':(.75,.4,.055,1)}
mats={}
for name,color in colors.items():
 m=bpy.data.materials.new(name);m.diffuse_color=color;mats[name]=m

def ell(name,pos,scale,mat):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,location=pos)
 o=bpy.context.object;o.name=name;o.scale=scale;o.data.materials.append(mats[mat]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for p in o.data.polygons:p.use_smooth=True
 return o

def bone(name,a,b,r,mat):
 mid=(Vector(a)+Vector(b))/2;length=(Vector(b)-Vector(a)).length
 bpy.ops.mesh.primitive_cone_add(vertices=10,radius1=r,radius2=r*.75,depth=length,location=mid)
 o=bpy.context.object;o.name=name;o.rotation_euler=(Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler();o.data.materials.append(mats[mat]);return o

def export(name):
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.export_scene.fbx(filepath=str(root/'Assets/Resources'/f'{name}.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
 bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource'/f'{name}.blend'))
 bpy.ops.object.delete(use_global=False)
# Blender Z-up, head towards -Y, exported as Unity forward.
ell('Ivory body',(0,0,1.1),(.47,.85,.51),'Ivory')
ell('Shoulders',(0,-.54,1.3),(.4,.36,.48),'Ivory')
ell('Head',(0,-.94,1.52),(.27,.38,.32),'Ivory')
ell('Nose',(0,-1.22,1.37),(.29,.18,.18),'Nose')
for sign in [-1,1]:
 ell('Ear',(sign*.36,-.83,1.72),(.21,.11,.085),'Ivory')
 ell('Ear inside',(sign*.38,-.87,1.74),(.14,.075,.035),'Nose')
 ell('Eye',(sign*.235,-1.09,1.62),(.045,.046,.055),'Hoof')
 ell('Nostril',(sign*.13,-1.38,1.4),(.045,.025,.03),'Patch')
 bone('Horn',(sign*.17,-.79,1.78),(sign*.24,-.75,1.98),.065,'Ivory')
 for y in [-.53,.52]:
  bone('Leg',(sign*.3,y,1),(sign*.32,y,.16),.1,'Ivory')
  ell('Hoof',(sign*.32,y-.025,.12),(.115,.14,.12),'Hoof')
 ell('Brown shoulder patch',(sign*.415,-.35,1.2),(.065,.3,.29),'Patch')
 ell('Brown flank patch',(sign*.43,.38,1.12),(.066,.26,.32),'Patch')
ell('Udder',(0,.27,.63),(.23,.3,.17),'Nose')
for x in [-.1,.1]:
 for y in [.14,.35]:bone('Teat',(x,y,.6),(x,y,.46),.045,'Nose')
bone('Tail',(0,.77,1.35),(.1,.91,.5),.036,'Ivory');ell('Tail tuft',(.1,.92,.44),(.065,.065,.15),'Patch')
export('Clover')
ell('Hen body',(0,0,.4),(.24,.32,.26),'Ivory')
ell('Neck',(0,-.22,.62),(.135,.16,.23),'Ivory');ell('Head',(0,-.28,.79),(.15,.15,.16),'Ivory')
for sign in [-1,1]:
 ell('Wing',(sign*.22,.015,.45),(.055,.24,.16),'Patch');ell('Eye',(sign*.13,-.34,.83),(.022,.025,.025),'Hoof')
 bone('Leg',(sign*.1,.015,.26),(sign*.1,-.02,.07),.025,'Beak')
 for t in [-1,0,1]:bone('Toe',(sign*.1,-.02,.06),(sign*.1+t*.06,-.15,.035),.014,'Beak')
for i in range(3):ell('Comb',(0,-.25+i*.07,.94),(.045,.055,.07),'Comb')
bone('Beak',(0,-.38,.8),(0,-.53,.77),.065,'Beak');ell('Wattle',(0,-.36,.68),(.045,.045,.075),'Comb')
for i in range(3):
 o=ell('Tail',(0,.27+i*.05,.57+i*.06),(.065,.2,.07),'Patch');o.rotation_euler.x=.65
export('Hen')
