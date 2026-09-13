"""Original ranch horse. Regenerate: env -u LD_LIBRARY_PATH blender -b -t 4 --python .agents/tools/make-horse-art.py"""
import bpy, math, pathlib, random, json
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/Horse'; OUT.mkdir(parents=True,exist_ok=True)
SOURCE=ROOT/'ArtSource'; SOURCE.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
M={}
for name,c in {'Bay':(.34,.115,.044),'Warm':(.40,.155,.061),'Dark':(.048,.024,.016),'Hoof':(.065,.058,.048),'Ivory':(.79,.74,.60),'Muzzle':(.105,.077,.066),'Eye':(.012,.009,.007),'Glint':(.80,.83,.78),'Leather':(.16,.062,.025),'LeatherEdge':(.32,.16,.069),'Blanket':(.17,.27,.29),'BlanketStripe':(.69,.52,.29),'Brass':(.54,.36,.11)}.items():
 m=bpy.data.materials.new('Horse'+name);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.32 if name in ['Eye','Brass'] else .75
 if name=='Brass':p.inputs['Metallic'].default_value=.7
 if name in ['Bay','Warm','Leather','Blanket']:
  im=bpy.data.images.new(m.name+'_BaseColor',width=256,height=256);r=random.Random(721+len(name));pixels=[]
  for y in range(256):
   for x in range(256):
    v=1+r.uniform(-.045,.045)+.025*math.sin(x*.21)*math.sin(y*.11)
    if name=='Blanket':v+=.045*((x+y)%2)
    pixels.extend((*[min(1,k*v) for k in c],1))
  im.pixels=pixels;im.filepath_raw=str(OUT/(m.name+'_BaseColor.png'));im.file_format='PNG';im.save();t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=im;m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
 M[name]=m
parts=[]
def add(o,name,mat):
 o.name=name;o.data.materials.append(M[mat]);parts.append(o)
 for f in o.data.polygons:f.use_smooth=True
 return o

def ball(name,pos,scale,mat,seg=24,rings=16):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=pos);o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return add(o,name,mat)

def tube(name,sections,mat,n=24):
 # Sections are x/y/z center, lateral radius, sagittal radius. Smooth oriented rings.
 vs=[]
 for j,(x,y,z,rx,rz) in enumerate(sections):
  prev=Vector(sections[max(0,j-1)][:3]);nxt=Vector(sections[min(len(sections)-1,j+1)][:3]);direction=(nxt-prev).normalized();a=Vector((1,0,0));b=direction.cross(a).normalized()
  for i in range(n):vs.append(Vector((x,y,z))+a*(rx*math.cos(i*math.tau/n))+b*(rz*math.sin(i*math.tau/n)))
 fs=[]
 for j in range(len(sections)-1):
  for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
 fs.extend([tuple(reversed(range(n))),tuple((len(sections)-1)*n+i for i in range(n))]);me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);return add(o,name,mat)

def pipe(name,pts,r,mat,res=2):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=res;c.resolution_u=8;s=c.splines.new('POLY');s.points.add(len(pts)-1)
 for p,v in zip(s.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');return add(bpy.context.object,name,mat)

def pivot(name,pos,parent=None):
 o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=pos;bpy.context.view_layer.update()
 if parent:o.parent=parent;o.matrix_parent_inverse=parent.matrix_world.inverted()
 bpy.context.view_layer.update();return o

def finish(name,parent):
 global parts
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name+'Mesh';bpy.context.scene.cursor.location=parent.matrix_world.translation;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.parent=parent;o.matrix_parent_inverse=parent.matrix_world.inverted()
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT');mod=o.modifiers.new('Triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name);parts=[];return o

def panel(name,vs,mat,thick=.012):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],[tuple(range(len(vs)))]);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);add(o,name,mat);m=o.modifiers.new('Thickness','SOLIDIFY');m.thickness=thick;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name);return o

root=pivot('Horse',(0,0,0));neck=pivot('Neck',(0,-.63,1.38),root);head=pivot('Head',(0,-1.0,1.93),neck);tail=pivot('Tail',(0,.83,1.45),root)
# Authored barrel, chest and haunch profile. The shoulders are slightly narrower than the barrel.
tube('Anatomical barrel',[(0,.94,1.30,.06,.13),(0,.87,1.29,.23,.28),(0,.66,1.26,.34,.33),(0,.37,1.20,.355,.35),(0,.03,1.18,.34,.36),(0,-.29,1.22,.30,.33),(0,-.55,1.27,.275,.30),(0,-.72,1.26,.18,.24),(0,-.78,1.25,.08,.13)],'Bay',40)
for s in [-1,1]:
 ball('Powerful haunch',(s*.215,.61,1.23),(.195,.295,.29),'Bay')
 ball('Shoulder muscle',(s*.19,-.51,1.21),(.15,.21,.31),'Bay')
