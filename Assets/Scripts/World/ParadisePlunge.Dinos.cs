using System.Collections.Generic;
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

        static void Dinosaurs()
        {
            Physics.SyncTransforms(); linePts = null; DinoSkipped.Clear(); Fossils.Clear();
            dinoRoot = new GameObject("Dinosaurs").transform; dinoRoot.SetParent(root, false);
            const string P = "Beach Promenade", C = "Cliffside Climb", A = "Sky Arch", L = "Lagoon Landing", MR = "Mangrove Run", G = "Sunken Gateway", V = "Coral Vault", WR = "Waterfall Return";
            // ---- Beach Promenade ----
            Herd("bronto", DinoActor.Kind.Wade, new Vector3(-118, -3.2f, 250), new Vector3(-128, -3.2f, 430), 3, 18f, P, "lake herd", 26f, false);
            Herd("bronto", DinoActor.Kind.Wade, new Vector3(-125, -3.2f, 640), new Vector3(-140, -3.2f, 820), 2, 19f, P, "lake herd north", 28f, false);
            for (int i = 0; i < 4; i++)
            {
                var r = Walker("raptor", DinoActor.Kind.Run, OnGround(40 + (i % 2) * 7, 262 + i * 6), OnGround(42 + (i % 2) * 7, 520 + i * 4), 2.6f, P, $"raptor {i + 1}");
                if (r) { r.runFrom = ProgressNear(new Vector3(0, 2.9f, 262)); r.runTo = ProgressNear(new Vector3(0, 2.9f, 520)); r.range = 700; }
            }
            Swooper(new Vector3(8, 88, 540), 46, 6, 9f, P, "loop ptero 1"); Swooper(new Vector3(-20, 98, 600), 60, 8, 8f, P, "loop ptero 2", 19f);
            { float p0 = CanyonFrom + 30, p1 = CanyonTo - 40; var a = At(p0) + Right(p0) * 55; var b = At(p1) + Right(p1) * 60;   // beyond the canyon's east wall: necks over it
              Herd("bronto", DinoActor.Kind.Graze, OnGround(a.x, a.z), OnGround(b.x, b.z), 2, 16f, P, "canyon long-necks", 24f); }
            Nest(new Vector3(-72, 0, 300), P); Footprints(new Vector3(-58, 0, 150), new Vector3(-78, 0, 330), P);
            // ---- Cliffside Climb: the T. rex on the north cliff ----
            {
                // on the climb's left cliff edge, ahead of the car halfway up, looking down at the road (build 1: on the north hill, hidden behind the cliff)
                float tp = M("climbMid") + 70; var edge = At(tp) - Right(tp) * 30f; var top = OnGround(edge.x, edge.z, edge.y + 10);
                var t = DinoActor.Make(dinoRoot, "trex", DinoActor.Kind.Roar, top, top, 12f, C, "cliff T. rex", Quaternion.LookRotation(Flat(At(tp - 120) - top)).eulerAngles.y); t.roarRange = 260; t.range = 1000;
                if (!Clear("cliff T. rex", new[] { top }, false)) Object.Destroy(t.gameObject);
            }
            Herd("trike", DinoActor.Kind.Graze, OnGround(300, 1070, .5f), OnGround(360, 1060, .5f), 2, 6.5f, C, "valley trike", 16f);
            // ---- Sky Arch: under and beside the flight ----
            Herd("bronto", DinoActor.Kind.Graze, OnGround(560, 650, .4f), OnGround(610, 540, .4f), 3, 18f, A, "valley herd", 34f);
            Herd("trike", DinoActor.Kind.Graze, OnGround(440, 300, .4f), OnGround(470, 250, .4f), 2, 7f, A, "valley trike", 18f);
            Swooper(new Vector3(408, 86, 634), 52, 9, 9f, A, "flight ptero 1", 18f); Swooper(new Vector3(300, 78, 250), 48, 7, 8f, A, "flight ptero 2", 17f);
            // ---- Lagoon Landing: a long-neck swimming (plesiosaur stand-in) ----
            Walker("plesio", DinoActor.Kind.Swim, new Vector3(640, -8.5f, -790), new Vector3(595, -8.5f, -880), 15f, L, "lagoon long-neck", 2.2f);
            Swooper(new Vector3(650, 42, -880), 40, 6, 7f, L, "lagoon ptero");
            // ---- Mangrove Run: triceratops on the banks, a nest ----
            foreach (var (mk, side, n) in new[] { ("bend2", 1f, 2), ("mangMid", -1f, 1), ("bend4", -1f, 2) })
            {
                float p = M(mk); var a = At(p) + Right(p) * side * 48; var b = a + Flat(Dir(p)) * 30;
                Herd("trike", DinoActor.Kind.Graze, OnGround(a.x, a.z, .8f), OnGround(b.x, b.z, .8f), n, 6.5f, MR, $"mangrove trike {mk}", 14f);
            }
            { float p = M("bend3"); var q = At(p) + Right(p) * 42; Nest(new Vector3(q.x, 0, q.z), MR); }
            // ---- Sunken Gateway: a brontosaurus on the ridge over the opening ----
            Walker("bronto", DinoActor.Kind.Graze, OnGround(205, -150, 1.5f), OnGround(250, -160, 1.5f), 16f, G, "ridge bronto", 1f);
            // ---- Coral Vault: skeletons and fossils in the cave walls ----
            foreach (var (mk, d, side) in new[] { ("underRoad", 20f, 1f), ("hi1", -10f, -1f), ("lo1", 15f, 1f), ("hi2", 0f, 1f), ("lo2", 10f, -1f) })
                Fossil(M(mk) + d, side, V);
            // ---- Waterfall Return ----
            Herd("bronto", DinoActor.Kind.Graze, OnGround(-70, -430, 1.5f), OnGround(-80, -360, 1.5f), 2, 15f, WR, "pool brontos", 26f);
            Walker("trike", DinoActor.Kind.Graze, OnGround(85, -230, 1.5f), OnGround(90, -170, 1.5f), 6.5f, WR, "finish trike");
            Swooper(new Vector3(-75, 60, -170), 40, 6, 8f, WR, "finish ptero");
            Debug.Log($"[Dinos] {DinoActor.All.Count} dinosaurs, {Fossils.Count} fossils; left out (too near the line): {(DinoSkipped.Count == 0 ? "none" : string.Join("; ", DinoSkipped))}");
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
        // a dinosaur skeleton in the cave wall: spine, ribs, skull, tail (bone, a faint glow so it reads in the dark water)
        static void Fossil(float prog, float side, string seg)
        {
            var at = At(prog); var f = Flat(Dir(prog)); var r = Right(prog); var wall = at + r * side * (CaveHalf - .4f) + Vector3.down * 2f;
            var rot = Quaternion.LookRotation(f, -r * side);   // lying on the wall, facing into the cave
            var b = new MeshKit.Batch(); var bone = JurassicLook.Bone;
            for (int i = 0; i < 12; i++) b.Add(MeshKit.Cube, wall + f * (-7 + i * 1.2f), rot, new Vector3(.5f, .3f, 1f), bone);                  // spine
            for (int i = 0; i < 7; i++) foreach (float s in new[] { -1f, 1f })                                                                    // ribs
                    b.Add(MeshKit.Cube, wall + f * (-3 + i * 1.1f) + Vector3.up * s * 1.3f, rot * Quaternion.Euler(0, 0, s * 70), new Vector3(.25f, .2f, 2.6f - Mathf.Abs(i - 3) * .3f), bone);
            b.Add(MeshKit.LowSphere, wall + f * 8f + Vector3.up * .3f, rot, new Vector3(1.6f, .6f, 2.4f), bone);                                     // skull
            b.Add(MeshKit.Cube, wall + f * 8.6f - Vector3.up * .5f, rot * Quaternion.Euler(12, 0, 0), new Vector3(1.1f, .3f, 1.8f), bone);            // jaw
            for (int i = 0; i < 4; i++) foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.Cube, wall + f * (-4 + (i / 2) * 6) + Vector3.up * s * 2.6f + f * (i % 2), rot * Quaternion.Euler(0, 0, s * 20), new Vector3(.3f, .3f, 1.8f), bone);
            b.Object(scen, "Dino fossil", JurassicLook.VertexColored(true, new Color(.25f, .22f, .15f)), false, false);
            Fossils.Add(("fossil " + seg, wall));
        }
    }
}
