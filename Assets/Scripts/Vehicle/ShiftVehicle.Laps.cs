using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 17 item 3 (Dan: "the laps being one lap doesn't really give you a chance to savor the courses"): 1-5 laps on every course.
    // Paradise Plunge, Coaster Coast and Big Top Breakdown finish exactly where their G0 stands (the finish road runs into the start), so a
    // lap is counted here, per racer: crossing the finish gate on a lap that is not the last puts the racer on the next lap at once (G0 is
    // the same place: it counts too), its progress back at the start of the line and its gates from G1 again. Race order = laps done x gates +
    // gates this lap (RaceDistance); the splits run on per lap (Splits holds every lap's segments, LapSplitBase = this lap's first); LapTimes
    // the time of each lap. Dam Shame's line already is its three laps end to end (Round 16: its water rises by lap), so it keeps that
    // (Course.laps = 3, no own laps). The lap count of a race is Course.raceLaps, set by Game when a race starts (Race / 2 Players: the
    // choose-your-racer page's Laps row, default 3; The Tour and Stunt Show 3; Time Trial and the rival challenges 1).
    public sealed partial class ShiftVehicle
    {
        public int Lap { get; private set; } = 1;
        public int LapsTotal => Course ? (Course.laps > 1 ? Course.laps : Mathf.Clamp(Course.raceLaps, 1, 5)) : 1;
        public bool OwnLaps => Course && Course.laps <= 1 && LapsTotal > 1;
        public int LapNow => !Course ? 1 : OwnLaps ? Lap : Course.LapAt(Progress);
        public int LapSplitBase { get; private set; }
        public readonly List<float> LapTimes = new List<float>();
        float lapStartedAt;
        // every gate of the race (all laps): the race order and "how far round" (the HUD, Barnaby's close-finish call)
        public int RaceGates => Course ? Course.gates.Count * (OwnLaps ? LapsTotal : 1) : 1;

        void ResetLaps() { Lap = 1; LapSplitBase = 0; LapTimes.Clear(); lapStartedAt = 0; }

        // the finish gate on a lap that is not the last: the next lap (true: handled, the racer races on)
        bool NextLap(ProgressGate finishGate)
        {
            if (!OwnLaps || Lap >= LapsTotal || Finished) return false;
            var segs = Course.segments; var p = Body.position;
            float now = Time.time - StartTime;
            while (Splits.Count - LapSplitBase < segs.Count) Splits.Add(now);   // this lap's last segment ends here
            LapTimes.Add(now - lapStartedAt); lapStartedAt = now;
            Lap++; LapSplitBase = Splits.Count;
            // G0 stands where the finish is: it counts at once; progress back at the start of the line, searched near G0
            GatesPassed = 1; GateLog.Add(Course.gates[0].index); MissedGate = null; missedFor = 0;
            Progress = Course.Project(p, Course.gates[0].progress, out _, out _); BestProgress = lastBest = Progress; noProgressFor = 0;
            OnShortcut = null; leftBranch = null; LastZone = null;
            Note("lap", $"lap {Lap} of {LapsTotal} (lap {Lap - 1}: {LapTimes[LapTimes.Count - 1]:0.00} s)");
            ShowCue(Lap == LapsTotal ? "FINAL LAP!" : $"LAP {Lap} / {LapsTotal}", CueKind.Guide, 2f);
            return true;
        }
        void FinishLaps() { if (OwnLaps || LapsTotal <= 1) LapTimes.Add(Time.time - StartTime - lapStartedAt); }
        public float BestLap { get { float b = -1; foreach (var t in LapTimes) if (t > 0 && (b < 0 || t < b)) b = t; return b; } }
    }
}
