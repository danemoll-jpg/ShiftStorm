using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // A course: its center line (with the height a racer aims for: road, flight, water, depth), ordered gates, recovery anchors and bounds.
    // Progress is the distance along the center line of the nearest point, never the distance to the finish or the height.
    public sealed class Course : MonoBehaviour
    {
        public string courseName;
        public List<Vector3> line = new List<Vector3>();
        public List<ProgressGate> gates = new List<ProgressGate>();
        public List<RecoveryAnchor> anchors = new List<RecoveryAnchor>();
        public float halfWidth = 45, minY = -30, maxY = 60, endMargin = 30;
        public Mode startMode = Mode.Drive;
        float[] cumulative;
        public float Length { get { Prepare(); return cumulative[cumulative.Length - 1]; } }

        void Prepare()
        {
            if (cumulative != null && cumulative.Length == line.Count) return;
            cumulative = new float[line.Count];
            for (int i = 1; i < line.Count; i++) cumulative[i] = cumulative[i - 1] + Vector3.Distance(line[i - 1], line[i]);
        }
        public void Finish()
        {
            cumulative = null; Prepare();
            foreach (var g in gates) g.progress = Project(g.transform.position, out _, out _);
            foreach (var a in anchors) a.progress = Project(a.transform.position, out _, out _);
            anchors.Sort((x, y) => x.progress.CompareTo(y.progress)); gates.Sort((x, y) => x.index.CompareTo(y.index));
        }
        // Progress (m) of the nearest center line point; at: that point; dir: the line's direction there.
        public float Project(Vector3 p, out Vector3 at, out Vector3 dir)
        {
            Prepare(); float best = float.MaxValue, prog = 0; at = line[0]; dir = Vector3.forward;
            for (int i = 1; i < line.Count; i++)
            {
                Vector3 a = line[i - 1], b = line[i], ab = b - a; var flat = new Vector3(ab.x, 0, ab.z);
                float t = Mathf.Clamp01(Vector3.Dot(new Vector3(p.x - a.x, 0, p.z - a.z), flat) / Mathf.Max(flat.sqrMagnitude, 1e-4f));
                var q = a + ab * t; float d = new Vector2(p.x - q.x, p.z - q.z).sqrMagnitude;
                if (d < best - 1e-3f) { best = d; prog = cumulative[i - 1] + ab.magnitude * t; at = q; dir = ab.normalized; }
            }
            return prog;
        }
        public Vector3 PointAt(float progress, out Vector3 dir)
        {
            Prepare(); dir = Vector3.forward;
            for (int i = 1; i < line.Count; i++)
                if (progress <= cumulative[i] || i == line.Count - 1)
                {
                    float seg = cumulative[i] - cumulative[i - 1]; float t = seg > 0 ? Mathf.Clamp01((progress - cumulative[i - 1]) / seg) : 0;
                    dir = (line[i] - line[i - 1]).normalized; return Vector3.Lerp(line[i - 1], line[i], t);
                }
            return line[line.Count - 1];
        }
        public bool OutOfBounds(Vector3 p)
        {
            float prog = Project(p, out var at, out _);
            return p.y < minY || p.y > maxY || new Vector2(p.x - at.x, p.z - at.z).magnitude > halfWidth
                || (prog <= 0.01f && Vector3.Distance(p, line[0]) > endMargin) || (prog >= Length - 0.01f && Vector3.Distance(p, line[line.Count - 1]) > endMargin);
        }
        // The last anchor at or behind both the best progress reached and the next unearned gate: never forward past an unearned checkpoint.
        public RecoveryAnchor AnchorFor(float bestProgress, int gatesPassed)
        {
            float limit = bestProgress;
            if (gatesPassed < gates.Count) limit = Mathf.Min(limit, gates[gatesPassed].progress);
            RecoveryAnchor pick = anchors.Count > 0 ? anchors[0] : null;
            foreach (var a in anchors) if (a.progress <= limit + .5f) pick = a;
            return pick;
        }
    }
}
