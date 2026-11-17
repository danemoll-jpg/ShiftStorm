using UnityEngine;

namespace ShiftStorm
{
    // Round 2b (Dan on the Deck: "everywhere I try to go is telling me it is the wrong way ... no indicators whatsoever"): where to go, and
    // the direction cues.
    // The cause of Dan's "wrong way": build 2's red centre cue in the air was the missed-gate cue "MISSED G3 arch exit - GO BACK". It came
    // from progress alone (progress along the line more than 25 m past the next gate's place) and was shown again every physics step
    // whatever the heading, so once a plane had gone past the Sky Arch beside G3's frame (easy: the line curves, a plane flies straight and
    // there was nothing to aim at), turning in any direction still said "go back" until the reset 5 s later. Next to that, a plane more than
    // 75 m to the side of the line (2 s of flying sideways) was reset "out of bounds" without any warning. The real backward cue (crossing the
    // last gate backward) almost never fired.
    // Now: WRONG WAY only when the racer really moves against the route direction here (the line's direction at its progress) for a moment,
    // and is on the route; off the route it is THIS WAY with the arrow; past the next gate it is TURN BACK until it heads back to it (then
    // THIS WAY), measured on the far side of the gate's own plane; in the air, out of bounds warns (THIS WAY) for a few seconds first.
    // GuideTarget is where the HUD's arrow points (and the guided check pilot steers): along the route ahead, never past the next gate.
    public sealed partial class ShiftVehicle
    {
        public ProgressGate NextGate => Course && GatesPassed < Course.gates.Count ? Course.gates[GatesPassed] : null;
        public ProgressGate GateAfter => Course && GatesPassed + 1 < Course.gates.Count ? Course.gates[GatesPassed + 1] : null;
        public Vector3 RouteDir { get; private set; } = Vector3.forward;   // the line's direction at the racer's progress
        public Vector3 RoutePoint { get; private set; }                    // the nearest line point
        public float OffLine { get; private set; }                         // metres from the line (ground plan on land and water, 3D in the air and underwater)
        public bool OffRoute { get; private set; }
        public bool WrongWay { get; private set; }
        public float RouteAlign { get; private set; } = 1;                 // cos(angle) between the racer's travel and the route direction
        public Vector3 GuideTarget { get; private set; }
        public float OutOfBoundsFor { get; private set; }
        float backwardFor, offFor;

        void ClearRoute() { WrongWay = OffRoute = false; backwardFor = offFor = 0; OutOfBoundsFor = 0; RouteAlign = 1; }

        public float OffRouteLimit => Mode == Mode.Fly ? shared.offRouteFly : Mode == Mode.Dive ? shared.offRouteDive : Mode == Mode.Boat ? shared.offRouteBoat : shared.offRouteDrive;

