"""Original sculpted ranch animals. Run: blender -b -t 4 --python .agents/tools/make-ranch-art.py.
FBX files contain explicit LOD0/LOD1 meshes; the blends also contain a portrait studio.
No downloaded meshes, textures, brushes, or other third-party art are used.
"""
import bpy, math, pathlib, json, os, tempfile, sys
from mathutils import Vector
# Fedora's Blender 5.2 package can ship a 2.5 OCIO profile with a 2.4 library.
# Re-exec with a temporary compatible copy; never modify installed data files.
try:
 bpy.context.scene.view_settings.view_transform='AgX'
except TypeError:
 config=pathlib.Path('/usr/share/blender/5.2/datafiles/colormanagement/config.ocio')
 if config.exists() and not os.environ.get('RANCH_OCIO_RETRY'):
  text=config.read_text().replace('ocio_profile_version: 2.5','ocio_profile_version: 2.4')
  text=text.replace('search_path: "icc:luts:filmic"',f'search_path: "{config.parent}/icc:{config.parent}/luts:{config.parent}/filmic"')
  fd,path=tempfile.mkstemp(prefix='ranch-ocio-',suffix='.ocio')
  with os.fdopen(fd,'w') as f:f.write(text)
  os.environ['OCIO']=path;os.environ['RANCH_OCIO_RETRY']='1'
  os.execv(bpy.app.binary_path,[bpy.app.binary_path,'-b','-t','4','--python',str(pathlib.Path(__file__).resolve())]+(sys.argv[sys.argv.index('--'):] if '--' in sys.argv else []))
 else:
  raise RuntimeError('A working OpenColorIO configuration is required for the sRGB coat bake.')
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource'; OUT.mkdir(exist_ok=True)
REPORT={}
M={}
PALETTE={
 'JerseyFawn':(.48,.265,.115), 'JerseyGolden':(.59,.355,.17),
 'JerseyCream':(.77,.61,.39), 'JerseySable':(.20,.095,.038),
 'MuzzleVelvet':(.13,.095,.069), 'Nostril':(.026,.019,.015),
 'HornIvory':(.74,.65,.46), 'HornTip':(.20,.13,.065),
 'Hoof':(.10,.075,.05), 'EarRose':(.37,.19,.125),
 'UdderRose':(.62,.37,.265), 'EyeAmber':(.23,.09,.022),
 'EyeBlack':(.011,.014,.013), 'EyeGlint':(.91,.87,.71),
 'HenBuff':(.69,.405,.16), 'HenGold':(.74,.45,.19),
 'HenCream':(.79,.53,.26), 'HenUmber':(.29,.14,.055),
 'HenTail':(.08,.11,.10), 'CombRed':(.60,.055,.033),
 'BeakGold':(.66,.36,.09), 'ClawIvory':(.63,.52,.31),
}
def reset():
 for o in list(bpy.data.objects): bpy.data.objects.remove(o,do_unlink=True)
 for m in list(bpy.data.materials): bpy.data.materials.remove(m)
 M.clear()
 for n,c in PALETTE.items():
  m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True
  p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.77
  if n.startswith('Eye'):p.inputs['Roughness'].default_value=.20
  M[n]=m

def finish(o,n,mat):
 o.name=n;o.data.materials.append(M[mat])
 for p in o.data.polygons:p.use_smooth=True
 return o

def ell(n,p,s,mat,rot=(0,0,0)):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=p)
 o=bpy.context.object;o.scale=s;o.rotation_euler=rot
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return finish(o,n,mat)

def join(obs,n):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=obs[0];o.name=n
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 return o

def apply(o,mod):
 bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)

def sculpt(obs,n,voxel=.027,target=9000):
 o=join(obs,n);m=o.modifiers.new('Unified anatomical surface','REMESH');m.mode='VOXEL';m.voxel_size=voxel;apply(o,m)
 m=o.modifiers.new('Relax sculpt','SMOOTH');m.factor=1.1;m.iterations=5;apply(o,m)
 count=sum(len(p.vertices)-2 for p in o.data.polygons)
 if count>target:
  m=o.modifiers.new('Retain sculpt silhouette','DECIMATE');m.ratio=target/count;apply(o,m)
 for p in o.data.polygons:p.use_smooth=True
 return o

