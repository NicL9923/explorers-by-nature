"""Original mallard and beaver. Run blender -b -t 4 --python .agents/tools/make-river-wildlife.py."""
from pathlib import Path
import bpy, math, json, os, tempfile
from mathutils import Vector
try:bpy.context.scene.view_settings.view_transform='AgX'
except TypeError:
    config=Path('/usr/share/blender/5.2/datafiles/colormanagement/config.ocio')
    if config.exists() and not os.environ.get('RIVER_OCIO_RETRY'):
        text=config.read_text().replace('ocio_profile_version: 2.5','ocio_profile_version: 2.4').replace('search_path: "icc:luts:filmic"',f'search_path: "{config.parent}/icc:{config.parent}/luts:{config.parent}/filmic"')
        fd,path=tempfile.mkstemp(prefix='river-ocio-',suffix='.ocio')
        with os.fdopen(fd,'w') as f:f.write(text)
        os.environ['OCIO']=path;os.environ['RIVER_OCIO_RETRY']='1'
        os.execv(bpy.app.binary_path,[bpy.app.binary_path,'-b','-t','4','--python',str(Path(__file__).resolve())])
    else:raise
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Resources/RiverWildlife';SRC=ROOT/'ArtSource/RiverWildlife'
OUT.mkdir(parents=True,exist_ok=True);SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,c,rough=.75):
    m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Base Color'].default_value=(*c,1);n.inputs['Roughness'].default_value=rough;return m
GREEN=mat('MallardEmerald',(.018,.17,.082),.34);BROWN=mat('MallardChestnut',(.19,.056,.022));GREY=mat('MallardSilver',(.39,.40,.34));WING=mat('MallardWing',(.22,.23,.18));IVORY=mat('RiverIvory',(.82,.79,.64));BLACK=mat('RiverEyes',(.008,.006,.004),.18);BILL=mat('MallardOchre',(.69,.43,.025),.5);BLUE=mat('MallardSpeculum',(.035,.085,.32),.4);FEATHER=mat('MallardFeatherEdge',(.38,.34,.25));FUR=mat('BeaverUmber',(.14,.068,.025));FURLIGHT=mat('BeaverGuardHair',(.21,.115,.044));MUZZLE=mat('BeaverMuzzle',(.26,.155,.075));TAIL=mat('BeaverTail',(.065,.045,.03));TOOTH=mat('BeaverIncisors',(.65,.30,.065));EAR=mat('BeaverInnerEar',(.10,.045,.025));GLINT=mat('RiverEyeGlint',(.9,.94,.85),.12)
def finish(o,name,m):
    o.name=name;o.data.materials.append(m)
    for p in o.data.polygons:p.use_smooth=True
    return o

def mesh(name,verts,faces,m):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);return finish(o,name,m)
def loft(name,rings,m,n=32):
    # Anatomical cross sections along fore/aft axis: y, center height, half-width, half-height.
    verts=[(w*math.cos(j*math.tau/n),y,z+h*math.sin(j*math.tau/n)) for y,z,w,h in rings for j in range(n)];faces=[]
    for k in range(len(rings)-1):
        for j in range(n):a=k*n+j;b=k*n+(j+1)%n;faces.append((a,a+n,b+n,b))
    faces += [tuple(reversed(range(n))),tuple((len(rings)-1)*n+j for j in range(n))]
    o=mesh(name,verts,faces,m)
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
    return o
def ell(name,p,s,m,seg=20,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=p);o=bpy.context.object;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,m)
def apply(o,mod):bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
def join(obs,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs:o.select_set(True)
    bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=obs[0];o.name=name;return o

def tube(name,pts,rs,m,n=7):
    verts=[];faces=[]
    for i,p in enumerate(pts):
        tangent=(Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(i-1,0)])).normalized();side=tangent.cross(Vector((1,0,0)))
        if side.length<.01:side=tangent.cross(Vector((0,1,0)))
        side.normalize();up=tangent.cross(side).normalized()
        verts += [Vector(p)+rs[i]*(side*math.cos(j*math.tau/n)+up*math.sin(j*math.tau/n)) for j in range(n)]
    for i in range(len(pts)-1):
        for j in range(n):a=i*n+j;b=i*n+(j+1)%n;faces.append((a,b,b+n,a+n))
    faces += [tuple(reversed(range(n))),tuple((len(pts)-1)*n+j for j in range(n))];return mesh(name,verts,faces,m)
