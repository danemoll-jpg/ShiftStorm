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
        [Serializable] sealed class Data { public int version = 1; public bool invertVertical; public bool showStats; public bool invertChosen; public int difficulty; public string design = ""; public bool invertVertical2; public int view, view2; public bool splitLeftRight; public int musicVol = 10, sfxVol = 10, voiceVol = 10; }   // Round 7: Music / Effects / Voices, 0..10   // Round 6: player 2's invert, each player's camera view (CameraViews), the split-screen layout   // Round 4: difficulty 0 Easy (default), 1 Normal, 2 Hard; Round 5: the vehicle design raced with ("" = the default: the first imported design, Dan's pick)
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
                try { if (File.Exists(FilePath)) d = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)); } catch (Exception e) { Debug.LogWarning("[Settings] could not read " + FilePath + ": " + e.Message); }
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
        public static int Step(string which)
        {
            int Next(int x) => x <= 0 ? 10 : Mathf.Max(0, x - 2);
            if (which == "music") D.musicVol = Next(D.musicVol); else if (which == "sfx") D.sfxVol = Next(D.sfxVol); else D.voiceVol = Next(D.voiceVol);
            if (!QuietRun.Check) Save();
            int v = which == "music" ? D.musicVol : which == "sfx" ? D.sfxVol : D.voiceVol; Debug.Log($"[Settings] {which} volume {v * 10} %"); return v;
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
        static bool overrideSet; static Difficulty overrideLevel;   // the player has set it once: the first-flight note is no longer needed
    }
}