def tube(n,points,radii,mat,sides=12):
 verts=[];faces=[]
 for i,p in enumerate(points):
  tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
  q=tangent.to_track_quat('Z','Y')
  for j in range(sides):
   v=Vector((radii[i]*math.cos(j*math.tau/sides),radii[i]*math.sin(j*math.tau/sides),0));verts.append(Vector(p)+q@v)
 for i in range(len(points)-1):
  for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
 faces.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))])
 mesh=bpy.data.meshes.new(n);mesh.from_pydata(verts,[],faces);o=bpy.data.objects.new(n,mesh);bpy.context.collection.objects.link(o);return finish(o,n,mat)

def leaf(n,base,tip,width,mat,normal=(0,-1,0),bend=.025):
 a=Vector(base);d=Vector(tip)-a;normal=Vector(normal).normalized();side=d.cross(normal).normalized()
 verts=[];faces=[];rows=9
 for k in range(rows):
  t=k/(rows-1);w=width*(math.sin(math.pi*t)**.72)*.5
  for u in [-1,-.5,0,.5,1]:
   verts.append(a+d*t+side*w*u+normal*(bend*math.sin(t*math.pi)+(1-u*u)*width*.04*math.sin(t*math.pi)))
 for k in range(rows-1):
  for j in range(4):a0=k*5+j;faces.append((a0,a0+1,a0+6,a0+5))
 mesh=bpy.data.meshes.new(n);mesh.from_pydata(verts,[],faces);o=bpy.data.objects.new(n,mesh);bpy.context.collection.objects.link(o);finish(o,n,mat)
 m=o.modifiers.new('Feather thickness','SOLIDIFY');m.thickness=.0018;apply(o,m)
 return o

def hoof(x,y):
 for s in [-1,1]:
  bpy.ops.mesh.primitive_cube_add(size=1,location=(x+s*.038,y-.018,.10));o=bpy.context.object;o.scale=(.073,.18,.13);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
  finish(o,'Cloven hoof','Hoof')
  for v in o.data.vertices:
   if v.co.z>0:v.co.x*=.8;v.co.y*=.77
  m=o.modifiers.new('Worn hoof edges','BEVEL');m.width=.029;m.segments=3;apply(o,m)

def bake_coat(o):
 """Bake smoothly blended original coat colors into one portable 1024px atlas."""
 from mathutils import noise
 def smooth(a,b,x):
  t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
 def mix(a,b,t):return tuple(x*(1-t)+y*t for x,y in zip(a,b))
 attr=o.data.color_attributes.new(name='Coat pigment',type='FLOAT_COLOR',domain='POINT')
 for v,c in zip(o.data.vertices,attr.data):
  p=o.matrix_world@v.co;n=noise.noise_vector(p*8)[0]
  col=mix(PALETTE['JerseyFawn'],PALETTE['JerseyGolden'],smooth(1.1,1.6,p.z)*.7)
  belly=(1-smooth(.88,1.05,p.z))*(1-smooth(.26,.39,abs(p.x)))*smooth(-.35,-.05,p.y)*(1-smooth(.55,.7,p.y))
  col=mix(col,PALETTE['JerseyCream'],belly*.85)
  face=(1-smooth(-1.15,-.91,p.y))*(1-smooth(1.4,1.69,p.z))
  col=mix(col,PALETTE['JerseySable'],face*.85)
  c.color=tuple(max(0,x*(1+n*.045)) for x in col)+(1,)
 o.data.materials.clear();m=bpy.data.materials.new('JerseyCoat');m.use_nodes=True;m.diffuse_color=(*PALETTE['JerseyFawn'],1);o.data.materials.append(m)
 nodes=m.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');em=nodes.new('ShaderNodeEmission');vc=nodes.new('ShaderNodeVertexColor');vc.layer_name=attr.name
 m.node_tree.links.new(vc.outputs['Color'],em.inputs[0]);m.node_tree.links.new(em.outputs[0],out.inputs[0])
 image=bpy.data.images.new('JerseyCoat_BaseColor',width=1024,height=1024)
 tex=nodes.new('ShaderNodeTexImage');tex.image=image;nodes.active=tex
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1;scene.render.bake.margin=12
 bpy.ops.object.bake(type='EMIT')
 folder=ROOT/'Assets/Resources/Animals';folder.mkdir(exist_ok=True)
 image.filepath_raw=str(folder/'JerseyCoat_BaseColor.png');image.file_format='PNG';image.save();image.pack()
 nodes.remove(em);nodes.remove(vc);bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.83
 m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);m.node_tree.links.new(bs.outputs[0],out.inputs[0])

