using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 3 item 5: a shortcut, fixed geometry between two main gates, with its own entry and exit sub-gates and its own line. A racer
    // that crosses the entry (forward, in its frame, in a permitted form, with nextGate as its next main gate) is on the shortcut: its progress
    // is measured along the shortcut's line and mapped onto the main line's progress between mainFrom and mainTo, so race position, splits
    // and recovery keep working. Crossing the exit ends it; if the shortcut stands in for a main gate (replacesGate: the Sky Arch slit
    // replaces G3 arch exit), that gate counts then. Leaving the shortcut's space (well off its line for a moment, or back out through the
    // entry) ends it without credit: the racer is back on the main line's progress and the normal cues and recovery apply.
    public sealed class Shortcut : MonoBehaviour
    {
        public string label;
        public List<Vector3> line = new List<Vector3>();
        public ProgressGate entry, exit;
        public int nextGate;            // the main gate the racer must be heading for when it enters
        public int replacesGate = -1;   // the main gate the shortcut stands in for (-1: none, both paths still pass the next main gate)
        public float mainFrom, mainTo;  // main-line progress where the shortcut leaves and rejoins
        public float halfWidth = 12;    // how far from its line a racer may be and still be on it
        public bool choice;             // Round 7: the choice jump's flight branch (the AI decides it with AiSkill.fly, and opens its wings for it)
        public readonly List<(float s, RecoveryAnchor a)> ownAnchors = new List<(float, RecoveryAnchor)>();   // Round 13: anchors on its own line (the Mangrove bypass: a reset in it restarts in it)
        public bool flight;             // Round 12: a flight branch (a reset on it, or just off it, goes forward to its rejoin: ShiftVehicle.Recover)
        float[] cum;
        // Round 16 item 1: the rings / hoops along it and its exit (distance along its line): the HUD arrow points at the next one ahead
        public readonly List<float> rings = new List<float>();
        public Vector3 GuideAt(float s, float ahead) { foreach (var r in rings) if (r > s + 6f) return PointAt(r, out _); return PointAt(s + ahead, out _); }

        public float Length { get { Prep(); return cum[cum.Length - 1]; } }
        void Prep()
        {
            if (cum != null && cum.Length == line.Count) return;
            cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Vector3.Distance(line[i - 1], line[i]);
        }
        public float MainProgress(float s) => Mathf.Lerp(mainFrom, mainTo, Mathf.Clamp01(s / Mathf.Max(1, Length)));

        // distance along the shortcut's line of the nearest point (3D, searched near the hint)
        public float Project(Vector3 p, float hint, out Vector3 at, out Vector3 dir)
        {
            Prep(); float best = float.MaxValue, s = 0; at = line[0]; dir = Vector3.forward;
            for (int i = 1; i < line.Count; i++)
            {
                if (hint >= 0 && (cum[i] < hint - 80 || cum[i - 1] > hint + 160)) continue;
                Vector3 a = line[i - 1], b = line[i], ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                var q = a + ab * t; float d = (p - q).sqrMagnitude;
                if (d < best) { best = d; s = cum[i - 1] + ab.magnitude * t; at = q; dir = ab.normalized; }
            }
            return s;
        }
        public Vector3 PointAt(float s, out Vector3 dir)
        {
            Prep(); dir = Vector3.forward; int i = 1;
            while (i < line.Count - 1 && cum[i] < s) i++;
            float seg = cum[i] - cum[i - 1], t = seg > 0 ? Mathf.Clamp01((s - cum[i - 1]) / seg) : 0;
            dir = (line[i] - line[i - 1]).normalized; return Vector3.Lerp(line[i - 1], line[i], t);
        }
        // only the hard limits apply on a shortcut (its own walls hold the racer); hard is always true when out
        public bool OutOfBounds(Vector3 p, Course c, out bool hard) { hard = p.y < c.minY || p.y > c.maxY; return hard; }

        // builds a smooth line through control points (Catmull-Rom, ~3 m steps), as the main line is built
        public static List<Vector3> Smooth(IList<Vector3> pts)
        {
            var o = new List<Vector3>(); int n = pts.Count;
            Vector3 P(int i) => pts[Mathf.Clamp(i, 0, n - 1)];
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
                int steps = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(p1, p2) / 3f));
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)steps, t2 = t * t, t3 = t2 * t;
                    o.Add(.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3));
                }
            }
            o.Add(P(n - 1)); return o;
        }
    }
}
