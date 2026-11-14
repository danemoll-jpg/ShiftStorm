"""Round 7 item 5b: TeedOff Barker (Barnaby, Tee'd Off's carnival barker) calls the race.

His new racing lines, made with ElevenLabs in his own Tee'd Off voice ("TeedOff Barker", the voice Tee'd Off's
Tools/Audio/voices.json uses for "barker" and "barnaby"; model eleven_v3 with [tags], as Tee'd Off's elevenlabs_voice.py).
His existing Tee'd Off clips that fit a race are copied by copy_existing() (Tee-dOff is read-only: copied, never changed).

Dry run by default (lists the lines and the estimated credits). --go records the missing ones into
Assets/Resources/Audio/Voice/barker/<id>.ogg and writes Assets/Resources/Audio/Voice/barker/lines.json (id -> words, for the
subtitles and the Caller's pools). The key comes from ELEVENLABS_API_KEY (environment or HKCU\\Environment); it is never
printed or written anywhere.
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(REPO, "Assets", "Resources", "Audio", "Voice", "barker")
TEEDOFF = r"C:\Users\danmo\Tee-dOff\Assets\_Project\Resources\Audio\Voice\barker"
VOICE_ID = "l7vIj65RwKyM6W9qAV3G"   # "TeedOff Barker" (Tee'd Off Tools/Audio/voices.json)
MODEL = "eleven_v3"
FFMPEG = shutil.which("ffmpeg") or "ffmpeg"

# pool_n: the Caller picks from each pool by its prefix (Assets/Scripts/Race/Caller.cs)
LINES = {
    "start_1": "[showman] Ladies and gentlemen, start your engines! And your wings! And your propellers!",
    "start_2": "[showman] Step right up, folks! Four racers, one lap, and every shape a machine can take!",
    "start_3": "[showman] Welcome to Paradise Plunge! Road, sky, lagoon and the deep blue below!",
    "go_1": "[shouting] And they're off!",
    "go_2": "[showman] Go, go, go! Off they go, folks!",
    "lead_1": "[showman] A new leader, folks! Hold on to your hats!",
    "lead_2": "[excited] Out in front! Did you see that pass?",
    "lead_3": "[showman] And the lead changes hands! What a race!",
    "loop_1": "[delighted] Round and round and right side up again! Spectacular!",
    "loop_2": "[showman] The loop-the-loop! Don't look down... or up!",
    "loop_3": "[gasps] Upside down at full throttle! Marvelous!",
    "loopfail_1": "[sympathetic] Ohhh, not enough speed for the loop! Back you go!",
    "jump_1": "[showman] Up, up and over! What airtime, folks!",
    "jump_2": "[excited] Look at it fly! And that's with wheels!",
    "jump_3": "[delighted] Sticks the landing! Give that driver a hand!",
    "wings_1": "[showman] Wings out! The scenic route over the lake!",
    "wings_2": "[excited] It's a car! It's a plane! It's both, folks!",
    "stay_1": "[showman] No wings for this one! Keeping it on four wheels!",
    "stay_2": "[chuckles] Who needs wings? Not this daredevil!",
    "shortcut_1": "[sly] Ooh, a sneaky shortcut! I didn't see a thing, folks.",
    "shortcut_2": "[showman] Through the secret passage! Clever, clever!",
    "miss_1": "[showman] Close, but no cigar! That's a three second penalty!",
    "miss_2": "[sympathetic] Ohhh, just wide of the gate! Penalty time, folks!",
    "wall_1": "[wincing] Ooh! That's gonna leave a mark!",
    "wall_2": "[chuckles] Bonk! The wall wins that round, folks!",
    "wall_3": "[showman] Mind the scenery, it was here first!",
    "splash_1": "[chuckles] Into the creek! A little swim never hurt anyone!",
    "final_1": "[excited] Here comes the final stretch! Who wants it most?",
    "final_2": "[showman] Back on the road and heading for home, folks!",
    "photo_1": "[shouting] It's a photo finish! Somebody get me a camera!",
    "photo_2": "[excited] Neck and neck to the line! Too close to call!",
    "win_abi": "[delighted] Abi takes it! The crowd goes wild!",
    "win_rae": "[showman] Rocket Rae takes the chequered flag!",
    "win_chip": "[showman] Chip Showtime Sterling wins it! Showtime indeed!",
    "win_vivi": "[showman] Vivi wins! What a performance, folks!",
    "win_p2": "[showman] Player two takes the crown! Winner, winner!",
    "results_1": "[showman] Thank you, thank you! Come back for the next race, folks!",
    "results_2": "[showman] Step right up and see the results!",
    "trial_1": "[showman] One racer, one clock, no excuses! Let's see that time, folks!",
}
# his Tee'd Off clips that fit a race (copied as they are): id here -> Tee'd Off file
EXISTING = {
    "win_any_1": "winner_1", "win_any_2": "winner_3", "win_any_3": "winner_5", "win_any_4": "finale_1",
    "jump_4": "great_2", "miss_3": "ohno_1", "results_lost_1": "ohno_3",
}
EXISTING_WORDS = {
    "win_any_1": "Winner, winner! We have a winner!", "win_any_2": "Step right up and see the champion! Winner, winner!",
    "win_any_3": "Winner, winner! Somebody light the fireworks!", "win_any_4": "And the crowd goes WILD! Step right up and see a champion, folks!",
    "jump_4": "Stuck it! Did you see that, ladies and gentlemen?", "miss_3": "Ohhh! So close, folks! Try again, try again!",
    "results_lost_1": "Oops! Not a winner this time, but don't go away!",
}


def api_key():
    key = None
    if sys.platform == "win32":
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as h:
                key = winreg.QueryValueEx(h, "ELEVENLABS_API_KEY")[0]
        except OSError:
            key = None
    return key or os.environ.get("ELEVENLABS_API_KEY") or None


def to_ogg(mp3, path):
    tmp = path + ".mp3"
    with open(tmp, "wb") as f:
        f.write(mp3)
    # trim leading / trailing silence, a short fade, mono 44.1 kHz Vorbis
    af = "silenceremove=start_periods=1:start_threshold=-45dB,areverse,silenceremove=start_periods=1:start_threshold=-45dB,areverse,afade=t=out:st=0:d=0.02:curve=tri"
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-i", tmp, "-af", af, "-ac", "1", "-ar", "44100", "-c:a", "libvorbis", "-q:a", "5", path], check=True)
    os.remove(tmp)


def copy_existing():
    n = 0
    for mine, theirs in EXISTING.items():
        src = os.path.join(TEEDOFF, theirs + ".ogg"); dst = os.path.join(OUT, mine + ".ogg")
        if os.path.exists(src) and not os.path.exists(dst):
            shutil.copyfile(src, dst); n += 1
    return n


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--go", action="store_true")
    ap.add_argument("--cap", type=int, default=4000)
    args = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    todo = [(k, t) for k, t in LINES.items() if not os.path.exists(os.path.join(OUT, k + ".ogg"))]
    est = sum(len(t) for _, t in todo)
    print(f"{len(LINES)} new lines, {len(todo)} not recorded yet (about {est} credits); {len(EXISTING)} Tee'd Off clips to copy")
    if not args.go:
        for k, t in todo:
            print(f"  {k}: {t}")
        print("dry run: add --go to record")
        return
    print(f"copied {copy_existing()} Tee'd Off clips")
    key = api_key()
    if not key:
        print("ELEVENLABS_API_KEY is not set. Nothing recorded."); sys.exit(2)
    spent = 0
    for k, text in todo:
        if spent + len(text) > args.cap:
            print(f"STOP at {k}: cap {args.cap}"); break
        body = {"text": text, "model_id": MODEL}
        req = urllib.request.Request(f"https://api.elevenlabs.io/v1/text-to-speech/{VOICE_ID}?output_format=mp3_44100_128", data=json.dumps(body).encode("utf-8"), method="POST")
        req.add_header("xi-api-key", key); req.add_header("Content-Type", "application/json")
        try:
            with urllib.request.urlopen(req, timeout=180) as r:
                audio = r.read(); cost = r.headers.get("character-cost")
        except urllib.error.HTTPError as e:
            print(f"  {k}: HTTP {e.code} {e.read().decode('utf-8', 'replace')[:200]}")
            if e.code in (401, 403): break
            continue
        to_ogg(audio, os.path.join(OUT, k + ".ogg"))
        used = int(cost) if cost and cost.isdigit() else len(text); spent += used
        print(f"  {k}: {used} credits (total {spent})", flush=True)
    words = {k: t.split("] ", 1)[-1] for k, t in LINES.items()}
    words.update(EXISTING_WORDS)
    with open(os.path.join(OUT, "lines.json"), "w", encoding="utf-8") as f:
        json.dump({"speaker": "TeedOff Barker", "lines": [{"id": k, "text": v} for k, v in sorted(words.items())]}, f, indent=1)
    print(f"done: {spent} credits; lines.json written")


if __name__ == "__main__":
    main()
