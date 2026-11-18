using System;
using System.IO;
using UnityEngine;

namespace ShiftStorm
{
    // Round 3: the player's settings in one small JSON file (settings.json beside besttimes.json: persistentDataPath, or "-saveDir <dir>",
    // or a check run's own folder), so a check can prove a setting survives a restart without touching Dan's own. On the first start with
    // no file, the Round 1-2b PlayerPrefs values (invertVertical, showStats) are taken over once.
    public static class Settings
    {
        [Serializable] sealed class Data { public int version = 2; public bool invertVertical; public bool showStats; public bool invertChosen; public int difficulty; public string design = ""; public bool invertVertical2; public int view, view2; public bool splitLeftRight; public int musicVol = 10, sfxVol = 10, voiceVol = 10; public int engineVol = EngineDefault; public bool playtestLogs = true; public int buck; public int driver; public int driver2 = 1; public string design2 = ""; public bool tourChosen, tourUnlocked; public int paint; public int graphics = -1; public int crowdVol = 10; public bool itemsOff; public int laps = 3; }   // Round 17: Items On / Off (default On), the race's lap count (1-5, default 3)   // Round 14: the Crowd volume (its own slider)   // Round 13: the Garage paint (Paints), the Graphics preset (-1 Auto)   // Round 12: "Unlock all Tour stops" (tourChosen false: UnlockTourDefault)   // Round 11: Buck's briefings 0 On / 1 Short / 2 Off; player 1's / 2's driver (Lineup.All: 0 Abi, 1 Rae, 2 Chip, 3 Vivi) and player 2's machine   // Round 8 item 6: Engines split out of Effects (default 60 %); item 10: playtest logs (default on)   // Round 7: Music / Effects / Voices, 0..10   // Round 6: player 2's invert, each player's camera view (CameraViews), the split-screen layout   // Round 4: difficulty 0 Easy (default), 1 Normal, 2 Hard; Round 5: the vehicle design raced with ("" = the default: the first imported design, Dan's pick)
        static Data d;

