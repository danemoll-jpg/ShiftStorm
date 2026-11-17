"""ShiftStorm Round 7: copied from Tee'd Off's Tools/Music/make_loops.py (read-only there). Paths and the track list are ShiftStorm's;
the loop search is unchanged.

Turns Dan's Suno tracks into game music with seamless loops.

    python Tools/Music/make_loops.py            # analyse, write the game files and the listening checks

For each looping track (title, course) it finds the tempo and the beat grid, then a loop region that starts and ends on
a bar line, with matching sound and loudness on both sides of the seam and before any fade-out. It writes:

  Assets/_Project/Resources/Audio/Music/<name>_intro.ogg   the track up to the loop start (played once)
  Assets/_Project/Resources/Audio/Music/<name>.ogg         the loop body; its end is crossfaded (about 100 ms, on the
                                                           beat) into the audio just before the loop start, so playing
                                                           it again from sample 0 continues the music with no seam
  Builds/audio_check/<name>_3loops.wav                     intro + the loop body three times, for listening
  Tools/Music/loops.json                                   tempo, loop points, seam checks

round_end plays once: it is copied whole (with a short fade-in guard against a click). The game plays the intro and
then the body with AudioSource.PlayScheduled (sample-accurate), the body looping on its own AudioSource.
The originals stay in Art/Music/ (moved out of incoming/ once imported).
"""
import json
import os
import shutil
import subprocess
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
SRC_DIRS = [os.path.join(REPO, "Art", "Incoming", "Music"), os.path.join(REPO, "Art", "Music")]
OUT = os.path.join(REPO, "Assets", "Resources", "Audio", "Music")
CHECK = os.path.join(REPO, "local", "audio_check")
SR = 44100
XFADE = 0.10  # seconds
TRACKS = {"race_paradise": True, "race_coaster": True, "race_bigtop": True, "menu": True, "results": False, "theme": True}  # ShiftStorm: the race, menu / Garage, results, title theme  # Round 30-31: Farmageddon (the front nine, the back nine's alien take)
# Round 20: tracks with sung parts get hand-picked loop points instead of the search, so no vocal is cut at the seam.
# course_lunar: Dan's space-age pop with wordless "oohs" / "pows". The vocals were located with a Demucs vocal stem
# (htdemucs, run once outside the repo): both points sit in vocal-free stretches (the stem stays below -70 dBFS for
# 0.6 s either side; sung parts run near -20 dBFS). Start = the downbeat of the instrumental phrase at bar 32
# (70.78 s), end = the phrase downbeat 4 bars into the outro's section (161.41 s), 42 bars. Both sit 30 ms before
# their downbeat's kick. The track speeds up slightly (110.75 -> 111.45 bpm) near 92 s, so the body is the exact
# samples between the two points, not a whole number of bars at one tempo.
# minigolf_lunar (Round 21): the same song's second take, sung from 8 s to 167 s. Same method (an htdemucs vocal stem, run
# once outside the repo), then the end lined up with the start by the spectrum of the two bars either side (the take drifts
# from 112.1 to 113.2 bpm, so a fixed bar grid lands 0.18 s off). Start 12.934 s: the stem is 32 dB under the singing there
# (synth bleed), and the first sung word starts 0.87 s later; end 174.730 s: the stem is silent (about -80 dBFS) for 0.6 s
# either side. 76 bars (161.80 s): nearly the whole song loops; only the first verse line before 12.9 s plays once.
# course_haunted (Round 22): Dan's "Bogey Manor", a ghost choir sung almost all the way through (wordless "oohs"). Same
# method: an htdemucs vocal stem (run once outside the repo), then every beat-grid pair whose both points are vocal-free
# (stem below -50 dBFS for 0.6 s either side) and a whole number of bars apart, ranked by the spectrum of the bar either
# side. Start 13.365 s: inside the instrumental stretch after the opening line (stem -77 dBFS); end 131.015 s: the gap
# before the last chorus (stem -56 dBFS, 38 dB under the singing). 49 bars at 99.96 bpm; the opening line plays once.
# minigolf_haunted (Round 23): Dan's second take of the Bogey Manor song for Haunted Putt (179.4 s, 101.25 bpm). Same method:
# an htdemucs vocal stem (run once outside the repo), every bar-line pair with the stem below -50 dBFS for 0.6 s either
# side, ranked by the spectrum of the bar either side. Start 16.639 s (stem -54 dBFS), end 137.532 s (stem -51 dBFS); the
# singing runs near -18 dBFS, so both seams sit 33-36 dB under it. 51 bars; the opening plays once.
# course_atlantis (Round 24): Dan's "Lost City Links" (aquatic exotica with a wordless siren choir; 179.9 s, 106.92 bpm).
# Same method (an htdemucs vocal stem, run once outside the repo; bar-line pairs with the stem below -50 dBFS for 0.6 s
# either side, ranked by the spectrum either side). Start 14.021 s (stem -72 dBFS), end 159.926 s (stem -50 dBFS; the choir
# sings near -19.5 dBFS). 65 bars; the opening plays once.
# course_funland (Round 26): Dan's "Fairway Funland" (a bouncy park tune, mostly instrumental with a few sung lines; 178.8 s,
# 123.36 bpm). Same method (an htdemucs vocal stem, run once outside the repo; bar-line pairs with the stem below -50 dBFS for
# 0.6 s either side, ranked by the spectrum either side). Start 33.439 s (stem -77 dBFS), end 173.520 s (stem -76 dBFS); the
# singing runs near -30 dBFS. 72 bars; the opening plays once.
# course_groovy (Round 28): Dan's "Groovy Greens" (a 1969 psychedelic tune with fuzz guitar, organ and wordless "la-la"s; 180.0 s,
# 104.22 bpm). Same method, now scripted (Tools/Music/find_vocal_seams.py on an htdemucs vocal stem made outside the repo).
# Start 4.803 s (stem -77 dBFS), end 161.393 s (stem -75 dBFS); the singing runs near -20 dBFS. 68 bars; the first bars play once.
# course_farm / course_farm_alien (Rounds 30-31): Dan's two Farmageddon tracks (country, then the same tune turned spacey for the
# back nine). find_vocal_seams.py on htdemucs stems: course_farm 7.040-159.377 s (stems -59 / -62 dBFS, singing near -21);
# course_farm_alien 9.091-190.878 s (stems -68 / -54 dBFS, singing near -20). The two crossfade at the turn.
# course_seasons_* (Round 33): Dan's four arrangements of the Fore! Seasons theme (all near 100 bpm). find_vocal_seams.py on
# htdemucs stems: spring, summer and winter are instrumental (the stem stays near -60 to -70 dBFS throughout); autumn is sung
# (near -26 dBFS) and both its seams sit in vocal-free gaps (-74 / -76 dBFS). The longest high-scoring loop of each.
# course_expedition / course_expedition_ruins (Rounds 35-36): Dan's jungle adventure and its ancient-mystery cover (same melody,
# both sung near -20 dBFS). find_vocal_seams.py on htdemucs stems: course_expedition 13.170-151.001 s (stems -75 / -74 dBFS);
# course_expedition_ruins 72.363-150.905 s (stems -56 / -73 dBFS; its first 72 s play once; 65.818 left a timbre jump). The two crossfade at the turn.
# course_impossible (Rounds 38-39): Dan's finale medley (mostly instrumental, singing near -36 dBFS). find_vocal_seams.py on an
# htdemucs stem: 26.422-168.552 s (stems -71 / -77 dBFS). credits plays once (the end credits).
PINNED = {"course_impossible": {"start": 26.422, "end": 168.552, "bars": 71, "bpm": 119.89},
          "course_expedition": {"start": 13.170, "end": 151.001, "bars": 63, "bpm": 109.70},
          "course_expedition_ruins": {"start": 72.363, "end": 150.905, "bars": 36, "bpm": 110.00},
          "course_seasons_spring": {"start": 10.022, "end": 98.085, "bars": 37, "bpm": 100.84},
          "course_seasons_summer": {"start": 12.359, "end": 117.979, "bars": 44, "bpm": 99.98},
          "course_seasons_autumn": {"start": 12.246, "end": 174.443, "bars": 67, "bpm": 99.14},
          "course_seasons_winter": {"start": 29.467, "end": 123.672, "bars": 39, "bpm": 99.36},
          "course_farm": {"start": 7.040, "end": 159.377, "bars": 47, "bpm": 74.05},
          "course_farm_alien": {"start": 9.091, "end": 190.878, "bars": 81, "bpm": 106.94},
          "course_groovy": {"start": 4.803, "end": 161.393, "bars": 68, "bpm": 104.22},
          "course_funland": {"start": 33.439, "end": 173.520, "bars": 72, "bpm": 123.36},
          "course_atlantis": {"start": 14.021, "end": 159.926, "bars": 65, "bpm": 106.92},
          "course_haunted": {"start": 13.365, "end": 131.015, "bars": 49, "bpm": 99.96},
          "minigolf_haunted": {"start": 16.639, "end": 137.532, "bars": 51, "bpm": 101.25},
          "course_lunar": {"start": 70.779, "end": 161.407, "bars": 42, "bpm": 110.75},
          "minigolf_lunar": {"start": 12.934, "end": 174.730, "bars": 76, "bpm": 112.73}}
