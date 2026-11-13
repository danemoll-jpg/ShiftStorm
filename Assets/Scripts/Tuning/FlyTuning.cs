using UnityEngine;

namespace ShiftStorm
{
    // Flying (Resources/Tuning/Fly.asset). Ported from chastain_rush's PlaneFlight: attitude flight along the nose, the bank turns it, auto-level,
    // limited pitch, never a stall (a speed floor instead of a stall speed).
    [CreateAssetMenu(menuName = "ShiftStorm/Fly Tuning")]
    public sealed class FlyTuning : ScriptableObject
    {
        public Vector2 entrySpeed = new Vector2(48, 77);
        public float minSpeed = 42, cruiseSpeed = 62, topSpeed = 80, speedGain = .5f, boostTopSpeed = 100;
        public float pitchRate = 42, maxPitchUp = 22, maxPitchDown = 28, pitchReturn = 18;
        public float rollRate = 110, maxBank = 55, levelRate = 70, bankTurnBonus = 18;
        public Vector2 entryPitch = new Vector2(-6, 12);   // the pitch the flight starts with is held in this range (safe initial trajectory)
        public float groundClearance = 4f, ceilingAboveStart = 60f;
    }
}
