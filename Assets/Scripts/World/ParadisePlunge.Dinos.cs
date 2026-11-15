using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 8 item 1 (Dan: "if it is [Jurassic Par] there should be more dinos"): it is meant to be Jurassic Par, so dinosaurs are where a
    // player looks, in every segment, alive and moving, never on the racing line (each path is checked against the line at build time and
    // left out if it comes within the route's width + 12 m; the pterodactyls' circles keep 35 m from the line in 3D):
    //   Beach Promenade: a brontosaurus herd wading in the lake; raptors running alongside the road (they pace you); pterodactyls round the
    //     loop; a nest of eggs and three-toed footprints on the beach; long-necks grazing by the canyon road (the Lake Jump's car line)
    //   Cliffside Climb: a T. rex on the north cliff above the climb that roars as you come by (its roar, a camera shake); triceratops below
    //   Sky Arch: a herd walking in the valley under the flight, triceratops, pterodactyls swooping past the flight
    //   Lagoon Landing: a long-neck swimming in the lagoon (the plesiosaur stand-in), pterodactyls over the water
    //   Mangrove Run: triceratops on the banks, a nest of eggs
    //   Sunken Gateway: a brontosaurus on the ridge over the gateway
    //   Coral Vault: dinosaur skeletons and fossils in the cave walls, glowing (and a skeleton arch high over the cave's widest chamber)
    //   Waterfall Return: brontosaurs at the pool's edge, a triceratops by the finishing road, pterodactyls over it
    // The Round 2 / 5 / 7 standing dinosaurs (Dino()) are now the animated models too (breathing, looking round, tail sway).
    public static partial class ParadisePlunge
    {
        public const int DinoLayer = 0;
        public static readonly List<(string name, Vector3 at)> Fossils = new List<(string, Vector3)>();
        public static readonly List<string> DinoSkipped = new List<string>();
        static List<Vector3> linePts;

        // the nearest distance from p to the racer line (3D), and its horizontal part
        public static float LineDistance(Vector3 p, out float flat, out float prog)
        {
            if (linePts == null) linePts = new List<Vector3>(c.line);
            float best = 1e9f; flat = 1e9f; prog = 0; float acc = 0;
            for (int i = 0; i < linePts.Count; i++)
            {
                if (i > 0) acc += Vector3.Distance(linePts[i - 1], linePts[i]);
                float d = (linePts[i] - p).sqrMagnitude; if (d < best) { best = d; prog = acc; var q = linePts[i] - p; flat = new Vector2(q.x, q.z).magnitude; }
            }
            return Mathf.Sqrt(best);
        }
        static float GroundY(Vector3 p, float fallback)
        {
            if (Physics.Raycast(new Vector3(p.x, 300, p.z), Vector3.down, out var hit, 600, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return fallback;
        }
        static Vector3 OnGround(float x, float z, float fallback = 1.5f) { var p = new Vector3(x, 0, z); p.y = GroundY(p, fallback); return p; }

        // a path is kept only clear of the route: every sample at least the road's half width + 12 m away on the ground plan (or 35 m in 3D)
        static bool Clear(string label, IEnumerable<Vector3> pts, bool air)
        {
            foreach (var p in pts)
            {
                float d = LineDistance(p, out float flat, out float prog);
                bool ok = air ? d > 35f : (flat > HalfWidthSafe(prog) + 12f || Mathf.Abs(At(prog).y - p.y) > 25f);
                if (!ok) { DinoSkipped.Add($"{label}: {d:0} m from the line at {prog:0} m"); return false; }
            }
            return true;
        }
        static float HalfWidthSafe(float p) { try { return Mathf.Max(13f, HalfWidth(p)); } catch { return 17f; } }
        static IEnumerable<Vector3> Segment(Vector3 a, Vector3 b, int n = 6) { for (int i = 0; i <= n; i++) yield return Vector3.Lerp(a, b, i / (float)n); }
        static IEnumerable<Vector3> Circle(Vector3 c0, float r, float dip, int n = 24) { for (int i = 0; i < n; i++) { float t = i * Mathf.PI * 2 / n; yield return c0 + new Vector3(Mathf.Cos(t) * r, Mathf.Sin(t * 2) * dip, Mathf.Sin(t) * r); } }

        static Transform dinoRoot;
        static DinoActor Walker(string style, DinoActor.Kind kind, Vector3 a, Vector3 b, float h, string seg, string label, float speed = 1.2f)
        {
            if (!Clear($"{style} {label}", Segment(a, b), false)) return null;
            var d = DinoActor.Make(dinoRoot, style, kind, a, b, h, seg, label, Quaternion.LookRotation(Flat(b - a)).eulerAngles.y); d.speed = speed; return d;
        }
        static DinoActor Swooper(Vector3 centre, float r, float dip, float span, string seg, string label, float speed = 16f)
        {
            if (!Clear($"ptero {label}", Circle(centre, r, dip), true)) return null;
            var d = DinoActor.Make(dinoRoot, "ptero", DinoActor.Kind.Swoop, centre, new Vector3(r, 0, dip), span, seg, label); d.speed = speed; return d;
        }
        // a herd: n walkers side by side along a stretch
        static void Herd(string style, DinoActor.Kind kind, Vector3 a, Vector3 b, int n, float h, string seg, string label, float spread = 22f, bool ground = true, float yOff = 0)
        {
            var side = Vector3.Cross(Vector3.up, Flat(b - a));
            for (int i = 0; i < n; i++)
            {
                float s = (i - (n - 1) * .5f) * spread, along = (i % 2) * spread * .6f; var off = side * s + Flat(b - a) * along;
                Vector3 pa = a + off, pb = b + off;
                if (ground) { pa.y = GroundY(pa, pa.y) + yOff; pb.y = GroundY(pb, pb.y) + yOff; }
                Walker(style, kind, pa, pb, h * (1f - .12f * (i % 3)), seg, $"{label} {i + 1}", kind == DinoActor.Kind.Swim ? 2.5f : 1.2f);
            }
        }

        // ---- Round 8b: big, close, in front ----
        // Dan (build 9): "not really seeing dinosaurs". Round 8's were 15-19 m brontos out in the lake, a 12 m T. rex 30 m up a side cliff,
        // specks of pterodactyls: a few per cent of the screen for a blink. Now they are cartoon-huge (brontos 26-30 m, the T. rex 15 m,
        // trikes 5.5-6 m, pterodactyls 24-30 m wingspan) and stand where the camera looks: at the end of a straight, on the outside of a
        // bend (Spot), 15-60 m from the road's edge, facing the racers coming; and every segment has a set-piece (setPiece).
        const float Bronto = 28f, TRex = 15f, Trike = 5.5f, Raptor = 5f, PteroSpan = 30f, Plesio = 24f;
        // +1 = the line turns right over [p - 120, p + 80] (so the outside of the bend is on the left), -1 = left, 0 = about straight
        static float Turn(float p) { float t = Vector3.SignedAngle(Flat(Dir(p - 120)), Flat(Dir(p + 80)), Vector3.up); return Mathf.Abs(t) < 12f ? 0 : Mathf.Sign(t); }
        static float Outside(float p, float fallback) { float t = Turn(p); return t == 0 ? fallback : -t; }
        // a point `ahead` metres on from p along the line's heading there, `off` metres to the side (+1 right), on the ground; pushed out 5 m
        // at a time until it keeps `margin` metres from the road's edge (or 25 m above / below the line)
        static Vector3 Spot(float p, float side, float off, float ahead = 0, float margin = 6f, bool ground = true, float y = 0)
        {
            var f = Flat(Dir(p)); Vector3 q = default;
            for (int i = 0; i < 12; i++, off += 5f)
            {
                q = At(p) + f * ahead + Right(p) * side * off; q.y = ground ? GroundY(q, y) : y;
                LineDistance(q, out float flat, out float prog);
                if (flat > HalfWidthSafe(prog) + margin || Mathf.Abs(At(prog).y - q.y) > 25f || Underground(At(prog), q)) return q;
            }
            DinoSkipped.Add($"spot at {p:0} m pushed {off:0} m out"); return q;
        }
        // the line there is in the cave under the land the dinosaur stands on
        public static bool Underground(Vector3 line, Vector3 q) => line.y < -3f && q.y > Water + .5f;
        // standing in the water up to its knees (the ground, or `depth` under the surface where the water is deeper)
        static Vector3 Wading(Vector3 q, float depth) { float g = GroundY(q, Water - depth); q.y = Mathf.Max(g, Water - depth); return q; }
        static float FaceFrom(Vector3 at, float p) => Quaternion.LookRotation(Flat(At(p) - at)).eulerAngles.y;
        static DinoActor Stand(string style, DinoActor.Kind kind, Vector3 at, float yaw, float h, string seg, string label, bool setPiece = false)
        {
            var d = DinoActor.Make(dinoRoot, style, kind, at, at + Quaternion.Euler(0, yaw, 0) * Vector3.forward * 6f, h, seg, label, yaw); d.speed = kind == DinoActor.Kind.Idle ? 0 : .4f; d.setPiece = setPiece; return d;
        }
        // a set-piece that runs / swims / flies with the nearest person's racer between from and to (see DinoActor.Pace)
        static DinoActor Pacer(string style, float h, float from, float to, float lead0, float lead1, float side0, float side1, string seg, string label, bool fly = false, float lift = 0, bool water = false, float waterY = 0)
        {
            var a = At(from + lead0) + Right(from + lead0) * side0;
            var d = DinoActor.Make(dinoRoot, style, DinoActor.Kind.Pace, a, a, h, seg, label, FaceFrom(a, from + lead0 + 50));
            d.paceFrom = from; d.paceTo = to; d.leadStart = lead0; d.leadEnd = lead1; d.sideStart = side0; d.sideEnd = side1; d.paceFly = fly; d.lift = lift; d.paceWater = water; d.paceY = waterY;
            d.setPiece = true; d.range = 900; return d;
        }

        static void Dinosaurs()
        {
            Physics.SyncTransforms(); linePts = null; DinoSkipped.Clear(); Fossils.Clear();
            dinoRoot = new GameObject("Dinosaurs").transform; dinoRoot.SetParent(root, false);
            const string P = "Beach Promenade", C = "Cliffside Climb", A = "Sky Arch", L = "Lagoon Landing", MR = "Mangrove Run", G = "Sunken Gateway", V = "Coral Vault", WR = "Waterfall Return";

            // ---- Beach Promenade ----
            // set-piece: two brontosaurs either side of the road between the Creek Jump and the loop, their necks reaching out over it like a
            // bridge (heads 17 m up, nothing below the necks; the gates and the loop are clear of them)
            { float pa = M("promMid") + 75;
              foreach (float s in new[] { -1f, 1f }) { var q = Spot(pa + s * 5, s, 27, 0, 0); Stand("bronto", DinoActor.Kind.Arch, q, Quaternion.LookRotation(-Right(pa) * s).eulerAngles.y, Bronto, P, s < 0 ? "neck bridge west" : "neck bridge east", true); } }
            // raptors running with you up the strip after the start, just outside the barriers
            for (int i = 0; i < 3; i++)
                Pacer("raptor", Raptor - i * .4f, M("G0") + 40, M("jA") - 25, 26 - i * 7, -4 - i * 6, 17 + i * 3.5f, 17 + i * 3.5f, P, $"raptor {i + 1}");
            // the lake herd wading along the beach, near the road (build 9: 120-140 m out in the lake)
            foreach (var (mk, d, n) in new[] { ("jA", 60f, 0), ("loopApp", 30f, 1) })
            { float p = M(mk) + d; var q = Wading(Spot(p, -1, 70 + n * 8, 30, 0, false, Water - 3), 3.5f); Stand("bronto", DinoActor.Kind.Wade, q, FaceFrom(q, p - 150) + 40, Bronto - n * 2, P, $"lake bronto {n + 1}"); }
            Swooper(new Vector3(8, 88, 540), 46, 6, 22f, P, "loop ptero 1"); Swooper(new Vector3(-20, 98, 600), 60, 8, 20f, P, "loop ptero 2", 19f);
            { float p0 = CanyonFrom + 30, p1 = CanyonTo - 40; var a = At(p0) + Right(p0) * 50; var b = At(p1) + Right(p1) * 54;   // beyond the canyon's east wall: necks over it
              Herd("bronto", DinoActor.Kind.Graze, OnGround(a.x, a.z), OnGround(b.x, b.z), 2, 26f, P, "canyon long-necks", 26f); }
            Nest(new Vector3(-72, 0, 300), P); Footprints(new Vector3(-58, 0, 150), new Vector3(-78, 0, 330), P);

            // ---- Cliffside Climb ----
            // a long-neck at the end of the first climb straight (dead ahead before corner1)
            { float p = M("corner1"); var q = Spot(p, Outside(p, -1), 22, 45); Stand("bronto", DinoActor.Kind.Graze, q, FaceFrom(q, p - 160), Bronto, C, "corner1 bronto", true); }
            // a long-neck up on the cliff top beside the long climb straight, looking down at the road
            { float p = M("climbMid") + 190; var q = Spot(p, -1, 30, 40, 8); Stand("bronto", DinoActor.Kind.Graze, q, FaceFrom(q, p - 100), Bronto, C, "climb cliff bronto", true); }
            // set-piece: the T. rex dead ahead at the end of the long climb (corner2), up on the rock, leaning out, roaring as you come
            {
                float tp = M("corner2"); var top = Spot(tp, Outside(tp, -1), 10, 34, 5);
                var t = Stand("trex", DinoActor.Kind.Roar, top, FaceFrom(top, tp - 140), TRex, C, "cliff T. rex", true); t.roarRange = 300; t.range = 1000;
            }
            Herd("trike", DinoActor.Kind.Graze, OnGround(300, 1070, .5f), OnGround(360, 1060, .5f), 2, Trike, C, "valley trike", 16f);

            // ---- Sky Arch: a pterodactyl flies with you, another swoops across in front; long-necks under the flight ----
            Pacer("ptero", PteroSpan, M("lip") + 110, M("arch") - 160, 55, 14, 30, 20, A, "flight ptero (alongside)", true, 5);
            Pacer("ptero", PteroSpan, M("arch") + 110, M("splash") - 330, 75, 30, -60, 48, A, "flight ptero (across)", true, 14);
            Herd("bronto", DinoActor.Kind.Graze, OnGround(560, 650, .4f), OnGround(610, 540, .4f), 3, Bronto, A, "valley herd", 40f);

            // ---- Lagoon Landing: brontosaurs wading right next to the boat line on the outside of the bends; a long-neck swimming with you ----
            foreach (var (d, n) in new[] { (170f, 0), (330f, 1), (470f, 2) })
            { float p = M("splash") + d; var q = Wading(Spot(p, Outside(p, n % 2 == 0 ? 1 : -1), 34, 25, 6, false), 4f); Stand("bronto", DinoActor.Kind.Wade, q, FaceFrom(q, p - 150), Bronto - n * 1.5f, L, $"lagoon bronto {n + 1}", n == 0); }
            { float a0 = M("splash") + 30, a1 = M("lagoonEnd") - 20; Pacer("plesio", Plesio, a0, a1, 45, 8, -25, -25, L, "lagoon long-neck", false, 0, true, -6); }
            Swooper(new Vector3(650, 42, -880), 40, 6, 20f, L, "lagoon ptero");

            // ---- Mangrove Run: a long-neck on the outside bank of every bend; a triceratops charging along the bank with you ----
            foreach (var (mk, n) in new[] { ("bend1", 0), ("bend2", 1), ("bend3", 2), ("bend4", 3) })
            { float p = M(mk); var q = Spot(p, Outside(p, n % 2 == 0 ? 1 : -1), 30, 20, 8); Stand("bronto", DinoActor.Kind.Graze, q, FaceFrom(q, p - 140), Bronto - (n % 2) * 3, MR, $"mangrove bronto {mk}", n == 1); }
            Pacer("trike", 6f, M("bend1") + 10, M("mangMid") + 40, 28, 4, 27, 27, MR, "charging trike");
            foreach (var (mk, side, n) in new[] { ("bend4", -1f, 1) })
            { float p = M(mk) + 60; var a = At(p) + Right(p) * side * 40; var b = a + Flat(Dir(p)) * 30; Herd("trike", DinoActor.Kind.Graze, OnGround(a.x, a.z, .8f), OnGround(b.x, b.z, .8f), n, Trike, MR, $"mangrove trike {mk}", 14f); }
            { float p = M("bend3"); var q = At(p) + Right(p) * 42; Nest(new Vector3(q.x, 0, q.z), MR); }

            // ---- Sunken Gateway: a long-neck rising out of the pool beside you before the dive; a brontosaurus on the ridge over the gateway ----
            {
                float p = M("G5") + 40; float s = Outside(p, 1); var q = Spot(p, s, 24, 45, 6, false, Water - 5);
                var r = Stand("plesio", DinoActor.Kind.Rise, q, FaceFrom(q, p - 120), Plesio, G, "pool long-neck (rising)", true); r.sink = 23; r.paceFrom = M("G5") - 110; r.paceTo = M("portal") + 30;
                var w = Wading(Spot(M("G5") + 15, -s, 28, 70, 6, false), 4f); Stand("bronto", DinoActor.Kind.Wade, w, FaceFrom(w, M("G5") - 80), Bronto, G, "gateway pool bronto");
            }

            // ---- Coral Vault: giant skeletons in the cave walls on the outside of its bends (they glow a little) ----
            foreach (var (mk, d) in new[] { ("underRoad", 10f), ("hi1", -25f), ("lo1", -10f), ("hi2", -20f), ("lo2", -15f) })
            { float p = M(mk) + d; Fossil(p, Outside(p, 1), V); }

            // ---- Waterfall Return: brontosaurs watching from either side of the waterfall landing; the pool's long-necks ----
            foreach (float s in new[] { -1f, 1f })
            { float p = M("landing") + 50 + s * 15; var q = Spot(p, s, 26, 0, 8); Stand("bronto", DinoActor.Kind.Graze, q, FaceFrom(q, p - 60), Bronto - (s > 0 ? 2 : 0), WR, s < 0 ? "landing bronto west" : "landing bronto east", s < 0); }
            { float p = M("surface") - 10; var q = Wading(Spot(p, -1, 30, 30, 6, false), 4f); Stand("bronto", DinoActor.Kind.Wade, q, FaceFrom(q, p - 100), Bronto - 3, WR, "pool bronto"); }
            Swooper(new Vector3(-75, 60, -170), 40, 6, 20f, WR, "finish ptero");
            // Round 8b item 3: what is not a set-piece is drawn within 500 m (650 before; at 500 m the haze has a third of it already)
            foreach (var x in DinoActor.All) if (!x.setPiece && x.kind != DinoActor.Kind.Roar && x.style != "skeleton" && x.range > 500f) x.range = 500f;
            Debug.Log($"[Dinos] {DinoActor.All.Count} dinosaurs ({DinoActor.All.Count(x => x.setPiece)} set-pieces), {Fossils.Count} fossils; notes: {(DinoSkipped.Count == 0 ? "none" : string.Join("; ", DinoSkipped))}");
        }
        static float ProgressNear(Vector3 p) { LineDistance(p, out _, out float prog); return prog; }

        // a nest: a ring of twigs with four speckled eggs
        static void Nest(Vector3 at, string seg)
        {
            at.y = GroundY(at, at.y); var b = new MeshKit.Batch();
            for (int i = 0; i < 14; i++) { float a = i * 360f / 14; b.Add(MeshKit.Cube, at + Quaternion.Euler(0, a, 0) * Vector3.forward * 2.2f + Vector3.up * .35f, Quaternion.Euler(10, a + 80, 15), new Vector3(.25f, .25f, 2.4f), JurassicLook.FernTrunk); }
            for (int i = 0; i < 4; i++) b.Add(MeshKit.LowSphere, at + new Vector3((i % 2 - .5f) * 1.1f, .75f, (i / 2 - .5f) * 1.1f), Quaternion.Euler(0, i * 40, 0), new Vector3(.9f, 1.25f, .9f), i % 2 == 0 ? new Color(.95f, .92f, .82f) : new Color(.85f, .9f, .78f));
            b.Object(scen, "Dino nest", JurassicLook.VertexColored(true), false, false).layer = DetailLayer;
            Fossils.Add(("nest " + seg, at));
        }
        // three-toed footprints along a line on the sand
        static void Footprints(Vector3 a, Vector3 b, string seg)
        {
            var batch = new MeshKit.Batch(); var f = Flat(b - a); var side = Vector3.Cross(Vector3.up, f); int n = Mathf.RoundToInt(Vector3.Distance(a, b) / 3.2f);
            for (int i = 0; i < n; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)n) + side * ((i % 2) - .5f) * 1.6f; p.y = GroundY(p, .5f) + .03f; var rot = Quaternion.LookRotation(f);
                foreach (float toe in new[] { -25f, 0f, 25f }) batch.Add(MeshKit.LowSphere, p + rot * Quaternion.Euler(0, toe, 0) * Vector3.forward * .5f, rot * Quaternion.Euler(0, toe, 0), new Vector3(.28f, .05f, .8f), new Color(.6f, .52f, .36f));
                batch.Add(MeshKit.LowSphere, p, rot, new Vector3(.6f, .05f, .55f), new Color(.6f, .52f, .36f));
            }
            batch.Object(scen, "Dino footprints", JurassicLook.VertexColored(false), false, false).layer = DetailLayer;
            Fossils.Add(("footprints " + seg, a));
        }
        // a giant dinosaur skeleton in the cave wall (Round 8b: 60 m long, 13 m tall, laid along the outside wall of a bend so the sub heads
        // straight at it and then runs along it): spine and tail following the wall, upright ribs, skull and jaw, legs; bone with a faint
        // glow so it reads in the dark water. It counts as a dinosaur on screen (DinoActor.Still, style "skeleton") for the visibility
        // check and TeedOff Barker's skeleton line.
        static void Fossil(float prog, float side, string seg)
        {
            const float L = 60f, H = 13f; float y = Mathf.Lerp(CaveFloor, CaveRoof, .45f); var up = Vector3.up;
            Vector3 WallAt(float x) { float q = prog + x; var w = At(q) + Right(q) * side * (CaveHalf - .8f); w.y = y; return w; }
            Vector3 Into(float x) => -Right(prog + x) * side;
            Quaternion Along(float x) => Quaternion.LookRotation(Flat(Dir(prog + x)), up);
            var b = new MeshKit.Batch(); var bone = JurassicLook.Bone;
            for (int i = 0; i < 24; i++) { float x = -L * .45f + i * L * .9f / 23f, t = 1f - Mathf.Abs(i - 12) / 15f; b.Add(MeshKit.Cube, WallAt(x) + Into(x) * .3f + up * Mathf.Sin(i * .3f) * .7f, Along(x), new Vector3(.9f, .8f, 1.7f) * (.55f + .55f * t), bone); }   // spine and tail
            for (int i = 0; i < 12; i++)                                                                                                                  // ribs, upright
            {
                float x = -L * .18f + i * 1.9f, len = H * .42f * (1f - Mathf.Abs(i - 5.5f) / 8f);
                foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.Cube, WallAt(x) + up * s * len * .5f + Into(x) * .6f, Quaternion.LookRotation(up * s + Flat(Dir(prog + x)) * .15f, Into(x)), new Vector3(.45f, .35f, len), bone);
            }
            float hx = L * .47f;
            b.Add(MeshKit.LowSphere, WallAt(hx) + up * .8f + Into(hx) * .7f, Along(hx), new Vector3(2.6f, 2.2f, 4.4f), bone);                       // skull
            b.Add(MeshKit.Cube, WallAt(hx) - up * .9f + Into(hx) * .7f, Along(hx) * Quaternion.Euler(14, 0, 0), new Vector3(1.8f, .5f, 3.4f), bone);   // jaw
            foreach (float x in new[] { -L * .16f, -L * .1f, L * .1f, L * .16f })                                                                       // legs
                b.Add(MeshKit.Cube, WallAt(x) - up * H * .3f + Into(x) * .4f, Quaternion.LookRotation(-up + Flat(Dir(prog + x)) * .25f, Into(x)), new Vector3(.8f, .8f, H * .42f), bone);
            var go = b.Object(scen, "Dino fossil", JurassicLook.VertexColored(true, new Color(.25f, .22f, .15f)), false, false);
            DinoActor.Still(dinoRoot, "skeleton", go.GetComponent<Renderer>(), WallAt(0) - up * H * .5f, Along(0), new Vector3(2f, H, L), seg, $"skeleton {Fossils.Count + 1}");
            Fossils.Add(("fossil " + seg, WallAt(0)));
        }
    }
}
