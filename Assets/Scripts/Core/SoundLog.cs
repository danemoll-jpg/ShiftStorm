using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 9 item 3 (Dan: "The finish plays both the finish from Tee'd Off and the new one we created for this game"): every sound the
    // game starts, with its time and kind, so a check can list event by event what plays and find two of a kind for one event (two
    // music / jingle cues, two crowd sounds, two splashes, two whooshes, two impacts). Kinds:
    //   music  a song starting (race, menu, theme, results)       jingle  a short musical cue (none left: Tee'd Off's win jingle was one)
    //   crowd  cheers, applause, claps, "aww", "ooh"               splash  water            whoosh  air / change / boost whooshes
    //   impact wall hits, landings, scrapes                         mech    gear, canopy, wings, the boost kick, beeps, the horn, resets
    //   chime  gate / ring chimes      voice  Barker and the drivers      ambience  far calls, dinosaurs      ui  menu clicks
    public static class SoundLog
    {
        public struct Entry { public float t; public string kind, what; }
        public static readonly List<Entry> Entries = new List<Entry>();
        public static bool On = true;
        static readonly Dictionary<string, float> last = new Dictionary<string, float>();
        // one sound of a kind at a time: true when one of this kind started less than `window` s ago (the caller then skips its own)
        public static bool Busy(string kind, float window = .5f) => Exclusive(kind) && last.TryGetValue(kind, out var t) && Time.time - t < window && Time.time >= t;
        public static int Merged;   // sounds skipped by that rule (reported)
        public static void Add(string kind, string what)
        {
            last[kind] = Time.time;
            if (!On) return;
            Entries.Add(new Entry { t = Time.time, kind = kind, what = what });
            if (Entries.Count > 20000) Entries.RemoveRange(0, 5000);
        }
        // the kinds where two at once for one event is a doubled sound
        public static bool Exclusive(string kind) => kind == "music" || kind == "jingle" || kind == "crowd" || kind == "splash" || kind == "whoosh" || kind == "impact" || kind == "chime";
    }
}
