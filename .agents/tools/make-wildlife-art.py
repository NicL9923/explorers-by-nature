"""Create original sculpted deer and cottontail, with baked coat textures and two LODs.
Run: blender -b -t 4 --python .agents/tools/make-wildlife-art.py
"""
from pathlib import Path
import bpy, math, random, json, os, tempfile
import numpy as np
from mathutils import Vector
# Fedora Blender may ship a newer OCIO profile than its linked OCIO library.
try:
    bpy.context.scene.view_settings.view_transform='AgX'
except TypeError:
    config=Path('/usr/share/blender/5.2/datafiles/colormanagement/config.ocio')
    if config.exists() and not os.environ.get('WILDLIFE_OCIO_RETRY'):
        text=config.read_text().replace('ocio_profile_version: 2.5','ocio_profile_version: 2.4')
        text=text.replace('search_path: "icc:luts:filmic"',f'search_path: "{config.parent}/icc:{config.parent}/luts:{config.parent}/filmic"')
        fd,path=tempfile.mkstemp(prefix='wildlife-ocio-',suffix='.ocio')
        with os.fdopen(fd,'w') as f:f.write(text)
        os.environ['OCIO']=path;os.environ['WILDLIFE_OCIO_RETRY']='1'
        os.execv(bpy.app.binary_path,[bpy.app.binary_path,'-b','-t','4','--python',str(Path(__file__).resolve())])
    else:raise RuntimeError('Working OCIO configuration required for coat bake.')
ROOT=Path(__file__).resolve().parents[2];OUT=Path(os.environ.get('WILDLIFE_OUTPUT',str(ROOT/'Assets/Resources/Wildlife')));SRC=Path(os.environ.get('WILDLIFE_SOURCE',str(ROOT/'ArtSource/Wildlife')))
OUT.mkdir(parents=True,exist_ok=True);SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def mat(name,c,rough=.8):
    m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=rough;return m
HOOF=mat('DeerHoof',(.045,.030,.018),.76)
DARK=mat('WildlifeDark',(.021,.014,.010),.28);PINK=mat('WildlifeEar',(.36,.17,.13));HORN=mat('DeerAntler',(.40,.29,.17));CREAM=mat('WildlifeCream',(.68,.63,.48));COAT=mat('SculptCoat',(.32,.18,.09))

def finish(o,name,m):
    o.name=name;o.data.materials.append(m)
    for p in o.data.polygons:p.use_smooth=True
    return o

def ell(name,p,s,m=COAT,rot=(0,0,0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=28,ring_count=18,location=p);o=bpy.context.object;o.scale=s;o.rotation_euler=rot;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,m)

def tube(name,pts,radii,m=COAT,sides=10):
    verts=[];faces=[]
    for i,p in enumerate(pts):
        tangent=(Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(0,i-1)])).normalized();side=tangent.cross(Vector((1,0,0)))
        if side.length<.01:side=tangent.cross(Vector((0,1,0)))
        side.normalize();up=tangent.cross(side).normalized()
        for j in range(sides):verts.append(Vector(p)+radii[i]*(side*math.cos(j*math.tau/sides)+up*math.sin(j*math.tau/sides)))
    for i in range(len(pts)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(pts)-1)*sides+j for j in range(sides))]);me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);return finish(o,name,m)

def join(obs,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs:o.select_set(True)
    bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=obs[0];o.name=name;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);return o

def apply(o,m):bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)

def sculpt(obs,name,voxel,target):
    o=join(obs,name);m=o.modifiers.new('Unify anatomical volumes','REMESH');m.mode='VOXEL';m.voxel_size=voxel;apply(o,m);m=o.modifiers.new('Relax muscle transitions','SMOOTH');m.factor=.8;m.iterations=5;apply(o,m)
    count=sum(len(p.vertices)-2 for p in o.data.polygons);m=o.modifiers.new('Sculpt retopology','DECIMATE');m.ratio=min(1,target/count);apply(o,m)
    for p in o.data.polygons:p.use_smooth=True
    return o

