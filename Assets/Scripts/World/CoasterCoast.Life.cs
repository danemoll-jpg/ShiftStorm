using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 10 item 4: Coaster Coast's life and set-pieces (looks only, no colliders), Round 8b's rule: big, close, in front.
    //   the hubless Ferris wheel (66 m) whose rim arches over the boardwalk (the car line drives under it, the fly line through it); the
    //   helter-skelter on its island (52 m) the flight turns round; fireworks bursting beside the three flight rings; the mascot balloon
    //   (a giant inflatable gull, 30 m) bobbing over the flight; a whale and dolphins pacing the boat on the sea; the sunken carousel, a
    //   drowned Ferris wheel and a giant octopus by the dive line; gulls over the boardwalk; the midway's stalls, carousel, drop tower
    //   and string lights; the crowd (Gridiron Glory's) at the start, the coaster station and the splash pool's landing.
    // Stand-ins (Tripo prompts in Docs/MODELS-NEEDED.md): the balloon, the whale, the octopus are built from primitives.
    public static partial class CoasterCoast
    {
        static void SetPieces()
        {
            FerrisWheel(); HelterSkelter(); Fireworks(); if (!OldWheel) WheelFireworks(); Balloon(); Whale(); Dolphins(); Carnival(); Octopus();
        }

        static void FerrisWheel()
        {
            var w = WheelCentre; var g = new GameObject("Ferris wheel (hubless)"); g.transform.SetParent(scen, false); g.transform.position = w;
            var rimT = new GameObject("rim").transform; rimT.SetParent(g.transform, false);
            var rim = new MeshKit.Batch(); var bulbs = new MeshKit.Batch(); int n = 48;
            for (int i = 0; i < n; i++)
            {
                float a = (i + .5f) * 360f / n; var p = Quaternion.Euler(0, 0, a) * Vector3.right * WheelR;
                rim.Add(MeshKit.Cube, p, Quaternion.Euler(0, 0, a), new Vector3(2.2f, 2 * Mathf.PI * WheelR / n * 1.05f, 2.6f), i % 2 == 0 ? CoasterLook.Steel : CoasterLook.Cream);
                rim.Add(MeshKit.Cube, Quaternion.Euler(0, 0, a) * Vector3.right * (WheelR + 1.6f), Quaternion.Euler(0, 0, a), new Vector3(.6f, 2 * Mathf.PI * (WheelR + 1.6f) / n * 1.05f, 3.6f), CoasterLook.CoasterRed);
                bulbs.Add(MeshKit.Cube, Quaternion.Euler(0, 0, a) * Vector3.right * (WheelR - 1.3f), Quaternion.identity, Vector3.one * .7f, i % 3 == 0 ? CoasterLook.Neon : i % 3 == 1 ? CoasterLook.Lemon : CoasterLook.NeonB);
            }
            rim.Object(rimT, "wheel rim", CoasterLook.Vc(true), false, true); bulbs.Object(rimT, "wheel lights", GateMarker.Beacon, false, false);
            var gond = new List<Transform>(); var cols = new[] { CoasterLook.Candy, CoasterLook.Mint, CoasterLook.Lemon, CoasterLook.Sky };
            for (int i = 0; i < 24; i++)
            {
                var gt = new GameObject("gondola").transform; gt.SetParent(g.transform, false);
                var b = new MeshKit.Batch(); b.Add(MeshKit.Cube, Vector3.zero, Quaternion.identity, new Vector3(3.2f, 2.6f, 3.2f), cols[i % 4]); b.Add(MeshKit.Cube, Vector3.up * 1.6f, Quaternion.identity, new Vector3(3.6f, .4f, 3.6f), CoasterLook.Cream); b.Add(MeshKit.Cube, Vector3.up * 2.6f, Quaternion.identity, new Vector3(.25f, 1.8f, .25f), CoasterLook.Steel);
                b.Object(gt, "car", CoasterLook.Vc(true), false, false); gond.Add(gt);
            }
            var wt = g.AddComponent<WheelTurn>(); wt.rim = rimT; wt.gondolas = gond.ToArray(); wt.radius = WheelR; wt.degPerSec = 6f;
            // two A-frames hold the rim near its bottom (on rollers), west of the boardwalk
            var leg = CoasterLook.Mat(CoasterLook.Steel, true);
            foreach (float s in new[] { -1f, 1f })
                foreach (float zz in new[] { -3.5f, 3.5f })
                {
                    var foot = new Vector3(w.x + s * 16, DeckY, w.z + zz); var head = w + Quaternion.Euler(0, 0, -90 + s * 32) * Vector3.right * (WheelR - 1);
                    var d = head - foot; Box(scen, "wheel leg", (foot + head) * .5f, new Vector3(1.2f, d.magnitude, 1.2f), leg, Quaternion.FromToRotation(Vector3.up, d), false);
                }
            SetPiece.Make(g, "Ferris wheel", "Boardwalk", "ccwheel", Vector3.zero, new Vector3(WheelR * 2 + 6, WheelR * 2 + 6, 6)).range = 700;
        }

        static void HelterSkelter()
        {
            var at = L(700, 0, 700); var g = new GameObject("Helter-skelter"); g.transform.SetParent(scen, false); g.transform.position = at;
            Cyl(root, "Helter-skelter island", at + Vector3.up * ((SeaFloor + DeckY) * .5f), 26, DeckY - SeaFloor, CoasterLook.Mat(CoasterLook.PlankDark), true);
            var b = new MeshKit.Batch(); const float H = 46, R = 7;
            for (int i = 0; i < 10; i++) b.Add(MeshKit.LowCylinder, Vector3.up * (DeckY + H * (i + .5f) / 10), Quaternion.identity, new Vector3(R * 2, H / 20, R * 2), i % 2 == 0 ? CoasterLook.CoasterRed : CoasterLook.Cream);
            b.Add(MeshKit.LowCone, Vector3.up * (DeckY + H + 5), Quaternion.identity, new Vector3(R * 2.6f, 10, R * 2.6f), CoasterLook.Lemon);
            for (int i = 0; i < 60; i++)
            {
                float u = i / 60f, a = u * 4 * Mathf.PI, y = DeckY + H * (1 - u) + 1; var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (R + 2.6f) + Vector3.up * y;
                b.Add(MeshKit.Cube, p, Quaternion.Euler(0, -a * Mathf.Rad2Deg, -14), new Vector3(2.6f, .5f, 3.4f), i % 2 == 0 ? CoasterLook.Mint : CoasterLook.Lemon);
            }
            b.Add(MeshKit.Cube, Vector3.up * (DeckY + H + 13), Quaternion.identity, new Vector3(.25f, 6, .25f), CoasterLook.Steel);
            b.Add(MeshKit.Cube, Vector3.up * (DeckY + H + 15) + Vector3.right * 1.6f, Quaternion.identity, new Vector3(3, 1.8f, .15f), CoasterLook.Candy);
            b.Object(g.transform, "tower", CoasterLook.Vc(true), false, true);
            SetPiece.Make(g, "Helter-skelter", "Sky Midway", "cchelter", Vector3.up * (DeckY + H * .5f + 6), new Vector3(R * 2 + 6, H + 14, R * 2 + 6));
        }

        static void Fireworks()
        {
            var rings = new MeshKit.Batch(); var gold = new Color(1f, .85f, .35f, .8f);
            var cols = new[] { CoasterLook.Neon, CoasterLook.Lemon, CoasterLook.NeonB, new Color(.6f, 1f, .4f), new Color(1f, .5f, .2f) }; int k = 0;
            foreach (var n in new[] { "fw1", "fw2", "fw3" })
            {
                float p = M(n); var q = At(p); var frame = Quaternion.LookRotation(Dir(p), Vector3.up); const float R = 14f;
                for (int i = 0; i < 18; i++) { float a = i * 360f / 18; rings.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0) * R), frame * Quaternion.Euler(0, 0, a), new Vector3(1f, R * .38f, 1f), gold); }
                // three bursts round each ring: either side and above, staggered
                var g = new GameObject("Fireworks " + n); g.transform.SetParent(scen, false); g.transform.position = q;
                var fws = new List<Firework>();
                foreach (var (off, dl) in new[] { (new Vector3(-38, 14, 25), 0f), (new Vector3(40, 22, 40), .7f), (new Vector3(5, 42, 30), 1.4f) })
                {
                    var fg = new GameObject("burst"); fg.transform.SetParent(g.transform, false); fg.transform.position = q + frame * off;
                    var fw = fg.AddComponent<Firework>(); fw.colour = cols[k++ % cols.Length]; fw.radius = 20; fw.every = 2.1f; fw.delay = dl; fws.Add(fw);
                }
                var sp = SetPiece.Make(g, "Fireworks at " + n, "Sky Midway", "ccfireworks", new Vector3(0, 26, 30), new Vector3(120, 70, 50)); sp.showing = () => fws.Exists(f => f.Bursting);
            }
            rings.Object(scen, "Firework rings", GateMarker.Beacon, false, false);
        }

        // Round 14 item 5: after the Ferris wheel, three firework rings on the way down to the coaster station (bursts either side as you come)
        static void WheelFireworks()
        {
            var rings = new MeshKit.Batch(); var gold = new Color(1f, .85f, .35f, .8f);
            var cols = new[] { CoasterLook.Neon, CoasterLook.Lemon, CoasterLook.NeonB, new Color(.6f, 1f, .4f), new Color(1f, .5f, .2f) }; int k = 0;
            foreach (var n in new[] { "mw1", "mw2", "mw3" })
            {
                float p = M(n); var q = At(p); var frame = Quaternion.LookRotation(Dir(p), Vector3.up); const float R = 15f;
                for (int i = 0; i < 18; i++) { float a = i * 360f / 18; rings.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0) * R), frame * Quaternion.Euler(0, 0, a), new Vector3(1f, R * .38f, 1f), gold); }
                var g = new GameObject("Fireworks " + n); g.transform.SetParent(scen, false); g.transform.position = q;
                var fws = new List<Firework>();
                foreach (var (off, dl) in new[] { (new Vector3(-34, 16, 30), 0f), (new Vector3(36, 20, 45), .5f), (new Vector3(0, 40, 60), 1.1f) })
                {
                    var fg = new GameObject("burst"); fg.transform.SetParent(g.transform, false); fg.transform.position = q + frame * off;
                    var fw = fg.AddComponent<Firework>(); fw.colour = cols[k++ % cols.Length]; fw.radius = 18; fw.every = 1.8f; fw.delay = dl; fws.Add(fw);
                }
                var sp = SetPiece.Make(g, "Fireworks at " + n, "Boardwalk", "ccfireworks", new Vector3(0, 26, 30), new Vector3(110, 70, 50)); sp.showing = () => fws.Exists(f => f.Bursting);
            }
            rings.Object(scen, "Wheel flight firework rings", GateMarker.Beacon, false, false);
        }

        // the mascot balloon: a giant inflatable gull in a sailor's cap (stand-in; Tripo prompt in Docs/MODELS-NEEDED.md)
        static void Balloon()
        {
            var g = new GameObject("Mascot balloon (stand-in)"); g.transform.SetParent(scen, false); g.transform.position = L(545, 62, 1146); g.transform.rotation = Quaternion.Euler(0, 160, 0);
            var body = new GameObject("body").transform; body.SetParent(g.transform, false); var bob = body.gameObject.AddComponent<Bob>(); bob.height = 2.2f; bob.period = 5; bob.sway = 5;
            var b = new MeshKit.Batch(); var white = new Color(.97f, .97f, .95f); var yellow = new Color(1f, .78f, .2f);
            b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, new Vector3(16, 18, 15), white);
            b.Add(MeshKit.LowSphere, new Vector3(0, 13, 1), Quaternion.identity, new Vector3(11, 10, 10), white);
            b.Add(MeshKit.LowCone, new Vector3(0, 12, 8), Quaternion.Euler(90, 0, 0), new Vector3(3.4f, 6, 2.4f), yellow);
            foreach (float s in new[] { -1f, 1f })
            {
                b.Add(MeshKit.LowSphere, new Vector3(s * 3, 15, 4.6f), Quaternion.identity, new Vector3(2.6f, 3, 1.4f), Color.white);
                b.Add(MeshKit.LowSphere, new Vector3(s * 3.1f, 15, 5.3f), Quaternion.identity, new Vector3(1.2f, 1.6f, .8f), new Color(.08f, .08f, .1f));
                b.Add(MeshKit.LowSphere, new Vector3(s * 10, 2, 0), Quaternion.Euler(0, 0, s * -35), new Vector3(14, 3.6f, 7), new Color(.85f, .88f, .9f));   // wings
                b.Add(MeshKit.LowSphere, new Vector3(s * 3.5f, -10, 1), Quaternion.identity, new Vector3(3, 2, 5), yellow);   // feet
            }
            b.Add(MeshKit.LowCylinder, new Vector3(0, 18.2f, 1), Quaternion.identity, new Vector3(9, 1.4f, 9), new Color(.1f, .2f, .6f));   // the sailor's cap
            b.Add(MeshKit.LowCylinder, new Vector3(0, 19.4f, 1), Quaternion.identity, new Vector3(7.6f, 1.2f, 7.6f), white);
            b.Object(body, "balloon", CoasterLook.Vc(true), false, true);
            var lines = new MeshKit.Batch(); foreach (float s in new[] { -1f, 1f }) lines.Add(MeshKit.Cube, new Vector3(s * 3.5f, -40, 1), Quaternion.Euler(0, 0, s * 3), new Vector3(.2f, 60, .2f), new Color(.3f, .3f, .3f));
            lines.Object(g.transform, "tethers", CoasterLook.Vc(false), false, false);
            SetPiece.Make(body.gameObject, "Mascot balloon", "Sky Midway", "ccballoon", new Vector3(0, 4, 0), new Vector3(36, 34, 16));
        }

        // a whale (stand-in) surfacing beside the boat on the sea, and dolphins leaping alongside it
        static void Whale()
        {
            var g = new GameObject("Whale (stand-in)"); g.transform.SetParent(scen, false); g.transform.position = At(M("sea1")) + Vector3.right * -40 + Vector3.down * 1;
            var b = new MeshKit.Batch(); var top = new Color(.22f, .3f, .45f); var belly = new Color(.82f, .85f, .88f);
            b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, new Vector3(9, 6.5f, 26), top);
            b.Add(MeshKit.LowSphere, new Vector3(0, -1.6f, 2), Quaternion.identity, new Vector3(8, 3.6f, 20), belly);
            b.Add(MeshKit.LowSphere, new Vector3(0, .4f, -15), Quaternion.identity, new Vector3(4, 3, 10), top);
            b.Add(MeshKit.Cube, new Vector3(0, .6f, -21), Quaternion.identity, new Vector3(12, .8f, 4), top);   // flukes
            foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.Cube, new Vector3(s * 5, -1.8f, 4), Quaternion.Euler(0, s * 25, s * 20), new Vector3(6, .6f, 2.4f), top);
            foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.LowSphere, new Vector3(s * 3.8f, .6f, 9), Quaternion.identity, Vector3.one * .9f, Color.black);
            b.Object(g.transform, "whale", CoasterLook.Vc(true), false, true);
            var pc = g.AddComponent<Pacer>(); pc.course = c; pc.from = M("seaOut") + 40; pc.to = M("diveZone") - 30; pc.side = -34; pc.lead = 12; pc.y = -1.2f; pc.speed = 75;
            SetPiece.Make(g, "Whale", "Lighthouse Sea", "ccwhale", new Vector3(0, 1.5f, -3), new Vector3(12, 4, 40));
        }
        static void Dolphins()
        {
            for (int i = 0; i < 3; i++)
            {
                var g = new GameObject("Dolphin (stand-in)"); g.transform.SetParent(scen, false); g.transform.position = At(M("seaOut") + 60) + Vector3.right * 20;
                var b = new MeshKit.Batch(); var grey = new Color(.5f, .58f, .66f);
                b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, new Vector3(1.6f, 1.5f, 5.5f), grey); b.Add(MeshKit.LowCone, new Vector3(0, .9f, -.3f), Quaternion.Euler(-20, 0, 0), new Vector3(.4f, 1.3f, 1f), grey);
                b.Add(MeshKit.Cube, new Vector3(0, 0, -3.2f), Quaternion.identity, new Vector3(2.4f, .2f, .9f), grey); b.Add(MeshKit.LowSphere, new Vector3(0, -.1f, 3f), Quaternion.identity, new Vector3(.6f, .5f, 1.4f), grey);
                b.Object(g.transform, "dolphin", CoasterLook.Vc(true), false, false);
                var pc = g.AddComponent<Pacer>(); pc.course = c; pc.from = M("seaOut") + 10; pc.to = M("sea1") + 60; pc.side = 16 + i * 4; pc.lead = 10 + i * 7; pc.y = -.6f; pc.hop = 3.5f; pc.speed = 80;
                if (i == 1) SetPiece.Make(g, "Dolphins", "Lighthouse Sea", "ccwhale", new Vector3(0, 1, 0), new Vector3(10, 6, 14));
            }
        }

        // the sunken carnival on the sea floor under the south pier: a drowned carousel (turning slowly), a fallen Ferris wheel, booths
        static void Carnival()
        {
            var at = At(M("carousel")) + Right(M("carousel")) * 30; at.y = SeaFloor;
            var g = new GameObject("Sunken carousel"); g.transform.SetParent(scen, false); g.transform.position = at;
            var turn = new GameObject("turn").transform; turn.SetParent(g.transform, false); turn.gameObject.AddComponent<Spin>().degPerSec = 8;
            var b = new MeshKit.Batch(); var gold = new Color(.85f, .7f, .35f); var moss = new Color(.35f, .55f, .45f);
            b.Add(MeshKit.LowCylinder, Vector3.up * .8f, Quaternion.identity, new Vector3(30, .8f, 30), new Color(.55f, .4f, .45f));
            b.Add(MeshKit.LowCylinder, Vector3.up * 9, Quaternion.identity, new Vector3(4, 8, 4), gold);
            b.Add(MeshKit.LowCone, Vector3.up * 19, Quaternion.identity, new Vector3(34, 5, 34), CoasterLook.CoasterRed * .7f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30 * Mathf.Deg2Rad; var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 11;
                b.Add(MeshKit.LowCylinder, p + Vector3.up * 8.5f, Quaternion.identity, new Vector3(.35f, 8, .35f), gold);
                b.Add(MeshKit.Cube, p + Vector3.up * (5 + (i % 3)), Quaternion.Euler(0, -i * 30, 0), new Vector3(1.3f, 2.2f, 3.8f), i % 2 == 0 ? CoasterLook.Cream : moss);
            }
            b.Object(turn, "carousel", CoasterLook.Vc(true), false, true);
            SetPiece.Make(g, "Sunken carousel", "Sunken Carnival", "cccarousel", Vector3.up * 11, new Vector3(34, 22, 34));
            // the fallen Ferris wheel, booths with weed
            var w = new MeshKit.Batch(); var wc = At(M("portal") + 70) - Right(M("portal") + 70) * 34; wc.y = SeaFloor + 6;
            for (int i = 0; i < 28; i++) { float a = i * 360f / 28; w.Add(MeshKit.Cube, wc + Quaternion.Euler(70, 20, a) * Vector3.right * 18, Quaternion.Euler(70, 20, a), new Vector3(1.4f, 4.2f, 1.6f), new Color(.6f, .55f, .5f)); }
            for (int i = 0; i < 10; i++) { var q = At(M("portal") + 30 + i * 16); var p = new Vector3(q.x, SeaFloor + 2, q.z) + Right(M("portal") + 30 + i * 16) * (i % 2 == 0 ? 26 : -28); w.Add(MeshKit.Cube, p, Quaternion.Euler(0, i * 23, i * 4), new Vector3(6, 4, 4), i % 3 == 0 ? CoasterLook.Candy * .7f : CoasterLook.Mint * .7f); w.Add(MeshKit.LowCone, p + Vector3.up * 4, Quaternion.identity, new Vector3(1.2f, 6, 1.2f), moss); }
            w.Object(scen, "Sunken rides", CoasterLook.Vc(true), false, true);
        }
        // a giant octopus beside the dive line (stand-in): a mantle and eight waving arms
        static void Octopus()
        {
            float p = M("octo"); var at = At(p) - Right(p) * 24; at.y = SeaFloor + 4;
            var g = new GameObject("Octopus (stand-in)"); g.transform.SetParent(scen, false); g.transform.position = at;
            var red = new Color(.85f, .35f, .3f); var b = new MeshKit.Batch();
            b.Add(MeshKit.LowSphere, Vector3.up * 9, Quaternion.Euler(-15, 0, 0), new Vector3(11, 13, 11), red);
            foreach (float s in new[] { -1f, 1f }) { b.Add(MeshKit.LowSphere, new Vector3(s * 3.4f, 7, 4.4f), Quaternion.identity, new Vector3(2.8f, 2.8f, 1.6f), Color.white); b.Add(MeshKit.LowSphere, new Vector3(s * 3.4f, 7, 5.1f), Quaternion.identity, new Vector3(1.3f, 1.6f, .7f), Color.black); }
            b.Object(g.transform, "mantle", CoasterLook.Vc(true), false, true);
            var roots = new List<Transform>(); var mat = CoasterLook.Mat(red, true);
            for (int i = 0; i < 8; i++)
            {
                var parent = new GameObject("arm").transform; parent.SetParent(g.transform, false); parent.localPosition = Vector3.up * 3.2f; parent.localRotation = Quaternion.Euler(0, i * 45, 0) * Quaternion.Euler(70, 0, 0);
                roots.Add(parent); var prev = parent;
                for (int k = 0; k < 6; k++)
                {
                    var seg = new GameObject("seg").transform; seg.SetParent(prev, false); seg.localPosition = k == 0 ? Vector3.zero : Vector3.up * 2.6f;
                    var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule); Object.Destroy(vis.GetComponent<Collider>()); vis.transform.SetParent(seg, false); vis.transform.localPosition = Vector3.up * 1.3f; vis.transform.localScale = new Vector3(1.6f - k * .2f, 1.6f, 1.6f - k * .2f); vis.GetComponent<Renderer>().sharedMaterial = mat;
                    prev = seg;
                }
            }
            g.AddComponent<Tentacles>().roots = roots.ToArray();
            SetPiece.Make(g, "Octopus", "Sunken Carnival", "ccoctopus", Vector3.up * 8, new Vector3(30, 18, 30));
        }

        // ---------------- the midway: stalls, a carousel, a drop tower, string lights, gulls ----------------
        static void Scenery()
        {
            var rnd = new System.Random(77); float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var stalls = new MeshKit.Batch(); var lights = new MeshKit.Batch();
            var cols = new[] { CoasterLook.Candy, CoasterLook.Mint, CoasterLook.Lemon, CoasterLook.Sky, new Color(.95f, .55f, .3f) };
            void Stall(Vector3 p, Quaternion f, int k)
            {
                var col = cols[k % cols.Length];
                stalls.Add(MeshKit.Cube, p + Vector3.up * 1.8f, f, new Vector3(6, 3.6f, 4.5f), CoasterLook.Cream);
                stalls.Add(MeshKit.Cube, p + Vector3.up * 1.2f + f * Vector3.forward * 2.3f, f, new Vector3(6, 2.4f, .2f), col);
                for (int i = 0; i < 4; i++) stalls.Add(MeshKit.Cube, p + Vector3.up * 4f + f * new Vector3(-2.25f + i * 1.5f, 0, .8f), f * Quaternion.Euler(-18, 0, 0), new Vector3(1.5f, .2f, 6.4f), i % 2 == 0 ? col : Color.white);
                stalls.Add(MeshKit.Cube, p + Vector3.up * 5.6f + f * Vector3.forward * .5f, f, new Vector3(4.4f, 1.1f, .3f), col * .9f);
                lights.Add(MeshKit.Cube, p + Vector3.up * 5.6f + f * Vector3.forward * .7f, f, new Vector3(3.6f, .3f, .1f), k % 2 == 0 ? CoasterLook.Neon : CoasterLook.NeonB);
            }
            // both sides of the boardwalk, facing it (the choice jump's area and the coaster's foot left open)
            int n = 0;
            for (float p = 30; p < M("railA") - 40; p += 15)
            {
                if (p > M("jM") - 60 && p < M("jM") + 75) continue; var q = At(p); var r = Right(p);
                foreach (float s in new[] { -1f, 1f })
                {
                    if (s < 0 && p > M("wheel") - 60 && p < M("wheel") + 40) continue;   // the Ferris wheel's feet
                    var at = new Vector3(q.x, DeckY, q.z) + r * s * R(26, 34); Stall(at, Quaternion.LookRotation(-r * s), n++);
                }
            }
            // the east midway along the flume's banks
            for (float p = M("splash") + 40; p < M("cross") - 140; p += 22) { var q = At(p); var r = Right(p); foreach (float s in new[] { -1f, 1f }) Stall(new Vector3(q.x, DeckY, q.z) + r * s * (FlumeHalf(p) + R(12, 30)), Quaternion.LookRotation(-r * s), n++); }
            stalls.Object(scen, "Midway stalls", CoasterLook.Vc(true), false, true).layer = ParadisePlunge.DetailLayer;
            // string lights along the boardwalk: poles every 20 m both sides, bulbs swung between them
            var poles = new MeshKit.Batch();
            void Strings(float a, float b)
            {
                for (float p = a; p < b; p += 20)
                {
                    if (InGap(p) || InGap(p + 20)) continue; var q = At(p); var r = Right(p); var q2 = At(p + 20); var r2 = Right(p + 20);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var top = new Vector3(q.x, DeckY + 7, q.z) + r * s * (RoadHalf + 2.5f); var top2 = new Vector3(q2.x, DeckY + 7, q2.z) + r2 * s * (RoadHalf + 2.5f);
                        poles.Add(MeshKit.Cube, top + Vector3.down * 3.5f, Quaternion.identity, new Vector3(.35f, 7, .35f), CoasterLook.Pier);
                        for (int i = 1; i < 8; i++) { float u = i / 8f; lights.Add(MeshKit.Cube, Vector3.Lerp(top, top2, u) + Vector3.down * Mathf.Sin(u * Mathf.PI) * 1.4f, Quaternion.identity, Vector3.one * .45f, i % 3 == 0 ? CoasterLook.Neon : i % 3 == 1 ? CoasterLook.Lemon : CoasterLook.NeonB); }
                    }
                }
            }
            Strings(20, M("railA") - 30); Strings(M("G9"), M("finish") - 20);
            poles.Object(scen, "Light poles", CoasterLook.Vc(true), false, false);
            lights.Object(scen, "String lights", GateMarker.Beacon, false, false);
            // a plaza carousel (turning) and a drop tower on the north plaza, bumper cars' pavilion on the south one
            {
                var g = new GameObject("Plaza carousel"); g.transform.SetParent(scen, false); g.transform.position = L(-75, DeckY, 120);
                var t = new GameObject("turn").transform; t.SetParent(g.transform, false); t.gameObject.AddComponent<Spin>().degPerSec = 20;
                var b = new MeshKit.Batch(); b.Add(MeshKit.LowCylinder, Vector3.up * .6f, Quaternion.identity, new Vector3(22, .6f, 22), CoasterLook.Cream); b.Add(MeshKit.LowCone, Vector3.up * 11, Quaternion.identity, new Vector3(25, 4, 25), CoasterLook.Candy);
                for (int i = 0; i < 10; i++) { float a = i * 36 * Mathf.Deg2Rad; var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 8.5f; b.Add(MeshKit.LowCylinder, p + Vector3.up * 5, Quaternion.identity, new Vector3(.3f, 5, .3f), CoasterLook.CoasterGold); b.Add(MeshKit.Cube, p + Vector3.up * 3, Quaternion.Euler(0, -i * 36, 0), new Vector3(1.1f, 1.8f, 3f), i % 2 == 0 ? Color.white : CoasterLook.Lemon); }
                b.Object(t, "carousel", CoasterLook.Vc(true), false, true);
            }
            {
                var at = L(-70, DeckY, 500); var b = new MeshKit.Batch();
                b.Add(MeshKit.Cube, at + Vector3.up * 28, Quaternion.identity, new Vector3(4, 56, 4), CoasterLook.Steel); b.Add(MeshKit.LowCylinder, at + Vector3.up * 57, Quaternion.identity, new Vector3(7, 1.5f, 7), CoasterLook.CoasterRed);
                b.Add(MeshKit.LowCylinder, at + Vector3.up * 20, Quaternion.identity, new Vector3(11, 1.2f, 11), CoasterLook.Lemon);
                b.Object(scen, "Drop tower", CoasterLook.Vc(true), false, true);
            }
            {
                var at = L(-80, DeckY, -150); var b = new MeshKit.Batch();
                b.Add(MeshKit.Cube, at + Vector3.up * .15f, Quaternion.identity, new Vector3(36, .3f, 26), new Color(.3f, .3f, .35f));
                for (int i = 0; i < 4; i++) for (int k = 0; k < 2; k++) b.Add(MeshKit.Cube, at + new Vector3(-17 + i * 11.3f, 4, -12 + k * 24), Quaternion.identity, new Vector3(.5f, 8, .5f), CoasterLook.Steel);
                for (int i = 0; i < 6; i++) b.Add(MeshKit.Cube, at + new Vector3(0, 8.4f, -12 + i * 4.8f), Quaternion.identity, new Vector3(37, .4f, 4.8f), i % 2 == 0 ? CoasterLook.Sky : Color.white);
                for (int i = 0; i < 9; i++) b.Add(MeshKit.Cube, at + new Vector3(-13 + (i * 7) % 26, .8f, -9 + (i * 5) % 18), Quaternion.Euler(0, i * 40, 0), new Vector3(2, 1.2f, 2.8f), cols[i % cols.Length]);
                b.Object(scen, "Bumper cars", CoasterLook.Vc(true), false, true);
            }
            // gulls over the boardwalk
            for (int i = 0; i < 8; i++)
            {
                var g = new GameObject("Gull"); g.transform.SetParent(scen, false); var b = new MeshKit.Batch();
                b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, new Vector3(.6f, .5f, 1.4f), Color.white); b.Add(MeshKit.Cube, Vector3.zero, Quaternion.identity, new Vector3(3.2f, .08f, .6f), new Color(.85f, .87f, .9f));
                b.Object(g.transform, "gull", CoasterLook.Vc(false), false, false);
                var cir = g.AddComponent<Circle>(); cir.center = L(-20 + (i % 4) * 30, 22 + i * 2.5f, -100 + i * 90); cir.radius = 25 + i * 4; cir.speed = .35f + i * .03f; cir.phase = i * 1.3f;
            }
        }

        // ---------------- the crowd: Gridiron Glory's fans in stands at the start, the coaster station and the splash pool's landing ----------------
        static void Grandstands()
        {
            foreach (float s in new[] { -1f, 1f }) Stand(s < 0 ? "CC start W" : "CC start E", L(s * 24, DeckY, -20), Vector3.forward, Vector3.right * s, 60, 6, false, true, s < 0 ? 31 : 32);
            Stand("CC coaster station", L(30, DeckY, 810), Vector3.forward, Vector3.right, 50, 5, true, false, 33);
            Stand("CC splash landing", L(-26, DeckY, -300), Vector3.forward, -Vector3.right, 48, 5, true, false, 34);
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
                b.Add(MeshKit.Cube, front + back * d + Vector3.up * (h * .5f), face, new Vector3(length, h, depth), row % 2 == 0 ? CoasterLook.Cream : CoasterLook.Sky * .9f);
                for (float x = -length * .5f + .8f; x <= length * .5f - .8f; x += spacing) seats.Add(front + back * (d + .15f) + along * (x + (float)(rnd.NextDouble() - .5) * .3f) + Vector3.up * h);
            }
            float top = rows * rise, deep = rows * depth; int stripes = Mathf.RoundToInt(length / 4f);
            b.Add(MeshKit.Cube, front + back * (deep + .25f) + Vector3.up * (top + 2.2f) * .5f, face, new Vector3(length, top + 2.2f, .5f), CoasterLook.Cream);
            for (float x = -length * .5f; x <= length * .5f + .01f; x += length / 6f) b.Add(MeshKit.LowCylinder, front + back * (deep * .45f) + along * x + Vector3.up * (top + 4.5f) * .5f, Quaternion.identity, new Vector3(.3f, (top + 4.5f) * .5f, .3f), CoasterLook.Steel);
            for (int i = 0; i < stripes; i++) b.Add(MeshKit.Cube, front + back * (deep * .55f) + along * (-length * .5f + (i + .5f) * length / stripes) + Vector3.up * (top + 4.6f), face * Quaternion.Euler(-12, 0, 0), new Vector3(length / stripes, .25f, deep * 1.05f), i % 2 == 0 ? CoasterLook.CoasterRed : CoasterLook.Cream);
            b.Object(scen, "Grandstand " + name, CoasterLook.Vc(true), false, true);
            Crowd.Build(scen, name, seats, along, Mathf.Max(30f, length * .5f + 12f) * .9f, passSound, goSound, seed);
        }

        // ---------------- the route, visible the whole way ----------------
        static void RouteMarks()
        {
            var edge = new MeshKit.Batch(); var chev = new MeshKit.Batch(); var amber = new Color(1f, .62f, .12f);
            void Edges(float a, float b)
            {
                for (float p = a; p < b - 3; p += 3) { if (InGap(p) || InGap(p + 3)) continue; var q = At(p + 1.5f); var r = Right(p + 1.5f); var f = Facing(p + 1.5f, false); float y = RoadTop(p + 1.5f) + .03f; foreach (float s in new[] { -1f, 1f }) edge.Add(MeshKit.Cube, new Vector3(q.x, y, q.z) + r * s * (RoadHalf - .8f), f, new Vector3(.6f, .05f, 3.02f), new Color(.97f, .95f, .88f)); }
                for (float p = a + 25; p < b - 20; p += 50) { if (InGap(p) || InGap(p + 5) || Lift(p) > 0) continue; var q = At(p); q.y = DeckY + .05f; var fw = Flat(Dir(p)); var r = Right(p); foreach (float s in new[] { -1f, 1f }) chev.Add(MeshKit.Cube, q + r * s * 1.5f - fw * 1.1f, Facing(p) * Quaternion.Euler(0, -s * 38, 0), new Vector3(.9f, .05f, 4.6f), amber); }
            }
            Edges(0, M("railA")); Edges(M("landA") - 20, M("railB")); Edges(M("G9") - 10, M("finish") - 10);
            edge.Object(scen, "Edge lines", CoasterLook.Vc(false, new Color(.15f, .15f, .14f)), false, false);
            chev.Object(scen, "Road chevrons", CoasterLook.Vc(false, new Color(.25f, .15f, .02f)), false, false);
            // the flight: rings along the line (not at the fireworks' rings)
            var rings = new MeshKit.Batch(); var gold = new Color(1f, .9f, .55f, .55f);
            for (float p = M("crest") + 80; p < M("splash") - 60; p += 75)
            {
                if (Mathf.Abs(p - M("fw1")) < 40 || Mathf.Abs(p - M("fw2")) < 40 || Mathf.Abs(p - M("fw3")) < 40) continue;
                var q = At(p); var frame = Quaternion.LookRotation(Dir(p), Vector3.up); const float R = 11f;
                for (int i = 0; i < 14; i++) { float a = i * 360f / 14; rings.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0) * R), frame * Quaternion.Euler(0, 0, a), new Vector3(.7f, R * .47f, .7f), gold); }
            }
            rings.Object(scen, "Flight rings", GateMarker.Beacon, false, false);
            // the water: a buoy lane through the flume and along the sea (bobbing, looks only)
            var buoys = new MeshKit.Batch(); int n = 0;
            for (float p = M("splash") + 30; p < M("diveZone") - 15; p += 26, n++)
            {
                float half = p < M("seaOut") ? Mathf.Min(14f, FlumeHalf(p) - 3) : 16f;
                foreach (float s in new[] { -1f, 1f }) { var q = At(p); q.y = Water + .45f; buoys.Add(MeshKit.LowCylinder, q + Right(p) * s * half, Quaternion.identity, new Vector3(1.1f, 1.5f, 1.1f), n % 2 == 0 ? new Color(1f, .45f, .1f) : Color.white); }
            }
            buoys.Object(scen, "Buoy lane", CoasterLook.Vc(true, new Color(.12f, .08f, .04f)), false, false);
            // under water: guide lights either side of the line
            var gl = new MeshKit.Batch(); n = 0;
            for (float p = M("diveZone") + 10; p < M("poolEdge"); p += 12, n++) foreach (float s in new[] { -1f, 1f }) gl.Add(MeshKit.Cube, At(p) + Right(p) * s * 9.5f, Facing(p), Vector3.one * .9f, n % 2 == 0 ? CoasterLook.NeonB : CoasterLook.Lemon);
            gl.Object(scen, "Dive guide lights", GateMarker.Beacon, false, false);
        }
    }
}
