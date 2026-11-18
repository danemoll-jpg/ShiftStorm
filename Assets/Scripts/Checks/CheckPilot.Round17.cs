using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 17 checks (built exe, -batchmode -mute, no window).
    //   items17   every item used in every form on Paradise Plunge (car on the promenade, plane over the valley, boat on the lagoon, sub in
    //             the cave) with a rival placed ahead / behind: hit, blocked or missed, pictures at the use and after, resets caused (none
    //             allowed); each course's own item on its own course
    //   stunts17  each form's tricks: the car off the Creek Jump (spin, front flip, a late flip = a wipeout), the plane (barrel roll, loop),
    //             the boat (a wave hop + spin), the sub (a breach + corkscrew), a SHIFT TRICK off the Lake Jump (a trick, then the wings);
    //             points, landings, pictures mid-trick; the human-like pilot's tricks off the Creek Jump (-tries N)
    //   laps17    a 3-lap race (-raceLaps 3 is set by the check) on Paradise Plunge, Coaster Coast and Big Top (the player's racer AI-driven):
    //             laps counted, lap times, the HUD's lap text and flashes, the results' lap lines; Dam Shame stays 3 laps
    //   show17    a Stunt Show race on Paradise Plunge (3 laps, items on, the player's racer AI-driven with AI items): the winner on points
    //   aisoak17  AI races with items on every course (-races N per course, default 3; 4 AI incl. the player's racer, AI items): finishes, stuck,
    //             resets within 4 s after an item hit (must be 0), items used by kind; the item odds by place over every race (-oddsRaces)
    //   pp17      Paradise Plunge's new lap: the check pilot's lap time, each big split timed both ways by the AI (Hard, no boost), pictures
    //             along both ways of each split
    //   round17 = items17 + stunts17 + laps17 + show17 + pp17 (aisoak17 runs on its own: long)
    public sealed partial class CheckPilot
    {
        // a new AI driver for the player's racer (Destroy is deferred: GetComponent would hand back the one just stopped)
        RivalPilot FreshAi() { foreach (var old in v.GetComponents<RivalPilot>()) DestroyImmediate(old); return v.gameObject.AddComponent<RivalPilot>(); }

        IEnumerator Round17(string which)
        {
            bool all = which == "round17";
            if (all || which == "items17") yield return Items17();
            if (all || which == "stunts17") yield return Stunts17();
            if (all || which == "laps17") yield return Laps17();
            if (all || which == "show17") yield return Show17();
            if (all || which == "pp17") yield return PP17();
            if (which == "aisoak17") yield return AiSoak17();
            if (which == "diag17") yield return Diag17();
            Save();
        }

        static readonly (Mode m, string at, float off)[] FormSpots = { (Mode.Drive, "promMid", 0), (Mode.Fly, "lip", 160), (Mode.Boat, "splash", 160), (Mode.Dive, "portal", 60) };
        // a race on a course, the player's racer put at a progress in a form (at speed), one rival a little ahead of it, the others parked far back
        IEnumerator ItemScene(int course, Mode m, float p, ShiftVehicle[] rivals, float ahead)
        {
            Clear(); aiDrives = false; StopAi(); game.StartCourse(course, true);
            yield return Until(() => !v.Holding, 8f);
            var c = v.Course; Quaternion r; var q = LineAt(c, p, out r);
            v.Begin(c, AnchorBefore(c, p - 5)); foreach (var rv in rivals) rv.Begin(c, AnchorBefore(c, p - 5));   // the gates behind count (no "missed gate" reset)
            if (m == Mode.Dive || m == Mode.Fly) q = c.PointAt(p, out _);
            v.DebugPlace(q, r, m, m == Mode.Drive ? 45 : m == Mode.Boat ? 40 : m == Mode.Fly ? 80 : 30, p);
            var front = rivals[0]; var fq = m == Mode.Dive || m == Mode.Fly ? c.PointAt(p + ahead, out _) : LineAt(c, p + ahead, out _);
            front.DebugPlace(fq, r, m, m == Mode.Drive ? 40 : m == Mode.Boat ? 35 : m == Mode.Fly ? 75 : 25, p + ahead);
            for (int k = 1; k < rivals.Length; k++) { var bq = m == Mode.Dive || m == Mode.Fly ? c.PointAt(p - 60 - 20 * k, out _) : LineAt(c, p - 60 - 20 * k, out _); rivals[k].DebugPlace(bq, r, m, 20, p - 60 - 20 * k); }
            smart = true; holdSpeed = m == Mode.Dive ? 30 : 45;
            yield return Wait(.4f);
        }

        IEnumerator Items17()
        {
            var c = game.Courses[0]; int uses = 0, hits = 0, blocked = 0, missed = 0, resetsCaused = 0, worked = 0;
            Line("ITEMS (Paradise Plunge, every item in every form; the player's racer uses it with Rae 45 m ahead; 6 s each):");
            foreach (var spot in FormSpots)
            {
                foreach (var k in Items.All)
                {
                    if (k == ItemKind.FireworkRocket || k == ItemKind.HumanCannonball || k == ItemKind.FloodWave) continue;   // the other courses' own items: below
                    scenario = $"item-{spot.m}-{k}"; var rv = game.ActiveRivals().ToArray();
                    float p = Mk(spot.at) + spot.off;
                    yield return ItemScene(0, spot.m, p, rv, k == ItemKind.SwapSpot ? 35f : 45f);
                    var target = rv[0]; int tr0 = target.Resets, r0 = v.Resets, th0 = target.HitsTaken, tb0 = target.HitsBlocked; int log0 = Items.Log.Count;
                    if (k == ItemKind.Rewind) yield return Wait(3.3f);
                    int u0 = v.Bag.Used.Count; v.Bag.Set(k); input.sItem = true; yield return Wait(.25f);
                    bool used = v.Bag.Used.Count > u0; if (used) uses++;
                    Capture("use"); yield return Wait(.6f); Capture("after-0.6s"); yield return Wait(1.4f); Capture("after-2s");
                    yield return Wait(4f);
                    string res = target.HitsTaken > th0 ? "HIT" : target.HitsBlocked > tb0 ? "BLOCKED" : "-";
                    if (res == "HIT") hits++; else if (res == "BLOCKED") blocked++;
                    int caused = (target.Resets - tr0) + (v.Resets - r0); resetsCaused += caused;
                    var lines = Items.Log.Skip(log0).ToList();
                    bool ok = used && caused == 0;
                    if (Items.Attack(k) && k != ItemKind.SwapSpot && res == "-") missed++;
                    if (ok) worked++;
                    Line($"  {spot.m,-5} {Items.Name(k),-17} {(ok ? "OK  " : "FAIL")} used {used}{(used ? "" : $" ({v.Bag.Status})")}, on Rae: {res}, resets {caused}{(caused > 0 ? $" (Rae: {target.LastResetReason}; player: {v.LastResetReason})" : "")}; {string.Join(" | ", lines.Select(l => l.Substring(l.IndexOf(' ') + 1)))}");
                    Save();
                }
            }
            // each course's own item, there, as a car near the start
            foreach (var (ci, k) in new[] { (0, ItemKind.DinoEgg), (1, ItemKind.FireworkRocket), (2, ItemKind.HumanCannonball), (3, ItemKind.FloodWave) })
            {
                scenario = $"item-course{ci}-{k}"; var rv = game.ActiveRivals().ToArray();
                Clear(); game.StartCourse(ci, true); yield return Until(() => !v.Holding, 8f);
                var cc = v.Course; float p = cc.anchors.Count > 2 ? cc.anchors[2].progress + 30 : 300;
                yield return ItemScene(ci, Mode.Drive, p, rv, 50f);
                var target = rv[0]; int th0 = target.HitsTaken, r0 = target.Resets + v.Resets;
                v.Bag.Set(k); input.sItem = true; yield return Wait(.5f); Capture("use"); yield return Wait(1f); Capture("after-1.5s"); yield return Wait(4f);
                bool hit = target.HitsTaken > th0; int caused = target.Resets + v.Resets - r0; resetsCaused += caused;
                Line($"  {cc.courseName}'s own item {Items.Name(k)} (themed for '{cc.look}': {Items.Themed(cc)}): {(hit ? "HIT" : "not hit")}, resets {caused}");
            }
            Line($"ITEMS {(resetsCaused == 0 && worked >= uses * .9f ? "PASS" : "CHECK")}: {uses} used, {hits} hits, {blocked} blocked, {missed} attacks missed, {resetsCaused} resets caused (must be 0)");
            // the grace: a second hit straight after the first does nothing
            scenario = "item-grace"; var rr = game.ActiveRivals().ToArray(); yield return ItemScene(0, Mode.Drive, Mk("promMid"), rr, 45f);
            int g0 = rr[0].HitsTaken; rr[0].Hit(ItemKind.Slick, v); bool second = rr[0].Hit(ItemKind.Slick, v); yield return Wait(2f); bool third = rr[0].Hit(ItemKind.Slick, v); yield return Wait(1.5f); bool fourth = rr[0].Hit(ItemKind.Slick, v);
            Line($"ITEM GRACE {(!second && !third && fourth ? "PASS" : "FAIL")}: a second hit at once took effect {second}; 2 s later (in the grace: the 1 s spin + 2 s) {third}; 3.5 s later (after it) {fourth}; hits taken {rr[0].HitsTaken - g0}");
            Clear(); StopAi();
        }

        IEnumerator Stunts17()
        {
            var c = game.Courses[0]; var rs = new List<string>(); int cleanCar = 0;
            // 1 the car off the Creek Jump: spin, front flip, back flip (clean), then a flip pressed late (a wipeout)
            foreach (var (name, stick, late) in new[] { ("spin", new Vector2(1, 0), false), ("front flip", new Vector2(0, 1), false), ("back flip", new Vector2(0, -1), false), ("late flip", new Vector2(0, 1), true) })
            {
                scenario = $"trick-car-{name.Replace(' ', '-')}"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                float lip = ParadisePlunge.Jumps[0].lip; PlaceBefore(lip - 120, 55, 0, 0); smart = true; holdSpeed = 62;
                int s0 = v.StuntScore, f0 = v.TricksFailed; float t0 = Time.time; bool pressed = false, shot = false;
                while (Time.time - t0 < 8f && v.Progress < lip + 140)
                {
                    if (!pressed && v.TrickAir && (!late || v.Body.linearVelocity.y < -6f)) { input.sTrick = stick; pressed = true; }
                    else if (pressed) input.sTrick = Vector2.zero;
                    if (pressed && !shot && v.TrickBusy && Time.time > 0) { yield return Wait(.25f); Capture("mid-trick"); shot = true; }
                    yield return null;
                }
                input.sTrick = Vector2.zero; yield return Wait(.5f); Capture("landed");
                bool clean = v.StuntScore > s0; bool wiped = v.TricksFailed > f0; if (clean && !late) cleanCar++;
                rs.Add($"car {name}: {(late ? (wiped ? "WIPEOUT as meant" : "NO WIPEOUT") : clean ? $"landed clean +{v.StuntScore - s0}" : "NOT LANDED")} ({string.Join("; ", v.TrickLog)}), resets {v.Resets}");
            }
            // 2 the plane: a barrel roll and a loop over the valley (the main flight)
            foreach (var (name, stick) in new[] { ("barrel roll", new Vector2(-1, 0)), ("loop", new Vector2(0, -1)) })
            {
                scenario = $"trick-plane-{name.Replace(' ', '-')}"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                float p = Mk("lip") + 200; var q = c.PointAt(p, out var d); v.Begin(c, AnchorBefore(c, p - 5)); v.DebugPlace(q, Quaternion.LookRotation(d), Mode.Fly, 85, p); smart = true; yield return Wait(.6f);
                int s0 = v.StuntScore; input.sTrick = stick; yield return Wait(.12f); input.sTrick = Vector2.zero; yield return Wait(.3f); Capture("mid-trick"); yield return Wait(2.6f); Capture("after");
                rs.Add($"plane {name}: {(v.StuntScore > s0 ? $"banked +{v.StuntScore - s0}" : "NOT BANKED")} ({string.Join("; ", v.TrickLog)})");
            }
            // 3 the boat: a wave hop (stick back) and a spin in the air, on the lagoon
            {
                scenario = "trick-boat-hop"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                float p = Mk("splash") + 140; var q = LineAt(c, p, out var r); q.y = .8f; v.Begin(c, AnchorBefore(c, p - 5)); v.DebugPlace(q, r, Mode.Boat, 45, p); smart = true; yield return Wait(1.5f);
                int s0 = v.StuntScore, h0 = v.Hops; yOverride = null; float t0 = Time.time; bool spun = false;
                while (Time.time - t0 < 5f) { input.sRawStick = false; if (v.Hops == h0) input.sVertical = 1; if (v.TrickAir && !spun) { input.sTrick = new Vector2(1, 0); spun = true; Capture("hop"); } else input.sTrick = Vector2.zero; yield return null; }
                input.sVertical = 0; Capture("after");
                rs.Add($"boat hop + spin: hops {v.Hops - h0}, {(v.StuntScore > s0 ? $"landed +{v.StuntScore - s0}" : "no points")} ({string.Join("; ", v.TrickLog)})");
            }
            // 4 the sub: a breach (open water above: the lagoon) and a corkscrew out of the water
            {
                scenario = "trick-sub-breach"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                float p = Mk("splash") + 160; var q = LineAt(c, p, out var r); q.y = -2.5f; v.Begin(c, AnchorBefore(c, p - 5)); v.DebugPlace(q, r, Mode.Dive, 40, p); yield return Wait(.3f);
                int s0 = v.StuntScore, b0 = v.Breaches; float t0 = Time.time; bool tricked = false; custom = () => { input.sThrottle = 1; input.sBoost = true; input.sSteer = 0; input.sRawStick = false; input.sVertical = v.Breaches == b0 ? 1 : 0; };
                while (Time.time - t0 < 6f) { if (v.Breaching && !tricked) { input.sTrick = new Vector2(-1, 0); tricked = true; yield return Wait(.2f); Capture("breach"); input.sTrick = Vector2.zero; } yield return null; }
                Capture("after"); custom = null;
                rs.Add($"sub breach + corkscrew: breaches {v.Breaches - b0}, {(v.StuntScore > s0 ? $"landed +{v.StuntScore - s0}" : "no points")} ({string.Join("; ", v.TrickLog)}), resets {v.Resets}");
            }
            // 5 SHIFT TRICK: off the Lake Jump a spin, then the wings while it spins
            {
                scenario = "trick-shift"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                float lip = Mk("jC"); PlaceBefore(lip - 80, 60, 0, 0); smart = true; holdSpeed = 64;
                int s0 = v.StuntScore, sh0 = v.ShiftTricks; float t0 = Time.time; bool tr = false, wings = false;
                while (Time.time - t0 < 9f)
                {
                    if (!tr && v.TrickAir) { input.sTrick = new Vector2(1, 0); tr = true; }
                    else if (tr && !wings && v.TrickBusy) { input.sTrick = Vector2.zero; input.sWings = true; wings = true; yield return Wait(.2f); Capture("shift-trick"); }
                    yield return null;
                }
                rs.Add($"shift trick: shift tricks {v.ShiftTricks - sh0}, points +{v.StuntScore - s0} ({string.Join("; ", v.TrickLog)}), on {(v.OnShortcut ? v.OnShortcut.label : v.Mode.ToString())}");
            }
            foreach (var x in rs) Line("  " + x);
            Line($"STUNTS {(cleanCar >= 3 && rs.Count(x => x.Contains("NOT") || x.Contains("no points")) == 0 ? "PASS" : "CHECK")}: {cleanCar} of 3 car tricks clean");
            // 6 the human-like pilot: late, noisy, presses the stick once it sees it is in the air (0.15-0.45 s late)
            int tries = ArgInt("-tries", 8), landed = 0, wip = 0;
            for (int k = 0; k < tries; k++)
            {
                scenario = "human-trick"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                var h = NewHuman(900 + k); float lip = ParadisePlunge.Jumps[0].lip; PlaceBefore(lip - 120, 55 + 5 * (k % 3), h.R(-3, 3), h.R(-3, 3)); UseHuman(h);
                int s0 = v.StuntScore, f0 = v.TricksFailed; float t0 = Time.time, airAt = -1; var dir = k % 2 == 0 ? new Vector2(1, 0) : new Vector2(0, -1); bool done = false;
                while (Time.time - t0 < 8f && v.Progress < lip + 140)
                {
                    if (v.TrickAir && airAt < 0) airAt = Time.time;
                    if (airAt > 0 && !done && Time.time - airAt > h.delay * .6f) { input.sTrick = dir + new Vector2(h.R(-.2f, .2f), h.R(-.2f, .2f)); done = true; } else if (done) input.sTrick = Vector2.zero;
                    yield return null;
                }
                input.sTrick = Vector2.zero; yield return Wait(.6f);
                if (v.StuntScore > s0) landed++; if (v.TricksFailed > f0) wip++;
                Line($"  human trick {k + 1}: {(dir.x != 0 ? "spin" : "back flip")}, {h.delay:0.00} s late: {(v.StuntScore > s0 ? $"landed +{v.StuntScore - s0}" : v.TricksFailed > f0 ? "wipeout" : "no trick")}");
            }
            Line($"HUMAN-LIKE TRICKS {(landed >= tries * .6f ? "PASS" : "CHECK")}: landed {landed} of {tries}, wipeouts {wip}");
            Clear();
        }

        IEnumerator Laps17()
        {
            Settings.Laps = 3;
            foreach (int ci in new[] { 0, 1, 2 })
            {
                scenario = $"laps-{ci}"; Clear(); aiDrives = true; game.StartCourse(ci, true);
                var pilot = FreshAi(); pilot.rival = Lineup.Abi; pilot.AlwaysOn = true; pilot.useDifficulty = false;
                v.Bag.AiOnPlayer = true; Time.timeScale = 2f;
                yield return Until(() => !v.Holding, 10f); float go = Time.time; var flashes = new List<string>(); string lapSeen = "";
                while (!v.Finished && Time.time - go < 900f)
                {
                    var hud = Hud.Instance; if (hud && hud.LapFlash != "" && (flashes.Count == 0 || flashes[flashes.Count - 1] != hud.LapFlash)) { flashes.Add(hud.LapFlash); Capture("lap-flash"); }
                    if (hud) lapSeen = hud.LapText;
                    yield return null;
                }
                Time.timeScale = 1; yield return Wait(1.5f); Capture("results");
                var c = v.Course; bool ok = v.Finished && v.LapTimes.Count == 3 && v.GateLog.Count == c.gates.Count * 3;
                Line($"LAPS {c.courseName} {(ok ? "PASS" : "CHECK")}: {v.LapsTotal} laps, finished {v.Finished} in {v.FinishTime:0.0} s; lap times {string.Join(" / ", v.LapTimes.Select(t => t.ToString("0.0")))} (best {v.BestLap:0.0}); gates logged {v.GateLog.Count} (3 x {c.gates.Count}); flashes {string.Join(", ", flashes)}; results show laps {Hud.Instance.ResultsBody.Contains("lap 3")}; resets {v.Resets}; order {string.Join(", ", game.Race.Order().Select(r => $"{r.RacerName} {(r.Finished ? r.FinishTime.ToString("0") : $"lap {r.Lap}")}"))}");
                StopAi(); v.Bag.AiOnPlayer = false; Save();
            }
            Line($"  3-lap race times (the player's racer AI-driven at Hard pace): see above; Dam Shame keeps its own 3 laps (Course.laps {game.Courses[3].laps})");
            Settings.Laps = 1;
        }

        IEnumerator Show17()
        {
            scenario = "show"; Clear(); aiDrives = true; Settings.Laps = 3;
            game.PickCourse(0); game.PickAndGo(Game.PickFor.Show);
            var pilot = FreshAi(); pilot.rival = Lineup.Abi; pilot.AlwaysOn = true; v.Bag.AiOnPlayer = true;
            Time.timeScale = 2f; yield return Until(() => !v.Holding, 10f); float go = Time.time;
            while (!game.Race.Racers.All(r => r.Finished) && Time.time - go < 900f) yield return null;
            Time.timeScale = 1; yield return Wait(2f); Capture("show-results");
            var o = game.ShowOrder();
            Line($"STUNT SHOW {(game.StuntShow && o.Count == 4 && v.LapsTotal == 3 ? "PASS" : "CHECK")}: laps {v.LapsTotal}, items {Items.OnNow(game)}; standing on points: {string.Join(", ", o.Select(r => $"{r.RacerName} {game.ShowPoints(r)} ({r.StuntScore} stunts, {Race.Ordinal(game.Race.Place(r))}, {r.TricksLanded} tricks, {r.TricksFailed} wipeouts, best combo {r.BestCombo}, shift {r.ShiftTricks})"))}; race order {string.Join(", ", game.Race.Order().Select(r => r.RacerName))}; the winner on points {(o[0] != game.Race.Order()[0] ? "is not" : "is")} the race winner");
            Line($"  results text: {Hud.Instance.ResultsBody.Split('\n').FirstOrDefault(l => l.Contains("STUNT SHOW"))}");
            StopAi(); v.Bag.AiOnPlayer = false; Settings.Laps = 1;
        }

        IEnumerator AiSoak17()
        {
            int races = ArgInt("-races", 3); float scale = ArgFloat("-timeScale", 3f); var byKind = new Dictionary<string, int>(); int fin = 0, all = 0, itemResets = 0, resets = 0, hitsAll = 0;
            Items.Rolls.Clear();
            foreach (int ci in new[] { 0, 1, 2, 3 })
                for (int k = 0; k < races; k++)
                {
                    scenario = $"aisoak-{ci}-{k}"; Clear(); aiDrives = true; game.StartCourse(ci, true);
                    var pilot = FreshAi(); pilot.rival = Lineup.Abi; pilot.AlwaysOn = true; v.Bag.AiOnPlayer = true;
                    var hitAt = new Dictionary<ShiftVehicle, float>(); var racers = game.Race.Racers.ToList();
                    foreach (var r in racers) { var rr = r; rr.ItemHit += (kind, by, how) => { if (how == "hit") { hitAt[rr] = Time.time; hitsAll++; } }; }
                    int log0 = Items.Log.Count; Time.timeScale = scale; yield return Until(() => !v.Holding, 10f); float go = Time.time;
                    float limit = 2.2f * 150f * Mathf.Max(1, v.Course.laps);
                    var rc = racers.ToDictionary(r => r, r => r.Resets);
                    while (Time.time - go < limit && !racers.All(r => r.Finished))
                    {
                        foreach (var r in racers) if (r.Resets > rc[r]) { rc[r] = r.Resets; resets++; if (hitAt.TryGetValue(r, out var t) && Time.time - t < 4f) { itemResets++; Line($"  RESET AFTER A HIT: {r.RacerName} {r.LastResetReason} ({Time.time - t:0.0} s after)"); } }
                        yield return null;
                    }
                    Time.timeScale = 1;
                    foreach (var r in racers) { all++; if (r.Finished) fin++; }
                    foreach (var l in Items.Log.Skip(log0)) { int u = l.IndexOf("uses "); if (u >= 0) { var kn = l.Substring(u + 5).Split(':')[0].Split(new[] { " as " }, StringSplitOptions.None)[0]; byKind[kn] = (byKind.TryGetValue(kn, out var n) ? n : 0) + 1; } }
                    Line($"  {v.Course.courseName} race {k + 1}: {string.Join(", ", game.Race.Order().Select(r => $"{r.RacerName} {(r.Finished ? r.FinishTime.ToString("0") : "DNF")} ({r.Resets} resets, {r.StuntScore} stunts, hit {r.HitsTaken})"))}; items used {Items.Log.Skip(log0).Count(l => l.Contains("uses "))}");
                    StopAi(); v.Bag.AiOnPlayer = false; Save();
                }
            Line($"AI SOAK WITH ITEMS {(fin == all && itemResets == 0 ? "PASS" : "CHECK")}: {fin} of {all} finished, {resets} resets in all, {itemResets} within 4 s after an item hit (must be 0), {hitsAll} hits; items used: {string.Join(", ", byKind.OrderByDescending(x => x.Value).Select(x => $"{x.Key} {x.Value}"))}");
            OddsReport();
        }
        void OddsReport()
        {
            // the real rolls from the soak, and 50 races' worth of rolls by place (each racer picks up ~8 boxes a race)
            var rnd = new System.Random(5);
            Line($"ITEM ODDS BY PLACE (rolls seen in the soak: {Items.Rolls.Count}; plus 50 races x 4 places x 8 boxes from Items.Roll):");
            for (int place = 1; place <= 4; place++)
            {
                var counts = new Dictionary<ItemKind, int>(); int n = 0;
                foreach (var r in Items.Rolls.Where(x => x.place == place)) { counts[r.k] = (counts.TryGetValue(r.k, out var a) ? a : 0) + 1; n++; }
                for (int i = 0; i < 50 * 8; i++) { var k = Items.Roll(place, 4, game.Courses[0], rnd); counts[k] = (counts.TryGetValue(k, out var a) ? a : 0) + 1; n++; }
                Line($"  place {place}: {string.Join(", ", counts.OrderByDescending(x => x.Value).Select(x => $"{Items.Name(x.Key)} {100f * x.Value / n:0}%"))} ({n} rolls)");
            }
        }

        // diagnostics: the AI from the anchor before the ramp, its place and form every 0.2 s
        IEnumerator Diag17()
        {
            var c = game.Courses[0];
            foreach (bool ai in new[] { true, false })
            {
                scenario = $"diag-ramp-{(ai ? "ai" : "pilot")}"; Clear(); aiDrives = ai; game.StartCourse(0, false);
                if (ai) { var pilot = FreshAi(); pilot.rival = new Rival { name = "Check AI", lane = 0, brakeMargin = .9f, boostMeter = 2f, seed = 3, shortcutRate = 1 }; pilot.AlwaysOn = true; pilot.useDifficulty = false; }
                else smart = true;
                var a = AnchorBefore(c, ParadisePlunge.CavesFrom - 40); v.Begin(c, a); if (ai) v.GetComponent<RivalPilot>().rival.shortcut = "Sunken Caves"; var cave = c.shortcuts.Find(x => x.label == "Sunken Caves"); Line($"DIAG {(ai ? "AI" : "pilot")} from the anchor at {a.progress:0} m ({a.mode}, speed {a.speed}); rampStart {Mk("rampStart"):0}, lip {Mk("lip"):0}");
                float t0 = Time.time; int k = 0;
                while (Time.time - t0 < 9f) { if (++k % 20 == 0) { float cs = cave.Project(v.Body.position, -1, out var cq, out _); var rp = v.GetComponent<RivalPilot>(); Line($"  {Time.time - t0:0.0} s {v.Mode} prog {v.Progress:0} (fork {cave.mainFrom:0}) on {(v.OnShortcut ? v.OnShortcut.label : "-")} pos {v.Body.position:F1} cave line at {cs:0} m {cq:F1} plan '{(rp ? rp.Plan : "-")}' {v.LastEvent}"); } if (k % 60 == 0) Capture($"t{(Time.time - t0):0.0}"); yield return null; }
                StopAi();
            }
        }

        IEnumerator PP17()
        {
            var c = game.Courses[0];
            // the human-like pilot (Round 12: steers by the HUD arrow only, late, keys or a noisy stick, no boost) on the Valley Flight, LB late in the window
            int tries = ArgInt("-tries", 8), made = 0; var times = new List<float>();
            for (int k = 0; k < tries; k++)
            {
                scenario = "pp17-human-valley"; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                var h = NewHuman(1700 + k); float lip = Mk("jC"); PlaceBefore(lip - 80, new[] { 45f, 55f, 65f, 75f }[k % 4], h.R(-4, 4), h.R(-4, 4)); UseHuman(h);
                int r0 = v.Resets; float t0 = Time.time, open = -1, tl = -1; bool pressed = false; var rs = new List<string>(); int rc = v.Resets;
                while (Time.time - t0 < 50f && v.GatesPassed < 3)
                {
                    if (tl < 0 && v.Progress >= lip) tl = Time.time;
                    if (!pressed && v.WingsOpen) { if (open < 0) open = Time.time; if (Time.time - open >= h.wingsAt) { input.sWings = true; pressed = true; } }
                    if (v.Resets > rc) { rc = v.Resets; rs.Add($"{v.LastResetReason} -> {v.LastResetHow}"); }
                    yield return null;
                }
                bool ok = v.GatesPassed >= 3 && v.Resets == r0 && v.ShortcutsTaken.Contains("Valley Flight"); if (ok) { made++; times.Add(Time.time - tl); }
                Line($"  human Valley Flight try {k + 1} {(ok ? "MADE IT" : "MISSED")}: {(h.keys ? "keys" : "stick")}, late {h.delay:0.00} s, {(tl > 0 ? $"lip -> G3's way {Time.time - tl:0.0} s" : "")}, flew it {v.ShortcutsTaken.Contains("Valley Flight")}, resets {v.Resets - r0}{(rs.Count > 0 ? ": " + string.Join("; ", rs) : "")}");
                if (!ok || k == 0) Capture($"human-{k + 1}-end");
                Clear(); Save();
            }
            Line($"HUMAN-LIKE VALLEY FLIGHT {(made >= tries - 1 ? "PASS" : "CHECK")}: made it {made} of {tries}{(times.Count > 0 ? $", lip to the rejoin mean {times.Average():0.0} s" : "")}");
            // the check pilot's lap (the safe line: car at the Lake Jump, boat on the river), its splits
            scenario = "pp17-lap"; Clear(); game.StartCourse(0, false); smart = true;
            yield return Until(() => v.Finished, 240f);
            Line($"PARADISE PLUNGE LAP{(ParadisePlunge.Old ? " (BUILD 19, -oldPP)" : "")}: line {c.Length:0} m, check pilot {(v.Finished ? v.FinishTime.ToString("0.0") : "not finished")} s, resets {v.Resets}; splits {string.Join(", ", v.Splits.Select((s, i) => $"{c.segments[i % c.segments.Count].name} {s - (i > 0 ? v.Splits[i - 1] : 0):0.0}"))}; 3 laps ~ {3 * v.FinishTime / 60f:0.0} min");
            Capture("pp17-finish");
            // the AI (Hard, no boost) both ways through each split: the time from the fork to the rejoin
            foreach (var sc in c.shortcuts)
            {
                if (sc.Length < 600) continue;
                foreach (bool take in new[] { true, false })
                {
                    scenario = $"pp17-{sc.label.Replace(' ', '-')}-{(take ? "branch" : "main")}"; Clear(); aiDrives = true; game.StartCourse(0, false);
                    var r = new Rival { name = "Check AI", lane = 0, brakeMargin = .9f, boostMeter = 2f, boostStraight = 0, shortcut = take && !sc.choice ? sc.label : "", shortcutRate = 1, seed = 3 };
                    var pilot = FreshAi(); pilot.rival = r; pilot.AlwaysOn = true; pilot.useDifficulty = false; pilot.forceChoice = sc.choice ? (take ? 1 : -1) : 0;
                    var a = AnchorBefore(c, sc.mainFrom - 250); v.Begin(c, a);
                    float tFrom = -1, tTo = -1, t0 = Time.time; int r0 = v.Resets; var shotsAt = new List<float> { .15f, .35f, .55f, .75f, .92f }; int si = 0;
                    while (Time.time - t0 < 90f && tTo < 0)
                    {
                        if (tFrom < 0 && v.Progress >= sc.mainFrom) tFrom = Time.time;
                        if (tFrom > 0 && si < shotsAt.Count && v.Progress >= Mathf.Lerp(sc.mainFrom, sc.mainTo, shotsAt[si])) { Capture($"{si + 1}"); si++; }
                        if (tFrom > 0 && v.Progress >= sc.mainTo + 5 && !v.OnShortcut) tTo = Time.time;
                        yield return null;
                    }
                    Line($"  {sc.label} {(take ? "BRANCH" : "MAIN")}: {(tTo > 0 ? $"{tTo - tFrom:0.0} s fork to rejoin" : "did not reach the rejoin")}, took it {v.ShortcutsTaken.Contains(sc.label)}, resets {v.Resets - r0} ({v.LastResetReason})");
                    StopAi(); Save();
                }
            }
        }
    }
}
