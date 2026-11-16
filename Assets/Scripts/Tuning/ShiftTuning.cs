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
        // Round 3 item 3 (the waterfall return "hung up a tad"): into a faster form, the speed's share of the old form's top speed carries
        // over (1 = all of it, capped by the new form's entry range), eased in over the visual change instead of a kick
        [Range(0, 1)] public float changeSpeedCarry = 1f;
        public float wheelsDownExtraGravity = 7f;     // the car springs onto its wheels: extra pull down while airborne just after a change into Drive
        public float wheelsDownSeconds = 2f;
        [Header("Recovery")]
        public float resetSpeed = 20f;                // a reset resumes at this modest speed
        public float graceSeconds = 1.5f;             // no collisions with obstacles after a reset
        public float stuckSeconds = 4f;               // under stuckSpeed this long while asking to go: recover
        public float stuckSpeed = 1.5f;
        public float noProgressSeconds = 15f;         // no new progress along the course this long: recover
        public float wrongWaterSeconds = .35f;        // a car or plane under the water outside a landing corridor: recover after this
        [Header("Race (Round 2)")]
        public float countdownSeconds = 3f;           // a time trial's countdown on the grid (3, 2, 1, GO)
        public float missedGateCueMeters = 40f;       // this far past the next gate's place (and through its plane) without earning it: TURN BACK
        public float missedGateResetMeters = 150f;    // this far past it, or ...
        public float missedGateResetSeconds = 6f;     // ... this long with the cue showing while not heading back to it: reset to the last anchor before that gate
        [Header("Direction cues (Round 2b)")]
        public float wrongWayCos = -.35f;             // WRONG WAY: moving against the route direction (cos of the angle below this, about 110 deg) ...
        public float wrongWaySeconds = .6f;           // ... for this long, while on the route
        public float offRouteDrive = 16f, offRouteBoat = 26f, offRouteFly = 55f, offRouteDive = 15f;   // further than this from the line: THIS WAY
        public float airOutOfBoundsSeconds = 4f;      // in the air, out of bounds to the side this long (THIS WAY meanwhile) before the reset
        [Header("Scrapes (Round 3: the Sky Arch slit's buttress only, since Round 4)")]
        public float scrapeSpeedKeep = .55f;          // a scrape on the slit's buttress keeps at most this much of the speed (never under the form's floor) ...
        public float scrapeSeconds = .8f;             // ... and the throttle is ignored this long
        [Header("Walls (Round 4: glance off, never stop)")]
        public float wallLossMin = .04f;              // speed lost by a fresh hit at a shallow angle ...
        public float wallLossHeadOn = .40f;           // ... and by a head-on hit (90 deg); in between by (angle / 90) ^ wallLossExponent
        public float wallLossExponent = 1.5f;
        public float wallNudge = 2.5f;                // m/s pushed away from the wall on a fresh hit (30 % of it while pressing on along it)
        public float wallSlideLossPerSecond = .1f;    // pressing on along a wall: this share of the speed per second
        public float wallRepeatSeconds = .35f;        // a new hit costs the angle's loss again only after this long
        [Range(0, 1)] public float wallNoseAlign = .8f;   // a car's nose is turned this share of the way along the wall when it points into it
        [Header("Near-miss gates (Round 4)")]
        public float nearMissPenalty = 3f;            // seconds added for each gate counted as a near miss
        public float branchAssistYaw = 14f;           // Round 12: on a flight branch a person's plane is turned toward its line ahead up to this many deg/s (less while steering hard)
        public float branchAssistPitch = 10f;         //   ... and pitched toward it up to this many deg/s while the stick is near centre
        public float branchRecoveryPenalty = 3f;      //   a reset on (or just off) a flight branch goes forward to its rejoin, with this many seconds added
        public float flightResetMaxBack = 200f;       //   in the air a reset never goes further back than this along the line (a point on the flight line instead of an anchor far behind)
        public float nearMissMargin = 1f;             // passing outside the frame but within this many gate widths (or heights) of it
    }
}
