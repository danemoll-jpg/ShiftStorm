using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
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
        float lastBackAt = -10; int backTries;
        float gridLane = float.NaN, slowFor, backUntil = -1, backSteer; public int Unsticks { get; private set; }
        float offset, sideStep; Shortcut planned; bool scDone; float scS; int lastTaken; System.Random rnd; int laps;
        readonly HashSet<string> decided = new HashSet<string>();

        void Start()
        {
            v = GetComponent<ShiftVehicle>(); input = v.Input; input.Scripted = true;
            rnd = new System.Random(rival != null ? rival.seed : 1);
            v.Event += (kind, text) => { if (kind == "start") { gridLane = float.NaN; decided.Clear(); planned = null; scDone = false; Plan = ""; laps++; lastTaken = 0; } };
        }

        // look-ahead: shorter inside a shortcut underwater (a tunnel: follow its line, never cut its corners into the walls)
        float Ahead(float speed) => v.Mode == Mode.Dive ? Mathf.Max(planned && !scDone && v.Progress >= planned.mainFrom ? 9f : 14f, speed * (planned && !scDone && v.Progress >= planned.mainFrom ? .45f : .9f)) : Mathf.Max(18f, speed * 1.1f);

        // the path the rival follows: the main line, or its shortcut's line from where the shortcut leaves the main line until it rejoins
        Vector3 PathAt(float ahead, out Vector3 dir)
        {
            var c = v.Course;
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
            if (!v || !v.Course || (v.IsPlayer && !AlwaysOn)) return;
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
                    if (rnd.NextDouble() < rival.shortcutRate) { planned = sc; scDone = false; Plan = sc.label; ShortcutsTried++; scS = 0; lastTaken = v.ShortcutsTaken.Count; v.Note("plan", $"{v.RacerName} goes for the {sc.label}"); }
                    else v.Note("plan", $"{v.RacerName} keeps to the safe line at the {sc.label}");
                    break;
                }
            if (planned && !scDone)
            {
                if (v.OnShortcut == planned || v.Progress >= planned.mainFrom) scS = planned.Project(p, v.OnShortcut == planned || scS > 0 ? scS : -1, out _, out _);
                if (v.ShortcutsTaken.Count > lastTaken || (v.OnShortcut != planned && v.Progress > planned.mainTo - 5) || v.GatesPassed > planned.nextGate + (planned.replacesGate >= 0 ? 1 : 0)) scDone = true;
            }

            float ahead = Ahead(speed);
            var look = PathAt(ahead, out _);
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
            if (v.Mode == Mode.Drive || v.Mode == Mode.Boat)
            {
                float turn = turnFlat * Mathf.Deg2Rad, r = turn > 1e-3f ? (d1 - d0) / turn : 1e4f;
                float aLat = (v.Mode == Mode.Drive ? 27f : 15f) * (rival != null ? rival.brakeMargin : 1f), vmax = Mathf.Sqrt(aLat * r);
                if (speed > vmax + 1.5f) { thr = 0; brk = Mathf.Clamp01((speed - vmax) / 8f); }
                sec = v.Mode == Mode.Drive ? Mathf.Abs(err) > 22f && speed > 20f : Mathf.Abs(err) > 14f;
            }
            else if (v.Mode == Mode.Dive)
            {
                // underwater the turn rate is the limit (v = r w): ease off the throttle before a tight bend (the tunnel's exit)
                float turn = turnFlat * Mathf.Deg2Rad, r = turn > 1e-3f ? (d1 - d0) / turn : 1e4f;
                float vmax = r * v.dive.turnRate * Mathf.Deg2Rad * .8f * (rival != null ? rival.brakeMargin : 1f);
                if (speed > vmax) thr = Mathf.Clamp(vmax / v.dive.topSpeed, .3f, 1f);
            }
            // boost: on a straight stretch with enough in the meter, never into a form change
            PathAt(Mathf.Max(80f, speed * 2.5f), out var a2);
            float straight = Vector3.Angle(a0, a2);
            bool boost = rival != null && v.Boost >= (v.Boosting ? .05f : rival.boostMeter) && straight < rival.boostStraight && brk <= 0 && !v.Changing && Mathf.Abs(err) < 12f && MaxOffset() > 0;
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
        // 2 x 2 grid behind the start anchor (the player starts at the back: a decent player still wins)
        public static Vector3 GridSlot(int i) => new Vector3(i % 2 == 0 ? -4.5f : 4.5f, 0, -i * 7f);

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
                if (fa >= 0 || fb >= 0) return fa < 0 ? 1 : fb < 0 ? -1 : fa.CompareTo(fb);
                int c = b.RaceDistance.CompareTo(a.RaceDistance);
                return c != 0 ? c : b.Progress.CompareTo(a.Progress);   // on the grid (before G0 counts): who is further along the line
            });
            return l;
        }
        public static string Ordinal(int n) => n == 1 ? "1st" : n == 2 ? "2nd" : n == 3 ? "3rd" : n + "th";
    }
}
