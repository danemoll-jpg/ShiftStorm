using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 15 checks (Dan on build 16: "The shimmering only happens on the 2nd track and it is before and after the race"; then Barnaby by
    // name and a third course, Big Top Breakdown).
    //   shimmer  every course: 10 s parked on the grid before GO, 10 s after the finish (results up), 10 s on the grid after Race again;
    //            every frame: the camera's and the car's position / aim, the sun's direction, the sky's evening value, the fog colour, and the
    //            rendered picture (320 x 200) compared with the frame before (mean change, share of pixels changing); a heat map of where
    //            the picture changed (Docs/Checks/round15/shimmer-*.png). "-oldSky": build 16's evening (the cause, for before numbers).
    //   bigtop   Big Top Breakdown: the check pilot's lap (segments, every form change, pictures per segment), the Globe of Death at full /
    //            slow / very slow speed, the wagons (hits), both splits timed (car / fly over the Globe; Showboat channel / main line),
    //            set-piece visibility per segment
    //   round15 = shimmer + bigtop (the soaks, AI laps per split and split screen run as soak / split / btsplits with -course 2)
    public sealed partial class CheckPilot
    {
        IEnumerator Round15(string which)
        {
            bool all = which == "round15";
            if (all || which == "shimmer") yield return ShimmerCheck();
            if (all || which == "bigtop" || which == "btlap") yield return BigTopLap();
            if (all || which == "bigtop" || which == "btglobe") yield return BigTopGlobe();
            if (all || which == "bigtop" || which == "btsplits") yield return BigTopSplits();
        }

        // ---------------- item 1: the shimmer ----------------
        sealed class Still { public List<Vector3> camP = new List<Vector3>(), camF = new List<Vector3>(), carP = new List<Vector3>(), sun = new List<Vector3>(); public List<float> eve = new List<float>(), diff = new List<float>(), share = new List<float>(); public List<Color> fog = new List<Color>(); public float[] heat; }
        RenderTexture smallRt; Texture2D smallTex;
        Color32[] SmallFrame()
        {
            if (!smallRt) { smallRt = new RenderTexture(320, 200, 0); smallTex = new Texture2D(320, 200, TextureFormat.RGB24, false); }
            cam.Render(); Graphics.Blit(rt, smallRt);
            var prev = RenderTexture.active; RenderTexture.active = smallRt; smallTex.ReadPixels(new Rect(0, 0, 320, 200), 0, 0); smallTex.Apply(); RenderTexture.active = prev;
            shotLastFrame = true; return smallTex.GetPixels32();
        }
        IEnumerator RecordStill(Still s, float seconds)
        {
            Color32[] last = null; s.heat = new float[320 * 200]; float t0 = Time.time;
            var sunL = RenderSettings.sun; if (!sunL) foreach (var l in FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sunL = l; break; }
            while (Time.time - t0 < seconds)
            {
                yield return new WaitForEndOfFrame();
                s.camP.Add(cam.transform.position); s.camF.Add(cam.transform.forward); s.carP.Add(v.transform.position); s.sun.Add(sunL ? sunL.transform.forward : Vector3.zero);
                s.eve.Add(CoasterLook.Now); s.fog.Add(RenderSettings.fogColor);
                var px = SmallFrame();
                if (last != null)
                {
                    double sum = 0; int ch = 0;
                    for (int i = 0; i < px.Length; i++)
                    {
                        int d = Mathf.Abs(px[i].r - last[i].r) + Mathf.Abs(px[i].g - last[i].g) + Mathf.Abs(px[i].b - last[i].b);
                        sum += d / 3.0; if (d > 24) { ch++; s.heat[i] += 1; }
                    }
                    s.diff.Add((float)(sum / px.Length)); s.share.Add(ch / (float)px.Length);
                }
                last = px;
            }
        }
        void HeatMap(Still s, string name)
        {
            var tex = new Texture2D(320, 200, TextureFormat.RGB24, false); float max = Mathf.Max(1, s.heat.Max()); var px = new Color32[s.heat.Length];
            var shot = smallTex.GetPixels32();
            for (int i = 0; i < px.Length; i++) { float k = s.heat[i] / max; var g = shot[i]; byte grey = (byte)((g.r + g.g + g.b) / 9); px[i] = k > 0 ? new Color32((byte)(80 + 175 * k), (byte)(40 * (1 - k)), (byte)(40 * (1 - k)), 255) : new Color32(grey, grey, grey, 255); }
            tex.SetPixels32(px); tex.Apply(); File.WriteAllBytes(Path.Combine(dir, $"{++shotIndex:00}-shimmer-{name}-heat.png"), tex.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(dir, $"{++shotIndex:00}-shimmer-{name}-frame.png"), smallTex.EncodeToPNG());
        }
        static float Spread(List<Vector3> l) { if (l.Count == 0) return 0; float m = 0; for (int i = 1; i < l.Count; i++) m = Mathf.Max(m, (l[i] - l[0]).magnitude); return m; }
        static float AngSpread(List<Vector3> l) { if (l.Count == 0) return 0; float m = 0; for (int i = 1; i < l.Count; i++) m = Mathf.Max(m, Vector3.Angle(l[i], l[0])); return m; }
        bool ReportStill(Still s, string what, bool cameraMoves = false)
        {
            int n = s.diff.Count; if (n == 0) { Line($"  {what}: no frames"); return false; }
            var d = s.diff.OrderBy(x => x).ToList(); var sh = s.share.OrderBy(x => x).ToList();
            float eveSpan = s.eve.Max() - s.eve.Min(); int eveSteps = 0; for (int i = 1; i < s.eve.Count; i++) if (s.eve[i] != s.eve[i - 1]) eveSteps++;
            float fogSpan = 0; for (int i = 1; i < s.fog.Count; i++) fogSpan = Mathf.Max(fogSpan, Mathf.Abs(s.fog[i].r - s.fog[0].r) + Mathf.Abs(s.fog[i].g - s.fog[0].g) + Mathf.Abs(s.fog[i].b - s.fog[0].b));
            float sunDeg = AngSpread(s.sun);
            // steady: the camera and the car still (under 1 mm / 0.01 deg), the sun and the sky not changing; the picture's frame-to-frame change
            // is then only what is meant to move (the sea's waves, rides, fireworks, rivals leaving the grid)
            // after the finish the camera circles the car on purpose (Round 14's finish camera; its smoothness is the finishcam check): there
            // only the sun and the sky must hold still
            bool ok = (cameraMoves || (Spread(s.camP) < .001f && AngSpread(s.camF) < .01f)) && sunDeg < .001f && eveSteps == 0;
            Line($"  {what} {(ok ? "STEADY" : "CHANGING")}: {n + 1} frames; camera moved at most {Spread(s.camP) * 1000:0.00} mm / {AngSpread(s.camF):0.0000} deg, car {Spread(s.carP) * 1000:0.00} mm; sun turned {sunDeg:0.0000} deg; evening {s.eve.Min():0.000}-{s.eve.Max():0.000} ({eveSteps} changes); fog colour changed {fogSpan:0.0000}; picture change per frame: mean {d.Average():0.00} / 255 (median {d[n / 2]:0.00}, 95th pct {d[(int)(n * .95f)]:0.00}), pixels changing (> 8 levels): median {sh[n / 2] * 100:0.00} %, 95th pct {sh[(int)(n * .95f)] * 100:0.00} %");
            return ok;
        }
        IEnumerator ShimmerCheck()
        {
            scenario = "shimmer"; Directory.CreateDirectory(dir);
            Line($"SHIMMER (item 1){(CoasterSky.OldSky ? " BUILD 16's SKY (-oldSky)" : "")}: per course, 10 s on the grid before GO (every racer held), 10 s after the finish (the results up, nothing pressed), 10 s on the grid after Race again; the camera, the car, the sun, the sky and the picture logged every frame (graphics {GraphicsPreset.Applied})");
            bool allOk = true;
            for (int course = 0; course < game.Courses.Count; course++)
            {
                var c = game.Courses[course];
                Clear(); game.StartCourse(course, true); yield return Wait(.3f);
                foreach (var r in game.Race.Racers) if (r) r.HoldFor(12f);
                yield return Wait(.5f);
                var a = new Still(); yield return RecordStill(a, 10f); bool okA = ReportStill(a, $"{c.courseName,-18} grid before GO     "); HeatMap(a, $"{course}-grid");
                // to the line from the last anchors, then the results
                yield return Until(() => !v.Holding, 8f);
                float fin = c.gates[c.gates.Count - 1].progress;
                v.Begin(c, c.anchors[AnchorNear(fin - 260)]); smart = true;
                yield return Until(() => v.Finished, 60f);
                Clear(); input.Scripted = false; yield return Wait(3f);
                var b = new Still(); yield return RecordStill(b, 10f); bool okB = ReportStill(b, $"{c.courseName,-18} after the finish   ", true); HeatMap(b, $"{course}-results");
                input.Scripted = true;
                game.Restart(); yield return Wait(.3f);
                foreach (var r in game.Race.Racers) if (r) r.HoldFor(12f);
                yield return Wait(.5f);
                var e = new Still(); yield return RecordStill(e, 10f); bool okE = ReportStill(e, $"{c.courseName,-18} grid after Race again"); HeatMap(e, $"{course}-regrid");
                allOk &= okA && okB && okE;
            }
            Line($"SHIMMER {(allOk ? "PASS" : "FAIL")}: parked = camera, car, sun and sky still on every course (after the finish: the sun and sky; the camera circles on purpose) (the picture then changes only where things are meant to move: see the heat maps)");
            Clear(); Save();
        }
    }
}