        void TrackRoute(Vector3 p, Vector3 at, Vector3 dir)
        {
            float dt = Time.fixedDeltaTime;
            // the route direction, smoothed over a few metres either side (corners and the line's 3 m steps)
            var sc = OnShortcut;   // Round 3: on a shortcut the route is the shortcut's own line
            var a = sc ? sc.PointAt(scS - 6, out _) : Course.PointAt(Progress - 6, out _); var b = sc ? sc.PointAt(scS + 6, out _) : Course.PointAt(Progress + 6, out _);
            var rd = b - a; RouteDir = rd.sqrMagnitude > 1e-4f ? rd.normalized : dir; RoutePoint = at;
            bool air = Mode == Mode.Fly || Mode == Mode.Dive;
            OffLine = air ? Vector3.Distance(p, at) : new Vector2(p.x - at.x, p.z - at.z).magnitude;
            // Round 3: heading for a shortcut (near its line, before its entry frame) is not "off the route": no THIS WAY, and the arrow
            // follows the shortcut
            Shortcut near = null; float nearS = 0;
            if (!sc) foreach (var s2 in Course.shortcuts)
                    if (s2.nextGate == GatesPassed && (OldFlight || s2.entry.Allows(Mode))) { float ss = s2.Project(p, -1, out var a2, out _); if (ss > 5 && ss < s2.Length - 5 && Vector3.Distance(p, a2) < s2.halfWidth) { near = s2; nearS = ss; break; } }
            offFor = OffLine > OffRouteLimit && !near ? offFor + dt : 0; OffRoute = offFor > .4f;
            // travel direction: the velocity when moving, else the nose
            var vel = Body.linearVelocity; var travel = vel.sqrMagnitude > 9f ? vel.normalized : transform.forward;
            var route = air ? RouteDir : Vector3.ProjectOnPlane(RouteDir, Vector3.up).normalized; if (!air) travel = Vector3.ProjectOnPlane(travel, Vector3.up).normalized;
            RouteAlign = Vector3.Dot(travel, route);
            bool backwards = RouteAlign < shared.wrongWayCos && !Holding && !Finished && !OnLoop;   // Round 7: upside down in the loop is not "wrong way"
            backwardFor = backwards ? backwardFor + dt : 0;
            bool wasWrong = WrongWay; WrongWay = backwardFor > shared.wrongWaySeconds && !OffRoute;
            if (WrongWay && !wasWrong) Note("wrong way", $"wrong way: heading {Mathf.Acos(Mathf.Clamp(RouteAlign, -1, 1)) * Mathf.Rad2Deg:0} deg against the route at {Progress:0} m ({Mode})");

            var gates = Course.gates; var g = NextGate;
            // past the next gate without earning it: on the far side of its own plane, and its place on the line well behind
            bool past = false; float over = 0;
            if (!Finished && g && !Changing && !sc && GatesPassed < gates.Count)
            {
                over = Progress - g.progress;
                past = over > shared.missedGateCueMeters && g.transform.InverseTransformPoint(p).z > 0;
            }
            bool returning = false;
            if (past)
            {
                if (MissedGate != g) { MissedGate = g; missedFor = 0; Note("missed", $"missed gate {g.label} ({over:0} m past it, {Mode}): turn back"); }
                if (!OldFlight && !g.Allows(Mode) && shared.missedGateResetMeters < 9000) { MissedGate = null; missedFor = 0; MissedRecover(g, $"missed gate {g.label} as {Mode}"); return; }   // Round 12: in the wrong form turning back cannot earn it
                var toGate = g.transform.position - p; returning = Vector3.Dot(travel, toGate.normalized) > .5f;
                if (!returning) missedFor += dt;
                if (over > shared.missedGateResetMeters || missedFor > shared.missedGateResetSeconds) { MissedGate = null; missedFor = 0; MissedRecover(g, $"missed gate {g.label}"); return; }
            }
            else if (MissedGate == g && over < 0) { MissedGate = null; missedFor = 0; }

            // where to go: the route ahead (never past the next gate); back to the gate when it was missed; back onto the line when off it
            float ahead = Mathf.Clamp(Speed * 1.6f, 45f, 170f);
            float aim = Progress + (OffRoute ? ahead * .6f : ahead);
            if (g) aim = Mathf.Min(aim, g.progress + 2f);
            // Round 12 item 3: the arrow follows a shortcut only once it is entered. Build 13 followed any shortcut whose line was within its width
            // before its entry: the Sky Arch slit's line starts on the route 490 m before the arch, so the arrow led every plane to a 22 m slit
            // 190 m off the line; a miss there was reset to the anchor before the flight (lip + 90) and the arrow led it back in: a loop.
            GuideTarget = MissedGate ? MissedGate.transform.position : sc ? (OldFlight || ChaseCam.OldFlightLook ? sc.PointAt(scS + ahead, out _) : sc.GuideAt(scS, ahead)) : OldFlight && near ? near.PointAt(nearS + ahead, out _) : Course.PointAt(Mathf.Min(aim, Course.Length), out _);

            // the cue, most important first
            if (Holding || Finished || Time.time < MissCueUntil) return;
            if (MissedGate) ShowCue(returning ? $"THIS WAY: {MissedGate.label}" : $"TURN BACK: {MissedGate.label}", returning ? CueKind.Guide : CueKind.Wrong, .3f);
            else if (WrongWay) ShowCue("WRONG WAY - TURN AROUND", CueKind.Wrong, .3f);
            else if (OffRoute || OutOfBoundsFor > 0) ShowCue("THIS WAY", CueKind.Guide, .3f);
        }
    
        // Round 16 (the Dam Shame soaks: an AI straggler reset 136-205 times at one gate, the water risen round it): the third reset in a row
        // for the same missed gate puts the racer past it instead, the gate counted with ShiftTuning.branchRecoveryPenalty seconds added
        int loopGate = -1, loopResets;
        void MissedRecover(ProgressGate g, string reason)
        {
            if (loopGate != GatesPassed) { loopGate = GatesPassed; loopResets = 0; }
            loopResets++;
            if (!OldFlight && loopResets >= 3 && GatesPassed < Course.gates.Count - 1)   // never the finish line
            {
                GatesPassed++; GateLog.Add(g.index); loopGate = -1; loopResets = 0;
                Penalty += shared.branchRecoveryPenalty; NearMisses.Add(g.label.Replace(" ", "-") + "-reset"); MissCueUntil = Time.time + 2.5f;
                Note("gate", $"gate {g.label} counted after 3 resets for it in a row (+{shared.branchRecoveryPenalty:0} s) ({GatesPassed}/{Course.gates.Count})");
                if (BestProgress < g.progress + 5f) { BestProgress = g.progress + 5f; lastBest = BestProgress; }
            }
            Recover(reason);
        }
}
}
