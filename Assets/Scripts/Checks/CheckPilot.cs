using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ShiftStorm
{
    // Check runs in the built exe ("-check strip|lanes|edge|all -checkOut <dir>", always muted, -batchmode: no window, never takes focus).
    // A scripted pilot drives through the player's own input path (ShiftInput.Scripted: throttle, brake, steer, vertical, boost, secondary),
    // aiming along the course line a little ahead, with the line's height as its climb / dive target. The camera renders into a 1280 x 720
    // texture; player-view pictures (HUD included) go to <dir>. Each change's timing and the frame times around it are logged, and the
    // measured audio output level is reported (it must be 0). The run quits when done.
    public sealed class CheckPilot : MonoBehaviour
    {
        Game game; ShiftVehicle v; ShiftInput input; Camera cam; RenderTexture rt; Texture2D grab; AudioMeter meter;
        string dir; readonly StringBuilder report = new StringBuilder();
        readonly List<(float t, float dt, bool shot)> frames = new List<(float, float, bool)>(); bool shotLastFrame;
        readonly List<(float at, string name)> shots = new List<(float, string)>();
        readonly List<ChangeRecord> changes = new List<ChangeRecord>();
        string scenario = ""; int shotIndex;
        class ChangeRecord { public string scenario, what; public float at, took = -1, z; }

        // pilot settings, per scenario
        float holdSpeed = -1; bool boost, secondary, reverse; Func<float> steerWeave, yOverride; float throttleCap = 1;

        void Start()
        {
            game = Game.Instance; v = game.Player; input = v.Input; cam = game.Cam; meter = cam.GetComponent<AudioMeter>();
            dir = QuietRun.Dir; Directory.CreateDirectory(dir);
            rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; grab = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            input.Scripted = true;
            v.Event += OnEvent;
            StartCoroutine(Run());
        }

        void OnEvent(string kind, string text)
        {
            if (kind == "change") { changes.Add(new ChangeRecord { scenario = scenario, what = $"{v.ChangeFrom}->{v.Mode}", at = Time.time, z = v.Body.position.z }); Shot(.2f, $"{v.ChangeFrom}-{v.Mode}-mid"); }
            else if (kind == "changed") { if (changes.Count > 0) changes[changes.Count - 1].took = v.LastChangeSeconds; Shot(.15f, $"{v.Mode}-after"); }
            else if (kind == "reset") { if (changes.Count > 0 && changes[changes.Count - 1].took < 0) changes[changes.Count - 1].took = -2; Shot(.3f, "after-reset"); }
        }
        void Shot(float delay, string name) => shots.Add((Time.time + delay, name));

        void Update()
        {
            frames.Add((Time.time, Time.unscaledDeltaTime, shotLastFrame)); shotLastFrame = false;
            for (int i = shots.Count - 1; i >= 0; i--) if (Time.time >= shots[i].at) { Capture(shots[i].name); shots.RemoveAt(i); }
        }
        bool hudOff; Canvas hud;
        void Capture(string name)
        {
            if (name.StartsWith("cover")) { hud = FindAnyObjectByType<Hud>()?.GetComponent<Canvas>(); if (hud) { hud.enabled = false; hudOff = true; } }
            cam.Render(); var prev = RenderTexture.active; RenderTexture.active = rt;
            grab.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); grab.Apply(); RenderTexture.active = prev;
            shotLastFrame = true; if (hudOff) { hudOff = false; if (hud) hud.enabled = true; } var file = Path.Combine(dir, $"{++shotIndex:00}-{scenario}-{name}.png"); File.WriteAllBytes(file, grab.EncodeToPNG());
        }

        void FixedUpdate()
        {
            if (!v || !v.Course) return;
            var p = v.Body.position; var c = v.Course;
            float prog = c.Project(p, out _, out _);
            float ahead = Mathf.Max(18f, v.Speed * 1.1f);
            var look = c.PointAt(prog + ahead, out _);
            var fwd = Vector3.ProjectOnPlane(v.transform.forward, Vector3.up).normalized;
            var want = Vector3.ProjectOnPlane(look - p, Vector3.up).normalized;
            float err = Vector3.SignedAngle(fwd, want, Vector3.up);
            float steer = steerWeave != null ? Mathf.Clamp(err / 90f, -.3f, .3f) + steerWeave() : Mathf.Clamp(err / 20f, -1, 1);   // weaving: the line only pulls gently, so the weave is really driven
            float ty = yOverride != null ? yOverride() : look.y;
            float vy = v.Body.linearVelocity.y;
            float vert = Mathf.Clamp((ty - p.y) * .15f - vy * .12f, -1, 1);
            float thr = throttleCap;
            if (holdSpeed > 0) thr = v.Speed < holdSpeed ? 1 : 0;
            input.sThrottle = reverse ? 0 : thr; input.sBrake = reverse ? 1 : 0; input.sSteer = reverse ? 0 : Mathf.Clamp(steer, -1, 1);
            input.sVertical = vert; input.sBoost = boost; input.sSecondary = secondary;
        }

        void Clear() { holdSpeed = -1; boost = secondary = reverse = false; steerWeave = null; yOverride = null; throttleCap = 1; v.debugEntryPitch = float.NaN; }
        IEnumerator Until(Func<bool> done, float timeout) { float end = Time.time + timeout; while (!done() && Time.time < end) yield return null; }
        IEnumerator Wait(float s) { float end = Time.time + s; while (Time.time < end) yield return null; }
        void Line(string s) { report.AppendLine(s); Debug.Log("[Check] " + s); }
        int ResetsNow => v.Resets;
        void BeginAt(int course, int anchor) { game.StartCourse(course); if (anchor > 0) v.Begin(v.Course, v.Course.anchors[anchor]); }
        List<ChangeRecord> Of(string s) => changes.FindAll(x => x.scenario == s);

        IEnumerator Run()
        {
            string which = "all"; var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-check"); if (i >= 0 && i + 1 < a.Length) which = a[i + 1];
            yield return Wait(1f);
            Line($"ShiftStorm check '{which}', {DateTime.Now:yyyy-MM-dd HH:mm}, model: {VehicleFactory.ModelSource}, build {Application.version}, {SystemInfo.graphicsDeviceName}");
            Line($"muted from the first frame: {QuietRun.Muted}; AudioListener.volume = {AudioListener.volume}; audio output {AudioSettings.outputSampleRate} Hz");
            if (which == "strip" || which == "all") yield return Strip();
            if (which == "lanes" || which == "all") yield return Lanes();
            if (which == "edge" || which == "all") yield return Edges();
            // frame times and audio
            float sum = 0; int n = 0; float worst = 0; foreach (var f in frames) { if (f.t < 2f || f.shot) continue; sum += f.dt; n++; worst = Mathf.Max(worst, f.dt); }
            var sorted = new List<float>(); foreach (var f in frames) if (f.t >= 2f && !f.shot) sorted.Add(f.dt); sorted.Sort();
            float p95 = sorted.Count > 0 ? sorted[(int)(sorted.Count * .95f)] : 0;
            Line($"frame time over the run (PC, 1280x720 off-screen, uncapped, frames that saved a picture left out): mean {1000 * sum / Mathf.Max(n, 1):0.00} ms, 95th percentile {1000 * p95:0.00} ms, worst {1000 * worst:0.00} ms over {n} frames");
            Line("changes (scenario, change, z, visual time, worst / mean frame time from 0.25 s before to 0.25 s after):");
            foreach (var c in changes)
            {
                float end = c.at + Mathf.Max(c.took, .6f) + .25f, w = 0, s = 0; int k = 0;
                foreach (var f in frames) if (f.t >= c.at - .25f && f.t <= end && !f.shot) { w = Mathf.Max(w, f.dt); s += f.dt; k++; }
                Line($"  {c.scenario,-14} {c.what,-11} z {c.z,7:0.0}  {(c.took >= 0 ? $"{c.took:0.000} s" : c.took < -1 ? "cancelled by the reset" : "not finished")}  frames worst {1000 * w:0.00} ms / mean {1000 * s / Mathf.Max(k, 1):0.00} ms {(c.took >= .5f && c.took <= .8f ? "" : c.took < 0 ? "" : " OUT OF 0.5-0.8 s")}");
            }
            Line($"audio output level (AudioListener.GetOutputData, all frames): peak {meter.MaxPeakSeen:0.000000}, last RMS {meter.Rms:0.000000}, AudioListener.volume {AudioListener.volume}");
            File.WriteAllText(Path.Combine(dir, "check_report.txt"), report.ToString());
            yield return Wait(.5f);
            Application.Quit(0);
        }

        // ---- full run of the strip (item 6) ----
        IEnumerator Strip()
        {
            scenario = "strip"; Clear(); BeginAt(0, 0); Shot(.5f, "start"); Shot(13f, "cover-flight"); Shot(31f, "cover-boat");
            yield return Until(() => v.Finished, 180f);
            Shot(0, "finish"); yield return Wait(.3f);
            var cs = Of("strip"); var names = string.Join(", ", cs.ConvertAll(x => x.what));
            bool pass = v.Finished && v.Resets == 0 && cs.Count == 5;
            Line($"STRIP {(pass ? "PASS" : "FAIL")}: finished {v.Finished} in {v.FinishTime:0.0} s, gates {v.GatesPassed}/{v.Course.gates.Count}, resets {v.Resets}{(v.Resets > 0 ? " (" + v.LastResetReason + ")" : "")}, changes {cs.Count}: {names}");
        }

        // ---- the four lanes (item 4) ----
        IEnumerator Lanes()
        {
            string[] names = { "", "drive-lane", "boat-lane", "fly-lane", "dive-lane" };
            for (int lane = 1; lane <= 4; lane++)
            {
                scenario = names[lane]; Clear(); BeginAt(lane, 0); float t0 = Time.time; var mode = v.Mode; float top = 0; bool slid = false; int r0 = v.Resets;
                steerWeave = () => { float t = Time.time - t0; return t > 3 && t < 11 ? Mathf.Sin(t * 1.4f) * .7f : 0; };
                if (lane == 3 || lane == 4) { var c = v.Course; yOverride = () => { c.Project(v.Body.position, out var at, out _); return at.y + Mathf.Sin((Time.time - t0) * .6f) * 3f; }; }
                Shot(2f, "a"); Shot(7f, "b"); Shot(12f, "c");
                while (Time.time - t0 < 16f)
                {
                    float t = Time.time - t0; secondary = t > 6 && t < 9; boost = t > 12 && t < 14;
                    top = Mathf.Max(top, v.Speed); slid |= v.Drifting || v.Carving; yield return null;
                }
                Line($"LANE {names[lane]}: form {mode} -> {v.Mode} (no change expected), top speed {top:0.0} m/s, {(lane <= 2 ? (slid ? "drift/carve used" : "drift/carve NOT seen") : "climb/dive weave")}, boost left {v.Boost:0.00}, resets {v.Resets - r0}, gates {v.GatesPassed}");
            }
        }

        // ---- edge cases (item 7) ----
        IEnumerator Edges()
        {
            // 1 low-speed ramp entry
            scenario = "e1-slow-ramp"; Clear(); BeginAt(0, 1); holdSpeed = 10; float speedAtLip = 0;
            yield return Until(() => v.Mode == Mode.Fly, 30f); speedAtLip = v.Speed; holdSpeed = -1; Shot(1f, "flying");
            yield return Until(() => v.Mode == Mode.Boat || v.Body.position.z > 620, 40f); yield return Wait(1f);
            var c1 = Of(scenario);
            Line($"E1 low-speed ramp entry {(c1.Exists(x => x.what == "Drive->Fly") && c1.Exists(x => x.what == "Fly->Boat") && v.Resets == 0 ? "PASS" : "FAIL")}: drove the ramp at 10 m/s, flight entered at {speedAtLip:0.0} m/s (blended into {game.Fly.entrySpeed.x}-{game.Fly.entrySpeed.y}), reached the lagoon in {v.Mode}, resets {v.Resets}");

            // 2 boosted water entry
            scenario = "e2-boost-water"; Clear(); BeginAt(0, 2); boost = true; float boatSpeed = 0, upright = -1; float changeAt = -1;
            yield return Until(() => v.Mode == Mode.Boat || v.Resets > 0, 30f); changeAt = Time.time; boatSpeed = v.Speed; float flySpeedBefore = Of(scenario).Count > 0 ? v.Speed : 0;
            while (Time.time - changeAt < 4f) { if (upright < 0 && v.transform.up.y > .9f && v.OnWater) upright = Time.time - changeAt; yield return null; }
            boost = false;
            Line($"E2 boosted water entry {(v.Mode == Mode.Boat && v.Resets == 0 && upright >= 0 && upright < 2f && boatSpeed <= game.Boat.entrySpeed.y + .5f ? "PASS" : "FAIL")}: boat speed after the change {boatSpeed:0.0} m/s (range max {game.Boat.entrySpeed.y}), level on the water after {upright:0.00} s, resets {v.Resets}");

            // 3 steep nose-down water entry
            scenario = "e3-nose-down"; Clear(); BeginAt(0, 2); yOverride = () => 12f; float entryPitch = 0; upright = -1;
            yield return Until(() => v.Body.position.z >= 497f || v.Resets > 0, 30f);
            v.debugEntryPitch = -50f;
            yield return Until(() => v.Mode == Mode.Boat || v.Resets > 0, 10f); entryPitch = Vector3.SignedAngle(Vector3.ProjectOnPlane(v.transform.forward, Vector3.up), v.transform.forward, v.transform.right) * -1; v.debugEntryPitch = float.NaN; yOverride = null;
            changeAt = Time.time; int r3 = v.Resets;
            while (Time.time - changeAt < 5f) { if (upright < 0 && v.transform.up.y > .9f) upright = Time.time - changeAt; yield return null; }
            Line($"E3 steep nose-down water entry {(v.Mode == Mode.Boat && v.Resets == 0 && upright >= 0 && upright < 1.5f ? "PASS" : "FAIL")}: entered at {-50} deg pitch (body {entryPitch:0} deg at the change), level after {upright:0.00} s, resets {v.Resets}, form {v.Mode}");

            // 4 reset pressed in the middle of a change
            scenario = "e4-reset-mid"; Clear(); BeginAt(0, 1); bool pressed = false; string after = "";
            yield return Until(() => v.Changing && v.Mode == Mode.Fly && Time.time - v.ChangeStartedAt >= .25f, 30f);
            float tIn = Time.time - v.ChangeStartedAt; input.RequestReset(); pressed = true;
            yield return Wait(.25f);
            var anchor = v.Course.anchors[1];
            bool ok4 = v.Mode == Mode.Drive && !v.Changing && v.Forms.Done && v.Forms.Current == Mode.Drive && Vector3.Distance(v.Body.position, anchor.transform.position) < 6f && v.GraceUntil > Time.time;
            after = $"after reset: form {v.Mode}, visual {v.Forms.Current} done {v.Forms.Done}, {Vector3.Distance(v.Body.position, anchor.transform.position):0.0} m from the anchor, grace {(v.GraceUntil > Time.time ? "on" : "off")}";
            int flies = Of(scenario).FindAll(x => x.what == "Drive->Fly").Count;
            yield return Until(() => Of(scenario).FindAll(x => x.what == "Drive->Fly").Count > flies, 20f);
            bool again = Of(scenario).FindAll(x => x.what == "Drive->Fly").Count > flies; yield return Wait(1f);
            Line($"E4 reset during a change {(pressed && ok4 && again ? "PASS" : "FAIL")}: reset {tIn:0.00} s into Drive->Fly; {after}; the ramp's wing zone fired again after the reset: {again}");

            // 5 the same trigger crossed twice (back and forth) at the dive portal, where the lagoon and the cave water overlap
            scenario = "e5-twice"; Clear(); BeginAt(0, 3);
            yield return Until(() => v.Body.position.z >= 888f || v.Resets > 0, 40f);
            reverse = true; yOverride = () => -6f; Shot(.5f, "reversing");
            yield return Until(() => v.Body.position.z <= 856f || v.Resets > 0, 20f);
            Shot(0, "back-before-zone"); reverse = false;
            yield return Until(() => v.Body.position.z >= 935f || v.Resets > 0, 25f); yOverride = null;
            var c5 = Of(scenario);
            Line($"E5 same trigger twice {(c5.Count == 1 && c5[0].what == "Boat->Dive" && v.Mode == Mode.Dive && v.Resets == 0 ? "PASS" : "FAIL")}: crossed the dive zone forward, back (to z 856, the zone starts at 862) and forward again: changes {c5.Count} ({string.Join(", ", c5.ConvertAll(x => x.what))}), form now {v.Mode}, resets {v.Resets}");

            // 6 surfacing pool -> road: dive -> boat -> drive done before the wheels touch
            scenario = "e6-surface-road"; Clear(); BeginAt(0, 4);
            yield return Until(() => (v.Mode == Mode.Drive && v.GroundedWheels > 0) || v.Resets > 0, 40f); yield return Wait(.5f);
            var c6 = Of(scenario); var d = c6.Find(x => x.what == "Boat->Drive");
            float doneAt = d != null && d.took > 0 ? d.at + d.took : -1, touch = v.FirstWheelContactAt;
            Line($"E6 surfacing pool -> road {(c6.Exists(x => x.what == "Dive->Boat") && d != null && doneAt > 0 && touch > 0 && doneAt <= touch && v.Resets == 0 ? "PASS" : "FAIL")}: changes {string.Join(", ", c6.ConvertAll(x => x.what))}; Boat->Drive done {(doneAt > 0 && touch > 0 ? $"{touch - doneAt:0.00} s before the first wheel touched" : "n/a")}, resets {v.Resets}");
        }
    }
}
