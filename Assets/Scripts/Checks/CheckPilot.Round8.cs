using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 8 checks (all muted, batch mode, no window):
    //   dinos     a lap (Lap(): pictures, perf per segment, hitches) counting the dinosaurs seen per segment (in the view, in range, not behind
    //             rock), the route clearance of every one, the T. rex's roars
    //   barker    a race (the player's car driven by an AI, a reset and a near miss forced): TeedOff Barker's lines per minute, longest
    //             silence, overlaps (his and the drivers'), repeats, pools; his level against the music and the engines (each source's own
    //             signal, which a muted run still has); the drivers' mess-ups
    //   places    races until the player has finished 1st, a middle place and last: the reaction clip and Barker's results line each time
    //   steering  every form in first person and chase, the mechanism at full left / centre / full right, the hands on the grips
    //   volumes   the four volumes (defaults or a migrated old settings.json in -saveDir), the would-be levels per bus in a race, the duck
    //   choice8   the Lake Jump's two lines (Round 7's ChoiceCheck) + pictures in the canyon and over the lake
    //   round8 = dinos + barker + places + steering + volumes + choice8 (then tunnel / shortcuts / stuntlaps / split / soak run on their own)
    public sealed partial class CheckPilot
    {
        readonly Dictionary<string, HashSet<DinoActor>> dinoSeen = new Dictionary<string, HashSet<DinoActor>>();
        readonly Dictionary<string, int> dinoMax = new Dictionary<string, int>(); bool dinoCount; int dinoFrame;
        readonly List<(string scen, float prog, string name)> progShots = new List<(string, float, string)>();

        void DinoSample()
        {
            if (!dinoCount || ++dinoFrame % 6 != 0 || segNow == "") return;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam); var cp = cam.transform.position; int now = 0;
            if (!dinoSeen.TryGetValue(segNow, out var set)) dinoSeen[segNow] = set = new HashSet<DinoActor>();
            foreach (var d in DinoActor.All)
            {
                var r = d.Rend; if (!r || !r.enabled) continue;
                var b = r.bounds; float dist = Vector3.Distance(cp, b.center);
                if (dist > d.range || !GeometryUtility.TestPlanesAABB(planes, b)) continue;
                // not behind rock: a ray to its middle or its top reaches it
                bool seen = false;
                foreach (var tp in new[] { b.center, b.center + Vector3.up * b.extents.y * .8f })
                    if (!Physics.Linecast(cp, tp, out var hit, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore) || hit.distance > dist - b.extents.magnitude) { seen = true; break; }
                if (!seen) continue;
                now++; set.Add(d);
            }
            dinoMax[segNow] = Mathf.Max(dinoMax.TryGetValue(segNow, out var m) ? m : 0, now);
        }

        IEnumerator DinosCheck()
        {
            dinoSeen.Clear(); dinoMax.Clear(); dinoCount = true;
            var c = game.Courses[0];
            Line($"DINOS (item 1): {DinoActor.All.Count} dinosaurs ({string.Join(", ", DinoActor.All.GroupBy(d => d.style).Select(g => $"{g.Key} {g.Count()}"))}), {ParadisePlunge.Fossils.Count} nests / footprints / fossils; stand-ins (no model yet): {string.Join(", ", DinoActor.All.Where(d => d.standIn).Select(d => d.style).Distinct())}");
            Line($"  rigs: {string.Join(", ", DinoRig.Styles.Select(s => $"{s} {DinoRig.Triangles(s) / 1000.0:0.0}k tris"))}; left out at build time (too near the line): {(ParadisePlunge.DinoSkipped.Count == 0 ? "none" : string.Join("; ", ParadisePlunge.DinoSkipped))}");
            // clearance: every dinosaur's path against the racer line
            float worst = 1e9f; string worstWho = "";
            foreach (var d in DinoActor.All)
            {
                var pts = new List<Vector3>();
                if (d.kind == DinoActor.Kind.Swoop) for (int i = 0; i < 24; i++) { float t = i * Mathf.PI * 2 / 24; pts.Add(d.a + new Vector3(Mathf.Cos(t) * d.b.x, Mathf.Sin(t * 2) * d.b.z, Mathf.Sin(t) * d.b.x)); }
                else for (int i = 0; i <= 6; i++) pts.Add(Vector3.Lerp(d.a, d.b, i / 6f));
                foreach (var p in pts) { float dist = ParadisePlunge.LineDistance(p, out float flat, out _); float clr = d.kind == DinoActor.Kind.Swoop || dist - flat > 20f ? dist : flat; if (clr < worst) { worst = clr; worstWho = d.label; } }
            }
            Line($"  ROUTE CLEARANCE: the nearest any dinosaur's path comes to the racer line: {worst:0.0} m ({worstWho}; walkers on the ground plan (in 3D where the line passes 20+ m over them), pterodactyls in 3D){(worst > 20 ? "  PASS" : "  CHECK")}");
            yield return Lap();
            dinoCount = false;
            var segs = c.segments.Select(s => s.name).ToList(); int with = 0;
            Line("  DINOS SEEN per segment on the lap (player view, in range, not behind rock): distinct / most at once / which");
            foreach (var s in segs)
            {
                dinoSeen.TryGetValue(s, out var set); int n = set?.Count ?? 0; if (n > 0) with++;
                Line($"    {s,-18} {n,3} / {(dinoMax.TryGetValue(s, out var mx) ? mx : 0),2}   {(set == null ? "-" : string.Join(", ", set.GroupBy(d => d.style).Select(g => $"{g.Key} x{g.Count()}")))}");
            }
            var fossils = ParadisePlunge.Fossils.GroupBy(f => f.name.Split(' ').Last()).Select(g => $"{g.Key} {g.Count()}");
            Line($"  DINOS {(with == segs.Count ? "PASS" : $"{with} of {segs.Count} segments")}: a group seen in {with} of {segs.Count} segments; fossils / nests / footprints per segment: {string.Join(", ", ParadisePlunge.Fossils.GroupBy(f => f.name.Substring(f.name.IndexOf(' ') + 1)).Select(g => $"{g.Key} {g.Count()}"))}; T. rex roars {DinoActor.All.Where(d => d.kind == DinoActor.Kind.Roar).Sum(d => d.Roars)}, last camera shake {(ChaseCam.LastShake > 0 ? "yes" : "no")}");
        }

        // ---- item 2 / 3: a race with TeedOff Barker and the drivers ----
        IEnumerator BarkerCheck()
        {
            scenario = "barker"; Clear(); Settings.Difficulty = Difficulty.Normal;
            game.StartCourse(0, true); var caller = Caller.Instance; Driver.AllSpoken.Clear();
            var pilot = v.gameObject.AddComponent<RivalPilot>(); pilot.rival = new Rival { name = "Abi (AI)", lane = 0, brakeMargin = .9f, boostMeter = .6f, boostStraight = 9f, seed = 77 }; pilot.AlwaysOn = true; aiDrives = true;
            Line($"BARKER (item 2): {caller.Clips} clips in {caller.Pools} pools; drivers' lines loaded: {string.Join(", ", Driver.All.Select(d => $"{d.Who} {d.LinesLoaded} (mess-ups {d.LinesIn("messup")})"))}");
            yield return Until(() => !v.Holding, 8f); float go = Time.time;
            var lv = new List<(float barker, float music, float engine)>(); var lq = new List<(float music, float engine)>(); bool resetDone = false, missDone = false; float bufT = 0;
            var buf = new float[512];
            // a muted run's sources give no output data, so the would-be level is the clip's own signal at the play position x the source's
            // volume (a streamed clip, the music, has no readable data: its measured loudness, -15.7 LUFS ~ RMS 0.16, stands in)
            float Rms(AudioSource s)
            {
                if (!s || !s.isPlaying || !s.clip) return 0; var c = s.clip; float x = 0;
                if (c.loadType == AudioClipLoadType.Streaming || !c.GetData(buf, Mathf.Clamp(s.timeSamples, 0, Mathf.Max(0, c.samples - buf.Length / c.channels - 1)))) return .16f * s.volume;
                foreach (var b in buf) x += b * b; return Mathf.Sqrt(x / buf.Length) * s.volume;
            }
            var snd = v.GetComponent<VehicleSound>();
            while (!v.Finished && Time.time - go < 260f)
            {
                // a forced reset on the climb and a forced near miss later (mess-ups for Abi and a call for Barker)
                if (!resetDone && v.Progress > Mk("climbMid")) { resetDone = true; input.RequestReset(); Line($"  forced a reset at {Time.time - go:0.0} s (the Cliffside Climb)"); }
                if (!missDone && v.Progress > Mk("bend2") && v.Mode == Mode.Boat) { missDone = true; pilot.missOffset = 0; }
                if (Time.time > bufT)
                {
                    bufT = Time.time + .1f; float m = Rms(CourseAudio.Instance ? CourseAudio.Instance.MusicSource : null), e = Rms(snd ? snd.EngineSource : null);
                    if (caller.Talking) lv.Add((Rms(caller.Voice), m, e)); else lq.Add((m, e));
                }
                yield return null;
            }
            float end = v.Finished ? v.StartTime + v.FinishTime - v.Penalty : Time.time; yield return Until(() => game.Trial.HasResults, 5f); yield return Wait(6f);
            StopAi();
            var lines = caller.Lines.Where(l => l.t >= go - 1).OrderBy(l => l.t).ToList(); var during = lines.Where(l => l.t >= go && l.t <= end).ToList();
            float raceSecs = end - go; float longest = 0; float prevEnd = go; foreach (var l in during) { longest = Mathf.Max(longest, l.t - prevEnd); prevEnd = l.t + l.len; } longest = Mathf.Max(longest, end - prevEnd);
            int overlaps = 0, cutIns = 0; var top = new[] { "go", "win", "photo" };
            for (int i = 1; i < lines.Count; i++) if (lines[i].t < lines[i - 1].t + lines[i - 1].len - .05f) { if (Array.IndexOf(top, lines[i].pool) >= 0) cutIns++; else overlaps++; }   // a top call stops the line before it (not heard together)
            int repeats = lines.Count - lines.Select(l => l.clip).Distinct().Count();
            // the drivers' lines against his
            var drv = Driver.AllSpoken.Where(s => !s.Contains("cut by")).Select(s => { var p = s.Split(' '); return (t: float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture), who: p[1], pool: p[2], clip: p[3], len: float.Parse(p[4], System.Globalization.CultureInfo.InvariantCulture)); }).ToList();
            int drvOver = 0; foreach (var d in drv) foreach (var l in lines) if (d.t < l.t + l.len - .05f && l.t < d.t + d.len - .05f) { bool cut = Driver.AllSpoken.Any(s => s.Contains("cut by") && s.Contains(d.who) && Mathf.Abs(float.Parse(s.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture) - l.t) < .1f); if (!cut) drvOver++; }
            Line($"BARKER {(overlaps == 0 && repeats == 0 && drvOver == 0 && longest < 16 ? "PASS" : "CHECK")}: race {raceSecs:0.0} s, {during.Count} lines during it ({during.Count / Mathf.Max(1f, raceSecs / 60f):0.0} per minute, one every {raceSecs / Mathf.Max(1, during.Count):0.0} s), {lines.Count} with the start and results; longest silence {longest:0.0} s; overlaps {overlaps} (cut-ins by GO / the winner, the line before stopped: {cutIns}); repeats {repeats}; pools: {string.Join(", ", lines.GroupBy(l => l.pool).Select(g => $"{g.Key} {g.Count()}"))}");
            foreach (var l in lines) Line($"    {l.t - go,6:0.0} s  {l.pool,-9} {l.clip} ({l.len:0.0} s)");
            Line($"  skipped calls (why): {string.Join(", ", caller.Skipped.Where(s => float.TryParse(s.Split(' ')[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var st) && st >= go).Select(s => s.Substring(s.IndexOf(' ') + 1).Split(':')[1].Trim()).GroupBy(x => x).Select(g => $"{g.Key} {g.Count()}"))}");
            float Db(float x) => 20 * Mathf.Log10(Mathf.Max(1e-5f, x));
            if (lv.Count > 0)
            {
                float b = lv.Average(x => x.barker), m = lv.Average(x => x.music), e = lv.Average(x => x.engine), mq = lq.Count > 0 ? lq.Average(x => x.music) : 0, eq = lq.Count > 0 ? lq.Average(x => x.engine) : 0;
                Line($"  LEVELS (would-be: each clip's signal at its play position x its source volume, the muted listener aside; {lv.Count} samples while he talks): Barker {Db(b):0.0} dBFS, music {Db(m):0.0} (between his lines {Db(mq):0.0}), player's engine {Db(e):0.0} (between {Db(eq):0.0}); Barker over the music by {Db(b) - Db(m):0.0} dB, over the engine by {Db(b) - Db(e):0.0} dB; the duck: music {Db(m) - Db(mq):0.0} dB, engine {Db(e) - Db(eq):0.0} dB");
            }
            // item 3: the mess-ups
            var mess = drv.Where(d => d.pool == "messup" || d.pool == "water").ToList();
            Line($"MESS-UPS (item 3) {(drvOver == 0 ? "PASS" : "FAIL")}: {mess.Count} yells in the race ({string.Join(", ", mess.Select(d => $"{d.t - go:0.0} s {d.who} {d.clip}"))}); overlapping Barker: {drvOver}; Barker cut a driver off {Driver.AllSpoken.Count(s => s.Contains("cut by"))} times");
            foreach (var d in Driver.All) Line($"  {d.Who}: {d.LinesIn("messup")} mess-up lines + {d.LinesIn("water")} water; this race: {string.Join("; ", d.Spoken.Where(s => s.StartsWith("messup") || s.StartsWith("water")).Take(6))}");
            Save();
        }

        // ---- item 5: the reaction by place ----
        IEnumerator PlacesCheck()
        {
            Line("PLACES (item 5): races (time scale 3) until the player has finished 1st, a middle place and last; the driver's reaction clip and TeedOff Barker's results line");
            var got = new Dictionary<string, string>(); float[] caps = { 1f, .86f, .7f, .93f, .62f };
            for (int k = 0; k < caps.Length && got.Count < 3; k++)
            {
                scenario = $"places{k + 1}"; Clear(); Settings.Difficulty = k == 0 ? Difficulty.Easy : Difficulty.Normal; smart = true; throttleCap = caps[k]; boost = k == 0;
                game.StartCourse(0, true); yield return Until(() => !v.Holding, 8f); Time.timeScale = 3f;
                yield return Until(() => v.Finished || v.Resets > 3, 260f); Time.timeScale = 1f;
                yield return Until(() => game.Trial.HasResults, 5f);
                var d = Driver.Instance; yield return Wait(.5f);
                int place = game.Race.Place(v), field = game.Race.Racers.Count;
                // the rivals still racing finish the lap (their places don't move the player's); wait for his results line
                yield return Until(() => Caller.Instance.Lines.Any(l => l.pool == "results" || l.pool == "podium" || l.pool == "last"), 12f);
                var res = Caller.Instance.Lines.LastOrDefault(l => l.pool == "results" || l.pool == "podium" || l.pool == "last");
                string kind = place == 1 ? "1st" : place == field ? "last" : "middle";
                string clip = d.Played.Count > 0 ? d.Played.Last() : "-";
                bool ok = kind == "1st" ? clip == "Sitting Clap" : kind == "last" ? clip == "Sitting Disapproval" : clip == "Sitting Thumbs Up";
                bool lineOk = kind == "1st" ? res.pool == "results" : kind == "last" ? res.pool == "last" : res.pool == "podium";
                Line($"  race {k + 1} (throttle {caps[k] * 100:0} %{(boost ? ", boost" : "")}, {Settings.Difficulty}): {Race.Ordinal(place)} of {field} in {v.FinishTime:0.0} s -> {d.FinishReaction}; clip {clip}; Barker: {res.pool} {res.clip}  {(ok && lineOk ? "PASS" : "FAIL")}");
                Shot(0, $"result-{kind}"); yield return Wait(.3f);
                if (!got.ContainsKey(kind)) got[kind] = ok && lineOk ? "PASS" : "FAIL";
                Clear();
            }
            Line($"PLACES {(got.Count == 3 && got.Values.All(x => x == "PASS") ? "PASS" : "CHECK")}: {string.Join(", ", got.Select(kv => $"{kv.Key} {kv.Value}"))}");
            Save();
        }

        // ---- item 4: the steering mechanism ----
        IEnumerator SteeringCheck()
        {
            scenario = "steering"; Clear(); straight = true; holdSpeed = 4; game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f); yield return Wait(2.5f);   // after the GO horn gesture, rolling at 4 m/s (a car standing still is reset as stuck)
            var d = Driver.Instance; var st = d ? d.Steering : null; var views = cam.GetComponent<CameraViews>();
            if (!st) { Line("STEERING: no mechanism (no driver?)"); yield break; }
            Line($"STEERING (item 4): {string.Join(", ", FormSet.All.Select(m => $"{Game.FormName(m)} {st.KindOf(m)}"))}; at {st.Anchor.ToString("F2")} in the vehicle (the hands' centre in the Driving pose), grips {st.Span:0.00} m apart");
            Steering.ForceInput = true; float worst = 0; var rows = new List<string>();
            foreach (var m in FormSet.All)
            {
                v.Forms.Snap(m); yield return Wait(.3f);
                foreach (var view in new[] { CameraViews.View.FirstPerson, CameraViews.View.Chase })
                {
                    views.SetView(view); yield return Wait(.4f);
                    foreach (float s in new[] { -1f, 0f, 1f })
                    {
                        Steering.ForcedSteer = s; Steering.ForcedPitch = m == Mode.Fly || m == Mode.Dive ? -s * .6f : 0; yield return Wait(.45f);
                        var e = d.GripError(); worst = Mathf.Max(worst, Mathf.Max(e.l, e.r));
                        if (view == CameraViews.View.FirstPerson) rows.Add($"{Game.FormName(m)} {(s < 0 ? "left" : s > 0 ? "right" : "centre")}: {st.Shown.kind} at {st.Shown.turn:0} deg, hands {e.l * 100:0} / {e.r * 100:0} cm off the grips, IK {d.IkWeight:0.00}");
                        Capture($"{Game.FormName(m).ToLowerInvariant()}-{(view == CameraViews.View.FirstPerson ? "first-person" : "chase")}-{(s < 0 ? "left" : s > 0 ? "right" : "centre")}");
                    }
                }
            }
            Steering.ForceInput = false; views.SetView(CameraViews.View.Chase); v.Forms.Snap(v.Mode);
            foreach (var r in rows) Line("  " + r);
            Line($"STEERING {(worst < .08f ? "PASS" : "CHECK")}: worst hand-to-grip distance {worst * 100:0.0} cm over every form and angle; the mechanism swaps with the model at the change's midpoint (it follows the shown model)");
            Save();
        }

        // ---- item 6: the volumes ----
        IEnumerator VolumesCheck()
        {
            Line($"VOLUMES (item 6): settings file {Settings.FilePath} ({(File.Exists(Settings.FilePath) ? "exists" : "none: defaults")}): Music {Settings.Music * 100:0} %, Voices {Settings.Voices * 100:0} %, Engines {Settings.Engines * 100:0} %, Effects {Settings.Effects * 100:0} %; playtest logs {(Settings.PlaytestLogs ? "on" : "off")}");
            Line($"  menu rows: {string.Join(" | ", MenuRows())}");
            scenario = "volumes"; Clear(); smart = true; game.StartCourse(0, true); yield return Until(() => !v.Holding, 8f); yield return Wait(12f);
            var snd = v.GetComponent<VehicleSound>(); var au = CourseAudio.Instance; var caller = Caller.Instance;
            Line($"  WOULD-BE LEVELS on the road at {v.Speed:0} m/s (source volumes, the muted listener aside): music {au.MusicVolume:0.00} (Music x{CourseAudio.MusicGain} road .5 = .65 at 100 %), engine {snd.EngineVolume:0.00} (Engines {Settings.Engines * 100:0} %: build 8 had ~.42 at Effects 100 %), wind {snd.WindVolume:0.00}, Barker voice {caller.Voice.volume:0.00}, duck now {Caller.Duck:0.00}");
            // the duck: force a line and read it again
            caller.Call("fill", v, true); yield return Wait(.6f);
            Line($"  under a Barker line: music {au.MusicVolume:0.00}, engine {snd.EngineVolume:0.00}, duck {Caller.Duck:0.00} ({20 * Mathf.Log10(Mathf.Max(1e-4f, Caller.Duck)):0.0} dB)");
            Clear(); Save();
        }
        IEnumerable<string> MenuRows() { game.OpenMenu(); game.Choose(game.MenuItems.FindIndex(x => x.StartsWith("Settings"))); var l = game.MenuItems.ToList(); game.CloseMenu(); return l; }

        IEnumerator Choice8()
        {
            progShots.Clear();
            progShots.Add(("choice-car", ParadisePlunge.CanyonFrom + 40, "canyon-entry")); progShots.Add(("choice-car", ParadisePlunge.CanyonTo - 60, "canyon-exit"));
            progShots.Add(("choice-fly", Mk("jC") + 120, "over-the-beach")); progShots.Add(("choice-fly", Mk("jC") + 200, "rings-over-the-lake"));
            yield return ChoiceCheck();
            progShots.Clear();
        }
        void ProgShots() { for (int i = progShots.Count - 1; i >= 0; i--) if (progShots[i].scen == scenario && v.Progress > progShots[i].prog && v.Progress < progShots[i].prog + 80) { Shot(0, progShots[i].name); progShots.RemoveAt(i); } }
    }
}
