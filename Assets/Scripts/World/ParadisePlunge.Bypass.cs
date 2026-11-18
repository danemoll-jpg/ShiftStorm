using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 13 item 3: the Mangrove submerged bypass (the concept's "later experiment"). Just after the lagoon, before the S-bend, a marked
    // dive lane on the river's left (violet buoys, a DIVE sign) turns the boat into a submarine; it goes down through an opening in the river
    // bed into a flooded root cavern that runs straight under the S-bend (under the banks), with hanging mangrove roots, schools of glowing
    // fish and a sunken log arch, a warm current along it, and comes up through a second opening after bend4, where a surfacing zone makes
    // it a boat again before G5. Boat on the surface (the bends and their currents) or sub underneath (shorter, slower, the current helps):
    // about equal. Its own dive / surface zones, entry / exit sub-gates and its own anchors (a reset in it restarts in it). "-noBypass":
    // build 14's river (before numbers).
    public static partial class ParadisePlunge
    {
        public static bool MangroveBypass => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-noBypass") < 0;
        public const float BypassDepth = -27f, BypassCurrent = 7f;   // the current tuned for an about-equal time (15 m/s: the sub 2.6 s faster)
        // the dive bay: the left bank cut back to BayOuter m from the line between BayFrom and BayTo; its mouth is the first BayMouth m, the
        // rest of its edge with the river is a row of mangrove root posts (a boat drifting wide in the bend glances off them, never dives)
        public static float BayFrom => M("bend1") - 118;
        public static float BayTo => M("bend1") - 22;
        public const float BayOuter = 38f, BayMouth = 16f;
        public static float BypassFrom => BayFrom - 60;   // main progress where it leaves the line (on the surface)
        public static float BypassTo => M("G5") - 28;      // and where it rejoins (a boat again)
        // the AI keeps exactly to the line beside the bay unless it goes for the bypass
        public static bool InDiveLane(float p) => Old ? MangroveBypass && mark.ContainsKey("bend1") && p > BypassFrom - 20 && p < BayTo + 10 : mark.ContainsKey("splash") && p > CavesFrom - 30 && p < CavesFrom + 110;
        static List<Vector3> bypassLine;

        static List<Vector3> BypassCtrl()
        {
            Vector3 P(float p, float left, float y) { var q = At(p) - Right(p) * left; q.y = y; return q; }
            float b1 = M("bend1"), b4 = M("bend4");
            var list = new List<Vector3> { P(BypassFrom, 0, .6f), P(BayFrom - 25, 9, .6f), P(BayFrom + 6, 22, .6f), P(BayFrom + 26, 29, .6f), P(BayFrom + 44, 29, -4f), P(BayFrom + 64, 28, -15f), P(BayFrom + 88, 25, -24f) };
            // the straight chord under the S-bend, in even ~45 m steps (a long Catmull-Rom segment between short ones overshoots: build 15's
            // first try looped into a 21 m bend at the chord's end and the sub ground along the cavern's wall)
            var s0 = P(b1 + 20, 18, BypassDepth); var s1 = P(b4 - 25, 4, BypassDepth); int n = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(s0, s1) / 45f));
            for (int i = 0; i <= n; i++) list.Add(Vector3.Lerp(s0, s1, i / (float)n));
            list.AddRange(new[] { P(b4 + 15, 5, -23f), P(b4 + 45, 5, -15f), P(b4 + 70, 6, -6f), P(b4 + 95, 6, -1.2f), P(BypassTo - 14, 2, .6f), P(BypassTo, 0, .6f), P(BypassTo + 15, 0, .6f) });
            return list;
        }

        // the bay: its water, its bed (with the bypass's way down), the root posts along its river edge, a mossy log over its mouth
        static readonly List<Vector2> bayHole = new List<Vector2>();
        static void BypassBay()
        {
            bypassLine ??= Shortcut.Smooth(BypassCtrl());
            float hint = BypassFrom; bayHole.Clear(); float h0 = -1, h1 = -1;
            foreach (var q in bypassLine) { float p = c.Project(q, hint, out var at, out _); hint = p; float lat = Vector3.Dot(q - at, Right(p)); if (lat < -18f && q.y + 7.5f > -16f && q.y - 8f < -17f) { if (h0 < 0) h0 = p; h1 = p; } }
            var mud = Mt(JurassicLook.Mud);
            for (float p = BayFrom; p < BayTo; p += 12) { float m = Mathf.Min(p + 6, BayTo); var q = At(m) - Right(m) * ((20f + BayOuter) * .5f); q.y = (Water - 16f) * .5f; Vol<WaterVolume>("Dive bay", q, Facing(m), new Vector3(BayOuter - 19f, 16f, 20f)); }
            void Bed(float x0, float x1) { if (x1 - x0 > 1) MeshKit.Strip(root, "Dive bay bed", Samples(x0, x1, -BayOuter - 1, -20.5f, (p, pt) => -16f, (p, pt) => -17f, 4f), mud, null, true, 0, 40, false); }
            if (h0 > 0) { Bed(BayFrom, h0 - 10); Bed(h1 + 10, BayTo); Debug.Log($"[Course] Mangrove bypass: dive bay {BayFrom:0}-{BayTo:0} m, its bed's opening {h0 - 10:0}-{h1 + 10:0} m"); } else Bed(BayFrom, BayTo);
            var posts = new MeshKit.Batch(); var wood = JurassicLook.FernTrunk;
            for (float p = BayFrom + BayMouth; p < BayTo + 1f; p += 2.6f)   // 1.5 m gaps: narrower than a hull
            {
                var q = At(p) - Right(p) * 21.2f; q.y = -3f;
                var g = new GameObject("bay root post"); g.transform.SetParent(root, false); g.transform.position = q; g.layer = ShiftVehicle.ObstacleLayer;
                var cc = g.AddComponent<CapsuleCollider>(); cc.radius = .55f; cc.height = 12f;
                posts.Add(MeshKit.LowCylinder, q + Vector3.up * 2.5f, Quaternion.Euler(0, 0, (p * 7) % 10 - 5), new Vector3(.9f, 5.5f, .9f), wood);
            }
            posts.Object(scen, "Bay root posts", JurassicLook.VertexColored(true));
        }

        // the river bed's pieces between a and b, with the two openings the bypass goes through (keeps the bed either side of each opening)
        static List<(float a, float b, float l, float r)> RiverBedPieces(float a, float b)
        {
            var list = new List<(float, float, float, float)>();
            if (Old && !MangroveBypass) { list.Add((a, b, -21, 21)); return list; }
            bypassLine ??= Shortcut.Smooth(Old ? BypassCtrl() : CavesCtrl());   // Round 17: the Sunken Caves come up through the river bed before the portal
            var holes = new List<(float p0, float p1, float l0, float l1)>(); bool inHole = false; float hp0 = 0, hp1 = 0, lmin = 0, lmax = 0, hint = Old ? BypassFrom : CavesFrom;
            foreach (var q in bypassLine)
            {
                bool cross = q.y + 5.5f + 2f > -16f && q.y - 5.5f - 2.5f < -17f;
                float p = c.Project(q, hint, out var at, out _); hint = p; float lat = Vector3.Dot(q - at, Right(p));
                if (cross) { if (!inHole) { inHole = true; hp0 = p; lmin = lat; lmax = lat; } hp1 = p; lmin = Mathf.Min(lmin, lat); lmax = Mathf.Max(lmax, lat); }
                else if (inHole) { inHole = false; holes.Add((hp0 - 8, hp1 + 8, lmin - 13, lmax + 16)); }
            }
            if (inHole) holes.Add((hp0 - 8, hp1 + 8, lmin - 13, lmax + 16));
            float from = a;
            foreach (var h in holes)
            {
                if (h.p1 < a || h.p0 > b || h.l1 < -21 || h.l0 > 21) continue;   // Round 17: only openings in this river's bed
                if (h.p0 > from) list.Add((from, h.p0, -21, 21));
                if (h.l0 > -19) list.Add((h.p0, h.p1, -21, h.l0));
                if (h.l1 < 19) list.Add((h.p0, h.p1, h.l1, 21));
                Debug.Log($"[Course] Mangrove bypass: river bed opening from {h.p0:0} to {h.p1:0} m, {h.l0:0.0} .. {h.l1:0.0} m across");
                from = h.p1;
            }
            if (b > from) list.Add((from, b, -21, 21));
            return list;
        }

        static void BypassShortcut()
        {
            var ctrl = BypassCtrl();
            var sc = MakeShortcut("Mangrove bypass", ctrl, BypassFrom, BypassTo, 5, -1, 13f, new Vector2(11, 8), new Vector2(11, 8), 1f, 1f, Mode.Dive);
            float L = sc.Length;
            // its sub-gates: in after the dive (still near the surface, a sub), out before the surfacing zone (still a sub)
            float sIn = -1, sOut = -1; for (float s = 0; s < L; s += 1) { var q = sc.PointAt(s, out _); if (sIn < 0 && q.y < -5f) sIn = s; if (q.y < -5f) sOut = s; }
            Object.Destroy(sc.entry.gameObject); Object.Destroy(sc.exit.gameObject);
            sc.entry = SubGate(sc, "Mangrove bypass entry", sIn, new Vector2(11, 8), Mode.Dive); sc.exit = SubGate(sc, "Mangrove bypass exit", sOut, new Vector2(12, 9), Mode.Dive);
            // the dive lane's zone just before the entry, the surfacing zone just after the exit (a boat on the main line never meets either:
            // the dive lane is 7-17 m left of the line, and a boat passing through a surfacing zone stays a boat)
            var dz = sc.PointAt(Mathf.Max(0, sIn - 22), out var dd); dz.y = -1;
            var zin = Vol<TransformZone>("Dive (mangrove bypass)", dz, Quaternion.LookRotation(Flat(dd)), new Vector3(10, 8, 12)); zin.target = Mode.Dive; zin.label = "Dive (mangrove bypass)";
            var uz = sc.PointAt(Mathf.Min(L, sOut + 26), out var ud); uz.y = -5;
            var zout = Vol<TransformZone>("Surface (mangrove bypass)", uz, Quaternion.LookRotation(Flat(ud)), new Vector3(24, 12, 14)); zout.target = Mode.Boat; zout.label = "Surface (mangrove bypass)";
            Debug.Log($"[Course] Mangrove bypass: line {L:0} m (main {BypassTo - BypassFrom:0} m), dive zone at {sIn - 22:0} m, entry {sIn:0} m, exit {sOut:0} m, surfacing at {sOut + 26:0} m along it");
            Tunnel(sc, -16f, -15f, false, "Root cavern");
            // the warm current along the cavern's straight (the sub is slower than the boat; the line is shorter)
            for (float s = sIn + 40; s < sOut - 40; s += 20) { var p0 = sc.PointAt(s, out var d0); var cz = Vol<CurrentZone>("Root cavern current", p0, Quaternion.LookRotation(d0), new Vector3(18, 11, 22)); cz.push = d0 * BypassCurrent; }
            // its own anchors: a reset in it restarts in it, a submarine on its line (never back on the surface behind the dive)
            foreach (float f in new[] { .3f, .55f, .78f })
            {
                float s = Mathf.Lerp(sIn, sOut, f); var q = sc.PointAt(s, out var d); var g = new GameObject($"Anchor Dive (bypass) {s:0}"); g.transform.SetParent(sc.transform, false);
                g.transform.SetPositionAndRotation(q, Quaternion.LookRotation(d, Vector3.up)); var a = g.AddComponent<RecoveryAnchor>(); a.mode = Mode.Dive; a.progress = sc.MainProgress(s); a.speed = -1;
                sc.ownAnchors.Add((s, a));
            }
            BypassLooks(sc, sIn, sOut);
        }

        static void BypassLooks(Shortcut sc, float sIn, float sOut)
        {
            float L = sc.Length; var violet = new Color(.75f, .4f, 1f);
            // the dive lane: violet buoys along it, a DIVE sign on the left bank
            var buoys = new MeshKit.Batch(); int n = 0;
            for (float s = 6; s < sIn - 4; s += 9, n++) { var q = sc.PointAt(s, out var d); var r = Vector3.Cross(Vector3.up, Flat(d)); q.y = Water + .4f; foreach (float k in new[] { -1f, 1f }) buoys.Add(MeshKit.LowCylinder, q + r * k * 6.5f, Quaternion.identity, new Vector3(.9f, 1.3f, .9f), n % 2 == 0 ? violet : Color.white); }
            buoys.Object(scen, "Dive lane buoys", JurassicLook.VertexColored(true, new Color(.1f, .05f, .15f)), false, false);
            { var q = sc.PointAt(Mathf.Max(0, sIn - 40), out var d); var h = new GameObject("Sign dive (bypass)"); h.transform.SetParent(scen, false); var r = Vector3.Cross(Vector3.up, Flat(d)); h.transform.SetPositionAndRotation(new Vector3(q.x, 1.4f, q.z) - r * 15f, Quaternion.LookRotation(Flat(d))); WorldBuilder.Sign(h.transform, Vector3.zero, Mode.Dive, 6); }
            // mangrove roots hanging from the cavern's roof, glowing fungus on its floor
            var roots = new MeshKit.Batch(); var glow = new MeshKit.Batch(); var rnd = new System.Random(313);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            for (float s = sIn + 25; s < sOut - 20; s += 6)
            {
                var q = sc.PointAt(s, out var d); var r = Vector3.Cross(Vector3.up, Flat(d)); float side = (int)(s / 6) % 2 == 0 ? -1 : 1, len = R(2.5f, 5f);
                var top = q + Vector3.up * 5.5f + r * side * R(3.5f, 9f);
                roots.Add(MeshKit.LowCylinder, top + Vector3.down * len * .5f, Quaternion.Euler(R(-12, 12), 0, R(-12, 12)), new Vector3(.3f, len, .3f), JurassicLook.FernTrunk);
                if (rnd.NextDouble() < .45) glow.Add(MeshKit.LowSphere, q + Vector3.down * 5.3f - r * side * R(4, 9), Quaternion.identity, new Vector3(R(.8f, 1.6f), .5f, R(.8f, 1.6f)), Color.white);
            }
            roots.Object(scen, "Root cavern roots", JurassicLook.VertexColored(true));
            glow.Object(scen, "Root cavern glow", JurassicLook.Mat(new Color(.4f, 1f, .75f), false, new Color(.3f, .9f, .6f)), false, false);
            // a sunken log arch across the middle (looks only: the sub passes under it)
            {
                float s = Mathf.Lerp(sIn, sOut, .5f); var q = sc.PointAt(s, out var d); var r = Vector3.Cross(Vector3.up, Flat(d)); var wood = Mt(JurassicLook.FernTrunk, true); var moss = Mt(JurassicLook.Moss);
                foreach (float k in new[] { -1f, 1f }) Box(scen, "Sunken log post", q + r * k * 9.6f, new Vector3(1.6f, 11, 1.6f), wood, Quaternion.LookRotation(Flat(d)), false);
                Box(scen, "Sunken log", q + Vector3.up * 4.2f, new Vector3(21, 1.8f, 1.8f), wood, Quaternion.LookRotation(Flat(d)) * Quaternion.Euler(0, 0, 3), false);
                Box(scen, "Sunken log moss", q + Vector3.up * 5.2f, new Vector3(19, .4f, 2.1f), moss, Quaternion.LookRotation(Flat(d)), false);
            }
            // schools of glowing fish circling in the cavern
            foreach (float f in new[] { .2f, .4f, .62f, .85f })
            {
                float s = Mathf.Lerp(sIn, sOut, f); var q = sc.PointAt(s, out _); var g = new GameObject("Glowing fish"); g.transform.SetParent(scen, false); g.transform.position = q + Vector3.up * 1.5f;
                var fs = g.AddComponent<FishSchool>(); fs.count = 9; fs.radius = 6.5f; fs.color = f < .5f ? new Color(.35f, 1f, .9f) : new Color(1f, .8f, .35f);
            }
        }
    }

    // looks only: a school of small glowing fish circling round a point (no colliders)
    public sealed class FishSchool : MonoBehaviour
    {
        public int count = 8; public float radius = 6, speed = .5f; public Color color = Color.cyan; readonly List<Transform> fish = new List<Transform>();
        void Start()
        {
            var mat = JurassicLook.Mat(color, false, color * .9f);
            for (int i = 0; i < count; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(transform, false);
                g.transform.localScale = new Vector3(.35f, .3f, .9f); var r = g.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; fish.Add(g.transform);
            }
        }
        void Update()
        {
            for (int i = 0; i < fish.Count; i++)
            {
                float a = Time.time * speed + i * Mathf.PI * 2 / fish.Count, rr = radius * (.75f + .25f * Mathf.Sin(i * 1.7f));
                var p = new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a * 2 + i) * 1.2f, Mathf.Sin(a) * rr);
                fish[i].localPosition = p; fish[i].localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)));
            }
        }
    }
}
