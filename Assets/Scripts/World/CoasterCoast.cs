using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 10 item 4: Coaster Coast (working name), the second course: a seaside boardwalk amusement park on a pier at sunset turning to
    // night. Same universe and toon style, reused systems (gates, zones, anchors, shortcuts, the crowd, TeedOff Barker), a new place (not a
    // Tee'd Off layout). Built at start-up from one racer line like Paradise Plunge; the two coaster rails are part of that line (their dense
    // points are its control points), so progress, gates and the route strip count along them. 8 km east of Paradise Plunge; only the
    // course being raced is shown (CourseLook). Water level y = 0, the pier's deck (the boardwalk, the plazas) y = 4.
    //   1 Boardwalk (car): start / finish on the boardwalk; the Midway Jump over the log flume, the choice jump: stay a car (under the big
    //     Ferris wheel's arc) or open the wings and fly through the middle of the hubless wheel; both rejoin before the coaster station.
    //   2 Coaster Run (car on rail A): the launch (boost pad), an airtime hill, a 180 deg banked turn, a vertical loop, the Coaster Gap (the
    //     track ends in a lip: a plain jump over the sea onto the landing deck).
    //   3 Top Hat (car on rail B): a corkscrew, a banked turn, the lift hill to the top hat 72 m up: off its top the wings come out.
    //   4 Sky Midway (plane): over the midway, round the helter-skelter, through the fireworks (three rings), past the mascot balloon.
    //   5 Log Flume (boat): the splashdown pool, the flume's bends through the park, under the boardwalk where the cars jump, out to sea.
    //   6 Lighthouse Sea (boat): along the coast, round the lighthouse (or through the rocks: the water shortcut), to the fishing pier.
    //   7 Sunken Carnival (sub): down between the fishing pier's legs, the drowned carousel and Ferris wheel, the octopus, the whale.
    //   8 Splash Return (sub, boat, car): up into the water ride's splash pool, up its ramp onto the boardwalk, the finish.
    public static partial class CoasterCoast
    {
        public static readonly Vector3 O = new Vector3(8000, 0, 0);
        public const float DeckY = 4f, Water = 0f, Road = DeckY + .9f, SeaFloor = -30f;
        public const float RoadHalf = 13f, RailHalf = 6f;
        static Transform root, scen; static Course c;
        static readonly Dictionary<string, float> mark = new Dictionary<string, float>();
        public static IReadOnlyDictionary<string, float> Marks => mark;
        public static Course Course => c;
        static Vector3 L(float x, float y, float z) => O + new Vector3(x, y, z);

        // the racer line: (mark, x, y, z) in metres from O; "railA" / "railB": the rails' own points are inserted there (Rails())
        static readonly (string n, float x, float y, float z)[] Points =
        {
            ("lineStart", 0, Road, -60), ("start", 0, Road, -20), ("G0", 0, Road, 0), ("", 0, Road, 20), ("", 0, Road, 60), ("promMid", 0, Road, 140),
            ("jM", 0, Road, 230), ("", 0, Road, 275), ("", 0, Road, 330), ("wheel", 0, Road, 430), ("", 0, Road, 500), ("", 0, Road, 620), ("G1", 0, Road, 760), ("pad", 0, Road, 800), ("", 0, Road, 830),
            ("railA", 0, Road, 860),
            // after rail A's lip (inserted): over the Coaster Gap onto the landing deck, to rail B
            ("", 126, 9.8f, 950), ("", 126, 8.0f, 922), ("", 126, 6.2f, 895), ("landA", 126, Road, 868), ("G2", 126, Road, 844), ("", 126, Road, 820),
            ("railB", 126, Road, 798),
            // after rail B's crest (inserted): the flight over the midway
            ("", 560, 73, 576), ("", 650, 71, 605), ("helter", 735, 67, 670), ("", 790, 63, 760), ("", 806, 61, 865), ("", 782, 59, 965), ("fw1", 712, 58, 1042),
            ("fw2", 605, 56, 1092), ("fw3", 500, 50, 1094), ("", 425, 42, 1081), ("", 344, 32, 1030), ("", 293, 18, 949), ("", 282, 8, 890), ("splash", 283, .6f, 840),
            // the log flume through the park, under the boardwalk's jump, out to sea
            ("", 292, .6f, 770), ("fl1", 330, .6f, 692), ("", 365, .6f, 645), ("fl2", 340, .6f, 595), ("", 362, .6f, 545), ("fl3", 380, .6f, 495), ("", 368, .6f, 440),
            ("underLift", 355, .6f, 380), ("", 342, .6f, 330), ("fl4", 318, .6f, 296), ("", 262, .6f, 264), ("", 150, .6f, 252), ("cross", 0, .6f, 252), ("seaOut", -130, .6f, 250),
            // the sea: along the coast, round the lighthouse (main line west of it), to the fishing pier
            ("", -232, .6f, 205), ("sea1", -300, .6f, 112), ("", -345, .6f, 38), ("lightW", -410, .6f, -45), ("", -402, .6f, -150), ("sea2", -356, .6f, -268),
            ("diveZone", -372, 0, -372), ("", -390, -5, -440), ("", -414, -9, -505), ("portal", -398, -10, -556), ("", -330, -10, -560), ("", -262, -11, -560),
            ("", -200, -12, -560), ("", -138, -12, -557), ("carousel", -78, -12, -548),
            ("octo", -30, -12, -530), ("", 0, -10, -506), ("poolEdge", 6, -6, -484), ("surface", 4, .6f, -458), ("G8", 2, .6f, -432), ("rampFoot", 0, .6f, -410),
            ("rampLip", 0, DeckY + 4.6f, -370), ("landing", 0, DeckY + 3.4f, -346), ("G9", 0, Road, -316), ("", 0, Road, -200), ("", 0, Road, -60), ("finish", 0, Road, 0), ("lineEnd", 0, Road, 20),
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
            var g = new GameObject("Course Coaster Coast"); g.transform.SetParent(parent, false); root = g.transform;
            scen = new GameObject("Scenery").transform; scen.SetParent(root, false);
            c = g.AddComponent<Course>(); c.courseName = "Coaster Coast"; c.shortName = "Coaster Coast"; c.timeTrial = true; c.look = "coaster"; c.music = "race_coaster";
            c.halfWidth = 75; c.airHalfWidth = 380; c.minY = SeaFloor - 6; c.maxY = 150; c.endMargin = 40; c.startMode = Mode.Drive;
            MakeLine();
            Sea(); Boardwalk(); Plazas(); Flume(); BuildRails(); CoasterGap(); FishingPier(); SplashPool(); Lighthouse(); UnderPier();   // Round 13 item 5c
            Scenery(); SetPieces(); Grandstands();
            Gates(); RouteMarks(); Zones(); Anchors(); Signs(); Segments(); Shortcuts();
            c.marks = new Dictionary<string, float>(mark); c.stunts = new CoasterStunts();
            g.AddComponent<CoasterSky>().course = c;
            Debug.Log($"[Course] Coaster Coast: line {c.Length:0} m, {c.gates.Count} gates, {c.anchors.Count} anchors, rails {string.Join(", ", RailList.ConvertAll(r => $"{r.label} {r.Length:0} m"))}");
            return c;
        }

        // ---------------- the line, with the rails ----------------
        static RailPath railA, railB;
        static void MakeLine()
        {
            railA = RailA(L(0, Road, 860)); railB = RailB(L(126, Road, 798));
            var ctl = new List<(string n, Vector3 p)>();
            foreach (var pt in Points)
            {
                ctl.Add((pt.n, L(pt.x, pt.y, pt.z)));
                var rp = pt.n == "railA" ? railA : pt.n == "railB" ? railB : null;
                if (rp == null) continue;
                // the rail's points every ~4 m (it is sampled every 1 m), its named places
                float last = 0;
                for (int i = 1; i < rp.p.Count; i++)
                {
                    string nm = ""; foreach (var kv in rp.names) if (kv.Value == i) nm = kv.Key;
                    if (rp.cum[i] - last < 4f && nm == "" && i < rp.p.Count - 1) continue;
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
            Jumps.Add(new CourseStunts.Jump { name = "Midway Jump", lip = M("jM"), gap = 45, choice = true });
            Jumps.Add(new CourseStunts.Jump { name = "Coaster Gap", lip = M("lipA"), gap = 45 });
        }

        // a rail's shape: pieces appended one after another from a start point and heading (flat), every 1 m
        sealed class RailPath
        {
            public readonly List<Vector3> p = new List<Vector3>(), u = new List<Vector3>(); public readonly List<float> cum = new List<float>();
            public readonly Dictionary<string, int> names = new Dictionary<string, int>();
            public Vector3 P, F; public Vector3 Rt => Vector3.Cross(Vector3.up, F);
            public RailPath(Vector3 start, Vector3 fwd) { P = start; F = fwd.normalized; Add(start, Vector3.up); }
            public void Add(Vector3 pos, Vector3 up) { if (p.Count > 0 && (pos - p[p.Count - 1]).sqrMagnitude < .04f) return; cum.Add(p.Count == 0 ? 0 : cum[cum.Count - 1] + Vector3.Distance(p[p.Count - 1], pos)); p.Add(pos); u.Add(up.normalized); }
            public void Name(string n) => names[n] = p.Count - 1;
            static int N(float len) => Mathf.Max(2, Mathf.CeilToInt(len));
            static float Smooth(float u) => u * u * (3 - 2 * u);
            public void Straight(float len, float rise = 0)
            {
                Vector3 p0 = P; int n = N(len);
                for (int i = 1; i <= n; i++) { float t = i / (float)n; Add(p0 + F * len * t + Vector3.up * rise * Smooth(t), Vector3.up); }
                P = p0 + F * len + Vector3.up * rise;
            }
            public void Hill(float len, float h)
            {
                Vector3 p0 = P; int n = N(len);
                for (int i = 1; i <= n; i++) { float t = i / (float)n; Add(p0 + F * len * t + Vector3.up * h * (1 - Mathf.Cos(2 * Mathf.PI * t)) * .5f, Vector3.up); }
                P = p0 + F * len;
            }
            // a flat turn (deg > 0: right), radius r, a bump up in the middle, banked towards the centre (bank deg at the middle)
            public void Turn(float deg, float r, float bump, float bank)
            {
                float s = Mathf.Sign(deg), a = Mathf.Abs(deg); Vector3 p0 = P, f0 = F; var ctr = p0 + Vector3.Cross(Vector3.up, f0) * s * r; var arm = p0 - ctr;
                int n = N(a * Mathf.Deg2Rad * r);
                for (int i = 1; i <= n; i++)
                {
                    float t = i / (float)n; var q = Quaternion.AngleAxis(s * a * t, Vector3.up);
                    var flat = ctr + q * arm; var inward = (ctr - flat).normalized;
                    float b = bank * Mathf.Deg2Rad * Mathf.Min(1, Smooth(Mathf.Clamp01(t / .25f)), Smooth(Mathf.Clamp01((1 - t) / .25f)));
                    Add(flat + Vector3.up * bump * Mathf.Sin(Mathf.PI * t), Vector3.up * Mathf.Cos(b) + inward * Mathf.Sin(b));
                }
                F = Quaternion.AngleAxis(s * a, Vector3.up) * f0; P = ctr + Quaternion.AngleAxis(s * a, Vector3.up) * arm;
            }
            // a vertical loop (LoopRail's shape), its exit shifted to the right
            public void Loop(float R, float shift)
            {
                Vector3 p0 = P, f0 = F, rt = Rt; int n = N(2 * Mathf.PI * R);
                for (int i = 1; i <= n; i++)
                {
                    float th = i * Mathf.PI * 2 / n; var pos = LoopRail.Pos(p0, f0, rt, R, shift, th);
                    Add(pos, -f0 * Mathf.Sin(th) + Vector3.up * Mathf.Cos(th));
                }
                P = p0 + rt * shift;
            }
            // a corkscrew: one full roll round an axis R above the track, over len (left: rolls to the left first)
            public void Cork(float len, float R, bool left)
            {
                Vector3 p0 = P, rt = Rt; int n = N(len * 1.6f); float sd = left ? -1 : 1;
                for (int i = 1; i <= n; i++)
                {
                    float t = i / (float)n, th = 2 * Mathf.PI * Smooth(t); var axis = p0 + Vector3.up * R + F * len * t;
                    var pos = axis - Vector3.up * R * Mathf.Cos(th) + rt * sd * R * Mathf.Sin(th);
                    Add(pos, (axis - pos).normalized);
                }
                P = p0 + F * len;
            }
        }

        // rail A: the launch, an airtime hill, a 180 deg banked right turn, the loop, the run-out to the Coaster Gap's lip
        static RailPath RailA(Vector3 e)
        {
            var r = new RailPath(e, Vector3.forward);
            r.Straight(70); r.Name("aLaunch");
            r.Hill(160, 26); r.Name("aHill");
            r.Straight(20); r.Turn(180, 70, 12, 62); r.Name("aTurn");
            r.Straight(40); r.Name("aLoopIn"); r.Loop(22, 14); r.Name("aLoopOut");
            r.Straight(50); r.Straight(40, 6.1f); r.Straight(8); r.Name("lipA");
            return r;
        }
        // rail B: a corkscrew, a banked left turn, the lift hill, the top hat's crest
        static RailPath RailB(Vector3 e)
        {
            var r = new RailPath(e, Vector3.back);
            r.Straight(22); r.Name("corkIn"); r.Cork(110, 8, true); r.Name("corkOut");
            r.Straight(14); r.Turn(-90, 80, 6, 55); r.Name("bTurn");
            r.Straight(30); r.Name("liftFoot"); r.Straight(220, 67.2f); r.Straight(26); r.Name("crest");
            return r;
        }
        public static readonly List<CoasterRail> RailList = new List<CoasterRail>();
        static CoasterRail MakeRail(string label, RailPath rp, float entry)
        {
            var g = new GameObject("Coaster rail " + label); g.transform.SetParent(root, false);
            var r = g.AddComponent<CoasterRail>(); r.label = label; r.Build(rp.p, rp.u); r.capture = 15f;
            r.entryProgress = entry; r.exitProgress = entry + r.Length; RailList.Add(r);
            return r;
        }
        static float RailS(RailPath rp, string name) => rp.cum[rp.names[name]];

        // ---------------- race logic ----------------
        static void Gates()
        {
            void G(int i, string label, float p, Vector2 half, params Mode[] modes)
            {
                var q = At(p); var d = Dir(p);
                var g = new GameObject("Gate " + label); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(q, Quaternion.LookRotation(d, Vector3.up));
                var pg = g.AddComponent<ProgressGate>(); pg.index = i; pg.label = label; pg.halfSize = half; pg.modes = modes; pg.progress = p; c.gates.Add(pg);
                if (i > 0) GateMarker.Make(pg, c, !Array.Exists(modes, m => m == Mode.Dive) || Array.Exists(modes, m => m == Mode.Boat));
            }
            G(0, "G0 start", M("G0"), new Vector2(15, 8), Mode.Drive);
            G(1, "G1 coaster station", M("G1"), new Vector2(15, 8), Mode.Drive);
            G(2, "G2 Coaster Gap landing", M("G2"), new Vector2(15, 8), Mode.Drive);
            G(3, "G3 top hat", M("crest") - 3, new Vector2(12, 9), Mode.Drive, Mode.Fly);
            G(4, "G4 fireworks", M("fw2"), new Vector2(36, 26), Mode.Fly);
            G(5, "G5 flume splashdown", M("splash") + 60, new Vector2(28, 10), Mode.Boat);
            G(6, "G6 out to sea", M("seaOut"), new Vector2(22, 10), Mode.Boat);
            G(7, "G7 under the pier", M("portal"), new Vector2(13, 8), Mode.Dive);
            G(8, "G8 splash pool", M("G8"), new Vector2(18, 10), Mode.Boat, Mode.Dive);
            G(9, "G9 boardwalk landing", M("G9"), new Vector2(15, 9), Mode.Drive);
            G(10, "G10 finish", M("finish"), new Vector2(15, 8), Mode.Drive);
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
            // off the top hat's crest (the rail ends there): wings out
            float cr = M("crest"); var cp = At(cr);
            Z("Wings out (top hat)", cp + Flat(Dir(cr)) * 9 + Vector3.up * 1, Facing(cr), new Vector3(30, 18, 16), Mode.Fly);
            float s = M("splash"); var sp = At(s); sp.y = 5;
            Z("Hull down (flume splashdown)", sp, Facing(s), new Vector3(56, 12, 140), Mode.Boat, true);   // Round 10: from 70 m before the pool's mark (the AI comes in low and early)
            float dz = M("diveZone"); var dp = At(dz); dp.y = -1;
            Z("Canopy seals (under the pier)", dp, Facing(dz), new Vector3(34, 8, 14), Mode.Dive);
            float su = M("surface"); var spp = At(su - 8); spp.y = -6;
            Z("Surface (splash pool)", spp, Facing(su), new Vector3(40, 14, 16), Mode.Boat);
            float rl = M("rampLip"); var rp = At(rl); rp.y = DeckY + 6;
            Z("Wheels down (splash pool ramp)", rp + Flat(Dir(rl)) * 5, Facing(rl), new Vector3(34, 12, 12), Mode.Drive);
            // a fall off the top hat (it can only happen to a car that left the crest sideways): straight back
            var fz = Vol<ResetZone>("Top hat fall", new Vector3(cp.x, 20, cp.z) + Flat(Dir(cr)) * 60, Facing(cr), new Vector3(140, 30, 120)); fz.label = "fell off the top hat";
        }
        static void Anchors()
        {
            void A(float p, Mode mode, float speed = -1)
            {
                var q = At(p); var g = new GameObject($"Anchor {mode} {p:0}"); g.transform.SetParent(root, false);
                g.transform.SetPositionAndRotation(q, Facing(p)); var a = g.AddComponent<RecoveryAnchor>(); a.mode = mode; a.progress = p; a.speed = speed; c.anchors.Add(a);
            }
            A(M("start"), Mode.Drive); A(M("jM") - 150, Mode.Drive, 45); A(M("jM") + 120, Mode.Drive, 40); if (!ShiftVehicle.OldFlight) A(M("G1") - 22, Mode.Drive, 40);   /* Round 12: the Midway flight's rejoin */ A(M("G1") + 5, Mode.Drive, 40);
            A(M("landA") + 6, Mode.Drive, 35);
            A(M("crest") + 120, Mode.Fly); A(M("helter") + 60, Mode.Fly); A(M("fw1"), Mode.Fly); A(M("fw3"), Mode.Fly);
            A(M("splash") + 80, Mode.Boat); A(M("fl2"), Mode.Boat); A(M("underLift"), Mode.Boat); A(M("cross") + 30, Mode.Boat);
            A(M("sea1"), Mode.Boat); A(M("sea2") - 30, Mode.Boat); A(M("diveZone") - 40, Mode.Boat);
            A(M("diveZone") + 70, Mode.Dive); A(M("portal") + 30, Mode.Dive); A(M("portal") + 190, Mode.Dive); A(M("carousel"), Mode.Dive); A(M("G8") - 12, Mode.Boat); A(M("G9") + 20, Mode.Drive);
            // Round 13 item 5c: under the boardwalk and the pier an anchor every ~60-80 m, so a lost racer is put back close by (and forward)
            if (!OldPier) { A(M("cross") - 70, Mode.Boat); A(M("seaOut") - 20, Mode.Boat); A(M("portal") - 35, Mode.Dive); A(M("portal") + 110, Mode.Dive); A(M("octo"), Mode.Dive); A(M("poolEdge") - 12, Mode.Dive); }
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
            S(M("jM") - 70, 15.5f, DeckY, Mode.Fly, 5);   // the choice: wings optional
            S(M("G2") + 10, 15f, DeckY, Mode.Fly, 6);       // the top hat's launch ahead
            S(M("splash") - 160, -40, 30, Mode.Boat, 8);
            S(M("diveZone") - 110, -24, Water, Mode.Dive, 6);
            S(M("rampFoot") - 14, -16, Water, Mode.Drive, 5);
        }
        static void Segments()
        {
            void Sg(string name, string form, float target, float end, int gates) => c.segments.Add(new Course.Segment { name = name, form = form, target = target, endProgress = end, gatesNeeded = gates });
            Sg("Boardwalk", "Driving", 15, M("G1"), 2);
            Sg("Coaster Run", "Driving", 16, M("G2"), 3);
            Sg("Top Hat", "Driving", 10, M("crest") - 3, 4);
            Sg("Sky Midway", "Flying", 16, M("splash"), 5);
            Sg("Log Flume", "Boating", 20, M("seaOut"), 7);
            Sg("Lighthouse Sea", "Boating", 17, M("diveZone"), 7);
            Sg("Sunken Carnival", "Diving", 16, M("poolEdge"), 8);
            Sg("Splash Return", "Dive-boat-drive", 10, M("finish"), 11);
        }

        // ---------------- shortcuts: the choice flight through the Ferris wheel, the lighthouse rocks ----------------
        static readonly Color ShortcutColor = new Color(.75f, .4f, 1f, .9f);
        public static Vector3 WheelCentre => L(-28, 36, 430);
        public const float WheelR = 30f;
        static void Shortcuts()
        {
            // 1 Midway flight: off the Midway Jump's lip with the wings out, up through the middle of the hubless Ferris wheel and back down to
            // the boardwalk's wheels-down zone before the coaster station (G1)
            float lip = M("jM"), rejoin = M("G1") - 45; var a = At(lip); var b = At(rejoin); var w = WheelCentre;
            // Round 10: back over the boardwalk's line 75 m before the wheels-down zone (the first shape swung the AI 76 m east of it)
            var ctrl = new List<Vector3> { a + Vector3.up * 1.5f, Vector3.Lerp(a, w, .35f) + Vector3.up * 4f, w + Vector3.back * 60 + Vector3.up * -2, w, w + Vector3.forward * 55 + Vector3.down * 6,
                                           new Vector3(-16, 22, 560) + O, new Vector3(-4, 14, b.z - O.z - 75) + O, b + Vector3.up * 6f, At(rejoin + 12) + Vector3.up * 5f };
            for (int i = 0; i < ctrl.Count; i++) { var p = ctrl[i]; p.y = Mathf.Max(p.y, DeckY + 2); ctrl[i] = p; }
            // Round 12 item 3: the entry right off the lip (and entered as the wings open), bigger entry / exit frames, the exit taking a car
            // (a plane already through the wheels-down zone), a wider and taller wheels-down zone; a reset on it goes forward (Shortcut.flight)
            bool old = ShiftVehicle.OldFlight;
            var sc = old ? MakeShortcut("Midway flight", ctrl, lip, rejoin + 12, 1, -1, 40f, new Vector2(14, 12), new Vector2(18, 12), 60f, 30f, Mode.Fly)
                         : MakeShortcut("Midway flight", ctrl, lip, rejoin + 12, 1, -1, 48f, new Vector2(26, 18), new Vector2(28, 18), 12f, 30f, Mode.Fly);
            sc.choice = true; sc.flight = true; if (!old) sc.exit.modes = new[] { Mode.Fly, Mode.Drive };
            var z = old ? Vol<TransformZone>("Wheels down (midway flight)", b + Vector3.up * 13, Facing(rejoin), new Vector3(40, 30, 16)) : Vol<TransformZone>("Wheels down (midway flight)", b + Vector3.up * 16, Facing(rejoin), new Vector3(60, 40, 24)); z.target = Mode.Drive; z.label = "Wheels down (midway flight)";
            var chev = new MeshKit.Batch();
            for (int i = 0; i < 4; i++) chev.Add(MeshKit.Cube, new Vector3(b.x, DeckY + .05f, b.z) + Flat(Dir(rejoin)) * (-10 + i * 6), Facing(rejoin) * Quaternion.Euler(0, i % 2 == 0 ? 20 : -20, 0), new Vector3(10, .05f, 1.2f), new Color(.95f, .5f, .1f));
            chev.Object(scen, "wheels-down chevrons", CoasterLook.Vc(false), false, false);
            // the choice jump's lip: crossing it as a car opens the wings window
            var lq = At(lip); var lipVol = Vol<ChoiceLip>("Midway Jump lip", new Vector3(lq.x, DeckY + Lift(lip) + 3, lq.z), Facing(lip), new Vector3(30, 8, 6)); lipVol.window = 1f;

            // 2 Lighthouse rocks: the main line goes round the lighthouse's west side; the shortcut threads the rocks on its east side (a 26 m
            // channel between the islet and three sea stacks, a current through it)
            float s0 = M("sea1") - 20, s1 = M("sea2") + 10; var li = Lighthouse0;
            var lc = new List<Vector3> { At(s0), At(s0 + 30), new Vector3(li.x + 44, .6f, li.z + 70), new Vector3(li.x + 46, .6f, li.z), new Vector3(li.x + 36, .6f, li.z - 80), At(s1 - 30), At(s1) };
            for (int i = 0; i < lc.Count; i++) { var p = lc[i]; p.y = .6f; lc[i] = p; }
            var ls = MakeShortcut("Lighthouse rocks", lc, s0, s1, 7, -1, 22f, new Vector2(13, 8), new Vector2(15, 8), 40f, 40f, Mode.Boat);
            for (float s = 60; s < ls.Length - 60; s += 26) { var p0 = ls.PointAt(s, out var d0); p0.y = -2; var cz = Vol<CurrentZone>("Lighthouse current", p0, Quaternion.LookRotation(Flat(d0)), new Vector3(20, 6, 26)); cz.push = Flat(d0) * 4f; }
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
        public const float KickerLen = 45f, KickerH = 3.2f;
        public static float Lift(float p)
        {
            float j = mark.TryGetValue("jM", out var x) ? x : -1e9f;
            if (p > j - KickerLen && p <= j + .01f) { float u = (p - (j - KickerLen)) / KickerLen; return KickerH * u * u; }
            return 0;
        }
        public static bool InGap(float p) { foreach (var j in Jumps) if (p > j.lip && p < j.lip + j.gap) return true; return false; }
        public static bool OnRailLine(float p) { foreach (var r in RailList) if (p > r.entryProgress - .5f && p < r.exitProgress + .5f) return true; return false; }
        sealed class CoasterStunts : CourseStunts
        {
            readonly List<Vector2> rails = new List<Vector2>();
            public CoasterStunts() { foreach (var r in RailList) rails.Add(new Vector2(r.entryProgress, r.exitProgress)); }
            public override IReadOnlyList<Jump> Jumps => CoasterCoast.Jumps;
            public override IReadOnlyList<Vector2> Rails => rails;
            public override bool Narrow(float p, Mode mode)
            {
                if (OnRailLine(p) || (p > M("railA") - 120 && p < M("railA")) || (p > M("lipA") - 10 && p < M("railB") + 5)) return true;
                if (p > M("jM") - KickerLen - 25 && p < M("jM") + 70) return true;
                foreach (var k in new[] { "splash", "seaOut", "cross", "diveZone", "portal", "surface", "rampFoot", "rampLip", "landing", "G1", "G8", "G9", "finish", "fl1", "fl2", "fl3", "fl4", "underLift" })
                    if (Mathf.Abs(p - M(k)) < 40) return true;
                return false;
            }
            public override bool NoBrake(float from, float to)
            {
                foreach (var r in RailList) if (to > r.entryProgress - 30 && from < r.exitProgress + 5) return true;
                return from < M("railB") && to > M("lipA") - 5;   // over the Coaster Gap
            }
            public override bool EntryAim(float p, float ahead, out Vector3 aim)
            {
                aim = default;
                foreach (var r in RailList) if (p < r.entryProgress && p + ahead > r.entryProgress) { aim = r.Start + r.F0 * (p + ahead - r.entryProgress); return true; }
                return false;
            }
        }
    }
}
