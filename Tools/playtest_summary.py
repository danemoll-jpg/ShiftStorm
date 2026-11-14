"""Round 8 item 10: turn a folder of ShiftStorm playtest logs into one readable summary.

The game writes <data folder>/playtest/session-<date>-<time>.jsonl (one JSON record per race per player; Menu > Settings > Open log
folder shows where). Usage:
    python Tools/playtest_summary.py <folder> [--out summary.md]
<folder> may hold the .jsonl files directly or in sub-folders (one per friend's machine, say). Prints (and with --out writes) Markdown:
per player (races, finishes, best / mean lap, places, resets, wall hits, penalties, shortcuts, choice-jump picks) and per segment (mean /
best time, resets, wall hits, missed gates there), then every reset and incident in order.
"""
import argparse
import collections
import json
import os
import statistics
import sys


def load(folder):
    recs = []
    for root, _, files in os.walk(folder):
        for f in sorted(files):
            if not f.endswith(".jsonl"):
                continue
            with open(os.path.join(root, f), encoding="utf-8") as h:
                for n, line in enumerate(h, 1):
                    line = line.strip()
                    if not line:
                        continue
                    try:
                        r = json.loads(line); r["_file"] = os.path.relpath(os.path.join(root, f), folder); recs.append(r)
                    except json.JSONDecodeError:
                        print(f"skipped a broken line: {f}:{n}", file=sys.stderr)
    return recs


def fmt(t):
    return "-" if t is None or t < 0 else f"{int(t // 60)}:{t % 60:05.2f}"


def summary(recs):
    out = []
    w = out.append
    w(f"# ShiftStorm playtest summary\n\n{len(recs)} records from {len({r['_file'] for r in recs})} session file(s)\n")
    by = collections.defaultdict(list)
    for r in recs:
        by[f"{r.get('driver', '?')} ({r.get('player', '?')}, {r['session']})"].append(r)
    w("## Per player\n")
    w("| player | races | finished | best lap | mean lap | places | resets | wall hits | near misses | missed gates | shortcuts | Lake Jump |")
    w("|---|---|---|---|---|---|---|---|---|---|---|---|")
    for who, rs in sorted(by.items()):
        fin = [r for r in rs if r.get("result") == "finished"]
        laps = [r["lap"] for r in fin if r.get("lap", -1) > 0]
        places = ", ".join(f"{r['place']}/{r['field']}" for r in fin if r.get("field", 1) > 1) or "-"
        sc = collections.Counter(s["what"] for r in rs for s in r.get("shortcuts", []) if s.get("detail") == "in")
        ch = collections.Counter(c["detail"] for r in rs for c in r.get("choices", []))
        w(f"| {who} | {len(rs)} | {len(fin)} | {fmt(min(laps) if laps else None)} | {fmt(statistics.mean(laps) if laps else None)} | {places} | "
          f"{sum(r.get('resets', 0) for r in rs)} | {sum(r.get('wallHits', 0) for r in rs)} | {sum(r.get('nearMisses', 0) for r in rs)} | {sum(r.get('missedGates', 0) for r in rs)} | "
          f"{', '.join(f'{k} x{v}' for k, v in sc.items()) or '-'} | {', '.join(f'{k} x{v}' for k, v in ch.items()) or '-'} |")
    w("\n## Per segment\n")
    seg_t = collections.defaultdict(list); seg_ev = collections.defaultdict(collections.Counter); order = []
    for r in recs:   # the lap's own order, from the first finished lap
        if len(r.get("segmentNames", [])) > len(order):
            order = list(r["segmentNames"])
    for r in recs:
        for n, t in zip(r.get("segmentNames", []), r.get("segmentTimes", [])):
            seg_t[n].append(t)
            if n not in order: order.append(n)
        for key, lst in (("resets", "resetList"), ("wall hits", "walls"), ("gate trouble", "gates"), ("form trouble", "changes")):
            for e in r.get(lst, []):
                seg_ev[e.get("segment", "")][key] += 1
                if e.get("segment") and e["segment"] not in order: order.append(e["segment"])
    w("| segment | runs | mean | best | resets | wall hits | gate trouble | form trouble |")
    w("|---|---|---|---|---|---|---|---|")
    for n in order:
        ts = seg_t.get(n, []); ev = seg_ev.get(n, {})
        w(f"| {n} | {len(ts)} | {statistics.mean(ts):.1f} s | {min(ts):.1f} s | {ev.get('resets', 0)} | {ev.get('wall hits', 0)} | {ev.get('gate trouble', 0)} | {ev.get('form trouble', 0)} |" if ts else
          f"| {n} | 0 | - | - | {ev.get('resets', 0)} | {ev.get('wall hits', 0)} | {ev.get('gate trouble', 0)} | {ev.get('form trouble', 0)} |")
    w("\n## Resets and incidents\n")
    for r in recs:
        evs = [("reset", e) for e in r.get("resetList", [])] + [("gate", e) for e in r.get("gates", [])] + [("form", e) for e in r.get("changes", [])]
        if not evs:
            continue
        w(f"- **{r.get('driver')} ({r.get('player')}), race {r.get('race')}, {r.get('mode')}, {r.get('result')}**")
        for kind, e in sorted(evs, key=lambda x: x[1].get("t", 0)):
            w(f"  - {e.get('t', 0):6.1f} s, {e.get('segment', '')}, {e.get('at', 0):.0f} m: {kind}: {e.get('what', '')}")
    return "\n".join(out) + "\n"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("folder")
    ap.add_argument("--out")
    a = ap.parse_args()
    recs = load(a.folder)
    if not recs:
        print(f"no playtest logs (*.jsonl) under {a.folder}"); return 1
    text = summary(recs)
    print(text)
    if a.out:
        with open(a.out, "w", encoding="utf-8") as f:
            f.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
