using UnityEngine;

namespace ShiftStorm
{
    // Round 7 items 1-2: the loop, the boost pad, the jumps and the choice jump's wings (course side: ParadisePlunge.Stunts).
    //   The loop: a car crossing its entry plane rides a rail (LoopRail): speed along the track with the car's own drive force, boost and
    //   real gravity along the track; its up is the track's (towards the loop's centre), steering only slides it a little across the
    //   track; the body follows the rail by velocity, so speed, sound, camera and collisions with other cars still see a moving body.
    //   Grip: the track can only push, so the car keeps it while v^2 / R + g cos(th) > -assist (a little "stick"); too slow or lost grip:
    //   it slides back down the track (never a fall) and after at most 2 s is reset at the anchor before the loop's boost pad.
    //   Jumps: after more than 0.35 s in the air as a car, the landing holds the flat speed it had in the air for 0.25 s (the hull's
    //   touch would otherwise brake it), and the airtime is logged. Choice jump: crossing its lip opens the wings window (1 s); LB / E in
    //   it, still in the air, changes into the plane. Anywhere else LB does nothing (logged once per press).
    public sealed partial class ShiftVehicle
    {
        public bool OnLoop => loop != null || rail != null;   // Round 10: a coaster rail counts as the loop (camera, no braking, no wrong way)
        public LoopRail Loop => loop;
        public float LoopAngle => loopTh;
        public bool LoopSlipping => loopSlip;
        public int Loops { get; private set; }
        public int LoopFails { get; private set; }
        public float WingsUntil { get; private set; } = -1;
        public bool WingsOpen => Mode == Mode.Drive && Time.time < WingsUntil && !Finished;
        // Round 14 item 5: the window opens on the ramp (the 45 m kicker before the lip: the HUD's "LB: FLY" is up there) and runs 1.5 s
        // into the air; LB pressed on the ramp is kept (armed) and opens the wings the moment the car leaves the lip
        public bool WingsArmed { get; private set; }
        public bool OnChoiceRamp { get; private set; }
        public int WingsArmedPresses { get; private set; }
        public static readonly bool OldChoice = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldChoice") >= 0;   // checks: build 15's window (from the lip, 1 s, in the air only)
        public int WingsUsed { get; private set; }
        public int WingsIgnored { get; private set; }
        public int Jumps { get; private set; }
        public float LastAirTime { get; private set; }
        public float LastLandingIn { get; private set; }
        public float LastLandingOut { get; private set; } = -1;
        public float LoopCam { get; private set; }   // 0 .. 1: how much the chase camera follows the car's own up (the loop)
        public event System.Action<string> Stunt;    // "loop", "loopfail", "jump", "wings", "pad" (the caller, sounds)
        const float LoopAssist = .35f;   // in g: the "stick" the track gives before the grip is lost

        LoopRail loop; float loopTh, loopV, loopLat, loopSlipAt, loopEnterSpeed; bool loopSlip;
        BoostPad lastPad; Vector3 stuntPrev; bool stuntPrevSet;
        float airFor, airFlat, landHoldUntil = -1, landHold; Vector3 airDir;

        void WingsOut(Vector3 p, string why) { WingsUsed++; WingsUntil = -1; WingsArmed = false; Change(Mode.Fly, why); Stunt?.Invoke("wings"); EnterChoiceBranch(p); }

        void ClearStunts()
        {
            if (loop || rail) { loop = null; rail = null; Body.useGravity = Mode == Mode.Drive || Mode == Mode.Boat; }
            loopSlip = false; lastPad = null; stuntPrevSet = false; WingsUntil = -1; WingsArmed = false; OnChoiceRamp = false; airFor = 0; landHoldUntil = -1;
        }

