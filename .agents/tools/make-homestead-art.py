"""Generate the original Pinewatch timber kit. blender -b -t 4 --python this-file.py."""
import bpy, math, pathlib, random, json
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/Homestead'; SOURCE=ROOT/'ArtSource/Homestead'
OUT.mkdir(parents=True,exist_ok=True);SOURCE.mkdir(parents=True,exist_ok=True)
random.seed(61204)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
# Original raster grain, shared by the wooden materials. No external source assets.
w,h=256,512
im=bpy.data.images.new('PinewatchWoodGrain',width=w,height=h)
pixels=[]
rng=random.Random(381)
strands=[rng.uniform(-1,1) for _ in range(w)]
for y in range(h):
 for x in range(w):
  xx=x+3*math.sin(y*.012+x*.018)
  grain=math.sin(xx*.42+math.sin(y*.023)*.5)*.018 + strands[int(xx)%w]*.010
  grain+=math.sin(xx*.091+math.sin(y*.011))*.028+rng.uniform(-.010,.010)
  # Dark narrow elongated knots repeat naturally along a timber.
  dx=(x-167)/19;dy=(y-285)/57;rad=math.sqrt(dx*dx+dy*dy)
  knot=0 if rad>2 else -.09*math.cos(rad*10)/(1+rad*rad)
  v=max(.15,min(.9,.63+grain+knot))
  pixels.extend((v,v*.86,v*.69,1))
im.pixels=pixels;im.filepath_raw=str(OUT/'PinewatchWoodGrain.png');im.file_format='PNG';im.save()
M={}
for name,col,wood in [('Timber',(.50,.34,.21,1),True),('Planks',(.72,.55,.36,1),True),('Cedar',(.39,.30,.23,1),True),('Iron',(.12,.14,.14,1),False),('Straw',(.67,.49,.21,1),False),('Stone',(.40,.42,.39,1),False)]:
 m=bpy.data.materials.new('Homestead'+name);m.diffuse_color=col;m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=col;p.inputs['Roughness'].default_value=.88
 if wood:
  tex=bpy.data.images.new(m.name+'_BaseColor',width=w,height=h)
  tinted=[]
  for i in range(0,len(pixels),4):
   v=pixels[i]/.63
   tinted.extend((min(1,col[0]*v),min(1,col[1]*v),min(1,col[2]*v),1))
  tex.pixels=tinted;tex.filepath_raw=str(OUT/(m.name+'_BaseColor.png'));tex.file_format='PNG';tex.save()
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=tex
  m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
 M[name]=m
parts=[]; LOW=False

def cube(name,pos,size,mat='Timber',bevel=.012,rotation=None):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 # UVs follow each board's longest axis, so a tall post gets vertical grain.
 axis=max(range(3),key=lambda i:size[i]);others=[i for i in range(3) if i!=axis]
 uv=o.data.uv_layers.active
 uoff=random.random();voff=random.random()
 for p in o.data.polygons:
  short=next((i for i in others if abs(p.normal[i])<.5),others[0])
  for li in p.loop_indices:
   v=o.data.vertices[o.data.loops[li].vertex_index].co
   uv.data[li].uv=(v[short]/max(size[short],.001)+.5+uoff,v[axis]/max(size[axis],.001)+.5+voff)
 o.data.materials.append(M[mat])
 if rotation:o.rotation_euler=rotation
 if bevel and not LOW:
  mod=o.modifiers.new('Worn arrises','BEVEL');mod.width=bevel;mod.segments=1
  bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
  mod=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 parts.append(o);return o

def beam(name,a,b,width,depth=None,mat='Timber'):
 a,b=Vector(a),Vector(b);o=cube(name,(a+b)*.5,(width,depth or width,(b-a).length),mat)
 o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o

def peg(x,y,z,deck=False):
 if LOW:return
 bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=.025,depth=.008 if deck else .018,location=(x,y,z),rotation=(0 if deck else math.pi/2,0,0));o=bpy.context.object;o.name='Oak joinery peg';o.data.materials.append(M['Timber']);parts.append(o)

