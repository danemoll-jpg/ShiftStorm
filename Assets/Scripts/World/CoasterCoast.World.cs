using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 10 item 4: Coaster Coast's world (the race logic is in CoasterCoast.cs). Everything a racer can drive on is flush: the boardwalk
    // is a marked strip on the pier's plazas, the plazas run out to the pier's edge, and an edge drops into the sea (the usual car-in-water
    // reset); there are no walls along the boardwalk to land behind (item 1's rule). The coaster track, the supports, the stalls and the rides
    // are looks only (no colliders), except the solid things a boat or sub must steer round: the flume's banks, the fishing pier's legs, the
    // splash pool's walls, the lighthouse's islet and its rocks.
    public static partial class CoasterCoast
    {
        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, Quaternion? rot = null, bool collider = true, int layer = 0)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
            g.transform.SetPositionAndRotation(center, rot ?? Quaternion.identity); g.transform.localScale = size; g.layer = layer;
            g.GetComponent<Renderer>().sharedMaterial = mat; if (!collider) UnityEngine.Object.Destroy(g.GetComponent<Collider>());
            return g;
        }
        static GameObject Cyl(Transform parent, string name, Vector3 at, float radius, float height, Material mat, bool collider, int layer = 0)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name; g.transform.SetParent(parent, false); g.transform.position = at;
            g.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2); g.layer = layer; g.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.Destroy(g.GetComponent<Collider>()); if (collider) { var cc = g.AddComponent<CapsuleCollider>(); cc.radius = .5f; cc.height = 2; }
            return g;
        }
        static List<MeshKit.Sample> Samples(float a, float b, Func<float, float> l, Func<float, float> r, Func<float, Vector3, float> top, Func<float, Vector3, float> bottom, float step = 4f)
        {
            var list = new List<MeshKit.Sample>(); int n = Mathf.Max(1, Mathf.CeilToInt((b - a) / step));
            for (int i = 0; i <= n; i++)
            {
                float p = Mathf.Lerp(a, b, i / (float)n); var pt = At(p);
                list.Add(new MeshKit.Sample { c = pt, right = Right(p), l = l(p), r = r(p), top = top(p, pt), bottom = bottom(p, pt) });
            }
            return list;
        }
        static Mesh Grid(float size, int cells)
        {
            var v = new List<Vector3>(); var t = new List<int>(); float h = size * .5f, s = size / cells;
            for (int z = 0; z <= cells; z++) for (int x = 0; x <= cells; x++) v.Add(new Vector3(-h + x * s, 0, -h + z * s));
            for (int z = 0; z < cells; z++) for (int x = 0; x < cells; x++) { int i = z * (cells + 1) + x; t.Add(i); t.Add(i + cells + 1); t.Add(i + 1); t.Add(i + 1); t.Add(i + cells + 1); t.Add(i + cells + 2); }
            var m = new Mesh { name = "sea grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static float RoadTop(float p) => DeckY + Lift(p);
        static bool Detail(GameObject g) { if (g) g.layer = ParadisePlunge.DetailLayer; return g; }

        // ---------------- the sea ----------------
        static void Sea()
        {
            // one water body under everything (a car off the pier's edge is in the sea and is recovered), the sea floor, the visible surface
            Vol<WaterVolume>("Sea", L(200, (Water + SeaFloor - 2) * .5f, 200), Quaternion.identity, new Vector3(2400, Water - SeaFloor + 2, 2600));
            Box(root, "Sea floor", L(200, SeaFloor - 1, 200), new Vector3(2400, 2, 2600), CoasterLook.Mat(CoasterLook.Sand * .8f));
            var sea = new GameObject("Sea (visual)"); sea.transform.SetParent(root, false); sea.AddComponent<MeshFilter>().sharedMesh = Grid(4200, 56); sea.transform.position = L(200, Water - .02f, 200);
            var sr = sea.AddComponent<MeshRenderer>(); sr.sharedMaterial = JurassicLook.WaterMat(CoasterLook.SeaDeep, CoasterLook.SeaLight, CoasterLook.SeaCrest, .16f); sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // the coast to the east and the north: a beach, a sea wall and the town's lights on the hills (far scenery for the flight)
            var town = new MeshKit.Batch(); var rnd = new System.Random(5);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            Box(root, "Beach", L(1250, -1, 200), new Vector3(260, 6, 2400), CoasterLook.Mat(CoasterLook.Sand), null, false);
            Box(root, "Hills", L(1480, 30, 200), new Vector3(260, 70, 2400), CoasterLook.Mat(new Color(.36f, .4f, .42f)), null, false);
            var cols = new[] { CoasterLook.Candy, CoasterLook.Mint, CoasterLook.Lemon, CoasterLook.Sky, CoasterLook.Cream, new Color(.95f, .6f, .35f) };
            for (int i = 0; i < 120; i++) { float z = R(-900, 1300), x = R(1330, 1450), h = R(8, 26); town.Add(MeshKit.Cube, L(x, 2 + h * .5f, z), Quaternion.Euler(0, R(-10, 10), 0), new Vector3(R(10, 22), h, R(10, 18)), cols[i % cols.Length]); }
            town.Object(scen, "Seafront town", CoasterLook.Vc(true), false, false);
            var lights = new MeshKit.Batch();
            for (int i = 0; i < 260; i++) lights.Add(MeshKit.Cube, L(R(1330, 1460), R(6, 30), R(-900, 1300)), Quaternion.identity, Vector3.one * 1.4f, i % 3 == 0 ? CoasterLook.Lemon : i % 3 == 1 ? CoasterLook.Cream : CoasterLook.Neon);
            lights.Object(scen, "Town lights", GateMarker.Beacon, false, false);
        }

        // ---------------- the boardwalk (the road strip on the pier) ----------------
        static void Boardwalk()
        {
            var plank = CoasterLook.Mat(CoasterLook.Plank); var side = CoasterLook.Mat(CoasterLook.Pier);
            float gapA = M("jM"), gapB = M("jM") + 45, station = M("railA");
            foreach (var (a, b) in new[] { (0f, gapA), (gapB, station) })
                MeshKit.Strip(root, "Boardwalk", Samples(a, b, p => -RoadHalf, p => RoadHalf, (p, pt) => RoadTop(p), (p, pt) => DeckY - 1.6f, 3f), plank, side);
            // the finishing boardwalk: from the splash pool ramp's landing to where the start's piece begins (the line's first point)
            float fa = M("landing") - 12, fb = M("lineEnd") - 80f;
            MeshKit.Strip(root, "Boardwalk (finish)", Samples(fa, fb, p => -RoadHalf, p => RoadHalf, (p, pt) => DeckY, (p, pt) => DeckY - 1.6f, 3f), plank, side);
            // the Midway Jump's kicker edge, chevrons up it, the landing's chequered apron
            var amber = new Color(1f, .62f, .12f); var lq = At(gapA);
            Box(scen, "Lip edge", new Vector3(lq.x, RoadTop(gapA) + .04f, lq.z) - Flat(Dir(gapA)) * .6f, new Vector3(26, .08f, 1.2f), CoasterLook.Glow(amber), Facing(gapA), false);
            var chev = new MeshKit.Batch();
            for (float p = gapA - KickerLen + 6; p < gapA - 4; p += 9) { var q = At(p); q.y = RoadTop(p) + .05f; foreach (float s in new[] { -1f, 1f }) chev.Add(MeshKit.Cube, q + Right(p) * s * 2f, Facing(p) * Quaternion.Euler(-Mathf.Atan(2 * KickerH * Mathf.InverseLerp(gapA - KickerLen, gapA, p) / KickerLen) * Mathf.Rad2Deg, -s * 38, 0), new Vector3(1f, .06f, 5f), amber); }
            var fq = At(gapB); for (int i = 0; i < 9; i++) chev.Add(MeshKit.Cube, new Vector3(fq.x, DeckY + .05f, fq.z) + Right(gapB) * (-12 + i * 3) + Flat(Dir(gapB)) * 1.2f, Facing(gapB), new Vector3(1.5f, .06f, 1.5f), i % 2 == 0 ? Color.white : new Color(.1f, .1f, .1f));
            chev.Object(scen, "Kicker chevrons", CoasterLook.Vc(false), false, false);
            // start / finish: the line, the chequers, the arch over the boardwalk ("COASTER COAST" in lights)
            var fin = At(M("finish")); fin.y = DeckY;
            Box(root, "Finish line", fin + Vector3.up * .04f, new Vector3(26, .06f, 2.2f), CoasterLook.Mat(new Color(.1f, .1f, .1f)), null, false);
            for (int i = 0; i < 13; i++) Box(root, "Finish check", fin + new Vector3(-12 + i * 2, .08f, (i % 2) * 1f - .5f), new Vector3(1, .02f, 1), CoasterLook.Mat(Color.white), null, false);
            foreach (float x in new[] { -16f, 16f }) { Box(scen, "Arch post", fin + new Vector3(x, 7, 2), new Vector3(1.2f, 14, 1.2f), CoasterLook.Mat(CoasterLook.Candy, true), null, false); Box(scen, "Arch post stripe", fin + new Vector3(x, 10, 2), new Vector3(1.3f, 2, 1.3f), CoasterLook.Mat(CoasterLook.Cream, true), null, false); }
            Box(scen, "Arch board", fin + new Vector3(0, 13.4f, 2), new Vector3(34, 3, .6f), CoasterLook.Mat(CoasterLook.CoasterRed, true), null, false);
            var bulbs = new MeshKit.Batch(); for (int i = 0; i < 30; i++) bulbs.Add(MeshKit.Cube, fin + new Vector3(-15.5f + i * 1.07f, 13.4f + (i % 2 == 0 ? 1.35f : -1.35f), 1.6f), Quaternion.identity, Vector3.one * .45f, i % 2 == 0 ? CoasterLook.Lemon : CoasterLook.Cream);
            bulbs.Object(scen, "Arch bulbs", GateMarker.Beacon, false, false);
            Label("COASTER COAST", fin + new Vector3(0, 13.4f, 1.6f), Quaternion.LookRotation(Vector3.back), 2.1f, CoasterLook.Lemon);
            Label("COASTER COAST", fin + new Vector3(0, 13.4f, 2.4f), Quaternion.LookRotation(Vector3.forward), 2.1f, CoasterLook.Lemon);
            // Docs/STORY.md: the tour's public opening night: "GRAND OPENING" banners over the boardwalk and at the coaster station
            foreach (var (p, h) in new[] { (M("promMid"), 11f), (M("G9") + 60, 11f), (M("wheel") + 50, 12f) })
            {
                var q = At(p); q.y = DeckY; var f = Facing(p); var r = Right(p);
                foreach (float s in new[] { -1f, 1f }) Box(scen, "Banner pole", q + r * s * 15 + Vector3.up * h * .5f, new Vector3(.5f, h, .5f), CoasterLook.Mat(CoasterLook.Steel, true), null, false);
                Box(scen, "Grand Opening banner", q + Vector3.up * (h - 1.4f), new Vector3(28, 2.6f, .2f), CoasterLook.Mat(CoasterLook.CoasterRed, true), f, false);
                Label("GRAND OPENING", q + Vector3.up * (h - 1.4f) - Flat(Dir(p)) * .15f, Quaternion.LookRotation(-Flat(Dir(p))), 1.9f, CoasterLook.Lemon);
            }
        }
        // lettering in carnival marquee bulbs (a 5 x 7 bulb font, glowing; build 12's first try was a TextMesh, whose built-in shader draws
        // through everything and read mirrored). rot's forward is the side it is read from.
        static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" }, ['C'] = new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." }, ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['G'] = new[] { ".####", "#....", "#....", "#.###", "#...#", "#...#", ".###." }, ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#" }, ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." }, ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." }, ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
        };
        static void Label(string text, Vector3 at, Quaternion rot, float height, Color col)
        {
            var b = new MeshKit.Batch(); float step = height / 7f; var face = rot * Vector3.forward; var right = Vector3.Cross(face, Vector3.up).normalized; var up = Vector3.up;
            float width = (text.Length * 6 - 1) * step; float x0 = -width * .5f;
            for (int i = 0; i < text.Length; i++)
            {
                if (!Font.TryGetValue(text[i], out var g)) continue;
                for (int row = 0; row < 7; row++) for (int k = 0; k < 5; k++)
                    if (g[row][k] == '#') b.Add(MeshKit.Cube, at + right * (x0 + (i * 6 + k + .5f) * step) + up * ((3 - row) * step) + face * .25f, rot, Vector3.one * step * .72f, col);
            }
            b.Object(scen, "Marquee " + text, GateMarker.Beacon, false, false);
        }

        // ---------------- the plazas: the pier's deck either side of the boardwalk, flush with it ----------------
        static void Plazas()
        {
            var deck = CoasterLook.Mat(CoasterLook.PlankDark);
            void Deck(string name, float x0, float x1, float z0, float z1) => Box(root, name, L((x0 + x1) * .5f, DeckY - .8f - .005f, (z0 + z1) * .5f), new Vector3(x1 - x0, 1.6f, z1 - z0), deck);
            Deck("South plaza", -150, 100, -362, 233);
            Deck("North plaza", -150, 100, 271, 840);
            Deck("Coaster station deck", -20, 20, 840, 902);
            // the pier's legs under the plazas' edges (looks only: no boat goes there)
            var legs = new MeshKit.Batch(); var wood = CoasterLook.Pier;
            for (float z = -350; z < 840; z += 24) foreach (float x in new[] { -149f, 99f }) if (z < 228 || z > 276) legs.Add(MeshKit.LowCylinder, L(x, (DeckY - 1.6f + Water - 2) * .5f, z), Quaternion.identity, new Vector3(1.4f, (DeckY - 1.6f - Water + 2) * .5f, 1.4f), wood);
            for (float x = -150; x < 100; x += 25) foreach (float z in new[] { -361f, 839f, 232f, 272f }) legs.Add(MeshKit.LowCylinder, L(x, (DeckY - 1.6f + Water - 2) * .5f, z), Quaternion.identity, new Vector3(1.4f, (DeckY - 1.6f - Water + 2) * .5f, 1.4f), wood);
            legs.Object(scen, "Pier legs", CoasterLook.Vc(true), false, false);
            // a low rail along the plazas' sea edges (looks only: a car can drive off the edge into the sea, never stand behind it)
            var rail = new MeshKit.Batch();
            void Rail(Vector3 a, Vector3 b) { var d = b - a; int n = Mathf.CeilToInt(d.magnitude / 4); for (int i = 0; i <= n; i++) rail.Add(MeshKit.Cube, Vector3.Lerp(a, b, i / (float)n) + Vector3.up * .6f, Quaternion.identity, new Vector3(.2f, 1.2f, .2f), CoasterLook.Cream); rail.Add(MeshKit.Cube, (a + b) * .5f + Vector3.up * 1.15f, Quaternion.LookRotation(d.normalized), new Vector3(.15f, .15f, d.magnitude), CoasterLook.Cream); }
            Rail(L(-150, DeckY, -362), L(-150, DeckY, 233)); Rail(L(-150, DeckY, 271), L(-150, DeckY, 840)); Rail(L(-150, DeckY, -362), L(-14, DeckY, -362)); Rail(L(14, DeckY, -362), L(100, DeckY, -362));
            rail.Object(scen, "Pier railing", CoasterLook.Vc(false), false, false);
        }

        // ---------------- the log flume: the splashdown pool and the flume's banks through the park ----------------
        static float FlumeHalf(float p) { float s = M("splash"); return Mathf.Lerp(34f, 18f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(s + 70, s + 120, p))); }
        static void Flume()
        {
            var bank = CoasterLook.Mat(CoasterLook.PlankDark); var side = CoasterLook.Mat(CoasterLook.Pier);
            float a = M("splash") - 45, b = M("cross") - 108;   // Round 10: the banks start where the landing corridor does (the AI's planes come in low)
            MeshKit.Strip(root, "Flume bank L", Samples(a, b, p => -FlumeHalf(p) - 44, p => -FlumeHalf(p), (p, pt) => DeckY, (p, pt) => -3f, 4f), bank, side);
            MeshKit.Strip(root, "Flume bank R", Samples(a, b, p => FlumeHalf(p), p => FlumeHalf(p) + 44, (p, pt) => DeckY, (p, pt) => -3f, 4f), bank, side);
            // the flume's log rim (wood, red / white posts), lanterns along it
            var rim = new MeshKit.Batch(); int k = 0;
            for (float p = a + 6; p < b; p += 6, k++)
            {
                var q = At(p); var r = Right(p); var f = Facing(p);
                foreach (float s in new[] { -1f, 1f })
                {
                    var e = new Vector3(q.x, DeckY + .35f, q.z) + r * s * (FlumeHalf(p) + .5f);
                    rim.Add(MeshKit.LowCylinder, e, f * Quaternion.Euler(90, 0, 0), new Vector3(.9f, 3.1f, .9f), new Color(.55f, .38f, .24f));
                    if (k % 4 == 0) rim.Add(MeshKit.Cube, e + Vector3.up * 1.5f, Quaternion.identity, new Vector3(.4f, 3f, .4f), k % 8 == 0 ? CoasterLook.CoasterRed : CoasterLook.Cream);
                }
            }
            rim.Object(scen, "Flume rim", CoasterLook.Vc(true), false, false);
            var lan = new MeshKit.Batch(); for (float p = a + 12; p < b; p += 24) foreach (float s in new[] { -1f, 1f }) { var q = At(p); lan.Add(MeshKit.Cube, new Vector3(q.x, DeckY + 3.3f, q.z) + Right(p) * s * (FlumeHalf(p) + .5f), Quaternion.identity, Vector3.one * .7f, s < 0 ? CoasterLook.Lemon : CoasterLook.Neon); }
            lan.Object(scen, "Flume lanterns", GateMarker.Beacon, false, false);
            // a gentle current down the flume (it is a flume): +5 m/s along it
            for (float p = M("splash") + 110; p < M("cross") - 20; p += 30) { var q = At(p); q.y = -2; var cz = Vol<CurrentZone>("Flume current", q, Facing(p), new Vector3(30, 6, 34)); cz.push = Flat(Dir(p)) * 5f; }
        }

        // ---------------- the coaster: the rails and their track, supports, the boost pad, the station ----------------
        static void BuildRails()
        {
            var ra = MakeRail("coaster (rail A)", railA, M("railA")); var rb = MakeRail("top hat (rail B)", railB, M("railB"));
            // booster tyres: never under 40 m/s on rail A, 52 over its last 60 m (so the Coaster Gap is always cleared); 28 on rail B (the lift)
            ra.Booster(0, ra.Length, 40); ra.Booster(ra.Length - 60, ra.Length, 52);
            rb.Booster(0, rb.Length, 28); rb.Booster(RailS(railB, "liftFoot"), rb.Length, 30);
            Physics.SyncTransforms();
            Track(railA, "Rail A track"); Track(railB, "Rail B track");
            // the launch: a boost pad on the station's straight (to 72 m/s, like the loop's)
            float bp = M("pad"); var bq = At(bp); bq.y = DeckY;
            var pad = Vol<BoostPad>("Coaster launch pad", bq + Vector3.up * 1.5f, Facing(bp), new Vector3(RoadHalf * 2, 4, 22)); pad.speed = 72;
            var chev = new MeshKit.Batch();
            for (int i = 0; i < 4; i++) foreach (float s in new[] { -1f, 1f }) chev.Add(MeshKit.Cube, bq + Vector3.forward * (-7 + i * 4.5f) + Vector3.right * s * 1.8f + Vector3.up * .06f, Quaternion.Euler(0, -s * 40, 0), new Vector3(1.1f, .06f, 5.2f), new Color(.3f, 1f, .95f));
            chev.Object(scen, "Launch pad chevrons", GateMarker.Beacon, false, false);
            // the station over rail A's start: a candy-striped roof on posts, the sign
            var st = L(0, DeckY, 880);
            foreach (float x in new[] { -15f, 15f }) for (int i = 0; i < 4; i++) Box(scen, "Station post", st + new Vector3(x, 5, -18 + i * 12), new Vector3(.8f, 10, .8f), CoasterLook.Mat(CoasterLook.Cream, true), null, false);
            for (int i = 0; i < 8; i++) Box(scen, "Station roof", st + new Vector3(-14 + i * 4, 10.6f, 0), new Vector3(4, .6f, 40), CoasterLook.Mat(i % 2 == 0 ? CoasterLook.CoasterRed : CoasterLook.Cream, true), null, false);
            Box(scen, "Station sign", st + new Vector3(0, 12.8f, -20), new Vector3(24, 3, .5f), CoasterLook.Mat(CoasterLook.CoasterGold, true, CoasterLook.CoasterGold * .3f), null, false);
        }
        // Round 10: no support on a road: rail A's track passes back over the boardwalk at the station's launch straight, and the final soak's
        // AI cars hit its posts dead centre there (75 m/s turned 90 deg along the post: 20 resets in 50 laps). A road at the foot's height
        // within the road's width (+4 m) of the foot means no support there.
        static bool OverRoad(Vector3 foot, float ground)
        {
            for (float p = 0; p < c.Length; p += 3f)
            {
                var q = At(p); if (Mathf.Abs(q.y - ground) > 3f) continue;
                if (new Vector2(q.x - foot.x, q.z - foot.z).sqrMagnitude < (RoadHalf + 4f) * (RoadHalf + 4f)) return true;
            }
            return false;
        }
        // the track along a rail: a deck plate under the car, two gold rails with neon strips, ties, the spine, supports down to the deck or the sea
        static void Track(RailPath rp, string name)
        {
            var deck = new MeshKit.Batch(); var glow = new MeshKit.Batch(); var sup = new MeshKit.Batch();
            int n = rp.p.Count; float lastSup = -99;
            for (int i = 0; i < n - 1; i += 2)
            {
                int j = Mathf.Min(i + 2, n - 1); Vector3 a = rp.p[i], b = rp.p[j]; var t = b - a; float len = t.magnitude; if (len < .01f) continue; t /= len;
                var up = Vector3.ProjectOnPlane((rp.u[i] + rp.u[j]) * .5f, t).normalized; var rot = Quaternion.LookRotation(t, up); var rt = Vector3.Cross(up, t);
                var surf = (a + b) * .5f - up * .9f;
                deck.Add(MeshKit.Cube, surf - up * .3f, rot, new Vector3(RailHalf * 2, .6f, len + .1f), (i / 2) % 2 == 0 ? CoasterLook.CoasterRed : CoasterLook.CoasterRed * .85f);
                deck.Add(MeshKit.Cube, surf - up * 1.4f, rot, new Vector3(1.6f, 1.6f, len + .1f), CoasterLook.Steel);   // the spine
                foreach (float s in new[] { -1f, 1f })
                {
                    deck.Add(MeshKit.Cube, surf + rt * s * (RailHalf - .3f) + up * .35f, rot, new Vector3(.6f, .7f, len + .1f), CoasterLook.CoasterGold);
                    glow.Add(MeshKit.Cube, surf + rt * s * (RailHalf + .05f) + up * .3f, rot, new Vector3(.15f, .25f, len + .1f), s < 0 ? CoasterLook.Neon : CoasterLook.NeonB);
                }
                if ((i / 2) % 2 == 0) deck.Add(MeshKit.Cube, surf - up * .75f, rot, new Vector3(RailHalf * 2 + 1.4f, .3f, .5f), new Color(.25f, .22f, .22f));   // a tie
                // a support every 14 m where the track is the right way up and well above what is under it (not into the flume)
                float s0 = rp.cum[i];
                if (s0 - lastSup > 14 && up.y > .5f)
                {
                    var bottom = surf - up * 2.2f;
                    if (Physics.Raycast(bottom, Vector3.down, out var hit, 200f, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore) || true)
                    {
                        float ground = Physics.Raycast(bottom, Vector3.down, out hit, 200f, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : Water - 1;
                        ground = Mathf.Max(ground, Water - 1);
                        float h = bottom.y - ground;
                        if (h > 2.5f && !NearFlume(bottom) && !OverRoad(bottom, ground))
                        {
                            lastSup = s0;
                            sup.Add(MeshKit.LowCylinder, new Vector3(bottom.x, ground + h * .5f, bottom.z), Quaternion.identity, new Vector3(1.1f, h * .5f, 1.1f), CoasterLook.Steel);
                            if (h > 8) for (float y = ground + 6; y < bottom.y - 2; y += 9) sup.Add(MeshKit.Cube, new Vector3(bottom.x, y, bottom.z), Quaternion.LookRotation(Flat(t)) * Quaternion.Euler(0, 0, 45), new Vector3(.35f, 6f, .35f), CoasterLook.Steel * .9f);
                        }
                    }
                }
            }
            deck.Object(root, name, CoasterLook.Vc(true), false, true);
            glow.Object(scen, name + " neon", GateMarker.Beacon, false, false);
            sup.Object(scen, name + " supports", CoasterLook.Vc(true), false, true);
        }
        static List<Vector3> flumePts;
        static bool NearFlume(Vector3 p)
        {
            if (flumePts == null) { flumePts = new List<Vector3>(); for (float s = M("splash") - 45; s < M("seaOut"); s += 8) flumePts.Add(At(s)); }
            foreach (var q in flumePts) if (new Vector2(p.x - q.x, p.z - q.z).sqrMagnitude < 24 * 24) return true;
            return false;
        }

        // ---------------- the Coaster Gap: rail A ends at its lip over the sea; the landing ramp and deck to rail B ----------------
        static void CoasterGap()
        {
            var deck = CoasterLook.Mat(CoasterLook.PlankDark); var lip = railA.P;   // the car's centre at the lip (y 11)
            var f = Vector3.back; var top0 = new Vector3(lip.x, 7f, lip.z - 45f); var top1 = new Vector3(lip.x, DeckY, lip.z - 100f);
            var d = top1 - top0; var rot = Quaternion.LookRotation(d.normalized, Vector3.up); var mid = (top0 + top1) * .5f;
            Box(root, "Coaster Gap landing ramp", mid - rot * Vector3.up * .6f, new Vector3(28, 1.2f, d.magnitude + .2f), deck, rot);
            Box(root, "Coaster Gap landing ramp support", mid + Vector3.down * 3.5f, new Vector3(26, 6, d.magnitude - 4), CoasterLook.Mat(CoasterLook.Pier), rot, false);
            float z0 = lip.z - 100f, z1 = railB.p[0].z - 26f;
            Box(root, "Coaster Gap landing deck", new Vector3(lip.x, DeckY - .8f, (z0 + z1) * .5f), new Vector3(28, 1.6f, z0 - z1), deck);
            var legs = new MeshKit.Batch(); for (float z = z1; z < lip.z - 45; z += 16) foreach (float x in new[] { -13f, 13f }) legs.Add(MeshKit.LowCylinder, new Vector3(lip.x + x, 0, z), Quaternion.identity, new Vector3(1.2f, DeckY, 1.2f), CoasterLook.Pier);
            legs.Object(scen, "Landing deck legs", CoasterLook.Vc(true), false, false);
            // the lip: amber edge, a sign; the landing's chequers
            Box(scen, "Coaster Gap lip", lip + Vector3.down * .85f + f * .2f, new Vector3(RailHalf * 2 + 1, .12f, .8f), CoasterLook.Glow(new Color(1f, .62f, .12f)), null, false);
            var ch = new MeshKit.Batch(); for (int i = 0; i < 9; i++) ch.Add(MeshKit.Cube, top0 + Vector3.right * (-12 + i * 3) + Vector3.up * .06f, rot, new Vector3(1.5f, .06f, 1.5f), i % 2 == 0 ? Color.white : new Color(.1f, .1f, .1f));
            ch.Object(scen, "Landing checks", CoasterLook.Vc(false), false, false);
        }

        // ---------------- the fishing pier (the dive goes between its legs) ----------------
        static void FishingPier()
        {
            var deck = CoasterLook.Mat(CoasterLook.PlankDark); const float z = -557, x0 = -470, x1 = -40, half = 12f;
            Box(root, "Fishing pier", L((x0 + x1) * .5f, DeckY - .6f, z), new Vector3(x1 - x0, 1.2f, half * 2), deck);
            var wood = CoasterLook.Mat(CoasterLook.Pier, true);
            for (float x = x0 + 6; x < x1; x += 22)
                foreach (float s in new[] { -1f, 1f })
                {
                    if (s < 0 && (x < -232 || x > -112)) { } else if (s > 0 || true) { }
                    bool north = s > 0;
                    if (north && (x < -372 || x > -112)) continue;   // the dive comes in under the north side at the west end and leaves it at the east end
                    Cyl(root, "Pier leg", L(x, (SeaFloor + DeckY - 1.2f) * .5f, z + s * (half + 1.5f)), 1.2f, DeckY - 1.2f - SeaFloor, wood, true, ShiftVehicle.ObstacleLayer);
                }
            var lamps = new MeshKit.Batch(); for (float x = x0 + 10; x < x1; x += 30) foreach (float s in new[] { -1f, 1f }) { lamps.Add(MeshKit.Cube, L(x, DeckY + 2, z + s * (half - .5f)), Quaternion.identity, new Vector3(.25f, 4, .25f), CoasterLook.Pier); }
            lamps.Object(scen, "Pier lamp posts", CoasterLook.Vc(true), false, false);
            var bulbs = new MeshKit.Batch(); for (float x = x0 + 10; x < x1; x += 30) foreach (float s in new[] { -1f, 1f }) bulbs.Add(MeshKit.LowSphere, L(x, DeckY + 4.3f, z + s * (half - .5f)), Quaternion.identity, Vector3.one * .9f, CoasterLook.Lemon);
            bulbs.Object(scen, "Pier lamps", GateMarker.Beacon, false, false);
        }

        // ---------------- the splash pool: the water ride's basin the sub surfaces in, its ramp up onto the boardwalk ----------------
        static void SplashPool()
        {
            var wall = CoasterLook.Mat(CoasterLook.Sky * .8f, true); var trim = CoasterLook.Mat(CoasterLook.Cream, true);
            const float zs = -497, zn = -404, hx = 24;
            foreach (float s in new[] { -1f, 1f }) { Box(root, "Pool wall", L(s * (hx + 1.5f), (SeaFloor + 5.5f) * .5f, (zs + zn) * .5f), new Vector3(3, 5.5f - SeaFloor, zn - zs), wall); Box(scen, "Pool wall trim", L(s * (hx + 1.5f), 5.7f, (zs + zn) * .5f), new Vector3(3.3f, .4f, zn - zs + .3f), trim, null, false); }
            // the south side: a curtain wall that stops 5 m under the surface (the sub comes in under it)
            Box(root, "Pool curtain", L(0, (5.5f - 4.5f) * .5f, zs), new Vector3(hx * 2 + 6, 5.5f + 4.5f, 3), wall);
            Box(scen, "Pool curtain trim", L(0, 5.7f, zs), new Vector3(hx * 2 + 6.3f, .4f, 3.3f), trim, null, false);
            // the ramp (a boat climbs it on its hull; WetRamp) and its support
            float rf = M("rampFoot"), rl = M("rampLip"); var a = At(rf); a.y = -.6f; var b = At(rl); b.y = DeckY + 3.7f;
            var d = b - a; var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            var ramp = Box(root, "Pool ramp", (a + b) * .5f - rot * Vector3.up * .5f, new Vector3(24, 1, d.magnitude + .4f), CoasterLook.Mat(new Color(.4f, .72f, .85f)), rot);
            ramp.AddComponent<WetRamp>();
            Box(root, "Pool ramp support", L(0, (b.y - 6 + SeaFloor) * .5f, b.z - 2 - O.z), new Vector3(20, b.y - 6 - SeaFloor, 8), CoasterLook.Mat(CoasterLook.Pier), null, false);
            // a splash arch over the ramp (looks only): water jets from its top
            foreach (float s in new[] { -1f, 1f }) Box(scen, "Splash arch post", L(s * 15, 9, -392), new Vector3(1.4f, 14, 1.4f), trim, null, false);
            Box(scen, "Splash arch", L(0, 16.6f, -392), new Vector3(32, 2.4f, 2), CoasterLook.Mat(CoasterLook.Mint, true), null, false);
            var jet = GameObject.CreatePrimitive(PrimitiveType.Quad); jet.name = "Splash curtain"; jet.transform.SetParent(scen, false); UnityEngine.Object.Destroy(jet.GetComponent<Collider>());
            jet.transform.position = L(0, 10, -393); jet.transform.localScale = new Vector3(26, 13, 1); jet.GetComponent<MeshRenderer>().sharedMaterial = JurassicLook.FallMat(.28f);
            var archGo = new GameObject("Splash arch (set-piece)"); archGo.transform.SetParent(scen, false); archGo.transform.position = L(0, 10, -392);
            SetPiece.Make(archGo, "Splash arch", "Splash Return", "ccpool", Vector3.zero, new Vector3(34, 16, 3));
            // a giant rubber duck bobbing in the splash pool, beside the line (looks only)
            var duck = new GameObject("Giant rubber duck"); duck.transform.SetParent(scen, false); duck.transform.position = L(-15, .2f, -446); duck.transform.rotation = Quaternion.Euler(0, 150, 0);
            var dk = new GameObject("bob").transform; dk.SetParent(duck.transform, false); var bob = dk.gameObject.AddComponent<Bob>(); bob.height = .4f; bob.period = 3; bob.sway = 4;
            var db = new MeshKit.Batch(); var yel = new Color(1f, .85f, .15f);
            db.Add(MeshKit.LowSphere, new Vector3(0, 1.5f, 0), Quaternion.identity, new Vector3(8, 6, 11), yel); db.Add(MeshKit.LowSphere, new Vector3(0, 6, 2.6f), Quaternion.identity, new Vector3(5.4f, 5.4f, 5.4f), yel);
            db.Add(MeshKit.LowSphere, new Vector3(0, 5.6f, 5.6f), Quaternion.identity, new Vector3(2.6f, 1.2f, 2.4f), new Color(1f, .5f, .1f)); db.Add(MeshKit.LowCone, new Vector3(0, 3, -5.4f), Quaternion.Euler(-60, 0, 0), new Vector3(3, 3, 2), yel);
            foreach (float s2 in new[] { -1f, 1f }) { db.Add(MeshKit.LowSphere, new Vector3(s2 * 1.5f, 7, 4.8f), Quaternion.identity, new Vector3(1.1f, 1.3f, .6f), Color.white); db.Add(MeshKit.LowSphere, new Vector3(s2 * 1.55f, 7, 5.1f), Quaternion.identity, new Vector3(.55f, .7f, .3f), Color.black); }
            db.Object(dk, "duck", CoasterLook.Vc(true), false, true);
            SetPiece.Make(dk.gameObject, "Giant rubber duck", "Splash Return", "ccpool", new Vector3(0, 4.5f, 0), new Vector3(8, 9, 12));
            var lc = new MeshKit.Batch(); for (int i = 0; i < 5; i++) lc.Add(MeshKit.Cube, L(0, DeckY + .05f, -340 + i * 7), Quaternion.Euler(0, i % 2 == 0 ? 20 : -20, 0), new Vector3(9, .05f, 1.2f), new Color(.95f, .5f, .1f));
            lc.Object(scen, "Landing chevrons", CoasterLook.Vc(false), false, false);
        }

        // ---------------- the lighthouse on its islet; the rocks of the shortcut ----------------
        public static Vector3 Lighthouse0 => L(-352, 0, -55);
        static void Lighthouse()
        {
            var li = Lighthouse0; var rock = CoasterLook.Mat(CoasterLook.Rock, true);
            Cyl(root, "Lighthouse islet", li + Vector3.up * ((SeaFloor + 5) * .5f), 26, 5 - SeaFloor, rock, true, ShiftVehicle.ObstacleLayer);
            var lumps = new MeshKit.Batch(); var rnd = new System.Random(3);
            for (int i = 0; i < 16; i++) { float a = i * Mathf.PI * 2 / 16; lumps.Add(MeshKit.LowSphere, li + new Vector3(Mathf.Cos(a) * 24, 3, Mathf.Sin(a) * 24), Quaternion.Euler(0, i * 37, 0), new Vector3(10, 6 + (float)rnd.NextDouble() * 4, 9), CoasterLook.Rock); }
            lumps.Object(scen, "Islet rocks", CoasterLook.Vc(true), false, true);
            for (int i = 0; i < 6; i++) Cyl(scen, "Lighthouse band", li + Vector3.up * (8 + i * 5), 5.2f - i * .3f, 5, CoasterLook.Mat(i % 2 == 0 ? CoasterLook.Cream : CoasterLook.CoasterRed, true), false);
            Cyl(scen, "Lighthouse lamp room", li + Vector3.up * 39.5f, 3.6f, 4, CoasterLook.Glow(CoasterLook.Lemon), false);
            Cyl(scen, "Lighthouse cap", li + Vector3.up * 43, 4.2f, 2, CoasterLook.Mat(CoasterLook.CoasterRed * .8f, true), false);
            var beam = new GameObject("Lighthouse beam"); beam.transform.SetParent(scen, false); beam.transform.position = li + Vector3.up * 39.5f; beam.AddComponent<Spin>().degPerSec = 40;
            var bb = Box(beam.transform, "beam", Vector3.zero, new Vector3(1.6f, 1.6f, 90), CoasterLook.Glow(CoasterLook.Lemon * .9f), null, false); bb.transform.localPosition = new Vector3(0, 0, 45); bb.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // three sea stacks east of the islet: the shortcut's channel between them and the islet
            foreach (var (dz, r) in new[] { (55f, 7f), (-5f, 8f), (-70f, 6.5f) }) Cyl(root, "Sea stack", li + new Vector3(68 + r * .3f, (SeaFloor + 9) * .5f, dz), r, 9 - SeaFloor, rock, true, ShiftVehicle.ObstacleLayer);
        }
    }
}
