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
            if (Input.ConsumeReset()) { if (Finished) { if (Human && Game.Instance) Game.Instance.ResetAfterFinish(this); else if (IsPlayer) Begin(Course); } else Recover("reset button"); return; }   // after the finish: start again (the race too)
            if (Finished) return;
            if (rail != null) { OutOfBoundsFor = 0; stuckFor = 0; noProgressFor = 0; return; }   // Round 10: locked to a coaster rail (it never stalls; it runs to its end)
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
            if (noProgressFor > shared.noProgressSeconds || (noProgressFor > Course.LostSeconds && Course.InLost(BestProgress))) { Recover(Course.InLost(BestProgress) ? "lost (no progress)" : "no progress"); return; }   // Round 13: lost under the pier: sooner
            bool wrongWater = (Mode == Mode.Drive || Mode == Mode.Fly) && WaterVolume.SurfaceAt(p, out float s, 0f) && p.y < s - .2f && !TransformZone.InCorridor(p);
            wrongWaterFor = wrongWater ? wrongWaterFor + dt : 0;
            if (wrongWaterFor > shared.wrongWaterSeconds) { Recover("into the water outside the landing corridor"); return; }
        }

        // Round 12 item 3 (Dan: the flying shortcut "kept ... resetting me back too far and kept making me repeat the same sequence"):
        //   on (or within 8 s of leaving) a flight branch, a reset goes FORWARD to the last anchor before the branch's rejoin (a branch that
        //   stands in for a gate counts it), with ShiftTuning.branchRecoveryPenalty seconds added: never back behind the jump just made;
        //   in the air anywhere else, a reset never goes more than flightResetMaxBack metres back along the line: when the earned anchor is
        //   further back it restarts on the flight line itself, 30 m before min(best progress, the next gate), flying.
        // a point of the line where a plane may restart: the next gate takes a plane, or the line there is well up in the air (8 m over the
        // ground and 6 m over any water), so the plane flies on into the next form's zone as it would have
        public static bool FlightPointOk(Course c, float p, ProgressGate next)
        {
            if (next == null || next.Allows(Mode.Fly)) return true;
            var q = c.PointAt(p, out _);
            if (Physics.Raycast(q, Vector3.down, 8f, GroundMask, QueryTriggerInteraction.Ignore)) return false;
            return !(WaterVolume.SurfaceAt(q, out float s, 0f) && q.y - s < 6f);
        }
        public string LastResetHow { get; private set; } = "";
        public float LastResetBack { get; private set; }   // metres of progress the last reset cost (negative: it went forward)
        RecoveryAnchor flightAnchor;
        public void Recover(string reason)
        {
            float best0 = BestProgress; string how = "anchor";
            // Round 13 item 3: in the Mangrove bypass a reset restarts in it (its own anchors, a submarine on its line)
            var own = OnShortcut; RecoveryAnchor ownPick = null; float ownS = 0;
            if (own && own.ownAnchors.Count > 0) foreach (var (s, a) in own.ownAnchors) if (s <= scS - 5f) { ownPick = a; ownS = s; }
            if (ownPick)
            {
                bool wc = Changing; var at0 = Body.position; float sp0 = Speed;
                Resets++; LastResetReason = reason; LastResetHow = $"in the {own.label}"; LastResetBack = scS - ownS; leftBranch = null;
                Place(ownPick, ownPick.mode, ownPick.speed >= 0 ? ownPick.speed : shared.resetSpeed);
                EnterShortcut(own, Body.position);
                GraceUntil = Time.time + shared.graceSeconds; SetLayer(GhostLayer);
                Note("reset", $"reset ({reason}{(wc ? ", during a change" : "")}, at {p3(at0)}, {sp0:0} m/s) to the {own.label}'s own anchor {ownS:0} m along it in {ownPick.mode}, {LastResetBack:0} m back along it");
                return;
            }
            var anchor = BranchAnchor(out var br);
            // Round 13 item 5c: lost / stuck / out of bounds under Coaster Coast's decks and pier: forward to the next anchor on the route that is
            // not past an unearned gate (never back, never a loop); no time added
            if (!anchor && Course.InLost(BestProgress) && (reason.StartsWith("lost") || reason == "stuck" || reason == "out of bounds" || reason == "reset button" || reason.StartsWith("missed gate")))
            {
                float lim = GatesPassed < Course.gates.Count ? Course.gates[GatesPassed].progress : Course.Length; RecoveryAnchor fwd = null;
                foreach (var a in Course.anchors) if (a.progress > BestProgress + 5f && a.progress <= lim + .5f) { fwd = a; break; }
                if (fwd)
                {
                    bool wc = Changing; var at0 = Body.position; float sp0 = Speed;
                    Resets++; LastResetReason = reason; LastResetHow = "forward (lost)"; LastResetBack = best0 - fwd.progress; leftBranch = null;
                    Place(fwd, fwd.mode, fwd.speed >= 0 ? fwd.speed : shared.resetSpeed);
                    if (fwd.progress > BestProgress) { BestProgress = fwd.progress; lastBest = BestProgress; }
                    GraceUntil = Time.time + shared.graceSeconds; SetLayer(GhostLayer); ShowCue("BACK ON COURSE", CueKind.Guide, 2.5f);
                    Note("reset", $"reset ({reason}{(wc ? ", during a change" : "")}, at {p3(at0)}, {sp0:0} m/s, {OffLine:0} m off the line) forward to the anchor at {fwd.progress:0} m in {fwd.mode}, {fwd.progress - best0:0} m ahead of the best progress");
                    return;
                }
            }
            if (anchor)
            {
                how = $"forward from the {br.label}";
                if (br.replacesGate >= 0 && GatesPassed == br.replacesGate) { var g = Course.gates[GatesPassed]; GatesPassed++; GateLog.Add(g.index); ShortcutsTaken.Add(br.label + " (recovered)"); Note("gate", $"gate {g.label} counted by the {br.label}'s forward recovery ({GatesPassed}/{Course.gates.Count})"); }
                Penalty += shared.branchRecoveryPenalty; NearMisses.Add(br.label.Replace(" ", "-") + "-reset"); MissCueUntil = Time.time + 2.5f;
            }
            else
            {
                anchor = Course.AnchorFor(BestProgress, GatesPassed);
                float limit = BestProgress; var ng = NextGate; if (ng) limit = Mathf.Min(limit, ng.progress);
                if (!OldFlight && anchor.mode == Mode.Fly && limit - anchor.progress > shared.flightResetMaxBack && FlightPointOk(Course, limit - 30f, ng))
                {
                    if (!flightAnchor) { var go = new GameObject("flight reset point"); go.transform.SetParent(transform.parent, false); flightAnchor = go.AddComponent<RecoveryAnchor>(); flightAnchor.mode = Mode.Fly; flightAnchor.speed = -1; }
                    float p = limit - 30f; var q = Course.PointAt(p, out var d); flightAnchor.transform.SetPositionAndRotation(q, Quaternion.LookRotation(d.sqrMagnitude > 1e-4f ? d : Vector3.forward, Vector3.up)); flightAnchor.progress = p;
                    anchor = flightAnchor; how = "on the flight line";
                }
            }
            bool wasChanging = Changing; var at = Body.position; float sp = Speed;
            Resets++; LastResetReason = reason; LastResetHow = how; LastResetBack = best0 - anchor.progress; leftBranch = null;
            Place(anchor, anchor.mode, anchor.speed >= 0 ? anchor.speed : shared.resetSpeed);
            if (anchor.progress > BestProgress) { BestProgress = anchor.progress; lastBest = BestProgress; }
            GraceUntil = Time.time + shared.graceSeconds; SetLayer(GhostLayer);
            if (how != "anchor") ShowCue(how.StartsWith("forward") ? $"BACK ON COURSE +{shared.branchRecoveryPenalty:0} s" : "BACK ON COURSE", CueKind.Guide, 2.5f);
            Note("reset", $"reset ({reason}{(wasChanging ? ", during a change" : "")}, at {p3(at)}, {sp:0} m/s, {OffLine:0} m off the line) to {(how == "anchor" ? "the anchor" : how == "on the flight line" ? "the flight line" : $"the anchor ({how}, +{shared.branchRecoveryPenalty:0} s)")} at {anchor.progress:0} m in {anchor.mode}, {best0 - anchor.progress:0} m behind the best progress");
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
            Changing = false; Authority = 1; assistLeft = 0; OnShortcut = null; ClearStunts();
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
