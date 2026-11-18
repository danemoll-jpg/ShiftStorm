using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 17 item 1: what the items do to a racer, in every form (items: Fun/Items.cs, the held item and its use: RacerItems).
    //   Wrong Form (2 s)  a car becomes a boat (its hull drags on the road), a boat a car (it bobs on the water), a plane a boat (it belly-
    //                     flops: falls at no more than 8 m/s), a sub a boat where there is open sky above (popped up), else its engine floods
    //                     (slow). Then back to the form it had, or to the form a zone passed meanwhile set. Gates still count in the old form,
    //                     the water / bounds / fall-zone resets wait: it never strands or resets anyone.
    //   Bubble Trap       2.2 s floating up (3.5 m/s) in a bubble, nearly stopped, then it pops (a plane flies on at 50 m/s).
    //   Pocket Storm      4 s soaked: the car slips (speed drag + a wobble), the boat chops, the plane buffets, the sub swims in murk.
    //   a knock (the dino egg's raptor, the rocket, the clown, the flood wave, a slick): 1 s spin, the speed cut to 55 %.
    //   After any hit a grace (ImmuneUntil: its end + 2 s): nothing else lands meanwhile. A held Slick / Bubble Shield blocks one hit, a raised
    //   shield one, a mirage's decoys one each.
    //   Rewind, Swap Spot, Buck's Big Finish move the racer (state snapshots; the history is 4 s at 60 Hz).
    public sealed partial class ShiftVehicle
    {
        RacerItems bag; public RacerItems Bag => bag ? bag : (bag = GetComponent<RacerItems>());
        public float ImmuneUntil { get; private set; } = -1;
        public float ForcedUntil { get; private set; } = -1;
        public bool Forced => Time.time < ForcedUntil;
        public float BubbleUntil { get; private set; } = -1;
        public bool Bubbled => Time.time < BubbleUntil;
        public float SoakUntil { get; private set; } = -1;
        public float SpinUntil { get; private set; } = -1;
        public float HypeUntil { get; set; } = -1;
        public float ShieldUntil { get; set; } = -1;
        public int Decoys { get; set; }
        public float DecoysUntil { get; set; } = -1;
        public bool Launching { get; private set; }
        public string Warning { get; private set; } = ""; public float WarningUntil { get; private set; } = -1;
        public int HitsTaken { get; private set; }
        public int HitsBlocked { get; private set; }
        public ItemFx.Lane Lane { get; set; }
        Mode forcedFrom; bool stalled; float funGraceUntil = -1;
        public bool FunHoldsRecovery => Launching || Bubbled || Forced || Time.time < funGraceUntil;
        public event System.Action<ItemKind, ShiftVehicle, string> ItemHit;   // (what, by whom, "hit" / "blocked" / "missed")

        void ResetFun()
        {
            ImmuneUntil = ForcedUntil = BubbleUntil = SoakUntil = SpinUntil = HypeUntil = ShieldUntil = DecoysUntil = WarningUntil = funGraceUntil = -1;
            Decoys = 0; HitsTaken = HitsBlocked = 0; stalled = false; Warning = "";
            if (Launching) { Launching = false; Body.isKinematic = false; }
            if (Lane) { Destroy(Lane.gameObject); Lane = null; }
            history.Clear(); ResetTricks(); if (Bag) Bag.NewRace();
        }
        // a reset / a placing ends what an item was doing to it (a bubble, the wrong form, the cannon); the grace stays
        void EffectsOffForPlace() { BubbleUntil = -1; ForcedUntil = -1; stalled = false; SpinUntil = -1; if (Launching) { Launching = false; } FailTricks("reset"); }
        public void Warn(string text, float seconds) { if (!Human) return; Warning = text; WarningUntil = Time.time + seconds; }

        // ---- a hit by an item (true: it took effect) ----
        public bool Hit(ItemKind kind, ShiftVehicle by)
        {
            if (Finished || Holding || !Course) return false;
            string who = by ? by.RacerName : "?";
            if (Launching || Time.time < ImmuneUntil) { ItemHit?.Invoke(kind, by, "missed"); Items.Note(by, $"{Items.Name(kind)} at {RacerName}: no effect ({(Launching ? "in the cannon" : "grace")})"); return false; }
            string block = Time.time < ShieldUntil ? "bubble shield" : Decoys > 0 && Time.time < DecoysUntil ? "a mirage" : Bag && Items.HeldBehind(Bag.Held) ? "held " + Items.Name(Bag.Held) : "";
            if (block != "")
            {
                if (block == "bubble shield") ShieldUntil = -1; else if (block == "a mirage") Decoys--; else Bag.Clear();
                HitsBlocked++; ImmuneUntil = Time.time + .8f; ItemHit?.Invoke(kind, by, "blocked");
                Items.Note(by, $"{Items.Name(kind)} at {RacerName}: blocked by {block}"); ItemFx.Puff(Body.position, new Color(.6f, .95f, 1f), 1.2f);
                return false;
            }
            float end = Time.time;
            switch (kind)
            {
                case ItemKind.WrongForm: end = ForceForm(2f); break;
                case ItemKind.BubbleTrap when OnLoop: Knock(); end = SpinUntil; break;
                case ItemKind.BubbleTrap: BubbleUntil = end = Time.time + 2.2f; Body.useGravity = false; ItemFx.TrapBubble(this, 2.2f); break;
                case ItemKind.PocketStorm: SoakUntil = end = Time.time + 4f; break;
                default: Knock(); end = SpinUntil; break;
            }
            ImmuneUntil = end + 2f; funGraceUntil = end + .8f; HitsTaken++;
            ItemHit?.Invoke(kind, by, "hit"); FailTricks("hit by " + Items.Name(kind));
            Note("hit", $"hit by {who}'s {Items.Name(kind)} as {Mode}");
            Bag.LastHitBy = by; return true;
        }
        void Knock()
        {
            SpinUntil = Time.time + 1f;
            switch (Mode)
            {
                case Mode.Fly: Airspeed = Mathf.Max(fly.minSpeed, Airspeed * .6f); break;
                case Mode.Dive: DiveSpeed *= .55f; break;
                default: if (!OnLoop) { var lv = Body.linearVelocity; Body.linearVelocity = new Vector3(lv.x * .55f, lv.y, lv.z * .55f); } break;
            }
            TrickSpin(720f, 1f);
        }
        // Wrong Form: the form it is turned into, never one that strands it (returns when it ends)
        float ForceForm(float secs)
        {
            if (OnLoop || Changing) { SoakUntil = Time.time + secs; return SoakUntil; }   // on a rail / mid-change: soaked instead
            Mode to = Mode; stalled = false;
            switch (Mode)
            {
                case Mode.Drive: to = Mode.Boat; break;
                case Mode.Boat: to = Mode.Drive; break;
                case Mode.Fly: to = Mode.Boat; break;
                case Mode.Dive: if (!Physics.Raycast(Body.position, Vector3.up, 60f, GroundMask, QueryTriggerInteraction.Ignore)) to = Mode.Boat; else stalled = true; break;
            }
            forcedFrom = Mode; ForcedUntil = Time.time + secs;
            if (to != Mode) Change(to, "Wrong Form!"); else Note("hit", "Wrong Form: the sub's engine floods (no open sky above)");
            return ForcedUntil;
        }
        void EndForced()
        {
            ForcedUntil = -1; if (stalled) { stalled = false; return; }
            var back = forcedFrom; if (Mode == back) return;
            Change(back, "Wrong Form wears off");
            if (back == Mode.Fly) { PitchDeg = 12; Airspeed = Mathf.Max(Airspeed, fly.entrySpeed.x); }
        }
        // a zone passed while in the wrong form: it is the form to go back to
        void ForcedZone(TransformZone z) { LastZone = z; forcedFrom = z.target; }

        // before the mode's own step: true when an effect moves the racer this step instead
        bool StepEffects(float dt)
        {
            if (Launching) { StepLaunch(dt); return true; }
            if (ForcedUntil > 0 && Time.time >= ForcedUntil) EndForced();
            if (Bubbled)
            {
                var lv = Body.linearVelocity; var flat = new Vector3(lv.x, 0, lv.z);
                flat = Vector3.MoveTowards(flat, flat.sqrMagnitude > 1 ? flat.normalized * Mathf.Min(flat.magnitude, 12f) : Vector3.zero, 40f * dt);
                float vy = Mathf.MoveTowards(lv.y, 3.5f, 12f * dt);
                if (Physics.Raycast(Body.position, Vector3.up, 4f, GroundMask, QueryTriggerInteraction.Ignore)) vy = Mathf.Min(vy, 0);
                if (Mode == Mode.Dive && WaterVolume.SurfaceAt(Body.position, out float s, 50f) && Body.position.y > s - 2.5f) vy = Mathf.Min(vy, 0);
                Body.linearVelocity = flat + Vector3.up * vy; Body.angularVelocity = Vector3.zero;
                return true;
            }
            if (BubbleUntil > 0 && Time.time >= BubbleUntil)
            {
                BubbleUntil = -1; Body.useGravity = Mode == Mode.Drive || Mode == Mode.Boat;
                if (Mode == Mode.Fly) Airspeed = Mathf.Max(50f, fly.entrySpeed.x); if (Mode == Mode.Dive) DiveSpeed = 15f;
                Note("hit", "the bubble pops"); ItemFx.Puff(Body.position, new Color(.7f, .9f, 1f), 2f);
            }
            return false;
        }
        // after the mode's own step: soaked, spun, a car bobbing, a plane-turned-boat falling slowly, the rainbow lane
        void AfterEffects(float dt)
        {
            float now = Time.time; var p = Body.position;
            if (Forced)
            {
                var lv = Body.linearVelocity;
                if (Mode == Mode.Boat && !OnWater && lv.y < -8f) Body.linearVelocity = new Vector3(lv.x, -8f, lv.z);
                if (Mode == Mode.Drive && WaterVolume.SurfaceAt(p, out float s, 2f) && p.y < s + .4f)
                {
                    float a = 9.81f + (s + .25f - p.y) * 9f - lv.y * 3f; Body.AddForce(Vector3.up * Mathf.Clamp(a, 0, 30f), ForceMode.Acceleration);
                    Body.AddForce(-new Vector3(lv.x, 0, lv.z) * .9f, ForceMode.Acceleration);
                }
                if (stalled) DiveSpeed = Mathf.MoveTowards(DiveSpeed, 10f, 40f * dt);
            }
            if (now < SoakUntil)
            {
                float w = Mathf.Sin(now * 7.3f) * Mathf.Sin(now * 2.1f + 1);
                switch (Mode)
                {
                    case Mode.Fly: Airspeed *= 1 - .25f * dt; yawDeg += w * 28f * dt; PitchDeg += Mathf.Sin(now * 9f) * 14f * dt; break;
                    case Mode.Dive: DiveSpeed *= 1 - .3f * dt; break;
                    default:
                        { var lv = Body.linearVelocity; Body.AddForce(-new Vector3(lv.x, 0, lv.z) * .3f, ForceMode.Acceleration); Body.AddTorque(Vector3.up * w * 2.2f, ForceMode.Acceleration); if (Mode == Mode.Boat && OnWater) Body.AddForce(Vector3.up * Mathf.Max(0, Mathf.Sin(now * 11f)) * 9f, ForceMode.Acceleration); }
                        break;
                }
            }
            if (Lane)
            {
                if (!Lane.On(this)) { if (Lane.Done) { Destroy(Lane.gameObject); Lane = null; } }
                else
                {
                    float top = BoostTopSpeedNow * 1.06f; Boost = Mathf.Min(1, Boost + .25f * dt);
                    switch (Mode)
                    {
                        case Mode.Fly: Airspeed = Mathf.MoveTowards(Airspeed, Mathf.Max(Airspeed, top), 30f * dt); break;
                        case Mode.Dive: DiveSpeed = Mathf.MoveTowards(DiveSpeed, Mathf.Max(DiveSpeed, top), 20f * dt); break;
                        default: if (Speed < top && !OnLoop) Body.AddForce(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * 28f, ForceMode.Acceleration); break;
                    }
                }
            }
            Record();
        }

        // ---- history (Rewind, the Mirage's decoys) ----
        public struct Snap
        {
            public float t; public Vector3 pos, vel; public Quaternion rot; public Mode mode; public float air, dive, yaw, pitch, roll, prog, best; public int gates, lap; public Shortcut sc; public float scS; public TransformZone zone; public bool rail;
        }
        readonly List<Snap> history = new List<Snap>();
        public IReadOnlyList<Snap> History => history;
        Snap Now() => new Snap
        {
            t = Time.time, pos = Body.position, vel = Body.linearVelocity, rot = Body.rotation, mode = Mode, air = Airspeed, dive = DiveSpeed, yaw = yawDeg, pitch = PitchDeg, roll = RollDeg,
            prog = Progress, best = BestProgress, gates = GatesPassed, lap = Lap, sc = OnShortcut, scS = scS, zone = LastZone, rail = OnLoop || Launching || Changing
        };
        void Record() { history.Add(Now()); while (history.Count > 0 && history[0].t < Time.time - 4f) history.RemoveAt(0); }
        void Apply(Snap s, string why)
        {
            ClearStunts(); Changing = false; Authority = 1; CarryTarget = -1; EnteredDriveAt = -1;
            Body.isKinematic = true; transform.SetPositionAndRotation(s.pos, s.rot); Body.position = s.pos; Body.rotation = s.rot; Body.isKinematic = false;
            if (s.mode != Mode) { Mode = s.mode; ChangeFrom = s.mode; Forms?.Snap(s.mode); }
            Body.useGravity = Mode == Mode.Drive || Mode == Mode.Boat; Body.linearVelocity = s.vel; Body.angularVelocity = Vector3.zero;
            Airspeed = s.air; DiveSpeed = s.dive; yawDeg = s.yaw; PitchDeg = s.pitch; RollDeg = s.roll;
            int g0 = GatesPassed;
            GatesPassed = s.gates; Progress = s.prog; BestProgress = lastBest = Mathf.Max(s.best, s.prog); OnShortcut = s.sc; scS = s.scS; LastZone = s.zone;
            if (GatesPassed > g0) for (int i = g0; i < GatesPassed; i++) GateLog.Add(Course.gates[i].index);
            else while (GatesPassed < g0 && GateLog.Count > 0 && g0-- > GatesPassed) GateLog.RemoveAt(GateLog.Count - 1);
            lastPos = s.pos; MissedGate = null; missedFor = 0; stuckFor = wrongWaterFor = noProgressFor = OutOfBoundsFor = 0; ClearRoute();
            GraceUntil = Time.time + 1f; SetLayer(GhostLayer); funGraceUntil = Time.time + .8f;
            Note("item", why);
        }
        // Rewind: back to where it was 3 s ago (on this lap, past the same gates, on the same branch, not mid-rail); false: nothing to undo
        public bool Rewind()
        {
            if (Launching || OnLoop) return false;
            Snap? pick = null;
            foreach (var s in history) { if (s.t > Time.time - .8f) break; if (s.gates != GatesPassed || s.lap != Lap || s.sc != OnShortcut || s.rail) continue; if (pick == null || s.t <= Time.time - 3f) pick = s; if (s.t > Time.time - 3f && pick != null) break; }
            if (pick == null) return false;
            var sp = pick.Value; float back = Time.time - sp.t;
            Apply(sp, $"Rewind: back {back:0.0} s ({Vector3.Distance(sp.pos, Body.position):0} m)");
            history.Clear(); FailTricks("rewind"); return true;
        }
        // Swap Spot: trade places (and forms, speeds, race progress) with the racer ahead; false when either is somewhere it cannot happen
        public bool CanSwap => !Launching && !OnLoop && !Changing && !Bubbled && !Finished && !Holding && (!OnShortcut || OnShortcut.ownAnchors.Count == 0);
        public bool SwapWith(ShiftVehicle o)
        {
            if (!o || !CanSwap || !o.CanSwap || o.Lap != Lap || o.Course != Course) return false;
            var a = Now(); var b = o.Now();
            Apply(b, $"Swap Spot: swapped with {o.RacerName}"); o.Apply(a, $"Swap Spot: swapped with {RacerName}");
            o.FailTricks("swapped"); FailTricks("swapped");
            ItemFx.Puff(a.pos, new Color(1f, .85f, .3f), 2.5f); ItemFx.Puff(b.pos, new Color(1f, .85f, .3f), 2.5f);
            return true;
        }

        // ---- Buck's Big Finish: his stunt cannon fires the racer up the field, to the furthest anchor within 450 m that is not past the gate
        // after the next one; the gate passed on the way counts ----
        RecoveryAnchor launchTo; Vector3 launchA; float launchStart, launchDur;
        public bool BigFinish()
        {
            if (!Course || Launching || OnLoop || Changing || Finished || OnShortcut) return false;
            int lim = Mathf.Min(GatesPassed + 1, Course.gates.Count - 1);
            float limit = Course.gates[lim].progress - 25f; RecoveryAnchor pick = null;
            foreach (var an in Course.anchors) if (an.progress > Progress + 120f && an.progress < Progress + 450f && an.progress < limit && an.mode != Mode.Fly) pick = an;
            if (!pick) return false;
            launchTo = pick; launchA = Body.position; launchStart = Time.time; launchDur = 1.7f; Launching = true;
            Body.isKinematic = true; FailTricks("cannon"); funGraceUntil = Time.time + 3f; ImmuneUntil = Time.time + launchDur + 1.5f;
            Note("item", $"Buck's Big Finish: fired {pick.progress - Progress:0} m up the course");
            return true;
        }
        void StepLaunch(float dt)
        {
            float t = (Time.time - launchStart) / launchDur; var b = launchTo.transform.position;
            if (t >= 1f)
            {
                Launching = false; Body.isKinematic = false;
                var gates = Course.gates; while (GatesPassed < gates.Count - 1 && gates[GatesPassed].progress < launchTo.progress) PassGate(gates[GatesPassed], " (Buck's Big Finish)");
                Place(launchTo, launchTo.mode, Mathf.Max(launchTo.speed, BoostTopSpeedNow * .8f));
                if (launchTo.progress > BestProgress) { BestProgress = lastBest = launchTo.progress; }
                GraceUntil = Time.time + shared.graceSeconds; SetLayer(GhostLayer); TrickSpin(360f, .6f);
                return;
            }
            float h = Mathf.Max(30f, Vector3.Distance(launchA, b) * .12f);
            var pos = Vector3.Lerp(launchA, b, t) + Vector3.up * h * 4f * t * (1 - t);
            var next = Vector3.Lerp(launchA, b, Mathf.Min(1, t + .02f)) + Vector3.up * h * 4f * Mathf.Min(1, t + .02f) * (1 - Mathf.Min(1, t + .02f));
            Body.MovePosition(pos); if ((next - pos).sqrMagnitude > 1e-4f) Body.MoveRotation(Quaternion.LookRotation(next - pos, Vector3.up));
            lastPos = pos;
        }
    }
}
