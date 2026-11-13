using UnityEngine;

namespace ShiftStorm
{
    // Boating (Resources/Tuning/Boat.asset), new for ShiftStorm: a broad surface slide, low wave hops, currents that push.
    [CreateAssetMenu(menuName = "ShiftStorm/Boat Tuning")]
    public sealed class BoatTuning : ScriptableObject
    {
        public Vector2 entrySpeed = new Vector2(8, 30);
        public float verticalEntryKeep = .3f;           // of the vertical speed on landing in the water (a shallow splash)
        public float topSpeed = 30, acceleration = 9, braking = 12, reverseSpeed = 6, drag = .05f;
        public float boostAcceleration = 8, boostTopSpeed = 38;
        [Header("Turning / slide")]
        public float turnRate = 70, turnResponse = 4, slideGrip = 1.4f, carveGrip = 3.6f, carveTurnBoost = 1.45f;
        [Header("Floating")]
        public float rideHeight = .55f, buoyancy = 22, buoyancyDamping = 5, maxBuoyancy = 40, uprightStrength = 14, uprightDamping = 5;
        public float waveHeight = .22f, waveLength = 18f, waveSpeed = 1.2f;
        [Header("Off the water")]
        public float wetRampThrust = 1f, dryThrust = .15f, dryDrag = 2.5f;
        public float currentScale = 1f;
    }
}
