"""Round 15 item 2: Barnaby by name. Is "Barker" (or "TeedOff Barker") shown or spoken anywhere a player meets it?

1. The built game's text: every string literal in the shipped Assembly-CSharp.dll (the .NET user-string heap, UTF-16) and every text
   asset in the shipped data files (lines.json etc.), searched for "Barker". Strings that are only ids, folder names, log lines or
   check-report text are listed apart from what a player can see.
2. What is spoken: every voice clip the game ships (Assets/Resources/Audio/Voice/<speaker>/*.ogg: Barnaby, Buck, Abi, Rae, Chip, Vivi)
   transcribed with local speech-to-text (faster-whisper small.en, via voice_audit.local_stt) and the written words (lines.json),
   searched for "Barker". Also lists the new Round 15 lines with a word match score (written vs heard).
Writes Docs/Checks/round15/barnaby.md. Usage: python Tools/Audio/barnaby_audit.py [--no-stt]
"""
import argparse, difflib, glob, json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import voice_audit  # noqa: E402
import buck_audit  # noqa: E402
import caller_lines, buck_lines, driver_lines  # noqa: E402

REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
VOICE = os.path.join(REPO, "Assets", "Resources", "Audio", "Voice")
BUILD = os.path.join(REPO, "Builds", "ShiftStorm")
OUT = os.path.join(REPO, "Docs", "Checks", "round15", "barnaby.md")
WORD = re.compile(r"barker", re.I)
# strings in the code a player never sees: log lines ([Check], [Caller], Debug.Log), SoundLog / check report text, ids and folders
NOT_SHOWN = re.compile(r"^\[|BarkerCheck|Audio/Voice/barker|^barker$|^barker/|Check|PROBE|LEVELS|MIX|MESS-UPS|PLACES|BUCK|CHALLENGES|skipped|cut by", re.I)


def user_strings(dll):
    """The #US heap of a .NET assembly: length-prefixed UTF-16 strings."""
    data = open(dll, "rb").read(); out = []
    i = data.find(b"#US\x00")
    if i < 0: return out
    # stream headers: find the #US entry's offset / size in the metadata root (offset, size precede the name)
    meta = data.find(b"BSJB"); off = int.from_bytes(data[i - 8:i - 4], "little"); size = int.from_bytes(data[i - 4:i], "little")
    heap = data[meta + off: meta + off + size]; p = 1
    while p < len(heap):
        b0 = heap[p]
        if b0 & 0x80 == 0: n, p = b0, p + 1
        elif b0 & 0xC0 == 0x80: n, p = ((b0 & 0x3F) << 8) | heap[p + 1], p + 2
        else: n, p = ((b0 & 0x1F) << 24) | (heap[p + 1] << 16) | (heap[p + 2] << 8) | heap[p + 3], p + 4
        if n == 0: continue
        s = heap[p:p + n - 1].decode("utf-16le", "replace"); p += n
        out.append(s)
    return out


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--no-stt", action="store_true"); a = ap.parse_args()
    lines = ["# Barnaby by name (Round 15 item 2)", "",
             "Is \"Barker\" shown or spoken anywhere a player meets it? (`Tools/Audio/barnaby_audit.py`)", ""]
    # 1. the built game's text
    dlls = glob.glob(os.path.join(BUILD, "*_Data", "Managed", "Assembly-CSharp.dll"))
    shown, hidden = [], []
    for d in dlls:
        for s in user_strings(d):
            if WORD.search(s): (hidden if NOT_SHOWN.search(s.strip()) else shown).append(s)
    data_hits = []
    for f in glob.glob(os.path.join(BUILD, "*_Data", "*.assets")) + glob.glob(os.path.join(BUILD, "*_Data", "*.resource")):
        raw = open(f, "rb").read()
        for m in re.finditer(rb"[ -~]{0,60}TeedOff Barker[ -~]{0,60}|\"[^\"]{0,80}Barker[^\"]{0,80}\"", raw): data_hits.append((os.path.basename(f), m.group(0).decode("ascii", "replace")))
    lines += ["## 1. The built game's text", "",
              f"Searched: {', '.join(os.path.relpath(d, REPO) for d in dlls) or '(no build)'} (every string literal) and the shipped data files.", "",
              f"- Strings a player can see containing \"Barker\": **{len(shown)}**" + ("" if not shown else ": " + "; ".join(f"`{s[:120]}`" for s in shown[:20])),
              f"- Strings containing \"Barker\" that are only log / check-report text or ids (never on screen): {len(hidden)}" + ("" if not hidden else ": " + "; ".join(f"`{s[:90]}`" for s in hidden[:12])),
              f"- \"Barker\" in the shipped data files (subtitles, lines.json): **{len(data_hits)}**" + ("" if not data_hits else ": " + "; ".join(f"{f}: `{t[:100]}`" for f, t in data_hits[:10])), ""]
    # 2. what is spoken
    rows, spoken, written = [], 0, 0; new15 = set(caller_lines.ROUND15) | {k for k in buck_lines.LINES if k.startswith(("brief_bt", "short_bt", "res_bt", "tour_s3", "ch_c3", "tour_done"))} | set(driver_lines.ROUND15["vivi"])
    total = 0
    for who in ["barker", "buck", "abigail", "rae", "chip", "vivi"]:
        folder = os.path.join(VOICE, who); words = {}
        js = os.path.join(folder, "lines.json")
        if os.path.exists(js):
            for l in json.load(open(js, encoding="utf-8")).get("lines", []): words[l["id"]] = l["text"]
        for p in sorted(glob.glob(os.path.join(folder, "*.ogg"))):
            k = os.path.splitext(os.path.basename(p))[0]; total += 1
            w = words.get(k, ""); heard = "" if a.no_stt else (voice_audit.local_stt(p) or "")
            if WORD.search(w): written += 1
            if WORD.search(heard): spoken += 1
            if k in new15 or WORD.search(w) or WORD.search(heard):
                score = difflib.SequenceMatcher(None, buck_audit.norm(w), buck_audit.norm(heard)).ratio() if heard and w else 0
                rows.append((who, k, w, heard, score))
            print(f"{who}/{k}", flush=True)
    lines += ["## 2. What is spoken", "",
              f"{total} voice clips (Barnaby, Buck, Abi, Rae, Chip, Vivi) transcribed with local speech-to-text (faster-whisper small.en)"
              f"{' (skipped: --no-stt)' if a.no_stt else ''}.", "",
              f"- Clips whose written words say \"Barker\": **{written}**", f"- Clips in which \"Barker\" is heard: **{spoken}**", "",
              "The Round 15 lines (and any clip that says \"Barker\"): written, heard, word match (1.00 = identical; under 0.80 flagged).", "",
              "| who | id | written | heard | match |", "|---|---|---|---|---|"]
    for who, k, w, h, sc in rows:
        name = "Barnaby" if who == "barker" else who.capitalize()
        lines.append(f"| {name} | {k} | {w} | {h} | {sc:.2f}{' CHECK' if sc < .8 else ''} |")
    verdict = "PASS" if not shown and not data_hits and spoken == 0 and written == 0 else "FAIL"
    lines += ["", f"**{verdict}**: no \"Barker\" shown or spoken to the player." if verdict == "PASS" else f"**{verdict}**: see above."]
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    open(OUT, "w", encoding="utf-8").write("\n".join(lines) + "\n")
    print(f"BARNABY AUDIT {verdict}: shown {len(shown)}, data {len(data_hits)}, written {written}, heard {spoken} ({total} clips); {OUT}")


if __name__ == "__main__":
    main()
