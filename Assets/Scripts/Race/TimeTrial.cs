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
        [Serializable] public sealed class Entry { public string course; public float lap; public float[] splits; public string date; }
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
        public static bool Submit(string course, float lap, List<float> splits)
        {
            var s = Load(); var e = s.entries.Find(x => x.course == course);
            if (e != null && e.lap <= lap) return false;
            if (e == null) { e = new Entry { course = course }; s.entries.Add(e); }
            e.lap = lap; e.splits = splits.ToArray(); e.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, JsonUtility.ToJson(s, true)); }
            catch (Exception ex) { Debug.LogWarning("[Best] could not save " + FilePath + ": " + ex.Message); return false; }
            Debug.Log($"[Best] new best on {course}: {lap:0.00} s -> {FilePath}");
            return true;
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
        public event Action Results;
        float finishedAt = -1;

        void Start() { v.Event += (kind, text) => { if (kind == "start") { HasResults = false; finishedAt = -1; NewBest = false; Best = v.Course && v.Course.timeTrial ? BestTimes.Get(v.Course.courseName) : null; } }; }

        void Update()
        {
            if (!v || !v.Course || !v.Course.timeTrial || !v.Finished || HasResults) return;
            if (finishedAt < 0) finishedAt = Time.unscaledTime;
            if (v.Splits.Count < v.Course.segments.Count && Time.unscaledTime - finishedAt < .5f) return;   // the last split lands a step after the finish
            Lap = v.FinishTime; SegmentTimes.Clear(); float prev = 0;
            foreach (var s in v.Splits) { SegmentTimes.Add(s - prev); prev = s; }
            BestBefore = BestTimes.Get(v.Course.courseName);
            // Round 3: a race against the rivals shows the lap and splits but only a Time Trial (alone) sets the best lap
            NewBest = !(Game.Instance && Game.Instance.RaceMode) && BestTimes.Submit(v.Course.courseName, Lap, SegmentTimes);
            Best = BestTimes.Get(v.Course.courseName);
            HasResults = true;
            v.Note("results", $"results: lap {Lap:0.00} s, {SegmentTimes.Count} splits, best {(Best != null ? Best.lap.ToString("0.00") : "-")} s{(NewBest ? " (NEW BEST)" : "")}");
            Results?.Invoke();
        }

        public static string Clock(float s) => s < 0 ? "--:--.--" : $"{(int)(s / 60)}:{s % 60:00.00}";
    }
}
