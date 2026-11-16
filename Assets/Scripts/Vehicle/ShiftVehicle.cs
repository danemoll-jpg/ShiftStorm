using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // The one racing framework (CLAUDE.md "one racing framework, four handling modes"). One Rigidbody and one hull collider for every form, so a
    // half-open wing can never catch a gate (the wings, wheels and fins are visual only). Position, forward intent (heading), boost and race
    // progress live here and survive every change; each change hands the physics to the new mode at one known point, the commit (t = 0 of the
    // 0.5-0.8 s visual change), with the speed blended into the receiving mode's range, and the player's control ramping from startAuthority to
    // full by the end of the visual change (never locked). Handling per mode: ShiftVehicle.Drive / .Boat / .Fly / .Dive (.cs).
    [RequireComponent(typeof(Rigidbody), typeof(ShiftInput))]
    public sealed partial class ShiftVehicle : MonoBehaviour
    {
        public const int VehicleLayer = 8, GhostLayer = 9, ObstacleLayer = 10;
        public static ShiftVehicle Player { get; private set; }
        public bool IsPlayer => Player == this;
        public bool Human;   // Round 6: driven by a person (player 1, or player 2 in split screen): never by RivalPilot, has the speed streaks
        public string RacerName = "Abi";                   // Round 3: who drives it (the HUD's standings, results)
        public Color Tint = new Color(1f, .78f, .15f);     // its colour on the route strip and the standings (cosmetic only)
        public static void MarkPlayer(ShiftVehicle v) => Player = v;
        public static int GroundMask => Physics.DefaultRaycastLayers & ~(1 << VehicleLayer) & ~(1 << GhostLayer);

        public ShiftTuning shared; public DriveTuning drive; public BoatTuning boat; public FlyTuning fly; public DiveTuning dive;
        public Rigidbody Body { get; private set; }
        public ShiftInput Input { get; private set; }
        public FormSet Forms { get; private set; }
        public Course Course { get; set; }

        public Mode Mode { get; private set; }
        public float Boost { get; private set; } = 1;
        public bool Boosting { get; private set; }
        public float Authority { get; private set; } = 1;
        public bool Changing { get; private set; }
        public Mode ChangeFrom { get; private set; }
        public float ChangeStartedAt { get; private set; } = -1;
        public float Speed => Body ? Body.linearVelocity.magnitude : 0;
        public float HeadingDeg { get; private set; }   // the forward intent: kept through every change
        public int Changes { get; private set; }
        public float LastChangeSeconds { get; private set; }
        public TransformZone LastZone { get; private set; }

        // Race progress (gates in order, best progress along the line), kept across changes and resets.
        public int GatesPassed { get; private set; }
        public float BestProgress { get; private set; }
        public float Progress { get; private set; }
        public bool Finished { get; private set; }
        public float StartTime { get; private set; }
        public float FinishTime { get; private set; } = -1;
        public int Resets { get; private set; }
        public float GraceUntil { get; private set; } = -1;
        public float HoldUntil { get; private set; } = -1;          // a time trial's countdown: the vehicle waits on the grid until GO
        public bool Holding => held;
        public float RaceDistance => Course ? Course.RaceDistance(GatesPassed, Progress) : 0;
        public readonly List<float> Splits = new List<float>();     // time from GO at the end of each course segment (Course.segments)
        public ProgressGate MissedGate { get; private set; }         // the next gate when the racer has gone past it without earning it
        public string Cue { get; private set; } = "";                // a direction cue for the HUD ("WRONG WAY", "THIS WAY", "TURN BACK ...")
        public float CueUntil { get; private set; } = -1;
        bool held; float missedFor;

        // Everything that happened, in order, for checks and the HUD's last line.
        public readonly List<string> Log = new List<string>();
        public event Action<string, string> Event;   // (kind, text): "change", "changed", "reset", "gate", "finish"
        public string LastEvent { get; private set; } = "";

        float boostIdle, stuckFor, wrongWaterFor, noProgressFor, lastBest; Vector3 lastPos;

        void Awake()
        {
            Body = GetComponent<Rigidbody>(); Input = GetComponent<ShiftInput>(); Forms = GetComponentInChildren<FormSet>();
            Body.interpolation = RigidbodyInterpolation.Interpolate; Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.mass = 1200; Body.linearDamping = 0; Body.angularDamping = .5f; Body.maxAngularVelocity = 5;
            Body.centerOfMass = new Vector3(0, -.35f, 0);
            SetLayer(VehicleLayer);
        }
        void OnDestroy() { if (Player == this) Player = null; }

        public void Note(string kind, string text)
        {
            string line = $"[{Time.time - StartTime,7:0.00}] {text}";
            Log.Add(line); LastEvent = text; Debug.Log("[ShiftStorm] " + line); Event?.Invoke(kind, text);
        }

        // Start (or restart) on a course at its first anchor, in the course's start form, with the progress cleared.
        // A time trial (course.timeTrial, from its first anchor) holds the vehicle on the grid for the countdown; the clock starts at GO.
        // grid (Round 3, a race): this racer's place on the grid, relative to the start anchor (x right, z forward, metres)
        public void Begin(Course course, RecoveryAnchor at = null, Vector3 grid = default)
        {
            Course = course; bool fromStart = at == null || at == course.anchors[0]; at = at ? at : course.anchors[0];
            GatesPassed = 0; BestProgress = lastBest = 0; Finished = false; FinishTime = -1; Resets = 0; Changes = 0;
            Boost = shared.boostStart; StartTime = Time.time; Log.Clear(); Splits.Clear(); Penalty = 0; NearMisses.Clear(); Scrapes = 0; WallHits = 0; MissedGate = null; Cue = ""; CueUntil = -1; missedFor = 0; ClearRoute();
            Place(at, at.mode, at.mode == Mode.Fly || at.mode == Mode.Dive ? shared.resetSpeed : 0, fromStart ? grid : default);
            OnShortcut = null; ShortcutsTaken.Clear(); GateLog.Clear(); scrapeUntil = -1;
            // starting at a later anchor: the gates behind it count as passed, and its place on the course as reached
            BestProgress = lastBest = Progress; foreach (var g in course.gates) if (g.progress < Progress - 1f) GatesPassed++;
            held = course.timeTrial && fromStart && shared.countdownSeconds > 0;
            if (held) { HoldUntil = Time.time + shared.countdownSeconds; StartTime = HoldUntil; Body.isKinematic = true; } else HoldUntil = -1;
            Note("start", $"start on {course.courseName} in {Mode}{(held ? $", countdown {shared.countdownSeconds:0} s" : "")}");
        }

        public static int FixedSteps;   // physics steps so far (the stats readout: steps per rendered frame)
        public float TopSpeedNow => Mode == Mode.Drive ? drive.topSpeed : Mode == Mode.Boat ? boat.topSpeed : Mode == Mode.Fly ? fly.topSpeed : dive.topSpeed;
        public float BoostTopSpeedNow => Mode == Mode.Drive ? drive.boostTopSpeed : Mode == Mode.Boat ? boat.boostTopSpeed : Mode == Mode.Fly ? fly.boostTopSpeed : dive.boostTopSpeed;

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime; if (IsPlayer) FixedSteps++;
            if (held)
            {
                if (Time.time < HoldUntil) { Input.ConsumeReset(); return; }
                held = false; Body.isKinematic = false; Body.linearVelocity = Vector3.zero; Note("go", "GO");
            }
            UpdateBoost(dt);
            if (Changing)
            {
                float t = (Time.time - ChangeStartedAt) / shared.changeSeconds;
                Authority = Mathf.Lerp(shared.startAuthority, 1, Mathf.Clamp01(t));
                if (Forms == null || Forms.Done)
                {
                    Changing = false; Authority = 1;
                    float took = Forms ? Forms.VisualDoneAt - ChangeStartedAt : Time.time - ChangeStartedAt;
                    LastChangeSeconds = took;
                    Note("changed", $"{ChangeFrom}->{Mode} done in {took:0.000} s");
                }
            }
            CheckZones();
            Carry(dt);
            bool railed = StepStunts(dt);   // Round 7: the loop's rail, the boost pad, jumps, the wings
            if (!railed)
            {
                switch (Mode)
                {
                    case Mode.Drive: StepDrive(dt); break;
                    case Mode.Boat: StepBoat(dt); break;
                    case Mode.Fly: StepFly(dt); break;
                    case Mode.Dive: StepDive(dt); break;
                }
                AlignAssist(dt);
            }
            TrackProgress();
            Recovery(dt);
            if (GraceUntil > 0 && Time.time >= GraceUntil) { GraceUntil = -1; SetLayer(VehicleLayer); }
            var f = Vector3.ProjectOnPlane(Mode == Mode.Fly || Mode == Mode.Dive ? transform.forward : (Body.linearVelocity.sqrMagnitude > 4 && Vector3.Dot(Body.linearVelocity, transform.forward) > 0 ? Body.linearVelocity : transform.forward), Vector3.up);
            if (f.sqrMagnitude > 1e-3f && !OnLoop) HeadingDeg = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            stepVel = Body.linearVelocity;   // Round 4: the velocity going into the physics step (a wall hit is measured against it)
        }
        Vector3 stepVel;

        // the speed carried into a faster form, eased in over the change; and the car pulled onto its wheels after a change into Drive
        void Carry(float dt)
        {
            if (Mode == Mode.Drive && EnteredDriveAt > 0 && Time.time - EnteredDriveAt < shared.wheelsDownSeconds)
            {
                var lv0 = Body.linearVelocity; float flat0 = new Vector2(lv0.x, lv0.z).magnitude;
                if (FirstWheelContactAt < 0) { if (GroundedWheels == 0) Body.AddForce(Vector3.down * shared.wheelsDownExtraGravity, ForceMode.Acceleration); landKeep = flat0; }
                // the spring takes the landing's downward speed, never the forward speed: for a moment after the first touch the flat speed
                // is held at what it was in the air (the hull's touch on the road would otherwise brake it by about 3 m/s)
                else if (Time.time - FirstWheelContactAt < .25f && flat0 < landKeep && flat0 > 1f) { var k = landKeep / flat0; Body.linearVelocity = new Vector3(lv0.x * k, lv0.y, lv0.z * k); }
            }
            if (CarryTarget < 0) return;
            if (!Changing || Speed >= CarryTarget - .05f) { CarryTarget = -1; return; }
            float step = carryRate * dt;
            switch (Mode)
            {
                case Mode.Fly: Airspeed = Mathf.MoveTowards(Airspeed, CarryTarget, step); break;
                case Mode.Dive: DiveSpeed = Mathf.MoveTowards(DiveSpeed, CarryTarget, step); break;
                default:
                    var lv = Body.linearVelocity; var flat = new Vector3(lv.x, 0, lv.z); if (flat.sqrMagnitude < 1) break;
                    Body.AddForce(flat.normalized * Mathf.Min(step, CarryTarget - Speed), ForceMode.VelocityChange); break;
            }
        }

        void UpdateBoost(float dt)
        {
            bool want = Input.Boost && Boost > 0.01f && !Finished;
            if (want) { Boost = Mathf.Max(0, Boost - shared.boostDrainPerSecond * dt); boostIdle = 0; }
            else { boostIdle += dt; if (boostIdle >= shared.boostDelayAfterUse) Boost = Mathf.Min(1, Boost + shared.boostRefillPerSecond * dt); }
            Boosting = want;
        }

        // ---- form changes ----
        void CheckZones()
        {
            var p = Body.position; var v = Body.linearVelocity;
            foreach (var z in TransformZone.All)
            {
                if (z == LastZone || z.target == Mode || !z.Contains(p)) continue;
                if (!z.MovingThrough(v)) continue;
                LastZone = z; Change(z.target, z.label);
                return;
            }
        }

        public float TopOf(Mode m) => m == Mode.Drive ? drive.topSpeed : m == Mode.Boat ? boat.topSpeed : m == Mode.Fly ? fly.topSpeed : dive.topSpeed;
        float EntryMax(Mode m) => m == Mode.Drive ? drive.entrySpeed.y : m == Mode.Boat ? boat.entrySpeed.y : m == Mode.Fly ? fly.entrySpeed.y : dive.entrySpeed.y;
        public float CarryTarget { get; private set; } = -1;   // Round 3: the speed being eased in over a change into a faster form
        float carryRate, landKeep; public float EnteredDriveAt { get; private set; } = -1;

        public void Change(Mode to, string why, bool animate = true)
        {
            var from = Mode; float speedIn = Speed;
            ChangeFrom = from; Mode = to; Changes++;
            switch (to)
            {
                case Mode.Drive: EnterDrive(speedIn); break;
                case Mode.Boat: EnterBoat(speedIn); break;
                case Mode.Fly: EnterFly(speedIn, false); break;
                case Mode.Dive: EnterDive(speedIn); break;
            }
            CarryTarget = -1; if (to == Mode.Drive) EnteredDriveAt = Time.time;
            if (animate && shared.changeSpeedCarry > 0 && TopOf(to) > TopOf(from) && speedIn > 1f)
            {
                float want = Mathf.Min(speedIn * Mathf.Lerp(1, TopOf(to) / TopOf(from), shared.changeSpeedCarry), EntryMax(to));
                if (want > Speed + .5f) { CarryTarget = want; carryRate = (want - Speed) / Mathf.Max(.1f, shared.changeSeconds); }
            }
            if (animate)
            {
                Changing = true; ChangeStartedAt = Time.time; Authority = shared.startAuthority;
                Forms?.Begin(to, shared.changeSeconds);
                assistLeft = shared.alignAssistSeconds; assistUsed = 0;
                Note("change", $"{from}->{to} at z {p3(Body.position)} ({why}); speed {speedIn:0.0} -> {Speed:0.0} m/s");
            }
            else { Changing = false; Authority = 1; Forms?.Snap(to); }
        }
        static string p3(Vector3 p) => $"{p.z:0.0} (x {p.x:0.0}, y {p.y:0.0})";

        // Gentle, bounded alignment help after a change for the ground and water forms (flight and diving set their attitude themselves and
        // hold their entry pitch in range): the body's roll and pitch are eased toward level at a limited rate, for a limited total angle.
        float assistLeft, assistUsed;
        void AlignAssist(float dt)
        {
            if (assistLeft <= 0) return; assistLeft -= dt;
            if (Mode == Mode.Fly || Mode == Mode.Dive) return;
            var fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up); if (fwd.sqrMagnitude < 1e-3f) fwd = Vector3.ProjectOnPlane(transform.up, Vector3.up);
            var level = Quaternion.LookRotation(fwd.normalized, Vector3.up);
            float off = Quaternion.Angle(Body.rotation, level); if (off < 2f) return;
            float step = Mathf.Min(shared.alignAssistMaxDegPerSecond * dt, shared.alignAssistMaxTotalDeg - assistUsed, off);
            if (step <= 0) return;
            assistUsed += step; Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, level, step));
        }

        // ---- progress ----
        // Progress is searched near the last progress (Course.Project with a hint), so the cave under the road is never mistaken for the road.
        // Gates count in order, forward, inside their frame and in a permitted form. Round 2b: the direction cues come from ShiftVehicle.Route
        // (heading against the route direction here, off the route, past the next gate), never from a gate crossing alone.
        void TrackProgress()
        {
            if (!Course) return;
            var p = Body.position; var gates = Course.gates; Vector3 at, dir;
            if (OnShortcut) { ShortcutProgress(p, out at, out dir); if (!Course) return; }
            else if (rail != null && rail.exitProgress > rail.entryProgress && rail.Length > 0)
            {
                // Round 10: on a coaster rail the progress is the share of the track ridden (the final soak: rail A's turn back south passes
                // 75 m from the boardwalk, the projection sometimes took the boardwalk and the car was reset "out of bounds" off the track)
                Progress = Mathf.Lerp(rail.entryProgress, rail.exitProgress, railS / rail.Length); at = Course.PointAt(Progress, out dir);
            }
            else
            {
                Progress = Course.Project(p, Progress, out at, out dir);
                foreach (var sc in Course.shortcuts)
                {
                    if (sc.nextGate != GatesPassed || !sc.entry.Allows(Mode)) continue;
                    // through the entry frame, or (a racer that slipped in past the frame's edge) already inside the shortcut beyond it
                    bool inside = sc.entry.transform.InverseTransformPoint(p).z > 0 && sc.exit.transform.InverseTransformPoint(p).z < 0 && Vector3.Distance(p, sc.entry.transform.position) < 60f
                                  && Vector3.Distance(p, sc.PointAt(sc.Project(p, -1, out _, out _), out _)) < sc.halfWidth * .8f && Course.Project(p, Progress, out var mainAt, out _) >= 0 && Vector3.Distance(p, mainAt) > sc.halfWidth;
                    if (sc.entry.Crossed(lastPos, p) || inside) { EnterShortcut(sc, p); break; }
                    if (sc.entry.CrossedNear(lastPos, p, shared.nearMissMargin, out float nb) && Vector3.Dot(Body.linearVelocity.normalized, sc.entry.transform.forward) > .5f) { EnterShortcut(sc, p, nb); break; }
                }
            }
            if (Progress > BestProgress) BestProgress = Progress;
            while (!OnShortcut && GatesPassed < gates.Count && gates[GatesPassed].Crossed(lastPos, p))
            {
                var g = gates[GatesPassed];
                if (!g.Allows(Mode))
                {
                    Note("rejected", $"gate {g.label} crossed as {Mode}: not counted (needs {string.Join("/", g.modes)})"); ShowCue($"{g.label}: NOT AS {Mode}", CueKind.Warn);
                    // Round 12 item 3: turning back cannot help (a zone changes the form only once, so the racer stays in the wrong form): build
                    // 13 said TURN BACK and a plane past Coaster Coast's G1 circled it for good. Now: at once to the anchor before the gate, in its form.
                    if (!OldFlight && !Finished) { Recover($"{g.label} crossed as {Mode}"); return; }
                    break;
                }
                PassGate(g, "");
            }
            // Round 4 item 6: just beside the next gate (through its plane, outside the frame, inside the near-miss band), in a permitted
            // form and carrying on along the route: it counts, with a time penalty. A far miss, the wrong form or a skipped section: as before.
            if (!OnShortcut && GatesPassed < gates.Count && gates[GatesPassed].CrossedNear(lastPos, p, shared.nearMissMargin, out float outBy))
            {
                var g = gates[GatesPassed];
                if (!g.Allows(Mode)) { Note("rejected", $"near miss of {g.label} as {Mode} ({outBy:0.0} m outside): not counted (needs {string.Join("/", g.modes)})"); ShowCue($"{g.label}: NOT AS {Mode}", CueKind.Warn); if (!OldFlight && !Finished) { Recover($"{g.label} passed as {Mode}"); return; } }
                else if (TravelAlong(p) < .5f) Note("rejected", $"near miss of {g.label} ({outBy:0.0} m outside) not heading along the route: not counted");
                else { AddPenalty(g.label, outBy); PassGate(g, $" (near miss, {outBy:0.0} m outside, +{shared.nearMissPenalty:0} s)"); }
            }
            if (GatesPassed > 0 && GatesPassed < gates.Count && gates[GatesPassed - 1].CrossedBackward(lastPos, p)) Note("back through", $"back through {gates[GatesPassed - 1].label} (still {GatesPassed} gates)");
            lastPos = p;
            var segs = Course.segments;
            while (Splits.Count < segs.Count && GatesPassed >= segs[Splits.Count].gatesNeeded && Progress >= segs[Splits.Count].endProgress - 1f)
            {
                var sg = segs[Splits.Count]; float t = Time.time - StartTime, prev = Splits.Count > 0 ? Splits[Splits.Count - 1] : 0; Splits.Add(t);
                Note("split", $"split {sg.name}: {t - prev:0.00} s (target {sg.target:0} s), total {t:0.00} s");
            }
            TrackRoute(p, at, dir);
        }
        void PassGate(ProgressGate g, string how)
        {
            var gates = Course.gates;
            GatesPassed++; MissedGate = null; missedFor = 0; GateLog.Add(g.index);
            Note("gate", $"gate {g.label} passed{how} ({GatesPassed}/{gates.Count})");
            if (GatesPassed == gates.Count && !Finished) { Finished = true; FinishTime = Time.time - StartTime + Penalty; Note("finish", $"finish in {FinishTime:0.00} s{(Penalty > 0 ? $" (including +{Penalty:0} s for {NearMisses.Count} near misses)" : "")}, {Resets} resets"); }
        }
        // Round 4 item 6: near misses, each with ShiftTuning.nearMissPenalty seconds added to the lap
        public float Penalty { get; private set; }
        public float MissCueUntil { get; private set; } = -1;   // the near-miss cue holds over the direction cues this long
        public readonly List<string> NearMisses = new List<string>();
        void AddPenalty(string label, float outBy)
        {
            Penalty += shared.nearMissPenalty; NearMisses.Add(label); MissCueUntil = Time.time + 2f;
            string shortName = label.Split(' ')[0];
            ShowCue($"MISSED {shortName} +{shared.nearMissPenalty:0} s", CueKind.Warn, 2f);
        }
        // cos of the angle between the racer's travel and the route direction here (the line's, or the shortcut's)
        float TravelAlong(Vector3 p)
        {
            var vel = Body.linearVelocity; var travel = vel.sqrMagnitude > 9f ? vel.normalized : transform.forward;
            Vector3 a, b;
            if (OnShortcut) { a = OnShortcut.PointAt(scS - 6, out _); b = OnShortcut.PointAt(scS + 6, out _); } else { a = Course.PointAt(Progress - 6, out _); b = Course.PointAt(Progress + 6, out _); }
            var rd = b - a; if (rd.sqrMagnitude < 1e-4f) return 1;
            bool air = Mode == Mode.Fly || Mode == Mode.Dive;
            if (!air) { rd.y = 0; travel.y = 0; }
            return Vector3.Dot(travel.normalized, rd.normalized);
        }
        public enum CueKind { Guide, Warn, Wrong }
        public CueKind CueType { get; private set; }
        void ShowCue(string text, CueKind kind, float seconds = 1.5f) { Cue = text; CueType = kind; CueUntil = Time.time + seconds; }

        void SetLayer(int layer) { foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }

    }
}
