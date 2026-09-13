"""Bake original directional coat/feather detail on the seven existing animal sculpts.
Run after the base generators and before rig-animals.py. No external art inputs.
Preserves named slots; a single full-surface color/normal atlas serves both LODs.
"""
from pathlib import Path
import bpy, math, json, os, tempfile, sys
from mathutils import Vector
try:bpy.context.scene.view_settings.view_transform='AgX'
except TypeError:
    config=Path('/usr/share/blender/5.2/datafiles/colormanagement/config.ocio')
    text=config.read_text().replace('ocio_profile_version: 2.5','ocio_profile_version: 2.4').replace('search_path: "icc:luts:filmic"',f'search_path: "{config.parent}/icc:{config.parent}/luts:{config.parent}/filmic"')
    fd,path=tempfile.mkstemp(prefix='detail-ocio-',suffix='.ocio')
    with os.fdopen(fd,'w') as f:f.write(text)
    os.environ['OCIO']=path
    os.execv(bpy.app.binary_path,[bpy.app.binary_path,'-b','-t','4','--python',str(Path(__file__).resolve())]+(sys.argv[sys.argv.index('--'):] if '--' in sys.argv else []))
ROOT=Path(__file__).resolve().parents[2]
SPECS=[('Clover','Clover.blend','',4400),('Hen','Hen.blend','',2496),('Fox','Fox/Fox.blend','Fox',2850),('Deer','Wildlife/Wildlife.blend','Wildlife',2600),('Rabbit','Wildlife/Wildlife.blend','Wildlife',1600),('Duck','RiverWildlife/RiverWildlife.blend','RiverWildlife',2400),('Beaver','RiverWildlife/RiverWildlife.blend','RiverWildlife',2600)]
requested=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [s[0] for s in SPECS]
report_path=ROOT/'ArtSource/Animation/detail-stats.json'
report=json.loads(report_path.read_text()) if report_path.exists() else {}
def apply(o,m):
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
def procedural(m,name,old_uv):
    nodes=m.node_tree.nodes;links=m.node_tree.links
    bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
    # Existing color atlases keep reading their original UV while the bake writes a new atlas.
    uv=nodes.new('ShaderNodeUVMap');uv.uv_map=old_uv
    for n in list(nodes):
        if n.type=='TEX_IMAGE':links.new(uv.outputs['UV'],n.inputs['Vector'])
    base=bs.inputs['Base Color'].links[0].from_socket if bs.inputs['Base Color'].is_linked else None
    if base is None:
        rgb=nodes.new('ShaderNodeRGB');rgb.outputs[0].default_value=bs.inputs['Base Color'].default_value;base=rgb.outputs[0]
    glossy=any(s in m.name.lower() for s in ['eye','pupil','glint','nostril','nose','charcoal','dark','incisor'])
    skin=any(s in m.name.lower() for s in ['hoof','horn','antler','udder','ear','bill','ochre','comb','wattle','leg','beak'])
    if not glossy:
        geom=nodes.new('ShaderNodeNewGeometry')
        stretch=nodes.new('ShaderNodeVectorMath');stretch.operation='MULTIPLY'
        scale=100 if name=='Clover' else 170 if name=='Deer' else 260
        stretch.inputs[1].default_value=(scale,scale*.07,scale*.85)
        links.new(geom.outputs['Position'],stretch.inputs[0])
        grain=nodes.new('ShaderNodeTexNoise');grain.inputs['Scale'].default_value=1;grain.inputs['Detail'].default_value=2.5;grain.inputs['Roughness'].default_value=.72;links.new(stretch.outputs[0],grain.inputs['Vector'])
        fine=nodes.new('ShaderNodeValToRGB');fine.color_ramp.elements[0].position=.22;fine.color_ramp.elements[0].color=(.48,.48,.48,1);fine.color_ramp.elements[1].position=.78;fine.color_ramp.elements[1].color=(1.15,1.15,1.15,1);links.new(grain.outputs['Fac'],fine.inputs[0])
        mul=nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=.24 if skin else .66;links.new(base,mul.inputs[1]);links.new(fine.outputs[0],mul.inputs[2]);links.new(mul.outputs[0],bs.inputs['Base Color'])
        coarse=nodes.new('ShaderNodeTexNoise');coarse.inputs['Scale'].default_value=7 if name=='Clover' else 15;coarse.inputs['Detail'].default_value=3;links.new(geom.outputs['Position'],coarse.inputs['Vector'])
        ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.63,.60,.55,1);ramp.color_ramp.elements[1].color=(1.06,1.025,.98,1);links.new(coarse.outputs['Fac'],ramp.inputs[0])
        mottled=nodes.new('ShaderNodeMixRGB');mottled.blend_type='MULTIPLY';mottled.inputs[0].default_value=.22 if skin else .48;links.new(mul.outputs[0],mottled.inputs[1]);links.new(ramp.outputs[0],mottled.inputs[2]);links.new(mottled.outputs[0],bs.inputs['Base Color'])
        bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12 if skin else .19;bump.inputs['Distance'].default_value=.003 if name=='Clover' else .0015;links.new(grain.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])
        bs.inputs['Roughness'].default_value=.78 if skin else .91
        if 'Sheen Weight' in bs.inputs:bs.inputs['Sheen Weight'].default_value=0 if skin else .18
    else:bs.inputs['Roughness'].default_value=.25
    return bs
