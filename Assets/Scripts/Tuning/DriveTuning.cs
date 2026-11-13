using UnityEngine;

namespace ShiftStorm
{
    // Driving (Resources/Tuning/Drive.asset). Numbers start from chastain_rush's ArcadeVehicle defaults; drift is new.
    [CreateAssetMenu(menuName = "ShiftStorm/Drive Tuning")]
    public sealed class DriveTuning : ScriptableObject
    {
        public Vector2 entrySpeed = new Vector2(6, 65);   // speed on arriving in this form is blended into this range
        public float topSpeed = 65, reverseSpeed = 14, acceleration = 22, reverseAcceleration = 9, braking = 40, coastingDrag = .45f;
        public float boostAcceleration = 16, boostTopSpeed = 82;
        [Header("Steering / grip")]
        public float slowSteerAngle = 30, fastSteerAngle = 7, wheelbase = 2.6f, steeringResponse = 7, yawResponse = 8, highSpeedYawCap = .7f;
        public float lateralGrip = 8, maxGripAcceleration = 38;
        [Header("Drift (secondary button)")]
        public float driftMinSpeed = 18, driftGrip = 1.6f, driftMaxGrip = 15, driftYawBoost = 1.55f, driftRecoverySeconds = .35f;
        [Header("Suspension")]
        public float suspensionLength = .8f, springStrength = 80, suspensionDamping = 9, maxSuspensionAcceleration = 60;
        public float uprightStrength = 12, uprightDamping = 4, airStability = .25f;
    }
}
