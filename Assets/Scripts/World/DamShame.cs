using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 16 item 2: Dam Shame, the fourth course and the game's first multi-lap race (3 laps). A huge concrete dam across a mountain canyon
    // (pine forests, cliffs, the reservoir behind it, the spillway, the turbine hall) in fresh morning light. The route stays the same place,
    // but the water in the valley below the dam rises each lap, so the forms change (Docs/STORY.md: Buck's "controlled water demonstration"):
    //   Lap 1 "dry run":          the crest road, down the dry spillway chute as a car, off the flip bucket (a choice jump: LB = the Canyon
    //                             flight over the river), the valley road, the bridge, the turbine hall, the dam road back up to the crest.
    //   Lap 2 "a smidge":         Buck opens one gate a smidge: the chute runs (boat down the torrent), the flip bucket throws you into the air
    //                             (wings), the valley floor is awash (boat over the drowned roads, through the hall), wheels down at the dam road.
    //   Lap 3 "the full demonstration": the valley floods 16 m deep: boat down the chute into the flood, dive along the drowned road, under the
    //                             bridge's deck level, through the flooded turbine hall, surface at the dam road, wheels down where it leaves the water.
    // How it is built: the racer line is the lap three times over, end to end (the same plan each lap), so progress, gates in order, race
    // positions, recovery and splits (one segment per lap = the lap times) need nothing new. One water level for the valley, raised when the
    // leader starts lap 2 and lap 3 (DamWater: siren, Buck and Barnaby, a 10 s rise). Every change zone and every recovery anchor decides its
    // form from the water as it is at that moment (not from the lap), so a racer a lap down is always right; DamWater also changes a car the
    // water rises over into a boat, a boat pinned under water into a sub and a boat left on dry road into a car (never stranded or drowned).
    // 9 km south of Paradise Plunge; only the course being raced is shown (CourseLook). Valley floor y 0, the crest road y 40.
    public static partial class DamShame
    {
        public static readonly Vector3 O = new Vector3(0, 0, -9000);
        public const float CrestY = 40f, Floor = -8f, RoadHalf = 13f, ChuteHalf = 18f, ChuteWallH = 3.5f;
        public const int Laps = 3;
        public static readonly float[] ValleyLevel = { -2.5f, 1.4f, 16f };   // the valley's water per stage (0 = lap 1)
        public const float LakeLevel = 37f, PoolLevel = -1f;
        public const float KickerLen = 30f, KickerH = 1.6f, PoolZ0 = -487f, PoolZ1 = -545f, PoolX0 = 372f, PoolX1 = 438f;
        public const float DamX0 = -385f, DamX1 = 375f, DamTop = 16f, DamToe = -75f;   // the dam's body: crest z -16..16, its toe 75 m downstream
        public const float HallZ0 = -622f, HallZ1 = -458f, HallX0 = -427f, HallX1 = -350f, HallRoof = 14f;
        static Transform root, scen; static Course c;
        static readonly Dictionary<string, float> mark = new Dictionary<string, float>();
        public static IReadOnlyDictionary<string, float> Marks => mark;
        public static Course Course => c;
        public static float LapLen { get; private set; }
        static Vector3 L(float x, float y, float z) => O + new Vector3(x, y, z);

        // one lap's control points: name, x, road y (NaN: in the air over the plunge pool), z, the line's height over the road
        // (the line is where a sub aims in the flooded lap 3: 3 m over the valley's roads; 1 m elsewhere)
        static readonly (string n, float x, float road, float z, float lift)[] Loop =
        {
            ("F", -250, CrestY, 0, 1), ("", -150, CrestY, 0, 1), ("tower", -40, CrestY, 0, 1), ("", 80, CrestY, 0, 1), ("crestE", 200, CrestY, 0, 1), ("", 290, CrestY, 0, 1),
            ("", 350, CrestY, -10, 1), ("", 388, 39.6f, -45, 1),
            // the spillway chute (its floor), the runout, the flip bucket's kicker (Lift) and lip
            ("chuteTop", 405, 38.5f, -95, 1), ("", 405, 33, -150, 1), ("chuteMid", 405, 22, -250, 1), ("", 405, 11.5f, -350, 1), ("chuteFoot", 405, 4.5f, -420, 1),
            ("kick", 405, 4.5f, -455, 1), ("lip", 405, 4.5f, -485, 1),
            // over the plunge pool (no road), the landing road, the valley road round to the bridge over the river
            ("gapMid", 405, float.NaN, -520, 7f), ("landing", 405, .4f, -575, 3), ("", 405, .4f, -660, 3), ("bend1", 388, .4f, -730, 3), ("", 320, .4f, -778, 3),
            ("", 200, .4f, -802, 3), ("", 90, .4f, -808, 3), ("bridge", 0, .4f, -808, 3), ("", -100, .4f, -800, 3), ("rejoin", -220, .4f, -778, 3), ("", -320, .4f, -735, 3),
            // north up the west side: the turbine hall, the dam road climbing the canyon's west wall back to the crest
            ("bend2", -385, .4f, -675, 3), ("hallIn", -405, .4f, -610, 3), ("hall", -405, .4f, -540, 3), ("hallOut", -405, .4f, -472, 3), ("climbFoot", -405, .6f, -450, 3),
            ("", -405, 10, -370, 2.4f), ("climb", -405, 21, -275, 1.5f), ("", -405, 31, -180, 1), ("", -403, 38, -100, 1), ("", -390, CrestY, -45, 1), ("", -345, CrestY, -8, 1),
            ("", -295, CrestY, 0, 1),
        };
        // lap 2 only: the flight off the flip bucket (the water throws you up) is higher and lands further on
        static float LineY(int lap, string n, float y) => lap == 2 && n == "gapMid" ? 18f : lap == 2 && n == "landing" ? 12f : y;

        public static float M(string n) => mark[n];
        public static float M(string n, int lap) => mark[lap <= 1 ? n : n + "@" + lap];
        public static int LapOf(float prog) => Mathf.Clamp(1 + Mathf.FloorToInt((prog - mark["F"]) / Mathf.Max(1f, LapLen)), 1, Laps);
        static Vector3 At(float prog) => c.PointAt(prog, out _);
        static Vector3 Dir(float prog) { c.PointAt(prog, out var d); return d; }
        static Vector3 Flat(Vector3 d) { d.y = 0; return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward; }
        static Vector3 Right(float prog) => Vector3.Cross(Vector3.up, Flat(Dir(prog)));
        static Quaternion Facing(float prog, bool flat = true) => Quaternion.LookRotation(flat ? Flat(Dir(prog)) : Dir(prog), Vector3.up);

        public static Course Build(Transform parent)
        {
            mark.Clear(); Jumps.Clear(); dynZones.Clear(); dynAnchors.Clear(); lips.Clear(); canyon = null; nearPts = null; GateLeaves.Clear(); torrent.Clear(); StageThings.Clear();
            var g = new GameObject("Course Dam Shame"); g.transform.SetParent(parent, false); root = g.transform;
            scen = new GameObject("Scenery").transform; scen.SetParent(root, false);
            c = g.AddComponent<Course>(); c.courseName = "Dam Shame"; c.shortName = "Dam Shame"; c.timeTrial = true; c.look = "dam"; c.music = "race_dam";
            c.halfWidth = 70; c.airHalfWidth = 380; c.minY = Floor - 6; c.maxY = 170; c.endMargin = 60; c.startMode = Mode.Drive; c.laps = Laps;
            MakeLine();
            Terrain(); Dam(); Roads(); Chute(); Valley(); Hall(); Reservoir();
            Forest(); SetPieces(); Grandstands();
            Gates(); Zones(); Anchors(); Signs(); Segments(); Shortcuts(); RouteMarks();
            c.marks = new Dictionary<string, float>(mark); c.stunts = new DamStunts();
            var w = g.AddComponent<DamWater>(); w.course = c;
            Debug.Log($"[Course] Dam Shame: line {c.Length:0} m ({Laps} laps of {LapLen:0} m), {c.gates.Count} gates, {c.anchors.Count} anchors, shortcuts {string.Join(", ", c.shortcuts.ConvertAll(s => $"{s.label} {s.Length:0} m"))}");
            return c;
        }

        // ---------------- the line: the lap three times, end to end ----------------
        static float[] roadAt;   // the road's height at each line point (NaN over the plunge pool)
        static void MakeLine()
        {
            var ctl = new List<(string n, Vector3 p, float road)>();
            ctl.Add(("lineStart", L(-335, CrestY + 1, 0), CrestY)); ctl.Add(("start", L(-285, CrestY + 1, 0), CrestY));
            for (int lap = 1; lap <= Laps; lap++)
                foreach (var pt in Loop)
                {
                    string n = pt.n == "" ? "" : lap == 1 ? pt.n : pt.n + "@" + lap;
                    float road = pt.road, y = float.IsNaN(road) ? CrestY : road + pt.lift;
                    if (float.IsNaN(road)) y = pt.lift + 4.5f;   // over the pool: the jump's arc (lap 1, 3) or the flight (lap 2)
                    y = LineY(lap, pt.n, y);
                    ctl.Add((n, L(pt.x, y, pt.z), road));
                }
            ctl.Add(("F@4", L(-250, CrestY + 1, 0), CrestY)); ctl.Add(("lineEnd", L(-185, CrestY + 1, 0), CrestY));
            int n0 = ctl.Count;
            // the road under the pool: from the lip to the landing (only for the interpolation; InGap marks it)
            float[] rd = new float[n0]; for (int i = 0; i < n0; i++) rd[i] = ctl[i].road;
            for (int i = 0; i < n0; i++) if (float.IsNaN(rd[i])) rd[i] = (rd[Mathf.Max(0, i - 1)] + rd[Mathf.Min(n0 - 1, i + 1)]) * .5f;
            var pts = new List<Vector3>(); var roads = new List<float>(); var at = new List<int>();
            Vector3 P(int i) => ctl[Mathf.Clamp(i, 0, n0 - 1)].p; float R(int i) => rd[Mathf.Clamp(i, 0, n0 - 1)];
            for (int i = 0; i < n0 - 1; i++)
            {
                at.Add(pts.Count);
                Vector3 p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
                float r0 = R(i - 1), r1 = R(i), r2 = R(i + 1), r3 = R(i + 2);
                int steps = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(p1, p2) / 3f));
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)steps, t2 = t * t, t3 = t2 * t;
                    pts.Add(.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3));
                    roads.Add(.5f * (2 * r1 + (-r0 + r2) * t + (2 * r0 - 5 * r1 + 4 * r2 - r3) * t2 + (-r0 + 3 * r1 - 3 * r2 + r3) * t3));
                }
            }
            at.Add(pts.Count); pts.Add(P(n0 - 1)); roads.Add(R(n0 - 1));
            c.line = pts; c.Finish(); roadAt = roads.ToArray();
            for (int i = 0; i < n0; i++) if (ctl[i].n != "") mark[ctl[i].n] = c.CumulativeAt(at[i]);
            LapLen = M("F@2") - M("F");
            for (int lap = 1; lap <= Laps; lap++)
                Jumps.Add(new CourseStunts.Jump { name = "Flip Bucket", lip = M("lip", lap), gap = PoolGap, choice = lap == 1 });
            c.lapStarts.Clear(); for (int lap = 1; lap <= Laps + 1; lap++) c.lapStarts.Add(M("F", lap));
        }
        public const float PoolGap = 62f;
        // the road's height at a progress (the kicker included); NaN over the pool
        static int IndexAt(float prog)
        {
            int lo = 0, hi = c.line.Count - 1;
            if (prog <= 0) return 0; if (prog >= c.CumulativeAt(hi)) return hi;
            while (hi - lo > 1) { int mid = (lo + hi) / 2; if (c.CumulativeAt(mid) <= prog) lo = mid; else hi = mid; }
            return lo;
        }
        public static float RoadY(float prog)
        {
            if (InGap(prog)) return float.NaN;
            int i = IndexAt(prog), j = Mathf.Min(i + 1, c.line.Count - 1); float a = c.CumulativeAt(i), b = c.CumulativeAt(j);
            float t = b > a ? Mathf.Clamp01((prog - a) / (b - a)) : 0;
            return Mathf.Lerp(roadAt[i], roadAt[j], t) + Lift(prog);
        }

        // ---------------- race logic ----------------
        static void Gates()
        {
            void Gt(int i, string label, float p, Vector2 half, bool column = true)
            {
                var q = At(p); var d = Dir(p);
                var g = new GameObject("Gate " + label); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(q, Quaternion.LookRotation(Flat(d), Vector3.up));
                var pg = g.AddComponent<ProgressGate>(); pg.index = i; pg.label = label; pg.halfSize = half; pg.modes = null; pg.progress = p; c.gates.Add(pg);   // any form: the water decides the form
                if (i > 0) GateMarker.Make(pg, c, column);
            }
            Gt(0, "G0 start", M("F"), new Vector2(18, 10));
            int k = 1;
            for (int lap = 1; lap <= Laps; lap++)
            {
                Gt(k, $"G{k} spillway", M("chuteMid", lap), new Vector2(ChuteHalf + 2, 12)); k++;
                Gt(k, $"G{k} bridge", M("bridge", lap), new Vector2(26, 22)); k++;   // tall: a plane off the flip bucket on lap 1's line (a straggler) may still be high
                Gt(k, $"G{k} turbine hall", M("hall", lap), new Vector2(16, 8.8f), false); k++;
                Gt(k, $"G{k} dam road", M("climb", lap), new Vector2(20, 12)); k++;
                Gt(k, lap < Laps ? $"G{k} lap {lap + 1}" : $"G{k} finish", M("F", lap + 1), new Vector2(18, 10)); k++;
            }
        }
        public static int GatesPerLap => 5;
        // the first gate index of a lap's own gates (lap 1: G1); the lap's last gate is its lap line
        public static int FirstGate(int lap) => 1 + (lap - 1) * GatesPerLap;
        static T Vol<T>(string name, Vector3 center, Quaternion rot, Vector3 size) where T : BoxVolume
        {
            var g = new GameObject(name); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(center, rot);
            var v = g.AddComponent<T>(); v.size = size; return v;
        }

        // the zones: one physical zone each, its form per water stage (null: off at that stage)
        public static readonly List<(TransformZone z, Mode?[] t)> dynZones = new List<(TransformZone, Mode?[])>();
        public static readonly List<ChoiceLip> lips = new List<ChoiceLip>();
        static ResetZone poolReset;
        public static ResetZone PoolReset => poolReset;
        public static bool InPool(Vector3 p) { var q = p - O; return q.x > PoolX0 && q.x < PoolX1 && q.z < PoolZ0 && q.z > PoolZ1; }
        static void Zones()
        {
            void Z(string label, Vector3 center, Quaternion rot, Vector3 size, Mode? s0, Mode? s1, Mode? s2, bool corridor = false)
            {
                var z = Vol<TransformZone>(label, center, rot, size); z.landingCorridor = corridor; z.label = label; z.target = s0 ?? s1 ?? s2 ?? Mode.Drive;
                dynZones.Add((z, new[] { s0, s1, s2 }));
            }
            float ct = M("chuteTop") + 4; var cq = At(ct); cq.y = RoadY(ct) + 4;
            Z("Hull down (the spillway runs)", cq, Facing(ct), new Vector3(ChuteHalf * 2 + 4, 12, 14), null, Mode.Boat, Mode.Boat);
            float lp = M("lip"); var lq = At(lp); lq.y = RoadY(lp - .5f) + 6;
            Z("Wings out (the flip bucket throws you up)", lq + Flat(Dir(lp)) * 3, Facing(lp), new Vector3(ChuteHalf * 2 + 6, 14, 10), null, Mode.Fly, null);
            // the valley landing: a landing corridor (a plane comes down onto the water here)
            var vq = L(405, 18, -625);
            Z("Hull down (the valley)", vq + Vector3.up * 16, Quaternion.identity, new Vector3(110, 80, 150), null, Mode.Boat, Mode.Boat, true);
            float b1 = M("bend1"); var bq = At(b1); bq.y = 8;
            Z("Canopy seals (the drowned valley)", bq, Facing(b1), new Vector3(70, 22, 14), null, null, Mode.Dive);
            float b2 = M("bend2"); var b2q = At(b2); b2q.y = 9;   // a boat the flood caught past the first one (on the surface it would hit the hall's roof)
            Z("Canopy seals (before the turbine hall)", b2q, Facing(b2), new Vector3(80, 18, 14), null, null, Mode.Dive);
            Z("Surface (the dam road)", L(-405, 9, -388), Quaternion.identity, new Vector3(60, 16, 14), null, null, Mode.Boat);
            Z("Wheels down (the dam road)", L(-405, 3, -444), Quaternion.identity, new Vector3(60, 9, 12), null, Mode.Drive, null);
            Z("Wheels down (the flood line)", L(-405, 16.5f, -322), Quaternion.identity, new Vector3(60, 12, 12), null, null, Mode.Drive);
            // the choice jump's lip (lap 1's water only): LB on the ramp for the Canyon flight
            var cl = Vol<ChoiceLip>("Flip bucket lip (choice)", new Vector3(lq.x, RoadY(lp - .5f) + 3, lq.z), Facing(lp), new Vector3(ChuteHalf * 2, 8, 6)); cl.window = 1.5f; cl.ramp = KickerLen; lips.Add(cl);
            // the plunge pool under the jump: a car that drops in is reset at once (lap 1's water; later it is under the flight / the flood)
            poolReset = Vol<ResetZone>("Plunge pool", L((PoolX0 + PoolX1) * .5f, -3, (PoolZ0 + PoolZ1) * .5f), Quaternion.identity, new Vector3(PoolX1 - PoolX0, 10, PoolZ0 - PoolZ1 - 2)); poolReset.label = "fell into the plunge pool";
        }

        // the anchors: dry (always a car), the chute (car, then a boat once it runs), the valley (car, boat or sub by the water's depth)
        public enum Wet { Dry, Chute, Valley }
        public static readonly List<(RecoveryAnchor a, Wet w, float road, Vector3 at)> dynAnchors = new List<(RecoveryAnchor, Wet, float, Vector3)>();
        static void Anchors()
        {
            for (int lap = 1; lap <= Laps; lap++)
            {
                float Lp(string n) => M(n, lap);
                void A(float p, Wet w, float speed = -1)
                {
                    var q = At(p); float road = RoadY(p); if (float.IsNaN(road)) return;
                    var g = new GameObject($"Anchor {w} {p:0}"); g.transform.SetParent(root, false); var pos = new Vector3(q.x, road + 1f, q.z);
                    g.transform.SetPositionAndRotation(pos, Facing(p)); var a = g.AddComponent<RecoveryAnchor>(); a.mode = Mode.Drive; a.progress = p; a.speed = speed; c.anchors.Add(a);
                    dynAnchors.Add((a, w, road, pos));
                }
                if (lap == 1) A(M("start"), Wet.Dry);
                A(Lp("F") + 40, Wet.Dry); A(Lp("tower") + 20, Wet.Dry); A(Lp("crestE"), Wet.Dry); A(Lp("chuteTop") - 25, Wet.Dry);
                A(Lp("chuteTop") + 40, Wet.Chute); A(Lp("chuteMid"), Wet.Chute); A(Lp("chuteMid") + 90, Wet.Chute); A(Lp("kick") - 60, Wet.Chute, 45);
                A(Lp("landing") + 40, Wet.Valley); A(Lp("bend1"), Wet.Valley); A(Lp("bend1") + 120, Wet.Valley); A(Lp("bridge") - 60, Wet.Valley); A(Lp("bridge") + 40, Wet.Valley);
                A(Lp("rejoin") - 25, Wet.Valley); A(Lp("rejoin") + 60, Wet.Valley); A(Lp("bend2"), Wet.Valley); A(Lp("hallIn") - 15, Wet.Valley); A(Lp("hall"), Wet.Valley); A(Lp("hallOut") + 4, Wet.Valley);
                A(Lp("climbFoot") + 40, Wet.Valley); A(Lp("climbFoot") + 110, Wet.Valley); A(Lp("climb") + 30, Wet.Dry); A(Lp("climb") + 140, Wet.Dry); A(M("F", lap + 1) - 40, Wet.Dry);
            }
            c.Finish();
        }
        // an anchor's form and place for the water as it is now (DamWater.Apply)
        public static (Mode m, Vector3 p) AnchorFor(Wet w, float road, Vector3 at, int stage)
        {
            float L0 = ValleyLevel[stage];
            if (w == Wet.Dry) return (Mode.Drive, at);
            if (w == Wet.Chute)
            {
                if (stage == 0) return (Mode.Drive, at);
                if (L0 > road + .5f) return (Mode.Boat, new Vector3(at.x, L0 + .4f, at.z));   // the flooded foot of the chute
                return (Mode.Boat, new Vector3(at.x, road + .9f, at.z));                      // on the torrent
            }
            if (L0 - road > 8f) return (Mode.Dive, new Vector3(at.x, Mathf.Min(road + 3.5f, L0 - 4f), at.z));
            if (L0 > road + .5f) return (Mode.Boat, new Vector3(at.x, L0 + .4f, at.z));
            return (Mode.Drive, at);
        }

        static void Segments()
        {
            void Sg(string name, string form, float target, float end, int gates) => c.segments.Add(new Course.Segment { name = name, form = form, target = target, endProgress = end, gatesNeeded = gates });
            // one segment per lap: the splits are the lap times (targets: the check pilot's laps, rounded up)
            Sg("Lap 1: Dry run", "Driving", 44, M("F", 2), FirstGate(2));
            Sg("Lap 2: A smidge", "Boating", 48, M("F", 3), FirstGate(3));
            Sg("Lap 3: The full show", "Diving", 56, M("F", 4), FirstGate(3) + GatesPerLap);
        }

        // ---------------- the choice: the Canyon flight (lap 1, LB at the flip bucket) ----------------
        static Shortcut canyon;
        public static Shortcut Canyon => canyon;
        static readonly Color ShortcutColor = new Color(.75f, .4f, 1f, .9f);
        static void Shortcuts()
        {
            // off the flip bucket with the wings out: south over the plunge pool's spray, a long curve out over the river and the beavers' dam,
            // back to the valley road west of the bridge (stands in for the bridge gate); every bend >= 300 m; Round 12's rules (entered as the
            // wings open, big frames, the exit takes a car, a reset on it goes forward to its rejoin)
            float lip = M("lip"), rejoin = M("rejoin"); var a = At(lip); a.y = RoadY(lip - .5f) + 1.5f;
            var ctrl = new List<Vector3> { a, L(407, 13, -560), L(404, 21, -660), L(372, 27, -760), L(300, 30, -845), L(190, 30, -900), L(65, 27, -918), L(-55, 22, -900), L(-150, 15, -855), L(-205, 9, -805), At(rejoin) + Vector3.up * 5, At(rejoin + 16) + Vector3.up * 4 };
            canyon = MakeShortcut("Canyon flight", ctrl, lip, rejoin + 16, FirstGate(1) + 1, FirstGate(1) + 1, 55f, new Vector2(30, 20), new Vector2(34, 22), 12f, 34f, Mode.Fly);
            canyon.choice = true; canyon.flight = true; canyon.exit.modes = new[] { Mode.Fly, Mode.Drive, Mode.Boat };
            // wheels down before the rejoin (lap 1's water: a car; a plane caught by the flood later: a boat)
            var b = At(rejoin - 30); b.y = 18;
            var z = Vol<TransformZone>("Wheels down (the Canyon flight)", b, Facing(rejoin - 30), new Vector3(80, 40, 30)); z.label = z.name; z.target = Mode.Drive;
            dynZones.Add((z, new Mode?[] { Mode.Drive, Mode.Boat, null }));   // lap 3: off (a sub on the drowned road stays a sub)
            // its rings: gold hoops along the flight (visual; the sub-gates count)
            var rings = new MeshKit.Batch(); var gold = new Color(1f, .82f, .3f, .7f); const float RR = 15f;
            foreach (float frac in new[] { .22f, .42f, .62f, .8f })
            {
                float s = canyon.Length * frac; var q = canyon.PointAt(s, out var d); var frame = Quaternion.LookRotation(d, Vector3.up);
                for (int i = 0; i < 16; i++) { float an = i * 360f / 16; rings.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(an * Mathf.Deg2Rad), Mathf.Sin(an * Mathf.Deg2Rad), 0) * RR), frame * Quaternion.Euler(0, 0, an), new Vector3(.9f, RR * .42f, .9f), gold); }
            }
            rings.Object(scen, "Canyon flight rings", GateMarker.Beacon, false, false);
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

        // ---------------- what the AI and the HUD need ----------------
        public static readonly List<CourseStunts.Jump> Jumps = new List<CourseStunts.Jump>();
        public static float Lift(float p)
        {
            foreach (var j in Jumps) if (p > j.lip - KickerLen && p <= j.lip + .01f) { float u = (p - (j.lip - KickerLen)) / KickerLen; return KickerH * u * u; }
            return 0;
        }
        public static bool InGap(float p) { foreach (var j in Jumps) if (p > j.lip && p < j.lip + j.gap) return true; return false; }
        static bool Near(float p, string n, float d) { for (int lap = 1; lap <= Laps; lap++) if (Mathf.Abs(p - M(n, lap)) < d) return true; return false; }
        sealed class DamStunts : CourseStunts
        {
            public override IReadOnlyList<Jump> Jumps => DamShame.Jumps;
            public override bool Narrow(float p, Mode mode)
            {
                foreach (var j in DamShame.Jumps) if (p > j.lip - KickerLen - 40 && p < j.lip + j.gap + 40) return true;
                foreach (var k in new[] { "chuteTop", "bridge", "hallIn", "hall", "hallOut", "F", "climbFoot" }) if (Near(p, k, 45)) return true;
                return false;
            }
            public override string Where(float p)
            {
                int lap = LapOf(p);
                return p < M("chuteTop", lap) ? "the dam crest" : p < M("lip", lap) + 40 ? "the spillway" : p < M("bridge", lap) + 60 ? "the valley" : p < M("hallOut", lap) + 20 ? "the turbine hall" : "the dam road";
            }
        }
    }
}