def ear(name,base,tip,width,m=COAT,depth=.025):
    a=Vector(base);d=Vector(tip)-a;side=d.cross(Vector((0,-1,0))).normalized();verts=[];faces=[]
    for k in range(9):
        t=k/8;w=math.sin(t*math.pi)**.7*width*.5
        for u in [-1,-.5,0,.5,1]:verts.append(a+d*t+side*w*u+Vector((0,depth*(1-u*u)*math.sin(t*math.pi),0)))
    for k in range(8):
        for j in range(4):i=k*5+j;faces.append((i,i+1,i+6,i+5))
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);finish(o,name,m);mod=o.modifiers.new('Ear thickness','SOLIDIFY');mod.thickness=.008;apply(o,mod);return o

def coat_bake(o,name,kind):
    bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
    ca=o.data.color_attributes.new(name='Coat',type='FLOAT_COLOR',domain='CORNER');r=random.Random(291)
    for p in o.data.polygons:
        for li in p.loop_indices:
            v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co;x,y,z=v
            if kind=='deer':
                c=Vector((.32,.16,.065));belly=max(0,min(1,(.95-z)*8))*max(0,min(1,(.63-abs(y))*8));throat=max(0,min(1,(-y-.49)*8))*max(0,min(1,(z-1.1)*4))*max(0,min(1,(1.53-z)*18))
                pale=max(belly,throat*.85);c=c.lerp(Vector((.68,.54,.35)),pale)
            else:
                c=Vector((.25,.19,.115));pale=max(0,min(1,(.20-z)*10));c=c.lerp(Vector((.64,.60,.49)),pale)
            grain=.005*math.sin(x*19+y*23+z*16)+.002*math.sin(x*57-y*43+z*61)
            ca.data[li].color=(*(max(.005,t+grain) for t in c),1)
    m=mat(name+'Coat',(1,1,1));o.data.materials.clear();o.data.materials.append(m);nodes=m.node_tree.nodes;links=m.node_tree.links
    nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');em=nodes.new('ShaderNodeEmission');vc=nodes.new('ShaderNodeVertexColor');vc.layer_name='Coat';links.new(vc.outputs['Color'],em.inputs[0]);links.new(em.outputs[0],out.inputs[0])
    im=bpy.data.images.new(name+'Coat',width=1024,height=1024);im.filepath_raw=str(OUT/(name+'Coat.png'));im.file_format='PNG';tex=nodes.new('ShaderNodeTexImage');tex.image=im;nodes.active=tex
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.0,island_margin=.018);bpy.ops.object.mode_set(mode='OBJECT')
    sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=1;sc.render.bake.margin=12;bpy.ops.object.bake(type='EMIT')
    # Fine original coat grain lives in the texture, avoiding vertex-scale mottling.
    pixels=np.empty(1024*1024*4,dtype=np.float32);im.pixels.foreach_get(pixels);pixels=pixels.reshape((1024,1024,4))
    noise=np.random.default_rng(721).normal(0,.028,(1024,1024))
    noise=(noise+np.roll(noise,1,axis=0)+np.roll(noise,2,axis=0))/3
    pixels[:,:,:3]=np.clip(pixels[:,:,:3]*(1+noise[:,:,None]),0,1);im.pixels.foreach_set(pixels.ravel());im.save()
    nodes.remove(em);nodes.remove(vc);bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.87;links.new(tex.outputs['Color'],bs.inputs['Base Color']);links.new(bs.outputs[0],out.inputs[0]);return o

def hoof(name,x,y):
    # Three octagonal rings: flat sole, broad toe, narrower coronet embedded in shin.
    outline=[(-.65,-1),(.65,-1),(1,-.65),(1,.65),(.65,1),(-.65,1),(-1,.65),(-1,-.65)]
    rings=[(0,.0135,.043,-.010),(.016,.0145,.043,-.010),(.106,.011,.027,.003)]
    verts=[(x+a*w,y+b*length+shift,z) for z,w,length,shift in rings for a,b in outline]
    faces=[tuple(reversed(range(8))),tuple(range(16,24))]
    for ring in range(2):
        for j in range(8):a=ring*8+j;b=ring*8+(j+1)%8;faces.append((a,b,b+8,a+8))
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);finish(ob,name,HOOF)
    for face in ob.data.polygons:face.use_smooth=False
    mod=ob.modifiers.new('Small hoof edge bevel','BEVEL');mod.width=.0025;mod.segments=2;apply(ob,mod)
    return ob

