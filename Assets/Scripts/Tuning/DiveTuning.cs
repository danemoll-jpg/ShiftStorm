using UnityEngine;

namespace ShiftStorm
{
    // Diving (Resources/Tuning/Dive.asset), new for ShiftStorm: slower turns, depth on the vertical stick, auto-level, no oxygen, no ballast.
    [CreateAssetMenu(menuName = "ShiftStorm/Dive Tuning")]
    public sealed class DiveTuning : ScriptableObject
    {
        public Vector2 entrySpeed = new Vector2(10, 34);
        public float cruiseSpeed = 28, topSpeed = 38, boostTopSpeed = 48, minSpeed = 0, reverseSpeed = 9, speedGain = .7f, brakeGain = 1.5f;
        public float turnRate = 60, turnResponse = 2.2f, bankForTurn = 22;
        public float pitchRate = 36, maxPitch = 32, pitchReturn = 26, entryDescentPitch = -10;
        public float minDepth = 1.2f, floorClearance = 1.6f, ceilingClearance = 1.4f;
        public float currentScale = 1f;
    }
}
