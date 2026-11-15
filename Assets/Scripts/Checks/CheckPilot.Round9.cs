using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 9 checks (all muted, batch mode, no window):
    //   steering  (Round 8's, now with the share of the screen height each mechanism covers in first person: Round 9 item 1)
    //   sounds    a race (the player's racer driven by an AI, a reset and a near miss forced): every race event and the sounds it started
    //             (SoundLog), two of a kind for one event flagged (item 3)
    //   crowd     the grandstands' fans (Crowd, ported from Gridiron Glory): counted, animated, pictures from the racer's view as it passes,
    //             frame time with the crowd and without (-noCrowd runs the same lap without it) (item 3b)
    //   bike      the Scrambler: the lean into turns, the rider astride, the glass sphere on the sub (on the design given by -design, or the
    //             placeholder "_Scrambler" from Tools/Blender/make_placeholder_designs.py) (item 3c)
    //   designs   every imported design in the Garage, every form, and which design each rival races (item 3c)
    //   round9 = steering + sounds + crowd + bike + designs (then round1 for item 4, soak for item 5)
    public sealed partial class CheckPilot
    {
        // ---- item 3: one sound per event ----
        IEnumerator SoundsCheck()
        {
            scenario = "sounds"; Clear(); Settings.Difficulty = Difficulty.Normal;
            var events = new List<(float t, string what)>();
            Action<string, string> onEvent = (k, t) => events.Add((Time.time, k == "gate" ? (t.Contains("near miss") ? "gate (near miss)" : t.Contains("G10") ? "gate G10 (the finish line)" : "gate") : k == "change" ? $"change into {Game.FormName(v.Mode)}" : k));
            Action<string> onStunt = s => events.Add((Time.time, "stunt " + s));
            Action<float, float, string> onWall = (a, keep, w) => events.Add((Time.time, keep > .85f ? "wall (shallow)" : "wall hit"));
            v.Event += onEvent; v.Stunt += onStunt; v.WallHit += onWall;
            SoundLog.Entries.Clear();
            game.StartCourse(0, true); float start = Time.time; events.Add((start, "race start (grid)"));
            var pilot = v.gameObject.AddComponent<RivalPilot>(); pilot.rival = new Rival { name = "Abi (AI)", lane = 0, brakeMargin = .9f, boostMeter = .6f, boostStraight = 9f, seed = 77 }; pilot.AlwaysOn = true; aiDrives = true;
            yield return Until(() => !v.Holding, 8f); float go = Time.time;
            bool resetDone = false, missDone = false;
            while (!v.Finished && Time.time - go < 260f)
            {
                if (!resetDone && v.Progress > Mk("climbMid")) { resetDone = true; input.RequestReset(); }
                if (!missDone && v.Progress > Mk("bend2") && v.Mode == Mode.Boat) { missDone = true; pilot.missOffset = 0; }
                yield return null;
            }
            float fin = Time.time; yield return Until(() => game.Trial.HasResults, 5f); events.Add((Time.time, "results")); yield return Wait(7f);
            StopAi(); v.Event -= onEvent; v.Stunt -= onStunt; v.WallHit -= onWall;
            var log = SoundLog.Entries.Where(e => e.t >= start - .1f).ToList();
            // event by event: the sounds started from 0.05 s before to 0.6 s after it (voices and ambience listed, never counted as doubled)
            var byKind = new Dictionary<string, (int n, int doubled, HashSet<string> sounds)>(); var doubles = new List<string>();
            foreach (var ev in events)
            {
                var near = log.Where(e => e.t >= ev.t - .05f && e.t <= ev.t + .6f).ToList();
                if (!byKind.TryGetValue(ev.what, out var row)) row = (0, 0, new HashSet<string>());
                row.n++; foreach (var e in near) row.sounds.Add($"{e.kind}: {Clean(e.what)}");
                var twice = near.Where(e => SoundLog.Exclusive(e.kind)).GroupBy(e => e.kind).Where(g => g.Count() > 1).ToList();
                if (twice.Count > 0) { row.doubled++; doubles.Add($"{ev.t - go:0.0} s {ev.what}: {string.Join("; ", twice.Select(g => $"{g.Count()} x {g.Key} ({string.Join(", ", g.Select(x => Clean(x.what)))})"))}"); }
                byKind[ev.what] = row;
            }
            Line($"SOUNDS (item 3): a race on Normal, {fin - go:0.0} s, {events.Count} events, {log.Count} sounds started; each event with the sounds started from 0.05 s before to 0.6 s after it:");
            foreach (var kv in byKind.OrderBy(k => events.First(e => e.what == k.Key).t))
                Line($"  {kv.Key} (x{kv.Value.n}): {(kv.Value.sounds.Count == 0 ? "(nothing)" : string.Join(" | ", kv.Value.sounds.OrderBy(s => s)))}{(kv.Value.doubled > 0 ? $"  DOUBLED {kv.Value.doubled}x" : "")}");
            // music: every start, and whether two music / jingle cues ever sound at once (a song plays until the next music entry replaces it)
            var music = log.Where(e => e.kind == "music" || e.kind == "jingle").ToList();
            int jingles = log.Count(e => e.kind == "jingle");
            Line($"  music / jingle starts: {string.Join(", ", music.Select(m => $"{m.t - go:0.0} s {m.kind} {m.what}"))}; jingles {jingles}");
            foreach (var d in doubles) Line("  DOUBLE " + d);
            Line($"SOUNDS {(doubles.Count == 0 && jingles == 0 ? "PASS" : "CHECK")}: {doubles.Count} events with two sounds of one kind (two music / jingle, crowd, splash, whoosh, impact or chime cues at once); Tee'd Off's win jingle played {jingles}x; sounds skipped by the one-of-a-kind rule: {SoundLog.Merged}");
            Save();
        }
        static string Clean(string s) => s.Replace("(Clone)", "").Trim();

        // ---- item 3b: the crowd ----
        IEnumerator CrowdCheck()
        {
            scenario = "crowd"; Clear(); Settings.Difficulty = Difficulty.Normal;
            Line($"CROWD (item 3b): {(Crowd.Off ? "OFF (-noCrowd: the frame-time comparison run)" : $"{Crowd.All.Count} stands, {Crowd.Total} fans (Gridiron Glory's instanced fan sprites)")}: {string.Join(", ", Crowd.All.Select(c => $"{c.StandName} {c.Fans}"))}");
            game.StartCourse(0, true); Crowd.CpuTotal = 0; Crowd.CpuFrames = 0;
            foreach (var c in Crowd.All) Line($"  {c.StandName}: centre {c.Center.ToString("F0")}, drawn by the camera at the grid: {c.DrawnLastFrame}");
            Shot(1.2f, "grid"); yield return Wait(1.5f);
            var pilot = v.gameObject.AddComponent<RivalPilot>(); pilot.rival = new Rival { name = "Abi (AI)", lane = 0, brakeMargin = .9f, boostMeter = .6f, boostStraight = 9f, seed = 77 }; pilot.AlwaysOn = true; aiDrives = true;
            yield return Until(() => !v.Holding, 8f); float go = Time.time;
            Shot(.7f, "go-cheer"); Shot(1.6f, "go-cheer-2");
            var shot = new HashSet<Crowd>(); var cheerAt = new Dictionary<Crowd, int>();
            while (!v.Finished && Time.time - go < 260f)
            {
                foreach (var c in Crowd.All)
                {
                    if (Time.time - go < 5f || shot.Contains(c)) continue;
                    if (c.DistanceTo(v.transform.position) < 26f) { shot.Add(c); Capture($"pass-{ShotName(c.StandName)}"); yield return Wait(.6f); Capture($"pass-{ShotName(c.StandName)}-cheering"); cheerAt[c] = c.Cheers; }
                }
                yield return null;
            }
            Shot(.5f, "finish"); Shot(1.5f, "finish-2"); yield return Wait(2.5f);
            StopAi();
            foreach (var c in Crowd.All) Line($"  {c.StandName}: {c.Fans} fans, cheers {c.Cheers} (a racer passing within reach, the GO, the finish), last {(c.LastCheer > 0 ? $"{c.LastCheer - go:0.0} s into the race" : "never")}");
            Line($"CROWD {(Crowd.Off || (Crowd.All.Count >= 4 && Crowd.All.All(c => c.Cheers > 0)) ? "PASS" : "CHECK")}: {Crowd.Total} fans in {Crowd.All.Count} stands, every stand cheered: {Crowd.All.All(c => c.Cheers > 0)}; the crowd's own CPU time {Crowd.CpuTotal / Mathf.Max(1, Crowd.CpuFrames):0.000} ms a frame (all stands; the GPU cost is in the frame time below, compared with a -noCrowd run)");
            Save();
        }

        // ---- item 3c: the Scrambler: lean, rider astride, handlebars, the glass sphere ----
        IEnumerator BikeCheck()
        {
            scenario = "bike"; Clear();
            string name = Settings.Design; { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-testDesign"); if (i >= 0 && i + 1 < a.Length) name = a[i + 1]; }
            if (name.StartsWith("_")) VehicleDesigns.AddHiddenForCheck(name);
            var d = VehicleDesigns.Find(name);
            if (d == null || d.lean == null) { Line($"BIKE: no motorcycle design '{name}' (designs.json \"lean\"); skipped"); yield break; }
            game.UseDesign(name); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
            var lean = v.GetComponent<BikeLean>(); var drv = Driver.Instance; var views = cam.GetComponent<CameraViews>();
            Line($"BIKE (item 3c): design {name}: forms {string.Join("/", d.forms)}, steering {string.Join("/", d.steering ?? new string[0])}, canopy {string.Join("/", FormSet.All.Select(d.CanopyOf))}, astride {string.Join("/", d.astride ?? new string[0])}, lean {string.Join("/", d.lean)}");
            // the lean: the course-following pilot with a weave on top, on the promenade (car form)
            steerWeave = () => .22f * Mathf.Sin((Time.time) * 2.4f); yield return Wait(4f);
            float maxRoll = 0, at = 0; float end = Time.time + 8f; bool shotL = false, shotR = false;
            while (Time.time < end)
            {
                if (Mathf.Abs(lean.Roll) > Mathf.Abs(maxRoll)) { maxRoll = lean.Roll; at = v.Speed; }
                if (!shotL && lean.Roll > 18f) { shotL = true; Capture("lean-left-chase"); }
                if (!shotR && lean.Roll < -18f) { shotR = true; Capture("lean-right-chase"); }
                yield return null;
            }
            Line($"  lean: up to {Mathf.Abs(maxRoll):0} deg ({(maxRoll > 0 ? "left" : "right")}) at {at:0} m/s in the weave (max {BikeLean.MaxLean:0}); leaning now {lean.Leaning}; the hull's own roll {Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(v.transform.up, v.transform.forward), v.transform.forward):0.0} deg (the handling is the hull's: unchanged)");
            steerWeave = null; Clear(); coast = true;
            // every form, on the grid (held: no reset can interrupt): the rider astride, the bars, the sphere on the sub; chase, first person, sides
            foreach (var m in FormSet.All)
            {
                game.StartCourse(0, false); yield return Wait(.6f); v.Forms.Snap(m); yield return Wait(.6f);
                var model = v.Forms.ModelOf(m); bool sphere = model && model.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Glass sphere")), bubble = model && model.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Canopy (game)"));
                var st = drv ? drv.Steering : null; var e = drv ? drv.GripError() : (0f, 0f);
                var sm = st ? st.Shown : null; var pv = v.Forms.Pivot;
                Line($"  {Game.FormName(m)}: rider astride {(drv ? drv.RidingAstride : false)} (scale {(drv ? drv.Scale : 0):0.00}), mechanism {(st ? st.KindOf(m) : "-")} at {(sm != null ? pv.InverseTransformPoint(sm.root.position).ToString("F2") : "-")}, seat {v.Forms.SeatOf(m).ToString("F2")}, hands {e.Item1 * 100:0} / {e.Item2 * 100:0} cm off the grips (IK {(drv ? drv.IkWeight : 0):0.00}), glass sphere {sphere}, bubble canopy {bubble}");
                foreach (var view in new[] { CameraViews.View.Chase, CameraViews.View.FirstPerson })
                { views.SetView(view); yield return Wait(.5f); Capture($"{Game.FormName(m).ToLowerInvariant()}-{ShotName(view.ToString())}"); }
                views.SetView(CameraViews.View.Chase); yield return Wait(.2f);
                Side($"{Game.FormName(m).ToLowerInvariant()}-side", 1f); Side($"{Game.FormName(m).ToLowerInvariant()}-three-quarter", .55f);
                if (m == Mode.Drive) { BikeLean.Forced = 30f; yield return Wait(.8f); Side("car-forced-lean-30-front", -.15f); Capture("car-forced-lean-30-chase"); BikeLean.Forced = float.NaN; yield return Wait(.5f); }
            }
            v.Forms.Snap(Mode.Drive); Clear(); game.UseDesign(VehicleDesigns.Default); yield return Wait(.3f);
            Save();
        }

        // a picture from beside the racer (side 1 = from its right, 0.55 = three quarters from the front right, -0.15 = nearly from the front)
        void Side(string name, float side)
        {
            var g = new GameObject("side camera"); var c = g.AddComponent<Camera>(); c.CopyFrom(cam); c.fieldOfView = 40;
            var t = v.transform; var dir = (t.right * side + t.forward * (1 - Mathf.Abs(side)) * 1.4f).normalized;
            c.transform.position = t.position + dir * 6.5f + Vector3.up * 3.2f; c.transform.LookAt(t.position + t.up * .7f);
            Capture(name, c); Destroy(g);
        }

        // ---- item 3c: who races what ----
        void RivalDesigns()
        {
            Line($"DESIGNS (item 3c): {string.Join(", ", VehicleDesigns.Imported.Select(d => $"{d.name} ({string.Join("/", d.forms)}{(string.IsNullOrEmpty(d.rival) ? "" : ", rival " + d.rival)})"))}");
            foreach (var r in game.Rivals)
            {
                var fs = r.GetComponentInChildren<FormSet>(true);
                Line($"  {r.RacerName}: races {fs.DesignName}{(string.IsNullOrEmpty(fs.OwnDesign) ? " (the player's design, recoloured)" : " (its own design)")}");
            }
        }
    }
}
