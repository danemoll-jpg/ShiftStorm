using UnityEngine;

namespace ShiftStorm
{
    // Diving (Resources/Tuning/Dive.asset), new for ShiftStorm: slower turns, depth on the vertical stick, auto-level, no oxygen, no ballast.
    [CreateAssetMenu(menuName = "ShiftStorm/Dive Tuning")]
    public sealed class DiveTuning : ScriptableObject
    {
        public Vector2 entrySpeed = new Vector2(8, 20);
        public float cruiseSpeed = 16, topSpeed = 22, boostTopSpeed = 28, minSpeed = 0, reverseSpeed = 7, speedGain = .7f, brakeGain = 1.5f;
        public float turnRate = 48, turnResponse = 2.2f, bankForTurn = 22;
        public float pitchRate = 30, maxPitch = 32, pitchReturn = 22, entryDescentPitch = -10;
        public float minDepth = 1.2f, floorClearance = 1.6f, ceilingClearance = 1.4f;
        public float currentScale = 1f;
    }
}
