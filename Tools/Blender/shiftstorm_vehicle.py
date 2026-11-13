"""ShiftStorm Round 1 placeholder transforming vehicle, built by script (hard-surface, simple shapes, separate parts).
Run: F:\\blender\\blender.exe -b -P Tools/Blender/shiftstorm_vehicle.py -- <repo root>
Writes Assets/Resources/Models/ShiftVehicle.fbx and Docs/Checks/round1/vehicle_rest.png (a preview of the rest pose).

Parts (names are what Assets/Scripts/Vehicle/FormAnimator.cs looks for), each with its origin at its hinge, rotation and scale applied:
  Body, Cockpit (shared, never move), Canopy (hinge at its back edge), Wheel_FL/FR/RL/RR (axle along X), Wing_L/R (root at the body side,
  reaching out), Tail (fin up), Fin_L/R (hull strakes reaching down), Keel (under the hull), Prop (rear propeller).
Rest pose = wheels down, wings and tail out, fins, keel and prop down, canopy closed; the FormAnimator folds what each form does not use.
Coordinates below are written in Unity's frame (x right, y up, z forward) and converted with B(): Blender x = x, y = z, z = y (what Unity's FBX import with axis conversion baked maps back), so the nose
points to Blender +Y."""
import bpy, bmesh, math, os, sys

root = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else os.getcwd()
out_fbx = os.path.join(root, "Assets", "Resources", "Models", "ShiftVehicle.fbx")
out_png = os.path.join(root, "Docs", "Checks", "round1", "vehicle_rest.png")
os.makedirs(os.path.dirname(out_fbx), exist_ok=True); os.makedirs(os.path.dirname(out_png), exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

def B(x, y, z): return (x, z, y)

def mat(name, rgb):
    m = bpy.data.materials.new(name); m.use_nodes = True; m.diffuse_color = (*rgb, 1)
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*rgb, 1); return m

MATS = {"body": mat("Body", (0.95, 0.45, 0.10)), "trim": mat("Trim", (0.95, 0.9, 0.8)), "glass": mat("Glass", (0.3, 0.6, 0.85)),
        "tire": mat("Tire", (0.08, 0.08, 0.09)), "wing": mat("Wing", (0.95, 0.95, 0.95)), "fin": mat("Fin", (0.1, 0.6, 0.65)), "metal": mat("Metal", (0.55, 0.57, 0.6))}

def finish(obj, name, hinge, material):
    """Origin to the hinge, transforms applied, material set."""
    obj.name = name; obj.data.name = name
    bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True); bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    scene.cursor.location = B(*hinge); bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    obj.data.materials.clear(); obj.data.materials.append(MATS[material])
    return obj

def box(name, center, size, hinge, material, bevel=0.06, taper_front=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=B(*center))
    o = bpy.context.active_object; o.scale = (size[0], size[2], size[1])
    finish(o, name, hinge, material)
    if taper_front:   # narrow and lower the front (Unity +z = Blender -y) end
        me = o.data; bm = bmesh.new(); bm.from_mesh(me)
        cy = max(v.co.y for v in bm.verts)
        for v in bm.verts:
            if abs(v.co.y - cy) < 1e-4: v.co.x *= taper_front[0]; v.co.z = v.co.z * taper_front[1] + taper_front[2]
        bm.to_mesh(me); bm.free()
    if bevel:
        m = o.modifiers.new("bevel", "BEVEL"); m.width = bevel; m.segments = 2
        bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier="bevel")
    return o

def wheel(name, at):
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.42, depth=0.32, location=B(*at), rotation=(0, math.pi / 2, 0))
    o = bpy.context.active_object; finish(o, name, at, "tire")
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.2, depth=0.34, location=B(*at), rotation=(0, math.pi / 2, 0))
    hub = bpy.context.active_object; hub.data.materials.append(MATS["metal"])
    bpy.ops.object.select_all(action="DESELECT"); hub.select_set(True); o.select_set(True); bpy.context.view_layer.objects.active = o; bpy.ops.object.join()
    return o

# shared body and cockpit
box("Body", (0, 0.05, 0), (1.8, 0.62, 3.9), (0, 0, 0), "body", 0.08, taper_front=(0.7, 0.55, -0.08))
box("Cockpit", (0, 0.42, 0.05), (1.25, 0.28, 1.7), (0, 0.35, 0.05), "trim", 0.06)
bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=1, location=B(0, 0.55, 0.15))
can = bpy.context.active_object; can.scale = (0.52, 0.85, 0.32); finish(can, "Canopy", (0, 0.55, -0.62), "glass")
# wheels (axle along x), hinge at the hub
for n, x, z in (("Wheel_FL", -1.0, 1.35), ("Wheel_FR", 1.0, 1.35), ("Wheel_RL", -1.0, -1.35), ("Wheel_RR", 1.0, -1.35)): wheel(n, (x, -0.45, z))
# wings: root at the body side, swept slightly, reaching out 3 m
box("Wing_L", (-2.4, 0.15, -0.3), (3.0, 0.1, 1.25), (-0.9, 0.15, -0.2), "wing", 0.03)
box("Wing_R", (2.4, 0.15, -0.3), (3.0, 0.1, 1.25), (0.9, 0.15, -0.2), "wing", 0.03)
box("Tail", (0, 0.9, -1.85), (0.1, 1.05, 0.75), (0, 0.35, -1.75), "wing", 0.02)
# hull strakes, keel, propeller
box("Fin_L", (-0.9, -0.55, -0.4), (0.08, 0.6, 1.5), (-0.9, -0.25, -0.4), "fin", 0.02)
box("Fin_R", (0.9, -0.55, -0.4), (0.08, 0.6, 1.5), (0.9, -0.25, -0.4), "fin", 0.02)
box("Keel", (0, -0.55, -0.2), (0.1, 0.5, 2.3), (0, -0.3, -0.2), "fin", 0.02)
p1 = box("Prop", (0, -0.1, -2.05), (1.0, 0.12, 0.05), (0, -0.1, -2.05), "metal", 0)
p2 = box("Prop_b", (0, -0.1, -2.05), (0.12, 1.0, 0.05), (0, -0.1, -2.05), "metal", 0)
bpy.ops.object.select_all(action="DESELECT"); p2.select_set(True); p1.select_set(True); bpy.context.view_layer.objects.active = p1; bpy.ops.object.join(); p1.name = "Prop"

names = [o.name for o in bpy.data.objects if o.type == "MESH"]
print("[shiftstorm_vehicle] parts:", sorted(names))

bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=False, object_types={"MESH"}, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, use_mesh_modifiers=True)
print("[shiftstorm_vehicle] wrote", out_fbx)

# preview render of the rest pose (all parts out), workbench, three-quarter view
cam_data = bpy.data.cameras.new("cam"); cam = bpy.data.objects.new("cam", cam_data); scene.collection.objects.link(cam)
cam.location = B(6.5, 3.2, 7.5); target = bpy.data.objects.new("target", None); scene.collection.objects.link(target); target.location = B(0, 0, 0)
c = cam.constraints.new("TRACK_TO"); c.target = target; c.track_axis = "TRACK_NEGATIVE_Z"; c.up_axis = "UP_Y"; scene.camera = cam
scene.render.engine = "BLENDER_WORKBENCH"; scene.display.shading.light = "STUDIO"; scene.display.shading.color_type = "MATERIAL"
scene.render.resolution_x, scene.render.resolution_y = 960, 600; scene.render.filepath = out_png
world = bpy.data.worlds.new("w"); scene.world = world
bpy.ops.render.render(write_still=True)
print("[shiftstorm_vehicle] preview", out_png)
