# ShiftStorm Round 5: a stand-in for a Tripo download, to test Tools/import_vehicles.py before Dan's models exist.
#   blender -b -P Tools/Blender/make_test_vehicle.py -- <ShiftVehicle.fbx> <out dir> <design name>
# Takes the gray-box part model (all parts out: the same shape for every form), joins it into ONE mesh with ONE texture
# (green body, cream panels, as Dan's prompts ask) and saves it the way a Tripo export can arrive: the wrong size (0.37 x), the front
# facing -Y (glTF / Tripo convention, which the importer turns round by its default yaw 180), off-centre and below the floor.
import bpy, sys, os, math

argv = sys.argv[sys.argv.index("--") + 1:]
src, out_dir, design = argv[0], argv[1], argv[2]
os.makedirs(os.path.join(out_dir, design), exist_ok=True)

for form in ("Car", "Boat", "Plane", "Sub"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=src)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    # one texture: 2 x 2 colours (green body, cream panels, silver, charcoal), each part mapped into one cell
    img = bpy.data.images.new("tex", 64, 64); px = []
    cells = [(.12, .5, .28), (.95, .92, .82), (.7, .72, .75), (.12, .12, .13)]
    for y in range(64):
        for x in range(64):
            c = cells[(1 if x >= 32 else 0) + (2 if y >= 32 else 0)]; px += [c[0], c[1], c[2], 1]
    img.pixels = px
    mat = bpy.data.materials.new("Tripo"); mat.use_nodes = True
    t = mat.node_tree.nodes.new("ShaderNodeTexImage"); t.image = img
    mat.node_tree.links.new(t.outputs["Color"], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    for o in meshes:
        cell = 0 if o.name.startswith(("Body", "Wing", "Tail")) else 1 if o.name.startswith(("Cockpit", "Fin", "Keel")) else 3 if o.name.startswith("Wheel") else 2
        u0, v0 = (cell % 2) * .5 + .1, (cell // 2) * .5 + .1
        o.data.materials.clear(); o.data.materials.append(mat)
        if not o.data.uv_layers: o.data.uv_layers.new()
        for l in o.data.uv_layers[0].data: l.uv = (u0 + .15, v0 + .15)
    for o in bpy.data.objects: o.select_set(o.type == "MESH")
    bpy.context.view_layer.objects.active = meshes[0]; bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active; obj.name = "tripo_mesh"; bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # the wrong size, facing -Y, off-centre
    obj.rotation_euler = (0, 0, math.radians(180)); obj.scale = (.37, .37, .37); obj.location = (.4, -.9, -.3)
    root = bpy.data.objects.new("Root", None); bpy.context.scene.collection.objects.link(root); obj.parent = root
    path = os.path.join(out_dir, design, f"{design}_{form}.fbx")
    img.filepath_raw = os.path.join(out_dir, design, f"{design}_{form}_tex.png"); img.file_format = "PNG"; img.save()
    bpy.ops.export_scene.fbx(filepath=path, object_types={"MESH", "EMPTY"}, path_mode="COPY", embed_textures=True)
    print("[make_test_vehicle] wrote", path)