def foundation():
 for x in [-1.18,1.18]:
  for y in [-1.18,1.18]:cube('Fieldstone pier',(x,y,.12),(.42,.44,.26),'Stone',.07)
 for y in [-1.37,1.37]:cube('Mortised sill',(0,y,.285),(3,.24,.22))
 for x in [-1.37,1.37]:cube('Cross sill',(x,0,.285),(.24,2.55,.22))
 for i in range(13):
  x=-1.5+(i+.5)*3/13
  cube('Deck board',(x,0,.405),(3/13-.006,3,.09),'Planks',.006)
  for y in [-1.28,1.28]:peg(x,y,.451,True)
 # Low entry apron matches the runtime's four existing step colliders.
 for y in [-1.9,1.9]:
  cube('Apron bearer',(0,y,-.02),(4.60,.65,.12),'Timber')
  for i in range(20):cube('Entry step board',(-2.3+(i+.5)*4.6/20,y,.08),(4.6/20-.006,.8,.08),'Planks',.005)
 for x in [-1.9,1.9]:
  cube('Side apron bearer',(x,0,-.02),(.65,3,.12),'Timber')
  for i in range(13):cube('Side entry board',(x,-1.5+(i+.5)*3/13,.08),(.8,3/13-.006,.08),'Planks',.005)

def wall(door=False):
 y=-1.45
 for x in [-1.38,1.38]:cube('Hewn corner post',(x,y,1.65),(.23,.23,2.4))
 cube('Tie beam',(0,y,2.73),(2.53,.24,.24))
 if not door:cube('Bottom sill',(0,y,.55),(3,.20,.20))
 # Plank segments stop at the real framed opening, no solid cube behind it.
 left,right=(-.60,.60) if door else (-.58,.58)
 bottom,top=(.45,2.51) if door else (1.36,2.20)
 for i in range(14):
  x=-1.25+(i+.5)*2.5/14;bw=2.5/14-.009
  if x+bw/2>left and x-bw/2<right:
   if not door:cube('Lower infill',(x,y,.92),(bw,.11,.74),'Planks',.004)
   cube('Upper infill',(x,y,(top+2.62)/2),(bw,.11,2.62-top),'Planks',.004)
  else:cube('Vertical infill',(x,y,1.535),(bw,.11,2.17),'Planks',.004)
 for x in [left-.045,right+.045]:cube('Opening jamb',(x,y-.025,(bottom+top)/2),(.10,.22,top-bottom+.13))
 cube('Lintel',(0,y-.025,top+.045),(right-left+.28,.26,.14))
 if not door:
  cube('Window sill',(0,y-.09,bottom),(1.42,.36,.12),'Planks')
  cube('Window mullion',(0,y,1.78),(.042,.065,.77),'Timber',.003)
  cube('Window transom',(0,y,1.79),(1.10,.065,.042),'Timber',.003)
  # Fixed side shutter with strap hinges; the center opening remains transparent.
  for sign in [-1,1]:
   x=sign*.91
   for i in range(3):cube('Shutter slat',(x+(i-1)*.15,y-.14,1.79),(.144,.055,.81),'Cedar',.006)
   for z in [1.52,2.07]:
    cube('Shutter iron strap',(x,y-.176,z),(.42,.018,.035),'Iron',.002)
    peg(x-.14,y-.19,z);peg(x+.14,y-.19,z)
 for x in [-1.38,1.38]:
  for z in [.64,2.69]:peg(x,y-.129,z)
 # Knee braces visible on the interior.
 for s in [-1,1]:beam('Interior knee brace',(s*1.29,y+.14,2.22),(s*.87,y+.14,2.63),.095)

