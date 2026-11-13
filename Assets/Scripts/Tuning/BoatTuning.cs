using UnityEngine;

namespace ShiftStorm
{
    // Boating (Resources/Tuning/Boat.asset), new for ShiftStorm: a broad surface slide, low wave hops, currents that push.
    [CreateAssetMenu(menuName = "ShiftStorm/Boat Tuning")]
    public sealed class BoatTuning : ScriptableObject
    {
        public Vector2 entrySpeed = new Vector2(10, 50);
        public float verticalEntryKeep = .3f;           // of the vertical speed on landing in the water (a shallow splash)
        public float topSpeed = 50, acceleration = 22, braking = 18, reverseSpeed = 8, drag = .03f;
        public float boostAcceleration = 14, boostTopSpeed = 62;
        [Header("Turning / slide")]
        public float turnRate = 74, turnResponse = 4, slideGrip = 1.6f, carveGrip = 4f, carveTurnBoost = 1.45f;
        [Header("Floating")]
        public float rideHeight = .55f, buoyancy = 22, buoyancyDamping = 5, maxBuoyancy = 40, uprightStrength = 14, uprightDamping = 5;
        public float waveHeight = .22f, waveLength = 18f, waveSpeed = 1.2f;
        [Header("Off the water")]
        public float wetRampThrust = 1f, dryThrust = .15f, dryDrag = 2.5f;
        public float currentScale = 1.3f;
    }
}
