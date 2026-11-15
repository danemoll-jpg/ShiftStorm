"""Round 11 item 1: every Buck line: the words written (lines.json), what local speech-to-text hears in the clip (faster-whisper small.en,
via Tools/Audio/voice_audit.py), a word match score, the clip's length, and where the game uses it (its pool). Writes
Docs/Checks/round11/buck_lines.md. A line is flagged when the match is under 0.8 or a briefing runs outside 8-22 s."""
import json, os, re, subprocess, sys, difflib, shutil

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import voice_audit  # noqa: E402

REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(REPO, "Assets", "Resources", "Audio", "Voice", "buck")
OUT = os.path.join(REPO, "Docs", "Checks", "round11", "buck_lines.md")
USE = [("brief_pp", "briefing, Paradise Plunge (the private trial)"), ("brief_cc", "briefing, Coaster Coast (opening night)"),
       ("short_pp", "short briefing, Paradise Plunge"), ("short_cc", "short briefing, Coaster Coast"),
       ("res_pp_first", "results, Paradise Plunge, 1st"), ("res_pp_podium", "results, Paradise Plunge, 2nd-3rd"), ("res_pp_back", "results, Paradise Plunge, 4th"),
       ("res_cc_first", "results, Coaster Coast, 1st"), ("res_cc_podium", "results, Coaster Coast, 2nd-3rd"), ("res_cc_back", "results, Coaster Coast, 4th"),
       ("yarn", "anecdote after a briefing (about one race in three)"), ("tour_s1_intro", "The Tour, stop 1 scene"), ("tour_s1_pass", "The Tour, stop 1 passed"),
       ("tour_s1_retry", "The Tour, stop 1 not passed"), ("tour_s2_intro", "The Tour, stop 2 scene"), ("tour_s2_pass", "The Tour, stop 2 passed"),
       ("tour_s2_retry", "The Tour, stop 2 not passed"), ("tour_done", "The Tour, finished")]


def norm(s): return re.findall(r"[a-z0-9']+", s.lower().replace("'", "'"))


def length(path):
    ff = shutil.which("ffprobe") or "ffprobe"
    r = subprocess.run([ff, "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path], capture_output=True, text=True)
    try: return float(r.stdout.strip())
    except ValueError: return 0.0


def main():
    words = {l["id"]: l["text"] for l in json.load(open(os.path.join(SRC, "lines.json"), encoding="utf-8"))["lines"]}
    rows, flagged, total_s = [], 0, 0.0
    for k in sorted(words):
        p = os.path.join(SRC, k + ".ogg")
        if not os.path.exists(p): rows.append((k, words[k], "(missing)", 0, 0, "MISSING")); flagged += 1; continue
        heard = voice_audit.local_stt(p) or ""
        score = difflib.SequenceMatcher(None, norm(words[k]), norm(heard)).ratio()
        sec = length(p); total_s += sec
        use = next((u for pre, u in USE if k.startswith(pre + "_")), "?")
        bad = score < .8 or (k.startswith("brief_") and not 8 <= sec <= 22)
        flagged += bad
        rows.append((k, words[k], heard, score, sec, ("CHECK " if bad else "") + use))
        print(f"{k}: {score:.2f} {sec:.1f}s {'CHECK' if bad else ''}", flush=True)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("# Buck's lines (Round 11)\n\nEvery clip in `Assets/Resources/Audio/Voice/buck/` (ElevenLabs voice \"ShiftStorm Buck\", eleven_v3, -16 LUFS): the words "
                "written, what local speech-to-text (faster-whisper small.en) hears, the word match (1.00 = identical), the clip length, and where the "
                f"game says it. {len(rows)} clips, {total_s:.0f} s in all; flagged: {flagged} (match under 0.80, or a briefing outside 8-22 s).\n\n")
        f.write("| id | written | heard | match | s | used for |\n|---|---|---|---|---|---|\n")
        for k, w, h, sc, sec, use in rows:
            f.write(f"| {k} | {w} | {h.strip()} | {sc:.2f} | {sec:.1f} | {use} |\n")
    print(f"{len(rows)} clips, flagged {flagged}; wrote {OUT}")


if __name__ == "__main__":
    main()