def roof():
 # Ridge runs Blender Y / Unity Z. Peak 3.6, eaves 2.84.
 pitch=math.atan2(.76,1.65)
 for y in [-1.48,0,1.48]:
  for s in [-1,1]:beam('Exposed rafter',(0,y,3.55),(s*1.66,y,2.79),.11)
  beam('Collar tie',(-.66,y,3.23),(.66,y,3.23),.085)
 for s in [-1,1]:
  cube('Eave fascia',(s*1.63,0,2.80),(.11,3.35,.18))
  # Underlay prevents cracks exposing sky; individual overlapped shingles give silhouette.
  o=cube('Roof sheathing',(s*.82,0,3.18),(1.83,3.30,.055),'Cedar');o.rotation_euler.y=s*pitch
  courses=7 if not LOW else 4;cols=13 if not LOW else 9
  for row in range(courses):
   x=1.65-(row+.5)*1.65/courses
   for c in range(cols+1):
    start=max(-1.65,-1.65+(c-(row%2)*.5)*3.3/cols)
    end=min(1.65,-1.65+(c+1-(row%2)*.5)*3.3/cols)
    if end-start<.02:continue
    cy=(start+end)/2
    jitter=0 if LOW else random.uniform(-.014,.014)
    xx=x+jitter;z=3.60-xx*.76/1.65+.045+row*.001
    o=cube('Split cedar shingle',(s*xx,cy,z),(1.65/courses*1.20,end-start-.009,.045),'Cedar',.004)
    o.rotation_euler.y=s*pitch
 for y in [-1.66,1.66]:
  for s in [-1,1]:beam('Gable bargeboard',(0,y,3.59),(s*1.7,y,2.80),.10,.15)
 for s in [-1,1]:
  o=cube('Ridge cap',(s*.065,0,3.64),(.18,3.4,.045),'Timber');o.rotation_euler.y=s*pitch

def fence():
 for x in [-1.36,1.36]:
  cube('Split chestnut post',(x,-1.45,.73),(.17,.19,1.46),'Timber',.025)
  cube('Post cap',(x,-1.45,1.47),(.20,.21,.07),'Planks',.022)
 for z in [.48,.99]:
  beam('Split rail',(-1.49,-1.46,z-.035),(1.49,-1.46,z+.035),.13,.12,'Planks')
  for x in [-1.36,1.36]:peg(x,-1.55,z)
 beam('Diagonal gate brace',(-1.25,-1.38,.50),(1.25,-1.38,1.05),.075,.065)

def coop():
 # A small raised hen house with nest hatches and a cleated approach ramp.
 for x in [-.65,.65]:
  for y in [-.45,.45]:cube('Coop leg',(x,y,.39),(.12,.12,.78))
 cube('Coop floor',(0,0,.58),(1.45,1.06,.10),'Planks')
 for y in [-.49,.49]:
  for i in range(9):
   x=-.64+i*.16
   if y<0 and abs(x)<.25:cube('Entry header plank',(x,y,1.42),(.153,.065,.32),'Planks',.004)
   else:cube('Coop siding',(x,y,1.08),(.153,.065,.93),'Planks',.004)
 for x in [-.69,.69]:
  for i in range(7):cube('Coop side plank',(x,-.44+i*.145,1.08),(.065,.14,.93),'Planks',.004)
  for y in [-.5,.5]:cube('Coop corner trim',(x,y,1.08),(.095,.10,1.01))
 for x in [-.27,.27]:cube('Hen doorway jamb',(x,-.54,.94),(.065,.09,.67))
 cube('Hen doorway lintel',(0,-.54,1.28),(.59,.09,.08))
 # Asymmetric roof, sloped rain lid over rear nesting boxes.
 for s in [-1,1]:
  pitch=math.atan2(.43,.86)
  o=cube('Coop roof backing',(s*.43,0,1.76),(.98,1.28,.065),'Cedar');o.rotation_euler.y=s*pitch
  for row in range(4):
   x=.86-(row+.5)*.86/4
   for c in range(6):
    o=cube('Coop shingle',(s*x,-.64+(c+.5)*1.28/6,2.055-x*.5),(.26,.207,.043),'Cedar',.004);o.rotation_euler.y=s*pitch
 for sign in [-1,1]:
  o=cube('Coop ridge cap',(sign*.04,0,2.065),(.13,1.32,.045),'Timber');o.rotation_euler.y=sign*math.atan(.5)
 cube('Nesting box',(0,.66,.87),(1.18,.44,.48),'Timber')
 for x in [-.4,0,.4]:
  cube('Nesting hatch',(x,.905,.88),(.37,.035,.35),'Planks',.006)
  cube('Hatch handle',(x,.94,.89),(.10,.045,.035),'Iron',.004)
 o=cube('Nest rain lid',(0,.70,1.16),(1.34,.61,.06),'Cedar');o.rotation_euler.x=-.14
 beam('Hen approach ramp',(0,-.55,.62),(0,-1.53,.055),.49,.06,'Planks')
 # Ramp boards lie across its slope, using orthogonal orientation.
 for i in range(7):
  t=(i+.5)/7;y=-.55-.98*t;z=.65-.565*t
  cube('Ramp cleat',(0,y,z),(.49,.055,.035),'Timber',.003,(-.523,0,0))
 # Straw visible through entry.
 if not LOW:
  for i in range(23):
   x=random.uniform(-.28,.28);y=random.uniform(-.4,.2)
   o=cube('Nest straw',(x,y,.653),(.008,.25,.006),'Straw',0);o.rotation_euler.z=random.uniform(-2,2)

