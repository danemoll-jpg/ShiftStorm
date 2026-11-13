using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // A course: its center line (with the height a racer aims for: road, flight, water, depth), ordered gates, recovery anchors, bounds and
    // (Paradise Plunge) its named segments with target times. Progress is the distance along the center line of the nearest point, never the
    // distance to the finish or the height.
    // Round 2: stacked route sections (Paradise Plunge's flooded cave runs under the finishing road) make a plain nearest-point search
    // ambiguous, so a racer's progress is searched near its last known progress (a window along the line) and in 3D. Only a racer with no
    // known progress yet (or a Round 1 course) uses the old whole-line search on the ground plan.
    public sealed class Course : MonoBehaviour
    {
        public string courseName;
        public List<Vector3> line = new List<Vector3>();
        public List<ProgressGate> gates = new List<ProgressGate>();
        public List<RecoveryAnchor> anchors = new List<RecoveryAnchor>();
        public List<Segment> segments = new List<Segment>();
        public float halfWidth = 45, minY = -30, maxY = 60, endMargin = 30;
        public Mode startMode = Mode.Drive;
        public bool timeTrial;                  // countdown, lap timer, splits, results (Paradise Plunge); the test courses start at once
        public string look = "";                // "jurassic": Paradise Plunge's look and sound (JurassicLook); "" = the Round 1 gray boxes
        public float windowBack = 60, windowAhead = 140;
        float[] cumulative;
        public float Length { get { Prepare(); return cumulative[cumulative.Length - 1]; } }

        // A named stretch of the lap: it ends when the racer's progress reaches endProgress with at least gatesNeeded gates passed.
        [System.Serializable]
        public sealed class Segment { public string name, form; public float target, endProgress; public int gatesNeeded; }

        void Prepare()
        {
            if (cumulative != null && cumulative.Length == line.Count) return;
            cumulative = new float[line.Count];
            for (int i = 1; i < line.Count; i++) cumulative[i] = cumulative[i - 1] + Vector3.Distance(line[i - 1], line[i]);
        }
        // Gates and anchors placed by progress (Paradise Plunge) keep it; others (Round 1) are projected. Gates and anchors are then sorted.
        public void Finish()
        {
            cumulative = null; Prepare();
            foreach (var g in gates) if (g.progress <= 0) g.progress = Project(g.transform.position, out _, out _);
            foreach (var a in anchors) if (a.progress <= 0) a.progress = Project(a.transform.position, out _, out _);
            anchors.Sort((x, y) => x.progress.CompareTo(y.progress)); gates.Sort((x, y) => x.index.CompareTo(y.index));
        }
        public float CumulativeAt(int i) { Prepare(); return cumulative[Mathf.Clamp(i, 0, cumulative.Length - 1)]; }

        // Whole-line search on the ground plan (Round 1 behaviour): only for a point with no known progress on a course without stacking.
        public float Project(Vector3 p, out Vector3 at, out Vector3 dir) => Project(p, -1f, out at, out dir);

        // Progress (m) of the nearest center line point; at: that point; dir: the line's direction there. hint >= 0: search only the window
        // [hint - windowBack, hint + windowAhead] and measure in 3D, so a racer in the cave is never matched to the road above it.
        public float Project(Vector3 p, float hint, out Vector3 at, out Vector3 dir)
        {
            Prepare(); float best = float.MaxValue, prog = 0; at = line[0]; dir = Vector3.forward;
            int i0 = 1, i1 = line.Count - 1; bool windowed = hint >= 0;
            if (windowed) { i0 = Mathf.Max(1, IndexAt(hint - windowBack)); i1 = Mathf.Min(line.Count - 1, IndexAt(hint + windowAhead) + 1); }
            for (int i = i0; i <= i1; i++)
            {
                Vector3 a = line[i - 1], b = line[i], ab = b - a; float t; Vector3 q; float d;
                if (windowed)
                {
                    t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f)); q = a + ab * t; d = (p - q).sqrMagnitude;
                }
                else
                {
                    var flat = new Vector3(ab.x, 0, ab.z);
                    t = Mathf.Clamp01(Vector3.Dot(new Vector3(p.x - a.x, 0, p.z - a.z), flat) / Mathf.Max(flat.sqrMagnitude, 1e-4f));
                    q = a + ab * t; d = new Vector2(p.x - q.x, p.z - q.z).sqrMagnitude;
                }
                if (d < best - 1e-3f) { best = d; prog = cumulative[i - 1] + ab.magnitude * t; at = q; dir = ab.normalized; }
            }
            return prog;
        }
        int IndexAt(float progress)
        {
            int lo = 0, hi = cumulative.Length - 1;
            if (progress <= 0) return 0; if (progress >= cumulative[hi]) return hi;
            while (hi - lo > 1) { int mid = (lo + hi) / 2; if (cumulative[mid] <= progress) lo = mid; else hi = mid; }
            return lo;
        }
        public Vector3 PointAt(float progress, out Vector3 dir)
        {
            Prepare(); dir = Vector3.forward;
            int i = Mathf.Clamp(IndexAt(progress) + 1, 1, line.Count - 1);
            float seg = cumulative[i] - cumulative[i - 1]; float t = seg > 0 ? Mathf.Clamp01((progress - cumulative[i - 1]) / seg) : 0;
            dir = (line[i] - line[i - 1]).normalized; return Vector3.Lerp(line[i - 1], line[i], t);
        }
        public bool OutOfBounds(Vector3 p, float hint = -1f)
        {
            float prog = Project(p, hint, out var at, out _);
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
        // Race position measure (concept: "completed main gates plus normalized progress along the current branch"): gates passed plus the
        // fraction of the way from the last gate passed to the next one (0 .. 0.999). Never distance to the finish or height.
        public float RaceDistance(int gatesPassed, float progress)
        {
            if (gates.Count == 0) return progress / Mathf.Max(1, Length);
            if (gatesPassed >= gates.Count) return gates.Count;
            float from = gatesPassed > 0 ? gates[gatesPassed - 1].progress : 0, to = gates[gatesPassed].progress;
            return gatesPassed + Mathf.Clamp(to > from ? (progress - from) / (to - from) : 0, 0, .999f);
        }
    }
}
