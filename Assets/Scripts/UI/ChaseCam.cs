using UnityEngine;

namespace ShiftStorm
{
    // Chase camera: behind and above the vehicle, distance and height blended per form (never a cut during a change), following the heading on
    // the ground and water and the nose (part of its pitch) in the air and underwater. Underwater it turns on a teal fog. Only a reset snaps it.
    // Starts from chastain_rush's ChaseCamera idea (smoothed follow behind the heading); written fresh for the four forms.
    public sealed class ChaseCam : MonoBehaviour
    {
        public ShiftVehicle target;
        float distance = 7.5f, height = 2.6f, pitchFollow; Vector3 velocity; bool snapped; int lastResets = -1;
        Color skyFog; float skyDensity; bool skyFogOn;

        void Start() { skyFog = RenderSettings.fogColor; skyDensity = RenderSettings.fogDensity; skyFogOn = RenderSettings.fog; }

        void LateUpdate()
        {
            if (!target) return;
            var m = target.Mode;
            float wantD = m == Mode.Fly ? 11f : m == Mode.Dive ? 8.5f : m == Mode.Boat ? 8.5f : 7.5f;
            float wantH = m == Mode.Fly ? 3.4f : m == Mode.Dive ? 2.4f : m == Mode.Boat ? 3f : 2.6f;
            float wantP = m == Mode.Fly || m == Mode.Dive ? .45f : 0f;
            float k = 1 - Mathf.Exp(-2.5f * Time.deltaTime);
            distance = Mathf.Lerp(distance, wantD, k); height = Mathf.Lerp(height, wantH, k); pitchFollow = Mathf.Lerp(pitchFollow, wantP, k);
            var t = target.transform;
            var fwd = Quaternion.Euler(0, target.HeadingDeg, 0) * Vector3.forward;
            if (pitchFollow > .01f) fwd = Vector3.Slerp(fwd, t.forward, pitchFollow).normalized;
            var focus = t.position + Vector3.up * 1.2f;
            var want = focus - fwd * distance + Vector3.up * height;
            bool reset = target.Resets != lastResets; lastResets = target.Resets;
            if (!snapped || reset) { transform.position = want; velocity = Vector3.zero; snapped = true; }
            else transform.position = Vector3.SmoothDamp(transform.position, want, ref velocity, .12f);
            transform.rotation = Quaternion.LookRotation(focus + fwd * 6f - transform.position, Vector3.up);
            // underwater look
            bool under = WaterVolume.SurfaceAt(transform.position, out float s, 0f) && transform.position.y < s;
            RenderSettings.fog = under || skyFogOn;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = under ? new Color(.05f, .35f, .42f) : skyFog;
            RenderSettings.fogDensity = under ? .035f : skyDensity;
        }
    }
}
