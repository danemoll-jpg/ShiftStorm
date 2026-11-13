using UnityEngine;

namespace ShiftStorm
{
    // Chase camera: behind and above the vehicle, distance and height blended per form (never a cut during a change), following the heading on
    // the ground and water and the nose (part of its pitch) in the air and underwater. Underwater it turns on a teal fog. Only a reset snaps it.
    // Starts from chastain_rush's ChaseCamera idea (smoothed follow behind the heading); written fresh for the four forms.
    // Round 2b (sense of speed): the field of view widens with speed (65 deg standing, up to about 85 at top speed, more on boost), the camera
    // sits a little further back at speed, a light shake on boost; the follow smooths the offset from the vehicle, not the world position,
    // so it no longer drops back at the new speeds.
    public sealed class ChaseCam : MonoBehaviour
    {
        public ShiftVehicle target;
        float distance = 7.5f, height = 2.6f, pitchFollow, fov = 65f; Vector3 velocity, lastTarget, offset; bool snapped; int lastResets = -1; Camera cam;
        public const float BaseFov = 65f;
        static Color skyFog; static float skyDensity; static bool skyFogOn, skyRead;
        public static readonly Color DefaultUnderwater = new Color(.05f, .35f, .42f); public const float DefaultUnderwaterDensity = .035f;
        public static Color UnderwaterColor = DefaultUnderwater; public static float UnderwaterDensity = DefaultUnderwaterDensity;
        public static bool Underwater { get; private set; }
        // the course's look changed (JurassicLook.Apply): read the above-water fog again
        public static void ResetSkyFog() { skyFog = RenderSettings.fogColor; skyDensity = RenderSettings.fogDensity; skyFogOn = RenderSettings.fog; skyRead = true; }

        void Start() { if (!skyRead) ResetSkyFog(); cam = GetComponent<Camera>(); }
        // Round 3 item 3: the sense of speed (field of view, distance) is measured against a top speed that eases from one form's to the next
        // (20 m/s per second) instead of jumping at the change: at the waterfall's Boat->Drive the old jump (50 -> 65 m/s) narrowed the view
        // about 4 deg and pulled the camera in at the moment the car left the ramp, which read as a stop. Legacy: the build 3 camera (checks).
        public static bool Legacy; float topRef = -1;
        public float SpeedFrac { get; private set; }

        void LateUpdate()
        {
            if (!target) return;
            var m = target.Mode;
            float wantD = m == Mode.Fly ? 11f : m == Mode.Dive ? 8.5f : m == Mode.Boat ? 8.5f : 7.5f;
            float wantH = m == Mode.Fly ? 3.4f : m == Mode.Dive ? 2.4f : m == Mode.Boat ? 3f : 2.6f;
            float wantP = m == Mode.Fly || m == Mode.Dive ? .45f : 0f;
            float k = 1 - Mathf.Exp((Legacy ? -2.5f : -1.6f) * Time.deltaTime);
            distance = Mathf.Lerp(distance, wantD, k); height = Mathf.Lerp(height, wantH, k); pitchFollow = Mathf.Lerp(pitchFollow, wantP, k);
            var t = target.transform;
            var fwd = Quaternion.Euler(0, target.HeadingDeg, 0) * Vector3.forward;
            if (pitchFollow > .01f) fwd = Vector3.Slerp(fwd, t.forward, pitchFollow).normalized;
            var focus = t.position + Vector3.up * 1.2f;
            if (topRef < 0 || Legacy || target.Holding) topRef = target.TopSpeedNow; else topRef = Mathf.MoveTowards(topRef, target.TopSpeedNow, 20f * Time.deltaTime);
            float speedFrac = Mathf.Clamp01(target.Speed / Mathf.Max(1f, topRef)); SpeedFrac = speedFrac;
            var wantOffset = -fwd * (distance + speedFrac * 1.6f) + Vector3.up * height;
            bool reset = target.Resets != lastResets || (t.position - lastTarget).sqrMagnitude > 30 * 30; lastResets = target.Resets; lastTarget = t.position;   // a reset or a restart (a jump) snaps
            if (!snapped || reset) { offset = wantOffset; velocity = Vector3.zero; snapped = true; }
            else offset = Vector3.SmoothDamp(offset, wantOffset, ref velocity, .12f);
            transform.position = focus + offset;
            transform.rotation = Quaternion.LookRotation(focus + fwd * 6f - transform.position, Vector3.up);
            // sense of speed: field of view, and a light shake on boost
            float wantFov = BaseFov + 18f * speedFrac + (target.Boosting ? 6f : 0f);
            fov = Mathf.Lerp(fov, wantFov, 1 - Mathf.Exp(-3f * Time.deltaTime)); if (cam) cam.fieldOfView = fov;
            if (target.Boosting && !target.Holding)
            {
                float a = .05f + .07f * speedFrac, tt = Time.time * 23f;
                transform.position += transform.right * ((Mathf.PerlinNoise(tt, 0) - .5f) * 2 * a) + transform.up * ((Mathf.PerlinNoise(0, tt) - .5f) * 2 * a);
            }
            // underwater look
            bool under = WaterVolume.SurfaceAt(transform.position, out float s, 0f) && transform.position.y < s;
            RenderSettings.fog = under || skyFogOn;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = under ? UnderwaterColor : skyFog;
            RenderSettings.fogDensity = under ? UnderwaterDensity : skyDensity;
            Underwater = under;
        }
    }
}
