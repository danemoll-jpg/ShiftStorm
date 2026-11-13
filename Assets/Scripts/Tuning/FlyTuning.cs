using UnityEngine;

namespace ShiftStorm
{
    // Flying (Resources/Tuning/Fly.asset). Ported from chastain_rush's PlaneFlight: attitude flight along the nose, the bank turns it, auto-level,
    // limited pitch, never a stall (a speed floor instead of a stall speed).
    [CreateAssetMenu(menuName = "ShiftStorm/Fly Tuning")]
    public sealed class FlyTuning : ScriptableObject
    {
        public Vector2 entrySpeed = new Vector2(30, 48);
        public float minSpeed = 26, cruiseSpeed = 38, topSpeed = 50, speedGain = .5f, boostTopSpeed = 62;
        public float pitchRate = 40, maxPitchUp = 22, maxPitchDown = 28, pitchReturn = 18;
        public float rollRate = 110, maxBank = 55, levelRate = 70, bankTurnBonus = 10;
        public Vector2 entryPitch = new Vector2(-6, 12);   // the pitch the flight starts with is held in this range (safe initial trajectory)
        public float groundClearance = 2.5f, ceilingAboveStart = 40f;
    }
}
