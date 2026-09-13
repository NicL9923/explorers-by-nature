"""Original first-person frontier props. Run with python3."""
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
import bpy,math,pathlib,json,random
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Resources/Tools';OUT.mkdir(parents=True,exist_ok=True);SOURCE=ROOT/'ArtSource'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
M={}
for name,col in [('Walnut',(.24,.10,.035)),('Steel',(.13,.15,.16)),('Edge',(.42,.45,.46)),('Brass',(.57,.36,.12)),('Leather',(.19,.085,.03)),('Bore',(.012,.014,.016)),('Skin',(.61,.38,.25)),('Cuff',(.65,.57,.43))]:
 m=bpy.data.materials.new('Frontier'+name);m.diffuse_color=(*col,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=.5 if name in ['Steel','Edge','Brass'] else .75;p.inputs['Metallic'].default_value=.7 if name in ['Steel','Edge','Brass'] else 0
 if name in ['Walnut','Steel','Leather']:
  im=bpy.data.images.new(m.name+'_BaseColor',width=256,height=256);px=[];rng=random.Random(428)
  for y in range(256):
   for x in range(256):
    v=1+rng.uniform(-.035,.035)
    if name=='Walnut':v+=.10*math.sin(x*.33+math.sin(y*.031)*1.4)+.05*math.sin(x*.91+y*.013)
    px.extend((*[min(1,c*v) for c in col],1))
  im.pixels=px;im.filepath_raw=str(OUT/(m.name+'_BaseColor.png'));im.file_format='PNG';im.save();node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;m.node_tree.links.new(node.outputs['Color'],p.inputs['Base Color'])
 M[name]=m
parts=[]
def put(o,name,mat):
 o.name=name;o.data.materials.append(M[mat]);parts.append(o);return o

def profile(name,points,width,mat,bevel=.004):
 # Outline in longitudinal Y / height Z. Extruded across X.
 n=len(points);vs=[(s*width*.5,y,z) for s in [-1,1] for y,z in points];fs=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);put(o,name,mat)
 if bevel:
  mod=o.modifiers.new('Soft worked edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
  mod=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=mod.name)
 return o

def rod(name,a,b,r,mat,n=16):
 a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=(b-a).length,location=(a+b)*.5);o=bpy.context.object;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return put(o,name,mat)

def curve(name,pts,r,mat):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=2;s=c.splines.new('POLY');s.points.add(len(pts)-1)
 for p,v in zip(s.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');return put(bpy.context.object,name,mat)

def pivot(name,loc,parent):
 o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=loc;bpy.context.view_layer.update()
 if parent:o.parent=parent;o.matrix_parent_inverse=parent.matrix_world.inverted()
 return o

def finish(name,p):
 global parts
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;bpy.context.scene.cursor.location=p.matrix_world.translation;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.parent=p;o.matrix_parent_inverse=p.matrix_world.inverted()
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT');mod=o.modifiers.new('Triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name);parts=[]

def export(root):
 bpy.ops.object.select_all(action='DESELECT');objs=[root]+list(root.children_recursive)
 for o in objs:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(root.name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=False,bake_anim=False,add_leaf_bones=False,path_mode='COPY',embed_textures=True,object_types={'MESH','EMPTY'})
 return sum(len(o.data.polygons) for o in objs if o.type=='MESH')

