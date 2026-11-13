using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Poses the placeholder vehicle's separate parts for each form and animates a change between two forms (smoothstep over the change time).
    // Every part's offset is in the vehicle's own space (X right, Y up, Z forward) from the part's rest transform, which is how
    // Tools/Blender/shiftstorm_vehicle.py builds it: wheels down (Drive), wings and tail out (Fly), hull fins, keel and propeller down (Boat),
    // canopy closed. Left / right come from the rest position's side, not from the name, so a mirrored import still folds the right way.
    // The parts are visual only: the hull collider is the same for every form (ShiftVehicle), so a half-open wing never catches anything.
    public sealed class FormAnimator : MonoBehaviour
    {
        sealed class Part { public string name; public Transform t; public Vector3 pos; public Quaternion rot; public Vector3 scale; public float side; public Vector3 fromPos, toPos, fromScale, toScale; public Quaternion fromRot, toRot, lastPose = Quaternion.identity; }
        readonly List<Part> parts = new List<Part>();
        public Mode Current { get; private set; }
        public Mode Target { get; private set; }
        public bool Done { get; private set; } = true;
        public float StartedAt { get; private set; }
        public float VisualDoneAt { get; private set; }
        public float Seconds { get; private set; }
        public float T { get; private set; } = 1;
        float wheelSpin, propSpin; ShiftVehicle vehicle;

        public static readonly string[] PartNames = { "Body", "Cockpit", "Canopy", "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR", "Wing_L", "Wing_R", "Tail", "Fin_L", "Fin_R", "Keel", "Prop" };

        // Collect the parts under 'model' and re-parent them under this transform (keeping where they are), so their parent space is the vehicle's.
        public void Setup(Transform model)
        {
            vehicle = GetComponentInParent<ShiftVehicle>();
            parts.Clear();
            var found = new Dictionary<string, Transform>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                foreach (var n in PartNames) if (t.name == n && !found.ContainsKey(n)) found[n] = t;
            foreach (var n in PartNames)
            {
                if (!found.TryGetValue(n, out var t)) { Debug.LogWarning("[Forms] missing part " + n); continue; }
                t.SetParent(transform, true);
                parts.Add(new Part { name = n, t = t, pos = t.localPosition, rot = t.localRotation, scale = t.localScale, side = Mathf.Sign(t.localPosition.x == 0 ? 1 : t.localPosition.x) });
            }
            if (model != transform && model.childCount == 0) Destroy(model.gameObject);
            Snap(Mode.Drive);
        }
        public int PartCount => parts.Count;

        // the pose of one part in one form: offset (vehicle space), rotation (vehicle space, applied after the rest rotation), scale factor
        static void Pose(Part p, Mode m, out Vector3 dPos, out Vector3 euler, out Vector3 scale)
        {
            dPos = Vector3.zero; euler = Vector3.zero; scale = Vector3.one; float s = p.side;
            switch (p.name)
            {
                case "Wheel_FL": case "Wheel_FR": case "Wheel_RL": case "Wheel_RR":
                    if (m != Mode.Drive) { dPos = new Vector3(-s * .28f, .38f, 0); euler = new Vector3(0, 0, 90 * s); scale = Vector3.one * .8f; }
                    break;
                case "Wing_L": case "Wing_R":
                    if (m == Mode.Drive || m == Mode.Boat) { euler = new Vector3(0, 86 * s, 0); scale = new Vector3(.42f, .9f, .45f); dPos = new Vector3(0, .12f, 0); }
                    else if (m == Mode.Dive) { euler = new Vector3(0, 55 * s, 0); scale = Vector3.one * .5f; }
                    break;
                case "Tail":
                    if (m == Mode.Drive || m == Mode.Boat) { euler = new Vector3(80, 0, 0); scale = Vector3.one * .7f; }
                    else if (m == Mode.Dive) scale = Vector3.one * .65f;
                    break;
                case "Fin_L": case "Fin_R":
                    if (m == Mode.Drive || m == Mode.Fly) { euler = new Vector3(0, 0, 90 * s); scale = Vector3.one * .3f; }
                    break;
                case "Keel":
                    if (m == Mode.Drive || m == Mode.Fly) { scale = new Vector3(1, .08f, .6f); dPos = new Vector3(0, .25f, 0); }
                    break;
                case "Prop":
                    if (m == Mode.Drive) { dPos = new Vector3(0, .1f, .45f); scale = Vector3.one * .15f; }
                    break;
                case "Canopy":
                    if (m == Mode.Drive || m == Mode.Boat) euler = new Vector3(-22, 0, 0);
                    break;
            }
        }
        void Target_(Part p, Mode m, out Vector3 pos, out Quaternion rot, out Vector3 scale)
        {
            Pose(p, m, out var d, out var e, out var k);
            pos = p.pos + d; rot = Quaternion.Euler(e) * p.rot; scale = Vector3.Scale(p.scale, k);
        }

        public void Snap(Mode m)
        {
            Current = Target = m; Done = true; T = 1; Seconds = 0; VisualDoneAt = Time.time;
            foreach (var p in parts) { Target_(p, m, out p.toPos, out p.toRot, out p.toScale); p.fromPos = p.toPos; p.fromRot = p.toRot; p.fromScale = p.toScale; Apply(p, 1); }
        }

        public void Begin(Mode m, float seconds)
        {
            foreach (var p in parts) { p.fromPos = p.t.localPosition; p.fromRot = Unspun(p); p.fromScale = p.t.localScale; Target_(p, m, out p.toPos, out p.toRot, out p.toScale); }
            Current = Target; Target = m; Seconds = seconds; StartedAt = Time.time; T = 0; Done = false;
        }

        Quaternion Unspun(Part p) => p.lastPose;
        void Apply(Part p, float k)
        {
            float e = k * k * (3 - 2 * k);
            p.t.localPosition = Vector3.Lerp(p.fromPos, p.toPos, e);
            p.t.localScale = Vector3.Lerp(p.fromScale, p.toScale, e);
            var pose = Quaternion.Slerp(p.fromRot, p.toRot, e); p.lastPose = pose;
            float spin = p.name.StartsWith("Wheel") ? wheelSpin : p.name == "Prop" ? propSpin : 0;
            p.t.localRotation = spin != 0 ? pose * Quaternion.Inverse(p.rot) * Quaternion.AngleAxis(spin, p.name == "Prop" ? Vector3.forward : Vector3.right) * p.rot : pose;
        }

        void LateUpdate()
        {
            if (vehicle && vehicle.Body)
            {
                float fwd = Vector3.Dot(vehicle.Body.linearVelocity, vehicle.transform.forward);
                if (Target == Mode.Drive) wheelSpin = (wheelSpin + fwd / .42f * Mathf.Rad2Deg * Time.deltaTime) % 360f;
                if (Target != Mode.Drive) propSpin = (propSpin + (300 + Mathf.Abs(fwd) * 40) * Time.deltaTime) % 360f;
            }
            if (!Done)
            {
                T = Seconds > 0 ? Mathf.Clamp01((Time.time - StartedAt) / Seconds) : 1;
                if (T >= 1) { Done = true; Current = Target; VisualDoneAt = Time.time; }
            }
            foreach (var p in parts) Apply(p, Done ? 1 : T);
        }
    }
}