for name,src,folder,budget in SPECS:
    if name not in requested:continue
    source=ROOT/'ArtSource'/src;bpy.ops.wm.open_mainfile(filepath=str(source))
    high=bpy.data.objects[name+'_LOD0']
    if high.get('detail_revision')==1:
        print('Already detailed:',name);continue
    high.hide_set(False);high.hide_render=False
    bpy.ops.object.select_all(action='DESELECT');high.select_set(True);bpy.context.view_layer.objects.active=high
    old=high.data.uv_layers.active
    if old is None:old=high.data.uv_layers.new(name='OriginalUV')
    old.name='OriginalUV'
    shaders=[procedural(m,name,old.name) for m in high.data.materials]
    atlas=high.data.uv_layers.new(name='DetailUV');high.data.uv_layers.active=atlas;atlas.active_render=True
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.05,island_margin=.007);bpy.ops.object.mode_set(mode='OBJECT')
    distance=bpy.data.objects[name+'_LOD1'] if name=='Hen' else None
    if distance is not None:
        # Give the authored feather-sheet LOD its own quarter of the same atlas.
        # Projecting across the near mesh's tiny feather UV islands creates seams.
        for loop in high.data.uv_layers.active.data:loop.uv.x*=.75
        high.select_set(False);distance.hide_set(False);distance.hide_render=False;distance.select_set(True);bpy.context.view_layer.objects.active=distance
        for layer in list(distance.data.uv_layers):distance.data.uv_layers.remove(layer)
        distance.data.uv_layers.new(name='DetailUV')
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.05,island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT')
        for loop in distance.data.uv_layers.active.data:loop.uv.x=.75+loop.uv.x*.25
        distance.select_set(False);high.select_set(True);bpy.context.view_layer.objects.active=high
    out=Path(os.environ.get('ANIMAL_DETAIL_OUTPUT',str(ROOT/'Assets/Resources')))/folder
    out.mkdir(parents=True,exist_ok=True)
    color=bpy.data.images.new(name+'Detail_BaseColor',width=2048,height=2048);color.filepath_raw=str(out/(name+'Detail_BaseColor.png'));color.file_format='PNG'
    normal=bpy.data.images.new(name+'Detail_Normal',width=1024,height=1024);normal.colorspace_settings.name='Non-Color';normal.filepath_raw=str(out/(name+'Detail_Normal.png'));normal.file_format='PNG'
    sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=1;sc.render.bake.margin=12
    bake_nodes=[]
    for mat in high.data.materials:
        n=mat.node_tree.nodes.new('ShaderNodeTexImage');n.image=color;mat.node_tree.nodes.active=n;bake_nodes.append(n)
    sc.render.bake.use_pass_direct=False;sc.render.bake.use_pass_indirect=False;sc.render.bake.use_pass_color=True
    def bake_atlas(kind):
        sc.render.bake.use_clear=True;bpy.ops.object.bake(type=kind)
        if distance is not None:
            high.select_set(False);distance.select_set(True);bpy.context.view_layer.objects.active=distance
            sc.render.bake.use_clear=False;bpy.ops.object.bake(type=kind)
            distance.select_set(False);high.select_set(True);bpy.context.view_layer.objects.active=high
    bake_atlas('DIFFUSE');color.save();color.pack()
    for n,m in zip(bake_nodes,high.data.materials):n.image=normal;m.node_tree.nodes.active=n
    bake_atlas('NORMAL');normal.save();normal.pack()
    # Replace procedural shading with exactly the portable atlases Unity receives.
    for m,bs in zip(high.data.materials,shaders):
        rough=bs.inputs['Roughness'].default_value;nodes=m.node_tree.nodes;links=m.node_tree.links;nodes.clear()
        output=nodes.new('ShaderNodeOutputMaterial');bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=rough
        uv=nodes.new('ShaderNodeUVMap');uv.uv_map='DetailUV';tex=nodes.new('ShaderNodeTexImage');tex.image=color;links.new(uv.outputs[0],tex.inputs[0]);links.new(tex.outputs[0],bs.inputs['Base Color'])
        nt=nodes.new('ShaderNodeTexImage');nt.image=normal;links.new(uv.outputs[0],nt.inputs[0]);nm=nodes.new('ShaderNodeNormalMap');nm.uv_map='DetailUV';links.new(nt.outputs[0],nm.inputs[1]);links.new(nm.outputs[0],bs.inputs['Normal']);links.new(bs.outputs[0],output.inputs[0])
    # Exporters select the first UV channel. Remove the superseded pigment UV.
    high.data.uv_layers.remove(high.data.uv_layers['OriginalUV'])
    while len(high.data.uv_layers)>1:
        high.data.uv_layers.remove(next(u for u in high.data.uv_layers if u.name!='DetailUV'))
    oldlow=bpy.data.objects[name+'_LOD1']
    if name=='Hen':
        low=oldlow
    else:
        bpy.data.objects.remove(oldlow,do_unlink=True)
        low=high.copy();low.data=high.data.copy();bpy.context.collection.objects.link(low);low.name=name+'_LOD1'
        count=sum(len(p.vertices)-2 for p in low.data.polygons);dec=low.modifiers.new('Detail distance budget','DECIMATE');dec.ratio=min(1,budget/count);apply(low,dec)
    low.hide_render=True;low.hide_set(True)
    assert sum(len(p.vertices)-2 for p in low.data.polygons)<=budget+4,(name,'Distance budget')
    high['detail_revision']=1
    for ob in bpy.context.scene.objects:
        if ob.type=='MESH' and ob.name.endswith(('_LOD0','_LOD1')):ob.hide_render=ob!=high
    sc.cycles.samples=20;sc.render.resolution_x=1100;sc.render.resolution_y=1000;sc.render.resolution_percentage=100
    bounds=[high.matrix_world@Vector(v) for v in high.bound_box];center=sum(bounds,Vector())/8;size=max(high.dimensions)
    cam=sc.camera;cam.location=center+Vector((size*1.65,-size*2,size*.72));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=size*1.17
    source=ROOT/'ArtSource/Detail'/f'{name}.blend'
    source.parent.mkdir(exist_ok=True)
    # Separate detail studios avoid cross-species shared material mutation.
    for ob in list(bpy.context.scene.objects):
        if ob.type=='MESH' and ob.name.endswith(('_LOD0','_LOD1')) and ob not in [high,low]:bpy.data.objects.remove(ob,do_unlink=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True)
    sc.render.filepath=str(ROOT/'ArtSource/Animation'/f'{name}-detail.png');bpy.ops.render.render(write_still=True)
    report[name]={'LOD0_triangles':sum(len(p.vertices)-2 for p in high.data.polygons),'LOD1_triangles':sum(len(p.vertices)-2 for p in low.data.polygons),'materials':[m.name for m in high.data.materials],'base_color':str(Path(folder)/(name+'Detail_BaseColor.png')),'normal':str(Path(folder)/(name+'Detail_Normal.png'))}
    backup=source.with_suffix('.blend1')
    if backup.exists():backup.unlink()
(ROOT/'ArtSource/Animation/detail-stats.json').write_text(json.dumps(report,indent=2)+'\n')