# Round 8: course tracks that are made later; until Dan drops the file in, the game plays "course" in their place.
OPTIONAL = {"course_seaside", "minigolf", "course_suburban", "course_candy", "course_jurassic", "course_backyard",
            "course_lunar", "minigolf_lunar", "course_haunted", "minigolf_haunted", "course_atlantis", "course_funland", "course_groovy", "course_farm", "course_farm_alien",
            "course_seasons_spring", "course_seasons_summer", "course_seasons_autumn", "course_seasons_winter",
            "course_expedition", "course_expedition_ruins", "course_impossible", "credits"}


# ShiftStorm: Dan's Suno downloads keep their own names
ALIASES = {"race_paradise": "ShiftStorm - Paradise Plunge", "race_coaster": "ShiftStorm - Coaster Coast", "race_bigtop": "ShiftStorm - Big Top Breakdown", "menu": "ShiftStorm - Garage", "theme": "ShiftStorm - Theme", "results": "ShiftStorm - Victory"}
def find_source(name):
    for d in SRC_DIRS:
        p = os.path.join(d, name + ".mp3")
        if not os.path.exists(p) and name in ALIASES: p = os.path.join(d, ALIASES[name] + ".mp3")
        if os.path.exists(p):
            return p
    if name in OPTIONAL:
        return None
    raise SystemExit(f"{name}.mp3 not found in Art/Music/incoming or Art/Music")


