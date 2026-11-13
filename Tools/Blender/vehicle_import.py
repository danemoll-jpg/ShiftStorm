# ShiftStorm Round 5: clean up one Tripo vehicle model (one form of one design) for the game. Run by Tools/import_vehicles.py:
#   blender -b -P Tools/Blender/vehicle_import.py -- <src .fbx/.glb/.obj> <out dir> <form: Car|Boat|Plane|Sub> <yaw deg> <seat x,y,z or auto> <preview.png>
# What it does: joins every mesh into one, applies the transforms, turns it by <yaw> about the vertical (the game's convention in Blender:
# +Y forward, +Z up, +X right), scales it to 4.0 m long (the hull collider is 4.0 m), centres it on the hull (x, y) and puts its floor at the
# form's floor height (car: the wheels' bottom; boat: the keel; plane / sub: the belly), adds a "Seat" empty (where Abi's hips go), makes a
# second LOD (about 35 % of the faces), saves the base-colour texture as <Form>_basecolor.png and exports <Form>.fbx (LOD0, LOD1, Seat)
# in the same axes as Tools/Blender/shiftstorm_vehicle.py. It renders a preview: side view and top view (front to the right / top).
import bpy, sys, os, math, mathutils

argv = sys.argv[sys.argv.index("--") + 1:]
src, out_dir, form, yaw, seat_arg, preview = argv[0], argv[1], argv[2], float(argv[3]), argv[4], argv[5]
LENGTH = 4.0
FLOOR = {"Car": -0.85, "Boat": -0.62, "Plane": -0.55, "Sub": -0.6}[form]   # the floor of each placeholder form, metres under the hull's centre
os.makedirs(out_dir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
ext = os.path.splitext(src)[1].lower()
if ext == ".fbx": bpy.ops.import_scene.fbx(filepath=src)
elif ext in (".glb", ".gltf"): bpy.ops.import_scene.gltf(filepath=src)
elif ext == ".obj": bpy.ops.wm.obj_import(filepath=src)
else: raise SystemExit("unknown model type " + ext)

meshes = [o for o in bpy.data.objects if o.type == "MESH"]
if not meshes: raise SystemExit("no mesh in " + src)
for o in bpy.data.objects: o.select_set(False)
# keep the world transforms, drop parents (Tripo FBX / GLB come with a root node)
for o in meshes:
    mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
for o in [o for o in bpy.data.objects if o.type != "MESH"]: bpy.data.objects.remove(o, do_unlink=True)
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1: bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active; obj.name = "LOD0"
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
faces_in = len(obj.data.polygons)
# Tripo's detailed meshes run to several hundred thousand faces: cap the game mesh (Steam Deck budget, four cars on screen)
MAX_FACES = 20000
if faces_in > MAX_FACES:
    d0 = obj.modifiers.new("cap", "DECIMATE"); d0.ratio = MAX_FACES / faces_in; bpy.ops.object.modifier_apply(modifier="cap")

# turn, scale to length, centre, floor
obj.rotation_euler = (0, 0, math.radians(yaw)); bpy.ops.object.transform_apply(rotation=True)
def bounds(o):
    vs = [o.matrix_world @ v.co for v in o.data.vertices]
    return mathutils.Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs))), mathutils.Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
lo, hi = bounds(obj); size = hi - lo
s = LENGTH / max(size.y, 1e-6)
obj.scale = (s, s, s); bpy.ops.object.transform_apply(scale=True)
lo, hi = bounds(obj); size = hi - lo
obj.location = (-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, FLOOR - lo.z); bpy.ops.object.transform_apply(location=True)
lo, hi = bounds(obj); size = hi - lo
wide = size.x > size.y * 1.15   # wider than long: very likely turned 90 deg (a plane's wings can do this too: the preview decides)

# seat: given, or found: Abi's hips 60 % back from the nose (the open cockpits of Dan's Jetstream models sit there) and 0.42 m under
# the hull's top (her head then clears the cockpit rim); the hull's top is the highest point within 0.5 m of the centre line around
# that spot (the tail fins are left out)
if seat_arg != "auto": seat = mathutils.Vector([float(c) for c in seat_arg.split(",")])
else:
    sy = hi.y - size.y * .6
    near = [obj.matrix_world @ v.co for v in obj.data.vertices]
    near = [v.z for v in near if abs(v.x) < .5 and abs(v.y - sy) < .6]
    top = max(near) if near else lo.z + size.z * .8
    seat = mathutils.Vector((0, sy, top - .42))
e = bpy.data.objects.new("Seat", None); bpy.context.scene.collection.objects.link(e); e.location = seat

# base colour texture: the first image used by the materials
tex_out = None
for m in obj.data.materials:
    if not m or not m.use_nodes: continue
    for n in m.node_tree.nodes:
        if n.type == "TEX_IMAGE" and n.image:
            img = n.image; tex_out = os.path.join(out_dir, form + "_basecolor.png")
            img.filepath_raw = tex_out; img.file_format = "PNG"
            try: img.save()
            except Exception:
                img.save_render(tex_out)
            break
    if tex_out: break

# LOD1
lod1 = obj.copy(); lod1.data = obj.data.copy(); lod1.name = "LOD1"; bpy.context.scene.collection.objects.link(lod1)
dec = lod1.modifiers.new("dec", "DECIMATE"); dec.ratio = .3
bpy.ops.object.select_all(action="DESELECT"); lod1.select_set(True); bpy.context.view_layer.objects.active = lod1; bpy.ops.object.modifier_apply(modifier="dec")
faces0, faces1 = len(obj.data.polygons), len(lod1.data.polygons)

out_fbx = os.path.join(out_dir, form + ".fbx")
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=True, object_types={"MESH", "EMPTY"}, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, use_mesh_modifiers=True, path_mode="STRIP")

# preview: side (front to the right) and top (front up), workbench, texture colours
lod1.hide_render = True
sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"; sc.display.shading.light = "STUDIO"; sc.display.shading.color_type = "TEXTURE" if tex_out else "MATERIAL"
sc.render.resolution_x, sc.render.resolution_y = 640, 400; sc.render.film_transparent = False
w = bpy.data.worlds.new("w"); sc.world = w; w.color = (.85, .87, .9)
cam = bpy.data.cameras.new("c"); cam.type = "ORTHO"; cam.ortho_scale = 5.2; co = bpy.data.objects.new("cam", cam); sc.collection.objects.link(co); sc.camera = co
base, _ = os.path.splitext(preview); shots = []
for name, loc, rot in (("side", (12, 0, 0), (math.radians(90), 0, math.radians(90))), ("top", (0, 0, 12), (0, 0, math.radians(-90))), ("three-quarter", (7, 7, 4), None)):
    co.location = loc
    if rot: co.rotation_euler = rot
    else:
        d = mathutils.Vector((0, 0, 0)) - mathutils.Vector(loc); co.rotation_euler = d.to_track_quat("-Z", "Y").to_euler(); cam.ortho_scale = 6
    sc.render.filepath = f"{base}-{name}.png"; bpy.ops.render.render(write_still=True); shots.append(sc.render.filepath)
print(f"[vehicle_import] {form}: {os.path.basename(src)} faces {faces_in} -> LOD0 {faces0}, LOD1 {faces1}; size {size.x:.2f} x {size.y:.2f} x {size.z:.2f} m "
      f"(scale x{s:.3f}, yaw {yaw:.0f}); seat {tuple(round(c, 2) for c in seat)}; texture {'saved' if tex_out else 'none'}{'; WIDER THAN LONG: check the facing' if wide else ''}")
print("[vehicle_import] wrote", out_fbx, "previews", ", ".join(shots))
