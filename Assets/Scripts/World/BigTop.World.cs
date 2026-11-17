using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 15 item 3: Big Top Breakdown's world (the race logic is in BigTop.cs). The meadow is flat ground (y 4) cut by the river and the
    // lake (a car that drives off a bank is in the water and is recovered); the roads are flush with the meadow (no walls to be stranded
    // behind: Round 10's rule). The big top's canvas wall and roof are solid (the top flap is the way out); the wagons are solid and move;
    // the river banks, the island, its rocks, the sunken wagon and the pool's walls are solid for a boat or a sub. The rest is looks only.
    public static partial class BigTop
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
            var m = new Mesh { name = "water grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static float RoadTop(float p) => G + Lift(p);
        static GameObject Detail(GameObject g) { if (g) g.layer = ParadisePlunge.DetailLayer; return g; }
        static float Rand(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        // ---------------- the meadow, the river, the lake ----------------
        // the water: the river (x 800..920, z -300..1400), its mouth (x 780..920, z -362..-300) and the lake (x -60..920, z -820..-362)
        public static readonly Rect[] WaterRects = { Rect.MinMaxRect(800, -300, 920, 1400), Rect.MinMaxRect(780, -362, 920, -300), Rect.MinMaxRect(-60, -820, 920, -362), Rect.MinMaxRect(760, -20, 960, 260) };
        static void Land()
        {
            var grass = BigTopLook.Mat(BigTopLook.Meadow);
            // the meadow: boxes round the water and round the clowns' pit (x -24..24 under the Clown Jump's gap)
            float pitZ0 = 640 + .5f, pitZ1 = 640 + PitLen - .5f;
            var rects = new List<Rect>
            {
                Rect.MinMaxRect(-1600, -1600, -60, 1900), Rect.MinMaxRect(960, -1600, 2300, 1900), Rect.MinMaxRect(920, -1600, 960, -20), Rect.MinMaxRect(920, 260, 960, 1900),
                Rect.MinMaxRect(-60, -1600, 920, -820), Rect.MinMaxRect(800, 1400, 920, 1900),
                Rect.MinMaxRect(-60, -300, -PitHalf, 1900), Rect.MinMaxRect(PitHalf, -300, 760, 1900), Rect.MinMaxRect(760, -300, 800, -20), Rect.MinMaxRect(760, 260, 800, 1900),
                Rect.MinMaxRect(-PitHalf, -300, PitHalf, pitZ0), Rect.MinMaxRect(-PitHalf, pitZ1, PitHalf, 1900),
                Rect.MinMaxRect(-60, -362, 780, -300),
            };
            foreach (var r in rects) Box(root, "Meadow", L(r.center.x, (G - .03f + Floor - 2) * .5f, r.center.y), new Vector3(r.width, G - .03f - Floor + 2, r.height), grass, null, true, ShiftVehicle.ObstacleLayer);
            // the island in the river (the Island rapids' channel east of it)
            Box(root, "Island", L(826, (G - 1 + Floor) * .5f, 120), new Vector3(40, G - 1 - Floor, 160), grass, null, true, ShiftVehicle.ObstacleLayer);
            // the water body under everything (a car off a bank is in the water and is recovered), its bed, the visible surface
            Vol<WaterVolume>("River and lake", L(300, (Water + Floor - 2) * .5f, 300), Quaternion.identity, new Vector3(3900, Water - Floor + 2, 3500));
            Box(root, "River bed", L(300, Floor - 1, 300), new Vector3(3900, 2, 3500), BigTopLook.Mat(BigTopLook.Dirt * .8f));
            var w = new GameObject("River (visual)"); w.transform.SetParent(root, false); w.AddComponent<MeshFilter>().sharedMesh = Grid(3200, 80); w.transform.position = L(400, Water - .02f, 250);
            var wr = w.AddComponent<MeshRenderer>(); wr.sharedMaterial = JurassicLook.WaterMat(BigTopLook.River, BigTopLook.RiverLight, BigTopLook.TopWhite, .1f); wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // the pit: its floor and the clown cars' pile-up are in Midway(); far hills and a tree line round the meadow
            var far = new MeshKit.Batch(); var rnd = new System.Random(17);
            for (int i = 0; i < 70; i++) { float a = i * Mathf.PI * 2 / 70, rr = Rand(1700, 2000); far.Add(MeshKit.LowSphere, L(Mathf.Cos(a) * rr + 300, Rand(rnd, -10, 10), Mathf.Sin(a) * rr + 300), Quaternion.Euler(0, i * 41, 0), new Vector3(Rand(rnd, 300, 520), Rand(rnd, 90, 200), Rand(rnd, 260, 420)), Color.Lerp(BigTopLook.MeadowDark, new Color(.45f, .62f, .5f), (float)rnd.NextDouble())); }
            far.Object(scen, "Far hills", BigTopLook.Vc(true), false, false);
            var trees = new MeshKit.Batch();
            void Tree(Vector3 p, float h) { trees.Add(MeshKit.LowCylinder, p + Vector3.up * h * .25f, Quaternion.identity, new Vector3(.9f, h * .25f, .9f), BigTopLook.WoodDark); trees.Add(MeshKit.LowSphere, p + Vector3.up * h * .7f, Quaternion.Euler(0, h * 31, 0), new Vector3(h * .55f, h * .5f, h * .55f), Color.Lerp(BigTopLook.MeadowDark, new Color(.25f, .5f, .22f), (h % 3) / 3f)); }
            for (int i = 0; i < 110; i++) { float x = Rand(rnd, -700, 1300), z = Rand(rnd, -900, 1700); if (Near(x, z, 60)) continue; Tree(L(x, G, z), Rand(rnd, 9, 16)); }
            // along the river banks
            for (float z = -280; z < 1350; z += 28) { if (z > 560 && z < 700) continue; bool bulge = z > -30 && z < 270; Tree(L((bulge ? 745 : 785) + Rand(rnd, -8, 4), G, z), Rand(rnd, 9, 14)); Tree(L((bulge ? 975 : 935) + Rand(rnd, -4, 8), G, z + 12), Rand(rnd, 9, 14)); }
            for (float z = 55; z < 190; z += 26) Tree(L(826 + Rand(rnd, -8, 8), G - 1, z), Rand(rnd, 8, 12));   // the island
            Detail(trees.Object(scen, "Trees", BigTopLook.Vc(true), false, true));
        }
        static float Rand(float a, float b) => a + UnityEngine.Random.value * (b - a);
        // is (x, z) from O within `d` of the line on the ground, the tent, or the water?
        static List<Vector2> nearPts;
        static bool Near(float x, float z, float d)
        {
            if (nearPts == null) { nearPts = new List<Vector2>(); for (float s = 0; s < c.Length; s += 10) { var q = At(s) - O; nearPts.Add(new Vector2(q.x, q.z)); } }
            foreach (var q in nearPts) if ((q - new Vector2(x, z)).sqrMagnitude < d * d) return true;
            if (new Vector2(x - TentC.x, z - TentC.z).magnitude < TentR + 20) return true;
            foreach (var r in WaterRects) if (x > r.xMin - 10 && x < r.xMax + 10 && z > r.yMin - 10 && z < r.yMax + 10) return true;
            return false;
        }

        // ---------------- the roads (flush with the meadow; sawdust and boards) ----------------
        static void Roads()
        {
            var top = BigTopLook.Mat(BigTopLook.Sawdust); var side = BigTopLook.Mat(BigTopLook.Dirt);
            float lip = M("jC"), land = lip + PitLen;
            Func<float, float> half = p => p > M("wagons") - 30 && p < M("wagonsEnd") + 30 ? WagonRoadHalf : RoadHalf;
            MeshKit.Strip(root, "Midway road", Samples(0, lip, p => -half(p), p => half(p), (p, pt) => RoadTop(p), (p, pt) => G - 1.6f, 3f), top, side);
            MeshKit.Strip(root, "Midway road (after the pit)", Samples(land, M("cannon") + 10, p => -RoadHalf, p => RoadHalf, (p, pt) => G, (p, pt) => G - 1.6f, 3f), top, side);
            float fa = M("landing") - 12, fb = M("lineEnd") - 80f;
            MeshKit.Strip(root, "Midway road (finish)", Samples(fa, fb, p => -RoadHalf, p => RoadHalf, (p, pt) => G, (p, pt) => G - 1.6f, 3f), top, side);
            // the Clown Jump's kicker edge, chevrons up it, the landing's chequers
            var amber = new Color(1f, .62f, .12f); var lq = At(lip);
            Box(scen, "Lip edge", new Vector3(lq.x, RoadTop(lip) + .04f, lq.z) - Flat(Dir(lip)) * .6f, new Vector3(26, .08f, 1.2f), BigTopLook.Glow(amber), Facing(lip), false);
            var chev = new MeshKit.Batch();
            for (float p = lip - KickerLen + 6; p < lip - 4; p += 9) { var q = At(p); q.y = RoadTop(p) + .05f; foreach (float s in new[] { -1f, 1f }) chev.Add(MeshKit.Cube, q + Right(p) * s * 2f, Facing(p) * Quaternion.Euler(-Mathf.Atan(2 * KickerH * Mathf.InverseLerp(lip - KickerLen, lip, p) / KickerLen) * Mathf.Rad2Deg, -s * 38, 0), new Vector3(1f, .06f, 5f), amber); }
            var fq = At(land); for (int i = 0; i < 9; i++) chev.Add(MeshKit.Cube, new Vector3(fq.x, G + .05f, fq.z) + Right(land) * (-12 + i * 3) + Flat(Dir(land)) * 1.2f, Facing(land), new Vector3(1.5f, .06f, 1.5f), i % 2 == 0 ? Color.white : new Color(.1f, .1f, .1f));
            chev.Object(scen, "Kicker chevrons", BigTopLook.Vc(false), false, false);
            // start / finish: the line, the chequers, the arch ("BIG TOP BREAKDOWN" in bulbs)
            var fin = At(M("finish")); fin.y = G;
            Box(root, "Finish line", fin + Vector3.up * .04f, new Vector3(28, .06f, 2.2f), BigTopLook.Mat(new Color(.1f, .1f, .1f)), null, false);
            for (int i = 0; i < 14; i++) Box(root, "Finish check", fin + new Vector3(-13 + i * 2, .08f, (i % 2) * 1f - .5f), new Vector3(1, .02f, 1), BigTopLook.Mat(Color.white), null, false);
            foreach (float x in new[] { -18f, 18f })
                for (int k = 0; k < 7; k++) Box(scen, "Arch post", fin + new Vector3(x, 1 + k * 2, 2), new Vector3(1.3f, 2, 1.3f), BigTopLook.Mat(k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite, true), null, false);
            Box(scen, "Arch board", fin + new Vector3(0, 15.4f, 2), new Vector3(40, 3.4f, .6f), BigTopLook.Mat(BigTopLook.Royal, true), null, false);
            foreach (float x in new[] { -18f, 18f }) { var fl = Box(scen, "Arch pennant", fin + new Vector3(x, 17.5f, 2), new Vector3(.2f, 2.4f, 4f), BigTopLook.Mat(BigTopLook.Gold, true), null, false); fl.AddComponent<Flap>(); }
            Label("BIG TOP BREAKDOWN", fin + new Vector3(0, 15.4f, 1.6f), Quaternion.LookRotation(Vector3.back), 2.1f, BigTopLook.Gold);
            Label("BIG TOP BREAKDOWN", fin + new Vector3(0, 15.4f, 2.4f), Quaternion.LookRotation(Vector3.forward), 2.1f, BigTopLook.Gold);
        }
        // lettering in marquee bulbs (5 x 7), glowing; rot's forward is the side it is read from
        static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" }, ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" }, ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" }, ['G'] = new[] { ".####", "#....", "#....", "#.###", "#...#", "#...#", ".###." },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" }, ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" }, ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#" }, ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." }, ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." }, ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." }, ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
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

        // ---------------- the rails: the Globe of Death and the cannon's barrel ----------------
        public static Vector3 GlobeCentre => globeP.p[globeP.names["gIn"]] + Vector3.up * GlobeR + Vector3.right * GlobeShift * .5f - Vector3.up * .45f;
        static void BuildRails()
        {
            var gr = MakeRail("Globe of Death", globeP, M("globe")); var cr = MakeRail("cannon", cannonP, M("cannon"));
            // booster tyres: never under 26 m/s in the Globe (a car crawling in still loops), 30 on the barrel, the launch to 80 over its last 22 m
            gr.Booster(0, gr.Length, 26); gr.Booster(RailS(globeP, "gIn"), RailS(globeP, "gOut"), 30);
            cr.Booster(0, cr.Length, 30); cr.Booster(cr.Length - 22, cr.Length, 80);
            Physics.SyncTransforms();
            Plate(globeP, "Globe track", BigTopLook.Steel, BigTopLook.TopRed);
            Plate(cannonP, "Cannon track", BigTopLook.Steel * .8f, BigTopLook.Gold);
            // the Globe of Death: a steel-mesh sphere round the loop (meridians and parallels of steel bars), open where the track goes in
            // and out, on a red and gold pedestal ring
            var gc = GlobeCentre;
            var bars = new MeshKit.Batch(); float R = GlobeSphere;
            // open where the track goes in and out: the bottom band over the track's width (the entry at x 0, the exit 6 m east)
            bool Open(Vector3 q) => q.y < G - .2f || (q.y < Road + 5f && q.x - O.x > GlobeX - 6f && q.x - O.x < GlobeX + 12f);
            void Bar(Vector3 p0, Vector3 p1, Color col) { var m = (p0 + p1) * .5f; if (Open(m)) return; var d = p1 - p0; var dn = d.normalized; bars.Add(MeshKit.Cube, m, Quaternion.LookRotation(dn, Mathf.Abs(dn.y) > .95f ? Vector3.right : Vector3.up), new Vector3(.32f, .32f, d.magnitude + .2f), col); }
            for (int mer = 0; mer < 14; mer++)
            {
                float ph = mer * Mathf.PI / 14;
                for (int k = 0; k < 28; k++) { float t0 = k * Mathf.PI * 2 / 28, t1 = (k + 1) * Mathf.PI * 2 / 28; Bar(gc + Sph(ph, t0) * R, gc + Sph(ph, t1) * R, mer % 2 == 0 ? BigTopLook.Steel : BigTopLook.Steel * .85f); }
            }
            for (int par = 1; par < 8; par++)
            {
                float lat = -Mathf.PI / 2 + par * Mathf.PI / 8, rr = R * Mathf.Cos(lat), y = R * Mathf.Sin(lat);
                for (int k = 0; k < 32; k++) { float t0 = k * Mathf.PI * 2 / 32, t1 = (k + 1) * Mathf.PI * 2 / 32; Bar(gc + new Vector3(Mathf.Cos(t0) * rr, y, Mathf.Sin(t0) * rr), gc + new Vector3(Mathf.Cos(t1) * rr, y, Mathf.Sin(t1) * rr), par == 4 ? BigTopLook.Gold : BigTopLook.Steel); }
            }
            bars.Object(scen, "Globe of Death", BigTopLook.Vc(true), false, true);
            var ped = new MeshKit.Batch(); for (int k = 0; k < 24; k++) { float t = k * Mathf.PI * 2 / 24; ped.Add(MeshKit.Cube, new Vector3(gc.x + Mathf.Cos(t) * 12.5f, G + .6f, gc.z + Mathf.Sin(t) * 12.5f), Quaternion.Euler(0, -t * Mathf.Rad2Deg, 0), new Vector3(1.2f, 1.2f, 3.4f), k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.Gold); }
            ped.Object(scen, "Globe pedestal", BigTopLook.Vc(true), false, false);
            var go = new GameObject("Globe of Death (set-piece)"); go.transform.SetParent(scen, false); go.transform.position = gc;
            SetPiece.Make(go, "Globe of Death", "The Midway", "btglobe", Vector3.zero, Vector3.one * R * 2).range = 600;
            // its sign over the road before it
            var sq = At(M("globe") - 60); sq.y = G;
            foreach (float s in new[] { -1f, 1f }) Box(scen, "Globe sign post", sq + Right(M("globe") - 60) * s * 15 + Vector3.up * 6, new Vector3(.6f, 12, .6f), BigTopLook.Mat(BigTopLook.Steel, true), null, false);
            Box(scen, "Globe sign", sq + Vector3.up * 11, new Vector3(30, 2.6f, .3f), BigTopLook.Mat(BigTopLook.Purple, true), Facing(M("globe") - 60), false);
            Label("GLOBE OF DEATH", sq + Vector3.up * 11 - Flat(Dir(M("globe") - 60)) * .2f, Quaternion.LookRotation(-Flat(Dir(M("globe") - 60))), 1.8f, BigTopLook.Gold);
        }
        static Vector3 Sph(float ph, float t) => new Vector3(Mathf.Cos(t) * Mathf.Cos(ph), Mathf.Sin(t), Mathf.Cos(t) * Mathf.Sin(ph));
        // a track plate along a rail (under the car), two edge strips
        static void Plate(CoasterCoast.RailPath rp, string name, Color deckCol, Color edge)
        {
            var deck = new MeshKit.Batch(); int n = rp.p.Count;
            for (int i = 0; i < n - 1; i += 2)
            {
                int j = Mathf.Min(i + 2, n - 1); Vector3 a = rp.p[i], b = rp.p[j]; var t = b - a; float len = t.magnitude; if (len < .01f) continue; t /= len;
                var up = Vector3.ProjectOnPlane((rp.u[i] + rp.u[j]) * .5f, t).normalized; var rot = Quaternion.LookRotation(t, up); var rt = Vector3.Cross(up, t);
                var surf = (a + b) * .5f - up * .9f;
                deck.Add(MeshKit.Cube, surf - up * .25f, rot, new Vector3(9, .5f, len + .1f), (i / 2) % 2 == 0 ? deckCol : deckCol * .92f);
                foreach (float s in new[] { -1f, 1f }) deck.Add(MeshKit.Cube, surf + rt * s * 4.6f + up * .25f, rot, new Vector3(.4f, .6f, len + .1f), edge);
            }
            deck.Object(root, name, BigTopLook.Vc(true), false, true);
        }

        // ---------------- the big top: the canvas wall (a door on the south), the striped roof with the top flap blown open ----------------
        static void Tent()
        {
            var tc = L(TentC.x, 0, TentC.z);
            // the wall: 72 panels, red and white, solid (except the door: 48 m wide, south)
            var wall = new MeshKit.Batch(); var door = new MeshKit.Batch();
            for (int k = 0; k < 72; k++)
            {
                float t = (k + .5f) * 5f; bool isDoor = Mathf.Abs(Mathf.DeltaAngle(t, 270)) < 10;
                var dir = new Vector3(Mathf.Cos(t * Mathf.Deg2Rad), 0, Mathf.Sin(t * Mathf.Deg2Rad)); var at = tc + dir * TentR;
                var rot = Quaternion.LookRotation(dir); float w = 2 * TentR * Mathf.Sin(2.5f * Mathf.Deg2Rad) + .4f;
                if (isDoor) { door.Add(MeshKit.Cube, at + Vector3.up * (G + TentWall - 1), rot, new Vector3(w, 2, .6f), k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite); continue; }
                wall.Add(MeshKit.Cube, at + Vector3.up * (G + TentWall * .5f), rot, new Vector3(w, TentWall, .6f), k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite);
            }
            var wo = wall.Object(root, "Big top wall", BigTopLook.Vc(true), true, true); wo.layer = ShiftVehicle.ObstacleLayer;
            door.Object(scen, "Big top door lintel", BigTopLook.Vc(true), false, false);
            // the roof: a striped cone (72 stripes, 14 rings) with the top flap's hole, both faces; solid
            for (int side = 0; side < 2; side++)
            {
                var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
                for (int i = 0; i < 14; i++)
                    for (int k = 0; k < 72; k++)
                    {
                        float r0 = i * TentR / 14, r1 = (i + 1) * TentR / 14, a0 = k * 5 * Mathf.Deg2Rad, a1 = (k + 1) * 5 * Mathf.Deg2Rad, rm = (r0 + r1) * .5f, am = (a0 + a1) * .5f;
                        if (new Vector2(TentC.x + Mathf.Cos(am) * rm - FlapC.x, TentC.z + Mathf.Sin(am) * rm - FlapC.z).magnitude < FlapR) continue;
                        Vector3 P(float r, float a) => tc + new Vector3(Mathf.Cos(a) * r, G + RoofAt(r) + (side == 0 ? 0 : -.12f), Mathf.Sin(a) * r);
                        int b0 = v.Count; v.Add(P(r0, a0)); v.Add(P(r0, a1)); v.Add(P(r1, a1)); v.Add(P(r1, a0));
                        var cc = k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite; if (side == 1) cc *= .82f; for (int q = 0; q < 4; q++) col.Add(cc);
                        if (side == 0) { tri.Add(b0); tri.Add(b0 + 1); tri.Add(b0 + 2); tri.Add(b0); tri.Add(b0 + 2); tri.Add(b0 + 3); }
                        else { tri.Add(b0); tri.Add(b0 + 2); tri.Add(b0 + 1); tri.Add(b0); tri.Add(b0 + 3); tri.Add(b0 + 2); }
                    }
                for (int q = 0; q < v.Count; q++) v[q] -= root.position;
                var m = new Mesh { name = "big top roof", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
                var g = new GameObject(side == 0 ? "Big top roof" : "Big top roof (inside)"); g.transform.SetParent(root, false); g.layer = ShiftVehicle.ObstacleLayer;
                g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterial = BigTopLook.Vc(side == 0);
                g.AddComponent<MeshCollider>().sharedMesh = m;
            }
            // the top flap: a canvas flap folded back from the hole, flapping in the wind; its gold rim
            var fc = L(FlapC.x, G + RoofAt(new Vector2(FlapC.x - TentC.x, FlapC.z - TentC.z).magnitude), FlapC.z);
            var hinge = new GameObject("Top flap"); hinge.transform.SetParent(scen, false); hinge.transform.position = fc + Vector3.right * FlapR + Vector3.up * 2; hinge.transform.rotation = Quaternion.Euler(0, 0, -60);
            var fl = new MeshKit.Batch(); for (int s = 0; s < 6; s++) fl.Add(MeshKit.Cube, new Vector3(-12 + s * 4 + 2, 9, 0), Quaternion.identity, new Vector3(4, 18, .3f), s % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite);
            fl.Object(hinge.transform, "flap canvas", BigTopLook.Vc(true), false, true); var ff = hinge.AddComponent<Flap>(); ff.axis = Vector3.forward; ff.deg = 14; ff.speed = 2.4f;
            var rim = new MeshKit.Batch(); for (int k = 0; k < 36; k++) { float t = k * 10 * Mathf.Deg2Rad; var q = new Vector3(FlapC.x + Mathf.Cos(t) * FlapR, 0, FlapC.z + Mathf.Sin(t) * FlapR); float rr = new Vector2(q.x - TentC.x, q.z - TentC.z).magnitude; rim.Add(MeshKit.Cube, L(q.x, G + RoofAt(rr) + .3f, q.z), Quaternion.Euler(0, -k * 10, 0), new Vector3(.6f, .6f, 4.4f), BigTopLook.Gold); }
            rim.Object(scen, "Top flap rim", BigTopLook.Vc(true), false, false);
            // the king poles (north of the ring, clear of the cannon's flight), the flags on top
            foreach (float x in new[] { -10f, 10f }) Cyl(root, "King pole", L(x, (G + TentApex + 6) * .5f, TentC.z + 14), 1.1f, TentApex + 6 - G, BigTopLook.Mat(BigTopLook.Gold, true), true, ShiftVehicle.ObstacleLayer);
            foreach (float x in new[] { -10f, 10f }) { var fg = new GameObject("Big top flag"); fg.transform.SetParent(scen, false); fg.transform.position = L(x, G + TentApex + 10, TentC.z + 14); var fb = new MeshKit.Batch(); fb.Add(MeshKit.Cube, new Vector3(0, 0, 4), Quaternion.identity, new Vector3(.2f, 4, 8), x < 0 ? BigTopLook.Royal : BigTopLook.Gold); fb.Object(fg.transform, "flag", BigTopLook.Vc(true), false, false); fg.AddComponent<Flap>().deg = 26; }
            // guy ropes and stakes round the outside (looks only, Detail)
            var ropes = new MeshKit.Batch();
            for (int k = 0; k < 36; k++)
            {
                float t = k * 10 * Mathf.Deg2Rad; if (Mathf.Abs(Mathf.DeltaAngle(k * 10, 270)) < 14) continue;
                var d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)); var top = tc + d * TentR + Vector3.up * (G + TentWall); var stake = tc + d * (TentR + 18) + Vector3.up * G;
                if (Near(stake.x - O.x, stake.z - O.z, 20)) continue;
                var dd = stake - top; ropes.Add(MeshKit.Cube, (top + stake) * .5f, Quaternion.LookRotation(dd.normalized), new Vector3(.12f, .12f, dd.magnitude), new Color(.85f, .8f, .65f));
                ropes.Add(MeshKit.Cube, stake + Vector3.up * .5f, Quaternion.identity, new Vector3(.4f, 1, .4f), BigTopLook.WoodDark);
            }
            Detail(ropes.Object(scen, "Guy ropes", BigTopLook.Vc(false), false, false));
            // inside: the ring (sawdust, a red and gold curb), strings of bulbs from the king poles
            Cyl(scen, "Big top floor", L(TentC.x, G - .015f, TentC.z), TentR - 1, .002f, BigTopLook.Mat(BigTopLook.Sawdust * .92f), false);
            Cyl(scen, "The ring", L(TentC.x, G + .02f, TentC.z + 10), 22, .06f, BigTopLook.Mat(BigTopLook.Sawdust * 1.05f), false);
            var curb = new MeshKit.Batch(); for (int k = 0; k < 40; k++) { float t = k * 9 * Mathf.Deg2Rad; curb.Add(MeshKit.Cube, L(TentC.x + Mathf.Cos(t) * 22, G + .5f, TentC.z + 10 + Mathf.Sin(t) * 22), Quaternion.Euler(0, -k * 9, 0), new Vector3(1, 1, 3.6f), k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.Gold); }
            curb.Object(scen, "Ring curb", BigTopLook.Vc(true), false, false);
            var bulbs = new MeshKit.Batch();
            for (int k = 0; k < 12; k++)
            {
                float t = k * 30 * Mathf.Deg2Rad; var from = L(0, G + TentApex - 6, TentC.z + 14); var to = tc + new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)) * (TentR - 4) + Vector3.up * (G + TentWall - 1);
                for (int i = 1; i < 16; i++) { var q = Vector3.Lerp(from, to, i / 16f) + Vector3.down * Mathf.Sin(i / 16f * Mathf.PI) * 4; if (new Vector2(q.x - O.x - FlapC.x, q.z - O.z - FlapC.z).magnitude < FlapR + 4) continue; bulbs.Add(MeshKit.LowSphere, q, Quaternion.identity, Vector3.one * .7f, i % 2 == 0 ? BigTopLook.Gold : BigTopLook.TopWhite); }
            }
            bulbs.Object(scen, "Big top bulbs", GateMarker.Beacon, false, false);
            // the hoops under the tent: three big rings on the line (red and gold), with the trapeze rigging beside them (BigTop.Life)
            var hoops = new MeshKit.Batch();
            foreach (var h in new[] { "hoop1", "hoop2", "hoop3" })
            {
                var q = At(M(h)); var frame = Quaternion.LookRotation(Dir(M(h)), Vector3.up); const float R = 12f;
                for (int i = 0; i < 24; i++) { float a = i * 15f; hoops.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0) * R), frame * Quaternion.Euler(0, 0, a), new Vector3(1.1f, R * .27f, 1.1f), i % 2 == 0 ? BigTopLook.TopRed : BigTopLook.Gold); }
                // hung from the roof by two lines
                foreach (float s in new[] { -1f, 1f }) { var top = q + frame * new Vector3(s * R * .7f, R * .7f, 0); var r = new Vector3(top.x, G + RoofAt(new Vector2(top.x - O.x - TentC.x, top.z - O.z - TentC.z).magnitude) - .3f, top.z); hoops.Add(MeshKit.Cube, (top + r) * .5f, Quaternion.identity, new Vector3(.15f, Mathf.Max(.1f, r.y - top.y), .15f), new Color(.3f, .3f, .3f)); }
            }
            hoops.Object(scen, "Hoops", BigTopLook.Vc(true), false, false);
            var hg = new GameObject("Hoops (set-piece)"); hg.transform.SetParent(scen, false); hg.transform.SetPositionAndRotation(At(M("hoop2")), Facing(M("hoop2")));
            SetPiece.Make(hg, "Hoops", "Human Cannonball", "", Vector3.zero, new Vector3(26, 26, 50));
            var tf = new GameObject("Top flap (set-piece)"); tf.transform.SetParent(scen, false); tf.transform.position = fc;
            SetPiece.Make(tf, "Top flap", "Human Cannonball", "", Vector3.zero, new Vector3(FlapR * 2, 14, FlapR * 2));
        }

        // ---------------- the giant cannon (the barrel round its rail), its carriage, the confetti at the muzzle ----------------
        static void Cannon()
        {
            var staves = new MeshKit.Batch(); int n = cannonP.p.Count; float lastS = -9; const float R = 5.6f;
            for (int i = 0; i < n - 1; i++)
            {
                if (cannonP.cum[i] - lastS < 2f) continue; lastS = cannonP.cum[i];
                var a = cannonP.p[i]; var t = (cannonP.p[Mathf.Min(i + 2, n - 1)] - a).normalized; var up = Vector3.ProjectOnPlane(Vector3.up, t).normalized; var rt = Vector3.Cross(up, t);
                var axis = a + up * 1.2f; bool band = Mathf.Repeat(cannonP.cum[i], 9f) < 2f;
                for (int k = 0; k < 16; k++)
                {
                    float th = k * Mathf.PI * 2 / 16; if (Mathf.Sin(th) < -.55f) continue;   // open underneath (the track runs there)
                    var o = (rt * Mathf.Cos(th) + up * Mathf.Sin(th)) * R; staves.Add(MeshKit.Cube, axis + o, Quaternion.LookRotation(t, o.normalized), new Vector3(2.3f, .5f, 2.2f), band ? BigTopLook.Gold : BigTopLook.Royal);
                }
            }
            // the muzzle's flared ring, the breech's ball
            { var m = cannonP.p[n - 1]; var t = (m - cannonP.p[n - 4]).normalized; var up = Vector3.ProjectOnPlane(Vector3.up, t).normalized; var rt = Vector3.Cross(up, t); var axis = m + up * 1.2f;
              for (int k = 0; k < 20; k++) { float th = k * Mathf.PI * 2 / 20; if (Mathf.Sin(th) < -.55f) continue; var o = (rt * Mathf.Cos(th) + up * Mathf.Sin(th)) * (R + .9f); staves.Add(MeshKit.Cube, axis + o, Quaternion.LookRotation(t, o.normalized), new Vector3(2.2f, 1f, 1.6f), BigTopLook.Gold); } }
            // the carriage: two big spoked wheels either side of the breech, a red base
            foreach (float s in new[] { -1f, 1f })
            {
                var wc = cannonP.p[0] + Vector3.right * 10 + Vector3.forward * s * 8f + Vector3.up * 3.5f;
                staves.Add(MeshKit.LowCylinder, wc, Quaternion.Euler(90, 0, 0), new Vector3(9, .7f, 9), BigTopLook.TopRed);
                for (int k = 0; k < 8; k++) staves.Add(MeshKit.Cube, wc, Quaternion.Euler(0, 0, k * 22.5f), new Vector3(.5f, 8.6f, 1.6f), BigTopLook.Gold);
            }
            staves.Object(scen, "Giant cannon", BigTopLook.Vc(true), false, true);
            var conf = new GameObject("Cannon confetti"); conf.transform.SetParent(scen, false); conf.transform.position = cannonP.p[n - 1] + Vector3.up * 3 + Vector3.right * 6;
            var cols = new[] { BigTopLook.Gold, BigTopLook.Pink, BigTopLook.Teal, BigTopLook.TopWhite };
            for (int i = 0; i < 4; i++) { var fw = new GameObject("burst " + i); fw.transform.SetParent(conf.transform, false); fw.transform.localPosition = new Vector3(i * 2, i, (i % 2 == 0 ? 1 : -1) * 2); var f = fw.AddComponent<Firework>(); f.colour = cols[i]; f.radius = 9; f.every = 2.6f; f.delay = i * .2f; }
            var cg = new GameObject("Giant cannon (set-piece)"); cg.transform.SetParent(scen, false); cg.transform.position = (cannonP.p[0] + cannonP.p[n - 1]) * .5f + Vector3.up * 2;
            SetPiece.Make(cg, "Giant cannon", "The Big Top", "btcannon", Vector3.zero, new Vector3(46, 16, 14));
        }

        // ---------------- the river and the lake: banks, the rapids' rocks, the sunken wagon, the escape artist's tank ----------------
        static void RiverAndLake()
        {
            var rock = BigTopLook.Mat(BigTopLook.Grey, true);
            // rocks in the Island rapids' channel, either side of its line (a boat steers between them)
            foreach (var (x, z, r) in new[] { (891f, 178f, 4f), (861f, 128f, 3f), (893f, 72f, 4f), (861f, 22f, 3f), (935f, 150f, 6f), (940f, 60f, 5f) }) Cyl(root, "Rapids rock", L(x, (Floor + 2.5f) * .5f, z), r, 2.5f - Floor, rock, true, ShiftVehicle.ObstacleLayer);
            // reeds along the banks, little jetties (Detail)
            var reeds = new MeshKit.Batch(); var rnd = new System.Random(41);
            for (float z = -280; z < 1350; z += 9) foreach (float x in z > -25 && z < 265 ? new[] { 762f, 958f } : new[] { 802f, 918f }) if (rnd.NextDouble() < .6) reeds.Add(MeshKit.Cube, L(x + Rand(rnd, -1.5f, 1.5f), 1.2f, z + Rand(rnd, -3, 3)), Quaternion.Euler(Rand(rnd, -10, 10), Rand(rnd, 0, 90), 0), new Vector3(.3f, 2.4f, .3f), new Color(.45f, .6f, .3f));
            Detail(reeds.Object(scen, "Reeds", BigTopLook.Vc(false), false, false));
            // the sunken circus wagon the sub drives through (G7): solid walls and roof, open ends, wheels and stripes
            float wp = M("wagon"); var wq = At(wp); var wf = Facing(wp); var wr = Right(wp);
            var wood = BigTopLook.Mat(BigTopLook.TopRed * .75f, true); var trimM = BigTopLook.Mat(BigTopLook.Gold * .8f, true);
            foreach (float s in new[] { -1f, 1f }) Box(root, "Sunken wagon side", wq + wr * s * 10 + Vector3.up * 0, new Vector3(2, 12, 34), wood, wf, true, ShiftVehicle.ObstacleLayer);
            Box(root, "Sunken wagon roof", wq + Vector3.up * 6.8f, new Vector3(22, 1.6f, 34), BigTopLook.Mat(BigTopLook.TopWhite * .7f, true), wf, true, ShiftVehicle.ObstacleLayer);
            for (int i = 0; i < 5; i++) Box(scen, "Sunken wagon trim", wq + Vector3.up * 7.8f + Flat(Dir(wp)) * (-14 + i * 7), new Vector3(22.4f, .6f, 1), trimM, wf, false);
            foreach (float s in new[] { -1f, 1f }) foreach (float f in new[] { -11f, 11f }) { var wh = Cyl(scen, "Sunken wagon wheel", wq + wr * s * 11.4f + Flat(Dir(wp)) * f + Vector3.down * 5f, 3.2f, .6f, trimM, false); wh.transform.rotation = wf * Quaternion.Euler(0, 0, 90); }
            Label("CIRCUS", wq + Vector3.up * 3.5f + wr * 11.05f, Quaternion.LookRotation(wr), 2.4f, BigTopLook.Gold);
            // things on the lake bed (Detail): drums, giant balls, a seesaw
            var bed = new MeshKit.Batch();
            for (int i = 0; i < 26; i++) { float s = M("tank") + i * 22; var q = At(s) + Right(s) * ((i % 2 == 0 ? 1 : -1) * Rand(rnd, 14, 26)); q.y = Floor + 1.5f; if (i % 3 == 0) bed.Add(MeshKit.LowSphere, q + Vector3.up * 1.5f, Quaternion.identity, Vector3.one * 4, i % 2 == 0 ? BigTopLook.Pink : BigTopLook.Teal); else bed.Add(MeshKit.LowCylinder, q, Quaternion.Euler(Rand(rnd, 0, 80), 0, 0), new Vector3(3, 1.6f, 3), i % 2 == 0 ? BigTopLook.TopRed : BigTopLook.Royal); }
            Detail(bed.Object(scen, "Lake bed props", BigTopLook.Vc(true), false, false));
        }

        // ---------------- the splash pool: the sub comes up under its curtain, the ramp onto the midway road ----------------
        static void SplashPool()
        {
            var wall = BigTopLook.Mat(BigTopLook.Royal * .9f, true); var trim = BigTopLook.Mat(BigTopLook.Gold, true);
            const float zs = -497, zn = -404, hx = 24;
            foreach (float s in new[] { -1f, 1f }) { Box(root, "Pool wall", L(s * (hx + 1.5f), (Floor + 5.5f) * .5f, (zs + zn) * .5f), new Vector3(3, 5.5f - Floor, zn - zs), wall); Box(scen, "Pool wall trim", L(s * (hx + 1.5f), 5.7f, (zs + zn) * .5f), new Vector3(3.3f, .4f, zn - zs + .3f), trim, null, false); }
            Box(root, "Pool curtain", L(0, (5.5f - 4.5f) * .5f, zs), new Vector3(hx * 2 + 6, 5.5f + 4.5f, 3), wall);
            Box(scen, "Pool curtain trim", L(0, 5.7f, zs), new Vector3(hx * 2 + 6.3f, .4f, 3.3f), trim, null, false);
            float rf = M("rampFoot"), rl = M("rampLip"); var a = At(rf); a.y = -.6f; var b = At(rl); b.y = G + 3.7f;
            var d = b - a; var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            var ramp = Box(root, "Pool ramp", (a + b) * .5f - rot * Vector3.up * .5f, new Vector3(24, 1, d.magnitude + .4f), BigTopLook.Mat(new Color(.4f, .72f, .85f)), rot);
            ramp.AddComponent<WetRamp>();
            Box(root, "Pool ramp support", L(0, (b.y - 6 + Floor) * .5f, b.z - 2 - O.z), new Vector3(20, b.y - 6 - Floor, 8), BigTopLook.Mat(BigTopLook.WoodDark), null, false);
            // a striped arch over the ramp with a water curtain; the landing's chevrons
            for (int k = 0; k < 7; k++) foreach (float s in new[] { -1f, 1f }) Box(scen, "Splash arch post", L(s * 15, 3 + k * 2, -392), new Vector3(1.4f, 2, 1.4f), BigTopLook.Mat(k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite, true), null, false);
            Box(scen, "Splash arch", L(0, 16.6f, -392), new Vector3(32, 2.4f, 2), BigTopLook.Mat(BigTopLook.Teal, true), null, false);
            var jet = GameObject.CreatePrimitive(PrimitiveType.Quad); jet.name = "Splash curtain"; jet.transform.SetParent(scen, false); UnityEngine.Object.Destroy(jet.GetComponent<Collider>());
            jet.transform.position = L(0, 10, -393); jet.transform.localScale = new Vector3(26, 13, 1); jet.GetComponent<MeshRenderer>().sharedMaterial = JurassicLook.FallMat(.28f);
            var archGo = new GameObject("Splash arch (set-piece)"); archGo.transform.SetParent(scen, false); archGo.transform.position = L(0, 10, -392);
            SetPiece.Make(archGo, "Splash arch", "Ring Return", "btpool", Vector3.zero, new Vector3(34, 16, 3));
            var lc = new MeshKit.Batch(); for (int i = 0; i < 5; i++) lc.Add(MeshKit.Cube, L(0, G + .05f, -340 + i * 7), Quaternion.Euler(0, i % 2 == 0 ? 20 : -20, 0), new Vector3(9, .05f, 1.2f), new Color(.95f, .5f, .1f));
            lc.Object(scen, "Landing chevrons", BigTopLook.Vc(false), false, false);
        }
    }
}
