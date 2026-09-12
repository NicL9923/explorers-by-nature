"""Original woodland meshes/textures. Run with blender --background --python this-file."""
from pathlib import Path
import math, random, json
import bpy
import numpy as np
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
OUT=R/'Assets/Resources/Woodland'; SRC=R/'ArtSource/Woodland'
OUT.mkdir(parents=True,exist_ok=True); SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
N=512
y,x=np.mgrid[0:N,0:N].astype(float); x/=N-1; y/=N-1
rng=np.random.default_rng(274)

def save_texture(name,rgb,alpha=None):
    a=np.ones((N,N,4),dtype=np.float32); a[:,:,:3]=np.clip(rgb,0,1)
    if alpha is not None:a[:,:,3]=alpha
    im=bpy.data.images.new(name,width=N,height=N,alpha=True); im.pixels.foreach_set(a.ravel()); im.filepath_raw=str(OUT/(name+'.png')); im.file_format='PNG'; im.save(); return im

def segment(a,b,width):
    dx=b[0]-a[0];dy=b[1]-a[1];t=np.clip(((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy),0,1)
    return ((x-a[0]-t*dx)**2+(y-a[1]-t*dy)**2)<width*width

noise=rng.random((N,N))
ridges=(np.sin(x*160+np.sin(y*21)*.7)+np.sin(x*349+y*3))*.025
fissure=(np.sin(x*110+np.sin(y*8)*.35)>0.90)
pinebark=np.stack([.27+ridges,.17+ridges*.8,.105+ridges*.5],axis=-1)+(noise[:,:,None]-.5)*.065
pinebark[fissure]*=.48
pinetex=save_texture('PineBark',pinebark)
marks=np.zeros((N,N),dtype=bool)
for _ in range(65):
    cx,cy=rng.random(2);w=rng.uniform(.015,.085);h=rng.uniform(.0015,.006)
    marks |= (((x-cx)/w)**2+((y-cy)/h)**2)<1
aspenbark=np.stack([.78+noise*.09,.77+noise*.08,.67+noise*.08],axis=-1); aspenbark[marks]=[.18,.20,.16]
aspentex=save_texture('AspenBark',aspenbark)
mask=np.zeros((N,N),dtype=bool)
mask|=segment((.5,.02),(.5,.95),.006)
pr=random.Random(711)
for i in range(39):
    v=.12+i*.020
    for sign in [-1,1]:
        tip=(.5+sign*pr.uniform(.10,.34)*(1-.43*v),v+pr.uniform(.08,.19))
        base=(.5,v-.02)
        mask|=segment(base,tip,.0034)
        for t in [.25,.48,.7]:
            bx=base[0]+(tip[0]-base[0])*t;by=base[1]+(tip[1]-base[1])*t
            mask|=segment((bx,by),(bx+sign*.046,by+.11),.0028)
rgb=np.stack([.12+noise*.08+y*.06,.24+noise*.12+y*.09,.075+noise*.035+y*.03],axis=-1)
needletex=save_texture('PineNeedles',rgb,mask.astype(float))
distant=mask.copy()
for dy in range(-3,4):
    for dx in range(-3,4):distant|=np.roll(np.roll(mask,dy,axis=0),dx,axis=1)
distanttex=save_texture('PineNeedlesDistant',rgb,distant.astype(float))
# Cordate aspen leaf with fine serration and radiating pale veins.
u=(x-.5)/.45;v=(y-.46)/.47
radius=np.sqrt(u*u+v*v); angle=np.arctan2(v,u)
leaf=radius<(1-.10*np.cos(angle*3))*(1+.025*np.sin(angle*36))
leaf &= y>.055
leaf |= segment((.5,.01),(.5,.25),.009)
vein=(np.abs(x-.5)<.007)
for h in [.22,.35,.48,.61,.72]:
    for s in [-1,1]:vein|=segment((.5,h),(.5+s*(.38-(h-.4)**2),h+.17),.003)
rgb=np.stack([.24+noise*.05+y*.09,.38+noise*.07+y*.11,.075+noise*.04+y*.025],axis=-1);rgb[vein]*=1.2
leaftex=save_texture('AspenLeaves',rgb,leaf.astype(float))

def material(name,image,foliage=False):
    m=bpy.data.materials.new(name); m.use_nodes=True;n=m.node_tree.nodes;p=n.get('Principled BSDF');p.inputs['Roughness'].default_value=.88
    t=n.new('ShaderNodeTexImage');t.image=image;m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
    if foliage:
        m.node_tree.links.new(t.outputs['Alpha'],p.inputs['Alpha']);p.inputs['Subsurface Weight'].default_value=.10
        m.surface_render_method='DITHERED';m.use_transparency_overlap=False
    return m
PB=material('PineBark',pinetex);PN=material('PineNeedles',needletex,True);AB=material('AspenBark',aspentex);AL=material('AspenLeaves',leaftex,True)

PD=material('PineNeedlesDistant',distanttex,True)

class Mesh:
    def __init__(self):self.v=[];self.f=[];self.mi=[];self.uv=[]
    def face(self,vs,uv,mat):
        j=len(self.v);self.v.extend(vs);self.f.append(tuple(range(j,j+len(vs))));self.uv.extend(uv);self.mi.append(mat)
    def tube(self,points,radii,sides=7,mat=0):
        for q in range(len(points)-1):
            p0,p1=Vector(points[q]),Vector(points[q+1]);d=(p1-p0).normalized();a=d.cross(Vector((0,1,0))).normalized();b=d.cross(a).normalized()
            for k in range(sides):
                t0=k*math.tau/sides;t1=(k+1)*math.tau/sides
                vs=[p0+radii[q]*(a*math.cos(t0)+b*math.sin(t0)),p0+radii[q]*(a*math.cos(t1)+b*math.sin(t1)),p1+radii[q+1]*(a*math.cos(t1)+b*math.sin(t1)),p1+radii[q+1]*(a*math.cos(t0)+b*math.sin(t0))]
                self.face(vs,[(k/sides,q/3),((k+1)/sides,q/3),((k+1)/sides,(q+1)/3),(k/sides,(q+1)/3)],mat)
    def card(self,center,direction,width,length,roll=0):
        d=Vector(direction).normalized();side=d.cross(Vector((0,0,1)))
        if side.length<.01:side=Vector((1,0,0))
        side.normalize();normal=side.cross(d).normalized();s=(side*math.cos(roll)+normal*math.sin(roll))*width/2;c=Vector(center);e=d*length
        self.face([c-s,c+s,c+e+s,c+e-s],[(0,0),(1,0),(1,1),(0,1)],1)
    def object(self,name,mats):
        me=bpy.data.meshes.new(name);me.from_pydata(self.v,[],self.f);me.update();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob)
        for m in mats:me.materials.append(m)
        uv=me.uv_layers.new(name='UVMap')
        for p,mi in zip(me.polygons,self.mi):
            p.material_index=mi;p.use_smooth=mi==0
            for li in p.loop_indices:uv.data[li].uv=self.uv[li]
        return ob

