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
    public static class ParadisePlunge
    {
        static Transform root, scen; static Course c;
        static readonly Dictionary<string, float> mark = new Dictionary<string, float>();
        public static IReadOnlyDictionary<string, float> Marks => mark;

        // the racer line: (mark, x, y, z); y is the height a racer aims for (road + 0.9, water + 0.6, flight altitude, dive depth)
        static readonly (string n, float x, float y, float z)[] Points =
        {
            ("lineStart", 0, 2.9f, -60), ("start", 0, 2.9f, -20), ("G0", 0, 2.9f, 0),
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

        public static Course Build(Transform parent)
        {
            mark.Clear();
            var g = new GameObject("Course Paradise Plunge"); g.transform.SetParent(parent, false); root = g.transform;
            scen = new GameObject("Scenery").transform; scen.SetParent(root, false);
            c = g.AddComponent<Course>(); c.courseName = "Paradise Plunge"; c.timeTrial = true; c.look = "jurassic";
            c.halfWidth = 75; c.minY = -30; c.maxY = 105; c.endMargin = 40; c.startMode = Mode.Drive;
            MakeLine();
            Land(); Road(); Lagoon(); Mangrove(); Cave(); Return(); Arch(); Scenery();
            Gates(); Zones(); Anchors(); Signs(); Segments();
            return c;
        }

        // ---------------- the line ----------------
        static void MakeLine()
        {
            var pts = new List<Vector3>(); var at = new List<int>();
            int n = Points.Length;
            Vector3 P(int i) => new Vector3(Points[Mathf.Clamp(i, 0, n - 1)].x, Points[Mathf.Clamp(i, 0, n - 1)].y, Points[Mathf.Clamp(i, 0, n - 1)].z);
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
            sea.AddComponent<MeshFilter>().sharedMesh = Grid(3200, 40); sea.transform.position = new Vector3(200, Water - .02f, 50);
            var sr = sea.AddComponent<MeshRenderer>(); sr.sharedMaterial = JurassicLook.WaterMat(JurassicLook.Water, JurassicLook.WaterLight, JurassicLook.WaterRim, 0f); sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // the lake west of the promenade (a car in it is recovered)
            Vol<WaterVolume>("Lake", new Vector3(-340, -4, 420), Quaternion.identity, new Vector3(600, 8, 960));
            // west arm (the promenade's island), its lakeshore beach, the island over the cave (with the pool's opening)
            Box(root, "West arm", new Vector3(10, 1.25f, 290), new Vector3(100, .5f, 700), ground);
            Box(root, "Beach", new Vector3(-48, -.5f, 330), new Vector3(16, 2.4f, 780), sand);
            Box(root, "Island W", new Vector3(-106, 1.25f, -225), new Vector3(168, .5f, 330), ground);
            Box(root, "Island E", new Vector3(46, 1.25f, -225), new Vector3(48, .5f, 330), ground);
            Box(root, "Island S", new Vector3(0, 1.25f, -340), new Vector3(44, .5f, 100), ground);
            Box(root, "Island N", new Vector3(0, 1.25f, -127), new Vector3(44, .5f, 134), ground);
            Box(root, "Ridge", new Vector3(122, 1.25f, -120), new Vector3(100, .5f, 70), ground);
            Box(root, "Island beach S", new Vector3(-60, -.5f, -400), new Vector3(280, 2.4f, 20), sand);
            // the central valley (dinosaurs), the east arm's high cliffs, the far north hills
            Box(root, "Valley floor", new Vector3(290, -.3f, 300), new Vector3(460, 1.4f, 800), Mt(JurassicLook.JungleDark));
            for (int i = 0; i < 9; i++)
            {
                float z = -80 + i * 95, h = 38 + (i * 37 % 17);
                Box(root, "East cliff", new Vector3(560 + (i % 2) * 14, h * .5f - 1, z), new Vector3(70, h, 100), rock);
                Box(root, "East cliff moss", new Vector3(560 + (i % 2) * 14, h - .5f, z), new Vector3(72, 2, 102), Mt(JurassicLook.Moss));
            }
            for (int i = 0; i < 6; i++) { float x = -20 + i * 80, h = 50 + (i * 23 % 19); Box(root, "North hill", new Vector3(x, h * .5f, 790 + (i % 2) * 20), new Vector3(90, h, 90), rock); Box(root, "North hill moss", new Vector3(x, h, 790 + (i % 2) * 20), new Vector3(92, 3, 92), Mt(JurassicLook.Moss)); }
        }
        static Mesh Grid(float size, int cells)
        {
            var v = new List<Vector3>(); var t = new List<int>(); float h = size * .5f, s = size / cells;
            for (int z = 0; z <= cells; z++) for (int x = 0; x <= cells; x++) v.Add(new Vector3(-h + x * s, 0, -h + z * s));
            for (int z = 0; z < cells; z++) for (int x = 0; x < cells; x++) { int i = z * (cells + 1) + x; t.Add(i); t.Add(i + cells + 1); t.Add(i + 1); t.Add(i + 1); t.Add(i + cells + 1); t.Add(i + cells + 2); }
            var m = new Mesh { name = "grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // ---------------- road: promenade, climb, ramp; and the finishing road ----------------
        static float HalfWidth(float p) => p < M("climb0") ? 11f : p < M("rampStart") - 10 ? 10f : 9f;
        static void Road()
        {
            var road = Mt(JurassicLook.Road); var rock = Mt(JurassicLook.Rock); var barrier = Mt(JurassicLook.Stone, true, default, .6f);
            float a = 0, b = M("lip"), climb = M("climb0");
            // the road in pieces so the width can change; its skirt reaches the land (promenade) or the valley floor (climb: a mossy cliff)
            float[] cuts = { a, climb, M("rampStart") - 10, b };
            for (int k = 0; k < cuts.Length - 1; k++)
            {
                float hw = HalfWidth(cuts[k] + 1), lo = k == 0 ? 1f : -1f;
                MeshKit.Strip(root, "Road", Samples(cuts[k], cuts[k + 1], -hw, hw, (p, pt) => pt.y - .9f, (p, pt) => lo, 3f), road, rock);
                // low barriers at both edges (the valley side a little higher on the climb)
                MeshKit.Strip(root, "Barrier L", Samples(cuts[k], cuts[k + 1], -hw - .7f, -hw, (p, pt) => pt.y - .9f + (k == 0 ? .8f : 1.1f), (p, pt) => pt.y - 1.6f, 3f), barrier, null, true, ShiftVehicle.ObstacleLayer);
                MeshKit.Strip(root, "Barrier R", Samples(cuts[k], cuts[k + 1], hw, hw + .7f, (p, pt) => pt.y - .9f + (k == 0 ? .8f : 1.3f), (p, pt) => pt.y - 1.6f, 3f), barrier, null, true, ShiftVehicle.ObstacleLayer);
            }
            // the hillside above the climb (left: the outside of both corners), mossy on top
            MeshKit.Strip(root, "Climb cliff", Samples(climb + 10, M("rampStart") - 20, -40f, -11.5f, (p, pt) => pt.y + 9f, (p, pt) => -1f, 4f), Mt(JurassicLook.Moss), rock);
            // ramp: wind streamers either side
            for (int i = 0; i < 5; i++)
            {
                float p = M("rampStart") - 30 + i * 12; var pt = At(p); var r = Right(p);
                foreach (float s in new[] { -1f, 1f }) Box(scen, "streamer", pt + r * s * 11 + Vector3.up * (5 + i * .8f), new Vector3(.2f, .2f, 7), Mt(new Color(.35f, .7f, 1f)), Facing(p), false);
            }
            // the finishing road: from the landing to where the promenade's start piece begins (no overlap)
            float fa = M("landing") - 12, fb = M("lineEnd") - 81f;
            MeshKit.Strip(root, "Finishing road", Samples(fa, fb, -12, 12, (p, pt) => RoadY, (p, pt) => 1f, 3f), road, rock);
            MeshKit.Strip(root, "Barrier L", Samples(fa + 8, fb, -12.7f, -12f, (p, pt) => RoadY + .8f, (p, pt) => RoadY - .7f, 3f), barrier, null, true, ShiftVehicle.ObstacleLayer);
            MeshKit.Strip(root, "Barrier R", Samples(fa + 8, fb, 12f, 12.7f, (p, pt) => RoadY + .8f, (p, pt) => RoadY - .7f, 3f), barrier, null, true, ShiftVehicle.ObstacleLayer);
            // start / finish banner and grid lines
            var fin = new Vector3(0, RoadY, 0);
            Box(root, "Finish line", fin + Vector3.up * .03f, new Vector3(22, .06f, 2.2f), Mt(new Color(.1f, .1f, .1f)), null, false);
            for (int i = 0; i < 11; i++) Box(root, "Finish check", fin + new Vector3(-10 + i * 2, .07f, (i % 2) * 1f - .5f), new Vector3(1, .02f, 1), Mt(Color.white), null, false);
            foreach (float x in new[] { -13.5f, 13.5f }) Box(root, "Banner post", fin + new Vector3(x, 4.5f, 0), new Vector3(.8f, 9, .8f), Mt(JurassicLook.FernTrunk, true));
            Box(root, "Banner", fin + new Vector3(0, 8.6f, 0), new Vector3(27.8f, 2.2f, .5f), Mt(JurassicLook.Amber, true));
        }

        // ---------------- lagoon ----------------
        static void Lagoon()
        {
            var bank = Mt(JurassicLook.Jungle); var mud = Mt(JurassicLook.Mud);
            Vol<WaterVolume>("Lagoon", new Vector3(400, -8, -460), Quaternion.identity, new Vector3(200, 16, 320));
            Box(root, "Lagoon bank N", new Vector3(415, -7, -290), new Vector3(230, 17, 20), bank);
            Box(root, "Lagoon bank E", new Vector3(510, -7, -470), new Vector3(20, 17, 380), bank);
            Box(root, "Lagoon bank S", new Vector3(400, -7, -630), new Vector3(240, 17, 20), bank);
            Box(root, "Lagoon bank W", new Vector3(290, -7, -415), new Vector3(20, 17, 250), bank);
            Box(root, "Lagoon bed", new Vector3(400, -16.5f, -460), new Vector3(200, 1, 320), mud);
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
            MeshKit.Strip(root, "Cave floor", Samples(M("diveZone") - 10, b + 10, -CaveHalf - 1, CaveHalf + 1, (p, pt) => CaveFloor, (p, pt) => CaveFloor - 2, 4f), Mt(JurassicLook.Sand), rock);
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
            Vol<WaterVolume>("Surfacing pool", new Vector3(0, (Water + CaveFloor - 2) * .5f, -246), Quaternion.identity, new Vector3(44, Water - CaveFloor + 2, 92));
            Box(root, "Pool wall W", new Vector3(-24, -8.5f, -240), new Vector3(4, 20, 100), rock); Box(root, "Pool wall E", new Vector3(24, -8.5f, -240), new Vector3(4, 20, 100), rock);
            Box(root, "Pool floor", new Vector3(0, CaveFloor - 1, -240), new Vector3(48, 2, 100), Mt(JurassicLook.Sand));
            // the water ramp (a boat climbs it on its hull) and its support
            var a = new Vector3(0, -.6f, -222); var b = new Vector3(0, 5f, -182); var d = b - a; var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            var ramp = Box(root, "Water ramp", (a + b) * .5f - rot * Vector3.up * .5f, new Vector3(20, 1, d.magnitude + .4f), Mt(new Color(.4f, .66f, .7f)), rot);
            ramp.AddComponent<WetRamp>();
            Box(root, "Water ramp support", new Vector3(0, -7f, -186), new Vector3(18, 22, 8), rock);
            Box(root, "Spillway", new Vector3(0, -9, -196), new Vector3(44, 2, 12), rock);
            // cliffs either side, the overhang the waterfall pours from, the curtain across the ramp near its lip, mist at its foot
            foreach (float s in new[] { -1f, 1f })
            {
                Box(root, "Falls cliff", new Vector3(s * 31, 12, -205), new Vector3(14, 26, 40), rock);
                Box(root, "Falls cliff moss", new Vector3(s * 31, 25.5f, -205), new Vector3(15, 1.5f, 41), moss);
            }
            Box(root, "Falls overhang", new Vector3(0, 22, -192), new Vector3(48, 6, 12), rock);
            Box(root, "Falls overhang moss", new Vector3(0, 25.3f, -192), new Vector3(49, 1, 13), moss);
            var curtain = GameObject.CreatePrimitive(PrimitiveType.Quad); curtain.name = "Waterfall curtain"; curtain.transform.SetParent(root, false);
            UnityEngine.Object.Destroy(curtain.GetComponent<Collider>()); curtain.transform.position = new Vector3(0, 10.5f, -187); curtain.transform.localScale = new Vector3(40, 23, 1);
            var cr = curtain.GetComponent<MeshRenderer>(); cr.sharedMaterial = JurassicLook.FallMat(.32f); cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // landing marker chevrons on the finishing road
            for (int i = 0; i < 5; i++) Box(root, "landing chevron", new Vector3(0, RoadY + .03f, -165 + i * 7), new Vector3(9, .05f, 1.2f), Mt(new Color(.95f, .5f, .1f)), Quaternion.Euler(0, i % 2 == 0 ? 20 : -20, 0), false);
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
            for (int i = 0; i < 110; i++) { var p = new Vector3(R(80, 500), .4f, R(-80, 640)); Plant(p, i % 9 == 0 ? 2 : (int)R(0, 2), R(1.2f, 2.2f)); }
            for (int i = 0; i < 70; i++)
            {
                var p = new Vector3(R(-165, 65), 1.5f, R(-345, -65));
                if (Mathf.Abs(p.x) < 30 && p.z > -300 && p.z < -60) continue;   // the pool, the ramp and the finishing road
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
            for (int i = 0; i < 30; i++) { float a = R(0, Mathf.PI * 2); var p = new Vector3(400 + Mathf.Cos(a) * 118, 1.4f, -460 + Mathf.Sin(a) * 175); Plant(p, (int)R(0, 2), R(1.2f, 1.8f)); }
            foreach (var kv in batches) kv.Value.Object(scen, $"Plants {kv.Key}", JurassicLook.VertexColored(true));
            // dinosaurs in the valley under the flight (scenery only), pterodactyls circling high
            Dino("bronto", new Vector3(170, .4f, 330), 140, 15f);
            Dino("bronto", new Vector3(300, .4f, 120), -40, 12f);
            Dino("trike", new Vector3(285, .4f, 430), 200, 5.5f);
            Dino("trex", new Vector3(130, .4f, -20), 60, 9f);
            for (int i = 0; i < 3; i++)
            {
                var pg = Dino("ptero", new Vector3(260, 95 + i * 9, 300), 0, 3f);
                if (pg) { var cir = pg.AddComponent<Circle>(); cir.center = new Vector3(240 + i * 30, 95 + i * 9, 260 + i * 40); cir.radius = 70 + i * 25; cir.speed = .18f + i * .04f; cir.phase = i * 2.1f; }
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
            var post = Mt(new Color(.98f, .5f, .12f), true, default, .6f);
            void G(int i, string label, float p, Vector2 half, params Mode[] modes)
            {
                var q = At(p); var d = Dir(p);
                var g = new GameObject("Gate " + label); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(q, Quaternion.LookRotation(d, Vector3.up));
                var pg = g.AddComponent<ProgressGate>(); pg.index = i; pg.label = label; pg.halfSize = half; pg.modes = modes; pg.progress = p; c.gates.Add(pg);
                if (i == 0 || i == 10) return;   // the start / finish banner marks those
                foreach (float s in new[] { -1f, 1f })
                {
                    var bottom = q + g.transform.right * s * (half.x - 1) + Vector3.down * half.y;
                    float h = Mathf.Min(half.y * 2, 16); var b = Box(g.transform, "post", Vector3.zero, new Vector3(.5f, h, .5f), post, null, false);
                    b.transform.position = q + g.transform.right * s * (half.x - 1) + Vector3.up * (h * .5f - Mathf.Min(half.y, 4f)); b.transform.rotation = g.transform.rotation;
                }
            }
            G(0, "G0 start", M("G0"), new Vector2(13, 7), Mode.Drive);
            G(1, "G1 promenade exit", M("G1"), new Vector2(13, 7), Mode.Drive);
            G(2, "G2 launch", M("lip") - 3, new Vector2(12, 9), Mode.Drive, Mode.Fly);
            G(3, "G3 arch exit", M("arch") + 55, new Vector2(42, 28), Mode.Fly);
            G(4, "G4 lagoon landing", M("splash") + 50, new Vector2(48, 12), Mode.Boat);
            G(5, "G5 mangrove exit", M("G5"), new Vector2(22, 8), Mode.Boat);
            G(6, "G6 dive portal", M("portal"), new Vector2(CaveHalf, 6), Mode.Dive);
            G(7, "G7 chamber exit", M("G7"), new Vector2(CaveHalf + 4, 10), Mode.Dive);
            G(8, "G8 surfacing pool", M("G8"), new Vector2(22, 10), Mode.Boat, Mode.Dive);
            G(9, "G9 road landing", M("G9"), new Vector2(13, 8), Mode.Drive);
            G(10, "G10 finish", M("finish"), new Vector2(13, 7), Mode.Drive);
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
            Z("Surface (pool)", new Vector3(0, -7, -270), Quaternion.identity, new Vector3(40, 14, 12), Mode.Boat);
            Z("Wheels down (water ramp lip)", new Vector3(0, 8, -176), Quaternion.identity, new Vector3(30, 10, 10), Mode.Drive);
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
            S(M("rampStart") - 22, 10.35f, At(M("rampStart") - 22).y - .9f + 1.1f, Mode.Fly, 5);
            S(M("splash") - 110, -30, 1.4f, Mode.Boat, 13);
            S(M("diveZone") - 110, -23, 1.4f, Mode.Dive, 6);
            S(M("rampFoot") - 14, -15, Water, Mode.Drive, 5);
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
