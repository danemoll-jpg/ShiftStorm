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
        // Round 8b item 2: a dinosaur counts as seen only while its box covers DinoActor.BigShare (8 %) of the screen's height in the
        // player's chase view (in the frame, not behind rock, not lost in the underwater fog), and only a stretch of 1 s or more counts.
        // Build 9's check counted anything in the frame at all, a few pixels tall: it passed when it should not have.
        readonly Dictionary<DinoActor, (float run, float peak, string seg)> dinoRun = new Dictionary<DinoActor, (float, float, string)>();
        readonly List<(string seg, DinoActor d, float len, float peak)> dinoRuns = new List<(string, DinoActor, float, float)>();
        readonly HashSet<DinoActor> dinoShot = new HashSet<DinoActor>(); bool dinoCount;
        readonly List<(string scen, float prog, string name)> progShots = new List<(string, float, string)>();
        static string ShotName(string x) => new string(x.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-').ToArray()).Replace("--", "-").Trim('-');

        void DinoSample()
        {
            if (!dinoCount) return;
            float dt = Time.deltaTime;
            foreach (var d in DinoActor.All)
            {
                float sh = segNow == "" ? 0 : d.ScreenShare(cam);
                dinoRun.TryGetValue(d, out var r);
                // a stretch is split where the segment changes (each segment gets the time spent in it)
                if (r.run > 0 && (sh < DinoActor.BigShare || r.seg != segNow)) { if (r.run >= .2f) dinoRuns.Add((r.seg, d, r.run, r.peak)); r = default; dinoRun[d] = r; }
                if (sh >= DinoActor.BigShare)
                {
                    if (r.run == 0) r.seg = segNow; r.run += dt; r.peak = Mathf.Max(r.peak, sh); dinoRun[d] = r;
                    // a picture once it fills a fair part of the screen (a quarter of its height), set-pieces first
                    if (r.run >= .3f && sh >= .25f && (d.setPiece || dinoShot.Count < 30) && dinoShot.Add(d)) Shot(0, $"dino-{ShotName(r.seg.Split(' ')[0])}-{ShotName(d.label)}");
                }
            }
        }

        IEnumerator DinosCheck()
        {
            dinoRun.Clear(); dinoRuns.Clear(); dinoShot.Clear(); Caller.DinoLines.Clear(); dinoCount = true;
            var c = game.Courses[0];
            Line($"DINOS (Round 8b): {DinoActor.All.Count} dinosaurs and skeletons ({string.Join(", ", DinoActor.All.GroupBy(d => d.style).Select(g => $"{g.Key} {g.Count()}"))}), {DinoActor.All.Count(d => d.setPiece)} set-pieces; stand-ins (no model yet): {string.Join(", ", DinoActor.All.Where(d => d.standIn).Select(d => d.style).Distinct())}");
            Line($"  sizes: {string.Join(", ", DinoActor.All.Where(d => d.style != "skeleton").GroupBy(d => d.style).Select(g => $"{g.Key} {g.Min(d => d.size.y):0.#}-{g.Max(d => d.size.y):0.#} m tall" + (g.Key == "ptero" ? $" ({g.Max(d => d.height):0} m wingspan)" : "")))}; rigs: {string.Join(", ", DinoRig.Styles.Select(s => $"{s} {DinoRig.Triangles(s) / 1000.0:0.0}k tris"))}");
            Line($"  placement notes: {(ParadisePlunge.DinoSkipped.Count == 0 ? "none" : string.Join("; ", ParadisePlunge.DinoSkipped))}");
            // clearance: every standing / walking dinosaur's feet against the road's edge (the neck bridge reaches over it on purpose; pacers keep their side offset)
            float worst = 1e9f; string worstWho = "";
            foreach (var d in DinoActor.All)
            {
                if (d.kind == DinoActor.Kind.Pace || d.style == "skeleton") continue;
                var pts = new List<Vector3>();
                if (d.kind == DinoActor.Kind.Swoop) for (int i = 0; i < 24; i++) { float t = i * Mathf.PI * 2 / 24; pts.Add(d.a + new Vector3(Mathf.Cos(t) * d.b.x, Mathf.Sin(t * 2) * d.b.z, Mathf.Sin(t) * d.b.x)); }
                else for (int i = 0; i <= 6; i++) pts.Add(Vector3.Lerp(d.a, d.b, i / 6f));
                foreach (var p in pts)
                {
                    float dist = ParadisePlunge.LineDistance(p, out float flat, out float prog);
                    if (ParadisePlunge.Underground(c.PointAt(prog, out _), p)) continue;   // over the cave
                    float clr = d.kind == DinoActor.Kind.Swoop || d.style == "ptero" || dist - flat > 20f ? dist : flat - ParadisePlunge.RoadHalfAt(prog);
                    if (clr < worst) { worst = clr; worstWho = d.label; }
                }
            }
            Line($"  ROUTE CLEARANCE: the nearest any standing / walking dinosaur's feet come to the road's edge (pterodactyl circles: to the line in 3D): {worst:0.0} m ({worstWho}){(worst > 4 ? "  PASS" : "  CHECK")}; pacers: {string.Join(", ", DinoActor.All.Where(d => d.kind == DinoActor.Kind.Pace).Select(d => $"{d.label} {Mathf.Min(Mathf.Abs(d.sideStart), Mathf.Abs(d.sideEnd)):0} m from the line" + (d.paceFly ? $", {d.lift:0} m over it" : "")))}");
            float bridge = ParadisePlunge.Marks["promMid"] + 75;
            progShots.Clear(); progShots.Add(("lap", bridge - 70, "dino-neck-bridge-ahead")); progShots.Add(("lap", bridge - 25, "dino-neck-bridge-under"));
            yield return Lap();
            dinoCount = false; progShots.Clear();
            foreach (var kv in dinoRun) if (kv.Value.run >= .2f) dinoRuns.Add((kv.Value.seg, kv.Key, kv.Value.run, kv.Value.peak));
            dinoRun.Clear();
            var segs = c.segments.Select(s => s.name).ToList(); int good = 0;
            Line($"  DINOS SEEN per segment (item 2: >= {DinoActor.BigShare * 100:0} % of the screen height for >= 1 s, chase view, the lap pilot at racing speed):");
            Line("    segment            seen  longest   time big   moments of 1 s+ (seconds, peak share of the screen height)");
            foreach (var s in segs)
            {
                var runs = dinoRuns.Where(r => r.seg == s && r.len >= 1f).OrderByDescending(r => r.len).ToList();
                float longest = runs.Count > 0 ? runs[0].len : 0, total = dinoRuns.Where(r => r.seg == s).Sum(r => r.len);
                if (longest >= 2f) good++;
                Line($"    {s,-18} {runs.Select(r => r.d).Distinct().Count(),4}  {longest,5:0.0} s  {total,6:0.0} s   {(runs.Count == 0 ? "-" : string.Join(", ", runs.Take(8).Select(r => $"{r.d.label} {r.len:0.0} s {r.peak * 100:0}%")))}");
            }
            Line($"  DINOS {(good == segs.Count ? "PASS" : $"{good} of {segs.Count}")}: segments with a moment of 2 s+ big on screen: {good} of {segs.Count}");
            var sp = DinoActor.All.Where(d => d.setPiece).ToList();
            Line($"  SET-PIECES ({sp.Count}): {string.Join("; ", sp.Select(d => { var rr = dinoRuns.Where(r => r.d == d).ToList(); return $"{d.label} ({d.segment}): {(rr.Count == 0 ? "NOT SEEN" : $"{rr.Max(r => r.len):0.0} s, peak {rr.Max(r => r.peak) * 100:0}%")}"; }))}");
            var dl = Caller.DinoLines;
            Line($"  BARKER's dinosaur lines (said only while one of that kind is big on screen): {dl.Count} in the lap, {dl.Count(x => x.share >= DinoActor.BigShare)} with it >= {DinoActor.BigShare * 100:0} % at that moment{(dl.Count > 0 && dl.All(x => x.share >= DinoActor.BigShare) ? "  PASS" : "")}: {string.Join(", ", dl.Select(x => $"{x.pool} {x.share * 100:0}%"))}");
            Line($"  T. rex roars {DinoActor.All.Where(d => d.kind == DinoActor.Kind.Roar).Sum(d => d.Roars)}, last camera shake {(ChaseCam.LastShake > 0 ? "yes" : "no")}");
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
                yield return Until(() => Caller.Instance.Lines.Any(l => (l.pool == "results" || l.pool == "ppres" || l.pool == "ccres") || l.pool == "podium" || l.pool == "last"), 12f);
                var res = Caller.Instance.Lines.LastOrDefault(l => (l.pool == "results" || l.pool == "ppres" || l.pool == "ccres") || l.pool == "podium" || l.pool == "last");
                string kind = place == 1 ? "1st" : place == field ? "last" : "middle";
                string clip = d.Played.Count > 0 ? d.Played.Last() : "-";
                bool ok = kind == "1st" ? clip == "Sitting Clap" : kind == "last" ? clip == "Sitting Disapproval" : clip == "Sitting Thumbs Up";
                bool lineOk = kind == "1st" ? (res.pool == "results" || res.pool == "ppres" || res.pool == "ccres") :   // Round 10: the course's own results lines
                              kind == "last" ? res.pool == "last" : res.pool == "podium";
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
            Line($"STEERING (Round 8 item 4, Round 9 item 1): {string.Join(", ", FormSet.All.Select(m => $"{Game.FormName(m)} {st.KindOf(m)}"))}; driver's eyes at {st.Eye.ToString("F2")} in the vehicle, wheel centre {st.Anchor.ToString("F2")}, grips {st.Span:0.00} m apart (driver scale {st.DriverScale:0.00})");
            Steering.ForceInput = true; float worst = 0, worstShare = 0; var rows = new List<string>();
            // Round 9: the share of the screen height each mechanism covers in first person: at rest (FOV 70), at top speed (85) and on boost (90)
            float[] fovs = { CameraViews.FirstPersonFov(ChaseCam.BaseFov), CameraViews.FirstPersonFov(ChaseCam.BaseFov + 25f), CameraViews.FirstPersonFov(ChaseCam.BaseFov + 33f) };
            // the visible share: the camera's picture rendered with and without the mechanism, the rows that differ (the mechanism's parts behind
            // the cockpit rim or the hull do not count: they are not seen)
            Color32[] Grab() { cam.Render(); var prev = RenderTexture.active; RenderTexture.active = rt; grab.ReadPixels(new Rect(0, 0, W, H), 0, 0); grab.Apply(); RenderTexture.active = prev; return grab.GetPixels32(); }
            float Rendered()
            {
                var rs = st.Shown.root.GetComponentsInChildren<Renderer>(); var with = Grab();
                foreach (var r in rs) r.enabled = false; var without = Grab(); foreach (var r in rs) r.enabled = true;
                int lo = H, hi = -1;
                for (int y = 0; y < H; y++)
                {
                    int diff = 0;
                    for (int x = 0; x < W; x += 2) { var a = with[y * W + x]; var b = without[y * W + x]; if (Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) > 24 && ++diff > 3) break; }
                    if (diff > 3) { lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); }
                }
                if (Steering.ForcedSteer == 0 && cam.fieldOfView < 71f)   // the evidence: the mechanism's own pixels (the difference), per form
                {
                    var dimg = new Texture2D(W, H, TextureFormat.RGB24, false); var dp = new Color32[W * H];
                    for (int i = 0; i < dp.Length; i++) { var a = with[i]; var b = without[i]; bool d = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) > 24; dp[i] = d ? a : new Color32((byte)(b.r / 4), (byte)(b.g / 4), (byte)(b.b / 4), 255); }
                    dimg.SetPixels32(dp); dimg.Apply(); File.WriteAllBytes(Path.Combine(dir, $"steering-{st.Shown.kind}-only.jpg"), dimg.EncodeToJPG(85)); Destroy(dimg);
                }
                shotLastFrame = true; return hi < 0 ? 0 : (hi - lo + 1) / (float)H;
            }
            string Shares() { var keep = cam.fieldOfView; var o = string.Join(" / ", fovs.Select(f => { cam.fieldOfView = f; float sh = Rendered(); worstShare = Mathf.Max(worstShare, sh); return $"{sh * 100:0} %"; })); cam.fieldOfView = keep; return o; }
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
                        if (view == CameraViews.View.FirstPerson) rows.Add($"{Game.FormName(m)} {(s < 0 ? "left" : s > 0 ? "right" : "centre")}: {st.Shown.kind} at {st.Shown.turn:0} deg, screen height covered {Shares()} (FOV {fovs[0]:0} / {fovs[1]:0} / {fovs[2]:0}: rest / top speed / boost), hands {e.l * 100:0} / {e.r * 100:0} cm off the grips, IK {d.IkWeight:0.00}");
                        else if (s == 0) rows.Add($"{Game.FormName(m)} chase: {st.Shown.kind} covers {st.ScreenShare(cam) * 100:0.0} % of the screen height");
                        Capture($"{Game.FormName(m).ToLowerInvariant()}-{(view == CameraViews.View.FirstPerson ? "first-person" : "chase")}-{(s < 0 ? "left" : s > 0 ? "right" : "centre")}");
                    }
                }
            }
            Steering.ForceInput = false; views.SetView(CameraViews.View.Chase); v.Forms.Snap(v.Mode);
            foreach (var r in rows) Line("  " + r);
            Line($"STEERING SIZE {(worstShare <= .2f ? "PASS" : "CHECK")}: the largest first-person share of the screen height {worstShare * 100:0} % (Dan: no more than ~15-20 %; build 10: ~65 %)");
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
