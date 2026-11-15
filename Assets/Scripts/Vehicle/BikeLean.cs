using UnityEngine;

namespace ShiftStorm
{
    // Round 9 item 3c: a motorcycle leans into its turns (the Scrambler's land form; designs.json "lean"). Looks only: the FormSet's pivot (the
    // model, the rider, the bars) rolls about the tyres' contact line by the lean a real bike needs for this turn (atan(speed x yaw rate / g),
    // at most 35 deg), eased in; the hull, its physics and the handling stats are the same as every design's. Not during a change (the
    // FormSet's squash and twirl own the pivot then); the camera follows the hull, so the world does not tilt.
    [DefaultExecutionOrder(-30)]   // before Steering (-20) and the Driver's IK read the grips
    public sealed class BikeLean : MonoBehaviour
    {
        public const float MaxLean = 35f, ContactY = -.85f;   // the car form's floor (Tools/Blender/vehicle_import.py FLOOR["Car"])
        public static float Forced = float.NaN;              // checks: a fixed lean for the pictures
        ShiftVehicle v; float roll; bool applied;
        public float Roll => roll;
        public bool Leaning { get; private set; }

        void LateUpdate()
        {
            if (!v) v = GetComponent<ShiftVehicle>();
            var fs = v ? v.Forms : null; if (!fs) return;
            var d = fs.Design; Leaning = d != null && d.Leans(fs.ShownMode);
            if (!fs.Done) { if (applied) { fs.Pivot.localPosition = Vector3.zero; applied = false; } roll = 0; return; }
            float want = 0;
            if (Leaning && v.Body && !v.Holding)
            {
                float yawRate = Vector3.Dot(v.Body.angularVelocity, transform.up);
                want = -Mathf.Atan2(v.Speed * yawRate, 9.81f) * Mathf.Rad2Deg;
                want = Mathf.Clamp(want, -MaxLean, MaxLean);
            }
            if (Leaning && !float.IsNaN(Forced) && v.Human) want = Forced;
            roll = Mathf.Lerp(roll, want, 1 - Mathf.Exp(-7f * Time.deltaTime));
            if (!Leaning && Mathf.Abs(roll) < .05f) { if (applied) { fs.Pivot.localRotation = Quaternion.identity; fs.Pivot.localPosition = Vector3.zero; applied = false; } return; }
            var c = new Vector3(0, ContactY, 0); var q = Quaternion.AngleAxis(roll, Vector3.forward);
            fs.Pivot.localRotation = q; fs.Pivot.localPosition = c - q * c; applied = true;
        }
    }
}
