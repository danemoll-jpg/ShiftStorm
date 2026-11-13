using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 2: the full Paradise Plunge lap in gray box, dressed as Jurassic Par (a new course in its look, not a Tee'd Off layout). Built at
    // start-up from one racer line (control points below, smoothed): every road, bank, cave wall, water body, gate, zone, anchor and split is
    // placed by progress along that line, so geometry and race logic always agree. Concept order and budgets (150 s on the safe line):
    //   1 Beach Promenade 20 s (jungle lakeshore road, start / finish)   2 Cliffside Climb 20 s (two drift corners up mossy cliffs, launch ramp)
    //   3 Sky Arch 25 s (flight through a huge mossy rock arch over the dinosaur valley)   4 Lagoon Landing 15 s (splashdown in a jungle lake)
    //   5 Mangrove Run 20 s (swamp river S-bend, current ribbons)   6 Sunken Gateway 10 s (descending stone opening under the cliffs)
    //   7 Coral Vault 25 s (flooded cave: mossy rock arches, roots, glowing crystals, vertical slalom)   8 Waterfall Return 15 s (surfacing pool
    //   behind a waterfall, water ramp through the curtain, landing on the finishing road).
    // Horseshoe: the road climbs round the outside of the island, the flight crosses the central valley to the far shore, the river curves back
    // and the flooded cave runs under the finishing road (the stacked sections Course.Project's progress window keeps apart). Sizes are metres;
    // water level is y = 0, the road's surface y = 2.
    public static partial class ParadisePlunge
    {
        static Transform root, scen; static Course c;
        static readonly Dictionary<string, float> mark = new Dictionary<string, float>();
        public static IReadOnlyDictionary<string, float> Marks => mark;

        // the racer line: (mark, x, y, z); y is the height a racer aims for (road + 0.9, water + 0.6, flight altitude, dive depth)
        static readonly (string n, float x, float y, float z)[] Points =
        {
            ("lineStart", 0, 2.9f, -60), ("start", 0, 2.9f, -20), ("G0", 0, 2.9f, 0),
            // Round 4 item 3: two points after G0 so the spline's spacing is even there. Build 4 went straight from G0 to (20, 140): the
            // tangent at G0 was four times the 20 m segment before it, so the line looped back on itself 8 m before G0 (sideways 2.5 m and
            // backwards); the promenade's barrier strips, built along the line, followed the loop and cut diagonally across the start road
            // (Dan's "two beams in the middle of the road at the start / finish line").
            ("", 0, 2.9f, 20), ("", 4, 2.9f, 60),
            ("", 20, 2.9f, 140), ("promMid", -20, 2.9f, 300), ("", 18, 2.9f, 460), ("G1", 0, 2.9f, 580),
            ("climb0", 0, 3.4f, 640), ("corner1", 12, 5, 676), ("", 50, 8, 690), ("climbMid", 190, 18, 697), ("", 340, 28, 694), ("corner2", 392, 31, 676), ("", 410, 32, 636),
            ("rampStart", 412, 33, 600), ("lip", 412, 43, 556),
            ("", 412, 48, 530), ("", 350, 52, 400), ("arch", 225, 52, 260), ("", 205, 47, 110), ("", 250, 37, -30), ("", 330, 24, -150), ("", 400, 10, -270), ("splash", 430, .6f, -380),
            ("", 436, .6f, -440), ("", 452, .6f, -505), ("", 432, .6f, -575), ("", 365, .6f, -600), ("lagoonEnd", 300, .6f, -588),
            ("", 250, .6f, -565), ("bend1", 222, .6f, -505), ("bend2", 240, .6f, -440), ("mangMid", 222, .6f, -370), ("bend3", 245, .6f, -300), ("bend4", 275, .6f, -235), ("", 296, .6f, -180),
            ("G5", 285, .6f, -138), ("", 250, .6f, -122), ("", 225, .6f, -121),
            ("diveZone", 205, 0, -121), ("", 180, -3.5f, -120), ("portal", 165, -5.5f, -120), ("", 125, -8.5f, -120), ("", 85, -9, -120),
            ("vaultStart", 50, -9, -120), ("underRoad", 0, -9, -120), ("hi1", -65, -6, -128), ("lo1", -112, -13, -160), ("hi2", -150, -6, -240), ("lo2", -140, -13, -322), ("", -95, -8, -360), ("G7", -45, -9, -362),
            ("", -12, -7, -330), ("poolEdge", 0, -3, -300), ("surface", 0, .6f, -268), ("G8", 0, .6f, -238), ("rampFoot", 0, .6f, -222), ("rampLip", 0, 5.6f, -182), ("landing", 0, 4.5f, -160),
            ("G9", 0, 2.9f, -140), ("", 0, 2.9f, -60), ("finish", 0, 2.9f, 0), ("lineEnd", 0, 2.9f, 20),
        };
        public const float RoadY = 2f, Water = 0f, CaveRoof = -1.5f, CaveFloor = -18f, CaveHalf = 17f;
        // Round 2b: the whole course is S times larger on the ground plan than build 2 (heights unchanged), so the lap stays about 150 s at
        // the new speeds (about 1.6 x). The control points, the land, the lagoon, the pool and the scenery are written in build 2's metres and
        // scaled by W(); everything placed along the line (roads, banks, cave, gates, zones, anchors, signs) follows the scaled line.
        public const float S = 1.7f;
        static Vector3 W(float x, float y, float z) => new Vector3(x * S, y, z * S);

        public static Course Build(Transform parent)
        {
            mark.Clear();
            var g = new GameObject("Course Paradise Plunge"); g.transform.SetParent(parent, false); root = g.transform;
            scen = new GameObject("Scenery").transform; scen.SetParent(root, false);
            c = g.AddComponent<Course>(); c.courseName = "Paradise Plunge"; c.timeTrial = true; c.look = "jurassic";
            c.halfWidth = 75; c.airHalfWidth = 420; c.minY = -45; c.maxY = 130; c.endMargin = 40; c.startMode = Mode.Drive;
            MakeLine();
            Land(); Road(); Lagoon(); Mangrove(); Cave(); Return(); Arch(); Scenery(); Polish();   // Round 5 item 6: the polished stretch
            Gates(); RouteMarks(); Zones(); Anchors(); Signs(); Segments(); Shortcuts();
            return c;
        }

        // ---------------- the line ----------------
        static void MakeLine()
        {
            var pts = new List<Vector3>(); var at = new List<int>();
            int n = Points.Length;
            Vector3 P(int i) => W(Points[Mathf.Clamp(i, 0, n - 1)].x, Points[Mathf.Clamp(i, 0, n - 1)].y, Points[Mathf.Clamp(i, 0, n - 1)].z);
            for (int i = 0; i < n - 1; i++)
            {
                at.Add(pts.Count);
                Vector3 p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
                int steps = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(p1, p2) / 3f));
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)steps, t2 = t * t, t3 = t2 * t;
                    pts.Add(.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3));
                }
            }
            at.Add(pts.Count); pts.Add(P(n - 1));
            c.line = pts; c.Finish();
            for (int i = 0; i < n; i++) if (Points[i].n != "") mark[Points[i].n] = c.CumulativeAt(at[i]);
        }
        static float M(string n) => mark[n];
        static Vector3 At(float prog) => c.PointAt(prog, out _);
        static Vector3 Dir(float prog) { c.PointAt(prog, out var d); return d; }
        static Vector3 Flat(Vector3 d) { d.y = 0; return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward; }
        static Vector3 Right(float prog) => Vector3.Cross(Vector3.up, Flat(Dir(prog)));
        static Quaternion Facing(float prog, bool flat = true) => Quaternion.LookRotation(flat ? Flat(Dir(prog)) : Dir(prog), Vector3.up);

        // cross-sections between two progress values
        static List<MeshKit.Sample> Samples(float a, float b, float l, float r, Func<float, Vector3, float> top, Func<float, Vector3, float> bottom, float step = 4f)
        {
            var list = new List<MeshKit.Sample>(); int n = Mathf.Max(1, Mathf.CeilToInt((b - a) / step));
            for (int i = 0; i <= n; i++)
            {
                float p = Mathf.Lerp(a, b, i / (float)n); var pt = At(p);
                list.Add(new MeshKit.Sample { c = pt, right = Right(p), l = l, r = r, top = top(p, pt), bottom = bottom(p, pt) });
            }
            return list;
        }
        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, Quaternion? rot = null, bool collider = true, int layer = 0)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
            g.transform.SetPositionAndRotation(center, rot ?? Quaternion.identity); g.transform.localScale = size; g.layer = layer;
            g.GetComponent<Renderer>().sharedMaterial = mat; if (!collider) UnityEngine.Object.Destroy(g.GetComponent<Collider>());
            return g;
        }
        static T Vol<T>(string name, Vector3 center, Quaternion rot, Vector3 size) where T : BoxVolume
        {
            var g = new GameObject(name); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(center, rot);
            var v = g.AddComponent<T>(); v.size = size; return v;
        }
        // water bodies along the line: overlapping boxes, top at the water level
        static void Channel(string name, float a, float b, float width, float bottom, float step = 24f)
        {
            for (float p = a; p < b; p += step)
            {
                float m = Mathf.Min(p + step * .5f, b); var pt = At(m); pt.y = (Water + bottom) * .5f;
                Vol<WaterVolume>(name, pt, Facing(m), new Vector3(width, Water - bottom, step + 10f));
            }
        }

        static Material Mt(Color col, bool outline = false, Color emission = default, float outlineScale = 1f) => JurassicLook.Mat(col, outline, emission, outlineScale);

        // ---------------- land ----------------
        static void Land()
        {
            var ground = Mt(JurassicLook.Jungle); var rock = Mt(JurassicLook.Rock); var sand = Mt(JurassicLook.Sand);
            // the sea and lakes everywhere (visual only; the route's water bodies are WaterVolumes)
            var sea = new GameObject("Sea (visual)"); sea.transform.SetParent(root, false);
            sea.AddComponent<MeshFilter>().sharedMesh = Grid(3200 * S, 48); sea.transform.position = W(200, Water - .02f, 50);
            var sr = sea.AddComponent<MeshRenderer>(); sr.sharedMaterial = JurassicLook.WaterMat(JurassicLook.Water, JurassicLook.WaterLight, JurassicLook.WaterRim, 0f); sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // the lake west of the promenade (a car in it is recovered)
            Vol<WaterVolume>("Lake", W(-340, -4, 420), Quaternion.identity, W(600, 8, 960));
            // west arm (the promenade's island), its lakeshore beach, the island over the cave (with the pool's opening)
            Box(root, "West arm", W(10, 1.25f, 290), W(100, .5f, 700), ground);
            Box(root, "Beach", W(-48, -.5f, 330), W(16, 2.4f, 780), sand);
            Box(root, "Island W", W(-106, 1.25f, -225), W(168, .5f, 330), ground);
            Box(root, "Island E", W(46, 1.25f, -225), W(48, .5f, 330), ground);
            Box(root, "Island S", W(0, 1.25f, -340), W(44, .5f, 100), ground);
            Box(root, "Island N", W(0, 1.25f, -127), W(44, .5f, 134), ground);
            Box(root, "Ridge", W(122, 1.25f, -120), W(100, .5f, 70), ground);
            Box(root, "Island beach S", W(-60, -.5f, -400), W(280, 2.4f, 20), sand);
            // the central valley (dinosaurs), the east arm's high cliffs, the far north hills
            Box(root, "Valley floor", W(290, -.3f, 300), W(460, 1.4f, 800), Mt(JurassicLook.JungleDark));
            for (int i = 0; i < 9; i++)
            {
                float z = -80 + i * 95, h = 38 + (i * 37 % 17);
                Box(root, "East cliff", W(560 + (i % 2) * 14, h * .5f - 1, z), W(70, h, 100), rock);
                Box(root, "East cliff moss", W(560 + (i % 2) * 14, h - .5f, z), W(72, 2, 102), Mt(JurassicLook.Moss));
            }
            for (int i = 0; i < 6; i++) { float x = -20 + i * 80, h = 50 + (i * 23 % 19); Box(root, "North hill", W(x, h * .5f, 790 + (i % 2) * 20), W(90, h, 90), rock); Box(root, "North hill moss", W(x, h, 790 + (i % 2) * 20), W(92, 3, 92), Mt(JurassicLook.Moss)); }
        }
        static Mesh Grid(float size, int cells)
        {
            var v = new List<Vector3>(); var t = new List<int>(); float h = size * .5f, s = size / cells;
            for (int z = 0; z <= cells; z++) for (int x = 0; x <= cells; x++) v.Add(new Vector3(-h + x * s, 0, -h + z * s));
            for (int z = 0; z < cells; z++) for (int x = 0; x < cells; x++) { int i = z * (cells + 1) + x; t.Add(i); t.Add(i + cells + 1); t.Add(i + 1); t.Add(i + 1); t.Add(i + cells + 1); t.Add(i + cells + 2); }
            var m = new Mesh { name = "grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // ---------------- road: promenade, climb, ramp; and the finishing road ----------------
        // Round 2b: wider for the new speeds. Round 4: the width (and the barriers' height) changes smoothly over 30 m instead of in steps,
        // so no barrier end ever faces the traffic (build 4: 0.5 m and 1.5 m steps at climb0 and the ramp); the finishing road is 13 m like
        // the promenade it overlaps at the start / finish (build 4: 14 m, its barriers 1 m outside the promenade's on the shared stretch).
        static float HalfWidth(float p)
        {
            if (p >= M("landing") - 12) return 13f;
            float a = M("climb0"), b = M("rampStart") - 10;
            return p < b - 30 ? Mathf.Lerp(13f, 12.5f, Mathf.InverseLerp(a - 15, a + 15, p)) : Mathf.Lerp(12.5f, 11f, Mathf.InverseLerp(b - 30, b + 5, p));
        }
        public static float RoadHalfAt(float p) => HalfWidth(p);
        static float BarrierTop(float p, bool right) => Mathf.Lerp(.8f, right ? 1.3f : 1.1f, Mathf.InverseLerp(M("climb0") - 15, M("climb0") + 15, p));
        // cross-sections whose edges vary along the line
        static List<MeshKit.Sample> SamplesW(float a, float b, Func<float, float> l, Func<float, float> r, Func<float, Vector3, float> top, Func<float, Vector3, float> bottom, float step = 4f)
        {
            var list = new List<MeshKit.Sample>(); int n = Mathf.Max(1, Mathf.CeilToInt((b - a) / step));
            for (int i = 0; i <= n; i++)
            {
                float p = Mathf.Lerp(a, b, i / (float)n); var pt = At(p);
                list.Add(new MeshKit.Sample { c = pt, right = Right(p), l = l(p), r = r(p), top = top(p, pt), bottom = bottom(p, pt) });
            }
            return list;
        }
        static void Road()
        {
            var road = Mt(JurassicLook.Road); var rock = Mt(JurassicLook.Rock); var barrier = Mt(JurassicLook.Stone, true, default, .6f);
            float a = 0, b = M("lip"), climb = M("climb0");
            // the road in pieces so the width can change; its skirt reaches the land (promenade) or the valley floor (climb: a mossy cliff)
            float[] cuts = { a, climb, M("rampStart") - 10, b };
            for (int k = 0; k < cuts.Length - 1; k++)
            {
                float lo = k == 0 ? 1f : -1f;
                MeshKit.Strip(root, "Road", SamplesW(cuts[k], cuts[k + 1], p => -HalfWidth(p), p => HalfWidth(p), (p, pt) => pt.y - .9f, (p, pt) => lo, 3f), road, rock);
                // low barriers at both edges (the valley side a little higher on the climb)
                MeshKit.Strip(root, "Barrier L", SamplesW(cuts[k], cuts[k + 1], p => -HalfWidth(p) - .7f, p => -HalfWidth(p), (p, pt) => pt.y - .9f + BarrierTop(p, false), (p, pt) => pt.y - 1.6f, 3f), barrier, null, true, ShiftVehicle.ObstacleLayer);
                MeshKit.Strip(root, "Barrier R", SamplesW(cuts[k], cuts[k + 1], p => HalfWidth(p), p => HalfWidth(p) + .7f, (p, pt) => pt.y - .9f + BarrierTop(p, true), (p, pt) => pt.y - 1.6f, 3f), barrier, null, true, ShiftVehicle.ObstacleLayer);
            }
            // the hillside above the climb (left: the outside of both corners), mossy on top
            MeshKit.Strip(root, "Climb cliff", Samples(climb + 10, M("rampStart") - 20, -40f, -13.3f,   // Round 4: outside the barrier (build 4: -11.5, 1 m inside the road's edge)
                 (p, pt) => pt.y + 9f, (p, pt) => -1f, 4f), Mt(JurassicLook.Moss), rock);
            // ramp: wind streamers either side
            for (int i = 0; i < 5; i++)
            {
                float p = M("rampStart") - 30 + i * 12; var pt = At(p); var r = Right(p);
                foreach (float s in new[] { -1f, 1f }) Box(scen, "streamer", pt + r * s * 11 + Vector3.up * (5 + i * .8f), new Vector3(.2f, .2f, 7), Mt(new Color(.35f, .7f, 1f)), Facing(p), false);
            }
            // the finishing road: from the landing to where the promenade's start piece begins (no overlap)
            float fa = M("landing") - 12, fb = M("lineEnd") - 81f;
            MeshKit.Strip(root, "Finishing road", Samples(fa, fb, -13, 13, (p, pt) => RoadY, (p, pt) => 1f, 3f), road, rock);
            // Round 4: the barriers end exactly where the promenade's begin (the line's first point), end to end with the same section; in
            // build 4 they ran 55 m past it beside the promenade's, whose end face then stood in the way of a car sliding along the wall
            float fbb = M("lineEnd") - Vector3.Distance(At(M("lineEnd")), c.line[0]);
            MeshKit.Strip(root, "Barrier L", Samples(fa + 8, fbb, -13.7f, -13f, (p, pt) => RoadY + .8f, (p, pt) => RoadY - .7f, 3f), barrier, null, true, ShiftVehicle.ObstacleLayer);
            MeshKit.Strip(root, "Barrier R", Samples(fa + 8, fbb, 13f, 13.7f, (p, pt) => RoadY + .8f, (p, pt) => RoadY - .7f, 3f), barrier, null, true, ShiftVehicle.ObstacleLayer);
            // start / finish banner and grid lines
            var fin = new Vector3(0, RoadY, 0);
            Box(root, "Finish line", fin + Vector3.up * .03f, new Vector3(26, .06f, 2.2f), Mt(new Color(.1f, .1f, .1f)), null, false);
            for (int i = 0; i < 13; i++) Box(root, "Finish check", fin + new Vector3(-12 + i * 2, .07f, (i % 2) * 1f - .5f), new Vector3(1, .02f, 1), Mt(Color.white), null, false);
            foreach (float x in new[] { -15.5f, 15.5f }) Box(root, "Banner post", fin + new Vector3(x, 4.5f, 0), new Vector3(.8f, 9, .8f), Mt(JurassicLook.FernTrunk, true));
            Box(root, "Banner", fin + new Vector3(0, 8.6f, 0), new Vector3(31.8f, 2.2f, .5f), Mt(JurassicLook.Amber, true));
        }

        // ---------------- lagoon ----------------
        static void Lagoon()
        {
            var bank = Mt(JurassicLook.Jungle); var mud = Mt(JurassicLook.Mud);
            Vol<WaterVolume>("Lagoon", W(400, -8, -460), Quaternion.identity, W(200, 16, 320));
            Box(root, "Lagoon bank N", W(415, -7, -290), W(230, 17, 20), bank);
            Box(root, "Lagoon bank E", W(510, -7, -470), W(20, 17, 380), bank);
            Box(root, "Lagoon bank S", W(400, -7, -630), W(240, 17, 20), bank);
            Box(root, "Lagoon bank W", W(290, -7, -415), W(20, 17, 250), bank);
            Box(root, "Lagoon bed", W(400, -16.5f, -460), W(200, 1, 320), mud);
            // foam markers outline the landing corridor; buoys after a wide runout
            float s = M("splash");
            for (int i = -3; i <= 4; i++) foreach (float side in new[] { -1f, 1f })
            {
                float p = s + i * 15; var pt = At(p); pt.y = Water; var r = Right(p);
                Cyl(root, "foam marker", pt + r * side * 20 + Vector3.up * .2f, .5f, 1.4f, Mt(Color.white, true), false);
            }
            foreach (float p in new[] { s + 95, s + 140, s + 185 }) foreach (float side in new[] { -1f, 1f })
            {
                var pt = At(p); pt.y = Water; Cyl(root, "buoy", pt + Right(p) * side * 17 + Vector3.up * .3f, .8f, 2.2f, Mt(new Color(1f, .5f, .12f), true), true, ShiftVehicle.ObstacleLayer);
            }
        }
        static GameObject Cyl(Transform parent, string name, Vector3 at, float radius, float height, Material mat, bool collider, int layer = 0)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name; g.transform.SetParent(parent, false); g.transform.position = at;
            g.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2); g.layer = layer; g.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.Destroy(g.GetComponent<Collider>()); if (collider) { var cc = g.AddComponent<CapsuleCollider>(); cc.radius = .5f; cc.height = 2; }
            return g;
        }

        // ---------------- mangrove river ----------------
        static void Mangrove()
        {
            float a = M("lagoonEnd") - 15, b = M("portal");
            Channel("River", a, b, 42, -16);
            var bankTop = Mt(JurassicLook.JungleDark); var mud = Mt(JurassicLook.Mud);
            MeshKit.Strip(root, "River bank L", Samples(a, M("portal"), -46, -20, (p, pt) => 1.4f, (p, pt) => -16f), bankTop, mud);
            MeshKit.Strip(root, "River bank R", Samples(a, M("portal"), 20, 46, (p, pt) => 1.4f, (p, pt) => -16f), bankTop, mud);
            MeshKit.Strip(root, "River bed", Samples(a, b, -21, 21, (p, pt) => -16f, (p, pt) => -17f, 6f), mud, null, true, 0, 40, false);
            // currents at the bends (pushing toward the outside of each bend), shown by foam ribbons drifting with them
            var foam = Mt(Color.white);
            foreach (var (n, side) in new[] { ("bend1", 1f), ("bend2", -1f), ("mangMid", 1f), ("bend3", -1f), ("bend4", 1f) })
            {
                float p = M(n); var pt = At(p); pt.y = -3; var r = Right(p);
                var cz = Vol<CurrentZone>("Current " + n, pt, Facing(p), new Vector3(38, 8, 44)); cz.push = r * side * 3.2f;
                for (int i = 0; i < 6; i++)
                {
                    var q = At(p - 18 + i * 7); q.y = Water + .04f;
                    var rib = Box(scen, "current ribbon", q + r * (-12 + (i * 5) % 24), new Vector3(.6f, .05f, 6), foam, Quaternion.LookRotation(r * side + Flat(Dir(p)) * .35f), false);
                    rib.AddComponent<Drift>().velocity = r * side * 2.2f;
                }
            }
        }

        // ---------------- the sunken gateway and the flooded cave ----------------
        static void Cave()
        {
            float a = M("portal") - 6, b = M("poolEdge");
            var rock = Mt(JurassicLook.CaveRock); var moss = Mt(JurassicLook.Moss); var stone = Mt(JurassicLook.Stone, true, default, .8f);
            var archStone = Mt(new Color(.62f, .6f, .5f), true, new Color(.05f, .07f, .06f), .8f); var edge = Mt(JurassicLook.Crystal, false, JurassicLook.Crystal * 1.2f);
            Channel("Cave water", M("portal") - 20, b + 6, CaveHalf * 2 + 4, CaveFloor - 1);
            // roof (just under the surface at the portal, so the opening is submerged), walls, floor
            MeshKit.Strip(root, "Cave roof", Samples(a, b, -CaveHalf - 5, CaveHalf + 5, (p, pt) => 1.0f, (p, pt) => CaveRoof, 3f), moss, rock);
            MeshKit.Strip(root, "Cave wall L", Samples(a, b, -CaveHalf - 5, -CaveHalf, (p, pt) => 1.0f, (p, pt) => CaveFloor - 2, 3f), rock);
            MeshKit.Strip(root, "Cave wall R", Samples(a, b, CaveHalf, CaveHalf + 5, (p, pt) => 1.0f, (p, pt) => CaveFloor - 2, 3f), rock);
            // Round 3: the floor has two openings on its left half (the Coral Vault tunnel's way down and back up)
            var sand = Mt(JurassicLook.Sand); float h0 = M("lo1") + HoleA.x, h1 = M("lo1") + HoleA.y, h2 = M("G7") + HoleB.x, h3 = M("G7") + HoleB.y;
            var pieces = VaultTunnel ? new[] { (M("diveZone") - 10, h0, -CaveHalf - 1), (h0, h1, 1f), (h1, h2, -CaveHalf - 1), (h2, h3, 1f), (h3, b + 10, -CaveHalf - 1) } : new[] { (M("diveZone") - 10, b + 10, -CaveHalf - 1) };
            foreach (var (fa, fb, l) in pieces)
                MeshKit.Strip(root, "Cave floor", Samples(fa, fb, l, CaveHalf + 1, (p, pt) => CaveFloor, (p, pt) => CaveFloor - 2, 4f), sand, rock);
            // the cliff over the portal, and the carved stone frame of the opening (its lintel just breaks the surface)
            float pp = M("portal") - 6; var at = At(pp); at.y = 0; var f = Facing(pp); var r = Right(pp);
            Box(root, "Gateway cliff", at + f * new Vector3(0, 13, 8), new Vector3(70, 30, 16), Mt(JurassicLook.Rock), f);
            Box(root, "Gateway cliff moss", at + f * new Vector3(0, 28.5f, 8), new Vector3(72, 2, 18), moss, f);
            Box(root, "Portal lintel", at + f * new Vector3(0, .4f, -1.2f), new Vector3(CaveHalf * 2 + 4, 2.6f, 2.4f), stone, f);
            foreach (float s in new[] { -1f, 1f }) Box(root, "Portal pillar", at + f * new Vector3(s * (CaveHalf + 1.5f), -6, -1.2f), new Vector3(3, 16, 3), stone, f);
            // descending guide lights into the opening
            var glow = JurassicLook.Mat(new Color(1f, .85f, .4f), false, new Color(1.6f, 1.2f, .5f));
            for (int i = 0; i < 7; i++) { float p = M("diveZone") - 12 + i * 9; var q = At(p); foreach (float s in new[] { -1f, 1f }) Box(scen, "guide light", q + Right(p) * s * 8 + Vector3.up * .5f, Vector3.one * .7f, glow, null, false); }
            // the Coral Vault's vertical slalom: mossy rock arches whose opening is low (a hanging curtain over a low line) or high (a floor ridge
            // under a high line); roots hang near the walls, glowing crystals and hot-spring vents on the floor
            foreach (var n in new[] { "hi1", "hi2" })
            {
                float p = M(n); var q = At(p); var fr = Facing(p);
                Box(root, "Arch ridge " + n, new Vector3(q.x, (CaveFloor + q.y - 3.5f) * .5f, q.z), new Vector3(CaveHalf * 2 + 2, q.y - 3.5f - CaveFloor, 8), archStone, fr);
                Box(root, "Arch ridge moss", new Vector3(q.x, q.y - 3.6f, q.z), new Vector3(CaveHalf * 2, .6f, 8.4f), moss, fr, false);
                Box(root, "Arch ridge glow", new Vector3(q.x, q.y - 3.2f, q.z) - Flat(Dir(p)) * 4.3f, new Vector3(CaveHalf * 2, .35f, .35f), edge, fr, false);
            }
            foreach (var n in new[] { "lo1", "lo2" })
            {
                float p = M(n); var q = At(p); var fr = Facing(p); float bottom = q.y + 3.5f;
                Box(root, "Arch curtain " + n, new Vector3(q.x, (CaveRoof + bottom) * .5f, q.z), new Vector3(CaveHalf * 2 + 2, CaveRoof - bottom, 8), archStone, fr);
                Box(root, "Arch curtain moss", new Vector3(q.x, bottom + .2f, q.z), new Vector3(CaveHalf * 2, .5f, 8.4f), moss, fr, false);
                Box(root, "Arch curtain glow", new Vector3(q.x, bottom - .1f, q.z) - Flat(Dir(p)) * 4.3f, new Vector3(CaveHalf * 2, .35f, .35f), edge, fr, false);
            }
            var bRoots = new MeshKit.Batch(); var bGlow = new MeshKit.Batch(); var rnd = new System.Random(7);
            for (float p = M("portal") + 10; p < M("G7"); p += 7)
            {
                var q = At(p); var rr = Right(p); float side = (int)(p / 7) % 2 == 0 ? -1 : 1;
                float len = 3 + (float)rnd.NextDouble() * 6; var top = new Vector3(q.x, CaveRoof, q.z) + rr * side * (CaveHalf - 2 - (float)rnd.NextDouble() * 3);
                bRoots.Add(MeshKit.LowCylinder, top + Vector3.down * len * .5f, Quaternion.Euler((float)rnd.NextDouble() * 14 - 7, 0, (float)rnd.NextDouble() * 14 - 7), new Vector3(.35f, len, .35f), JurassicLook.FernTrunk);
                if (rnd.NextDouble() < .55)
                {
                    var fl = new Vector3(q.x, CaveFloor, q.z) + rr * -side * (CaveHalf - 3 - (float)rnd.NextDouble() * 4);
                    for (int k = 0; k < 3; k++) bGlow.Add(MeshKit.LowCone, fl + new Vector3(k * .8f - .8f, 1 + k * .3f, (k % 2) * .6f), Quaternion.Euler(k * 12 - 12, 0, k * 9 - 9), new Vector3(.7f, 2 + k * .6f, .7f), Color.white);
                }
            }
            bRoots.Object(scen, "Cave roots", JurassicLook.VertexColored(true));
            bGlow.Object(scen, "Cave crystals", JurassicLook.Mat(JurassicLook.Crystal, true, JurassicLook.Crystal * .9f), false, false);
            // hot-spring vents: warm glowing rings on the floor with a column of bubbles
            foreach (var n in new[] { "underRoad", "lo1", "lo2" })
            {
                float p = M(n) + 14; var q = At(p); var fl = new Vector3(q.x, CaveFloor + .2f, q.z) + Right(p) * 7;
                Cyl(scen, "vent", fl, 1.4f, .5f, JurassicLook.Mat(JurassicLook.Amber, true, JurassicLook.Amber * .8f), false);
                var bub = new GameObject("vent bubbles"); bub.transform.SetParent(scen, false); bub.transform.position = fl; bub.AddComponent<Bubbles>().top = CaveRoof - .5f;
            }
        }

        // ---------------- the waterfall return: surfacing pool, water ramp through the curtain ----------------
        static void Return()
        {
            var rock = Mt(JurassicLook.Rock); var moss = Mt(JurassicLook.Moss);
            Vol<WaterVolume>("Surfacing pool", W(0, (Water + CaveFloor - 2) * .5f, -246), Quaternion.identity, W(44, Water - CaveFloor + 2, 92));
            Box(root, "Pool wall W", W(-24, -8.5f, -240), W(4, 20, 100), rock); Box(root, "Pool wall E", W(24, -8.5f, -240), W(4, 20, 100), rock);
            Box(root, "Pool floor", W(0, CaveFloor - 1, -240), W(48, 2, 100), Mt(JurassicLook.Sand));
            // the water ramp (a boat climbs it on its hull) and its support
            var a = W(0, -.6f, -222); var b = W(0, 5f, -182); var d = b - a; var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            var ramp = Box(root, "Water ramp", (a + b) * .5f - rot * Vector3.up * .5f, new Vector3(24, 1, d.magnitude + .4f), Mt(new Color(.4f, .66f, .7f)), rot);
            ramp.AddComponent<WetRamp>();
            Box(root, "Water ramp support", W(0, -7f, -186), W(18, 22, 8), rock);
            Box(root, "Spillway", W(0, -9, -196), W(44, 2, 12), rock);
            // cliffs either side, the overhang the waterfall pours from, the curtain across the ramp near its lip, mist at its foot
            foreach (float s in new[] { -1f, 1f })
            {
                Box(root, "Falls cliff", W(s * 31, 12, -205), W(14, 26, 40), rock);
                Box(root, "Falls cliff moss", W(s * 31, 25.5f, -205), W(15, 1.5f, 41), moss);
            }
            Box(root, "Falls overhang", W(0, 22, -192), W(48, 6, 12), rock);
            Box(root, "Falls overhang moss", W(0, 25.3f, -192), W(49, 1, 13), moss);
            var curtain = GameObject.CreatePrimitive(PrimitiveType.Quad); curtain.name = "Waterfall curtain"; curtain.transform.SetParent(root, false);
            UnityEngine.Object.Destroy(curtain.GetComponent<Collider>()); curtain.transform.position = W(0, 10.5f, -187); curtain.transform.localScale = new Vector3(40 * S, 23, 1);
            var cr = curtain.GetComponent<MeshRenderer>(); cr.sharedMaterial = JurassicLook.FallMat(.32f); cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // landing marker chevrons on the finishing road
            for (int i = 0; i < 5; i++) Box(root, "landing chevron", W(0, RoadY + .03f, -165 + i * 7), new Vector3(9, .05f, 1.2f), Mt(new Color(.95f, .5f, .1f)), Quaternion.Euler(0, i % 2 == 0 ? 20 : -20, 0), false);
        }

        // ---------------- the Sky Arch ----------------
        static void Arch()
        {
            float p = M("arch"); var q = At(p); var f = Facing(p); var r = Right(p);
            var rock = Mt(JurassicLook.Rock, true, default, 1.5f); var moss = Mt(JurassicLook.Moss, true, default, 1.5f);
            foreach (float s in new[] { -1f, 1f })
            {
                var basePt = new Vector3(q.x, 0, q.z) + r * s * 44;
                Box(root, "Arch pillar", basePt + Vector3.up * 40, new Vector3(22, 80, 24), rock, f);
                Box(root, "Arch pillar moss", basePt + Vector3.up * 30 + r * s * 2, new Vector3(20, 8, 26), moss, f, false);
            }
            Box(root, "Arch span", new Vector3(q.x, 82, q.z), new Vector3(110, 18, 26), rock, f);
            Box(root, "Arch span moss", new Vector3(q.x, 91.5f, q.z), new Vector3(112, 2, 28), moss, f, false);
            // hanging moss along the span's underside
            var b = new MeshKit.Batch();
            for (int i = 0; i < 14; i++) { var at = new Vector3(q.x, 72, q.z) + r * (-36 + i * 5.5f); b.Add(MeshKit.LowCone, at + Vector3.down * 2, Quaternion.Euler(180, 0, 0), new Vector3(2.2f, 4 + (i * 7 % 5), 2.2f), JurassicLook.Moss); }
            b.Object(scen, "Arch moss", JurassicLook.VertexColored(true));
        }

        // ---------------- scenery: cycads, tree ferns, mossy boulders, dinosaurs, pterodactyls ----------------
        static void Scenery()
        {
            var rnd = new System.Random(1717);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var batches = new Dictionary<Vector2Int, MeshKit.Batch>();
            MeshKit.Batch B(Vector3 p) { var k = new Vector2Int(Mathf.FloorToInt(p.x / 200f), Mathf.FloorToInt(p.z / 200f)); if (!batches.TryGetValue(k, out var b)) batches[k] = b = new MeshKit.Batch(); return b; }
            void Plant(Vector3 p, int kind, float s) { var b = B(p); float yaw = R(0, 360); if (kind == 0) Cycad(b, p, s, yaw); else if (kind == 1) Fern(b, p, s, yaw); else Boulder(b, p, s, yaw); }
            // along the promenade: both sides (the lake side on the beach strip's edge)
            for (float p = 10; p < M("climb0"); p += 18)
            {
                var q = At(p); var r = Right(p);
                Plant(new Vector3(q.x, 1.5f, q.z) + r * R(18, 34), (int)R(0, 3), R(1f, 1.5f));
                if (rnd.NextDouble() < .6) Plant(new Vector3(q.x, 1.5f, q.z) - r * R(15, 19), (int)R(0, 2), R(.8f, 1.2f));
            }
            // the finishing road
            for (float p = M("landing"); p < M("finish") - 10; p += 16) { var q = At(p); var r = Right(p); foreach (float s in new[] { -1f, 1f }) Plant(new Vector3(q.x, 1.5f, q.z) + r * s * R(16, 26), (int)R(0, 3), R(.9f, 1.4f)); }
            // on the climb's hillside
            for (float p = M("climb0") + 20; p < M("rampStart") - 25; p += 16) { var q = At(p); Plant(new Vector3(q.x, q.y - .9f + 9f, q.z) - Right(p) * R(16, 36), (int)R(0, 2), R(1.1f, 1.6f)); }
            // the valley (well clear of the flight line's low end) and the island over the cave
            for (int i = 0; i < 150; i++) { var p = W(R(80, 500), .4f, R(-80, 640)); Plant(p, i % 9 == 0 ? 2 : (int)R(0, 2), R(1.2f, 2.2f)); }
            for (int i = 0; i < 70; i++)
            {
                var p = W(R(-165, 65), 1.5f, R(-345, -65));
                if (Mathf.Abs(p.x) < 30 * S && p.z > -300 * S && p.z < -60 * S) continue;   // the pool, the ramp and the finishing road
                Plant(p, i % 6 == 0 ? 2 : (int)R(0, 2), R(1f, 1.8f));
            }
            // river and lagoon banks: ferns and mangrove roots
            for (float p = M("lagoonEnd"); p < M("diveZone"); p += 14)
            {
                var q = At(p); var r = Right(p);
                foreach (float s in new[] { -1f, 1f })
                {
                    var bp = new Vector3(q.x, 1.4f, q.z) + r * s * R(25, 40);
                    Plant(bp, (int)R(0, 2), R(1f, 1.5f));
                    var root0 = new Vector3(q.x, 0, q.z) + r * s * 21.5f; var b = B(root0);
                    for (int k = 0; k < 3; k++) b.Add(MeshKit.LowCylinder, root0 + new Vector3(0, 1.2f, 0) + Flat(Dir(p)) * (k * 2.5f - 2.5f), Quaternion.AngleAxis(s * 35, Flat(Dir(p))), new Vector3(.3f, 3.2f, .3f), JurassicLook.FernTrunk);
                }
            }
            for (int i = 0; i < 30; i++) { float a = R(0, Mathf.PI * 2); var p = W(400 + Mathf.Cos(a) * 118, 1.4f, -460 + Mathf.Sin(a) * 175); Plant(p, (int)R(0, 2), R(1.2f, 1.8f)); }
            foreach (var kv in batches) kv.Value.Object(scen, $"Plants {kv.Key}", JurassicLook.VertexColored(true));
            // dinosaurs in the valley under the flight (scenery only), pterodactyls circling high
            Dino("bronto", W(170, .4f, 330), 140, 18f);
            Dino("bronto", W(300, .4f, 120), -40, 15f);
            Dino("trike", W(285, .4f, 430), 200, 7f);
            Dino("trex", W(130, .4f, -20), 60, 11f);
            for (int i = 0; i < 3; i++)
            {
                var pg = Dino("ptero", W(260, 95 + i * 9, 300), 0, 3.5f);
                if (pg) { var cir = pg.AddComponent<Circle>(); cir.center = W(240 + i * 30, 95 + i * 9, 260 + i * 40); cir.radius = (70 + i * 25) * S; cir.speed = .18f + i * .04f; cir.phase = i * 2.1f; }
            }
        }
        // Tee'd Off's Jurassic tree kinds (TO JurassicBuilder.Tree, kinds 8-10), same shapes, ShiftStorm sizes
        static void Fern(MeshKit.Batch b, Vector3 p, float s, float yaw)
        {
            float tr = .35f * s, cy = 5f * s, rx = 3.2f * s; var rot = Quaternion.Euler(0, yaw, 0);
            for (int i = 0; i < 6; i++) b.Add(MeshKit.LowCylinder, p + Vector3.up * (cy * (i + .5f) / 6f), rot * Quaternion.Euler(0, i * 25f, 0), new Vector3(tr * (2.2f - i * .12f), cy / 6f, tr * (2.2f - i * .12f)), i % 2 == 0 ? JurassicLook.FernTrunk : JurassicLook.FernTrunk * .88f);
            for (int i = 0; i < 8; i++)
            {
                var q = rot * Quaternion.Euler(0, i * 45f, 0); var dir = q * Vector3.forward; var col = Color.Lerp(JurassicLook.Frond, JurassicLook.FrondLight, (i * .37f) % 1f);
                b.Add(MeshKit.LowSphere, p + Vector3.up * (cy + .2f * s) + dir * rx * .45f, q * Quaternion.Euler(-12f, 0, 0), new Vector3(.9f * s, .18f * s, rx), col);
                b.Add(MeshKit.LowSphere, p + Vector3.up * (cy - .5f * s) + dir * rx * .95f, q * Quaternion.Euler(28f, 0, 0), new Vector3(.7f * s, .15f * s, rx * .7f), col * .95f);
            }
            b.Add(MeshKit.LowSphere, p + Vector3.up * (cy + .25f * s), rot, new Vector3(1.1f, .6f, 1.1f) * s, JurassicLook.Frond * .8f);
        }
        static void Cycad(MeshKit.Batch b, Vector3 p, float s, float yaw)
        {
            float tr = .45f * s, th = 2.2f * s, cy = 2.6f * s, rx = 2.6f * s; var rot = Quaternion.Euler(0, yaw, 0);
            for (int i = 0; i < 5; i++) b.Add(MeshKit.LowSphere, p + Vector3.up * (.3f * s + i * th / 5f), rot * Quaternion.Euler(0, i * 40f, 0), new Vector3(tr * 2.3f, th / 4f, tr * 2.3f), i % 2 == 0 ? JurassicLook.CycadA : JurassicLook.CycadB);
            for (int i = 0; i < 9; i++)
            {
                var q = rot * Quaternion.Euler(0, i * 40f + (i % 2) * 12f, 0); var dir = q * Vector3.forward;
                b.Add(MeshKit.LowSphere, p + Vector3.up * (cy + .1f * s) + dir * rx * .5f, q * Quaternion.Euler(-30f + (i % 3) * 15f, 0, 0), new Vector3(.45f * s, .12f * s, rx * 1.05f), Color.Lerp(JurassicLook.Frond, JurassicLook.FrondLight, (i % 3) / 3f));
            }
        }
        static void Boulder(MeshKit.Batch b, Vector3 p, float s, float yaw)
        {
            float tr = 1.3f * s, th = 2f * s; var rot = Quaternion.Euler(0, yaw, 0);
            b.Add(MeshKit.LowSphere, p + Vector3.up * (th * .4f), rot, new Vector3(tr * 2.2f, th * 1.1f, tr * 1.9f), JurassicLook.Rock);
            b.Add(MeshKit.LowSphere, p + Vector3.up * (th * .3f) + rot * new Vector3(tr * .6f, 0, tr * .3f), rot, new Vector3(tr * 1.3f, th * .8f, tr * 1.2f), JurassicLook.RockDark);
            b.Add(MeshKit.LowSphere, p + Vector3.up * (th * .85f), rot, new Vector3(tr * 1.7f, th * .35f, tr * 1.5f), JurassicLook.Moss);
        }
        // a Tee'd Off dinosaur mesh (copied to Resources/Dinos), standing still, sized by its height; a primitive stand-in when it is missing
        static GameObject Dino(string kind, Vector3 at, float yaw, float height)
        {
            var mesh = Resources.Load<Mesh>($"Dinos/{kind}_mesh"); var tex = Resources.Load<Texture2D>($"Dinos/{kind}_basecolor");
            var g = new GameObject("Dino " + kind); g.transform.SetParent(scen, false);
            if (!mesh)
            {
                var b = new MeshKit.Batch(); b.Add(MeshKit.LowSphere, at + Vector3.up * height * .5f, Quaternion.Euler(0, yaw, 0), new Vector3(height * .5f, height * .5f, height), JurassicLook.Moss);
                b.Object(g.transform, "stand-in", JurassicLook.VertexColored(true)); return g;
            }
            var child = new GameObject("model"); child.transform.SetParent(g.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = child.AddComponent<MeshRenderer>(); mr.sharedMaterial = JurassicLook.Textured(tex, true);
            var bb = mesh.bounds; float k = height / Mathf.Max(bb.size.y, .01f);
            child.transform.localScale = Vector3.one * k; child.transform.localPosition = new Vector3(-bb.center.x * k, -bb.min.y * k, -bb.center.z * k);
            g.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            return g;
        }

        // ---------------- race logic: gates, zones, anchors, signs, segments ----------------
        static void Gates()
        {
            void G(int i, string label, float p, Vector2 half, params Mode[] modes)
            {
                var q = At(p); var d = Dir(p);
                var g = new GameObject("Gate " + label); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(q, Quaternion.LookRotation(d, Vector3.up));
                var pg = g.AddComponent<ProgressGate>(); pg.index = i; pg.label = label; pg.halfSize = half; pg.modes = modes; pg.progress = p; c.gates.Add(pg);
                // Round 2b: a big glowing frame (GateMarker: next gold with a light column, the one after cyan); G0 is the start line itself
                // (the frames of the cave gates and the river's exit sit inside the cave walls / the banks, so those are drawn smaller)
                var look = i == 6 ? new Vector2(CaveHalf - 3, 3.5f) : i == 7 ? new Vector2(CaveHalf - 3, 5) : i == 5 ? new Vector2(17, 8) : default;
                if (i > 0) GateMarker.Make(pg, c, !Array.Exists(modes, m => m == Mode.Dive) || Array.Exists(modes, m => m == Mode.Boat), look);
            }
            G(0, "G0 start", M("G0"), new Vector2(15, 8), Mode.Drive);
            G(1, "G1 promenade exit", M("G1"), new Vector2(15, 8), Mode.Drive);
            G(2, "G2 launch", M("lip") - 3, new Vector2(13, 10), Mode.Drive, Mode.Fly);
            G(3, "G3 arch exit", M("arch") + 55, new Vector2(42, 28), Mode.Fly);
            G(4, "G4 lagoon landing", M("splash") + 50, new Vector2(48, 12), Mode.Boat);
            G(5, "G5 mangrove exit", M("G5"), new Vector2(22, 8), Mode.Boat);
            G(6, "G6 dive portal", M("portal"), new Vector2(CaveHalf, 6), Mode.Dive);
            G(7, "G7 chamber exit", M("G7"), new Vector2(CaveHalf + 4, 10), Mode.Dive);
            G(8, "G8 surfacing pool", M("G8"), new Vector2(22, 10), Mode.Boat, Mode.Dive);
            G(9, "G9 road landing", M("G9"), new Vector2(15, 9), Mode.Drive);
            G(10, "G10 finish", M("finish"), new Vector2(15, 8), Mode.Drive);
        }
        // Round 2b item 1: the route is visible the whole way, not just at gates. Road: white edge lines and amber chevrons on the surface;
        // flight: a chain of glowing rings along the line; water: a buoy lane (visual only, nothing to hit at speed); the cave: guide lights
        // either side at the racer line's height, so its climbs and dips read ahead.
        static void RouteMarks()
        {
            var edge = Mt(new Color(.97f, .95f, .88f), false, new Color(.18f, .18f, .16f));
            float[] cuts = { 0, M("climb0"), M("rampStart") - 10, M("lip") };
            for (int k = 0; k < cuts.Length - 1; k++)
            {
                foreach (float s in new[] { -1f, 1f })
                    MeshKit.Strip(scen, "Edge line", SamplesW(cuts[k], cuts[k + 1], p => s < 0 ? -HalfWidth(p) + .5f : HalfWidth(p) - 1.2f, p => s < 0 ? -HalfWidth(p) + 1.2f : HalfWidth(p) - .5f, (p, pt) => pt.y - .9f + .03f, (p, pt) => pt.y - .9f - .02f, 3f), edge, null, false);
            }
            float fa = M("landing") - 12, fb = M("lineEnd") - 81f;
            foreach (float s in new[] { -1f, 1f }) MeshKit.Strip(scen, "Edge line", Samples(fa, fb, s < 0 ? -12.5f : 11.8f, s < 0 ? -11.8f : 12.5f, (p, pt) => RoadY + .03f, (p, pt) => RoadY - .02f, 3f), edge, null, false);
            // chevrons on the road, every 55 m, pointing the way
            var chev = new MeshKit.Batch(); var amber = new Color(1f, .62f, .15f);
            void Chevron(float p)
            {
                var q = At(p); q.y -= .9f - .04f; var f = Facing(p); var fw = Flat(Dir(p)); var r = Right(p);
                foreach (float s in new[] { -1f, 1f }) chev.Add(MeshKit.Cube, q + r * s * 1.5f - fw * 1.1f, f * Quaternion.Euler(0, -s * 38, 0), new Vector3(.9f, .05f, 4.6f), amber);
            }
            // Round 5 item 1: things at a steady rhythm beside and on the road, so movement reads against the scenery at 65+ m/s:
            // white marker posts with an orange band every 20 m on both edges, and a dashed centre line (5 m dashes every 14 m)
            var rhythm = new MeshKit.Batch(); var postWhite = new Color(.97f, .95f, .9f); var postBand = new Color(1f, .5f, .12f); var dash = new Color(.98f, .93f, .78f);
            void Post(Vector3 at) { rhythm.Add(MeshKit.Cube, at + Vector3.up * 1f, Quaternion.identity, new Vector3(.4f, 2f, .4f), postWhite); rhythm.Add(MeshKit.Cube, at + Vector3.up * 1.6f, Quaternion.identity, new Vector3(.46f, .45f, .46f), postBand); }
            void Rhythm(float a, float b, Func<float, float> half, Func<float, float> y)
            {
                for (float p = a; p < b; p += 20f) { var q = At(p); var r = Right(p); foreach (float sd in new[] { -1f, 1f }) Post(new Vector3(q.x, y(p), q.z) + r * sd * (half(p) + 1.1f)); }
                for (float p = a + 4; p < b - 4; p += 14f) { var q = At(p); rhythm.Add(MeshKit.Cube, new Vector3(q.x, y(p) + .035f, q.z), Facing(p, false), new Vector3(.35f, .05f, 5f), dash); }
            }
            {
                Rhythm(25, M("climb0") - 5, HalfWidth, p => At(p).y - .9f);
                Rhythm(M("climb0") + 5, M("rampStart") - 15, HalfWidth, p => At(p).y - .9f);
                Rhythm(M("G9") + 12, M("finish") - 12, p => 12.5f, p => RoadY);
                rhythm.Object(scen, "Road rhythm (posts, centre dashes)", JurassicLook.VertexColored(false, new Color(.15f, .12f, .08f)), false, false);
            }
            for (float p = 40; p < M("lip") - 25; p += 55) Chevron(p);
            for (float p = M("G9") + 10; p < M("finish") - 15; p += 55) Chevron(p);
            chev.Object(scen, "Road chevrons", JurassicLook.VertexColored(false, new Color(.25f, .15f, .02f)), false, false);
            // the flight: rings along the line (not at the arch itself)
            var rings = new MeshKit.Batch(); var gold = new Color(1f, .9f, .55f, .55f);
            for (float p = M("lip") + 70; p < M("splash") - 50; p += 75)
            {
                if (Mathf.Abs(p - M("arch")) < 45) continue;
                var q = At(p); var frame = Quaternion.LookRotation(Dir(p), Vector3.up); const float R = 11f;
                for (int i = 0; i < 14; i++) { float a = i * 360f / 14; rings.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0) * R), frame * Quaternion.Euler(0, 0, a), new Vector3(.7f, R * .47f, .7f), gold); }
            }
            rings.Object(scen, "Flight rings", GateMarker.Beacon, false, false);
            // the water: a buoy lane through the lagoon and the river
            var buoys = new MeshKit.Batch(); int n = 0;
            for (float p = M("splash") + 25; p < M("diveZone") - 15; p += 24, n++)
                foreach (float s in new[] { -1f, 1f }) { var q = At(p); q.y = Water + .45f; buoys.Add(MeshKit.LowCylinder, q + Right(p) * s * 15f, Quaternion.identity, new Vector3(1.1f, 1.5f, 1.1f), n % 2 == 0 ? new Color(1f, .45f, .1f) : Color.white); }
            buoys.Object(scen, "Buoy lane", JurassicLook.VertexColored(true, new Color(.12f, .08f, .04f)), false, false);
            // the cave: guide lights either side at the line's height
            var lights = new MeshKit.Batch(); n = 0;
            for (float p = M("portal") + 6; p < M("poolEdge"); p += 12, n++)
                foreach (float s in new[] { -1f, 1f }) lights.Add(MeshKit.Cube, At(p) + Right(p) * s * (CaveHalf - 2.5f), Facing(p), Vector3.one * .9f, n % 2 == 0 ? new Color(.35f, 1f, .9f) : new Color(1f, .78f, .35f));
            lights.Object(scen, "Cave guide lights", GateMarker.Beacon, false, false);
        }
        static void Zones()
        {
            void Z(string label, Vector3 center, Quaternion rot, Vector3 size, Mode target, bool corridor = false)
            { var z = Vol<TransformZone>(label, center, rot, size); z.target = target; z.landingCorridor = corridor; z.label = label; }
            float lip = M("lip"); var lp = At(lip); lp.y -= .9f;
            Z("Wings out (ramp lip)", lp + Flat(Dir(lip)) * 7 + Vector3.up * 6, Facing(lip), new Vector3(30, 16, 14), Mode.Fly);
            float s = M("splash"); var sp = At(s + 10); sp.y = 5;
            Z("Hull down (landing corridor)", sp, Facing(s + 10), new Vector3(42, 12, 100), Mode.Boat, true);
            float dz = M("diveZone"); var dp = At(dz); dp.y = -1;
            Z("Canopy seals (dive portal)", dp, Facing(dz), new Vector3(34, 8, 12), Mode.Dive);
            Z("Surface (pool)", W(0, -7, -270), Quaternion.identity, new Vector3(64, 14, 16), Mode.Boat);
            Z("Wheels down (water ramp lip)", W(0, 8, -176), Quaternion.identity, new Vector3(34, 12, 12), Mode.Drive);
        }
        static void Anchors()
        {
            void A(float p, Mode mode)
            {
                var q = At(p); var g = new GameObject($"Anchor {mode} {p:0}"); g.transform.SetParent(root, false);
                g.transform.SetPositionAndRotation(q, Facing(p)); var a = g.AddComponent<RecoveryAnchor>(); a.mode = mode; a.progress = p; c.anchors.Add(a);
            }
            A(M("start"), Mode.Drive); A(M("promMid"), Mode.Drive); A(M("climb0") - 15, Mode.Drive); A(M("climbMid"), Mode.Drive); A(M("rampStart") - 35, Mode.Drive);
            A(M("lip") + 90, Mode.Fly); A(M("arch") + 90, Mode.Fly);
            A(M("splash") + 70, Mode.Boat); A(M("bend2"), Mode.Boat); A(M("bend4") - 20, Mode.Boat); A(M("G5") + 12, Mode.Boat);
            A(M("portal") + 45, Mode.Dive); A(M("hi2") - 25, Mode.Dive); A(M("G7") - 30, Mode.Dive);
            A(M("G8") - 14, Mode.Boat); A(M("G9") + 20, Mode.Drive);
            c.Finish();
        }
        static void Signs()
        {
            void S(float p, float side, float y, Mode form, float post)
            {
                var q = At(p); var h = new GameObject("Sign " + form); h.transform.SetParent(scen, false);
                h.transform.SetPositionAndRotation(new Vector3(q.x, y, q.z) + Right(p) * side, Facing(p));
                WorldBuilder.Sign(h.transform, Vector3.zero, form, post);
            }
            S(M("rampStart") - 22, 14.6f, At(M("rampStart") - 22).y - .9f, Mode.Fly, 6);   // Round 4: beside the road (build 4: 10.35 m, its post on the road)
            S(M("splash") - 110, -30, 1.4f, Mode.Boat, 13);
            S(M("diveZone") - 110, -23, 1.4f, Mode.Dive, 6);
            S(M("rampFoot") - 14, -15, Water, Mode.Drive, 5);
        }
        // ---------------- Round 3 item 5: the two shortcuts ----------------
        // Round 4 item 5: the openings are longer (the way down starts 10 m after the curtain instead of 20, the way up ends 20 m before G7
        // instead of 26) and their edge is past the line (1 m right of it instead of 1 m left), so a submarine diving on the tunnel's line
        // has 7 m to the floor's edge instead of 5; the tunnel is wider (20 m, the exit 17 m; build 4: 18 / 13) and taller.
        static readonly Vector2 HoleA = new Vector2(10, 96), HoleB = new Vector2(-116, -20);
        // Round 3: built but switched off for build 4 (the sub caught the floor's edge, the chamber wall and the tunnel walls; a miss could
        // grind along it). Round 4: back on: walls glance a racer off (ShiftVehicle.Walls), so a touch costs a little and never traps.
        // "-noTunnel" (checks) builds the course without it.
        public static bool VaultTunnel => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-noTunnel") < 0;   // the cave floor openings, metres from lo1 / G7
        static readonly Color ShortcutColor = new Color(.75f, .4f, 1f, .9f);
        static void Shortcuts()
        {
            // 1 Sky Arch inside slit: a mossy rock buttress runs from the arch's inner pillar 240 m out over the valley, on the inside of the
            // flight's bend; a 22 m slit through it, 190 m beside the line, cuts the corner (about 57 m shorter) and an updraft funnelled
            // through the slit throws the plane up to 108 m/s. A scrape on its edges costs the speed (ShiftTuning.scrape*). It stands in
            // for G3 and rejoins before G4's lagoon approach.
            float ar = M("arch"); var q = At(ar); q.y = 0; var f = Facing(ar); var r = Right(ar);
            var rock = Mt(JurassicLook.Rock, true, default, 1.5f); var moss = Mt(JurassicLook.Moss, true, default, 1.5f);
            const float slit = -190, half = 11, lo = 38, hi = 66, top = 96;
            void Wall(float a, float b, float y0, float y1) { Box(root, "Slit buttress", q + r * ((a + b) * .5f) + Vector3.up * ((y0 + y1) * .5f), new Vector3(Mathf.Abs(b - a), y1 - y0, 24), rock, f); }
            Wall(-58, slit + half, 0, top); Wall(slit - half, -300, 0, top); Wall(slit + half, slit - half, hi, top); Wall(slit + half, slit - half, 0, lo);
            Box(root, "Slit buttress moss", q + r * -179 + Vector3.up * (top + 1), new Vector3(242, 2, 26), moss, f, false);
            var sz = Vol<SlipstreamZone>("Slit updraft", q + r * slit + Vector3.up * 52, f, new Vector3(half * 2 - 2, hi - lo - 2, 44)); sz.speed = 108;
            var sk = new List<Vector3> { At(ar - 490), At(ar - 450), q + r * slit + Vector3.up * 52, At(ar + 600), At(ar + 640) };
            MakeShortcut("Sky Arch slit", sk, ar - 490, ar + 640, 3, 3, 30f, new Vector2(half + 3, (hi - lo) * .5f + 3), new Vector2(32, 26), -2, 70f, Mode.Fly);

            // 2 Coral Vault low tunnel: down through an opening in the cave floor just after the first curtain (lo1), under the chamber's
            // wall, a straight run under the island inside the cave's loop, and up through a second opening before G7, the last 75 m
            // narrower (10 x 7 m instead of 14 x 9). About 2 s at diving speed; both paths pass G7.
            if (!VaultTunnel) return;
            float l1 = M("lo1"), g7 = M("G7"); Vector3 Left(float p, float d, float y) { var pt = At(p) - Right(p) * d; pt.y = y; return pt; }
            var A = At(l1 + 15); var B = At(g7 - 35); var mid = (A + B) * .5f; mid.y = -27;
            var tk = new List<Vector3> { At(l1 - 10), A, Left(l1 + 45, 6, -22), Left(l1 + 72, 15, -27.5f), mid, Left(g7 - 98, 15, -27.5f), Left(g7 - 66, 6, -22), B, At(g7 - 5) };
            var sc = MakeShortcut("Coral Vault tunnel", tk, l1 - 10, g7 - 5, 7, -1, 10f, new Vector2(10, 8), new Vector2(8, 6), -1, -1, Mode.Dive);
            Tunnel(sc);
            // a hot-spring current runs through the tunnel (Round 4: about +11 m/s for a submarine riding it; build 4: +5), the rest of its time saving
            for (float s = 50; s < sc.Length - 60; s += 20)
            {
                var p0 = sc.PointAt(s, out var d0); var cz = Vol<CurrentZone>("Tunnel current", p0, Quaternion.LookRotation(d0), new Vector3(12, 9, 20)); cz.push = d0 * 9f;
            }
        }
        // the shortcut, its line and its entry / exit sub-gates (violet frames). entryAt: metres along its line (-2: 40 m before the
        // point nearest its control point 3, the slit; -1: where the line first goes below y = -21, the exit where it last is)
        static Shortcut MakeShortcut(string label, List<Vector3> ctrl, float from, float to, int next, int replaces, float halfWidth, Vector2 entryHalf, Vector2 exitHalf, float entryAt, float exitBack, Mode mode)
        {
            var g = new GameObject("Shortcut " + label); g.transform.SetParent(root, false); var sc = g.AddComponent<Shortcut>();
            sc.label = label; sc.line = Shortcut.Smooth(ctrl); sc.mainFrom = from; sc.mainTo = to; sc.nextGate = next; sc.replacesGate = replaces; sc.halfWidth = halfWidth;
            float L = sc.Length, se = entryAt, sx = L - exitBack;
            if (entryAt == -2) { float sp = sc.Project(ctrl[2], -1, out _, out _); se = sp - 40; sx = sp + 40; }
            else if (entryAt < 0) { se = -1; sx = -1; for (float s = 0; s < L; s += 1) { float y = sc.PointAt(s, out _).y; if (se < 0 && y < -21) se = s; if (se > 0 && y < -21) sx = s; } }
            sc.entry = SubGate(sc, label + " entry", se, entryHalf, mode); sc.exit = SubGate(sc, label + " exit", sx, exitHalf, mode);
            c.shortcuts.Add(sc);
            Debug.Log($"[Course] shortcut {label}: line {L:0} m (main {to - from:0} m), entry at {se:0} m, exit at {sx:0} m along it");
            return sc;
        }
        static ProgressGate SubGate(Shortcut sc, string label, float s, Vector2 half, Mode mode)
        {
            var at = sc.PointAt(s, out var d);
            var g = new GameObject("Sub-gate " + label); g.transform.SetParent(sc.transform, false); g.transform.SetPositionAndRotation(at, Quaternion.LookRotation(d, Vector3.up));
            var pg = g.AddComponent<ProgressGate>(); pg.label = label; pg.halfSize = half; pg.modes = new[] { mode }; pg.index = -1;
            float hx = half.x, hy = half.y, t = Mathf.Clamp(Mathf.Max(hx, hy) * .05f, .5f, 1.6f);
            var b = new MeshKit.Batch();
            b.Add(MeshKit.Cube, new Vector3(-hx, 0, 0), Quaternion.identity, new Vector3(t, 2 * hy + t, t), ShortcutColor); b.Add(MeshKit.Cube, new Vector3(hx, 0, 0), Quaternion.identity, new Vector3(t, 2 * hy + t, t), ShortcutColor);
            b.Add(MeshKit.Cube, new Vector3(0, hy, 0), Quaternion.identity, new Vector3(2 * hx + t, t, t), ShortcutColor); b.Add(MeshKit.Cube, new Vector3(0, -hy, 0), Quaternion.identity, new Vector3(2 * hx + t, t, t), ShortcutColor);
            var fr = b.Object(g.transform, "shortcut frame", GateMarker.Beacon, false, false);
            var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Color", ShortcutColor); mpb.SetFloat("_Pulse", 0); fr.GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
            return pg;
        }
        // the tunnel's rock: floor, walls and roof along its line wherever it runs below the cave floor, its own water, guide lights
        static void Tunnel(Shortcut sc)
        {
            var rock = Mt(JurassicLook.CaveRock); var sand = Mt(JurassicLook.Sand); float L = sc.Length;
            // the exit narrows over 30 m (build 4 and the first Round 4 build: in one step, a wall face across the way)
            float HW(float s) => Mathf.Lerp(10f, 8.5f, Mathf.InverseLerp(L - 95, L - 65, s)); float HH(float s) => Mathf.Lerp(5.5f, 4.5f, Mathf.InverseLerp(L - 95, L - 65, s));
            // the climb-out turns right toward the main line while rising: its right side opens to 12 m (every AI run rode that wall at 8.5 m)
            float HWR(float s) => Mathf.Lerp(10f, 12f, Mathf.InverseLerp(L - 120, L - 90, s));
            MeshKit.Sample? Sec(float s, float l, float r, Func<Vector3, float> top, Func<Vector3, float> bot, Func<Vector3, bool> keep)
            {
                var p = sc.PointAt(s, out var d); var fl = new Vector3(d.x, 0, d.z); if (fl.sqrMagnitude < 1e-4f) fl = Vector3.forward;
                if (!keep(p)) return null;
                return new MeshKit.Sample { c = p, right = Vector3.Cross(Vector3.up, fl.normalized), l = l, r = r, top = top(p), bottom = bot(p) };
            }
            var fl0 = new List<MeshKit.Sample>(); var wl = new List<MeshKit.Sample>(); var wr = new List<MeshKit.Sample>(); var rf = new List<MeshKit.Sample>();
            void Flush()
            {
                if (fl0.Count > 1) MeshKit.Strip(root, "Tunnel floor", fl0, sand, rock); if (wl.Count > 1) MeshKit.Strip(root, "Tunnel wall L", wl, rock);
                if (wr.Count > 1) MeshKit.Strip(root, "Tunnel wall R", wr, rock); if (rf.Count > 1) MeshKit.Strip(root, "Tunnel roof", rf, rock);
                fl0 = new List<MeshKit.Sample>(); wl = new List<MeshKit.Sample>(); wr = new List<MeshKit.Sample>(); rf = new List<MeshKit.Sample>();
            }
            bool lastRoof = false;
            for (float s = 0; s <= L; s += 3)
            {
                float hw = HW(s), hh = HH(s);
                bool below(Vector3 p) => p.y - hh < CaveFloor - .5f;
                float hwr = HWR(s);
                var f0 = Sec(s, -hw - 1, hwr + 1, p => p.y - hh, p => p.y - hh - 2, below); if (f0.HasValue) fl0.Add(f0.Value);
                var w0 = Sec(s, -hw - 3, -hw, p => Mathf.Min(p.y + hh + 2, CaveFloor), p => p.y - hh - 2, below); if (w0.HasValue) wl.Add(w0.Value);
                var w1 = Sec(s, hwr, hwr + 3, p => Mathf.Min(p.y + hh + 2, CaveFloor), p => p.y - hh - 2, below); if (w1.HasValue) wr.Add(w1.Value);
                var r0 = Sec(s, -hw - 3, hwr + 3, p => p.y + hh + 2, p => p.y + hh, p => p.y + hh <= CaveFloor - 3.5f);   // starts well below the floor: no lip to catch
                if (r0.HasValue) rf.Add(r0.Value); else if (lastRoof && rf.Count > 1) { MeshKit.Strip(root, "Tunnel roof", rf, rock); rf = new List<MeshKit.Sample>(); }
                lastRoof = r0.HasValue;
                if (!f0.HasValue && fl0.Count > 0) Flush();
            }
            Flush();
            for (float s = 0; s < L; s += 20) { var p = sc.PointAt(s, out var d); p.y = (Water - 36) * .5f; var fd = new Vector3(d.x, 0, d.z); Vol<WaterVolume>("Tunnel water", p, Quaternion.LookRotation(fd.sqrMagnitude > 1e-4f ? fd : Vector3.forward), new Vector3(20, 36, 28)); }
            var lights = new MeshKit.Batch(); int n = 0;
            for (float s = 30; s < L - 20; s += 10, n++)
            {
                var p = sc.PointAt(s, out var d); var rr = Vector3.Cross(Vector3.up, new Vector3(d.x, 0, d.z).normalized);
                foreach (float k in new[] { -1f, 1f }) lights.Add(MeshKit.Cube, p + rr * k * (HW(s) - 1.2f), Quaternion.LookRotation(d), Vector3.one * .7f, n % 2 == 0 ? ShortcutColor : new Color(.35f, 1f, .9f));
            }
            lights.Object(scen, "Tunnel guide lights", GateMarker.Beacon, false, false);
        }

        static void Segments()
        {
            void Sg(string name, string form, float target, float end, int gates) => c.segments.Add(new Course.Segment { name = name, form = form, target = target, endProgress = end, gatesNeeded = gates });
            Sg("Beach Promenade", "Driving", 20, M("G1"), 2);
            Sg("Cliffside Climb", "Driving", 20, M("lip") - 3, 3);
            Sg("Sky Arch", "Flying", 25, M("splash"), 4);
            Sg("Lagoon Landing", "Boating", 15, M("lagoonEnd"), 5);
            Sg("Mangrove Run", "Boating", 20, M("G5"), 6);
            Sg("Sunken Gateway", "Diving", 10, M("vaultStart"), 7);
            Sg("Coral Vault", "Diving", 25, M("G7"), 8);
            Sg("Waterfall Return", "Dive-boat-drive", 15, M("finish"), 11);
        }
    }

    // looks only: a foam ribbon drifting with a current and wrapping back
    public sealed class Drift : MonoBehaviour
    {
        public Vector3 velocity; Vector3 start; float t;
        void Start() { start = transform.position; t = UnityEngine.Random.value * 4f; }
        void Update() { t = (t + Time.deltaTime) % 4f; transform.position = start + velocity * (t - 2f); }
    }
    // looks only: a pterodactyl circling
    public sealed class Circle : MonoBehaviour
    {
        public Vector3 center; public float radius = 60, speed = .2f, phase;
        void Update()
        {
            float a = phase + Time.time * speed; var p = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 2.3f) * .05f, Mathf.Sin(a)) * radius;
            var d = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)); transform.SetPositionAndRotation(p, Quaternion.LookRotation(d) * Quaternion.Euler(0, 0, -18));
        }
    }
    // looks only: a column of bubbles rising from a hot-spring vent
    public sealed class Bubbles : MonoBehaviour
    {
        public float top = -2; readonly List<Transform> b = new List<Transform>();
        void Start()
        {
            var mat = JurassicLook.Mat(new Color(.8f, .97f, 1f), false, new Color(.25f, .35f, .35f));
            for (int i = 0; i < 6; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(transform, false);
                g.transform.localScale = Vector3.one * (.3f + i % 3 * .15f); g.GetComponent<Renderer>().sharedMaterial = mat; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; b.Add(g.transform);
            }
        }
        void Update()
        {
            float h = top - transform.position.y;
            for (int i = 0; i < b.Count; i++) { float y = (Time.time * 2.2f + i * h / b.Count) % h; b[i].localPosition = new Vector3(Mathf.Sin(Time.time * 3 + i) * .4f, y, Mathf.Cos(Time.time * 2 + i) * .4f); }
        }
    }
}
