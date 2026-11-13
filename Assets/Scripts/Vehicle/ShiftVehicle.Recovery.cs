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
            if (Input.ConsumeReset()) { if (Finished) { if (IsPlayer && Game.Instance) Game.Instance.Restart(); else if (IsPlayer) Begin(Course); } else Recover("reset button"); return; }   // after the finish: start again (the race too)
            if (Finished) return;
            var p = Body.position;
            // out of bounds: at once on land and water and below / above the course; in the air (a much wider corridor) after a few seconds of
            // THIS WAY, so a plane that strays is shown the way back before it is reset (Round 2b)
            bool fly = Mode == Mode.Fly;
            bool hard = false;
            if (OnShortcut ? OnShortcut.OutOfBounds(p, Course, out hard) : Course.OutOfBounds(p, Progress, fly ? Course.AirHalfWidth : Course.halfWidth, out hard))
            {
                OutOfBoundsFor += dt;
                if (hard || !fly || OutOfBoundsFor > shared.airOutOfBoundsSeconds) { Recover("out of bounds"); return; }
            }
            else OutOfBoundsFor = 0;
            bool asking = Input.Throttle > .3f || Input.Brake > .3f;
            stuckFor = asking && Speed < shared.stuckSpeed ? stuckFor + dt : 0;
            if (stuckFor > shared.stuckSeconds) { Recover("stuck"); return; }
            // no new progress: the timer waits while the racer is heading along the route at speed on it (coming back after turning round,
            // as the arrow says), so only a racer going nowhere is reset (Round 2b)
            bool returning = RouteAlign > .7f && Speed > 10f && !OffRoute;
            if (BestProgress > lastBest + .5f) { lastBest = BestProgress; noProgressFor = 0; } else if (!returning) noProgressFor += dt;
            if (noProgressFor > shared.noProgressSeconds) { Recover("no progress"); return; }
            bool wrongWater = (Mode == Mode.Drive || Mode == Mode.Fly) && WaterVolume.SurfaceAt(p, out float s, 0f) && p.y < s - .2f && !TransformZone.InCorridor(p);
            wrongWaterFor = wrongWater ? wrongWaterFor + dt : 0;
            if (wrongWaterFor > shared.wrongWaterSeconds) { Recover("into the water outside the landing corridor"); return; }
        }

        public void Recover(string reason)
        {
            var anchor = Course.AnchorFor(BestProgress, GatesPassed);
            bool wasChanging = Changing; var at = Body.position; float sp = Speed;
            Resets++; LastResetReason = reason;
            Place(anchor, anchor.mode, shared.resetSpeed);
            GraceUntil = Time.time + shared.graceSeconds; SetLayer(GhostLayer);
            Note("reset", $"reset ({reason}{(wasChanging ? ", during a change" : "")}, at {p3(at)}, {sp:0} m/s, {OffLine:0} m off the line) to the anchor at {anchor.progress:0} m in {anchor.mode}");
        }

        // Checks only: put the vehicle somewhere by hand (the skip / out-of-order tests), in a form, at a speed along its facing. Race progress
        // (gates, best progress) is kept: this is how a cheat would look to the race logic.
        public void DebugPlace(Vector3 pos, Quaternion rot, Mode mode, float speed, float hint = -1)
        {
            var g = new GameObject("check place"); g.transform.SetPositionAndRotation(pos, rot); var a = g.AddComponent<RecoveryAnchor>(); a.mode = mode; a.progress = Course.Project(pos, hint >= 0 ? hint : Progress, out _, out _);
            float best = BestProgress; Place(a, mode, speed); BestProgress = Mathf.Max(best, Progress); lastBest = BestProgress; Destroy(g);
            Note("check", $"placed by the check at {p3(pos)} as {mode}");
        }

        // Put the vehicle at an anchor in a form, at a speed along the anchor's facing; no change animation (the form snaps).
        void Place(RecoveryAnchor anchor, Mode mode, float speed, Vector3 offset = default)
        {
            Changing = false; Authority = 1; assistLeft = 0; OnShortcut = null;
            Body.isKinematic = true;
            var pos = anchor.transform.position + anchor.transform.rotation * offset;
            transform.SetPositionAndRotation(pos, anchor.transform.rotation);
            Body.position = pos; Body.rotation = anchor.transform.rotation;
            Body.isKinematic = false;
            Body.angularVelocity = Vector3.zero;
            var v = anchor.transform.forward * speed;
            Mode = mode; ChangeFrom = mode;
            Body.linearVelocity = v; scrapeUntil = -1; CarryTarget = -1; EnteredDriveAt = -1;
            switch (mode)
            {
                case Mode.Drive: EnterDrive(speed); break;
                case Mode.Boat: EnterBoat(speed); break;
                case Mode.Fly: EnterFly(Mathf.Max(speed, fly.entrySpeed.x), true); break;
                case Mode.Dive: EnterDive(speed); break;
            }
            Forms?.Snap(mode);
            LastZone = null; stuckFor = wrongWaterFor = noProgressFor = 0; lastBest = BestProgress; ClearRoute();
            lastPos = Body.position; Progress = Course ? (anchor.progress > 0 ? anchor.progress : Course.Project(lastPos, out _, out _)) : 0;
            MissedGate = null; missedFor = 0;
            HeadingDeg = anchor.transform.eulerAngles.y;
        }
    }
}