def decode(path):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-f", "f32le", "-ac", "2", "-ar", str(SR), "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.float32).reshape(-1, 2).copy()


def write_wav(path, x):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2").tobytes()
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ar", str(SR), "-ac", "2", "-i", "-", path], input=pcm, check=True)


def write_ogg(path, x):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2").tobytes()
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ar", str(SR), "-ac", "2", "-i", "-",
                    "-c:a", "libvorbis", "-q:a", "6", path], input=pcm, check=True)


HOP, NFFT = 512, 2048


def stft_mag(mono):
    win = np.hanning(NFFT).astype(np.float32)
    n = 1 + (len(mono) - NFFT) // HOP
    frames = np.lib.stride_tricks.as_strided(mono, shape=(n, NFFT), strides=(mono.strides[0] * HOP, mono.strides[0]))
    return np.abs(np.fft.rfft(frames * win, axis=1)).astype(np.float32)


def bands(mag, nb=40):
    """Log-spaced band energies (a rough mel spectrum) per frame."""
    freqs = np.fft.rfftfreq(NFFT, 1 / SR)
    edges = np.geomspace(60, 14000, nb + 1)
    out = np.zeros((mag.shape[0], nb), np.float32)
    for i in range(nb):
        m = (freqs >= edges[i]) & (freqs < edges[i + 1])
        if m.any():
            out[:, i] = mag[:, m].mean(axis=1)
    return np.log1p(out * 10)


