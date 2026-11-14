using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 7 item 5b: TeedOff Barker (Barnaby, Tee'd Off's carnival barker; "Barnaby" is only his folder / id) calls the race.
    // His clips: Resources/Audio/Voice/barker (new racing lines made with ElevenLabs in his Tee'd Off voice, Tools/Audio/caller_lines.py,
    // plus seven of his Tee'd Off clips that fit; lines.json has the words for the subtitles). One caller for everyone (split screen too),
    // through one non-spatial source at the Voices volume. Each call has a pool (the clip id's prefix), a cooldown per pool and a gap
    // after any line, a priority (a winner call cuts in; a wall bonk waits its turn), and never repeats a clip while another in its pool
    // is fresher. What he reacts to: the people's racers (player 1, player 2), plus the race as a whole (the start, GO, a lead change,
    // the photo finish, the winner, the results).
    public sealed class Caller : MonoBehaviour
    {
        public static Caller Instance { get; private set; }
        public const string Name = "TeedOff Barker";
        AudioSource voice; readonly Dictionary<string, List<AudioClip>> pools = new Dictionary<string, List<AudioClip>>();
        readonly Dictionary<string, string> words = new Dictionary<string, string>();
        readonly Dictionary<string, float> poolNext = new Dictionary<string, float>(), clipLast = new Dictionary<string, float>();
        float busyUntil, nextAny; int busyPriority;
        public readonly List<string> Said = new List<string>();      // "t pool clip" for checks
        public readonly List<string> Skipped = new List<string>();
        public string Subtitle { get; private set; } = ""; public float SubtitleUntil { get; private set; } = -1;
        public int Clips => pools.Values.Sum(l => l.Count);
        Game game; ShiftVehicle leader; float leaderSince; readonly HashSet<ShiftVehicle> watched = new HashSet<ShiftVehicle>();
        bool finalCalled, winnerCalled, resultsCalled; float raceStart = -1; bool stayPending; ShiftVehicle stayWho;

        // pool -> (cooldown s, priority 0..3)
        static readonly Dictionary<string, (float cd, int pri)> Rules = new Dictionary<string, (float, int)>
        {
            ["start"] = (30, 2), ["trial"] = (30, 2), ["go"] = (10, 3), ["lead"] = (22, 1), ["loop"] = (30, 1), ["loopfail"] = (20, 2), ["jump"] = (18, 1),
            ["wings"] = (25, 2), ["stay"] = (25, 1), ["shortcut"] = (25, 1), ["miss"] = (15, 2), ["wall"] = (14, 0), ["splash"] = (20, 1), ["final"] = (60, 1),
            ["photo"] = (60, 3), ["win"] = (60, 3), ["results"] = (60, 2),
        };
        const float AnyGap = 2.5f;

        public static Caller Make(Game g)
        {
            var c = g.gameObject.AddComponent<Caller>(); Instance = c; c.game = g;
            c.voice = g.gameObject.AddComponent<AudioSource>(); c.voice.spatialBlend = 0; c.voice.playOnAwake = false; c.voice.volume = 1f;
            foreach (var clip in Resources.LoadAll<AudioClip>("Audio/Voice/barker"))
            {
                string id = clip.name, pool = id.StartsWith("win_") ? "win" : id.StartsWith("results") ? "results" : id.Split('_')[0];
                if (!c.pools.TryGetValue(pool, out var l)) c.pools[pool] = l = new List<AudioClip>(); l.Add(clip);
            }
            var ta = Resources.Load<TextAsset>("Audio/Voice/barker/lines");
            if (ta) foreach (var line in JsonUtility.FromJson<Lines>(ta.text).lines) c.words[line.id] = line.text;
            Debug.Log($"[Caller] {Name}: {c.Clips} clips in {c.pools.Count} pools ({string.Join(", ", c.pools.Select(kv => $"{kv.Key} {kv.Value.Count}"))}), {c.words.Count} subtitles");
            return c;
        }
        [System.Serializable] sealed class Lines { public Line[] lines; }
        [System.Serializable] sealed class Line { public string id, text; }

        // a people's racer: their events and stunts
        public void Watch(ShiftVehicle v)
        {
            if (!v || !watched.Add(v)) return;
            v.Event += (kind, text) => OnEvent(v, kind, text);
            v.Stunt += s => OnStunt(v, s);
            v.WallHit += (angle, keep, what) => { if (keep < .8f && !v.Finished) Call("wall", v); };
        }

        void OnEvent(ShiftVehicle v, string kind, string text)
        {
            if (!v.Course || !v.Course.timeTrial) return;
            switch (kind)
            {
                case "start":
                    if (v != game.Player) break;
                    finalCalled = winnerCalled = resultsCalled = false; leader = null; raceStart = Time.time; stayPending = false;
                    poolNext.Clear(); busyUntil = nextAny = 0;
                    Call(game.Race && game.Race.Active ? "start" : "trial", v, true);
                    break;
                case "go": if (v == game.Player) Call("go", v, true); break;
                case "shortcut": if (text.Contains(": in") && !text.Contains("Lake flight")) Call("shortcut", v); break;
                case "gate":
                    if (text.Contains("near miss")) Call("miss", v);
                    if (text.Contains("G9") && !finalCalled) { finalCalled = true; Call("final", v); }
                    break;
                case "reset": if (text.Contains("water") && v.Mode == Mode.Drive && v.Progress < ParadisePlunge.Marks.GetValueOrDefault("G1", 0)) Call("splash", v); break;
                case "wings":
                    if (text.Contains("window open")) { stayPending = true; stayWho = v; }
                    break;
            }
        }
        void OnStunt(ShiftVehicle v, string s)
        {
            switch (s)
            {
                case "loop": Call("loop", v); break;
                case "loopfail": Call("loopfail", v); break;
                case "jump": if (v.LastAirTime > 1.3f && !(stayPending && stayWho == v)) Call("jump", v); break;
                case "wings": stayPending = false; Call("wings", v); break;
            }
        }

        void Update()
        {
            if (!game || !game.Player || !game.Player.Course || !game.Player.Course.timeTrial) return;
            voice.volume = Settings.Voices;
            // the choice jump: the window closed and they stayed a car
            if (stayPending && stayWho && !stayWho.WingsOpen && stayWho.Mode == Mode.Drive) { stayPending = false; Call("stay", stayWho); }
            var race = game.Race;
            if (race && race.Active && !game.Player.Holding && raceStart > 0)
            {
                var order = race.Order(); var first = order.Count > 0 ? order[0] : null;
                if (first != leader)
                {
                    bool real = leader != null && Time.time - game.Player.StartTime > 12f && !first.Finished;
                    leader = first; leaderSince = Time.time;
                    if (real) Call("lead", first);
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
            else if (!winnerCalled && game.Player.Finished) { winnerCalled = true; Call("results", game.Player, true); resultsCalled = true; }
            if (!resultsCalled && game.Trial && game.Trial.HasResults && winnerCalled && Time.time > busyUntil + 1.5f)
            {
                resultsCalled = true; bool won = race && race.Active && race.Finishers.Count > 0 && (race.Finishers[0].v.Human);
                if (!won && race && race.Active && pools.ContainsKey("results") && pools["results"].Any(x => x.name.StartsWith("results_lost"))) Play("results", pools["results"].Where(x => x.name.StartsWith("results_lost")).ToList(), 2, null);
                else Call("results", game.Player);
            }
        }
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
            return Play(pool, list, Rules.TryGetValue(pool, out var r) ? r.pri : 1, who, force);
        }
        bool Play(string pool, List<AudioClip> list, int pri, ShiftVehicle who, bool force = false)
        {
            float now = Time.time; var rule = Rules.TryGetValue(pool, out var r) ? r : (20f, 1);
            if (poolNext.TryGetValue(pool, out var t) && now < t && !force) { Skipped.Add($"{now:0.0} {pool}: cooldown"); return false; }
            bool talking = now < busyUntil;
            if (talking && !(force || pri > busyPriority)) { Skipped.Add($"{now:0.0} {pool}: talking"); return false; }
            if (!talking && now < nextAny && !force && pri < 2) { Skipped.Add($"{now:0.0} {pool}: just spoke"); return false; }
            // the clip of this pool heard longest ago (never one still within the pool's cooldown when another is free)
            var clip = list.OrderBy(c => clipLast.TryGetValue(c.name, out var l) ? l : -1000f).ThenBy(_ => Random.value).First();
            if (talking) voice.Stop();
            voice.clip = clip; voice.Play();
            busyUntil = now + clip.length; busyPriority = pri; nextAny = busyUntil + AnyGap; poolNext[pool] = now + rule.Item1; clipLast[clip.name] = now;
            Subtitle = words.TryGetValue(clip.name, out var w) ? w : ""; SubtitleUntil = now + Mathf.Max(2f, clip.length + .6f);
            string line = $"{now:0.0} {pool} {clip.name}{(who ? $" ({who.RacerName})" : "")}"; Said.Add(line);
            Debug.Log($"[Caller] {Name}: {line}: \"{Subtitle}\"");
            return true;
        }
    }
}