def feather(name,a,b,width,depth,m):
    a=Vector(a);b=Vector(b);d=b-a;side=Vector((0,0,1)).cross(d).normalized();verts=[]
    for i in range(7):
        t=i/6;w=width*math.sin(math.pi*t)**.65
        for u in [-1,0,1]:verts.append(a+d*t+side*w*u+Vector((0,0,depth*(1-u*u)*math.sin(math.pi*t))))
    faces=[]
    for i in range(6):
        for j in range(2):k=i*3+j;faces.append((k,k+1,k+4,k+3))
    o=mesh(name,verts,faces,m);mod=o.modifiers.new('Feather thickness','SOLIDIFY');mod.thickness=.001;apply(o,mod);return o

def duck():
    p=[loft('Streamlined hull',[(.31,.13,.004,.006),(.25,.13,.06,.04),(.17,.13,.115,.085),(.05,.14,.143,.114),(-.07,.145,.137,.123),(-.15,.16,.106,.115),(-.20,.18,.069,.089),(-.22,.19,.006,.01)],GREY)]
    # Upright breast and tapering neck meet under a thin white neck ring.
    p += [loft('Chestnut breast',[(-.09,.20,.035,.06),(-.15,.20,.087,.10),(-.20,.23,.075,.107),(-.225,.25,.035,.08)],BROWN)]
    p += [tube('Emerald neck',[(0,-.175,.245),(0,-.198,.31),(0,-.22,.355)],[.054,.039,.042],GREEN,24)]
    # Collar color is assigned on the unified neck below.
    p += [loft('Mallard head',[(-.17,.365,.007,.013),(-.19,.38,.038,.05),(-.23,.382,.052,.056),(-.27,.374,.044,.045),(-.29,.357,.032,.025)],GREEN)]
    unified=join(p,'Continuous mallard anatomy');mod=unified.modifiers.new('Merge breast neck head','REMESH');mod.mode='VOXEL';mod.voxel_size=.003;apply(unified,mod);mod=unified.modifiers.new('Smooth feather volumes','SMOOTH');mod.factor=.7;mod.iterations=5;apply(unified,mod);mod=unified.modifiers.new('Body budget','DECIMATE');mod.ratio=6600/sum(len(f.vertices)-2 for f in unified.data.polygons);apply(unified,mod)
    # Bake continuous feather-color transitions so LOD decimation cannot jag the neck ring.
    bpy.context.view_layer.objects.active=unified
    ca=unified.data.color_attributes.new(name='Plumage',type='FLOAT_COLOR',domain='CORNER')
    def smooth(a,b,x):
        t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
    for f in unified.data.polygons:
        f.use_smooth=True
        for li in f.loop_indices:
            v=unified.data.vertices[unified.data.loops[li].vertex_index].co
            c=Vector(GREY.diffuse_color[:3]).lerp(Vector(BROWN.diffuse_color[:3]),(1-smooth(-.145,-.115,v.y))*smooth(.115,.16,v.z))
            c=c.lerp(Vector(GREEN.diffuse_color[:3]),smooth(.275,.28,v.z))
            ring=smooth(.263,.267,v.z)*(1-smooth(.275,.28,v.z))*(1-smooth(-.15,-.13,v.y))
            c=c.lerp(Vector(IVORY.diffuse_color[:3]),ring);ca.data[li].color=(*c,1)
    m=mat('MallardPlumage',(1,1,1),.72);unified.data.materials.clear();unified.data.materials.append(m);nodes=m.node_tree.nodes;links=m.node_tree.links;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');em=nodes.new('ShaderNodeEmission');vc=nodes.new('ShaderNodeVertexColor');vc.layer_name='Plumage';links.new(vc.outputs['Color'],em.inputs[0]);links.new(em.outputs[0],out.inputs[0])
    # Evaluate bands per bake pixel, independent of the decimated body's triangle shape.
    geom=nodes.new('ShaderNodeNewGeometry');xyz=nodes.new('ShaderNodeSeparateXYZ');links.new(geom.outputs['Position'],xyz.inputs[0])
    def compare(socket,operation,value):
        n=nodes.new('ShaderNodeMath');n.operation=operation;links.new(socket,n.inputs[0]);n.inputs[1].default_value=value;return n.outputs[0]
    def multiply(a,b):
        n=nodes.new('ShaderNodeMath');n.operation='MULTIPLY';links.new(a,n.inputs[0]);links.new(b,n.inputs[1]);return n.outputs[0]
    def mix(a,b,factor):
        n=nodes.new('ShaderNodeMixRGB');n.blend_type='MIX';links.new(factor,n.inputs[0])
        if hasattr(a,'node'):links.new(a,n.inputs[1])
        else:n.inputs[1].default_value=a.diffuse_color
        n.inputs[2].default_value=b.diffuse_color;return n.outputs[0]
    front=compare(xyz.outputs['Y'],'LESS_THAN',-.13);chest=multiply(front,compare(xyz.outputs['Z'],'GREATER_THAN',.13));color=mix(GREY,BROWN,chest);color=mix(color,GREEN,compare(xyz.outputs['Z'],'GREATER_THAN',.28));ring=multiply(front,multiply(compare(xyz.outputs['Z'],'GREATER_THAN',.267),compare(xyz.outputs['Z'],'LESS_THAN',.28)));color=mix(color,IVORY,ring);links.new(color,em.inputs[0])
    im=bpy.data.images.new('MallardPlumage_BaseColor',width=1024,height=1024);im.filepath_raw=str(OUT/'MallardPlumage_BaseColor.png');im.file_format='PNG';tex=nodes.new('ShaderNodeTexImage');tex.image=im;nodes.active=tex
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.0,island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT');sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=1;sc.render.bake.margin=12;bpy.ops.object.bake(type='EMIT');im.save()
    nodes.remove(em);nodes.remove(vc);bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.72;links.new(tex.outputs['Color'],bs.inputs['Base Color']);links.new(bs.outputs[0],out.inputs[0])
    p=[unified]
    p += [loft('Flattened spatulate bill',[(-.28,.354,.026,.014),(-.31,.35,.034,.012),(-.36,.344,.038,.008),(-.382,.344,.03,.007),(-.39,.344,.005,.002)],BILL,20)]
    for s in [-1,1]:
        p += [ell('Eye rim',(s*.046,-.255,.389),(.010,.013,.013),BROWN),ell('Eye',(s*.051,-.258,.390),(.007,.009,.010),BLACK),ell('Catchlight',(s*.055,-.262,.394),(.002,.0025,.0025),GLINT,12,8),ell('Nostril',(s*.014,-.322,.361),(.004,.008,.0015),BLACK,12,8)]
        wing=loft('Folded wing',[(-.095,.21,.008,.01),(-.045,.222,.032,.047),(.065,.212,.035,.046),(.17,.184,.027,.03),(.27,.159,.003,.003)],WING,20);wing.location.x=s*.115;p += [wing]
        for i in range(7):
            p += [feather('Flight feather',(s*(.092+.005*i),.00+.018*i,.258-.005*i),(s*(.048+.006*i),.28-.010*i,.176),.012,.003,FEATHER if i%3==0 else WING)]
        p += [feather('White speculum border',(s*.142,.027,.248),(s*.116,.16,.215),.033,.003,IVORY),feather('Blue speculum',(s*.145,.042,.253),(s*.12,.15,.224),.026,.004,BLUE)]
        p += [feather('Tail feather',(s*.025,.19,.17),(s*.045,.35,.174),.022,.003,IVORY)]
        for j in range(3):p += [tube('Webbed toe',[(s*.068,.04,.045),(s*.068+(j-1)*.021,-.045,.006)],[.009,.004],BILL)]
        p += [mesh('Webbed foot',[(s*.068,.04,.042),(s*.068-.025,-.045,.007),(s*.068+.025,-.045,.007)],[(0,1,2)],BILL)]
    p += [tube('Drake curled tail',[(0,.25,.181),(0,.30,.206),(0,.31,.226),(0,.285,.23),(0,.278,.212)],[.008,.009,.008,.006,.002],BLACK,8)]
    return join(p,'Duck_LOD0')

