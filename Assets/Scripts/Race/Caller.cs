using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 7 item 5b: TeedOff Barker (Barnaby, Tee'd Off's carnival barker; "Barnaby" is only his folder / id) calls the race.
    // His clips: Resources/Audio/Voice/barker (new racing lines made with ElevenLabs in his Tee'd Off voice, Tools/Audio/caller_lines.py,
    // plus seven of his Tee'd Off clips that fit; lines.json has the words for the subtitles). One caller for everyone (split screen too),
    // through one non-spatial source at the Voices volume. Each call has a pool (the clip id's prefix), a cooldown per pool and a gap
    // after any line, a priority (a winner call cuts in; a wall bonk waits its turn).
    // Round 8 item 2 (Dan: "Can't hear most of Barnaby's lines and he didn't talk as much as I thought he should"):
    //   - loud: his Round 7 ElevenLabs lines were silent after 20 ms (a fade-out from the first sample in the converter); all re-recorded
    //     and every voice levelled to -16 LUFS like the music; while he talks the music and the engines duck by 8 dB (Duck) and a driver's
    //     yell stops (he narrates, the drivers only react).
    //   - more: about 150 lines in 50 pools: every form change, overtakes by rival name, places gained / lost, close racing, falling behind
    //     and catching up, near misses and penalties, walls, the loop, each jump, the boost, landmarks and dinosaurs along the lap, the last
    //     stretch, the finish and the results by place; and when nothing has happened for 6-10 s, a line about the course (filler), so he
    //     talks roughly every 6-10 s of racing. Never over another line, never the same clip twice in one race.
    public sealed class Caller : MonoBehaviour
    {
        public static Caller Instance { get; private set; }
        public const string Name = "TeedOff Barker";
        AudioSource voice; readonly Dictionary<string, List<AudioClip>> pools = new Dictionary<string, List<AudioClip>>();
        readonly Dictionary<string, string> words = new Dictionary<string, string>();
        readonly Dictionary<string, float> poolNext = new Dictionary<string, float>(), clipLast = new Dictionary<string, float>();
        readonly HashSet<string> usedThisRace = new HashSet<string>();
        float busyUntil, nextAny, nextFill = -1; int busyPriority;
        public readonly List<string> Said = new List<string>();      // "t pool clip" for checks
        public readonly List<(float t, float len, string pool, string clip)> Lines = new List<(float, float, string, string)>();
        public readonly List<string> Skipped = new List<string>();
        public string Subtitle { get; private set; } = ""; public float SubtitleUntil { get; private set; } = -1;
        public int Clips => pools.Values.Sum(l => l.Count);
        public int Pools => pools.Count;
        public bool Talking => Time.time < busyUntil && voice && voice.isPlaying;
        public AudioSource Voice => voice;
        // Round 8: the music and the engines under his lines (and a little under a driver's yell): 1 = full, 0.4 = -8 dB
        public static float Duck { get; private set; } = 1;
        public const float DuckDb = -8f;
        Game game; ShiftVehicle leader; float leaderSince; readonly HashSet<ShiftVehicle> watched = new HashSet<ShiftVehicle>();
        bool finalCalled, winnerCalled, resultsCalled; float raceStart = -1; bool stayPending; ShiftVehicle stayWho;
        readonly Dictionary<(ShiftVehicle, ShiftVehicle), bool> ahead = new Dictionary<(ShiftVehicle, ShiftVehicle), bool>();
        readonly Dictionary<ShiftVehicle, int> lastPlace = new Dictionary<ShiftVehicle, int>();
        readonly Dictionary<ShiftVehicle, float> closeSince = new Dictionary<ShiftVehicle, float>(), gapMax = new Dictionary<ShiftVehicle, float>();
        readonly HashSet<string> marksDone = new HashSet<string>(); float nextRaceLook;

        // pool -> (cooldown s, priority 0..3)
        static readonly Dictionary<string, (float cd, int pri)> Rules = new Dictionary<string, (float, int)>
        {
            ["start"] = (30, 2), ["trial"] = (30, 2), ["go"] = (10, 3), ["lead"] = (16, 1), ["loop"] = (30, 1), ["loopfail"] = (20, 2), ["jump"] = (14, 1),
            ["wings"] = (25, 2), ["stay"] = (25, 1), ["shortcut"] = (25, 1), ["miss"] = (12, 2), ["wall"] = (12, 0), ["splash"] = (20, 1), ["final"] = (60, 1),
            ["photo"] = (60, 3), ["win"] = (60, 3), ["results"] = (60, 2), ["podium"] = (60, 2), ["last"] = (60, 2),
            ["tocar"] = (14, 1), ["toboat"] = (14, 1), ["toplane"] = (14, 1), ["tosub"] = (14, 1),
            ["passrae"] = (20, 1), ["passchip"] = (20, 1), ["passvivi"] = (20, 1), ["byrae"] = (20, 1), ["bychip"] = (20, 1), ["byvivi"] = (20, 1),
            ["up"] = (14, 1), ["down"] = (14, 1), ["close"] = (22, 1), ["behind"] = (35, 0), ["catch"] = (30, 1),
            ["creek"] = (30, 1), ["cliff"] = (30, 1), ["boost"] = (30, 0), ["canyon"] = (40, 0),
            ["fill"] = (7, 0),
        };
        // landmarks along the lap: (pool, mark, metres after it) for the first person's racer, once a lap, low priority (Round 8 item 1: the dinosaurs)
        static readonly (string pool, string mark, float at)[] Landmarks =
        {
            ("bronto", "promMid", -60), ("ptero", "loopApp", -30), ("trex", "climbMid", -120), ("raptor", "climbMid", 140), ("arch", "arch", -200),
            ("nest", "arch", 60), ("lagoon", "splash", -60), ("plesio", "splash", 120), ("mangrove", "bend1", -40), ("trike", "mangMid", -30),
            ("gateway", "portal", -70), ("vault", "vaultStart", 10), ("skeleton", "hi2", -40), ("waterfall", "rampLip", -45),
        };
        const float AnyGap = 1.2f;

        public static Caller Make(Game g)
        {
            var c = g.gameObject.AddComponent<Caller>(); Instance = c; c.game = g;
            c.voice = g.gameObject.AddComponent<AudioSource>(); c.voice.spatialBlend = 0; c.voice.playOnAwake = false; c.voice.volume = 1f; c.voice.priority = 0;
            foreach (var clip in Resources.LoadAll<AudioClip>("Audio/Voice/barker"))
            {
                string id = clip.name, pool = id.StartsWith("win_") ? "win" : id.StartsWith("results") ? "results" : id.Split('_')[0];
                if (!c.pools.TryGetValue(pool, out var l)) c.pools[pool] = l = new List<AudioClip>(); l.Add(clip);
            }
            var ta = Resources.Load<TextAsset>("Audio/Voice/barker/lines");
            if (ta) foreach (var line in JsonUtility.FromJson<LinesFile>(ta.text).lines) c.words[line.id] = line.text;
            Debug.Log($"[Caller] {Name}: {c.Clips} clips in {c.pools.Count} pools ({string.Join(", ", c.pools.Select(kv => $"{kv.Key} {kv.Value.Count}"))}), {c.words.Count} subtitles");
            return c;
        }
        [System.Serializable] sealed class LinesFile { public Line[] lines; }
        [System.Serializable] sealed class Line { public string id, text; }

        // a people's racer: their events and stunts
        public void Watch(ShiftVehicle v)
        {
            if (!v || !watched.Add(v)) return;
            v.Event += (kind, text) => OnEvent(v, kind, text);
            v.Stunt += s => OnStunt(v, s);
            v.WallHit += (angle, keep, what) => { if (keep < .8f && !v.Finished) Call("wall", v); };
            bool boosting = false;
            StartCoroutine(BoostWatch());
            System.Collections.IEnumerator BoostWatch()
            {
                while (v) { if (v.Boosting && !boosting && !v.Holding && raceStart > 0 && Time.time - raceStart > 8f) Call("boost", v); boosting = v.Boosting; yield return null; }
            }
        }

        void OnEvent(ShiftVehicle v, string kind, string text)
        {
            if (!v.Course || !v.Course.timeTrial) return;
            switch (kind)
            {
                case "start":
                    if (v != game.Player) break;
                    finalCalled = winnerCalled = resultsCalled = false; leader = null; raceStart = Time.time; stayPending = false;
                    poolNext.Clear(); busyUntil = nextAny = 0; usedThisRace.Clear(); ahead.Clear(); lastPlace.Clear(); closeSince.Clear(); gapMax.Clear(); marksDone.Clear();
                    nextFill = -1; Lines.Clear();
                    Call(game.Race && game.Race.Active ? "start" : "trial", v, true);
                    break;
                case "go": if (v == game.Player) { Call("go", v, true); nextFill = Time.time + 9f; } break;
                case "shortcut": if (text.Contains(": in") && !text.Contains("Lake flight")) Call("shortcut", v); break;
                case "gate":
                    if (text.Contains("near miss")) Call("miss", v);
                    if (text.Contains("G9") && !finalCalled) { finalCalled = true; Call("final", v); }
                    break;
                case "reset": if (text.Contains("water") && v.Mode == Mode.Drive && v.Progress < ParadisePlunge.Marks.GetValueOrDefault("G1", 0)) Call("splash", v); break;
                case "wings":
                    if (text.Contains("window open")) { stayPending = true; stayWho = v; }
                    break;
                case "change":
                    if (v.Holding || v.Finished) break;
                    if (v.Mode == Mode.Fly && stayPending && stayWho == v) break;   // the wings call covers it
                    Call(v.Mode == Mode.Drive ? "tocar" : v.Mode == Mode.Boat ? "toboat" : v.Mode == Mode.Fly ? "toplane" : "tosub", v);
                    break;
            }
        }
        void OnStunt(ShiftVehicle v, string s)
        {
            switch (s)
            {
                case "loop": Call("loop", v); break;
                case "loopfail": Call("loopfail", v); break;
                case "jump":
                    if (stayPending && stayWho == v) break;
                    // which jump: the nearest lip behind the car
                    string which = ""; float best = 1e9f;
                    foreach (var j in ParadisePlunge.Jumps) { float d = v.Progress - j.lip; if (d > 0 && d < best) { best = d; which = j.name; } }
                    if (which == "Creek Jump" && Call("creek", v)) break;
                    if (which == "Cliff Gap" && Call("cliff", v)) break;
                    if (v.LastAirTime > 1.3f) Call("jump", v);
                    break;
                case "wings": stayPending = false; Call("wings", v); break;
            }
        }

        void Update()
        {
            // the duck: down under his lines (8 dB), a little under a driver's yell
            float want = Talking ? Mathf.Pow(10, DuckDb / 20f) : Time.time < Driver.AnySpeakingUntil ? .7f : 1f;
            Duck = Mathf.MoveTowards(Duck, want, Time.unscaledDeltaTime * (want < Duck ? 6f : 1.5f));
            if (!game || !game.Player || !game.Player.Course || !game.Player.Course.timeTrial) return;
            voice.volume = Settings.Voices;
            // the choice jump: the window closed and they stayed a car
            if (stayPending && stayWho && !stayWho.WingsOpen && stayWho.Mode == Mode.Drive) { stayPending = false; if (!Call("stay", stayWho)) Call("canyon", stayWho); }
            var race = game.Race; var p1 = game.Player;
            bool racing = !p1.Holding && raceStart > 0 && !p1.Finished;
            if (racing) Watchers(race);
            if (race && race.Active && !p1.Holding && raceStart > 0)
            {
                var order = race.Order(); var first = order.Count > 0 ? order[0] : null;
                if (first != leader)
                {
                    bool real = leader != null && Time.time - p1.StartTime > 12f && !first.Finished;
                    leader = first; leaderSince = Time.time;
                    if (real && first.Human) Call("lead", first);
                }
                // the finish: a photo finish (the first two within 0.4 s) or the winner, then the results
                if (!winnerCalled && race.Finishers.Count > 0)
                {
                    var w = race.Finishers.OrderBy(f => f.time).First();
                    var second = order.Count > 1 ? order[1] : null;
                    bool close = second && !second.Finished && second.RaceDistance > w.v.Course.gates.Count - 1 && Vector3.Distance(second.Body.position, w.v.Body.position) < 25f;
                    bool closeFinished = race.Finishers.Count > 1 && Mathf.Abs(race.Finishers[1].time - race.Finishers[0].time) < .4f;
                    if (close && Time.time - FinishedAt(w.v) < .5f) return;   // wait a moment: is it a photo?
                    winnerCalled = true;
                    if (closeFinished || close) Call("photo", w.v, true);
                    CallWinner(w.v);
                }
            }
            else if (!winnerCalled && p1.Finished) { winnerCalled = true; Call("results", p1, true); resultsCalled = true; }
            if (!resultsCalled && game.Trial && game.Trial.HasResults && winnerCalled && Time.time > busyUntil + 1.2f)
            {
                resultsCalled = true;
                // Round 8 item 5: the results line by the first person's place (1st: the winner's call already said it, then results)
                int place = race && race.Active ? race.Place(p1) : 1, field = race && race.Active ? race.Racers.Count : 1;
                if (race && race.Active && place == field && field > 1) { if (!Call("last", p1, true)) Call("results", p1, true); }
                else if (race && race.Active && place > 1) { if (!Call("podium", p1, true)) Call("results", p1, true); }
                else Call("results", p1, true);
            }
            // nothing for a while: a line about the course (he talks every 6-10 s of racing)
            if (racing && nextFill > 0 && Time.time > nextFill && !Talking && Time.time > Driver.AnySpeakingUntil)
            {
                if (!Landmark(p1)) Call("fill", p1);
                nextFill = Time.time + Random.Range(6f, 10f);
            }
        }

        // places, overtakes by name, close racing, falling behind / catching up (every 0.25 s), and the landmarks
        void Watchers(Race race)
        {
            foreach (var h in game.Humans) if (h && h.Course) LandmarkPass(h);
            if (!race || !race.Active || Time.time < nextRaceLook || Time.time - game.Player.StartTime < 10f) return;
            nextRaceLook = Time.time + .25f;
            var order = race.Order();
            foreach (var h in game.Humans)
            {
                if (!h || h.Finished || !race.Racers.Contains(h)) continue;
                int place = order.IndexOf(h) + 1;
                bool named = false;
                foreach (var r in order)
                {
                    if (r == h || r.Human || r.Finished) continue;
                    bool now = order.IndexOf(h) < order.IndexOf(r);
                    if (ahead.TryGetValue((h, r), out var was) && was != now && Mathf.Abs(h.RaceDistance - r.RaceDistance) < .02f + 30f / Mathf.Max(1f, h.Course.Length))
                    {
                        string who = r.RacerName.StartsWith("Rocket") ? "rae" : r.RacerName.StartsWith("Chip") ? "chip" : r.RacerName.StartsWith("Vivi") ? "vivi" : "";
                        if (who != "" && Call((now ? "pass" : "by") + who, now ? h : r)) named = true;
                    }
                    ahead[(h, r)] = now;
                    // close racing: within 9 m for 2 s
                    float d = Vector3.Distance(h.Body.position, r.Body.position);
                    if (d < 9f) { if (!closeSince.ContainsKey(r)) closeSince[r] = Time.time; else if (Time.time - closeSince[r] > 2f) { Call("close", h); closeSince[r] = Time.time + 20f; } }
                    else closeSince.Remove(r);
                }
                if (lastPlace.TryGetValue(h, out var lp) && lp != place && !named) Call(place < lp ? "up" : "down", h);
                lastPlace[h] = place;
                // the gap to the racer just ahead (metres along the line)
                int k = order.IndexOf(h);
                if (k > 0)
                {
                    var front = order[k - 1]; float gap = (front.RaceDistance - h.RaceDistance) * Mathf.Max(1f, h.Course.Length) / Mathf.Max(1, h.Course.gates.Count);
                    gapMax.TryGetValue(h, out var gm);
                    if (gap > 180f && k == order.Count - 1) { Call("behind", h); }
                    if (gap > gm) gapMax[h] = gap;
                    else if (gm > 110f && gap < 45f) { if (Call("catch", h)) gapMax[h] = gap; }
                }
            }
        }
        void LandmarkPass(ShiftVehicle h)
        {
            if (h.Course != game.Courses[0] || h.Holding) return;
            foreach (var lm in Landmarks)
            {
                if (!ParadisePlunge.Marks.TryGetValue(lm.mark, out var m)) continue;
                float at = m + lm.at; string key = (h == game.Player ? "1" : "2") + lm.pool;
                if (!marksDone.Contains(key) && h.Progress > at && h.Progress < at + 120f) { marksDone.Add(key); pendingMark = lm.pool; pendingUntil = Time.time + 5f; }
            }
        }
        string pendingMark = ""; float pendingUntil;
        // a landmark seen in the last 5 s: said now if he is free
        bool Landmark(ShiftVehicle h) { if (pendingMark == "" || Time.time > pendingUntil) return false; var p = pendingMark; pendingMark = ""; return Call(p, h); }
        void LateUpdate() { if (pendingMark != "" && !Talking && Time.time > nextAny && Time.time > Driver.AnySpeakingUntil && Time.time <= pendingUntil) Landmark(game ? game.Player : null); }

        readonly Dictionary<ShiftVehicle, float> finishedAt = new Dictionary<ShiftVehicle, float>();
        float FinishedAt(ShiftVehicle v) { if (!finishedAt.TryGetValue(v, out var t)) finishedAt[v] = t = Time.time; return t; }

        void CallWinner(ShiftVehicle w)
        {
            string id = w == game.Player ? "win_abi" : w == game.Player2 ? "win_p2" : w.RacerName.StartsWith("Rocket") ? "win_rae" : w.RacerName.StartsWith("Chip") ? "win_chip" : w.RacerName.StartsWith("Vivi") ? "win_vivi" : "";
            if (!pools.TryGetValue("win", out var all)) return;
            var own = all.Where(c => c.name == id).ToList(); var any = all.Where(c => c.name.StartsWith("win_any")).ToList();
            var pick = own.Count > 0 && (any.Count == 0 || Random.value < .7f) ? own : any.Count > 0 ? any : all;
            Play("win", pick, 3, w, true);
        }

        public bool Call(string pool, ShiftVehicle who, bool force = false)
        {
            if (!pools.TryGetValue(pool, out var list) || list.Count == 0) { Skipped.Add($"{Time.time:0.0} {pool}: no clips"); return false; }
            if (pool == "results") list = list.Where(c => !c.name.StartsWith("results_lost")).ToList();
            return Play(pool, list, Rules.TryGetValue(pool, out var r) ? r.pri : 1, who, force);
        }
        bool Play(string pool, List<AudioClip> list, int pri, ShiftVehicle who, bool force = false)
        {
            float now = Time.time; var rule = Rules.TryGetValue(pool, out var r) ? r : (20f, 1);
            if (poolNext.TryGetValue(pool, out var t) && now < t && !force) { Skipped.Add($"{now:0.0} {pool}: cooldown"); return false; }
            bool talking = Talking;
            // never over another line: only the top calls (GO, the winner, a photo finish) cut him off; a driver's short yell waits out a normal call
            if (talking && !(pri >= 3 && pri > busyPriority)) { Skipped.Add($"{now:0.0} {pool}: talking"); return false; }
            if (!talking && now < nextAny && !force && pri < 2) { Skipped.Add($"{now:0.0} {pool}: just spoke"); return false; }
            if (now < Driver.AnySpeakingUntil && pri < 2 && !force) { Skipped.Add($"{now:0.0} {pool}: a driver is speaking"); return false; }
            if (Driver.PendingYell && pri < 2 && !force) { Skipped.Add($"{now:0.0} {pool}: a driver's yell is waiting"); return false; }
            // never the same clip twice in a race: the one of this pool heard longest ago among those not yet used
            var fresh = list.Where(c => !usedThisRace.Contains(c.name)).ToList();
            if (fresh.Count == 0) { Skipped.Add($"{now:0.0} {pool}: every clip used this race"); return false; }
            var clip = fresh.OrderBy(c => clipLast.TryGetValue(c.name, out var l) ? l : -1000f).ThenBy(_ => Random.value).First();
            if (talking) voice.Stop();
            Driver.Hush();
            voice.clip = clip; voice.Play();
            busyUntil = now + clip.length; busyPriority = pri; nextAny = busyUntil + AnyGap; poolNext[pool] = now + rule.Item1; clipLast[clip.name] = now; usedThisRace.Add(clip.name);
            if (nextFill > 0) nextFill = Mathf.Max(nextFill, busyUntil + Random.Range(5f, 8f));
            Subtitle = words.TryGetValue(clip.name, out var w) ? w : ""; SubtitleUntil = now + Mathf.Max(2f, clip.length + .6f);
            string line = $"{now:0.0} {pool} {clip.name}{(who ? $" ({who.RacerName})" : "")}"; Said.Add(line); Lines.Add((now, clip.length, pool, clip.name));
            Debug.Log($"[Caller] {Name}: {line}: \"{Subtitle}\"");
            return true;
        }
    }
}
