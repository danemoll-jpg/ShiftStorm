"""Round 8 item 3: the drivers' mess-up reactions (Dan: "Have the driver scream just 'Noooo' or 'oh no!' when they mess up. They could
say something else based on their specific character.").

Each driver (Abi, Rocket Rae, Chip, Vivi) gets three new short lines made with ElevenLabs in their own Tee'd Off voice (the voice ids
Tee'd Off's Tools/Audio/voices.json uses; model eleven_v3 with [tags]), plus their Tee'd Off clips that fit a racing mess-up, copied
(Tee-dOff is read-only: copied, never changed). Into Assets/Resources/Audio/Voice/<who>/.

Dry run by default; --go records the missing ones. The key comes from ELEVENLABS_API_KEY (environment or HKCU\\Environment); it is
never printed or written anywhere.
"""
import argparse
import json
import os
import shutil
import sys
import urllib.error
import urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from caller_lines import api_key, to_ogg  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(REPO, "Assets", "Resources", "Audio", "Voice")
TEEDOFF = r"C:\Users\danmo\Tee-dOff\Assets\_Project\Resources\Audio\Voice"
MODEL = "eleven_v3"
VOICES = {"abigail": "Px642hSzdoBY1t049JRR", "rae": "s2thlDxFu9hhIBNJMjH4", "chip": "kAk3sPhgIhPLV2Ry5eIM", "vivi": "vW3XlvUbGnV0KTd3aFI2"}

NEW = {
    "abigail": {"messup_1": "[screaming] Noooooo!", "messup_2": "[shocked] Oh no!", "messup_3": "[furious] Oh, come ON! Not now!"},
    "rae": {"messup_1": "[yelling] Noooooo!", "messup_2": "[groans] Oh no!", "messup_3": "[frustrated grunt] Should've hit the gym harder!"},
    "chip": {"messup_1": "[wailing] Noooooo!", "messup_2": "[panicked] Oh no, oh no!", "messup_3": "[groans] That is NOT going in the highlight reel!"},
    "vivi": {"messup_1": "[dramatic shriek] Noooooo!", "messup_2": "[gasps] Oh no!", "messup_3": "[dramatic] My hair! Darling, my HAIR!"},
}
WORDS_NEW = {who: {k: v.split("] ", 1)[-1] for k, v in d.items()} for who, d in NEW.items()}
# their Tee'd Off clips that fit (copied as they are)
COPY = {
    "abigail": ["disaster_1", "mishit_1", "gasp_1", "bogey_1", "trouble_1", "water_1", "lose_1", "quipbad_1", "win_1", "quipgood_1", "quipgood_2", "laugh_1", "lucky_1"],
    "rae": ["disaster_1", "mishit_1", "gasp_1", "trouble_2", "water_1", "lose_1", "win_1", "quipgood_1"],
    "chip": ["disaster_1", "disaster_2", "mishit_1", "gasp_1", "trouble_2", "lose_1", "win_1", "quipgood_1"],
    "vivi": ["disaster_1", "disaster_2", "mishit_1", "gasp_1", "trouble_1", "water_1", "lose_1", "win_1", "quipgood_1"],
}


def teedoff_words():
    try:
        d = json.load(open(os.path.join(r"C:\Users\danmo\Tee-dOff\Tools\Audio", "lines.json"), encoding="utf-8"))["lines"]
        return {(x["speaker"], x["id"]): x["text"].split("] ", 1)[-1] for x in d}
    except Exception:
        return {}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--go", action="store_true")
    args = ap.parse_args()
    todo = [(who, k, t) for who, d in NEW.items() for k, t in d.items() if not os.path.exists(os.path.join(OUT, who, k + ".ogg"))]
    print(f"{sum(len(d) for d in NEW.values())} new lines, {len(todo)} to record (about {sum(len(t) for *_, t in todo)} credits); clips to copy: {sum(len(c) for c in COPY.values())}")
    if not args.go:
        for who, k, t in todo:
            print(f"  {who}/{k}: {t}")
        print("dry run: add --go to record")
        return
    copied = 0
    for who, names in COPY.items():
        os.makedirs(os.path.join(OUT, who), exist_ok=True)
        for n in names:
            src, dst = os.path.join(TEEDOFF, who, n + ".ogg"), os.path.join(OUT, who, n + ".ogg")
            if os.path.exists(src) and not os.path.exists(dst):
                shutil.copyfile(src, dst); copied += 1
    print(f"copied {copied} Tee'd Off clips")
    key = api_key()
    if not key:
        print("ELEVENLABS_API_KEY is not set. Nothing recorded."); sys.exit(2)
    spent = 0
    for who, k, text in todo:
        body = {"text": text, "model_id": MODEL}
        req = urllib.request.Request(f"https://api.elevenlabs.io/v1/text-to-speech/{VOICES[who]}?output_format=mp3_44100_128", data=json.dumps(body).encode("utf-8"), method="POST")
        req.add_header("xi-api-key", key); req.add_header("Content-Type", "application/json")
        try:
            with urllib.request.urlopen(req, timeout=180) as r:
                audio = r.read(); cost = r.headers.get("character-cost")
        except urllib.error.HTTPError as e:
            print(f"  {who}/{k}: HTTP {e.code} {e.read().decode('utf-8', 'replace')[:200]}")
            if e.code in (401, 403): break
            continue
        to_ogg(audio, os.path.join(OUT, who, k + ".ogg"))
        used = int(cost) if cost and cost.isdigit() else len(text); spent += used
        print(f"  {who}/{k}: {used} credits (total {spent})", flush=True)
    tw = teedoff_words()
    for who in NEW:
        words = dict(WORDS_NEW[who])
        for n in COPY[who]:
            words[n] = tw.get((who, n), "")
        with open(os.path.join(OUT, who, "lines.json"), "w", encoding="utf-8") as f:
            json.dump({"speaker": who, "lines": [{"id": k, "text": v} for k, v in sorted(words.items())]}, f, indent=1)
    print(f"done: {spent} credits")


if __name__ == "__main__":
    main()
