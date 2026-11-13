using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

namespace ShiftStorm
{
    // Check runs in the built exe ("-check <which> -checkOut <dir>", always muted, -batchmode: no window, never takes focus).
    // A scripted pilot drives through the player's own input path (ShiftInput.Scripted), aiming along the course line a little ahead, with the
    // line's height as its climb / dive target; Round 2 adds speed control before bends (the line's curvature ahead), drift / carve in bends,
    // and it reads its place on the line from the vehicle's own progress (stack-safe). The camera renders into a 1280 x 800 texture (the
    // Steam Deck's screen); player-view pictures (HUD included) go to <dir>. The measured audio output level is reported (it must be 0).
    //   round2 = lap + skip + abi; round1 = strip + lanes + edge (Round 1 regression); all = both; best = the best-time survival check
    //   (a second process with the same -saveDir).
    public sealed class CheckPilot : MonoBehaviour
    {
        Game game; ShiftVehicle v; ShiftInput input; Camera cam; RenderTexture rt; Texture2D grab; AudioMeter meter;
        const int W = 1280, H = 800;
        string dir; readonly StringBuilder report = new StringBuilder();
        readonly List<(float t, float dt, bool shot, float gpu, string seg)> frames = new List<(float, float, bool, float, string)>(); bool shotLastFrame;
        readonly List<(float at, string name)> shots = new List<(float, string)>();
        readonly List<ChangeRecord> changes = new List<ChangeRecord>();
        string scenario = ""; int shotIndex; string segNow = "";
        class ChangeRecord { public string scenario, what; public float at, took = -1, z; }
        readonly FrameTiming[] timing = new FrameTiming[1];
        // item 6: in batch mode nothing renders by itself, so during the lap every frame is rendered here and the GPU is waited for
        // (a 1-pixel read back): that time is the PC's CPU render + GPU cost of the frame at 1280 x 800. Draw calls / triangles from the
        // render counters where the player has them.
        bool measure; Texture2D px; readonly List<(string seg, float ms, long draws, long tris, long batches)> render = new List<(string, float, long, long, long)>();
        ProfilerRecorder draws, tris, batches; readonly System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        // pilot settings, per scenario
        float holdSpeed = -1; bool boost, secondary, reverse, smart; Func<float> steerWeave, yOverride; float throttleCap = 1;

        void Start()
        {
            game = Game.Instance; v = game.Player; input = v.Input; cam = game.Cam; meter = cam.GetComponent<AudioMeter>();
            dir = QuietRun.Dir; Directory.CreateDirectory(dir);
            rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; grab = new Texture2D(W, H, TextureFormat.RGB24, false);
            input.Scripted = true;
            v.Event += OnEvent;
            StartCoroutine(Run());
        }

        void OnEvent(string kind, string text)
        {
            if (kind == "change") { changes.Add(new ChangeRecord { scenario = scenario, what = $"{v.ChangeFrom}->{v.Mode}", at = Time.time, z = v.Body.position.z }); Shot(.2f, $"{v.ChangeFrom}-{v.Mode}-mid"); }
            else if (kind == "changed") { if (changes.Count > 0) changes[changes.Count - 1].took = v.LastChangeSeconds; }
            else if (kind == "reset") { if (changes.Count > 0 && changes[changes.Count - 1].took < 0) changes[changes.Count - 1].took = -2; Shot(.3f, "after-reset"); }
        }
        void Shot(float delay, string name) => shots.Add((Time.time + delay, name));