def tempo_and_phase(flux):
    fps = SR / HOP
    ac = np.correlate(flux - flux.mean(), flux - flux.mean(), "full")[len(flux) - 1:]
    lags = np.arange(len(ac))
    bpm_of = lambda lag: 60 * fps / lag
    lo, hi = int(60 * fps / 180), int(60 * fps / 70)
    lag = lo + int(np.argmax(ac[lo:hi]))
    # refine to sub-frame with a parabola
    if 0 < lag < len(ac) - 1:
        a, b, c = ac[lag - 1], ac[lag], ac[lag + 1]
        den = a - 2 * b + c
        if abs(den) > 1e-9:
            lag = lag + float(np.clip(0.5 * (a - c) / den, -0.5, 0.5))
    period = lag  # frames per beat
    best, phase = -1, 0.0
    for ph in np.linspace(0, period, 64, endpoint=False):
        idx = np.round(np.arange(ph, len(flux), period)).astype(int)
        idx = idx[idx < len(flux)]
        s = flux[idx].sum()
        if s > best:
            best, phase = s, ph
    return bpm_of(lag), period / fps, phase / fps


def find_loop(x, bpm, beat_s, phase_s):
    mono = x.mean(axis=1)
    mag = stft_mag(mono)
    bd = bands(mag)
    fps = SR / HOP
    rms = moving_rms(mono, SR)
    dur = len(mono) / SR
    med = np.median(rms[int(SR * 5):int(len(rms) * 0.8)])
    # The usable end: before the fade-out / ending (loudness stays near the body's).
    usable_end = dur
    for t in np.arange(dur - 1, dur * 0.5, -0.25):
        if rms[int(t * SR)] > 0.75 * med:
            usable_end = t - 0.5
            break
    bar = beat_s * 4
    beats = np.arange(phase_s, dur, beat_s)
    bars = beats[::4]
    best = None
    ranked = []  # Round 18: every candidate, best first (the caller takes the first with a clean seam)
    for a in bars:
        if a < 2.0 or a > dur * 0.45:
            continue
        for b in bars:
            n = round((b - a) / bar)
            if b <= a or n < 16 or b > usable_end:  # at least 16 bars, so the loop is not obviously short
                continue
            fa, fb = int(a * fps), int(b * fps)
            w = int(bar * fps)
            if fb + w >= len(bd) or fa - w < 0:
                continue
            # The sound just before / after each point should match (the seam joins "before b" to "after a").
            pre = np.corrcoef(bd[fa - w:fa].ravel(), bd[fb - w:fb].ravel())[0, 1]
            post = np.corrcoef(bd[fa:fa + w].ravel(), bd[fb:fb + w].ravel())[0, 1]
            loud = min(rms[int(a * SR)], rms[int(b * SR)]) / max(1e-6, max(rms[int(a * SR)], rms[int(b * SR)]))
            score = pre + post + 0.5 * loud + 0.004 * (b - a)  # prefer longer loops a little
            ranked.append((score, a, b, pre, post, loud))
            if best is None or score > best[0]:
                best = (score, a, b, pre, post, loud)
    ranked.sort(key=lambda c: -c[0])
    find_loop.ranked = ranked
    return best, usable_end


