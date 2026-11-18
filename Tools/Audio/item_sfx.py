"""Round 17: the items' sound effects, made with ElevenLabs' sound generation -> Assets/Resources/Audio/Sfx/it_<name>.ogg
(Core/GameSounds.cs plays them; Docs/SOUNDS.md lists them). Dry run without --go.
Usage: python Tools/Audio/item_sfx.py [--go] [--redo name ...]"""
import argparse
import json
import os
import subprocess
import sys
import urllib.error
import urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import caller_lines  # noqa: E402  (api key, ffmpeg)

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Resources", "Audio", "Sfx")
SFX = {
    "pick": ("cartoon item box pickup, sparkly arcade chime with a quick rising roulette tick, cheerful", 1.0),
    "zap": ("cartoon magic zap, buzzy electric bolt with a silly boing at the end", 1.0),
    "pop": ("big soap bubble popping, wet cartoon pop with a little splash of droplets", 0.8),
    "bubble": ("cartoon bubble inflating and wobbling, gloopy rising bloop", 1.2),
    "rain": ("small cartoon thunder rumble then a quick burst of heavy rain shower", 2.0),
    "rocket": ("firework rocket launch whistle then a crackling burst", 1.6),
    "boom": ("big cartoon circus cannon boom with a whoosh, comic", 1.4),
    "squeak": ("baby dinosaur hatching from an egg, eggshell crack then a tiny excited squeaky roar", 1.4),
    "rewind": ("tape rewind sound, fast warbling reverse whoosh, retro", 1.0),
    "shimmer": ("magic shimmer whoosh, ghostly mirage sparkle", 1.0),
    "wave": ("big water wave rushing and crashing, cartoon surf", 1.6),
    "swap": ("cartoon teleport swap, quick zip and a confetti popper", 0.9),
    "slick": ("cartoon slip on a banana skin, squeaky skid", 0.9),
}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--go", action="store_true")
    ap.add_argument("--redo", nargs="*")
    a = ap.parse_args()
    redo = set(SFX) if a.redo == [] else set(a.redo or [])
    todo = [k for k in SFX if k in redo or not os.path.exists(os.path.join(OUT, f"it_{k}.ogg"))]
    print(f"{len(SFX)} item sounds, {len(todo)} to make")
    if not a.go:
        for k in todo:
            print(f"  it_{k}: {SFX[k][0]} ({SFX[k][1]} s)")
        print("dry run: add --go")
        return
    key = caller_lines.api_key()
    if not key:
        print("ELEVENLABS_API_KEY is not set"); sys.exit(2)
    for k in todo:
        text, dur = SFX[k]
        body = {"text": text, "duration_seconds": dur, "prompt_influence": 0.5}
        req = urllib.request.Request("https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128", data=json.dumps(body).encode("utf-8"), method="POST")
        req.add_header("xi-api-key", key); req.add_header("Content-Type", "application/json")
        try:
            with urllib.request.urlopen(req, timeout=180) as r:
                audio = r.read()
        except urllib.error.HTTPError as e:
            print(f"  it_{k}: HTTP {e.code} {e.read().decode('utf-8', 'replace')[:200]}")
            if e.code in (401, 403):
                break
            continue
        tmp = os.path.join(OUT, f"it_{k}.mp3")
        with open(tmp, "wb") as f:
            f.write(audio)
        dst = os.path.join(OUT, f"it_{k}.ogg")
        af = "afade=t=out:st=%.2f:d=0.08,loudnorm=I=-18:TP=-2:LRA=11,aresample=44100" % max(0.0, dur - 0.1)
        subprocess.run([caller_lines.FFMPEG, "-y", "-loglevel", "error", "-i", tmp, "-af", af, "-ac", "1", "-c:a", "libvorbis", "-q:a", "5", dst], check=True)
        os.remove(tmp)
        print(f"  it_{k}: made", flush=True)


if __name__ == "__main__":
    main()
