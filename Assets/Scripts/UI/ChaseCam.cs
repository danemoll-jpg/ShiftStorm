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
        static float shakeUntil, shakeAmp, shakeLen;
        public static float LastShake { get; private set; } = -1;
        public static void Shake(float amp, float seconds) { shakeAmp = Mathf.Max(amp, Time.time < shakeUntil ? shakeAmp : 0); shakeLen = seconds; shakeUntil = Time.time + seconds; LastShake = Time.time; }

        public ShiftVehicle target;
        float distance = 7.5f, height = 2.6f, pitchFollow, fov = 65f; Vector3 velocity, lastTarget, offset; bool snapped; int lastResets = -1; Camera cam;
        public const float BaseFov = 65f;
        static Color skyFog; static float skyDensity, skyStart, skyEnd; static FogMode skyMode = FogMode.Exponential; static bool skyFogOn, skyRead;
        public static readonly Color DefaultUnderwater = new Color(.05f, .35f, .42f); public const float DefaultUnderwaterDensity = .035f;
        public static Color UnderwaterColor = DefaultUnderwater; public static float UnderwaterDensity = DefaultUnderwaterDensity;
        public static bool Underwater { get; private set; }
        // the course's look changed (JurassicLook.Apply): read the above-water fog again
        public static void ResetSkyFog() { skyFog = RenderSettings.fogColor; skyDensity = RenderSettings.fogDensity; skyFogOn = RenderSettings.fog; skyMode = RenderSettings.fogMode; skyStart = RenderSettings.fogStartDistance; skyEnd = RenderSettings.fogEndDistance; skyRead = true; }

        void Start() { if (!skyRead) ResetSkyFog(); cam = GetComponent<Camera>(); }
        // Round 6 (split screen): two cameras share one RenderSettings, so each camera sets the underwater fog for itself just before it
        // renders, from where it really is (after CameraViews has placed it). primary: the camera with the listener (CourseAudio's ambience).
        public bool primary = true;
        // Round 6: Far chase (CameraViews): the offset behind / above is scaled (Chastain Rush: 1.5 back, 1.55 up)
        public float distanceScale = 1, heightScale = 1;
        void OnEnable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += BeforeRender;
        void OnDisable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= BeforeRender;
        void BeforeRender(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera c) { if (c == cam && target && !Garage.Active) Fog(); }
        public bool UnderwaterHere { get; private set; }
        void Fog()
        {
            bool under = WaterVolume.SurfaceAt(transform.position, out float s, 0f) && transform.position.y < s;
            RenderSettings.fog = under || skyFogOn;
            RenderSettings.fogMode = under ? FogMode.Exponential : skyMode; if (!under) { RenderSettings.fogStartDistance = skyStart; RenderSettings.fogEndDistance = skyEnd; }
            RenderSettings.fogColor = under ? UnderwaterColor : skyFog;
            RenderSettings.fogDensity = under ? UnderwaterDensity : skyDensity;
            UnderwaterHere = under; if (primary) Underwater = under;
        }
        // Round 3 item 3: the sense of speed (field of view, distance) is measured against a top speed that eases from one form's to the next
        // (20 m/s per second) instead of jumping at the change: at the waterfall's Boat->Drive the old jump (50 -> 65 m/s) narrowed the view
        // about 4 deg and pulled the camera in at the moment the car left the ramp, which read as a stop. Legacy: the build 3 camera (checks).
        public static bool Legacy; float topRef = -1;
        public static readonly bool OldFlightLook = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldBtFlight") >= 0;   // checks: build 18's camera in flight
        float guideW;
        public float SpeedFrac { get; private set; }
        public float Kick { get; private set; }
        bool wasBoosting; float kickAt = -1;

        // Round 14 item 4 (Dan: "screen is shaky after race"): after the finish no shake of any kind (boost, wall bump, the T. rex's roar),
        // and the camera leaves the car: it eases out to the side and slowly turns round it, looking at a smoothed point (bumps from the
        // AI going by, the car rolling to a stop on the boards and a head view's bobbing head no longer reach the picture)
        bool finishCam; float finishAt, orbitDeg; Vector3 finishFocus, finishFocusVel, finishPosVel;
        public bool FinishCamOn => finishCam;
        public static readonly bool OldFinish = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldFinish") >= 0;   // checks: build 15's camera after the finish
        void FinishCam()
        {
            var t = target.transform; var want = t.position + Vector3.up * 1.2f;
            if (!finishCam) { finishCam = true; finishAt = Time.time; finishFocus = want; finishFocusVel = finishPosVel = Vector3.zero; var back = transform.position - want; back.y = 0; orbitDeg = back.sqrMagnitude > .01f ? Mathf.Atan2(back.x, back.z) * Mathf.Rad2Deg : target.HeadingDeg + 180; }
            float dt = Time.deltaTime; if (dt <= 0) return;
            finishFocus = Vector3.SmoothDamp(finishFocus, want, ref finishFocusVel, .7f, 60f, dt);
            float since = Time.time - finishAt; orbitDeg += Mathf.Lerp(0, 7f, Mathf.Clamp01(since / 3f)) * dt;   // a slow turn round the car
            float dist = Mathf.Lerp(8f, 11f, Mathf.Clamp01(since / 4f)), h = Mathf.Lerp(2.6f, 3.6f, Mathf.Clamp01(since / 4f));
            var pos = finishFocus + Quaternion.Euler(0, orbitDeg, 0) * Vector3.forward * dist + Vector3.up * h;
            transform.position = Vector3.SmoothDamp(transform.position, pos, ref finishPosVel, .5f, 80f, dt);
            transform.rotation = Quaternion.LookRotation(finishFocus - transform.position, Vector3.up);
            fov = Mathf.Lerp(fov, 62f, 1 - Mathf.Exp(-2f * dt)); if (cam) cam.fieldOfView = fov;
        }

        void LateUpdate()
        {
            if (!target || Garage.Active) return;
            if (target.Finished && !OldFinish) { FinishCam(); Fog(); return; }
            if (finishCam) { finishCam = false; snapped = false; }   // a new race: snap back behind the car
            var m = target.Mode;
            float wantD = m == Mode.Fly ? 11f : m == Mode.Dive ? 8.5f : m == Mode.Boat ? 8.5f : 7.5f;
            float wantH = m == Mode.Fly ? 3.4f : m == Mode.Dive ? 2.4f : m == Mode.Boat ? 3f : 2.6f;
            float wantP = m == Mode.Fly || m == Mode.Dive ? .45f : 0f;
            float k = 1 - Mathf.Exp((Legacy ? -2.5f : -1.6f) * Time.deltaTime);
            distance = Mathf.Lerp(distance, wantD, k); height = Mathf.Lerp(height, wantH, k); pitchFollow = Mathf.Lerp(pitchFollow, wantP, k);
            var t = target.transform;
            var fwd = Quaternion.Euler(0, target.HeadingDeg, 0) * Vector3.forward;
            if (pitchFollow > .01f) fwd = Vector3.Slerp(fwd, t.forward, pitchFollow).normalized;
            // Round 7: through the loop the camera follows the car's own forward and up (eased in and out over 0.35 s), so it rides round
            // the loop behind the car instead of staying level
            var up = Vector3.up; float lw = Mathf.SmoothStep(0, 1, target.LoopCam);
            if (lw > 0) { fwd = Vector3.Slerp(fwd, t.forward, lw).normalized; up = Vector3.Slerp(Vector3.up, t.up, lw).normalized; }
            var focus = t.position + up * 1.2f;
            if (topRef < 0 || Legacy || target.Holding) topRef = target.TopSpeedNow; else topRef = Mathf.MoveTowards(topRef, target.TopSpeedNow, 20f * Time.deltaTime);
            float speedFrac = Mathf.Clamp01(target.Speed / Mathf.Max(1f, topRef)); SpeedFrac = speedFrac;
            // Round 5 item 1: lower and closer at speed (build 5 pulled back 1.6 m, which made speed look smaller); on a boost press a short
            // kick: the camera drops back 1.2 m and the view widens 6 deg, easing out over 0.6 s, while the boost's own +8 deg holds (90 deg at top speed, 98 on boost)
            bool boostOn = target.Boosting && !target.Holding;
            if (boostOn && !wasBoosting) kickAt = Time.time; wasBoosting = boostOn;
            float kick = kickAt > 0 ? Mathf.Clamp01(1 - (Time.time - kickAt) / .6f) : 0; kick *= kick; Kick = kick;
            var wantOffset = Legacy ? -fwd * (distance + speedFrac * 1.6f) + Vector3.up * height
                : -fwd * ((distance - speedFrac * speedFrac * 1.4f + kick * 1.2f) * distanceScale) + up * ((height - speedFrac * .55f) * heightScale);
            bool reset = target.Resets != lastResets || (t.position - lastTarget).sqrMagnitude > 30 * 30;   // (the loop moves under 2 m per frame) lastResets = target.Resets; lastTarget = t.position;   // a reset or a restart (a jump) snaps
            if (!snapped || reset) { offset = wantOffset; velocity = Vector3.zero; snapped = true; }
            else offset = Vector3.SmoothDamp(offset, wantOffset, ref velocity, Mathf.Lerp(.12f, .03f, lw));   // Round 7: tight round the loop
            transform.position = focus + offset;
            // Round 16 item 1 (Dan: "confusing where the first flight is supposed to go"): on a flight branch, and for 3 s after a change into
            // the plane, the view turns part of the way (at most 25 deg) toward where the route goes (the HUD arrow's target), not only where
            // the nose points, so the next ring / the exit is on the screen; eased in and out
            var lookDir = fwd;
            if (!Legacy && !OldFlightLook)
            {
                bool flightNow = m == Mode.Fly && (target.OnShortcut && target.OnShortcut.flight || (target.ChangeStartedAt > 0 && Time.time - target.ChangeStartedAt < 3f && target.ChangeFrom != Mode.Fly));
                guideW = Mathf.MoveTowards(guideW, flightNow ? .5f : 0, Time.deltaTime * 1.2f);
                if (guideW > .001f)
                {
                    var to = target.GuideTarget - focus; if (to.sqrMagnitude > 4f)
                    {
                        var want = Vector3.RotateTowards(fwd, to.normalized, 25f * Mathf.Deg2Rad, 0f);
                        lookDir = Vector3.Slerp(fwd, want, guideW).normalized;
                    }
                }
            }
            transform.rotation = Quaternion.LookRotation(focus + lookDir * 6f - transform.position, up);
            // sense of speed: field of view, and a light shake on boost
            float wantFov = Legacy ? BaseFov + 18f * speedFrac + (target.Boosting ? 6f : 0f) : BaseFov + 20f * speedFrac * speedFrac + 5f * speedFrac + (target.Boosting ? 8f : 0f);
            fov = Mathf.Lerp(fov, wantFov, 1 - Mathf.Exp(-3f * Time.deltaTime)); if (cam) cam.fieldOfView = fov + (Legacy ? 0 : kick * 6f);
            if (target.Boosting && !target.Holding)
            {
                float a = (Legacy ? .05f + .07f * speedFrac : .06f + .1f * speedFrac) * (1 + kick), tt = Time.time * 23f;
                transform.position += transform.right * ((Mathf.PerlinNoise(tt, 0) - .5f) * 2 * a) + transform.up * ((Mathf.PerlinNoise(0, tt) - .5f) * 2 * a);
            }
            // Round 4: a wall hit reads as a hit: a short camera bump, bigger for a harder hit
            float since = Time.time - target.LastWallAt;
            if (target.LastWallAt > 0 && since < .3f)
            {
                float a = (.06f + (1 - target.LastWallKeep) * .8f) * (1 - since / .3f), tt = Time.time * 41f;
                transform.position += transform.right * ((Mathf.PerlinNoise(tt, 3) - .5f) * 2 * a) + transform.up * ((Mathf.PerlinNoise(3, tt) - .5f) * 2 * a);
            }
            // Round 8 item 1: a shake from the world (the T. rex's roar)
            if (Time.time < shakeUntil) { float a = shakeAmp * (shakeUntil - Time.time) / Mathf.Max(.01f, shakeLen), tt = Time.time * 33f; transform.position += transform.right * ((Mathf.PerlinNoise(tt, 7) - .5f) * 2 * a) + transform.up * ((Mathf.PerlinNoise(7, tt) - .5f) * 2 * a); }
            // underwater look (set again just before this camera renders: BeforeRender)
            Fog();
        }
    }
}
