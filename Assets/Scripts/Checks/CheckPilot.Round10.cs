using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 10.
    //   Item 2: the Round 1 checks that used the transition strip and the four mode lanes now run on Paradise Plunge (the strip and lanes are
    //   gone from the game): strip = the second half of the lap from the climb's last anchor (every change once: Drive -> Fly -> Boat -> Dive
    //   -> Boat -> Drive); lanes = each form on a stretch of the lap (weave, drift / carve, boost); E1-E6 at the same places on the lap
    //   (slow ramp, boosted / nose-down lagoon entry, reset mid-change, the dive zone twice, pool -> road); slowfps as a plane high over the
    //   valley; the invert check's flight / dive at a plane / sub anchor (BeginForm).
    //   Item 1: strand = scripted bad landings off every jump, the loop and the water ramp: 0 strandings (each case drives back onto the road,
    //   lands on it, or is clearly reset by water / a fall); edges = pictures of each changed road edge ("-oldEdges": build 11's layout).
    public sealed partial class CheckPilot
    {
        // a time trial started at the first anchor of a form after the start (no countdown)
        void BeginForm(Mode m)
        {
            var c = game.Courses[0]; game.StartCourse(0, false);
            int k = c.anchors.FindIndex(a => a.mode == m && a.progress > 100); if (k < 0) k = 0;
            v.Begin(c, c.anchors[k]);
            // the sub: 5 m down in the Gateway pool at its first anchor (the pool's floor is ~11 m down: at -10 the floor clearance held the
            // nose up and invert-on read -0.4 deg; the anchor itself is just under the surface, which holds the nose down)
            if (m == Mode.Dive) { var a = c.anchors[k]; var p = a.transform.position; p.y = -5f; v.DebugPlace(p, a.transform.rotation, Mode.Dive, 12f, a.progress); }
        }
        int AnchorIndexNear(float progress) { game.StartCourse(0, false); return AnchorNear(progress); }

        // ---- the Round 1 "strip": every change once, on the lap ----
        IEnumerator Strip()
        {
            scenario = "strip"; Clear(); smart = true; BeginAt(0, AnchorIndexNear(Mk("rampStart") - 35)); Shot(.5f, "start");
            yield return Until(() => v.Finished, 180f);
            Shot(0, "finish"); yield return Wait(.3f);
            var cs = Of("strip"); var names = string.Join(", ", cs.ConvertAll(x => x.what));
            bool order = names == "Drive->Fly, Fly->Boat, Boat->Dive, Dive->Boat, Boat->Drive";
            bool pass = v.Finished && v.Resets == 0 && cs.Count == 5 && order;
            Line($"STRIP (on Paradise Plunge, from the climb's last anchor to the finish) {(pass ? "PASS" : "FAIL")}: finished {v.Finished} at {v.FinishTime:0.0} s, gates {v.GatesPassed}/{v.Course.gates.Count}, resets {v.Resets}{(v.Resets > 0 ? " (" + v.LastResetReason + ")" : "")}, changes {cs.Count}: {names}");
        }

        // ---- the Round 1 "lanes": each form on a stretch of the lap ----
        IEnumerator Lanes()
        {
            foreach (var (name, mode, mark) in new[] { ("drive-lane", Mode.Drive, Mk("climbMid")), ("boat-lane", Mode.Boat, Mk("splash") + 70), ("fly-lane", Mode.Fly, Mk("lip") + 90), ("dive-lane", Mode.Dive, Mk("portal") + 45) })
            {
                scenario = name; Clear(); BeginAt(0, AnchorIndexNear(mark)); float t0 = Time.time; var m0 = v.Mode; float top = 0; bool slid = false; int r0 = v.Resets;
                steerWeave = () => { float t = Time.time - t0; return t > 1 && t < 5 ? Mathf.Sin(t * 1.4f) * .5f : 0; };
                if (mode == Mode.Fly || mode == Mode.Dive) { var c = v.Course; yOverride = () => { c.Project(v.Body.position, v.Progress, out var at, out _); return at.y + Mathf.Sin((Time.time - t0) * .6f) * (mode == Mode.Dive ? 2f : 3f); }; }
                Shot(4f, "b");
                while (Time.time - t0 < 8f)
                {
                    float t = Time.time - t0; secondary = t > 2 && t < 4; boost = t > 5.5f && t < 7.5f;
                    top = Mathf.Max(top, v.Speed); slid |= v.Drifting || v.Carving; yield return null;
                }
                Line($"LANE {name} (on Paradise Plunge at {mark:0} m): form {m0} -> {v.Mode}, top speed {top:0.0} m/s (tuning top {(m0 == Mode.Drive ? game.Drive.topSpeed : m0 == Mode.Boat ? game.Boat.topSpeed : m0 == Mode.Fly ? game.Fly.topSpeed : game.Dive.topSpeed):0} / boost {(m0 == Mode.Drive ? game.Drive.boostTopSpeed : m0 == Mode.Boat ? game.Boat.boostTopSpeed : m0 == Mode.Fly ? game.Fly.boostTopSpeed : game.Dive.boostTopSpeed):0}), {(m0 == Mode.Drive || m0 == Mode.Boat ? (slid ? "drift/carve used" : "drift/carve NOT seen") : "climb/dive weave")}, resets {v.Resets - r0}{(v.Resets > r0 ? " (" + v.LastResetReason + ")" : "")}");
            }
        }

        // ---- the Round 1 edge cases, at the same places on the lap ----
        IEnumerator Edges()
        {
            int ramp = AnchorIndexNear(Mk("rampStart") - 35), arch = AnchorIndexNear(Mk("arch") + 90), river = AnchorIndexNear(Mk("G5") + 12), vault = AnchorIndexNear(Mk("G7") - 30);
            scenario = "e1-slow-ramp"; Clear(); BeginAt(0, ramp); holdSpeed = 10; float speedAtLip;
            yield return Until(() => v.Mode == Mode.Fly, 30f); speedAtLip = v.Speed; holdSpeed = -1;
            yield return Until(() => v.Mode == Mode.Boat || v.Resets > 0 || v.Progress > Mk("splash") + 200, 80f); yield return Wait(1f);
            var c1 = Of(scenario);
            Line($"E1 low-speed ramp entry {(c1.Exists(x => x.what == "Drive->Fly") && c1.Exists(x => x.what == "Fly->Boat") && v.Resets == 0 ? "PASS" : "FAIL")}: ramp at 10 m/s, flight entered at {speedAtLip:0.0} m/s, reached the lagoon in {v.Mode}, resets {v.Resets}{(v.Resets > 0 ? " (" + v.LastResetReason + ")" : "")}");

            scenario = "e2-boost-water"; Clear(); BeginAt(0, arch); boost = true; float boatSpeed, upright = -1, changeAt;
            yield return Until(() => v.Mode == Mode.Boat || v.Resets > 0, 40f); changeAt = Time.time; boatSpeed = v.Speed;
            while (Time.time - changeAt < 4f) { if (upright < 0 && v.transform.up.y > .9f && v.OnWater) upright = Time.time - changeAt; yield return null; }
            boost = false;
            Line($"E2 boosted water entry {(v.Mode == Mode.Boat && v.Resets == 0 && upright >= 0 && upright < 2f && boatSpeed <= game.Boat.entrySpeed.y + .5f ? "PASS" : "FAIL")}: boat speed {boatSpeed:0.0} m/s (entry cap {game.Boat.entrySpeed.y:0}), level after {upright:0.00} s, resets {v.Resets}");

            scenario = "e3-nose-down"; Clear(); BeginAt(0, arch); upright = -1;
            yield return Until(() => (TransformZone.InCorridor(v.Body.position) && v.Body.position.y < 9f) || v.Resets > 0, 40f);   // inside the landing corridor, low (the strip: 3 m before it)
            v.debugEntryPitch = -50f;
            yield return Until(() => v.Mode == Mode.Boat || v.Resets > 0, 15f); v.debugEntryPitch = float.NaN;
            changeAt = Time.time;
            while (Time.time - changeAt < 5f) { if (upright < 0 && v.transform.up.y > .9f) upright = Time.time - changeAt; yield return null; }
            Line($"E3 steep nose-down water entry {(v.Mode == Mode.Boat && v.Resets == 0 && upright >= 0 && upright < 1.5f ? "PASS" : "FAIL")}: -50 deg, level after {upright:0.00} s, resets {v.Resets}");

            scenario = "e4-reset-mid"; Clear(); BeginAt(0, ramp);
            yield return Until(() => v.Changing && v.Mode == Mode.Fly && Time.time - v.ChangeStartedAt >= .25f, 30f);
            float tIn = Time.time - v.ChangeStartedAt; input.RequestReset();
            yield return Wait(.25f);
            var anchor = v.Course.anchors[ramp];
            bool ok4 = v.Mode == Mode.Drive && !v.Changing && v.Forms.Done && v.Forms.Current == Mode.Drive && Vector3.Distance(v.Body.position, anchor.transform.position) < 6f && v.GraceUntil > Time.time;
            int flies = Of(scenario).FindAll(x => x.what == "Drive->Fly").Count;
            yield return Until(() => Of(scenario).FindAll(x => x.what == "Drive->Fly").Count > flies, 20f);
            bool again = Of(scenario).FindAll(x => x.what == "Drive->Fly").Count > flies; yield return Wait(1f);
            Line($"E4 reset during a change {(ok4 && again ? "PASS" : "FAIL")}: reset {tIn:0.00} s into Drive->Fly; back in {v.Mode} at the anchor ({Vector3.Distance(v.Body.position, anchor.transform.position):0.0} m); wing zone fired again: {again}");

            scenario = "e5-twice"; Clear(); BeginAt(0, river);
            float dz = Mk("diveZone");
            yield return Until(() => v.Progress >= dz + 14 || v.Resets > 0, 40f);
            reverse = true; yOverride = () => -2f;
            yield return Until(() => v.Progress <= dz - 16 || v.Resets > 0, 20f);
            reverse = false;
            yield return Until(() => v.Progress >= Mk("portal") + 25 || v.Resets > 0, 25f); yOverride = null;
            var c5 = Of(scenario);
            Line($"E5 same trigger twice {(c5.Count == 1 && c5[0].what == "Boat->Dive" && v.Mode == Mode.Dive && v.Resets == 0 ? "PASS" : "FAIL")}: through the dive zone, back out of it under water (-2 m, inside the zone) and through it again: changes {c5.Count} ({string.Join(", ", c5.ConvertAll(x => x.what))}), form {v.Mode}, resets {v.Resets}");

            scenario = "e6-surface-road"; Clear(); BeginAt(0, vault);
            yield return Until(() => (v.Mode == Mode.Drive && v.GroundedWheels > 0) || v.Resets > 0, 40f); yield return Wait(.5f);
            var c6 = Of(scenario); var d = c6.Find(x => x.what == "Boat->Drive");
            float doneAt = d != null && d.took > 0 ? d.at + d.took : -1, touch = v.FirstWheelContactAt;
            Line($"E6 surfacing pool -> road {(c6.Exists(x => x.what == "Dive->Boat") && d != null && doneAt > 0 && touch > 0 && doneAt <= touch && v.Resets == 0 ? "PASS" : "FAIL")}: changes {string.Join(", ", c6.ConvertAll(x => x.what))}; Boat->Drive done {(doneAt > 0 && touch > 0 ? $"{touch - doneAt:0.00} s before the wheels touched" : "n/a")}, resets {v.Resets}");
        }

        // ---- slow frame rate: the same 6 s of game time at 3 frame rates (a plane flying straight, high over the valley) ----
        IEnumerator SlowFps()
        {
            var res = new List<(float fps, float dist, float gameT, float wallT)>(); int k0 = AnchorIndexNear(Mk("lip") + 90);
            foreach (int sleep in new[] { 0, 40, 90 })
            {
                scenario = $"slowfps-{sleep}"; Clear(); BeginAt(0, k0); straight = true; straightSteer = 0; throttleCap = 1; yOverride = () => 112f; yield return Wait(.2f);
                var start = v.Body.position; float g0 = Time.time, w0 = Time.realtimeSinceStartup; int f0 = Time.frameCount; sleepMs = sleep; float worst = 0, sumU = 0, sumD = 0;
                while (Time.time - g0 < 6f) { worst = Mathf.Max(worst, Time.unscaledDeltaTime); sumU += Time.unscaledDeltaTime; sumD += Time.deltaTime; yield return null; }
                sleepMs = 0;
                float gt = Time.time - g0, wt = Time.realtimeSinceStartup - w0; res.Add(((Time.frameCount - f0) / wt, Vector3.Distance(start, v.Body.position), gt, wt)); Line($"  slowfps sleep {sleep} ms: worst frame {worst * 1000:0} ms, sum of unscaled frame times {sumU:0.00} s, of game frame times {sumD:0.00} s, resets {v.Resets}");
            }
            float d0 = res[0].dist; bool pass = true; var parts = new List<string>();
            foreach (var x in res) { float ratio = x.dist / d0, pace = x.gameT / x.wallT; pass &= Mathf.Abs(ratio - 1) < .02f && pace > .97f; parts.Add($"{x.fps:0} fps: {x.dist:0.0} m in {x.gameT:0.00} s game time ({x.wallT:0.00} s real, {pace:0.00} x), {ratio * 100:0.0} %"); }
            Line($"SLOW FPS {(pass ? "PASS" : "FAIL")}: full throttle, a plane flying straight 112 m over the valley for 6 s of game time; {string.Join("; ", parts)}. Fixed step {Time.fixedDeltaTime * 1000:0.00} ms, maximum catch-up {Time.maximumDeltaTime:0.00} s per frame");
        }

        // ---------------- item 1: the stranding sweep ----------------
        struct StrandCase { public string name; public float place, lat, yaw, speed; public bool boost, wings, boat; public float lip, land; }
        readonly List<string> strandRows = new List<string>();

        static Vector3 OnLine(Course c, float p, float lat, float yaw, out Quaternion rot, float lift = 0)
        {
            var at = c.PointAt(p, out var d); var f = Vector3.ProjectOnPlane(d, Vector3.up).normalized; var r = Vector3.Cross(Vector3.up, f);
            rot = Quaternion.LookRotation(Quaternion.Euler(0, yaw, 0) * f, Vector3.up); return at + r * lat + Vector3.up * lift;
        }

        IEnumerator StrandCheck()
        {
            var c = game.Courses[0]; bool old = ParadisePlunge.OldEdges;
            Line($"STRAND SWEEP ({(old ? "BUILD 11 EDGES (-oldEdges)" : "Round 10 edges")}): each case placed before a jump / the loop / the water ramp, placed on the kicker 12 m before the lip (the loop: 150 m before it; the ramp: in the pool) at an offset and angle, driven straight (steer held) off the lip, then the pilot drives on for 15 s");
            Line("  outcome: ON ROAD (landed on it), DROVE BACK (off the road, back on it and 120 m on; off-road = the metres of the lap driven off it), RESET (water / a fall / the loop rule: clearly recovered), STRANDED (not back on the road in 20 s or 600 m, round the outside of the loop, or reset as stuck / no progress)");
            var cases = new List<StrandCase>();
            foreach (var j in ParadisePlunge.Jumps)
            {
                float lip = j.lip, land = j.lip + j.gap;
                foreach (var (n, lat, yaw, sp, bo) in new[] { ("centre", 0f, 0f, 62f, false), ("left", -6f, -9f, 62f, false), ("right", 6f, 9f, 62f, false), ("wide-left", -9f, -18f, 66f, false), ("wide-right", 9f, 18f, 66f, false), ("long", 0f, 4f, 80f, true), ("short", 0f, 0f, 30f, false) })
                    cases.Add(new StrandCase { name = $"{j.name} {n}", place = lip - 12, lat = lat, yaw = yaw, speed = sp, boost = bo, lip = lip, land = land });
                if (j.choice)
                    foreach (var (n, lat, yaw) in new[] { ("fly centre", 0f, 0f), ("fly left", -6f, -10f), ("fly right", 6f, 10f) })
                        cases.Add(new StrandCase { name = $"{j.name} {n}", place = lip - 12, lat = lat, yaw = yaw, speed = 62, wings = true, lip = lip, land = Mk("G1") - 60 });
            }
            float li = Mk("loopIn"), lo = Mk("loopOut");
            foreach (var (n, lat, yaw, sp) in new[] { ("full", 0f, 0f, 70f), ("slow", 0f, 0f, 34f), ("very slow", 0f, 0f, 15f), ("off-centre left", -5f, 0f, 60f), ("off-centre right", 5f, 0f, 60f), ("wide left", -22f, 0f, 60f), ("wide right", 24f, 0f, 60f), ("angled out left", -14f, -10f, 60f), ("angled out right", 14f, 10f, 60f) })
                cases.Add(new StrandCase { name = $"Loop {n}", place = li - 150, lat = lat, yaw = yaw, speed = sp, lip = li, land = lo + 10 });
            foreach (var (n, lat, yaw) in new[] { ("centre", 0f, 0f), ("left", -6f, -12f), ("right", 6f, 12f) })
                cases.Add(new StrandCase { name = $"Water ramp {n}", place = Mk("G8") - 10, lat = lat, yaw = yaw, speed = 30, boat = true, lip = Mk("rampLip"), land = Mk("landing") });

            int strandings = 0, n0 = 0; var counts = new Dictionary<string, int>();
            foreach (var k in cases)
            {
                scenario = "strand-" + k.name.ToLower().Replace(' ', '-'); Clear(); game.StartCourse(0, false);
                int ai = 0; for (int i = 0; i < c.anchors.Count; i++) if (c.anchors[i].progress < k.place + (k.boat ? 6 : -5)) ai = i;
                v.Begin(c, c.anchors[ai]); yield return Wait(.1f);
                var pos = OnLine(c, k.place, k.lat, k.yaw, out var rot, k.boat ? -.3f : ParadisePlunge.Lift(k.place) + .1f); if (k.boat) pos.y = ParadisePlunge.Water + .6f;
                v.DebugPlace(pos, rot, k.boat ? Mode.Boat : Mode.Drive, k.speed, k.place);
                int r0 = v.Resets; straight = true; straightSteer = 0; holdSpeed = k.boost ? -1 : k.speed; boost = k.boost; throttleCap = 1;
                float t0 = Time.time; bool air = false, landed = false, wingsSent = false; float landOff = 0, landProg = -1, landAt = -1; string outcome = ""; bool shot = false;
                float offMetres = 0, lastProg = -1; int loops0 = v.Loops; bool beforeLoop = k.place < li; string why = "";
                while (Time.time - t0 < 40f)
                {
                    if (v.Resets > r0) break;
                    if (k.wings && !wingsSent && v.WingsOpen && v.GroundedWheels == 0) { input.sWings = true; wingsSent = true; }
                    if (!air && v.Progress > k.lip - 2 && (v.GroundedWheels == 0 || v.Mode != Mode.Drive || v.OnLoop)) air = true;
                    if (air && v.Mode == Mode.Fly && straight) { straight = false; holdSpeed = -1; smart = true; }   // the wings are out: fly the line
                    if (air && !landed && ((v.Mode == Mode.Drive && v.GroundedWheels > 0 && !v.OnLoop) || (k.lip == li && v.Progress > lo + 2 && !v.OnLoop)))
                    {
                        landed = true; landAt = Time.time; landProg = v.Progress; landOff = v.OffLine;
                        straight = false; holdSpeed = -1; boost = false; smart = true;   // now the pilot drives back the way a player would
                    }
                    if (landed && !shot && Time.time - landAt > .3f) { shot = true; Capture("landed"); }
                    if (landed)
                    {
                        float half = RoadHalfOn(c, v.Progress);
                        bool onRoad = (v.Mode == Mode.Drive && v.OffLine <= half + .5f) || v.OnLoop;
                        // Round 10: the metres of the lap driven off the road since the landing (build 11: beside the barriers, often hundreds,
                        // round the outside of the loop) and the loop bypassed
                        if (lastProg >= 0 && !onRoad && v.Progress > lastProg) offMetres += v.Progress - lastProg; lastProg = v.Progress;
                        if (offMetres > 600f) { outcome = "STRANDED"; why = $"{offMetres:0} m off the road"; break; }
                        if (beforeLoop && v.Progress > lo + 5 && v.Loops == loops0) { outcome = "STRANDED"; why = "went round the outside of the loop"; break; }
                        if (onRoad && v.Progress > Mathf.Max(landProg, k.land) + 120) { outcome = landOff <= RoadHalfOn(c, landProg) + .5f ? "ON ROAD" : "DROVE BACK"; break; }
                        if (Time.time - landAt > 20f) { outcome = "STRANDED"; why = "not back on the road in 20 s"; break; }
                    }
                    yield return null;
                }
                if (outcome == "" && v.Resets > r0)
                {
                    string rr = v.LastResetReason;
                    bool clear = rr.Contains("water") || rr.Contains("fell") || rr.Contains("loop") || rr.Contains("out of bounds");
                    outcome = clear ? "RESET" : "STRANDED";
                    if (!shot) Capture("reset");
                }
                if (outcome == "") outcome = landed ? "STRANDED" : "NEVER LANDED";
                if (outcome.StartsWith("STRANDED")) { strandings++; Capture("stranded"); }
                var key = outcome.Split(' ')[0] + (outcome.StartsWith("DROVE") ? " BACK" : outcome.StartsWith("ON ") ? " ROAD" : ""); counts[key] = counts.TryGetValue(key, out var cc) ? cc + 1 : 1; n0++;
                if (why != "") outcome += $" ({why})";
                string row = $"  {k.name,-26} {outcome,-12} off-road {offMetres,4:0} m, landed {(landed ? $"{landOff:0.0} m off the line (road half {RoadHalfOn(c, landProg):0.0})" : "-"),-38} resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, now {v.Mode} at {v.Progress:0} m, {v.OffLine:0.0} m off";
                Line(row); strandRows.Add(row);
                Clear();
            }
            Line($"STRAND SWEEP {(strandings == 0 ? "PASS" : "FAIL")}: {n0} cases, {strandings} strandings; {string.Join(", ", counts.Select(kv => $"{kv.Key} {kv.Value}"))}");
        }
        static float RoadHalfOn(Course c, float p) => ParadisePlunge.RoadHalfAt(p);

        // ---- item 1: pictures of each changed edge, from the same camera spots (run once with -oldEdges for the build 11 "before") ----
        IEnumerator EdgePictures()
        {
            var c = game.Courses[0]; game.StartCourse(0, false); yield return Wait(.5f); scenario = ParadisePlunge.OldEdges ? "edges-before" : "edges-after";
            var camGo = new GameObject("edge cam"); var ec = camGo.AddComponent<Camera>(); ec.enabled = false; ec.CopyFrom(cam); ec.targetTexture = null; ec.fieldOfView = 70; ec.farClipPlane = 2000;
            foreach (var (name, p, lat, h, back) in new[] {
                ("creek-landing", Mk("jA") + 40 + 30, 22f, 9f, 40f), ("promenade-mid", Mk("promMid"), -24f, 8f, 50f), ("loop-approach", Mk("loopIn") - 130, 0f, 14f, 60f),
                ("loop-approach-side", Mk("loopIn") - 90, 45f, 18f, 30f), ("lake-landing", Mk("jC") + 60, -26f, 9f, 40f), ("canyon-mouth", ParadisePlunge.CanyonFrom - 30, 20f, 10f, 40f),
                ("climb-valley-side", Mk("climbMid") - 40, 30f, 6f, 60f), ("finishing-road", Mk("G9") + 40, 20f, 8f, 45f) })
            {
                var at = c.PointAt(p, out var d); var f = Vector3.ProjectOnPlane(d, Vector3.up).normalized; var r = Vector3.Cross(Vector3.up, f);
                var eye = at - f * back + r * lat + Vector3.up * h; camGo.transform.position = eye; camGo.transform.LookAt(at + f * 30 + Vector3.up * 1f);
                Capture(name, ec); yield return null;
            }
            Destroy(camGo);
            Line($"EDGE PICTURES ({scenario}): creek landing, promenade, loop approach (front and side), Lake Jump landing, canyon mouth, the climb's valley side, finishing road");
        }
    }
}
