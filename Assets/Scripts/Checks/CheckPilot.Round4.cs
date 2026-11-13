using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 4 checks (built exe, -batchmode -mute, no window):
    //   scan        item 3: every collider inside the route's clear width along the whole lap (and standing signs / gate posts), pictures of
    //               the start line
    //   walls       item 1: scripted hits at 10 / 25 / 45 deg in each form (speed kept, no stop, no spin, no reset) and 3 s held against a wall
    //   nearmiss    item 6: a near miss at a road, flight, water and pool gate (counted + penalty), a far miss and a wrong-form pass
    //               (rejected), and the most a near miss could save anywhere (against the penalty)
    //   starts      item 4: 25 race starts, resets / unsticks / wall hits in the first 10 s
    //   difficulty  item 2: races per difficulty ("-races N", "-timeScale X"): AI lap times next to the check pilot's (no boost)
    //   tunnel      item 5: "-legs N" AI runs through the Coral Vault tunnel (anchor after the portal to G7), plus scrape runs
    //   round4 = scan + walls + nearmiss + starts + difficulty
    public sealed partial class CheckPilot
    {
        // ---------------- item 3: the scan ----------------
        IEnumerator Scan()
        {
            scenario = "scan"; Clear(); game.StartCourse(0, true); yield return Wait(.5f);
            var c = game.Courses[0];
            string Kind(float p) => p < Mk("lip") ? "road" : p < Mk("splash") ? "air" : p < Mk("diveZone") ? "water" : p < Mk("surface") ? "cave" : p < Mk("rampLip") ? "pool/ramp" : p < Mk("landing") - 12 ? "air" : "road";
            float RoadHalf(float p) => ParadisePlunge.RoadHalfAt(p);
            int mask = Physics.DefaultRaycastLayers & ~(1 << ShiftVehicle.VehicleLayer) & ~(1 << ShiftVehicle.GhostLayer);
            var found = new Dictionary<Collider, (float p0, float p1, float side, string kind)>();
            Line("SCAN: every collider inside the route's clear width, every 2 m along the line: road = between the edge lines (half width - 1 m), 0.3-3.8 m over the surface; " +
                 "air = 22 m wide (the flight rings' size), from 1.5 m under the line to 11 m over it; water = the buoy lane (+-13 m), 1.6 m under to 2.4 m over the surface; cave = +-14 m, +-2.5 m around the line's depth; pool / ramp = +-10 m, 0.2-3.2 m over the line");
            int n = 0;
            for (float p = 0; p < c.Length; p += 2f)
            {
                var at = c.PointAt(p, out var d); var flat = Vector3.ProjectOnPlane(d, Vector3.up); if (flat.sqrMagnitude < 1e-4f) continue; flat.Normalize();
                var right = Vector3.Cross(Vector3.up, flat); string k = Kind(p); Vector3 center = at, half; var rot = Quaternion.LookRotation(flat);
                switch (k)
                {
                    case "road": half = new Vector3(RoadHalf(p) - 1f, 1.75f, 1f); center = at + Vector3.up * (-.9f + .3f + 1.75f); break;
                    case "air": half = new Vector3(11, 6.25f, 1); center = at + Vector3.up * 4.75f; break;   // 1.5 m under the line to 11 m over it (what is lower is the ground under a jump)
                    case "water": half = new Vector3(13, 2, 1); center = new Vector3(at.x, .4f, at.z); break;
                    case "cave": half = new Vector3(ParadisePlunge.CaveHalf - 3, 2.5f, 1); break;
                    default: half = new Vector3(10, 1.5f, 1); center = at + Vector3.up * 1.7f; break;
                }
                foreach (var h in Physics.OverlapBox(center, half, rot, mask, QueryTriggerInteraction.Ignore))
                {
                    if (h.attachedRigidbody && h.attachedRigidbody.GetComponent<ShiftVehicle>()) continue;
                    float side = Vector3.Dot(h.bounds.center - at, right);
                    found[h] = found.TryGetValue(h, out var e) ? (e.p0, p, e.side, e.kind) : (p, p, side, k);
                }
                if (++n % 300 == 0) yield return null;
            }
            Line($"SCAN {(found.Count == 0 ? "PASS" : "FOUND")}: {found.Count} colliders inside the route's clear width{(found.Count > 0 ? ":" : "")}");
            foreach (var kv in found.OrderBy(x => x.Value.p0))
            {
                var h = kv.Key; var b = h.bounds;
                Line($"  {h.name} (under {(h.transform.parent ? h.transform.parent.name : "-")}, layer {h.gameObject.layer}): {kv.Value.kind} from {kv.Value.p0:0} to {kv.Value.p1:0} m, its centre {kv.Value.side:+0.0;-0.0} m from the line, bounds centre {b.center:F1} size {b.size:F1}");
            }
            // standing things without a collider: signs and gate posts beside the route
            foreach (var t in c.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("Sign ")))
            {
                float p = c.Project(t.position, -1, out var at, out var d); var right = Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(d, Vector3.up).normalized); float side = Vector3.Dot(t.position - at, right);
                string k = Kind(p); float clear = k == "road" ? RoadHalf(p) : k == "water" ? 15 : k == "pool/ramp" ? 12 : 20;
                Line($"  sign {t.name} at {p:0} m ({k}): {side:+0.0;-0.0} m from the line, the route's edge at {clear:0.0} m: {(Mathf.Abs(side) < clear + .5f ? "INSIDE the route" : "outside")}");
            }
            foreach (var m in GateMarker.All.Where(x => x.course == c))
            {
                float p = m.gate.progress; string k = Kind(p); float hx = m.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents.x;
                float clear = k == "road" ? RoadHalf(p) : k == "water" ? 15 : k == "pool/ramp" ? 12 : k == "cave" ? ParadisePlunge.CaveHalf : 11;
                Line($"  gate frame {m.gate.label} ({k}): posts at +-{hx:0.0} m, the route's edge at {clear:0.0} m: {(hx > clear ? "outside" : "INSIDE or at the edge")}");
            }
            // the start line: the grid as the player sees it, and from 40 m behind the grid in the middle of the road
            Capture("start-line-grid");
            var camGo = new GameObject("scan cam"); var sc = camGo.AddComponent<Camera>(); sc.enabled = false; sc.fieldOfView = 70; sc.farClipPlane = 2600; sc.clearFlags = CameraClearFlags.Skybox;
            var a0 = c.anchors[0]; var back = a0.transform.position - a0.transform.forward * 72f + Vector3.up * 2.2f;
            camGo.transform.SetPositionAndRotation(back, Quaternion.LookRotation(a0.transform.forward)); Capture("start-line-from-behind", sc);
            camGo.transform.SetPositionAndRotation(a0.transform.position + a0.transform.forward * 20f + Vector3.up * 2.2f, Quaternion.LookRotation(a0.transform.forward)); Capture("finish-line-ahead", sc);
            Destroy(camGo);
            Save();
        }

        // ---------------- item 1: walls ----------------
        struct WallCase { public string name, wall; public Mode form; public Vector3 point, n, along; public float speed, y, prog; public bool hold; }
        IEnumerator Walls()
        {
            var c = game.Courses[0]; game.StartCourse(0, false); yield return Wait(.3f);
            Vector3 R(float p) { c.PointAt(p, out var d); return Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(d, Vector3.up).normalized); }
            Vector3 F(float p) { c.PointAt(p, out var d); return Vector3.ProjectOnPlane(d, Vector3.up).normalized; }
            float pD = Mk("G9") + 70, pB = (Mk("bend2") + Mk("mangMid")) * .5f, pA = Mk("arch"), pC = Mk("vaultStart") + 15;
            var cases = new List<WallCase>
            {
                new WallCase { name = "drive", wall = "finishing road barrier", form = Mode.Drive, point = c.PointAt(pD, out _) + R(pD) * 13f, n = -R(pD), along = F(pD), speed = 45, prog = pD, hold = true },
                new WallCase { name = "boat", wall = "Mangrove river bank", form = Mode.Boat, point = c.PointAt(pB, out _) + R(pB) * 20f, n = -R(pB), along = F(pB), speed = 35, prog = pB, hold = true },
                new WallCase { name = "fly", wall = "Sky Arch pillar", form = Mode.Fly, point = new Vector3(c.PointAt(pA, out _).x, 52, c.PointAt(pA, out _).z) + R(pA) * 33f, n = -R(pA), along = F(pA), speed = 70, y = 52, prog = pA },
                new WallCase { name = "dive", wall = "Coral Vault cave wall", form = Mode.Dive, point = c.PointAt(pC, out _) + R(pC) * ParadisePlunge.CaveHalf, n = -R(pC), along = F(pC), speed = 30, y = c.PointAt(pC, out _).y, prog = pC, hold = true },
            };
            Line("WALLS: each form driven straight into a course wall at 10, 25 and 45 deg (the angle measured from the real contact normal), the throttle holding the speed; " +
                 "kept = speed 0.25 s after the hit / speed just before it; the slowest speed and the heading's largest turn in the 1.5 s after (a spin would turn it far past the wall's direction); then 3 s steering into the wall");
            bool all = true;
            foreach (var wc in cases)
            {
                var pt = wc.point; pt.y = wc.form == Mode.Drive ? 2.9f : wc.form == Mode.Boat ? .6f : wc.y;
                foreach (float ang in new[] { 10f, 25f, 45f })
                {
                    scenario = $"wall-{wc.name}-{ang:0}"; Clear(); straight = true; holdSpeed = wc.speed;
                    float yy = pt.y; if (wc.form == Mode.Fly || wc.form == Mode.Dive) yOverride = () => yy;
                    BeginAt(0, AnchorNear(wc.prog)); yield return Wait(.2f);
                    var dirIn = (wc.along * Mathf.Cos(ang * Mathf.Deg2Rad) - wc.n * Mathf.Sin(ang * Mathf.Deg2Rad)).normalized;
                    float gap = wc.form == Mode.Fly ? 9f : 7f;
                    var start = pt + wc.n * gap - wc.along * (gap / Mathf.Tan(ang * Mathf.Deg2Rad));
                    v.DebugPlace(start, Quaternion.LookRotation(dirIn), wc.form, wc.speed, wc.prog);
                    int r0 = v.Resets, h0 = v.WallHits; float before = v.Speed, hitAt = -1, after = -1, minSp = 999, spin = 0, angle = -1, keep = -1, h0deg = 0; string what = "";
                    Action<float, float, string> on = (a, k, w) => { if (hitAt < 0) { hitAt = Time.time; angle = a; keep = k; what = w; } };
                    v.WallHit += on;
                    float end = Time.time + 6f;
                    while (Time.time < end && v.Resets == r0)
                    {
                        if (hitAt < 0) { before = v.Speed; h0deg = v.HeadingDeg; }
                        else
                        {
                            if (after < 0 && Time.time - hitAt >= .25f) after = v.Speed;
                            minSp = Mathf.Min(minSp, v.Speed);
                            spin = Mathf.Max(spin, Mathf.Abs(Mathf.DeltaAngle(h0deg, v.HeadingDeg)));   // the heading's largest change from just before the hit
                            if (Time.time - hitAt > 1.5f) break;
                        }
                        if (hitAt > 0 && Mathf.Abs(Time.time - hitAt - .05f) < .02f) Shot(0, "hit");
                        yield return null;
                    }
                    v.WallHit -= on;
                    bool ok = hitAt > 0 && after > 0 && v.Resets == r0 && minSp > before * .5f && spin < ang + 25f;
                    all &= ok;
                    Line($"  {wc.name,-5} {ang,2:0} deg into the {wc.wall}: {(hitAt > 0 ? $"hit {what} at {angle:0} deg (rule keeps {keep * 100:0} %), {before:0.0} -> {after:0.0} m/s 0.25 s later = {after / Mathf.Max(before, .1f) * 100:0} % kept, slowest {minSp:0.0} m/s in 1.5 s, heading turned at most {spin:0} deg (no spin: under {ang + 25:0}), wall hits {v.WallHits - h0}, resets {v.Resets - r0}" : $"no hit in 6 s (resets {v.Resets - r0}{(v.Resets > r0 ? " " + v.LastResetReason : "")})")}  {(ok ? "OK" : "CHECK")}");
                }
                if (!wc.hold) { Line($"  {wc.name,-5} held against a wall: n/a (the walls in the air are the arch's short pillars; a plane passes them in under a second)"); continue; }
                // 3 s steering into the wall at full throttle
                scenario = $"wall-{wc.name}-hold"; Clear(); straight = true; straightSteer = .5f; throttleCap = 1;
                float y2 = pt.y; if (wc.form == Mode.Dive) yOverride = () => y2;
                BeginAt(0, AnchorNear(wc.prog)); yield return Wait(.2f);
                var dir2 = (wc.along * Mathf.Cos(15 * Mathf.Deg2Rad) - wc.n * Mathf.Sin(15 * Mathf.Deg2Rad)).normalized;
                var st2 = pt + wc.n * 5f - wc.along * (5f / Mathf.Tan(15 * Mathf.Deg2Rad)) - wc.along * 40f;
                v.DebugPlace(st2, Quaternion.LookRotation(dir2), wc.form, wc.speed * .8f, wc.prog - 40);
                int rr0 = v.Resets, hh0 = v.WallHits; float t1 = -1; Vector3 p1 = Vector3.zero; float minS = 999, sum = 0; int cnt = 0;
                float end2 = Time.time + 10f;
                while (Time.time < end2 && v.Resets == rr0)
                {
                    if (t1 < 0 && v.WallHits > hh0) { t1 = Time.time; p1 = v.Body.position; Shot(1.5f, "holding"); }
                    if (t1 > 0) { minS = Mathf.Min(minS, v.Speed); sum += v.Speed; cnt++; if (Time.time - t1 >= 3f) break; }
                    yield return null;
                }
                float moved = t1 > 0 ? Vector3.Dot(v.Body.position - p1, wc.along) : 0;
                bool okH = t1 > 0 && v.Resets == rr0 && moved > 30f && minS > 3f;
                all &= okH;
                Line($"  {wc.name,-5} held against the {wc.wall} for 3 s (steering into it, full throttle): {(t1 > 0 ? $"moved {moved:0} m forward along it, average {sum / Mathf.Max(1, cnt):0.0} m/s, slowest {minS:0.0} m/s, wall hits {v.WallHits - hh0}, resets {v.Resets - rr0}" : "never touched it")}  {(okH ? "OK" : "CHECK")}");
                Save();
            }
            Line($"WALLS {(all ? "PASS" : "CHECK")}");
            Save();
        }

        // ---------------- item 6: near misses ----------------
        IEnumerator NearPass(string name, int gate, Mode form, float lateral, float vertical, float back, float speed, Action<string, bool> done, bool expectCounted)
        {
            var c = game.Courses[0]; var g = c.gates[gate];
            scenario = "near-" + name; Clear(); holdSpeed = speed; laneOffset = lateral;
            int ai = 0; for (int k = 0; k < c.anchors.Count; k++) if (c.anchors[k].progress < g.progress - back - 5) ai = k;   // the last anchor before the gate
            BeginAt(0, ai); yield return Wait(.2f);
            if (v.GatesPassed != gate) Line($"  (note: the anchor left {v.GatesPassed} gates passed, the test expects {gate})");
            var at = c.PointAt(g.progress - back, out var d); var flat = Vector3.ProjectOnPlane(d, Vector3.up).normalized; var right = Vector3.Cross(Vector3.up, flat);
            float gy = g.transform.position.y + vertical; if (form == Mode.Fly || form == Mode.Dive || vertical != 0) yOverride = () => gy;
            var pos = at + right * lateral; if (vertical != 0 || form == Mode.Fly) pos.y = gy;
            v.DebugPlace(pos, Quaternion.LookRotation(flat), form, speed, g.progress - back);
            int g0 = v.GatesPassed, r0 = v.Resets; float p0 = v.Penalty; string cue = ""; bool turnBack = false;
            float end = Time.time + 12f; bool shot = false;
            while (Time.time < end && v.Resets == r0)
            {
                if (Time.time < v.CueUntil && v.Cue.StartsWith("MISSED")) { cue = v.Cue; if (!shot) { shot = true; Capture("cue"); } }
                if (Time.time < v.CueUntil && v.Cue.StartsWith("TURN BACK")) { turnBack = true; if (!shot) { shot = true; Capture("turn-back"); } }
                if (v.GatesPassed > g0 && Time.time > end - 10.5f) break;
                if (!expectCounted && (turnBack || v.Log.Exists(x => x.Contains("rejected") && x.Contains(g.label)))) { yield return Wait(.5f); break; }
                yield return null;
            }
            yield return Wait(1.5f);
            bool counted = v.GatesPassed > g0; var gl = string.Join(" | ", v.Log.FindAll(x => x.Contains(g.label) && (x.Contains("passed") || x.Contains("rejected") || x.Contains("missed"))).Select(x => x.Trim()));
            string s = $"{(counted ? $"counted, penalty {v.Penalty - p0:+0;-0} s" : "not counted")}, cue \"{cue}\"{(turnBack ? ", TURN BACK shown" : "")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}; log: {gl}";
            done(s, counted && v.Penalty - p0 > 0 && v.Resets == r0);
        }
        IEnumerator NearMiss()
        {
            var c = game.Courses[0]; game.StartCourse(0, false); yield return Wait(.3f);
            float pen = game.Shared.nearMissPenalty, margin = game.Shared.nearMissMargin;
            Line($"NEAR MISS: a gate passed outside its frame but within {margin:0.0} gate width (8-30 m) of it, in a permitted form, heading along the route, counts with +{pen:0} s; the band per gate:");
            foreach (var g in c.gates) { var b = g.NearBand(margin); Line($"  {g.label}: frame +-{g.halfSize.x:0} x +-{g.halfSize.y:0} m, near-miss band to +-{g.halfSize.x + b.x:0} x +-{g.halfSize.y + b.y:0} m"); }
            bool all = true; string r = ""; bool ok = false;
            var lagoon = new Vector3(400 * ParadisePlunge.S, 0, -460 * ParadisePlunge.S);
            float g4 = c.gates[4].progress; var g4at = c.PointAt(g4 - 60, out var g4d); float lagoonSide = Mathf.Sign(Vector3.Dot(lagoon - g4at, Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(g4d, Vector3.up).normalized)));
            var tests = new (string name, int gate, Mode form, float lateral, float vertical, float back, float speed, bool counted, string what)[]
            {
                ("road-G1", 1, Mode.Drive, c.gates[1].halfSize.x + 3, 0, 60, 35, true, "road: G1 passed 3 m outside its post, on the grass beyond the barrier"),
                ("air-G3", 3, Mode.Fly, 0, -(c.gates[3].halfSize.y + 8), 160, 70, true, "flight: G3 passed 8 m under its frame (beside it the arch's pillars stand in the way)"),
                ("water-G4", 4, Mode.Boat, lagoonSide * (c.gates[4].halfSize.x + 8), 0, 70, 35, true, "water: G4 passed 8 m outside its frame on the lagoon"),
                ("pool-G8", 8, Mode.Boat, c.gates[8].halfSize.x + 5, 0, 8, 15, true, "pool: G8 passed 5 m outside its frame (the cave gates G6 / G7 fill the cave's cross-section: they cannot be passed beside)"),
                ("far-G3", 3, Mode.Fly, c.gates[3].halfSize.x + c.gates[3].NearBand(margin).x + 15, 0, 160, 70, false, "far miss: G3 passed 15 m beyond the band"),
                ("wrongform-G4", 4, Mode.Fly, 0, c.gates[4].halfSize.y + 8, 160, 70, false, "wrong form: G4 (boat only) flown over as a plane 8 m above its frame"),
            };
            foreach (var t in tests)
            {
                yield return NearPass(t.name, t.gate, t.form, t.lateral, t.vertical, t.back, t.speed, (s, k) => { r = s; ok = k; }, t.counted);
                bool pass = t.counted ? ok : !ok && !v.Log.Exists(x => x.Contains(c.gates[t.gate].label) && x.Contains("near miss") && x.Contains("passed"))
                                             && (!t.name.StartsWith("wrongform") || v.Log.Exists(x => x.Contains("near miss of " + c.gates[t.gate].label) && x.Contains("not counted")));
                all &= pass;
                Line($"  {t.what}: {r}  {(pass ? (t.counted ? "PASS" : "PASS (rejected)") : "FAIL")}");
                Save();
            }
            // the most a near miss could save: for every gate and shortcut sub-gate, the shortest chord from L m before to L m after through
            // the frame's nearest point against through the band's furthest point, at the form's top speed (no boost)
            float worst = 0; string worstAt = "";
            void Chord(ProgressGate g, Func<float, Vector3> path, float at, float speed)
            {
                var b = g.NearBand(margin);
                foreach (float L in new[] { 40f, 80f, 160f, 320f })
                {
                    Vector3 pb = g.transform.InverseTransformPoint(path(at - L)), pa = g.transform.InverseTransformPoint(path(at + L));
                    if (pb.z >= 0 || pa.z <= 0) continue;
                    float t = -pb.z / (pa.z - pb.z); var h = Vector3.Lerp(pb, pa, t);
                    var xf = new Vector3(Mathf.Clamp(h.x, -g.halfSize.x, g.halfSize.x), Mathf.Clamp(h.y, -g.halfSize.y, g.halfSize.y), 0);
                    // the band's furthest point toward the chord (the most a near miss can straighten the path)
                    var xn = new Vector3(Mathf.Clamp(h.x, -g.halfSize.x - b.x, g.halfSize.x + b.x), Mathf.Clamp(h.y, -g.halfSize.y - b.y, g.halfSize.y + b.y), 0);
                    float save = (Vector3.Distance(pb, xf) + Vector3.Distance(xf, pa)) - (Vector3.Distance(pb, xn) + Vector3.Distance(xn, pa));
                    // a chord that leaves the frame on purpose: also the worst case of a whole band's sideways offset on the line itself
                    float sec = save / speed; if (sec > worst) { worst = sec; worstAt = $"{g.label} ({L:0} m either side, {save:0.0} m shorter at {speed:0} m/s)"; }
                }
            }
            float Top(ProgressGate g) => g.Allows(Mode.Dive) && !g.Allows(Mode.Boat) ? game.Dive.topSpeed : g.Allows(Mode.Fly) && !g.Allows(Mode.Drive) ? game.Fly.topSpeed : g.Allows(Mode.Boat) ? game.Boat.topSpeed : game.Drive.topSpeed;
            foreach (var g in c.gates) Chord(g, p => c.PointAt(p, out _), g.progress, Top(g));
            foreach (var sc in c.shortcuts) foreach (var g in new[] { sc.entry, sc.exit }) { float s0 = sc.Project(g.transform.position, -1, out _, out _); Chord(g, s => sc.PointAt(s, out _), s0, Top(g)); }
            // the line's own racing room: a racer can also cut a corner by the band beside the gate; the chord above covers it (straight
            // lines through the furthest band point are the shortest possible paths)
            bool penOk = pen > worst * 1.5f && pen >= worst + 1f;
            all &= penOk;
            Line($"NEAR MISS PENALTY {(penOk ? "PASS" : "FAIL")}: the most a near miss could save anywhere on the lap: {worst:0.00} s at {worstAt}; penalty {pen:0} s (clearly above it: a lap with a near miss is never faster than the clean lap)");
            Line($"NEAR MISS {(all ? "PASS" : "FAIL")}");
            Save();
        }

        // ---------------- item 4: starts ----------------
        IEnumerator Starts()
        {
            int starts = ArgInt("-starts", 25); float scale = ArgFloat("-timeScale", 3f);
            Line($"STARTS: {starts} race starts (the check pilot in Abi's car at the back, the 3 AI at {Settings.Difficulty}), the first 10 s after GO at time scale {scale:0.0} (physics steps unchanged)");
            int resets = 0, unsticks = 0, walls = 0, bumps = 0; float closest = 99; var why = new List<string>(); var spread = new List<float>();
            for (int i = 0; i < starts; i++)
            {
                scenario = $"start{i + 1}"; Clear(); smart = true; game.StartCourse(0, true);
                if (i == 0) Shot(.4f, "grid");
                yield return Until(() => !v.Holding, 6f); Time.timeScale = scale; float go = Time.time;
                var rs = game.Race.Racers.ToList(); var u0 = game.Rivals.Select(x => x.GetComponent<RivalPilot>().Unsticks).ToList(); var w0 = rs.Select(x => x.WallHits).ToList();
                int contacts = 0; Action<Collision, bool> onC = (col, first) => { if (first && col.rigidbody && col.rigidbody.GetComponent<ShiftVehicle>()) contacts++; };
                foreach (var r in rs) r.Contact += onC;
                while (Time.time - go < 10f)
                {
                    for (int a = 0; a < rs.Count; a++) for (int b = a + 1; b < rs.Count; b++) closest = Mathf.Min(closest, Vector3.Distance(rs[a].Body.position, rs[b].Body.position));
                    if (i == 0 && Mathf.Abs(Time.time - go - 1.5f) < .03f) Shot(0, "1.5s-after-go");
                    yield return null;
                }
                Time.timeScale = 1;
                foreach (var r in rs) r.Contact -= onC;
                foreach (var r in rs) { resets += r.Resets; foreach (var l in r.Log.FindAll(x => x.Contains("reset ("))) why.Add($"start {i + 1} {r.RacerName}: {l}"); }
                unsticks += game.Rivals.Select((x, k) => x.GetComponent<RivalPilot>().Unsticks - u0[k]).Sum();
                walls += rs.Select((x, k) => x.WallHits - w0[k]).Sum(); bumps += contacts / 2;
                var prog = rs.Select(x => x.Progress).OrderBy(x => x).ToList(); spread.Add(prog[prog.Count - 1] - prog[0]);
            }
            Line($"STARTS {(resets == 0 ? "PASS" : "FAIL")}: {starts} starts: resets in the first 10 s {resets}, AI back-outs {unsticks}, wall hits {walls}, racer-to-racer touches {bumps}, closest two racers came {closest:0.0} m (centres), field spread after 10 s {spread.Min():0}-{spread.Max():0} m");
            foreach (var w in why.Take(10)) Line("  " + w);
            Save();
        }

        // ---------------- item 2: difficulty ----------------
        IEnumerator DifficultyCheck()
        {
            int races = ArgInt("-races", 3); float scale = ArgFloat("-timeScale", 3f);
            var levels = Arg("-level") ? new[] { Settings.Difficulty } : new[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard };
            Line($"DIFFICULTY: {races} races per level against the check pilot (Abi's car, no boost, the safe line), time scale {scale:0.0} (physics steps unchanged)");
            var summary = new List<string>();
            foreach (var d in levels)
            {
                Settings.Difficulty = d; var ai = new List<float>(); var pilot = new List<float>(); int firsts = 0; var per = new Dictionary<string, List<float>>();
                for (int k = 0; k < races; k++)
                {
                    scenario = $"diff-{d}-{k + 1}"; Clear(); smart = true; game.StartCourse(0, true);
                    yield return Until(() => !v.Holding, 6f); Time.timeScale = scale; float go = Time.time;
                    var race = game.Race;
                    while (Time.time - go < 330f && !race.Racers.All(r => r.Finished)) yield return null;
                    Time.timeScale = 1; yield return Wait(.2f);
                    var order = race.Order();
                    foreach (var r in game.Rivals) { if (r.Finished) ai.Add(r.FinishTime); if (!per.ContainsKey(r.RacerName)) per[r.RacerName] = new List<float>(); per[r.RacerName].Add(r.Finished ? r.FinishTime : -1); }
                    if (v.Finished) pilot.Add(v.FinishTime); if (order[0] == v) firsts++;
                    Line($"  {d} race {k + 1}: {string.Join(", ", order.Select((r, i) => $"{i + 1}. {r.RacerName} {(r.Finished ? r.FinishTime.ToString("0.0") : "DNF")}{(r.Penalty > 0 ? $" (+{r.Penalty:0} penalty)" : "")}{(r.Resets > 0 ? $" ({r.Resets} resets)" : "")}{(r.ShortcutsTaken.Count > 0 ? " [" + string.Join("+", r.ShortcutsTaken) + "]" : "")}"))}; AI mistakes {string.Join("/", game.Rivals.Select(x => x.GetComponent<RivalPilot>().Mistakes))}");
                    Save();
                }
                float pm = pilot.Count > 0 ? pilot.Average() : 0, am = ai.Count > 0 ? ai.Average() : 0;
                string line = $"{d}: AI pack mean {am:0.0} s ({(pm > 0 ? (am / pm - 1) * 100 : 0):+0;-0} % against the check pilot's {pm:0.0} s); per rival {string.Join(", ", per.Select(kv => $"{kv.Key.Split(' ')[0]} {string.Join("/", kv.Value.Select(x => x < 0 ? "DNF" : x.ToString("0.0")))}"))}; check pilot 1st in {firsts} of {races}";
                summary.Add(line); Line("DIFFICULTY " + line);
            }
            Settings.Difficulty = Difficulty.Easy;
            foreach (var s in summary) Line("  " + s);
            Save();
        }

        // ---------------- item 5: the tunnel ----------------
        IEnumerator TunnelLaps()
        {
            var c = game.Courses[0]; var sc = c.shortcuts.Find(x => x.label == "Coral Vault tunnel");
            if (!sc) { Line("TUNNEL: not built (VaultTunnel off)"); yield break; }
            int legs = ArgInt("-legs", 25); float scale = ArgFloat("-timeScale", 3f);
            int anchor = AnchorNear(Mk("portal") + 45);
            Line($"TUNNEL: {legs} AI runs from the anchor at {c.anchors[anchor].progress:0} m to G7 going for the tunnel (lane and seed varied), then scrape runs 4-6.5 m off its line; time scale {scale:0.0}");
            var times = new List<float>(); int taken = 0, stuck = 0, resets = 0, hits = 0; var notes = new List<string>();
            for (int i = 0; i < legs + 3; i++)
            {
                bool scrape = i >= legs; float off = scrape ? new[] { 4f, -5f, 6.5f }[i - legs] : 0;
                scenario = scrape ? $"tunnel-scrape{i - legs + 1}" : $"tunnel{i + 1}"; Clear(); BeginAt(0, anchor);
                var p = DriveWith(sc.label); p.rival.lane = ((i % 5) - 2) * 1.2f; p.rival.seed = 100 + i; p.shortcutOffset = off;
                yield return null; Time.timeScale = scale; float t0 = Time.time; int r0 = v.Resets, h0 = v.WallHits, u0 = 0;
                if (i == 0) Shot(4f, "in-the-tunnel");
                while (Time.time - t0 < 90f && v.GatesPassed < 8) yield return null;
                Time.timeScale = 1;
                float t = v.GatesPassed >= 8 ? Time.time - t0 : -1; bool tk = v.ShortcutsTaken.Contains(sc.label);
                bool st = t < 0 || v.Log.Exists(x => x.Contains("reset (") && (x.Contains("stuck") || x.Contains("no progress")));
                if (!scrape) { if (t > 0) times.Add(t); if (tk) taken++; if (st) stuck++; resets += v.Resets - r0; hits += v.WallHits - h0; }
                if (scrape || st || v.Resets > r0) notes.Add($"{scenario}: {(t > 0 ? $"{t:0.00} s" : "did not reach G7")}, tunnel {(tk ? "taken" : "not taken")}, wall hits {v.WallHits - h0}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, back-outs {p.Unsticks}");
                StopAi();
            }
            times.Sort();
            Line($"TUNNEL {(stuck == 0 ? "PASS" : "FAIL")}: {legs} runs: reached G7 {times.Count}, through the tunnel {taken}, stuck {stuck}, resets {resets}, wall hits {hits}; time to G7 min {(times.Count > 0 ? times[0] : 0):0.00} / median {(times.Count > 0 ? times[times.Count / 2] : 0):0.00} / max {(times.Count > 0 ? times[times.Count - 1] : 0):0.00} s");
            foreach (var n in notes) Line("  " + n);
            Save();
        }
    }
}
