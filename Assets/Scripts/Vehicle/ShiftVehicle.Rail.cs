using UnityEngine;

namespace ShiftStorm
{
    // Round 10 item 4: riding a coaster rail (CoasterRail; course side: CoasterCoast). As the Round 7 loop: speed along the track from the
    // car's own drive force, boost and gravity along the track; the body follows the rail by velocity (sound, camera and contacts still see
    // a moving body); its up is the track's. One lane: a car caught off-centre at the entry eases onto the track's centre. Booster tyres hold
    // the section's minimum speed. At the end it leaves with its speed along the track's last direction (a jump, a launch into flight).
    public sealed partial class ShiftVehicle
    {
        CoasterRail rail; float railS, railV, railLat, railEnterSpeed;
        public bool OnRail => rail != null;
        public CoasterRail Rail => rail;
        public float RailS => railS;
        public int RailsRidden { get; private set; }
        public float RailMinSpeed { get; private set; } = -1;   // the lowest speed on the last rail ridden (checks)

        bool TryEnterRail(Vector3 prev, Vector3 p, float dt)
        {
            foreach (var r in CoasterRail.All)
                if (r.Count > 1 && r.Entered(prev, p, out float lat) && Vector3.Dot(Body.linearVelocity, r.F0) > 0)
                {
                    rail = r; railS = 0; railV = Mathf.Max(r.MinSpeedAt(0), Vector3.Dot(Body.linearVelocity, r.F0)); railLat = lat; railEnterSpeed = railV; RailMinSpeed = railV;
                    Body.useGravity = false; Drifting = false; GroundedWheels = 4;
                    Note("rail", $"onto the {r.label} at {railV:0} m/s ({lat:0.0} m across)"); Stunt?.Invoke("railin");
                    StepRail(dt); return true;
                }
            return false;
        }

        void StepRail(float dt)
        {
            var r = rail; float g = -Physics.gravity.y;
            r.Sample(railS, out _, out var T, out _);
            float top = Boosting ? drive.boostTopSpeed : drive.topSpeed, acc = drive.acceleration + (Boosting ? drive.boostAcceleration : 0);
            float a = Mathf.Max(Input.Throttle, Boosting ? 1 : 0) * acc * Mathf.Clamp01(1 - railV / top) - Input.Brake * drive.braking * .4f - g * T.y;
            if (Input.Throttle < .05f && !Boosting) a -= railV * drive.coastingDrag;
            railV += a * dt;
            float min = r.MinSpeedAt(railS); if (railV < min) railV = Mathf.Min(min, railV + 30f * dt);   // the booster tyres
            railV = Mathf.Clamp(railV, 4f, r.maxSpeed);   // never stops or rolls back on the track
            RailMinSpeed = Mathf.Min(RailMinSpeed, railV);
            railLat = Mathf.MoveTowards(railLat, 0, 4f * dt);
            railS += railV * dt;
            if (railS >= r.Length) { ExitRail(); return; }
            r.Sample(railS, out var at, out var t2, out var n2);
            at += Vector3.Cross(n2, t2).normalized * railLat;
            Body.linearVelocity = (at - Body.position) / dt; Body.angularVelocity = Vector3.zero;
            Body.MoveRotation(Quaternion.LookRotation(t2, n2));
            GroundedWheels = 4;
        }

        void ExitRail()
        {
            var r = rail; rail = null; Body.useGravity = true;
            r.Sample(r.Length, out var at, out var t, out var n);
            Body.position = at; transform.position = at;
            Body.rotation = Quaternion.LookRotation(t, Vector3.ProjectOnPlane(Vector3.up, t).sqrMagnitude > 1e-4f ? Vector3.ProjectOnPlane(Vector3.up, t) : n);
            Body.linearVelocity = t * railV; Body.angularVelocity = Vector3.zero; GroundedWheels = 0;
            RailsRidden++; Note("rail", $"off the {r.label} at {railV:0} m/s (on at {railEnterSpeed:0}, lowest {RailMinSpeed:0})"); Stunt?.Invoke("railout");
        }
    }
}
