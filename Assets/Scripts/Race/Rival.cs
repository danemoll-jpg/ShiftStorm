using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 4 item 2: the AI's difficulty (Menu > Settings, default Easy). It changes only how the rivals drive, never their vehicle, and
    // never looks at the player's place (no rubber-banding):
    //   pace       the share of the form's top speed the driver holds (it lifts off above it); boost still adds its own speed on top
    //   brake      multiplies the rival's braking margin (lower: brakes earlier and harder before a bend)
    //   wobble     metres of slow side-to-side wander off its line (line accuracy), where the course is wide enough
    //   boostMeter added to the meter a rival waits for before it boosts (Easy boosts rarely), straight: multiplies how straight it must be
    //   mistakeEvery / mistakeSeconds  a lift off the throttle every so many seconds (random within +-40 %), this long
    //   shortcut   multiplies how often it goes for its shortcut
    //   fly        Round 7: how often it opens its wings on the choice jump (decided before the lip, kept)
    public enum Difficulty { Easy, Normal, Hard }
    public struct AiSkill
    {
        public float pace, brake, wobble, boostMeter, straight, mistakeEvery, mistakeSeconds, shortcut, fly;
        public static AiSkill For(Difficulty d) => d switch
        {
            Difficulty.Easy => new AiSkill { pace = .76f, brake = .8f, wobble = 3f, boostMeter = .6f, straight = .4f, mistakeEvery = 18f, mistakeSeconds = 1.1f, shortcut = .2f, fly = .35f },
            Difficulty.Normal => new AiSkill { pace = .87f, brake = .9f, wobble = 1.6f, boostMeter = .3f, straight = .7f, mistakeEvery = 35f, mistakeSeconds = .7f, shortcut = .6f, fly = .5f },
            _ => new AiSkill { pace = 1f, brake = 1f, wobble = 0f, boostMeter = 0f, straight = 1f, mistakeEvery = 0f, mistakeSeconds = 0f, shortcut = 1f, fly = .6f },   // Hard = Round 3's AI
        };
    }

    // Round 3 item 4: a rival's identity and its two difficulty knobs (no rubber-banding: nothing here ever looks at the player's place).
    //   brakeMargin: the share of the cornering grip it plans bends with (lower = brakes earlier and harder before a bend)
    //   boostMeter / boostStraight: it boosts only with at least this much in the meter and when the path ahead turns less than this
    // shortcut / shortcutRate: the one shortcut it may take, and how often (decided before the entry, kept until the rejoin).
    public sealed class Rival
    {
        public string name, character; public Color color; public float lane;
        public float brakeMargin = .9f, boostMeter = .55f, boostStraight = 10f;
        public string shortcut; public float shortcutRate;
        public int seed;
        public static readonly Rival[] Roster =
        {
            new Rival { name = "Rocket Rae", character = "Rae", color = new Color(.85f, .12f, .15f), lane = -2.5f, brakeMargin = .92f, boostMeter = .5f, boostStraight = 11f, shortcut = "Sky Arch slit", shortcutRate = .5f, seed = 11 },
            new Rival { name = "Chip \"Showtime\" Sterling", character = "Chip", color = new Color(.15f, .4f, .95f), lane = 2.5f, brakeMargin = .9f, boostMeter = .6f, boostStraight = 9f, shortcut = "Coral Vault tunnel", shortcutRate = .5f, seed = 23 },
            new Rival { name = "Vivi", character = "Vivi", color = new Color(.62f, .25f, .9f), lane = 0f, brakeMargin = .88f, boostMeter = .65f, boostStraight = 8f, seed = 37 },
        };
    }

    // Drives a vehicle like a player: only through ShiftInput (throttle, brake, steer, climb / dive, boost, drift / carve), never by moving
    // it. It follows the racer line (or its chosen shortcut's line) a little ahead, with its own lane offset and a side step to pass a racer
    // ahead; on land and water it plans the speed for the bend ahead from the path's curvature (v = sqrt(a r)) and brakes before it; in the
    // air and underwater it aims at the path's height / depth ahead. Recovery (stuck, out of bounds, missed gate) is the vehicle's own,
    // the same as the player's. Ideas from chastain_rush's RoadDriver (look-ahead, curvature speed) and PlanePilot (height target), read
    // only; the code is ShiftStorm's own.
    public sealed class RivalPilot : MonoBehaviour
    {
        public Rival rival;
        public ShiftVehicle v; ShiftInput input;
        public bool AlwaysOn;          // checks: drive even the player's vehicle (the soak test)
        public string Plan { get; private set; } = "";   // the shortcut chosen for this lap ("" = the safe line)
        public int ShortcutsTried { get; private set; }
        public float missOffset;       // checks only: aim this far to the side of a shortcut's entry (the deliberate miss)
        public float shortcutOffset;   // checks only: drive this far off the shortcut's line inside it (the scrape run)
        public int forceChoice;    // checks only: 1 always flies on the choice jump, -1 always stays a car, 0 by AiSkill.fly
        public bool useDifficulty = true;   // checks that measure the course itself (shortcuts) drive at Round 3's (Hard) level
        public AiSkill Skill => useDifficulty ? AiSkill.For(Settings.Difficulty) : AiSkill.For(Difficulty.Hard);
        public int Mistakes { get; private set; }
        float nextMistake = -1, mistakeUntil = -1, launchAt = -1, startProgress; bool launched;
        float lastBackAt = -10; int backTries;
        float gridLane = float.NaN, slowFor, backUntil = -1, backSteer; public int Unsticks { get; private set; }
        float offset, sideStep; Shortcut planned; bool scDone; float scS; int lastTaken; System.Random rnd; int laps;
        readonly HashSet<string> decided = new HashSet<string>(); readonly List<float> hitTimes = new List<float>();

        void Start()
        {
            v = GetComponent<ShiftVehicle>(); input = v.Input; input.Scripted = true;
            rnd = new System.Random(rival != null ? rival.seed : 1);
            v.WallHit += (a, k, w) => hitTimes.Add(Time.time);
            v.Event += (kind, text) => { if (kind == "start") { gridLane = float.NaN; decided.Clear(); planned = null; scDone = false; Plan = ""; laps++; lastTaken = 0; launched = false; launchAt = -1; nextMistake = -1; mistakeUntil = -1; } };
        }

        // look-ahead: shorter inside a shortcut underwater (a tunnel: follow its line, never cut its corners into the walls)
        float Ahead(float speed) => v.Mode == Mode.Dive ? Mathf.Max(planned && !scDone && v.Progress >= planned.mainFrom ? 9f : 14f, speed * (planned && !scDone && v.Progress >= planned.mainFrom ? .45f : .9f)) : Mathf.Max(18f, speed * 1.1f);

        // the path the rival follows: the main line, or its shortcut's line from where the shortcut leaves the main line until it rejoins
        Vector3 PathAt(float ahead, out Vector3 dir)
        {
            var c = v.Course;
            // Round 7: the choice jump: car or plane, decided once 300 m before the lip and kept (every rival, AiSkill.fly)
            if (!planned || scDone)
                foreach (var sc in c.shortcuts)
                {
                    if (!sc.choice || decided.Contains(sc.label) || !(v.Progress > sc.mainFrom - 300 && v.Progress < sc.mainFrom - 3 && v.GatesPassed <= sc.nextGate)) continue;
                    decided.Add(sc.label);
                    if (forceChoice != 0 ? forceChoice > 0 : rnd.NextDouble() < Skill.fly) { planned = sc; scDone = false; Plan = sc.label; ShortcutsTried++; scS = 0; lastTaken = v.ShortcutsTaken.Count; v.Note("plan", $"{v.RacerName} will fly at the {sc.label}"); }
                    else v.Note("plan", $"{v.RacerName} stays a car at the {sc.label}");
                    break;
                }
            if (planned && planned.choice && !scDone && v.WingsOpen && v.GroundedWheels == 0 && !v.Changing) input.RequestWings();
            if (planned && !scDone)
            {
                if (v.OnShortcut == planned || v.Progress >= planned.mainFrom) return planned.PointAt(scS + ahead, out dir);
                float d = v.Progress + ahead;
                if (d > planned.mainFrom) return planned.PointAt(d - planned.mainFrom, out dir);
            }
            return c.PointAt(Mathf.Min(v.Progress + ahead, c.Length), out dir);
        }

        // the largest side offset that is safe here: none near a form change, a gate frame or a narrow piece; more in wide places
        float MaxOffset()
        {
            if (planned && !scDone && v.Progress >= planned.mainFrom - 30) return 0;
            var m = ParadisePlunge.Marks; float p = v.Progress;
            if (v.Course != Game.Instance?.Courses[0] || m.Count == 0) return 0;
            if (ParadisePlunge.StuntNear(p)) return 0;   // Round 7: on its line into the loop (7 m half) and over the jumps
            foreach (var k in new[] { "lip", "splash", "diveZone", "portal", "surface", "rampFoot", "rampLip", "landing", "G1", "G5", "G7", "G8", "G9", "finish" })
                if (Mathf.Abs(p - m[k]) < 45) return 0;
            if (p > m["rampStart"] - 20 && p < m["lip"]) return 0;
            if (p > m["poolEdge"] - 20 && p < m["G9"] + 10) return 0;
            switch (v.Mode)
            {
                case Mode.Drive: return 6f;
                case Mode.Boat: return 7f;
                case Mode.Fly: return 9f;
                default: return 5f;
            }
        }

        void FixedUpdate()
        {
            if (!v || !v.Course || (v.Human && !AlwaysOn)) return;
            if (v.Holding) { Set(0, 0, 0, 0, false, false); return; }
            if (v.Finished) { Set(0, .4f, 0, 0, false, false); return; }
            var c = v.Course; var p = v.Body.position; float speed = v.Speed;
            // the shortcut: decided once, 300 m before it leaves the main line; kept until the racer is past its rejoin
            if ((!planned || scDone) && rival != null && !string.IsNullOrEmpty(rival.shortcut))
                foreach (var label in rival.shortcut.Split('|'))
                {
                    if (decided.Contains(label)) continue;
                    var sc = c.shortcuts.Find(x => x.label == label);
                    if (!sc || !(v.Progress > sc.mainFrom - 300 && v.Progress < sc.mainFrom - 3 && v.GatesPassed <= sc.nextGate)) continue;
                    decided.Add(label);
                    if (rnd.NextDouble() < rival.shortcutRate * Skill.shortcut) { planned = sc; scDone = false; Plan = sc.label; ShortcutsTried++; scS = 0; lastTaken = v.ShortcutsTaken.Count; v.Note("plan", $"{v.RacerName} goes for the {sc.label}"); }
                    else v.Note("plan", $"{v.RacerName} keeps to the safe line at the {sc.label}");
                    break;
                }
            // Round 7: the choice jump: car or plane, decided once 300 m before the lip and kept (every rival, AiSkill.fly)
            if (!planned || scDone)
                foreach (var sc in c.shortcuts)
                {
                    if (!sc.choice || decided.Contains(sc.label) || !(v.Progress > sc.mainFrom - 300 && v.Progress < sc.mainFrom - 3 && v.GatesPassed <= sc.nextGate)) continue;
                    decided.Add(sc.label);
                    if (forceChoice != 0 ? forceChoice > 0 : rnd.NextDouble() < Skill.fly) { planned = sc; scDone = false; Plan = sc.label; ShortcutsTried++; scS = 0; lastTaken = v.ShortcutsTaken.Count; v.Note("plan", $"{v.RacerName} will fly at the {sc.label}"); }
                    else v.Note("plan", $"{v.RacerName} stays a car at the {sc.label}");
                    break;
                }
            if (planned && planned.choice && !scDone && v.WingsOpen && v.GroundedWheels == 0 && !v.Changing) input.RequestWings();
            if (planned && !scDone)
            {
                if (v.OnShortcut == planned || v.Progress >= planned.mainFrom) scS = planned.Project(p, v.OnShortcut == planned || scS > 0 ? scS : -1, out _, out _);
                if (v.ShortcutsTaken.Count > lastTaken || (v.OnShortcut != planned && v.Progress > planned.mainTo - 5) || v.GatesPassed > planned.nextGate + (planned.replacesGate >= 0 ? 1 : 0)) scDone = true;
            }

            var skill = Skill;
            // Round 4 item 4: the launch. Each row waits a moment longer than the row ahead (0.15 s per 10 m back), and the racer keeps
            // its grid lane for the first 200 m, so the grid opens out instead of four cars squeezing into the same lanes at GO.
            if (!launched)
            {
                launched = true; var front = c.anchors[0]; startProgress = v.Progress;
                launchAt = Time.time + Mathf.Max(0, front.progress - v.Progress) * .015f;
                c.Project(p, v.Progress, out var lineAt, out var lineDir); gridLane = Vector3.Dot(p - lineAt, Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(lineDir, Vector3.up).normalized));
            }
            if (Time.time < launchAt) { Set(0, 0, 0, 0, false, false); return; }
            // Round 4 item 2: small mistakes (a lift off the throttle), never near a form change
            if (skill.mistakeEvery > 0)
            {
                if (nextMistake < 0) nextMistake = Time.time + skill.mistakeEvery * (.6f + .8f * (float)rnd.NextDouble());
                if (Time.time >= nextMistake && mistakeUntil < Time.time && MaxOffset() > 0) { mistakeUntil = Time.time + skill.mistakeSeconds; nextMistake = Time.time + skill.mistakeEvery * (.6f + .8f * (float)rnd.NextDouble()); Mistakes++; }
            }
            // Round 4: missed the shortcut's way in (three wall hits in 1.5 s before getting onto it): give it up and take the main line
            hitTimes.RemoveAll(t => Time.time - t > 1.5f);
            if (planned && !scDone && v.OnShortcut != planned && hitTimes.Count >= 3) { v.Note("plan", $"{v.RacerName} gives up the {planned.label} (hitting rock)"); scDone = true; Plan = ""; hitTimes.Clear(); }
            float ahead = Ahead(speed);
            var look = PathAt(ahead, out _);
            if (v.Mode == Mode.Drive && !(planned && !scDone) && v.Course == Game.Instance?.Courses[0] && ParadisePlunge.LoopAim(v.Progress, ahead, out var la)) look = la;
            // blocked (pinned against another car or a wall) on land or water: back out with the wheel turned the other way, as a player
            // would, well before the vehicle's own stuck reset (4 s)
            bool ground = v.Mode == Mode.Drive || v.Mode == Mode.Boat;
            slowFor = ground && speed < 2.5f && Time.time >= backUntil && Time.time - v.StartTime > 3f ? slowFor + Time.fixedDeltaTime : 0;
            if (slowFor > 1.2f)
            {
                var f0 = Vector3.ProjectOnPlane(v.transform.forward, Vector3.up).normalized; var w0 = Vector3.ProjectOnPlane(look - p, Vector3.up).normalized;
                backSteer = -.35f * Mathf.Sign(Vector3.SignedAngle(f0, w0, Vector3.up));   // a little lock only: a full-lock reverse spun cars round on the grid
                // still pinned after two tries in a row: press Reset, as a player would (the same recovery as the player's)
                if (Time.time - lastBackAt < 4f && ++backTries >= 2) { backTries = 0; input.RequestReset(); v.Note("unstick", $"{v.RacerName} presses Reset (pinned)"); }
                else if (Time.time - lastBackAt >= 4f) backTries = 1;
                lastBackAt = Time.time;
                backUntil = Time.time + .7f; slowFor = 0; Unsticks++; v.Note("unstick", $"{v.RacerName} backs out ({speed:0.0} m/s)");
            }
            if (Time.time < backUntil) { Set(0, 1, backSteer, 0, false, false); return; }
            // lane and passing: a side step when a racer is close ahead in the lane
            float max = MaxOffset();
            var fwd3 = (look - p).normalized; var right = Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(fwd3, Vector3.up).normalized);
            float want = rival != null ? rival.lane : 0; float stepWant = 0;
            if (!float.IsNaN(gridLane) && v.Progress < startProgress + 200f) want = gridLane;
            else if (skill.wobble > 0) want += skill.wobble * Mathf.Sin(Time.time * .37f + (rival != null ? rival.seed : 0)) * Mathf.Sin(Time.time * .13f + 1.7f);
            if (Game.Instance && Game.Instance.Race)
                foreach (var o in Game.Instance.Race.Racers)
                {
                    if (!o || o == v || !o.gameObject.activeInHierarchy) continue;
                    var rel = o.Body.position - p; float along = Vector3.Dot(rel, fwd3), side = Vector3.Dot(rel, right);
                    if (along > 2 && along < 32 && Mathf.Abs(side - offset) < 3.2f && Mathf.Abs(rel.y) < 4 && (o.Speed < speed + 2 || along < 12)) stepWant = side > offset ? -5f : 5f;
                }
            sideStep = Mathf.MoveTowards(sideStep, stepWant, 4f * Time.fixedDeltaTime);
            offset = Mathf.MoveTowards(offset, Mathf.Clamp(want + sideStep, -max, max), 3f * Time.fixedDeltaTime);
            look += right * offset;
            if (missOffset != 0 && planned && !scDone && v.OnShortcut != planned && planned.entry.transform.InverseTransformPoint(p).z < -5) look += right * missOffset;
            if (shortcutOffset != 0 && planned && !scDone && v.Progress >= planned.mainFrom) look += right * shortcutOffset;   // checks: the scrape run

            var fwd = Vector3.ProjectOnPlane(v.transform.forward, Vector3.up).normalized;
            var wantDir = Vector3.ProjectOnPlane(look - p, Vector3.up).normalized;
            float err = Vector3.SignedAngle(fwd, wantDir, Vector3.up);
            float steer = Mathf.Clamp(err / 20f, -1, 1);
            float vy = v.Body.linearVelocity.y;
            bool tunnel = v.Mode == Mode.Dive && planned && !scDone && v.Progress >= planned.mainFrom;
            float vert = Mathf.Clamp((look.y - p.y) * (tunnel ? .4f : .15f) - vy * .12f, -1, 1);
            float thr = 1, brk = 0; bool sec = false;
            // the bend ahead (on the path being followed) sets the speed it may carry: brake before it, not after hitting something
            float d0 = 6, d1 = 6 + Mathf.Max(25f, speed * 1.6f);
            PathAt(d0, out var a0); PathAt(d1, out var a1);
            float turnFlat = Vector3.Angle(Vector3.ProjectOnPlane(a0, Vector3.up), Vector3.ProjectOnPlane(a1, Vector3.up));
            bool stuntLine = v.Mode == Mode.Drive && v.Course == Game.Instance?.Courses[0] && (v.OnLoop || ParadisePlunge.NoBrake(v.Progress, v.Progress + d1));   // Round 7: the loop's line turns over itself: no braking for it
            if ((v.Mode == Mode.Drive || v.Mode == Mode.Boat) && !stuntLine)
            {
                float turn = turnFlat * Mathf.Deg2Rad, r = turn > 1e-3f ? (d1 - d0) / turn : 1e4f;
                float aLat = (v.Mode == Mode.Drive ? 27f : 15f) * (rival != null ? rival.brakeMargin : 1f) * skill.brake, vmax = Mathf.Sqrt(aLat * r);
                if (speed > vmax + 1.5f) { thr = 0; brk = Mathf.Clamp01((speed - vmax) / 8f); }
                sec = v.Mode == Mode.Drive ? Mathf.Abs(err) > 22f && speed > 20f : Mathf.Abs(err) > 14f;
            }
            else if (v.Mode == Mode.Dive)
            {
                // underwater the turn rate is the limit (v = r w): ease off the throttle before a tight bend (the tunnel's exit)
                float turn = turnFlat * Mathf.Deg2Rad, r = turn > 1e-3f ? (d1 - d0) / turn : 1e4f;
                float vmax = r * v.dive.turnRate * Mathf.Deg2Rad * .8f * (rival != null ? rival.brakeMargin : 1f) * skill.brake;
                if (speed > vmax) thr = Mathf.Clamp(vmax / v.dive.topSpeed, .3f, 1f);
            }
            // boost: on a straight stretch with enough in the meter, never into a form change
            PathAt(Mathf.Max(80f, speed * 2.5f), out var a2);
            float straight = Vector3.Angle(a0, a2);
            bool boost = rival != null && v.Boost >= (v.Boosting ? .05f : Mathf.Min(1f, rival.boostMeter + skill.boostMeter)) && straight < rival.boostStraight * skill.straight && brk <= 0 && !v.Changing && Mathf.Abs(err) < 12f && MaxOffset() > 0;
            // the pace it holds (lifting off above it), and a mistake: off the throttle for a moment
            // Round 5: the pace cap and the mistakes start 600 m after the grid: at the faster launch (30 m/s2) a held-back Easy car right
            // in front of the grid's back row was rammed in the first seconds (15 touches in 25 starts); the field now gets away cleanly
            bool launchRun = v.Progress < startProgress + 600f;
            if (skill.pace < 1f && !boost && !launchRun && speed > skill.pace * v.TopSpeedNow) thr = 0;
            if (Time.time < mistakeUntil && !launchRun) { thr = 0; boost = false; }
            if (v.Mode == Mode.Dive && thr > 0) thr = Mathf.Min(thr, skill.pace);
            Set(thr, brk, steer, vert, boost, sec);
        }

        void Set(float thr, float brk, float steer, float vert, bool boost, bool sec)
        {
            input.sThrottle = thr; input.sBrake = brk; input.sSteer = steer; input.sVertical = vert; input.sBoost = boost; input.sSecondary = sec;
        }
    }

    // The race: the racers on the grid, their order (main gates passed plus progress to the next gate, Course.RaceDistance; a finished racer
    // by its finishing time) and the finishing order with times.
    public sealed class Race : MonoBehaviour
    {
        public bool Active { get; private set; }
        public readonly List<ShiftVehicle> Racers = new List<ShiftVehicle>();
        public readonly List<(ShiftVehicle v, float time)> Finishers = new List<(ShiftVehicle, float)>();
        public const int PlayerSlot = 3;
        public static int RivalSlot(int k) => k;
        // 2 x 2 grid behind the start anchor (the player starts at the back: a decent player still wins). Round 4 item 4: spaced wider
        // (11 m across, 10 m between rows; build 4: 9 m, 7 m) so nobody is pinned at GO; the AI also launches row by row (RivalPilot).
        public static Vector3 GridSlot(int i) => new Vector3(i % 2 == 0 ? -5.5f : 5.5f, 0, -i * 10f);

        public void Setup(bool race, ShiftVehicle player, List<ShiftVehicle> rivals)
        {
            foreach (var r in Racers) if (r) r.Event -= OnEvent;
            Racers.Clear(); Finishers.Clear(); Active = race;
            Racers.Add(player); if (race && rivals != null) Racers.AddRange(rivals);
            foreach (var r in Racers) r.Event += OnEvent;
        }
        void OnEvent(string kind, string text)
        {
            if (kind != "finish") return;
            foreach (var r in Racers) if (r.Finished && !Finishers.Exists(f => f.v == r)) Finishers.Add((r, r.FinishTime));
        }
        // 1-based place of a racer now
        public int Place(ShiftVehicle who) { var o = Order(); return o.IndexOf(who) + 1; }
        public List<ShiftVehicle> Order()
        {
            var l = new List<ShiftVehicle>(Racers);
            l.Sort((a, b) =>
            {
                int fa = Finishers.FindIndex(f => f.v == a), fb = Finishers.FindIndex(f => f.v == b);
                if (fa >= 0 && fb >= 0) { int ct = Finishers[fa].time.CompareTo(Finishers[fb].time); return ct != 0 ? ct : fa.CompareTo(fb); }   // Round 4: by time (penalties included)
                if (fa >= 0 || fb >= 0) return fa < 0 ? 1 : -1;
                int c = b.RaceDistance.CompareTo(a.RaceDistance);
                return c != 0 ? c : b.Progress.CompareTo(a.Progress);   // on the grid (before G0 counts): who is further along the line
            });
            return l;
        }
        public static string Ordinal(int n) => n == 1 ? "1st" : n == 2 ? "2nd" : n == 3 ? "3rd" : n + "th";
    }
}
