using System;
using System.IO;
using UnityEngine;

namespace ShiftStorm
{
    // Testing rule 1 (CLAUDE.md): every test run is muted from the first frame. -mute (or any check argument: -check, -batchmode, -executeMethod,
    // the launcher's own start-up check -racerTestSave) sets AudioListener.volume to 0 at SubsystemRegistration, before the first scene loads and so
    // before anything can make a sound, and turns run-in-background on (testing rule 2: a check keeps running minimized / unfocused).
    // Adapted from chastain_rush's CheckRun.cs (same idea; ShiftStorm adds the explicit -mute flag Dan asked for, and the -checkOut folder).
    // The game Dan starts himself has none of these arguments and is never muted.
    public static class QuietRun
    {
        public static bool Muted { get; private set; }
        public static bool Check { get; private set; }
        public static string Dir { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            Muted = Check = false; Dir = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a == "-mute") Muted = true;
                if (a == "-check" || a == "-batchmode" || a == "-executeMethod" || a == "-racerTestSave") { Muted = true; if (a != "-racerTestSave") Check = true; }
                if (a == "-checkOut" && i + 1 < args.Length) Dir = args[i + 1];
                else if (a == "-racerTestSave" && i + 1 < args.Length && Dir == null) Dir = args[i + 1];
            }
            if (!Muted) return;
            AudioListener.volume = 0f;
            Application.runInBackground = true;
            if (string.IsNullOrEmpty(Dir)) Dir = Path.Combine(Path.GetTempPath(), "ShiftStormCheck");
            try { Dir = Path.GetFullPath(Dir); Directory.CreateDirectory(Dir); } catch (Exception) { }
            Debug.Log("[Quiet] muted from the first frame (AudioListener.volume = 0); files go to " + Dir);
        }

        // Sound on for one moment of a check, then off again (say which step and for how long in the report).
        public static void Hear(bool on)
        {
            if (!Muted) return;
            AudioListener.volume = on ? 1f : 0f;
            Debug.Log("[Quiet] sound " + (on ? "ON" : "off"));
        }
    }
}
