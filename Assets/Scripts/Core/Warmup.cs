using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 8 item 8: the hitches. Profiled in a development build (HitchProbe, Docs/Checks/round8): the long frames were not GC, audio or
    // scripts but the first render of scenery the camera had not seen before (no GC, no allocations; the time inside the render). The
    // game runs on Direct3D 12 (and on the Deck through Proton's vkd3d), where each new shader / render-state combination builds its
    // pipeline state on first draw, and runtime-built meshes and textures upload on first use. So, once at start-up (before the first
    // frame, while the game is still loading), a hidden camera renders the course from points along the whole racer line (forward and
    // back, wide angle, every layer, shadows on) plus every vehicle form: each pipeline, mesh and texture is made then, not mid-race.
    public static class Warmup
    {
        public static float Seconds { get; private set; }
        public static int Views { get; private set; }

        public static void Run(Course c, Camera like, IEnumerable<FormSet> sets)
        {
            if (!c || c.line == null || c.line.Count < 2) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var g = new GameObject("Warm-up camera"); var cam = g.AddComponent<Camera>();
            if (like) cam.CopyFrom(like);
            cam.enabled = false; cam.fieldOfView = 100; cam.farClipPlane = 2600; cam.nearClipPlane = .2f; cam.cullingMask = ~0;
            cam.layerCullDistances = new float[32];
            var rt = new RenderTexture(640, 400, 24); cam.targetTexture = rt;
            int n = 0;
            float len = c.Length, step = Mathf.Max(60f, len / 32f);
            for (float p = 0; p < len; p += step)
            {
                var at = c.PointAt(p, out var d); if (d.sqrMagnitude < 1e-4f) d = Vector3.forward;
                foreach (var look in new[] { d, -d })
                {
                    if (look.sqrMagnitude < 1e-4f) continue;
                    cam.transform.SetPositionAndRotation(at + Vector3.up * 4f, Quaternion.LookRotation(look.normalized, Vector3.up));
                    cam.Render(); n++;
                }
            }
            // high over the island (the flight and the jumps see the whole lake and valley)
            foreach (var at in new[] { new Vector3(0, 60, 500), new Vector3(400, 120, 0), new Vector3(300, 80, -600), new Vector3(-200, 80, 300) })
                for (int k = 0; k < 4; k++) { cam.transform.SetPositionAndRotation(at, Quaternion.Euler(25, k * 90, 0)); cam.Render(); n++; }
            // every form of every vehicle, close up (their materials, the swap burst)
            if (sets != null)
                foreach (var fs in sets)
                {
                    if (!fs || !fs.gameObject.activeInHierarchy) continue;
                    var keep = fs.Current;
                    foreach (var m in FormSet.All) { fs.Snap(m); cam.transform.position = fs.transform.position + fs.transform.forward * 7 + Vector3.up * 2.5f; cam.transform.LookAt(fs.transform.position); cam.Render(); n++; }
                    fs.Snap(keep);
                }
            cam.targetTexture = null; Object.Destroy(g); rt.Release(); Object.Destroy(rt);
            Seconds = (float)sw.Elapsed.TotalSeconds; Views = n;
            Debug.Log($"[Warmup] {n} views rendered in {Seconds:0.00} s (pipelines, meshes and textures made before the race)");
        }
    }
}
