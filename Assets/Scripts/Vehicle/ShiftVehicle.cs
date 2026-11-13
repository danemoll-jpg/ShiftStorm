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
        public static int GroundMask => Physics.DefaultRaycastLayers & ~(1 << VehicleLayer) & ~(1 << GhostLayer);

        public ShiftTuning shared; public DriveTuning drive; public BoatTuning boat; public FlyTuning fly; public DiveTuning dive;
        public Rigidbody Body { get; private set; }
        public ShiftInput Input { get; private set; }
        public FormAnimator Forms { get; private set; }
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
        public string Cue { get; private set; } = "";                // a return cue for the HUD ("missed gate ...", "wrong way")
        public float CueUntil { get; private set; } = -1;
        bool held; float missedFor;

        // Everything that happened, in order, for checks and the HUD's last line.
        public readonly List<string> Log = new List<string>();
        public event Action<string, string> Event;   // (kind, text): "change", "changed", "reset", "gate", "finish"
        public string LastEvent { get; private set; } = "";

        float boostIdle, stuckFor, wrongWaterFor, noProgressFor, lastBest; Vector3 lastPos;

        void Awake()
        {
            Body = GetComponent<Rigidbody>(); Input = GetComponent<ShiftInput>(); Forms = GetComponentInChildren<FormAnimator>();
            Body.interpolation = RigidbodyInterpolation.Interpolate; Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.mass = 1200; Body.linearDamping = 0; Body.angularDamping = .5f; Body.maxAngularVelocity = 5;
            Body.centerOfMass = new Vector3(0, -.35f, 0);
            SetLayer(VehicleLayer);
            Player = this;
        }
        void OnDestroy() { if (Player == this) Player = null; }

        public void Note(string kind, string text)
        {
            string line = $"[{Time.time - StartTime,7:0.00}] {text}";
            Log.Add(line); LastEvent = text; Debug.Log("[ShiftStorm] " + line); Event?.Invoke(kind, text);
        }

        // Start (or restart) on a course at its first anchor, in the course's start form, with the progress cleared.
        // A time trial (course.timeTrial, from its first anchor) holds the vehicle on the grid for the countdown; the clock starts at GO.
        public void Begin(Course course, RecoveryAnchor at = null)
        {
            Course = course; bool fromStart = at == null || at == course.anchors[0]; at = at ? at : course.anchors[0];
            GatesPassed = 0; BestProgress = lastBest = 0; Finished = false; FinishTime = -1; Resets = 0; Changes = 0;
            Boost = shared.boostStart; StartTime = Time.time; Log.Clear(); Splits.Clear(); MissedGate = null; Cue = ""; CueUntil = -1; missedFor = 0;
            Place(at, at.mode, at.mode == Mode.Fly || at.mode == Mode.Dive ? shared.resetSpeed : 0);
            // starting at a later anchor: the gates behind it count as passed, and its place on the course as reached
            BestProgress = lastBest = Progress; foreach (var g in course.gates) if (g.progress < Progress - 1f) GatesPassed++;
            held = course.timeTrial && fromStart && shared.countdownSeconds > 0;
            if (held) { HoldUntil = Time.time + shared.countdownSeconds; StartTime = HoldUntil; Body.isKinematic = true; } else HoldUntil = -1;
            Note("start", $"start on {course.courseName} in {Mode}{(held ? $", countdown {shared.countdownSeconds:0} s" : "")}");
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
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
            switch (Mode)
            {
                case Mode.Drive: StepDrive(dt); break;
                case Mode.Boat: StepBoat(dt); break;
                case Mode.Fly: StepFly(dt); break;
                case Mode.Dive: StepDive(dt); break;
            }
            AlignAssist(dt);
            TrackProgress();
            Recovery(dt);
            if (GraceUntil > 0 && Time.time >= GraceUntil) { GraceUntil = -1; SetLayer(VehicleLayer); }
            var f = Vector3.ProjectOnPlane(Mode == Mode.Fly || Mode == Mode.Dive ? transform.forward : (Body.linearVelocity.sqrMagnitude > 4 && Vector3.Dot(Body.linearVelocity, transform.forward) > 0 ? Body.linearVelocity : transform.forward), Vector3.up);
            if (f.sqrMagnitude > 1e-3f) HeadingDeg = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
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
        // Gates count in order, forward, inside their frame and in a permitted form; a backward crossing of the last gate is a "wrong way"
        // cue; going past the next gate without earning it gives a return cue, then a reset (ShiftTuning.missedGate*).
        void TrackProgress()
        {
            if (!Course) return;
            var p = Body.position; var gates = Course.gates;
            Progress = Course.Project(p, Progress, out _, out _);
            if (Progress > BestProgress) BestProgress = Progress;
            while (GatesPassed < gates.Count && gates[GatesPassed].Crossed(lastPos, p))
            {
                var g = gates[GatesPassed];
                if (!g.Allows(Mode)) { Note("rejected", $"gate {g.label} crossed as {Mode}: not counted (needs {string.Join("/", g.modes)})"); ShowCue($"{g.label}: NOT AS {Mode}"); break; }
                GatesPassed++; MissedGate = null; missedFor = 0;
                Note("gate", $"gate {g.label} passed ({GatesPassed}/{gates.Count})");
                if (GatesPassed == gates.Count && !Finished) { Finished = true; FinishTime = Time.time - StartTime; Note("finish", $"finish in {FinishTime:0.00} s, {Resets} resets"); }
            }
            if (GatesPassed > 0 && GatesPassed < gates.Count && gates[GatesPassed - 1].CrossedBackward(lastPos, p)) { Note("wrong way", $"wrong way: back through {gates[GatesPassed - 1].label}"); ShowCue("WRONG WAY"); }
            lastPos = p;
            var segs = Course.segments;
            while (Splits.Count < segs.Count && GatesPassed >= segs[Splits.Count].gatesNeeded && Progress >= segs[Splits.Count].endProgress - 1f)
            {
                var sg = segs[Splits.Count]; float t = Time.time - StartTime, prev = Splits.Count > 0 ? Splits[Splits.Count - 1] : 0; Splits.Add(t);
                Note("split", $"split {sg.name}: {t - prev:0.00} s (target {sg.target:0} s), total {t:0.00} s");
            }
            // missed gate: past the next gate's place without earning it
            if (!Finished && GatesPassed < gates.Count && !Changing)
            {
                var g = gates[GatesPassed]; float over = Progress - g.progress;
                if (over > shared.missedGateCueMeters)
                {
                    if (MissedGate != g) { MissedGate = g; missedFor = 0; Note("missed", $"missed gate {g.label} ({over:0} m past it, {Mode}): go back"); }
                    missedFor += Time.fixedDeltaTime; ShowCue($"MISSED {g.label} - GO BACK");
                    if (over > shared.missedGateResetMeters || missedFor > shared.missedGateResetSeconds) { MissedGate = null; missedFor = 0; Recover($"missed gate {g.label}"); }
                }
                else if (MissedGate == g && over < 0) { MissedGate = null; missedFor = 0; }
            }
        }
        void ShowCue(string text) { Cue = text; CueUntil = Time.time + 1.5f; }

        void SetLayer(int layer) { foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }
    }
}
