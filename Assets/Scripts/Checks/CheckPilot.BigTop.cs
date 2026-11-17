using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 15 item 3: Big Top Breakdown's checks (course 2).
    //   btlap     the check pilot's safe-line lap from the grid: segment times, a picture at every landmark (player view), the rails' lowest
    //             speeds, set-piece visibility per segment (>= 8 % of the screen's height for >= 1 s), wagon hits, frame time
    //   btglobe   the Globe of Death and the cannon entered at full, slow and very slow (coasting) speed: through, off the end, no reset
    //   btsplits  both route choices timed (the Clown Jump: car through the Globe / fly past it; the river: main channel / Island rapids),
    //             AI runs on each (-runs N, default 3): reached the next gate, resets, stuck, wagon hits on the midway
    public sealed partial class CheckPilot
    {
        static float Bm(string n) => BigTop.Marks[n];
        Course BT => game.Courses[2];
        int WagonHits() => v.Log.Count(x => x.Contains("Circus wagon"));

        IEnumerator BigTopLap()
        {
            scenario = "btlap"; Clear(); smart = true; game.StartCourse(2, false);
            var c = v.Course; int segs = c.segments.Count;
            Line($"BT LAP: {c.courseName}, line {c.Length:0} m, {c.gates.Count} gates, {segs} segments, {c.anchors.Count} anchors, rails {string.Join(", ", BigTop.RailList.Select(r => $"{r.label} {r.Length:0} m"))}, shortcuts {string.Join(", ", c.shortcuts.Select(s => $"{s.label} {s.Length:0} m"))}");
            Shot(.6f, "grid-countdown");
            var marks = new (string name, float p)[]
            {
                ("1-midway", Bm("wagons") - 40), ("1-wagons", Bm("wagons") + 60), ("1-strongman", Bm("wagons") + 80), ("1-tightrope", Bm("midway") - 45), ("1-wagons-end", Bm("wagonsEnd") - 30),
                ("1-clown-jump", Bm("jC") - 30), ("1-over-the-pit", Bm("jC") + 15), ("1-globe-in", Bm("globe") + 10), ("1-in-the-globe", Bm("gIn") + 30), ("1-door", Bm("door") - 50),
                ("2-in-the-tent", Bm("door") + 25), ("2-ring", Bm("ringE")), ("2-ring-north", Bm("ringN")), ("2-to-the-cannon", Bm("ringW") + 5), ("2-in-the-cannon", Bm("breech") + 8),
                ("3-hoops", Bm("hoop1") - 5), ("3-trapeze", Bm("hoop2") - 5), ("3-top-flap", Bm("flap") - 20), ("3-over-the-camp", Bm("flap") + 120), ("3-to-the-river", Bm("over") + 60), ("3-splashdown", Bm("splash") - 40),
                ("4-elephants", Bm("elephants") - 50), ("4-show-boat", Bm("steamer") - 30), ("4-island", Bm("islandN")), ("4-seals", Bm("seals") - 40), ("4-dive-buoys", Bm("diveZone") - 30),
                ("5-dive", Bm("diveZone") + 40), ("5-escape-tank", Bm("tank") - 20), ("5-sunken-wagon", Bm("wagon") - 25), ("5-lake-bed", Bm("wagon") + 150),
                ("6-pool", Bm("G8") - 10), ("6-ramp", Bm("rampLip") + 3), ("6-finish", Bm("G9") + 80),
            };
            int next = 0; int resets0 = v.Resets, wall0 = v.WallHits, wag0 = WagonHits(); topSpeed.Clear();
            var seen = new Dictionary<string, (float best, float run, string what)>(); foreach (var s in c.segments) seen[s.name] = (0, 0, "");
            var railMin = new List<string>(); bool wasRail = false; float railLow = 999; var spikes = new List<string>();
            yield return Until(() => !v.Holding, 6f);
            measure = true;
            while (!v.Finished && Time.time - v.StartTime < 400f)
            {
                segNow = v.Splits.Count < segs ? c.segments[v.Splits.Count].name : "finish";
                while (next < marks.Length && v.Progress >= marks[next].p) { Shot(0, marks[next].name); next++; }
                SetPiece.Track(cam);
                if (seen.TryGetValue(segNow, out var sv))
                {
                    var big = SetPiece.All.Where(x => x.ShareNow >= DinoActor.BigShare).OrderByDescending(x => x.ShareNow).FirstOrDefault();
                    float run = big ? sv.run + Time.deltaTime : 0;
                    seen[segNow] = (Mathf.Max(sv.best, run), run, run > sv.best && big ? big.label : sv.what);
                }
                if (v.OnRail) railLow = Mathf.Min(railLow, v.Speed);
                if (v.Mode == Mode.Drive && v.Speed > 110 && spikes.Count < 6) spikes.Add($"{v.Speed:0} m/s at {v.Progress:0} m (on a rail {v.OnRail}, {v.Rail?.label}, wheels {v.GroundedWheels}, last: {v.LastEvent})");
                if (wasRail && !v.OnRail) { railMin.Add($"{v.Log.LastOrDefault(x => x.Contains("off the"))} (lowest {railLow:0.0} m/s)"); railLow = 999; }
                wasRail = v.OnRail;
                yield return null;
            }
            segNow = ""; measure = false;
            yield return Until(() => game.Trial.HasResults, 3f); yield return Wait(1.2f);
            Shot(0, "results"); yield return Wait(.2f);
            var gateLines = v.Log.FindAll(x => x.Contains("gate G") && x.Contains("passed")); bool ordered = true;
            for (int k = 0; k < gateLines.Count; k++) if (!gateLines[k].Contains($"G{k} ")) ordered = false;
            bool pass = v.Finished && v.Resets == resets0 && gateLines.Count == c.gates.Count && ordered;
            Line($"BT LAP {(pass ? "PASS" : "FAIL")}: finished {v.Finished} in {v.FinishTime:0.00} s (target ~2 min), gates {v.GatesPassed}/{c.gates.Count} in order {ordered}, resets {v.Resets - resets0}{(v.Resets > resets0 ? " (" + v.LastResetReason + ")" : "")}, wall hits {v.WallHits - wall0}, wagon hits {WagonHits() - wag0}, changes {Of("btlap").Count}: {string.Join(", ", Of("btlap").ConvertAll(x => x.what))}");
            Line("  segment               form              time   target   delta   longest set-piece moment (>= 8 % of the screen's height)");
            float prev = 0; int segSeen = 0;
            for (int k = 0; k < segs; k++)
            {
                var s = c.segments[k]; float t = k < v.Splits.Count ? v.Splits[k] - prev : -1; if (k < v.Splits.Count) prev = v.Splits[k];
                var sv = seen[s.name]; if (sv.best >= 1f) segSeen++;
                Line($"  {k + 1} {s.name,-19} {s.form,-16} {(t >= 0 ? $"{t,6:0.0} s" : "    --")}  {s.target,4:0} s  {(t >= 0 ? (t - s.target).ToString("+0.0;-0.0") : ""),6}   {sv.best:0.0} s{(sv.what != "" ? $" ({sv.what})" : "")}");
            }
            Line($"BT SET-PIECES {(segSeen >= 5 ? "PASS" : "CHECK")}: {segSeen} of {segs} segments had a set-piece >= 8 % of the screen for >= 1 s");
            Line($"  rails: {string.Join("; ", railMin)}");
            if (spikes.Count > 0) Line($"  car speed over 110 m/s: {string.Join("; ", spikes)}");
            Line($"  top speed per form: {string.Join(", ", new[] { Mode.Drive, Mode.Boat, Mode.Fly, Mode.Dive }.Select(m => $"{m} {(topSpeed.TryGetValue(m, out var x) ? x : 0):0.0} m/s"))}");
            foreach (var l in v.Log) if (l.Contains("missed") || l.Contains("rejected") || l.Contains("wrong way") || l.Contains("reset") || l.Contains("back through") || l.Contains("wall") || l.Contains("wagon")) Line("  log: " + l);
            RenderReport(c);
            Line($"  frames over 50 ms during the lap: {hitches.Count}");
            var tt = game.Trial;
            Line($"BT RESULTS {(tt.HasResults && tt.SegmentTimes.Count == segs && tt.Best != null ? "PASS" : "FAIL")}: lap {tt.Lap:0.00} s, {tt.SegmentTimes.Count} splits, best on file {(tt.Best != null ? tt.Best.lap.ToString("0.00") : "none")} s");
            Save();
        }

        IEnumerator BigTopGlobe()
        {
            var c = BT; game.StartCourse(2, false); yield return Wait(.3f); bool allOk = true;
            foreach (var r in BigTop.RailList)
                foreach (var (n, speed, coastIt) in new[] { ("full", 70f, false), ("slow", 30f, false), ("very slow", 8f, true) })
                {
                    string nm = r.label.Contains("Globe") ? "globe" : "cannon";
                    scenario = $"bt-{nm}-{n.Replace(' ', '-')}"; Clear(); game.StartCourse(2, false);
                    float placeAt = r.entryProgress - (nm == "globe" ? 50 : 22);
                    int ai = 0; for (int i = 0; i < c.anchors.Count; i++) if (c.anchors[i].progress < placeAt - 5) ai = i;
                    v.Begin(c, c.anchors[ai]); yield return Wait(.1f);
                    var at = c.PointAt(placeAt, out var d); var f = Vector3.ProjectOnPlane(d, Vector3.up).normalized;
                    v.DebugPlace(at, Quaternion.LookRotation(f), Mode.Drive, speed, placeAt);
                    holdSpeed = speed; smart = false; float entrySpeed = -1;
                    int r0 = v.Resets, rails0 = v.RailsRidden; float t0 = Time.time; bool on = false, shot = false; float minSpeed = 999;
                    while (Time.time - t0 < 30f && v.Resets == r0)
                    {
                        if (v.OnRail && entrySpeed < 0) { entrySpeed = v.Speed; if (coastIt) coast = true; }
                        if (v.OnRail) { on = true; minSpeed = Mathf.Min(minSpeed, v.Speed); if (!shot && v.RailS > r.Length * .5f) { shot = true; Capture("halfway"); } }
                        if (on && !v.OnRail) break;
                        yield return null;
                    }
                    if (nm == "cannon") { coast = false; holdSpeed = -1; yield return Wait(.6f); Capture("out-of-the-muzzle"); }
                    yield return Wait(2.0f);
                    bool through = on && v.RailsRidden > rails0 && v.Resets == r0; allOk &= through;
                    Line($"BT RAIL {r.label} {n} ({speed:0} m/s{(coastIt ? ", coasting on it" : "")}) {(through ? "PASS" : "FAIL")}: onto it at {entrySpeed:0.0} m/s, rode it {on}, off its end {v.RailsRidden > rails0}, lowest speed on it {minSpeed:0.0} m/s (booster minimum {r.MinSpeedAt(r.Length * .5f):0}), then {v.Mode} at {v.Progress:0} m ({v.Speed:0} m/s), resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}");
                    Clear();
                }
            Line($"BT RAILS {(allOk ? "PASS" : "FAIL")}: the Globe of Death and the cannon at full / slow / very slow speed");
            Save();
        }

        IEnumerator BigTopSplits()
        {
            var c = BT; int runs = ArgInt("-runs", 3);
            // the Clown Jump: the car line (through the Globe) vs the fly line (past it), from the anchor before the jump to G1 (the door)
            foreach (int choice in new[] { -1, 1 })
            {
                var times = new List<float>(); int stuck = 0, resets = 0, wag = 0;
                for (int k = 0; k < runs; k++)
                {
                    scenario = $"bt-clown-{(choice > 0 ? "fly" : "car")}-{k + 1}"; Clear(); game.StartCourse(2, false);
                    int ai = c.anchors.FindIndex(a => a.progress > Bm("wagons")); v.Begin(c, c.anchors[ai]);
                    var pilot = DriveWith(""); pilot.forceChoice = choice; int r0 = v.Resets, w0 = WagonHits(); float t0 = Time.time; bool shot = false;
                    while (Time.time - t0 < 50f && v.GatesPassed < 2 && v.Resets == r0) { if (!shot && v.Progress > Bm("gIn") + 20) { shot = true; if (k == 0) Capture("at-the-globe"); } yield return null; }
                    bool ok = v.GatesPassed >= 2 && v.Resets == r0; if (ok) times.Add(Time.time - t0); else if (v.Resets == r0) stuck++; resets += v.Resets - r0; wag += WagonHits() - w0;
                    if (k < 3 || !ok) Line($"  BT clown jump {(choice > 0 ? "fly (past the Globe)" : "car (through the Globe)")} run {k + 1}: {(ok ? $"{Time.time - t0:0.00} s to G1" : "did not reach G1")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, wagon hits {WagonHits() - w0}, wings used {v.WingsUsed}, shortcuts {string.Join("+", v.ShortcutsTaken)}");
                    StopAi();
                }
                Line($"BT CLOWN JUMP {(choice > 0 ? "fly" : "car")} {(times.Count == runs ? "PASS" : "FAIL")}: {times.Count} of {runs} clean, {stuck} stuck, {resets} resets, {wag} wagon hits, mean {(times.Count > 0 ? times.Average() : 0):0.00} s (anchor -> G1)");
            }
            // the river: the main channel vs the Island rapids, from the anchor after the splashdown to G7 (the sunken wagon)
            foreach (bool rapids in new[] { false, true })
            {
                var times = new List<float>(); int stuck = 0, resets = 0;
                for (int k = 0; k < runs; k++)
                {
                    scenario = $"bt-river-{(rapids ? "rapids" : "main")}-{k + 1}"; Clear(); game.StartCourse(2, false);
                    int ai = c.anchors.FindIndex(a => a.progress >= Bm("splash") + 60); v.Begin(c, c.anchors[ai]);
                    DriveWith(rapids ? "Island rapids" : ""); int r0 = v.Resets; float t0 = Time.time; bool shot = false;
                    while (Time.time - t0 < 70f && v.GatesPassed < 8 && v.Resets == r0) { if (!shot && v.Progress > Bm("islandN") + 20) { shot = true; if (k == 0) Capture("island"); } yield return null; }
                    bool ok = v.GatesPassed >= 8 && v.Resets == r0; if (ok) times.Add(Time.time - t0); else if (v.Resets == r0) stuck++; resets += v.Resets - r0;
                    if (k < 3 || !ok) Line($"  BT river {(rapids ? "rapids" : "main channel")} run {k + 1}: {(ok ? $"{Time.time - t0:0.00} s to G7" : "did not reach G7")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, wall hits {v.WallHits}, shortcuts {string.Join("+", v.ShortcutsTaken)}");
                    StopAi();
                }
                Line($"BT RIVER {(rapids ? "rapids" : "main channel")} {(times.Count == runs ? "PASS" : "FAIL")}: {times.Count} of {runs} clean, {stuck} stuck, {resets} resets, mean {(times.Count > 0 ? times.Average() : 0):0.00} s (anchor -> G7)");
            }
            Save();
        }
    }
}
