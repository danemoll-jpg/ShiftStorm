using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 17 item 2 (Dan: "I would want to lean more towards tricks ... build up points so a track isn't just about being first but also
    // who can build up stunt points"): tricks and stunt points. In the air in any form the right stick (T / F / G / H) does a trick; on the
    // ground it still looks around (CameraViews ignores it in the air).
    //   car   left / right: Spin (360 about its up), forward: Front flip, back: Back flip
    //   boat  pull back on the left stick on the water at speed = a wave hop; in the air: Spin, Flip
    //   plane left / right: Barrel roll, back: Loop, forward: Bunt (an outside loop)
    //   sub   at speed just under open water, pull up: it breaches (a leap out of the water); in the air: Corkscrew, Flip
    // A trick is the model turning (looks only: the hull, its physics and the race never turn with it; FormSet's Forms object, ShiftVehicle.
    // Drive's LateUpdate). Holding the stick chains the next one. Tricks chain into a combo (x1, x1.5, x2 ...; the same trick a third time and
    // on counts 60 %); land clean (the trick at least 80 % round) = the combo's points banked, boost and a little speed; land mid-trick =
    // WIPEOUT: the points lost, a wobble and a speed cut. A plane's combo banks 1.2 s after its last trick. A trick while the machine changes
    // form = SHIFT TRICK: +500 and x1.5. The Hype Horn doubles it all. The AI does tricks too (by difficulty).
    public sealed partial class ShiftVehicle
    {
        public bool TrickAir { get; private set; }
        public bool TrickBusy => trick != null;
        public string TrickName => trick != null ? trick.name : "";
        public int StuntScore { get; private set; }
        public int ComboCount => combo.Count;
        public int ComboPoints { get { float s = trick != null ? trick.pts : 0; foreach (var c in combo) s += c.pts; return Mathf.RoundToInt(s); } }   // the trick turning now included
        public float ComboMultiplier => combo.Count == 0 ? 1 : (1f + .5f * (combo.Count - 1)) * (comboShift ? 1.5f : 1f) * (Time.time < HypeUntil ? 2f : 1f);
        public bool ComboShift => comboShift;
        public int TricksLanded { get; private set; }
        public int TricksFailed { get; private set; }
        public int ShiftTricks { get; private set; }
        public int BestCombo { get; private set; }
        public int Breaches { get; private set; }
        public int Hops { get; private set; }
        public bool Breaching => breaching;
        public string Pop { get; private set; } = ""; public float PopUntil { get; private set; } = -1; public Color PopColor { get; private set; } = Color.white;
        public readonly List<string> TrickLog = new List<string>();
        public event System.Action<string, int, int> TrickEvent;   // ("land" / "fail" / "shift" / "trick", points, tricks in the combo)

        sealed class Trick { public string name; public Vector3 axis; public float deg, dur, t0, pts; public Vector2 dir; }
        Trick trick; readonly List<(string name, float pts)> combo = new List<(string, float)>();
        bool comboShift, wasAir, breaching; float lastTrickEnd = -1, wobbleUntil = -1, knockStart = -1, knockDeg, knockDur, hopCool, aiNextFly; int changesSeen; Vector3 breachVel;
        Quaternion trickVis = Quaternion.identity;
        System.Random trickRnd;

        void ResetTricks() { trick = null; combo.Clear(); comboShift = false; StuntScore = 0; TricksLanded = TricksFailed = ShiftTricks = BestCombo = Breaches = Hops = 0; TrickLog.Clear(); Pop = ""; wobbleUntil = knockStart = -1; breaching = false; changesSeen = Changes; aiDecided = false; }
        public void AddStuntPoints(int pts, string why) { StuntScore += pts; TrickLog.Add($"{Time.time - StartTime:0.0} bonus {pts} ({why})"); }

        float HeightOver()
        {
            var p = Body.position; float h = 40f;
            if (Physics.Raycast(p, Vector3.down, out var hit, 40f, GroundMask, QueryTriggerInteraction.Ignore)) h = hit.distance;
            if (WaterVolume.SurfaceAt(p, out float s, 40f) && p.y > s) h = Mathf.Min(h, p.y - s);
            return h;
        }

        void StepTricks(float dt)
        {
            trickRnd ??= new System.Random(GetHashCode());
            bool can = !Holding && !Finished && !OnLoop && !Launching && !Bubbled && Course && Course.timeTrial;
            bool air = false;
            if (can)
                switch (Mode)
                {
                    case Mode.Drive: air = GroundedWheels == 0 && airFor > .12f && HeightOver() > 1.1f; break;
                    case Mode.Boat: air = HeightOver() > 1.2f && Body.linearVelocity.y > -30f; break;
                    case Mode.Fly: air = HeightOver() > 3f; break;
                    case Mode.Dive: air = breaching; break;
                }
            TrickAir = air;
            // a change mid-trick (or with a combo going): SHIFT TRICK
            if (Changes != changesSeen) { changesSeen = Changes; if (trick != null || (combo.Count > 0 && Time.time - lastTrickEnd < 1f)) { if (!comboShift) { comboShift = true; ShiftTricks++; Popup("SHIFT TRICK! +500", new Color(.6f, 1f, .7f)); TrickEvent?.Invoke("shift", 500, combo.Count); TrickLog.Add($"{Time.time - StartTime:0.0} shift trick ({ChangeFrom}->{Mode})"); } } }
            // the boat's wave hop: pull back on the water at speed
            if (!Human && can && !air && (Mode == Mode.Boat || Mode == Mode.Dive) && !(Course.stunts != null && Course.stunts.Narrow(Progress, Mode)) && !OnShortcut)
            {
                float rate = (Settings.Difficulty == Difficulty.Easy ? .03f : Settings.Difficulty == Difficulty.Normal ? .06f : .1f) * dt;
                bool zoneNear = false; foreach (var z in TransformZone.All) if (z && (z.transform.position - Body.position).sqrMagnitude < 150f * 150f) { zoneNear = true; break; }
                if (zoneNear) aiHop = aiBreach = false; else if (trickRnd.NextDouble() < rate) { if (Mode == Mode.Boat) aiHop = true; else aiBreach = true; }   // never near a form change (a breach over a surfacing pool)
            }
            if (Mode == Mode.Boat && can && !air && OnWater && Speed > 22f && Time.time > hopCool && (Human && !AiDriven ? Input.Vertical > .6f : aiHop))
            { Body.AddForce(Vector3.up * 9f, ForceMode.VelocityChange); hopCool = Time.time + 1.4f; Hops++; aiHop = false; Note("trick", "wave hop"); }
            if (trick != null)
            {
                float t = (Time.time - trick.t0) / trick.dur;
                if (t >= 1f) { Done(trick); trick = null; trickVis = Quaternion.identity; }
                else trickVis = Quaternion.AngleAxis(trick.deg * Smooth(t), trick.axis);
            }
            // landing: clean or a wipeout (a plane: below 3 m with a trick half done)
            if (wasAir && !air)
            {
                if (trick != null) { float t = (Time.time - trick.t0) / trick.dur; if (t >= .8f) { Done(trick); trick = null; trickVis = Quaternion.identity; Bank("landed"); } else Fail("landed mid-" + trick.name); }
                else if (combo.Count > 0) Bank("landed");
            }
            if (Mode == Mode.Fly && trick == null && combo.Count > 0 && Time.time - lastTrickEnd > 1.2f) Bank("flown");
            if (air && trick == null && can)
            {
                var st = Input.TrickHeld;
                if (!Human) st = AiTrick(st);
                if (st.magnitude > .6f) { StartTrick(st); Input.ConsumeTrick(); }
            }
            if (!air) aiDecided = false;
            wasAir = air;
        }
        static float Smooth(float t) => t * t * (3f - 2f * t);

        void StartTrick(Vector2 st)
        {
            bool side = Mathf.Abs(st.x) > Mathf.Abs(st.y); float sx = Mathf.Sign(st.x), sy = Mathf.Sign(st.y);
            var k = new Trick { t0 = Time.time, dir = st };
            switch (Mode)
            {
                case Mode.Fly:
                    if (side) { k.name = "Barrel roll"; k.axis = Vector3.forward; k.deg = -360 * sx; k.dur = .6f; k.pts = 80; }
                    else if (sy < 0) { k.name = "Loop"; k.axis = Vector3.right; k.deg = -360; k.dur = 1.1f; k.pts = 200; }
                    else { k.name = "Bunt"; k.axis = Vector3.right; k.deg = 360; k.dur = 1.1f; k.pts = 220; }
                    break;
                case Mode.Dive:
                    if (side) { k.name = "Corkscrew"; k.axis = Vector3.forward; k.deg = -360 * sx; k.dur = .7f; k.pts = 160; }
                    else { k.name = "Breach flip"; k.axis = Vector3.right; k.deg = 360 * sy; k.dur = .9f; k.pts = 240; }
                    break;
                case Mode.Boat:
                    if (side) { k.name = "Hull spin"; k.axis = Vector3.up; k.deg = 360 * sx; k.dur = .7f; k.pts = 130; }
                    else { k.name = "Boat flip"; k.axis = Vector3.right; k.deg = 360 * sy; k.dur = .85f; k.pts = 200; }
                    break;
                default:
                    if (side) { k.name = "Spin"; k.axis = Vector3.up; k.deg = 360 * sx; k.dur = .7f; k.pts = 100; }
                    else if (sy > 0) { k.name = "Front flip"; k.axis = Vector3.right; k.deg = 360; k.dur = .85f; k.pts = 150; }
                    else { k.name = "Back flip"; k.axis = Vector3.right; k.deg = -360; k.dur = .85f; k.pts = 160; }
                    break;
            }
            int same = 0; foreach (var c in combo) if (c.name == k.name) same++;
            if (same >= 2) k.pts *= .6f;
            trick = k; TrickEvent?.Invoke("trick", Mathf.RoundToInt(k.pts), combo.Count);
        }
        void Done(Trick k) { combo.Add((k.name, k.pts)); lastTrickEnd = Time.time; Popup($"{k.name.ToUpper()}  x{ComboMultiplier:0.#}", new Color(1f, .9f, .4f), .9f); }
        void Bank(string how)
        {
            if (combo.Count == 0) return;
            float baseSum = 0; foreach (var c in combo) baseSum += c.pts;
            int n = combo.Count; float mult = ComboMultiplier; int total = Mathf.RoundToInt((baseSum * mult + (comboShift ? 500 : 0)) / 10f) * 10;
            StuntScore += total; TricksLanded += n; BestCombo = Mathf.Max(BestCombo, total);
            Boost = Mathf.Min(1, Boost + Mathf.Min(.4f, .1f * n));
            float kick = Mathf.Min(8f, 3f + 1.5f * n);
            switch (Mode)
            {
                case Mode.Fly: Airspeed = Mathf.Min(fly.boostTopSpeed, Airspeed + kick); break;
                case Mode.Dive: if (!breaching) DiveSpeed = Mathf.Min(dive.boostTopSpeed, DiveSpeed + kick * .6f); break;
                default: if (Speed < BoostTopSpeedNow) Body.AddForce(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * kick, ForceMode.VelocityChange); break;
            }
            string names = string.Join(" + ", combo.ConvertAll(c => c.name));
            TrickLog.Add($"{Time.time - StartTime:0.0} {how}: {names} x{mult:0.##}{(comboShift ? " +shift" : "")} = {total}");
            Note("trick", $"stunt {how} as {Mode}: {names} x{mult:0.##}{(comboShift ? " + SHIFT TRICK" : "")} = {total} (total {StuntScore})");
            Popup(n > 1 || comboShift ? $"{(comboShift ? "SHIFT " : "")}COMBO x{n}  +{total}" : $"{names.ToUpper()}  +{total}", comboShift ? new Color(.6f, 1f, .7f) : new Color(1f, .85f, .3f), 1.6f);
            TrickEvent?.Invoke("land", total, n);
            combo.Clear(); comboShift = false;
        }
        void Fail(string why)
        {
            float lost = 0; foreach (var c in combo) lost += c.pts; if (trick != null) lost += trick.pts;
            int n = combo.Count + (trick != null ? 1 : 0); lost *= ComboMultiplier;
            trick = null; combo.Clear(); comboShift = false; trickVis = Quaternion.identity; TricksFailed++;
            wobbleUntil = Time.time + .7f;
            switch (Mode)
            {
                case Mode.Fly: Airspeed *= .85f; break;
                case Mode.Dive: DiveSpeed *= .85f; break;
                default: { var lv = Body.linearVelocity; Body.linearVelocity = new Vector3(lv.x * .8f, lv.y, lv.z * .8f); } break;
            }
            TrickLog.Add($"{Time.time - StartTime:0.0} WIPEOUT ({why}): -{lost:0}");
            Note("trick", $"stunt WIPEOUT as {Mode} ({why}): {lost:0} points lost");
            Popup(lost > 0 ? $"WIPEOUT!  -{Mathf.RoundToInt(lost / 10f) * 10}" : "WIPEOUT!", new Color(1f, .4f, .3f), 1.4f);
            TrickEvent?.Invoke("fail", Mathf.RoundToInt(lost), n); Stunt?.Invoke("trickfail");
        }
        // a hit, a reset, a swap: the combo going is lost (no wobble: the hit has its own)
        void FailTricks(string why)
        {
            if (trick == null && combo.Count == 0) return;
            if (why == "reset" || why == "rewind" || why == "swapped" || why == "cannon") { trick = null; combo.Clear(); comboShift = false; trickVis = Quaternion.identity; TrickLog.Add($"{Time.time - StartTime:0.0} combo dropped ({why})"); return; }
            Fail(why);
        }
        void Popup(string text, Color c, float secs = 1.2f) { if (!Human) return; Pop = text; PopColor = c; PopUntil = Time.time + secs; }
        // a knock's spin (the egg's raptor, a rocket, a slick) and the cannon's landing twirl: looks only
        void TrickSpin(float deg, float secs) { knockStart = Time.time; knockDeg = deg; knockDur = secs; }
        // the model's turn this frame (Drive's LateUpdate puts it on the Forms object)
        Quaternion TrickVisual()
        {
            var q = trickVis;
            if (knockStart > 0) { float t = (Time.time - knockStart) / knockDur; if (t >= 1) knockStart = -1; else q = Quaternion.AngleAxis(knockDeg * Smooth(t), Vector3.up) * q; }
            if (Time.time < wobbleUntil) { float k = (wobbleUntil - Time.time) / .7f; q = Quaternion.Euler(Mathf.Sin(Time.time * 31f) * 9f * k, 0, Mathf.Sin(Time.time * 23f) * 14f * k) * q; }
            return q;
        }

        // ---- the sub's breach: at speed just under open water, pull up: out of the water in an arc, back in with a splash ----
        bool StepBreach(float dt)
        {
            var p = Body.position;
            if (!breaching)
            {
                if (Holding || Finished || Changing || Bubbled || DiveSpeed < 24f || !(Human && !AiDriven ? Input.Vertical > .55f : aiBreach)) return false;
                if (!(Human && !AiDriven) && ZoneNear(150f)) { aiBreach = false; return false; }
                if (Time.time - ChangeStartedAt < 1.5f) return false;   // never straight out again at a dive entrance
                if (!WaterVolume.SurfaceAt(p, out float s, 50f) || s - p.y > 4.5f || s - p.y < 0) return false;
                if (Physics.Raycast(p, Vector3.up, 45f, GroundMask, QueryTriggerInteraction.Ignore)) return false;   // a cave roof, a pier, the boardwalk
                var fwd = Quaternion.Euler(0, yawDeg, 0) * Vector3.forward;
                breachVel = fwd * DiveSpeed * .92f + Vector3.up * (9f + DiveSpeed * .06f); breaching = true; Breaches++; aiBreach = false;
                Note("trick", $"breach at {DiveSpeed:0} m/s"); GameSounds.Instance?.Play("splash", Human ? .6f : 0f);
            }
            breachVel.y -= 9.81f * dt; Body.linearVelocity = breachVel; Body.angularVelocity = Vector3.zero;
            var look = breachVel.sqrMagnitude > 1 ? breachVel : Quaternion.Euler(0, yawDeg, 0) * Vector3.forward;
            Body.MoveRotation(Quaternion.LookRotation(look, Vector3.up));
            if (breachVel.y < 0 && WaterVolume.SurfaceAt(p, out float s2, 50f) && p.y < s2 - .8f)
            {
                breaching = false; PitchDeg = -12f; RollDeg = 0; DiveSpeed = new Vector2(breachVel.x, breachVel.z).magnitude; descentLeft = 0;
                if (Human) GameSounds.Instance?.Play("splash", .6f);
            }
            return true;
        }

        // ---- the AI's tricks (by difficulty): a jump with time in the air for it, now and then a roll in flight; sometimes too late ----
        bool aiDecided, aiHop, aiBreach; float aiTrickAt = -1; Vector2 aiDir;
        Vector2 AiTrick(Vector2 st)
        {
            var d = Settings.Difficulty; float chance = d == Difficulty.Easy ? .3f : d == Difficulty.Normal ? .6f : .85f, late = d == Difficulty.Easy ? .25f : d == Difficulty.Normal ? .1f : .03f;
            if (Mode == Mode.Fly)
            {
                if (aiNextFly < 0) aiNextFly = Time.time + (d == Difficulty.Easy ? 28f : d == Difficulty.Normal ? 16f : 9f) * (.6f + (float)trickRnd.NextDouble() * .8f);
                if (Time.time < aiNextFly || Changing || HeightOver() < 25f || (Course.stunts != null && Course.stunts.Narrow(Progress, Mode)) || (OnShortcut && OnShortcut.rings.Count > 0)) return Vector2.zero;
                aiNextFly = -1; return new Vector2(trickRnd.NextDouble() < .5 ? -1 : 1, 0);
            }
            if (!aiDecided)
            {
                aiDecided = true; aiTrickAt = -1;
                float h = HeightOver(), vy = Body.linearVelocity.y, ta = (vy + Mathf.Sqrt(Mathf.Max(0, vy * vy + 2 * 9.81f * h))) / 9.81f;
                bool side = trickRnd.NextDouble() < .55; aiDir = side ? new Vector2(trickRnd.NextDouble() < .5 ? -1 : 1, 0) : new Vector2(0, trickRnd.NextDouble() < .5 ? -1 : 1);
                float dur = side ? .7f : .85f;
                if (ta > dur + .3f && trickRnd.NextDouble() < chance) aiTrickAt = Time.time + (trickRnd.NextDouble() < late ? Mathf.Max(0, ta - dur * .5f) : .05f);
            }
            if (aiTrickAt > 0 && Time.time >= aiTrickAt && combo.Count == 0) { aiTrickAt = -1; return aiDir; }
            return Vector2.zero;
        }
        bool ZoneNear(float r) { foreach (var z in TransformZone.All) if (z && (z.transform.position - Body.position).sqrMagnitude < r * r) return true; return false; }
        public void AiWantsHop() => aiHop = true;
        // a person's racer driven by an AI (the checks' soaks): the AI's breach / hop rules, not the stick's
        bool AiDriven { get { if (aiDrivenAt != Time.frameCount) { aiDrivenAt = Time.frameCount; var rp = GetComponent<RivalPilot>(); aiDrivenNow = rp && rp.AlwaysOn && rp.enabled; } return aiDrivenNow; } }
        int aiDrivenAt = -1; bool aiDrivenNow;
        public void AiWantsBreach() => aiBreach = true;
        public float ShortcutS => scS;
    }
}
