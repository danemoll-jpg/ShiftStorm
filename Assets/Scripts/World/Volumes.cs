using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Boxes tested by point (no trigger callbacks), so every check is deterministic and order-free.
    public abstract class BoxVolume : MonoBehaviour
    {
        public Vector3 size = new Vector3(10, 10, 10);
        public bool Contains(Vector3 world)
        {
            var p = transform.InverseTransformPoint(world);
            return Mathf.Abs(p.x) <= size.x * .5f && Mathf.Abs(p.y) <= size.y * .5f && Mathf.Abs(p.z) <= size.z * .5f;
        }
        public bool ContainsXZ(Vector3 world) { var l = transform.InverseTransformPoint(world); return Mathf.Abs(l.x) <= size.x * .5f && Mathf.Abs(l.z) <= size.z * .5f; }
        protected virtual Color GizmoColor => Color.cyan;
        void OnDrawGizmos() { Gizmos.matrix = transform.localToWorldMatrix; Gizmos.color = GizmoColor; Gizmos.DrawWireCube(Vector3.zero, size); }
    }

    // A body of water: its top face is the surface. Volumes may overlap (the lagoon and the cave do at the dive portal); being in water never
    // changes the form by itself: only TransformZones do.
    public sealed class WaterVolume : BoxVolume
    {
        public static readonly List<WaterVolume> All = new List<WaterVolume>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public float Surface => transform.position.y + size.y * .5f;
        public float Floor => transform.position.y - size.y * .5f;
        // The highest water surface over this point (of the volumes it is inside or just above), or false when there is none.
        public static bool SurfaceAt(Vector3 p, out float surface, float above = 3f)
        {
            surface = float.NegativeInfinity; bool any = false;
            foreach (var w in All)
                if (w.ContainsXZ(p) && p.y <= w.Surface + above && p.y >= w.Floor - 1f && w.Surface > surface) { surface = w.Surface; any = true; }
            return any;
        }
        public static bool Inside(Vector3 p) { foreach (var w in All) if (w.Contains(p)) return true; return false; }
        protected override Color GizmoColor => new Color(0, .5f, 1, .6f);
    }

    // A form change zone. Fires once on entry (moving along its forward), then a new valid zone is needed before another change can fire: the
    // vehicle remembers the last zone that fired, and that zone never fires again until a reset clears the memory.
    public sealed class TransformZone : BoxVolume
    {
        public static readonly List<TransformZone> All = new List<TransformZone>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public Mode target;
        public bool landingCorridor;   // water under this zone is a valid landing; a car or plane in any other water is reset
        public string label;
        public bool MovingThrough(Vector3 velocity) => Vector3.Dot(velocity, transform.forward) > 0f;
        public static bool InCorridor(Vector3 p) { foreach (var z in All) if (z.landingCorridor && z.ContainsXZ(p)) return true; return false; }
        protected override Color GizmoColor => target == Mode.Fly ? Color.white : target == Mode.Boat ? Color.cyan : target == Mode.Dive ? Color.blue : Color.yellow;
    }

    // A current: pushes boats and submarines along its direction (acceleration, m/s^2).
    public sealed class CurrentZone : BoxVolume
    {
        public static readonly List<CurrentZone> All = new List<CurrentZone>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public Vector3 push = new Vector3(4, 0, 0);
        public static Vector3 PushAt(Vector3 p) { var sum = Vector3.zero; foreach (var c in All) if (c.Contains(p)) sum += c.push; return sum; }
    }

    // Round 3: an updraft funnelled through the Sky Arch slit: a plane inside it is lifted to at least this airspeed (m/s), which then eases
    // back to its normal speed as usual.
    public sealed class SlipstreamZone : BoxVolume
    {
        public static readonly List<SlipstreamZone> All = new List<SlipstreamZone>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public float speed = 108;
        public static float SpeedAt(Vector3 p) { float s = 0; foreach (var z in All) if (z.Contains(p)) s = Mathf.Max(s, z.speed); return s; }
    }

    // A ramp a boat can climb on its hull (the surfacing pool's water ramp): full thrust and a hull hover, instead of the dry ground drag.
    public sealed class WetRamp : MonoBehaviour { }

    // A safe place to restart: position, facing and the form to restart in. Course.AnchorFor picks the last one earned.
    public sealed class RecoveryAnchor : MonoBehaviour
    {
        public Mode mode;
        [System.NonSerialized] public float progress;   // set when placed by progress (Paradise Plunge), else projected by Course.Finish
        void OnDrawGizmos() { Gizmos.color = Color.green; Gizmos.DrawSphere(transform.position, .6f); Gizmos.DrawRay(transform.position, transform.forward * 4); }
    }

    // A race-progress gate, separate from the form change zones (crossing water is not proof that a section was done). Passed in order only,
    // forward only (its +Z), inside its frame, and (Round 2) only in a permitted form: a plane over the flooded cave never earns the dive gates.
    public sealed class ProgressGate : MonoBehaviour
    {
        public int index; public Vector2 halfSize = new Vector2(30, 25); public string label;
        public Mode[] modes;   // permitted forms (null or empty: any)
        [System.NonSerialized] public float progress;
        public bool Crossed(Vector3 from, Vector3 to) => Through(from, to, false);
        public bool CrossedBackward(Vector3 from, Vector3 to) => Through(from, to, true);
        public bool Allows(Mode m) => modes == null || modes.Length == 0 || System.Array.IndexOf(modes, m) >= 0;
        bool Through(Vector3 from, Vector3 to, bool backward)
        {
            Vector3 a = transform.InverseTransformPoint(from), b = transform.InverseTransformPoint(to);
            if (backward ? !(a.z >= 0 && b.z < 0) : !(a.z < 0 && b.z >= 0)) return false;
            float t = -a.z / (b.z - a.z); var hit = Vector3.Lerp(a, b, t);
            return Mathf.Abs(hit.x) <= halfSize.x && Mathf.Abs(hit.y) <= halfSize.y;
        }
    }
}
