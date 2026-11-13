using UnityEngine;

namespace ShiftStorm
{
    // Manual reset and automatic recovery (stuck, out of bounds, no progress, a car or plane in water outside a landing corridor): back to the last
    // safe anchor earned (Course.AnchorFor: never past an unearned gate), in that anchor's form, at a modest speed, with a short grace period with
    // no obstacle collisions (the Ghost layer). A reset in the middle of a change cancels the change and snaps to the anchor's form. The zone
    // memory is cleared, so the next zone ahead fires again.
    public sealed partial class ShiftVehicle
    {
        public string LastResetReason { get; private set; } = "";

        void Recovery(float dt)
        {
            if (!Course) return;
            if (Input.ConsumeReset()) { if (Finished) Begin(Course); else Recover("reset button"); return; }   // after the finish: start again
            if (Finished) return;
            var p = Body.position;
            if (Course.OutOfBounds(p)) { Recover("out of bounds"); return; }
            bool asking = Input.Throttle > .3f || Input.Brake > .3f;
            stuckFor = asking && Speed < shared.stuckSpeed ? stuckFor + dt : 0;
            if (stuckFor > shared.stuckSeconds) { Recover("stuck"); return; }
            if (BestProgress > lastBest + .5f) { lastBest = BestProgress; noProgressFor = 0; } else noProgressFor += dt;
            if (noProgressFor > shared.noProgressSeconds) { Recover("no progress"); return; }
            bool wrongWater = (Mode == Mode.Drive || Mode == Mode.Fly) && WaterVolume.SurfaceAt(p, out float s, 0f) && p.y < s - .2f && !TransformZone.InCorridor(p);
            wrongWaterFor = wrongWater ? wrongWaterFor + dt : 0;
            if (wrongWaterFor > shared.wrongWaterSeconds) { Recover("into the water outside the landing corridor"); return; }
        }

        public void Recover(string reason)
        {
            var anchor = Course.AnchorFor(BestProgress, GatesPassed);
            bool wasChanging = Changing;
            Resets++; LastResetReason = reason;
            Place(anchor, anchor.mode, shared.resetSpeed);
            GraceUntil = Time.time + shared.graceSeconds; SetLayer(GhostLayer);
            Note("reset", $"reset ({reason}{(wasChanging ? ", during a change" : "")}) to the anchor at {anchor.progress:0} m in {anchor.mode}");
        }

        // Put the vehicle at an anchor in a form, at a speed along the anchor's facing; no change animation (the form snaps).
        void Place(RecoveryAnchor anchor, Mode mode, float speed)
        {
            Changing = false; Authority = 1; assistLeft = 0;
            Body.isKinematic = true;
            transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation);
            Body.position = anchor.transform.position; Body.rotation = anchor.transform.rotation;
            Body.isKinematic = false;
            Body.angularVelocity = Vector3.zero;
            var v = anchor.transform.forward * speed;
            Mode = mode; ChangeFrom = mode;
            Body.linearVelocity = v;
            switch (mode)
            {
                case Mode.Drive: EnterDrive(speed); break;
                case Mode.Boat: EnterBoat(speed); break;
                case Mode.Fly: EnterFly(Mathf.Max(speed, fly.entrySpeed.x), true); break;
                case Mode.Dive: EnterDive(speed); break;
            }
            Forms?.Snap(mode);
            LastZone = null; stuckFor = wrongWaterFor = noProgressFor = 0; lastBest = BestProgress;
            lastPos = Body.position; Progress = Course ? Course.Project(lastPos, out _, out _) : 0;
            HeadingDeg = anchor.transform.eulerAngles.y;
        }
    }
}