stats={};gun=pivot('Muzzleloader',(0,0,0),None)
profile('Carved walnut stock',[(.40,-.145),(.405,.025),(.33,.05),(.20,.05),(.115,.037),(.04,.045),(-.04,.07),(-.53,.068),(-.57,.052),(-.54,.011),(-.06,.008),(.015,-.027),(.09,-.077),(.15,-.099),(.29,-.146)],.068,'Walnut',.014)
profile('Curved brass butt plate',[(.406,-.147),(.414,-.142),(.414,.025),(.402,.033),(.397,.011),(.399,-.124)],.072,'Brass',.004)
rod('Octagonal browned barrel',(0,.025,.087),(0,-.87,.087),.021,'Steel',8)
# Recessed bore and a visible metal muzzle ring.
rod('Muzzle ring',(0,-.865,.087),(0,-.878,.087),.022,'Edge',24)
rod('Dark recessed bore',(0,-.8781,.087),(0,-.8786,.087),.014,'Bore',24)
for y in [-.48,-.22]:
 profile('Barrel band',[(y-.012,.014),(y+.012,.014),(y+.012,.11),(y-.012,.11)],.076,'Brass',.003)
 # Remove silhouette block by using thin side accents would require boolean; band stays narrow longitudinally.
rod('Rear sight base',(0,-.13,.103),(0,-.13,.125),.012,'Steel',8)
profile('Front sight',[(-.822,.106),(-.806,.106),(-.812,.133)],.005,'Brass',.001)
profile('Lock plate',[(-.075,.055),(.07,.034),(.09,.018),(.066,-.002),(-.065,.014),(-.083,.036)],.073,'Steel',.003)
for y,z in [(.055,.022),(-.05,.036)]:
 rod('Lock screw',(.037,y,z),(.04,y,z),.006,'Edge',16)
 curve('Screw slot',[(.0404,y-.004,z),(.0404,y+.004,z)],.0008,'Bore')
curve('Swept trigger guard',[(0,.082,-.03),(0,.073,-.065),(0,.037,-.083),(0,-.024,-.083),(0,-.045,-.053),(0,-.043,-.008)],.006,'Brass')
curve('Curved trigger',[(0,.028,-.010),(0,.023,-.045),(0,.002,-.057)],.0035,'Steel')
# Decorative but restrained brass pin and original engraving scroll.
for y in [.18,.28]:rod('Stock pin',(-.036,y,-.06),(.036,y,-.06),.004,'Brass',12)
for s in [-1,1]:
 curve('Stock inlay',[(s*.035,.18,-.07),(s*.035,.225,-.054),(s*.035,.27,-.067),(s*.035,.30,-.091)],.0015,'Brass')
for y in [-.49,-.28]:rod('Ramrod thimble',(0,y+.025,-.007),(0,y-.025,-.007),.009,'Brass',16)
finish('MuzzleloaderBody',gun)
hammer=pivot('Hammer',(.043,.035,.04),gun)
curve('S shaped hammer',[(.044,.037,.043),(.044,.065,.067),(.044,.061,.105),(.044,.035,.12),(.044,.006,.115)],.008,'Steel')
rod('Hammer thumb spur',(.042,.057,.105),(.061,.077,.113),.006,'Edge',12);finish('HammerMesh',hammer)
ramrod=pivot('Ramrod',(0,-.49,-.007),gun);rod('Hickory ramrod',(0,-.79,-.007),(0,-.12,-.007),.004,'Walnut',16);rod('Ramrod brass tip',(0,-.81,-.007),(0,-.77,-.007),.006,'Brass',16);finish('RamrodMesh',ramrod)
stats['Muzzleloader']=export(gun)
# Root at the lower grip. Hand tools rise along Unity +Y; cutting direction Unity +Z.
for name in ['Axe','Pickaxe']:
 root=pivot(name,(0,0,0),None)
 curve('Shaped hickory handle',[(0,.024,-.13),(0,.02,0),(0,-.008,.18),(0,0,.39),(0,.023,.59)],.022,'Walnut')
 for z in [-.055,-.043,-.031,-.019,-.007,.005,.017,.029,.041]:
  curve('Leather grip winding',[(.023*math.cos(i*math.tau/24),.02+.023*math.sin(i*math.tau/24),z+.001*i/24) for i in range(25)],.003,'Leather')
 if name=='Axe':
  profile('Forged axe head',[(.10,.66),(-.045,.675),(-.15,.73),(-.235,.718),(-.25,.53),(-.19,.508),(-.055,.563),(.10,.58)],.067,'Steel',.008)
  me=bpy.data.meshes.new('Honed axe wedge');me.from_pydata([(-.034,-.205,.708),(.034,-.205,.708),(0,-.241,.718),(-.034,-.205,.538),(.034,-.205,.538),(0,-.254,.526)],[],[(0,1,2),(3,5,4),(0,2,5,3),(2,1,4,5),(0,3,4,1)]);me.update();o=bpy.data.objects.new('Honed axe wedge',me);bpy.context.collection.objects.link(o);put(o,'Honed axe wedge','Edge')
  rod('Handle wedge',(-.018,.02,.655),(.018,.02,.655),.006,'Brass',8)
 else:
  profile('Forged pick head',[(.40,.50),(.24,.62),(.09,.665),(-.055,.66),(-.21,.60),(-.40,.48),(-.36,.49),(-.18,.553),(-.045,.587),(.075,.596),(.23,.584)],.058,'Steel',.009)
  profile('Bright worn pick point',[(-.40,.48),(-.31,.543),(-.28,.53),(-.36,.49)],.025,'Edge',.001)
 finish(name+'Body',root);stats[name]=export(root);root.location=(.8 if name=='Axe' else 1.4,0,0)
