using UnityEngine;

namespace ShiftStorm
{
    // Shared tuning (Resources/Tuning/Shift.asset): boost, form changes, recovery. Handling per mode lives in DriveTuning / BoatTuning / FlyTuning /
    // DiveTuning, so handling changes never touch the track (CLAUDE.md: tuning values live in data).
    [CreateAssetMenu(menuName = "ShiftStorm/Shift Tuning")]
    public sealed class ShiftTuning : ScriptableObject
    {
        [Header("Boost (one meter for every form)")]
        [Range(0, 1)] public float boostStart = 1f;
        public float boostDrainPerSecond = .32f;      // a full meter lasts about 3 s
        public float boostRefillPerSecond = .1f;      // fixed refill: empty to full in 10 s, in every form
        public float boostDelayAfterUse = .4f;        // the refill starts this long after the button is let go
        [Header("Form change")]
        [Range(.5f, .8f)] public float changeSeconds = .6f;   // the visual change; the physics hands over at its start (t = 0)
        [Range(0, 1)] public float startAuthority = .45f;     // the player's control at the handover, rising to 1 by the end of the change
        public float alignAssistSeconds = .45f;               // gentle, bounded alignment help after a change
        public float alignAssistMaxDegPerSecond = 80f;
        public float alignAssistMaxTotalDeg = 40f;
        [Header("Recovery")]
        public float resetSpeed = 12f;                // a reset resumes at this modest speed
        public float graceSeconds = 1.5f;             // no collisions with obstacles after a reset
        public float stuckSeconds = 4f;               // under stuckSpeed this long while asking to go: recover
        public float stuckSpeed = 1.5f;
        public float noProgressSeconds = 15f;         // no new progress along the course this long: recover
        public float wrongWaterSeconds = .35f;        // a car or plane under the water outside a landing corridor: recover after this
    }
}