def deer():
    # A light-bodied young buck; narrow chest and delicate planted legs.
    vol=[ell('Ribcage',(0,.03,.99),(.235,.55,.30)),ell('Shoulder',(0,-.34,1.02),(.22,.27,.33)),ell('Haunch',(0,.40,1.01),(.245,.29,.26)),tube('Upright neck',[(0,-.38,1.04),(0,-.52,1.38),(0,-.65,1.60)],[.18,.12,.10]),ell('Skull',(0,-.69,1.59),(.105,.19,.14)),ell('Tapered muzzle',(0,-.86,1.52),(.073,.15,.072))]
    for s in [-1,1]:
        x=s*.15
        vol += [tube('Foreleg',[(x,-.33,.98),(x,-.38,.58),(x,-.35,.10)],[.074,.037,.025]),tube('Hindleg',[(x,.42,1.0),(x,.27,.70),(x,.48,.39),(x,.40,.10)],[.11,.065,.040,.025])]
    body=coat_bake(sculpt(vol,'DeerBody',.012,8700),'Deer','deer');parts=[body]
    for s in [-1,1]:
        parts.append(ear('Pointed ear',(s*.06,-.62,1.69),(s*.28,-.52,1.93),.15,CREAM))
        parts.append(ear('Inner ear',(s*.073,-.632,1.705),(s*.25,-.54,1.90),.10,PINK,.012))
        parts.append(ell('Dark almond eye',(s*.092,-.76,1.63),(.012,.029,.017),DARK))
        parts.append(ell('Warm iris',(s*.103,-.765,1.631),(.003,.014,.012),HORN))
        parts.append(ell('Horizontal pupil',(s*.106,-.767,1.632),(.002,.010,.006),DARK))
        parts.append(ell('Small catchlight',(s*.108,-.774,1.637),(.002,.003,.003),CREAM))
        parts.append(tube('Upper eyelid',[(s*.095,-.789,1.636),(s*.106,-.762,1.65),(s*.09,-.736,1.637)],[.003,.005,.002],COAT,6))
        parts.append(tube('Lower jaw line',[(s*.027,-.99,1.503),(s*.064,-.895,1.471),(s*.085,-.80,1.49)],[.0015,.002,.0008],DARK,5))
        for yy in [-.35,.40]:
            for split in [-1,1]:parts.append(hoof('Cloven hoof',s*.15+split*.016,yy))
        parts.append(tube('Antler beam',[(s*.065,-.60,1.72),(s*.10,-.51,1.92),(s*.17,-.37,2.10),(s*.20,-.25,2.13)],[.023,.019,.012,.001],HORN,8))
        parts.append(tube('Antler tine',[(s*.13,-.45,2.0),(s*.10,-.57,2.12)],[.012,.001],HORN,7))
        parts.append(tube('Antler brow tine',[(s*.09,-.54,1.86),(s*.13,-.68,1.98),(s*.145,-.70,2.035)],[.016,.008,.001],HORN,9))
        parts.append(tube('Antler outer fork',[(s*.16,-.39,2.08),(s*.25,-.38,2.15),(s*.30,-.39,2.20)],[.012,.007,.001],HORN,9))
    parts.append(ell('Nose',(0,-.983,1.53),(.057,.031,.041),DARK))
    parts.append(ear('White tail',(0,.60,1.12),(0,.76,.95),.11,CREAM))
    return join(parts,'Deer_LOD0')

