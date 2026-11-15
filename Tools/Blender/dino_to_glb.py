# ShiftStorm Round 9: Dan's Tripo dinosaur (FBX in a zip, ~2 M faces) -> one mesh, decimated, transforms applied, as the GLB Tee'd Off's rigger
# reads (Tools/Characters/rig_dino.py: <Name>_meshy.glb with one primitive, Y up) + its base colour as <Name>_meshy_basecolor.png.
#   blender -b -P Tools/Blender/dino_to_glb.py -- <model .fbx/.glb> <out dir> <Name> [faces, default 24000]
import bpy, sys, os
argv = sys.argv[sys.argv.index("--") + 1:]
src, out_dir, name = argv[0], argv[1], argv[2]; faces = int(argv[3]) if len(argv) > 3 else 24000
os.makedirs(out_dir, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
if src.lower().endswith(".fbx"): bpy.ops.import_scene.fbx(filepath=src)
else: bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
for o in meshes: mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
for o in [o for o in bpy.data.objects if o.type != "MESH"]: bpy.data.objects.remove(o, do_unlink=True)
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1: bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
n0 = len(obj.data.polygons)
if n0 > faces:
    m = obj.modifiers.new("cap", "DECIMATE"); m.ratio = faces / n0; bpy.ops.object.modifier_apply(modifier="cap")
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT"); bpy.ops.mesh.quads_convert_to_tris(); bpy.ops.object.mode_set(mode="OBJECT")
# one material slot (one glTF primitive)
mat = obj.data.materials[0] if obj.data.materials else None
tex = None
for m in obj.data.materials:
    if m and m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type == "TEX_IMAGE" and n.image: tex = n.image; break
    if tex: break
while len(obj.data.materials) > 1: obj.data.materials.pop(index=len(obj.data.materials) - 1)
for p in obj.data.polygons: p.material_index = 0
if tex:
    path = os.path.abspath(os.path.join(out_dir, f"{name}_meshy_basecolor.png")); tex.filepath_raw = path; tex.file_format = "PNG"
    try: tex.save()
    except Exception: tex.save_render(path)
bpy.ops.export_scene.gltf(filepath=os.path.join(out_dir, f"{name}_meshy.glb"), export_format="GLB", use_selection=False, export_yup=True,
                          export_normals=True, export_materials="NONE", export_apply=True)
print(f"[dino_to_glb] {name}: {n0} faces -> {len(obj.data.polygons)}; texture {'saved' if tex else 'NONE'}")
