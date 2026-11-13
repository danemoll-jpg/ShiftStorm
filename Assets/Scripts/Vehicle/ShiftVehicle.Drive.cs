using UnityEngine;

namespace ShiftStorm
{
    // Driving: ported from chastain_rush's ArcadeVehicle.Simulate (raycast suspension, drive force with opposite-pedal braking, lateral grip,
    // bicycle-model yaw, damped self-righting). Chastain Rush's stunt park, weather grip, shallow water, hold-at-rest and air steering are left
    // out. New: boost, the drift on the secondary button (lower side grip and more yaw while held, grip eased back on release), and the
    // change authority on the steering.
    public sealed partial class ShiftVehicle
    {
        public static readonly Vector3[] Wheels = { new(-.85f, -.2f, 1.35f), new(.85f, -.2f, 1.35f), new(-.85f, -.2f, -1.35f), new(.85f, -.2f, -1.35f) };
        public int GroundedWheels { get; private set; }
        public bool Drifting { get; private set; }
        public float FirstWheelContactAt { get; private set; } = -1;   // first touch after entering Drive (checks: did the change finish first?)
        float steer, driftBlend;

        void EnterDrive(float speedIn)
        {
            Body.useGravity = true; Body.maxAngularVelocity = 5;
            var v = Body.linearVelocity; float m = v.magnitude;
            float want = Mathf.Clamp(m, drive.entrySpeed.x * (m > .5f ? 1 : 0), drive.entrySpeed.y);
            if (m > .01f) Body.linearVelocity = v * (want / m);
            steer = 0; driftBlend = 0; FirstWheelContactAt = -1;
        }

        void StepDrive(float dt)
        {
            var physics = gameObject.scene.GetPhysicsScene();
            GroundedWheels = 0; Vector3 normal = Vector3.zero;
            foreach (var local in Wheels)
            {
                var origin = transform.TransformPoint(local);
                if (!physics.Raycast(origin, -transform.up, out RaycastHit hit, drive.suspensionLength, GroundMask, QueryTriggerInteraction.Ignore)) continue;
                if (Vector3.Dot(hit.normal, Vector3.up) < .35f) continue;
                GroundedWheels++; normal += hit.normal;
                float compression = drive.suspensionLength - hit.distance;
                float speed = Vector3.Dot(Body.GetPointVelocity(origin), transform.up);
                float lift = Mathf.Clamp(compression * drive.springStrength - speed * drive.suspensionDamping, 0, drive.maxSuspensionAcceleration);
                Body.AddForceAtPosition(transform.up * (lift / Wheels.Length), origin, ForceMode.Acceleration);
            }
            if (GroundedWheels > 0 && FirstWheelContactAt < 0) FirstWheelContactAt = Time.time;
            bool grounded = GroundedWheels >= 2;
            Vector3 up = grounded ? normal.normalized : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up).normalized, right = Vector3.Cross(up, forward);
            float speedForward = Vector3.Dot(Body.linearVelocity, forward);
            float throttle = Input.Throttle, brakeIn = Input.Brake;
            steer = Mathf.MoveTowards(steer, Mathf.Clamp(Input.Steer, -1, 1) * Authority, drive.steeringResponse * dt);
            bool wantDrift = Input.Secondary && grounded && speedForward > drive.driftMinSpeed && Mathf.Abs(Input.Steer) > .2f;
            driftBlend = Mathf.MoveTowards(driftBlend, wantDrift ? 1 : 0, dt / (wantDrift ? .12f : drive.driftRecoverySeconds));
            Drifting = driftBlend > .5f;
            if (grounded)
            {
                float top = Boosting ? drive.boostTopSpeed : drive.topSpeed;
                float acc = drive.acceleration + (Boosting ? drive.boostAcceleration : 0);
                float drv;
                if (brakeIn > .05f && speedForward > .6f) drv = -drive.braking * brakeIn;
                else if (throttle > .05f && speedForward < -.6f) drv = drive.braking * throttle;
                else drv = Mathf.Max(throttle, Boosting ? 1 : 0) * acc * Mathf.Clamp01(1 - Mathf.Max(0, speedForward) / top)
                          - brakeIn * drive.reverseAcceleration * Mathf.Clamp01(1 - Mathf.Max(0, -speedForward) / drive.reverseSpeed);
                if (throttle < .05f && brakeIn < .05f && !Boosting) drv -= speedForward * drive.coastingDrag;
                Body.AddForce(forward * drv, ForceMode.Acceleration);
                float sideways = Vector3.Dot(Body.linearVelocity, right);
                float grip = Mathf.Lerp(drive.lateralGrip, drive.driftGrip, driftBlend), maxGrip = Mathf.Lerp(drive.maxGripAcceleration, drive.driftMaxGrip, driftBlend);
                Body.AddForce(-right * Mathf.Clamp(sideways * grip, -maxGrip, maxGrip), ForceMode.Acceleration);
                float angle = Mathf.Lerp(drive.slowSteerAngle, drive.fastSteerAngle, Mathf.Clamp01(Mathf.Abs(speedForward) / drive.topSpeed));
                float yaw = Mathf.Tan(steer * angle * Mathf.Deg2Rad) * speedForward / drive.wheelbase * Mathf.Lerp(1, drive.driftYawBoost, driftBlend);
                // Round 2b: the yaw rate cap eases from 1.8 rad/s standing to highSpeedYawCap at top speed (calmer at 145 mph on a stick)
                float cap = Mathf.Lerp(1.8f, drive.highSpeedYawCap, Mathf.Clamp01(Mathf.Abs(speedForward) / drive.topSpeed)) * Mathf.Lerp(1, drive.driftYawBoost, driftBlend);
                yaw = Mathf.Clamp(yaw, -cap, cap);
                Body.AddTorque(up * ((yaw - Vector3.Dot(Body.angularVelocity, up)) * drive.yawResponse), ForceMode.Acceleration);
            }
            Vector3 tilt = Vector3.Cross(transform.up, up), tiltVelocity = Vector3.ProjectOnPlane(Body.angularVelocity, up);
            float level = grounded ? 1 : drive.airStability;
            Body.AddTorque((tilt * drive.uprightStrength - tiltVelocity * drive.uprightDamping) * level, ForceMode.Acceleration);
        }
        public float VisualSteer => steer;
    }
}
