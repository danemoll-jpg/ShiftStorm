using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShiftStorm
{
    // Round 8 item 10 (Dan, 2026-11-14): playtest logs for the concept's playtest with friends. Every race / time trial on Paradise Plunge
    // writes one record per person playing (player 1, player 2 in split screen) to this session's file: <data folder>/playtest/
    // session-<date>-<time>.jsonl (one JSON object per line: easy to append, a crash keeps every race before it). The data folder is
    // persistentDataPath (or "-saveDir <dir>", or a check run's own folder). A record: driver, mode (race / time trial, 1 or 2 players),
    // difficulty, design, place / field, lap time, penalties, segment times, resets (where and why), missed gates and near-miss penalties,
    // shortcuts taken, choice-jump picks, form-change incidents (a change cut short by a reset, a gate crossed in the wrong form), wall hits
    // (where, angle, speed kept). A race left before the finish is written as "abandoned". Menu > Settings: "Save playtest logs" On / Off
    // (default On), "Open log folder". Tools/playtest_summary.py turns a folder of these into one readable summary.
    public sealed class PlaytestLog : MonoBehaviour
    {
        [Serializable] public sealed class Ev { public float t; public float at; public string segment = "", what = "", detail = ""; }
        [Serializable] public sealed class Record
        {
            public string session = "", build = "", date = "", course = "", player = "", driver = "", mode = "", difficulty = "", design = "", result = "";
            public int race, players, place, field, resets, wallHits, nearMisses, missedGates;
            public float lap = -1, penalty;
            // Round 17: laps, stunts, items
            public int laps = 1, stunts, tricksLanded, wipeouts, shiftTricks, bestCombo, itemsUsed, hitsTaken, hitsBlocked; public List<float> lapTimes = new List<float>(); public List<string> itemUses = new List<string>();
            public List<string> segmentNames = new List<string>(); public List<float> segmentTimes = new List<float>();
            public List<Ev> resetList = new List<Ev>(), gates = new List<Ev>(), shortcuts = new List<Ev>(), choices = new List<Ev>(), changes = new List<Ev>(), walls = new List<Ev>();
        }
        public static PlaytestLog Instance { get; private set; }
        static string session;
        public static string Folder { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-saveDir"); string dir = i >= 0 && i + 1 < a.Length ? a[i + 1] : QuietRun.Check ? QuietRun.Dir : Application.persistentDataPath; return Path.GetFullPath(Path.Combine(dir, "playtest")); } }
        public static string FilePath => Path.Combine(Folder, $"session-{session}.jsonl");
        public int Written { get; private set; }
        readonly Dictionary<ShiftVehicle, Record> open = new Dictionary<ShiftVehicle, Record>();
        readonly Dictionary<ShiftVehicle, bool> choicePending = new Dictionary<ShiftVehicle, bool>();
        readonly HashSet<ShiftVehicle> hooked = new HashSet<ShiftVehicle>();
        int races; Game game;

        public static PlaytestLog Make(Game g)
        {
            var p = g.gameObject.AddComponent<PlaytestLog>(); Instance = p; p.game = g;
            session = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            return p;
        }

        void Update()
        {
            if (!game) return;
            // a racer met for the first time (player 2 joins after its start event): hooked, and its race opened now if it is under way
            foreach (var h in game.Humans) if (h && hooked.Add(h)) { Hook(h); if (h.Course && h.Course.timeTrial && !h.Finished && !open.ContainsKey(h)) open[h] = Begin(h); }
            // the choice jump: the window closed and the racer is still a car
            foreach (var h in game.Humans)
                if (h && choicePending.TryGetValue(h, out var p) && p && !h.WingsOpen && Time.time > h.WingsUntil + .05f)
                { choicePending[h] = false; if (h.Mode == Mode.Drive && open.TryGetValue(h, out var r)) r.choices.Add(E(h, ChoiceName(h), "car")); }
        }

        void Hook(ShiftVehicle v)
        {
            v.Event += (kind, text) => OnEvent(v, kind, text);
            v.Stunt += s => { if (s == "wings" && open.TryGetValue(v, out var r)) { choicePending[v] = false; r.choices.Add(E(v, ChoiceName(v), "fly")); } if (s == "loopfail" && open.TryGetValue(v, out var r2)) r2.changes.Add(E(v, "loop failed", "")); };
            v.WallHit += (angle, keep, what) => { if (open.TryGetValue(v, out var r) && !v.Finished && keep < .97f) { r.wallHits++; r.walls.Add(E(v, what, $"{angle:0} deg, {keep * 100:0} % speed kept")); } };
        }
        // Round 13: which choice jump (the nearest choice lip behind the racer: Lake Jump, Cliff Gap, Midway Jump)
        static string ChoiceName(ShiftVehicle v)
        {
            string best = "choice jump"; float d = float.MaxValue; var st = v.Course ? v.Course.stunts : null; if (st == null) return best;
            foreach (var j in st.Jumps) if (j.choice && v.Progress >= j.lip - 30 && v.Progress - j.lip < d) { d = v.Progress - j.lip; best = j.name; }
            return best;
        }
        Ev E(ShiftVehicle v, string what, string detail) => new Ev { t = Round(v.Holding ? 0 : Time.time - v.StartTime), at = Mathf.Round(v.Progress), segment = SegmentOf(v), what = what, detail = detail };
        static float Round(float x) => Mathf.Round(x * 100) / 100f;
        static string SegmentOf(ShiftVehicle v) { var c = v.Course; if (!c || c.segments.Count == 0) return ""; return c.segments[Mathf.Clamp(v.Splits.Count, 0, c.segments.Count - 1)].name; }

        void OnEvent(ShiftVehicle v, string kind, string text)
        {
            if (!v.Course || !v.Course.timeTrial) return;
            open.TryGetValue(v, out var r);
            switch (kind)
            {
                case "start":
                    if (r != null) Write(v, r, "abandoned");
                    open[v] = Begin(v); break;
                case "reset": if (r != null) { r.resets++; r.resetList.Add(E(v, text.Contains("(") ? text.Substring(text.IndexOf('(') + 1).Split(',')[0].TrimEnd(')') : text, text)); if (text.Contains("during a change")) r.changes.Add(E(v, "change cut short by a reset", text)); } break;
                case "missed": if (r != null) { r.missedGates++; r.gates.Add(E(v, "missed gate", text)); } break;
                case "gate": if (r != null && text.Contains("near miss")) { r.nearMisses++; r.gates.Add(E(v, "near miss (+3 s)", text)); } break;
                case "rejected": if (r != null) r.changes.Add(E(v, "gate in the wrong form", text)); break;
                case "shortcut": if (r != null && text.Contains(": in")) r.shortcuts.Add(E(v, text.Replace("shortcut ", "").Split(':')[0], "in")); else if (r != null && text.Contains("left without")) r.shortcuts.Add(E(v, text.Replace("shortcut ", "").Split(':')[0], "left early")); break;
                case "wings": if (text.Contains("window open")) choicePending[v] = true; break;
                case "finish": if (r != null) StartCoroutine(Finish(v, r)); break;
            }
        }
        Record Begin(ShiftVehicle v)
        {
            var g = game; bool split = SplitScreen.Active; races++;
            return new Record
            {
                session = session, build = Application.version, date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), course = v.Course.courseName, race = races,
                player = v == g.Player ? "P1" : "P2", driver = v.RacerName, mode = (g.StuntShow ? "stunt show" : g.RaceMode ? "race" : "time trial") + (split ? ", 2 players" : "") + (g.RaceMode && Items.OnNow(g) ? ", items" : ""), players = split ? 2 : 1,
                difficulty = g.RaceMode ? Settings.Difficulty.ToString() : "-", design = Settings.Design,
            };
        }
        System.Collections.IEnumerator Finish(ShiftVehicle v, Record r)
        {
            yield return null; yield return null;
            var race = game.Race; bool raced = race && race.Active && race.Racers.Contains(v);
            r.place = raced ? race.Finishers.FindIndex(f => f.v == v) + 1 : 1; r.field = raced ? race.Racers.Count : 1;
            r.lap = Round(v.FinishTime); r.penalty = v.Penalty;
            r.laps = v.LapsTotal; foreach (var t in v.LapTimes) r.lapTimes.Add(Round(t));
            r.stunts = v.StuntScore; r.tricksLanded = v.TricksLanded; r.wipeouts = v.TricksFailed; r.shiftTricks = v.ShiftTricks; r.bestCombo = v.BestCombo;
            r.hitsTaken = v.HitsTaken; r.hitsBlocked = v.HitsBlocked; if (v.Bag) { r.itemsUsed = v.Bag.Used.Count; r.itemUses.AddRange(v.Bag.Used); }
            float prev = 0; var c = v.Course;
            for (int k = 0; k < v.Splits.Count && k < c.segments.Count; k++) { r.segmentNames.Add(c.segments[k].name); r.segmentTimes.Add(Round(v.Splits[k] - prev)); prev = v.Splits[k]; }
            Write(v, r, "finished");
        }
        void Write(ShiftVehicle v, Record r, string result)
        {
            open.Remove(v); r.result = result;
            if (!Settings.PlaytestLogs) return;
            try { Directory.CreateDirectory(Folder); File.AppendAllText(FilePath, JsonUtility.ToJson(r) + "\n"); Written++; Debug.Log($"[Playtest] {r.player} {r.driver} {r.mode}: {result}, lap {r.lap:0.00} s -> {FilePath}"); }
            catch (Exception e) { Debug.LogWarning("[Playtest] could not write " + FilePath + ": " + e.Message); }
        }
        void OnApplicationQuit() { foreach (var kv in new List<KeyValuePair<ShiftVehicle, Record>>(open)) if (kv.Key && !kv.Key.Holding) Write(kv.Key, kv.Value, "abandoned (quit)"); }

        public static void OpenFolder()
        {
            try { Directory.CreateDirectory(Folder); } catch (Exception) { }
            if (QuietRun.Check) { Debug.Log("[Playtest] would open " + Folder + " (check run: not opened)"); return; }
            Application.OpenURL("file:///" + Folder.Replace('\\', '/'));
        }
    }
}