def pine(lod):
    r=random.Random(390);m=Mesh(); sides=[10,7,5][lod]
    points=[(.10*math.sin(i*.7),.08*math.cos(i*.9)-.08,i) for i in range(11)]
    m.tube(points,[.24*(1-i/11)**1.3+.012 for i in range(11)],sides)
    for tier in range(9):
        z=1.7+tier*.88; spread=2.5*(1-tier/10)**.8
        for arm in range(7):
            theta=arm*math.tau/7+tier*2.39+r.uniform(-.21,.21);d=Vector((math.cos(theta),math.sin(theta),0));length=spread*r.uniform(.76,1.1)
            zj=z+r.uniform(-.28,.28);length*=r.uniform(.8,1.15)
            a=Vector((0,0,zj));b=a+d*length*.52+Vector((0,0,-.16));c=a+d*length+Vector((0,0,.30))
            if lod<2:m.tube([a,b,c],[.065*(1-tier/12),.03,.008],max(4,sides-3))
            fronds=[15,8,2][lod]
            for j in range(fronds):
                t=(j+.6)/fronds;pos=a+d*length*t+Vector((0,0,-.18*math.sin(t*math.pi)+.26*t))
                side=Vector((-d.y,d.x,0));sgn=1 if j%2 else -1
                direction=(d*.4+side*sgn*.65+Vector((0,0,.30+r.random()*.45))).normalized()
                sz=r.uniform(.62,.95)*(1-tier*.035)
                if lod==1:sz*=1.40
                if lod==2:sz*=1.80
                m.card(pos,direction,sz*.82,sz,r.uniform(-.5,.5))
                m.card(pos,direction,sz*.82,sz,1.5+r.uniform(-.3,.3))
    for k in range([15,9,4][lod]):
        theta=k*2.39;z=9.0+(k%5)*.16
        m.card((0,0,z),(math.cos(theta)*.3,math.sin(theta)*.3,1),.30,.65,k*.8)
    return m.object('Pine_LOD'+str(lod),[PB,PD if lod==2 else PN])