# Withers crest sits at 1.55m; saddle below includes a readable leather seat.
tube('Withers',[(0,-.64,1.37,.10,.09),(0,-.48,1.45,.14,.105),(0,-.22,1.45,.15,.06),(0,.05,1.43,.10,.04)],'Bay')
# Fuse torso and muscle masses for a continuous skin surface.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();skin=bpy.context.object
mod=skin.modifiers.new('Unified skin','REMESH');mod.mode='VOXEL';mod.voxel_size=.025;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=skin.modifiers.new('Relax skin','SMOOTH');mod.factor=1.0;mod.iterations=4;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=skin.modifiers.new('Skin budget','DECIMATE');mod.ratio=.45;bpy.ops.object.modifier_apply(modifier=mod.name)
for f in skin.data.polygons:f.use_smooth=True
parts=[skin]
# Woven saddle blanket follows the barrel curvature with dropped corners.
for s in [-1,1]:
 panel('Wool blanket top',[(0,-.35,1.54),(s*.29,-.35,1.49),(s*.29,.46,1.49),(0,.45,1.54)],'Blanket')
 panel('Wool blanket side',[(s*.29,-.35,1.49),(s*.385,-.34,1.28),(s*.385,.46,1.28),(s*.29,.46,1.49)],'Blanket')
 pipe('Blanket binding',[(s*.385,-.34,1.28),(s*.38,.42,1.25),(s*.27,.46,1.47)],.014,'BlanketStripe')
 for y in [-.23,-.19,.29,.33]:pipe('Woven blanket stripe',[(s*.30,y,1.45),(s*.38,y,1.29)],.009,'BlanketStripe')
 panel('Saddle skirt top',[(s*.13,-.28,1.59),(s*.325,-.28,1.51),(s*.325,.31,1.51),(s*.13,.31,1.59)],'Leather')
 panel('Saddle skirt side',[(s*.325,-.28,1.51),(s*.375,-.20,1.39),(s*.375,.27,1.39),(s*.325,.31,1.51)],'Leather')
 pipe('Skirt welt',[(s*.31,-.27,1.48),(s*.345,-.19,1.38),(s*.34,.26,1.37),(s*.27,.34,1.47)],.006,'LeatherEdge')
 # Wide stirrup fenders and real open stirrup irons.
 panel('Stirrup fender',[(s*.29,-.12,1.51),(s*.34,.09,1.47),(s*.425,.09,1.08),(s*.435,-.065,1.03),(s*.38,-.14,1.19)],'Leather')
 pipe('Stirrup iron',[(s*.44,-.07,1.07),(s*.46,-.115,.92),(s*.46,.10,.92),(s*.44,.08,1.07)],.013,'Brass')
 pipe('Stirrup foot tread',[(s*.46,-.115,.925),(s*.46,.10,.925)],.022,'Leather')
 for y in [-.18,.26]:ball('Saddle concho',(s*.381,y,1.43),(.008,.022,.022),'Brass',16,10)
# Cinch passes visibly below the belly.
pipe('Cinch',[(.32,-.16,1.35),(.35,-.16,1.0),(.23,-.16,.87),(0,-.16,.84),(-.23,-.16,.87),(-.35,-.16,1.0),(-.32,-.16,1.35)],.032,'Leather')
ball('Dished saddle seat',(0,.065,1.565),(.225,.295,.075),'Leather')
ball('Raised cantle',(0,.31,1.63),(.225,.070,.12),'Leather');pipe('Cantle rolled edge',[(-.20,.315,1.67),(-.14,.34,1.72),(0,.35,1.75),(.14,.34,1.72),(.20,.315,1.67)],.012,'LeatherEdge')
ball('Saddle pommel',(0,-.23,1.64),(.21,.069,.10),'Leather');tube('Saddle horn',[(0,-.24,1.68,.037,.035),(0,-.25,1.80,.030,.027)],'Leather',20);ball('Horn cap',(0,-.25,1.80),(.058,.045,.018),'LeatherEdge',20,10)
finish('Body',root);pivot('SaddleSeat',(0,.065,1.65),root)
for s,l in [(-1,'L'),(1,'R')]:pivot('Stirrup'+l,(s*.46,0,.94),root)
# Neck rises from the chest with a broad lower attachment and arched crest.
tube('Arched neck',[(0,-.57,1.32,.20,.25),(0,-.67,1.50,.195,.27),(0,-.81,1.71,.15,.24),(0,-.91,1.89,.115,.19),(0,-1.0,2.00,.095,.13)],'Bay',32)
# Flowing mane on the near side: a solid tapered silhouette plus individual locks.
for k in range(15):
 t=k/14;y=-.40-.46*t;z=1.58+.50*t
 tube('Mane lock',[(0,y,z,.035,.026),(-.055,y+.035,z-.07,.032,.025),(-.13+.04*t,y+.065,z-.17,.018,.017),(-.15+.04*t,y+.095,z-.23,.002,.003)],'Dark',10)
