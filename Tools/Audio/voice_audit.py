"""Round 9 item 2: every voice line in the game, written down and heard back.

For each speaker folder in Assets/Resources/Audio/Voice/ (abigail, rae, chip, vivi, barker) it lists every clip with the words its
lines.json gives, transcribes the clip itself (faster-whisper "small.en", local and offline from the Hugging Face cache; the ElevenLabs
key has no speech-to-text permission) so the list is what is actually said, and
flags golf words (aim, shot, putt, club, hole, green, fore, caddie, par, birdie, bogey, eagle, tee, fairway, bunker, rough, swing, ...).
Writes Docs/Checks/round9/voice_lines.md. The key comes from ELEVENLABS_API_KEY (environment or HKCU\\Environment); never printed.
Usage: python Tools/Audio/voice_audit.py [--no-stt]
"""
import argparse, json, os, re, sys, urllib.request, uuid

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
VOICE = os.path.join(REPO, "Assets", "Resources", "Audio", "Voice")
OUT = os.path.join(REPO, "Docs", "Checks", "round9", "voice_lines.md")
sys.path.insert(0, HERE)
from caller_lines import api_key  # noqa: E402

GOLF = re.compile(r"\b(aim(ed)?|shot|putt(s|ed|ing)?|club(s)?|hole(s)?|green|fore|caddie|caddy|par|birdie|bogey|eagle|tee|fairway|bunker|rough|swing|slice|hook|chip shot|links|course record)\b", re.I)
NAMES = {"abigail": "Abi", "rae": "Rocket Rae", "chip": "Chip", "vivi": "Vivi", "barker": "TeedOff Barker"}


def stt(key, path):
    b = "----" + uuid.uuid4().hex
    with open(path, "rb") as f:
        data = f.read()
    body = (f"--{b}\r\nContent-Disposition: form-data; name=\"model_id\"\r\n\r\nscribe_v1\r\n"
            f"--{b}\r\nContent-Disposition: form-data; name=\"language_code\"\r\n\r\neng\r\n"
            f"--{b}\r\nContent-Disposition: form-data; name=\"tag_audio_events\"\r\n\r\nfalse\r\n"
            f"--{b}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{os.path.basename(path)}\"\r\nContent-Type: audio/ogg\r\n\r\n").encode() + data + f"\r\n--{b}--\r\n".encode()
    req = urllib.request.Request("https://api.elevenlabs.io/v1/speech-to-text", data=body, method="POST")
    req.add_header("xi-api-key", key); req.add_header("Content-Type", f"multipart/form-data; boundary={b}")
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read().decode("utf-8")).get("text", "").strip()


_model = None
def local_stt(path):
    global _model
    if _model is None:
        from faster_whisper import WhisperModel
        _model = WhisperModel("small.en", device="cpu", compute_type="int8", local_files_only=True)
    import numpy as np, shutil, subprocess   # decoded by ffmpeg (this PyAV is too new for faster-whisper's own decoder)
    raw = subprocess.run([shutil.which("ffmpeg") or "ffmpeg", "-loglevel", "error", "-i", path, "-ac", "1", "-ar", "16000", "-f", "f32le", "-"], capture_output=True, check=True).stdout
    segs, _ = _model.transcribe(np.frombuffer(raw, np.float32), language="en", beam_size=5)
    return " ".join(s.text.strip() for s in segs).strip()


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--no-stt", action="store_true"); a = ap.parse_args()
    key = None if a.no_stt else "local"
    rows, flagged, total = [], [], 0
    for who in ["abigail", "rae", "chip", "vivi", "barker"]:
        folder = os.path.join(VOICE, who)
        words = {}
        lj = os.path.join(folder, "lines.json")
        if os.path.exists(lj):
            words = {x["id"]: x["text"] for x in json.load(open(lj, encoding="utf-8"))["lines"]}
        clips = sorted(f[:-4] for f in os.listdir(folder) if f.endswith(".ogg"))
        rows.append(f"\n## {NAMES[who]} ({len(clips)} clips)\n\n| clip | words (lines.json) | heard (speech-to-text) | golf words |\n|---|---|---|---|")
        for c in clips:
            heard = ""
            if key:
                try: heard = local_stt(os.path.join(folder, c + ".ogg"))
                except Exception as e: heard = f"(stt failed: {type(e).__name__})"
            w = words.get(c, "(not in lines.json)")
            g = sorted(set(m.group(0).lower() for m in GOLF.finditer(w + " " + heard)))
            if g: flagged.append(f"{who}/{c}: {', '.join(g)}")
            rows.append(f"| {c} | {w} | {heard} | {', '.join(g)} |"); total += 1
            print(f"{who}/{c}: {w} | {heard} {'| GOLF: ' + ','.join(g) if g else ''}", flush=True)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("# Every voice line in ShiftStorm (Round 9 item 2)\n\nWritten by `Tools/Audio/voice_audit.py`: each clip in "
                "`Assets/Resources/Audio/Voice/<speaker>/`, the words its `lines.json` gives and what speech-to-text (faster-whisper small.en, local) hears in "
                "the clip itself. Golf words flagged automatically (then judged by hand: \"bogey_1\" is only a clip name).\n")
        f.write("\n".join(rows) + "\n")
        f.write(f"\n## Flags\n\n{total} clips; golf words found: " + ("none" if not flagged else "\n- " + "\n- ".join(flagged)) + "\n")
    print(f"{total} clips; flagged {len(flagged)}; wrote {OUT}")


if __name__ == "__main__":
    main()
