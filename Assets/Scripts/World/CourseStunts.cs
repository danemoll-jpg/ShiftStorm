using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 10: what the AI, the check pilot, the HUD's route strip and TeedOff Barker need to know about a course's stunts, per course
    // (build 11 asked ParadisePlunge directly, so only course 0 had any). Paradise Plunge: its jumps and the loop; Coaster Coast: its
    // jumps and the two coaster rails.
    public abstract class CourseStunts
    {
        public struct Jump { public string name; public float lip, gap; public bool choice; }
        public virtual IReadOnlyList<Jump> Jumps => System.Array.Empty<Jump>();
        // stretches the route strip shows as rails / loops (progress from, to)
        public virtual IReadOnlyList<Vector2> Rails => System.Array.Empty<Vector2>();
        // the AI keeps exactly to the line here (no lane, no passing step): stunts, form changes, gates, narrow pieces
        public virtual bool Narrow(float p, Mode mode) => false;
        // no braking for the line's bends between these (the loop / a rail turns over itself; the car rides it)
        public virtual bool NoBrake(float from, float to) => false;
        // the aim on the way into a loop / rail: straight on along its entry lane (not up into the rail's own line)
        public virtual bool EntryAim(float p, float ahead, out Vector3 aim) { aim = default; return false; }
        // the landmark name for a progress (TeedOff Barker's "where" lines), "" if none
        public virtual string Where(float p) => "";
    }

    public sealed class ParadiseStunts : CourseStunts
    {
        readonly List<Jump> jumps = new List<Jump>(); readonly List<Vector2> rails = new List<Vector2>();
        public ParadiseStunts()
        {
            foreach (var j in ParadisePlunge.Jumps) jumps.Add(new Jump { name = j.name, lip = j.lip, gap = j.gap, choice = j.choice });
            var m = ParadisePlunge.Marks; if (m.ContainsKey("loopIn")) rails.Add(new Vector2(m["loopIn"], m["loopOut"]));
        }
        public override IReadOnlyList<Jump> Jumps => jumps;
        public override IReadOnlyList<Vector2> Rails => rails;
        public override bool Narrow(float p, Mode mode)
        {
            var m = ParadisePlunge.Marks; if (m.Count == 0) return true;
            if (ParadisePlunge.StuntNear(p)) return true;   // Round 7: on its line into the loop (7 m half) and over the jumps
            foreach (var k in new[] { "lip", "splash", "diveZone", "portal", "surface", "rampFoot", "rampLip", "landing", "G1", "G5", "G7", "G8", "G9", "finish" })
                if (Mathf.Abs(p - m[k]) < 45) return true;
            if (p > m["rampStart"] - 20 && p < m["lip"]) return true;
            if (p > m["poolEdge"] - 20 && p < m["G9"] + 10) return true;
            return false;
        }
        public override bool NoBrake(float from, float to) => ParadisePlunge.NoBrake(from, to);
        public override bool EntryAim(float p, float ahead, out Vector3 aim) => ParadisePlunge.LoopAim(p, ahead, out aim);
    }
}