def rabbit():
    vol=[ell('Body',(0,.075,.23),(.14,.23,.20)),ell('Rump',(0,.21,.22),(.155,.16,.19)),ell('Chest',(0,-.065,.24),(.12,.13,.19)),ell('Head',(0,-.18,.33),(.112,.11,.116)),ell('Muzzle',(0,-.27,.285),(.077,.07,.048))]
    for s in [-1,1]:
        vol+=[ell('Hind haunch',(s*.11,.14,.15),(.072,.12,.12)),ell('Long hind foot',(s*.11,.005,.043),(.052,.14,.043)),tube('Front leg',[(s*.068,-.10,.25),(s*.065,-.14,.055)],[.033,.022]),ell('Front paw',(s*.065,-.17,.030),(.029,.060,.030))]
    body=coat_bake(sculpt(vol,'RabbitBody',.005,4100),'Rabbit','rabbit');parts=[body]
    for s in [-1,1]:
        parts.append(ear('Long outer ear',(s*.043,-.15,.41),(s*.09,-.09,.69),.082,CREAM,.023))
        parts.append(ear('Pink ear interior',(s*.045,-.158,.44),(s*.084,-.104,.66),.050,PINK,.013))
        parts.append(ell('Rabbit eye',(s*.097,-.225,.35),(.014,.019,.020),DARK))
        for j in range(3):parts.append(tube('Whisker',[(s*.04,-.303,.29),(s*.12,-.325,.29+(j-1)*.011),(s*.18,-.34,.29+(j-1)*.024)],[.0018,.0012,.0001],CREAM,4))
    parts.append(ell('Cottontail',(0,.37,.23),(.067,.061,.068),CREAM));parts.append(ell('Rabbit nose',(0,-.331,.293),(.014,.009,.010),PINK));parts.append(tube('Mouth',[(0,-.331,.285),(0,-.334,.274)],[.002,.001],DARK,5))
    return join(parts,'Rabbit_LOD0')

stats={};highs=[]
for name,fn,target in [('Deer',deer,2600),('Rabbit',rabbit,1600)]:
    high=fn();bpy.context.view_layer.objects.active=high
    high_count=sum(len(p.vertices)-2 for p in high.data.polygons);budget=14500 if name=='Deer' else 7500
    if high_count>budget:
        mod=high.modifiers.new('Final triangle budget','DECIMATE');mod.ratio=budget/high_count;apply(high,mod)
    # Ground-origin coordinates, preserving all anatomy in one mesh per LOD.
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');highs.append(high)
    low=high.copy();low.data=high.data.copy();bpy.context.collection.objects.link(low);low.name=name+'_LOD1';count=sum(len(p.vertices)-2 for p in low.data.polygons);m=low.modifiers.new('Distance simplification','DECIMATE');m.ratio=target/count;apply(low,m)
    bpy.ops.object.select_all(action='DESELECT');high.select_set(True);low.select_set(True);bpy.context.view_layer.objects.active=high
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
    stats[name]={o.name:sum(len(p.vertices)-2 for p in o.data.polygons) for o in [high,low]};low.hide_render=True;low.hide_set(True)
(SRC/'mesh-stats.json').write_text(json.dumps(stats,indent=2)+'\n')
# Original studio scene, lit to reveal the integrated sculpt and silhouette.
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Studio ground';floor.data.materials.append(mat('StudioSand',(.17,.21,.15)))
sc=bpy.context.scene;sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.60,.72,.85,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.35
bpy.ops.object.light_add(type='AREA',location=(-3,-4,5));bpy.context.object.data.energy=450;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=4
bpy.ops.object.light_add(type='AREA',location=(3,2,4));bpy.context.object.data.energy=550;bpy.context.object.data.size=3
bpy.ops.object.camera_add(location=(3,-4,2.1));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.8;sc.camera=cam
sc.render.engine='CYCLES';sc.cycles.samples=48;sc.render.resolution_x=1200;sc.render.resolution_y=1200;sc.render.resolution_percentage=100;sc.view_settings.view_transform='AgX'
highs[1].hide_render=True;bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Wildlife.blend'));sc.render.filepath=str(SRC/'deer-portrait.png');bpy.ops.render.render(write_still=True)
highs[0].hide_render=True;highs[1].hide_render=False;cam.location=(1,-1.5,.75);cam.rotation_euler=(Vector((0,0,.32))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.95;sc.render.filepath=str(SRC/'rabbit-portrait.png');bpy.ops.render.render(write_still=True)
backup=SRC/'Wildlife.blend1'
if backup.exists():backup.unlink()
print('WILDLIFE_STATS',json.dumps(stats))
