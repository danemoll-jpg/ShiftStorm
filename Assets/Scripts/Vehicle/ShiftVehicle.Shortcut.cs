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

        void EnterShortcut(Shortcut sc, Vector3 p)
        {
            OnShortcut = sc; scOffFor = 0; scS = sc.Project(p, -1, out _, out _);
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
            if (sc.exit.Crossed(lastPos, p))
            {
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
                OnShortcut = null;
                Note("shortcut", $"shortcut {sc.label}: left without finishing it ({(backOut ? "back out through the entry" : $"{off:0} m off its line")})");
                Progress = Course.Project(p, sc.mainFrom, out at, out dir);
            }
        }
    }
}