finish('Neck',neck)
# Long equine face with flat forehead, pronounced jaw and softer dark muzzle.
tube('Head profile',[(0,-.98,2.04,.09,.10),(0,-1.08,2.02,.135,.15),(0,-1.20,1.91,.12,.155),(0,-1.34,1.78,.091,.12),(0,-1.48,1.65,.085,.095),(0,-1.53,1.62,.07,.073)],'Warm',32)
for s in [-1,1]:ball('Jaw muscle',(s*.078,-1.13,1.87),(.069,.13,.135),'Bay',24,14)
ball('Velvet muzzle',(0,-1.505,1.635),(.098,.104,.083),'Muzzle',28,16)
pipe('Mouth line',[(-.083,-1.52,1.60),(-.048,-1.586,1.591),(0,-1.6,1.588),(.048,-1.586,1.591),(.083,-1.52,1.60)],.003,'Dark')
for s in [-1,1]:
 ball('Nostril',(s*.076,-1.56,1.67),(.023,.025,.014),'Dark',20,12)
 ball('Eye socket',(s*.119,-1.14,2.006),(.025,.039,.029),'Dark',20,12)
 ball('Amber eye',(s*.137,-1.148,2.01),(.008,.019,.016),'Eye',20,12)
 ball('Eye glint',(s*.145,-1.156,2.017),(.0028,.005,.004),'Glint',12,8)
 pipe('Upper eyelid',[(s*.133,-1.175,2.013),(s*.145,-1.15,2.028),(s*.132,-1.12,2.02)],.004,'Warm')
 ear=ball('Alert ear',(s*.081,-.999,2.17),(.032,.049,.108),'Bay',20,14);ear.rotation_euler.y=s*.20
 inset=ball('Soft inner ear',(s*.083,-1.036,2.18),(.019,.013,.069),'Muzzle',16,12);inset.rotation_euler.y=s*.20
 # Bridle straps follow the face, browband and cheek pieces.
 pipe('Cheek strap',[(s*.104,-.998,2.095),(s*.147,-1.095,2.058),(s*.132,-1.25,1.887),(s*.103,-1.447,1.683)],.010,'Leather')
 ball('Bridle buckle',(s*.148,-1.183,1.963),(.007,.018,.025),'Brass',12,8)
 pipe('Bit ring',[(s*.108,-1.452+.029*math.cos(i*math.tau/24),1.647+.029*math.sin(i*math.tau/24)) for i in range(25)],.004,'Brass')
pipe('Browband',[(-.139,-1.08,2.07),(-.08,-1.145,2.085),(0,-1.16,2.091),(.08,-1.145,2.085),(.139,-1.08,2.07)],.011,'Leather')
pipe('Noseband',[(-.093,-1.43,1.743),(-.06,-1.49,1.769),(0,-1.51,1.783),(.06,-1.49,1.769),(.093,-1.43,1.743)],.012,'Leather')
# Small irregular white star rather than a painted cartoon eye stripe.
panel('White forehead star',[(-.015,-1.224,2.031),(0,-1.246,2.026),(.018,-1.235,2.014),(.012,-1.260,1.982),(0,-1.269,1.961),(-.014,-1.255,1.990)],'Ivory',.001)
for k in range(5):pipe('Forelock',[(-.034+k*.017,-1.041,2.12),(-.042+k*.017,-1.14,2.09),(-.015+k*.010,-1.20,2.025)],.014,'Dark')
finish('Head',head)
# Reins hang back to the pommel; separate rigid mesh under the body for easy later replacement.
for s in [-1,1]:pipe('Loose leather rein',[(s*.108,-1.452,1.647),(s*.19,-1.13,1.52),(s*.22,-.76,1.49),(s*.18,-.44,1.60),(s*.09,-.23,1.75)],.005,'Leather')
finish('Reins',root)
# Four articulated legs. Knees/hocks own lower-leg meshes so runtime gait can bend naturally.
for s,l in [(-1,'L'),(1,'R')]:
 x=s*.225
 for front in [True,False]:
  label=('Front' if front else 'Hind');y=-.55 if front else .63;z=1.27 if front else 1.28
  p=pivot(label+'Leg'+l,(x,y,z),root);ky=y-.015 if front else y+.16;kz=.58 if front else .56
  tube('Upper '+label+' leg',[(x,y,z,.098,.11),(x,y+.015,1.08,.105,.12),(x,y-.025,.90,.072,.092),(x,ky,.70,.042,.056),(x,ky,kz,.046,.045)],'Bay',24)
  ball('Knee joint',(x,ky,kz),(.052,.060,.059),'Dark',20,12);finish(label+'Leg'+l,p)
  knee=pivot(label+'Knee'+l,(x,ky,kz),p);fy=y-.025
  tube('Cannon and fetlock',[(x,ky,kz,.043,.042),(x,ky,.45,.031,.037),(x,fy+.025,.25,.028,.035),(x,fy+.020,.17,.043,.045),(x,fy-.020,.10,.043,.040)],'Dark',20)
  if not front and s==-1:tube('Single white sock',[(x,fy+.025,.27,.033,.040),(x,fy+.021,.21,.036,.041),(x,fy+.017,.16,.045,.047),(x,fy-.02,.105,.044,.042)],'Ivory',20)
  tube('Hoof',[(x,fy-.013,.126,.045,.050),(x,fy-.025,.10,.056,.065),(x,fy-.039,.027,.063,.078),(x,fy-.038,.015,.061,.077)],'Hoof',24)
  pipe('Hoof growth ridge',[(x+.061*math.cos(i*math.tau/24),fy-.037+.074*math.sin(i*math.tau/24),.040) for i in range(25)],.002,'Muzzle')
  finish(label+'Knee'+l,knee)
