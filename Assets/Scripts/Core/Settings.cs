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
        [Serializable] sealed class Data { public int version = 1; public bool invertVertical; public bool showStats; public bool invertChosen; public int difficulty; }   // Round 4: difficulty 0 Easy (default), 1 Normal, 2 Hard
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
        public static bool InvertChosen => D.invertChosen;
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
        static bool overrideSet; static Difficulty overrideLevel;   // the player has set it once: the first-flight note is no longer needed
    }
}
