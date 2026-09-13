"""Called by tree importer: flatten source vertex-mask / triplanar trunk material to a UV atlas."""
import bpy,bmesh

def bake_trunk(mesh,name,output):
    trunk_index=next(i for i,m in enumerate(mesh.materials) if m and m.name.endswith('_trunk_a'))
    pieces=[]
    for trunk in [False,True]:
        data=mesh.copy();bm=bmesh.new();bm.from_mesh(data)
        bmesh.ops.delete(bm,geom=[f for f in bm.faces if (f.material_index==trunk_index)!=trunk],context='FACES')
        bm.to_mesh(data);bm.free()
        o=bpy.data.objects.new('Trunk bake' if trunk else 'Tree remainder',data);bpy.context.collection.objects.link(o);pieces.append(o)
    o=pieces[1];bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    uv=o.data.uv_layers.new(name='BakedTrunk');o.data.uv_layers.active=uv;uv.active_render=True
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.018);bpy.ops.object.mode_set(mode='OBJECT')
    material=o.data.materials[trunk_index]
    o.data.materials.clear();o.data.materials.append(material)
    for polygon in o.data.polygons:polygon.material_index=0
    nodes=material.node_tree.nodes;links=material.node_tree.links
    for n in nodes:
        if n.type=='NORMAL_MAP':n.uv_map='UVMap'
    surface=next(n for n in nodes if n.type=='OUTPUT_MATERIAL');original=surface.inputs['Surface'].links[0].from_socket
    mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MIX'
    color_nodes=[n for n in nodes if n.type=='TEX_IMAGE' and n.image and '_diff_' in n.image.filepath]
    base=next(n for n in color_nodes if '_trunk_a_' in n.image.filepath)
    bark=next(n for n in color_nodes if '_bark_' in n.image.filepath)
    vertex=next(n for n in nodes if n.type=='VERTEX_COLOR')
    links.new(vertex.outputs['Color'],mix.inputs[0]);links.new(base.outputs['Color'],mix.inputs[1]);links.new(bark.outputs['Color'],mix.inputs[2])
    emission=nodes.new('ShaderNodeEmission');links.new(mix.outputs[0],emission.inputs['Color']);links.new(emission.outputs[0],surface.inputs['Surface'])
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.render.bake.margin=12;scene.render.bake.use_selected_to_active=False
    target=nodes.new('ShaderNodeTexImage')
    for kind,bake_type in [('BaseColor','EMIT'),('Normal','NORMAL')]:
        image=bpy.data.images.new(name+'_trunk_a_'+kind,width=2048,height=2048,alpha=False)
        image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
        target.image=image;nodes.active=target
        if kind=='Normal':links.new(original,surface.inputs['Surface'])
        bpy.ops.object.bake(type=bake_type,uv_layer='BakedTrunk')
        image.filepath_raw=str(output/(name+'_trunk_a_'+kind+'.png'));image.file_format='PNG';image.save()
    # UVMap is the exported channel. Keep the baked coordinates after source shading is flattened.
    baked=[tuple(v.uv) for v in o.data.uv_layers['BakedTrunk'].data]
    o.data.uv_layers.remove(o.data.uv_layers['BakedTrunk'])
    for v,coordinate in zip(o.data.uv_layers['UVMap'].data,baked):v.uv=coordinate
    o.data.uv_layers.active_index=0
    for p in pieces:p.select_set(True)
    bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join()
    result=pieces[0].data.copy();bpy.data.objects.remove(pieces[0],do_unlink=True)
    return result
