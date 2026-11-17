using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 16 item 2: Dam Shame's life and dressing: the set-pieces (Round 8b's rule: big, close, in front: SetPiece, 8 % of the screen's
    // height for 1 s): the dam itself, the spillway's gates and torrent (DamShame.World), the giant turbines, Buck waving from the control room,
    // the beaver colony with its own little dam (a joke beside the real one), a heron, trout leaping, logs and debris riding the flood. The
    // animals are stand-ins built from simple shapes until Dan's Tripo models arrive (Docs/MODELS-NEEDED.md Round 16: the beaver, the heron,
    // Hattie Sluice the dam keeper). The crowd stands on a platform on the crest's reservoir side. Signs and markings show the coming form
    // for the water as it is (DamWater sets which are up).
    public static partial class DamShame
    {
        public static readonly List<(GameObject g, bool[] up)> StageThings = new List<(GameObject, bool[])>();
        static void ByStage(GameObject g, bool s0, bool s1, bool s2) => StageThings.Add((g, new[] { s0, s1, s2 }));

        static void SetPieces()
        {
            // the dam's downstream face, seen from the valley and the dam road
            var dg = new GameObject("The dam (set-piece)"); dg.transform.SetParent(scen, false); dg.transform.position = L((DamX0 + DamX1) * .5f, CrestY * .5f, (DamToe - DamTop) * .5f);
            SetPiece.Make(dg, "The dam", "", "dmdam", Vector3.zero, new Vector3(DamX1 - DamX0, CrestY, DamToe + DamTop)).range = 1200;
            Beavers(); HeronStandIn(); Trout(); Logs();
        }

        // the beavers' lodge and their little stick dam across the river, 70 m south of the bridge (under the Canyon flight)
        static void Beavers()
        {
            float z = -885, rx = RiverX(z); var g = new GameObject("Beaver colony"); g.transform.SetParent(scen, false); g.transform.position = L(rx, 0, z);
            var b = new MeshKit.Batch(); var rnd = new System.Random(5);
            // the little dam: sticks piled across the river (2.5 m high), mud between
            for (int i = 0; i < 70; i++) { float x = Rand(rnd, -RiverHalf - 2, RiverHalf + 2); b.Add(MeshKit.LowCylinder, new Vector3(x, Rand(rnd, -2.5f, .4f), Rand(rnd, -1.5f, 1.5f)), Quaternion.Euler(Rand(rnd, 60, 120), Rand(rnd, -50, 50), Rand(rnd, -30, 30)), new Vector3(.35f, Rand(rnd, 3, 7), .35f), Color.Lerp(DamLook.Bark, DamLook.FurDark, (float)rnd.NextDouble())); }
            b.Add(MeshKit.Cube, new Vector3(0, -1.6f, 0), Quaternion.identity, new Vector3(RiverHalf * 2 + 4, 3.6f, 2.6f), new Color(.36f, .27f, .2f));
            // the lodge on the east bank: a mound of sticks, a door, a little sign
            var lodge = new Vector3(RiverHalf + 14, 0, -6);
            b.Add(MeshKit.LowSphere, lodge + Vector3.up * 3f, Quaternion.identity, new Vector3(18, 12, 15), new Color(.4f, .29f, .2f));
            for (int i = 0; i < 40; i++) { float a = Rand(rnd, 0, 360) * Mathf.Deg2Rad; b.Add(MeshKit.LowCylinder, lodge + new Vector3(Mathf.Cos(a) * Rand(rnd, 3, 8), Rand(rnd, 2, 8), Mathf.Sin(a) * Rand(rnd, 3, 7)), Quaternion.Euler(Rand(rnd, 30, 150), Rand(rnd, 0, 360), 0), new Vector3(.3f, Rand(rnd, 3, 6), .3f), DamLook.Bark); }
            b.Add(MeshKit.Cube, lodge + new Vector3(-7.5f, 1.4f, 0), Quaternion.identity, new Vector3(.5f, 2.4f, 2.2f), new Color(.15f, .1f, .08f));
            b.Add(MeshKit.LowCylinder, lodge + new Vector3(-9, 2.5f, 7), Quaternion.identity, new Vector3(.3f, 5, .3f), DamLook.Bark);
            b.Add(MeshKit.Cube, lodge + new Vector3(-9, 5.6f, 7), Quaternion.Euler(0, 90, 0), new Vector3(9, 2.2f, .3f), new Color(.85f, .7f, .45f));
            b.Object(g.transform, "lodge and little dam", DamLook.Vc(true), false, true);
            Label("DAM GOOD", g.transform.position + lodge + new Vector3(-9.2f, 5.6f, 7), Quaternion.LookRotation(Vector3.left), 1.3f, new Color(.25f, .15f, .08f), false);
            // the beavers (stand-ins): one on the dam slapping its tail, one carrying a stick along it, two on the lodge
            Beaver(g.transform, new Vector3(-6, .6f, 0), Quaternion.Euler(0, 90, 0), 4.2f, true);
            var walker = Beaver(g.transform, new Vector3(6, .6f, .5f), Quaternion.Euler(0, -90, 0), 3.8f, false); var pace = walker.AddComponent<Pace>(); pace.half = 9; pace.speed = 1.6f;
            Beaver(g.transform, lodge + new Vector3(-3, 8.6f, -2), Quaternion.Euler(0, -120, 0), 3.4f, false);
            Beaver(g.transform, lodge + new Vector3(2, 8.2f, 3), Quaternion.Euler(0, -70, 0), 3.2f, true);
            SetPiece.Make(g, "Beaver colony", "", "dmbeaver", new Vector3(14, 4, -2), new Vector3(70, 13, 20)).range = 500;
        }
        // a cartoon beaver (stand-in), feet at the origin, facing +z, about len long; buck teeth, a flat tail (slapping: a Swing)
        static GameObject Beaver(Transform parent, Vector3 local, Quaternion rot, float len, bool slap)
        {
            var g = new GameObject("beaver"); g.transform.SetParent(parent, false); g.transform.localPosition = local; g.transform.localRotation = rot;
            var b = new MeshKit.Batch(); float k = len / 3f;
            b.Add(MeshKit.LowSphere, new Vector3(0, .8f, 0) * k, Quaternion.Euler(-15, 0, 0), new Vector3(1.4f, 1.3f, 2f) * k, DamLook.Fur);
            b.Add(MeshKit.LowSphere, new Vector3(0, 1.5f, .95f) * k, Quaternion.identity, new Vector3(.95f, .85f, .9f) * k, DamLook.Fur);
            b.Add(MeshKit.LowSphere, new Vector3(0, 1.35f, 1.35f) * k, Quaternion.identity, new Vector3(.5f, .38f, .35f) * k, DamLook.FurDark);
            b.Add(MeshKit.Cube, new Vector3(0, 1.08f, 1.45f) * k, Quaternion.identity, new Vector3(.26f, .26f, .06f) * k, Color.white);   // the teeth
            foreach (float s in new[] { -1f, 1f }) { b.Add(MeshKit.LowSphere, new Vector3(s * .25f, 1.75f, 1.3f) * k, Quaternion.identity, Vector3.one * .14f * k, Color.black); b.Add(MeshKit.LowSphere, new Vector3(s * .38f, 1.95f, .85f) * k, Quaternion.identity, new Vector3(.22f, .22f, .1f) * k, DamLook.FurDark); }
            b.Object(g.transform, "body", DamLook.Vc(true), false, false);
            var tail = new GameObject("tail"); tail.transform.SetParent(g.transform, false); tail.transform.localPosition = new Vector3(0, .5f, -.9f) * k;
            var tb = new MeshKit.Batch(); tb.Add(MeshKit.LowSphere, new Vector3(0, 0, -.7f) * k, Quaternion.identity, new Vector3(.9f, .18f, 1.5f) * k, new Color(.22f, .17f, .14f)); tb.Object(tail.transform, "tail", DamLook.Vc(true), false, false);
            if (slap) { var sw = tail.AddComponent<Swing>(); sw.deg = 35; sw.period = .9f; }
            return g;
        }
        // a grey heron (stand-in) on one leg on a rock by the valley road, a fish in its beak; it is gone when the valley floods
        static void HeronStandIn()
        {
            var at = L(-300, 0, -790); var g = new GameObject("Heron"); g.transform.SetParent(scen, false); g.transform.SetPositionAndRotation(at, Quaternion.Euler(0, 40, 0));
            var b = new MeshKit.Batch(); float k = 2.6f; var grey = DamLook.Heron;
            b.Add(MeshKit.LowSphere, new Vector3(0, .6f, 0) * k, Quaternion.identity, new Vector3(6, 2.4f, 5), DamLook.RockDark);
            b.Add(MeshKit.LowCylinder, new Vector3(0, 1.9f, 0) * k, Quaternion.identity, new Vector3(.08f, 1.3f, .08f) * k, new Color(.85f, .7f, .3f));
            b.Add(MeshKit.LowSphere, new Vector3(0, 2.9f, 0) * k, Quaternion.Euler(-20, 0, 0), new Vector3(.75f, .7f, 1.3f) * k, grey);
            for (int i = 0; i < 5; i++) { float u = i / 4f; b.Add(MeshKit.LowCylinder, new Vector3(0, 3.2f + u * .9f, .45f + Mathf.Sin(u * 2.4f) * .3f) * k, Quaternion.Euler(-20 + u * 30, 0, 0), new Vector3(.18f, .28f, .18f) * k, grey); }
            b.Add(MeshKit.LowSphere, new Vector3(0, 4.25f, .6f) * k, Quaternion.identity, new Vector3(.32f, .3f, .4f) * k, Color.white);
            b.Add(MeshKit.LowCone, new Vector3(0, 4.2f, 1.05f) * k, Quaternion.Euler(90, 0, 0), new Vector3(.12f, .7f, .12f) * k, new Color(.95f, .75f, .2f));
            b.Add(MeshKit.LowSphere, new Vector3(0, 4.15f, 1.3f) * k, Quaternion.Euler(0, 90, 0), new Vector3(.6f, .18f, .12f) * k, new Color(.7f, .75f, .7f));   // the fish
            b.Add(MeshKit.Cube, new Vector3(0, 4.45f, .4f) * k, Quaternion.Euler(-30, 0, 0), new Vector3(.06f, .06f, .6f) * k, new Color(.15f, .15f, .2f));   // the crest feather
            b.Object(g.transform, "heron", DamLook.Vc(true), false, true);
            var sp = SetPiece.Make(g, "Heron", "", "dmheron", new Vector3(0, 6, 0), new Vector3(6, 12, 6)); sp.range = 260; sp.showing = () => DamWater.Stage < 2;
            ByStage(g, true, true, false);
        }
        // trout leaping in arcs out of the water (they ride the water level up)
        static void Trout()
        {
            foreach (var (x, z) in new[] { (RiverX(-700f) + 4, -700f), (RiverX(-1000f), -1000f), (RiverX(-760f) - 3, -760f) })
            {
                var g = new GameObject("Trout"); g.transform.SetParent(scen, false); g.transform.position = L(x, 0, z); g.AddComponent<OnWater>();
                var items = new List<Transform>();
                for (int i = 0; i < 5; i++)
                {
                    var f = new GameObject("trout " + i); f.transform.SetParent(g.transform, false); var fb = new MeshKit.Batch();
                    fb.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, new Vector3(.7f, .8f, 2.6f), new Color(.55f, .6f, .5f)); fb.Add(MeshKit.LowSphere, new Vector3(0, -.15f, 0), Quaternion.identity, new Vector3(.6f, .5f, 2.3f), new Color(.95f, .55f, .5f));
                    fb.Add(MeshKit.Cube, new Vector3(0, 0, -1.5f), Quaternion.Euler(0, 0, 0), new Vector3(.12f, 1f, .7f), new Color(.5f, .55f, .45f));
                    fb.Object(f.transform, "fish", DamLook.Vc(true), false, false); items.Add(f.transform);
                }
                var arcs = g.AddComponent<Arcs>(); arcs.items = items.ToArray(); arcs.throwVel = new Vector3(0, 11, 4); arcs.spread = 4; arcs.gravity = 14; arcs.life = 1.6f; arcs.spin = 140;
                SetPiece.Make(g, "Trout leaping", "", "dmtrout", new Vector3(0, 3, 3), new Vector3(10, 7, 10)).range = 160;
            }
        }
        // logs and debris riding the flood down the valley (lap 2 and 3), drifting slowly south and round again
        static void Logs()
        {
            var g = new GameObject("Flood debris"); g.transform.SetParent(scen, false); g.transform.position = L(0, 0, 0); g.AddComponent<OnWater>().offset = .3f;
            var rnd = new System.Random(9);
            for (int i = 0; i < 26; i++)
            {
                var log = new GameObject("log " + i); log.transform.SetParent(g.transform, false);
                var lb = new MeshKit.Batch(); float len = Rand(rnd, 6, 12);
                lb.Add(MeshKit.LowCylinder, Vector3.zero, Quaternion.Euler(90, 0, 0), new Vector3(1.1f, len, 1.1f), Color.Lerp(DamLook.Bark, DamLook.FurDark, (float)rnd.NextDouble()));
                if (i % 4 == 0) lb.Add(MeshKit.Cube, new Vector3(0, .9f, 0), Quaternion.Euler(0, 30, 0), new Vector3(3, .4f, 2), new Color(.85f, .2f, .15f));   // a picnic table's top, a sign
                if (i % 5 == 1) lb.Add(MeshKit.LowSphere, new Vector3(0, .9f, len * .3f), Quaternion.identity, new Vector3(1.6f, 1.4f, 1.6f), new Color(.98f, .98f, .98f));   // a duck. A large duck.
                lb.Object(log.transform, "log", DamLook.Vc(true), false, false);
                var d = log.AddComponent<FloodDrift>(); d.at = Rand(rnd, 0, 1); d.speed = Rand(rnd, .006f, .012f); d.side = Rand(rnd, -60, 60); d.spin = Rand(rnd, -12, 12);
            }
            var sp = new GameObject("Flood debris (set-piece)"); sp.transform.SetParent(g.transform, false); sp.transform.position = L(250, 0, -760);
            var s = SetPiece.Make(sp, "Logs riding the flood", "", "dmlogs", new Vector3(0, 1, 0), new Vector3(220, 4, 120)); s.showing = () => DamWater.Stage >= 1; s.range = 300;
            ByStage(g, false, true, true);
        }

        // ---------------- the crowd: two stands on a platform on the crest's reservoir side ----------------
        static void Grandstands()
        {
            var deck = DamLook.Mat(DamLook.ConcreteDark, true);
            foreach (var (x0, x1, name, go, seed) in new[] { (-215f, -125f, "DS crest W", true, 61), (40f, 130f, "DS crest E", false, 62) })
            {
                Box(scen, "Stand platform", L((x0 + x1) * .5f, CrestY - 2, DamTop + 8), new Vector3(x1 - x0 + 4, 4, 16), deck, null, false);
                Stand(name, L((x0 + x1) * .5f, CrestY, DamTop + 1.2f), Vector3.right, Vector3.forward, x1 - x0, 5, !go, go, seed);
            }
        }
        static void Stand(string name, Vector3 front, Vector3 along, Vector3 back, float length, int rows, bool passSound, bool goSound, int seed)
        {
            along = new Vector3(along.x, 0, along.z).normalized; back = new Vector3(back.x, 0, back.z).normalized;
            var face = Quaternion.LookRotation(-back); var rnd = new System.Random(seed * 97 + 5);
            var b = new MeshKit.Batch(); var seats = new List<Vector3>();
            const float depth = 2.2f, rise = 1.2f, spacing = 1.15f;
            for (int row = 0; row < rows; row++)
            {
                float h = (row + 1) * rise, d = row * depth + depth * .5f;
                b.Add(MeshKit.Cube, front + back * d + Vector3.up * (h * .5f), face, new Vector3(length, h, depth), row % 2 == 0 ? DamLook.Concrete : DamLook.Navy * 1.1f);
                for (float x = -length * .5f + .8f; x <= length * .5f - .8f; x += spacing) seats.Add(front + back * (d + .15f) + along * (x + (float)(rnd.NextDouble() - .5) * .3f) + Vector3.up * h);
            }
            float top = rows * rise, deep = rows * depth; int stripes = Mathf.RoundToInt(length / 4f);
            b.Add(MeshKit.Cube, front + back * (deep + .25f) + Vector3.up * (top + 2.2f) * .5f, face, new Vector3(length, top + 2.2f, .5f), DamLook.Concrete);
            for (float x = -length * .5f; x <= length * .5f + .01f; x += length / 6f) b.Add(MeshKit.LowCylinder, front + back * (deep * .45f) + along * x + Vector3.up * (top + 4.5f) * .5f, Quaternion.identity, new Vector3(.3f, top + 4.5f, .3f), DamLook.Safety);
            for (int i = 0; i < stripes; i++) b.Add(MeshKit.Cube, front + back * (deep * .55f) + along * (-length * .5f + (i + .5f) * length / stripes) + Vector3.up * (top + 4.6f), face * Quaternion.Euler(-12, 0, 0), new Vector3(length / stripes, .25f, deep * 1.05f), i % 2 == 0 ? DamLook.Safety : DamLook.Concrete);
            b.Object(scen, "Grandstand " + name, DamLook.Vc(true), false, true);
            Crowd.Build(scen, name, seats, along, Mathf.Max(30f, length * .5f + 12f) * .9f, passSound, goSound, seed);
        }

        // ---------------- signs (the coming form for the water as it is) ----------------
        static void Signs()
        {
            void S(float p, float side, Mode form, float post, bool s0, bool s1, bool s2)
            {
                var q = At(p); float y = RoadY(p); if (float.IsNaN(y)) y = q.y;
                var h = new GameObject("Sign " + form); h.transform.SetParent(scen, false);
                h.transform.SetPositionAndRotation(new Vector3(q.x, y, q.z) + Right(p) * side, Facing(p));
                WorldBuilder.Sign(h.transform, Vector3.zero, form, post); ByStage(h, s0, s1, s2);
            }
            S(M("chuteTop") - 70, RoadHalf + 3, Mode.Boat, 5, false, true, true);
            S(M("lip") - 70, ChuteHalf + 3, Mode.Fly, 5, true, true, false);                  // lap 1: wings optional (LB); lap 2: the lip throws you up
            S(M("landing") - 15, -RoadHalf - 6, Mode.Boat, 6, false, true, false);
            S(M("bend1") - 70, RoadHalf + 4, Mode.Dive, 18, false, false, true);
            S(M("climbFoot") - 30, RoadHalf + 4, Mode.Drive, 5, false, true, false);
            S(M("climbFoot") + 30, RoadHalf + 4, Mode.Boat, 18, false, false, true);
            S(M("climbFoot") + 95, RoadHalf + 4, Mode.Drive, 6, false, false, true);
            S(M("rejoin") - 70, -RoadHalf - 5, Mode.Drive, 6, true, false, false);            // the Canyon flight's wheels down
        }

        // ---------------- the route, visible the whole way ----------------
        static void RouteMarks()
        {
            var edge = new MeshKit.Batch(); var studs = new MeshKit.Batch(); var amber = new Color(1f, .62f, .12f);
            void Edges(float a, float b, float half)
            {
                for (float p = a; p < b - 3; p += 3)
                {
                    float y = RoadY(p + 1.5f); if (float.IsNaN(y) || float.IsNaN(RoadY(p)) || float.IsNaN(RoadY(p + 3))) continue;
                    var q = At(p + 1.5f); var r = Right(p + 1.5f); var f = Quaternion.LookRotation(At(p + 3) - At(p), Vector3.up);
                    foreach (float s in new[] { -1f, 1f }) edge.Add(MeshKit.Cube, new Vector3(q.x, y + .03f, q.z) + r * s * (half - .8f), f, new Vector3(.6f, .05f, 3.02f), new Color(.97f, .95f, .88f));
                }
                // cat's eyes: glowing studs along both edges every 12 m (they show the way under the flood in lap 3)
                int n = 0;
                for (float p = a; p < b; p += 12, n++)
                {
                    float y = RoadY(p); if (float.IsNaN(y)) continue; var q = At(p); var r = Right(p);
                    foreach (float s in new[] { -1f, 1f }) studs.Add(MeshKit.Cube, new Vector3(q.x, y + .12f, q.z) + r * s * (half - 2.2f), Facing(p), new Vector3(.7f, .22f, .7f), s < 0 ? amber : new Color(.95f, .95f, 1f));
                }
            }
            Edges(M("start") - 40, M("chuteTop") - 6, RoadHalf); Edges(M("lip") + PoolGap, M("F@2"), RoadHalf); Edges(M("chuteTop"), M("lip") - 2, ChuteHalf);
            edge.Object(scen, "Edge lines", DamLook.Vc(false, new Color(.15f, .15f, .14f)), false, false);
            studs.Object(scen, "Cat's eyes", GateMarker.Beacon, false, false);
            // lap 2: the flight off the flip bucket: two rings over the plunge pool (on lap 2's line)
            var rings = new MeshKit.Batch(); var gold = new Color(1f, .9f, .55f, .6f);
            foreach (float d in new[] { 30f, 75f })
            {
                float p = M("lip", 2) + d; var q = At(p); var frame = Quaternion.LookRotation(Dir(p), Vector3.up); const float R = 12f;
                for (int i = 0; i < 14; i++) { float a = i * 360f / 14; rings.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0) * R), frame * Quaternion.Euler(0, 0, a), new Vector3(.7f, R * .47f, .7f), gold); }
            }
            var rg = rings.Object(scen, "Flip bucket rings", GateMarker.Beacon, false, false); ByStage(rg, false, true, false);
            // lap 3: guide lights either side of the drowned road's line (looks only; lit at every stage, they read best under water)
            var gl = new MeshKit.Batch(); int k = 0;
            for (float p = M("bend1"); p < M("climbFoot") + 80; p += 16, k++)
            {
                if (p > M("hallIn") - 10 && p < M("hallOut") + 10) continue; var q = At(p); float y = RoadY(p); if (float.IsNaN(y)) continue;
                foreach (float s in new[] { -1f, 1f }) gl.Add(MeshKit.LowCylinder, new Vector3(q.x, y + 1.6f, q.z) + Right(p) * s * (RoadHalf + 2.5f), Quaternion.identity, new Vector3(.5f, 3.2f, .5f), k % 2 == 0 ? new Color(.3f, .95f, .9f) : DamLook.Yellow);
            }
            gl.Object(scen, "Road guide posts", GateMarker.Beacon, false, false);
        }
    }

    // ---- looks only ----
    // keeps its height at the valley's water (the trout, the flood's debris)
    public sealed class OnWater : MonoBehaviour
    {
        public float offset;
        void LateUpdate() { var p = transform.position; float y = (DamWater.Instance ? DamWater.Level : DamShame.ValleyLevel[0]) + offset; if (Mathf.Abs(p.y - y) > .001f) transform.position = new Vector3(p.x, y, p.z); }
    }
    // a piece of flood debris drifting down the valley (round a long loop: south down the middle, back up the sides, out of sight in the far
    // south), turning slowly; local to its parent (which rides the water)
    public sealed class FloodDrift : MonoBehaviour
    {
        public float at, speed = .01f, side, spin = 8;
        void Update()
        {
            at = (at + speed * Time.deltaTime) % 1f; float z = Mathf.Lerp(-120, -1150, at), x = DamShame.RiverX(z) + side + Mathf.Sin(at * 40 + side) * 6f;
            transform.localPosition = new Vector3(x, 0, z); transform.localRotation = Quaternion.Euler(0, spin * Time.time + side * 7, 0);
        }
    }
}