def cow():
 parts=[]
 def e(n,p,s):parts.append(ell(n,p,s,'JerseyFawn'))
 e('Ribcage',(0,.05,1.13),(.40,.76,.42));e('Barrel',(0,.18,1.02),(.41,.54,.39));e('Withers',(0,-.43,1.35),(.27,.28,.24));e('Rump',(0,.61,1.16),(.36,.30,.28))
 e('Neck',(0,-.62,1.36),(.255,.34,.35));e('Raised neck',(0,-.77,1.48),(.205,.24,.33))
 e('Skull',(0,-.95,1.61),(.192,.225,.24));e('Face bridge',(0,-1.09,1.43),(.18,.22,.28));e('Jaw',(0,-1.025,1.35),(.205,.19,.19))
 e('Dewlap',(0,-.69,1.12),(.10,.22,.28))
 for s in [-1,1]:
  e('Front shoulder',(s*.275,-.40,1.13),(.145,.235,.29))
  e('Foreleg',(s*.285,-.44,.78),(.085,.10,.31));e('Front knee',(s*.29,-.46,.48),(.075,.083,.10));e('Front cannon',(s*.29,-.45,.30),(.053,.064,.22))
  e('Haunch',(s*.26,.55,1.04),(.165,.23,.30));e('Thigh',(s*.275,.49,.77),(.10,.125,.22));e('Hock',(s*.29,.65,.49),(.072,.083,.13));e('Rear cannon',(s*.29,.61,.29),(.05,.06,.21))
  parts.append(tube('Continuous rear gaskin',[(s*.275,.49,.77),(s*.285,.57,.63),(s*.29,.65,.49)],[.079,.065,.064],'JerseyFawn',16))
 body=sculpt(parts,'Clover continuous sculpt',.016,12500)
 bake_coat(body)
 ell('Soft charcoal nose',(0,-1.255,1.245),(.21,.13,.13),'MuzzleVelvet')
 ell('Pale muzzle surround',(0,-1.21,1.29),(.22,.115,.151),'JerseyCream')
 ell('Velvet nose pad',(0,-1.29,1.26),(.197,.104,.114),'MuzzleVelvet')
 tube('Lower lip',[(-.15,-1.335,1.201),(0,-1.37,1.19),(.15,-1.335,1.201)],[.009]*3,'Nostril')
 for s in [-1,1]:
  ell('Inset nostril',(s*.115,-1.373,1.285),(.041,.018,.026),'Nostril',(.0,s*.25,s*.2))
  ell('Eye socket',(s*.184,-1.045,1.62),(.024,.069,.06),'JerseySable')
  ell('Eye amber',(s*.205,-1.065,1.624),(.016,.04,.038),'EyeAmber')
  ell('Gentle pupil',(s*.218,-1.074,1.627),(.006,.025,.027),'EyeBlack')
  ell('Catchlight',(s*.224,-1.085,1.641),(.003,.007,.007),'EyeGlint')
  tube('Upper eyelid',[(s*.199,-1.103,1.641),(s*.22,-1.074,1.66),(s*.195,-1.023,1.654)],[.008,.010,.007],'JerseySable')
  tube('Lower eyelid',[(s*.199,-1.103,1.614),(s*.22,-1.074,1.597),(s*.195,-1.023,1.62)],[.004,.006,.004],'JerseySable')
  for j in range(4):
   yy=-1.093+j*.016
   tube('Upper eyelashes',[(s*.217,yy,1.65),(s*.238,yy-.004,1.668)],[.002,.0003],'EyeBlack',5)
  leaf('Velvet ear',(s*.17,-.91,1.72),(s*.53,-.86,1.76),.205,'JerseyFawn',(0,-1,.4),.035)
  leaf('Ear hollow',(s*.245,-.948,1.735),(s*.49,-.899,1.759),.125,'EarRose',(0,-1,.4),.015)
  tube('Small ivory horn',[(s*.135,-.83,1.79),(s*.21,-.81,1.88),(s*.25,-.79,1.98),(s*.24,-.81,2.035)],[.055,.044,.021,.001],'HornIvory')
  for y in [-.45,.61]:hoof(s*.29,y)
 udder=ell('Udder',(0,.43,.69),(.195,.215,.125),'UdderRose')
 for x in [-.09,.09]:
  for y in [.34,.49]:tube('Teat',[(x,y,.65),(x,y,.54),(x*.97,y-.005,.52)],[.029,.023,.013],'UdderRose')
 tube('Relaxed tail',[(0,.80,1.38),(.025,.9,1.18),(.045,.94,.90),(.09,.96,.65),(.13,.97,.52)],[.036,.031,.025,.021,.018],'JerseyFawn')
 ell('Tail switch',(.14,.975,.43),(.057,.052,.145),'JerseySable',(0,.16,0))
 for j in range(7):
  a=j*math.tau/7;tube('Tail hair',[(.14+.035*math.cos(a),.975+.032*math.sin(a),.49),(.16+.04*math.cos(a),.98+.03*math.sin(a),.34)],[.007,.001],'JerseySable',6)
 # Low relief forehead forelock, with a softly scalloped lower edge.
 for j in range(7):
  x=(j-3)*.032;leaf('Forelock',(x,-1.025,1.815),(x*.8,-1.15,1.73-abs(j-3)*.006),.05,'JerseyGolden',(0,-1,0),.012)

