using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 13 checks.
    //   trace     item 1's recorder: soak-like AI races (-course N, -laps N, -timeScale 3) with a 4 s ring buffer per racer (position, velocity,
    //             heading, body forward, form, wheels on the ground, shortcut, progress, metres off the line, the inputs, the collider under it,
    //             wall hits); every reset dumps the racer's last 4 s, so a reset's cause is read from what happened, not guessed
    public sealed partial class CheckPilot
    {
        sealed class TraceSample { public float t; public Vector3 p, vel, fwd; public float yaw, prog, off, steer, thr, brk; public Mode mode; public int wheels; public bool changing, boost; public string sc, under, ev; }

        static string Under(ShiftVehicle r)
        {
            return Physics.Raycast(r.Body.position + Vector3.up * .5f, Vector3.down, out var hit, 30f, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore) ? $"{hit.collider.name}@{hit.distance - .5f:0.0}" : "nothing within 30 m";
        }

        IEnumerator Trace()
        {
            int course = ArgInt("-course", 0), laps = ArgInt("-laps", 40); float scale = ArgFloat("-timeScale", 3f), limit = 300f;
            Line($"TRACE ({game.Courses[course].courseName}): {laps} AI laps (four AI cars per race, Abi's car driven by an AI too), time scale {scale:0.0}; every reset dumps the racer's last 4 s (every 0.1 s)");
            var abi = new Rival { name = "Abi (AI)", lane = 0, brakeMargin = .9f, boostMeter = .6f, boostStraight = 9f, shortcut = "Sky Arch slit|Coral Vault tunnel|Lighthouse rocks", shortcutRate = .25f, seed = 51 };
            int done = 0, race = 0, resets = 0; var spots = new List<string>();
            while (done < laps)
            {
                race++; scenario = $"trace{race}"; Clear(); aiDrives = true;
                game.StartCourse(course, true);
                var pilot = v.GetComponent<RivalPilot>() ?? v.gameObject.AddComponent<RivalPilot>(); pilot.rival = abi; pilot.AlwaysOn = true;
                var racers = game.Race.Racers.ToList();
                int force = Array.IndexOf(Environment.GetCommandLineArgs(), "-carLine") >= 0 ? -1 : Array.IndexOf(Environment.GetCommandLineArgs(), "-flyLine") >= 0 ? 1 : 0;
                if (force != 0) foreach (var r in racers) { var rp = r.GetComponent<RivalPilot>(); if (rp) rp.forceChoice = force; }
                var buf = racers.ToDictionary(r => r, r => new Queue<TraceSample>());
                var evs = racers.ToDictionary(r => r, r => "");
                var hooks = new List<(ShiftVehicle r, Action<float, float, string> w, Action<string, string> e)>();
                foreach (var r in racers)
                {
                    var rr = r;
                    Action<float, float, string> w = (a, k, what) => evs[rr] += $"wall {what} {a:0} deg keep {k:0.00}; ";
                    Action<string, string> e = (kind, text) =>
                    {
                        if (kind == "change" || kind == "shortcut" || kind == "gate" || kind == "rejected" || kind == "plan" || kind == "unstick") evs[rr] += $"{kind}: {text}; ";
                        if (kind != "reset") return;
                        resets++; spots.Add($"{rr.RacerName}: {text}");
                        Line($"  RESET race {race} {rr.RacerName}: {text}");
                        foreach (var s in buf[rr])
                            Line($"    {s.t,6:0.00} {s.mode,-5}{(s.changing ? "*" : " ")} p ({s.p.x:0.0},{s.p.y:0.0},{s.p.z:0.0}) v ({s.vel.x:0},{s.vel.y:0},{s.vel.z:0}) |v| {s.vel.magnitude:0} yaw {s.yaw:0} fwd ({s.fwd.x:0.00},{s.fwd.y:0.00},{s.fwd.z:0.00}) prog {s.prog:0} off {s.off:0} wheels {s.wheels} in thr {s.thr:0.0} brk {s.brk:0.0} steer {s.steer:+0.00;-0.00}{(s.boost ? " BOOST" : "")} sc {s.sc} under {s.under}{(s.ev != "" ? " | " + s.ev : "")}");
                        Save();
                    };
                    r.WallHit += w; r.Event += e; hooks.Add((r, w, e));
                    r.Contact += (col, first) =>
                    {
                        if (!first || !col.collider || col.contactCount == 0 || col.rigidbody) return; var n = col.GetContact(0).normal;
                        evs[rr] += $"touch {col.collider.name} n ({n.x:0.00},{n.y:0.00},{n.z:0.00}) rel {col.relativeVelocity.magnitude:0}; ";
                    };
                }
                Time.timeScale = scale;
                yield return Until(() => !v.Holding, 10f); float go = Time.time, next = 0;
                while (Time.time - go < limit + 2 && !racers.All(r => r.Finished))
                {
                    if (Time.time >= next)
                    {
                        next = Time.time + .1f;
                        foreach (var r in racers)
                        {
                            if (r.Finished) continue; var q = buf[r]; var inp = r.Input;
                            q.Enqueue(new TraceSample { t = Time.time - r.StartTime, p = r.Body.position, vel = r.Body.linearVelocity, fwd = r.transform.forward, yaw = r.HeadingDeg, prog = r.Progress, off = r.OffLine, mode = r.Mode, wheels = r.GroundedWheels, changing = r.Changing, steer = inp.Steer, thr = inp.Throttle, brk = inp.Brake, boost = inp.Boost, sc = r.OnShortcut ? r.OnShortcut.label : "-", under = Under(r), ev = evs[r] });
                            evs[r] = ""; while (q.Count > 40) q.Dequeue();
                        }
                    }
                    yield return null;
                }
                Time.timeScale = 1;
                foreach (var (r, w, e) in hooks) { r.WallHit -= w; r.Event -= e; }
                done += racers.Count;
                Line($"  race {race}: {string.Join(", ", game.Race.Order().Select(r => $"{r.RacerName} {(r.Finished ? r.FinishTime.ToString("0.0") : "DNF")}{(r.Resets > 0 ? $" ({r.Resets} resets)" : "")}{(r.ShortcutsTaken.Count > 0 ? " [" + string.Join("+", r.ShortcutsTaken) + "]" : "")}"))}");
                Save();
            }
            aiDrives = false; StopAi();
            Line($"TRACE: {resets} resets in {done} laps");
            foreach (var s in spots) Line("  " + s);
        }

        // ---------------- items 2 and 3: the Cliff flight and the Mangrove bypass ----------------
        static float CliffLip => ParadisePlunge.Jumps.Find(j => j.name == "Cliff Gap").lip;
        string Changes(float from, float to) => string.Join(" ", v.Log.Where(l => l.Contains("->") && l.Contains(" at z ")).Select(l => l.Substring(l.IndexOf(']') + 2).Split(' ')[0]));

        // one AI leg: from an anchor, a plan / a forced choice, until done(); the time from `mark` progress
        IEnumerator RouteLeg(string name, float anchorAt, string plan, int choice, float mark, Func<bool> doneWhen, Action<float, string> done)
        {
            scenario = name; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
            v.Begin(v.Course, v.Course.anchors[AnchorNear(anchorAt)]); var pilot = DriveWith(plan); pilot.forceChoice = choice;
            int r0 = v.Resets, ch0 = v.Changes; float tMark = -1, t0 = Time.time; bool shot = false;
            while (Time.time - t0 < 60f && !doneWhen() && v.Resets == r0)
            {
                if (tMark < 0 && v.Progress >= mark) tMark = Time.time;
                if (!shot && v.OnShortcut && Time.time - (tMark > 0 ? tMark : t0) > 3f) { shot = true; Capture("on-" + v.OnShortcut.label.Replace(" ", "-")); }
                yield return null;
            }
            float t = doneWhen() && tMark > 0 ? Time.time - tMark : -1;
            done(t, $"{(t >= 0 ? $"{t:0.00} s" : "did not get there")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, changes {v.Changes - ch0} ({Changes(0, 0)}), shortcut {(v.ShortcutsTaken.Count > 0 ? string.Join(", ", v.ShortcutsTaken) : "none")}, penalty {v.Penalty:0} s, gates {string.Join(" ", v.GateLog)}");
            StopAi();
        }

        IEnumerator Choice13()
        {
            game.StartCourse(0, false); var c = game.Courses[0];
            var cf = c.shortcuts.Find(x => x.label == "Cliff flight"); var mb = c.shortcuts.Find(x => x.label == "Mangrove bypass");
            float lip = CliffLip, after = Mk("lip") + ParadisePlunge.CliffRejoinAfter + 60;
            if (cf) Line($"CLIFF GAP CHOICE: car line {cf.mainTo - cf.mainFrom:0} m of climb road + ramp from the gap's lip to the rejoin in the air; fly line {cf.Length:0} m (three rings over the hillside, entry / exit sub-gates; stands in for G2)");
            int runs = ArgInt("-runs", 3);
            var tc = new List<float>(); var tf = new List<float>();
            for (int k = 0; k < runs; k++)
            {
                float t1 = 0, t2 = 0; string s1 = "", s2 = "";
                yield return RouteLeg("cliff-car", lip - 160, "", -1, lip, () => v.GatesPassed >= 3 && v.Progress >= after, (t, s) => { t1 = t; s1 = s; });
                yield return RouteLeg("cliff-fly", lip - 160, "", 1, lip, () => v.GatesPassed >= 3 && v.Progress >= after, (t, s) => { t2 = t; s2 = s; });
                if (t1 > 0) tc.Add(t1); if (t2 > 0 && s2.Contains("Cliff flight")) tf.Add(t2);
                Line($"  run {k + 1}: car line {s1}; fly line {s2}");
            }
            float mc = tc.Count > 0 ? tc.Average() : -1, mf = tf.Count > 0 ? tf.Average() : -1;
            Line($"CLIFF GAP {(tc.Count == runs && tf.Count == runs ? "PASS" : "FAIL")}: lip -> {after - lip:0} m on (past the rejoin, G2 counted): car {mc:0.00} s, fly {mf:0.00} s ({(mc > 0 && mf > 0 ? $"fly {mc - mf:+0.00;-0.00} s, {100 * (mc - mf) / mc:+0;-0} %" : "-")}; aim: about equal)");
            Save();
            if (!mb) yield break;
            Line($"MANGROVE BYPASS: boat line {mb.mainTo - mb.mainFrom:0} m of river (the S-bend), sub line {mb.Length:0} m (dive lane, root cavern under the bed, surfacing zone); both pass G5");
            var tb = new List<float>(); var ts = new List<float>(); int flicker = 0;
            for (int k = 0; k < runs; k++)
            {
                float t1 = 0, t2 = 0; string s1 = "", s2 = "";
                yield return RouteLeg("bypass-boat", Mk("splash") + 70, "", 0, ParadisePlunge.BypassFrom, () => v.GatesPassed >= 6, (t, s) => { t1 = t; s1 = s; });
                yield return RouteLeg("bypass-sub", Mk("splash") + 70, "Mangrove bypass", 0, ParadisePlunge.BypassFrom, () => v.GatesPassed >= 6, (t, s) => { t2 = t; s2 = s; });
                // clean transitions: exactly Boat->Dive then Dive->Boat in the bypass (no flicker), nothing else
                int bd = v.Log.Count(l => l.Contains("Boat->Dive at z")), db = v.Log.Count(l => l.Contains("Dive->Boat at z"));
                if (bd != 1 || db != 1) flicker++;
                if (t1 > 0) tb.Add(t1); if (t2 > 0 && s2.Contains("Mangrove bypass")) ts.Add(t2);
                Line($"  run {k + 1}: boat line {s1}; sub line {s2} (Boat->Dive {bd}, Dive->Boat {db})");
            }
            float mbt = tb.Count > 0 ? tb.Average() : -1, mst = ts.Count > 0 ? ts.Average() : -1;
            Line($"MANGROVE BYPASS {(tb.Count == runs && ts.Count == runs && flicker == 0 ? "PASS" : "FAIL")}: from the lane ({ParadisePlunge.BypassFrom:0} m) to G5: boat {mbt:0.00} s, sub {mst:0.00} s ({(mbt > 0 && mst > 0 ? $"sub {mbt - mst:+0.00;-0.00} s, {100 * (mbt - mst) / mbt:+0;-0} %" : "-")}; aim: about equal); transitions clean in {runs - flicker} of {runs}");
            Save();
        }

        // the human-like pilot on the Cliff Gap's fly line (LB late in the window) and its car line
        IEnumerator HumanCliff()
        {
            int tries = ArgInt("-tries", 10); game.StartCourse(0, false); var c = game.Courses[0]; float lip = CliffLip, after = Mk("lip") + ParadisePlunge.CliffRejoinAfter + 60;
            Line($"HUMAN-LIKE PILOT (Paradise Plunge, Cliff flight): steers by the HUD arrow only, 0.25-0.45 s late, keys or a noisy stick, no boost, LB 0.2-0.7 s into the window; entry 45-75 m/s, +-5 m, +-5 deg");
            int okF = 0, okC = 0, clean = 0; var tf = new List<float>(); var tc = new List<float>();
            for (int k = 0; k < tries + Math.Max(4, tries / 2); k++)
            {
                bool fly = k < tries; scenario = fly ? "human-cliff-fly" : "human-cliff-car"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                var h = NewHuman((fly ? 500 : 700) + k); float speed = new[] { 45f, 55f, 65f, 75f }[k % 4], lane = h.R(-4, 4), head = h.R(-5, 5);
                PlaceBefore(lip - 80, speed, lane, head); UseHuman(h);
                int r0 = v.Resets; float tLip = -1, t0 = Time.time, wo = -1; bool pressed = false; var resets = new List<string>(); int rc = r0;
                while (Time.time - t0 < 45f && !(v.GatesPassed >= 3 && v.Progress >= after))
                {
                    if (tLip < 0 && v.Progress >= lip) tLip = Time.time;
                    if (fly && !pressed && v.WingsOpen) { if (wo < 0) wo = Time.time; if (Time.time - wo >= h.wingsAt && v.GroundedWheels == 0) { input.sWings = true; pressed = true; } }
                    if (v.Resets > rc) { rc = v.Resets; resets.Add($"{v.LastResetReason} -> {v.LastResetHow} at {v.Progress - lip:+0;-0} m from the lip"); }
                    yield return null;
                }
                bool flew = v.ShortcutsTaken.Contains("Cliff flight"); bool through = v.GatesPassed >= 3 && v.Progress >= after;
                float t = through && tLip > 0 ? Time.time - tLip : -1; bool ok = through && v.Resets == r0 && (!fly || flew);
                if (fly) { if (ok) { okF++; tf.Add(t); if (v.Penalty == 0) clean++; } } else if (ok) { okC++; tc.Add(t); }
                Line($"  {(fly ? "fly" : "car")} try {k + 1} {(ok ? "MADE IT" : "MISSED")}: {(h.keys ? "keys" : "stick")}, {speed:0} m/s, lane {lane:+0.0;-0.0}, heading {head:+0;-0}, late {h.delay:0.00} s, LB {(fly ? (pressed ? $"{h.wingsAt:0.00} s in" : "not in time") : "-")}: {(t > 0 ? $"{t:0.00} s" : "not through")}, flew {flew}, penalty {v.Penalty:0} s{(v.NearMisses.Count > 0 ? $" ({string.Join(", ", v.NearMisses)})" : "")}, resets {v.Resets - r0}{(resets.Count > 0 ? ": " + string.Join("; ", resets) : "")}");
                if (!ok || k == 0) Capture($"cliff-{(fly ? "fly" : "car")}-{k + 1}-end");
                Clear();
            }
            bool pass = okF * 10 >= tries * 9;
            Line($"HUMAN-LIKE CLIFF {(pass ? "PASS" : "FAIL")}: fly line made {okF} of {tries} ({clean} with no penalty), car line {okC} of {Math.Max(4, tries / 2)}; lip -> past the rejoin: fly {(tf.Count > 0 ? tf.Average() : 0):0.00} s, car {(tc.Count > 0 ? tc.Average() : 0):0.00} s");
            Save();
        }

        // scripted failures on the Cliff flight and in the Mangrove bypass
        IEnumerator Branches13()
        {
            Line("BRANCHES 13: scripted failures on the Cliff flight and in the Mangrove bypass; after the fault the human-like pilot carries on by the HUD; pass = through, never reset behind the jump / the dive lane, no loop");
            Action<ShiftVehicle> Hold(float steer, float vert) => x => { input.sThrottle = 1; input.sBrake = 0; input.sBoost = false; input.sSteer = steer; input.sVertical = vert; };
            var faults = new (string n, Action<ShiftVehicle> f, float s)[]
            {
                ("no-steering", Hold(0, 0), 6f), ("hard-left", Hold(-1, 0), 3f), ("hard-right", Hold(1, 0), 2.5f), ("too-high", Hold(0, 1), 4f), ("too-low", Hold(0, -1), 3f),
                ("reset-button", Once(() => input.RequestReset()), .5f),
            };
            int pass = 0, total = 0; float lip = CliffLip;
            foreach (var (n, f, s) in faults)
            {
                total++; string r = ""; bool ok = false;
                yield return Fault13("cliff-" + n, true, f, s, (a, b) => { r = a; ok = b; }); if (ok) pass++;
                Line($"  Cliff flight {n} {(ok ? "PASS" : "FAIL")}: {r}"); Save();
            }
            var bf = new (string n, Action<ShiftVehicle> f, float s)[]
            {
                ("no-steering", Hold(0, 0), 5f), ("hard-left", Hold(-1, 0), 2.5f), ("hard-right", Hold(1, 0), 2.5f), ("climb-hard", Hold(0, 1), 3f), ("dive-hard", Hold(0, -1), 3f),
                ("reset-button", Once(() => input.RequestReset()), .5f),
            };
            foreach (var (n, f, s) in bf)
            {
                total++; string r = ""; bool ok = false;
                yield return Fault13("bypass-" + n, false, f, s, (a, b) => { r = a; ok = b; }); if (ok) pass++;
                Line($"  Mangrove bypass {n} {(ok ? "PASS" : "FAIL")}: {r}"); Save();
            }
            // the dive lane taken but the entry missed (a submarine in the river): G5 takes a sub, the lap goes on
            {
                total++; scenario = "bypass-entry-missed"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                v.Begin(v.Course, v.Course.anchors[AnchorNear(Mk("splash") + 70)]); DriveWith("Mangrove bypass"); float t0 = Time.time; int r0 = v.Resets;
                yield return Until(() => v.Mode == Mode.Dive || Time.time - t0 > 30, 31f); StopAi();
                var hh = NewHuman(9); hh.keys = false; hh.noise = .03f; UseHuman(hh); hh.target = () => v.Course.PointAt(v.Progress + 40, out _) + Vector3.down * 4;
                yield return Until(() => v.GatesPassed >= 7 || Time.time - t0 > 60, 60f);
                bool ok = v.GatesPassed >= 7 && v.Resets == r0; if (ok) pass++;
                Line($"  Mangrove bypass entry-missed {(ok ? "PASS" : "FAIL")}: dived in the lane, stayed in the river as a sub (not into the cavern: on it {(v.ShortcutsTaken.Contains("Mangrove bypass") ? "yes" : "no")}); gates {v.GatesPassed} ({string.Join(" ", v.GateLog)}), resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}");
                Capture("bypass-entry-missed-end"); Clear(); Save();
            }
            Line($"BRANCHES 13 {(pass == total ? "PASS" : "FAIL")}: {pass} of {total} scripted failures ended ahead or at most one anchor back with no loop");
            Save();
        }

        IEnumerator Fault13(string name, bool cliff, Action<ShiftVehicle> fault, float seconds, Action<string, bool> done)
        {
            scenario = "branch-" + name; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
            var c = v.Course; var sc = c.shortcuts.Find(x => x.label == (cliff ? "Cliff flight" : "Mangrove bypass")); float lip = CliffLip, mark = cliff ? lip : ParadisePlunge.BypassFrom;
            int gatesTarget = cliff ? 4 : 6;
            var h = NewHuman(7); h.keys = false; h.noise = .05f; h.delay = .3f; h.wingsAt = .3f;
            if (cliff) { PlaceBefore(lip - 80, 60, 0, 0); UseHuman(h); }
            else { v.Begin(c, c.anchors[AnchorNear(Mk("splash") + 70)]); DriveWith("Mangrove bypass"); }
            float t0 = Time.time, faultFrom = -1, wo = -1; int r0 = v.Resets, rc = r0; bool pressed = false, faulting = false; var resets = new List<string>(); int behind = 0, loops = 0; var resetAt = new List<float>();
            while (Time.time - t0 < 70f && v.GatesPassed < gatesTarget)
            {
                if (cliff && !pressed && v.WingsOpen) { if (wo < 0) wo = Time.time; if (Time.time - wo >= h.wingsAt && v.GroundedWheels == 0) { input.sWings = true; pressed = true; } }
                bool start = faultFrom < 0 && (cliff ? v.Mode == Mode.Fly && v.Progress > lip : v.OnShortcut == sc && v.Mode == Mode.Dive && v.Body.position.y < -20f);
                if (start) { faultFrom = Time.time; faulting = true; if (!cliff) { StopAi(); UseHuman(h); } custom = () => fault(v); }
                if (faulting && Time.time - faultFrom > seconds) { faulting = false; custom = HumanStep; h.target = null; }
                if (v.Resets > rc)
                {
                    rc = v.Resets; float at = v.Progress; resetAt.Add(at); if (at < mark - 1f) behind++;
                    if (resetAt.Count >= 2 && Mathf.Abs(resetAt[resetAt.Count - 1] - resetAt[resetAt.Count - 2]) < 30f) loops++;
                    resets.Add($"{v.LastResetReason} -> {v.LastResetHow} at {at - mark:+0;-0} m from the {(cliff ? "lip" : "lane")}");
                    if (faulting) { faulting = false; custom = HumanStep; h.target = null; }
                }
                yield return null;
            }
            StopAi(); bool through = v.GatesPassed >= gatesTarget;
            done($"{(faultFrom > 0 ? "" : "(fault never started) ")}resets {v.Resets - r0}{(resets.Count > 0 ? ": " + string.Join("; ", resets) : "")}; behind {behind}, loops {loops}; {(through ? $"reached {c.gates[gatesTarget - 1].label} after {Time.time - t0:0.0} s" : $"stuck (gates {v.GatesPassed})")}, penalty {v.Penalty:0} s, form changes {Changes(0, 0)}", through && behind == 0 && loops == 0 && faultFrom > 0);
            Capture("end"); Clear();
        }

        // ---------------- item 4: rival challenges ----------------
        IEnumerator Challenges()
        {
            Buck.Enabled = true; Tour.Reload();
            Line($"CHALLENGES: fresh tour.json {!System.IO.File.Exists(Tour.FilePath)} ({Tour.FilePath}); {Challenge.All.Length} challenges; open: {string.Join(", ", Enumerable.Range(0, Challenge.All.Length).Select(i => $"{Challenge.All[i].key} {Challenge.Open(i)}"))} (Unlock all Tour stops {(Settings.UnlockAllTour ? "On" : "Off")})");
            bool keep = Settings.UnlockAllTour; Settings.UnlockAllTour = false;
            Line($"  with the setting Off and no stop cleared: {string.Join(" | ", Enumerable.Range(0, Challenge.All.Length).Select(Challenge.Row))}");
            Settings.UnlockAllTour = keep;
            int pass = 0, total = 0;
            for (int i = 0; i < Challenge.All.Length; i++)
                foreach (bool win in new[] { false, true })
                {
                    total++; var ch = Challenge.All[i]; scenario = $"challenge-{ch.key}-{(win ? "win" : "lose")}"; Clear();
                    Settings.Difficulty = win ? Difficulty.Easy : Difficulty.Normal;
                    int tries0 = Tour.ChallengeTries(i); bool won0 = Tour.ChallengeWon(i); int bl = Buck.Instance.Events.Count, cl = Caller.Instance.Lines.Count; float clT = Time.time;   // Round 14: the caller clears its list at each start: lines by time
                    var spoken0 = Driver.AllSpoken.Count;
                    game.ChallengeRun.Play(i); yield return null; yield return null;
                    yield return Until(() => !v.Holding || !game.ChallengeRun.Running, 60f);
                    Shot(.5f, "go"); float t0 = Time.time;
                    if (win) { var pl = DriveWith(""); pl.useDifficulty = false; pl.rival.boostMeter = .25f; pl.rival.boostStraight = 14f; } else custom = () => { input.sThrottle = 0; input.sBrake = 0; input.sSteer = 0; input.sVertical = 0; input.sBoost = false; };
                    yield return Until(() => !game.ChallengeRun.Running, 150f);
                    StopAi(); Capture("result"); Clear();
                    var steps = game.ChallengeRun.Steps.Skip(Math.Max(0, game.ChallengeRun.Steps.Count - 8)).ToList();
                    bool saved = Tour.ChallengeTries(i) == tries0 + 1 && (!win || Tour.ChallengeWon(i));
                    Tour.Reload(); bool reloaded = Tour.ChallengeTries(i) == tries0 + 1 && Tour.ChallengeWon(i) == (won0 || game.ChallengeRun.LastWon);
                    bool okResult = game.ChallengeRun.LastWon == win;
                    var buckSaid = Buck.Instance.Events.Skip(bl).Where(e => e.Contains(" said ") || e.Contains("scene")).ToList();
                    var barker = Caller.Instance.Lines.Where(l => l.t >= clT).Select(l => $"{l.pool}/{l.clip}").ToList();
                    var drivers = Driver.AllSpoken.Skip(spoken0).Where(s => s.Contains(" chintro") || s.Contains(" chwin") || s.Contains(" chlose")).ToList();
                    bool lines = barker.Any(x => x.StartsWith("ch")) && drivers.Count >= 2 && buckSaid.Count > 0;
                    bool ok = okResult && saved && reloaded && lines; if (ok) pass++;
                    Line($"  {ch.key} {(win ? "win" : "lose")} run {(ok ? "PASS" : "FAIL")}: result {(game.ChallengeRun.LastWon ? "won" : "lost")} by {game.ChallengeRun.LastMargin:0.0} s ({Challenge.Note}); saved {saved}, after a reload {reloaded} (tries {Tour.ChallengeTries(i)}, won {Tour.ChallengeWon(i)}, best {Tour.ChallengeBest(i):0.0} s); paint {ch.paint} unlocked {Paints.Unlocked(i + 1)}");
                    Line($"     lines: Buck {string.Join("; ", buckSaid)} | Barnaby {string.Join(", ", barker)} | drivers {string.Join("; ", drivers)}");
                    Line($"     steps: {string.Join(" / ", steps)}; menu after: {(game.MenuOpen ? game.MenuTitle : "closed")}");
                    if (game.MenuOpen) game.CloseMenu(); yield return Wait(.3f);
                    Save();
                }
            // the Garage's paint row cycles through the won paints, saved per run
            game.ShowGarage(); yield return null; string row0 = game.MenuItems.FirstOrDefault(x => x.StartsWith("Paint")); int pi = game.MenuItems.FindIndex(x => x.StartsWith("Paint"));
            game.Choose(pi); yield return null; string row1 = game.MenuItems.FirstOrDefault(x => x.StartsWith("Paint")); Capture("garage-paint"); game.Choose(pi); yield return null; game.Choose(pi); yield return null; game.CloseMenu();
            Line($"  Garage paint: \"{row0}\" -> \"{row1}\"");
            Line($"CHALLENGES {(pass == total ? "PASS" : "FAIL")}: {pass} of {total} runs (each challenge won and lost: result, tour.json saved and read back, Buck + the rival + Barnaby lines)");
            Buck.Enabled = false; Save();
        }

        // ---------------- item 5: Graphics presets ----------------
        IEnumerator Presets()
        {
            float secs = ArgFloat("-seconds", 40f);
            Line($"GRAPHICS PRESETS: frame times per preset, both courses, one player and 2 players (PC: {SystemInfo.graphicsDeviceName}, every frame rendered at {W}x{H} with the GPU waited for; batch mode, so CPU + GPU of the render, not the Deck); Steam Deck detected here: {GraphicsPreset.IsSteamDeck}; Auto here = {GraphicsPreset.Names[GraphicsPreset.Base]}");
            foreach (bool split in new[] { false, true })
                for (int course = 0; course < game.Courses.Count; course++)   // Round 15: all three courses
                    for (int lvl = 0; lvl < 4; lvl++)
                    {
                        scenario = $"preset-{GraphicsPreset.Names[lvl]}-{course}{(split ? "-split" : "")}"; Clear(); GraphicsPreset.Override = lvl;
                        if (split)
                        {
                            BeginAt(course, 0); yield return Wait(.2f); Pads(); SplitScreen.P1Device = pad1; SplitScreen.P2Device = pad2; Settings.Driver2 = 1; SplitScreen.AiCount = 2;
                            game.StartSplit(true); yield return null; input.Scripted = true; var p2 = SplitScreen.Session.P2; var pl2 = p2.gameObject.AddComponent<RivalPilot>(); pl2.AlwaysOn = true; pl2.useDifficulty = false;
                        }
                        else { game.StartCourse(course, true); GraphicsPreset.Apply(); }
                        yield return Until(() => !v.Holding, 8f);
                        DriveWith("").useDifficulty = false; render.Clear(); measure = true; int f0 = frames.Count; float t0 = Time.time;
                        while (Time.time - t0 < secs) { segNow = v.Course.segments[Mathf.Min(v.Splits.Count, v.Course.segments.Count - 1)].name; yield return null; }
                        measure = false; StopAi();
                        var ms = render.Select(r => r.ms).OrderBy(x => x).ToList(); var fr = frames.Skip(f0).Where(f => !f.shot).Select(f => f.dt * 1000).OrderBy(x => x).ToList();
                        float P(List<float> l, float q) => l.Count > 0 ? l[Mathf.Clamp((int)(l.Count * q), 0, l.Count - 1)] : 0;
                        long tris = render.Count > 0 ? (long)render.Average(r => (double)r.tris) : 0;
                        Line($"  {(split ? "2 players" : "1 player ")} {game.Courses[course].shortName,-15} {GraphicsPreset.Names[lvl],-6}: render mean {ms.DefaultIfEmpty(0).Average():0.00} ms, 95th {P(ms, .95f):0.00}, 99th {P(ms, .99f):0.00} ms; whole frame mean {fr.DefaultIfEmpty(0).Average():0.00} ms, 95th {P(fr, .95f):0.00} ms; triangles {tris / 1000:0}k; {GraphicsPreset.Applied}");
                        if (lvl == 0 || lvl == 3) Capture($"{GraphicsPreset.Names[lvl]}-{course}{(split ? "-split" : "")}");
                        if (split) { game.EndSplit(false); yield return null; }
                        Save();
                    }
            GraphicsPreset.Override = -2; GraphicsPreset.Apply();
            Line("GRAPHICS PRESETS done (Dan checks the Deck: Menu > Settings > Graphics, Auto should say \"Deck, Steam Deck\")");
        }

        // ---------------- item 5c: Coaster Coast under the boardwalk and the pier ----------------
        IEnumerator UnderPierCheck()
        {
            game.StartCourse(1, false); var c = game.Courses[1]; float M1(string n) => CoasterCoast.Marks[n];
            Line($"UNDER THE PIER (Coaster Coast{(CoasterCoast.OldPier ? ", BUILD 14's: -oldPier" : "")}): the flume's channel under the boardwalk to the sea, and the dive under the fishing pier to the splash pool");
            // pictures along it, player view
            var pics = new (string n, float p, Mode m, float y)[] { ("channel-in", M1("cross") - 60, Mode.Boat, -1), ("under-boardwalk", M1("cross"), Mode.Boat, -1), ("channel-out", M1("seaOut") - 30, Mode.Boat, -1),
                ("dive-down", M1("diveZone") + 40, Mode.Dive, 99), ("pier-entry", M1("portal") - 15, Mode.Dive, 99), ("under-pier", M1("portal") + 90, Mode.Dive, 99), ("pier-east", M1("portal") + 200, Mode.Dive, 99),
                ("carousel", M1("carousel"), Mode.Dive, 99), ("octopus", M1("octo"), Mode.Dive, 99), ("exit-ahead", M1("poolEdge") - 35, Mode.Dive, 99) };
            foreach (var (n, p, m, y) in pics)
            {
                scenario = "pier-" + n; Clear(); coast = true; game.StartCourse(1, false); yield return Until(() => !v.Holding, 6f);
                int a = 0; for (int i = 0; i < c.anchors.Count; i++) if (c.anchors[i].progress < p - 5) a = i; v.Begin(c, c.anchors[a]);
                var q = LineAt(c, p, out var r); if (y < 90) q.y = .3f; v.DebugPlace(q, r, m, 15f, p); yield return Wait(.6f); Capture(n); Clear();
            }
            // wander off: from points along it, driven straight off at 50 / 90 deg left and right for 2.5 s (or down / up), then the human-like
            // pilot by the HUD for up to 30 s: back on the route, or recovered forward; never stuck, never reset back past where it was
            var starts = new List<(float p, Mode m)>(); for (float p = M1("cross") - 80; p < M1("seaOut") + 40; p += 50) starts.Add((p, Mode.Boat));
            for (float p = M1("diveZone") + 50; p < M1("poolEdge") - 20; p += 55) starts.Add((p, Mode.Dive));
            var dirs = new (string n, float yaw, float vert)[] { ("left50", -50, 0), ("left90", -90, 0), ("right50", 50, 0), ("right90", 90, 0), ("down", 0, -1), ("up", 0, 1) };
            int ok = 0, total = 0, back = 0, stuck = 0, fwd = 0; var bad = new List<string>(); float tSum = 0;
            foreach (var (p, m) in starts)
                foreach (var (dn, yaw, vert) in dirs)
                {
                    if (m == Mode.Boat && vert != 0) continue;
                    total++; scenario = $"wander-{p:0}-{dn}"; Clear(); game.StartCourse(1, false); yield return Until(() => !v.Holding, 6f);
                    int a = 0; for (int i = 0; i < c.anchors.Count; i++) if (c.anchors[i].progress < p - 5) a = i; v.Begin(c, c.anchors[a]);
                    var q = LineAt(c, p, out var r); if (m == Mode.Boat) q.y = .3f; v.DebugPlace(q, r * Quaternion.Euler(0, yaw, 0), m, m == Mode.Boat ? 30f : 22f, p);
                    float best0 = v.BestProgress, t0 = Time.time; int r0 = v.Resets;
                    custom = () => { input.sThrottle = 1; input.sBrake = 0; input.sBoost = false; input.sSteer = 0; input.sVertical = vert; };
                    yield return Wait(2.5f);
                    var h = NewHuman(900 + total); h.keys = total % 2 == 0; UseHuman(h); float tHud = Time.time; bool rejoined = false;
                    while (Time.time - tHud < 30f)
                    {
                        if (v.Resets > r0) break;
                        if (v.OffLine < 12f && v.Progress > best0 + 15f) { rejoined = true; break; }
                        yield return null;
                    }
                    string res; bool good;
                    if (v.Resets > r0) { float rel = v.Progress - p; bool forward = rel >= -80f; res = $"reset ({v.LastResetReason}) {v.LastResetHow}, restarted {rel:+0;-0} m from where it wandered off{(forward ? "" : " (BACK)")}"; good = forward; if (forward) fwd++; else back++; }
                    else if (rejoined) { res = $"back on the route after {Time.time - t0:0.0} s"; good = true; tSum += Time.time - t0; }
                    else { res = $"STUCK at ({v.Body.position.x - CoasterCoast.O.x:0},{v.Body.position.y:0},{v.Body.position.z:0}), {v.OffLine:0} m off, under {Under(v)}"; good = false; stuck++; }
                    if (good) ok++; else { bad.Add($"{p:0} {dn}: {res}"); Capture($"wander-{p:0}-{dn}"); }
                    Line($"  {m,-4} from {p:0} m, {dn,-7}: {res}");
                    Clear(); Save();
                }
            Line($"WANDER {(ok == total ? "PASS" : "FAIL")}: {ok} of {total} back on the route by the HUD or recovered no more than 80 m behind where they wandered off ({fwd} recoveries), {stuck} stuck, {back} reset further back; {(bad.Count > 0 ? "problems: " + string.Join("; ", bad) : "")}");
            // the human-like pilot through the whole stretch without help: from the anchor before the flume's end to G8
            int hok = 0, ht = ArgInt("-tries", 6);
            for (int k = 0; k < ht; k++)
            {
                scenario = $"pier-human-{k + 1}"; Clear(); game.StartCourse(1, false); yield return Until(() => !v.Holding, 6f);
                v.Begin(c, c.anchors[AnchorNear(M1("cross") - 120)]); var h = NewHuman(950 + k); UseHuman(h); int r0 = v.Resets; float t0 = Time.time;
                yield return Until(() => v.GatesPassed >= 9 || Time.time - t0 > 90f, 91f);
                bool through = v.GatesPassed >= 9 && v.Resets == r0; if (through) hok++;
                Line($"  human-like try {k + 1} {(through ? "THROUGH" : "NOT THROUGH")}: {(h.keys ? "keys" : "stick")}, late {h.delay:0.00} s: {(v.GatesPassed >= 9 ? $"G8 after {Time.time - t0:0.0} s" : $"gates {v.GatesPassed} at ({v.Body.position.x - CoasterCoast.O.x:0},{v.Body.position.y:0},{v.Body.position.z:0})")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason}, {v.LastResetHow})" : "")}");
                Clear(); Save();
            }
            Line($"UNDER THE PIER human-like {(hok == ht ? "PASS" : "FAIL")}: {hok} of {ht} from the flume's end to the splash pool (G8) by the HUD alone with no reset");
            Save();
        }

        IEnumerator Round13(string which)
        {
            if (which == "underpier") { yield return UnderPierCheck(); yield break; }
            bool all = which == "round13";
            if (all || which == "choice13") yield return Choice13();
            if (all || which == "humancliff") yield return HumanCliff();
            if (all || which == "branches13") yield return Branches13();
            if (all || which == "challenges") yield return Challenges();
            if (all || which == "presets") yield return Presets();
        }
    }
}
