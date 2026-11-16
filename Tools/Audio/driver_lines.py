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
# Round 9 item 2 (Dan: "She says 'that's not where I aimed' which doesn't make sense in the context of driving"): every Tee'd Off clip that
# only makes sense in golf (or in another place than the one it is played for) is out; a racing line in the same voice replaces each.
ROUND9 = {
    "abigail": {"line_1": "[annoyed] That was NOT the racing line."},                       # for trouble_1 "That's not where I aimed."
    "rae": {"throttle_1": "[frustrated, speaking clearly] Too much throttle!",                               # for mishit_1 "Too much muscle!" (a golf swing)
            "wall_1": "[yells] Who put a WALL there?!",                                    # for trouble_2 "Who planted that?!" (a ball in a tree)
            "swim_1": "[angry] I came here to race, not to swim!"},                        # for water_1 "The lake can have it!" (the ball)
    "vivi": {"last_1": "[sighs] Last? Darling, that is simply not my colour."},            # for lose_1 "Second place suits no one." (said for last place)
}
# Round 13 item 4: the rival challenges (one on one with Rae on Paradise Plunge, with Chip on Coaster Coast): a taunt before, a line when
# they win, a line when they lose
ROUND13 = {
    "rae": {"chintro_1": "[smug] One on one, rookie? Try to keep my bubbles in sight!",
            "chwin_1": "[laughing] Ha! The waterfall is MY turf!",
            "chlose_1": "[grudging] Okay. Okay! You earned that one."},
    "chip": {"chintro_1": "[showman] Ladies and gentlemen, the Showtime coaster special! Try to keep up!",
             "chwin_1": "[cheering] And THAT, folks, is how you ride a coaster!",
             "chlose_1": "[stunned] You beat me on my own coaster? That's going in YOUR highlight reel."},
}
REMOVED = {"abigail": ["trouble_1"], "rae": ["mishit_1", "trouble_2", "water_1"], "vivi": ["lose_1"]}
for _who, _d in ROUND9.items():
    NEW.setdefault(_who, {}).update(_d)
for _who, _d in ROUND13.items():
    NEW.setdefault(_who, {}).update(_d)
WORDS_NEW = {who: {k: v.split("] ", 1)[-1] for k, v in d.items()} for who, d in NEW.items()}
# their Tee'd Off clips that fit a race (copied as they are)
COPY = {
    "abigail": ["disaster_1", "mishit_1", "gasp_1", "bogey_1", "water_1", "lose_1", "quipbad_1", "win_1", "quipgood_1", "quipgood_2", "laugh_1", "lucky_1"],
    "rae": ["disaster_1", "gasp_1", "lose_1", "win_1", "quipgood_1"],
    "chip": ["disaster_1", "disaster_2", "mishit_1", "gasp_1", "trouble_2", "lose_1", "win_1", "quipgood_1"],
    "vivi": ["disaster_1", "disaster_2", "mishit_1", "gasp_1", "trouble_1", "water_1", "win_1", "quipgood_1"],
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
    for who, names in REMOVED.items():   # Round 9: the golf-only clips leave the game
        for n in names:
            for ext in (".ogg", ".ogg.meta"):
                p = os.path.join(OUT, who, n + ext)
                if os.path.exists(p): os.remove(p); print(f"  removed {who}/{n}{ext}")
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