def moving_rms(mono, n):
    """RMS over a centred window of n samples (running sum: fast on long tracks)."""
    c = np.concatenate([[0.0], np.cumsum(mono.astype(np.float64) ** 2)])
    i = np.arange(len(mono))
    lo, hi = np.clip(i - n // 2, 0, len(mono)), np.clip(i + n // 2, 0, len(mono))
    return np.sqrt((c[hi] - c[lo]) / np.maximum(1, hi - lo)).astype(np.float32)


def zero_cross(x, i, radius=200):
    m = x[max(0, i - radius):i + radius].mean(axis=1)
    s = np.where(np.diff(np.signbit(m)))[0]
    if len(s) == 0:
        return i
    return max(0, i - radius) + int(s[np.argmin(np.abs(s - radius))])


def make_body(x, a, b):
    """Loop body x[a:b] whose last XFADE seconds crossfade (equal power) into x[a-X:a], so the end flows into sample 0."""
    X = int(XFADE * SR)
    body = x[a:b].copy()
    t = np.linspace(0, 1, X, dtype=np.float32)[:, None]
    fade_out, fade_in = np.cos(t * np.pi / 2), np.sin(t * np.pi / 2)
    body[-X:] = x[b - X:b] * fade_out + x[a - X:a] * fade_in
    return body


def seam_check(body):
    """Discontinuity at the wrap vs the body's normal sample-to-sample steps, and the spectral jump at the seam vs elsewhere."""
    loop = np.concatenate([body[-SR:], body[:SR]])
    d = np.abs(np.diff(loop.mean(axis=1)))
    seam_step = float(d[SR - 3:SR + 3].max())
    typical = float(np.percentile(np.abs(np.diff(body.mean(axis=1))), 99.9))
    bd = bands(stft_mag(np.concatenate([body[-SR * 2:], body[:SR * 2]]).mean(axis=1)))
    flux = np.r_[0, np.sqrt((np.diff(bd, axis=0).clip(0) ** 2).sum(axis=1))]
    centre = len(flux) // 2
    seam_flux = float(flux[centre - 3:centre + 3].max())
    flux_body = np.r_[0, np.sqrt((np.diff(bands(stft_mag(body.mean(axis=1))), axis=0).clip(0) ** 2).sum(axis=1))]
    return {"seam_step": seam_step, "typical_step_p99_9": typical, "step_ok": bool(seam_step <= typical * 1.05),
            "seam_flux": seam_flux, "flux_p95": float(np.percentile(flux_body, 95)), "flux_ok": bool(seam_flux <= np.percentile(flux_body, 99))}


def main():
    # Round 16: "python make_loops.py course_candy" makes only the named tracks (the others keep their files as they are).
    only = set(sys.argv[1:])
    report = {}
    if only and os.path.exists(os.path.join(HERE, "loops.json")):
        with open(os.path.join(HERE, "loops.json"), encoding="utf-8") as f:
            report = json.load(f)
    for name, loops in TRACKS.items():
        if only and name not in only:
            continue
        src = find_source(name)
        if src is None:
            print(f"{name}: not made yet (the game plays the course track in its place)")
            continue
        x = decode(src)
        dur = len(x) / SR
        if not loops:
            y = x.copy()
            g = np.linspace(0, 1, int(0.005 * SR), dtype=np.float32)[:, None]
            y[:len(g)] *= g
            y[-len(g):] *= g[::-1]
            write_ogg(os.path.join(OUT, name + ".ogg"), y)
            write_wav(os.path.join(CHECK, name + "_once.wav"), y)
            report[name] = {"seconds": round(dur, 2), "loops": False}
            print(f"{name}: {dur:.1f} s, plays once")
            continue
        mono = x.mean(axis=1)
        bd = bands(stft_mag(mono))
        flux = np.r_[0, (np.diff(bd, axis=0).clip(0)).sum(axis=1)]
        bpm, beat_s, phase_s = tempo_and_phase(flux)
        best, usable_end = find_loop(x, bpm, beat_s, phase_s)
        if name in PINNED:
            pin = PINNED[name]
            bpm, beat_s = pin["bpm"], 60.0 / pin["bpm"]
            a, b = zero_cross(x, int(round(pin["start"] * SR))), zero_cross(x, int(round(pin["end"] * SR)))
            fa, fb, w = a // HOP, b // HOP, int(beat_s * 4 * SR / HOP)
            pre = np.corrcoef(bd[fa - w:fa].ravel(), bd[fb - w:fb].ravel())[0, 1]
            post = np.corrcoef(bd[fa:fa + w].ravel(), bd[fb:fb + w].ravel())[0, 1]
            rms = moving_rms(mono, SR)
            loud = min(rms[a], rms[b]) / max(1e-6, max(rms[a], rms[b]))
            best = (0.0, a / SR, b / SR, pre, post, loud)
            find_loop.ranked = []
        if best is None:
            raise SystemExit(f"{name}: no loop region found")
        # Round 18: the best-scoring loop's seam can still jump (a fill or an accent right on the join): try the next
        # candidates in score order and keep the first whose seam is clean (the best one if none is).
        chosen = None
        for cand in [best] + find_loop.ranked[:60]:
            score, a_s, b_s, pre, post, loud = cand
            a = zero_cross(x, int(round(a_s * SR)))
            b = a + int(round((b_s - a_s) / (beat_s * 4)) * beat_s * 4 * SR)  # whole bars, sample-exact length
            if name in PINNED:
                b = int(round(b_s * SR))  # the hand-picked end (already on a zero crossing)
            body = make_body(x, a, b)
            check = seam_check(body)
            if chosen is None:
                chosen = (cand, a, b, body, check)
            if check["step_ok"] and check["flux_ok"]:
                chosen = (cand, a, b, body, check)
                break
        (score, a_s, b_s, pre, post, loud), a, b, body, check = chosen
        intro = x[:a].copy()
        write_ogg(os.path.join(OUT, name + "_intro.ogg"), intro)
        write_ogg(os.path.join(OUT, name + ".ogg"), body)
        write_wav(os.path.join(CHECK, name + "_3loops.wav"), np.concatenate([intro, body, body, body]))
        write_wav(os.path.join(CHECK, name + "_seam.wav"), np.concatenate([body[-4 * SR:], body[:4 * SR]]))  # the seam at 4.0 s
        # The encoded files must keep the exact lengths (no encoder padding = no gap) and the clean seam.
        enc_body, enc_intro = decode(os.path.join(OUT, name + ".ogg")), decode(os.path.join(OUT, name + "_intro.ogg"))
        enc = seam_check(enc_body)
        check["encoded_exact"] = bool(len(enc_body) == len(body) and len(enc_intro) == len(intro))
        check["encoded_seam_ok"] = bool(enc["step_ok"] and enc["flux_ok"])
        report[name] = {
            "seconds": round(dur, 2), "loops": True, "bpm": round(bpm, 2), "bar_seconds": round(beat_s * 4, 4),
            "loop_start_s": round(a / SR, 4), "loop_end_s": round(b / SR, 4), "loop_bars": PINNED[name]["bars"] if name in PINNED else int(round((b - a) / SR / (beat_s * 4))),
            "intro_samples": int(a), "body_samples": int(b - a), "usable_end_s": round(float(usable_end), 2),
            "match_before": round(float(pre), 3), "match_after": round(float(post), 3), "loudness_match": round(float(loud), 3),
            "crossfade_ms": int(XFADE * 1000), "seam": check,
        }
        print(f"{name}: {dur:.1f} s, {bpm:.1f} bpm, loop {a / SR:.2f}-{b / SR:.2f} s ({report[name]['loop_bars']} bars), "
              f"match {pre:.2f}/{post:.2f}, loudness {loud:.2f}; seam step {check['seam_step']:.4f} vs typical {check['typical_step_p99_9']:.4f} "
              f"({'OK' if check['step_ok'] else 'SPIKE'}), seam flux {check['seam_flux']:.2f} vs p95 {check['flux_p95']:.2f} ({'OK' if check['flux_ok'] else 'JUMP'})")
    with open(os.path.join(HERE, "loops.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, indent=1)
    ok = all(not r.get("loops") or "seam" not in r or all(r["seam"][k] for k in ("step_ok", "flux_ok", "encoded_exact", "encoded_seam_ok")) for r in report.values())
    print("LOOPS OK" if ok else "LOOP SEAM CHECK FAILED")
    return 0 if ok else 3


if __name__ == "__main__":
    sys.exit(main())