        public static string FilePath
        {
            get
            {
                var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-saveDir");
                string dir = i >= 0 && i + 1 < a.Length ? a[i + 1] : QuietRun.Check ? QuietRun.Dir : Application.persistentDataPath;
                return Path.Combine(dir, "settings.json");
            }
        }
        static Data D
        {
            get
            {
                if (d != null) return d;
                // Round 14: a build 15 file has no Crowd volume: 100 %
                try { if (File.Exists(FilePath)) { var text = File.ReadAllText(FilePath); d = JsonUtility.FromJson<Data>(text); if (d != null && !text.Contains("\"crowdVol\"")) d.crowdVol = 10; } } catch (Exception e) { Debug.LogWarning("[Settings] could not read " + FilePath + ": " + e.Message); }
                if (d != null && d.version < 2)
                {
                    // Round 8 item 6: a build 1-8 file had one Effects volume for engines and effects: engines now have their own, quieter
                    // (60 % of what the old Effects was, never louder than the new default); music, voices and effects keep their values
                    d.engineVol = Mathf.Min(EngineDefault, Mathf.RoundToInt(Mathf.Clamp(d.sfxVol, 0, 10) * .6f)); d.version = 2; d.playtestLogs = true;
                    Debug.Log($"[Settings] settings.json from an older build: Engines volume set to {d.engineVol * 10} % (Effects was {d.sfxVol * 10} %)");
                    if (!QuietRun.Check) Save();
                }
                if (d == null)
                {
                    d = new Data();
                    if (!QuietRun.Check) { d.invertVertical = PlayerPrefs.GetInt("invertVertical", 0) == 1; d.showStats = PlayerPrefs.GetInt("showStats", 0) == 1; }
                }
                Debug.Log($"[Settings] {FilePath}: invert flight / dive {(d.invertVertical ? "on" : "off")}, FPS readout {(d.showStats ? "on" : "off")}");
                return d;
            }
        }
        static void Save()
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath + ".tmp", JsonUtility.ToJson(d, true)); if (File.Exists(FilePath)) File.Delete(FilePath); File.Move(FilePath + ".tmp", FilePath); }
            catch (Exception e) { Debug.LogWarning("[Settings] could not save " + FilePath + ": " + e.Message); }
        }
        public static bool InvertVertical { get => D.invertVertical; set { D.invertVertical = value; D.invertChosen = true; Save(); Debug.Log($"[Settings] invert flight / dive controls {(value ? "on" : "off")} (saved)"); } }
        public static bool ShowStats { get => D.showStats; set { D.showStats = value; Save(); } }
        // Round 6: player 2's own invert setting, each player's camera view (0 Chase, 1 Far chase, 2 First person, 3 Front) and the
        // split-screen layout (false: top / bottom, player 1 on top, as Chastain Rush). A check run reads the defaults and saves nothing.
        public static bool InvertVertical2 { get => D.invertVertical2; set { D.invertVertical2 = value; if (!QuietRun.Check) Save(); Debug.Log($"[Settings] player 2 invert flight / dive controls {(value ? "on" : "off")}"); } }
        public static int View(int player) => QuietRun.Check ? 0 : player == 2 ? D.view2 : D.view;
        public static void SetView(int player, int view) { if (QuietRun.Check) return; if (player == 2) D.view2 = view; else D.view = view; Save(); }
        public static bool SplitLeftRight { get => D.splitLeftRight; set { D.splitLeftRight = value; if (!QuietRun.Check) Save(); } }
        public static bool InvertChosen => D.invertChosen;
        // Round 7: the three volumes (Menu > Settings), 0..1; a check run reads them (all full) and saves nothing. The run's own -mute
        // still holds AudioListener.volume at 0 under them.
        public static float Music => Mathf.Clamp(D.musicVol, 0, 10) / 10f;
        public static float Effects => Mathf.Clamp(D.sfxVol, 0, 10) / 10f;
        public static float Voices => Mathf.Clamp(D.voiceVol, 0, 10) / 10f;
        public static float Engines => Mathf.Clamp(D.engineVol, 0, 10) / 10f;
        // Round 14 item 2 (Dan: "the crowd noise is far too loud"): the crowd (murmur, cheers, the GO roar, the finish's crowd sound, the
        // near-miss "aww", the loop's "ooh") has its own volume (default 100 %, up to 200 %), and sits CrowdGain (-19 dB) under where build 15
        // had it: the mix check measured -11 dB still leaving the GO roar and the finish cheer over the engine and level with the music
        public static float Crowd => Mathf.Clamp(D.crowdVol, 0, 20) / 10f;
        public const float CrowdGain = .11f;
        public static int MaxVolume(string which) => which == "crowd" ? 20 : 10;
        public static float CrowdLevel => Crowd * CrowdGain;
        // the volume rows: "music", "voice", "engine", "sfx", "crowd" (0..10 steps of 10 %)
        public static int Volume(string which) => which == "music" ? D.musicVol : which == "sfx" ? D.sfxVol : which == "engine" ? D.engineVol : which == "crowd" ? D.crowdVol : D.voiceVol;
        public static void SetVolume(string which, int v)
        {
            v = Mathf.Clamp(v, 0, MaxVolume(which));
            if (which == "music") D.musicVol = v; else if (which == "sfx") D.sfxVol = v; else if (which == "engine") D.engineVol = v; else if (which == "crowd") D.crowdVol = v; else D.voiceVol = v;
            if (!QuietRun.Check) Save(); Debug.Log($"[Settings] {which} volume {v * 10} %");
        }
        public const int EngineDefault = 6;
        // Round 8 item 10: playtest logs (Menu > Settings), on by default; a check run reads the default and saves nothing
        public static bool PlaytestLogs { get => D.playtestLogs; set { D.playtestLogs = value; if (!QuietRun.Check) Save(); Debug.Log($"[Settings] playtest logs {(value ? "on" : "off")}"); } }
        public static int Step(string which)
        {
            int Next(int x) => x <= 0 ? 10 : Mathf.Max(0, x - 2);
            SetVolume(which, Next(Volume(which))); return Volume(which);
        }
        // Round 4: the AI's difficulty (how the rivals drive, never their vehicle). A check may set it for its run only ("-difficulty
        // Easy|Normal|Hard"), without saving.
        public static Difficulty Difficulty
        {
            get
            {
                var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-difficulty");
                if (overrideSet) return overrideLevel;
                if (i >= 0 && i + 1 < a.Length && Enum.TryParse<Difficulty>(a[i + 1], true, out var x)) return x;
                return (Difficulty)Mathf.Clamp(D.difficulty, 0, 2);
            }
            set { if (QuietRun.Check) { overrideSet = true; overrideLevel = value; } else { D.difficulty = (int)value; Save(); } Debug.Log($"[Settings] AI difficulty {value}{(QuietRun.Check ? " (check run: not saved)" : " (saved)")}"); }
        }
        // Round 5: the vehicle design (Garage > Race with this design); "-design <name>" for a run only
        public static string Design
        {
            get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-design"); if (i >= 0 && i + 1 < a.Length) return a[i + 1]; return string.IsNullOrEmpty(D.design) ? VehicleDesigns.Default : D.design; }
            set { D.design = value; if (!QuietRun.Check) Save(); Debug.Log($"[Settings] vehicle design {value}{(QuietRun.Check ? " (check run: not saved)" : " (saved)")}"); }
        }
        // Round 11: Buck's briefings (Menu > Settings: On / Short / Off); each player's driver and player 2's machine (choose your racer).
        // A check run reads the defaults (briefings On, Abi + Jetstream) and saves nothing.
        public static int BuckBriefings { get => Mathf.Clamp(D.buck, 0, 2); set { D.buck = Mathf.Clamp(value, 0, 2); if (!QuietRun.Check) Save(); Debug.Log($"[Settings] Buck's briefings {BuckNames[D.buck]}"); } }
        public static readonly string[] BuckNames = { "On", "Short", "Off" };
        // Round 12 item 1 (Dan: "it was locked behind the tour ... I don't want to keep racing through the first track"): Race, Time Trial and
        // 2 Players never lock a course; only The Tour's own stops are in order, and while the game is being built all of them are open too.
        // Menu > Settings "Unlock all Tour stops" On / Off (saved once chosen). For the release: set UnlockTourDefault to false (a player who
        // never touched the setting then plays the Tour in order; one who chose keeps the choice).
        public const bool UnlockTourDefault = true;
        public static bool UnlockAllTour { get => D.tourChosen ? D.tourUnlocked : UnlockTourDefault; set { D.tourChosen = true; D.tourUnlocked = value; if (!QuietRun.Check) Save(); Debug.Log($"[Settings] unlock all Tour stops {(value ? "on" : "off")}"); } }
        public static int Driver { get => Mathf.Clamp(D.driver, 0, 3); set { D.driver = Mathf.Clamp(value, 0, 3); if (!QuietRun.Check) Save(); } }
        public static int Driver2 { get => Mathf.Clamp(D.driver2, 0, 3); set { D.driver2 = Mathf.Clamp(value, 0, 3); if (!QuietRun.Check) Save(); } }
        public static string Design2 { get => string.IsNullOrEmpty(D.design2) ? "" : D.design2; set { D.design2 = value ?? ""; if (!QuietRun.Check) Save(); } }
        // Round 13: the Garage paint (Paints.All index; won in rival challenges) and the Graphics preset (-1 Auto, 0 Deck, 1 Low, 2 Medium, 3 High;
        // "-graphics Deck|Low|Medium|High" for a run only). A check run reads the defaults and saves nothing.
        public static int Paint { get => Mathf.Max(0, D.paint); set { D.paint = Mathf.Max(0, value); if (!QuietRun.Check) Save(); Debug.Log($"[Settings] paint {value}"); } }
        public static int Graphics { get => Mathf.Clamp(D.graphics, -1, 3); set { D.graphics = Mathf.Clamp(value, -1, 3); if (!QuietRun.Check) Save(); } }
        // Round 17: Menu > Settings > Gameplay > Items (default On; "-noItems" turns them off for a run); the race's lap count, chosen on the
        // choose-your-racer page (1-5, default 3). A check run races 1 lap (the older checks' timings) unless "-raceLaps N"; it saves nothing.
        public static bool Items { get => !D.itemsOff && Array.IndexOf(Environment.GetCommandLineArgs(), "-noItems") < 0; set { D.itemsOff = !value; if (!QuietRun.Check) Save(); Debug.Log($"[Settings] items {(value ? "on" : "off")}"); } }
        public static int Laps { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-raceLaps"); if (lapsSet) return lapsOverride; if (i >= 0 && i + 1 < a.Length && int.TryParse(a[i + 1], out int n)) return Mathf.Clamp(n, 1, 5); if (QuietRun.Check) return 1; return Mathf.Clamp(D.laps <= 0 ? 3 : D.laps, 1, 5); } set { if (QuietRun.Check) { lapsSet = true; lapsOverride = Mathf.Clamp(value, 1, 5); } else { D.laps = Mathf.Clamp(value, 1, 5); Save(); } } }
        static bool lapsSet; static int lapsOverride = 3;
        public static bool LapsSetByCheck => lapsSet;
        static bool overrideSet; static Difficulty overrideLevel;   // the player has set it once: the first-flight note is no longer needed
    }
}
