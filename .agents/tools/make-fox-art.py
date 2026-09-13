"""Create an original red fox sculpt with baked coat and two LODs.
Run: blender -b -t 4 --python .agents/tools/make-fox-art.py
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
ROOT=Path(__file__).resolve().parents[2];OUT=Path(os.environ.get('WILDLIFE_OUTPUT',str(ROOT/'Assets/Resources/Fox')));SRC=Path(os.environ.get('WILDLIFE_SOURCE',str(ROOT/'ArtSource/Fox')))
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
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=p);o=bpy.context.object;o.scale=s;o.rotation_euler=rot;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,m)

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
    o=join(obs,name);m=o.modifiers.new('Unify anatomical volumes','REMESH');m.mode='VOXEL';m.voxel_size=voxel;apply(o,m);m=o.modifiers.new('Relax muscle transitions','SMOOTH');m.factor=1.0;m.iterations=18;apply(o,m)
    count=sum(len(p.vertices)-2 for p in o.data.polygons);m=o.modifiers.new('Sculpt retopology','DECIMATE');m.ratio=min(1,target/count);apply(o,m)
    for p in o.data.polygons:p.use_smooth=True
    return o

def ear(name,base,tip,width,m=COAT,depth=.025):
    a=Vector(base);d=Vector(tip)-a;side=d.cross(Vector((0,-1,0))).normalized();verts=[];faces=[]
    for k in range(9):
        t=k/8;w=(t/.22 if t<.22 else (1-t)/.78)**.72*width*.5
        for u in [-1,-.5,0,.5,1]:verts.append(a+d*t+side*w*u+Vector((0,depth*(1-u*u)*math.sin(t*math.pi),0)))
    for k in range(8):
        for j in range(4):i=k*5+j;faces.append((i,i+1,i+6,i+5))
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);finish(o,name,m);mod=o.modifiers.new('Ear thickness','SOLIDIFY');mod.thickness=.008;apply(o,mod);return o


RED=mat('FoxRust',(.48,.15,.032));CREAM=mat('FoxIvory',(.88,.81,.64));DARK=mat('FoxCharcoal',(.015,.012,.009),.32);INNER=mat('FoxEarVelvet',(.18,.085,.060));IRIS=mat('FoxAmber',(.36,.16,.028),.27)
# Slight asymmetry in the tail and feet gives a quiet alert pose.
vol=[ell('Long ribcage',(0,.01,.43),(.115,.30,.137),RED),ell('Shoulders',(0,-.20,.445),(.109,.125,.154),RED),ell('Haunch',(0,.23,.405),(.125,.15,.137),RED),tube('Neck',[(0,-.19,.45),(0,-.28,.56),(0,-.32,.62)],[.113,.091,.075],RED,20),ell('Cranium',(0,-.35,.624),(.085,.099,.085),RED),ell('Cheek ruff',(0,-.337,.584),(.108,.075,.063),RED),tube('Fine tapered muzzle',[(0,-.39,.61),(0,-.46,.583),(0,-.525,.568)],[.062,.039,.020],RED,18)]
for s in [-1,1]:
    x=s*.073
    vol += [tube('Foreleg',[(x,-.20,.43),(x,-.222,.26),(x,-.218,.035)],[.045,.025,.017],RED,14),ell('Front paw',(x,-.231,.027),(.026,.049,.027),RED),tube('Hindleg',[(s*.085,.22,.40),(s*.09,.137,.265),(s*.085,.26,.136),(s*.08,.231,.05)],[.076,.041,.023,.017],RED,14),ell('Hind paw',(s*.08,.21,.025),(.027,.049,.025),RED)]
# A broad tapering brush sweeps out from the rump with a lifted tip.
tailpts=[(0,.30,.435),(.025,.405,.375),(.068,.54,.276),(.113,.69,.215),(.152,.84,.235),(.173,.965,.292),(.182,1.065,.365),(.175,1.105,.402)]
rads=[.064,.085,.104,.107,.091,.065,.031,.003]
curve=[];rr=[]
for i in range(len(tailpts)-1):
    a,b,c,d=[Vector(tailpts[min(max(j,0),len(tailpts)-1)]) for j in [i-1,i,i+1,i+2]]
    for step in range(4):
        t=step/4;curve.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));rr.append(rads[i]*(1-t)+rads[i+1]*t)
curve.append(Vector(tailpts[-1]));rr.append(rads[-1]);vol.append(tube('Sweeping brush',curve,rr,RED,24))
body=sculpt(vol,'Fox sculpt',.006,13700)
# Bake hand-authored anatomical masks into a conventional portable base-color map.
bpy.context.view_layer.objects.active=body;bpy.ops.object.select_all(action='DESELECT');body.select_set(True)
ca=body.data.color_attributes.new(name='FoxCoat',type='FLOAT_COLOR',domain='CORNER')
def clamp(t):return max(0,min(1,t))
for poly in body.data.polygons:
    for li in poly.loop_indices:
        x,y,z=body.matrix_world@body.data.vertices[body.data.loops[li].vertex_index].co
        c=Vector((.54,.17,.030));back=clamp((z-.48)*8)*clamp((y+.23)*7)*clamp((.42-y)*9)
        c=c.lerp(Vector((.34,.081,.017)),back*.4)
        belly=clamp((.385-z)*30)*clamp((z-.27)*30)*clamp((.14-y)*24)*clamp((y+.17)*24)*clamp((.065-abs(x))*45)
        bib=clamp((-y-.24)*32)*clamp((z-.40)*25)*clamp((.575-z)*25)*clamp((.08-abs(x))*30)
        chin=clamp((-y-.368)*35)*clamp((.595-z)*65)
        cheek=clamp((abs(x)-.068)*65)*clamp((-y-.285)*30)*clamp((y+.423)*35)*clamp((.620-z)*45)*clamp((z-.548)*40)
        tip=clamp((y-.891)*38)
        c=c.lerp(Vector((.86,.78,.60)),max(belly,bib,chin,cheek,tip))
        stocking=clamp((.185-z)*35)*clamp((.39-y)*25)
        c=c.lerp(Vector((.021,.015,.011)),stocking)
        grain=.97+.025*math.sin(x*377+y*221+z*195)+.012*math.sin(x*817-y*559+z*771)
        ca.data[li].color=(*(v*grain for v in c),1)
m=mat('FoxCoat',(1,1,1));body.data.materials.clear();body.data.materials.append(m);nodes=m.node_tree.nodes;links=m.node_tree.links
nodes.clear();output=nodes.new('ShaderNodeOutputMaterial');em=nodes.new('ShaderNodeEmission');vc=nodes.new('ShaderNodeVertexColor');vc.layer_name='FoxCoat';links.new(vc.outputs['Color'],em.inputs[0]);links.new(em.outputs[0],output.inputs[0])
im=bpy.data.images.new('FoxCoat_BaseColor',width=2048,height=2048);im.filepath_raw=str(OUT/'FoxCoat_BaseColor.png');im.file_format='PNG';tex=nodes.new('ShaderNodeTexImage');tex.image=im;nodes.active=tex
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.0,island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT');sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=1;sc.render.bake.margin=12;bpy.ops.object.bake(type='EMIT');im.save();im.pack()
nodes.remove(em);nodes.remove(vc);bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Base Color'].default_value=(1,1,1,1);bs.inputs['Roughness'].default_value=.83;links.new(tex.outputs[0],bs.inputs['Base Color']);links.new(bs.outputs[0],output.inputs[0])
parts=[body]
for s in [-1,1]:
    # Outer dark backs, inset velvet, and a smaller pale inner rim.
    parts += [ear('Pointed dark ear',(s*.05,-.323,.661),(s*.083,-.303,.806),.115,DARK,.029),ear('Rust ear face',(s*.05,-.332,.673),(s*.080,-.315,.794),.096,RED,.019),ear('Recessed inner ear',(s*.052,-.338,.687),(s*.079,-.323,.774),.058,INNER,.013)]
    # Small almond sockets embedded into cranium, warm irises and light catch.
    from mathutils.bvhtree import BVHTree
    tree=BVHTree.FromPolygons([body.matrix_world@v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons])
    hit,normal,idx,dist=tree.ray_cast(Vector((s*.3,-.400,.642)),Vector((-s,0,0)))
    ex=hit.x
    parts += [ell('Eye socket',(ex-s*.003,-.400,.642),(.005,.019,.012),DARK),ell('Amber iris',(ex+s*.001,-.403,.642),(.002,.009,.008),IRIS),ell('Vertical pupil',(ex+s*.0025,-.403,.642),(.001,.003,.007),DARK),ell('Eye glint',(ex+s*.003,-.406,.646),(.0015,.0015,.0015),CREAM)]

    parts.append(tube('Upper almond lid',[(ex,-.419,.644),(ex+s*.003,-.403,.653),(ex,-.383,.645)],[.002,.003,.001],DARK,6))
    for j in range(4):
        parts.append(tube('Muzzle whisker',[(s*.025,-.488,.570),(s*.078,-.48,.566+(j-1)*.009),(s*.12,-.46,.565+(j-1)*.015)],[.0008,.0005,.0001],CREAM,4))
    # Tapered cheek locks break the toy-smooth facial silhouette.
    for j in range(3):
        parts.append(ear('Cheek fur lock',(s*(.073+j*.007),-.346+j*.012,.602-j*.009),(s*(.111+j*.003),-.315+j*.011,.583-j*.015),.016,CREAM,.003))
    parts.append(tube('Mouth crease',[(s*.008,-.525,.558),(s*.024,-.487,.553),(s*.048,-.441,.566)],[.0018,.002,.0006],DARK,5))
parts += [ell('Velvet nose',(0,-.526,.569),(.022,.016,.014),DARK),tube('Nose philtrum',[(0,-.529,.562),(0,-.525,.555)],[.0015,.001],DARK,5)]
high=join(parts,'Fox_LOD0');bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
ground=min(v.co.z for v in high.data.vertices)
for v in high.data.vertices:v.co.z-=ground
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
if tris(high)>17900:
    d=high.modifiers.new('High budget','DECIMATE');d.ratio=17900/tris(high);apply(high,d)
low=high.copy();low.data=high.data.copy();bpy.context.collection.objects.link(low);low.name='Fox_LOD1';d=low.modifiers.new('Distance simplification','DECIMATE');d.ratio=2850/tris(low);apply(low,d)
# Blender front is -Y, FBX conversion maps that to Unity +Z.
bpy.ops.object.select_all(action='DESELECT');high.select_set(True);low.select_set(True);bpy.context.view_layer.objects.active=high
bpy.ops.export_scene.fbx(filepath=str(OUT/'Fox.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
stats={'Fox_LOD0':tris(high),'Fox_LOD1':tris(low),'dimensions_blender_xyz_m':list(high.dimensions),'unity_forward':'+Z','provenance':'Original procedural sculpt and original baked coat. No external source assets.'};(SRC/'mesh-stats.json').write_text(json.dumps(stats,indent=2)+'\n');low.hide_render=True;low.hide_set(True)
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.name='Studio only';bpy.context.object.data.materials.append(mat('StudioMoss',(.12,.16,.125)))
sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.60,.72,.85,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.40
for loc,power,size in [((-3,-4,5),420,4),((2,2,3),350,3)]:
    bpy.ops.object.light_add(type='AREA',location=loc);bpy.context.object.data.energy=power;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=size
bpy.ops.object.camera_add(location=(1.55,-2.3,1.05));cam=bpy.context.object;cam.rotation_euler=(Vector((.03,.20,.39))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=1.72;sc.camera=cam
sc.render.engine='CYCLES';sc.cycles.samples=40;sc.render.resolution_x=1400;sc.render.resolution_y=1100;sc.render.resolution_percentage=100;sc.view_settings.view_transform='AgX'
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Fox.blend'));sc.render.filepath=str(SRC/'fox-portrait.png');bpy.ops.render.render(write_still=True)
cam.location=(2.3,.12,.79);cam.rotation_euler=(Vector((0,.24,.39))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=1.87;sc.render.filepath=str(SRC/'fox-side.png');bpy.ops.render.render(write_still=True)
backup=SRC/'Fox.blend1'
if backup.exists():backup.unlink()
print('FOX_STATS',json.dumps(stats))
