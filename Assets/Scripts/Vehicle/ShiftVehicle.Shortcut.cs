using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 3 item 5: progress while on a shortcut (see Shortcut). GateLog keeps every main gate earned, in order (and "via" shortcuts), so
    // the soak test can check each counted finish has a valid gate sequence.
    public sealed partial class ShiftVehicle
    {
        public Shortcut OnShortcut { get; private set; }
        public readonly List<string> ShortcutsTaken = new List<string>();
        public readonly List<int> GateLog = new List<int>();
        float scS, scOffFor;
        Shortcut leftBranch; float leftBranchAt = -99;
        // checks only ("-oldFlight"): build 13's flight branches for before / after runs: its Lake flight and Midway flight, the arrow
        // following a nearby shortcut, no forward recovery or flight-line reset, no assist, the branch entered only at its entry frame
        public static readonly bool OldFlight = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldFlight") >= 0;

        // Round 12: the wings opened off a choice jump: on its flight branch at once (build 13 waited for the entry frame 70 m out, and the
        // arrow, the assist and the forward recovery only knew the branch from there)
        void EnterChoiceBranch(Vector3 p)
        {
            if (!Course || OnShortcut || OldFlight) return;
            foreach (var sc in Course.shortcuts)
                if (sc.choice && sc.nextGate == GatesPassed && sc.entry.Allows(Mode.Fly) && Vector3.Distance(p, sc.line[0]) < 80f) { EnterShortcut(sc, p); return; }
        }

        // Round 12: a light steering assist on a flight branch (people only): the plane is turned toward the branch's line ahead, less while
        // the player steers hard, and pitched toward it while the stick is near centre. Never more than ShiftTuning.branchAssist* deg/s.
        void BranchAssist(float dt, ref float yaw, ref float pitch, float steerIn, float vertIn)
        {
            var sc = OnShortcut; if (!Human || !sc || !sc.flight || Changing || OldFlight) return;
            var tgt = sc.PointAt(scS + Mathf.Max(45f, Airspeed * 1.1f), out _); var to = tgt - Body.position; var flat = new Vector2(to.x, to.z);
            if (flat.sqrMagnitude < 1f) return;
            float wantYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, k = 1f - .7f * Mathf.Clamp01(Mathf.Abs(steerIn));
            if (Mathf.Abs(Mathf.DeltaAngle(yaw, wantYaw)) < 100f) yaw = Mathf.MoveTowardsAngle(yaw, wantYaw, shared.branchAssistYaw * k * dt);
            if (Mathf.Abs(vertIn) < .3f) { float wantPitch = Mathf.Clamp(Mathf.Atan2(to.y, flat.magnitude) * Mathf.Rad2Deg, -fly.maxPitchDown, fly.maxPitchUp); pitch = Mathf.MoveTowards(pitch, wantPitch, shared.branchAssistPitch * dt); }
        }

        // Round 12 item 3: where a reset on a flight branch goes: forward, to the last anchor before its rejoin (+ a little), never past an
        // unearned gate (a branch standing in for a gate counts it). null: not on (or just off) a flight branch.
        RecoveryAnchor BranchAnchor(out Shortcut br)
        {
            if (OldFlight) { br = null; return null; }
            br = OnShortcut && OnShortcut.flight ? OnShortcut : leftBranch && Time.time - leftBranchAt < 8f && Progress < leftBranch.mainTo + 80f ? leftBranch : null;
            if (!br || br.nextGate != GatesPassed) { br = null; return null; }
            int gates = GatesPassed + (br.replacesGate >= 0 && GatesPassed == br.replacesGate ? br.replacesCount : 0);
            float limit = br.mainTo + 60f; if (gates < Course.gates.Count) limit = Mathf.Min(limit, Course.gates[gates].progress);
            RecoveryAnchor pick = null; foreach (var a in Course.anchors) if (a.progress <= limit + .5f && a.progress > br.mainFrom + 1f) pick = a;
            if (!pick) br = null;
            return pick;
        }

        readonly List<(string label, float outBy)> scPenalties = new List<(string, float)>();
        void EnterShortcut(Shortcut sc, Vector3 p, float nearBy = -1)
        {
            OnShortcut = sc; scOffFor = 0; scPenalties.Clear(); if (nearBy >= 0) scPenalties.Add((sc.entry.label, nearBy)); scS = sc.Project(p, -1, out _, out _);
            Progress = sc.MainProgress(scS);
            Note("shortcut", $"shortcut {sc.label}: in ({Mode}, {Speed:0} m/s)");
        }

        void ShortcutProgress(Vector3 p, out Vector3 at, out Vector3 dir)
        {
            var sc = OnShortcut;
            scS = sc.Project(p, scS, out at, out dir);
            Progress = sc.MainProgress(scS);
            float off = Vector3.Distance(p, at);
            scOffFor = off > sc.halfWidth * 1.6f ? scOffFor + Time.fixedDeltaTime : 0;
            float outBy = 0; bool nearExit = !sc.exit.Crossed(lastPos, p) && sc.exit.CrossedNear(lastPos, p, shared.nearMissMargin, out outBy) && sc.exit.Allows(Mode) && TravelAlong(p) > .5f;
            if (sc.exit.Crossed(lastPos, p) || nearExit)
            {
                // Round 4: a near miss of the entry or the exit frame counts with the penalty, charged when the shortcut is finished
                if (nearExit) scPenalties.Add((sc.exit.label, outBy));
                foreach (var (l, o) in scPenalties) { AddPenalty(l, o); Note("near miss", $"near miss of {l} ({o:0.0} m outside, +{shared.nearMissPenalty:0} s)"); }
                scPenalties.Clear();
                OnShortcut = null; ShortcutsTaken.Add(sc.label);
                if (sc.replacesGate >= 0 && GatesPassed == sc.replacesGate)
                    for (int k = 0; k < sc.replacesCount && GatesPassed < Course.gates.Count - 1; k++)   // Round 17: one or more gates in a row
                    {
                        var g = Course.gates[GatesPassed]; GatesPassed++; GateLog.Add(g.index); MissedGate = null; missedFor = 0;
                        Note("gate", $"gate {g.label} passed via {sc.label} ({GatesPassed}/{Course.gates.Count})");
                    }
                Note("shortcut", $"shortcut {sc.label}: out ({Mode}, {Speed:0} m/s)");
                Progress = Course.Project(p, sc.mainTo, out at, out dir);
                return;
            }
            bool backOut = sc.entry.CrossedBackward(lastPos, p);
            if (backOut || scOffFor > 1f)
            {
                if (sc.flight) { leftBranch = sc; leftBranchAt = Time.time; }   // Round 12: a reset soon after still goes forward (Recover)
                OnShortcut = null; scPenalties.Clear();
                Note("shortcut", $"shortcut {sc.label}: left without finishing it ({(backOut ? "back out through the entry" : $"{off:0} m off its line")})");
                Progress = Course.Project(p, sc.mainFrom, out at, out dir);
            }
        }
    }
}