def aspen(lod):
    r=random.Random(807);m=Mesh();sides=[10,7,5][lod]
    points=[(.11*math.sin(i*.7),.10*math.sin(i*.43),i*.8) for i in range(11)]
    m.tube(points,[.17*(1-i/11)**1.4+.007 for i in range(11)],sides)
    for branch in range(26):
        z=2.8+branch*.18;angle=branch*2.399+r.uniform(-.25,.25);d=Vector((math.cos(angle),math.sin(angle),0));length=(1.8-.95*(z-2.8)/5)*r.uniform(.7,1.2)
        a=Vector((.05,0,z));b=a+d*length*.48+Vector((0,0,.45));tip=a+d*length+Vector((0,0,.9))
        if lod<2:m.tube([a,b,tip],[.042,.023,.004],max(4,sides-3))
        for twig in range([6,4,2][lod]):
            t=(twig+1)/[6,4,2][lod];sgn=1 if twig%2 else -1
            center=a.lerp(tip,t)+Vector((-d.y,d.x,.4))*sgn*.35
            if lod==0:m.tube([a.lerp(tip,t*.8),center],[.013,.003],4)
            for leaf in range([12,9,5][lod]):
                phi=r.uniform(0,math.tau);zz=r.uniform(-1,1);rad=r.random()**(1/3)*.58
                delta=Vector((math.cos(phi)*math.sqrt(1-zz*zz),math.sin(phi)*math.sqrt(1-zz*zz),zz))*rad
                pos=center+delta;direction=Vector((r.uniform(-1,1),r.uniform(-1,1),r.uniform(-.3,1)))
                size=r.uniform(.19,.31)*[1,1.35,2.65][lod]
                m.card(pos,direction,size,size*1.13,r.uniform(-math.pi,math.pi))
    return m.object('Aspen_LOD'+str(lod),[AB,AL])

