using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 15 item 3: Big Top Breakdown's life and dressing: the midway (stalls, bunting, the wagon parade, the strongman, the tightrope
    // walker, the clowns' pile-up), the camp round the big top, the set-pieces along the river and in the lake (the elephants, the show boat,
    // the seals, the trapeze artists, the escape artist's tank) and the crowd (Gridiron Glory's fans in the stands). Round 8b's rule for the
    // set-pieces: big, close, in front (SetPiece: 8 % of the screen's height for 1 s). The performers and animals are stand-ins built from
    // simple shapes until Dan's Tripo models arrive (Docs/MODELS-NEEDED.md: the elephant, the sea lion, the ringmaster).
    public static partial class BigTop
    {
        // ---------------- stand-in figures ----------------
        // a cartoon person, feet at the origin, facing +z, h metres tall
        static GameObject Figure(Transform parent, string name, Vector3 at, Quaternion rot, float h, Color shirt, Color pants, Color hat, bool armsUp = false, bool nose = false)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = at; g.transform.localRotation = rot;   // local to the parent
            var b = new MeshKit.Batch(); float k = h / 1.8f;
            foreach (float s in new[] { -1f, 1f })
            {
                b.Add(MeshKit.LowCylinder, new Vector3(s * .14f, .42f, 0) * k, Quaternion.identity, new Vector3(.18f, .42f, .18f) * k, pants);
                b.Add(MeshKit.LowSphere, new Vector3(s * .14f, .05f, .06f) * k, Quaternion.identity, new Vector3(.22f, .12f, .32f) * k, new Color(.15f, .12f, .1f));
                if (armsUp) b.Add(MeshKit.LowCylinder, new Vector3(s * .36f, 1.62f, 0) * k, Quaternion.Euler(0, 0, s * -15), new Vector3(.13f, .36f, .13f) * k, shirt);
                else b.Add(MeshKit.LowCylinder, new Vector3(s * .36f, 1.08f, 0) * k, Quaternion.Euler(0, 0, s * 12), new Vector3(.13f, .32f, .13f) * k, shirt);
            }
            b.Add(MeshKit.LowSphere, new Vector3(0, 1.12f, 0) * k, Quaternion.identity, new Vector3(.62f, .7f, .42f) * k, shirt);
            b.Add(MeshKit.LowSphere, new Vector3(0, 1.62f, 0) * k, Quaternion.identity, new Vector3(.36f, .4f, .36f) * k, BigTopLook.Skin);
            if (nose) b.Add(MeshKit.LowSphere, new Vector3(0, 1.6f, .19f) * k, Quaternion.identity, Vector3.one * .12f * k, new Color(.95f, .1f, .1f));
            b.Add(MeshKit.LowCylinder, new Vector3(0, 1.88f, 0) * k, Quaternion.identity, new Vector3(.3f, .12f, .3f) * k, hat);
            b.Object(g.transform, "figure", BigTopLook.Vc(true), false, true);
            return g;
        }
        // a cartoon elephant (stand-in), feet at the origin, facing +z, about h tall at the back; its trunk raised (the spray comes from its tip)
        static GameObject Elephant(Transform parent, string name, Vector3 at, Quaternion rot, float h)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.SetPositionAndRotation(at, rot);
            var b = new MeshKit.Batch(); float k = h / 3f; var grey = BigTopLook.Grey; var dark = grey * .85f;
            b.Add(MeshKit.LowSphere, new Vector3(0, 2, 0) * k, Quaternion.identity, new Vector3(2.4f, 2.1f, 3.4f) * k, grey);
            foreach (var (x, z) in new[] { (-.7f, 1f), (.7f, 1f), (-.7f, -1f), (.7f, -1f) }) b.Add(MeshKit.LowCylinder, new Vector3(x, .6f, z) * k, Quaternion.identity, new Vector3(.75f, .6f, .75f) * k, dark);
            b.Add(MeshKit.LowSphere, new Vector3(0, 2.6f, 1.9f) * k, Quaternion.identity, new Vector3(1.6f, 1.6f, 1.5f) * k, grey);
            foreach (float s in new[] { -1f, 1f }) { b.Add(MeshKit.LowSphere, new Vector3(s * .95f, 2.6f, 1.6f) * k, Quaternion.Euler(0, s * 20, 0), new Vector3(.25f, 1.4f, 1.1f) * k, BigTopLook.Pink * .9f + grey * .2f); b.Add(MeshKit.LowSphere, new Vector3(s * .45f, 2.85f, 2.55f) * k, Quaternion.identity, new Vector3(.18f, .22f, .1f) * k, Color.black); }
            for (int i = 0; i < 6; i++) { float u = i / 5f; b.Add(MeshKit.LowCylinder, new Vector3(0, 2.2f + u * 1.6f, 2.6f + Mathf.Sin(u * 1.6f) * 1.1f) * k, Quaternion.Euler(-30 - u * 70, 0, 0), new Vector3(.42f - u * .16f, .32f, .42f - u * .16f) * k, grey); }
            // a red and gold circus saddle cloth, a feather
            b.Add(MeshKit.Cube, new Vector3(0, 3.05f, -.1f) * k, Quaternion.identity, new Vector3(2.3f, .12f, 1.8f) * k, BigTopLook.TopRed); b.Add(MeshKit.Cube, new Vector3(0, 3.1f, -.1f) * k, Quaternion.identity, new Vector3(2.4f, .1f, .25f) * k, BigTopLook.Gold);
            b.Add(MeshKit.LowCone, new Vector3(0, 3.6f, 1.9f) * k, Quaternion.identity, new Vector3(.25f, .6f, .25f) * k, BigTopLook.Pink);
            b.Object(g.transform, "elephant", BigTopLook.Vc(true), false, true);
            return g;
        }
        // a cartoon sea lion on its flippers, nose up, a ball balanced on it (stand-in)
        static GameObject Seal(Transform parent, string name, Vector3 at, Quaternion rot, float len, Color ball)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.SetPositionAndRotation(at, rot);
            var b = new MeshKit.Batch(); float k = len / 3f; var body = new Color(.25f, .24f, .28f);
            b.Add(MeshKit.LowSphere, new Vector3(0, .7f, -.6f) * k, Quaternion.Euler(-20, 0, 0), new Vector3(1.1f, 1f, 2.2f) * k, body);
            b.Add(MeshKit.LowSphere, new Vector3(0, 1.7f, .35f) * k, Quaternion.Euler(-55, 0, 0), new Vector3(.8f, .8f, 1.5f) * k, body);
            b.Add(MeshKit.LowSphere, new Vector3(0, 2.45f, .7f) * k, Quaternion.identity, new Vector3(.62f, .6f, .75f) * k, body);
            b.Add(MeshKit.LowSphere, new Vector3(0, 2.55f, 1.05f) * k, Quaternion.identity, new Vector3(.25f, .2f, .3f) * k, Color.black);
            foreach (float s in new[] { -1f, 1f }) { b.Add(MeshKit.LowSphere, new Vector3(s * .7f, .35f, .5f) * k, Quaternion.Euler(0, s * 40, 0), new Vector3(.6f, .12f, .9f) * k, body * .8f); b.Add(MeshKit.LowSphere, new Vector3(s * .22f, 2.6f, .98f) * k, Quaternion.identity, Vector3.one * .1f * k, Color.white); }
            b.Object(g.transform, "sea lion", BigTopLook.Vc(true), false, true);
            var ballT = new GameObject("ball"); ballT.transform.SetParent(g.transform, false); ballT.transform.localPosition = new Vector3(0, 3.35f, .85f) * k;
            var bb = new MeshKit.Batch(); bb.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, Vector3.one * 1.1f * k, ball); bb.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.Euler(0, 0, 90), new Vector3(1.12f, .3f, 1.12f) * k, BigTopLook.TopWhite);
            bb.Object(ballT.transform, "ball", BigTopLook.Vc(true), false, false); var sp = ballT.AddComponent<Spin>(); sp.axis = Vector3.up; sp.degPerSec = 160;
            var bob = ballT.AddComponent<Bob>(); bob.height = .25f * k; bob.period = 1.1f; bob.sway = 6;
            return g;
        }
        // a circus wagon (looks + a box collider): red / blue / green body, gold trim, big spoked wheels, a roof
        static GameObject WagonBody(Transform parent, string name, Color body, int seed)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            var b = new MeshKit.Batch();
            b.Add(MeshKit.Cube, new Vector3(0, 2.6f, 0), Quaternion.identity, new Vector3(3.6f, 3f, 9f), body);
            b.Add(MeshKit.Cube, new Vector3(0, 4.25f, 0), Quaternion.identity, new Vector3(4.1f, .4f, 9.6f), BigTopLook.Gold);
            b.Add(MeshKit.Cube, new Vector3(0, 4.6f, 0), Quaternion.Euler(0, 0, 0), new Vector3(3f, .5f, 9f), BigTopLook.TopWhite);
            foreach (float s in new[] { -1f, 1f })
            {
                b.Add(MeshKit.Cube, new Vector3(s * 1.82f, 2.6f, 0), Quaternion.identity, new Vector3(.1f, 2.2f, 7.6f), body * 1.15f);
                for (int i = 0; i < 5; i++) b.Add(MeshKit.Cube, new Vector3(s * 1.86f, 2.6f, -3f + i * 1.5f), Quaternion.identity, new Vector3(.1f, 2.2f, .18f), BigTopLook.Gold);
                foreach (float z in new[] { -3f, 3f })
                {
                    b.Add(MeshKit.LowCylinder, new Vector3(s * 2f, 1.1f, z), Quaternion.Euler(0, 0, 90), new Vector3(2.2f, .2f, 2.2f), BigTopLook.TopRed);
                    for (int sp = 0; sp < 4; sp++) b.Add(MeshKit.Cube, new Vector3(s * 2.1f, 1.1f, z), Quaternion.Euler(sp * 45, 0, 0), new Vector3(.12f, 2f, .16f), BigTopLook.Gold);
                }
            }
            b.Object(g.transform, "wagon", BigTopLook.Vc(true), false, true);
            g.layer = ShiftVehicle.ObstacleLayer;
            var col = g.AddComponent<BoxCollider>(); col.center = new Vector3(0, 2.5f, 0); col.size = new Vector3(3.8f, 4.4f, 9.4f);
            var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
            return g;
        }

        // ---------------- the midway ----------------
        static void Midway()
        {
            var rnd = new System.Random(7);
            var stripes = new[] { (BigTopLook.TopRed, BigTopLook.TopWhite), (BigTopLook.Royal, BigTopLook.Gold), (BigTopLook.Teal, BigTopLook.TopWhite), (BigTopLook.Purple, BigTopLook.Gold), (BigTopLook.Orange, BigTopLook.TopWhite) };
            // stalls both sides: a counter, a striped awning, a sign board
            var stalls = new MeshKit.Batch(); int n = 0;
            for (float p = M("G0") + 30; p < M("jC") - 60; p += 24, n++)
            {
                var q = At(p); q.y = G; var f = Facing(p); var r = Right(p); float half = (p > M("wagons") - 30 && p < M("wagonsEnd") + 30 ? WagonRoadHalf : RoadHalf);
                if (Mathf.Abs(p - (M("wagons") - 25)) < 16 || Mathf.Abs(p - (M("wagonsEnd") + 25)) < 16) continue;
                foreach (float s in new[] { -1f, 1f })
                {
                    if (Mathf.Abs(p - M("midway")) < 30 || Mathf.Abs(p - (M("wagons") + 110)) < 22) continue;   // the tightrope's and the strongman's places
                    var (ca, cb) = stripes[(n + (s > 0 ? 2 : 0)) % stripes.Length];
                    var at = q + r * s * (half + 7.5f); var face = Quaternion.LookRotation(-r * s);
                    stalls.Add(MeshKit.Cube, at + Vector3.up * .9f, face, new Vector3(7, 1.8f, 3), BigTopLook.Wood);
                    stalls.Add(MeshKit.Cube, at + Vector3.up * 2.4f - r * s * 1.2f, face, new Vector3(7, 3f, .3f), cb * .9f);
                    for (int i = 0; i < 6; i++) stalls.Add(MeshKit.Cube, at + face * new Vector3(-3.5f + (i + .5f) * 7f / 6, 4.6f, .4f), face * Quaternion.Euler(-24, 0, 0), new Vector3(7f / 6 + .02f, .25f, 4.4f), i % 2 == 0 ? ca : cb);
                    foreach (float x in new[] { -3.3f, 3.3f }) stalls.Add(MeshKit.Cube, at + face * new Vector3(x, 2.2f, 1.6f), face, new Vector3(.25f, 4.4f, .25f), BigTopLook.TopWhite);
                    stalls.Add(MeshKit.Cube, at + face * new Vector3(0, 6.2f, -.6f), face, new Vector3(5, 1.2f, .25f), BigTopLook.Gold);
                }
            }
            stalls.Object(scen, "Midway stalls", BigTopLook.Vc(true), false, true);
            // bunting across the road every 45 m (pennants flap)
            var bunt = new MeshKit.Batch(); var cols = new[] { BigTopLook.TopRed, BigTopLook.Gold, BigTopLook.Royal, BigTopLook.Teal, BigTopLook.Pink };
            for (float p = M("G0") + 50; p < M("jC") - 40; p += 45)
            {
                var q = At(p); q.y = G; var r = Right(p); float half = WagonRoadHalf + 4;
                foreach (float s in new[] { -1f, 1f }) bunt.Add(MeshKit.LowCylinder, q + r * s * half + Vector3.up * 5.5f, Quaternion.identity, new Vector3(.35f, 5.5f, .35f), BigTopLook.TopWhite);
                for (int i = 0; i <= 24; i++) { float u = i / 24f; var pt = q + r * Mathf.Lerp(-half, half, u) + Vector3.up * (10.5f - Mathf.Sin(u * Mathf.PI) * 2.2f); bunt.Add(MeshKit.Cube, pt + Vector3.down * .55f, Quaternion.LookRotation(r) * Quaternion.Euler(0, 0, 45), new Vector3(.9f, .9f, .06f), cols[i % cols.Length]); }
            }
            bunt.Object(scen, "Bunting", BigTopLook.Vc(false), false, false);
            // the wagon parade: five wagons each side, staggered, rolling north at 11 m/s along the side lanes (both ends under a canvas
            // gateway); the middle of the road (+-7.5 m) is always clear
            float from = M("wagons") - 25, to = M("wagonsEnd") + 25, len = to - from;
            var bodies = new[] { BigTopLook.TopRed, BigTopLook.Royal, BigTopLook.Teal * .9f, BigTopLook.Purple, BigTopLook.Orange };
            Wagon first = null;
            for (int i = 0; i < 10; i++)
            {
                float side = (i % 2 == 0 ? -1 : 1) * 10.5f;
                var go = WagonBody(root, "Circus wagon " + (i + 1), bodies[i % bodies.Length], i); var w = go.AddComponent<Wagon>();
                w.course = c; w.from = from; w.to = to; w.side = side; w.speed = 11; w.at = from + len * i / 10f + 5; if (!first) first = w;
            }
            foreach (float p in new[] { from, to })
                foreach (float s in new[] { -1f, 1f })
                {
                    var q = At(p); q.y = G; var r = Right(p); var f = Facing(p); var ctr = q + r * s * 10.5f;
                    var arch = new MeshKit.Batch();
                    foreach (float x in new[] { -2.6f, 2.6f }) foreach (float z in new[] { -6f, 6f }) arch.Add(MeshKit.Cube, ctr + f * new Vector3(x, 3.5f, z), f, new Vector3(.4f, 7f, .4f), BigTopLook.Gold);
                    for (int k = 0; k < 5; k++) arch.Add(MeshKit.Cube, ctr + f * new Vector3(-2.75f + (k + .5f) * 1.1f, 7.2f, 0), f, new Vector3(1.12f, .5f, 13), k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite);
                    // canvas curtains at both ends of the gateway (the wagon passes through them)
                    foreach (float z in new[] { -6.2f, 6.2f }) for (int k = 0; k < 4; k++) arch.Add(MeshKit.Cube, ctr + f * new Vector3(-1.95f + k * 1.3f, 4.5f, z), f, new Vector3(1.25f, 5.2f, .12f), k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite);
                    arch.Add(MeshKit.Cube, ctr + f * new Vector3(s * 2.75f, 4.5f, 0), f, new Vector3(.12f, 5.2f, 12), BigTopLook.TopRed);
                    arch.Object(scen, "Wagon gateway", BigTopLook.Vc(true), false, true);
                }
            if (first) SetPiece.Make(first.gameObject, "Wagon parade", "The Midway", "btwagons", new Vector3(0, 2.5f, 0), new Vector3(4, 5, 10));
            // the strongman (a giant inflatable, wobbling in the wind) beside the road, his barbell held over it
            float sp = M("wagons") + 110; var sq = At(sp); sq.y = G; var sr = Right(sp);
            var sm = new GameObject("Strongman"); sm.transform.SetParent(scen, false); sm.transform.SetPositionAndRotation(sq - sr * (WagonRoadHalf + 12), Quaternion.LookRotation(sr));
            var smb = new GameObject("wobble"); smb.transform.SetParent(sm.transform, false); var bo = smb.AddComponent<Bob>(); bo.height = .4f; bo.period = 3.2f; bo.sway = 3;
            Figure(smb.transform, "strongman", Vector3.zero, Quaternion.identity, 17, new Color(.85f, .2f, .15f), new Color(.15f, .15f, .2f), new Color(.15f, .1f, .08f), true);
            var bar = new MeshKit.Batch(); bar.Add(MeshKit.LowCylinder, new Vector3(0, 19.4f, 13), Quaternion.Euler(90, 0, 0), new Vector3(.8f, 16, .8f), BigTopLook.Steel);
            foreach (float z in new[] { -2f, 28f }) bar.Add(MeshKit.LowSphere, new Vector3(0, 19.4f, z), Quaternion.identity, Vector3.one * 6, new Color(.12f, .12f, .14f));
            bar.Object(smb.transform, "barbell", BigTopLook.Vc(true), false, true);
            SetPiece.Make(smb, "Strongman", "The Midway", "btstrongman", new Vector3(0, 10, 4), new Vector3(8, 20, 10));
            // the tightrope walker high over the midway (a wire between two masts, walking back and forth with a balance pole)
            float tp = M("midway"); var tq = At(tp); tq.y = G; var tr = Right(tp);
            var masts = new MeshKit.Batch(); float tH = 22f;
            foreach (float s in new[] { -1f, 1f }) { masts.Add(MeshKit.LowCylinder, tq + tr * s * 28 + Vector3.up * tH * .5f, Quaternion.identity, new Vector3(.8f, tH * .5f, .8f), BigTopLook.TopRed); masts.Add(MeshKit.Cube, tq + tr * s * 28 + Vector3.up * (tH - .2f), Quaternion.identity, new Vector3(3, .4f, 3), BigTopLook.Gold); }
            masts.Add(MeshKit.Cube, tq + Vector3.up * tH, Quaternion.LookRotation(tr), new Vector3(.12f, .12f, 56), new Color(.25f, .25f, .25f));
            masts.Object(scen, "Tightrope masts", BigTopLook.Vc(true), false, false);
            var walk = new GameObject("Tightrope walker"); walk.transform.SetParent(scen, false); walk.transform.SetPositionAndRotation(tq + Vector3.up * tH, Quaternion.LookRotation(Flat(Dir(tp))));
            var pace = new GameObject("pace"); pace.transform.SetParent(walk.transform, false); var pc = pace.AddComponent<Pace>(); pc.half = 16; pc.speed = 2.2f;
            Figure(pace.transform, "walker", Vector3.zero, Quaternion.Euler(0, 90, 0), 4.5f, BigTopLook.Pink, BigTopLook.TopWhite, BigTopLook.Gold);
            var polb = new MeshKit.Batch(); polb.Add(MeshKit.Cube, new Vector3(0, 2.6f, 0), Quaternion.identity, new Vector3(.12f, .12f, 10), BigTopLook.Gold); polb.Object(pace.transform, "pole", BigTopLook.Vc(true), false, false);
            SetPiece.Make(pace, "Tightrope walker", "The Midway", "bttightrope", new Vector3(0, 2.3f, 0), new Vector3(3, 5, 10));
            // the clowns' pit: its floor, the clown cars' pile-up in it, and a tiny car beside the road with clowns tumbling out of it
            var pq = At(M("jC") + PitLen * .5f);
            Box(root, "Clown pit floor", new Vector3(pq.x, (Floor + PitFloor) * .5f, pq.z), new Vector3(PitHalf * 2, PitFloor - Floor, PitLen), BigTopLook.Mat(BigTopLook.Dirt), Facing(M("jC")), true, ShiftVehicle.ObstacleLayer);
            var pile = new MeshKit.Batch(); var carCols = new[] { BigTopLook.Pink, BigTopLook.Teal, BigTopLook.Gold, BigTopLook.Purple, BigTopLook.Orange, BigTopLook.Royal };
            for (int i = 0; i < 9; i++)
            {
                var at = pq + new Vector3(Rand(rnd, -14, 14), PitFloor - pq.y + 1.2f + (i / 3) * 1.6f, Rand(rnd, -12, 12)); var rot = Quaternion.Euler(Rand(rnd, -25, 25), Rand(rnd, 0, 360), Rand(rnd, -30, 30));
                pile.Add(MeshKit.Cube, at, rot, new Vector3(2.4f, 1.4f, 3.6f), carCols[i % carCols.Length]); pile.Add(MeshKit.LowSphere, at + rot * new Vector3(0, .9f, -.3f), rot, new Vector3(2, 1.2f, 2), carCols[(i + 2) % carCols.Length]);
                foreach (float s in new[] { -1f, 1f }) foreach (float z in new[] { -1.2f, 1.2f }) pile.Add(MeshKit.LowCylinder, at + rot * new Vector3(s * 1.25f, -.6f, z), rot * Quaternion.Euler(0, 0, 90), new Vector3(1.1f, .3f, 1.1f), new Color(.1f, .1f, .1f));
            }
            pile.Object(scen, "Clown car pile-up", BigTopLook.Vc(true), false, true);
            float cp0 = M("jC") - 20; var cq = At(cp0); cq.y = G; var cr = Right(cp0);
            var tiny = new MeshKit.Batch(); var tcar = cq - cr * (RoadHalf + 9); tiny.Add(MeshKit.Cube, tcar + Vector3.up * 1.2f, Facing(cp0), new Vector3(2.6f, 1.6f, 3.4f), BigTopLook.Gold); tiny.Add(MeshKit.LowSphere, tcar + Vector3.up * 2.3f, Facing(cp0), new Vector3(2.2f, 1.4f, 2.2f), BigTopLook.Pink);
            foreach (float s in new[] { -1f, 1f }) foreach (float z in new[] { -1.1f, 1.1f }) tiny.Add(MeshKit.LowCylinder, tcar + Vector3.up * .55f + Facing(cp0) * new Vector3(s * 1.3f, 0, z), Facing(cp0) * Quaternion.Euler(0, 0, 90), new Vector3(1.1f, .3f, 1.1f), new Color(.1f, .1f, .1f));
            tiny.Object(scen, "Tiny clown car", BigTopLook.Vc(true), false, true);
            var clownsGo = new GameObject("Clowns"); clownsGo.transform.SetParent(scen, false); clownsGo.transform.SetPositionAndRotation(tcar + Vector3.up * 2.4f, Quaternion.LookRotation(cr));
            var clowns = new List<Transform>(); var suits = new[] { (BigTopLook.Gold, BigTopLook.Royal), (BigTopLook.Teal, BigTopLook.TopRed), (BigTopLook.Pink, BigTopLook.Purple), (BigTopLook.Orange, BigTopLook.Teal), (BigTopLook.TopWhite, BigTopLook.TopRed) };
            for (int i = 0; i < 5; i++) { var cl = Figure(clownsGo.transform, "clown", Vector3.zero, Quaternion.identity, 2.6f, suits[i].Item1, suits[i].Item2, BigTopLook.TopRed, true, true); clowns.Add(cl.transform); }
            var arcs = clownsGo.AddComponent<Arcs>(); arcs.items = clowns.ToArray(); arcs.throwVel = new Vector3(0, 11, 3); arcs.spread = 2.5f; arcs.gravity = 16; arcs.life = 1.5f; arcs.spin = 300;
            var clownSet = new GameObject("Clowns (set-piece)"); clownSet.transform.SetParent(scen, false); clownSet.transform.position = pq;
            SetPiece.Make(clownSet, "Clowns", "The Midway", "btclowns", new Vector3(0, -1, 0), new Vector3(PitHalf * 2, 8, PitLen));
        }

        // ---------------- the camp round the big top: small tents, caravans, hay, flags, a water tower ----------------
        static void Camp()
        {
            var rnd = new System.Random(23); var b = new MeshKit.Batch(); var flags = new List<Vector3>();
            var stripes = new[] { (BigTopLook.TopRed, BigTopLook.TopWhite), (BigTopLook.Royal, BigTopLook.Gold), (BigTopLook.Teal, BigTopLook.TopWhite), (BigTopLook.Purple, BigTopLook.Gold) };
            int placed = 0;
            for (int i = 0; i < 400 && placed < 26; i++)
            {
                float x = Rand(rnd, -420, 470), z = Rand(rnd, -250, 1500); float R = Rand(rnd, 9, 18);
                if (Near(x, z, R + 40)) continue; placed++;
                var at = L(x, G, z); var (ca, cb) = stripes[placed % stripes.Length]; float h = R * 1.1f, wall = 4f;
                for (int k = 0; k < 16; k++) { float t = k * 22.5f * Mathf.Deg2Rad; b.Add(MeshKit.Cube, at + new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)) * R + Vector3.up * wall * .5f, Quaternion.Euler(0, -k * 22.5f + 90, 0), new Vector3(2 * R * Mathf.Sin(11.25f * Mathf.Deg2Rad) + .2f, wall, .3f), k % 2 == 0 ? ca : cb); }
                for (int k = 0; k < 16; k++) { float t = (k + .5f) * 22.5f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)); float sl = Mathf.Atan2(h, R) * Mathf.Rad2Deg; float run = Mathf.Sqrt(R * R + h * h); b.Add(MeshKit.Cube, at + d * R * .5f + Vector3.up * (wall + h * .5f), Quaternion.LookRotation(d) * Quaternion.Euler(-sl, 0, 0), new Vector3(2 * R * Mathf.Sin(11.25f * Mathf.Deg2Rad) * .55f + .2f, .3f, run), k % 2 == 0 ? ca : cb); }
                flags.Add(at + Vector3.up * (wall + h + 2));
                b.Add(MeshKit.LowCylinder, at + Vector3.up * (wall + h + 1), Quaternion.identity, new Vector3(.25f, 1.2f, .25f), BigTopLook.Gold);
            }
            for (int i = 0; i < 300 && placed < 60; i++)
            {
                float x = Rand(rnd, -420, 470), z = Rand(rnd, -250, 1500); if (Near(x, z, 30)) continue; placed++;
                var at = L(x, G, z); var rot = Quaternion.Euler(0, Rand(rnd, 0, 360), 0); var col = new[] { BigTopLook.Teal, BigTopLook.Royal, BigTopLook.TopRed, BigTopLook.Purple }[placed % 4];
                if (placed % 3 == 0) { b.Add(MeshKit.LowCylinder, at + Vector3.up * .8f, rot * Quaternion.Euler(90, 0, 0), new Vector3(1.6f, 1.2f, 1.6f), BigTopLook.Gold * .9f); b.Add(MeshKit.LowCylinder, at + Vector3.up * .8f + rot * Vector3.right * 2.2f, rot * Quaternion.Euler(90, 0, 0), new Vector3(1.6f, 1.2f, 1.6f), BigTopLook.Gold * .85f); continue; }   // hay bales
                b.Add(MeshKit.Cube, at + Vector3.up * 2.2f, rot, new Vector3(3.4f, 3f, 8), col); b.Add(MeshKit.Cube, at + Vector3.up * 3.9f, rot, new Vector3(3.6f, .4f, 8.4f), BigTopLook.TopWhite);
                foreach (float s in new[] { -1f, 1f }) foreach (float zz in new[] { -2.5f, 2.5f }) b.Add(MeshKit.LowCylinder, at + rot * new Vector3(s * 1.8f, .9f, zz), rot * Quaternion.Euler(0, 0, 90), new Vector3(1.8f, .2f, 1.8f), BigTopLook.TopRed);
            }
            // a water tower near the river
            var wt = L(450, G, 980); if (!Near(450, 980, 18)) { foreach (var (x, z) in new[] { (-3f, -3f), (3f, -3f), (-3f, 3f), (3f, 3f) }) b.Add(MeshKit.LowCylinder, wt + new Vector3(x, 9, z), Quaternion.identity, new Vector3(.5f, 9, .5f), BigTopLook.WoodDark); b.Add(MeshKit.LowCylinder, wt + Vector3.up * 22, Quaternion.identity, new Vector3(9, 4, 9), BigTopLook.Wood); b.Add(MeshKit.LowCone, wt + Vector3.up * 27.5f, Quaternion.identity, new Vector3(10, 2, 10), BigTopLook.TopRed); }
            b.Object(scen, "Circus camp", BigTopLook.Vc(true), false, true);
            foreach (var f in flags) { var fg = new GameObject("Camp flag"); fg.transform.SetParent(scen, false); fg.transform.position = f; var fb = new MeshKit.Batch(); fb.Add(MeshKit.Cube, new Vector3(0, 0, 1.6f), Quaternion.identity, new Vector3(.1f, 1.6f, 3.2f), BigTopLook.Gold); fb.Object(fg.transform, "flag", BigTopLook.Vc(false), false, false); fg.AddComponent<Flap>().deg = 30; Detail(fg); }
        }

        // ---------------- set-pieces along the river, under the tent and in the lake ----------------
        static void SetPieces()
        {
            // the elephants on the west bank, spraying water over the river (arcs of droplets)
            float ep = M("elephants");
            for (int i = 0; i < 2; i++)
            {
                var at = L(791 - i * 8, G, At(ep).z - O.z - 18 + i * 34); var el = Elephant(scen, "Elephant " + (i + 1), at, Quaternion.Euler(0, 90 - i * 10, 0), 7.5f);
                var spray = new GameObject("spray"); spray.transform.SetParent(el.transform, false); spray.transform.localPosition = new Vector3(0, 9.5f, 9.2f);
                var drops = new List<Transform>(); var mat = JurassicLook.FallMat(.7f);
                for (int k = 0; k < 18; k++) { var d = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(d.GetComponent<Collider>()); d.transform.SetParent(spray.transform, false); d.transform.localScale = Vector3.one * Rand(.6f, 1.2f); var r = d.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; drops.Add(d.transform); }
                var a = spray.AddComponent<Arcs>(); a.items = drops.ToArray(); a.throwVel = new Vector3(0, 9, 13); a.spread = 1.5f; a.gravity = 12; a.life = 1.9f; a.spin = 0;
                if (i == 0) SetPiece.Make(el, "Elephants", "The River", "btelephants", new Vector3(0, 5, 4), new Vector3(9, 11, 14));
                Bob bob = el.AddComponent<Bob>(); bob.height = .15f; bob.period = 2.5f; bob.sway = 2;
            }
            // the paddle-steamer show boat moored on the east bank (a turning paddle wheel, two funnels, "SHOWBOAT" in bulbs)
            var sq = At(M("steamer") + 30); var boat = new GameObject("Show boat"); boat.transform.SetParent(scen, false); boat.transform.SetPositionAndRotation(new Vector3(O.x + 901, Water, sq.z), Quaternion.Euler(0, 180, 0));
            var hull = new MeshKit.Batch();
            hull.Add(MeshKit.Cube, new Vector3(0, .6f, 0), Quaternion.identity, new Vector3(13, 3.2f, 54), BigTopLook.TopWhite); hull.Add(MeshKit.Cube, new Vector3(0, -.6f, 0), Quaternion.identity, new Vector3(13.2f, 1.2f, 54.2f), BigTopLook.TopRed);
            hull.Add(MeshKit.Cube, new Vector3(0, 4.2f, -2), Quaternion.identity, new Vector3(11, 4, 36), BigTopLook.TopWhite); hull.Add(MeshKit.Cube, new Vector3(0, 6.5f, -2), Quaternion.identity, new Vector3(12, .6f, 38), BigTopLook.Royal);
            hull.Add(MeshKit.Cube, new Vector3(0, 8.6f, 4), Quaternion.identity, new Vector3(8, 3.6f, 12), BigTopLook.TopWhite); hull.Add(MeshKit.Cube, new Vector3(0, 10.6f, 4), Quaternion.identity, new Vector3(9, .5f, 13), BigTopLook.TopRed);
            foreach (float x in new[] { -2.5f, 2.5f }) { hull.Add(MeshKit.LowCylinder, new Vector3(x, 15, 9), Quaternion.identity, new Vector3(1.4f, 5, 1.4f), new Color(.15f, .15f, .15f)); hull.Add(MeshKit.LowCylinder, new Vector3(x, 20.2f, 9), Quaternion.identity, new Vector3(2.2f, .4f, 2.2f), BigTopLook.Gold); }
            for (int i = 0; i < 9; i++) hull.Add(MeshKit.Cube, new Vector3(-5.6f, 4.2f, -17 + i * 4), Quaternion.identity, new Vector3(.2f, 2, 2), BigTopLook.Gold * .9f);
            hull.Object(boat.transform, "hull", BigTopLook.Vc(true), false, true);
            Box(boat.transform, "Show boat hull (solid)", boat.transform.position + Vector3.up * .5f, new Vector3(13, 6, 54), BigTopLook.Mat(BigTopLook.TopWhite), boat.transform.rotation, true, ShiftVehicle.ObstacleLayer).GetComponent<Renderer>().enabled = false;
            var wheel = new GameObject("paddle wheel"); wheel.transform.SetParent(boat.transform, false); wheel.transform.localPosition = new Vector3(0, 3, -29); wheel.transform.localRotation = Quaternion.identity;
            var wb = new MeshKit.Batch(); for (int k = 0; k < 10; k++) wb.Add(MeshKit.Cube, Quaternion.Euler(k * 36, 0, 0) * new Vector3(0, 3.5f, 0), Quaternion.Euler(k * 36, 0, 0), new Vector3(10, 3, .4f), BigTopLook.TopRed);
            wb.Add(MeshKit.LowCylinder, Vector3.zero, Quaternion.Euler(0, 0, 90), new Vector3(1.4f, 5.4f, 1.4f), BigTopLook.Gold); wb.Object(wheel.transform, "paddles", BigTopLook.Vc(true), false, true);
            var spn = wheel.AddComponent<Spin>(); spn.axis = Vector3.right; spn.degPerSec = 50;
            Label("SHOWBOAT", boat.transform.position + boat.transform.rotation * new Vector3(5.6f, 4.2f, -2), Quaternion.LookRotation(boat.transform.rotation * Vector3.right), 1.6f, BigTopLook.Gold);
            SetPiece.Make(boat, "Show boat", "The River", "btsteamer", new Vector3(0, 6, 0), new Vector3(13, 20, 54)).range = 700;
            // sea lions on rocks by the line, balancing balls
            float slp = M("seals"); var slq = At(slp); var slr = Right(slp); var rockM = BigTopLook.Mat(BigTopLook.Grey, true);
            var seals = new GameObject("Sea lions"); seals.transform.SetParent(scen, false); seals.transform.position = new Vector3(slq.x, 0, slq.z) + slr * 30;
            var balls = new[] { BigTopLook.TopRed, BigTopLook.Gold, BigTopLook.Royal };
            for (int i = 0; i < 3; i++)
            {
                var at = seals.transform.position + slr * (i - 1) * 2 + Flat(Dir(slp)) * (i - 1) * 11;
                Cyl(root, "Seal rock", new Vector3(at.x, (Floor + 2.2f) * .5f, at.z), 4.2f, 2.2f - Floor, rockM, true, ShiftVehicle.ObstacleLayer);
                Seal(scen, "Sea lion " + (i + 1), new Vector3(at.x, 2.2f, at.z), Quaternion.LookRotation(-slr), 4.2f, balls[i]);
            }
            SetPiece.Make(seals, "Sea lions", "The River", "btseals", new Vector3(0, 5, 0), new Vector3(10, 8, 28));
            // the trapeze artists swinging beside the hoops under the tent (looks only, 16 m off the line)
            foreach (var (h, side, ph) in new[] { ("hoop1", 1f, 0f), ("hoop2", -1f, 1.3f), ("hoop3", 1f, 2.1f) })
            {
                var q = At(M(h)); var r = Right(M(h)); var pivot = q + r * side * 17; float rr = new Vector2(pivot.x - O.x - TentC.x, pivot.z - O.z - TentC.z).magnitude; pivot.y = G + RoofAt(rr) - 1;
                var tz = new GameObject("Trapeze " + h); tz.transform.SetParent(scen, false); tz.transform.SetPositionAndRotation(pivot, Facing(M(h)));   // swings along the line, never across it
                float ropeL = Mathf.Max(6, pivot.y - (q.y + 1));
                var tb = new MeshKit.Batch(); foreach (float x in new[] { -1.2f, 1.2f }) tb.Add(MeshKit.Cube, new Vector3(x, -ropeL * .5f, 0), Quaternion.identity, new Vector3(.1f, ropeL, .1f), new Color(.85f, .8f, .6f));
                tb.Add(MeshKit.LowCylinder, new Vector3(0, -ropeL, 0), Quaternion.Euler(0, 0, 90), new Vector3(.2f, 1.3f, .2f), BigTopLook.Gold);
                tb.Object(tz.transform, "ropes", BigTopLook.Vc(true), false, false);
                Figure(tz.transform, "trapeze artist", new Vector3(0, -ropeL - 3.3f, 0), Quaternion.identity, 3.4f, h == "hoop2" ? BigTopLook.Teal : BigTopLook.Pink, BigTopLook.TopWhite, BigTopLook.Gold, true).transform.localPosition = new Vector3(0, -ropeL - 3.3f, 0);
                var sw = tz.AddComponent<Swing>(); sw.deg = 38; sw.period = 3.6f; sw.offset = ph;
                if (h == "hoop2") SetPiece.Make(tz, "Trapeze artists", "Human Cannonball", "bttrapeze", new Vector3(0, -ropeL - 1.6f, 0), new Vector3(4, 5, 4));
            }
            // the escape artist's giant water tank on the lake bed beside the line: glass, a performer inside waving, bubbles rising
            float tkp = M("tank"); var tkq = At(tkp); var tkr = Right(tkp); var tank = new GameObject("Escape tank"); tank.transform.SetParent(scen, false);
            tank.transform.SetPositionAndRotation(new Vector3(tkq.x, Floor, tkq.z) + tkr * 20, Quaternion.LookRotation(-tkr));
            var glass = JurassicLook.FallMat(.22f); var frame = new MeshKit.Batch();
            foreach (var (x, z) in new[] { (-5f, -5f), (5f, -5f), (-5f, 5f), (5f, 5f) }) frame.Add(MeshKit.Cube, new Vector3(x, 8, z), Quaternion.identity, new Vector3(.6f, 16, .6f), BigTopLook.Gold);
            frame.Add(MeshKit.Cube, new Vector3(0, 16.2f, 0), Quaternion.identity, new Vector3(11, .6f, 11), BigTopLook.Gold); frame.Add(MeshKit.Cube, new Vector3(0, .5f, 0), Quaternion.identity, new Vector3(11.4f, 1, 11.4f), BigTopLook.TopRed);
            frame.Object(tank.transform, "tank frame", BigTopLook.Vc(true), false, true);
            foreach (var (pos, rot, sc) in new[] { (new Vector3(0, 8, -5), Quaternion.identity, new Vector3(10, 15, .2f)), (new Vector3(0, 8, 5), Quaternion.identity, new Vector3(10, 15, .2f)), (new Vector3(-5, 8, 0), Quaternion.identity, new Vector3(.2f, 15, 10)), (new Vector3(5, 8, 0), Quaternion.identity, new Vector3(.2f, 15, 10)) })
            { var gp = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(gp.GetComponent<Collider>()); gp.transform.SetParent(tank.transform, false); gp.transform.localPosition = pos; gp.transform.localRotation = rot; gp.transform.localScale = sc; gp.GetComponent<Renderer>().sharedMaterial = glass; gp.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            var esc = Figure(tank.transform, "escape artist", new Vector3(0, 4, 0), Quaternion.identity, 5.5f, new Color(.12f, .12f, .14f), new Color(.12f, .12f, .14f), BigTopLook.TopRed, true);
            esc.transform.localPosition = new Vector3(0, 4, 0); var eb = esc.AddComponent<Bob>(); eb.height = .8f; eb.period = 2.4f; eb.sway = 12;
            var chain = new MeshKit.Batch(); for (int i = 0; i < 10; i++) chain.Add(MeshKit.Cube, new Vector3(0, 1 + i * .45f, 0), Quaternion.Euler(0, i * 90, 0), new Vector3(.5f, .5f, .15f), BigTopLook.Steel); chain.Object(tank.transform, "chain", BigTopLook.Vc(true), false, false);
            var bub = new GameObject("bubbles"); bub.transform.SetParent(tank.transform, false); bub.transform.localPosition = new Vector3(0, 10, 0); var bl = new List<Transform>(); var bm = JurassicLook.FallMat(.6f);
            for (int k = 0; k < 10; k++) { var d = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(d.GetComponent<Collider>()); d.transform.SetParent(bub.transform, false); d.transform.localScale = Vector3.one * .5f; d.GetComponent<Renderer>().sharedMaterial = bm; bl.Add(d.transform); }
            var ba = bub.AddComponent<Arcs>(); ba.items = bl.ToArray(); ba.throwVel = new Vector3(0, 2, 0); ba.spread = 1.2f; ba.gravity = -3; ba.life = 2.2f; ba.spin = 0;
            SetPiece.Make(tank, "Escape tank", "Sunken Circus", "bttank", new Vector3(0, 8, 0), new Vector3(11, 16, 11));
            var wg = new GameObject("Sunken wagon (set-piece)"); wg.transform.SetParent(scen, false); wg.transform.SetPositionAndRotation(At(M("wagon")), Facing(M("wagon")));
            SetPiece.Make(wg, "Sunken wagon", "Sunken Circus", "btsunken", Vector3.zero, new Vector3(22, 14, 34));
        }

        // ---------------- the crowd: the start, round the ring under the big top, the river bank at the splashdown ----------------
        static void Grandstands()
        {
            foreach (float s in new[] { -1f, 1f }) Stand(s < 0 ? "BT start W" : "BT start E", L(s * 27, G, -20), Vector3.forward, Vector3.right * s, 56, 5, false, true, s < 0 ? 51 : 52);
            int seed = 53;
            foreach (float ang in new[] { -40f, 5f, 50f, 95f, 140f, 185f })
            {
                float t = ang * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)); var along = new Vector3(-d.z, 0, d.x);
                Stand("BT ring " + ang, L(TentC.x, G, TentC.z) + d * 91, along, d, 44, 5, ang == 50f || ang == 140f, false, seed++);
            }
            Stand("BT river bank", L(783, G, At(M("splash") + 40).z - O.z), Vector3.forward, -Vector3.right, 44, 4, true, false, seed++);
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
                b.Add(MeshKit.Cube, front + back * d + Vector3.up * (h * .5f), face, new Vector3(length, h, depth), row % 2 == 0 ? BigTopLook.TopWhite : BigTopLook.Royal * .9f);
                for (float x = -length * .5f + .8f; x <= length * .5f - .8f; x += spacing) seats.Add(front + back * (d + .15f) + along * (x + (float)(rnd.NextDouble() - .5) * .3f) + Vector3.up * h);
            }
            float top = rows * rise, deep = rows * depth; int stripes = Mathf.RoundToInt(length / 4f);
            b.Add(MeshKit.Cube, front + back * (deep + .25f) + Vector3.up * (top + 2.2f) * .5f, face, new Vector3(length, top + 2.2f, .5f), BigTopLook.TopWhite);
            for (float x = -length * .5f; x <= length * .5f + .01f; x += length / 6f) b.Add(MeshKit.LowCylinder, front + back * (deep * .45f) + along * x + Vector3.up * (top + 4.5f) * .5f, Quaternion.identity, new Vector3(.3f, (top + 4.5f) * .5f, .3f), BigTopLook.Gold);
            for (int i = 0; i < stripes; i++) b.Add(MeshKit.Cube, front + back * (deep * .55f) + along * (-length * .5f + (i + .5f) * length / stripes) + Vector3.up * (top + 4.6f), face * Quaternion.Euler(-12, 0, 0), new Vector3(length / stripes, .25f, deep * 1.05f), i % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite);
            b.Object(scen, "Grandstand " + name, BigTopLook.Vc(true), false, true);
            Crowd.Build(scen, name, seats, along, Mathf.Max(30f, length * .5f + 12f) * .9f, passSound, goSound, seed);
        }

        // ---------------- the route, visible the whole way ----------------
        static void RouteMarks()
        {
            var edge = new MeshKit.Batch(); var chev = new MeshKit.Batch(); var amber = new Color(1f, .62f, .12f);
            void Edges(float a, float b)
            {
                for (float p = a; p < b - 3; p += 3)
                {
                    if (InGap(p) || InGap(p + 3)) continue; var q = At(p + 1.5f); var r = Right(p + 1.5f); var f = Facing(p + 1.5f, false); float y = RoadTop(p + 1.5f) + .03f;
                    float half = p > M("wagons") - 30 && p < M("wagonsEnd") + 30 ? WagonRoadHalf : RoadHalf;
                    foreach (float s in new[] { -1f, 1f }) edge.Add(MeshKit.Cube, new Vector3(q.x, y, q.z) + r * s * (half - .8f), f, new Vector3(.6f, .05f, 3.02f), new Color(.97f, .95f, .88f));
                }
                for (float p = a + 25; p < b - 20; p += 50) { if (InGap(p) || InGap(p + 5) || Lift(p) > 0 || OnRailLine(p)) continue; var q = At(p); q.y = G + .05f; var fw = Flat(Dir(p)); var r = Right(p); foreach (float s in new[] { -1f, 1f }) chev.Add(MeshKit.Cube, q + r * s * 1.5f - fw * 1.1f, Facing(p) * Quaternion.Euler(0, -s * 38, 0), new Vector3(.9f, .05f, 4.6f), amber); }
            }
            Edges(0, M("globe")); Edges(M("globe") + RailList[0].Length, M("cannon")); Edges(M("G9") - 10, M("finish") - 10);
            edge.Object(scen, "Edge lines", BigTopLook.Vc(false, new Color(.15f, .15f, .14f)), false, false);
            chev.Object(scen, "Road chevrons", BigTopLook.Vc(false, new Color(.25f, .15f, .02f)), false, false);
            // the flight over the camp: rings along the line
            // Round 16 item 1: bigger and brighter (the cannonball flight's way over the camp reads from the top flap on), every 70 m
            var rings = new MeshKit.Batch(); var gold = OldClown ? new Color(1f, .9f, .55f, .55f) : new Color(1f, .86f, .4f, .8f);
            for (float p = M("flap") + 70; p < M("splash") - 60; p += OldClown ? 75 : 70)
            {
                var q = At(p); var frame = Quaternion.LookRotation(Dir(p), Vector3.up); float R = OldClown ? 11f : 14f;
                for (int i = 0; i < 14; i++) { float a = i * 360f / 14; rings.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0) * R), frame * Quaternion.Euler(0, 0, a), new Vector3(.7f, R * .47f, .7f), gold); }
            }
            rings.Object(scen, "Flight rings", GateMarker.Beacon, false, false);
            // the river: a buoy lane (looks only)
            var buoys = new MeshKit.Batch(); int n = 0;
            for (float p = M("splash") + 30; p < M("diveZone") - 15; p += 26, n++)
                foreach (float s in new[] { -1f, 1f }) { var q = At(p); q.y = Water + .45f; buoys.Add(MeshKit.LowCylinder, q + Right(p) * s * 16, Quaternion.identity, new Vector3(1.1f, 1.5f, 1.1f), n % 2 == 0 ? new Color(1f, .45f, .1f) : Color.white); }
            buoys.Object(scen, "Buoy lane", BigTopLook.Vc(true, new Color(.12f, .08f, .04f)), false, false);
            // under water: guide lights either side of the line, a glowing stripe on the lake bed
            var gl = new MeshKit.Batch(); n = 0;
            for (float p = M("diveZone") + 10; p < M("poolEdge"); p += 12, n++)
            {
                foreach (float s in new[] { -1f, 1f }) { if (Mathf.Abs(p - M("wagon")) < 20) continue; gl.Add(MeshKit.Cube, At(p) + Right(p) * s * 9.5f, Facing(p), Vector3.one * .9f, n % 2 == 0 ? BigTopLook.Teal : BigTopLook.Gold); }
                var fq = At(p); fq.y = Floor + .1f; if (n % 2 == 0) gl.Add(MeshKit.Cube, fq, Facing(p), new Vector3(1.2f, .1f, 5), BigTopLook.Gold);
            }
            gl.Object(scen, "Dive guide lights", GateMarker.Beacon, false, false);
        }
    }
}
