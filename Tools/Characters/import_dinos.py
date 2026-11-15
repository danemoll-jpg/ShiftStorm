"""ShiftStorm Round 9: Dan's Tripo dinosaurs into the game, one command (replacing the stand-ins: the raptor was a small T. rex, the
plesiosaur a swimming brontosaurus).

    python Tools/Characters/import_dinos.py            # every one in Art/Incoming/Dinos/<Name>/ (a .zip as downloaded, or .fbx / .glb)

For each: unzip into its own empty folder under local/dinos/, Blender (Tools/Blender/dino_to_glb.py: one mesh, 24,000 triangles, Y up) ->
local/dinos/<Name>/<Name>_meshy.glb + base colour, Tee'd Off's rigger copied (Tools/Characters/rig_dino.py: feet, skeleton, weights; plots in
local/dinos/plots/) -> Assets/Resources/Dinos/<style>_rig.bytes + <style>_basecolor.png (World/Dinos.cs loads them; without them the
stand-ins stay). Below Normal priority, one job at a time.
"""
import os, shutil, subprocess, sys, zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "Art", "Incoming", "Dinos"); WORK = os.path.join(ROOT, "local", "dinos")
OUT = os.path.join(ROOT, "Assets", "Resources", "Dinos")
STYLE = {"Raptor": "raptor", "Plesiosaur": "plesio"}
BLENDER = r"F:\blender\blender.exe"; LOW = 0x00004000 if os.name == "nt" else 0


def main():
    for name, style in STYLE.items():
        d = os.path.join(SRC, name)
        if not os.path.isdir(d): print(f"{name}: no folder"); continue
        files = [f for f in os.listdir(d) if os.path.splitext(f)[1].lower() in (".zip", ".fbx", ".glb")]
        if not files: print(f"{name}: no model yet"); continue
        src = os.path.join(d, files[0])
        if src.lower().endswith(".zip"):
            unz = os.path.join(WORK, "unz_" + name); shutil.rmtree(unz, ignore_errors=True); os.makedirs(unz)
            with zipfile.ZipFile(src) as z:
                for m in z.namelist():
                    if os.path.isabs(m) or ".." in m.replace("\\", "/").split("/"): raise SystemExit(f"unsafe path in {src}: {m}")
                z.extractall(unz)
            src = next(os.path.join(p, f) for p, _, fs in os.walk(unz) for f in fs if f.lower().endswith((".fbx", ".glb")))
            tex = next((os.path.join(p, f) for p, _, fs in os.walk(unz) for f in fs if "basecolor" in f.lower()), None)
        else: tex = None
        out = os.path.join(WORK, name); os.makedirs(out, exist_ok=True)
        r = subprocess.run([BLENDER, "-b", "-P", os.path.join(ROOT, "Tools", "Blender", "dino_to_glb.py"), "--", src, out, name], capture_output=True, text=True, creationflags=LOW)
        print("\n".join(l for l in r.stdout.splitlines() if l.startswith("[dino_to_glb]")) or r.stdout[-1500:])
        if tex:
            from PIL import Image
            im = Image.open(tex).convert("RGB"); im.thumbnail((2048, 2048)); im.save(os.path.join(out, f"{name}_meshy_basecolor.png"))
        r = subprocess.run([sys.executable, os.path.join(ROOT, "Tools", "Characters", "rig_dino.py"), "--only", name], capture_output=True, text=True, creationflags=LOW)
        print("\n".join(l for l in r.stdout.splitlines() if l.startswith(f"[{name}]")))
        rig = os.path.join(WORK, "rigs", style)
        for f in (f"{style}_rig.bytes", f"{style}_basecolor.png"):
            shutil.copyfile(os.path.join(rig, f), os.path.join(OUT, f))
        print(f"{name}: -> Assets/Resources/Dinos/{style}_rig.bytes + {style}_basecolor.png")


if __name__ == "__main__":
    main()
