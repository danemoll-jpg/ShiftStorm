using System;
using System.IO;
using UnityEngine;

namespace ShiftStorm
{
    // Round 2b item 3 (Dan on the Deck: "The game is not coming up fullscreen"). Copied from chastain_rush's ScreenSetting (Round 24 A3,
    // the one Dan uses on the Deck), trimmed to the screen mode: the game opens borderless full screen (FullScreenWindow) at the desktop's
    // own resolution (Display.main.systemWidth x systemHeight: 1280 x 800 on the Deck), or windowed if Dan last chose that in the menu
    // (Menu > Screen) or with Alt+Enter. The choice is saved in display.json beside the best times and applied before the first scene loads,
    // so it wins over the window mode and size Unity remembers from an earlier run (its own saved setting is ignored), whether the game is
    // started directly or by the launcher (the launcher passes no screen arguments). A test run (-mute / -check / -batchmode / the launcher's
    // start-up check) never applies or saves it: it only logs what it would do, so a check never opens or resizes a window.
    public static class ScreenSetting
    {
        [Serializable] sealed class Saved { public bool fullscreen = true; }
        public static bool Fullscreen { get; private set; } = true;
        public static string StartupLine { get; private set; } = "";

        static string FilePath() => Path.Combine(Application.persistentDataPath, "display.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Fullscreen = true;
            bool test = QuietRun.Muted || Application.isBatchMode;
            string before = $"Unity started it {Screen.fullScreenMode} {Screen.width} x {Screen.height}";
            string args = string.Join(" ", Array.FindAll(Environment.GetCommandLineArgs(), a => a.StartsWith("-screen") || a == "-popupwindow" || a == "-window-mode"));
            bool launcher = Array.IndexOf(Environment.GetCommandLineArgs(), "-racerLauncherReady") >= 0;
            if (!test)
            {
                try { if (File.Exists(FilePath())) { var s = new Saved(); JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath()), s); Fullscreen = s.fullscreen; } }
                catch (Exception e) { Debug.LogWarning("[Screen] display.json unreadable: " + e.Message); }
            }
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            StartupLine = $"[Screen] start-up ({(launcher ? "started by the launcher" : "started directly")}, screen arguments: {(args == "" ? "none" : args)}): {before}; desktop {w} x {h}; " +
                          $"{(test ? "test run: left as it is, would set" : "set")} {(Fullscreen ? $"FullScreenWindow (borderless full screen) {w} x {h}" : $"Windowed {Mathf.RoundToInt(w * .75f)} x {Mathf.RoundToInt(h * .75f)}")}";
            Debug.Log(StartupLine);
            if (!test) Apply();
        }

        public static void Set(bool fullscreen)
        {
            Fullscreen = fullscreen;
            if (QuietRun.Muted || Application.isBatchMode) { Debug.Log($"[Screen] test run: would switch to {(fullscreen ? "full screen" : "windowed")} (not applied, not saved)"); return; }
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath())); File.WriteAllText(FilePath(), JsonUtility.ToJson(new Saved { fullscreen = fullscreen }, true)); }
            catch (Exception e) { Debug.LogWarning("[Screen] could not save display.json: " + e.Message); }
            Apply();
        }
        public static void Toggle() => Set(!Fullscreen);

        static void Apply()
        {
            if (Application.isEditor) return;
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            if (Fullscreen) Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(Mathf.RoundToInt(w * .75f), Mathf.RoundToInt(h * .75f), FullScreenMode.Windowed);
            Debug.Log($"[Screen] {(Fullscreen ? "full screen" : "windowed")}, desktop {w} x {h}");
        }
    }
}
