using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 7 checks: loop (full speed, slow, very slow, pictures at four points in every view, 25 AI runs), jumps (each plain jump:
    // airtime, landing speed, no reset; a slow jump resets cleanly), choice (car line and fly line timed, LB late and LB elsewhere do
    // nothing), stuntlaps (four AI racers from the grid to G2 through all of it, -races N, at -timeScale; Normal unless -difficulty).
    public sealed partial class CheckPilot
    {
        static Vector3 LineAt(Course c, float p, out Quaternion rot) { var q = c.PointAt(p, out var d); d.y = 0; rot = Quaternion.LookRotation(d.normalized, Vector3.up); return q; }
        // from the anchor before p (so the gates behind it count, as a racer who drove there), then put at p
        void PlaceAt(float p, float speed) { var c = v.Course; int a = 0; for (int i = 0; i < c.anchors.Count; i++) if (c.anchors[i].progress < p - 5) a = i; v.Begin(c, c.anchors[a]); var q = LineAt(c, p, out var r); v.DebugPlace(q, r, Mode.Drive, speed, p); }

        IEnumerator LoopRun(string name, float placeAt, float speed, bool coastIn, bool shots, Action<string, bool> done)
        {
            scenario = name; Clear(); BeginAt(0, 0); yield return Until(() => !v.Holding, 6f);
            if (placeAt < 0) { v.Begin(v.Course, v.Course.anchors[AnchorNear(Mk("loopIn") - ParadisePlunge.BoostPadBefore - 60)]); smart = true; }
            else { PlaceAt(placeAt, speed); if (coastIn) coast = true; else { holdSpeed = -1; smart = true; } }
            int loops0 = v.Loops, fails0 = v.LoopFails, r0 = v.Resets; float inSpeed = -1, topSpeed = -1, minTop = 999;
            var cv = cam.GetComponent<CameraViews>();
            float t0 = Time.time; int taken = 0; float[] at = { 1.5708f, 3.1416f, 4.7124f, 6.0f }; string[] nm = { "1-up", "2-top", "3-down", "4-out" };
            while (Time.time - t0 < 25f && v.Progress < Mk("loopOut") + 25 && v.Resets == r0)
            {
                if (v.OnLoop)
                {
                    if (inSpeed < 0) inSpeed = v.Speed;
                    if (Mathf.Abs(v.LoopAngle - Mathf.PI) < .3f) minTop = Mathf.Min(minTop, v.Speed);
                    if (shots) while (taken < at.Length && v.LoopAngle >= at[taken]) { Capture($"{cv.CurrentName.Replace(' ', '-').ToLower()}-{nm[taken]}"); taken++; }
                }
                yield return null;
            }
            if (v.Resets > r0) yield return Wait(.4f);
            bool through = v.Loops > loops0, failed = v.LoopFails > fails0, reset = v.Resets > r0;
            if (minTop < 999) topSpeed = minTop;
            string s = $"in at {(inSpeed >= 0 ? inSpeed.ToString("0") : "-")} m/s, over the top {(topSpeed >= 0 ? topSpeed.ToString("0") : "-")} m/s, {(through ? "through" : failed ? "too slow: slid back" : "did not get through")}, resets {v.Resets - r0}{(reset ? $" ({v.LastResetReason}), back at the anchor at {v.Progress:0} m in {v.Mode}" : "")}, wrong-way cues {v.Log.Count(l => l.Contains("wrong way"))}, gates {v.GatesPassed}";
            done(s, through);
            coast = false; Clear();
        }

        IEnumerator LoopCheck()
        {
            var c = game.Courses[0]; game.StartCourse(0, false);
            Line($"LOOP: radius {ParadisePlunge.LoopR:0} m (car centre), {ParadisePlunge.LoopShift:0} m side shift, line through it {Mk("loopOut") - Mk("loopIn"):0} m (progress {Mk("loopIn"):0} -> {Mk("loopOut"):0}), boost pad {ParadisePlunge.BoostPadBefore:0} m before it tops up to {ParadisePlunge.BoostPadSpeed:0} m/s");
            string full = "", slow = "", vslow = ""; bool okFull = false, okSlow = false, okV = false;
            yield return LoopRun("loop-full", -1, 0, false, true, (s, ok) => { full = s; okFull = ok; });
            yield return LoopRun("loop-slow", Mk("loopIn") - 25, 34, false, false, (s, ok) => { slow = s; okSlow = ok; });
            yield return LoopRun("loop-vslow", Mk("loopIn") - 12, 15, true, true, (s, ok) => { vslow = s; okV = !ok; });
            bool vReset = vslow.Contains("too slow for the loop");
            Line($"LOOP full speed (from the anchor, the pilot's line, no boost) {(okFull ? "PASS" : "FAIL")}: {full}");
            Line($"LOOP slow entry (placed after the pad at 34 m/s, throttle on, no boost) {(okSlow ? "PASS" : "FAIL")}: {slow}");
            Line($"LOOP very slow entry (placed at 15 m/s, coasting) {(okV && vReset ? "PASS" : "FAIL")}: {vslow}");
            // every camera view at the four points
            var cv = cam.GetComponent<CameraViews>(); var inside = new List<string>();
            for (int view = 1; view < CameraViews.Names.Length; view++)
            {
                while ((int)cv.Current != view) { input.sView = true; yield return null; yield return null; }
                string r = ""; bool ok = false; yield return LoopRun("loop-view", -1, 0, false, true, (s, k) => { r = s; ok = k; });
                inside.Add($"{CameraViews.Names[view]} {(ok ? "through" : "FAILED")}");
            }
            while (cv.Current != CameraViews.View.Chase) { input.sView = true; yield return null; yield return null; }
            Line($"LOOP views: pictures at 90 / 180 / 270 / 345 deg in Chase, Far chase, First person, Front ({string.Join(", ", inside)})");
            // 25 AI runs
            int runs = ArgInt("-loopRuns", 25), ok25 = 0, stuck = 0; var bad = new List<string>(); float minIn = 999;
            for (int k = 0; k < runs; k++)
            {
                scenario = "loop-ai"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                v.Begin(v.Course, v.Course.anchors[AnchorNear(Mk("loopIn") - ParadisePlunge.BoostPadBefore - 60)]);
                var pilot = DriveWith(""); pilot.useDifficulty = true; int r0 = v.Resets, l0 = v.Loops; float t0 = Time.time;
                yield return Until(() => v.Progress > Mk("loopOut") + 30 || v.Resets > r0, 30f);
                var entry = v.Log.LastOrDefault(l => l.Contains("into the loop")); if (entry != null) { var m = System.Text.RegularExpressions.Regex.Match(entry, @"at (\d+) m/s"); if (m.Success) minIn = Mathf.Min(minIn, float.Parse(m.Groups[1].Value)); }
                if (v.Loops > l0 && v.Resets == r0) ok25++; else { stuck++; bad.Add($"run {k + 1}: {(v.Resets > r0 ? v.LastResetReason : "no loop in 30 s")}"); }
                StopAi();
            }
            Line($"LOOP AI ({Settings.Difficulty}) {(ok25 == runs ? "PASS" : "FAIL")}: {ok25}/{runs} runs through the loop, {stuck} stuck / reset{(bad.Count > 0 ? ": " + string.Join("; ", bad) : "")}; slowest entry {minIn:0} m/s");
            Save();
        }

        IEnumerator JumpsCheck()
        {
            var c = game.Courses[0]; game.StartCourse(0, false);
            foreach (var j in ParadisePlunge.Jumps)
            {
                Line($"JUMP {j.name}: lip at {j.lip:0} m, kicker {j.kicker:0} m rising {j.height:0.0} m (about {Mathf.Atan(2 * j.height / j.kicker) * Mathf.Rad2Deg:0} deg at the lip), gap {j.gap:0} m over {(j.water ? "a creek" : "a ravine")}{(j.choice ? ", the choice jump (LB / E: wings)" : ", no wings")}");
                if (j.choice) continue;
                foreach (bool boosted in new[] { false, true })
                {
                    scenario = $"jump-{j.name.Split(' ')[0].ToLower()}{(boosted ? "-boost" : "")}"; Clear(); BeginAt(0, 0); yield return Until(() => !v.Holding, 6f);
                    v.Begin(v.Course, v.Course.anchors[AnchorNear(j.lip - 150)]); smart = true; boost = boosted;
                    int r0 = v.Resets, j0 = v.Jumps; float before = -1, t0 = Time.time; bool shotAir = false, shotKick = false;
                    while (Time.time - t0 < 30f && v.Progress < j.lip + j.gap + 300 && v.Resets == r0)
                    {
                        if (!shotKick && v.Progress > j.lip - 25) { shotKick = true; Capture("kicker"); before = v.Speed; }
                        if (!shotAir && v.Progress > j.lip + 35 && v.GroundedWheels == 0) { shotAir = true; Capture("airborne"); }
                        yield return null;
                    }
                    yield return Wait(.4f);
                    bool pass = v.Jumps > j0 && v.Resets == r0 && v.LastLandingOut >= v.LastLandingIn - 3f;
                    Line($"  JUMP {j.name}{(boosted ? " (boost)" : "")} {(pass ? "PASS" : "FAIL")}: {before:0} m/s up the kicker, {v.LastAirTime:0.00} s in the air, {v.LastLandingIn:0} m/s at the touch -> {v.LastLandingOut:0} m/s after it, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}");
                    boost = false;
                }
                // short / slow: placed 50 m before the lip at 28 m/s, coasting
                scenario = $"jump-{j.name.Split(' ')[0].ToLower()}-slow"; Clear(); BeginAt(0, 0); yield return Until(() => !v.Holding, 6f);
                PlaceAt(j.lip - 50, 28); coast = true; int rr = v.Resets; float ts = Time.time;
                yield return Until(() => v.Resets > rr || v.Progress > j.lip + j.gap + 60, 12f);
                bool clean = v.Resets > rr ? Time.time - ts < 6f : v.Progress > j.lip + j.gap;
                yield return Wait(.5f); Capture("after");
                Line($"  JUMP {j.name} short / slow (28 m/s, coasting) {(clean ? "PASS" : "FAIL")}: {(v.Resets > rr ? $"reset after {Time.time - ts - .5f:0.0} s ({v.LastResetReason}), back at {v.Progress:0} m in {v.Mode} at {v.Speed:0} m/s" : v.Progress > j.lip + j.gap ? "made it across" : "neither across nor reset in 12 s")}");
                coast = false;
            }
            Save();
        }

        IEnumerator ChoiceLeg(string name, int choice, Action<float, string> done)
        {
            scenario = name; Clear(); BeginAt(0, 0); yield return Until(() => !v.Holding, 6f);
            float lip = Mk("jC"); v.Begin(v.Course, v.Course.anchors[AnchorNear(lip - 150)]);
            var pilot = DriveWith(""); pilot.forceChoice = choice; int r0 = v.Resets, g1 = 2; float tLip = -1, t0 = Time.time; bool air = false, wheels = false, ringShot = false;
            while (Time.time - t0 < 40f && v.GatesPassed < g1 && v.Resets == r0)
            {
                if (tLip < 0 && v.Progress >= lip) tLip = Time.time;
                if (!air && tLip > 0 && Time.time - tLip > .5f) { air = true; Capture(v.Mode == Mode.Fly ? "wings-out" : "car-airborne"); }
                if (!ringShot && v.OnShortcut && v.Progress > lip + 140) { ringShot = true; Capture("rings"); }
                if (!wheels && v.Log.Count > 0 && v.LastEvent.Contains("Fly->Drive")) { wheels = true; Shot(.3f, "wheels-down"); }
                yield return null;
            }
            float t = v.GatesPassed >= g1 && tLip > 0 ? Time.time - tLip : -1;
            done(t, $"{(t >= 0 ? $"{t:0.00} s from the lip to G1" : "did not reach G1")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, wings {v.WingsUsed}, changes {string.Join(" ", v.Log.Where(l => l.Contains("->") && l.Contains(" at z ")).Select(l => l.Substring(l.IndexOf(']') + 2).Split(' ')[0]))}, shortcut {(v.ShortcutsTaken.Count > 0 ? string.Join(", ", v.ShortcutsTaken) : "none")}, penalty {v.Penalty:0} s, gates {string.Join(" ", v.GateLog)}");
            StopAi();
        }

        IEnumerator ChoiceCheck()
        {
            game.StartCourse(0, false); var sc = game.Courses[0].shortcuts.Find(x => x.choice);
            Line($"CHOICE (Lake Jump): car line {sc.mainTo - sc.mainFrom:0} m of road from the lip to the rejoin; fly line {sc.Length:0} m (rings, entry / exit sub-gates, wheels-down zone before G1)");
            float tc = 0, tf = 0; string sc1 = "", sf = "";
            yield return ChoiceLeg("choice-car", -1, (t, s) => { tc = t; sc1 = s; });
            yield return ChoiceLeg("choice-fly", 1, (t, s) => { tf = t; sf = s; });
            Line($"  car line {(tc > 0 && !sc1.Contains("resets 1") ? "PASS" : "FAIL")}: {sc1}");
            Line($"  fly line {(tf > 0 && sf.Contains("Lake flight") && sf.Contains("resets 0") ? "PASS" : "FAIL")}: {sf}");
            Line($"  CHOICE balance: fly {(tf > 0 && tc > 0 ? $"{tc - tf:+0.00;-0.00} s faster than the car line ({(tc > 0 ? 100 * (tc - tf) / tc : 0):0} %)" : "-")} (aim: about equal, the fly line slightly faster)");
            // LB late: the car line, LB pressed 0.4 s after the window closed
            scenario = "choice-late"; Clear(); BeginAt(0, 0); yield return Until(() => !v.Holding, 6f);
            float lip = Mk("jC"); v.Begin(v.Course, v.Course.anchors[AnchorNear(lip - 150)]); smart = true;
            yield return Until(() => v.WingsOpen, 15f); yield return Until(() => !v.WingsOpen, 5f); yield return Wait(.4f);
            int ig = v.WingsIgnored; input.sWings = true; yield return Wait(.3f);
            bool lateOk = v.Mode == Mode.Drive && v.WingsIgnored > ig;
            Line($"  LB late {(lateOk ? "PASS" : "FAIL")}: pressed 0.4 s after the 1 s window -> still {v.Mode}, logged \"{v.Log.LastOrDefault(l => l.Contains("wings pressed"))?.Substring(10)}\"");
            yield return Until(() => v.GatesPassed >= 2 || v.Resets > 0, 20f);
            // LB elsewhere: on the start straight, and in the air off the Creek Jump
            scenario = "choice-elsewhere"; Clear(); BeginAt(0, 0); yield return Until(() => !v.Holding, 6f); smart = true; yield return Wait(1.5f);
            int ig2 = v.WingsIgnored, wu2 = v.WingsUsed; input.sWings = true; yield return Wait(.2f);
            yield return Until(() => v.Progress > Mk("jA") + 5, 15f); yield return Wait(.3f); input.sWings = true; yield return Wait(.3f);
            bool elseOk = v.Mode == Mode.Drive && v.WingsIgnored == ig2 + 2 && v.WingsUsed == wu2;
            Line($"  LB elsewhere {(elseOk ? "PASS" : "FAIL")}: pressed on the start straight and in the air off the Creek Jump -> {v.Mode}, {v.WingsIgnored - ig2} presses ignored, wings used {v.WingsUsed - wu2}");
            Clear(); Save();
        }

        // four AI racers from the grid to G2 (all three jumps and the loop), several races
        IEnumerator StuntLaps()
        {
            int races = ArgInt("-races", 7); float scale = ArgFloat("-timeScale", 3f);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-difficulty") < 0) Settings.Difficulty = Difficulty.Normal;
            Line($"STUNT RUNS: {races} races x 4 AI racers (Abi's car by an AI too), the grid to G2 (Creek Jump, the loop, the Lake Jump, the Cliff Gap), {Settings.Difficulty}, time scale {scale:0.0}");
            var abi = new Rival { name = "Abi (AI)", lane = 0, brakeMargin = .9f, boostMeter = .6f, boostStraight = 9f, seed = 51 };
            int runs = 0, clean = 0, loops = 0, fails = 0, flew = 0, stayed = 0, jumps = 0; var why = new List<string>(); var times = new List<float>();
            for (int r = 0; r < races; r++)
            {
                scenario = $"stunt{r + 1}"; Clear(); aiDrives = true; game.StartCourse(0, true);
                var pilot = v.GetComponent<RivalPilot>() ?? v.gameObject.AddComponent<RivalPilot>(); pilot.rival = abi; pilot.AlwaysOn = true;
                Time.timeScale = scale; yield return Until(() => !v.Holding, 10f); float go = Time.time;
                var racers = game.Race.Racers.ToList(); var loops0 = racers.ToDictionary(x => x, x => x.Loops); var fails0 = racers.ToDictionary(x => x, x => x.LoopFails); var jumps0 = racers.ToDictionary(x => x, x => x.Jumps);
                while (Time.time - go < 120f && !racers.All(x => x.GatesPassed >= 3)) yield return null;
                Time.timeScale = 1;
                foreach (var x in racers)
                {
                    int xl = x.Loops - loops0[x]; runs++; loops += xl; fails += x.LoopFails - fails0[x]; jumps += x.Jumps - jumps0[x];
                    if (x.ShortcutsTaken.Contains("Lake flight")) flew++; else stayed++;
                    bool ok = x.GatesPassed >= 3 && x.Resets == 0 && xl == 1; if (ok) { clean++; } else why.Add($"race {r + 1} {x.RacerName}: gates {x.GatesPassed}, loops {xl}, resets {x.Resets}{(x.Resets > 0 ? $" ({x.LastResetReason})" : "")}");
                    if (x.GatesPassed >= 3) times.Add(x.Splits.Count >= 2 ? x.Splits[1] : -1);
                }
                // (the driver stays on Abi's car between races: destroying it and fetching it again in the same frame left her car
                // driverless with its last input held: the first runs' "out of bounds" resets)
            }
            StopAi(); aiDrives = false; Time.timeScale = 1;
            Line($"STUNT RUNS {(why.Count == 0 ? "PASS" : "FAIL")}: {clean}/{runs} clean (G2 reached, one loop, no reset); loops {loops}, loop fails {fails}, landings logged {jumps}; Lake Jump: {flew} flew, {stayed} stayed a car");
            foreach (var w in why) Line("  " + w);
            Save();
        }
    }
}
