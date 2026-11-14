using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 7 items 5 / 5b: one race (Normal unless -difficulty) with the check pilot driving Abi, everything muted: which caller lines
    // played when (and why others were skipped), no clip again within its pool's cooldown, which effects played how often, the engine
    // per form (its pitch and its own volume, sampled), the music's mix per form (volume, low-pass), the volumes the Settings set.
    public sealed partial class CheckPilot
    {
        IEnumerator CallerCheck()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-difficulty") < 0) Settings.Difficulty = Difficulty.Normal;
            var cl = Caller.Instance; var gs = GameSounds.Instance; var au = CourseAudio.Instance;
            Line($"CALLER: {Caller.Name}, {cl.Clips} clips; effects: {gs.Played.Count} kinds played so far, missing files: {(gs.Missing.Count > 0 ? string.Join(", ", gs.Missing) : "none")}");
            int said0 = cl.Said.Count, sk0 = cl.Skipped.Count; var played0 = new Dictionary<string, int>(gs.Played);
            scenario = "caller"; Clear(); smart = true; game.StartCourse(0, true);
            var vs = v.GetComponent<VehicleSound>(); var perForm = new Dictionary<Mode, List<(float pitch, float vol, float music, float lp)>>();
            yield return Until(() => !v.Holding, 6f);
            float t0 = Time.time; bool wings = false;
            while (!(v.Finished && game.Race.Finishers.Count >= 2) && Time.time - t0 < 260f)
            {
                if (!wings && v.WingsOpen && v.GroundedWheels == 0) { wings = true; input.sWings = true; }   // fly the choice jump: the wings call
                if (vs && Time.frameCount % 20 == 0) { if (!perForm.TryGetValue(v.Mode, out var l)) perForm[v.Mode] = l = new List<(float, float, float, float)>(); l.Add((vs.Pitch, vs.EngineVolume, au ? au.MusicVolume : 0, au ? au.MusicCutoff : 0)); }
                yield return null;
            }
            yield return Until(() => game.Trial.HasResults, 6f); yield return Wait(4f);
            Shot(0, "results-with-caller"); yield return Wait(.2f);
            var said = cl.Said.Skip(said0).ToList();
            Line($"CALLER race: {(v.Finished ? $"Abi finished {Race.Ordinal(game.Race.Place(v))} in {v.FinishTime:0.0} s" : "not finished")}; {said.Count} lines:");
            foreach (var s in said) Line("  " + s + $"  \"{Sub(s)}\"");
            // repeats inside a pool's cooldown
            int repeats = 0; var lastBy = new Dictionary<string, float>();
            foreach (var s in said)
            {
                var parts = s.Split(' '); float t = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture); string clip = parts[2];
                if (lastBy.TryGetValue(clip, out var prev) && t - prev < 20f) repeats++; lastBy[clip] = t;
            }
            var pools = said.Select(s => s.Split(' ')[1]).Distinct().ToList();
            var skips = cl.Skipped.Skip(sk0).GroupBy(s => s.Substring(s.IndexOf(' ') + 1)).Select(g => $"{g.Key} x{g.Count()}");
            Line($"CALLER {(repeats == 0 && said.Count >= 6 ? "PASS" : "FAIL")}: {said.Count} lines from {pools.Count} pools ({string.Join(", ", pools)}), {repeats} clips again within 20 s; skipped: {string.Join("; ", skips)}");
            var fx = gs.Played.Select(kv => (kv.Key, kv.Value - (played0.TryGetValue(kv.Key, out var x) ? x : 0))).Where(k => k.Item2 > 0).Select(k => $"{k.Key} {k.Item2}");
            Line($"SOUNDS played in the race: {string.Join(", ", fx)}");
            foreach (var kv in perForm.OrderBy(k => k.Key))
            {
                var l = kv.Value; Line($"  {Game.FormName(kv.Key),-5} engine pitch {l.Min(x => x.pitch):0.00}-{l.Max(x => x.pitch):0.00}, engine volume {l.Average(x => x.vol):0.00}; music volume {l.Average(x => x.music):0.00}, low-pass {l.Average(x => x.lp):0} Hz ({l.Count} samples)");
            }
            Line($"  volumes from Settings: music {Settings.Music:0.0}, effects {Settings.Effects:0.0}, voices {Settings.Voices:0.0}; music clip {(au ? au.MusicClip : "-")}");
            Save();
        }
        string Sub(string said)
        {
            var clip = said.Split(' ')[2]; var ta = Resources.Load<TextAsset>("Audio/Voice/barker/lines");
            if (!ta) return ""; int i = ta.text.IndexOf($"\"id\": \"{clip}\""); if (i < 0) return "";
            int t = ta.text.IndexOf("\"text\": \"", i) + 9; return ta.text.Substring(t, ta.text.IndexOf('"', t) - t);
        }
    }
}
