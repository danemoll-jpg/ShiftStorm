using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 17 item 4 (Dan: "when we have the options they seem to be over in a couple of seconds so kind of pointless. I would want them to be
    // a complete different experience"): Paradise Plunge's two big route splits, each a large part of the lap, the other way a different form,
    // different scenery and different hazards, about equal in time:
    //   Valley Flight / Cliffside Run (from the Lake Jump, LB on its ramp): the plane leaves the promenade and flies a long, low loop through the
    //     dinosaur valley (the long-neck herd at the waterhole, the triceratops on the east flats, the T. rex by the east cliffs, pterodactyls
    //     over the north end), six gold rings, and climbs to meet the Sky Arch flight 120 m past the climb's ramp; it stands in for G1 and G2.
    //     The car keeps the jungle canyon, the climb (the Cliff Gap is a plain jump again: big airtime for tricks) and the ramp. Both then
    //     fly through the Sky Arch.
    //   Sunken Caves / Mangrove River (after the lagoon's splashdown): the violet dive lane on the lagoon's east side turns the boat into a
    //     submarine; down through a sinkhole in the lagoon's bed into a long winding cave under the lagoon, the river's mouth and the whole
    //     S-bend (hanging roots, glowing fish, a sunken log arch, a gentle current), up through the river bed just before the Sunken
    //     Gateway; it stands in for G5. The boat keeps the lagoon and the mangrove river (its bends and currents).
    // Folded in: the Lake flight (the Valley Flight replaces it), the Cliff flight (gone), the Mangrove bypass (now the Sunken Caves, from the
    // lagoon), the Coral Vault tunnel (the vault is compact now). Round 12 / 14 rules: room at every exit, forward recovery on the flight,
    // its own anchors in the caves, LB on the ramp. "-oldPP": build 19's.
    public static partial class ParadisePlunge
    {
        public const float ValleyRejoinAfter = 260f, CavesCurrent = 4f, CavesDepth = -27f;
        public static float ValleyRejoin => M("lip") + ValleyRejoinAfter;
        public static float CavesFrom => M("splash") + 110;
        public static float CavesTo => M("diveZone") + 30;
        public static readonly List<Vector3> ValleyHerd = new List<Vector3>();

        // ---------------- the Valley Flight ----------------
        static void ValleyFlight()
        {
            float lip = M("jC"), rejoin = ValleyRejoin; var up = Vector3.up;
            var ctrl = new List<Vector3>
            {
                At(lip) + up * 1.5f, new Vector3(18, 9, 667), new Vector3(42, 17, 722), new Vector3(100, 21, 778), new Vector3(185, 22, 812), new Vector3(300, 20, 832),
                new Vector3(420, 22, 805), new Vector3(540, 24, 750), new Vector3(640, 26, 650), new Vector3(740, 28, 640), new Vector3(835, 31, 680),
                new Vector3(868, 34, 770), new Vector3(835, 38, 860), new Vector3(740, 42, 900), new Vector3(650, 46, 880), new Vector3(575, 50, 820),
                At(rejoin), At(rejoin + 30),
            };
            var sc = MakeShortcut("Valley Flight", ctrl, lip, rejoin + 30, 1, 1, 150f, new Vector2(30, 20), new Vector2(34, 24), 12f, 40f, Mode.Fly);
            sc.replacesCount = 2; sc.choice = true; sc.flight = true;
            // six gold rings along it (the HUD arrow points at the next one: Shortcut.rings)
            var rings = new MeshKit.Batch(); var gold = new Color(1f, .82f, .3f, .7f); const float RR = 17f;
            foreach (float frac in new[] { .12f, .27f, .42f, .57f, .72f, .86f })
            {
                float s = sc.Length * frac; sc.rings.Add(s); var q = sc.PointAt(s, out var d); var frame = Quaternion.LookRotation(d, up);
                for (int i = 0; i < 18; i++) { float an = i * 360f / 18; rings.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(an * Mathf.Deg2Rad), Mathf.Sin(an * Mathf.Deg2Rad), 0) * RR), frame * Quaternion.Euler(0, 0, an), new Vector3(1f, RR * .38f, 1f), gold); }
            }
            rings.Object(scen, "Valley Flight rings", GateMarker.Beacon, false, false);
            // the herds it flies past (the valley floor is y 0.4): beside the line, 60-110 m off it, never under it
            ValleyHerd.Clear();
            void Herd(string kind, float frac, float side, float height, int n, float spread)
            {
                float s = sc.Length * frac; var q = sc.PointAt(s, out var d); var r = Vector3.Cross(Vector3.up, Flat(d)); var rnd = new System.Random((int)(frac * 1000));
                for (int i = 0; i < n; i++)
                {
                    var at = new Vector3(q.x, .4f, q.z) + r * side + Flat(d) * ((float)rnd.NextDouble() - .5f) * spread + r * Mathf.Sign(side) * (float)rnd.NextDouble() * 25f;
                    float yaw = Mathf.Atan2(-r.x * Mathf.Sign(side), -r.z * Mathf.Sign(side)) * Mathf.Rad2Deg + ((float)rnd.NextDouble() - .5f) * 60f;
                    Dino(kind, at, yaw, height * (.85f + .3f * (float)rnd.NextDouble())); ValleyHerd.Add(at);
                }
            }
            Herd("bronto", .2f, 75f, 24f, 3, 90f);   // the long-neck herd at the north waterhole
            Herd("trike", .45f, -60f, 5.8f, 4, 70f);   // triceratops on the east flats
            Herd("trex", .58f, 80f, 15f, 1, 0f);       // the T. rex by the east cliffs
            Herd("bronto", .66f, -95f, 26f, 2, 60f);
            Herd("trike", .3f, -65f, 5.5f, 3, 60f);
            // the waterhole under the herd (looks only)
            { var q = sc.PointAt(sc.Length * .2f, out var d); var r = Vector3.Cross(Vector3.up, Flat(d)); var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder); w.name = "Valley waterhole"; w.transform.SetParent(scen, false); Object.Destroy(w.GetComponent<Collider>()); w.transform.position = new Vector3(q.x, .45f, q.z) + r * 70f; w.transform.localScale = new Vector3(60, .05f, 40); w.GetComponent<MeshRenderer>().sharedMaterial = JurassicLook.WaterMat(JurassicLook.Water, JurassicLook.WaterLight, JurassicLook.WaterRim, .05f); }
            // pterodactyls circling over the north end
            for (int i = 0; i < 3; i++) { var q = sc.PointAt(sc.Length * (.8f + i * .04f), out _); var pt = Dino("ptero", q + Vector3.up * 30f, 0, 9f); var cc = pt.AddComponent<Circle>(); cc.center = q + Vector3.up * (34 + i * 6); cc.radius = 70 + i * 20; cc.speed = .25f; cc.phase = i * 2f; }
            // a winged sign at the Lake Jump: VALLEY FLIGHT one way, CLIFFSIDE RUN the other (the jump's own sign stands 70 m before the lip)
            Debug.Log($"[Course] Valley Flight: line {sc.Length:0} m (main {rejoin + 30 - lip:0} m: canyon, climb, ramp, flight), {sc.rings.Count} rings, {ValleyHerd.Count} dinosaurs beside it");
        }

        // ---------------- the Sunken Caves ----------------
        static List<Vector3> CavesCtrl()
        {
            Vector3 P(float p, float left, float y) { var q = At(p) - Right(p) * left; q.y = y; return q; }
            float f0 = CavesFrom, le = M("lagoonEnd"), g5 = M("G5"), dz = M("diveZone");
            // the dive lane on the left of the boat line (south of it), down through the lagoon's sinkhole, then a winding drowned riverbed
            // under the mangrove river (its banks and bends above) at -27 m, up into the pool before the Sunken Gateway
            var list = new List<Vector3> { P(f0, 0, .6f), P(f0 + 25, 10, .6f), P(f0 + 50, 22, .6f), P(f0 + 75, 26, .6f), P(f0 + 98, 26, -4f), P(f0 + 120, 24, -13f), P(f0 + 142, 22, -22f), P(f0 + 165, 20, CavesDepth) };
            float a = Mathf.Max(f0 + 210, le + 20), b = g5 - 40; int n = Mathf.Max(2, Mathf.CeilToInt((b - a) / 45f));
            for (int i = 0; i <= n; i++) { float p = Mathf.Lerp(a, b, i / (float)n); list.Add(P(p, 9f * Mathf.Sin(i * 1.4f + .6f), CavesDepth - (i % 2) * 1.5f)); }   // winding under the river
            list.AddRange(new[] { P(g5, 2, -25f), P(g5 + 30, 1, -18f), P(dz - 15, 0, -10f), P(dz, 0, -5f), At(dz + 15), At(dz + 30) });
            return list;
        }
        static void SunkenCaves()
        {
            var ctrl = CavesCtrl(); bypassLine = Shortcut.Smooth(ctrl);
            var sc = MakeShortcut("Sunken Caves", ctrl, CavesFrom, CavesTo, 5, 5, 13f, new Vector2(11, 8), new Vector2(11, 8), 1f, 1f, Mode.Dive);
            float L = sc.Length;
            float sIn = -1, sOut = -1; for (float s = 0; s < L; s += 1) { var q = sc.PointAt(s, out _); if (sIn < 0 && q.y < -5f) sIn = s; if (q.y < -5f) sOut = s; }
            Object.Destroy(sc.entry.gameObject); Object.Destroy(sc.exit.gameObject);
            sc.entry = SubGate(sc, "Sunken Caves entry", sIn, new Vector2(11, 8), Mode.Dive); sc.exit = SubGate(sc, "Sunken Caves exit", sOut, new Vector2(13, 9), Mode.Dive);
            // the dive lane's zone just before the entry (the lane is 22-28 m left of the boat line: a boat on its line never meets it); no
            // surfacing: the caves come up in the pool before the Sunken Gateway, already a submarine
            var dz = sc.PointAt(Mathf.Max(0, sIn - 24), out var dd); dz.y = -1;
            var zin = Vol<TransformZone>("Dive (sunken caves)", dz, Quaternion.LookRotation(Flat(dd)), new Vector3(22, 10, 18)); zin.target = Mode.Dive; zin.label = "Dive (sunken caves)";
            Tunnel(sc, -16f, -15f, false, "Sunken caves");
            for (float s = sIn + 40; s < sOut - 40; s += 20) { var p0 = sc.PointAt(s, out var d0); var cz = Vol<CurrentZone>("Sunken caves current", p0, Quaternion.LookRotation(d0), new Vector3(18, 11, 22)); cz.push = d0 * CavesCurrent; }
            foreach (float f in new[] { .2f, .4f, .6f, .8f })
            {
                float s = Mathf.Lerp(sIn, sOut, f); var q = sc.PointAt(s, out var d); var g = new GameObject($"Anchor Dive (caves) {s:0}"); g.transform.SetParent(sc.transform, false);
                g.transform.SetPositionAndRotation(q, Quaternion.LookRotation(d, Vector3.up)); var a = g.AddComponent<RecoveryAnchor>(); a.mode = Mode.Dive; a.progress = sc.MainProgress(s); a.speed = -1;
                sc.ownAnchors.Add((s, a));
            }
            BypassLooks(sc, sIn, sOut);
            // a second log arch and a glowing crystal chamber near the start (the caves' own look)
            { float s = Mathf.Lerp(sIn, sOut, .18f); var q = sc.PointAt(s, out var d); var r = Vector3.Cross(Vector3.up, Flat(d)); var crystal = Mt(JurassicLook.Crystal, true, JurassicLook.Crystal * .9f);
              for (int i = 0; i < 6; i++) Box(scen, "Cave crystal", q + r * ((i % 2 == 0 ? -1 : 1) * (8.5f - i * .3f)) + Vector3.down * 4.5f + Flat(d) * (i * 6 - 15), new Vector3(1.2f, 3.5f + i * .4f, 1.2f), crystal, Quaternion.Euler(i * 7 - 18, i * 40, i * 5 - 12), false); }
            Debug.Log($"[Course] Sunken Caves: line {L:0} m (main {CavesTo - CavesFrom:0} m: the lagoon and the river), dive zone at {sIn - 24:0} m, entry {sIn:0} m, exit {sOut:0} m along it, current {CavesCurrent:0} m/s");
        }

        // the lagoon's bed with the caves' sinkhole: the bed box split round where the caves' line goes through the bed's level
        static void LagoonBedWithSinkhole(Material mud)
        {
            bypassLine ??= Shortcut.Smooth(CavesCtrl());
            Vector3 c0 = W(400, -16.5f, -460), sz = W(200, 1, 320); float x0 = c0.x - sz.x * .5f, x1 = c0.x + sz.x * .5f, z0 = c0.z - sz.z * .5f, z1 = c0.z + sz.z * .5f;
            float hx0 = float.MaxValue, hx1 = float.MinValue, hz0 = float.MaxValue, hz1 = float.MinValue;
            foreach (var q in bypassLine) if (q.y < -10f && q.y > -24f && q.x > x0 && q.x < x1 && q.z > z0 && q.z < z1) { hx0 = Mathf.Min(hx0, q.x); hx1 = Mathf.Max(hx1, q.x); hz0 = Mathf.Min(hz0, q.z); hz1 = Mathf.Max(hz1, q.z); }
            if (hx0 > hx1) { Box(root, "Lagoon bed", c0, sz, mud); return; }
            hx0 -= 14; hx1 += 14; hz0 -= 14; hz1 += 14;
            void Piece(float a0, float a1, float b0, float b1) { if (a1 - a0 > .5f && b1 - b0 > .5f) Box(root, "Lagoon bed", new Vector3((a0 + a1) * .5f, c0.y, (b0 + b1) * .5f), new Vector3(a1 - a0, sz.y, b1 - b0), mud); }
            Piece(x0, x1, z0, hz0); Piece(x0, x1, hz1, z1); Piece(x0, hx0, hz0, hz1); Piece(hx1, x1, hz0, hz1);
            // the sinkhole's rim: mossy rocks round it, a violet glow down in it
            var rim = new MeshKit.Batch(); var rnd = new System.Random(17);
            for (int i = 0; i < 14; i++) { float a = i * Mathf.PI * 2 / 14; var p = new Vector3((hx0 + hx1) * .5f + Mathf.Cos(a) * (hx1 - hx0) * .55f, -15.5f, (hz0 + hz1) * .5f + Mathf.Sin(a) * (hz1 - hz0) * .55f); rim.Add(MeshKit.LowSphere, p, Quaternion.Euler(0, i * 30, 0), new Vector3(5f, 2.4f, 4f) * (.8f + (float)rnd.NextDouble() * .5f), i % 3 == 0 ? JurassicLook.Moss : JurassicLook.Rock); }
            rim.Object(scen, "Sinkhole rim", JurassicLook.VertexColored(true));
            Debug.Log($"[Course] Sunken Caves: lagoon bed sinkhole x {hx0:0}..{hx1:0}, z {hz0:0}..{hz1:0}");
        }
    }
}