def beaver():
    body=loft('Pear-shaped torso',[(.35,.235,.008,.01),(.30,.24,.12,.17),(.19,.255,.22,.237),(.04,.265,.235,.255),(-.09,.26,.205,.24),(-.20,.25,.16,.195),(-.27,.24,.10,.12)],FUR,40)
    skull=loft('Broad blunt skull',[(-.16,.30,.09,.10),(-.24,.33,.135,.13),(-.33,.32,.145,.12),(-.405,.28,.118,.077),(-.44,.27,.07,.048)],FUR,36)
    vols=[body,skull]
    for s in [-1,1]:
        vols += [ell('Hip',(s*.17,.15,.14),(.10,.16,.13),FUR),tube('Forearm',[(s*.14,-.16,.24),(s*.16,-.23,.13),(s*.17,-.30,.064)],[.054,.042,.030],FUR,14)]
    base=join(vols,'Unified beaver');mod=base.modifiers.new('Continuous anatomy','REMESH');mod.mode='VOXEL';mod.voxel_size=.007;apply(base,mod);mod=base.modifiers.new('Relax surface','SMOOTH');mod.factor=.7;mod.iterations=4;apply(base,mod);mod=base.modifiers.new('Body triangle budget','DECIMATE');mod.ratio=6200/sum(len(f.vertices)-2 for f in base.data.polygons);apply(base,mod)
    p=[base,loft('Paddle tail',[(.24,.046,.075,.021),(.36,.031,.104,.025),(.49,.028,.127,.026),(.63,.027,.112,.024),(.73,.026,.067,.019),(.77,.025,.008,.006)],TAIL,30)]
    # Intersecting fine scale ridges follow the flat paddle, visibly distinct from fur.
    for i in range(10):
        y=.39+i*.033;w=.108*math.sin((y-.30)/.49*math.pi)**.5
        p += [tube('Tail scale chevron',[(-w,y,.045),(0,y+.03,.056),(w,y,.045)],[.0015,.0018,.0015],FUR,5)]
    for s in [-1,1]:
        p += [ell('Ear',(s*.112,-.218,.416),(.037,.027,.040),FUR),ell('Ear hollow',(s*.116,-.241,.423),(.023,.008,.025),EAR)]
        p += [ell('Soft cheek',(s*.049,-.411,.279),(.059,.039,.044),MUZZLE),ell('Eye socket',(s*.119,-.34,.36),(.022,.030,.025),MUZZLE),ell('Alert eye',(s*.132,-.350,.365),(.014,.016,.016),BLACK),ell('Catchlight',(s*.138,-.359,.371),(.0035,.0035,.0035),GLINT,12,8)]
        p += [ell('Hind webbed foot',(s*.183,.055,.029),(.065,.126,.029),TAIL),ell('Fore paw',(s*.17,-.32,.037),(.043,.062,.031),FUR)]
        for i in range(4):
            x=s*.17+(i-1.5)*.018
            p += [tube('Fore finger',[(x,-.337,.041),(x,-.378,.028),(x,-.39,.020)],[.011,.008,.003],MUZZLE,7),tube('Hind toe',[(s*.183+(i-1.5)*.024,.005,.043),(s*.183+(i-1.5)*.024,-.061,.018)],[.009,.003],MUZZLE,7)]
        for i in range(4):
            p += [tube('Whisker',[(s*.05,-.437,.286),(s*.13,-.455,.294+(i-1.5)*.014),(s*.20,-.449,.299+(i-1.5)*.023)],[.0013,.0008,.0001],MUZZLE,5)]
        p += [loft('Orange incisor',[(-.431,.248,.013,.020),(-.449,.245,.013,.019)],TOOTH,12)]
        p[-1].location.x=s*.014
    p += [ell('Leathery nose',(0,-.452,.302),(.035,.017,.021),BLACK),tube('Mouth crease',[(-.035,-.443,.256),(0,-.453,.251),(.035,-.443,.256)],[.0017,.002,.0017],BLACK,6)]
    return join(p,'Beaver_LOD0')

