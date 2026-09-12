"""Skin existing original sculpts; run after any animal generator. No topology/material changes.
blender -b -t 4 --python .agents/tools/rig-animals.py
"""
from pathlib import Path
import bpy, math, json
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
# Blender anatomy: X lateral, -Y forward, Z height. All bones share world axes.
SPECS={
'Deer':('Wildlife/Wildlife.blend','Wildlife/Deer',(.0,-.38,1.04),(0,-.64,1.57),(.06,-.62,1.69),(0,.60,1.12),[(.15,-.33,.98,.58,-.38,.10,-.35),(.15,.42,1.,.39,.48,.10,.40)],.78),
'Rabbit':('Wildlife/Wildlife.blend','Wildlife/Rabbit',(0,-.06,.24),(0,-.16,.33),(.043,-.15,.41),(0,.31,.23),[(.065,-.10,.25,.12,-.12,.03,-.17),(.11,.15,.20,.085,.12,.043,.005)],.13),
'Fox':('Fox/Fox.blend','Fox/Fox',(0,-.19,.45),(0,-.32,.62),(.05,-.323,.661),(0,.33,.435),[(.073,-.20,.43,.26,-.222,.027,-.231),(.085,.22,.40,.136,.26,.025,.21)],.30),
'Beaver':('RiverWildlife/RiverWildlife.blend','RiverWildlife/Beaver',(0,-.18,.26),(0,-.27,.33),(.112,-.218,.416),(0,.29,.046),[(.16,-.16,.24,.13,-.23,.037,-.32),(.183,.15,.20,.09,.11,.029,.055)],.14),
'Duck':('RiverWildlife/RiverWildlife.blend','RiverWildlife/Duck',(0,-.175,.245),(0,-.22,.355),None,(0,.25,.18),[(.068,.035,.09,.045,.04,.006,-.045)],.075),
'Clover':('Clover.blend','Clover',(0,-.43,1.15),(0,-.84,1.58),(.20,-.91,1.72),(0,.83,1.38),[(.29,-.44,1.05,.48,-.46,.07,-.45),(.29,.55,1.04,.49,.65,.07,.61)],.85),
'Hen':('Hen.blend','Hen',(0,-.16,.46),(0,-.23,.73),None,(0,.225,.44),[(.09,.015,.28,.14,-.005,.045,-.025)],.26),
}
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def rig(name,spec):
 src,path,neck,head,ear,tail,legs,legtop=spec
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource'/src))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name in [name+'_LOD0',name+'_LOD1']]
 assert len(meshes)==2,(name,[o.name for o in meshes])
 for o in meshes:o.hide_set(False);o.hide_render=False
 bpy.ops.object.select_all(action='DESELECT');bpy.ops.object.armature_add();arm=bpy.context.object;arm.name=name+'_MotionRig'
 bpy.ops.object.mode_set(mode='EDIT');arm.data.edit_bones.remove(arm.data.edit_bones[0]);bones={}
 def bone(n,p,parent=None):
  b=arm.data.edit_bones.new(n);b.head=p;b.tail=Vector(p)+Vector((0,.10,0));
  if parent:b.parent=bones[parent]
  bones[n]=b
 bone('MotionRoot',(0,0,0));bone('Neck',neck,'MotionRoot');bone('Head',head,'Neck');bone('Tail',tail,'MotionRoot')
 if ear:
  for s,side in [(-1,'L'),(1,'R')]:bone('Ear'+side,(s*ear[0],ear[1],ear[2]),'Head')
 for i,leg in enumerate(legs):
  x,y,z,kz,ky,fz,fy=leg
  for s,side in [(-1,'L'),(1,'R')]:
   prefix=('Front' if i==0 else 'Hind')+side
   bone(prefix+'Upper',(s*x,y,z),'MotionRoot');bone(prefix+'Lower',(s*x,ky,kz),prefix+'Upper');bone(prefix+'Foot',(s*x,fy,fz),prefix+'Lower')
 bpy.ops.object.mode_set(mode='OBJECT')
 # Smooth region masks preserve the sculpt's continuous joints. Tiny facial details
 # inherit Head, and antlers stay on Head rather than accidentally flapping with ears.
 for ob in meshes:
  bpy.context.view_layer.objects.active=ob;ob.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);ob.select_set(False)
  groups={n:ob.vertex_groups.new(name=n) for n in bones}
  rigid_head=set()
  for face in ob.data.polygons:
   material=ob.data.materials[face.material_index].name
   if material.startswith(("DeerAntler","HornIvory")):rigid_head.update(face.vertices)
  for v in ob.data.vertices:
   x,y,z=v.co;weights={};remaining=1.
   if v.index in rigid_head:
    groups["Head"].add([v.index],1.,"REPLACE");continue
   def assign(n,w):
    nonlocal remaining
    w=max(0,min(1,w))*remaining
    if w>0:weights[n]=weights.get(n,0)+w
    remaining-=w
   # Tails have species-specific masks to avoid pulling the hindquarters.
   tw=smooth(tail[1]-.02,tail[1]+(.11 if name!='Clover' else .09),y)
   if name=='Clover':tw*=1-smooth(.07,.16,abs(x-.06))
   if name=='Beaver':tw*=1-smooth(.08,.15,z)
   assign('Tail',tw)
   if ear:
    ew=smooth(ear[2]-.015,ear[2]+.045,z)
    if name=='Clover':ew*=smooth(.22,.29,abs(x))
    elif name in ['Rabbit','Fox']:ew*=smooth(.018,.042,abs(x))
    elif name=='Deer':ew*=smooth(.11,.17,abs(x))*(1-smooth(-.48,-.38,y))
    assign('EarL' if x<0 else 'EarR',ew)
   headmask=(1-smooth(head[1]-.03,neck[1]+.08,y))*smooth(neck[2]-.12,head[2]-.03,z)
   if name in ['Clover','Hen','Duck']:headmask=(1-smooth(head[1]-.04,neck[1]+.06,y))*smooth(neck[2]-.25,head[2]-.08,z)
   headmask=max(headmask,(1-smooth(head[1]-.08,head[1]+.025,y))*smooth(neck[2]-.25,neck[2]-.10,z))
   assign('Head',headmask)
   assign('Neck',(1-smooth(neck[1]-.08,neck[1]+.13,y))*smooth(neck[2]-.13,neck[2]+.18,z))
   for i,leg in enumerate(legs):
    lx,ly,lz,kz,ky,fz,fy=leg
    front=i==0
    split=(legs[0][1]+legs[-1][1])*.5
    region=(1-smooth(split-.03,split+.03,y)) if front and len(legs)>1 else smooth(split-.03,split+.03,y) if len(legs)>1 else 1
    region*=1-smooth(legtop-.05,legtop+.08,z)
    # Keep belly/chest central vertices attached to body.
    region*=smooth(lx*.22,lx*.65,abs(x))
    amount=remaining*region
    prefix=('Front' if front else 'Hind')+('L' if x<0 else 'R')
    lower=1-smooth(kz-.055,kz+.055,z);foot=1-smooth(fz+.008,fz+.04,z)
    for n,w in [(prefix+'Upper',1-lower),(prefix+'Lower',lower*(1-foot)),(prefix+'Foot',lower*foot)]:
     if w*amount>0:weights[n]=weights.get(n,0)+w*amount
    remaining-=amount
   weights['MotionRoot']=remaining
   # Importer supports four influences; trim negligible boundaries and renormalize.
   best=sorted(weights.items(),key=lambda p:p[1],reverse=True)[:4];total=sum(w for n,w in best)
   for n,w in best:
    if w>1e-7:groups[n].add([v.index],w/total,'REPLACE')
  mod=ob.modifiers.new('Anatomical skin','ARMATURE');mod.object=arm;ob.parent=arm
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
 for ob in meshes:ob.select_set(True)
 bpy.context.view_layer.objects.active=arm
 # Armature exporter must retain its axis transform; baking space transforms is
 # unsupported for armatures. Unity applies the FBX coordinate conversion.
 bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Resources'/ (path+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',bake_space_transform=False,bake_anim=False,add_leaf_bones=False,path_mode='STRIP',use_mesh_modifiers=False)
 bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Animation'/ (name+'.blend')),compress=True)
 backup=ROOT/'ArtSource/Animation'/(name+'.blend1')
 if backup.exists():backup.unlink()
 return {'bones':len(bones),'meshes':{ob.name:len(ob.data.vertices) for ob in meshes}}
stats={name:rig(name,spec) for name,spec in SPECS.items()}
(ROOT/'ArtSource/Animation/rig-stats.json').write_text(json.dumps(stats,indent=2)+'\n')
print('RIG_STATS',json.dumps(stats))
