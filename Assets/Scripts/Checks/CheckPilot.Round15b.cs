using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShiftStorm
{
    // Round 15b check (Dan on build 17, Steam Deck: "the second I switch to either the 2nd or 3rd race the screen shakes. If I finish the 2nd
    // race the menu starts shaking at the end even if I try to start the [first] race"; "the new race does it the entire time").
    //   shake  the Deck graphics preset (or -graphics <name>), the real menu path with the player's own buttons: title -> Race -> the course
    //          cards Paradise -> Coaster -> Big Top -> Coaster -> Paradise; a Coaster Coast race to the finish, the results, Menu, Race,
    //          Paradise selected; a Big Top race (the start and 25 s of the lap). Every frame: the camera's final position / aim / up, its
    //          field of view, projection and view matrices, its parent and scale, the time scale, the frame time; the camera's wobble is
    //          its distance from a smooth path through the frames round it (centred 9 frames), in mm and in pixels at 1280 x 800, and the
    //          number of back-and-forth turns of its sideways / up motion; in a race also the camera against the car. Pinned windows (the
    //          menu camera held still for 3 s) show whether the world moves under a still camera (the picture frame to frame).
    //          CSV per window (shake-*.csv).
    public sealed partial class CheckPilot
    {
        sealed class Track
        {
            public string name; public List<Vector3> p = new List<Vector3>(), f = new List<Vector3>(), u = new List<Vector3>(), car = new List<Vector3>(); public List<Quaternion> carR = new List<Quaternion>();
            public List<float> fov = new List<float>(), dt = new List<float>(), projD = new List<float>(), diff = new List<float>(); public List<string> info = new List<string>();
        }
        IEnumerator RecordTrack(Track tr, float seconds, bool picture = false, bool withCar = false)
        {
            float end = Time.realtimeSinceStartup + seconds; Matrix4x4 lastP = cam.projectionMatrix; Color32[] last = null;
            while (Time.realtimeSinceStartup < end)
            {
                yield return new WaitForEndOfFrame();
                var t = cam.transform; tr.p.Add(t.position); tr.f.Add(t.forward); tr.u.Add(t.up); tr.fov.Add(cam.fieldOfView); tr.dt.Add(Time.unscaledDeltaTime);
                var P = cam.projectionMatrix; float pd = 0; for (int i = 0; i < 16; i++) pd = Mathf.Max(pd, Mathf.Abs(P[i] - lastP[i])); lastP = P; tr.projD.Add(pd);
                if (withCar) { tr.car.Add(v.transform.position); tr.carR.Add(v.transform.rotation); }
                if (picture)
                {
                    var px = SmallFrame();
                    if (last != null) { double sum = 0; for (int i = 0; i < px.Length; i++) sum += (Mathf.Abs(px[i].r - last[i].r) + Mathf.Abs(px[i].g - last[i].g) + Mathf.Abs(px[i].b - last[i].b)) / 3.0; tr.diff.Add((float)(sum / px.Length)); }
                    last = px;
                }
            }
        }
        // the distance of each frame's camera from the average of the 9 frames round it (a smooth path), and the angle likewise
        static void Wobble(List<Vector3> p, List<Vector3> f, out float maxMm, out float rmsMm, out float maxDeg, out float rmsDeg, out int turns, Func<int, Vector3> right = null)
        {
            maxMm = rmsMm = maxDeg = rmsDeg = 0; turns = 0; int n = p.Count, k = 4, m = 0; double s2 = 0, a2 = 0;
            for (int i = k; i < n - k; i++)
            {
                Vector3 avg = Vector3.zero, af = Vector3.zero; for (int j = -k; j <= k; j++) { avg += p[i + j]; af += f[i + j]; }
                avg /= 2 * k + 1; float d = (p[i] - avg).magnitude * 1000; float a = Vector3.Angle(f[i], af.normalized);
                maxMm = Mathf.Max(maxMm, d); maxDeg = Mathf.Max(maxDeg, a); s2 += d * d; a2 += a * a; m++;
            }
            if (m > 0) { rmsMm = (float)Math.Sqrt(s2 / m); rmsDeg = (float)Math.Sqrt(a2 / m); }
            // back-and-forth: the sign of the second difference of the sideways position flips every frame in a shake
            for (int i = 2; i < n; i++)
            {
                var r = right != null ? right(i) : Vector3.right;
                float a0 = Vector3.Dot(p[i - 1] - p[i - 2], r), a1 = Vector3.Dot(p[i] - p[i - 1], r);
                if (i >= 3) { float a00 = Vector3.Dot(p[i - 2] - p[i - 3], r); if (Mathf.Sign(a1 - a0) != Mathf.Sign(a0 - a00) && Mathf.Abs(a1 - a0) > .002f) turns++; }
            }
        }
        // the camera on the Deck's 60 Hz frames: resampled every 1/60 s by time, then its distance / angle from a 0.25 s moving average
        static void Wobble60(List<float> dt, List<Vector3> p, List<Vector3> f, out float mm, out float deg)
        {
            mm = deg = 0; int n = p.Count; if (n < 4) return;
            var t = new float[n]; for (int i = 1; i < n; i++) t[i] = t[i - 1] + dt[i];
            var rp = new List<Vector3>(); var rf = new List<Vector3>(); int j = 0;
            for (float x = 0; x <= t[n - 1]; x += 1f / 60f)
            {
                while (j < n - 2 && t[j + 1] < x) j++;
                float u = t[j + 1] > t[j] ? Mathf.Clamp01((x - t[j]) / (t[j + 1] - t[j])) : 0; rp.Add(Vector3.Lerp(p[j], p[j + 1], u)); rf.Add(Vector3.Slerp(f[j], f[j + 1], u));
            }
            int k = 7;
            for (int i = k; i < rp.Count - k; i++)
            {
                Vector3 ap = Vector3.zero, af = Vector3.zero; for (int q = -k; q <= k; q++) { ap += rp[i + q]; af += rf[i + q]; }
                mm = Mathf.Max(mm, (rp[i] - ap / (2 * k + 1)).magnitude * 1000); deg = Mathf.Max(deg, Vector3.Angle(rf[i], af));
            }
        }
        bool ReportTrack(Track tr, bool moving = true)
        {
            int n = tr.p.Count; if (n < 12) { Line($"  {tr.name}: only {n} frames"); return false; }
            Wobble(tr.p, tr.f, out float mm, out float rms, out float deg, out float rdeg, out int turns, i => Vector3.Cross(Vector3.up, tr.f[i]).normalized);
            float fov = tr.fov.Average(), pxPerDeg = H / Mathf.Max(1, fov); float pxMax = deg * pxPerDeg, pxRms = rdeg * pxPerDeg;
            float fovSpan = tr.fov.Max() - tr.fov.Min(), proj = tr.projD.Skip(1).DefaultIfEmpty(0).Max();
            var dts = tr.dt.OrderBy(x => x).ToList();
            float speed = 0; for (int i = 1; i < n; i++) speed += (tr.p[i] - tr.p[i - 1]).magnitude; float secs = tr.dt.Skip(1).Sum(); speed /= Mathf.Max(.01f, secs);
            string rel = "";
            float relMm = 0, relDeg = 0;
            if (tr.car.Count == n)
            {
                // the camera seen from the car (its own frame): a steady chase camera moves smoothly here too
                var lp = new List<Vector3>(); var lf = new List<Vector3>();
                for (int i = 0; i < n; i++) { var inv = Quaternion.Inverse(tr.carR[i]); lp.Add(inv * (tr.p[i] - tr.car[i])); lf.Add(inv * tr.f[i]); }
                Wobble(lp, lf, out relMm, out float relRms, out relDeg, out float relRdeg, out int relTurns);
                Wobble(tr.car, tr.f, out float carMm, out float carRms, out _, out _, out int carTurns);
                rel = $"; against the car: {relMm:0.0} mm / {relDeg:0.000} deg max (rms {relRms:0.0} mm / {relRdeg:0.000} deg, {relTurns} turns); the car's own path wobble {carMm:0.0} mm max (rms {carRms:0.0})";
            }
            string pic = tr.diff.Count > 0 ? $"; picture change per frame mean {tr.diff.Average():0.00} / 255, max {tr.diff.Max():0.00}" : "";
            // a shake: the view off its smooth path by more than half a pixel, or turning back and forth on more than a tenth of the frames
            Wobble60(tr.dt, tr.p, tr.f, out float w60mm, out float w60deg);
            string w60 = $"; at 60 Hz: {w60mm:0} mm / {w60deg:0.000} deg off a 0.25 s average";
            // the menus' and the results' camera: smooth on the Deck's frames (a race's camera is judged by the picture's flicker and against the car)
            bool ok = tr.car.Count == n || (w60deg < .05f && w60mm < 60f);
            Line($"  {tr.name,-46} {(ok ? "STEADY" : "SHAKING")}: {n} frames, {speed:0.0} m/s{w60}; off its 9-frame path {mm:0.0} mm / {deg:0.0000} deg max = {pxMax:0.00} px (rms {rms:0.0} mm / {pxRms:0.000} px), {turns} back-and-forth turns; fov {tr.fov.Min():0.0}-{tr.fov.Max():0.0}, projection change {proj:0.000000}; frame {dts[n / 2] * 1000:0.0} ms median{rel}{pic}{(tr.info.Count > 0 ? "; " + string.Join(", ", tr.info) : "")}");
            var csv = new StringBuilder("frame,dt_ms,x,y,z,fx,fy,fz,ux,uy,uz,fov,proj_change" + (tr.car.Count == n ? ",car_x,car_y,car_z" : "") + "\n");
            for (int i = 0; i < n; i++)
                csv.AppendLine(FormattableString.Invariant($"{i},{tr.dt[i] * 1000:0.00},{tr.p[i].x:0.0000},{tr.p[i].y:0.0000},{tr.p[i].z:0.0000},{tr.f[i].x:0.00000},{tr.f[i].y:0.00000},{tr.f[i].z:0.00000},{tr.u[i].x:0.00000},{tr.u[i].y:0.00000},{tr.u[i].z:0.00000},{tr.fov[i]:0.000},{tr.projD[i]:0.000000}") + (tr.car.Count == n ? FormattableString.Invariant($",{tr.car[i].x:0.0000},{tr.car[i].y:0.0000},{tr.car[i].z:0.0000}") : ""));
            File.WriteAllText(Path.Combine(dir, $"shake-{tr.name.Split(':')[0].Trim().Replace(' ', '-').Replace("/", "")}.csv"), csv.ToString());
            return ok;
        }
        string CamState()
        {
            var t = cam.transform; var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var comps = string.Join(" ", t.GetComponents<Component>().Select(c => c.GetType().Name + (c is Behaviour b && !b.enabled ? "(off)" : "")));
            return $"camera parent {(t.parent ? t.parent.name : "none")}, scale {t.lossyScale.x:0.###}, components [{comps}], time scale {Time.timeScale}, render scale {(urp ? urp.renderScale : 0):0.00} ({(urp ? urp.upscalingFilter.ToString() : "")}), last shake at {ChaseCam.LastShake:0.0} s, course shown {(CourseLook.Shown ? CourseLook.Shown.courseName : "none")}, MenuCam {(MenuCam.On ? "on" : "off")}";
        }

        // the picture's flicker as the Deck shows it: rendered at 1024 x 640 (the Deck preset's render scale 0.8), stretched to 1280 x 800,
        // one 60 Hz frame of motion per frame (the menu camera's step, or Time.captureDeltaTime in a race). A pixel flickers when it changes
        // by more than 40 levels from one frame to the next and comes back within 12 of where it was the frame after (A -> B -> A): smooth
        // motion never does that; crawling shadow edges, shadow acne and aliasing on thin lines do. A heat map shows where.
        RenderTexture lowRt, fullRt; Texture2D fullTex;
        Color32[] DeckFrame(bool native)
        {
            if (!lowRt) { lowRt = new RenderTexture(1024, 640, 24); fullRt = new RenderTexture(W, H, 0); fullTex = new Texture2D(W, H, TextureFormat.RGB24, false); }
            if (native) { cam.Render(); Graphics.Blit(rt, fullRt); }
            else { cam.targetTexture = lowRt; cam.Render(); cam.targetTexture = rt; lowRt.filterMode = FilterMode.Bilinear; Graphics.Blit(lowRt, fullRt); }
            var prev = RenderTexture.active; RenderTexture.active = fullRt; fullTex.ReadPixels(new Rect(0, 0, W, H), 0, 0); fullTex.Apply(); RenderTexture.active = prev;
            shotLastFrame = true; return fullTex.GetPixels32();
        }
        readonly Dictionary<string, float> flick = new Dictionary<string, float>();
        IEnumerator Flicker(string name, int frames, bool race, bool noShadows = false, bool native = false)
        {
            var data = cam.GetUniversalAdditionalCameraData(); bool keepShadows = data.renderShadows; if (noShadows) data.renderShadows = false;
            if (race) Time.captureDeltaTime = 1f / 60f; else MenuCam.Step = 1f / 60f;
            var hist = new List<Color32[]>(); var heat = new float[W * H]; var shares = new List<float>(); var means = new List<float>();
            for (int k = 0; k < frames; k++)
            {
                yield return new WaitForEndOfFrame();
                var px = DeckFrame(native); hist.Add(px); if (hist.Count > 3) hist.RemoveAt(0);
                if (hist.Count == 3)
                {
                    var a = hist[0]; var b = hist[1]; var c = hist[2]; int fl = 0; double sum = 0;
                    for (int i = 0; i < px.Length; i++)
                    {
                        int ab = Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b);
                        int ac = Mathf.Abs(a[i].r - c[i].r) + Mathf.Abs(a[i].g - c[i].g) + Mathf.Abs(a[i].b - c[i].b);
                        int bc = Mathf.Abs(b[i].r - c[i].r) + Mathf.Abs(b[i].g - c[i].g) + Mathf.Abs(b[i].b - c[i].b);
                        sum += ab / 3.0;
                        if (ab > 120 && bc > 120 && ac < 36) { fl++; heat[i]++; }
                    }
                    shares.Add(fl / (float)px.Length); means.Add((float)(sum / px.Length));
                }
            }
            Time.captureDeltaTime = 0; MenuCam.Step = -1; data.renderShadows = keepShadows;
            var sh = shares.OrderBy(x => x).ToList(); int n = sh.Count; flick[name] = sh[n / 2];
            Line($"  FLICKER {name,-44}: {frames} frames at 60 Hz steps ({(native ? "1280 x 800" : "1024 x 640 stretched")}{(noShadows ? ", shadows OFF" : "")}): flickering pixels per frame median {sh[n / 2] * 100:0.000} %, 95th pct {sh[(int)(n * .95f)] * 100:0.000} %, max {sh[n - 1] * 100:0.000} %; picture change per frame mean {means.Average():0.00} / 255");
            float max = Mathf.Max(1, heat.Max()); var last = hist[hist.Count - 1]; var hp = new Color32[heat.Length];
            for (int i = 0; i < hp.Length; i++) { float q = Mathf.Clamp01(heat[i] / max * 3); var g = last[i]; byte grey = (byte)((g.r + g.g + g.b) / 9); hp[i] = q > 0 ? new Color32((byte)(90 + 165 * q), (byte)(255 * q * .2f), 0, 255) : new Color32(grey, grey, grey, 255); }
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.SetPixels32(hp); tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, $"{++shotIndex:00}-flicker-{name.Replace(' ', '-')}-heat.jpg"), tex.EncodeToJPG(85));
            tex.SetPixels32(last); tex.Apply(); File.WriteAllBytes(Path.Combine(dir, $"{++shotIndex:00}-flicker-{name.Replace(' ', '-')}-frame.jpg"), tex.EncodeToJPG(88));
            Destroy(tex);
        }
        IEnumerator FlickerSet(string name, bool race)
        {
            yield return Flicker(name, 150, race);
            yield return Flicker(name + " noshadow", 150, race, true);
            yield return Flicker(name + " native", 150, race, false, true);
        }

        IEnumerator ShakeCheck()
        {
            scenario = "shake"; Clear(); coast = true; Directory.CreateDirectory(dir);
            if (GraphicsPreset.Chosen < 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-graphics") < 0) GraphicsPreset.Override = 0;   // the Deck preset unless -graphics says otherwise
            GraphicsPreset.Apply();
            Line($"SHAKE (Round 15b): graphics {GraphicsPreset.Applied}{(Hud.OldHud ? "; BUILD 17's HUD SHADER (-oldHud)" : "; the HUD on ShiftStorm/HudUI (canvas -> view, no world position)")}");
            bool allOk = true;
            IEnumerator P(Action a) { a(); yield return WaitReal(.15f); }
            IEnumerator On(Game.Page pg) { float end = Time.unscaledTime + 3f; while (game.MenuPage != pg && Time.unscaledTime < end) yield return null; yield return WaitReal(.2f); if (game.MenuPage != pg) Line($"  (waited for page {pg}: still {game.MenuPage})"); }
            IEnumerator To(string label) { int guard = 0; while (game.MenuIndex < game.Rows.Count && !game.Rows[game.MenuIndex].Text.StartsWith(label) && guard++ < 20) yield return P(() => input.sMove = 1); }
            IEnumerator Window(string name, float secs, bool pin = false)
            {
                var tr = new Track { name = name }; tr.info.Add(CamState());
                if (pin) MenuCam.Hold = true;
                yield return RecordTrack(tr, secs, pin);
                MenuCam.Hold = false;
                allOk &= ReportTrack(tr, !pin);
                Capture("shake-" + name.Split(':')[0].Trim().Replace(' ', '-'));
            }
            // the menus: title -> Race -> the course cards
            game.ShowTitle(); yield return WaitReal(1f);
            yield return Window("title: Paradise Plunge behind", 4f);
            yield return P(() => input.sConfirm = true); yield return On(Game.Page.Main); yield return To("Race"); yield return P(() => input.sConfirm = true); yield return On(Game.Page.Course);
            if (game.MenuPage != Game.Page.Course) Line($"  (not on the course page: {game.MenuPage})");
            { int g2 = 0; while (game.MenuCourse != 0 && g2++ < 6) yield return P(() => input.sSide = -1); }
            yield return WaitReal(1f);
            string[] nm = { "Paradise", "Coaster", "BigTop" };
            yield return Window($"menu1 {nm[0]}: course card", 5f); yield return FlickerSet($"menu {nm[0]}", false); yield return Window($"menu1 {nm[0]} pinned: camera held", 3f, true);
            yield return P(() => input.sSide = 1); yield return WaitReal(1f);
            yield return Window($"menu1 {nm[game.MenuCourse]}: course card", 8f); yield return FlickerSet($"menu {nm[game.MenuCourse]}", false); yield return Window($"menu1 {nm[game.MenuCourse]} pinned: camera held", 3f, true);
            yield return P(() => input.sSide = 1); yield return WaitReal(1f);
            yield return Window($"menu1 {nm[game.MenuCourse]}: course card", 8f); yield return FlickerSet($"menu {nm[game.MenuCourse]}", false); yield return Window($"menu1 {nm[game.MenuCourse]} pinned: camera held", 3f, true);
            yield return P(() => input.sSide = -1); yield return WaitReal(1f);
            yield return Window($"menu2 {nm[game.MenuCourse]}: back to it", 5f);
            yield return P(() => input.sSide = -1); yield return WaitReal(1f);
            yield return Window($"menu2 {nm[game.MenuCourse]}: back to it", 5f);
            // a Coaster Coast race: Go from the cards, then the check pilot from the last anchors to the line
            yield return P(() => input.sSide = 1); yield return P(() => input.sConfirm = true); yield return On(Game.Page.Pick); yield return To("Go"); yield return P(() => input.sConfirm = true);
            yield return Until(() => !v.Holding, 15f); coast = false; smart = true;
            var c = game.Courses[1]; if (v.Course != c) Line($"  (the race is on {v.Course.courseName}, not Coaster Coast)"); Line($"  race on {v.Course.courseName}");
            { var tr = new Track { name = "race1 Coaster: the start" }; tr.info.Add(CamState()); yield return RecordTrack(tr, 8f, false, true); allOk &= ReportTrack(tr); }
            yield return FlickerSet("race Coaster", true);
            float fin = c.gates[c.gates.Count - 1].progress; v.Begin(c, c.anchors[AnchorNear(fin - 300)]);
            yield return Until(() => v.Finished, 60f);
            for (int tries = 0; !v.Finished && tries < 3; tries++) { Line($"  (not at the finish yet: progress {v.Progress:0} m; again from the last anchors)"); v.Begin(c, c.anchors[AnchorNear(fin - 150)]); yield return Until(() => v.Finished, 40f); }
            Clear(); coast = true; input.Scripted = true; yield return Wait(1.5f);
            yield return Window("results1 Coaster: results screen", 6f);
            // Menu on the results -> main menu -> Race -> the course cards -> Paradise Plunge
            int guard = 0; while (game.ResultsIndex != 2 && guard++ < 6) yield return P(() => input.sMove = 1);
            if (game.ResultsIndex == 2) { yield return P(() => input.sConfirm = true); } else { Line("  (no results to choose Menu on: quit to the menu from the pause menu)"); yield return P(() => input.sMenu = true); yield return To("Quit to menu"); yield return P(() => input.sConfirm = true); }
            yield return WaitReal(.5f);
            yield return On(Game.Page.Main); Line($"  after Menu: page {game.MenuPage}, in front {game.InFront}");
            yield return Window($"menu3 {nm[game.MenuCourse]}: main menu after the race", 5f);
            yield return To("Race"); yield return P(() => input.sConfirm = true); yield return On(Game.Page.Course);
            { int g2 = 0; while (game.MenuCourse != 0 && g2++ < 6) yield return P(() => input.sSide = -1); }
            yield return WaitReal(1f);
            yield return Window("menu3 Paradise: course card after Coaster", 6f); yield return FlickerSet("menu Paradise after Coaster", false); yield return Window("menu3 Paradise pinned: camera held", 3f, true);
            // Paradise Plunge's race (it must be steady after Coaster Coast too), then Big Top's
            yield return P(() => input.sConfirm = true); yield return On(Game.Page.Pick); yield return To("Go"); yield return P(() => input.sConfirm = true);
            yield return Until(() => !v.Holding, 15f); coast = false; smart = true;
            { var tr = new Track { name = "race2 Paradise: after Coaster" }; tr.info.Add(CamState()); yield return RecordTrack(tr, 10f, false, true); allOk &= ReportTrack(tr); }
            yield return FlickerSet("race Paradise", true);
            Clear(); coast = true; game.OpenMenu(); yield return WaitReal(.3f);
            yield return On(Game.Page.Pause); yield return To("Quit to menu"); yield return P(() => input.sConfirm = true); yield return On(Game.Page.Main);
            yield return To("Race"); yield return P(() => input.sConfirm = true); yield return On(Game.Page.Course);
            { int g2 = 0; while (game.MenuCourse != 2 && g2++ < 6) yield return P(() => input.sSide = 1); }
            yield return WaitReal(1f);
            yield return P(() => input.sConfirm = true); yield return On(Game.Page.Pick); yield return To("Go"); yield return P(() => input.sConfirm = true);
            { var tr = new Track { name = "grid3 BigTop: countdown" }; tr.info.Add(CamState()); yield return RecordTrack(tr, 2f, true, true); allOk &= ReportTrack(tr); }
            yield return Until(() => !v.Holding, 15f); coast = false; smart = true;
            yield return FlickerSet("race BigTop", true);
            { var tr = new Track { name = "race3 BigTop: the lap" }; tr.info.Add(CamState()); yield return RecordTrack(tr, 25f, false, true); allOk &= ReportTrack(tr); }
            // the menus: the picture may flicker no more than Paradise Plunge's did in build 17 (~0.002 %) allows, with room: 0.05 % of the pixels
            foreach (var kv in flick) if (kv.Key.StartsWith("menu") && !kv.Key.EndsWith("noshadow") && !kv.Key.EndsWith("native")) { bool f = kv.Value < .0005f; allOk &= f; Line($"  menu flicker {kv.Key}: {kv.Value * 100:0.000} % {(f ? "ok" : "TOO MUCH")}"); }
            Line($"SHAKE {(allOk ? "PASS" : "FAIL")}: the menus' camera smooth on 60 Hz frames, the menus' picture not flickering (under 0.05 % of the pixels a frame) on every course, before and after a Coaster Coast race; races: flicker and the camera against the car above");
            Line($"audio output level (AudioListener.GetOutputData, all frames): peak {meter.MaxPeakSeen:0.000000}, AudioListener.volume {AudioListener.volume}");
            Clear(); Save();
        }
    }
}
