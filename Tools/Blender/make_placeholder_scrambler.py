# ShiftStorm Round 9 item 3c: a stand-in for the Scrambler (Dan's motorcycle design) until his Tripo models arrive, to build and check the
# motorcycle's lean, the rider astride, the handlebars, the cut boxes (its kickstand) and the sub's glass sphere on its chrome ring.
#   blender -b -P Tools/Blender/make_placeholder_scrambler.py -- <out dir> [design name, default _Scrambler]
# Writes <out>/<design>/<design>_<Form>.fbx (Car = motorcycle with a kickstand, Boat = jet ski, Plane = flying motorbike with the wheels
# folded flat into rotor discs and short wings, Sub = sea scooter with a wide chrome ring round the seat) in one mesh with one texture
# (cream, orange, charcoal, chrome: Dan's concept colours), 4.0 m long, front +Y, floor at z 0, plus import.json (the seat, the bars, the cut
# box, canopy / astride / lean). Names starting "_" never show in the game's design list (hidden; checks use -design _Scrambler).
import bpy, sys, os, math, json

argv = sys.argv[sys.argv.index("--") + 1:]
out_dir = argv[0]; design = argv[1] if len(argv) > 1 else "_Scrambler"
os.makedirs(os.path.join(out_dir, design), exist_ok=True)
CELLS = [(.95, .91, .8), (.95, .45, .1), (.16, .16, .17), (.8, .82, .86)]   # cream, orange, charcoal, chrome
CREAM, ORANGE, DARK, CHROME = range(4)

def new_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    img = bpy.data.images.new("tex", 64, 64); px = []
    for y in range(64):
        for x in range(64):
            c = CELLS[(1 if x >= 32 else 0) + (2 if y >= 32 else 0)]; px += [c[0], c[1], c[2], 1]
    img.pixels = px
    mat = bpy.data.materials.new("Tripo"); mat.use_nodes = True
    t = mat.node_tree.nodes.new("ShaderNodeTexImage"); t.image = img
    mat.node_tree.links.new(t.outputs["Color"], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    return img, mat

parts = []
def part(kind, loc, size=(1, 1, 1), rot=(0, 0, 0), cell=CREAM, **kw):
    if kind == "cube": bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    elif kind == "cyl": bpy.ops.mesh.primitive_cylinder_add(vertices=kw.get("n", 20), radius=.5, depth=1, location=loc, rotation=rot)
    elif kind == "sphere": bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, radius=.5, location=loc, rotation=rot)
    elif kind == "torus": bpy.ops.mesh.primitive_torus_add(major_radius=kw["R"], minor_radius=kw["r"], major_segments=40, minor_segments=10, location=loc, rotation=rot)
    o = bpy.context.active_object
    if kind != "torus": o.scale = size
    o["cell"] = cell; parts.append(o); return o

def wheel(y, z, r=.55, flat=False):
    rot = (0, 0, 0) if flat else (0, math.radians(90), 0)
    part("torus", (0, y, z), rot=rot, cell=DARK, R=r * .78, r=r * .22)
    part("cyl", (0, y, z), (r * 1.2, r * 1.2, .12), rot=rot, cell=ORANGE)

def bike_core(z0=0.0, kick=True):
    part("cube", (0, 0, .95 + z0), (.32, 2.3, .28), cell=DARK)                       # frame spine
    part("sphere", (0, .45, 1.32 + z0), (.62, 1.05, .45), cell=CREAM)                 # tank
    part("cube", (0, -.45, 1.28 + z0), (.5, .95, .16), cell=ORANGE)                   # seat
    part("cube", (0, -1.2, 1.15 + z0), (.42, .6, .2), cell=CREAM)                     # tail
    part("cyl", (0, .05, .72 + z0), (.62, .62, .5), rot=(0, math.radians(90), 0), cell=DARK, n=12)   # finned engine
    for i in range(4): part("cube", (0, .05, .5 + i * .12 + z0), (.7, .5, .03), cell=CHROME)
    part("cube", (0, 1.3, 1.25 + z0), (.08, .1, .9), rot=(math.radians(-25), 0, 0), cell=CHROME)   # fork
    part("sphere", (0, 1.55, 1.5 + z0), (.42, .3, .42), cell=CREAM)                  # headlight pod
    part("cyl", (0, 1.7, 1.5 + z0), (.3, .3, .05), rot=(math.radians(90), 0, 0), cell=CHROME)
    part("cube", (0, .55, 1.72 + z0), (.95, .06, .06), cell=CHROME)                   # bars (a quarter of the length ahead of the seat, as a real bike)
    for s in (-1, 1): part("cube", (s * .5, .52, 1.72 + z0), (.18, .08, .08), cell=ORANGE)
    part("cube", (0, .7, 1.55 + z0), (.06, .06, .4), rot=(math.radians(-30), 0, 0), cell=CHROME)   # riser
    part("cyl", (.3, -.8, .7 + z0), (.16, .16, 1.1), rot=(0, 0, 0), cell=CHROME)      # exhaust (upright stub)
    if kick: part("cube", (.36, -.3, .3), (.05, .05, .65), rot=(0, math.radians(30), 0), cell=CHROME)   # the kickstand (cut by import.json)

forms = {}
def car():
    wheel(1.4, .55); wheel(-1.4, .55); bike_core()
def boat():
    part("sphere", (0, 0, .45), (1.35, 4.0, .9), cell=CREAM)
    for s in (-1, 1): part("cube", (s * .6, 0, .55), (.04, 3.2, .12), cell=ORANGE)
    part("cube", (0, -.45, 1.0), (.5, 1.0, .16), cell=ORANGE); part("sphere", (0, .5, .98), (.7, 1.1, .4), cell=CREAM)
    part("cube", (0, .45, 1.32), (.95, .06, .06), cell=CHROME)
    for s in (-1, 1): part("cube", (s * .5, .43, 1.32), (.18, .08, .08), cell=ORANGE)
    part("cyl", (0, -1.95, .45), (.3, .3, .3), rot=(math.radians(90), 0, 0), cell=DARK)