tube('Tail dock',[(0,.84,1.45,.066,.066),(0,.99,1.31,.055,.061),(0,1.055,1.06,.073,.062),(0,1.065,.74,.08,.057),(0,1.10,.46,.070,.046),(0,1.13,.29,.025,.025)],'Dark',24)
for k in range(14):
 a=k*math.tau/14
 pipe('Tail strand',[(.055*math.cos(a),.985+.045*math.sin(a),1.32),(.071*math.cos(a),1.07+.054*math.sin(a),.84),(.073*math.cos(a+.2),1.12+.047*math.sin(a),.45),(.024*math.cos(a),1.15+.025*math.sin(a),.26)],.007,'Dark',1)
finish('Tail',tail)
objects=[root]+list(root.children_recursive)
expected_pivots={o.name:tuple(o.matrix_world.translation) for o in objects if o.type=='EMPTY'}
def world_bounds(meshes):
 points=[o.matrix_world@v.co for o in meshes if o.type=='MESH' for v in o.data.vertices]
 return ([min(v[i] for v in points) for i in range(3)],[max(v[i] for v in points) for i in range(3)])
expected_bounds=world_bounds(objects)
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'Horse.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True,object_types={'MESH','EMPTY'})
stats={'triangles':sum(len(o.data.polygons) for o in objects if o.type=='MESH'),'withers_m':1.55,'seat_m':1.65,'forward':'Unity +Z','pivots':[o.name for o in objects if o.type=='EMPTY'],'provenance':'Original scripted geometry and textures; no third-party assets.'};(OUT/'mesh-budget.json').write_text(json.dumps(stats,indent=2)+'\n')
# Retain studio scene in editable source; studio objects are excluded from FBX.
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.008));bpy.context.object.data.materials.append(M['Blanket'])
world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.60,.67,.75,1);world.node_tree.nodes['Background'].inputs[1].default_value=.5
for pos,energy,size in [((-3,-4,6),1000,5),((4,1,4),850,4)]:
 bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=energy;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(-4,-5,2.7));cam=bpy.context.object;cam.rotation_euler=(Vector((0,-.15,1.1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=3.65;bpy.context.scene.camera=cam
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.view_settings.exposure=-.8;scene.render.filepath=str(SOURCE/'Horse-preview.png')
bpy.ops.file.pack_all();bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Horse.blend'));bpy.ops.render.render(write_still=True);print(json.dumps(stats,indent=2))

# Validate the exported file, not merely the editable source. Space baking corrupts
# empty-parented mesh transforms in FBX; keep the hierarchy and axis conversion intact.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(OUT/'Horse.fbx'))
imported=list(bpy.context.scene.objects);actual_bounds=world_bounds(imported)
for expected,actual in zip(expected_bounds,actual_bounds):
 assert max(abs(a-b) for a,b in zip(expected,actual))<.001,(expected_bounds,actual_bounds)
for name,expected in expected_pivots.items():
 obj=bpy.data.objects.get(name);assert obj is not None,name
 assert (obj.matrix_world.translation-Vector(expected)).length<.001,(name,tuple(obj.matrix_world.translation),expected)
lo,hi=actual_bounds
assert 1.8<hi[2]-lo[2]<2.6 and .5<hi[0]-lo[0]<2,actual_bounds
print('FBX roundtrip validated: '+json.dumps({'blender_bounds_m':actual_bounds,'unity_seat_m':[expected_pivots['SaddleSeat'][0],expected_pivots['SaddleSeat'][2],-expected_pivots['SaddleSeat'][1]],'named_pivots':len(expected_pivots)}))