builders={'Foundation':foundation,'Wall':wall,'Door':lambda:wall(True),'Roof':roof,'Fence':fence,'Coop':coop}
stats={};models={}
for name,build in builders.items():
 for lod in [0,1]:
  LOW=lod==1;parts=[];build()
  bpy.ops.object.select_all(action='DESELECT')
  for o in parts:o.select_set(True)
  bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name+'_LOD'+str(lod)
  bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
  bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
  # Triangulate for reproducible topology and triangle accounting.
  mod=o.modifiers.new('Export triangulation','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
  stats[o.name]={'triangles':len(o.data.polygons),'vertices':len(o.data.vertices),'dimensions':list(o.dimensions)}
  models[o.name]=o
  o.hide_render=True;o.hide_set(True)
 # One resource with child meshes for the runtime LODGroup.
 bpy.ops.object.select_all(action='DESELECT')
 for lod in [0,1]:
  o=models[name+'_LOD'+str(lod)];o.hide_set(False);o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True,object_types={'MESH'})
 for lod in [0,1]:models[name+'_LOD'+str(lod)].hide_set(True)
(SOURCE/'mesh-budget.json').write_text(json.dumps(stats,indent=2)+'\n')
# Editable source keeps models at origin, hidden except the assembled demonstration.
for name,pos,rot in [('Foundation',(0,0,0),0),('Wall',(0,0,0),0),('Door',(0,0,0),math.pi/2),('Roof',(0,0,0),0),('Fence',(3.3,0,0),0),('Coop',(3.1,-2.9,0),0)]:
 src=models[name+'_LOD0'];o=src.copy();o.data=src.data.copy();bpy.context.collection.objects.link(o);o.name='Preview_'+name;o.hide_set(False);o.hide_render=False;o.location=pos;o.rotation_euler.z=rot
# Reverse wall so the assembled reference cabin has rear and left walls.
for o in bpy.context.scene.objects:
 if o.name=='Preview_Wall':o.rotation_euler.z=math.pi
 if o.name=='Preview_Door':o.rotation_euler.z=-math.pi/2
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.015));ground=bpy.context.object;ground.name='Preview ground'
gm=bpy.data.materials.new('Preview ground');gm.diffuse_color=(.19,.22,.16,1);ground.data.materials.append(gm)
world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.53,.65,.79,1);world.node_tree.nodes['Background'].inputs[1].default_value=.6
bpy.ops.object.light_add(type='AREA',location=(-3,-4,9));bpy.context.object.data.energy=1600;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=7
bpy.ops.object.camera_add(location=(8,-10,7));cam=bpy.context.object;cam.rotation_euler=(Vector((.8,-.4,1.3))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=10.6;bpy.context.scene.camera=cam
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.view_settings.exposure=-.7;scene.render.filepath=str(SOURCE/'homestead-assembled.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'PinewatchHomestead.blend'));bpy.ops.render.render(write_still=True)
# Asset sheet, separated in world space, proves the individual module openings.
for o in list(scene.objects):
 if o.name.startswith('Preview_'):bpy.data.objects.remove(o,do_unlink=True)
for i,name in enumerate(builders):
 o=models[name+'_LOD0'].copy();o.data=models[name+'_LOD0'].data.copy();bpy.context.collection.objects.link(o);o.hide_set(False);o.hide_render=False;o.location=((i%3)*4.2,(i//3)*5,0)
cam.location=(12,-15,14);cam.rotation_euler=(Vector((4,2.3,1.2))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=16
scene.render.resolution_x=1800;scene.render.resolution_y=1250;scene.render.filepath=str(SOURCE/'homestead-contact-sheet.png');bpy.ops.render.render(write_still=True)
print(json.dumps(stats,indent=2))