stats={};highs=[]
for name,fn in [('Duck',duck),('Beaver',beaver)]:
    high=fn();bpy.context.view_layer.objects.active=high
    for f in high.data.polygons:f.use_smooth=True
    count=sum(len(p.vertices)-2 for p in high.data.polygons)
    if count>13800:
        m=high.modifiers.new('Final budget','DECIMATE');m.ratio=13800/count;apply(high,m)
    # Place underside on zero plane and all exported object transforms at identity.
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bottom=min(v.co.z for v in high.data.vertices)
    for v in high.data.vertices:v.co.z-=bottom
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    low=high.copy();low.data=high.data.copy();bpy.context.collection.objects.link(low);low.name=name+'_LOD1';m=low.modifiers.new('Distance simplification','DECIMATE');m.ratio=2350/sum(len(p.vertices)-2 for p in low.data.polygons);apply(low,m)
    bpy.ops.object.select_all(action='DESELECT');high.select_set(True);low.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
    stats[name]={'triangles':{o.name:sum(len(p.vertices)-2 for p in o.data.polygons) for o in [high,low]},'unity_dimensions_m':{'width':high.dimensions.x,'height':high.dimensions.z,'length':high.dimensions.y},'forward':'Unity +Z; Blender -Y','origin':'underside; identity object transform','materials':[m.name for m in high.data.materials]}
    low.hide_render=True;low.hide_set(True);highs.append(high)