        // before the mode's own step; true: the loop rail moved the car this step (the mode's step is skipped)
        bool StepStunts(float dt)
        {
            var p = Body.position; var prev = stuntPrevSet ? stuntPrev : p; stuntPrev = p; stuntPrevSet = true;
            LoopCam = Mathf.MoveTowards(LoopCam, OnLoop ? 1 : 0, dt / .35f);
            if (!FunHoldsRecovery) foreach (var z in ResetZone.All) if (z.Contains(p)) { Recover(z.label); return true; }   // Round 17: not while an item's effect holds it
            // the wings button
            if (Input.ConsumeWings())
            {
                if (WingsOpen && GroundedWheels == 0 && !Changing) { WingsOut(p, "wings out (choice jump)"); }
                else if (!OldChoice && WingsOpen && !Changing) { if (!WingsArmed) { WingsArmed = true; WingsArmedPresses++; Note("wings", "wings armed on the ramp: out at the lip"); } }
                else { WingsIgnored++; Note("wings", $"wings pressed: nothing here ({(Mode != Mode.Drive ? Mode.ToString() : WingsUntil > 0 && Time.time >= WingsUntil ? "after the window" : GroundedWheels > 0 && WingsOpen ? "still on the lip" : "not a choice jump")})"); }
            }
            if (rail) { StepRail(dt); return true; }
            if (loop) { StepLoop(dt); return true; }
            if (Mode != Mode.Drive) { airFor = 0; return false; }
            bool ramp = false;
            foreach (var l in ChoiceLip.All)
            {
                if (l.Contains(p)) { if (WingsUntil < Time.time) Note("wings", "choice jump: wings window open"); WingsUntil = Time.time + (OldChoice ? 1f : l.window); }
                else if (!OldChoice && l.OnRamp(p)) { ramp = true; if (WingsUntil < Time.time) Note("wings", "choice jump: wings window open (on the ramp)"); WingsUntil = Mathf.Max(WingsUntil, Time.time + .25f); }
            }
            OnChoiceRamp = ramp;
            if (WingsArmed && !WingsOpen) WingsArmed = false;   // the window closed (stopped on the ramp, a reset)
            if (WingsArmed && WingsOpen && GroundedWheels == 0 && !Changing) WingsOut(p, "wings out (choice jump, pressed on the ramp)");
            // the loop's entry plane
            foreach (var r in LoopRail.All)
                if (r.Entered(prev, p, out float lat) && Vector3.Dot(Body.linearVelocity, r.F) > 0) { EnterLoop(r, lat); StepLoop(dt); return true; }
            if (TryEnterRail(prev, p, dt)) return true;   // Round 10: a coaster rail's entry plane
            // the boost pad
            BoostPad onPad = null; foreach (var b in BoostPad.All) if (b.Contains(p)) onPad = b;
            if (onPad && onPad != lastPad)
            {
                var v = Body.linearVelocity; var flat = new Vector3(v.x, 0, v.z); float sp = flat.magnitude;
                if (sp < onPad.speed) { var dir = sp > 1 ? flat / sp : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized; Body.linearVelocity = dir * onPad.speed + Vector3.up * v.y; }
                Note("pad", $"boost pad: {sp:0} -> {Mathf.Max(sp, onPad.speed):0} m/s"); Stunt?.Invoke("pad");
            }
            lastPad = onPad;
            // airtime and the landing's speed hold
            var lv = Body.linearVelocity; float fl = new Vector2(lv.x, lv.z).magnitude;
            if (GroundedWheels == 0) { airFor += dt; airFlat = fl; if (fl > 1f) airDir = new Vector3(lv.x, 0, lv.z) / fl; if (airFor >= .35f && airFor - dt < .35f) Stunt?.Invoke("air"); }
            else
            {
                if (airFor > .35f && !(EnteredDriveAt > 0 && Time.time - EnteredDriveAt < shared.wheelsDownSeconds))
                {
                    landHold = airFlat; landHoldUntil = Time.time + .25f; LastAirTime = airFor; LastLandingIn = airFlat;
                    if (airFor > .8f) { Jumps++; Note("jump", $"landed after {airFor:0.00} s in the air at {airFlat:0} m/s"); Stunt?.Invoke("jump"); }
                }
                airFor = 0;
            }
            if (Time.time < landHoldUntil && fl < landHold && fl > 1f) HoldLanding(lv, fl, landHold, airDir);   // Round 13: in the way it came down
            else if (landHoldUntil > 0 && Time.time >= landHoldUntil) { LastLandingOut = fl; landHoldUntil = -1; }
            return false;
        }

