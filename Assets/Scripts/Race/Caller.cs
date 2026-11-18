using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 7 item 5b: Barnaby (Tee'd Off's carnival barker; Round 15: shown and spoken as "Barnaby" everywhere, "barker" is only his folder / ids) calls the race.
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
        public const string Name = "Barnaby";
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
        public bool BarkerTalking => Time.time < busyUntil && voice && voice.isPlaying;
        public bool Talking => BarkerTalking || (Buck.Instance && Buck.Instance.Busy);   // Round 11: the drivers wait for Buck too
        // Round 11: after Buck's briefing, Barnaby's start line (held back while Buck talked)
        public void Quiet() { if (voice) voice.Stop(); busyUntil = Time.time; SubtitleUntil = 0; }   // Round 11: Buck starts a briefing / scene
        public void AfterBriefing(ShiftVehicle p) { if (p && p.Course && p.Course.timeTrial) Call(game.Race && game.Race.Active ? "start" : "trial", p, true); }
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
            ["passrae"] = (20, 1), ["passchip"] = (20, 1), ["passvivi"] = (20, 1), ["byrae"] = (20, 1), ["bychip"] = (20, 1), ["byvivi"] = (20, 1), ["passabi"] = (20, 1), ["byabi"] = (20, 1),
            ["up"] = (14, 1), ["down"] = (14, 1), ["close"] = (22, 1), ["behind"] = (35, 0), ["catch"] = (30, 1),
            ["creek"] = (30, 1), ["cliff"] = (30, 1), ["boost"] = (30, 0), ["canyon"] = (40, 0),
            ["fill"] = (7, 0),
            // Round 10: Coaster Coast's own pools (and a results line naming each course)
            ["ccstart"] = (30, 2), ["ccopen"] = (30, 2), ["ccfill"] = (7, 0), ["ccwings"] = (25, 2), ["ccsplash"] = (20, 1), ["ccres"] = (60, 2), ["ppres"] = (60, 2),
            ["cccoaster"] = (40, 1), ["ccgap"] = (40, 1), ["cccork"] = (40, 1), ["cctophat"] = (40, 2), ["ccwheel"] = (60, 1), ["cchelter"] = (60, 1),
            ["ccfireworks"] = (60, 1), ["ccballoon"] = (60, 1), ["ccflume"] = (60, 1), ["cclighthouse"] = (60, 1), ["ccpier"] = (60, 1), ["cccarousel"] = (60, 1),
            ["ccoctopus"] = (60, 1), ["ccwhale"] = (60, 1), ["ccpool"] = (60, 1),
            // Round 15: Big Top Breakdown's own pools
            ["btstart"] = (30, 2), ["btfill"] = (7, 0), ["btwings"] = (25, 2), ["btsplash"] = (20, 1), ["btres"] = (60, 2), ["btjump"] = (40, 1),
            ["btglobe"] = (60, 1), ["btcannon"] = (60, 2), ["btwagons"] = (60, 1), ["btstrongman"] = (60, 1), ["bttightrope"] = (60, 1), ["btclowns"] = (60, 1),
            ["bttent"] = (60, 1), ["bttrapeze"] = (60, 1), ["btelephants"] = (60, 1), ["btsteamer"] = (60, 1), ["btseals"] = (60, 1), ["btrapids"] = (60, 1),
            ["bttank"] = (60, 1), ["btsunken"] = (60, 1), ["btpool"] = (60, 1), ["bthoops"] = (60, 1),
            // Round 16: Dam Shame's own pools (three laps: the landmarks once a lap, the set-pieces once a race)
            ["dmstart"] = (30, 2), ["dmfill"] = (7, 0), ["dmwings"] = (25, 2), ["dmsplash"] = (20, 1), ["dmres"] = (60, 2), ["dmjump"] = (30, 1),
            ["dmrise1"] = (30, 3), ["dmrise2"] = (30, 3), ["dmlap2"] = (30, 2), ["dmlap3"] = (30, 2),
            ["dmspill"] = (60, 1), ["dmgates"] = (60, 1), ["dmturbine"] = (60, 1), ["dmbuck"] = (60, 1), ["dmbeaver"] = (60, 1), ["dmheron"] = (60, 1),
            ["dmtrout"] = (60, 1), ["dmlogs"] = (60, 1), ["dmdam"] = (60, 1), ["dmhall"] = (40, 1), ["dmflood"] = (40, 1), ["dmdive"] = (40, 1),
            // Round 17: laps on every course, the items (a person's racer firing or hit), the stunts (combos, shift tricks, wipeouts), Stunt Show
            ["lapnext"] = (30, 2), ["lapfinal"] = (30, 2), ["showstart"] = (30, 2), ["showwin"] = (60, 2),
            ["combo"] = (14, 1), ["bigcombo"] = (20, 2), ["shifttrick"] = (18, 2), ["wipeout"] = (16, 1), ["breach"] = (30, 1), ["hop"] = (40, 0),
            ["ithit"] = (12, 1), ["itblock"] = (20, 1), ["itwrong"] = (18, 2), ["itbubble"] = (18, 1), ["itswap"] = (18, 2), ["itstorm"] = (18, 1), ["itegg"] = (20, 1),
            ["itrocket"] = (20, 1), ["itcannon"] = (20, 1), ["itflood"] = (20, 1), ["itbuck"] = (30, 3), ["itrewind"] = (20, 1), ["ithorn"] = (20, 1), ["itmirage"] = (20, 1), ["itrainbow"] = (20, 1),
        };
        // Round 17: an item used, by its pool (Slick and Bubble Shield have none: the block / the hit is called)
        static string ItemPool(string what) => what switch
        {
            "WrongForm" => "itwrong", "BubbleTrap" => "itbubble", "SwapSpot" => "itswap", "PocketStorm" => "itstorm", "DinoEgg" => "itegg", "FireworkRocket" => "itrocket",
            "HumanCannonball" => "itcannon", "FloodWave" => "itflood", "BigFinish" => "itbuck", "Rewind" => "itrewind", "HypeHorn" => "ithorn", "Mirage" => "itmirage", "RainbowBridge" => "itrainbow", _ => ""
        };
        // Round 10: Coaster Coast's landmarks (places on its lap) and its set-pieces (said only while big on screen, SetPiece)
        static readonly (string pool, string mark, float at)[] CoasterLandmarks =
        {
            ("cchelter", "helter", -160), ("ccflume", "splash", -30), ("cclighthouse", "sea1", -20), ("ccpier", "portal", -50), ("ccpool", "G8", -25),
        };
        // Round 15: Big Top Breakdown's landmarks
        static readonly (string pool, string mark, float at)[] BigTopLandmarks =
        {
            ("bttent", "door", -90), ("btrapids", "islandN", -60), ("btpool", "G8", -25),
        };
        static readonly string[] SetPiecePools = { "ccwheel", "ccballoon", "ccfireworks", "ccwhale", "ccoctopus", "cccarousel",
            "btwagons", "btstrongman", "bttightrope", "btclowns", "btglobe", "btcannon", "bttrapeze", "btelephants", "btsteamer", "btseals", "bttank", "btsunken",
            "bthoops", "dmgates", "dmspill", "dmturbine", "dmbuck", "dmbeaver", "dmheron", "dmtrout", "dmlogs", "dmdam" };
        // Round 16: Dam Shame's landmarks: once a lap each (the line is the lap three times over); the stage: the water they are for (-1: any)
        static readonly (string pool, string mark, float at, int stage)[] DamLandmarks =
        {
            ("dmhall", "hallIn", -70, 0), ("dmflood", "landing", -30, 1), ("dmdive", "bend1", -60, 2), ("dmhall", "hallIn", -70, 2),
        };
        string Look(ShiftVehicle who) { var c = who ? who.Course : game && game.Player ? game.Player.Course : null; return c ? c.look : ""; }
        bool Coaster(ShiftVehicle who) => Look(who) == "coaster";
        bool BigTopCourse(ShiftVehicle who) => Look(who) == "bigtop";
        bool DamCourse(ShiftVehicle who) => Look(who) == "dam";
        // the Paradise Plunge pools that name the jungle, the lake or the dinosaurs, swapped on Coaster Coast for its own
        string ForCourse(string pool, ShiftVehicle who)
        {
            if (DamCourse(who))   // Round 16: Dam Shame's own lines; no jungle, lake, dinosaur, Coaster Coast or circus line
            {
                switch (pool)
                {
                    case "results": return pools.ContainsKey("dmres") ? "dmres" : pool;
                    case "start": return pools.ContainsKey("dmstart") ? "dmstart" : pool;
                    case "fill": return pools.ContainsKey("dmfill") ? "dmfill" : "";
                    case "wings": return pools.ContainsKey("dmwings") ? "dmwings" : pool;
                    case "splash": return pools.ContainsKey("dmsplash") ? "dmsplash" : pool;
                    case "creek": case "cliff": case "canyon": case "final": return "";   // "final": the lap calls say it (G9 is a gate on lap 2 here)
                }
                return pool.StartsWith("cc") || pool.StartsWith("bt") ? "" : pool;
            }
            if (BigTopCourse(who))   // Round 15: no jungle, lake, dinosaur or Coaster Coast line on Big Top Breakdown
            {
                switch (pool)
                {
                    case "results": return pools.ContainsKey("btres") ? "btres" : pool;
                    case "start": return pools.ContainsKey("btstart") ? "btstart" : pool;
                    case "fill": return pools.ContainsKey("btfill") ? "btfill" : "";
                    case "wings": return pools.ContainsKey("btwings") ? "btwings" : pool;
                    case "splash": return pools.ContainsKey("btsplash") ? "btsplash" : pool;
                    case "creek": case "cliff": case "canyon": return "";
                }
                return pool.StartsWith("cc") || pool.StartsWith("dm") ? "" : pool;
            }
            bool cc = Coaster(who);
            if (pool == "results") return cc ? (pools.ContainsKey("ccres") ? "ccres" : pool) : (pools.ContainsKey("ppres") && Random.value < .6f ? "ppres" : pool);
            if (!cc) return pool;
            switch (pool)
            {
                case "start": return Random.value < .6f && pools.ContainsKey("ccopen") ? "ccopen" : "ccstart";   // Docs/STORY.md: the tour's public opening night
                case "fill": return "ccfill";
                case "wings": return "ccwings";
                case "splash": return "ccsplash";
                case "creek": case "cliff": case "canyon": return "";
            }
            return pool;
        }
        // landmarks along the lap: (pool, mark, metres after it) for the first person's racer, once a lap, low priority (Round 8 item 1: the dinosaurs)
        // Round 8b: the dinosaur lines are no longer said at a place on the lap but only while that kind of dinosaur is big on the player's
        // screen (DinoActor.BigShare of its height for DinoHeld s; DinoLines), once a race each
        static readonly (string pool, string mark, float at)[] Landmarks =
        {
            ("arch", "arch", -200), ("nest", "arch", 60), ("lagoon", "splash", -60), ("mangrove", "bend1", -40),
            ("gateway", "portal", -70), ("vault", "vaultStart", 10), ("waterfall", "rampLip", -45),
        };
        static readonly string[] DinoPools = { "bronto", "trex", "raptor", "trike", "ptero", "plesio", "skeleton" };
        const float DinoHeld = .5f;
        public static bool IsDinoPool(string pool) => System.Array.IndexOf(DinoPools, pool) >= 0;
        // every dinosaur line said: when, which, how big that kind was on screen at that moment (the check reads it)
        public static readonly List<(float t, string pool, float share)> DinoLines = new List<(float, string, float)>();
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
            RacerItems.Fired += (by, what, at) => { if (!by || !c.game) return; bool person = by.Human || (at && at.Human); string pool = ItemPool(what); if (person && pool != "") c.Call(pool, by.Human ? by : at); };   // Round 17
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
            // Round 17: the stunts (and the crowd cheers a good landing), an item hitting or blocked
            v.TrickEvent += (kind, pts, n) =>
            {
                if (v.Finished) return;
                if (kind == "land") { if (pts >= 900 || n >= 4) Call("bigcombo", v); else if (n >= 2 || pts >= 300) Call("combo", v); if (pts >= 250) GameSounds.Instance?.Play(pts >= 800 ? "applause" : "loopcrowd", pts >= 800 ? .45f : .35f); }
                else if (kind == "shift") Call("shifttrick", v);
                else if (kind == "fail" && pts >= 150) Call("wipeout", v);
                if (Buck.Instance && kind != "trick") Buck.Instance.Judge(v, kind, pts);
            };
            v.ItemHit += (k, by, how) => { if (how == "hit") Call("ithit", v); else if (how == "blocked") Call("itblock", v); };
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
                    if (Challenge.Active == null) Call(game.StuntShow && pools.ContainsKey("showstart") ? "showstart" : game.Race && game.Race.Active ? "start" : "trial", v, true);   // Round 17: Stunt Show   // Round 13: a rival challenge has its own line (ChallengeRunner)
                    break;
                case "go": if (v == game.Player) { Call("go", v, true); nextFill = Time.time + 9f; } break;
                case "lap": if (v == game.Player) Call(v.Lap >= v.LapsTotal ? "lapfinal" : "lapnext", v, true); break;   // Round 17: laps on every course
                case "shortcut": if (text.Contains(": in") && !text.Contains(" flight")) Call("shortcut", v); break;   // Round 13: no flight branch of a choice jump (Lake / Cliff / Midway flight): the wings lines cover those
                case "gate":
                    if (text.Contains("near miss")) Call("miss", v);
                    if (text.Contains("G9") && !finalCalled && v.Lap >= v.LapsTotal) { finalCalled = true; Call("final", v); }   // Round 17: the final stretch of the last lap
                    if (v.Course.laps > 1 && v == game.Player && (text.Contains(" lap 2 ") || text.Contains(" lap 3 "))) Call(text.Contains(" lap 3 ") ? "dmlap3" : "dmlap2", v, true);   // Round 16: lap 2 / the final lap
                    break;
                case "reset": if (text.Contains("water") && v.Mode == Mode.Drive && v.Course.marks != null && v.Progress < v.Course.marks.GetValueOrDefault("G1", 0)) Call("splash", v); break;
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
                    if (v.Course && v.Course.stunts != null) foreach (var j in v.Course.stunts.Jumps) { float d = v.Progress - j.lip; if (d > 0 && d < best) { best = d; which = j.name; } }
                    if (which == "Coaster Gap" && Call("ccgap", v)) break;
                    if (which == "Clown Jump" && Call("btjump", v)) break;
                    if (which == "Flip Bucket" && Call("dmjump", v)) break;
                    if (which == "Creek Jump" && Call("creek", v)) break;
                    if (which == "Cliff Gap" && Call("cliff", v)) break;
                    if (v.LastAirTime > 1.3f) Call("jump", v);
                    break;
                case "wings": stayPending = false; Call("wings", v); break;
                // Round 10: the coaster rails (Coaster Coast)
                case "railin": if (v.Rail && v.Rail.label.Contains("Globe")) Call("btglobe", v); else if (v.Rail && v.Rail.label.Contains("cannon")) Call("btcannon", v); else if (v.Rail && v.Rail.label.Contains("rail A")) Call("cccoaster", v); else if (v.Rail) Call("cccork", v); break;
                case "railout": if (Coaster(v) && v.Mode == Mode.Drive && v.Progress > (v.Course && v.Course.marks != null && v.Course.marks.TryGetValue("crest", out var cr) ? cr - 20 : 1e9f)) Call("cctophat", v); break;
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
                if (!winnerCalled && race.Finishers.Count > 0 && Challenge.Active == null)   // Round 13: a rival challenge has its own result lines
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
                        string who = NameKey(r);   // Round 11: by the racer's name (any driver may be an AI rival now)
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
            if (!h.Course || h.Holding) return;
            if (DamCourse(h))   // Round 16: the set-pieces once a race, the landmarks once a lap for the water they are for
            {
                if (h == game.Player && game.Cam)
                {
                    SetPiece.Track(game.Cam);
                    foreach (var pool in SetPiecePools)
                    {
                        if (!pool.StartsWith("dm")) continue; string key = "1" + pool; if (marksDone.Contains(key) || !pools.ContainsKey(pool)) continue;
                        if (SetPiece.BigOnScreen(pool, DinoHeld)) { marksDone.Add(key); pendingMark = pool; pendingUntil = Time.time + 4f; break; }
                    }
                }
                int lap = h.Course.LapAt(h.Progress);
                foreach (var lm in DamLandmarks)
                {
                    if (lm.stage >= 0 && lm.stage != DamWater.Stage) continue;
                    string mk = lap <= 1 ? lm.mark : lm.mark + "@" + lap;
                    if (h.Course.marks == null || !h.Course.marks.TryGetValue(mk, out var cm)) continue;
                    float at = cm + lm.at; string key = (h == game.Player ? "1" : "2") + lm.pool + lap;
                    if (!marksDone.Contains(key) && h.Progress > at && h.Progress < at + 120f) { marksDone.Add(key); pendingMark = lm.pool; pendingUntil = Time.time + 5f; }
                }
                return;
            }
            if (Coaster(h) || BigTopCourse(h))
            {
                if (h == game.Player && game.Cam)
                {
                    SetPiece.Track(game.Cam);
                    foreach (var pool in SetPiecePools)
                    {
                        string key = "1" + pool; if (marksDone.Contains(key) || !pools.ContainsKey(pool)) continue;
                        if (SetPiece.BigOnScreen(pool, DinoHeld)) { marksDone.Add(key); pendingMark = pool; pendingUntil = Time.time + 4f; break; }
                    }
                }
                foreach (var lm in Coaster(h) ? CoasterLandmarks : BigTopLandmarks)
                {
                    if (h.Course.marks == null || !h.Course.marks.TryGetValue(lm.mark, out var cm)) continue;
                    float at = cm + lm.at; string key = (h == game.Player ? "1" : "2") + lm.pool;
                    if (!marksDone.Contains(key) && h.Progress > at && h.Progress < at + 120f) { marksDone.Add(key); pendingMark = lm.pool; pendingUntil = Time.time + 5f; }
                }
                return;
            }
            if (h.Course != game.Courses[0]) return;
            if (h == game.Player && game.Cam)
            {
                DinoActor.Track(game.Cam);
                foreach (var pool in DinoPools)
                {
                    string key = "1" + pool; if (marksDone.Contains(key) || !pools.ContainsKey(pool)) continue;
                    if (DinoActor.BigOnScreen(pool, DinoHeld)) { marksDone.Add(key); pendingMark = pool; pendingUntil = Time.time + 4f; break; }
                }
            }
            foreach (var lm in Landmarks)
            {
                if (!ParadisePlunge.Marks.TryGetValue(lm.mark, out var m)) continue;
                float at = m + lm.at; string key = (h == game.Player ? "1" : "2") + lm.pool;
                if (!marksDone.Contains(key) && h.Progress > at && h.Progress < at + 120f) { marksDone.Add(key); pendingMark = lm.pool; pendingUntil = Time.time + 5f; }
            }
        }
        string pendingMark = ""; float pendingUntil;
        // a landmark seen in the last 5 s: said now if he is free
        bool Landmark(ShiftVehicle h)
        {
            if (pendingMark == "" || Time.time > pendingUntil) return false; var p = pendingMark;
            if (System.Array.IndexOf(SetPiecePools, p) >= 0 && game.Cam) { SetPiece.Track(game.Cam); var sp = SetPiece.BigOnScreen(p, 0f); if (!sp) return false; pendingMark = ""; return Call(p, h); }
            if (IsDinoPool(p) && game.Cam) { DinoActor.Track(game.Cam); var d = DinoActor.BigOnScreen(p, 0f); if (!d) return false; pendingMark = ""; if (!Call(p, h)) return false; DinoLines.Add((Time.time, p, d.ShareNow)); return true; }   // still big, or wait
            pendingMark = ""; return Call(p, h);
        }
        void LateUpdate() { if (pendingMark != "" && !Talking && Time.time > nextAny && Time.time > Driver.AnySpeakingUntil && Time.time <= pendingUntil) Landmark(game ? game.Player : null); }

        readonly Dictionary<ShiftVehicle, float> finishedAt = new Dictionary<ShiftVehicle, float>();
        float FinishedAt(ShiftVehicle v) { if (!finishedAt.TryGetValue(v, out var t)) finishedAt[v] = t = Time.time; return t; }

        static string NameKey(ShiftVehicle r) => r.RacerName.StartsWith("Rocket") ? "rae" : r.RacerName.StartsWith("Chip") ? "chip" : r.RacerName.StartsWith("Vivi") ? "vivi" : r.RacerName.StartsWith("Abi") ? "abi" : "";
        void CallWinner(ShiftVehicle w)
        {
            string id = w == game.Player2 ? "win_p2" : NameKey(w) != "" ? "win_" + NameKey(w) : "";   // Round 11: the winner by name (player 1 may drive anyone)
            if (!pools.TryGetValue("win", out var all)) return;
            var own = all.Where(c => c.name == id).ToList(); var any = all.Where(c => c.name.StartsWith("win_any")).ToList();
            var pick = own.Count > 0 && (any.Count == 0 || Random.value < .7f) ? own : any.Count > 0 ? any : all;
            Play("win", pick, 3, w, true);
        }

        public bool Call(string pool, ShiftVehicle who, bool force = false)
        {
            pool = ForCourse(pool, who); if (pool == "") return false;
            if (Buck.Instance && Buck.Instance.Busy) { Skipped.Add($"{Time.time:0.0} {pool}: Buck talking"); return false; }   // Round 11: never over Buck
            if (!pools.TryGetValue(pool, out var list) || list.Count == 0) { Skipped.Add($"{Time.time:0.0} {pool}: no clips"); return false; }
            if (pool == "results") list = list.Where(c => !c.name.StartsWith("results_lost")).ToList();
            return Play(pool, list, Rules.TryGetValue(pool, out var r) ? r.pri : 1, who, force);
        }
        bool Play(string pool, List<AudioClip> list, int pri, ShiftVehicle who, bool force = false)
        {
            float now = Time.time; var rule = Rules.TryGetValue(pool, out var r) ? r : (20f, 1);
            if (Buck.Instance && Buck.Instance.Busy) { Skipped.Add($"{now:0.0} {pool}: Buck talking"); return false; }   // Round 11: never over Buck, not even GO / the winner
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
            voice.clip = clip; voice.Play(); SoundLog.Add("voice", "Barnaby " + clip.name);
            busyUntil = now + clip.length; busyPriority = pri; nextAny = busyUntil + AnyGap; poolNext[pool] = now + rule.Item1; clipLast[clip.name] = now; usedThisRace.Add(clip.name);
            if (nextFill > 0) nextFill = Mathf.Max(nextFill, busyUntil + Random.Range(5f, 8f));
            Subtitle = words.TryGetValue(clip.name, out var w) ? w : ""; SubtitleUntil = now + Mathf.Max(2f, clip.length + .6f);
            string line = $"{now:0.0} {pool} {clip.name}{(who ? $" ({who.RacerName})" : "")}"; Said.Add(line); Lines.Add((now, clip.length, pool, clip.name));
            Debug.Log($"[Caller] {Name}: {line}: \"{Subtitle}\"");
            return true;
        }
    }
}
