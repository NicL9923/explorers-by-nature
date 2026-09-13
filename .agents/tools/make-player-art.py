"""Original peaceful frontier characters. python3 .agents/tools/make-player-art.py"""
import importlib.util
if importlib.util.find_spec('bpy') is None:
 import os, pathlib, re, subprocess, tempfile
 with tempfile.TemporaryDirectory(prefix='player-art-color-') as tmp:
  env=os.environ.copy();env.pop('LD_LIBRARY_PATH',None)
  if 'OCIO' not in env:
   original=sorted(pathlib.Path('/usr/share/blender').glob('*/datafiles/colormanagement/config.ocio'))[-1]
   config=original.read_text().replace('ocio_profile_version: 2.5','ocio_profile_version: 2.4')
   config=re.sub(r'^\s*(interop_id|interchange|icc_profile_name):.*\n','',config,flags=re.MULTILINE)
   config=config.replace('search_path: "icc:luts:filmic"','search_path: "'+':'.join(str(original.parent/p) for p in ['icc','luts','filmic'])+'"')
   path=pathlib.Path(tmp)/'config.ocio';path.write_text(config);env['OCIO']=str(path)
  subprocess.run(['/usr/bin/blender','-b','-t','4','--python-exit-code','1','--python',str(pathlib.Path(__file__).resolve())],env=env,check=True)
 raise SystemExit()
import bpy, math, pathlib, json, random
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/Players'; OUT.mkdir(parents=True,exist_ok=True)
SOURCE=ROOT/'ArtSource'; SOURCE.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
M={}
colors={'Denim':(.13,.23,.31),'Sage':(.32,.39,.28),'Cream':(.79,.71,.54),'Rust':(.46,.20,.105),'Wine':(.29,.095,.13),'Apron':(.70,.64,.50),'Leather':(.18,.085,.035),'TanLeather':(.38,.22,.10),'Gold':(.64,.46,.20),'Black':(.022,.028,.026),'White':(.80,.79,.69),'Skin1':(.61,.38,.25),'Skin2':(.33,.16,.09),'Skin3':(.78,.52,.36),'Skin4':(.48,.28,.17),'Hair1':(.095,.052,.025),'Hair2':(.025,.02,.018),'Hair3':(.35,.14,.045),'Hair4':(.10,.065,.042),'Lip':(.34,.14,.095)}
for name,c in colors.items():
 m=bpy.data.materials.new('Player'+name);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.84 if name!='Gold' else .48
 if name in ['Denim','Sage','Cream','Rust','Wine','Apron','Leather','TanLeather']:
  tex=bpy.data.images.new(m.name+'_BaseColor',width=128,height=128);rng=random.Random(884+len(name));pixels=[]
  for y in range(128):
   for x in range(128):
    if 'Leather' in name:v=1+rng.uniform(-.045,.045)+.018*math.sin(x*.17)*math.sin(y*.21)
    else:v=1+(.024 if (x+y)%2 else -.024)+.016*math.sin(x*math.pi/2)+rng.uniform(-.018,.018)
    pixels.extend((*[min(1,k*v) for k in c],1))
  tex.pixels=pixels;tex.filepath_raw=str(OUT/(m.name+'_BaseColor.png'));tex.file_format='PNG';tex.save()
  node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=tex;m.node_tree.links.new(node.outputs['Color'],p.inputs['Base Color'])
 M[name]=m
parts=[]
def add(o,name,mat):
 o.name=name;o.data.materials.append(M[mat]);parts.append(o)
 for p in o.data.polygons:p.use_smooth=True
 return o

def ball(name,pos,scale,mat,seg=20,rings=12):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=pos);o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return add(o,name,mat)