        void Update()
        {
            float gpu = -1;
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timing) > 0) gpu = (float)timing[0].gpuFrameTime;
            frames.Add((Time.time, Time.unscaledDeltaTime, shotLastFrame, gpu, segNow)); shotLastFrame = false;
            if (measure)
            {
                if (!px) { px = new Texture2D(1, 1, TextureFormat.RGB24, false); draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"); tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count"); batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count"); }
                sw.Restart(); cam.Render(); var prev = RenderTexture.active; RenderTexture.active = rt; px.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); px.Apply(); RenderTexture.active = prev; sw.Stop();
                render.Add((segNow, (float)sw.Elapsed.TotalMilliseconds, draws.Valid ? draws.LastValue : -1, tris.Valid ? tris.LastValue : -1, batches.Valid ? batches.LastValue : -1));
            }
            for (int i = shots.Count - 1; i >= 0; i--) if (Time.time >= shots[i].at) { Capture(shots[i].name); shots.RemoveAt(i); }
        }
        void Capture(string name, Camera with = null)
        {
            if (with) { with.targetTexture = rt; with.Render(); with.targetTexture = null; } else cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            grab.ReadPixels(new Rect(0, 0, W, H), 0, 0); grab.Apply(); RenderTexture.active = prev;
            shotLastFrame = true; var file = Path.Combine(dir, $"{++shotIndex:00}-{scenario}-{name}.jpg"); File.WriteAllBytes(file, grab.EncodeToJPG(88));
        }

        void FixedUpdate()
        {
            if (!v || !v.Course) return;
            var p = v.Body.position; var c = v.Course;
            float prog = v.Progress;
            float ahead = v.Mode == Mode.Dive ? Mathf.Max(14f, v.Speed * .9f) : Mathf.Max(18f, v.Speed * 1.1f);
            var look = c.PointAt(prog + ahead, out _);
            var fwd = Vector3.ProjectOnPlane(v.transform.forward, Vector3.up).normalized;
            var want = Vector3.ProjectOnPlane(look - p, Vector3.up).normalized;
            float err = Vector3.SignedAngle(fwd, want, Vector3.up);
            float steer = steerWeave != null ? Mathf.Clamp(err / 90f, -.3f, .3f) + steerWeave() : Mathf.Clamp(err / 20f, -1, 1);
            float ty = yOverride != null ? yOverride() : look.y;
            float vy = v.Body.linearVelocity.y;
            float vert = Mathf.Clamp((ty - p.y) * .15f - vy * .12f, -1, 1);
            float thr = throttleCap, brk = 0; bool sec = secondary;
            if (holdSpeed > 0) thr = v.Speed < holdSpeed ? 1 : 0;
            if (smart && (v.Mode == Mode.Drive || v.Mode == Mode.Boat))
            {
                // the line's bend ahead sets a safe speed (v = sqrt(a r)); over it: off the throttle, brake; drift / carve through bends
                float d0 = 6, d1 = 6 + Mathf.Max(25f, v.Speed * 1.6f);
                c.PointAt(prog + d0, out var a0); c.PointAt(prog + d1, out var a1);
                float turn = Vector3.Angle(Vector3.ProjectOnPlane(a0, Vector3.up), Vector3.ProjectOnPlane(a1, Vector3.up)) * Mathf.Deg2Rad;
                float r = turn > 1e-3f ? (d1 - d0) / turn : 1e4f;
                float aLat = v.Mode == Mode.Drive ? 17f : 10f, vmax = Mathf.Sqrt(aLat * r);
                if (v.Speed > vmax + 1.5f) { thr = 0; brk = Mathf.Clamp01((v.Speed - vmax) / 8f); }
                sec |= v.Mode == Mode.Drive ? Mathf.Abs(err) > 22f && v.Speed > 14f : Mathf.Abs(err) > 14f;
            }
            input.sThrottle = reverse ? 0 : thr; input.sBrake = reverse ? 1 : brk; input.sSteer = reverse ? 0 : Mathf.Clamp(steer, -1, 1);
            input.sVertical = vert; input.sBoost = boost; input.sSecondary = sec;
        }

        void Clear() { holdSpeed = -1; boost = secondary = reverse = smart = false; steerWeave = null; yOverride = null; throttleCap = 1; v.debugEntryPitch = float.NaN; }
        IEnumerator Until(Func<bool> done, float timeout) { float end = Time.time + timeout; while (!done() && Time.time < end) yield return null; }
        IEnumerator Wait(float s) { float end = Time.time + s; while (Time.time < end) yield return null; }
        void Line(string s) { report.AppendLine(s); Debug.Log("[Check] " + s); }
        void BeginAt(int course, int anchor) { game.StartCourse(course); if (anchor > 0) v.Begin(v.Course, v.Course.anchors[anchor]); }
        List<ChangeRecord> Of(string s) => changes.FindAll(x => x.scenario == s);
        static float Mk(string n) => ParadisePlunge.Marks[n];
        int AnchorNear(float progress) { var a = v.Course.anchors; int best = 0; for (int i = 0; i < a.Count; i++) if (Mathf.Abs(a[i].progress - progress) < Mathf.Abs(a[best].progress - progress)) best = i; return best; }

        IEnumerator Run()
        {
            string which = "all"; var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-check"); if (i >= 0 && i + 1 < a.Length) which = a[i + 1];
            yield return Wait(1f);
            Line($"ShiftStorm check '{which}', {DateTime.Now:yyyy-MM-dd HH:mm}, build {Application.version}, {SystemInfo.graphicsDeviceName}, {W}x{H} off-screen");
            Line($"model: {VehicleFactory.ModelSource}; driver: {Driver.Instance?.Source}");
            Line($"muted from the first frame: {QuietRun.Muted}; AudioListener.volume = {AudioListener.volume}; audio output {AudioSettings.outputSampleRate} Hz");
            bool r2 = which == "round2" || which == "all", r1 = which == "round1" || which == "all";
            if (r2 || which == "lap") yield return Lap();
            if (r2 || which == "skip") yield return Skip();
            if (r2 || which == "abi") yield return Abi();
            if (which == "best") yield return Best();
            if (r1 || which == "strip") yield return Strip();
            if (r1 || which == "lanes") yield return Lanes();
            if (r1 || which == "edge") yield return Edges();
            Frames();
            var au = CourseAudio.Instance;
            Line($"sound: music {(au ? au.MusicClip : "-")} {(au && au.MusicPlaying ? "playing" : "not playing")} (its own signal peak {(au ? au.MusicPeak : 0):0.000}), splashes played {(au ? au.Splashes : 0)}, Abi lines: {(Driver.Instance ? string.Join("; ", Driver.Instance.Spoken) : "-")}");
            if (Driver.Instance) Line($"Abi's clips: {(Driver.Instance.Animated ? $"{Driver.Instance.ClipCount} loaded, one-shots played: {string.Join(", ", Driver.Instance.Played)}" : "none (fixed seated pose)")}");
            Line($"audio output level (AudioListener.GetOutputData, all frames): peak {meter.MaxPeakSeen:0.000000}, last RMS {meter.Rms:0.000000}, AudioListener.volume {AudioListener.volume}");
            File.WriteAllText(Path.Combine(dir, "check_report.txt"), report.ToString());
            yield return Wait(.5f);
            Application.Quit(0);
        }

        void Frames()
        {
            var ms = new List<float>(); var gp = new List<float>(); float sum = 0, worst = 0;
            foreach (var f in frames) { if (f.t < 2f || f.shot) continue; ms.Add(f.dt * 1000); sum += f.dt * 1000; worst = Mathf.Max(worst, f.dt * 1000); if (f.gpu > 0) gp.Add(f.gpu); }
            ms.Sort(); gp.Sort();
            if (ms.Count > 0) Line($"frame time over the run (PC, {W}x{H} off-screen, uncapped, frames that saved a picture left out): mean {sum / ms.Count:0.00} ms, 95th pct {ms[(int)(ms.Count * .95f)]:0.00} ms, 99th pct {ms[(int)(ms.Count * .99f)]:0.00} ms, worst {worst:0.00} ms over {ms.Count} frames; GPU time {(gp.Count > 0 ? $"mean {Avg(gp):0.00} ms, 95th pct {gp[(int)(gp.Count * .95f)]:0.00} ms ({gp.Count} frames)" : "not available in batch mode")}");
            if (changes.Count > 0) Line("changes (scenario, change, z, visual time, worst / mean frame time from 0.25 s before to 0.25 s after):");
            foreach (var c in changes)
            {
                float end = c.at + Mathf.Max(c.took, .6f) + .25f, w = 0, s = 0; int k = 0;
                foreach (var f in frames) if (f.t >= c.at - .25f && f.t <= end && !f.shot) { w = Mathf.Max(w, f.dt); s += f.dt; k++; }
                Line($"  {c.scenario,-12} {c.what,-11} z {c.z,7:0.0}  {(c.took >= 0 ? $"{c.took:0.000} s" : c.took < -1 ? "cancelled by the reset" : "not finished")}  frames worst {1000 * w:0.00} / mean {1000 * s / Mathf.Max(k, 1):0.00} ms{(c.took >= .5f && c.took <= .8f ? "" : c.took < 0 ? "" : "  OUT OF 0.5-0.8 s")}");
            }
        }
        void RenderReport(Course c)
        {
            if (render.Count == 0) return;
            var all = new List<float>(); long maxDraw = 0, maxTri = 0, maxBatch = 0; foreach (var r in render) { all.Add(r.ms); maxDraw = Math.Max(maxDraw, r.draws); maxTri = Math.Max(maxTri, r.tris); maxBatch = Math.Max(maxBatch, r.batches); }
            all.Sort(); int over = all.FindAll(x => x > 16.7f).Count;
            Line($"PERF (item 6): every lap frame rendered at {W}x{H} and the GPU waited for (PC: {SystemInfo.graphicsDeviceName}, {SystemInfo.processorType.Trim()}): mean {Avg(all):0.00} ms, 95th pct {all[(int)(all.Count * .95f)]:0.00} ms, 99th pct {all[(int)(all.Count * .99f)]:0.00} ms, worst {all[all.Count - 1]:0.00} ms over {all.Count} frames ({over} over 16.7 ms); peak draw calls {maxDraw}, batches {maxBatch}, triangles {maxTri / 1000}k");
            Line("  per segment: render+GPU mean / 95th pct / worst ms, peak draw calls, peak triangles");
            foreach (var s in c.segments)
            {
                var l = new List<float>(); long d = 0, t = 0; foreach (var r in render) if (r.seg == s.name) { l.Add(r.ms); d = Math.Max(d, r.draws); t = Math.Max(t, r.tris); }
                if (l.Count == 0) continue; l.Sort();
                Line($"  {s.name,-18} {Avg(l),6:0.00} / {l[(int)(l.Count * .95f)],6:0.00} / {l[l.Count - 1],6:0.00} ms   {d,5} draws   {t / 1000,5}k tris");
            }
        }
        static float Avg(List<float> l) { float s = 0; foreach (var x in l) s += x; return l.Count > 0 ? s / l.Count : 0; }

        // ---- item 1/3/4/6: one full lap of Paradise Plunge from the grid, pictures of every segment, results, restart ----
        IEnumerator Lap()
        {
            scenario = "lap"; Clear(); smart = true; BeginAt(0, 0);
            var c = v.Course; int segs = c.segments.Count;
            Line($"LAP: Paradise Plunge, line {c.Length:0} m, {c.gates.Count} gates, {segs} segments, {c.anchors.Count} anchors");
            Shot(.6f, "grid-countdown");
            // player-view pictures along the lap: the approach to every change and one in each segment (is the next turn / change visible early?)
            var marks = new (string name, float p)[]
            {
                ("1-promenade", Mk("promMid") - 40), ("2-climb-corner1", Mk("corner1") - 55), ("2-climb", Mk("climbMid")), ("2-ramp-approach", Mk("rampStart") - 75),
                ("3-flight", Mk("lip") + 70), ("3-arch-ahead", Mk("arch") - 170), ("3-arch", Mk("arch") - 25), ("3-lagoon-approach", Mk("splash") - 140),
                ("4-lagoon", Mk("splash") + 80), ("5-mangrove", Mk("bend2") - 30), ("5-dive-entrance-ahead", Mk("diveZone") - 55),
                ("6-gateway", Mk("portal") - 8), ("7-vault-arch", Mk("hi1") - 40), ("7-vault-curtain", Mk("lo2") - 35), ("8-exit-channel", Mk("poolEdge") - 25),
                ("8-pool-ramp", Mk("G8") - 6), ("8-through-curtain", Mk("rampLip") + 3), ("8-finish-road", Mk("G9") + 25),
            };
            int next = 0; float t0 = Time.time; int resets0 = v.Resets;
            yield return Until(() => !v.Holding, 6f);
            measure = true;
            Line($"LAP countdown: held on the grid {v.StartTime - t0:0.0} s, then GO (clock started at GO)");
            while (!v.Finished && Time.time - v.StartTime < 400f)
            {
                segNow = v.Splits.Count < segs ? c.segments[v.Splits.Count].name : "finish";
                while (next < marks.Length && v.Progress >= marks[next].p) { Shot(0, marks[next].name); next++; }
                yield return null;
            }
            segNow = ""; measure = false;
            yield return Until(() => game.Trial.HasResults, 3f); yield return Wait(1.2f);
            Shot(0, "results"); yield return Wait(.2f);
            // gates in order (from the vehicle's own log), resets
            var gateLines = v.Log.FindAll(x => x.Contains("gate G") && x.Contains("passed")); bool ordered = true;
            for (int k = 0; k < gateLines.Count; k++) if (!gateLines[k].Contains($"G{k} ")) ordered = false;
            bool pass = v.Finished && v.Resets == resets0 && gateLines.Count == c.gates.Count && ordered;
            Line($"LAP {(pass ? "PASS" : "FAIL")}: finished {v.Finished} in {v.FinishTime:0.00} s (target 150), gates {v.GatesPassed}/{c.gates.Count} in order {ordered}, resets {v.Resets - resets0}{(v.Resets > resets0 ? " (" + v.LastResetReason + ")" : "")}, changes {Of("lap").Count}: {string.Join(", ", Of("lap").ConvertAll(x => x.what))}");
            Line("  segment               form              time   target   delta");
            float prev = 0;
            for (int k = 0; k < segs; k++)
            {
                var s = c.segments[k]; float t = k < v.Splits.Count ? v.Splits[k] - prev : -1; if (k < v.Splits.Count) prev = v.Splits[k];
                Line($"  {k + 1} {s.name,-19} {s.form,-16} {(t >= 0 ? $"{t,6:0.0} s" : "    --")}  {s.target,4:0} s  {(t >= 0 ? (t - s.target).ToString("+0.0;-0.0") : "")}");
            }
            foreach (var l in v.Log) if (l.Contains("missed") || l.Contains("rejected") || l.Contains("wrong way") || l.Contains("reset")) Line("  log: " + l);
            RenderReport(c);
            // results, best saved, restart
            var tt = game.Trial;
            Line($"RESULTS {(tt.HasResults && tt.SegmentTimes.Count == segs && tt.Best != null && File.Exists(BestTimes.FilePath) ? "PASS" : "FAIL")}: shown {tt.HasResults}, lap {tt.Lap:0.00} s, {tt.SegmentTimes.Count} splits, new best {tt.NewBest}, best on file {(tt.Best != null ? tt.Best.lap.ToString("0.00") : "none")} s ({BestTimes.FilePath})");
            input.RequestReset(); yield return Wait(.3f);
            bool restarted = v.Holding && !v.Finished && v.GatesPassed == 0 && v.Splits.Count == 0 && !tt.HasResults;
            Shot(0, "restart-countdown"); yield return Wait(.2f);
            Line($"RESTART {(restarted ? "PASS" : "FAIL")}: after the finish, Reset started the course again: countdown {v.Holding}, finished {v.Finished}, gates {v.GatesPassed}, results hidden {!tt.HasResults}, best kept {(BestTimes.Get(c.courseName)?.lap ?? -1):0.00} s");
            yield return Until(() => !v.Holding, 5f);
        }

        // ---- item 2: skipping the dive by air, crossing a gate backward, reaching the finish out of order ----
        IEnumerator Skip()
        {
            var c = Game.Instance.Courses[0];
            // 1 fly over the flooded cave instead of diving through it
            scenario = "skip-air"; Clear(); BeginAt(0, AnchorNear(Mk("G5") + 12));
            int g0 = v.GatesPassed; yield return Wait(.3f);
            var start = v.Body.position + Vector3.up * 40; v.DebugPlace(start, Quaternion.LookRotation(Vector3.left), Mode.Fly, 40);
            yOverride = () => 40f; Shot(1.5f, "flying-over-the-cave");
            int r0 = v.Resets; string cue = "";
            float until = Time.time + 30f;
            while (Time.time < until && v.Resets == r0) { if (v.Cue.StartsWith("MISSED") && cue == "") { cue = v.Cue; Shot(0, "missed-cue"); } yield return null; }
            yield return Wait(.5f);
            bool pass1 = v.Resets > r0 && v.LastResetReason.Contains("G6") && v.GatesPassed == g0 && !v.Finished && v.Mode == v.Course.AnchorFor(v.BestProgress, v.GatesPassed).mode;
            Line($"SKIP BY AIR {(pass1 ? "PASS (rejected)" : "FAIL")}: after G5 the check turned it into a plane 40 m up and flew the cave's line: cue \"{cue}\", then reset ({v.LastResetReason}) to the anchor at {v.Course.AnchorFor(v.BestProgress, v.GatesPassed).progress:0} m in {v.Mode}; gates {v.GatesPassed}/{c.gates.Count} (G6-G10 never counted), finished {v.Finished}");
            yOverride = null;
            // 2 back through a gate (wrong way): no gate is gained, the cue shows
            scenario = "skip-reverse"; Clear(); BeginAt(0, AnchorNear(Mk("start")));
            yield return Until(() => !v.Holding, 5f);
            yield return Until(() => v.GatesPassed >= 1 && v.Body.position.z > 25, 10f);
            reverse = true; bool cueSeen = false;
            float until2 = Time.time + 10f; while (Time.time < until2 && v.Body.position.z > -12) { cueSeen |= v.Cue == "WRONG WAY" && Time.time < v.CueUntil; yield return null; }
            reverse = false; int gAfter = v.GatesPassed;
            yield return Until(() => v.Body.position.z > 30, 10f);
            bool pass2 = cueSeen && gAfter == 1 && v.GatesPassed == 1 && v.Log.Exists(x => x.Contains("wrong way"));
            Line($"REVERSE CROSSING {(pass2 ? "PASS (rejected)" : "FAIL")}: through G0, back through it (cue WRONG WAY: {cueSeen}), forward again: gates {v.GatesPassed} (still 1)");
            // 3 to the finish without the last gates: placed on the finishing road after G9 from the surfacing pool
            scenario = "skip-order"; Clear(); smart = true; BeginAt(0, AnchorNear(Mk("G8") - 14));
            int g3 = v.GatesPassed; yield return Wait(.3f);
            float hint = Mk("G9") + 25; var at = v.Course.PointAt(hint, out var d);
            // the missed-gate reset would stop it before the line: hold it off for this test, so the finish line itself must refuse it
            float keepM = game.Shared.missedGateResetMeters, keepS = game.Shared.missedGateResetSeconds; game.Shared.missedGateResetMeters = 9999; game.Shared.missedGateResetSeconds = 9999;
            v.DebugPlace(at, Quaternion.LookRotation(d), Mode.Drive, 20, hint); r0 = v.Resets;
            float until3 = Time.time + 20f; bool sawFinishCross = false;
            while (Time.time < until3 && v.Resets == r0 && !(sawFinishCross && v.Body.position.z > 15)) { sawFinishCross |= v.Body.position.z > 1 && v.Progress > Mk("finish"); yield return null; }
            bool pass3 = sawFinishCross && !v.Finished && v.GatesPassed == g3;
            game.Shared.missedGateResetMeters = keepM; game.Shared.missedGateResetSeconds = keepS;
            Line($"FINISH OUT OF ORDER {(pass3 ? "PASS (rejected)" : "FAIL")}: from the pool (gates {g3}) placed on the road past G9 and driven over the line: crossed it {sawFinishCross}, finished {v.Finished}, gates {v.GatesPassed}, then {(v.Resets > r0 ? "reset (" + v.LastResetReason + ")" : "no reset")}");
            // race position measure: monotonic through the stacked section (cave under the road)
            Line($"RACE POSITION: gates + progress to the next gate (Course.RaceDistance); see the lap's log for its rise through the cave under the road");
        }

        // ---- item 5: Abi in each form, close-up pictures, nothing through the canopy ----
        IEnumerator Abi()
        {
            scenario = "abi"; Clear(); BeginAt(0, 0); yield return Wait(.2f);
            var d = Driver.Instance;
            if (!d || d.Scale <= 0) { Line("ABI FAIL: no driver in the seat"); yield break; }
            var camGo = new GameObject("abi cam"); var ac = camGo.AddComponent<Camera>(); ac.enabled = false; ac.fieldOfView = 40; ac.nearClipPlane = .05f; ac.farClipPlane = 600; ac.clearFlags = CameraClearFlags.Skybox;
            var t = v.transform; var results = new List<string>();
            foreach (var m in new[] { Mode.Drive, Mode.Boat, Mode.Fly, Mode.Dive })
            {
                v.Forms.Snap(m); yield return null;
                foreach (var (view, off) in new[] { ("side", new Vector3(3.6f, 1.6f, 1.2f)), ("front", new Vector3(-1.4f, 2.0f, 4.2f)) })
                {
                    camGo.transform.position = t.TransformPoint(off); camGo.transform.LookAt(t.TransformPoint(new Vector3(0, .55f, 0)));
                    Capture($"{m}-{view}", ac);
                }
                var (outside, total, below) = d.PokeTest(m);
                results.Add(m == Mode.Dive ? $"{m}: {outside} of {total} vertices outside the closed canopy above the body, {below} below the hull" : $"{m}: {below} below the hull");
            }
            v.Forms.Snap(v.Mode); Destroy(camGo);
            bool clean = results.TrueForAll(x => x.Contains(" 0 of ") || !x.StartsWith("Dive")) && results.TrueForAll(x => x.Contains(": 0 below") || x.Contains(", 0 below"));
            Line($"ABI {(clean ? "PASS" : "CHECK")}: {d.Source}; head top {d.HeadTop:0.00} m under a closed canopy top of {d.CanopyTop:0.00} m; {string.Join("; ", results)}");
            yield return Until(() => !v.Holding, 4f);
        }

        // ---- item 3: the best time survives a restart of the game (second process, same -saveDir) ----
        IEnumerator Best()
        {
            scenario = "best"; Clear(); BeginAt(0, 0); yield return Wait(.5f);
            var e = BestTimes.Get("Paradise Plunge"); var tt = game.Trial;
            Shot(0, "start-with-best");
            Line($"BEST SURVIVES A RESTART {(e != null && tt.Best != null && Mathf.Approximately(e.lap, tt.Best.lap) ? "PASS" : "FAIL")}: a new game process read {BestTimes.FilePath}: best lap {(e != null ? $"{e.lap:0.00} s set {e.date}, {e.splits?.Length ?? 0} splits" : "none")}; the HUD's time trial has it: {(tt.Best != null ? tt.Best.lap.ToString("0.00") : "no")}");
            yield return Wait(.5f);
        }

        // ---- Round 1 regression: the transition strip (course 1) ----
        IEnumerator Strip()
        {
            scenario = "strip"; Clear(); BeginAt(1, 0); Shot(.5f, "start");
            yield return Until(() => v.Finished, 180f);
            Shot(0, "finish"); yield return Wait(.3f);
            var cs = Of("strip"); var names = string.Join(", ", cs.ConvertAll(x => x.what));
            bool pass = v.Finished && v.Resets == 0 && cs.Count == 5;
            Line($"STRIP {(pass ? "PASS" : "FAIL")}: finished {v.Finished} in {v.FinishTime:0.0} s, gates {v.GatesPassed}/{v.Course.gates.Count}, resets {v.Resets}{(v.Resets > 0 ? " (" + v.LastResetReason + ")" : "")}, changes {cs.Count}: {names}");
        }

        // ---- Round 1 regression: the four lanes (courses 2-5) ----
        IEnumerator Lanes()
        {
            string[] names = { "", "", "drive-lane", "boat-lane", "fly-lane", "dive-lane" };
            for (int lane = 2; lane <= 5; lane++)
            {
                scenario = names[lane]; Clear(); BeginAt(lane, 0); float t0 = Time.time; var mode = v.Mode; float top = 0; bool slid = false; int r0 = v.Resets;
                steerWeave = () => { float t = Time.time - t0; return t > 3 && t < 11 ? Mathf.Sin(t * 1.4f) * .7f : 0; };
                if (lane == 4 || lane == 5) { var c = v.Course; yOverride = () => { c.Project(v.Body.position, v.Progress, out var at, out _); return at.y + Mathf.Sin((Time.time - t0) * .6f) * 3f; }; }
                Shot(7f, "b");
                while (Time.time - t0 < 16f)
                {
                    float t = Time.time - t0; secondary = t > 6 && t < 9; boost = t > 12 && t < 14;
                    top = Mathf.Max(top, v.Speed); slid |= v.Drifting || v.Carving; yield return null;
                }
                Line($"LANE {names[lane]}: form {mode} -> {v.Mode} (no change expected), top speed {top:0.0} m/s, {(lane <= 3 ? (slid ? "drift/carve used" : "drift/carve NOT seen") : "climb/dive weave")}, resets {v.Resets - r0}");
            }
        }

        // ---- Round 1 regression: edge cases on the strip (course 1) ----
        IEnumerator Edges()
        {
            scenario = "e1-slow-ramp"; Clear(); BeginAt(1, 1); holdSpeed = 10; float speedAtLip;
            yield return Until(() => v.Mode == Mode.Fly, 30f); speedAtLip = v.Speed; holdSpeed = -1;
            yield return Until(() => v.Mode == Mode.Boat || v.Body.position.z > 620, 40f); yield return Wait(1f);
            var c1 = Of(scenario);
            Line($"E1 low-speed ramp entry {(c1.Exists(x => x.what == "Drive->Fly") && c1.Exists(x => x.what == "Fly->Boat") && v.Resets == 0 ? "PASS" : "FAIL")}: ramp at 10 m/s, flight entered at {speedAtLip:0.0} m/s, reached the lagoon in {v.Mode}, resets {v.Resets}");

            scenario = "e2-boost-water"; Clear(); BeginAt(1, 2); boost = true; float boatSpeed, upright = -1, changeAt;
            yield return Until(() => v.Mode == Mode.Boat || v.Resets > 0, 30f); changeAt = Time.time; boatSpeed = v.Speed;
            while (Time.time - changeAt < 4f) { if (upright < 0 && v.transform.up.y > .9f && v.OnWater) upright = Time.time - changeAt; yield return null; }
            boost = false;
            Line($"E2 boosted water entry {(v.Mode == Mode.Boat && v.Resets == 0 && upright >= 0 && upright < 2f && boatSpeed <= game.Boat.entrySpeed.y + .5f ? "PASS" : "FAIL")}: boat speed {boatSpeed:0.0} m/s, level after {upright:0.00} s, resets {v.Resets}");

            scenario = "e3-nose-down"; Clear(); BeginAt(1, 2); yOverride = () => 12f; upright = -1;
            yield return Until(() => v.Body.position.z >= 497f || v.Resets > 0, 30f);
            v.debugEntryPitch = -50f;
            yield return Until(() => v.Mode == Mode.Boat || v.Resets > 0, 10f); v.debugEntryPitch = float.NaN; yOverride = null;
            changeAt = Time.time;
            while (Time.time - changeAt < 5f) { if (upright < 0 && v.transform.up.y > .9f) upright = Time.time - changeAt; yield return null; }
            Line($"E3 steep nose-down water entry {(v.Mode == Mode.Boat && v.Resets == 0 && upright >= 0 && upright < 1.5f ? "PASS" : "FAIL")}: -50 deg, level after {upright:0.00} s, resets {v.Resets}");

            scenario = "e4-reset-mid"; Clear(); BeginAt(1, 1);
            yield return Until(() => v.Changing && v.Mode == Mode.Fly && Time.time - v.ChangeStartedAt >= .25f, 30f);
            float tIn = Time.time - v.ChangeStartedAt; input.RequestReset();
            yield return Wait(.25f);
            var anchor = v.Course.anchors[1];
            bool ok4 = v.Mode == Mode.Drive && !v.Changing && v.Forms.Done && v.Forms.Current == Mode.Drive && Vector3.Distance(v.Body.position, anchor.transform.position) < 6f && v.GraceUntil > Time.time;
            int flies = Of(scenario).FindAll(x => x.what == "Drive->Fly").Count;
            yield return Until(() => Of(scenario).FindAll(x => x.what == "Drive->Fly").Count > flies, 20f);
            bool again = Of(scenario).FindAll(x => x.what == "Drive->Fly").Count > flies; yield return Wait(1f);
            Line($"E4 reset during a change {(ok4 && again ? "PASS" : "FAIL")}: reset {tIn:0.00} s into Drive->Fly; back in {v.Mode}; wing zone fired again: {again}");

            scenario = "e5-twice"; Clear(); BeginAt(1, 3);
            yield return Until(() => v.Body.position.z >= 888f || v.Resets > 0, 40f);
            reverse = true; yOverride = () => -6f;
            yield return Until(() => v.Body.position.z <= 856f || v.Resets > 0, 20f);
            reverse = false;
            yield return Until(() => v.Body.position.z >= 935f || v.Resets > 0, 25f); yOverride = null;
            var c5 = Of(scenario);
            Line($"E5 same trigger twice {(c5.Count == 1 && c5[0].what == "Boat->Dive" && v.Mode == Mode.Dive && v.Resets == 0 ? "PASS" : "FAIL")}: changes {c5.Count} ({string.Join(", ", c5.ConvertAll(x => x.what))}), form {v.Mode}, resets {v.Resets}");

            scenario = "e6-surface-road"; Clear(); BeginAt(1, 4);
            yield return Until(() => (v.Mode == Mode.Drive && v.GroundedWheels > 0) || v.Resets > 0, 40f); yield return Wait(.5f);
            var c6 = Of(scenario); var d = c6.Find(x => x.what == "Boat->Drive");
            float doneAt = d != null && d.took > 0 ? d.at + d.took : -1, touch = v.FirstWheelContactAt;
            Line($"E6 surfacing pool -> road {(c6.Exists(x => x.what == "Dive->Boat") && d != null && doneAt > 0 && touch > 0 && doneAt <= touch && v.Resets == 0 ? "PASS" : "FAIL")}: changes {string.Join(", ", c6.ConvertAll(x => x.what))}; Boat->Drive done {(doneAt > 0 && touch > 0 ? $"{touch - doneAt:0.00} s before the wheels touched" : "n/a")}, resets {v.Resets}");
        }
    }
}
