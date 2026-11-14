"""Round 8 item 2: voice loudness. Measured: TeedOff Barker's ElevenLabs lines from Round 7 came out at about -43 LUFS (his Tee'd Off
clips and the music are at -15 / -16 LUFS), so in a race they were ~28 dB under everything: Dan's "can't hear most of Barnaby's lines".

Every voice clip in Assets/Resources/Audio/Voice/<who>/*.ogg is measured (ffmpeg ebur128: integrated loudness, true peak) and, when it is
more than 1 dB off the target, re-encoded at the target (default -16 LUFS) with a peak limiter (-1 dBTP). Short yells (under 1.2 s) are
measured by their momentary maximum instead of the integrated value (the gate drops most of a 0.5 s clip).
Usage: python Tools/Audio/normalize_voice.py [--target -16] [--only barker] [--dry]
"""
import argparse
import os
import re
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
VOICE = os.path.join(REPO, "Assets", "Resources", "Audio", "Voice")
FFMPEG = shutil.which("ffmpeg") or "ffmpeg"


def measure(path):
    r = subprocess.run([FFMPEG, "-hide_banner", "-nostats", "-i", path, "-af", "ebur128=peak=true", "-f", "null", "-"], capture_output=True, text=True)
    out = r.stderr
    i = re.findall(r"I:\s+(-?[\d.]+|-inf) LUFS", out); tp = re.findall(r"Peak:\s+(-?[\d.]+|-inf) dBFS", out)
    mom = [float(x) for x in re.findall(r" M:\s*(-?[\d.]+)", out)]
    dur = re.findall(r"Duration: (\d+):(\d+):([\d.]+)", out)
    secs = 0.0
    if dur:
        h, m, s = dur[0]; secs = int(h) * 3600 + int(m) * 60 + float(s)
    integ = float(i[-1]) if i and i[-1] != "-inf" else -70.0
    peak = float(tp[-1]) if tp and tp[-1] != "-inf" else -70.0
    mmax = max(mom) if mom else integ
    return integ, peak, mmax, secs


def loudness(path):
    integ, peak, mmax, secs = measure(path)
    return (mmax - 2.0 if secs < 1.2 or integ < -69 else integ), peak, secs


def fix(path, gain):
    tmp = path + ".tmp.ogg"
    af = f"volume={gain:.2f}dB,alimiter=limit=0.89:attack=2:release=40:level=false"
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-i", path, "-af", af, "-ac", "1", "-ar", "44100", "-c:a", "libvorbis", "-q:a", "5", tmp], check=True)
    os.replace(tmp, path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--target", type=float, default=-16.0)
    ap.add_argument("--only")
    ap.add_argument("--dry", action="store_true")
    a = ap.parse_args()
    n = changed = 0
    for who in sorted(os.listdir(VOICE)):
        d = os.path.join(VOICE, who)
        if not os.path.isdir(d) or (a.only and who != a.only):
            continue
        for f in sorted(os.listdir(d)):
            if not f.endswith(".ogg"):
                continue
            p = os.path.join(d, f); lu, peak, secs = loudness(p); n += 1
            gain = a.target - lu
            if abs(gain) <= 1.0:
                continue
            print(f"{who}/{f}: {lu:6.1f} LUFS (peak {peak:5.1f} dBFS, {secs:.1f} s) -> {gain:+.1f} dB")
            if not a.dry:
                fix(p, gain); changed += 1
    print(f"{n} clips measured, {changed} re-levelled to {a.target} LUFS")


if __name__ == "__main__":
    sys.exit(main())