        void EnterLoop(LoopRail r, float lat)
        {
            loop = r; loopTh = 0; loopV = Mathf.Max(0, Vector3.Dot(Body.linearVelocity, r.F)); loopLat = Mathf.Clamp(lat, -r.half, r.half); loopSlip = false;
            loopEnterSpeed = loopV; Body.useGravity = false; Drifting = false; GroundedWheels = 4;
            Note("loop", $"into the loop at {loopV:0} m/s ({loopLat:0.0} m across)"); Stunt?.Invoke("loopin");
        }

        void StepLoop(float dt)
        {
            var r = loop; float g = -Physics.gravity.y;
            var T = r.Tangent(loopTh); var N = r.Normal(loopTh);
            float cos = Vector3.Dot(N, Vector3.up);
            if (!loopSlip)
            {
                float top = Boosting ? drive.boostTopSpeed : drive.topSpeed, acc = drive.acceleration + (Boosting ? drive.boostAcceleration : 0);
                float a = Mathf.Max(Input.Throttle, Boosting ? 1 : 0) * acc * Mathf.Clamp01(1 - loopV / top) - Input.Brake * drive.braking * .5f - g * T.y;
                if (Input.Throttle < .05f && !Boosting) a -= loopV * drive.coastingDrag;
                loopV += a * dt;
                bool grip = loopV * loopV / r.R + g * cos > -LoopAssist * g;
                if (loopV < 3f || !grip)
                {
                    loopSlip = true; loopSlipAt = Time.time; LoopFails++;
                    Note("loop", $"too slow for the loop ({loopV:0} m/s at {loopTh * Mathf.Rad2Deg:0} deg): slides back");
                    Stunt?.Invoke("loopfail");
                }
            }
            if (loopSlip)
            {
                loopV = Mathf.MoveTowards(loopV, -16f, 14f * dt);
                if (loopTh <= .03f || Time.time - loopSlipAt > 2f) { ExitLoop(false); Recover("too slow for the loop"); return; }
            }
            // steering slides the car across the track a little (people only; a rival keeps its place)
            if (!Input.Scripted) loopLat = Mathf.Clamp(loopLat + Input.Steer * 4f * dt, -r.half, r.half);
            float ds = r.Deriv(loopTh).magnitude;
            loopTh = Mathf.Max(0, loopTh + loopV * dt / Mathf.Max(1f, ds));
            if (loopTh >= Mathf.PI * 2) { ExitLoop(true); return; }
            var at = r.Pos(loopTh) + r.Rt * loopLat;
            Body.linearVelocity = (at - Body.position) / dt; Body.angularVelocity = Vector3.zero;
            var t2 = r.Tangent(loopTh); var n2 = r.Normal(loopTh);
            Body.MoveRotation(Quaternion.LookRotation(loopV >= 0 ? t2 : t2, n2));
        }

        void ExitLoop(bool through)
        {
            var r = loop; loop = null; Body.useGravity = true;
            if (through)
            {
                var at = r.Pos(Mathf.PI * 2) + r.Rt * loopLat; Body.position = at; transform.position = at;
                Body.rotation = Quaternion.LookRotation(r.F, Vector3.up); Body.linearVelocity = r.F * loopV; Body.angularVelocity = Vector3.zero;
                Loops++; Note("loop", $"out of the loop at {loopV:0} m/s (in at {loopEnterSpeed:0})"); Stunt?.Invoke("loop");
            }
            loopSlip = false;
        }
    }
}
