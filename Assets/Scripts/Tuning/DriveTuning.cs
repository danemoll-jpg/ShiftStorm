using UnityEngine;

namespace ShiftStorm
{
    // Driving (Resources/Tuning/Drive.asset). Numbers start from chastain_rush's ArcadeVehicle defaults; drift is new.
    [CreateAssetMenu(menuName = "ShiftStorm/Drive Tuning")]
    public sealed class DriveTuning : ScriptableObject
    {
        public Vector2 entrySpeed = new Vector2(6, 40);   // speed on arriving in this form is blended into this range
        public float topSpeed = 40, reverseSpeed = 11, acceleration = 14, reverseAcceleration = 7, braking = 26, coastingDrag = .45f;
        public float boostAcceleration = 10, boostTopSpeed = 50;
        [Header("Steering / grip")]
        public float slowSteerAngle = 32, fastSteerAngle = 10, wheelbase = 2.6f, steeringResponse = 7, yawResponse = 8;
        public float lateralGrip = 7, maxGripAcceleration = 24;
        [Header("Drift (secondary button)")]
        public float driftMinSpeed = 12, driftGrip = 1.6f, driftMaxGrip = 9, driftYawBoost = 1.55f, driftRecoverySeconds = .35f;
        [Header("Suspension")]
        public float suspensionLength = .8f, springStrength = 65, suspensionDamping = 8, maxSuspensionAcceleration = 45;
        public float uprightStrength = 12, uprightDamping = 4, airStability = .25f;
    }
}