def loft(name,rings,mat,n=24):
 # Cross sections (center x, center y, z, x radius, y radius).
 vs=[]
 for x,y,z,rx,ry in rings:
  vs += [(x+rx*math.cos(i*math.tau/n),y+ry*math.sin(i*math.tau/n),z) for i in range(n)]
 fs=[]
 for j in range(len(rings)-1):
  for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
 fs += [tuple(reversed(range(n))),tuple((len(rings)-1)*n+i for i in range(n))]
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);add(o,name,mat);o.data.polygons[-1].use_smooth=False;o.data.polygons[-2].use_smooth=False;return o

def pipe(name,points,radius,mat):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=radius;c.bevel_resolution=1;c.resolution_u=8;s=c.splines.new('POLY');s.points.add(len(points)-1)
 for p,v in zip(s.points,points):p.co=(*v,1)
 o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.select_set(False);return add(o,name,mat)

def patch(name,vs,mat):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],[tuple(range(len(vs)))]);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);add(o,name,mat)
 for face in o.data.polygons:face.use_smooth=False
 mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.004;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name);return o

def pivot(name,pos,parent=None):
 o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=pos
 if parent:o.parent=parent;o.matrix_parent_inverse=parent.matrix_world.inverted()
 bpy.context.view_layer.update();return o