def hen():
 body=sculpt([ell('Body',(0,.035,.40),(.22,.285,.23),'HenBuff'),ell('Breast',(0,-.13,.46),(.195,.20,.245),'HenBuff'),ell('Neck',(0,-.215,.64),(.10,.11,.19),'HenBuff'),ell('Head',(0,-.255,.77),(.10,.117,.113),'HenBuff')],'Hen continuous sculpt',.011,3300)
 for s in [-1,1]:
  # Shingled wing coverts and flight feathers follow the curved flanks.
  for row in range(5):
   for j in range(7):
    y=-.15+j*.042+row*.01;z=.56-row*.039
    x=s*(.198+math.sin(j/6*math.pi)*.032)
    leaf('Wing covert',(x,y,z),(x+s*.004,y+.115,z-.060),.063,'HenGold' if (j+row)%3==0 else 'HenBuff',(s,0,.25),.003)
  for j in range(7):
   leaf('Layered primary',(s*(.19-j*.007),.005+j*.026,.39-j*.008),(s*(.17-j*.011),.245+j*.014,.29+j*.006),.075,'HenUmber' if j%3==0 else 'HenBuff',(s,0,-.1),.018)
  ell('Face skin',(s*.085,-.284,.785),(.009,.054,.049),'CombRed')
  ell('Eye surround',(s*.092,-.297,.802),(.008,.026,.028),'HenGold')
  ell('Eye',(s*.099,-.302,.803),(.006,.020,.021),'EyeAmber')
  ell('Pupil',(s*.104,-.305,.804),(.003,.012,.013),'EyeBlack')
  ell('Eye light',(s*.107,-.31,.813),(.001,.004,.004),'EyeGlint')
  ell('Wattle',(s*.024,-.338,.687),(.025,.032,.065),'CombRed',(0,s*.18,0))
  tube('Scaled shank',[(s*.095,.015,.28),(s*.09,-.005,.14),(s*.09,-.025,.065)],[.024,.018,.018],'BeakGold')
  for j in range(5):
   ell('Leg scale',(s*.09,-.022,.092+j*.021),(.018,.006,.007),'HenGold')
  for j in [-1,0,1]:
   end=(s*.09+j*.065,-.14-(.027 if j==0 else 0),.025)
   tube('Toe',[(s*.09,-.025,.061),(s*.09+j*.03,-.085,.034),end],[.014,.012,.006],'BeakGold',8)
   tube('Claw',[end,(end[0]+j*.012,end[1]-.022,.019)],[.007,.001],'ClawIvory',8)
  tube('Rear toe',[(s*.09,0,.054),(s*.105,.075,.023)],[.012,.004],'BeakGold',8)
 # Neck hackles point down and backward around the collar.
 for row in range(3):
  for j in range(14):
   a=j*math.tau/14;r=.096+row*.021;z=.68-row*.065
   x=math.cos(a);y=math.sin(a)
   leaf('Neck hackle',(x*r,-.20+y*r,z),(x*(r+.028),-.18+y*(r+.027),z-.11),.04,'HenCream' if j%3 else 'HenGold',(x,y,.05),.002)
 # Upright rounded fan, dark teal-brown tail with warm overlapping saddle feathers.
 for j in range(9):
  a=(j-4)/4;leaf('Tail fan',(a*.026,.225,.44),(a*.105,.49-abs(a)*.04,.72-abs(a)*.12),.087,'HenTail',(0,-1,.4),.035)
 for j in range(7):
  a=(j-3)/3;leaf('Saddle feather',(a*.05,.15,.56),(a*.08,.365,.55),.09,'HenGold',(0,0,1),.025)
 comb=[]
 comb.append(ell('Comb root',(0,-.235,.857),(.025,.10,.033),'CombRed'))
 for j in range(5):comb.append(ell('Comb lobe',(0,-.313+j*.037,.877+.021*math.sin(j/4*math.pi)),(.021,.027,.044),'CombRed'))
 sculpt(comb,'Soft five-point comb',.007,650)
 tube('Upper beak',[(0,-.343,.76),(0,-.398,.748),(0,-.423,.737)],[.037,.023,.001],'BeakGold',12)
 tube('Lower beak',[(0,-.344,.732),(0,-.408,.733)],[.026,.001],'ClawIvory',12)
 tube('Beak seam',[(-.027,-.359,.738),(0,-.409,.735),(.027,-.359,.738)],[.0025]*3,'HenUmber',6)


