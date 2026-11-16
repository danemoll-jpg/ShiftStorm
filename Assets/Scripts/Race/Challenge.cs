using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 13 item 4: rival challenges in The Tour (Docs/STORY.md: "optional rival challenges and practice events reuse the tracks to develop
    // relationships"). After a Tour stop is cleared (top 3; or while "Unlock all Tour stops" is On) a short one-on-one challenge on that track
    // opens on The Tour page: you and one rival start side by side from an anchor part-way round the lap, after Buck's word and the rival's
    // taunt and a 3-2-1; the first to the target gate wins. A win gives a paint colour for the Garage (Paints, settings.json "paint") the
    // first time; win or lose, the rival says something about it. Saved in tour.json (won, best margin, tries).
    public sealed class Challenge
    {
        public int stop, course, gates; public string key, title, rival, rivalShort, blurb, paint; public Func<float> startAt;
        public static readonly Challenge[] All =
        {
            new Challenge { stop = 0, course = 0, key = "c1", title = "Rae's Waterfall Dash", rival = "Rocket Rae", rivalShort = "Rae", paint = "Rocket Red",
                            blurb = "beat Rae from the Coral Vault up the waterfall to the landing", gates = 10, startAt = () => ParadisePlunge.Marks["hi2"] - 25 },
            new Challenge { stop = 1, course = 1, key = "c2", title = "Chip's Coaster Showdown", rival = "Chip \"Showtime\" Sterling", rivalShort = "Chip", paint = "Showtime Blue",
                            blurb = "stay ahead of Chip over both coasters to the top hat", gates = 4, startAt = () => CoasterCoast.Marks["G1"] + 5 },
        };
        public static Challenge Active;   // the challenge being played
        public static int Last = -1;      // the last one started (Restart replays it)
        public static bool Open(int i) => Settings.UnlockAllTour || (Tour.Best(All[i].stop) > 0 && Tour.Best(All[i].stop) <= Tour.QualifyPlace);
        public static string Row(int i)
        {
            var ch = All[i]; string course = Game.Instance ? Game.Instance.Courses[ch.course].courseName : "";
            if (!Open(i)) return $"Challenge: {ch.title} ({course})   - locked: top {Tour.QualifyPlace} at Stop {ch.stop + 1} -";
            string st = Tour.ChallengeWon(i) ? $"WON (best by {Tour.ChallengeBest(i):0.0} s), {ch.paint} paint" : Tour.ChallengeTries(i) > 0 ? $"not won yet ({Tour.ChallengeTries(i)} tries)" : $"win: {ch.paint} paint";
            bool earned = Tour.Best(ch.stop) > 0 && Tour.Best(ch.stop) <= Tour.QualifyPlace;
            return $"Challenge: {ch.title} ({course}): {ch.blurb}   {st}{(earned ? "" : "   (open: Unlock all Tour stops)")}";
        }
        public static string Note = "";   // the HUD's result line
    }

    // the Garage's paint colours: the machine's own colours, and one per challenge won
    public static class Paints
    {
        public static readonly (string name, Color color, int challenge)[] All =
        {
            ("Factory", VehicleDesigns.PlayerTint, -1), ("Rocket Red", new Color(.85f, .12f, .15f), 0), ("Showtime Blue", new Color(.15f, .4f, .95f), 1),
        };
        public static bool Unlocked(int i) => All[i].challenge < 0 || Tour.ChallengeWon(All[i].challenge);
        public static int Current => Unlocked(Mathf.Clamp(Settings.Paint, 0, All.Length - 1)) ? Mathf.Clamp(Settings.Paint, 0, All.Length - 1) : 0;
        public static Color Color => All[Current].color;
        public static int Next() { int i = Current; for (int k = 0; k < All.Length; k++) { i = (i + 1) % All.Length; if (Unlocked(i)) return i; } return 0; }
        public static string Row { get { int won = All.Count(p => p.challenge >= 0 && Tour.ChallengeWon(p.challenge)), n = All.Count(p => p.challenge >= 0); return $"Paint: {All[Current].name}   ({won} of {n} challenge paints won{(won < n ? "; win rival challenges in The Tour" : "")}, A: next)"; } }
    }

    // plays a challenge: Buck's word, the rival's taunt, 3-2-1, the run, the result, back to The Tour page
    public sealed class ChallengeRunner : MonoBehaviour
    {
        Game game;
        public static ChallengeRunner Make(Game g) { var t = g.gameObject.AddComponent<ChallengeRunner>(); t.game = g; return t; }
        public readonly List<string> Steps = new List<string>();   // checks: what happened, in order
        public bool Running { get; private set; }
        public bool LastWon { get; private set; }
        public float LastMargin { get; private set; }
        void Step(string s) { Steps.Add($"{Time.realtimeSinceStartup:0.00} {s}"); Debug.Log("[Challenge] " + s); }

        public bool Frozen { get; private set; }   // the result is in: both cars held where they were
        public void Play(int i) { StopAllCoroutines(); Frozen = false; Challenge.Last = i; StartCoroutine(Run(i)); }
        public void Abort() { StopAllCoroutines(); Running = false; Frozen = false; Challenge.Active = null; }

        IEnumerator Run(int i)
        {
            var ch = Challenge.All[i]; Running = true; Challenge.Note = "";
            Tour.ActiveStop = -1; Lineup.Apply(game);
            var rival = game.Rivals.Find(r => r.RacerName == ch.rival);
            if (!rival) { Challenge.Note = $"{ch.rivalShort} is your own driver: pick another driver for {ch.title}"; Step("no rival: " + Challenge.Note); Running = false; game.ShowTourPage(); yield break; }
            Challenge.Active = ch;
            var c = game.Courses[ch.course]; float at = ch.startAt(); int ai = 0; for (int k = 0; k < c.anchors.Count; k++) if (c.anchors[k].progress <= at + .5f) ai = k;
            var buck = Buck.Instance; bool talk = buck && Buck.Enabled;
            if (talk) buck.Hold();
            game.StartChallenge(ch.course, c.anchors[ai], rival);
            var p = game.Player; float hold = 60f; p.HoldFor(hold); rival.HoldFor(hold);
            Step($"start: {ch.title}, {p.RacerName} against {rival.RacerName} from the anchor at {c.anchors[ai].progress:0} m ({c.anchors[ai].mode}), first through gate {ch.gates} ({c.gates[ch.gates - 1].label})");
            if (talk) yield return buck.Scene(buck.Pool($"ch_{ch.key}_intro"), $"challenge {ch.key}");
            { var d = Driver.All.Find(x => x && x.Vehicle == rival); float t0 = Time.realtimeSinceStartup;
              if (d && d.Say("chintro", true)) { Step($"{ch.rivalShort}: chintro"); while (Time.time < Driver.AnySpeakingUntil && Time.realtimeSinceStartup - t0 < 8f) yield return null; } }
            if (Challenge.Active != ch) { Running = false; yield break; }
            p.HoldFor(3f); rival.HoldFor(3f);
            if (Caller.Instance) Caller.Instance.Call("chstart", p, true);
            Step("go in 3 s");
            float pAt = -1, rAt = -1, t1 = Time.time;
            while (Challenge.Active == ch && Time.time - t1 < 240f)
            {
                if (pAt < 0 && p.GatesPassed >= ch.gates) pAt = Time.time;
                if (rAt < 0 && rival.GatesPassed >= ch.gates) rAt = Time.time;
                if (pAt > 0 || rAt > 0) break;
                yield return null;
            }
            if (Challenge.Active != ch) { Running = false; yield break; }
            bool won = pAt > 0 && (rAt < 0 || pAt <= rAt);
            // the margin: the other one's time to the gate (at most 6 s more is waited for), or the gap in race distance at that moment
            var other = won ? rival : p; (won ? p : rival).HoldFor(9999f, false); float t2 = Time.time;   // the winner stops at the gate
            float gap = Mathf.Max(0, (won ? p : rival).Progress - other.Progress);
            while (Time.time - t2 < 6f && other.GatesPassed < ch.gates) yield return null;
            float margin = other.GatesPassed >= ch.gates ? Time.time - t2 : gap / Mathf.Max(20f, other.Speed > 5f ? other.Speed : 30f);
            p.HoldFor(9999f, false); rival.HoldFor(9999f, false); Frozen = true;
            bool first = won && !Tour.ChallengeWon(i);
            Tour.RecordChallenge(i, won, margin);
            LastWon = won; LastMargin = margin;
            Challenge.Note = won ? $"CHALLENGE WON: {ch.title}, by {margin:0.0} s{(first ? $"  -  new paint: {ch.paint} (Garage)" : "")}" : $"CHALLENGE LOST: {ch.title}, {ch.rivalShort} by {margin:0.0} s  -  try again from The Tour page";
            Step(Challenge.Note);
            float t3 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t3 < 5f && Caller.Instance && (Caller.Instance.BarkerTalking || Time.time < Driver.AnySpeakingUntil)) yield return null;   // his result line never under another
            if (Caller.Instance) Caller.Instance.Call(won ? "chwon" : "chlost", p, true);
            t3 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t3 < 6f && Caller.Instance && (Caller.Instance.BarkerTalking || Caller.Instance.Voice && Caller.Instance.Voice.isPlaying)) yield return null;
            var rd = Driver.All.Find(x => x && x.Vehicle == rival);
            if (rd && rd.Say(won ? "chlose" : "chwin", true)) { Step($"{ch.rivalShort}: {(won ? "chlose" : "chwin")}"); float t4 = Time.realtimeSinceStartup; while (Time.time < Driver.AnySpeakingUntil && Time.realtimeSinceStartup - t4 < 8f) yield return null; }
            if (talk && first) { yield return buck.Scene(buck.Pool("ch_reward"), "challenge reward"); Step("Buck: reward"); }
            float t5 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t5 < 2.5f) yield return null;
            Running = false; Challenge.Active = null;
            if (!game.MenuOpen) game.ShowTourPage();
        }
    }
}