def finish(name,p):
 global parts
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name+'Mesh'
 bpy.context.scene.cursor.location=p.matrix_world.translation;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.parent=p;o.matrix_parent_inverse=p.matrix_world.inverted()
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.012);bpy.ops.object.mode_set(mode='OBJECT')
 mod=o.modifiers.new('Triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name);parts=[];return o

stats={};roots=[]
for index,(name,shirt,pants,skin,hair,hat) in enumerate([('RanchHand','Rust','Denim','Skin1','Hair1','TanLeather'),('TrailScout','Sage','TanLeather','Skin2','Hair2','Cream'),('Homesteader','Wine','Wine','Skin3','Hair3','Cream'),('Frontiersman','Cream','Denim','Skin4','Hair4','Leather')]):
 root=pivot(name,(0,0,0));hips=pivot('Hips',(0,0,.94),root);torso=pivot('Torso',(0,0,1.02),hips);head=pivot('Head',(0,0,1.56),torso)
 female=index in (1,2); width=.20 if female else .235
 loft('Tailored shirt',[(0,0,.95,.17,.10),(0,0,1.02,.17,.11),(0,0,1.13,.16 if female else .19,.105),(0,0,1.28,width,.12),(0,0,1.39,width,.11),(0,0,1.45,.18,.09),(0,0,1.49,.072,.065)],shirt)
 loft('Standing collar',[(0,0,1.455,.079,.071),(0,0,1.49,.078,.071)],'Cream')
 # Proper pointed shirt collar and placket.
 for sign in [-1,1]:
  patch('Pointed collar',[(sign*.015,-.08,1.485),(sign*.085,-.087,1.46),(sign*.11,-.116,1.38),(sign*.028,-.119,1.42)],'Cream')
 pipe('Shirt placket',[(0,-.10 if index==2 else -.112,1.04),(0,-.125,1.31),(0,-.104,1.43)],.008,shirt)
 for z in ([1.40] if index==2 else [1.09,1.17,1.25,1.33,1.40]):ball('Sewn button',(0,-.128 if z<1.36 else -.115,z),(.007,.005,.007),'Gold',12,8)
 for sign in ([] if index in [2,3] else [-1,1]):
  x=sign*.112
  patch('Patch pocket',[(x-.043,-.121,1.33),(x+.043,-.121,1.33),(x+.04,-.128,1.25),(x,-.129,1.235),(x-.04,-.128,1.25)],shirt)
  pipe('Pocket top seam',[(x-.04,-.129,1.32),(x+.04,-.129,1.32)],.0026,'Cream')
 if index==3:
  for s in [-1,1]:
   patch('Waistcoat panel',[(s*.012,-.143,1.06),(s*.165,-.143,1.075),(s*.212,-.138,1.38),(s*.14,-.13,1.45),(s*.027,-.147,1.26)],'TanLeather')
   pipe('Waistcoat welt',[(s*.08,-.135,1.15),(s*.145,-.121,1.155)],.005,'Leather')
 if index in [0,1]:
  loft('Neckerchief',[(0,0,1.455,.086,.077),(0,0,1.477,.086,.077)],'Wine' if index==0 else 'Rust')
  patch('Neckerchief tail',[(-.035,-.088,1.46),(.026,-.09,1.46),(.068,-.13,1.30),(0,-.14,1.325)],'Wine' if index==0 else 'Rust')
 if index==2:
  patch('Apron bib',[(-.085,-.127,1.36),(.085,-.127,1.36),(.12,-.145,1.015),(-.12,-.145,1.015)],'Apron')
  for s in [-1,1]:pipe('Apron shoulder strap',[(s*.08,-.13,1.36),(s*.12,-.085,1.455),(s*.12,.075,1.42)],.016,'Apron')
 finish('Torso',torso)
 loft('Trouser seat',[(0,0,.86,.155,.079),(0,0,.91,.174,.103),(0,0,.96,.17,.105),(0,0,1.025,.17,.105)],pants)
 loft('Leather belt',[(0,0,.99,.176,.113),(0,0,1.024,.176,.113)],'Leather')
 pipe('Belt buckle',[(-.027,-.12,1.025),(.027,-.12,1.025),(.027,-.12,.989),(-.027,-.12,.989),(-.027,-.12,1.025)],.005,'Gold')
 finish('Hips',hips)
 if index==2:
  standing=pivot('StandingSkirt',(0,0,.94),torso)
  # Widening ankle-length cloth silhouette with individually authored folds.
  rings=[(.21,.98),(.235,.88),(.28,.70),(.33,.49),(.36,.27)]
  vs=[];n=48
  for r,z in rings:
   for i in range(n):
    a=i*math.tau/n;rr=r*(1+.032*math.cos(a*12));vs.append((rr*math.cos(a),rr*.68*math.sin(a),z))
  fs=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(rings)-1) for i in range(n)]
  me=bpy.data.meshes.new('Gored skirt');me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new('Gored skirt',me);bpy.context.collection.objects.link(o);add(o,'Gored skirt','Wine')
  patch('Full apron',[(-.13,-.151,.98),(.13,-.151,.98),(.23,-.22,.40),(0,-.251,.38),(-.23,-.22,.40)],'Apron')
  pipe('Apron hem',[(-.23,-.224,.405),(0,-.255,.385),(.23,-.224,.405)],.004,'Cream')
  finish('StandingSkirt',standing)
 for s,label in [(-1,'L'),(1,'R')]:
  p=pivot('Leg'+label,(s*.10,0,.94),hips);x=s*.10
  loft('Trousers',[(x,0,.90,.085,.097),(x,0,.78,.084,.094),(x,0,.58,.067,.076),(x,-.013,.51,.067,.071)],pants)
  finish('Leg'+label,p)
  if index==2:
   drape=pivot('RidingSkirt'+label,(x,0,.94),p)
   # Each half follows the thigh instead of intersecting the saddle below the hips.
   # Wide cloth folds cover trousers down to the bent knee; apron splits at the saddle.
   n=32;vs=[]
   for z,rx,ry in [(.955,.100,.11),(.86,.115,.125),(.70,.125,.135),(.55,.14,.14),(.485,.145,.135)]:
    for i in range(n):
     a=i*math.tau/n;fold=1+.035*math.cos(a*8);vs.append((x+rx*math.cos(a)*fold,ry*math.sin(a)*fold,z))
   fs=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(4) for i in range(n)]
   me=bpy.data.meshes.new('Split riding skirt');me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new('Split riding skirt',me);bpy.context.collection.objects.link(o);add(o,'Split riding skirt','Wine')
   patch('Split apron panel',[(x-.065,-.125,.93),(x+.065,-.125,.93),(x+.092,-.15,.53),(x,-.157,.51),(x-.092,-.15,.53)],'Apron')
   pipe('Riding skirt hem',[(x+.145*math.cos(i*math.tau/32),.135*math.sin(i*math.tau/32),.488) for i in range(33)],.004,'Wine')
   finish('RidingSkirt'+label,drape)
  p=pivot('Knee'+label,(x,-.013,.51),p)
  loft('Trouser calf',[(x,-.013,.54,.057,.061),(x,-.012,.495,.066,.071),(x,0,.44,.063,.073),(x,0,.28,.055,.063)],pants)
  pipe('Trouser outseam',[(x+s*.067,-.013,.51),(x+s*.058,0,.29)],.0025,'Cream')
  loft('Boot shaft',[(x,0,.12,.065,.079),(x,.003,.23,.065,.075),(x,0,.36,.064,.070)],'Leather')
  ball('Shaped leather boot',(x,-.052,.085),(.072,.142,.082),'Leather');loft('Boot sole',[(x,-.052,.024,.075,.145),(x,-.052,.039,.075,.145)],'Black')
  pipe('Boot top welt',[(x+.064*math.cos(i*math.tau/24),.07*math.sin(i*math.tau/24),.36) for i in range(25)],.004,'TanLeather')
  finish('Knee'+label,p)
  p=pivot('Arm'+label,(s*(width+.015),0,1.42),torso);x=s*(width+.015)
  loft('Shirt sleeve',[(x-s*.012,0,1.452,.025,.030),(x,0,1.437,.059,.063),(x+s*.012,0,1.406,.076,.081),(x+s*.026,0,1.33,.073,.075),(x+s*.043,0,1.17,.06,.062),(x+s*.047,-.014,1.12,.061,.064),(x+s*.054,-.035,.98,.047,.05)],shirt)
  loft('Turned cuff',[(x+s*.054,-.035,.977,.052,.053),(x+s*.054,-.03,1.019,.054,.056)],'Cream')
  ball('Hand',(x+s*.056,-.037,.916),(.046,.032,.076),skin)
  ball('Thumb',(x+s*.017,-.054,.931),(.018,.024,.04),skin,16,10)
  for f in range(3):pipe('Finger crease',[(x+s*.034+f*.011,-.066,.896),(x+s*.034+f*.011,-.061,.868)],.0012,'Lip')
  finish('Arm'+label,p)
 # Head proportions 7.5 heads tall; sculpted cheeks/jaw, nose, inset eyes and brows.
 loft('Neck',[(0,0,1.46,.056,.055),(0,0,1.565,.056,.054)],skin)
 loft('Face',[(0,-.007,1.54,.045,.055),(0,-.018,1.565,.064,.066),(0,-.008,1.60,.082,.077),(0,0,1.65,.088,.081),(0,.004,1.69,.083,.079),(0,.008,1.73,.071,.068),(0,.008,1.75,.04,.045)],skin,32)
 for s in [-1,1]:
  ball('Ear',(s*.084,.001,1.628),(.018,.023,.036),skin,16,10)
  ball('Earlobe inset',(s*.094,-.011,1.631),(.006,.011,.020),'Lip',12,8)
  ball('Eye white',(s*.033,-.075,1.654),(.019,.010,.009),'White',16,10)
  ball('Iris',(s*.033,-.084,1.654),(.007,.0025,.007),'Hair'+str(index+1),12,8)
  ball('Pupil',(s*.033,-.086,1.654),(.0036,.0015,.004),'Black',12,8)
  pipe('Upper eyelid',[(s*.015,-.079,1.657),(s*.032,-.086,1.665),(s*.052,-.075,1.658)],.0028,skin)
  pipe('Eyebrow',[(s*.014,-.075,1.675),(s*.034,-.080,1.679),(s*.056,-.067,1.673)],.004,hair)
 ball('Nose bridge',(0,-.078,1.64),(.012,.019,.030),skin)
 ball('Nose tip',(0,-.096,1.626),(.017,.017,.012),skin)
 pipe('Upper lip',[(-.025,-.076,1.594),(0,-.085,1.596),(.025,-.076,1.594)],.0034,'Lip')
 pipe('Lower lip',[(-.020,-.078,1.591),(0,-.083,1.588),(.020,-.078,1.591)],.0028,skin)
 # Hair fitted to scalp, temples and back. Braids for both women.
 loft('Hair cap',[(0,.016,1.697,.085,.077),(0,.008,1.735,.078,.074),(0,.01,1.763,.05,.049)],hair)
 for s in [-1,1]:ball('Temple hair',(s*.074,.015,1.675),(.015,.046,.054),hair)
 if female:
  for s in [-1,1]:
   for k in range(9):ball('Braided hair',(s*(.074+.004*math.sin(k*2)),.062,1.63-k*.022),(.019,.018,.019),hair,12,8)
   ball('Braid tie',(s*.076,.06,1.441),(.021,.02,.012),'Wine',12,8)
 elif index==3:
  for s in [-1,1]:ball('Trim moustache',(s*.014,-.088,1.606),(.019,.008,.006),hair,16,8)
 # Broad curved western brim with rolled edge and creased tapered crown.
 vs=[];n=64
 for r in [.079,.16,.205]:
  for i in range(n):
   a=i*math.tau/n;vs.append((r*math.cos(a),r*.83*math.sin(a),1.723+.035*(r/.205)**3*abs(math.cos(a))**3-.014*(r/.205)**2*abs(math.sin(a))))
 fs=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(2) for i in range(n)]
 me=bpy.data.meshes.new('Shaped brim');me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new('Shaped brim',me);bpy.context.collection.objects.link(o);add(o,'Shaped brim',hat)
 mod=o.modifiers.new('Felt thickness','SOLIDIFY');mod.thickness=.006;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 pipe('Rolled brim edge',vs[128:]+[vs[128]],.004,hat)
 loft('Pinched hat crown',[(0,.008,1.725,.097,.086),(0,.008,1.75,.098,.085),(0,.008,1.817,.081,.072),(0,.008,1.836,.065,.065)],hat,32)
 loft('Hatband',[(0,.008,1.734,.10,.088),(0,.008,1.753,.098,.087)],'Leather')
 pipe('Crown crease',[(-.036,-.016,1.838),(0,-.035,1.825),(.036,-.016,1.838)],.004,hat)
 finish('Head',head)
 bpy.ops.object.select_all(action='DESELECT');objects=[root]+list(root.children_recursive)
 for o in objects:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True,object_types={'MESH','EMPTY'})
 stats[name]={'triangles':sum(len(o.data.polygons) for o in objects if o.type=='MESH'),'height_m':1.84,'parts':['Hips','Torso','Head','ArmL','ArmR','LegL','LegR','KneeL','KneeR']+(['StandingSkirt','RidingSkirtL','RidingSkirtR'] if index==2 else [])}
 for obj in objects:
  if obj.name.startswith('RidingSkirt') and obj.type=='MESH':obj.hide_render=True;obj.hide_set(True)
  if obj != root:obj.name=name+'_'+obj.name
 root.location.x=(index-1.5)*.92;roots.append(root)
(OUT/'mesh-budget.json').write_text(json.dumps(stats,indent=2)+'\n')
# Studio contact sheet retained in the editable source.
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.008));ground=bpy.context.object;ground.name='Studio ground';ground.data.materials.append(M['Sage'])
world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.60,.67,.75,1);world.node_tree.nodes['Background'].inputs[1].default_value=.5
for pos,energy,size in [((-3,-4,6),900,5),((4,1,4),700,4)]:
 bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=energy;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-9,3.2));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.98))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=4.3;bpy.context.scene.camera=cam
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1800;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.view_settings.exposure=0;scene.render.filepath=str(SOURCE/'Players-contact-sheet.png')
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Players.blend'));bpy.ops.render.render(write_still=True)
print(json.dumps(stats,indent=2))
