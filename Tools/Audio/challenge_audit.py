"""Round 13 item 4: the rival challenges' lines (Rae, Chip, Buck, Barnaby): the words written, what local speech-to-text hears in the
clip (faster-whisper small.en, via Tools/Audio/voice_audit.py), a word match score, the clip's length and where the game says it. Writes
Docs/Checks/round13/challenge_lines.md. A line is flagged when the match is under 0.8."""
import difflib, os, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import voice_audit  # noqa: E402
import buck_audit  # noqa: E402
import driver_lines, buck_lines, caller_lines  # noqa: E402

REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
VOICE = os.path.join(REPO, "Assets", "Resources", "Audio", "Voice")
OUT = os.path.join(REPO, "Docs", "Checks", "round13", "challenge_lines.md")
USE = {"chintro": "the rival's taunt before the 3-2-1", "chwin": "the rival won the challenge", "chlose": "the rival lost the challenge",
       "ch_c1_intro": "Buck before Rae's Waterfall Dash", "ch_c2_intro": "Buck before Chip's Coaster Showdown", "ch_reward": "Buck after a first win (the paint)",
       "chstart": "Barnaby at the challenge's start", "chwon": "Barnaby: the player won", "chlost": "Barnaby: the rival won"}


def main():
    items = []
    for who, d in driver_lines.ROUND13.items():
        for k, t in d.items(): items.append((who, k, t, os.path.join(VOICE, who, k + ".ogg")))
    for k, t in buck_lines.LINES.items():
        if k.startswith("ch_"): items.append(("buck", k, t, os.path.join(VOICE, "buck", k + ".ogg")))
    for k, t in caller_lines.ROUND13.items(): items.append(("barker", k, t, os.path.join(VOICE, "barker", k + ".ogg")))
    rows, flagged = [], 0
    for who, k, t, p in items:
        words = " ".join(part.split("] ", 1)[-1] for part in t.split("["))
        if not os.path.exists(p): rows.append((who, k, words, "(missing)", 0, 0, "MISSING")); flagged += 1; continue
        heard = voice_audit.local_stt(p) or ""
        score = difflib.SequenceMatcher(None, buck_audit.norm(words), buck_audit.norm(heard)).ratio(); sec = buck_audit.length(p)
        use = next((u for pre, u in USE.items() if k.startswith(pre + "_") or k.startswith(pre)), "?")
        bad = score < .8; flagged += bad
        rows.append((who, k, words.strip(), heard.strip(), score, sec, ("CHECK " if bad else "") + use)); print(f"{who}/{k}: {score:.2f} {sec:.1f}s", flush=True)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("# Rival challenge lines (Round 13)\n\nThe new clips for The Tour's rival challenges (ElevenLabs, each in its own voice: Rae and Chip as in Round 8, "
                "\"ShiftStorm Buck\", the TeedOff Barker voice; eleven_v3, -16 LUFS): the words written, what local speech-to-text (faster-whisper "
                f"small.en) hears, the word match (1.00 = identical), the length and where the game says it. {len(rows)} clips; flagged: {flagged} (match under 0.80).\n\n")
        f.write("| who | id | written | heard | match | s | used for |\n|---|---|---|---|---|---|---|\n")
        for who, k, w, h, sc, sec, use in rows: f.write(f"| {who} | {k} | {w} | {h} | {sc:.2f} | {sec:.1f} | {use} |\n")
    print(f"{len(rows)} clips, flagged {flagged}; wrote {OUT}")


if __name__ == "__main__":
    main()