# Two articulated-looking hands in a relaxed grip; rigid meshes animate as whole wrists.
def ellipsoid(name,pos,scale,mat):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=pos);o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for face in o.data.polygons:face.use_smooth=True
 return put(o,name,mat)
for sign,label in [(-1,'L'),(1,'R')]:
 root=pivot('Hand'+label,(0,0,0),None)
 ellipsoid('Palm',(0,-.065,0),(.037,.054,.019),'Skin')
 ellipsoid('Wrist',(0,-.012,0),(.027,.027,.018),'Skin')
 # Fingers curl down around a grip, leaving a usable open center under the palm.
 for i in range(4):
  x=(i-1.5)*.018;length=[.047,.056,.053,.04][i]
  curve('Curled finger',[(x,-.10,0),(x,-.10-length*.57,-.003),(x,-.10-length,-.022),(x,-.09-length,-.047)],.008,'Skin')
  ellipsoid('Knuckle',(x,-.105,.005),(.010,.013,.012),'Skin')
 curve('Opposed thumb',[(-sign*.027,-.048,-.005),(-sign*.046,-.07,-.018),(-sign*.035,-.095,-.032),(-sign*.021,-.105,-.034)],.011,'Skin')
 # A rolled canvas cuff covers the forearm terminus.
 rod('Canvas cuff',(0,.021,0),(0,-.006,0),.032,'Cuff',24)
 for side in [-1,1]:curve('Palm fold',[(side*.012,-.071,.019),(side*.021,-.087,.015)],.0008,'Leather')
 finish('Hand'+label+'Mesh',root);stats['Hand'+label]=export(root);root.location=(-.1+sign*.13,-.6,.05)
(OUT/'mesh-budget.json').write_text(json.dumps(stats,indent=2)+'\n')
# Studio objects retain original local pivots; display on a neutral surface.
gun.location=(-.6,0,.35)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.15));o=bpy.context.object;o.data.materials.append(M['Leather'])
world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.6,.68,.78,1);world.node_tree.nodes['Background'].inputs[1].default_value=.7
for pos,power in [((1,-3,5),1000),((-3,2,3),700)]:
 bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=4;o.rotation_euler=(Vector((0,0,.3))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(4,-5,3.8));cam=bpy.context.object;cam.rotation_euler=(Vector((.35,-.08,.22))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.65;bpy.context.scene.camera=cam
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.view_settings.view_transform='AgX';scene.render.filepath=str(SOURCE/'FrontierTools-contact-sheet.png')
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'FrontierTools.blend'));bpy.ops.render.render(write_still=True);print(stats)
