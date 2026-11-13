"""ShiftStorm Round 5: the one-command vehicle import (Tripo models made by Dan, see Docs/MODELS-NEEDED.md).

    python Tools/import_vehicles.py                 # every design in Art/Incoming/Vehicles/
    python Tools/import_vehicles.py --design TeeShot
    python Tools/import_vehicles.py --src local/round5/testfbx --design _Test     # the placeholder test

For each <src>/<Design>/<Design>_<Form>.fbx (or .glb / .obj; Form = Car, Boat, Plane, Sub) it runs Blender
(Tools/Blender/vehicle_import.py: scale to 4.0 m, origin on the hull, facing, floor, Seat point, LOD1, texture) into
Assets/Resources/Vehicles/<Design>/<Form>.fbx + <Form>_basecolor.png, renders previews to Docs/Checks/round5/import/, and
rewrites Assets/Resources/Vehicles/designs.json (the list the Garage and the race read). Rival colours are made in the game
(the green parts of the texture are recoloured), so nothing per rival is stored.

Per design, an optional <src>/<Design>/import.json: {"yaw": 180, "seat": {"Car": [0, -0.2, 0.1]}, "driver": true}
  yaw    turn about the vertical, degrees (default 180: Tripo models face +Z in glTF terms, the game's front is +Y in Blender)
  seat   Abi's hips per form, metres in the model (default: found from the hull's size)
  driver false hides Abi in this design (a closed model with no cockpit)
Runs Blender at Below Normal priority, one model at a time.
"""
import argparse, json, os, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BLENDER = r"F:\blender\blender.exe"
FORMS = ["Car", "Boat", "Plane", "Sub"]
EXTS = [".fbx", ".glb", ".gltf", ".obj"]


def find(src, design, form):
    for e in EXTS:
        p = os.path.join(src, design, f"{design}_{form}{e}")
        if os.path.exists(p): return p
    return None


def run_blender(args):
    flags = 0x00004000 if os.name == "nt" else 0   # BELOW_NORMAL_PRIORITY_CLASS
    cmd = [BLENDER, "-b", "-P", os.path.join(ROOT, "Tools", "Blender", "vehicle_import.py"), "--"] + args
    r = subprocess.run(cmd, capture_output=True, text=True, creationflags=flags)
    lines = [l for l in r.stdout.splitlines() if l.startswith("[vehicle_import]")]
    if r.returncode != 0 or not lines:
        print(r.stdout[-2000:], r.stderr[-2000:]); raise SystemExit(f"Blender failed on {args[0]}")
    return lines


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", default=os.path.join(ROOT, "Art", "Incoming", "Vehicles"))
    ap.add_argument("--design", default=None)
    a = ap.parse_args()
    src = os.path.abspath(a.src)
    out_root = os.path.join(ROOT, "Assets", "Resources", "Vehicles")
    previews = os.path.join(ROOT, "Docs", "Checks", "round5", "import")
    os.makedirs(out_root, exist_ok=True); os.makedirs(previews, exist_ok=True)
    designs = [a.design] if a.design else sorted(d for d in os.listdir(src) if os.path.isdir(os.path.join(src, d)) and not d.startswith("."))
    index_path = os.path.join(out_root, "designs.json")
    index = json.load(open(index_path)) if os.path.exists(index_path) else {"designs": []}
    by_name = {d["name"]: d for d in index["designs"]}
    for design in designs:
        cfg_path = os.path.join(src, design, "import.json")
        cfg = json.load(open(cfg_path)) if os.path.exists(cfg_path) else {}
        made = []
        for form in FORMS:
            f = find(src, design, form)
            if not f: continue
            seat = cfg.get("seat", {}).get(form)
            args = [f, os.path.join(out_root, design), form, str(cfg.get("yaw", 180)), ",".join(str(c) for c in seat) if seat else "auto",
                    os.path.join(previews, f"{design}-{form}.png")]
            for l in run_blender(args): print(l)
            made.append(form)
        if not made:
            print(f"{design}: no models yet"); continue
        prev = by_name.get(design, {}).get("forms", [])
        by_name[design] = {"name": design, "forms": sorted(set(prev) | set(made), key=FORMS.index), "driver": cfg.get("driver", True)}
        print(f"{design}: imported {', '.join(made)}")
    index["designs"] = sorted(by_name.values(), key=lambda d: d["name"])
    json.dump(index, open(index_path, "w"), indent=2)
    print("designs.json:", ", ".join(f"{d['name']} ({'/'.join(d['forms'])})" for d in index["designs"]) or "none")
    print("Next: build the player (CLAUDE.md, Commands) - Unity imports Assets/Resources/Vehicles on the build.")


if __name__ == "__main__":
    main()
