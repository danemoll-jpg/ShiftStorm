using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 11 checks (built exe, -batchmode -mute):
    //   buck    a race on each course: Buck's briefing -> Barker (start line, the race) -> Buck's results remark after Barker's results line;
    //           frames with two voices at once (Buck, Barker, any driver); the skip (A / Enter); once per session per course; Short; Off
    //   tour    The Tour from a fresh tour.json: stop 1 (scene played in full) and stop 2 (scene skipped): scene -> race -> reaction ->
    //           unlock; tour.json read back as after a restart; a 4th place at stop 1 keeps the best place, the points and the unlock and
    //           plays the retry line; Race again retries the stop without the scene
    //   racers  every driver x machine (4 x 4) starts a race: the driver seated in the picked machine (hands on its grips), its steering
    //           mechanism, the three others as the AI in their own machines and colours; a picture of each; split screen: player 2's own
    //           driver and machine, the AI filled by the two drivers nobody picked
    //   round11 = buck + tour + racers
    public sealed partial class CheckPilot
    {
        IEnumerator RealWait(float s) { float end = Time.realtimeSinceStartup + s; while (Time.realtimeSinceStartup < end) yield return null; }
        IEnumerator RealUntil(Func<bool> done, float timeout) { float end = Time.realtimeSinceStartup + timeout; while (!done() && Time.realtimeSinceStartup < end) yield return null; }

        // voices playing at once, sampled every frame (real time)
        int overlapFrames, voiceFrames, buckOver, barkerDriver, driverDriver; bool sampling; readonly List<string> overlapSeen = new List<string>();
        IEnumerator VoiceSampler()
        {
            sampling = true; overlapFrames = voiceFrames = buckOver = barkerDriver = driverDriver = 0; overlapSeen.Clear();
            while (sampling)
            {
                var b = Buck.Instance; var cl = Caller.Instance;
                bool bk = b && b.Voice && b.Voice.isPlaying, br = cl && cl.Voice && cl.Voice.isPlaying;
                var drv = Driver.All.Where(d => d && d.VoiceSource && d.VoiceSource.isPlaying).ToList();
                int n = (bk ? 1 : 0) + (br ? 1 : 0) + drv.Count;
                if (n > 0) voiceFrames++;
                if (n > 1)
                {
                    overlapFrames++; if (bk) buckOver++; else if (br) barkerDriver++; else driverDriver++;
                    if (overlapSeen.Count < 6) { string w = $"{(bk ? "Buck " + b.Voice.clip.name + " " : "")}{(br ? "Barker " + cl.Voice.clip.name + " " : "")}{string.Join(" ", drv.Select(d => d.Who + " " + d.VoiceSource.clip.name))}"; if (!overlapSeen.Contains(w)) overlapSeen.Add(w); }
                }
                yield return null;
            }
        }

        IEnumerator BuckCheck()
        {
            var b = Buck.Instance; Buck.Enabled = true; b.ForgetBriefings(); Settings.BuckBriefings = 0; Settings.Difficulty = Difficulty.Easy;
            Line($"BUCK: {b.LinesLoaded} lines loaded (Resources/Audio/Voice/buck); briefings per course: Paradise Plunge {b.Pool("brief_pp").Count} + {b.Pool("short_pp").Count} short, Coaster Coast {b.Pool("brief_cc").Count} + {b.Pool("short_cc").Count} short; results remarks {b.Pool("res_pp_first").Count + b.Pool("res_pp_podium").Count + b.Pool("res_pp_back").Count} / {b.Pool("res_cc_first").Count + b.Pool("res_cc_podium").Count + b.Pool("res_cc_back").Count}; anecdotes {b.Pool("yarn").Count}");
            bool allOk = true;
            foreach (int course in new[] { 0, 1 })
            {
                scenario = $"buck-{(course == 0 ? "pp" : "cc")}"; Clear(); smart = true; boost = true;
                b.Said.Clear(); int caller0 = 0; StartCoroutine(VoiceSampler());
                float t0 = Time.realtimeSinceStartup;
                game.StartCourse(course, true);
                bool card = b.CardOn; yield return RealWait(1.2f); if (b.CardOn) Capture("briefing-card");
                yield return RealUntil(() => !b.CardOn, 60f);
                float briefEnd = Time.realtimeSinceStartup;
                var brief = b.Said.Where(s => s.what == "briefing").ToList();
                // Barker after the briefing: his start line
                yield return RealUntil(() => Caller.Instance.Lines.Count > caller0, 8f);
                string firstBarker = Caller.Instance.Lines.Count > 0 ? Caller.Instance.Lines[0].pool : "-";
                yield return Until(() => v.Finished, 240f);   // at normal speed: Barker's timing is game time, the voices real time
                yield return RealUntil(() => b.Said.Any(s => s.what.StartsWith("results")), 30f);
                yield return RealUntil(() => !(b.Voice && b.Voice.isPlaying), 20f);
                sampling = false; yield return null;
                var res = b.Said.FirstOrDefault(s => s.what.StartsWith("results"));
                bool barkerResults = Caller.Instance.Lines.Any(l => l.pool == "results" || l.pool == "ppres" || l.pool == "ccres" || l.pool == "podium" || l.pool == "last");
                bool ok = card && brief.Count > 0 && firstBarker != "-" && res.id != null && barkerResults && buckOver == 0;   // Buck never with anyone (Barker / drivers among themselves: the Round 8 rules, reported)
                allOk &= ok;
                Line($"BUCK {(course == 0 ? "Paradise Plunge" : "Coaster Coast")} {(ok ? "PASS" : "FAIL")}: card at the start {card}; briefing {string.Join(" + ", brief.Select(s => $"{s.id} ({s.len:0.0} s)"))}, the game stopped meanwhile; then Barker's {firstBarker}; race finished {Race.Ordinal(game.Race.Place(v))}; Barker's results line {barkerResults}; Buck's remark {(res.id ?? "none")} ({res.what}); frames with two voices at once: {overlapFrames} of {voiceFrames} with a voice (Buck with anyone {buckOver}, Barker with a driver {barkerDriver}, two drivers {driverDriver}){(overlapSeen.Count > 0 ? "; e.g. " + string.Join(" | ", overlapSeen) : "")}");
                foreach (var e in b.Events.Skip(Math.Max(0, b.Events.Count - 3))) Line("    " + e);
            }
            // the skip: a briefing skipped 1.5 s in with A / Enter (the scripted pad's A)
            scenario = "buck-skip"; Clear(); b.ForgetBriefings(); b.Said.Clear();
            game.StartCourse(0, true); yield return RealWait(1.5f);
            bool talking = b.Voice.isPlaying; float ts = Time.realtimeSinceStartup; input.sConfirm = true;
            yield return RealUntil(() => !b.CardOn, 3f); float took = Time.realtimeSinceStartup - ts;
            yield return RealUntil(() => Caller.Instance.Lines.Count > 0, 6f);
            bool skipOk = talking && !b.CardOn && !b.Voice.isPlaying && took < .5f && Time.timeScale == 1f;
            Line($"BUCK SKIP {(skipOk ? "PASS" : "FAIL")}: talking when A was pressed {talking}; card gone after {took:0.00} s, his voice stopped {!b.Voice.isPlaying}, the game running again {Time.timeScale == 1f}, Barker's start line next {Caller.Instance.Lines.Count > 0}");
            // once per session per course
            game.StartCourse(0, true); yield return RealWait(.3f);
            bool again = b.CardOn;
            Line($"BUCK ONCE {(again ? "FAIL" : "PASS")}: the same course started again in the session: briefing {(again ? "again" : "not repeated")}");
            if (again) { input.sConfirm = true; yield return RealUntil(() => !b.CardOn, 3f); }
            // Short and Off
            Settings.BuckBriefings = 1; b.ForgetBriefings(); b.Said.Clear(); game.StartCourse(1, true); yield return RealUntil(() => !b.CardOn, 30f);
            var sh = b.Said.FirstOrDefault(); bool shortOk = sh.id != null && sh.id.StartsWith("short_cc");
            Settings.BuckBriefings = 2; b.ForgetBriefings(); b.Said.Clear(); game.StartCourse(0, true); yield return RealWait(.4f);
            bool offOk = !b.CardOn && b.Said.Count == 0;
            Line($"BUCK SETTING {(shortOk && offOk ? "PASS" : "FAIL")}: Short -> {sh.id ?? "nothing"} ({sh.len:0.0} s); Off -> no card {offOk}");
            Settings.BuckBriefings = 0; Buck.Enabled = false; Time.timeScale = 1;
            Line($"BUCK {(allOk && skipOk && !again && shortOk && offOk ? "PASS" : "FAIL")}");
        }

        IEnumerator TourCheck()
        {
            try { if (File.Exists(Tour.FilePath)) File.Delete(Tour.FilePath); } catch (Exception) { }
            Tour.Reload(); Buck.Enabled = true; Settings.BuckBriefings = 0; Settings.Difficulty = Difficulty.Easy;
            var run = game.TourRun; bool ok = true;
            Line($"TOUR: fresh {Tour.FilePath}; stops {Tour.Stops.Length}, unlocked {Tour.Unlocked}; points 1st {Tour.PointsFor(1)} / 2nd {Tour.PointsFor(2)} / 3rd {Tour.PointsFor(3)} / 4th {Tour.PointsFor(4)}, top {Tour.QualifyPlace} unlocks the next");
            for (int stop = 0; stop < Tour.Stops.Length; stop++)
            {
                scenario = $"tour-stop{stop + 1}"; Clear(); smart = true; boost = true; run.Steps.Clear(); Buck.Instance.Said.Clear();
                game.PickAndGo(Game.PickFor.Tour, stop);
                yield return RealWait(3f); if (Buck.Instance.CardOn) Capture("scene");
                if (stop == 1) { input.sConfirm = true; }   // stop 2: the scene skipped
                yield return RealUntil(() => run.Steps.Any(s => s.Contains(" race")), 90f);
                Time.timeScale = 3f; yield return Until(() => v.Finished, 300f); Time.timeScale = 1f;
                yield return RealUntil(() => !Tour.Running, 60f);
                Capture("results");
                bool pass = Tour.LastPassed && Tour.Unlocked >= Math.Min(Tour.Stops.Length, stop + 2) && !Tour.Running;
                ok &= pass;
                Line($"TOUR stop {stop + 1} {Tour.Stops[stop].title} {(pass ? "PASS" : "FAIL")}: {string.Join(" -> ", run.Steps.Select(s => s.Substring(s.IndexOf(' ') + 1)))}; unlocked {Tour.Unlocked}, total {Tour.Total} points; results line \"{Tour.Note}\"");
                Line($"    Buck: {string.Join(", ", Buck.Instance.Said.Select(s => $"{s.id} ({s.what})"))}");
            }
            // read back as after a restart
            string file = File.Exists(Tour.FilePath) ? File.ReadAllText(Tour.FilePath).Replace("\n", " ").Replace("  ", " ") : "(missing)";
            int[] best = { Tour.Best(0), Tour.Best(1) }; int total = Tour.Total, unl = Tour.Unlocked;
            Tour.Reload();
            bool reloadOk = Tour.Best(0) == best[0] && Tour.Best(1) == best[1] && Tour.Total == total && Tour.Unlocked == unl && Tour.Done;
            Line($"TOUR RELOAD {(reloadOk ? "PASS" : "FAIL")}: read back best {Race.Ordinal(Tour.Best(0))} / {Race.Ordinal(Tour.Best(1))}, {Tour.Total} points, unlocked {Tour.Unlocked}, done {Tour.Done}; file: {file}");
            // a 4th place at stop 1: retry without losing anything
            scenario = "tour-retry"; Clear(); smart = true; throttleCap = .5f; run.Steps.Clear(); Buck.Instance.Said.Clear();
            int best0 = Tour.Best(0), pts0 = Tour.Points(0), tries0 = Tour.Tries(0);
            game.PickAndGo(Game.PickFor.Tour, 0); yield return RealWait(1f); input.sConfirm = true;
            yield return RealUntil(() => run.Steps.Any(s => s.Contains(" race")), 30f);
            Time.timeScale = 3f; yield return Until(() => v.Finished, 400f); Time.timeScale = 1f;
            yield return RealUntil(() => !Tour.Running, 60f);
            bool retryOk = Tour.LastPlace > Tour.QualifyPlace && Tour.Best(0) == best0 && Tour.Points(0) == pts0 && Tour.Unlocked == unl && Tour.Tries(0) == tries0 + 1 && Buck.Instance.Said.Any(s => s.id.StartsWith("tour_s1_retry"));
            Line($"TOUR RETRY {(retryOk ? "PASS" : "FAIL")}: finished {Race.Ordinal(Tour.LastPlace)} (throttle 50 %): best kept {Race.Ordinal(Tour.Best(0))}, points kept {Tour.Points(0)}, unlocked {Tour.Unlocked}, tries {Tour.Tries(0)}; Buck: {string.Join(", ", Buck.Instance.Said.Select(s => s.id))}; results line \"{Tour.Note}\"");
            Capture("retry-results");
            // Race again: the same stop, no scene
            run.Steps.Clear(); game.Restart(); yield return RealWait(1f);
            bool againOk = Tour.ActiveStop == 0 && Tour.Running && !run.Steps.Any(s => s.Contains("scene")) && !Buck.Instance.CardOn;
            Line($"TOUR RACE AGAIN {(againOk ? "PASS" : "FAIL")}: stop {Tour.ActiveStop + 1} started again, steps {string.Join(" -> ", run.Steps.Select(s => s.Substring(s.IndexOf(' ') + 1)))}");
            Tour.ActiveStop = -1; Tour.Running = false; Buck.Enabled = false; Clear();
            game.StartCourse(0, false); yield return Wait(.2f);
            Line($"TOUR {(ok && reloadOk && retryOk && againOk ? "PASS" : "FAIL")}");
        }

        IEnumerator RacersCheck()
        {
            var machines = Lineup.Machines; int good = 0, n = 0; var bad = new List<string>();
            Line($"RACERS: drivers {string.Join(", ", Lineup.All.Select(r => r.name))}; machines {string.Join(", ", machines.Select(VehicleDesigns.Title))}");
            for (int di = 0; di < 4; di++)
                foreach (var m in machines)
                {
                    scenario = $"racer-{Lineup.Short(di)}-{m}"; Clear(); smart = true;
                    Settings.Driver = di; Settings.Design = m; game.UseDesign(m); Lineup.Apply(game);
                    game.StartCourse(0, true); yield return Wait(.6f);
                    var drv = v.GetComponent<Driver>(); var me = Lineup.All[di];
                    bool seated = drv && drv.Who == me.character && Driver.Instance == drv && drv.Scale > 0;
                    var steer = v.GetComponent<Steering>(); bool mech = steer && steer.Shown != null;
                    var (gl, gr) = drv ? drv.GripError() : (-1f, -1f);
                    bool design = v.Forms.DesignName == m && v.RacerName == me.name;
                    var ai = game.Race.Racers.Where(r => r != v).ToList(); var others = Lineup.All.Where(r => r != me).ToList();
                    bool aiOk = ai.Count == 3 && others.All(o => ai.Any(r => r.RacerName == o.name && r.GetComponent<Driver>()?.Who == o.character && r.Forms.DesignName == Lineup.MachineOf(o) && r.Tint == o.color));
                    yield return Until(() => v.Progress > 120f || v.Resets > 0, 8f);
                    bool drives = v.Progress > 60f && v.Resets == 0;
                    if (di == 0 || m == machines[0] || di == machines.IndexOf(m)) { Capture("grid"); }
                    bool ok = seated && mech && design && aiOk && drives && gl >= 0 && gl < .2f && gr < .2f;
                    n++; if (ok) good++; else bad.Add(scenario);
                    Line($"  {me.name,-26} in {VehicleDesigns.Title(m),-11}: seated {seated} ({(drv ? drv.Who : "-")}, scale {(drv ? drv.Scale : 0):0.00}), mechanism {(mech ? steer.Shown.kind : "none")}, hands {gl * 100:0} / {gr * 100:0} cm off the grips, AI {string.Join(", ", ai.Select(r => $"{r.RacerName} ({VehicleDesigns.Title(r.Forms.DesignName)})"))} {(aiOk ? "ok" : "WRONG")}, drove off {drives}  {(ok ? "PASS" : "FAIL")}");
                }
            Line($"RACERS {(good == n ? "PASS" : "FAIL")}: {good} of {n} driver x machine combinations{(bad.Count > 0 ? ": failed " + string.Join(", ", bad) : "")}");
            // split screen: player 1 Chip in the Hot Rod, player 2 Abi in the Scrambler; the AI = Rae and Vivi in their own machines
            scenario = "racers-split"; Clear(); coast = true; Pads();
            Settings.Driver = 2; Settings.Design = machines.Contains("Hotrod") ? "Hotrod" : machines[0]; game.UseDesign(Settings.Design);
            Settings.Driver2 = 0; Settings.Design2 = machines.Contains("Scrambler") ? "Scrambler" : machines[machines.Count - 1];
            SplitScreen.P1Device = pad1; SplitScreen.P2Device = pad2; SplitScreen.AiCount = 2;
            game.StartSplit(true); yield return Wait(1f);
            var p2 = game.Player2; var aiS = game.ActiveRivals();
            bool splitOk = p2 && p2.RacerName == "Abi" && p2.GetComponent<Driver>()?.Who == "Abigail" && p2.Forms.DesignName == Settings.Design2 && v.RacerName == Lineup.All[2].name && v.Forms.DesignName == Settings.Design
                           && aiS.Count == 2 && aiS.All(r => r.RacerName != "Abi" && r.RacerName != v.RacerName);
            Capture("split");
            Line($"RACERS SPLIT {(splitOk ? "PASS" : "FAIL")}: P1 {v.RacerName} in {VehicleDesigns.Title(v.Forms.DesignName)}, P2 {(p2 ? $"{p2.RacerName} ({p2.GetComponent<Driver>()?.Who}) in {VehicleDesigns.Title(p2.Forms.DesignName)}" : "none")}; AI {string.Join(", ", aiS.Select(r => $"{r.RacerName} in {VehicleDesigns.Title(r.Forms.DesignName)}"))}");
            game.EndSplit(false);
            Settings.Driver = 0; Settings.Design = VehicleDesigns.Default; game.UseDesign(Settings.Design); Lineup.Apply(game); Clear();
            game.StartCourse(0, false); yield return Wait(.2f);
        }
    }
}
