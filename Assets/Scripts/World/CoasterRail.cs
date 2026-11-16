using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 10 item 4: a roller-coaster rail of any shape (Coaster Coast: drops, banked turns, a loop, a corkscrew, a lift hill), the
    // Round 7 loop's rail made general. A dense list of points along the track (the car's centre on it) with the car's up at each (the
    // track's banking; towards the loop's centre upside down). A car crossing its entry plane rides it (ShiftVehicle.Rail): locked to the
    // track like a coaster car, so it can never fall off or be stranded on it; its speed along the track comes from its own drive force,
    // boost and real gravity along the track; booster tyres keep it above the section's minimum speed (it never stalls on a climb, even
    // coasting from a crawl); at the end it leaves the track with its velocity (a jump's lip, the top hat's launch into flight).
    public sealed class CoasterRail : MonoBehaviour
    {
        public static readonly List<CoasterRail> All = new List<CoasterRail>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public string label = "rail";
        public float capture = 14f;      // the entry plane catches a car this far either side of the track's centre (the station is no wider)
        public float maxSpeed = 105f;    // a cap (a long drop on boost)
        public float entryProgress, exitProgress;
        Vector3[] p, up, tan; float[] cum;
        readonly List<Vector3> boosters = new List<Vector3>();   // (from s, to s, min speed)
        public int Count => p != null ? p.Length : 0;
        public float Length => cum != null && cum.Length > 0 ? cum[cum.Length - 1] : 0;
        public Vector3 F0 => tan[0];
        public Vector3 Start => p[0];
        public Vector3 End => p[p.Length - 1];

        public void Build(List<Vector3> pts, List<Vector3> ups)
        {
            p = pts.ToArray(); up = ups.ToArray(); int n = p.Length; cum = new float[n]; tan = new Vector3[n];
            for (int i = 1; i < n; i++) cum[i] = cum[i - 1] + Vector3.Distance(p[i - 1], p[i]);
            for (int i = 0; i < n; i++) { var d = p[Mathf.Min(n - 1, i + 1)] - p[Mathf.Max(0, i - 1)]; tan[i] = d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward; }
            for (int i = 0; i < n; i++) up[i] = Vector3.ProjectOnPlane(up[i], tan[i]).normalized;
        }
        public void Booster(float from, float to, float minSpeed) => boosters.Add(new Vector3(from, to, minSpeed));
        public float MinSpeedAt(float s) { float m = 0; foreach (var b in boosters) if (s >= b.x && s <= b.y) m = Mathf.Max(m, b.z); return m; }

        int Index(float s)
        {
            int lo = 0, hi = cum.Length - 1; if (s <= 0) return 0; if (s >= cum[hi]) return hi - 1;
            while (hi - lo > 1) { int mid = (lo + hi) / 2; if (cum[mid] <= s) lo = mid; else hi = mid; }
            return lo;
        }
        public void Sample(float s, out Vector3 pos, out Vector3 t, out Vector3 n)
        {
            int i = Index(s); int j = Mathf.Min(i + 1, p.Length - 1); float seg = cum[j] - cum[i]; float u = seg > 1e-5f ? Mathf.Clamp01((s - cum[i]) / seg) : 0;
            pos = Vector3.Lerp(p[i], p[j], u); t = Vector3.Slerp(tan[i], tan[j], u).normalized; n = Vector3.ProjectOnPlane(Vector3.Slerp(up[i], up[j], u), t).normalized;
        }
        public Vector3 PointAt(float s) { Sample(s, out var q, out _, out _); return q; }
        // forward through the entry plane at the track's start, near it
        public bool Entered(Vector3 from, Vector3 to, out float lateral)
        {
            lateral = 0; var F = tan[0]; float a = Vector3.Dot(from - p[0], F), b = Vector3.Dot(to - p[0], F);
            if (!(a < 0 && b >= 0)) return false;
            var hit = Vector3.Lerp(from, to, -a / (b - a)); var right = Vector3.Cross(up[0], F).normalized; lateral = Vector3.Dot(hit - p[0], right);
            // Round 12: up to 9 m over the track (build 13: 4.5 m either way): a car down from the Midway flight is still a few metres up at
            // the station's entry plane, missed the rail, ran on along the boardwalk and was reset "out of bounds" 76 m off the line (Round 10 /
            // 11's known reset after the Midway flight: 19 in the Round 11 soak)
            float h = Vector3.Dot(hit - p[0], up[0]);
            return Mathf.Abs(lateral) < capture && h > -4.5f && h < (ShiftVehicle.OldFlight ? 4.5f : 9f);
        }
    }
}