def studio(name,hi,lo):
 lo.hide_render=True;lo.hide_set(True)
 bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Studio floor (not exported)'
 m=bpy.data.materials.new('Studio clay');m.diffuse_color=(.055,.085,.065,1);m.use_nodes=True;m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=m.diffuse_color;m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.9;floor.data.materials.append(m)
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48
 scene.world.color=(.25,.25,.25)
 for n,pos,power,size in [('Large softbox',(-3,-4,6),700,4),('Warm rim',(3,3,5),900,3),('Face fill',(3,-4,3),260,3)]:
  bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.name=n;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,.8))-o.location).to_track_quat('-Z','Y').to_euler()
 bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=3.8 if name=='Clover' else 1.45
 scene.render.resolution_x=1100;scene.render.resolution_y=900;scene.render.resolution_percentage=100
 try: scene.view_settings.view_transform='AgX'
 except TypeError: scene.view_settings.view_transform='Standard';scene.view_settings.exposure=-.7
 for angle,pos in [('portrait',(3.4,-4.5,2.5)),('profile',(4,-.1,1.9)),('rear',(3,4,2.2)),('distance',(3.4,-4.5,2.5))]:
  hi.hide_render=angle=='distance';lo.hide_render=angle!='distance'
  target=Vector((0,-.1,.97 if name=='Clover' else .45));cam.location=target+Vector(pos)*(1 if name=='Clover' else .5);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
  scene.render.filepath=str(OUT/f'{name}-{angle}.png');bpy.ops.render.render(write_still=True)
 hi.hide_render=False;lo.hide_render=True
 # Save portrait camera as default.
 target=Vector((0,-.1,.97 if name=='Clover' else .45));cam.location=target+Vector((3.4,-4.5,2.5))*(1 if name=='Clover' else .5);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/f'{name}.blend'))

