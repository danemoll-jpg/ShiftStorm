using UnityEngine;

namespace ShiftStorm
{
    // Boating (new): the hull floats at the surface (spring-damper buoyancy toward the surface plus a low swell, which gives small hops at speed),
    // thrust along the flat heading, low side grip for a broad slide (the secondary button carves: more grip, faster turn), currents push.
    // Off the water it hovers on its hull: full thrust on a WetRamp (the surfacing pool's water ramp), heavy drag on dry ground.
    public sealed partial class ShiftVehicle
    {
        public bool OnWater { get; private set; }
        public bool Carving { get; private set; }
        static readonly Vector3[] HullPoints = { new(0, -.3f, 1.4f), new(-.8f, -.3f, -1.2f), new(.8f, -.3f, -1.2f) };

        void EnterBoat(float speedIn)
        {
            Body.useGravity = true; Body.maxAngularVelocity = 4;
            var v = Body.linearVelocity; var flat = new Vector3(v.x, 0, v.z); float m = flat.magnitude;
            float want = Mathf.Clamp(m, m > .5f ? boat.entrySpeed.x : 0, boat.entrySpeed.y);
            var dir = m > .5f ? flat / m : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Body.linearVelocity = dir * want + Vector3.up * (v.y * boat.verticalEntryKeep);
            Body.angularVelocity *= .3f;
        }

        public float WaveAt(Vector3 p) => boat.waveHeight * Mathf.Sin((p.x * .6f + p.z) / boat.waveLength * 6.283f + Time.time * boat.waveSpeed);

        void StepBoat(float dt)
        {
            var p = Body.position; Vector3 up = Vector3.up; bool supported = false; float thrustScale = 1, extraDrag = 0;
            OnWater = WaterVolume.SurfaceAt(p, out float surface, 2.5f);
            // on its hull over ground (a ramp, a beach, shallow water over a ramp): a stiff hover, like the car's suspension on three hull points
            var physics = gameObject.scene.GetPhysicsScene(); Vector3 n = Vector3.zero; int hits = 0; bool wet = false;
            foreach (var local in HullPoints)
            {
                var origin = transform.TransformPoint(local);
                if (!physics.Raycast(origin, -transform.up, out RaycastHit hit, .9f, GroundMask, QueryTriggerInteraction.Ignore)) continue;
                hits++; n += hit.normal; var wr = hit.collider.GetComponentInParent<WetRamp>(); if (wr && wr.enabled) wet = true;   // Round 16: Dam Shame's chute is wet only once its water runs
                float comp = .9f - hit.distance, sp = Vector3.Dot(Body.GetPointVelocity(origin), transform.up);
                Body.AddForceAtPosition(transform.up * (Mathf.Clamp(comp * 70 - sp * 8, 0, 50) / HullPoints.Length), origin, ForceMode.Acceleration);
            }
            if (hits >= 2)
            {
                supported = true; up = n.normalized; bool wetHere = wet || OnWater;
                thrustScale = wetHere ? boat.wetRampThrust : boat.dryThrust; extraDrag = wetHere ? 0 : boat.dryDrag;
            }
            else if (OnWater)
            {
                float target = surface + WaveAt(p) + boat.rideHeight, depth = target - p.y;
                if (depth > -.25f)
                {
                    float a = Mathf.Clamp(9.81f + depth * boat.buoyancy - Body.linearVelocity.y * boat.buoyancyDamping, 0, boat.maxBuoyancy);
                    if (depth > 1f && Body.linearVelocity.y > 6f) a = 9.81f;   // rising from deep (surfacing): at most 6 m/s, no launch out of the water
                    Body.AddForce(Vector3.up * a, ForceMode.Acceleration); supported = true;
                    Body.AddForce(CurrentZone.PushAt(p) * boat.currentScale, ForceMode.Acceleration);
                }
            }
            var forward = Vector3.ProjectOnPlane(transform.forward, up).normalized; var right = Vector3.Cross(up, forward);
            float speedF = Vector3.Dot(Body.linearVelocity, forward);
            Carving = Input.Secondary && supported && Mathf.Abs(Input.Steer) > .2f;
            if (supported)
            {
                float top = Boosting ? boat.boostTopSpeed : boat.topSpeed, acc = boat.acceleration + (Boosting ? boat.boostAcceleration : 0);
                float drv;
                if (Input.Brake > .05f && speedF > .5f) drv = -boat.braking * Input.Brake;
                else drv = Mathf.Max(Input.Throttle, Boosting ? 1 : 0) * acc * Mathf.Clamp01(1 - Mathf.Max(0, speedF) / top) * thrustScale
                          - Input.Brake * boat.acceleration * .6f * Mathf.Clamp01(1 - Mathf.Max(0, -speedF) / boat.reverseSpeed);
                Body.AddForce(forward * drv, ForceMode.Acceleration);
                var planar = Vector3.ProjectOnPlane(Body.linearVelocity, up);
                Body.AddForce(-planar * (boat.drag + extraDrag), ForceMode.Acceleration);
                float sideways = Vector3.Dot(Body.linearVelocity, right);
                Body.AddForce(-right * sideways * (Carving ? boat.carveGrip : boat.slideGrip), ForceMode.Acceleration);
                float rate = Input.Steer * Authority * boat.turnRate * Mathf.Deg2Rad * Mathf.Clamp01(Mathf.Abs(speedF) / 6f) * (Carving ? boat.carveTurnBoost : 1) * (speedF < -.5f ? -1 : 1);
                Body.AddTorque(up * ((rate - Vector3.Dot(Body.angularVelocity, up)) * boat.turnResponse), ForceMode.Acceleration);
            }
            Vector3 tilt = Vector3.Cross(transform.up, up), tiltVelocity = Vector3.ProjectOnPlane(Body.angularVelocity, up);
            float level = supported ? 1 : .3f;
            Body.AddTorque((tilt * boat.uprightStrength - tiltVelocity * boat.uprightDamping) * level, ForceMode.Acceleration);
        }
    }
}
