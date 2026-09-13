"""Create a separate NPS-photo-referenced adult doe study with a compatible rig.
Run: blender -b -t 6 --python .agents/tools/make-reference-deer.py
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
ROOT=Path(__file__).resolve().parents[2];OUT=Path(os.environ.get('REFERENCE_DEER_OUTPUT',str(ROOT/'Assets/Resources/ReferenceDeer')));SRC=ROOT/'ArtSource/ReferenceDeer'
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
    o=join(obs,name);m=o.modifiers.new('Unify anatomical volumes','REMESH');m.mode='VOXEL';m.voxel_size=voxel;apply(o,m);m=o.modifiers.new('Relax muscle transitions','SMOOTH');m.factor=.75;m.iterations=14;apply(o,m)
    # Broad relaxation only at large anatomical junctions; retain thin distal legs.
    weights=o.vertex_groups.new(name='Muscle blending')
    for v in o.data.vertices:
        x,y,z=v.co
        weight=max(0,min(1,(z-.63)/.16))*max(0,min(1,(1.5-z)/.20))
        if weight:weights.add([v.index],weight,'REPLACE')
    m=o.modifiers.new('Broad muscle junction blending','SMOOTH');m.factor=.95;m.iterations=180;m.vertex_group=weights.name;apply(o,m)
    o.vertex_groups.clear()
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
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);finish(o,name,m);mod=o.modifiers.new('Ear thickness','SOLIDIFY');mod.thickness=.004;apply(o,mod);mod=o.modifiers.new('Smooth ear rim','SUBSURF');mod.levels=2;apply(o,mod);return o

def coat_bake(o,name,kind):
    bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
    ca=o.data.color_attributes.new(name='Coat',type='FLOAT_COLOR',domain='CORNER');r=random.Random(291)
    for p in o.data.polygons:
        for li in p.loop_indices:
            v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co;x,y,z=v
            if kind=='deer':
                c=Vector((.29,.145,.062));belly=max(0,min(1,(.83-z)*14))*max(0,min(1,(.60-abs(y))*9))*max(0,min(1,(.15-abs(x))*24));throat=max(0,min(1,(-y-.78)*12))*max(0,min(1,(z-1.23)*12))*max(0,min(1,(1.48-z)*15))
                pale=max(belly,throat*.85);c=c.lerp(Vector((.66,.59,.46)),pale)
                dorsal=max(0,min(1,(z-1.02)*5)) * max(0,min(1,(y+.6)*5))
                c=c.lerp(Vector((.20,.112,.058)),dorsal*.35)
                # Pinna color follows ear coordinates, including a narrow dark rim.
                if z>1.67 and abs(x)>.075:
                    tip=Vector((.207,-.774 if x>0 else -.81,1.834 if x>0 else 1.821))
                    base=Vector((.044,-.842,1.619));point=Vector((abs(x),y,z))
                    axis=tip-base;t=max(0,min(1,(point-base).dot(axis)/axis.length_squared))
                    side=axis.cross(Vector((0,-1,0))).normalized()
                    w=max(.002,math.sin(t*math.pi)**.7*.054)
                    u=abs((point-base-axis*t).dot(side))/w
                    interior=max(0,min(1,(.87-u)*5))*max(0,min(1,(t-.12)*6))
                    c=c.lerp(Vector((.49,.425,.33)),interior*.85)
                    c*=1-.16*max(0,min(1,(u-.8)*5))
                if y<-.82 and z>1.40:
                    bridge=math.exp(-((x/.052)**2+((y+1.04)/.14)**2+((z-1.56)/.105)**2))* .58
                    c=c.lerp(Vector((.10,.075,.056)),bridge)
                    orbit=math.exp(-(((abs(x)-.071)/.025)**2+((y+.959)/.042)**2+((z-1.608)/.026)**2))
                    c=c.lerp(Vector((.46,.37,.26)),orbit*.65)
            else:
                c=Vector((.25,.19,.115));pale=max(0,min(1,(.20-z)*10));c=c.lerp(Vector((.64,.60,.49)),pale)
            grain=.005*math.sin(x*19+y*23+z*16)+.002*math.sin(x*57-y*43+z*61)
            ca.data[li].color=(*(max(.005,t+grain) for t in c),1)
    m=mat(name+'Coat',(1,1,1));o.data.materials.clear();o.data.materials.append(m);nodes=m.node_tree.nodes;links=m.node_tree.links
    nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');em=nodes.new('ShaderNodeEmission');vc=nodes.new('ShaderNodeVertexColor');vc.layer_name='Coat';links.new(vc.outputs['Color'],em.inputs[0]);links.new(em.outputs[0],out.inputs[0])
    im=bpy.data.images.new(name+'Coat',width=2048,height=2048);im.filepath_raw=str(OUT/(name+'Coat.png'));im.file_format='PNG';tex=nodes.new('ShaderNodeTexImage');tex.image=im;nodes.active=tex
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=.55,island_margin=.012);bpy.ops.object.mode_set(mode='OBJECT')
    sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=1;sc.render.bake.margin=12;bpy.ops.object.bake(type='EMIT')
    # Fine original coat grain lives in the texture, avoiding vertex-scale mottling.
    pixels=np.empty(2048*2048*4,dtype=np.float32);im.pixels.foreach_get(pixels);pixels=pixels.reshape((2048,2048,4))
    noise=np.random.default_rng(721).normal(0,.026,(2048,2048))
    noise=(noise+np.roll(noise,1,axis=0)+np.roll(noise,2,axis=0))/3
    # Dense directional hair flecks plus restrained larger coat variation, baked into albedo.
    rng=np.random.default_rng(529)
    grain=rng.normal(0,.045,(2048,2048))
    for shift in range(1,6):grain+=np.roll(grain,shift,axis=0)*.15
    pixels[:,:,:3]=np.clip(pixels[:,:,:3]*(1+noise[:,:,None]+grain[:,:,None]),0,1);im.pixels.foreach_set(pixels.ravel());im.save()
    nodes.remove(em);nodes.remove(vc);bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.87;links.new(tex.outputs['Color'],bs.inputs['Base Color']);links.new(bs.outputs[0],out.inputs[0]);return o

def hoof(name,x,y):
    # Three octagonal rings: flat sole, broad toe, narrower coronet embedded in shin.
    outline=[(-.65,-1),(.65,-1),(1,-.65),(1,.65),(.65,1),(-.65,1),(-1,.65),(-1,-.65)]
    rings=[(0,.0105,.030,-.010),(.016,.011,.030,-.010),(.076,.009,.023,.003)]
    verts=[(x+a*w,y+b*length+shift,z) for z,w,length,shift in rings for a,b in outline]
    faces=[tuple(reversed(range(8))),tuple(range(16,24))]
    for ring in range(2):
        for j in range(8):a=ring*8+j;b=ring*8+(j+1)%8;faces.append((a,b,b+8,a+8))
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);finish(ob,name,HOOF)
    for face in ob.data.polygons:face.use_smooth=False
    mod=ob.modifiers.new('Small hoof edge bevel','BEVEL');mod.width=.0025;mod.segments=2;apply(ob,mod)
    return ob

# Original adult doe, modeled against NPS Big Thicket photographs. See source README.
def rings(name,sections,sides=40):
    # Cross sections perpendicular to longitudinal Y: y, x radius, bottom, top.
    verts=[];faces=[]
    for y,w,lo,hi in sections:
        for j in range(sides):
            a=j*math.tau/sides;z=(lo+hi)/2+(hi-lo)/2*math.cos(a)
            verts.append((w*math.sin(a),y,z))
    for i in range(len(sections)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(sections)-1)*sides+j for j in range(sides))])
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);finish(ob,name,COAT)
    mod=ob.modifiers.new('Anatomical surface interpolation','SUBSURF');mod.levels=2;apply(ob,mod);return ob

vol=[rings('Ribcage with tuck and withers',[
 (-.51,.06,.91,1.07),(-.44,.145,.80,1.17),(-.32,.185,.73,1.23),(-.18,.219,.71,1.215),(.03,.232,.73,1.20),(.24,.208,.80,1.215),(.43,.199,.85,1.245),(.57,.15,.87,1.22),(.63,.055,.98,1.13)]),
 rings('S-neck', [(-.91,.049,1.41,1.62),(-.84,.062,1.31,1.65),(-.76,.072,1.25,1.56),(-.65,.086,1.12,1.41),(-.52,.12,.94,1.25),(-.40,.135,.90,1.17),(-.28,.12,.94,1.15)]),
 rings('Long wedge skull',[(-1.18,.042,1.42,1.49),(-1.125,.053,1.412,1.525),(-1.04,.061,1.425,1.59),(-.97,.08,1.46,1.65),(-.895,.080,1.49,1.665),(-.82,.061,1.50,1.655),(-.79,.025,1.54,1.61)])]
# Thin forearm and cannon; rear stifle is forward of hock. Slight stance asymmetry.
feet=[]
for s in [-1,1]:
    x=s*.139;shift=.035 if s==1 else 0
    vol += [ell('Scapula',(s*.09,-.31,1.015),(.072,.14,.17),rot=(.2,0,0)),
            tube('Forearm',[(x*.66,-.32,1.15),(x,-.335,.99),(x,-.32,.79),(x,-.375,.55),(x,-.377,.48),(x,-.355,.14),(x,-.37,.075)],[.062,.058,.048,.029,.024,.018,.023],sides=18),
            ell('Hindquarter',(s*.086,.43,1.015),(.082,.158,.195),rot=(-.24,0,0)),
            tube('Rear leg',[(x*.65,.43,1.16),(x,.40,.95),(x,.29,.76),(x,.44,.55),(x,.50+shift,.43),(x,.453+shift,.15),(x,.425+shift,.075)],[.075,.080,.060,.035,.027,.018,.024],sides=18)]
    vol += [tube('Ear root',[(s*.039,-.84,1.615),(s*.065,-.832,1.665),(s*.075,-.827,1.678)],[.027,.022,.007],sides=16)]
    feet.extend([(x,-.37),(x,.425+shift)])
body=sculpt(vol,'Doe continuous skin',.0055,22500)
# One curved pinna per ear, with a baked pale bowl. Avoid stacked inset plates.
for s in [-1,1]:
    tip=(s*.207,-.774 if s==1 else -.81,1.834 if s==1 else 1.821)
    pinna=ear('Furred pinna',(s*.044,-.842,1.619),tip,.108,COAT,.019)
    body=join([body,pinna],'Doe continuous skin')
body=coat_bake(body,'Deer','deer');parts=[body]
# Soft facial masks follow skull, without white spherical eye decorations.
CREAM.diffuse_color=(.48,.43,.34,1);CREAM.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.48,.43,.34,1)
PINK.diffuse_color=(.30,.235,.19,1);PINK.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.30,.235,.19,1)
LID=mat('Doe eyelid',(.09,.053,.027),.77)
for s in [-1,1]:
    # Almond eye viewed laterally; the bony orbit and dark upper lid carry expression.
    parts.append(ell('Eye',(s*.067,-.964,1.595),(.009,.024,.014),DARK,rot=(0,0,s*.15)))
    parts.append(tube('Upper eyelid',[(s*.061,-.986,1.597),(s*.075,-.970,1.606),(s*.075,-.95,1.604),(s*.067,-.942,1.595)],[.001,.002,.002,.001],LID,8))
    parts.append(tube('Lower eyelid',[(s*.061,-.986,1.596),(s*.077,-.965,1.583),(s*.067,-.942,1.595)],[.001,.0025,.001],LID,8))
    #parts.append(tube('Tear duct',[(s*.075,-.984,1.589),(s*.068,-1.006,1.574)],[.0025,.0008],LID,7))
parts.append(ell('Chin pale lip',(0,-1.128,1.424),(.032,.037,.006),CREAM))
# Nose flattened dorsally, broad at nostrils and tapering into upper lip.
parts.append(rings('Nose leather',[(-1.194,.019,1.439,1.473),(-1.191,.035,1.435,1.484),(-1.177,.040,1.435,1.488),(-1.16,.031,1.445,1.488)],24))
parts[-1].data.materials.clear();parts[-1].data.materials.append(DARK)
# Cut nostril hollows into the leather instead of attaching dark beads.
nose=parts[-1]
for s in [-1,1]:
    cutter=ell('Nostril cutter',(s*.034,-1.184,1.474),(.009,.012,.006),DARK,rot=(0,0,s*.35))
    mod=nose.modifiers.new('Inset nostril','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cutter;apply(nose,mod)
    bpy.data.objects.remove(cutter,do_unlink=True)
for x,y in feet:
    for split in [-1,1]:parts.append(hoof('Cloven hoof',x+split*.0105,y))
    for dx in [-.015,.015]:parts.append(ell('Dewclaw',(x+dx,y+.018,.11),(.005,.007,.010),HOOF))
parts.append(tube('Resting tail',[(0,.575,1.14),(0,.653,1.03),(0,.67,.85)],[.042,.037,.004],COAT,20))
parts.append(ear('White tail underside',(0,.668,1.103),(0,.689,.865),.076,CREAM,.012))
high=join(parts,'Deer_LOD0');bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
m=high.modifiers.new('Close view triangle budget','DECIMATE');m.ratio=min(1,30000/sum(len(p.vertices)-2 for p in high.data.polygons));apply(high,m)
low=high.copy();low.data=high.data.copy();bpy.context.collection.objects.link(low);low.name='Deer_LOD1';m=low.modifiers.new('Distance simplification','DECIMATE');m.ratio=6000/sum(len(p.vertices)-2 for p in low.data.polygons);apply(low,m)
stats={o.name:sum(len(p.vertices)-2 for p in o.data.polygons) for o in [high,low]}
# Save unrigged source then reuse original compatible skinning routine with this anatomy.
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Deer.blend'),compress=True)
rigcode=(ROOT/'.agents/tools/rig-animals.py').read_text();rigcode=rigcode[rigcode.index('def smooth'):rigcode.index('stats={name:rig')]
rigcode=rigcode.replace("detail=ROOT/'ArtSource/Detail'/f'{name}.blend'","detail=ROOT/'ArtSource/ReferenceDeer/Deer.blend'")
rigcode=rigcode.replace("ROOT/'ArtSource/Animation'","ROOT/'ArtSource/ReferenceDeer'")
rigcode=rigcode.replace("ROOT/'Assets/Resources'/ (path+'.fbx')","OUT/'Deer.fbx'")
exec(rigcode)
rig('Deer',('ReferenceDeer/Deer.blend','ReferenceDeer/Deer',(0,-.48,1.06),(0,-.85,1.55),(.062,-.825,1.637),(0,.61,1.1),[(.139,-.335,1.01,.50,-.377,.10,-.37),(.139,.43,1.02,.43,.50,.10,.44)],.77))
# Studio views are inspection evidence; the deliverable is the geometry, not this lighting.
high=bpy.data.objects['Deer_LOD0'];low=bpy.data.objects['Deer_LOD1'];low.hide_render=True;low.hide_set(True)
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.data.materials.append(mat('Studio',(.13,.145,.12)))
sc=bpy.context.scene;sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.74,.84,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.45
for pos,energy,size in [((3,-4,5),500,4),((-3,1,4),500,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);bpy.context.object.data.energy=energy;bpy.context.object.data.size=size
bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.35
sc.render.engine='CYCLES';sc.cycles.samples=16;sc.render.resolution_x=1200;sc.render.resolution_y=1100;sc.render.resolution_percentage=100;sc.view_settings.view_transform='AgX'
for view,pos,target,zoom in [('profile',(4,.1,1.6),(0,-.1,.96),2.3),('three-quarter',(3,-3,1.9),(0,-.18,1),2.35),('face',(1,-2,1.8),(0,-.94,1.60),.72)]:
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=zoom;sc.render.filepath=str(SRC/(view+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Deer.blend'),compress=True)
(SRC/'mesh-stats.json').write_text(json.dumps(stats,indent=2)+'\n')
for backup in SRC.glob('*.blend1'):backup.unlink()
print('REFERENCE_DEER_STATS',json.dumps(stats))