def export(name,budget):
 obs=[o for o in bpy.context.scene.objects if o.type=='MESH']
 low_parts=[]
 if name=='Hen':
  detail_count=0
  for o in obs:
   if o.name.startswith(('Leg scale','Eye light','Claw','Neck hackle','Eye surround','Beak seam')):continue
   if o.name.startswith('Wing covert'):
    detail_count+=1
    if detail_count%5:continue
   copy=o.copy();copy.data=o.data.copy();bpy.context.collection.objects.link(copy)
   if o.name.startswith(('Wing covert','Layered primary','Tail fan','Saddle feather')):
    vertices=[copy.data.vertices[i].co.copy() for i in [22,0,10,20,30,40,34,24,14]]
    faces=[(0,j+1,(j+1)%8+1) for j in range(8)]
    faces+=list(tuple(reversed(f)) for f in faces)
    mesh=bpy.data.meshes.new(copy.name+' silhouette');mesh.from_pydata(vertices,[],faces)
    for mat in copy.data.materials:mesh.materials.append(mat)
    copy.data=mesh
   else:
    target=1000 if o.name.startswith('Hen continuous') else 160 if o.name.startswith('Soft five') else 32
    count=sum(len(p.vertices)-2 for p in copy.data.polygons)
    if count>target:
     mod=copy.modifiers.new('Preserve each anatomical part','DECIMATE');mod.ratio=target/count;apply(copy,mod)
   low_parts.append(copy)
 hi=join(obs,name+'_LOD0')
 triangles=sum(len(p.vertices)-2 for p in hi.data.polygons)
 high_budget=23500 if name=='Clover' else 11500
 if triangles>high_budget:
  m=hi.modifiers.new('High mesh budget','DECIMATE');m.ratio=high_budget/triangles;apply(hi,m)
 # Put both LOD transforms at the feet, retaining original world-space geometry.
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 if low_parts:
  lo=join(low_parts,name+'_LOD1');bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 else:
  lo=hi.copy();lo.data=hi.data.copy();bpy.context.collection.objects.link(lo);lo.name=name+'_LOD1'
 tris=sum(len(p.vertices)-2 for p in lo.data.polygons)
 if not low_parts:
  m=lo.modifiers.new('Distance simplification','DECIMATE');m.ratio=min(1,budget/tris);apply(lo,m)
 ground=min((hi.matrix_world@v.co).z for v in hi.data.vertices)
 for o in [hi,lo]:
  for v in o.data.vertices:v.co.z-=ground
 # Triangulation makes budgets match imported FBX exactly.
 for o in [hi,lo]:
  m=o.modifiers.new('Export triangles','TRIANGULATE');apply(o,m)
 bpy.ops.object.select_all(action='DESELECT');hi.select_set(True);lo.select_set(True);bpy.context.view_layer.objects.active=hi
 bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Resources'/f'{name}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=True)
 REPORT[name]={'LOD0_triangles':len(hi.data.polygons),'LOD1_triangles':len(lo.data.polygons),'dimensions_m':list(hi.dimensions),'materials':[m.name for m in hi.data.materials]}
 studio(name,hi,lo)

requested=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['Clover','Hen']
if (OUT/'animal-model-stats.json').exists():REPORT.update(json.loads((OUT/'animal-model-stats.json').read_text()))
for name,builder,budget in [('Clover',cow,4400),('Hen',hen,2400)]:
 if name in requested:reset();builder();export(name,budget)
(OUT/'animal-model-stats.json').write_text(json.dumps(REPORT,indent=2))
print('ANIMAL_STATS',json.dumps(REPORT))
