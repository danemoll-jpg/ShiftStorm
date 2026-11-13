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
                OnShortcut = null; scPenalties.Clear();
                Note("shortcut", $"shortcut {sc.label}: left without finishing it ({(backOut ? "back out through the entry" : $"{off:0} m off its line")})");
                Progress = Course.Project(p, sc.mainFrom, out at, out dir);
            }
        }
    }
}
