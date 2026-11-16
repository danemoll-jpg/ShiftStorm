using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftStorm
{
    // Round 12 checks (Dan on build 13: Coaster Coast locked behind the Tour; "not enough space to direct your plane into the end of that
    // sequence"; the flying shortcut "resetting me back too far ... repeat the same sequence over and over").
    //   unlock    fresh settings.json / tour.json: Coaster Coast in Race, Time Trial and 2 Players straight away; the Tour's stops open while
    //             "Unlock all Tour stops" is On (the default), in order when Off
    //   humanfly  a human-like pilot (steers by the HUD arrow's target only, 0.25-0.45 s late, keyboard taps or a noisy stick, LB late in the
    //             window, no boost, varied entry speeds / angles / lanes) on the Lake Jump's fly line (-tries N, default 10) and its car line
    //   branches  scripted failures on every flight branch (Lake flight, Sky Arch slit, Coaster Coast's Midway flight): no steering, hard left /
    //             right, too high, too low, the exit missed, the slit missed, the reset button; each must end ahead or at most one anchor
    //             back, never behind the jump already made, no loop, and the lap goes on
    //   resetaudit every 20 m of both laps: how far back a reset there would put you (anchors and the flight-line rule)
    //   round12 = all four; "-oldFlight" builds and runs build 13's flight branches (before numbers).
    public sealed partial class CheckPilot
    {
        // ---------------- the human-like pilot ----------------
        sealed class Human
        {
            public System.Random rnd; public float delay, gain, pitchGain, noise, wingsAt; public bool keys; public Func<Vector3> target;
            public readonly Queue<(float t, float yaw, float pitch)> seen = new Queue<(float, float, float)>(); public float lastYaw, lastPitch, seed;
            public float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        }
        Human hp;
        Human NewHuman(int seed)
        {
            var h = new Human { rnd = new System.Random(seed) }; h.delay = h.R(.25f, .45f); h.gain = h.R(20f, 34f); h.pitchGain = h.R(9f, 15f); h.noise = h.R(.05f, .18f);
            h.keys = h.rnd.NextDouble() < .5; h.wingsAt = h.R(.2f, .7f); h.seed = h.R(0, 100); return h;
        }
        // the pilot's look at the world: where the HUD arrow points (or a scenario's own aim), as an angle left / right and up / down
        void HumanStep()
        {
            var h = hp; if (h == null || !v.Course) return;
            var p = v.Body.position; var tgt = h.target != null ? h.target() : v.GuideTarget; var to = tgt - p;
            var fwd = Vector3.ProjectOnPlane(v.transform.forward, Vector3.up).normalized; var flat = Vector3.ProjectOnPlane(to, Vector3.up);
            float yaw = flat.sqrMagnitude > 1 ? Vector3.SignedAngle(fwd, flat.normalized, Vector3.up) : 0;
            float pitch = Mathf.Atan2(to.y, Mathf.Max(1f, flat.magnitude)) * Mathf.Rad2Deg - (v.Mode == Mode.Fly ? v.PitchDeg : 0);
            h.seen.Enqueue((Time.time, yaw, pitch));
            while (h.seen.Count > 1 && Time.time - h.seen.Peek().t > h.delay) { var x = h.seen.Dequeue(); h.lastYaw = x.yaw; h.lastPitch = x.pitch; }
            float n = (Mathf.PerlinNoise(h.seed, Time.time * 1.3f) - .5f) * 2f * h.noise;
            float steer = Mathf.Clamp(h.lastYaw / h.gain + n, -1, 1), vert = v.Mode == Mode.Fly ? Mathf.Clamp(h.lastPitch / h.pitchGain + n * .6f, -1, 1) : 0;
            if (h.keys) { steer = Mathf.Abs(steer) > .35f ? Mathf.Sign(steer) : 0; vert = Mathf.Abs(vert) > .35f ? Mathf.Sign(vert) : 0; }
            else { if (Mathf.Abs(steer) < .08f) steer = 0; if (Mathf.Abs(vert) < .08f) vert = 0; }
            input.sThrottle = 1; input.sBrake = 0; input.sBoost = false; input.sSecondary = false; input.sSteer = steer; input.sVertical = vert;
        }
        void UseHuman(Human h) { hp = h; custom = HumanStep; }

        // placed before a lip on the course line: speed, a lane offset and a heading offset
        void PlaceBefore(float p, float speed, float lane, float headingDeg)
        {
            var c = v.Course; int a = 0; for (int i = 0; i < c.anchors.Count; i++) if (c.anchors[i].progress < p - 5) a = i; v.Begin(c, c.anchors[a]);
            var q = LineAt(c, p, out var r); q += r * Vector3.right * lane; v.DebugPlace(q, r * Quaternion.Euler(0, headingDeg, 0), Mode.Drive, speed, p);
        }

        // one go at a choice jump: the human drives from 80 m (Paradise Plunge) / 140 m before the lip, presses LB late in the window (fly) or not (car), on to G1
        IEnumerator HumanChoice(int course, float lip, bool flyIt, int seed, Action<bool, float, string> done)
        {
            Clear(); game.StartCourse(course, false); yield return Until(() => !v.Holding, 6f);
            var h = NewHuman(seed); float speed = new[] { 45f, 55f, 65f, 75f }[seed % 4], lane = h.R(-5, 5), head = h.R(-5, 5);
            PlaceBefore(lip - (course == 0 ? 80 : 140), speed, lane, head); UseHuman(h);   // Paradise Plunge: 140 m before the lip is on the loop
            int r0 = v.Resets; float tLip = -1, t0 = Time.time, wingsOpenAt = -1; bool pressed = false; float minBack = 0; var resets = new List<string>();
            int rc = v.Resets;
            while (Time.time - t0 < 45f && v.GatesPassed < 2)
            {
                if (tLip < 0 && v.Progress >= lip) tLip = Time.time;
                if (flyIt && !pressed && v.WingsOpen) { if (wingsOpenAt < 0) wingsOpenAt = Time.time; if (Time.time - wingsOpenAt >= h.wingsAt && v.GroundedWheels == 0) { input.sWings = true; pressed = true; } }
                if (v.Resets > rc) { rc = v.Resets; resets.Add($"{v.LastResetReason} -> {v.LastResetHow} at {v.Progress - lip:+0;-0} m from the lip ({v.LastResetBack:0} m back)"); minBack = Mathf.Min(minBack, v.Progress - lip); }
                yield return null;
            }
            bool flew = v.ShortcutsTaken.Contains("Lake flight") || v.ShortcutsTaken.Contains("Midway flight");
            float t = v.GatesPassed >= 2 && tLip > 0 ? Time.time - tLip : -1;
            bool ok = t > 0 && v.Resets == r0 && (!flyIt || flew);
            done(ok, t, $"{(h.keys ? "keys" : "stick")}, {speed:0} m/s, lane {lane:+0.0;-0.0} m, heading {head:+0;-0} deg, late {h.delay:0.00} s, LB {(flyIt ? (pressed ? $"{h.wingsAt:0.00} s into the window" : "not in time") : "-")}: {(t > 0 ? $"{t:0.00} s lip -> G1" : "did not reach G1")}, flew {flew}, penalty {v.Penalty:0} s{(v.NearMisses.Count > 0 ? $" ({string.Join(", ", v.NearMisses)})" : "")}, resets {v.Resets - r0}{(resets.Count > 0 ? ": " + string.Join("; ", resets) : "")}");
            Clear();
        }

        IEnumerator HumanFly()
        {
            int tries = ArgInt("-tries", 10);
            foreach (int course in new[] { 0, 1 })
            {
                game.StartCourse(course, false); var c = game.Courses[course]; var sc = c.shortcuts.Find(x => x.choice); float lip = course == 0 ? Mk("jC") : CoasterCoast.Marks["jM"];
                if (sc == null) { Line($"HUMAN-LIKE {c.courseName}: no choice jump (Round 14: the Ferris wheel flight is on the main route; see the wheel check)"); continue; }
                Line($"HUMAN-LIKE PILOT ({c.courseName}, {sc.label}{(ShiftVehicle.OldFlight ? ", BUILD 13's flight (-oldFlight)" : "")}): line {sc.Length:0} m, main {sc.mainTo - sc.mainFrom:0} m; steers by the HUD arrow's target only, 0.25-0.45 s late, keys or a noisy stick, no boost, LB 0.2-0.7 s into the 1 s window; entry 45 / 55 / 65 / 75 m/s, +-5 m, +-5 deg");
                int okF = 0, okC = 0, clean = 0; var tf = new List<float>(); var tc = new List<float>();
                for (int k = 0; k < tries; k++)
                {
                    scenario = $"human-fly{course}"; bool ok = false; float t = -1; string s = "";
                    yield return HumanChoice(course, lip, true, 100 + k, (a, b, d) => { ok = a; t = b; s = d; });
                    if (ok) { okF++; tf.Add(t); if (s.Contains("penalty 0 s")) clean++; }
                    Line($"  fly try {k + 1} {(ok ? "MADE IT" : "MISSED")}: {s}");
                    if (k == 0 || !ok) Capture($"fly-{k + 1}-end");
                }
                for (int k = 0; k < Math.Max(4, tries / 2); k++)
                {
                    scenario = $"human-car{course}"; bool ok = false; float t = -1; string s = "";
                    yield return HumanChoice(course, lip, false, 300 + k, (a, b, d) => { ok = a; t = b; s = d; });
                    if (ok) { okC++; tc.Add(t); }
                    Line($"  car try {k + 1} {(ok ? "MADE IT" : "MISSED")}: {s}");
                }
                float mf = tf.Count > 0 ? tf.Average() : -1, mc = tc.Count > 0 ? tc.Average() : -1;
                bool pass = okF * 10 >= tries * 9;
                Line($"HUMAN-LIKE {c.courseName} {(pass ? "PASS" : "FAIL")}: fly line made {okF} of {tries} ({clean} with no penalty), car line {okC} of {Math.Max(4, tries / 2)}; lip -> G1 mean fly {mf:0.00} s, car {mc:0.00} s ({(mf > 0 && mc > 0 ? $"fly {mc - mf:+0.00;-0.00} s faster" : "-")})");
                Save();
            }
        }

        // ---------------- scripted failures on every flight branch ----------------
        // fault: the inputs for `seconds` after the wings open (or the slit's start), then the human pilot takes over by the HUD to `gates`
        IEnumerator BranchFault(string name, int course, bool slit, Action<ShiftVehicle> fault, float seconds, int gatesTarget, Func<Vector3> aim, Action<string, bool> done)
        {
            scenario = "branch-" + name; Clear(); game.StartCourse(course, false); yield return Until(() => !v.Holding, 6f);
            var c = v.Course; float lip = slit ? -1 : course == 0 ? Mk("jC") : CoasterCoast.Marks["jM"]; Shortcut sc = slit ? c.shortcuts.Find(x => x.label == "Sky Arch slit") : c.shortcuts.Find(x => x.choice);
            var h = NewHuman(7); h.keys = false; h.noise = .05f; h.delay = .3f; h.wingsAt = .3f;
            if (slit) { v.Begin(c, c.anchors[AnchorNear(Mk("lip") + 90)]); }
            else PlaceBefore(lip - (course == 0 ? 80 : 140), 62, 0, 0);
            UseHuman(h); if (aim != null) h.target = aim;
            float startBest = v.BestProgress, mark = slit ? sc.mainFrom : lip, t0 = Time.time; int r0 = v.Resets, rc = r0; bool pressed = false, faulting = false; float faultFrom = -1, wingsOpenAt = -1;
            var resets = new List<string>(); int behind = 0, loops = 0; var resetAt = new List<float>();
            while (Time.time - t0 < 60f && v.GatesPassed < gatesTarget)
            {
                if (!slit && !pressed && v.WingsOpen) { if (wingsOpenAt < 0) wingsOpenAt = Time.time; if (Time.time - wingsOpenAt >= h.wingsAt && v.GroundedWheels == 0) { input.sWings = true; pressed = true; } }
                bool startFault = faultFrom < 0 && (slit ? v.Progress > sc.mainFrom + (name.Contains("veer") ? 520 : 200) : v.Mode == Mode.Fly && v.Progress > lip);
                if (startFault) { faultFrom = Time.time; faulting = true; custom = () => { fault(v); }; }
                if (faulting && Time.time - faultFrom > seconds) { faulting = false; custom = HumanStep; h.target = null; }
                if (v.Resets > rc)
                {
                    rc = v.Resets; float at = v.Progress; resetAt.Add(at);
                    if (at < mark - 1f) behind++;
                    if (resetAt.Count >= 2 && Mathf.Abs(resetAt[resetAt.Count - 1] - resetAt[resetAt.Count - 2]) < 30f) loops++;
                    resets.Add($"{v.LastResetReason} -> {v.LastResetHow} at {at - mark:+0;-0} m from the {(slit ? "branch start" : "lip")} ({v.LastResetBack:0} m back)");
                    if (faulting) { faulting = false; custom = HumanStep; h.target = null; }
                }
                yield return null;
            }
            bool through = v.GatesPassed >= gatesTarget;
            bool ok = through && behind == 0 && loops == 0;
            done($"{(faultFrom > 0 ? "" : "(fault never started) ")}resets {v.Resets - r0}{(resets.Count > 0 ? ": " + string.Join("; ", resets) : "")}; behind the jump / branch {behind}, loops {loops}; {(through ? $"reached {c.gates[gatesTarget - 1].label} after {Time.time - t0:0.0} s" : $"stuck before {c.gates[Mathf.Min(gatesTarget - 1, c.gates.Count - 1)].label} (gates {v.GatesPassed})")}, penalty {v.Penalty:0} s", ok);
            Capture("end"); Clear();
        }

        static Action<ShiftVehicle> Once(Action a) { bool done = false; return x => { if (!done) { done = true; a(); } }; }
        IEnumerator Branches()
        {
            Line($"BRANCHES: scripted failures on every flight branch{(ShiftVehicle.OldFlight ? " (BUILD 13's flight: -oldFlight)" : "")}; after the fault the human-like pilot carries on by the HUD; pass = through, never reset behind the jump / branch start, no loop");
            Action<ShiftVehicle> Hold(float steer, float vert) => x => { input.sThrottle = 1; input.sBrake = 0; input.sBoost = false; input.sSteer = steer; input.sVertical = vert; };
            var faults = new (string n, Action<ShiftVehicle> f, float s)[]
            {
                ("no-steering", Hold(0, 0), 6f), ("hard-left", Hold(-1, 0), 3f), ("hard-right", Hold(1, 0), 2.5f), ("too-high", Hold(0, 1), 4f), ("too-low", Hold(0, -1), 3f),
                ("exit-missed", null, 0f), ("reset-button", Once(() => input.RequestReset()), .5f),
            };
            int pass = 0, total = 0;
            foreach (int course in new[] { 0, 1 })
            {
                var c = game.Courses[course]; var sc = c.shortcuts.Find(x => x.choice);
                if (sc == null) { Line($"  {c.courseName}: no choice flight (Round 14: the Ferris wheel flight is on the main route; the wheel check covers it)"); continue; }
                foreach (var (n, f, s) in faults)
                {
                    string r = ""; bool ok = false; total++;
                    if (n == "exit-missed")
                    {
                        // fly the branch, then climb hard over its exit frame: the last fifth of the line
                        float climbFrom = -1;
                        Func<Vector3> aim = () => { if (v.OnShortcut && climbFrom < 0 && v.Progress > Mathf.Lerp(sc.mainFrom, sc.mainTo, .8f)) climbFrom = Time.time; if (climbFrom > 0 && Time.time - climbFrom < 2.5f) return v.Body.position + v.transform.forward * 50 + Vector3.up * 40; return v.GuideTarget; };
                        yield return BranchFault($"{course}-{n}", course, false, x => HumanStep(), 30f, 2, aim, (a, b) => { r = a; ok = b; });
                    }
                    else yield return BranchFault($"{course}-{n}", course, false, f, s, 2, null, (a, b) => { r = a; ok = b; });
                    if (ok) pass++;
                    Line($"  {c.courseName} {sc.label} {n} {(ok ? "PASS" : "FAIL")}: {r}");
                    Save();
                }
            }
            // the Sky Arch slit: aimed at it but 16 m off (the buttress), into it and veering out, flown past G3 wide, the reset button on it,
            // and the HUD pilot alone (build 13's arrow led it into the slit)
            var slit = game.Courses[0].shortcuts.Find(x => x.label == "Sky Arch slit");
            Vector3 SlitAim(float off) { var q = slit.entry.transform.position; return q + slit.entry.transform.right * off; }
            var sf = new (string n, Action<ShiftVehicle> f, float s, Func<Vector3> aim)[]
            {
                ("slit-buttress", x => { hp.target = () => SlitAim(16); HumanStep(); }, 12f, null),
                ("slit-veer", Hold(1, .3f), 2.5f, () => slit.entry.transform.position + slit.entry.transform.forward * 80),
                ("past-G3-wide", x => { hp.target = () => { var q = game.Courses[0].gates[3].transform; return q.position + q.right * 75 + q.forward * 60; }; HumanStep(); }, 9f, null),
                ("slit-reset-button", Once(() => input.RequestReset()), .5f, () => slit.entry.transform.position + slit.entry.transform.forward * 80),
                ("hud-only", x => HumanStep(), 0f, null),
            };
            foreach (var (n, f, s, aim) in sf)
            {
                string r = ""; bool ok = false; total++;
                yield return BranchFault(n, 0, true, f, s, 4, aim, (a, b) => { r = a; ok = b; });
                if (ok) pass++;
                Line($"  Paradise Plunge Sky Arch slit {n} {(ok ? "PASS" : "FAIL")}: {r}");
                Save();
            }
            Line($"BRANCHES {(pass == total ? "PASS" : "FAIL")}: {pass} of {total} scripted failures ended ahead or at most one anchor back with no loop");
            Save();
        }

        // ---------------- how far back a reset goes, every 20 m of both laps ----------------
        void ResetAudit()
        {
            foreach (var c in game.Courses)
            {
                var worst = new Dictionary<string, (float back, float at)>(); var over = new List<string>();
                for (float p = 30; p < c.Length - 10; p += 20)
                {
                    int g = 0; while (g < c.gates.Count && c.gates[g].progress < p - 1) g++;
                    var a = c.AnchorFor(p, g); float limit = g < c.gates.Count ? Mathf.Min(p, c.gates[g].progress) : p; float back = p - a.progress; string how = "anchor";
                    if (!ShiftVehicle.OldFlight && a.mode == Mode.Fly && limit - a.progress > game.Shared.flightResetMaxBack && ShiftVehicle.FlightPointOk(c, limit - 30, g < c.gates.Count ? c.gates[g] : null)) { back = p - (limit - 30); how = "flight line"; }
                    string seg = "?"; int gs = 0; foreach (var s in c.segments) { if (p <= s.endProgress) { seg = s.name; break; } gs++; }
                    if (!worst.TryGetValue(seg, out var w) || back > w.back) worst[seg] = (back, p);
                    float secs = back / Mathf.Max(20f, (a.mode == Mode.Fly ? 80 : a.mode == Mode.Drive ? 65 : a.mode == Mode.Boat ? 50 : 38));
                    if (secs > 6f) over.Add($"{p:0} m ({seg}, {how} at {p - back:0} m in {a.mode}: {back:0} m, ~{secs:0.0} s)");
                }
                Line($"RESET AUDIT {c.courseName}: worst distance back per segment: {string.Join("; ", worst.Select(kv => $"{kv.Key} {kv.Value.back:0} m (at {kv.Value.at:0} m)"))}");
                Line($"  places where a reset would cost more than ~6 s of progress: {(over.Count == 0 ? "none" : over.Count + ": " + string.Join(", ", over.Take(14)) + (over.Count > 14 ? " ..." : ""))}");
            }
            Save();
        }

        // ---------------- every course open ----------------
        IEnumerator UnlockCheck()
        {
            scenario = "unlock"; Clear(); coast = true; game.StartCourse(0, false); yield return Wait(.3f);
            bool fresh = !System.IO.File.Exists(Tour.FilePath);
            Line($"UNLOCK: fresh tour.json {fresh} ({Tour.FilePath}), fresh settings.json {!System.IO.File.Exists(Settings.FilePath)}; Unlock all Tour stops {(Settings.UnlockAllTour ? "On" : "Off")} (default {(Settings.UnlockTourDefault ? "On" : "Off")}); Tour stops playable {Tour.Unlocked} of {Tour.Stops.Length}, earned {Tour.Earned}");
            game.OpenFrontMenu(); yield return null;
            int Row(string start) => game.MenuItems.FindIndex(x => x.StartsWith(start));
            // Round 14: main menu > Race > the course page (right: Coaster Coast) > choose your racer > Go
            game.Choose(Row("Race")); yield return null;
            while (!game.MenuItems[Row("Course")].Contains("Coaster Coast")) { input.sSide = 1; yield return null; yield return null; }
            string raceRow = game.MenuItems[Row("Course")]; Capture("course-race-coaster");
            game.Choose(Row("Course")); yield return null; string pickCourse = game.MenuItems[Row("Go:")]; Capture("pick-race-coaster");
            game.Choose(Row("Go:")); yield return Wait(.5f);
            bool race = game.CourseIndex == 1 && game.RaceMode && !game.MenuOpen;
            Line($"UNLOCK Race {(race ? "PASS" : "FAIL")}: main menu \"{raceRow}\"; pick page \"{pickCourse}\"; started course {game.Courses[game.CourseIndex].courseName}, race {game.RaceMode}");
            game.OpenFrontMenu(); yield return null; game.Choose(Row("Time Trial")); yield return null; string trialRow = game.MenuItems[Row("Course")]; game.Choose(Row("Course")); yield return null; game.Choose(Row("Go:")); yield return Wait(.5f);
            bool trial = game.CourseIndex == 1 && !game.RaceMode;
            Line($"UNLOCK Time Trial {(trial ? "PASS" : "FAIL")}: \"{trialRow}\"; started {game.Courses[game.CourseIndex].courseName}, race {game.RaceMode}");
            // 2 Players: a second (virtual) pad joins, the page's Course row says Coaster Coast, its Time Trial starts it
            Pads(); game.OpenFrontMenu(); yield return null; game.Choose(Row("2 Players")); yield return null;
            SplitScreen.P1Device = pad1; game.Join(pad2); yield return null;
            string splitCourse = game.MenuItems[Row("Course:")]; Capture("split-page-coaster");
            game.Choose(Row("Time Trial")); yield return Wait(.8f);
            bool split = SplitScreen.Active && game.CourseIndex == 1;
            Line($"UNLOCK 2 Players {(split ? "PASS" : "FAIL")}: page \"{splitCourse}\"; split active {SplitScreen.Active}, course {game.Courses[game.CourseIndex].courseName}");
            game.EndSplit(false); yield return null;
            // the Tour page: both stops open while the setting is On; Off: stop 2 locked again (top 3 at stop 1 opens it)
            game.OpenFrontMenu(); yield return null; game.Choose(Row("The Tour")); yield return null;
            string on2 = game.MenuItems[1]; bool on2ok = !on2.Contains("locked");
            bool keep = Settings.UnlockAllTour; Settings.UnlockAllTour = false; game.Choose(game.MenuItems.Count - 1); yield return null; game.Choose(Row("The Tour")); yield return null;
            string off2 = game.MenuItems[1]; bool off2ok = off2.Contains("locked") && Tour.Unlocked == 1;
            Settings.UnlockAllTour = keep; game.CloseMenu();
            Line($"UNLOCK Tour {(on2ok && off2ok ? "PASS" : "FAIL")}: setting On: \"{on2}\"; Off: \"{off2}\"");
            Line($"UNLOCK {(race && trial && split && on2ok && off2ok ? "PASS" : "FAIL")}: Coaster Coast open in Race, Time Trial and 2 Players on a fresh save; only the Tour's own order can lock (and not while \"Unlock all Tour stops\" is On)");
            coast = false; Clear(); Save();
        }

        IEnumerator Round12(string which)
        {
            bool all = which == "round12";
            if (all || which == "unlock") yield return UnlockCheck();
            if (all || which == "resetaudit") ResetAudit();
            if (all || which == "humanfly") yield return HumanFly();
            if (all || which == "branches") yield return Branches();
        }
    }
}
