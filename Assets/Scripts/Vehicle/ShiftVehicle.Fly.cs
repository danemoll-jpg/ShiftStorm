using UnityEngine;

namespace ShiftStorm
{
    // Flying: ported from chastain_rush's PlaneFlight (attitude flight: the body flies along its nose at the airspeed, the bank turns it with a
    // coordinated turn plus a little extra, the stick sets the pitch rate and it eases back to level when let go, climbing costs speed and diving
    // gains it). Changed: no take-off run, no landing or crash (the course's zones hand flight to the next form), no stall (a speed floor), the
    // bank follows the steering directly (Chastain's player plane, Round 93), limited pitch, ground clearance and a ceiling. On entry the pitch is
    // held in a safe range (FlyTuning.entryPitch) and the stick's authority ramps up over the change.
    public sealed partial class ShiftVehicle
    {
        public float PitchDeg { get; private set; }
        public float RollDeg { get; private set; }
        public float Airspeed { get; private set; }
        float yawDeg, ceilingY; bool inSlip;
        [System.NonSerialized] public float debugEntryPitch = float.NaN;   // checks only: force an extreme pitch (the nose-down water entry test)

        void EnterFly(float speedIn, bool placed)
        {
            Body.useGravity = false; Body.maxAngularVelocity = 4;
            var v = Body.linearVelocity; float m = v.magnitude;
            Airspeed = Mathf.Clamp(m, fly.entrySpeed.x, fly.entrySpeed.y);
            var dir = m > 1f && !placed ? v / m : transform.forward;
            float pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1, 1)) * Mathf.Rad2Deg;
            PitchDeg = Mathf.Clamp(pitch, fly.entryPitch.x, fly.entryPitch.y);
            var flat = Vector3.ProjectOnPlane(dir, Vector3.up); if (flat.sqrMagnitude < 1e-3f) flat = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            yawDeg = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg; RollDeg = 0;
            ceilingY = Body.position.y + fly.ceilingAboveStart;
        }

        void StepFly(float dt)
        {
            if (!float.IsNaN(debugEntryPitch)) { PitchDeg = debugEntryPitch; }
            float top = Boosting ? fly.boostTopSpeed : fly.topSpeed;
            float target = Input.Brake > .05f || Scraped ? fly.minSpeed : Mathf.Lerp(fly.cruiseSpeed, top, Mathf.Max(Input.Throttle, Boosting ? 1 : 0));
            Airspeed += ((target - Airspeed) * fly.speedGain - 9.81f * Mathf.Sin(PitchDeg * Mathf.Deg2Rad) * .5f) * dt;
            float lift = ScrapedRecently ? 0 : SlipstreamZone.SpeedAt(Body.position); if (lift > Airspeed) { if (!inSlip) Note("slipstream", $"updraft: {Airspeed:0} -> {lift:0} m/s"); Airspeed = lift; inSlip = true; } else inSlip = false;   // Round 3: the Sky Arch slit
            Airspeed = Mathf.Clamp(Airspeed, fly.minSpeed, Mathf.Max(fly.boostTopSpeed * 1.1f, lift));
            float vin = Input.Vertical * Authority;
            if (float.IsNaN(debugEntryPitch))
            {
                if (Mathf.Abs(vin) > .05f) PitchDeg += vin * fly.pitchRate * dt;
                else PitchDeg = Mathf.MoveTowards(PitchDeg, 0, fly.pitchReturn * dt);
                // ground clearance: never fly into the ground (water has no collider: the zones and the recovery handle it)
                if (gameObject.scene.GetPhysicsScene().Raycast(Body.position, Vector3.down, out RaycastHit hit, fly.groundClearance + 2f, GroundMask, QueryTriggerInteraction.Ignore) && hit.distance < fly.groundClearance)
                    PitchDeg = Mathf.MoveTowards(PitchDeg, 8, 60 * dt);
                if (Body.position.y > ceilingY) PitchDeg = Mathf.Min(PitchDeg, Mathf.Lerp(0, -6, (Body.position.y - ceilingY) / 10f));
                PitchDeg = Mathf.Clamp(PitchDeg, -fly.maxPitchDown, fly.maxPitchUp);
            }
            float bankIn = Input.Steer * Authority;
            RollDeg = Mathf.MoveTowards(RollDeg, bankIn * fly.maxBank, (Mathf.Abs(bankIn) > .05f ? fly.rollRate : fly.levelRate) * dt);
            float turn = Mathf.Rad2Deg * 9.81f * Mathf.Tan(RollDeg * Mathf.Deg2Rad) / Mathf.Max(Airspeed, 15f) + bankIn * fly.bankTurnBonus;
            yawDeg += turn * dt;
            var rot = Quaternion.Euler(-PitchDeg, yawDeg, -RollDeg);
            Body.linearVelocity = rot * Vector3.forward * Airspeed;
            Body.angularVelocity = Vector3.zero; Body.MoveRotation(rot);
        }
    }
}
