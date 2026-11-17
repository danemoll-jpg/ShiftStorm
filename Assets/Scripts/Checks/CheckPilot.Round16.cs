using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 16 checks.
    //   btflight   Big Top's two flights, player view: a picture every 0.5 s from the Clown Jump's lip to the big top's door (LB on the ramp)
    //              and from the cannon's muzzle to the splashdown; at every one: is the next target (the next hoop / ring, the exit, the
    //              splashdown arch) on the screen? ("-oldBtFlight": build 18's Clown flight and flight camera, the before numbers)
    //   humanclown the human-like pilot (Round 12: steers only by the HUD arrow, late, keys or a noisy stick, no boost) on the Clown flight
    //              (LB late on the ramp / in the air) and through the cannon's flight to the river (-tries N, default 10 each)
    //   bt16 = both
    //   damlap     Dam Shame, the check pilot's 3-lap time trial: lap and segment times, the water's stages, every form change and who
    //              decided it, set-pieces per lap (>= 8 % of the screen's height for >= 1 s), pictures of every lap's water state, frame time
    //   damrise    stragglers while the water rises (a rival put on the next lap so the water rises round the player's racer): a car in the
    //              valley and one in the turbine hall when the spillway opens, a boat in the hall and one on the drowned road when the valley
    //              floods, a car at the dam road's foot when it floods, a car a lap down through both rises; each: the forms the water gave
    //              it, no reset, on to its lap line
    //   damviews   pictures of every lap's water state from fixed viewpoints (the dam, the chute, the valley, the hall, the dam road)
    //   dam = damlap + damrise + damviews; round16 = bt16 + dam
    public sealed partial class CheckPilot
    {
        Course DM => game.Courses[3];
        static float Dm(string n) => DamShame.Marks[n];
        static float Dm(string n, int lap) => DamShame.M(n, lap);
        RecoveryAnchor AnchorBefore(Course c, float p) { RecoveryAnchor a = c.anchors[0]; foreach (var x in c.anchors) if (x.progress <= p + .5f) a = x; return a; }

        IEnumerator Round16(string which)
        {
            bool all = which == "round16", bt = all || which == "bt16", dm = all || which == "dam";
            if (bt || which == "btflight") yield return BtFlight();
            if (bt || which == "humanclown") yield return HumanClown();
            if (dm || which == "damlap") yield return DamLap();
            if (dm || which == "damrise") yield return DamRise();
            if (dm || which == "damviews") yield return DamViews();
        }

        // ---------------- Big Top's flights: is the next target on the screen? ----------------
        bool InView(Vector3 p) { var vp = cam.WorldToViewportPoint(p); return vp.z > 1f && vp.x > .04f && vp.x < .96f && vp.y > .04f && vp.y < .96f; }
        IEnumerator BtFlight()
        {
            bool old = BigTop.OldClown; var c = BT;
            // 1 the Clown flight: LB on the ramp (a steady human-like pilot), a picture every 0.5 s from the lip to G1 (the door)
            scenario = "bt-clown-flight"; Clear(); game.StartCourse(2, false); yield return Until(() => !v.Holding, 6f);
            var h = NewHuman(41); h.keys = false; h.noise = .04f; h.delay = .3f;
            float lip = Bm("jC"); PlaceBefore(lip - 140, 60, 0, 0); UseHuman(h);
            var sc = c.shortcuts.Find(x => x.label == "Clown flight");
            int r0 = v.Resets, n = 0, seen = 0; float next = -1, t0 = Time.time; bool pressed = false; var misses = new List<string>();
            while (Time.time - t0 < 30f && v.GatesPassed < 2 && v.Resets == r0)
            {
                if (!pressed && v.WingsOpen) { input.sWings = true; pressed = true; }
                if (v.Progress >= lip - 2 && next < 0) next = Time.time;
                if (next > 0 && Time.time >= next)
                {
                    next = Time.time + .5f; n++; var tg = v.GuideTarget; bool iv = InView(tg); if (iv) seen++; else misses.Add($"{n}: {(v.OnShortcut ? $"on {v.OnShortcut.label} at {v.Progress - lip:0} m" : $"main {v.Progress - lip:0} m past the lip")}, target {(tg - v.Body.position).magnitude:0} m away");
                    Capture($"{n:00}-{(iv ? "target-in-view" : "TARGET-OFF-SCREEN")}");
                }
                yield return null;
            }
            bool made = v.GatesPassed >= 2 && v.Resets == r0 && v.ShortcutsTaken.Contains("Clown flight");
            Line($"BT CLOWN FLIGHT VIEW{(old ? " (BUILD 18, -oldBtFlight)" : "")} {(made && seen == n ? "PASS" : "CHECK")}: flew it {v.ShortcutsTaken.Contains("Clown flight")}, reached the door {v.GatesPassed >= 2}, resets {v.Resets - r0}; the next target (hoop / exit / door) on the screen in {seen} of {n} pictures (every 0.5 s from the lip){(misses.Count > 0 ? "; off: " + string.Join("; ", misses) : "")}");
            if (sc != null) Line($"  the flight: line {sc.Length:0} m, hoops at {string.Join(", ", sc.rings.Select(r => $"{r:0} m"))} along it; Globe centre x {BigTop.GlobeCentre.x - BigTop.O.x:0.0} m");
            Clear();
            // 2 the cannonball flight: from the muzzle to the splashdown
            scenario = "bt-cannon-flight"; Clear(); game.StartCourse(2, false); yield return Until(() => !v.Holding, 6f);
            h = NewHuman(43); h.keys = false; h.noise = .04f; h.delay = .3f;
            PlaceBefore(Bm("ringW") - 10, 24, 0, 0); UseHuman(h);
            r0 = v.Resets; n = 0; seen = 0; next = -1; t0 = Time.time; misses.Clear();
            var rings = new List<Vector3>(); for (float p = Bm("flap") + 70; p < Bm("splash") - 60; p += old ? 75 : 70) rings.Add(c.PointAt(p, out _));
            var hoops = new[] { "hoop1", "hoop2", "hoop3", "flap" }.Select(x => c.PointAt(Bm(x), out _)).ToList();
            var arch = c.PointAt(Bm("splash") + 20, out _) + Vector3.up * 12;
            while (Time.time - t0 < 60f && v.GatesPassed < 6 && v.Resets == r0)
            {
                if (v.Progress >= Bm("muzzle") && next < 0) next = Time.time;
                if (next > 0 && Time.time >= next && v.Progress < Bm("splash") + 10)
                {
                    next = Time.time + .5f; n++;
                    var ahead = hoops.Concat(rings).Concat(new[] { arch }).FirstOrDefault(q => c.Project(q, v.Progress, out _, out _) > v.Progress + 12);
                    if (ahead == default) ahead = arch;
                    bool iv = InView(ahead); if (iv) seen++; else misses.Add($"{n}: at {v.Progress - Bm("muzzle"):0} m past the muzzle, target {(ahead - v.Body.position).magnitude:0} m away");
                    Capture($"{n:00}-{(iv ? "target-in-view" : "TARGET-OFF-SCREEN")}");
                }
                yield return null;
            }
            made = v.GatesPassed >= 6 && v.Resets == r0;
            Line($"BT CANNON FLIGHT VIEW{(old ? " (BUILD 18)" : "")} {(made && seen == n ? "PASS" : "CHECK")}: reached the river's G5 {v.GatesPassed >= 6}, resets {v.Resets - r0}; the next target (hoop, top flap, ring, splashdown arch) on the screen in {seen} of {n} pictures{(misses.Count > 0 ? "; off: " + string.Join("; ", misses) : "")}");
            Clear(); Save();
        }

        IEnumerator HumanClown()
        {
            int tries = ArgInt("-tries", 10); var c = BT; float lip = Bm("jC");
            Line($"HUMAN-LIKE PILOT (Big Top Breakdown, Clown flight{(BigTop.OldClown ? ", BUILD 18 (-oldBtFlight)" : "")}): steers by the HUD arrow's target only, 0.25-0.45 s late, keys or a noisy stick, no boost, LB 0.2-0.7 s into the window; entry 45 / 55 / 65 / 75 m/s, +-5 m, +-5 deg");
            int okF = 0; var tf = new List<float>();
            for (int k = 0; k < tries; k++)
            {
                scenario = "human-clown"; bool ok = false; float t = -1; string s = "";
                yield return HumanChoice(2, lip, true, 500 + k, (a, b, d) => { ok = a; t = b; s = d; });
                if (ok) { okF++; tf.Add(t); }
                Line($"  Clown flight try {k + 1} {(ok ? "MADE IT" : "MISSED")}: {s}");
                if (k == 0 || !ok) Capture($"clown-{k + 1}-end");
            }
            Line($"HUMAN-LIKE CLOWN FLIGHT {(okF == tries ? "PASS" : "FAIL")}: made it {okF} of {tries} (lip -> the door mean {(tf.Count > 0 ? tf.Average() : 0):0.00} s)");
            // the cannonball flight: from before the turn into the cannon, through the hoops and the top flap, over the camp, to the river's G5
            int okC = 0;
            for (int k = 0; k < tries; k++)
            {
                scenario = "human-cannon"; Clear(); game.StartCourse(2, false); yield return Until(() => !v.Holding, 6f);
                var h = NewHuman(700 + k); float speed = new[] { 18f, 22f, 26f, 30f }[k % 4], lane = h.R(-4, 4), head = h.R(-5, 5);
                PlaceBefore(Bm("ringW") - 10, speed, lane, head); UseHuman(h);
                int r0 = v.Resets; float t0 = Time.time; var resets = new List<string>(); int rc = v.Resets;
                while (Time.time - t0 < 60f && v.GatesPassed < 6) { if (v.Resets > rc) { rc = v.Resets; resets.Add($"{v.LastResetReason} at {v.Progress - Bm("muzzle"):+0;-0} m from the muzzle"); } yield return null; }
                bool ok = v.GatesPassed >= 6 && v.Resets == r0; if (ok) okC++;
                Line($"  cannon flight try {k + 1} {(ok ? "MADE IT" : "MISSED")}: {(h.keys ? "keys" : "stick")}, {speed:0} m/s, lane {lane:+0.0;-0.0} m, late {h.delay:0.00} s: {(v.GatesPassed >= 6 ? $"{Time.time - t0:0.0} s to the river's G5" : $"gates {v.GatesPassed}")}, penalty {v.Penalty:0} s, resets {v.Resets - r0}{(resets.Count > 0 ? ": " + string.Join("; ", resets) : "")}");
                if (k == 0 || !ok) Capture($"cannon-{k + 1}-end");
                Clear();
            }
            Line($"HUMAN-LIKE CANNON FLIGHT {(okC == tries ? "PASS" : "FAIL")}: made it {okC} of {tries}");
            Save();
        }

        // ---------------- Dam Shame ----------------
        static string Forms(ShiftVehicle r) => string.Join(" ", r.Log.Where(x => x.Contains("->") && x.Contains("(") && !x.Contains("done in")).Select(x => { int i = x.IndexOf("] "); var t = i >= 0 ? x.Substring(i + 2) : x; int j = t.IndexOf(" at z"); return j > 0 ? t.Substring(0, j) : t; }));
        IEnumerator DamLap()
        {
            scenario = "damlap"; Clear(); smart = true; game.StartCourse(3, false);
            var c = v.Course; int segs = c.segments.Count; var w = DamWater.Instance; int ev0 = w ? w.Events.Count : 0;
            Line($"DAM LAP: {c.courseName}, line {c.Length:0} m ({c.laps} laps of {DamShame.LapLen:0} m), {c.gates.Count} gates, {segs} segments, {c.anchors.Count} anchors, shortcuts {string.Join(", ", c.shortcuts.Select(s => $"{s.label} {s.Length:0} m"))}");
            Shot(.6f, "grid-countdown");
            var marks = new List<(string name, float p)>();
            for (int lap = 1; lap <= 3; lap++)
            {
                foreach (var (n, k, d) in new[] { ("crest", "tower", 20f), ("into-the-chute", "chuteTop", 25f), ("chute", "chuteMid", 0f), ("flip-bucket", "lip", -12f), ("over-the-pool", "lip", 35f), ("valley", "landing", 50f),
                                                   ("bend", "bend1", 20f), ("bridge", "bridge", -40f), ("turbine-hall", "hall", 0f), ("dam-road-foot", "climbFoot", 50f), ("dam-road", "climb", 20f), ("lap-line", "F", -60f) })
                {
                    float p = k == "F" ? Dm("F", lap + 1) + d : Dm(k, lap) + d; marks.Add(($"L{lap}-{n}", p));
                }
            }
            marks.Sort((a, b) => a.p.CompareTo(b.p));
            int next = 0, resets0 = v.Resets; topSpeed.Clear();
            var seen = new Dictionary<string, (float best, float run, string what)>(); foreach (var s in c.segments) seen[s.name] = (0, 0, "");
            yield return Until(() => !v.Holding, 6f);
            measure = true; int lastLap = 1; var stageAtLap = new List<string>();
            while (!v.Finished && Time.time - v.StartTime < 600f)
            {
                segNow = v.Splits.Count < segs ? c.segments[v.Splits.Count].name : "finish";
                while (next < marks.Count && v.Progress >= marks[next].p) { Shot(0, marks[next].name + $"-{v.Mode}"); next++; }
                SetPiece.Track(cam);
                if (seen.TryGetValue(segNow, out var sv))
                {
                    var big = SetPiece.All.Where(x => x.ShareNow >= DinoActor.BigShare).OrderByDescending(x => x.ShareNow).FirstOrDefault();
                    float run = big ? sv.run + Time.deltaTime : 0;
                    seen[segNow] = (Mathf.Max(sv.best, run), run, run > sv.best && big ? big.label : sv.what);
                }
                int lap = c.LapAt(v.Progress); if (lap != lastLap) { stageAtLap.Add($"lap {lap} began at water stage {DamWater.Stage} ({DamWater.Level:0.0} m)"); lastLap = lap; }
                yield return null;
            }
            segNow = ""; measure = false;
            yield return Until(() => game.Trial.HasResults, 3f); yield return Wait(1.2f);
            Shot(0, "results"); yield return Wait(.2f);
            var gateLines = v.Log.FindAll(x => x.Contains("gate G") && x.Contains("passed")); bool ordered = true;
            for (int k = 0; k < gateLines.Count; k++) if (!gateLines[k].Contains($"G{k} ")) ordered = false;
            bool pass = v.Finished && v.Resets == resets0 && gateLines.Count == c.gates.Count && ordered;
            Line($"DAM LAP {(pass ? "PASS" : "FAIL")}: finished {v.Finished} in {v.FinishTime:0.00} s, gates {v.GatesPassed}/{c.gates.Count} in order {ordered}, resets {v.Resets - resets0}{(v.Resets > resets0 ? " (" + v.LastResetReason + ")" : "")}, wall hits {v.WallHits}");
            Line($"  water: {string.Join("; ", stageAtLap)}");
            Line("  lap                      form          time   target   delta   longest set-piece moment (>= 8 % of the screen's height)");
            float prev = 0; int segSeen = 0;
            for (int k = 0; k < segs; k++)
            {
                var s = c.segments[k]; float t = k < v.Splits.Count ? v.Splits[k] - prev : -1; if (k < v.Splits.Count) prev = v.Splits[k];
                var sv = seen[s.name]; if (sv.best >= 1f) segSeen++;
                Line($"  {k + 1} {s.name,-22} {s.form,-10} {(t >= 0 ? $"{t,6:0.0} s" : "    --")}  {s.target,4:0} s  {(t >= 0 ? (t - s.target).ToString("+0.0;-0.0") : ""),6}   {sv.best:0.0} s{(sv.what != "" ? $" ({sv.what})" : "")}");
            }
            Line($"DAM SET-PIECES {(segSeen == segs ? "PASS" : "CHECK")}: {segSeen} of {segs} laps had a set-piece >= 8 % of the screen for >= 1 s");
            Line($"  form changes: {Forms(v)}");
            if (w) foreach (var e in w.Events.Skip(ev0)) Line("  water: " + e);
            Line($"  top speed per form: {string.Join(", ", new[] { Mode.Drive, Mode.Boat, Mode.Fly, Mode.Dive }.Select(m => $"{m} {(topSpeed.TryGetValue(m, out var x) ? x : 0):0.0} m/s"))}");
            foreach (var l in v.Log) if (l.Contains("missed") || l.Contains("rejected") || l.Contains("wrong way") || l.Contains("reset") || l.Contains("back through")) Line("  log: " + l);
            RenderReport(c);
            Line($"  frames over 50 ms during the race: {hitches.Count}");
            var tt = game.Trial;
            Line($"DAM RESULTS {(tt.HasResults && tt.SegmentTimes.Count == segs && tt.Best != null && tt.Best.single > 0 ? "PASS" : "FAIL")}: race {tt.Lap:0.00} s, laps {string.Join(" / ", tt.SegmentTimes.Select(x => x.ToString("0.00")))}, best on file {(tt.Best != null ? tt.Best.lap.ToString("0.00") : "none")} s, best single lap on file {(tt.Best != null ? tt.Best.single.ToString("0.00") : "none")} s; HUD: \"{(Hud.Instance ? Hud.Instance.LapText : "")}\"");
            Save();
        }

        // a straggler while the water rises: the player's racer (driven by the AI pilot) put at `at` (its form from the water there), the
        // water at `stage0` first, then a rival put on the next lap's start so the water rises round it; on to its lap line
        IEnumerator DamStraggler(string name, float at, int stage0, int stage1, Action<bool, string> done)
        {
            scenario = "damrise-" + name; Clear(); var c = DM; game.StartCourse(3, true); yield return Until(() => !v.Holding, 8f); yield return Wait(1.6f);
            var w = DamWater.Instance; int ev0 = w.Events.Count;
            var leaders = game.Rivals; int li = 0;
            void Lead(int lap) { var r = leaders[li++ % leaders.Count]; r.Begin(c, AnchorBefore(c, Dm("F", lap) + 45)); }
            if (stage0 >= 1) { Lead(2); yield return Wait(.3f); yield return Until(() => !DamWater.Rising, 14f); }
            v.Begin(c, AnchorBefore(c, at)); yield return Wait(.2f); var pilot = DriveWith(""); int r0 = v.Resets; int lap = c.LapAt(v.Progress); int target = DamShame.FirstGate(lap + 1);
            Shot(.4f, $"before-{v.Mode}");
            yield return Wait(1.5f);
            for (int s = stage0 + 1; s <= stage1; s++) { Lead(s + 1); yield return Wait(.3f); Shot(5f, $"rising-{s}"); yield return Until(() => !DamWater.Rising, 14f); Shot(0, $"risen-{s}-{v.Mode}"); }
            float t0 = Time.time;
            while (Time.time - t0 < 120f && v.GatesPassed < target && v.Resets == r0) yield return null;
            Shot(0, $"end-{v.Mode}"); yield return Wait(.1f);
            bool ok = v.GatesPassed >= target && v.Resets == r0;
            done(ok, $"lap {lap}, water {stage0} -> {stage1} (now {DamWater.Level:0.0} m): {(v.GatesPassed >= target ? $"reached its lap line ({c.gates[target - 1].label})" : $"did not reach its lap line (gates {v.GatesPassed}/{target}, {v.Mode} at {v.Progress - Dm("F", lap):0} m into lap {lap})")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}; forms: {Forms(v)}; water: {string.Join("; ", w.Events.Skip(ev0).Where(e => e.Contains(v.RacerName) || e.Contains("stage")))}");
            StopAi(); Clear();
        }
        IEnumerator DamRise()
        {
            int pass = 0, n = 0;
            foreach (var (name, at, s0, s1) in new[] {
                ("car-in-the-valley", Dm("landing") + 60, 0, 1), ("car-in-the-hall", Dm("hall") - 20, 0, 1), ("car-on-the-bridge-two-rises", Dm("bridge") - 70, 0, 2),
                ("boat-in-the-hall", Dm("hall", 2) - 20, 1, 2), ("boat-on-the-drowned-road", Dm("bridge", 2) + 20, 1, 2), ("car-at-the-dam-road-foot", Dm("climbFoot", 2) + 25, 1, 2),
                ("car-on-the-chute", Dm("chuteMid") - 40, 0, 1) })
            {
                bool ok = false; string s = ""; n++;
                yield return DamStraggler(name, at, s0, s1, (a, b) => { ok = a; s = b; });
                if (ok) pass++;
                Line($"  DAM STRAGGLER {name} {(ok ? "PASS" : "FAIL")}: {s}");
                Save();
            }
            Line($"DAM STRAGGLERS {(pass == n ? "PASS" : "FAIL")}: {pass} of {n} reached their lap line with no reset in the form the water gave them");
            Save();
        }

        // fixed viewpoints, every water stage (looks only: the water as a racer sees it)
        IEnumerator DamViews()
        {
            scenario = "damviews"; Clear(); coast = true; game.StartCourse(3, false); yield return Until(() => !v.Holding, 6f);
            var w = DamWater.Instance; var c = DM;
            var views = new (string n, Vector3 from, Vector3 to)[]
            {
                ("dam-from-the-valley", DamShame.O + new Vector3(60, 22, -330), DamShame.O + new Vector3(0, 25, -20)),
                ("chute-from-the-crest", DamShame.O + new Vector3(395, 52, -60), DamShame.O + new Vector3(405, 8, -420)),
                ("plunge-pool-and-valley", DamShame.O + new Vector3(430, 30, -430), DamShame.O + new Vector3(300, 0, -780)),
                ("bridge-and-beavers", DamShame.O + new Vector3(140, 14, -760), DamShame.O + new Vector3(0, 0, -880)),
                ("turbine-hall-door", DamShame.O + new Vector3(-395, 6, -680), DamShame.O + new Vector3(-405, 4, -560)),
                ("dam-road", DamShame.O + new Vector3(-330, 30, -520), DamShame.O + new Vector3(-405, 20, -260)),
            };
            for (int s = 0; s <= 2; s++)
            {
                // the water's stage set straight from the leader rule: a rival parked on the lap that earns it
                if (s > 0) { game.Rivals[0].gameObject.SetActive(true); game.Rivals[0].Begin(c, AnchorBefore(c, Dm("F", s + 1) + 45)); game.Rivals[0].HoldFor(9999f, false); }
                yield return Wait(.5f); yield return Until(() => !DamWater.Rising, 14f); yield return Wait(.5f);
                var cv = cam.GetComponent<CameraViews>(); var ch = cam.GetComponent<ChaseCam>(); bool cvOn = cv && cv.enabled, chOn = ch && ch.enabled; if (cv) cv.enabled = false; if (ch) ch.enabled = false;
                foreach (var vw in views) { cam.transform.SetPositionAndRotation(vw.from, Quaternion.LookRotation(vw.to - vw.from, Vector3.up)); yield return null; Capture($"stage{s}-{vw.n}"); }
                if (cv) cv.enabled = cvOn; if (ch) ch.enabled = chOn;
                Line($"DAM VIEWS stage {s}: water {DamWater.Level:0.0} m, {views.Length} pictures");
            }
            game.Rivals[0].gameObject.SetActive(false);
            Save();
        }
    }
}