objects=[];stats={}
for fn in [pine,aspen]:
    for lod in range(3):
        ob=fn(lod);objects.append(ob)
        bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
        assert ob.location.length < 1e-6, f'{ob.name}: export origin must be ground zero'
        bpy.ops.export_scene.fbx(filepath=str(OUT/(ob.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
        stats[ob.name]={'triangles':sum(len(p.vertices)-2 for p in ob.data.polygons),'height_m':round(ob.dimensions.z,2),'materials':[m.name for m in ob.data.materials]}
        ob.location.x=(0 if fn==pine else 12)+lod*3.6;ob.hide_render=lod!=0
# A small oxeye-daisy clump with curved petal geometry and lanceolate stem leaves.
def solid(name,color):
    mat=bpy.data.materials.new(name);mat.diffuse_color=(*color,1);mat.use_nodes=True;mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*color,1);mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.8;return mat
FS=solid('WildflowerStem',(.13,.27,.045));FP=solid('WildflowerPetals',(.93,.88,.73));FC=solid('WildflowerCenter',(.89,.48,.025))
for lod in range(3):
    r=random.Random(334);m=Mesh()
    for i in range(7):
        x0=r.uniform(-.22,.22);y0=r.uniform(-.22,.22);height=r.uniform(.34,.65);lean=Vector((r.uniform(-.09,.09),r.uniform(-.09,.09),0));base=Vector((x0,y0,0));top=base+lean+Vector((0,0,height))
        m.tube([base,base.lerp(top,.5),top],[.009,.006,.003],4)
        for j in range([4,3,2][lod]):
            p=base.lerp(top,.22+j*.16);theta=j*2.4+i;d=Vector((math.cos(theta),math.sin(theta),.35));s=Vector((-d.y,d.x,0))*.025;e=p+d*.13
            m.face([p,p+d*.06+s,e,p+d*.06-s],[(0,0),(0,0),(0,0),(0,0)],0)
        petals=[13,10,7][lod]
        for petal in range(petals):
            theta=petal*math.tau/petals;d=Vector((math.cos(theta),math.sin(theta),0));s=Vector((-d.y,d.x,0));p=top+d*.018
            # Tapered spoon petal, raised at base, gently drooping tip.
            vs=[p-s*.004,p+d*.040-s*.012+Vector((0,0,.010)),p+d*.075-s*.008+Vector((0,0,-.003)),p+d*.081+Vector((0,0,-.007)),p+d*.075+s*.008+Vector((0,0,-.003)),p+d*.040+s*.012+Vector((0,0,.010)),p+s*.004]
            m.face(vs,[(0,0)]*7,1)
        for k in range(10):
            a=k*math.tau/10;b=(k+1)*math.tau/10
            m.face([top+Vector((0,0,.019)),top+Vector((math.cos(a)*.027,math.sin(a)*.027,.005)),top+Vector((math.cos(b)*.027,math.sin(b)*.027,.005))],[(0,0)]*3,2)
    ob=m.object('Wildflower_LOD'+str(lod),[FS,FP,FC]);ob.hide_render=True
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
    assert ob.location.length < 1e-6, f'{ob.name}: export origin must be ground zero'
    bpy.ops.export_scene.fbx(filepath=str(OUT/(ob.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,bake_anim=False,add_leaf_bones=False)
    stats[ob.name]={'triangles':sum(len(p.vertices)-2 for p in ob.data.polygons),'height_m':round(ob.dimensions.z,2),'materials':[m.name for m in ob.data.materials]}
    ob.location.x=30+lod
# Save editable source with every LOD but render only high meshes.
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Woodland.blend'))
(SRC/'mesh-stats.json').write_text(json.dumps(stats,indent=2)+'\n')
# Close, legible studio field plate.
objects[0].location.x=-3;objects[3].location.x=3
bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.name='PreviewGround';gm=bpy.data.materials.new('Preview earth');gm.diffuse_color=(.17,.20,.13,1);ground.data.materials.append(gm)
world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.52,.66,.78,1);world.node_tree.nodes['Background'].inputs[1].default_value=.35
bpy.ops.object.light_add(type='AREA',location=(-6,-8,14));bpy.context.object.data.energy=2200;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=9
bpy.ops.object.light_add(type='SUN',location=(4,5,12));bpy.context.object.rotation_euler=(.3,-.6,-.5);bpy.context.object.data.energy=2
bpy.ops.object.camera_add(location=(13,-25,11));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=16.4;bpy.context.scene.camera=cam
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=32;sc.render.resolution_x=1500;sc.render.resolution_y=1150;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.filepath=str(SRC/'woodland-contact-sheet.png')
sc.view_settings.view_transform='Standard';bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Woodland.blend'));bpy.ops.render.render(write_still=True)
objects[0].hide_render=True;objects[3].hide_render=True
flower=bpy.data.objects['Wildflower_LOD0'];flower.hide_render=False;flower.location.x=0
cam.location=(1.1,-1.8,1.15);cam.rotation_euler=(Vector((0,0,.3))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.95
sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.filepath=str(SRC/'wildflower-detail.png');bpy.ops.render.render(write_still=True)
flower.hide_render=True
for i,ob in enumerate(objects):
    ob.hide_render=False;ob.location.x=i*5.6
cam.location=(14,-35,12);cam.rotation_euler=(Vector((14,0,4.8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=34
sc.render.resolution_x=2040;sc.render.resolution_y=800;sc.render.filepath=str(SRC/'woodland-lod-sheet.png');bpy.ops.render.render(write_still=True)
backup=SRC/'Woodland.blend1'
if backup.exists():backup.unlink()
print('WOODLAND_STATS',json.dumps(stats))
