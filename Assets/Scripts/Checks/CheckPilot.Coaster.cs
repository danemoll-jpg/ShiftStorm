using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 10 item 4: Coaster Coast's checks (course 1).
    //   cclap     the check pilot's safe-line lap from the grid: segment times, a picture in every segment (player view), the rails' lowest
    //             speeds, set-piece visibility per segment (>= 8 % of the screen's height for >= 1 s, SetPiece.ScreenShare), frame time
    //   ccrails   each rail entered at full, slow and very slow (coasting) speed: through it, off its end, no reset, no fall-through
    //   ccsplits  each split timed both ways: the Midway Jump (car / fly through the wheel) and the Lighthouse (round it / the rocks)
    //   coaster   = cclap + ccrails + ccsplits. The soak and the split-screen lap take "-course 1" (soak, split).
    public sealed partial class CheckPilot
    {
        static float Cm(string n) => CoasterCoast.Marks[n];
        Course CC => game.Courses[1];

        IEnumerator CoasterLap()
        {
            scenario = "cclap"; Clear(); smart = true; game.StartCourse(1, false);
            var c = v.Course; int segs = c.segments.Count;
            Line($"CC LAP: {c.courseName}, line {c.Length:0} m, {c.gates.Count} gates, {segs} segments, {c.anchors.Count} anchors, rails {string.Join(", ", CoasterCoast.RailList.Select(r => $"{r.label} {r.Length:0} m"))}");
            Shot(.6f, "grid-countdown");
            var marks = new (string name, float p)[]
            {
                ("1-boardwalk", Cm("promMid")), ("1-midway-jump", Cm("jM") - 30), ("1-under-the-wheel", Cm("wheel") - 70), ("1-station", Cm("railA") - 40),
                ("2-airtime-hill", Cm("aLaunch") + 30), ("2-banked-turn", Cm("aTurn") - 110), ("2-loop", Cm("aLoopIn") + 40), ("2-coaster-gap", Cm("lipA") - 5),
                ("3-corkscrew", Cm("corkIn") + 50), ("3-lift-hill", Cm("liftFoot") + 120), ("3-top-hat", Cm("crest") - 2),
                ("4-flight", Cm("crest") + 120), ("4-helter-skelter", Cm("helter") - 60), ("4-fireworks", Cm("fw2") - 60), ("4-balloon", Cm("fw3") - 40), ("4-splashdown", Cm("splash") - 60),
                ("5-flume", Cm("fl2")), ("5-under-the-lift", Cm("underLift") - 40), ("5-under-the-boardwalk", Cm("cross") - 50),
                ("6-sea", Cm("sea1") - 40), ("6-lighthouse", Cm("lightW") - 40), ("7-dive", Cm("portal") + 10), ("7-carousel", Cm("carousel") - 20), ("7-octopus", Cm("octo") - 15),
                ("8-pool", Cm("G8") - 10), ("8-ramp", Cm("rampLip") + 3), ("8-finish", Cm("G9") + 60),
            };
            int next = 0; int resets0 = v.Resets; topSpeed.Clear(); float t0 = Time.time;
            var seen = new Dictionary<string, (float best, float run, string what)>(); foreach (var s in c.segments) seen[s.name] = (0, 0, "");
            var railMin = new List<string>(); bool wasRail = false;
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
                if (wasRail && !v.OnRail) railMin.Add($"{v.Log.LastOrDefault(x => x.Contains("off the"))}");
                wasRail = v.OnRail;
                yield return null;
            }
            segNow = ""; measure = false;
            yield return Until(() => game.Trial.HasResults, 3f); yield return Wait(1.2f);
            Shot(0, "results"); yield return Wait(.2f);
            var gateLines = v.Log.FindAll(x => x.Contains("gate G") && x.Contains("passed")); bool ordered = true;
            for (int k = 0; k < gateLines.Count; k++) if (!gateLines[k].Contains($"G{k} ")) ordered = false;
            bool pass = v.Finished && v.Resets == resets0 && gateLines.Count == c.gates.Count && ordered;
            Line($"CC LAP {(pass ? "PASS" : "FAIL")}: finished {v.Finished} in {v.FinishTime:0.00} s (target ~120), gates {v.GatesPassed}/{c.gates.Count} in order {ordered}, resets {v.Resets - resets0}{(v.Resets > resets0 ? " (" + v.LastResetReason + ")" : "")}, changes {Of("cclap").Count}: {string.Join(", ", Of("cclap").ConvertAll(x => x.what))}");
            Line("  segment               form              time   target   delta   longest set-piece moment (>= 8 % of the screen's height)");
            float prev = 0; int segSeen = 0;
            for (int k = 0; k < segs; k++)
            {
                var s = c.segments[k]; float t = k < v.Splits.Count ? v.Splits[k] - prev : -1; if (k < v.Splits.Count) prev = v.Splits[k];
                var sv = seen[s.name]; if (sv.best >= 1f) segSeen++;
                Line($"  {k + 1} {s.name,-19} {s.form,-16} {(t >= 0 ? $"{t,6:0.0} s" : "    --")}  {s.target,4:0} s  {(t >= 0 ? (t - s.target).ToString("+0.0;-0.0") : ""),6}   {sv.best:0.0} s{(sv.what != "" ? $" ({sv.what})" : "")}");
            }
            Line($"CC SET-PIECES {(segSeen >= 6 ? "PASS" : "CHECK")}: {segSeen} of {segs} segments had a set-piece >= 8 % of the screen for >= 1 s");
            Line($"  rails: {string.Join("; ", railMin)}");
            Line($"  top speed per form: {string.Join(", ", new[] { Mode.Drive, Mode.Boat, Mode.Fly, Mode.Dive }.Select(m => $"{m} {(topSpeed.TryGetValue(m, out var x) ? x : 0):0.0} m/s"))}");
            foreach (var l in v.Log) if (l.Contains("missed") || l.Contains("rejected") || l.Contains("wrong way") || l.Contains("reset") || l.Contains("back through") || l.Contains("rail")) Line("  log: " + l);
            RenderReport(c);
            Line($"  frames over 50 ms during the lap: {hitches.Count}");
            var tt = game.Trial;
            Line($"CC RESULTS {(tt.HasResults && tt.SegmentTimes.Count == segs && tt.Best != null ? "PASS" : "FAIL")}: lap {tt.Lap:0.00} s, {tt.SegmentTimes.Count} splits, best on file {(tt.Best != null ? tt.Best.lap.ToString("0.00") : "none")} s (per course: Paradise Plunge {(BestTimes.Get("Paradise Plunge")?.lap ?? -1):0.00})");
            input.RequestReset(); yield return Wait(.3f);
            Line($"CC RESTART {(v.Holding && !v.Finished && v.GatesPassed == 0 ? "PASS" : "FAIL")}: after the finish, Reset started Coaster Coast again (course {game.CourseIndex})");
            yield return Until(() => !v.Holding, 5f);
        }

        // Round 10: diagnostics for the soak's resets after the Midway flight (a car's velocity turned 90 deg near 810 m): drive through
        // 740-840 m at 75 m/s on five lines, log any frame whose heading jumps, with the colliders round the car
        IEnumerator CoasterProbe()
        {
            var c = CC; game.StartCourse(1, false); yield return Wait(.3f);
            foreach (float lat in new[] { -8f, -4f, 0f, 4f, 8f })
                foreach (float y in new[] { 0f, 6f })
                {
                    scenario = $"probe{lat:0}-{y:0}"; Clear(); game.StartCourse(1, false);
                    float placeAt = c.marks["G1"] - 30; int ai = 0; for (int i = 0; i < c.anchors.Count; i++) if (c.anchors[i].progress < placeAt) ai = i;
                    v.Begin(c, c.anchors[ai]); yield return Wait(.1f); var at = c.PointAt(placeAt, out var d); var f = Vector3.ProjectOnPlane(d, Vector3.up).normalized;
                    at += Vector3.Cross(Vector3.up, f) * lat + Vector3.up * (y + .5f);
                    v.DebugPlace(at, Quaternion.LookRotation(f), Mode.Drive, 75f, placeAt); holdSpeed = 75f;
                    var prevV = v.Body.linearVelocity; float t0 = Time.time; int jumps = 0;
                    while (Time.time - t0 < 3f && v.Progress < c.marks["G1"] + 110 && !v.OnRail)
                    {
                        yield return new WaitForFixedUpdate();
                        var nv = v.Body.linearVelocity; var a = new Vector3(prevV.x, 0, prevV.z); var b = new Vector3(nv.x, 0, nv.z);
                        if (a.magnitude > 10 && b.magnitude > 10 && Vector3.Angle(a, b) > 8f && jumps < 6)
                        {
                            jumps++; var near = new System.Text.StringBuilder();
                            foreach (var col in Physics.OverlapSphere(v.Body.position, 6f, ~0, QueryTriggerInteraction.Collide)) if (!col.transform.IsChildOf(v.transform)) near.Append($"{col.name}[{LayerMask.LayerToName(col.gameObject.layer)}{(col.isTrigger ? ",trig" : "")}] ");
                            Line($"  PROBE lat {lat} y {y}: heading jump {Vector3.Angle(a, b):0} deg at {v.Progress:0} m, pos {v.Body.position - c.PointAt(v.Progress, out _)}, vel {prevV} -> {nv}, wheels {v.GroundedWheels}; near: {near}");
                        }
                        prevV = nv;
                    }
                    Line($"CC PROBE lat {lat} y {y}: {jumps} heading jumps, reached {v.Progress:0} m, {v.OffLine:0} m off, on rail {v.OnRail}, resets {v.Resets} {v.LastResetReason}");
                    Clear();
                }
        }
        IEnumerator CoasterRails()
        {
            var c = CC; game.StartCourse(1, false); yield return Wait(.3f);
            foreach (var r in CoasterCoast.RailList)
                foreach (var (n, speed, coastIt) in new[] { ("full", 70f, false), ("slow", 30f, false), ("very slow", 8f, true) })
                {
                    scenario = $"rail-{(r.label.Contains("A") ? "A" : "B")}-{n.Replace(' ', '-')}"; Clear(); game.StartCourse(1, false);
                    int ai = 0; for (int i = 0; i < c.anchors.Count; i++) if (c.anchors[i].progress < r.entryProgress - 40) ai = i;
                    v.Begin(c, c.anchors[ai]); yield return Wait(.1f);
                    // rail A: just after the launch pad (the pad itself always launches to 72 m/s); rail B: at the landing deck's anchor (before G2)
                    float placeAt = r.label.Contains("rail A") ? r.entryProgress - 45 : c.anchors[ai].progress;
                    var at = c.PointAt(placeAt, out var d); var f = Vector3.ProjectOnPlane(d, Vector3.up).normalized;
                    v.DebugPlace(at, Quaternion.LookRotation(f), Mode.Drive, speed, placeAt);
                    holdSpeed = speed; float entrySpeed = -1;
                    int r0 = v.Resets, rails0 = v.RailsRidden; float t0 = Time.time; bool on = false, shot = false; float minSpeed = 999, minY = 999; var exitMode = Mode.Drive;
                    while (Time.time - t0 < 40f && v.Resets == r0)
                    {
                        if (v.OnRail && entrySpeed < 0) { entrySpeed = v.Speed; if (coastIt) coast = true; }   // very slow: coasting once on the rail
                        if (v.OnRail) { on = true; minSpeed = Mathf.Min(minSpeed, v.Speed); if (!shot && v.RailS > r.Length * .5f) { shot = true; Capture("halfway"); } }
                        if (on && !v.OnRail) { exitMode = v.Mode; break; }
                        minY = Mathf.Min(minY, v.Body.position.y);
                        yield return null;
                    }
                    yield return Wait(2.5f);
                    bool through = on && v.RailsRidden > rails0 && v.Resets == r0;
                    Line($"CC RAIL {r.label} {n} ({speed:0} m/s{(coastIt ? ", coasting on the rail" : "")}) {(through ? "PASS" : "FAIL")}: onto it at {entrySpeed:0.0} m/s, rode it {on}, off its end {v.RailsRidden > rails0}, lowest speed on it {minSpeed:0.0} m/s (booster minimum {r.MinSpeedAt(r.Length * .5f):0}), {(r.label.Contains("A") ? "after the Coaster Gap" : "after the top hat")}: {v.Mode} at {v.Progress:0} m, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}");
                    Clear();
                }
        }

        IEnumerator CoasterSplits()
        {
            var c = CC;
            // the Midway Jump: car line vs fly line, from the anchor before the jump to G1 (3 runs each, the AI pilot on the player's car)
            foreach (int choice in new[] { -1, 1 })
            {
                var times = new List<float>();
                for (int k = 0; k < 3; k++)
                {
                    scenario = $"cc-choice-{(choice > 0 ? "fly" : "car")}-{k + 1}"; Clear(); game.StartCourse(1, false);
                    int ai = c.anchors.FindIndex(a => a.progress > Cm("jM") - 200); v.Begin(c, c.anchors[ai]);
                    var pilot = DriveWith(""); pilot.forceChoice = choice; int r0 = v.Resets; float t0 = Time.time; bool shot = false;
                    while (Time.time - t0 < 40f && v.GatesPassed < 2 && v.Resets == r0) { if (!shot && v.Progress > Cm("wheel") - 20) { shot = true; Capture("at-the-wheel"); } yield return null; }
                    if (v.GatesPassed >= 2 && v.Resets == r0) times.Add(Time.time - t0);
                    Line($"  CC choice {(choice > 0 ? "fly (through the wheel)" : "car (under it)")} run {k + 1}: {(v.GatesPassed >= 2 ? $"{Time.time - t0:0.00} s to G1" : "did not reach G1")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, wings used {v.WingsUsed}, shortcuts {string.Join("+", v.ShortcutsTaken)}");
                    StopAi();
                }
                Line($"CC CHOICE {(choice > 0 ? "fly" : "car")}: {times.Count} of 3 clean, mean {(times.Count > 0 ? times.Average() : 0):0.00} s anchor -> G1");
            }
            // the Lighthouse: round it (main line) vs the rocks (shortcut), from the anchor at the coast to G7
            foreach (bool rocks in new[] { false, true })
            {
                var times = new List<float>();
                for (int k = 0; k < 3; k++)
                {
                    scenario = $"cc-lighthouse-{(rocks ? "rocks" : "round")}-{k + 1}"; Clear(); game.StartCourse(1, false);
                    int ai = c.anchors.FindIndex(a => a.progress >= Cm("cross") + 20); v.Begin(c, c.anchors[ai]);
                    DriveWith(rocks ? "Lighthouse rocks" : ""); int r0 = v.Resets, g0 = v.GatesPassed; float t0 = Time.time; bool shot = false;
                    while (Time.time - t0 < 60f && v.GatesPassed < 8 && v.Resets == r0) { if (!shot && v.Progress > Cm("lightW") - 30) { shot = true; Capture("lighthouse"); } yield return null; }
                    if (v.GatesPassed >= 8 && v.Resets == r0) times.Add(Time.time - t0);
                    Line($"  CC lighthouse {(rocks ? "rocks" : "round")} run {k + 1}: {(v.GatesPassed >= 8 ? $"{Time.time - t0:0.00} s to G7" : "did not reach G7")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, wall hits {v.WallHits}, shortcuts {string.Join("+", v.ShortcutsTaken)}");
                    StopAi();
                }
                Line($"CC LIGHTHOUSE {(rocks ? "rocks" : "round")}: {times.Count} of 3 clean, mean {(times.Count > 0 ? times.Average() : 0):0.00} s");
            }
        }
    }
}