(SRC/'mesh-stats.json').write_text(json.dumps(stats,indent=2)+'\n')
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Studio ground';floor.data.materials.append(mat('StudioSage',(.12,.17,.14)))
sc=bpy.context.scene;sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.60,.72,.85,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.35
for loc,power,size in [((-3,-4,5),450,4),((3,2,4),550,3)]:
    bpy.ops.object.light_add(type='AREA',location=loc);bpy.context.object.data.energy=power;bpy.context.object.data.size=size
bpy.ops.object.camera_add();cam=bpy.context.object;cam.data.type='ORTHO';sc.camera=cam
sc.render.engine='CYCLES';sc.cycles.samples=40;sc.render.resolution_x=1200;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.view_settings.view_transform='AgX'
for i,name in enumerate(['Duck','Beaver']):
    for j,o in enumerate(highs):o.hide_render=j!=i
    cam.location=(1.3,-1.7,.92) if i==0 else (1.5,-1.8,1.1);target=Vector((0,-.03,.20)) if i==0 else Vector((0,.06,.23));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.95 if i==0 else 1.45
    sc.render.filepath=str(SRC/(name.lower()+'-studio.png'));bpy.ops.render.render(write_still=True)
for o in highs:o.hide_render=False
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'RiverWildlife.blend'))
backup=SRC/'RiverWildlife.blend1'
if backup.exists():backup.unlink()
print('RIVER_WILDLIFE_STATS',json.dumps(stats))
