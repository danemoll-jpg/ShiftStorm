using UnityEngine;

namespace ShiftStorm
{
    // Diving (new): attitude movement like flight, but slower and heavier: the turn rate eases toward the stick (slower directional response),
    // the vertical stick sets the pitch for depth and it levels when let go, it never rises through the surface (only a surfacing zone ends a
    // dive) and keeps clear of the floor and a cave roof. No oxygen, no ballast. Currents push it. On entry it carries its forward speed into a
    // gentle descent.
    public sealed partial class ShiftVehicle
    {
        public float DiveSpeed { get; private set; }
        float diveYawRate, descentLeft; Vector3 currentVelocity;

        void EnterDive(float speedIn)
        {
            Body.useGravity = false; Body.maxAngularVelocity = 3;
            var v = Body.linearVelocity; var flat = new Vector3(v.x, 0, v.z); float m = flat.magnitude;
            DiveSpeed = Mathf.Clamp(m, m > .5f ? dive.entrySpeed.x : 0, dive.entrySpeed.y);
            var dir = m > .5f && Vector3.Dot(flat, transform.forward) > 0 ? flat / m : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            yawDeg = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; PitchDeg = 0; RollDeg = 0; diveYawRate = 0; currentVelocity = Vector3.zero;
            descentLeft = 1.2f;
        }

        void StepDive(float dt)
        {
            var p = Body.position; var physics = gameObject.scene.GetPhysicsScene();
            float target = Input.Brake > .05f ? -dive.reverseSpeed * Input.Brake : Boosting ? dive.boostTopSpeed : Input.Throttle * dive.topSpeed;
            DiveSpeed = Mathf.MoveTowards(DiveSpeed, target, (target < DiveSpeed ? dive.brakeGain : dive.speedGain) * Mathf.Max(4f, Mathf.Abs(target - DiveSpeed)) * dt);
            diveYawRate = Mathf.MoveTowards(diveYawRate, Input.Steer * Authority * dive.turnRate * Mathf.Clamp01(Mathf.Abs(DiveSpeed) / 4f + .3f), dive.turnRate * dive.turnResponse * dt);
            yawDeg += diveYawRate * dt * (DiveSpeed < -.5f ? -1 : 1);
            RollDeg = Mathf.MoveTowards(RollDeg, diveYawRate / dive.turnRate * dive.bankForTurn, 40 * dt);   // + right wing down, as in flight
            float vin = Input.Vertical * Authority;
            if (descentLeft > 0) { descentLeft -= dt; PitchDeg = Mathf.MoveTowards(PitchDeg, Mathf.Min(dive.entryDescentPitch, vin * dive.maxPitch), 30 * dt); }
            else if (Mathf.Abs(vin) > .05f) PitchDeg += vin * dive.pitchRate * dt;
            else PitchDeg = Mathf.MoveTowards(PitchDeg, 0, dive.pitchReturn * dt);
            // depth limits: under the surface, over the floor, under a roof
            bool water = WaterVolume.SurfaceAt(p, out float surface, 50f);
            if (water && p.y > surface - dive.minDepth) PitchDeg = Mathf.Min(PitchDeg, Mathf.Lerp(0, -10, (p.y - (surface - dive.minDepth)) / 1f));
            if (physics.Raycast(p, Vector3.down, out RaycastHit floor, dive.floorClearance + 1, GroundMask, QueryTriggerInteraction.Ignore) && floor.distance < dive.floorClearance) PitchDeg = Mathf.Max(PitchDeg, 8);
            if (physics.Raycast(p, Vector3.up, out RaycastHit roof, dive.ceilingClearance + 1, GroundMask, QueryTriggerInteraction.Ignore) && roof.distance < dive.ceilingClearance) PitchDeg = Mathf.Min(PitchDeg, -8);
            PitchDeg = Mathf.Clamp(PitchDeg, -dive.maxPitch, dive.maxPitch);
            currentVelocity += CurrentZone.PushAt(p) * dive.currentScale * dt; currentVelocity *= 1 - Mathf.Min(1, .8f * dt);
            var rot = Quaternion.Euler(-PitchDeg, yawDeg, -RollDeg);
            var vel = rot * Vector3.forward * DiveSpeed + currentVelocity;
            if (water && p.y > surface - dive.minDepth * .5f && vel.y > 0) vel.y = -.5f;   // the surface holds it down
            Body.linearVelocity = vel; Body.angularVelocity = Vector3.zero; Body.MoveRotation(rot);
        }
    }
}
