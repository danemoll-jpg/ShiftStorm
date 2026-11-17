using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 16 item 2: Dam Shame's world (the race logic is in DamShame.cs). The ground is one heightfield (the canyon floor, its walls, the
    // reservoir's bed) with the roads pressed into it (flush, no walls to be stranded behind: Round 10's rule): the climb road is cut into
    // the canyon's west wall, the chute sits on an embankment against the east wall. Solid: the dam's body and the crest's parapets (a drop on
    // both sides), the chute's walls (a channel), the turbine hall's walls and roof, the bridge's deck. The rest is looks only.
    public static partial class DamShame
    {
        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, Quaternion? rot = null, bool collider = true, int layer = 0)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
            g.transform.SetPositionAndRotation(center, rot ?? Quaternion.identity); g.transform.localScale = size; g.layer = layer;
            g.GetComponent<Renderer>().sharedMaterial = mat; if (!collider) UnityEngine.Object.Destroy(g.GetComponent<Collider>());
            return g;
        }
        static List<MeshKit.Sample> Samples(float a, float b, float half, Func<float, float> top, float depth, float step = 3f)
        {
            var list = new List<MeshKit.Sample>(); int n = Mathf.Max(1, Mathf.CeilToInt((b - a) / step));
            for (int i = 0; i <= n; i++)
            {
                float p = Mathf.Lerp(a, b, i / (float)n); var pt = At(p); float y = top(p);
                list.Add(new MeshKit.Sample { c = pt, right = Right(p), l = -half, r = half, top = y, bottom = y - depth });
            }
            return list;
        }
        static Mesh Grid(float sx, float sz, int nx, int nz)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int z = 0; z <= nz; z++) for (int x = 0; x <= nx; x++) v.Add(new Vector3(-sx * .5f + x * sx / nx, 0, -sz * .5f + z * sz / nz));
            for (int z = 0; z < nz; z++) for (int x = 0; x < nx; x++) { int i = z * (nx + 1) + x; t.Add(i); t.Add(i + nx + 1); t.Add(i + 1); t.Add(i + 1); t.Add(i + nx + 1); t.Add(i + nx + 2); }
            var m = new Mesh { name = "water grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static GameObject Detail(GameObject g) { if (g) g.layer = ParadisePlunge.DetailLayer; return g; }
        static float Rand(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }

        // ---------------- the ground: one heightfield ----------------
        public const float WallW = -440f, WallE = 448f, RiverHalf = 22f;
        // the canyon without the roads: the floor (y 0), the walls rising either side to ~150 m, the reservoir's bed behind the dam
        static float Base(float x, float z)
        {
            float n = Mathf.PerlinNoise(x * .011f + 3.1f, z * .011f + 7.7f) - .5f, n2 = Mathf.PerlinNoise(x * .04f, z * .04f) - .5f;
            float dw = WallW - x, de = x - WallE;
            float wall = Mathf.Max(dw, de);
            float h = wall > 0 ? Mathf.Min(165f, Smooth(0, 110, wall) * 150f + wall * .15f) + n * 30f * Smooth(10, 60, wall) + n2 * 6f * Smooth(0, 30, wall) : 0;
            // the rock shoulders the dam stands between (crest height, sloping away downstream)
            if (z > -150 && z < 80 && (x < DamX0 + 5 || x > DamX1 - 5)) { float s = Smooth(-150, -30, z) * (z > 40 ? Smooth(80, 40, z) : 1); h = Mathf.Max(h, CrestY * s * Smooth(0, 30, Mathf.Max(DamX0 + 5 - x, x - (DamX1 - 5)))); }
            if (z > DamTop) h = Mathf.Max(wall > 0 ? h : -6f, h - 6f);   // the reservoir's bed
            // the river's channel down the middle of the valley, from the dam's toe
            if (z < DamToe - 4 && wall <= 0) { float d = Mathf.Abs(x - RiverX(z)); if (d < RiverHalf + 8) h = Mathf.Min(h, Mathf.Lerp(-5f, 0, Smooth(RiverHalf - 4, RiverHalf + 8, d))); }
            return h;
        }
        public static float RiverX(float z) => Mathf.Sin(z * .004f) * 18f;
        // the line's road samples (lap 1), for pressing the roads into the ground
        static List<(Vector2 p, float y, float half)> roadPts;
        static List<Vector2> nearPts;
        static void RoadPoints()
        {
            roadPts = new List<(Vector2, float, float)>(); nearPts = new List<Vector2>();
            for (float s = M("start") - 40; s < M("F@2"); s += 4)
            {
                var q = At(s) - O; nearPts.Add(new Vector2(q.x, q.z));
                float y = RoadY(s); if (float.IsNaN(y)) continue;
                bool onDam = q.x > DamX0 - 2 && q.x < DamX1 + 2 && q.z > -DamTop - 6 && q.z < DamTop + 6;
                if (onDam) continue;
                bool chute = s > M("chuteTop") - 10 && s < M("lip") + 1;
                roadPts.Add((new Vector2(q.x, q.z), y - .35f, chute ? ChuteHalf + 2 : RoadHalf + 3));
            }
            buckets.Clear();
            foreach (var r in roadPts) { var key = (Mathf.FloorToInt(r.p.x / Bucket), Mathf.FloorToInt(r.p.y / Bucket)); if (!buckets.TryGetValue(key, out var l)) buckets[key] = l = new List<(Vector2, float, float)>(); l.Add(r); }
        }
        const float Bucket = 50f;
        static readonly Dictionary<(int, int), List<(Vector2 p, float y, float half)>> buckets = new Dictionary<(int, int), List<(Vector2 p, float y, float half)>>();
        public static float GroundAt(float x, float z)
        {
            float h = Base(x, z);
            float bestD = 1e9f, bestY = 0, bestHalf = 0;
            int bx = Mathf.FloorToInt(x / Bucket), bz = Mathf.FloorToInt(z / Bucket);
            for (int i = -2; i <= 2; i++) for (int k = -2; k <= 2; k++)
                if (buckets.TryGetValue((bx + i, bz + k), out var list))
                    foreach (var r in list) { float d = (r.p - new Vector2(x, z)).sqrMagnitude; if (d < bestD) { bestD = d; bestY = r.y; bestHalf = r.half; } }
            bestD = Mathf.Sqrt(bestD);
            if (bestD < 100)
            {
                float sh = Mathf.Clamp(Mathf.Abs(h - bestY) * 1.4f, 10f, 70f);
                float t = Smooth(bestHalf, bestHalf + sh, bestD);
                h = Mathf.Lerp(bestY, h, t);
            }
            // the plunge pool, the river under the bridge (the deck spans it), the turbine hall's floor
            if (x > PoolX0 && x < PoolX1 && z < PoolZ0 && z > PoolZ1) h = Mathf.Min(h, -7f);
            if (z < DamToe - 4 && Mathf.Abs(x - RiverX(z)) < RiverHalf && Mathf.Abs(z + 805) < 30) h = Mathf.Min(h, -5f);
            if (x > HallX0 - 4 && x < HallX1 + 4 && z > HallZ0 - 4 && z < HallZ1 + 4) h = Mathf.Min(h, .05f);
            return h;
        }
        static void Terrain()
        {
            RoadPoints();
            float x0 = -820, x1 = 830, z0 = -1250, z1 = 900; const float cell = 7f;
            int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
            // in tiles (culling, collider size)
            const int tile = 40;
            var grass = new Color(.36f, .55f, .27f); var pine = new Color(.2f, .4f, .24f); var rock = new Color(.55f, .52f, .5f); var rockDark = new Color(.42f, .4f, .4f); var gravel = new Color(.62f, .58f, .5f);
            var mat = DamLook.Vc(true);
            float[,] H = new float[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++) for (int k = 0; k <= nz; k++) H[i, k] = GroundAt(x0 + i * cell, z0 + k * cell);
            for (int ti = 0; ti < nx; ti += tile)
                for (int tk = 0; tk < nz; tk += tile)
                {
                    int ni = Mathf.Min(tile, nx - ti), nk = Mathf.Min(tile, nz - tk);
                    var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
                    for (int k = 0; k <= nk; k++) for (int i = 0; i <= ni; i++)
                    {
                        int gi = ti + i, gk = tk + k; float x = x0 + gi * cell, z = z0 + gk * cell, y = H[gi, gk];
                        v.Add(O + new Vector3(x, y, z));
                        float sx = H[Mathf.Min(gi + 1, nx), gk] - H[Mathf.Max(gi - 1, 0), gk], sz = H[gi, Mathf.Min(gk + 1, nz)] - H[gi, Mathf.Max(gk - 1, 0)];
                        float slope = new Vector2(sx, sz).magnitude / (2 * cell);
                        float n = Mathf.PerlinNoise(x * .05f, z * .05f);
                        Color cc = slope > .9f ? Color.Lerp(rock, rockDark, n) : slope > .45f ? Color.Lerp(pine, rock, (slope - .45f) / .45f) : Color.Lerp(grass, pine, n * .8f);
                        if (y < -1.5f) cc = Color.Lerp(gravel, rockDark, .3f);
                        if (y > 120) cc = Color.Lerp(cc, new Color(.92f, .93f, .96f), Smooth(120, 150, y) * (slope < 1.2f ? 1 : .4f));   // a dusting of snow on the tops
                        col.Add(cc);
                    }
                    for (int k = 0; k < nk; k++) for (int i = 0; i < ni; i++) { int a = k * (ni + 1) + i; tri.Add(a); tri.Add(a + ni + 1); tri.Add(a + 1); tri.Add(a + 1); tri.Add(a + ni + 1); tri.Add(a + ni + 2); }
                    var m = new Mesh { name = "canyon" }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
                    var g = new GameObject("Canyon ground"); g.transform.SetParent(root, false); g.layer = ShiftVehicle.ObstacleLayer;
                    g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterial = mat; g.AddComponent<MeshCollider>().sharedMesh = m;
                }
            // far peaks round the canyon
            var far = new MeshKit.Batch(); var rnd = new System.Random(23);
            for (int i = 0; i < 46; i++)
            {
                float a = i * Mathf.PI * 2 / 46, rr = Rand(rnd, 1500, 1900);
                var p = L(Mathf.Cos(a) * rr * .9f, -10, Mathf.Sin(a) * rr - 150); float h = Rand(rnd, 260, 520), d = Rand(rnd, 420, 640);
                far.Add(MeshKit.LowCone, p + Vector3.up * h * .5f, Quaternion.Euler(0, i * 37, 0), new Vector3(d, h, d), Color.Lerp(new Color(.36f, .44f, .5f), new Color(.5f, .56f, .62f), (float)rnd.NextDouble()));
                far.Add(MeshKit.LowCone, p + Vector3.up * h * .855f, Quaternion.Euler(0, i * 37, 0), new Vector3(d * .3f, h * .3f, d * .3f), new Color(.95f, .96f, .98f));
            }
            far.Object(scen, "Far peaks", DamLook.Vc(true), false, false);
        }

        // ---------------- the dam ----------------
        static void Dam()
        {
            var concrete = DamLook.Concrete; var dark = DamLook.ConcreteDark;
            // the body: crest 32 m wide at y 40, the upstream face straight down, the downstream face sloping to its toe
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            var sec = new[] { new Vector2(DamTop, Floor), new Vector2(DamTop, CrestY - .02f), new Vector2(-DamTop, CrestY - .02f), new Vector2(DamToe, Floor) };
            void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Color k) { int i = v.Count; v.Add(a); v.Add(b); v.Add(cc); v.Add(d); for (int q = 0; q < 4; q++) col.Add(k); tri.Add(i); tri.Add(i + 1); tri.Add(i + 2); tri.Add(i); tri.Add(i + 2); tri.Add(i + 3); }
            Vector3 P(float x, Vector2 s) => L(x, s.y, s.x);
            for (int f = 0; f < 4; f++)
            {
                var s0 = sec[f]; var s1 = sec[(f + 1) % 4];
                for (float x = DamX0; x < DamX1 - .01f; x += 20f)
                {
                    float xb = Mathf.Min(DamX1, x + 20f); var k = ((int)((x - DamX0) / 20f) % 2 == 0) ? concrete : Color.Lerp(concrete, dark, .25f);
                    Quad(P(x, s0), P(x, s1), P(xb, s1), P(xb, s0), k);
                }
            }
            // ends
            for (int e = 0; e < 2; e++) { float x = e == 0 ? DamX0 : DamX1; if (e == 0) Quad(P(x, sec[0]), P(x, sec[3]), P(x, sec[2]), P(x, sec[1]), dark); else Quad(P(x, sec[0]), P(x, sec[1]), P(x, sec[2]), P(x, sec[3]), dark); }
            var m = new Mesh { name = "dam" }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
            var g = new GameObject("The dam"); g.transform.SetParent(root, false); g.layer = ShiftVehicle.ObstacleLayer;
            g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterial = DamLook.Vc(true); g.AddComponent<MeshCollider>().sharedMesh = m;
            // the crest: parapets both sides (a drop either way), lamp posts, the control tower (Buck's window), the siren, the name
            var par = DamLook.Mat(concrete * .95f, true);
            // the reservoir side the whole length; the valley side only between the bends where the road leaves the crest (the dam road
            // comes up onto it at the west end, the road turns off it into the spillway at the east end)
            Box(root, "Crest parapet N", L((DamX0 + DamX1) * .5f, CrestY + .55f, DamTop - .6f), new Vector3(DamX1 - DamX0, 1.1f, 1.2f), par, null, true, ShiftVehicle.ObstacleLayer);
            Box(root, "Crest parapet S", L(0, CrestY + .55f, -(DamTop - .6f)), new Vector3(560, 1.1f, 1.2f), par, null, true, ShiftVehicle.ObstacleLayer);
            var deco = new MeshKit.Batch();
            for (float x = DamX0 + 20; x < DamX1 - 10; x += 40)
                foreach (float s in new[] { -1f, 1f })
                {
                    deco.Add(MeshKit.LowCylinder, L(x, CrestY + 4f, s * (DamTop - .6f)), Quaternion.identity, new Vector3(.35f, 8f, .35f), new Color(.25f, .27f, .3f));
                    deco.Add(MeshKit.Cube, L(x, CrestY + 8.1f, s * (DamTop - 1.6f)), Quaternion.identity, new Vector3(.7f, .3f, 2.2f), new Color(.25f, .27f, .3f));
                }
            // buttress ribs down the downstream face, the toe's stilling apron
            for (float x = DamX0 + 40; x < DamX1 - 20; x += 80)
                for (int k = 0; k < 8; k++) { float u = k / 8f; deco.Add(MeshKit.Cube, L(x, CrestY * (1 - u) - 2, Mathf.Lerp(-DamTop, DamToe, u) - .8f), Quaternion.Euler(-Mathf.Atan2(CrestY, DamToe + DamTop) * Mathf.Rad2Deg, 0, 0), new Vector3(4, 1.2f, 6.5f), dark); }
            deco.Object(scen, "Dam details", DamLook.Vc(true), false, true);
            var lamps = new MeshKit.Batch();
            for (float x = DamX0 + 20; x < DamX1 - 10; x += 40) foreach (float s in new[] { -1f, 1f }) lamps.Add(MeshKit.LowSphere, L(x, CrestY + 7.8f, s * (DamTop - 2.4f)), Quaternion.identity, Vector3.one * .8f, new Color(1f, .9f, .6f));
            lamps.Object(scen, "Crest lamps", GateMarker.Beacon, false, false);
            Tower();
            // the slogan painted down the dam's face (read from the valley), the name at the start
            var faceN = new Vector3(0, -(DamToe + DamTop), -CrestY).normalized * -1f; faceN = new Vector3(0, Mathf.Abs(faceN.y), -Mathf.Abs(faceN.z));   // the downstream face's outward normal
            float FaceZ(float y) => -DamTop + (CrestY - y) / CrestY * (DamToe + DamTop);
            Label("CONTROLLED WATER DEMONSTRATION", L(0, 24, FaceZ(24)) + faceN * .3f, Quaternion.LookRotation(faceN), 7.5f, DamLook.Yellow, false);
            Label("TODAY!", L(0, 11, FaceZ(11)) + faceN * .3f, Quaternion.LookRotation(faceN), 6f, DamLook.Yellow, false);
            var arch = At(M("F")); arch.y = CrestY;
            foreach (float s in new[] { -1f, 1f }) Box(scen, "Start arch post", arch + new Vector3(2, 7, s * (DamTop - 1.5f)), new Vector3(1.4f, 14, 1.4f), DamLook.Mat(DamLook.Safety, true), null, false);
            Box(scen, "Start arch board", arch + new Vector3(2, 14.5f, 0), new Vector3(1, 3.6f, DamTop * 2 - 1), DamLook.Mat(DamLook.Navy, true), null, false);
            Label("DAM SHAME", arch + new Vector3(1.4f, 14.5f, 0), Quaternion.LookRotation(Vector3.left), 2.4f, DamLook.Yellow, true);
            Label("DAM SHAME", arch + new Vector3(2.6f, 14.5f, 0), Quaternion.LookRotation(Vector3.right), 2.4f, DamLook.Yellow, true);
            var fin = arch;
            Box(root, "Finish line", fin + new Vector3(0, .04f, 0), new Vector3(2.2f, .06f, RoadHalf * 2), DamLook.Mat(new Color(.1f, .1f, .1f)), null, false);
            for (int i = 0; i < 13; i++) Box(root, "Finish check", fin + new Vector3((i % 2) * 1f - .5f, .08f, -RoadHalf + 1 + i * 2), new Vector3(1, .02f, 1), DamLook.Mat(Color.white), null, false);
        }
        // the control tower on the crest's upstream side: Buck waves from its window; the siren on its roof
        public static Transform BuckFigure { get; private set; }
        static void Tower()
        {
            float tx = -40; var b = new MeshKit.Batch(); var wall = DamLook.Concrete * 1.04f;
            b.Add(MeshKit.Cube, L(tx, CrestY + 9, DamTop + 7), Quaternion.identity, new Vector3(22, 18, 14), wall);
            b.Add(MeshKit.Cube, L(tx, CrestY + 18.6f, DamTop + 7), Quaternion.identity, new Vector3(24, 1.2f, 16), DamLook.Navy);
            b.Add(MeshKit.Cube, L(tx, CrestY + 21.5f, DamTop + 7), Quaternion.identity, new Vector3(14, 5, 10), wall);   // the control room
            b.Add(MeshKit.Cube, L(tx, CrestY + 24.3f, DamTop + 7), Quaternion.identity, new Vector3(16, .8f, 12), DamLook.Navy);
            b.Add(MeshKit.LowCylinder, L(tx + 5, CrestY + 26.5f, DamTop + 7), Quaternion.identity, new Vector3(1.2f, 1.6f, 1.2f), new Color(.3f, .3f, .32f));
            b.Add(MeshKit.LowCone, L(tx + 5, CrestY + 28.4f, DamTop + 6), Quaternion.Euler(-90, 0, 0), new Vector3(2.4f, 1.6f, 2.4f), DamLook.Safety);   // the siren's horn
            b.Object(scen, "Control tower", DamLook.Vc(true), false, true);
            var win = new MeshKit.Batch();
            win.Add(MeshKit.Cube, L(tx, CrestY + 21.6f, DamTop + 1.9f), Quaternion.identity, new Vector3(11, 3, .3f), new Color(.75f, .9f, 1f));
            for (int i = 0; i < 4; i++) win.Add(MeshKit.Cube, L(tx - 7 + i * 4.6f, CrestY + 10, DamTop - .1f + .05f), Quaternion.identity, new Vector3(2.4f, 3, .3f), new Color(1f, .9f, .6f));
            win.Object(scen, "Tower windows", GateMarker.Beacon, false, false);
            // Buck in the window (a stand-in: Docs/MODELS-NEEDED.md has his look), waving
            var bk = new GameObject("Buck at the window"); bk.transform.SetParent(scen, false); bk.transform.position = L(tx, CrestY + 19.2f, DamTop + 2.6f); bk.transform.rotation = Quaternion.LookRotation(Vector3.back);
            var fb = new MeshKit.Batch(); float k = 1.6f;
            fb.Add(MeshKit.LowSphere, new Vector3(0, 1.1f, 0) * k, Quaternion.identity, new Vector3(.9f, .9f, .6f) * k, new Color(.85f, .3f, .2f));
            fb.Add(MeshKit.LowSphere, new Vector3(0, 1.75f, 0) * k, Quaternion.identity, new Vector3(.45f, .48f, .45f) * k, new Color(.95f, .76f, .6f));
            fb.Add(MeshKit.LowSphere, new Vector3(0, 1.66f, .2f) * k, Quaternion.identity, new Vector3(.5f, .1f, .16f) * k, Color.white);   // the mustache
            fb.Add(MeshKit.LowCylinder, new Vector3(0, 2.05f, 0) * k, Quaternion.identity, new Vector3(.62f, .04f, .62f) * k, new Color(.15f, .12f, .15f));
            fb.Add(MeshKit.LowCylinder, new Vector3(0, 2.22f, 0) * k, Quaternion.identity, new Vector3(.38f, .18f, .38f) * k, new Color(.15f, .12f, .15f));
            fb.Object(bk.transform, "Buck (stand-in)", DamLook.Vc(true), false, false);
            var arm = new GameObject("arm"); arm.transform.SetParent(bk.transform, false); arm.transform.localPosition = new Vector3(.45f, 1.35f, 0) * k;
            var ab = new MeshKit.Batch(); ab.Add(MeshKit.LowCylinder, new Vector3(0, .35f, 0) * k, Quaternion.identity, new Vector3(.14f, .35f, .14f) * k, new Color(.85f, .3f, .2f)); ab.Add(MeshKit.LowSphere, new Vector3(0, .75f, 0) * k, Quaternion.identity, Vector3.one * .18f * k, new Color(.95f, .76f, .6f));
            ab.Object(arm.transform, "waving arm", DamLook.Vc(true), false, false);
            var sw = arm.AddComponent<Swing>(); sw.deg = 28; sw.period = 1.1f; arm.transform.localRotation = Quaternion.Euler(0, 0, -20); BuckFigure = bk.transform;
            SetPiece.Make(bk, "Buck at the window", "", "dmbuck", new Vector3(0, 2, 0), new Vector3(14, 7, 4)).range = 260;
        }

        // ---------------- the roads ----------------
        static void Roads()
        {
            var asphalt = DamLook.Mat(DamLook.Asphalt); var side = DamLook.Mat(DamLook.ConcreteDark);
            // the crest and the abutments, the dam road; the valley road from the landing round to the climb (the bridge's deck over the river)
            float s0 = M("start") - 60, s1 = M("chuteTop") - 6;
            MeshKit.Strip(root, "Crest road", Samples(s0, s1, RoadHalf, p => RoadY(p), 1.5f), asphalt, side);
            float v0 = M("lip") + PoolGap, v1 = M("F@2");
            MeshKit.Strip(root, "Valley and dam road", Samples(v0, v1 + 70, RoadHalf, p => RoadY(p), 1.6f), asphalt, side);
            // the bridge: arches under its deck over the river, posts (looks only)
            var bq = At(M("bridge")); var br = new MeshKit.Batch();
            for (int i = -1; i <= 1; i++) br.Add(MeshKit.Cube, new Vector3(bq.x + RiverX(bq.z - O.z) + i * 16, -3f, bq.z), Quaternion.identity, new Vector3(3, 7, RoadHalf * 2 + 1), DamLook.ConcreteDark);
            foreach (float s in new[] { -1f, 1f }) for (int i = -6; i <= 6; i++) br.Add(MeshKit.Cube, new Vector3(bq.x + i * 4, RoadY(M("bridge")) + .5f, bq.z + s * (RoadHalf + .3f)), Quaternion.identity, new Vector3(.4f, 1f, .4f), DamLook.Safety);
            br.Object(scen, "Bridge", DamLook.Vc(true), false, true);
            // the start / finish arch is on the crest (Dam()); the kerb lines along every road: RouteMarks()
        }

        // ---------------- the spillway chute and the flip bucket ----------------
        public static GameObject ChuteFloor { get; private set; }
        static readonly List<GameObject> torrent = new List<GameObject>();
        public static IReadOnlyList<GameObject> Torrent => torrent;
        static void Chute()
        {
            float a = M("chuteTop") - 8, b = M("lip");
            var floor = DamLook.Mat(DamLook.Concrete); var wall = DamLook.Mat(DamLook.ConcreteDark, true);
            var parts = MeshKit.Strip(root, "Spillway chute", Samples(a, b, ChuteHalf, p => RoadY(p), 2.5f, 2f), floor, wall);
            ChuteFloor = parts.Count > 0 ? parts[0] : null;
            foreach (var g in parts) { g.layer = 0; var w = g.AddComponent<WetRamp>(); w.enabled = false; }   // a boat hovers down it on its hull once the water runs (DamWater)
            // its walls (a channel: nothing beyond them but the drop to the valley or the canyon's wall)
            foreach (float s in new[] { -1f, 1f })
            {
                var list = new List<MeshKit.Sample>(); int n = Mathf.CeilToInt((b - a) / 3f);
                for (int i = 0; i <= n; i++) { float p = Mathf.Lerp(a, b - 2, i / (float)n); var pt = At(p); float y = RoadY(p); list.Add(new MeshKit.Sample { c = pt + Right(p) * s * (ChuteHalf + .8f), right = Right(p), l = -.8f, r = .8f, top = y + ChuteWallH, bottom = y - 2.5f }); }
                MeshKit.Strip(root, "Chute wall", list, wall, wall, true, ShiftVehicle.ObstacleLayer);
            }
            // the torrent: a sheet of white water down the chute and spray off the lip (shown once the gates open)
            var sheetSamples = Samples(a, b - 1, ChuteHalf - .4f, p => RoadY(p) + .35f, .2f, 3f);
            foreach (var g in MeshKit.Strip(scen, "Spillway torrent", sheetSamples, JurassicLook.WaterMat(new Color(.55f, .78f, .8f), DamLook.Foam, Color.white, .35f, 2f), null, false, 0, 40, false)) torrent.Add(g);
            var lq = At(b); lq.y = RoadY(b - .5f) + .5f;
            var spray = Spray.Make(scen, "Flip bucket spray", lq, Facing(b) * Quaternion.Euler(-28, 0, 0), ChuteHalf * 1.8f, 160); torrent.Add(spray.gameObject);
            var foot = Spray.Make(scen, "Plunge pool mist", L(405, 1, -540), Quaternion.Euler(-80, 0, 0), 50, 90); foot.speed = new Vector2(3, 7); torrent.Add(foot.gameObject);
            // the set-piece box: the chute's lower half and the spray
            var sp = new GameObject("Spillway torrent (set-piece)"); sp.transform.SetParent(scen, false); sp.transform.position = L(405, 14, -400);
            var spc = SetPiece.Make(sp, "Spillway torrent", "Lap 2: A smidge", "dmspill", Vector3.zero, new Vector3(ChuteHalf * 2, 22, 170)); spc.showing = () => DamWater.Stage >= 1; spc.range = 500;
            // the radial gates at the chute's head (they swing up when Buck opens them: DamWater)
            var gates = new GameObject("Spillway gates"); gates.transform.SetParent(scen, false);
            float gq = M("chuteTop") - 30; var gp = At(gq); gp.y = CrestY;
            var frame = new MeshKit.Batch();
            foreach (float s in new[] { -1f, 1f }) frame.Add(MeshKit.Cube, gp + Right(gq) * s * (ChuteHalf + 3) + Vector3.up * 9, Facing(gq), new Vector3(3, 18, 6), DamLook.ConcreteDark);
            frame.Add(MeshKit.Cube, gp + Vector3.up * 18.5f, Facing(gq), new Vector3(ChuteHalf * 2 + 9, 2.2f, 7), DamLook.Concrete);
            frame.Object(gates.transform, "gate piers", DamLook.Vc(true), false, true);
            for (int i = 0; i < 3; i++)
            {
                var leaf = new GameObject("gate leaf " + i); leaf.transform.SetParent(gates.transform, false);
                leaf.transform.SetPositionAndRotation(gp + Right(gq) * ((i - 1) * (ChuteHalf * 2 / 3f)) + Vector3.up * 16 - Flat(Dir(gq)) * 4, Facing(gq));
                var lb = new MeshKit.Batch(); lb.Add(MeshKit.Cube, new Vector3(0, -8f, 4f), Quaternion.Euler(-30, 0, 0), new Vector3(ChuteHalf * 2 / 3f - .6f, 9f, .7f), DamLook.Safety);
                for (int r = 0; r < 4; r++) lb.Add(MeshKit.Cube, new Vector3(0, -4.5f - r * 2f, 2.2f + r * 1.1f), Quaternion.Euler(-30, 0, 0), new Vector3(ChuteHalf * 2 / 3f - .4f, .35f, .5f), DamLook.Steel);
                lb.Object(leaf.transform, "leaf", DamLook.Vc(true), false, false);
                GateLeaves.Add(leaf.transform);
            }
            // the gates are over the crest road's bend into the chute, 16 m up: a car drives under them
            var gsp = new GameObject("Spillway gates (set-piece)"); gsp.transform.SetParent(scen, false); gsp.transform.position = gp + Vector3.up * 10;
            var gsc = SetPiece.Make(gsp, "Spillway gates", "Lap 2: A smidge", "dmgates", Vector3.zero, new Vector3(ChuteHalf * 2 + 9, 20, 8)); gsc.range = 400; gsc.showing = () => DamWater.Stage >= 1;   // open only from lap 2's water
            // the kicker's edge
            var kq = At(b); Box(scen, "Lip edge", new Vector3(kq.x, RoadY(b - .5f) + .05f, kq.z) - Flat(Dir(b)) * .6f, new Vector3(ChuteHalf * 2, .08f, 1.2f), DamLook.Glow(DamLook.Safety), Facing(b), false);
        }
        public static readonly List<Transform> GateLeaves = new List<Transform>();

        // ---------------- the valley: the river, the pool, the valley's water ----------------
        public static WaterVolume ValleyWater { get; private set; }
        public static Transform ValleySurface { get; private set; }
        static void Valley()
        {
            ValleyWater = Vol<WaterVolume>("Valley water", L(0, (ValleyLevel[0] + Floor) * .5f, -660), Quaternion.identity, new Vector3(WallE - WallW + 80, ValleyLevel[0] - Floor, 1170));
            Vol<WaterVolume>("Plunge pool water", L((PoolX0 + PoolX1) * .5f, (PoolLevel + Floor) * .5f, (PoolZ0 + PoolZ1) * .5f), Quaternion.identity, new Vector3(PoolX1 - PoolX0, PoolLevel - Floor, PoolZ0 - PoolZ1));
            Box(root, "Valley bed", L(0, Floor - 1, -660), new Vector3(WallE - WallW + 200, 2, 1300), DamLook.Mat(DamLook.ConcreteDark * .7f));
            var w = new GameObject("Valley water (visual)"); w.transform.SetParent(root, false); w.AddComponent<MeshFilter>().sharedMesh = Grid(WallE - WallW + 160, 1300, 60, 80); w.transform.position = L(4, ValleyLevel[0] - .02f, -680);
            var wr = w.AddComponent<MeshRenderer>(); wr.sharedMaterial = JurassicLook.WaterMat(DamLook.Water, DamLook.WaterLight, DamLook.Foam, .1f); wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ValleySurface = w.transform;
            var pw = new GameObject("Plunge pool (visual)"); pw.transform.SetParent(root, false); pw.AddComponent<MeshFilter>().sharedMesh = Grid(PoolX1 - PoolX0, PoolZ0 - PoolZ1, 8, 8); pw.transform.position = L((PoolX0 + PoolX1) * .5f, PoolLevel - .02f, (PoolZ0 + PoolZ1) * .5f);
            var pr = pw.AddComponent<MeshRenderer>(); pr.sharedMaterial = JurassicLook.WaterMat(DamLook.Water, DamLook.WaterLight, DamLook.Foam, .2f); pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // the pool's concrete walls (looks: a car that falls in is reset by the zone)
            var pb = new MeshKit.Batch();
            pb.Add(MeshKit.Cube, L((PoolX0 + PoolX1) * .5f, -3, PoolZ1 - .6f), Quaternion.identity, new Vector3(PoolX1 - PoolX0 + 2, 7.6f, 1.2f), DamLook.ConcreteDark);
            foreach (float x in new[] { PoolX0, PoolX1 }) pb.Add(MeshKit.Cube, L(x, -3, (PoolZ0 + PoolZ1) * .5f), Quaternion.identity, new Vector3(1.2f, 7.6f, PoolZ0 - PoolZ1), DamLook.ConcreteDark);
            pb.Object(scen, "Plunge pool walls", DamLook.Vc(true), false, false);
        }

        // ---------------- the turbine hall ----------------
        static void Hall()
        {
            var wall = DamLook.Mat(DamLook.Concrete * 1.02f, true); var roof = DamLook.Mat(DamLook.Navy * 1.1f, true);
            float zc = (HallZ0 + HallZ1) * .5f, len = HallZ1 - HallZ0;
            Box(root, "Hall wall W", L(HallX0 - .8f, HallRoof * .5f, zc), new Vector3(1.6f, HallRoof, len), wall, null, true, ShiftVehicle.ObstacleLayer);
            Box(root, "Hall wall E", L(HallX1 + .8f, HallRoof * .5f, zc), new Vector3(1.6f, HallRoof, len), wall, null, true, ShiftVehicle.ObstacleLayer);
            Box(root, "Hall roof", L((HallX0 + HallX1) * .5f, HallRoof + .8f, zc), new Vector3(HallX1 - HallX0 + 3.2f, 1.6f, len + 2), roof, null, true, ShiftVehicle.ObstacleLayer);
            // the doors' frames and a sign over each end
            var b = new MeshKit.Batch();
            foreach (float z in new[] { HallZ0, HallZ1 })
            {
                b.Add(MeshKit.Cube, L((HallX0 + HallX1) * .5f, HallRoof - 1.2f, z), Quaternion.identity, new Vector3(HallX1 - HallX0 + 3, 2.4f, 1.2f), DamLook.Safety);
                for (int i = 0; i < 8; i++) b.Add(MeshKit.Cube, L(HallX0 + 4 + i * 9.4f, HallRoof - 2.6f, z), Quaternion.Euler(0, 0, 45), new Vector3(1.6f, 1.6f, 1.3f), i % 2 == 0 ? new Color(.1f, .1f, .1f) : DamLook.Yellow);
            }
            // the overhead crane rail, the windows' light strips down the walls
            b.Add(MeshKit.Cube, L(-370, HallRoof - 1.5f, zc), Quaternion.identity, new Vector3(1.2f, 1.2f, len - 4), DamLook.Safety);
            b.Object(scen, "Hall dressing", DamLook.Vc(true), false, false);
            var lights = new MeshKit.Batch();
            for (float z = HallZ0 + 8; z < HallZ1 - 4; z += 16) { lights.Add(MeshKit.Cube, L((HallX0 + HallX1) * .5f, HallRoof - .3f, z), Quaternion.identity, new Vector3(30, .3f, 1.4f), new Color(1f, .95f, .8f)); foreach (float x in new[] { HallX0 + .2f, HallX1 - .2f }) lights.Add(MeshKit.Cube, L(x, 6, z), Quaternion.identity, new Vector3(.2f, 4, 6), new Color(.75f, .9f, 1f)); }
            lights.Object(scen, "Hall lights", GateMarker.Beacon, false, false);
            Label("TURBINE HALL", L(-388, HallRoof - 1.2f, HallZ0 - .7f), Quaternion.LookRotation(Vector3.back), 1.4f, new Color(.1f, .1f, .1f), false);
            // the giant turbines: five generators in a row on the hall's east side, their tops turning
            var gen = new MeshKit.Batch(); var turbines = new GameObject("Turbines"); turbines.transform.SetParent(scen, false);
            for (int i = 0; i < 5; i++)
            {
                float z = HallZ0 + 18 + i * 32; var at = L(-369, 0, z);
                gen.Add(MeshKit.LowCylinder, at + Vector3.up * 2.5f, Quaternion.identity, new Vector3(16, 5f, 16), DamLook.ConcreteDark);
                gen.Add(MeshKit.LowCylinder, at + Vector3.up * 5.8f, Quaternion.identity, new Vector3(13, 1.6f, 13), DamLook.Yellow);
                var top = new GameObject("rotor " + i); top.transform.SetParent(turbines.transform, false); top.transform.position = at + Vector3.up * 6.9f;
                var tb = new MeshKit.Batch(); tb.Add(MeshKit.LowCylinder, Vector3.zero, Quaternion.identity, new Vector3(9, .6f, 9), DamLook.Safety);
                for (int k = 0; k < 6; k++) tb.Add(MeshKit.Cube, Quaternion.Euler(0, k * 60, 0) * new Vector3(3.2f, .8f, 0), Quaternion.Euler(0, k * 60, 0), new Vector3(3.4f, .5f, .9f), DamLook.Steel);
                tb.Object(top.transform, "rotor", DamLook.Vc(true), false, false); var spin = top.AddComponent<Spin>(); spin.degPerSec = 70 + i * 6;
            }
            gen.Object(turbines.transform, "generators", DamLook.Vc(true), false, true);
            var tg = new GameObject("Turbines (set-piece)"); tg.transform.SetParent(scen, false); tg.transform.position = L(-369, 5, (HallZ0 + HallZ1) * .5f);
            SetPiece.Make(tg, "Giant turbines", "", "dmturbine", Vector3.zero, new Vector3(16, 10, len - 10)).range = 200;
        }

        // ---------------- the reservoir behind the dam ----------------
        static void Reservoir()
        {
            Vol<WaterVolume>("Reservoir", L(0, (LakeLevel - 8) * .5f, 640), Quaternion.identity, new Vector3(1600, LakeLevel + 8, 1240));
            var w = new GameObject("Reservoir (visual)"); w.transform.SetParent(root, false); w.AddComponent<MeshFilter>().sharedMesh = Grid(1500, 1200, 40, 40); w.transform.position = L(0, LakeLevel - .02f, 620);
            var wr = w.AddComponent<MeshRenderer>(); wr.sharedMaterial = JurassicLook.WaterMat(DamLook.Lake, DamLook.WaterLight, DamLook.Foam, .06f); wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // buoys along the dam's upstream face
            var bb = new MeshKit.Batch(); for (float x = DamX0 + 20; x < DamX1; x += 22) bb.Add(MeshKit.LowSphere, L(x, LakeLevel + .3f, DamTop + 26), Quaternion.identity, Vector3.one * 1.4f, ((int)(x / 22)) % 2 == 0 ? DamLook.Safety : Color.white);
            bb.Object(scen, "Boom buoys", DamLook.Vc(true), false, false);
        }

        // ---------------- pines and rocks ----------------
        static bool NearLine(float x, float z, float d)
        {
            foreach (var q in nearPts) if ((q - new Vector2(x, z)).sqrMagnitude < d * d) return true;
            if (x > HallX0 - 20 && x < HallX1 + 20 && z > HallZ0 - 20 && z < HallZ1 + 20) return true;
            if (x > DamX0 - 10 && x < DamX1 + 10 && z > DamToe - 10 && z < DamTop + 30) return true;
            if (x > PoolX0 - 30 && x < PoolX1 + 30 && z > PoolZ1 - 20 && z < PoolZ0 + 20) return true;
            return false;
        }
        static void Forest()
        {
            var trees = new MeshKit.Batch(); var rocks = new MeshKit.Batch(); var rnd = new System.Random(31);
            void Pine(Vector3 p, float h)
            {
                var g = Color.Lerp(DamLook.Pine, DamLook.PineLight, (float)rnd.NextDouble() * .7f);
                trees.Add(MeshKit.LowCylinder, p + Vector3.up * h * .13f, Quaternion.identity, new Vector3(.8f, h * .26f, .8f), DamLook.Bark);
                trees.Add(MeshKit.LowCone, p + Vector3.up * h * .42f, Quaternion.Euler(0, h * 13, 0), new Vector3(h * .42f, h * .32f, h * .42f), g * .92f);
                trees.Add(MeshKit.LowCone, p + Vector3.up * h * .68f, Quaternion.Euler(0, h * 29, 0), new Vector3(h * .32f, h * .26f, h * .32f), g);
            }
            int placed = 0;
            for (int i = 0; i < 2600 && placed < 900; i++)
            {
                float x = Rand(rnd, -760, 780), z = Rand(rnd, -1200, 820);
                if (NearLine(x, z, 34)) continue;
                if (z > DamTop && x > WallW + 10 && x < WallE - 10) continue;   // the reservoir
                float y = GroundAt(x, z); if (y < .5f && Mathf.Abs(x - RiverX(z)) < RiverHalf + 6) continue;
                if (y > 135) continue;
                Pine(L(x, y - .3f, z), Rand(rnd, 10, 22)); placed++;
            }
            for (int i = 0; i < 160; i++)
            {
                float x = Rand(rnd, -720, 740), z = Rand(rnd, -1150, 700); if (NearLine(x, z, 40)) continue; float y = GroundAt(x, z);
                rocks.Add(MeshKit.LowSphere, L(x, y + 1, z), Quaternion.Euler(Rand(rnd, 0, 40), Rand(rnd, 0, 360), 0), new Vector3(Rand(rnd, 6, 18), Rand(rnd, 4, 10), Rand(rnd, 6, 16)), Color.Lerp(DamLook.Rock, DamLook.RockDark, (float)rnd.NextDouble()));
            }
            Detail(trees.Object(scen, "Pines", DamLook.Vc(true), false, true)); Detail(rocks.Object(scen, "Rocks", DamLook.Vc(true), false, true));
            Debug.Log($"[Course] Dam Shame: {placed} pines");
        }

        // ---------------- lettering (blocky 5 x 7, bulbs or paint) ----------------
        static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" }, ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" }, ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" }, ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".####", "#....", "#....", "#.###", "#...#", "#...#", ".###." }, ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" }, ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" }, ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#" }, ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." }, ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." }, ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." }, ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", ".#.#.", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" }, ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['!'] = new[] { "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#.." },
        };
        static void Label(string text, Vector3 at, Quaternion rot, float height, Color col, bool glow)
        {
            var b = new MeshKit.Batch(); float step = height / 7f; var face = rot * Vector3.forward; var right = Vector3.Cross(face, Vector3.up).normalized; var up = Vector3.Cross(right, face).normalized;
            float width = (text.Length * 6 - 1) * step; float x0 = -width * .5f;
            for (int i = 0; i < text.Length; i++)
            {
                if (!Font.TryGetValue(text[i], out var g)) continue;
                for (int row = 0; row < 7; row++) for (int k = 0; k < 5; k++)
                    if (g[row][k] == '#') b.Add(MeshKit.Cube, at + right * (x0 + (i * 6 + k + .5f) * step) + up * ((3 - row) * step) + face * .25f, rot, new Vector3(step * .86f, step * .86f, step * .3f), col);
            }
            b.Object(scen, "Letters " + text, glow ? GateMarker.Beacon : DamLook.Vc(false), false, false);
        }
    }
}
