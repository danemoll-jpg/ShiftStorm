using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShiftStorm
{
    // Best lap per course, kept in a small JSON file (persistentDataPath/besttimes.json; a check run keeps its own in its folder, or in
    // "-saveDir <dir>", so Dan's real best time is never touched by a test).
    public static class BestTimes
    {
        [Serializable] public sealed class Entry { public string course; public float lap; public float[] splits; public string date; public float single; }   // Round 16: single = the best one lap of a multi-lap course
        [Serializable] sealed class Store { public List<Entry> entries = new List<Entry>(); }

        public static string FilePath
        {
            get
            {
                var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-saveDir");
                string dir = i >= 0 && i + 1 < a.Length ? a[i + 1] : QuietRun.Check ? QuietRun.Dir : Application.persistentDataPath;
                return Path.Combine(dir, "besttimes.json");
            }
        }
        static Store Load()
        {
            try { if (File.Exists(FilePath)) return JsonUtility.FromJson<Store>(File.ReadAllText(FilePath)) ?? new Store(); } catch (Exception e) { Debug.LogWarning("[Best] could not read " + FilePath + ": " + e.Message); }
            return new Store();
        }
        public static Entry Get(string course) { foreach (var e in Load().entries) if (e.course == course) return e; return null; }
        // Saves the lap when it is the course's best; returns true when it is.
        public static bool Submit(string course, float lap, List<float> splits) => Submit(course, lap, splits, -1, out _);
        // single: the best one lap of this run on a multi-lap course (-1: none); newSingle: it beat the best lap on file
        public static bool Submit(string course, float lap, List<float> splits, float single, out bool newSingle)
        {
            var s = Load(); var e = s.entries.Find(x => x.course == course); newSingle = false; bool best = false;
            if (e == null) { e = new Entry { course = course }; s.entries.Add(e); best = true; }
            else if (e.lap <= 0 || lap < e.lap) best = true;
            if (best) { e.lap = lap; e.splits = splits.ToArray(); e.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"); }
            if (single > 0 && (e.single <= 0 || single < e.single)) { e.single = single; newSingle = true; }
            if (!best && !newSingle) return false;
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, JsonUtility.ToJson(s, true)); }
            catch (Exception ex) { Debug.LogWarning("[Best] could not save " + FilePath + ": " + ex.Message); return false; }
            Debug.Log($"[Best] {(best ? $"new best on {course}: {lap:0.00} s" : $"{course}: best time kept")}{(newSingle ? $", best single lap {single:0.00} s" : "")} -> {FilePath}");
            return best;
        }
    }

    // Time trial on a course with timeTrial set (Paradise Plunge): the countdown and the clock are the vehicle's (ShiftVehicle.Begin holds it
    // on the grid, StartTime is GO), the splits are its segment times; this keeps the results: on the finish it waits for the last split,
    // saves the best lap, and the HUD shows the results until a restart (Reset, or the menu).
    public sealed class TimeTrial : MonoBehaviour
    {
        public ShiftVehicle v;
        public bool HasResults { get; private set; }
        public float Lap { get; private set; }
        public List<float> SegmentTimes { get; } = new List<float>();   // per segment (not cumulative)
        public BestTimes.Entry BestBefore { get; private set; }
        public BestTimes.Entry Best { get; private set; }
        public bool NewBest { get; private set; }
        public bool NewBestLap { get; private set; }   // Round 16: a multi-lap course's best single lap
        public event Action Results;
        float finishedAt = -1;

        void Start() { v.Event += (kind, text) => { if (kind == "start") { HasResults = false; finishedAt = -1; NewBest = false; Best = v.Course && v.Course.timeTrial ? BestTimes.Get(v.Course.courseName) : null; } }; }

        void Update()
        {
            if (!v || !v.Course || !v.Course.timeTrial || !v.Finished || HasResults) return;
            if (finishedAt < 0) finishedAt = Time.unscaledTime;
            if (v.Splits.Count < v.Course.segments.Count * (v.OwnLaps ? v.LapsTotal : 1) && Time.unscaledTime - finishedAt < .5f) return;   // Round 17: every lap's segments   // the last split lands a step after the finish
            Lap = v.FinishTime; SegmentTimes.Clear(); float prev = 0;
            foreach (var s in v.Splits) { SegmentTimes.Add(s - prev); prev = s; }
            BestBefore = BestTimes.Get(v.Course.courseName);
            // Round 3: a race against the rivals shows the lap and splits but only a Time Trial (alone) sets the best lap
            float single = v.Course.laps > 1 && SegmentTimes.Count > 0 ? Mathf.Min(SegmentTimes.ToArray()) : v.OwnLaps ? v.BestLap : -1; NewBestLap = false;
            bool alone = !(Game.Instance && Game.Instance.RaceMode);
            bool nbl = false; NewBest = alone && BestTimes.Submit(v.Course.courseName, Lap, SegmentTimes, single, out nbl); NewBestLap = alone && nbl;
            Best = BestTimes.Get(v.Course.courseName);
            HasResults = true;
            v.Note("results", $"results: lap {Lap:0.00} s, {SegmentTimes.Count} splits, best {(Best != null ? Best.lap.ToString("0.00") : "-")} s{(NewBest ? " (NEW BEST)" : "")}");
            Results?.Invoke();
        }

        public static string Clock(float s) => s < 0 ? "--:--.--" : $"{(int)(s / 60)}:{s % 60:00.00}";
    }
}