def plane():
    bike_core(.1, kick=False)
    for y in (1.4, -1.4): part("torus", (0, y, .45), R=.5, r=.1, cell=ORANGE); part("cyl", (0, y, .45), (1.1, 1.1, .05), cell=DARK)
    for s in (-1, 1): part("cube", (s * 1.1, -.2, 1.0), (1.6, .8, .08), rot=(0, math.radians(s * -6), 0), cell=CREAM); part("cube", (s * 1.85, -.2, 1.05), (.2, .8, .1), cell=ORANGE)
    part("cyl", (0, -1.6, 1.1), (.4, .4, .5), rot=(math.radians(90), 0, 0), cell=DARK)
def sub():
    part("cyl", (0, 0, .75), (1.3, 1.3, 3.6), rot=(math.radians(90), 0, 0), cell=CREAM)
    part("sphere", (0, 1.8, .75), (1.3, .6, 1.3), cell=CREAM); part("sphere", (0, -1.8, .75), (1.0, .5, 1.0), cell=CREAM)
    part("cube", (0, -.35, 1.42), (.5, .9, .14), cell=ORANGE)                         # seat (hips at z 1.5)
    part("cube", (0, .4, 1.65), (.95, .06, .06), cell=CHROME)
    for s in (-1, 1): part("cube", (s * .5, .38, 1.65), (.18, .08, .08), cell=ORANGE); part("cube", (s * .8, -1.6, .75), (.5, .4, .06), cell=ORANGE)
    part("torus", (0, -2.05, .75), rot=(math.radians(90), 0, 0), R=.55, r=.08, cell=CHROME)
    part("torus", (0, -.25, 1.5 + .42), R=1.07, r=.06, cell=CHROME)                   # the chrome ring the glass sphere sits on
    for a in (0, 90, 180, 270):
        x, y = math.cos(math.radians(a)) * 1.0, math.sin(math.radians(a)) * 1.0 - .25
        part("cube", (x, y, 1.65), (.06, .06, .5), cell=CHROME)                           # ring posts
for name, fn in (("Car", car), ("Boat", boat), ("Plane", plane), ("Sub", sub)):
    parts.clear(); img, mat = new_scene(); fn()
    for o in parts:
        cell = o["cell"]; u0, v0 = (cell % 2) * .5 + .1, (cell // 2) * .5 + .1
        o.data.materials.clear(); o.data.materials.append(mat)
        if not o.data.uv_layers: o.data.uv_layers.new()
        for l in o.data.uv_layers[0].data: l.uv = (u0 + .15, v0 + .15)
    for o in bpy.data.objects: o.select_set(o.type == "MESH")
    bpy.context.view_layer.objects.active = parts[0]; bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active; obj.name = "tripo_mesh"; bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    path = os.path.join(out_dir, design, f"{design}_{name}.fbx")
    img.filepath_raw = os.path.join(out_dir, design, f"{design}_{name}_tex.png"); img.file_format = "PNG"; img.save()
    bpy.ops.export_scene.fbx(filepath=path, object_types={"MESH"}, path_mode="COPY", embed_textures=True)
    print("[placeholder] wrote", path)

# the game's settings (import.json), in the cleaned model: the importer scales each form to LENGTH (2.6 m: a bike in real proportion to a
# 1.4x rider, designs.json "riderScale") and puts its lowest point on the form's floor. seat = the rider's hips, steer = the bars' centre.
LENGTH = 2.6
FLOOR = {"Car": -0.85, "Boat": -0.62, "Plane": -0.55, "Sub": -0.6}
model_len = {"Car": 3.9, "Boat": 4.1, "Plane": 4.0, "Sub": 4.23}; lo_z = {"Car": 0.0, "Boat": 0.0, "Plane": .35, "Sub": 0.1}
seat = {"Car": (-.45, 1.36), "Boat": (-.45, 1.08), "Plane": (-.45, 1.46), "Sub": (-.35, 1.5)}
bars = {"Car": (.55, 1.72), "Boat": (.45, 1.32), "Plane": (.55, 1.82), "Sub": (.4, 1.65)}
def fin(f, y, z): k = LENGTH / model_len[f]; return [0, round(k * y, 3), round(FLOOR[f] + k * (z - lo_z[f]), 3)]
kc = LENGTH / model_len["Car"]
cfg = {"_note": "Written by Tools/Blender/make_placeholder_scrambler.py (a stand-in; never shipped: names starting _ are hidden).", "yaw": 0, "length": LENGTH,
       "seat": {f: fin(f, *seat[f]) for f in FLOOR}, "steer": {f: fin(f, *bars[f]) for f in FLOOR},
       "cut": {"Car": [[round(.18 * kc, 3), round(-.75 * kc, 3), FLOOR["Car"] - .05, round(.62 * kc, 3), round(.15 * kc, 3), round(FLOOR["Car"] + .66 * kc, 3)]]},
       "steering": ["handlebars", "handlebars", "handlebars", "handlebars"], "canopy": ["none", "none", "none", "sphere"],
       "astride": ["Car", "Boat", "Plane", "Sub"], "lean": ["Car"], "sphere": 1.15, "sphereY": .5, "riderScale": 1.4}
json.dump(cfg, open(os.path.join(out_dir, design, "import.json"), "w"), indent=2)
print("[placeholder] wrote import.json")
