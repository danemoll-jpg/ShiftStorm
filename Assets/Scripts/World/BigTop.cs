using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 15 item 3: Big Top Breakdown, the third course: a travelling circus camped on a river meadow in bright, gusty daylight (Coaster
    // Coast is a seaside fair at night). The show is a bit ramshackle ("Breakdown"): wagons rolling about the midway, a clown car pile-up
    // left in a pit, a top flap that blew open. Same universe, toon style and systems (gates, zones, anchors, rails, shortcuts, the crowd,
    // Barnaby); a new place. Built at start-up from one racer line like Coaster Coast; the Globe of Death and the cannon's barrel are rails
    // on that line (CoasterRail). 9 km west of Paradise Plunge; only the course being raced is shown (CourseLook). Ground (the meadow, the
    // roads) y = 4, the river and the lake y = 0.
    //   1 The Midway (car): start / finish on the midway road; the wagon parade rolling along both sides of the road; the Clown Jump over
    //     the clowns' car pile-up: the choice jump (LB): stay a car and ride the Globe of Death (a steel-mesh sphere, the loop inside it),
    //     or open the wings and fly past it; both rejoin before the big top's door.
    //   2 The Big Top (car): in through the door, round the ring past the grandstands, into the giant cannon (a rail up its barrel).
    //   3 Human Cannonball (plane): out of the cannon's muzzle, through three hoops under the tent past the trapeze, out through the top
    //     flap that blew open, a long curve over the circus camp to the river.
    //   4 The River (boat): the splashdown past the elephants, the paddle-steamer show boat, the island (main channel west; the Island
    //     rapids east: the water's choice), the seals, to the dive buoys.
    //   5 Sunken Circus (sub): down into the lake, past the escape artist's giant water tank, through a sunken circus wagon, along the lake
    //     bed to the splash pool.
    //   6 Ring Return (sub, boat, car): up into the pool, up its ramp onto the midway road, the finish.
    public static partial class BigTop
    {
        public static readonly Vector3 O = new Vector3(-9000, 0, 0);
        public const float G = 4f, Water = 0f, Road = G + .9f, Floor = -18f;
        public const float RoadHalf = 14f, WagonRoadHalf = 17f;
        public static readonly Vector3 TentC = new Vector3(0, 0, 1000);   // from O
        public const float TentR = 140f, TentWall = 16f, TentApex = 75f;
        public static float RoofAt(float r) => TentApex - (TentApex - TentWall) * Mathf.Clamp01(r / TentR);
        public static readonly Vector3 FlapC = new Vector3(88, 0, 960); public const float FlapR = 24f;   // the top flap's hole (plan centre, radius)
        static Transform root, scen; static Course c;
        static readonly Dictionary<string, float> mark = new Dictionary<string, float>();
        public static IReadOnlyDictionary<string, float> Marks => mark;
        public static Course Course => c;
        static Vector3 L(float x, float y, float z) => O + new Vector3(x, y, z);

        // the racer line: (mark, x, y, z) in metres from O; "globe" / "cannon": the rails' own points are inserted there
        static readonly (string n, float x, float y, float z)[] Points =
        {
            ("lineStart", 0, Road, -60), ("start", 0, Road, -20), ("G0", 0, Road, 0), ("", 0, Road, 40), ("wagons", 0, Road, 100), ("", 0, Road, 200), ("midway", 0, Road, 330),
            ("", 0, Road, 460), ("wagonsEnd", 0, Road, 560), ("", 0, Road, 600), ("jC", 0, Road, 640), ("", 0, Road, 662), ("", 3, Road, 700), ("", 9, Road, 730),
            ("globe", GlobeX, Road, 760),   // Round 16 item 1: the Globe 12 m east (was on the midway's axis): the Clown flight's hoops go by its west side
            // after the Globe (its exit is 6 m east): to the big top's door, round the ring (radius 72 about the tent's centre, east side
            // first), a left turn into the cannon
            ("", GlobeX + 6, Road, 828), ("", 10, Road, 858), ("door", 0, Road, 890), ("", 18, Road, 922), ("", 41.3f, Road, 941), ("", 65.3f, Road, 969.6f), ("ringE", 71.7f, Road, 1006.3f),
            ("", 59f, Road, 1041.3f), ("", 30.4f, Road, 1065.3f), ("ringN", -6.3f, Road, 1071.7f), ("", -41.3f, Road, 1059f), ("", -65.3f, Road, 1030.4f), ("ringW", -72f, Road, 1000f),
            ("", -60.3f, Road, 971.7f), ("", -32f, Road, 960f),
            ("cannon", -20, Road, 960),
            // after the muzzle (inserted): the hoops under the tent, out of the top flap, the long curve over the camp to the river
            // (Round 15 first lap: 75 s; the river moved 305 m east and the lake 180 m further south for a ~2 min lap: the flight swings left
            // then right round the camp in two 300 m arcs to land heading south on the river)
            ("hoop1", 40, 17.5f, 960), ("hoop2", 60, 24.7f, 960), ("hoop3", 77, 30.8f, 960), ("flap", 88, 34.6f, 960), ("", 130, 45, 972), ("", 175, 54, 988), ("", 258, 62, 1030),
            ("", 322, 65, 1092), ("over", 425, 66, 1179), ("", 561, 64, 1212), ("", 711, 58, 1172), ("", 821, 46, 1062), ("", 861, 30, 912), ("", 861, 13, 830), ("splash", 861, .6f, 750),
            // the river: the elephants, the show boat, the island (main channel west of it), the seals, the dive buoys
            ("", 861, .6f, 690), ("elephants", 862, .6f, 620), ("", 862, .6f, 540), ("steamer", 860, .6f, 440), ("", 855, .6f, 340), ("", 838, .6f, 280), ("", 805, .6f, 225), ("islandN", 785, .6f, 150),
            ("", 782, .6f, 100), ("islandS", 786, .6f, 40), ("", 815, .6f, -40), ("seals", 855, .6f, -120), ("", 860, .6f, -180), ("diveZone", 858, 0, -235),
            // the lake: the escape tank, the sunken wagon, a long curve along the lake bed to the splash pool
            ("", 852, -5, -290), ("tank", 835, -11, -370), ("", 790, -12, -470), ("wagon", 700, -12, -560), ("", 590, -12, -640), ("", 470, -12, -700), ("", 340, -12, -728),
            ("", 210, -12, -718), ("", 110, -12, -670), ("", 45, -12, -605), ("", 14, -11, -548), ("", 0, -10, -506), ("poolEdge", -4, -6, -484), ("surface", -3, .6f, -458), ("G8", -2, .6f, -432), ("rampFoot", 0, .6f, -410),
            ("rampLip", 0, G + 4.6f, -370), ("landing", 0, G + 3.4f, -346), ("G9", 0, Road, -316), ("", 0, Road, -200), ("", 0, Road, -60), ("finish", 0, Road, 0), ("lineEnd", 0, Road, 20),
        };
        public static float M(string n) => mark[n];
        static Vector3 At(float prog) => c.PointAt(prog, out _);
        static Vector3 Dir(float prog) { c.PointAt(prog, out var d); return d; }
        static Vector3 Flat(Vector3 d) { d.y = 0; return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward; }
        static Vector3 Right(float prog) => Vector3.Cross(Vector3.up, Flat(Dir(prog)));
        static Quaternion Facing(float prog, bool flat = true) => Quaternion.LookRotation(flat ? Flat(Dir(prog)) : Dir(prog), Vector3.up);

        public static Course Build(Transform parent)
        {
            mark.Clear(); Jumps.Clear(); RailList.Clear();
            var g = new GameObject("Course Big Top Breakdown"); g.transform.SetParent(parent, false); root = g.transform;
            scen = new GameObject("Scenery").transform; scen.SetParent(root, false);
            c = g.AddComponent<Course>(); c.courseName = "Big Top Breakdown"; c.shortName = "Big Top Breakdown"; c.timeTrial = true; c.look = "bigtop"; c.music = "race_bigtop";
            c.halfWidth = 75; c.airHalfWidth = 380; c.minY = Floor - 6; c.maxY = 150; c.endMargin = 40; c.startMode = Mode.Drive;
            MakeLine();
            Land(); Roads(); BuildRails(); Tent(); Cannon(); RiverAndLake(); SplashPool();
            Midway(); Camp(); SetPieces(); Grandstands();
            Gates(); RouteMarks(); Zones(); Anchors(); Signs(); Segments(); Shortcuts();
            c.marks = new Dictionary<string, float>(mark); c.stunts = new BigTopStunts();
            Debug.Log($"[Course] Big Top Breakdown: line {c.Length:0} m, {c.gates.Count} gates, {c.anchors.Count} anchors, rails {string.Join(", ", RailList.ConvertAll(r => $"{r.label} {r.Length:0} m"))}");
            return c;
        }

        // ---------------- the line, with the rails ----------------
        static CoasterCoast.RailPath globeP, cannonP;
        public const float GlobeR = 13f, GlobeShift = 6f, GlobeSphere = 15.5f, GlobeX = 12f;
        static void MakeLine()
        {
            globeP = new CoasterCoast.RailPath(L(GlobeX, Road, 760), Vector3.forward);
            globeP.Straight(36); globeP.Name("gIn"); globeP.Loop(GlobeR, GlobeShift); globeP.Name("gOut"); globeP.Straight(16); globeP.Name("gEnd");   // a long lead-in: a car off the Clown Jump is down by then
            cannonP = new CoasterCoast.RailPath(L(-20, Road, 960), Vector3.right);
            cannonP.Straight(8); cannonP.Name("breech"); cannonP.Ramp(34, 6.1f); cannonP.Name("muzzle");
            var ctl = new List<(string n, Vector3 p)>();
            foreach (var pt in Points)
            {
                ctl.Add((pt.n, L(pt.x, pt.y, pt.z)));
                var rp = pt.n == "globe" ? globeP : pt.n == "cannon" ? cannonP : null;
                if (rp == null) continue;
                float last = 0;
                for (int i = 1; i < rp.p.Count; i++)
                {
                    string nm = ""; foreach (var kv in rp.names) if (kv.Value == i) nm = kv.Key;
                    if (rp.cum[i] - last < 3f && nm == "" && i < rp.p.Count - 1) continue;
                    last = rp.cum[i]; ctl.Add((nm, rp.p[i]));
                }
            }
            var pts = new List<Vector3>(); var at = new List<int>(); int n = ctl.Count;
            Vector3 P(int i) => ctl[Mathf.Clamp(i, 0, n - 1)].p;
            for (int i = 0; i < n - 1; i++)
            {
                at.Add(pts.Count);
                Vector3 p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
                int steps = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(p1, p2) / 3f));
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)steps, t2 = t * t, t3 = t2 * t;
                    pts.Add(.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3));
                }
            }
            at.Add(pts.Count); pts.Add(P(n - 1));
            c.line = pts; c.Finish();
            for (int i = 0; i < n; i++) if (ctl[i].n != "") mark[ctl[i].n] = c.CumulativeAt(at[i]);
            Jumps.Add(new CourseStunts.Jump { name = "Clown Jump", lip = M("jC"), gap = PitLen, choice = true });
        }

        public static readonly List<CoasterRail> RailList = new List<CoasterRail>();
        static CoasterRail MakeRail(string label, CoasterCoast.RailPath rp, float entry)
        {
            var g = new GameObject("Rail " + label); g.transform.SetParent(root, false);
            var r = g.AddComponent<CoasterRail>(); r.label = label; r.Build(rp.p, rp.u); r.capture = 13f;
            r.entryProgress = entry; r.exitProgress = entry + r.Length; RailList.Add(r);
            return r;
        }
        static float RailS(CoasterCoast.RailPath rp, string name) => rp.cum[rp.names[name]];

        // ---------------- race logic ----------------
        static void Gates()
        {
            void Gt(int i, string label, float p, Vector2 half, params Mode[] modes)
            {
                var q = At(p); var d = Dir(p);
                var g = new GameObject("Gate " + label); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(q, Quaternion.LookRotation(d, Vector3.up));
                var pg = g.AddComponent<ProgressGate>(); pg.index = i; pg.label = label; pg.halfSize = half; pg.modes = modes; pg.progress = p; c.gates.Add(pg);
                if (i > 0) GateMarker.Make(pg, c, !Array.Exists(modes, m => m == Mode.Dive) || Array.Exists(modes, m => m == Mode.Boat));
            }
            Gt(0, "G0 start", M("G0"), new Vector2(16, 8), Mode.Drive);
            Gt(1, "G1 big top door", M("door"), new Vector2(17, 9), Mode.Drive);
            Gt(2, "G2 the ring", M("ringN"), new Vector2(15, 8), Mode.Drive);
            Gt(3, "G3 cannon", M("muzzle") - 3, new Vector2(12, 9), Mode.Drive, Mode.Fly);
            Gt(4, "G4 top flap", M("flap"), new Vector2(22, 16), Mode.Fly);
            Gt(5, "G5 river splashdown", M("splash") + 60, new Vector2(28, 10), Mode.Boat);
            Gt(6, "G6 show boat", M("steamer"), new Vector2(24, 10), Mode.Boat);
            Gt(7, "G7 sunken wagon", M("wagon"), new Vector2(8, 4.5f), Mode.Dive);
            Gt(8, "G8 splash pool", M("G8"), new Vector2(18, 10), Mode.Boat, Mode.Dive);
            Gt(9, "G9 midway landing", M("G9"), new Vector2(16, 12), Mode.Drive);   // Round 15: 12 m high (a car off the pool ramp passed 0.6 m over a 9 m frame)
            Gt(10, "G10 finish", M("finish"), new Vector2(16, 8), Mode.Drive);
        }
        static T Vol<T>(string name, Vector3 center, Quaternion rot, Vector3 size) where T : BoxVolume
        {
            var g = new GameObject(name); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(center, rot);
            var v = g.AddComponent<T>(); v.size = size; return v;
        }
        static void Zones()
        {
            void Z(string label, Vector3 center, Quaternion rot, Vector3 size, Mode target, bool corridor = false)
            { var z = Vol<TransformZone>(label, center, rot, size); z.target = target; z.landingCorridor = corridor; z.label = label; }
            // the cannon's muzzle (its rail ends there): wings out
            float mz = M("muzzle"); var mp = At(mz);
            // Round 15 split check: build 17's first zone (30 x 18 m, down to 3.5 m) reached the ring road just inside the door, and a rival's
            // car swinging wide there grew wings on the ground; now it starts 7.5 m up (the muzzle is at 11 m, the road at 4.9) and is 20 m wide
            // Round 16 item 1: the human-like pilot left the muzzle twice in ten without wings (through the 16 m deep zone too high); now from
            // 1 m past the muzzle to 41 m past it, 8.5 to 38.5 m up (the ring road's cars are under 7 m), 34 m wide
            if (OldClown) Z("Wings out (human cannonball)", new Vector3(mp.x, 0, mp.z) + Flat(Dir(mz)) * 8 + Vector3.up * 15f, Facing(mz), new Vector3(20, 15, 16), Mode.Fly);
            else Z("Wings out (human cannonball)", new Vector3(mp.x, 0, mp.z) + Flat(Dir(mz)) * 21 + Vector3.up * 23.5f, Facing(mz), new Vector3(34, 30, 40), Mode.Fly);
            float s = M("splash"); var sp = At(s); sp.y = 5;
            Z("Hull down (river splashdown)", sp, Facing(s), new Vector3(56, 12, 140), Mode.Boat, true);
            float dz = M("diveZone"); var dp = At(dz); dp.y = -1;
            Z("Canopy seals (dive buoys)", dp, Facing(dz), new Vector3(40, 8, 14), Mode.Dive);
            float su = M("surface"); var spp = At(su - 8); spp.y = -6;
            Z("Surface (splash pool)", spp, Facing(su), new Vector3(40, 14, 16), Mode.Boat);
            float rl = M("rampLip"); var rp = At(rl); rp.y = G + 6;
            Z("Wheels down (splash pool ramp)", rp + Flat(Dir(rl)) * 5, Facing(rl), new Vector3(34, 12, 12), Mode.Drive);
            // the clowns' pit under the Clown Jump: a car that falls in is reset at once (the anchor before the jump, at speed)
            float j = M("jC"); var jq = At(j + PitLen * .5f);
            var pit = Vol<ResetZone>("Clown pit", new Vector3(jq.x, G - 6, jq.z), Facing(j), new Vector3(PitHalf * 2 + 4, 10, PitLen - 1)); pit.label = "fell into the clowns' pit";
        }
        static void Anchors()
        {
            void A(float p, Mode mode, float speed = -1)
            {
                var q = At(p); var g = new GameObject($"Anchor {mode} {p:0}"); g.transform.SetParent(root, false);
                g.transform.SetPositionAndRotation(q, Facing(p)); var a = g.AddComponent<RecoveryAnchor>(); a.mode = mode; a.progress = p; a.speed = speed; c.anchors.Add(a);
            }
            A(M("start"), Mode.Drive); A(M("wagons") + 60, Mode.Drive); A(M("midway"), Mode.Drive); A(M("jC") - 150, Mode.Drive, 45);
            A(M("door") - (OldClown ? 25 : 14), Mode.Drive, 35);   // the Clown flight's and the Globe's rejoin (Round 16: after the later rejoin)
            A(M("door") + 30, Mode.Drive, 25); A(M("ringE"), Mode.Drive, 25); A(M("ringN") + 5, Mode.Drive, 25); A(M("ringW") - 10, Mode.Drive, 22);
            A(M("hoop2"), Mode.Fly); A(M("flap") + 60, Mode.Fly); A(M("over"), Mode.Fly); A(M("splash") - 120, Mode.Fly);
            A(M("splash") + 80, Mode.Boat); A(M("elephants") + 20, Mode.Boat); A(M("steamer") + 10, Mode.Boat); A(M("islandS") + 20, Mode.Boat); A(M("seals"), Mode.Boat); A(M("diveZone") - 40, Mode.Boat);
            A(M("diveZone") + 70, Mode.Dive); A(M("tank") + 40, Mode.Dive); A(M("wagon") + 40, Mode.Dive); A(M("wagon") + 160, Mode.Dive); A(M("wagon") + 280, Mode.Dive); A(M("poolEdge") - 12, Mode.Dive);
            A(M("G8") - 12, Mode.Boat); A(M("G9") + 20, Mode.Drive); A(M("finish") - 150, Mode.Drive);
            c.Finish();
        }
        static void Signs()
        {
            void S(float p, float side, float y, Mode form, float post)
            {
                var q = At(p); var h = new GameObject("Sign " + form); h.transform.SetParent(scen, false);
                h.transform.SetPositionAndRotation(new Vector3(q.x, y, q.z) + Right(p) * side, Facing(p));
                WorldBuilder.Sign(h.transform, Vector3.zero, form, post);
            }
            S(M("jC") - 70, WagonRoadHalf + 2, G, Mode.Fly, 5);   // the choice: wings optional
            S(M("ringW") - 30, 10, G, Mode.Fly, 5);                // the cannon ahead
            S(M("splash") - 150, -36, 34, Mode.Boat, 8);
            S(M("diveZone") - 110, -30, Water, Mode.Dive, 6);
            S(M("rampFoot") - 14, -16, Water, Mode.Drive, 5);
        }
        static void Segments()
        {
            void Sg(string name, string form, float target, float end, int gates) => c.segments.Add(new Course.Segment { name = name, form = form, target = target, endProgress = end, gatesNeeded = gates });
            // targets: the check pilot's safe line (build 17: 16.2 / 9.4 / 14.6 / 18.5 / 28.4 / 7.4 s), rounded up
            Sg("The Midway", "Driving", 17, M("door"), 2);
            Sg("The Big Top", "Driving", 10, M("muzzle") - 3, 4);
            Sg("Human Cannonball", "Flying", 15, M("splash"), 5);
            Sg("The River", "Boating", 19, M("diveZone"), 7);
            Sg("Sunken Circus", "Diving", 29, M("poolEdge"), 8);
            Sg("Ring Return", "Dive-boat-drive", 8, M("finish"), 11);
        }

        // ---------------- shortcuts: the Clown flight past the Globe, the Island rapids ----------------
        static readonly Color ShortcutColor = new Color(.75f, .4f, 1f, .9f);
        static void Shortcuts()
        {
            // 1 Clown flight: off the Clown Jump's lip with the wings out, past the Globe of Death's west side (every bend >= 250 m) and down
            // to the wheels-down zone before the big top's door (Round 12's rules: entered as the wings open, big frames, the exit takes a car)
            // Round 16 item 1 (Dan: "a little confusing where the first flight is supposed to go"): four striped circus hoops along it (the HUD
            // arrow points at the next one, the camera looks toward it), the Globe moved 12 m east to give them room, the exit framed by a
            // lit arch ("TO THE BIG TOP") with spotlights on the wheels-down chevrons, the rejoin 13 m later than build 18's (a gentler last bend)
            float lip = M("jC"), rejoin = M("door") - ClownRejoinBack; var a = At(lip); var b = At(rejoin);
            var ctrl = OldClown ? new List<Vector3> { a + Vector3.up * 1.5f, L(-2, 10, 690), L(-12, 17, 740), L(-22, 20, 796), L(-17, 15, 826), b + Vector3.up * 5f, At(rejoin + 12) + Vector3.up * 4f }
                                : new List<Vector3> { a + Vector3.up * 1.5f, L(-3, 11, 688), L(-10, 15.5f, 733), L(-15, 18, 778), L(-10, 15, 813), L(-1, 10.5f, 840), b + Vector3.up * 5f, At(rejoin + 12) + Vector3.up * 4f };
            for (int i = 0; i < ctrl.Count; i++) { var p = ctrl[i]; p.y = Mathf.Max(p.y, G + 2); ctrl[i] = p; }
            var sc = MakeShortcut("Clown flight", ctrl, lip, rejoin + 12, 1, -1, 48f, new Vector2(26, 18), new Vector2(28, 18), 12f, 30f, Mode.Fly);
            sc.choice = true; sc.flight = true; sc.exit.modes = new[] { Mode.Fly, Mode.Drive };
            WheelsDown(rejoin);
            if (!OldClown) ClownHoops(sc);
            var lq = At(lip); var lipVol = Vol<ChoiceLip>("Clown Jump lip", new Vector3(lq.x, G + Lift(lip) + 3, lq.z), Facing(lip), new Vector3(30, 8, 6)); lipVol.window = 1.5f; lipVol.ramp = KickerLen;
            // 2 Island rapids: the main line swings out through the island's narrow west channel; the rapids run straight down the rocky
            // east side with a current (shorter and quicker, rocks either side of the line)
            float s0 = M("islandN") - 165, s1 = M("islandS") + 125;   // leaves before the main line swings west, rejoins after it is back
            var lc = new List<Vector3> { At(s0), At(s0 + 25), L(857, .6f, 262), L(870, .6f, 205), L(876, .6f, 140), L(876, .6f, 80), L(870, .6f, 20), L(857, .6f, -35), At(s1 - 20), At(s1) };
            for (int i = 0; i < lc.Count; i++) { var p = lc[i]; p.y = .6f; lc[i] = p; }
            var ir = MakeShortcut("Island rapids", lc, s0, s1, 7, -1, 22f, new Vector2(16, 8), new Vector2(18, 8), 40f, 30f, Mode.Boat);
            for (float s = 50; s < ir.Length - 40; s += 24) { var p0 = ir.PointAt(s, out var d0); p0.y = -2; var cz = Vol<CurrentZone>("Rapids current", p0, Quaternion.LookRotation(Flat(d0)), new Vector3(22, 6, 24)); cz.push = Flat(d0) * 8f; }
        }
        public static readonly bool OldClown = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldBtFlight") >= 0;   // checks: build 18's Clown flight
        public static float ClownRejoinBack => OldClown ? 45f : 32f;
        // the Clown flight's four hoops (red and white, a gold rim, lit) and the exit's arch with its spotlights
        static void ClownHoops(Shortcut sc)
        {
            var b = new MeshKit.Batch(); const float R = 12.5f;
            foreach (float frac in new[] { .18f, .38f, .58f, .76f })
            {
                float s = sc.Length * frac; var q = sc.PointAt(s, out var d); var frame = Quaternion.LookRotation(d, Vector3.up); sc.rings.Add(s);
                for (int i = 0; i < 20; i++) { float an = i * 360f / 20; b.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(an * Mathf.Deg2Rad), Mathf.Sin(an * Mathf.Deg2Rad), 0) * R), frame * Quaternion.Euler(0, 0, an), new Vector3(1.3f, R * .34f, 1.3f), i % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite); }
                for (int i = 0; i < 20; i++) { float an = (i + .5f) * 360f / 20; b.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(an * Mathf.Deg2Rad), Mathf.Sin(an * Mathf.Deg2Rad), 0) * (R + .9f)), frame * Quaternion.Euler(0, 0, an), new Vector3(.5f, R * .3f, .5f), BigTopLook.Gold); }
                float postH = Mathf.Max(.5f, q.y - R - G); b.Add(MeshKit.Cube, new Vector3(q.x, G + postH * .5f, q.z), Quaternion.identity, new Vector3(.6f, postH, .6f), BigTopLook.Gold);   // a post down to the ground
            }
            sc.rings.Add(sc.exit ? sc.Project(sc.exit.transform.position, -1, out _, out _) : sc.Length - 30);
            b.Object(scen, "Clown flight hoops", GateMarker.Beacon, false, false);
            var sp = new GameObject("Clown flight hoops (set-piece)"); sp.transform.SetParent(scen, false); sp.transform.position = sc.PointAt(sc.Length * .47f, out _);
            SetPiece.Make(sp, "Clown flight hoops", "The Midway", "bthoops", Vector3.zero, new Vector3(30, 26, 120)).range = 400;
            // the exit: a lit arch over the road at the wheels-down zone, "TO THE BIG TOP", two spotlight beams on the chevrons
            float rj = M("door") - ClownRejoinBack; var e = At(rj); e.y = G; var fw = Flat(Dir(rj)); var f = Facing(rj); var rt = Right(rj); var arch = new MeshKit.Batch();
            foreach (float s in new[] { -1f, 1f }) for (int k = 0; k < 9; k++) arch.Add(MeshKit.Cube, e + rt * s * 22 + Vector3.up * (1 + k * 2.2f), f, new Vector3(1.6f, 2.2f, 1.6f), k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite);
            arch.Add(MeshKit.Cube, e + Vector3.up * 21, f, new Vector3(46, 3.6f, .8f), BigTopLook.Royal);
            arch.Object(scen, "Clown flight exit arch", BigTopLook.Vc(true), false, false);
            var bulbs = new MeshKit.Batch(); foreach (float s in new[] { -1f, 1f }) for (int k = 0; k < 9; k++) bulbs.Add(MeshKit.LowSphere, e + rt * s * 22 + Vector3.up * (1.6f + k * 2.2f) - fw * .9f, Quaternion.identity, Vector3.one * .7f, BigTopLook.Gold);
            for (int k = -10; k <= 10; k++) bulbs.Add(MeshKit.LowSphere, e + rt * k * 2.1f + Vector3.up * 23.2f, Quaternion.identity, Vector3.one * .6f, BigTopLook.Gold);
            bulbs.Object(scen, "Clown flight exit bulbs", GateMarker.Beacon, false, false);
            Label("TO THE BIG TOP", e + Vector3.up * 21 - fw * .6f, Quaternion.LookRotation(-fw), 2.2f, BigTopLook.Gold);
            var beams = new MeshKit.Batch(); foreach (float s in new[] { -1f, 1f }) beams.Add(MeshKit.LowCone, e + rt * s * 14 + Vector3.up * 11, Quaternion.AngleAxis(s * 18f, fw), new Vector3(9, 22, 9), new Color(1f, .95f, .7f, .22f));
            var bm = beams.Object(scen, "Clown flight exit spotlights", GateMarker.Beacon, false, false);
            var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Color", new Color(1f, .95f, .75f, .28f)); mpb.SetFloat("_Pulse", 0); bm.GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
            var ex = new GameObject("Clown flight exit (set-piece)"); ex.transform.SetParent(scen, false); ex.transform.position = e + Vector3.up * 12;
            SetPiece.Make(ex, "Clown flight exit arch", "The Midway", "", Vector3.zero, new Vector3(46, 24, 3)).range = 400;
            // the cannonball flight's landing, framed the same way: a floating arch of buoys over the river at the splashdown, "SPLASHDOWN"
            float sd = M("splash") + 20; var sq = At(sd); sq.y = Water; var sf = Flat(Dir(sd)); var sr = Right(sd); var sa = new MeshKit.Batch();
            foreach (float s in new[] { -1f, 1f }) for (int k = 0; k < 8; k++) sa.Add(MeshKit.Cube, sq + sr * s * 26 + Vector3.up * (1 + k * 2.4f), Facing(sd), new Vector3(1.8f, 2.4f, 1.8f), k % 2 == 0 ? BigTopLook.TopRed : BigTopLook.TopWhite);
            sa.Add(MeshKit.Cube, sq + Vector3.up * 20.5f, Facing(sd), new Vector3(54, 3.4f, .8f), BigTopLook.Royal);
            foreach (float s in new[] { -1f, 1f }) sa.Add(MeshKit.LowCylinder, sq + sr * s * 26 + Vector3.up * .2f, Quaternion.identity, new Vector3(5, 1.6f, 5), BigTopLook.Orange);
            sa.Object(scen, "Splashdown arch", BigTopLook.Vc(true), false, false);
            Label("SPLASHDOWN", sq + Vector3.up * 20.5f - sf * .6f, Quaternion.LookRotation(-sf), 2.2f, BigTopLook.Gold);
        }
        static void WheelsDown(float rejoin)
        {
            var b = At(rejoin); b.y = Road;
            // Round 16 item 1: wider to the west and deeper (the Clown flight comes in from the west of the road: a late pilot reached the tent
            // wall beside the door with the wings still out); the meadow there is flush with the road
            var z = OldClown ? Vol<TransformZone>("Wheels down (Clown flight)", b + Vector3.up * 30, Facing(rejoin), new Vector3(60, 68, 36))
                             : Vol<TransformZone>("Wheels down (Clown flight)", b + Vector3.up * 30 - Right(rejoin) * 18 - Flat(Dir(rejoin)) * 12, Facing(rejoin), new Vector3(100, 68, 60));
            z.target = Mode.Drive; z.label = z.name;
            // and a last one in the big top's doorway (Round 15 split check: Rae's plane came over the first one high and flew into the tent)
            float dp = M("door") - 8; var dq = At(dp); dq.y = Road;
            var dz = Vol<TransformZone>("Wheels down (big top door)", dq + Vector3.up * 9, Facing(dp), new Vector3(46, 22, 14)); dz.target = Mode.Drive; dz.label = dz.name;
            var chev = new MeshKit.Batch();
            for (int i = 0; i < 4; i++) chev.Add(MeshKit.Cube, new Vector3(b.x, G + .05f, b.z) + Flat(Dir(rejoin)) * (-10 + i * 6), Facing(rejoin) * Quaternion.Euler(0, i % 2 == 0 ? 20 : -20, 0), new Vector3(10, .05f, 1.2f), new Color(.95f, .5f, .1f));
            chev.Object(scen, "wheels-down chevrons", BigTopLook.Vc(false), false, false);
        }
        static Shortcut MakeShortcut(string label, List<Vector3> ctrl, float from, float to, int next, int replaces, float halfWidth, Vector2 entryHalf, Vector2 exitHalf, float entryAt, float exitBack, Mode mode)
        {
            var g = new GameObject("Shortcut " + label); g.transform.SetParent(root, false); var sc = g.AddComponent<Shortcut>();
            sc.label = label; sc.line = Shortcut.Smooth(ctrl); sc.mainFrom = from; sc.mainTo = to; sc.nextGate = next; sc.replacesGate = replaces; sc.halfWidth = halfWidth;
            float Ln = sc.Length; sc.entry = SubGate(sc, label + " entry", entryAt, entryHalf, mode); sc.exit = SubGate(sc, label + " exit", Ln - exitBack, exitHalf, mode);
            c.shortcuts.Add(sc);
            Debug.Log($"[Course] shortcut {label}: line {Ln:0} m (main {to - from:0} m)");
            return sc;
        }
        static ProgressGate SubGate(Shortcut sc, string label, float s, Vector2 half, Mode mode)
        {
            var at = sc.PointAt(s, out var d);
            var g = new GameObject("Sub-gate " + label); g.transform.SetParent(sc.transform, false); g.transform.SetPositionAndRotation(at, Quaternion.LookRotation(d, Vector3.up));
            var pg = g.AddComponent<ProgressGate>(); pg.label = label; pg.halfSize = half; pg.modes = new[] { mode }; pg.index = -1;
            float hx = half.x, hy = half.y, t = Mathf.Clamp(Mathf.Max(hx, hy) * .05f, .5f, 1.6f);
            var b = new MeshKit.Batch();
            b.Add(MeshKit.Cube, new Vector3(-hx, 0, 0), Quaternion.identity, new Vector3(t, 2 * hy + t, t), ShortcutColor); b.Add(MeshKit.Cube, new Vector3(hx, 0, 0), Quaternion.identity, new Vector3(t, 2 * hy + t, t), ShortcutColor);
            b.Add(MeshKit.Cube, new Vector3(0, hy, 0), Quaternion.identity, new Vector3(2 * hx + t, t, t), ShortcutColor); b.Add(MeshKit.Cube, new Vector3(0, -hy, 0), Quaternion.identity, new Vector3(2 * hx + t, t, t), ShortcutColor);
            var fr = b.Object(g.transform, "shortcut frame", GateMarker.Beacon, false, false);
            var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Color", ShortcutColor); mpb.SetFloat("_Pulse", 0); fr.GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
            return pg;
        }

        // ---------------- what the AI and the HUD need (CourseStunts) ----------------
        public static readonly List<CourseStunts.Jump> Jumps = new List<CourseStunts.Jump>();
        public const float KickerLen = 45f, KickerH = 3.2f, PitLen = 44f, PitHalf = 24f, PitFloor = .5f;
        public static float Lift(float p)
        {
            float j = mark.TryGetValue("jC", out var x) ? x : -1e9f;
            if (p > j - KickerLen && p <= j + .01f) { float u = (p - (j - KickerLen)) / KickerLen; return KickerH * u * u; }
            return 0;
        }
        public static bool InGap(float p) { foreach (var j in Jumps) if (p > j.lip && p < j.lip + j.gap) return true; return false; }
        public static bool OnRailLine(float p) { foreach (var r in RailList) if (p > r.entryProgress - .5f && p < r.exitProgress + .5f) return true; return false; }
        sealed class BigTopStunts : CourseStunts
        {
            readonly List<Vector2> rails = new List<Vector2>();
            public BigTopStunts() { foreach (var r in RailList) rails.Add(new Vector2(r.entryProgress, r.exitProgress)); }
            public override IReadOnlyList<Jump> Jumps => BigTop.Jumps;
            public override IReadOnlyList<Vector2> Rails => rails;
            public override bool Narrow(float p, Mode mode)
            {
                if (OnRailLine(p)) return true;
                foreach (var r in RailList) if (p > r.entryProgress - 90 && p < r.entryProgress) return true;   // lined up for the Globe / the cannon
                if (p > M("jC") - KickerLen - 25 && p < M("jC") + PitLen + 40) return true;
                if (p > M("ringW") - 40 && p < M("flap") + 40) return true;   // the turn into the cannon, the hoops, the top flap
                foreach (var k in new[] { "door", "splash", "diveZone", "tank", "wagon", "surface", "rampFoot", "rampLip", "landing", "G8", "G9", "finish", "islandN", "islandS" })
                    if (Mathf.Abs(p - M(k)) < 40) return true;
                return false;
            }
            public override bool NoBrake(float from, float to)
            {
                foreach (var r in RailList) if (to > r.entryProgress - 30 && from < r.exitProgress + 5) return true;
                return false;
            }
            public override bool EntryAim(float p, float ahead, out Vector3 aim)
            {
                aim = default;
                foreach (var r in RailList) if (p < r.entryProgress && p + ahead > r.entryProgress) { aim = r.Start + r.F0 * (p + ahead - r.entryProgress); return true; }
                return false;
            }
            public override string Where(float p) => p < M("door") ? "the midway" : p < M("muzzle") ? "the big top" : p < M("splash") ? "over the camp" : p < M("diveZone") ? "the river" : p < M("poolEdge") ? "the lake" : "the home straight";
        }
    }
}
