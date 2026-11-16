using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 11 item 3: story mode skeleton, "The Tour" (Docs/STORY.md). Stops in order, one per course built so far: Stop 1 The Trial
    // (Paradise Plunge, Buck's private trial), Stop 2 Opening Night (Coaster Coast, the tour's public debut). Before a stop a scene (Buck's
    // voice and subtitles over a slow camera flight along the course, skippable with A / Enter), then the race against the three AI
    // (Settings' difficulty), then a short reaction from Buck by the result. Progress by championship points, not by winning: 1st 10,
    // 2nd 6, 3rd 4, 4th 2; a top-3 finish unlocks the next stop (and you can retry any unlocked stop: your best place and points stay).
    // Saved in tour.json beside settings.json (persistentDataPath, "-saveDir", or a check run's folder).
    public static class Tour
    {
        public sealed class Stop { public int course; public string title, key; }
        public static readonly Stop[] Stops = { new Stop { course = 0, title = "The Trial", key = "s1" }, new Stop { course = 1, title = "Opening Night", key = "s2" } };
        public const int QualifyPlace = 3;
        public static int PointsFor(int place) => place == 1 ? 10 : place == 2 ? 6 : place == 3 ? 4 : place == 4 ? 2 : 1;

        [Serializable] sealed class Data { public int version = 1; public int unlocked = 1; public int[] best = new int[0]; public int[] points = new int[0]; public int[] tries = new int[0]; public bool done; }
        static Data d;
        public static string FilePath
        {
            get
            {
                var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-saveDir");
                string dir = i >= 0 && i + 1 < a.Length ? a[i + 1] : QuietRun.Check ? QuietRun.Dir : Application.persistentDataPath;
                return Path.Combine(dir, "tour.json");
            }
        }
        static Data D
        {
            get
            {
                if (d != null) return d;
                try { if (File.Exists(FilePath)) d = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)); } catch (Exception e) { Debug.LogWarning("[Tour] could not read " + FilePath + ": " + e.Message); }
                if (d == null) d = new Data();
                int n = Stops.Length;
                if (d.best == null || d.best.Length < n) Array.Resize(ref d.best, n);
                if (d.points == null || d.points.Length < n) Array.Resize(ref d.points, n);
                if (d.tries == null || d.tries.Length < n) Array.Resize(ref d.tries, n);
                d.unlocked = Mathf.Clamp(d.unlocked, 1, n);
                return d;
            }
        }
        static void Save()
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath + ".tmp", JsonUtility.ToJson(d, true)); if (File.Exists(FilePath)) File.Delete(FilePath); File.Move(FilePath + ".tmp", FilePath); }
            catch (Exception e) { Debug.LogWarning("[Tour] could not save " + FilePath + ": " + e.Message); }
        }
        public static void Reload() => d = null;   // checks: read the file again (a "restart")
        public static int Unlocked => Settings.UnlockAllTour ? Stops.Length : D.unlocked;   // Round 12: "Unlock all Tour stops" (Settings)
        public static int Earned => D.unlocked;   // what the Tour itself has unlocked (top 3 finishes)
        public static bool Done => D.done;
        public static int Best(int stop) => D.best[stop];
        public static int Points(int stop) => D.points[stop];
        public static int Tries(int stop) => D.tries[stop];
        public static int Total => D.points.Sum();

        public static int ActiveStop = -1;      // the stop being played (Race again retries it); -1 = not in The Tour
        public static bool Running;             // a stop's scene / race / reaction is in progress
        public static string Note = "";         // the results screen's Tour line
        public static int LastPlace, LastPoints; public static bool LastPassed, LastUnlocked;

        // a stop's result: the best place and its points kept, the next stop unlocked by a top-3 finish
        public static void Record(int stop, int place)
        {
            var x = D; x.tries[stop]++;
            int pts = PointsFor(place); bool better = x.best[stop] == 0 || place < x.best[stop];
            if (better) { x.best[stop] = place; x.points[stop] = pts; }
            bool pass = place <= QualifyPlace; bool unlocked = false;
            if (pass && stop + 1 < Stops.Length && x.unlocked < stop + 2) { x.unlocked = stop + 2; unlocked = true; }
            if (pass && stop == Stops.Length - 1) x.done = true;
            Save();
            LastPlace = place; LastPoints = pts; LastPassed = pass; LastUnlocked = unlocked;
            Note = $"THE TOUR - Stop {stop + 1} {Stops[stop].title}: {Race.Ordinal(place)}, {pts} points" + (better ? "" : $" (best {Race.Ordinal(x.best[stop])})") +
                   (unlocked ? $"  -  Stop {stop + 2} unlocked!" : pass ? (stop == Stops.Length - 1 ? "  -  Tour complete!" : "") : $"  -  top {QualifyPlace} to go on: Race again to retry");
            Debug.Log($"[Tour] stop {stop + 1}: {Race.Ordinal(place)}, {pts} points, best {Race.Ordinal(x.best[stop])}, unlocked {x.unlocked}, total {Total}, tries {x.tries[stop]} -> {FilePath}");
        }
        public static string Row(int stop)
        {
            var s = Stops[stop]; string course = Game.Instance ? Game.Instance.Courses[s.course].courseName : "";
            if (stop + 1 > Unlocked) return $"Stop {stop + 1}: {s.title} ({course})   - locked: top {QualifyPlace} at Stop {stop} -";
            return $"Stop {stop + 1}: {s.title} ({course})   " + (Best(stop) > 0 ? $"best {Race.Ordinal(Best(stop))}, {Points(stop)} pts" : "not raced yet") + (stop + 1 > Earned ? "   (open: Unlock all Tour stops)" : "");
        }
    }

    // plays a stop: scene -> race -> reaction (Game.PlayTour)
    public sealed class TourRunner : MonoBehaviour
    {
        Game game;
        public static TourRunner Make(Game g) { var t = g.gameObject.AddComponent<TourRunner>(); t.game = g; return t; }
        public readonly List<string> Steps = new List<string>();   // checks: what happened, in order
        void Step(string s) { Steps.Add($"{Time.realtimeSinceStartup:0.00} {s}"); Debug.Log("[Tour] " + s); }

        public void Play(int stop, bool scene)
        {
            StopAllCoroutines(); StartCoroutine(Run(stop, scene));
        }
        IEnumerator Run(int stop, bool scene)
        {
            var st = Tour.Stops[stop]; var buck = Buck.Instance;
            Tour.ActiveStop = stop; Tour.Running = true; Tour.Note = "";
            Lineup.Apply(game);
            bool talk = buck && Buck.Enabled;
            if (scene && talk) buck.Hold();   // Barker waits for the scene
            game.StartTourCourse(st.course);
            if (scene && talk)
            {
                Step($"scene: stop {stop + 1} {st.title}");
                Time.timeScale = 0;
                var ids = buck.Pool($"tour_{st.key}_intro");
                var fly = StartCoroutine(FlyThrough(game.Courses[st.course], ids.Sum(id => Length(id)) + .4f * ids.Count));
                yield return buck.Scene(ids, $"tour stop {stop + 1} scene");
                StopCoroutine(fly); EndFlyThrough();
                Step(buck.Skipped ? "scene skipped" : "scene done");
                if (!game.MenuOpen) Time.timeScale = 1;
                if (Caller.Instance) Caller.Instance.AfterBriefing(game.Player);
            }
            Step("race");
            var p = game.Player;
            while (!(p.Finished && game.Trial.HasResults)) { if (Tour.ActiveStop != stop) { Tour.Running = false; yield break; } yield return null; }
            int place = game.Race.Place(p);
            Tour.Record(stop, place);
            Step($"finished {Race.Ordinal(place)}: {Tour.LastPoints} points, passed {Tour.LastPassed}, unlocked {Tour.Earned} (playable {Tour.Unlocked})");
            if (talk)
            {
                // after Barker's results line, when nobody is talking (as Buck's results remark)
                float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 10f && !(Caller.Instance && Caller.Instance.Lines.Any(l => l.pool == "results" || l.pool == "ppres" || l.pool == "ccres" || l.pool == "podium" || l.pool == "last"))) yield return null;
                float t1 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t1 < 8f && ((Caller.Instance && (Caller.Instance.BarkerTalking || (Caller.Instance.Voice && Caller.Instance.Voice.isPlaying))) || Time.time < Driver.AnySpeakingUntil)) yield return null;
                var retry = buck.Pool($"tour_{st.key}_retry");
                var ids = Tour.LastPassed ? buck.Pool($"tour_{st.key}_pass") : retry.Count > 0 ? new List<string> { retry[(Tour.Tries(stop) - 1) % retry.Count] } : new List<string>();
                if (Tour.LastPassed && stop == Tour.Stops.Length - 1) ids.AddRange(buck.Pool("tour_done"));
                yield return buck.Scene(ids, $"tour stop {stop + 1} {(Tour.LastPassed ? "passed" : "retry")}");
                Step("reaction " + string.Join(" + ", ids));
            }
            Tour.Running = false;
        }
        float Length(string id) { var c = Resources.Load<AudioClip>("Audio/Voice/buck/" + id); return c ? c.length : 4f; }

        // a slow flight along the course, 30 m up, looking ahead (the chase camera paused meanwhile)
        ChaseCam chase; CameraViews views; bool chaseWas, viewsWas;
        IEnumerator FlyThrough(Course c, float seconds)
        {
            var cam = game.Cam; chase = cam.GetComponent<ChaseCam>(); views = cam.GetComponent<CameraViews>();
            chaseWas = chase && chase.enabled; viewsWas = views && views.enabled; if (chase) chase.enabled = false; if (views) views.enabled = false;
            float t0 = Time.realtimeSinceStartup, len = c.Length * .92f;
            while (true)
            {
                float k = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / Mathf.Max(1f, seconds));
                float pr = Mathf.Lerp(30f, len, Mathf.SmoothStep(0, 1, k));
                var at = c.PointAt(pr, out var dir); var ahead = c.PointAt(Mathf.Min(pr + 140f, c.Length), out _);
                var side = Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(dir, Vector3.up).normalized);
                var pos = at + Vector3.up * 30f - Vector3.ProjectOnPlane(dir, Vector3.up).normalized * 40f + side * 12f;
                cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation((ahead - pos).normalized, Vector3.up));
                yield return null;
            }
        }
        void EndFlyThrough() { if (chase) chase.enabled = chaseWas; if (views) views.enabled = viewsWas; }
    }
}
