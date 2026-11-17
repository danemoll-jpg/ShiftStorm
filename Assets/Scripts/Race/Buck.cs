using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 11 item 2: Buck "Big Finish" Barlow, the tour's organiser (Docs/STORY.md). Barnaby calls the races; Buck talks before
    // and after them and in The Tour's scenes; they never talk over each other.
    //   Briefing: before a course's first race or time trial of the session (Settings "Buck's briefings": On = a briefing, sometimes an
    //   anecdote after it; Short = one sentence; Off), a card at the bottom of the screen (a placeholder portrait until his model exists,
    //   his name, subtitles) with his voice; the game waits (time stopped) until he is done or A / Enter skips him; then Barnaby's start
    //   line and the countdown.
    //   Results remark: after Barnaby's results line, by the player's place (1st / 2nd-3rd / back), per course; not in a time trial, not
    //   in The Tour (its own reactions).
    // Clips: Resources/Audio/Voice/buck/<id>.ogg + lines.json (Tools/Audio/buck_lines.py, ElevenLabs voice "ShiftStorm Buck").
    // Split screen: one Buck for both players (the card on both halves). While he talks, Barnaby and the drivers wait (Caller.Talking).
    public sealed class Buck : MonoBehaviour
    {
        public static Buck Instance { get; private set; }
        public static bool Enabled = !QuietRun.Check;   // a check run turns him on for its own steps
        public const string Title = "BUCK \"BIG FINISH\" BARLOW";
        AudioSource voice; readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(); readonly Dictionary<string, string> words = new Dictionary<string, string>();
        readonly HashSet<int> briefed = new HashSet<int>(); readonly HashSet<string> used = new HashSet<string>();
        public bool CardOn { get; private set; }
        public string Subtitle { get; private set; } = "";
        public string Hint { get; private set; } = "";
        public bool Busy => CardOn || (voice && voice.isPlaying);
        public AudioSource Voice => voice;
        public int LinesLoaded => clips.Count;
        // every line said this run: (real time, length, id, what for) and the steps of a briefing (checks)
        public readonly List<(float t, float len, string id, string what)> Said = new List<(float, float, string, string)>();
        public readonly List<string> Events = new List<string>();
        bool skip; Game game; int remarkFor = -1;

        public static Buck Make(Game g)
        {
            var b = g.gameObject.AddComponent<Buck>(); b.game = g; Instance = b;
            b.voice = g.gameObject.AddComponent<AudioSource>(); b.voice.spatialBlend = 0; b.voice.playOnAwake = false; b.voice.ignoreListenerPause = true;
            foreach (var c in Resources.LoadAll<AudioClip>("Audio/Voice/buck")) b.clips[c.name] = c;
            var js = Resources.Load<TextAsset>("Audio/Voice/buck/lines");
            if (js) foreach (var l in JsonUtility.FromJson<LineFile>(js.text).lines) b.words[l.id] = l.text;
            Debug.Log($"[Buck] {b.clips.Count} lines loaded, {b.words.Count} subtitles");
            return b;
        }
        [System.Serializable] sealed class LineFile { public Line[] lines; }
        [System.Serializable] sealed class Line { public string id, text; }

        public List<string> Pool(string prefix) => clips.Keys.Where(k => k.StartsWith(prefix + "_")).OrderBy(k => k).ToList();
        string Pick(string prefix)
        {
            var all = Pool(prefix); if (all.Count == 0) return "";
            var fresh = all.Where(k => !used.Contains(k)).ToList(); if (fresh.Count == 0) { foreach (var k in all) used.Remove(k); fresh = all; }
            var id = fresh[Random.Range(0, fresh.Count)]; used.Add(id); return id;
        }
        static string Key(int course) => course == 1 ? "cc" : course == 2 ? "bt" : "pp";

        // ---------------- the briefing ----------------
        public bool WantsBriefing(int course) => Enabled && Settings.BuckBriefings != 2 && !briefed.Contains(course) && clips.Count > 0;
        // before the course starts: from now Barnaby waits (the start line comes after the briefing)
        public void Hold() { CardOn = true; Subtitle = ""; Hint = "A / Enter: skip"; if (Caller.Instance) Caller.Instance.Quiet(); Driver.Hush(); }   // Barnaby and the drivers stop for him
        public void Briefing(int course) { briefed.Add(course); StopAllCoroutines(); StartCoroutine(Brief(course)); }
        public void ForgetBriefings() => briefed.Clear();   // checks: a new "session"
        IEnumerator Brief(int course)
        {
            Hold(); skip = false; Time.timeScale = 0;
            var ids = new List<string>();
            if (Settings.BuckBriefings == 1) ids.Add(Pick("short_" + Key(course)));
            else { ids.Add(Pick("brief_" + Key(course))); if (Random.value < .34f) ids.Add(Pick("yarn")); }
            Events.Add($"{Time.realtimeSinceStartup:0.00} briefing {Key(course)} {Settings.BuckNames[Settings.BuckBriefings]}: {string.Join(" + ", ids)}");
            foreach (var id in ids) { if (skip) break; yield return Say(id, "briefing"); }
            Events.Add($"{Time.realtimeSinceStartup:0.00} briefing {(skip ? "skipped" : "done")}");
            CardOn = false; Subtitle = ""; Hint = "";
            if (!(game && game.MenuOpen)) Time.timeScale = 1;
            if (Caller.Instance && game) Caller.Instance.AfterBriefing(game.Player);
        }
        public void Skip() { if (!CardOn && !(voice && voice.isPlaying)) return; skip = true; if (voice) voice.Stop(); Events.Add($"{Time.realtimeSinceStartup:0.00} skip pressed"); }

        // one line: his voice and the subtitle; waits on real time (the game is stopped under a briefing or a scene)
        public IEnumerator Say(string id, string what)
        {
            if (string.IsNullOrEmpty(id) || !clips.TryGetValue(id, out var clip)) yield break;
            words.TryGetValue(id, out var text); Subtitle = text ?? "";
            voice.Stop(); voice.clip = clip; voice.volume = Settings.Voices; voice.Play();
            Said.Add((Time.realtimeSinceStartup, clip.length, id, what)); SoundLog.Add("voice", "Buck " + id);
            Debug.Log($"[Buck] {what}: {id} \"{text}\" ({clip.length:0.0} s)");
            float end = Time.realtimeSinceStartup + clip.length + .1f;
            while (Time.realtimeSinceStartup < end && !skip) yield return null;
            if (!skip) { float gap = Time.realtimeSinceStartup + .35f; while (Time.realtimeSinceStartup < gap && !skip) yield return null; }
        }

        // ---------------- The Tour's scenes (Tour.cs): several lines in order, skippable as a whole ----------------
        public IEnumerator Scene(IList<string> ids, string what)
        {
            Hold(); skip = false; Events.Add($"{Time.realtimeSinceStartup:0.00} scene {what}: {string.Join(" + ", ids)}");
            foreach (var id in ids) { if (skip) break; yield return Say(id, what); }
            Events.Add($"{Time.realtimeSinceStartup:0.00} scene {what} {(skip ? "skipped" : "done")}");
            CardOn = false; Subtitle = ""; Hint = "";
        }
        public bool Skipped => skip;

        // ---------------- the results remark ----------------
        public void Watch(ShiftVehicle v) { v.Event += (kind, text) => { if (kind == "finish" && v == game.Player) StartCoroutine(Remark(v)); if (kind == "start") remarkFor = -1; }; }
        IEnumerator Remark(ShiftVehicle v)
        {
            if (!Enabled || Settings.BuckBriefings == 2 || Tour.Running || clips.Count == 0) yield break;
            var race = game.Race; if (!(race && race.Active && race.Racers.Contains(v))) yield break;   // races only (a time trial has no place)
            int run = ++remarkFor;
            float t0 = Time.realtimeSinceStartup;
            // after Barnaby's results line (or 10 s), when nobody is talking
            while (Time.realtimeSinceStartup - t0 < 10f && !(Caller.Instance && Caller.Instance.Lines.Any(l => l.pool == "results" || l.pool == "ppres" || l.pool == "ccres" || l.pool == "podium" || l.pool == "last"))) yield return null;
            float t1 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t1 < 8f && ((Caller.Instance && (Caller.Instance.BarkerTalking || (Caller.Instance.Voice && Caller.Instance.Voice.isPlaying))) || Time.time < Driver.AnySpeakingUntil)) yield return null;
            if (run != remarkFor || !v.Finished) yield break;
            int place = race.Place(v), field = race.Racers.Count;
            string by = place == 1 ? "first" : place <= 3 && place < field ? "podium" : "back";
            yield return Say(Pick($"res_{Key(game.CourseIndex)}_{by}"), $"results ({Race.Ordinal(place)})");
        }

        // ---------------- the portrait placeholder (until Dan's model): a showman's silhouette, mustache and scarf ----------------
        static Texture2D portrait;
        public static Texture2D Portrait
        {
            get
            {
                if (portrait) return portrait;
                const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, false); var px = new Color32[N * N];
                Color bg = new Color(.98f, .78f, .3f), sil = new Color(.22f, .14f, .1f), stache = new Color(.96f, .94f, .9f), scarf = new Color(.9f, .15f, .2f), hat = new Color(.12f, .1f, .14f);
                bool E(float x, float y, float cx, float cy, float rx, float ry) { float dx = (x - cx) / rx, dy = (y - cy) / ry; return dx * dx + dy * dy <= 1; }
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N; Color c = E(u, v, .5f, .5f, .5f, .5f) ? bg : new Color(0, 0, 0, 0);
                    if (E(u, v, .5f, .05f, .46f, .3f)) c = sil;                        // barrel chest
                    if (E(u, v, .5f, .5f, .2f, .23f)) c = sil;                         // head
                    if (v > .66f && v < .72f && u > .24f && u < .76f) c = hat;          // hat brim
                    if (v >= .72f && v < .86f && u > .34f && u < .66f) c = hat;         // hat crown
                    if (E(u, v, .41f, .44f, .12f, .045f) || E(u, v, .59f, .44f, .12f, .045f)) c = stache;   // the impressive mustache
                    if (E(u, v, .29f, .47f, .035f, .05f) || E(u, v, .71f, .47f, .035f, .05f)) c = stache;   // its curled ends
                    if (v > .24f && v < .31f && u > .3f && u < .7f) c = scarf;          // bright scarf
                    if (E(u, v, .62f, .17f, .07f, .12f)) c = scarf;                     // its loose end
                    px[y * N + x] = c;
                }
                t.SetPixels32(px); t.Apply(); t.wrapMode = TextureWrapMode.Clamp; portrait = t; return t;
            }
        }
    }
}
