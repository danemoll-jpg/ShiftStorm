using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 7 items 1-2: the Beach Promenade is now a stunt strip and the Cliffside Climb has a gap.
    //   Creek Jump (promenade, plain car jump): a stone kicker over a creek to the lake, wide landing straight, no wings.
    //   The Loop (promenade): a mossy stone / wooden loop-the-loop, radius 24 m to the car's centre (52 m tall), the exit lane 16 m to the right
    //     of the entry lane (a corkscrew shift, so the lanes never meet); a boost pad 70 m before it. The racer line itself runs round the
    //     loop (inserted control points), so progress, gates and the route strip simply count along it; a car on it rides a rail
    //     (ShiftVehicle.Stunts: speed along the track, real gravity along it, grip check; too slow: it slides back and is reset at the
    //     loop's anchor).
    //   Lake Jump (promenade, the choice jump): off the lip the car may open its wings (LB / E, the first 1 s of the airtime; HUD "LB: FLY");
    //     the plane takes a short ring branch out over the lake (a Shortcut with its own entry / exit sub-gates) and lands as a car again in
    //     the marked wheels-down zone before G1; otherwise the car lands on the long straight across the creek.
    //   Cliff Gap (Cliffside Climb, plain car jump): a gap in the climb road over a ravine with a stream; a fall into it resets at once.
    // Every gap: water (the promenade's creeks: the usual car-in-water reset) or a ResetZone (the ravine), anchors before each with a
    // higher reset speed so a reset car always clears the next gap.
    public static partial class ParadisePlunge
    {
        // the loop: entry point (car centre), forward, right, radius (car centre), side shift over the whole turn
        public const float LoopR = 24f, LoopShift = 16f, LoopTrackHalf = 7f, BoostPadBefore = 70f, BoostPadSpeed = 72f;
        static readonly Vector3 LoopF = Vector3.forward, LoopRt = Vector3.right;
        static Vector3 LoopE => W(0, 2.9f, 317.6f);
        public static Vector3 LoopPos(float th) => LoopRail.Pos(LoopE, LoopF, LoopRt, LoopR, LoopShift, th);
        const int LoopSteps = 36;   // control points round the loop (every 10 deg)

        // the jumps: lip progress, kicker length / height, gap length
        public struct Jump { public string name; public float lip, kicker, height, gap; public bool choice, water; }
        public static readonly List<Jump> Jumps = new List<Jump>();
        static void PlanJumps()
        {
            Jumps.Clear();
            Jumps.Add(new Jump { name = "Creek Jump", lip = M("jA"), kicker = 45, height = 3.2f, gap = 40, water = true });
            Jumps.Add(new Jump { name = "Lake Jump", lip = M("jC"), kicker = 45, height = 3.2f, gap = 40, water = true, choice = true });
            Jumps.Add(new Jump { name = "Cliff Gap", lip = M("climbMid") - 80, kicker = 45, height = 3.2f, gap = 36 });
        }
        // the kicker's rise above the line's road height (an ease-in: steepest at the lip, about 8 deg)
        public static float Lift(float p)
        {
            foreach (var j in Jumps) if (p > j.lip - j.kicker && p <= j.lip + .01f) { float u = (p - (j.lip - j.kicker)) / j.kicker; return j.height * u * u; }
            return 0;
        }
        public static bool InGap(float p) { foreach (var j in Jumps) if (p > j.lip && p < j.lip + j.gap) return true; return false; }
        public static bool InLoop(float p) => mark.Count > 0 && p > M("loopIn") - .5f && p < M("loopOut") + .5f;
        static bool OffSurface(float p) => InGap(p) || InLoop(p);
        static float RoadTop(float p) => At(p).y - .9f + Lift(p);
        // the AI plans no braking for these (the loop's line turns over itself, the kickers are straight)
        public static bool NoBrake(float from, float to) => mark.ContainsKey("loopIn") && to > M("loopIn") - 40 && from < M("loopOut") + 5;
        public static bool StuntNear(float p) => mark.ContainsKey("loopIn") && ((p > M("loopIn") - 160 && p < M("loopOut") + 40) || NearLip(p));
        // the AI's aim on the way into the loop: straight on along the entry lane (not up into the loop's own line)
        public static bool LoopAim(float p, float ahead, out Vector3 aim)
        {
            aim = default; if (!mark.ContainsKey("loopIn")) return false; float a = M("loopIn");
            if (p < a && p + ahead > a) { aim = At(a) + LoopF * (p + ahead - a); return true; }
            return false;
        }
        static bool NearLip(float p) { foreach (var j in Jumps) if (p > j.lip - j.kicker - 20 && p < j.lip + j.gap + 30) return true; return false; }

        // road width: 13 m half on the promenade, narrowing to the loop's 7 m before it and widening after
        static float LoopNarrow(float p, float hw)
        {
            if (!mark.ContainsKey("loopIn")) return hw;
            float a = M("loopIn"), b = M("loopOut");
            if (p > a - 110 && p <= a + 1) return Mathf.Lerp(hw, LoopTrackHalf, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a - 110, a - 30, p)));
            if (p >= b - 1 && p < b + 100) return Mathf.Lerp(LoopTrackHalf, hw, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(b + 20, b + 100, p)));
            return hw;
        }

        // the racer line's control points in metres, with the loop inserted after "loopIn"
        static List<(string n, Vector3 p)> Controls()
        {
            var list = new List<(string, Vector3)>();
            foreach (var pt in Points)
            {
                if (pt.n == "loopOut") continue;   // written by the loop itself
                list.Add((pt.n, W(pt.x, pt.y, pt.z)));
                if (pt.n == "loopIn")
                    for (int i = 1; i <= LoopSteps; i++)
                    {
                        float th = i * Mathf.PI * 2 / LoopSteps;
                        list.Add((i == LoopSteps ? "loopOut" : i == LoopSteps / 2 ? "loopTop" : i == LoopSteps / 4 ? "loopUp" : i == LoopSteps * 3 / 4 ? "loopDown" : "", LoopPos(th)));
                    }
            }
            return list;
        }

        // ---------------- build ----------------
        static void Stunts()
        {
            BuildLoop(); foreach (var j in Jumps) BuildJump(j);
        }

        static void BuildLoop()
        {
            var g = new GameObject("The Loop"); g.transform.SetParent(root, false);
            var rail = g.AddComponent<LoopRail>(); rail.E = LoopE; rail.F = LoopF; rail.Rt = LoopRt; rail.R = LoopR; rail.shift = LoopShift; rail.half = LoopTrackHalf - 1.5f;
            rail.entryProgress = M("loopIn"); rail.exitProgress = M("loopOut");
            // the track: mossy stone deck with wooden planks, low stone kerbs and glowing edge lines, built from short pieces round the rail
            var deck = new MeshKit.Batch(); var kerb = new MeshKit.Batch(); var glow = new MeshKit.Batch();
            int n = 96; var plank = new Color(.62f, .44f, .26f); var plankDark = new Color(.5f, .35f, .2f);
            for (int i = 0; i < n; i++)
            {
                float t0 = i * Mathf.PI * 2 / n, t1 = (i + 1) * Mathf.PI * 2 / n, tm = (t0 + t1) * .5f;
                Vector3 a = rail.Pos(t0), b = rail.Pos(t1), m = rail.Pos(tm); var tan = (b - a); float len = tan.magnitude + .15f; tan /= Mathf.Max(.01f, tan.magnitude);
                var nrm = rail.Normal(tm); var rot = Quaternion.LookRotation(tan, nrm);
                var surf = m - nrm * .9f;   // the track surface under the car's centre
                deck.Add(MeshKit.Cube, surf - nrm * .6f, rot, new Vector3(LoopTrackHalf * 2, 1.2f, len), i % 2 == 0 ? plank : plankDark);
                deck.Add(MeshKit.Cube, surf - nrm * 1.6f, rot, new Vector3(LoopTrackHalf * 2 + 1.2f, 1f, len), JurassicLook.Stone);   // stone underside
                foreach (float s in new[] { -1f, 1f })
                {
                    kerb.Add(MeshKit.Cube, surf + LoopRt * s * (LoopTrackHalf + .3f) + nrm * .45f, rot, new Vector3(.8f, 1.1f, len), i % 6 < 3 ? JurassicLook.Stone : JurassicLook.Moss);
                    glow.Add(MeshKit.Cube, surf + LoopRt * s * (LoopTrackHalf - .6f) + nrm * .04f, rot, new Vector3(.45f, .08f, len), JurassicLook.Crystal);
                }
            }
            deck.Object(g.transform, "Loop deck", JurassicLook.VertexColored(true));
            kerb.Object(g.transform, "Loop kerbs", JurassicLook.VertexColored(true));
            glow.Object(g.transform, "Loop edge lights", GateMarker.Beacon, false, false);
            // two mossy stone towers holding it (outside the track, front and back), vines on them
            var rock = Mt(JurassicLook.Rock, true, default, 1.2f); var moss = Mt(JurassicLook.Moss, true, default, 1.2f);
            foreach (float s in new[] { -1f, 1f })
            {
                var at = LoopE + LoopF * s * (LoopR + 6f) + LoopRt * LoopShift * .5f; at.y = RoadY;
                Box(scen, "Loop tower", at + Vector3.up * 14, new Vector3(10, 28, 8), rock, null, false);
                Box(scen, "Loop tower moss", at + Vector3.up * 28.6f, new Vector3(11, 1.6f, 9), moss, null, false);
            }
            // a carved arch over the entry: "THE LOOP" in amber
            var arch = LoopE + LoopRt * 0 - LoopF * 60; arch.y = RoadY;
            foreach (float s in new[] { -1f, 1f }) Box(scen, "Loop arch post", arch + LoopRt * s * 9.5f + Vector3.up * 5, new Vector3(1.2f, 10, 1.2f), Mt(JurassicLook.FernTrunk, true), null, false);
            Box(scen, "Loop arch board", arch + Vector3.up * 10.4f, new Vector3(20.5f, 2f, .6f), Mt(JurassicLook.Amber, true), null, false);

            // the boost pad: glowing chevrons on the road, a volume that tops a car up to BoostPadSpeed
            float bp = M("loopIn") - BoostPadBefore; var bq = At(bp); bq.y = RoadY;
            var pad = Vol<BoostPad>("Loop boost pad", bq + Vector3.up * 1.5f, Facing(bp), new Vector3(LoopTrackHalf * 2 + 4, 4, 20)); pad.speed = BoostPadSpeed;
            var chev = new MeshKit.Batch();
            for (int i = 0; i < 4; i++) foreach (float s in new[] { -1f, 1f })
                    chev.Add(MeshKit.Cube, bq + LoopF * (-7 + i * 4.5f) + LoopRt * s * 1.8f + Vector3.up * .06f, Quaternion.Euler(0, -s * 40, 0), new Vector3(1.1f, .06f, 5.2f), new Color(.3f, 1f, .95f));
            chev.Object(scen, "Boost pad chevrons", GateMarker.Beacon, false, false);
        }

        static void BuildJump(Jump j)
        {
            var g = new GameObject(j.name); g.transform.SetParent(root, false);
            float lip = j.lip, far = j.lip + j.gap; var lq = At(lip); var fq = At(far); var mid = (lq + fq) * .5f; var f = Facing(lip); var r = Right(lip);
            var rock = Mt(JurassicLook.Rock, true); var moss = Mt(JurassicLook.Moss, true); var stone = Mt(JurassicLook.Stone, true, default, .8f);
            // the kicker's lip: a stone sill with an amber edge, and amber chevrons painted up the kicker
            Box(g.transform, "Lip edge", new Vector3(lq.x, RoadTop(lip) + .04f, lq.z) - Flat(Dir(lip)) * .6f, new Vector3(26, .08f, 1.2f), Mt(JurassicLook.Amber, false, JurassicLook.Amber * .6f), f, false);
            var chev = new MeshKit.Batch();
            for (float p = lip - j.kicker + 6; p < lip - 4; p += 9) { var q = At(p); q.y = RoadTop(p) + .05f; foreach (float s in new[] { -1f, 1f }) chev.Add(MeshKit.Cube, q + Right(p) * s * 2f, Facing(p) * Quaternion.Euler(-Mathf.Atan(2 * j.height * Mathf.InverseLerp(lip - j.kicker, lip, p) / j.kicker) * Mathf.Rad2Deg, -s * 38, 0), new Vector3(1f, .06f, 5f), JurassicLook.Amber); }
            chev.Object(g.transform, "Kicker chevrons", JurassicLook.VertexColored(false, new Color(.3f, .18f, .02f)), false, false);
            // the landing side: a broad stone apron with a chequered edge (reads from the air)
            for (int i = 0; i < 9; i++) Box(g.transform, "Landing check", new Vector3(fq.x, RoadTop(far + 1) + .04f, fq.z) + r * (-12 + i * 3) + Flat(Dir(far)) * 1.2f, new Vector3(1.5f, .06f, 1.5f), Mt(i % 2 == 0 ? Color.white : new Color(.1f, .1f, .1f)), f, false);
            if (j.water)
            {
                // the creek: water under the gap out to the lake (the usual car-in-water reset) and its visible surface
                var wc = mid; wc.y = -4;
                Vol<WaterVolume>(j.name + " creek", wc, f, new Vector3(260, 8, j.gap + 2));
                var sea = GameObject.CreatePrimitive(PrimitiveType.Quad); sea.name = "creek water"; sea.transform.SetParent(g.transform, false); Object.Destroy(sea.GetComponent<Collider>());
                sea.transform.SetPositionAndRotation(new Vector3(mid.x, Water + .02f, mid.z), f * Quaternion.Euler(90, 0, 0)); sea.transform.localScale = new Vector3(260, j.gap + 6, 1);
                var sr = sea.GetComponent<MeshRenderer>(); sr.sharedMaterial = JurassicLook.WaterMat(JurassicLook.Water, JurassicLook.WaterLight, JurassicLook.WaterRim, .05f); sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // mossy banks either side of the creek (faces under the road ends)
                foreach (var (p, s) in new[] { (lip, -1f), (far, 1f) })
                {
                    var q = At(p); q.y = 0;
                    Box(g.transform, "creek bank", q + Flat(Dir(p)) * s * -1.5f + Vector3.up * .4f, new Vector3(120, 2.4f, 3), rock, f, false);
                    Box(g.transform, "creek bank moss", q + Flat(Dir(p)) * s * -1.5f + Vector3.up * 1.7f, new Vector3(120, .4f, 3.4f), moss, f, false);
                }
            }
            else
            {
                // the ravine: rock walls down to the valley floor, a stream at the bottom, a reset just under the road
                foreach (var (p, s) in new[] { (lip, -1f), (far, 1f) })
                {
                    var q = At(p); float top = RoadTop(p);
                    Box(g.transform, "ravine wall", new Vector3(q.x, top * .5f - 1, q.z) + Flat(Dir(p)) * s * -2f, new Vector3(34, top + 2, 4), rock, f, false);
                }
                var rz = mid; rz.y = (RoadTop(lip) - 3 + -2) * .5f;
                Vol<ResetZone>(j.name + " fall", rz, f, new Vector3(60, RoadTop(lip) - 3 + 2, j.gap - 2)).label = "fell into the " + j.name;
                var stream = GameObject.CreatePrimitive(PrimitiveType.Quad); stream.name = "ravine stream"; stream.transform.SetParent(g.transform, false); Object.Destroy(stream.GetComponent<Collider>());
                stream.transform.SetPositionAndRotation(new Vector3(mid.x, .5f, mid.z), f * Quaternion.Euler(90, 0, 0)); stream.transform.localScale = new Vector3(70, j.gap - 6, 1);
                stream.GetComponent<MeshRenderer>().sharedMaterial = JurassicLook.WaterMat(JurassicLook.Water, JurassicLook.WaterLight, JurassicLook.WaterRim, .05f);
                // a little waterfall down the hillside into the ravine (looks only)
                var fall = GameObject.CreatePrimitive(PrimitiveType.Quad); fall.name = "ravine falls"; fall.transform.SetParent(g.transform, false); Object.Destroy(fall.GetComponent<Collider>());
                fall.transform.SetPositionAndRotation(mid - r * 18 + Vector3.up * (RoadTop(lip) * .5f + 3), Quaternion.LookRotation(r)); fall.transform.localScale = new Vector3(j.gap - 10, RoadTop(lip) + 10, 1);
                fall.GetComponent<MeshRenderer>().sharedMaterial = JurassicLook.FallMat(.5f);
            }
            if (j.choice)
            {
                // the wings window: crossing the lip as a car opens it (ShiftVehicle.Stunts); a winged sign beside the kicker
                var lipVol = Vol<ChoiceLip>(j.name + " lip", new Vector3(lq.x, RoadTop(lip) + 3, lq.z), f, new Vector3(30, 8, 6)); lipVol.window = 1f;
                var q = At(lip - 70); var h = new GameObject("Sign wings optional"); h.transform.SetParent(scen, false);
                h.transform.SetPositionAndRotation(new Vector3(q.x, RoadY, q.z) + Right(lip - 70) * 15.5f, Facing(lip - 70)); WorldBuilder.Sign(h.transform, Vector3.zero, Mode.Fly, 5);
            }
        }

        // the choice jump's flight branch: out over the lake through three rings and back to the road before G1 (Shortcut "Lake flight")
        static void LakeFlight()
        {
            float lip = M("jC"), rejoin = M("G1") - 75; var a = At(lip); var b = At(rejoin);
            var up = Vector3.up; float y0 = RoadY;
            // a climb over the lake-side beach and back (first runs: a 95 m swing out over the lake needed turns tighter than the plane's
            // ~170 m turning circle at 80 m/s; climbing and diving it does quickly): never more than 20 m to the side, up to 26 m up
            Vector3 L(float p, float side, float h) => At(p) - Right(p) * side + up * h;
            var ctrl = new List<Vector3> { a + up * 1.5f, L(lip + 40, 0, 8), L(lip + 90, 7, 16), L(lip + 150, 16, 24), L(lip + 205, 20, 26), L(lip + 255, 15, 21),
                                           L(rejoin - 45, 5, 10), b + up * 6f, At(rejoin + 12) + up * 5f };
            for (int i = 0; i < ctrl.Count; i++) { var p = ctrl[i]; p.y = Mathf.Max(p.y, y0 + 2); ctrl[i] = p; }
            var sc = MakeShortcut("Lake flight", ctrl, lip, rejoin + 12, 1, -1, 45f, new Vector2(16, 12), new Vector2(18, 12), 70f, 30f, Mode.Fly);
            sc.choice = true;
            // three gold rings to thread (visual: the sub-gates count), a buoy at each foot so they read against the lake
            var rings = new MeshKit.Batch(); var gold = new Color(1f, .82f, .3f, .7f);
            foreach (float frac in new[] { .32f, .5f, .68f })
            {
                float s = sc.Length * frac; var q = sc.PointAt(s, out var d); var frame = Quaternion.LookRotation(d, up); const float R = 12f;
                for (int i = 0; i < 16; i++) { float an = i * 360f / 16; rings.Add(MeshKit.Cube, q + frame * (new Vector3(Mathf.Cos(an * Mathf.Deg2Rad), Mathf.Sin(an * Mathf.Deg2Rad), 0) * R), frame * Quaternion.Euler(0, 0, an), new Vector3(.9f, R * .42f, .9f), gold); }
            }
            rings.Object(scen, "Lake flight rings", GateMarker.Beacon, false, false);
            // wheels down: the marked zone on the road where the plane turns back into a car (cars pass through it unchanged)
            var z = Vol<TransformZone>("Wheels down (lake flight)", b + up * 13, Facing(rejoin), new Vector3(40, 30, 16)); z.target = Mode.Drive; z.label = "Wheels down (lake flight)";
            for (int i = 0; i < 4; i++) Box(root, "wheels-down chevron", new Vector3(b.x, RoadY + .04f, b.z) + Flat(Dir(rejoin)) * (-10 + i * 6), new Vector3(10, .05f, 1.2f), Mt(new Color(.95f, .5f, .1f)), Facing(rejoin) * Quaternion.Euler(0, i % 2 == 0 ? 20 : -20, 0), false);
            var hs = new GameObject("Sign wheels down"); hs.transform.SetParent(scen, false); hs.transform.SetPositionAndRotation(new Vector3(b.x, RoadY, b.z) - Right(rejoin) * 15.5f - Flat(Dir(rejoin)) * 40, Facing(rejoin)); WorldBuilder.Sign(hs.transform, Vector3.zero, Mode.Drive, 6);
        }
    }

    // ---------------- volumes ----------------
    // Round 7: the loop's rail (see ParadisePlunge.Stunts): pos(th) = E + F R sin th + up R (1 - cos th) + right shift smoothstep(th / 2 pi)
    public sealed class LoopRail : MonoBehaviour
    {
        public static readonly List<LoopRail> All = new List<LoopRail>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public Vector3 E, F = Vector3.forward, Rt = Vector3.right; public float R = 24, shift = 16, half = 5.5f, entryProgress, exitProgress;
        public static Vector3 Pos(Vector3 e, Vector3 f, Vector3 rt, float r, float shift, float th)
        {
            float u = Mathf.Clamp01(th / (Mathf.PI * 2));
            return e + f * (r * Mathf.Sin(th)) + Vector3.up * (r * (1 - Mathf.Cos(th))) + rt * (shift * u * u * (3 - 2 * u));
        }
        public Vector3 Pos(float th) => Pos(E, F, Rt, R, shift, th);
        public Vector3 Deriv(float th)
        {
            float u = Mathf.Clamp01(th / (Mathf.PI * 2));
            return F * (R * Mathf.Cos(th)) + Vector3.up * (R * Mathf.Sin(th)) + Rt * (shift * 6 * u * (1 - u) / (Mathf.PI * 2));
        }
        public Vector3 Tangent(float th) => Deriv(th).normalized;
        // towards the loop's centre (the car's up while on it), square to the tangent
        public Vector3 Normal(float th) { var n = -F * Mathf.Sin(th) + Vector3.up * Mathf.Cos(th); var t = Tangent(th); return (n - t * Vector3.Dot(n, t)).normalized; }
        // forward through the entry plane at E, on the track
        public bool Entered(Vector3 from, Vector3 to, out float lateral)
        {
            lateral = 0; float a = Vector3.Dot(from - E, F), b = Vector3.Dot(to - E, F);
            if (!(a < 0 && b >= 0)) return false;
            var hit = Vector3.Lerp(from, to, -a / (b - a)); lateral = Vector3.Dot(hit - E, Rt);
            return Mathf.Abs(lateral) < half + 3f && Mathf.Abs(hit.y - E.y) < 4f;
        }
    }
    // a strip that tops a car's speed up to `speed` along its travel (once per pass)
    public sealed class BoostPad : BoxVolume
    {
        public static readonly List<BoostPad> All = new List<BoostPad>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public float speed = 72;
        protected override Color GizmoColor => Color.green;
    }
    // inside it: an immediate reset (the ravine under the Cliff Gap)
    public sealed class ResetZone : BoxVolume
    {
        public static readonly List<ResetZone> All = new List<ResetZone>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public string label = "fell";
        protected override Color GizmoColor => Color.red;
    }
    // the choice jump's lip: a car crossing it may open its wings (LB / E) for `window` seconds after
    public sealed class ChoiceLip : BoxVolume
    {
        public static readonly List<ChoiceLip> All = new List<ChoiceLip>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public float window = 1f;
        protected override Color GizmoColor => Color.magenta;
    }
}
