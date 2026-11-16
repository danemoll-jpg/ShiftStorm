using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ShiftStorm
{
    // Round 3 checks (built exe, -batchmode -mute, no window):
    //   settings / settings2  the invert setting from the menu flips climb / dive in Fly and Dive and survives a restart (second process)
    //   quit / quitresults / quitlauncher  Quit from the menu, from the results, and when started by the launcher: the process exits with 0
    //   waterfall  item 3: a recorded run through the Waterfall Return (dive -> boat -> car): every physics step's speed, form, contacts,
    //              frame time, pictures; 5 runs (3 normal, boosted, slow). "-before" keeps Round 3's new scrape rule out of it.
    //   race       full races (the check pilot plus 3 AI): order, times, resets, pass-throughs
    //   shortcuts  time saved on each shortcut (same start, same speed, no boost), a deliberate miss, a full-lap comparison
    //   soak       100 AI laps (four AI cars per race), gate sequences, stuck spots
    public sealed partial class CheckPilot
    {
        bool aiDrives;
        static bool Arg(string n) => Array.IndexOf(Environment.GetCommandLineArgs(), n) >= 0;
        static int ArgInt(string n, int d) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length && int.TryParse(a[i + 1], out var x) ? x : d; }
        static float ArgFloat(string n, float d) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x) ? x : d; }
        void Save() => File.WriteAllText(Path.Combine(dir, "check_report.txt"), report.ToString());

        // ---------------- menu helpers: the check presses the same buttons a player would ----------------
        IEnumerator Steps(int n) { for (int k = 0; k < n; k++) yield return null; }
        IEnumerator Press(Action a) { a(); yield return Steps(3); }
        IEnumerator MenuTo(string startsWith)
        {
            int idx = game.MenuItems.FindIndex(x => x.StartsWith(startsWith));
            if (idx < 0) { Line($"  menu: no item \"{startsWith}\" on {game.MenuPage}"); yield break; }
            int guard = 0; while (game.MenuIndex != idx && guard++ < 30) yield return Press(() => input.sMove = 1);
        }

        // ---------------- item 1: invert from the menu ----------------
        IEnumerator Pitch(int course, Mode m, string tag, Action<float> got)
        {
            BeginForm(m);   // Round 10: a plane / sub anchor on Paradise Plunge (build 11: the fly / dive lanes)
            yield return Wait(m == Mode.Dive ? 1.6f : .6f);
            input.sRawStick = false; input.sVertical = 0; input.sThrottle = 1; yield return Wait(.3f);
            float p0 = v.PitchDeg; input.sRawStick = true; input.sVertical = -1f;   // the stick pulled back (as on the pad: forward = +)
            yield return Wait(.45f);
            float d = v.PitchDeg - p0; input.sVertical = 0; input.sRawStick = false;
            got(d);
        }
        IEnumerator SettingsCheck()
        {
            scenario = "settings"; Clear(); aiDrives = true;
            Line($"SETTINGS: file {Settings.FilePath}, invert at start {(ShiftInput.Invert ? "on" : "off")}, chosen before {Settings.InvertChosen}");
            BeginForm(Mode.Fly); yield return Wait(1.2f);
            bool noteShown = Hud.Instance.NoteText.Contains("Menu > Settings"); Capture("first-flight-note");
            Line($"FIRST FLIGHT NOTE {(noteShown ? "PASS" : "FAIL")}: \"{Hud.Instance.NoteText}\" shown on the first flight; nothing asked at start-up");
            float fOff = 0, dOff = 0, fOn = 0, dOn = 0;
            yield return Pitch(4, Mode.Fly, "off", x => fOff = x); yield return Pitch(5, Mode.Dive, "off", x => dOff = x);
            // the menu: Menu > Settings > Invert flight / dive controls
            yield return Press(() => input.sMenu = true); bool opened = game.MenuOpen; Capture("menu");
            yield return MenuTo("Settings"); yield return Press(() => input.sConfirm = true);
            bool onSettings = game.MenuPage == Game.Page.Settings;
            yield return MenuTo("Invert"); yield return Press(() => input.sConfirm = true); Capture("settings-invert-on");
            string item = game.MenuItems.Find(x => x.StartsWith("Invert"));
            yield return MenuTo("Back"); yield return Press(() => input.sConfirm = true);
            yield return Press(() => input.sBack = true); bool closed = !game.MenuOpen;
            yield return Pitch(4, Mode.Fly, "on", x => fOn = x); yield return Pitch(5, Mode.Dive, "on", x => dOn = x);
            bool pass = opened && onSettings && closed && ShiftInput.Invert && fOff > 1 && dOff > 1 && fOn < -1 && dOn < -1;
            Line($"INVERT FROM THE MENU {(pass ? "PASS" : "FAIL")}: Menu opened {opened}, Settings page {onSettings}, item now \"{item}\", menu closed {closed}; stick pulled back for 0.45 s: " +
                 $"invert off: Fly pitch {fOff:+0.0;-0.0} deg, Dive {dOff:+0.0;-0.0} deg (nose up); invert on: Fly {fOn:+0.0;-0.0} deg, Dive {dOn:+0.0;-0.0} deg (nose down)");
            Line($"  saved: {Settings.FilePath}: {File.ReadAllText(Settings.FilePath).Replace("\n", " ").Replace("  ", " ")}");
            aiDrives = false;
        }
        IEnumerator Settings2()
        {
            scenario = "settings2"; Clear(); aiDrives = true;
            float f = 0; yield return Pitch(4, Mode.Fly, "restart", x => f = x);
            bool pass = ShiftInput.Invert && f < -1;
            Line($"INVERT SURVIVES A RESTART {(pass ? "PASS" : "FAIL")}: a new game process read {Settings.FilePath}: invert {(ShiftInput.Invert ? "on" : "off")}; stick pulled back in Fly: pitch {f:+0.0;-0.0} deg (nose down = inverted)");
            aiDrives = false;
        }

        // ---------------- item 2: quit ----------------
        IEnumerator QuitCheck(string which)
        {
            scenario = which; Clear();
            if (which == "quitresults")
            {
                smart = true; BeginAt(0, AnchorNear(Mk("G9") + 20)); yield return Until(() => game.Trial.HasResults, 40f); yield return Wait(1f);
                Capture("results"); bool shown = game.Trial.HasResults;
                yield return Press(() => input.sMove = 1); yield return Press(() => input.sMove = 1);
                Capture("results-quit-selected"); string sel = Game.ResultsItems[game.ResultsIndex];
                yield return Press(() => input.sConfirm = true);
                Line($"QUIT FROM RESULTS: results shown {shown} (finished {v.Finished} in {v.FinishTime:0.00} s), selected \"{sel}\", then the Quit page: {game.MenuOpen && game.MenuPage == Game.Page.Quit}");
            }
            else
            {
                BeginAt(0, 0); yield return Wait(1.5f);
                yield return Press(() => input.sMenu = true); Capture("menu");
                yield return MenuTo("Quit game"); yield return Press(() => input.sConfirm = true); Capture("quit-page");
                bool page = game.MenuPage == Game.Page.Quit, defaultNo = game.MenuItems[game.MenuIndex].StartsWith("No");
                yield return Press(() => input.sConfirm = true); bool backToMain = game.MenuOpen && game.MenuPage == Game.Page.Main && !game.QuitChosen;
                yield return MenuTo("Quit game"); yield return Press(() => input.sConfirm = true);
                Line($"QUIT FROM THE MENU{(which == "quitlauncher" ? " (started by the launcher)" : "")}: Quit page shown {page}, \"No\" chosen by default {defaultNo}; No went back to the menu {backToMain}");
            }
            yield return Press(() => input.sMove = 1);
            Line($"  choosing \"{game.MenuItems[game.MenuIndex]}\" now; the process must end with exit code 0 (Run-Check / the launcher report it)");
            Line($"audio output level (AudioListener.GetOutputData, all frames): peak {meter.MaxPeakSeen:0.000000}, AudioListener.volume {AudioListener.volume}");
            Save();
            input.sConfirm = true; yield return Steps(2);
            float until = Time.realtimeSinceStartup + 10; while (Time.realtimeSinceStartup < until) yield return null;
            Line("QUIT FAIL: still running 10 s after Yes"); Save(); Application.Quit(3);
        }

        // ---------------- item 3: the waterfall return, recorded ----------------
        sealed class Sample { public float t, speed, hs, vy, fwd, dt, auth, fov, camDist; public Vector3 pos; public Mode mode; public bool changing, onWater, boosting; public int wheels; public string contacts = "", ev = ""; }
        List<Sample> rec; float recFrom; readonly List<string> contactsNow = new List<string>(); float lastFrameMs;
        void Record()
        {
            if (rec == null) return;
            var s = new Sample { t = Time.time - recFrom, speed = v.Speed, vy = v.Body.linearVelocity.y, mode = v.Mode, changing = v.Changing, onWater = v.OnWater, wheels = v.GroundedWheels, pos = v.Body.position, dt = lastFrameMs, auth = v.Authority, boosting = v.Boosting };
            s.fov = cam.fieldOfView; s.camDist = Vector3.Distance(cam.transform.position, v.transform.position);
            var lv = v.Body.linearVelocity; s.hs = new Vector2(lv.x, lv.z).magnitude; s.fwd = Vector3.Dot(lv, v.transform.forward);
            s.contacts = string.Join(" ", contactsNow.Distinct()); contactsNow.Clear();
            s.ev = v.LastEvent != lastEv ? v.LastEvent : ""; lastEv = v.LastEvent;
            rec.Add(s);
        }
        string lastEv = "";
        void OnContact(Collision c, bool first) { if (rec == null) return; float imp = c.impulse.magnitude; contactsNow.Add($"{(first ? "+" : "")}{c.collider.name}({imp:0})"); }

        IEnumerator Waterfall()
        {
            bool before = Arg("-before"); string tag = before ? "before" : "after";
            float keepK = game.Shared.scrapeSpeedKeep, keepS = game.Shared.scrapeSeconds;
            float keepC = game.Shared.changeSpeedCarry, keepG = game.Shared.wheelsDownExtraGravity;
            if (before) { game.Shared.scrapeSpeedKeep = 1; game.Shared.scrapeSeconds = 0; game.Shared.changeSpeedCarry = 0; game.Shared.wheelsDownExtraGravity = 0; ChaseCam.Legacy = true; }
            Line($"WATERFALL ({tag}): the run from the Coral Vault's last anchor through the surfacing pool, the water ramp, the curtain and the landing on the finishing road, recorded every physics step (60 Hz)");
            Line($"  the place: pool surface zone z {ParadisePlunge.S * -270:0}, water ramp z {ParadisePlunge.S * -222:0} (y -0.6) to its lip z {ParadisePlunge.S * -182:0} (y 5.0), Wheels-down zone z {ParadisePlunge.S * -176:0} +-6, finishing road from z {v.Course.PointAt(Mk("landing") - 12, out _).z:0} (y 2.0)");
            v.Contact += OnContact;
            var runs = new (string name, float cap, bool boost)[] { ("normal-1", 1, false), ("normal-2", 1, false), ("normal-3", 1, false), ("boosted", 1, true), ("slow", .5f, false) };
            int n = 0;
            foreach (var (name, cap, b) in runs)
            {
                scenario = $"wf-{tag}-{name}"; Clear(); smart = true; throttleCap = cap; boost = b; measure = true;
                BeginAt(0, AnchorNear(Mk("G7") - 30)); yield return Wait(.2f);
                rec = new List<Sample>(); recFrom = Time.time; bool shooting = false; float nextShot = 0, shotEvery = n == 0 ? .1f : .25f, landedAt = -1;
                float end = Time.time + 40;
                while (Time.time < end)
                {
                    lastFrameMs = frames.Count > 0 && frames[frames.Count - 1].shot ? 0 : Time.unscaledDeltaTime * 1000;   // a frame that saved a picture is left out
                    if (!shooting && v.Mode == Mode.Boat && v.Progress > Mk("poolEdge") - 10) shooting = true;
                    if (shooting && Time.time >= nextShot) { Shot(0, $"t{Time.time - recFrom:00.00}-{v.Mode}"); nextShot = Time.time + shotEvery; }
                    if (landedAt < 0 && v.Mode == Mode.Drive && v.GroundedWheels >= 2) landedAt = Time.time;
                    if (landedAt > 0 && Time.time - landedAt > 1.5f) break;
                    if (v.Resets > 0) break;
                    yield return null;
                }
                measure = false; var r = rec; rec = null;
                WriteTrace(r, Path.Combine(dir, $"waterfall-{tag}-{name}.csv"));
                AnalyseWaterfall(name, r, cap, b);
                n++; Save();
            }
            v.Contact -= OnContact; game.Shared.scrapeSpeedKeep = keepK; game.Shared.scrapeSeconds = keepS; game.Shared.changeSpeedCarry = keepC; game.Shared.wheelsDownExtraGravity = keepG; ChaseCam.Legacy = false;
        }
        static void WriteTrace(List<Sample> r, string file)
        {
            var sb = new StringBuilder("t,mode,changing,speed,horiz,vy,fwd,x,y,z,wheels,onWater,authority,boost,frame_ms,fov,cam_dist,contacts,event\n");
            foreach (var s in r) sb.AppendLine(FormattableString.Invariant($"{s.t:0.000},{s.mode},{(s.changing ? 1 : 0)},{s.speed:0.00},{s.hs:0.00},{s.vy:0.00},{s.fwd:0.00},{s.pos.x:0.0},{s.pos.y:0.00},{s.pos.z:0.0},{s.wheels},{(s.onWater ? 1 : 0)},{s.auth:0.00},{(s.boosting ? 1 : 0)},{s.dt:0.0},{s.fov:0.0},{s.camDist:0.00},{s.contacts.Replace(',', ';')},\"{s.ev.Replace("\"", "'")}\""));
            File.WriteAllText(file, sb.ToString());
        }
        void AnalyseWaterfall(string name, List<Sample> r, float cap, bool b)
        {
            int iSurf = r.FindIndex(s => s.mode == Mode.Boat), iDrive = r.FindIndex(s => s.mode == Mode.Drive);
            if (iSurf < 0 || iDrive < 0) { Line($"  {name}: did not get through ({(v.Resets > 0 ? "reset: " + v.LastResetReason : "timeout")}), samples {r.Count}"); return; }
            int iLand = r.FindIndex(iDrive, s => s.wheels >= 2);
            int iRamp = r.FindIndex(iSurf, s => s.mode == Mode.Boat && s.pos.z > ParadisePlunge.S * -223 && s.pos.y > -.2f);
            float window0 = r[iSurf].t - .5f, window1 = (iLand >= 0 ? r[iLand].t : r[r.Count - 1].t) + 1f;
            var w = r.FindAll(s => s.t >= window0 && s.t <= window1);
            // the biggest speed drop over 0.25 s in the window, and what was happening then
            float worstDrop = 0; Sample at = null, from = null;
            for (int k = 0; k < w.Count; k++) for (int j = k + 1; j < w.Count && w[j].t - w[k].t <= .25f; j++) { float d = w[k].speed - w[j].speed; if (d > worstDrop) { worstDrop = d; at = w[j]; from = w[k]; } }
            float worstMs = 0; Sample wf = null; foreach (var s in w) if (s.dt > worstMs) { worstMs = s.dt; wf = s; }
            var touched = w.FindAll(s => s.contacts.Contains("+")).Select(s => $"{s.t - r[iSurf].t:+0.00;-0.00}s {s.mode} {s.contacts}").Distinct().Take(8);
            float minSp = w.Min(s => s.speed); var minS = w.First(s => s.speed == minSp);
            var land = iLand >= 0 ? r[iLand] : null;
            string trace = string.Join(" ", w.Where((s, k) => k % 6 == 0).Select(s => $"{s.speed:0}{(s.mode == Mode.Drive ? "d" : s.mode == Mode.Boat ? "b" : "s")}"));
            Line($"  {name} (throttle {cap:0.0}{(b ? ", boost" : "")}): Dive->Boat at {r[iSurf].speed:0.0} m/s (z {r[iSurf].pos.z:0}); on the ramp foot {(iRamp >= 0 ? $"{r[iRamp].speed:0.0} m/s" : "-")}; Boat->Drive at {r[iDrive].speed:0.0} m/s, y {r[iDrive].pos.y:0.0}, z {r[iDrive].pos.z:0} ({r[iDrive].t - r[iSurf].t:0.00} s after surfacing); " +
                 $"wheels down {(land != null ? $"{land.t - r[iDrive].t:0.00} s later at {land.speed:0.0} m/s, vy {land.vy:0.0}, z {land.pos.z:0}" : "never")}");
            Line($"    slowest {minSp:0.0} m/s at {minS.t - r[iSurf].t:+0.00;-0.00} s ({minS.mode}, z {minS.pos.z:0}); biggest drop in 0.25 s: {worstDrop:0.0} m/s ({from?.speed:0.0} -> {at?.speed:0.0}) at {(at != null ? at.t - r[iSurf].t : 0):+0.00;-0.00} s ({at?.mode}, z {at?.pos.z:0}, y {at?.pos.y:0.0}, wheels {at?.wheels}, contacts \"{at?.contacts}\"); worst frame {worstMs:0.0} ms at {(wf != null ? wf.t - r[iSurf].t : 0):+0.00;-0.00} s ({wf?.mode}{(wf != null && wf.changing ? ", changing" : "")}); frames over 16.7 ms: {w.Count(s => s.dt > 16.7f)} of {w.Count} steps");
            Line($"    first touches: {(touched.Any() ? string.Join(" | ", touched) : "none")}");
            // the camera through the change into Drive: field of view and distance, 0.3 s before to 1.5 s after
            var cw = r.FindAll(s => s.t >= r[iDrive].t - .3f && s.t <= r[iDrive].t + 1.5f);
            float fovBefore = cw[0].fov, fovMin = cw.Min(s => s.fov), dBefore = cw[0].camDist, dMin = cw.Min(s => s.camDist);
            Line($"    camera from 0.3 s before Boat->Drive to 1.5 s after: field of view {fovBefore:0.0} -> lowest {fovMin:0.0} deg ({fovMin - fovBefore:+0.0;-0.0}), distance {dBefore:0.00} -> nearest {dMin:0.00} m ({dMin - dBefore:+0.00;-0.00}); airtime lip to wheels {(land != null && iRamp >= 0 ? land.t - r.FindLast(s => s.mode == Mode.Boat).t : 0):0.00} s; car speed 1.5 s after the change {(cw.Count > 0 ? cw[cw.Count - 1].speed : 0):0.0} m/s");
            Line($"    speed every 0.1 s (s sub / b boat / d car): {trace}");
        }

        // ---------------- item 4: races against the AI ----------------
        sealed class PassThrough { public float t, depth; public string a, b; public Vector3 at; }
        IEnumerator RaceCheck()
        {
            int races = ArgInt("-races", 2);
            for (int k = 0; k < races; k++)
            {
                scenario = $"race{k + 1}"; Clear(); smart = true; topSpeed.Clear();
                game.StartCourse(0, true); var race = game.Race; Shot(.6f, "grid");
                yield return Until(() => !v.Holding, 6f); float go = Time.time;
                var shotsAt = new Queue<float>(new[] { 3f, 25f, 60f, 95f, 125f });
                var pts = new List<PassThrough>(); int step = 0; var places = new List<int>();
                while (Time.time - go < 330f && !race.Racers.All(r => r.Finished))
                {
                    if (shotsAt.Count > 0 && Time.time - go > shotsAt.Peek()) { shotsAt.Dequeue(); Shot(0, $"t{Time.time - go:000}-{Race.Ordinal(race.Place(v))}"); }
                    if (++step % 3 == 0) PassThroughs(race, pts, Time.time - go);
                    if (Time.time - go >= places.Count * 10f) places.Add(race.Place(v));
                    yield return null;
                }
                yield return Until(() => game.Trial.HasResults, 3f); yield return Wait(1f); Shot(0, "results"); yield return Wait(.2f);
                var order = race.Order();
                Line($"RACE {k + 1}: {string.Join(", ", order.Select((r, i) => $"{i + 1}. {r.RacerName} {(r.Finished ? TimeTrial.Clock(r.FinishTime) : "DNF")}"))}");
                Line($"  player (check pilot, no boost) place every 10 s: {string.Join(" ", places)}; finished {Race.Ordinal(race.Place(v))}");
                bool stuck = false;
                foreach (var r in game.Rivals)
                {
                    var pilot = r.GetComponent<RivalPilot>(); var resets = r.Log.FindAll(x => x.Contains("reset ("));
                    stuck |= !r.Finished || resets.Exists(x => x.Contains("stuck") || x.Contains("no progress"));
                    Line($"  {r.RacerName}: {(r.Finished ? $"{r.FinishTime:0.00} s" : "DNF")}, gates {r.GatesPassed}/{r.Course.gates.Count} ({string.Join(" ", r.GateLog)}), resets {r.Resets}{(resets.Count > 0 ? ": " + string.Join("; ", resets) : "")}, backed out {pilot.Unsticks}x, scrapes {r.Scrapes}, shortcut plan {(pilot.ShortcutsTried > 0 ? pilot.Plan : "none")}, taken {(r.ShortcutsTaken.Count > 0 ? string.Join(", ", r.ShortcutsTaken) : "none")}");
                }
                Line($"  player: resets {v.Resets}, scrapes {v.Scrapes}; pass-throughs (hulls overlapping more than 0.4 m): {pts.Count}{(pts.Count > 0 ? ", worst " + string.Join("; ", pts.OrderByDescending(x => x.depth).Take(4).Select(x => $"{x.a} / {x.b} {x.depth:0.00} m at {x.t:0.0} s z {x.at.z:0}")) : "")}");
                Line($"RACE {k + 1} {(!stuck && pts.Count == 0 ? "PASS" : "CHECK")}: AI never stuck {!stuck}, no pass-through {pts.Count == 0}");
                Save();
            }
        }
        void PassThroughs(Race race, List<PassThrough> pts, float t)
        {
            var rs = race.Racers;
            for (int i = 0; i < rs.Count; i++) for (int j = i + 1; j < rs.Count; j++)
            {
                var a = rs[i]; var b = rs[j]; if ((a.Body.position - b.Body.position).sqrMagnitude > 36) continue;
                var ca = a.GetComponent<BoxCollider>(); var cb = b.GetComponent<BoxCollider>();
                if (Physics.ComputePenetration(ca, a.transform.position, a.transform.rotation, cb, b.transform.position, b.transform.rotation, out _, out float d) && d > .4f)
                    pts.Add(new PassThrough { t = t, depth = d, a = a.RacerName, b = b.RacerName, at = a.Body.position });
            }
        }

        // ---------------- item 5: the shortcuts ----------------
        RivalPilot DriveWith(string shortcuts, float miss = 0)
        {
            var old = v.GetComponent<RivalPilot>(); if (old) Destroy(old);
            var p = v.gameObject.AddComponent<RivalPilot>();
            p.rival = new Rival { name = "check", brakeMargin = 1, boostMeter = 2, boostStraight = 0, shortcut = shortcuts, shortcutRate = 1, seed = 1 };
            p.AlwaysOn = true; p.missOffset = miss; p.useDifficulty = false; aiDrives = true; return p;
        }
        void StopAi() { var p = v.GetComponent<RivalPilot>(); if (p) Destroy(p); aiDrives = false; }
        IEnumerator Leg(string label, string plan, int anchor, int gatesTarget, float miss, Action<float, string> done)
        {
            Clear(); BeginAt(0, anchor); DriveWith(plan, miss); yield return null;
            float t0 = Time.time; int r0 = v.Resets; int sc0 = v.Scrapes;
            yield return Until(() => v.GatesPassed >= gatesTarget, 90f);
            float t = v.GatesPassed >= gatesTarget ? Time.time - t0 : -1;
            done(t, $"{(t >= 0 ? $"{t:0.00} s" : "did not get there in 90 s")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, scrapes {v.Scrapes - sc0}, shortcut {(v.ShortcutsTaken.Count > 0 ? string.Join(", ", v.ShortcutsTaken) : "not taken")}");
            StopAi();
        }
        IEnumerator Shortcuts()
        {
            var c = game.Courses[0]; game.StartCourse(0, false);
            foreach (var sc in c.shortcuts)
            {
                Line($"SHORTCUT {sc.label}: its line {sc.Length:0} m from main progress {sc.mainFrom:0} to {sc.mainTo:0} m (the main line there: {sc.mainTo - sc.mainFrom:0} m); entry / exit sub-gates; stands in for {(sc.replacesGate >= 0 ? c.gates[sc.replacesGate].label : "no main gate")}; rejoins before {c.gates[sc.nextGate + (sc.replacesGate >= 0 ? 1 : 0)].label}");
                bool air = sc.replacesGate >= 0;
                int anchor = AnchorNear(air ? Mk("lip") + 90 : Mk("portal") + 45), target = air ? 5 : 8;   // to G4 passed / to G7 passed
                float tSafe = 0, tShort = 0, tMiss = 0; string sSafe = "", sShort = "", sMiss = "";
                scenario = "sc-safe"; yield return Leg(sc.label, "", anchor, target, 0, (t, s) => { tSafe = t; sSafe = s; });
                scenario = "sc-take"; Shot(air ? 6f : 4f, "on-the-shortcut");
                yield return Leg(sc.label, sc.label, anchor, target, 0, (t, s) => { tShort = t; sShort = s; });
                scenario = "sc-miss"; Shot(air ? 6f : 4f, "missing-it");
                yield return Leg(sc.label, sc.label, anchor, target, air ? 9f : 6f, (t, s) => { tMiss = t; sMiss = s; });
                float gain = tSafe - tShort;
                Line($"  from the anchor at {c.anchors[anchor].progress:0} m to {c.gates[target - 1].label}, the same start speed, no boost: safe line {sSafe}; shortcut {sShort}; deliberate miss (aimed {(air ? 9 : 6)} m beside its entry) {sMiss}");
                Line($"  SHORTCUT {sc.label} {(gain > 0 ? "PASS" : "FAIL")}: saves {gain:0.00} s (target {(air ? "1-2" : "2-3")} s); the miss {(tMiss > 0 ? $"costs {tMiss - tSafe:+0.00;-0.00} s against the safe line, {tMiss - tShort:+0.00;-0.00} s against the shortcut, not trapped" : "did not reach the gate")}");
                Save();
            }
            // full laps: safe line, then both shortcuts
            float lapSafe = 0, lapShort = 0; string lsS = "", lsT = "";
            foreach (var (plan, nm) in new[] { ("", "safe"), (string.Join("|", c.shortcuts.Select(x => x.label)), "shortcuts") })
            {
                scenario = "sc-lap-" + nm; Clear(); BeginAt(0, 0); DriveWith(plan); yield return Until(() => !v.Holding, 6f);
                yield return Until(() => v.Finished, 300f);
                string s = $"{(v.Finished ? $"{v.FinishTime:0.00} s" : "DNF")}, resets {v.Resets}, scrapes {v.Scrapes}, shortcuts {(v.ShortcutsTaken.Count > 0 ? string.Join(", ", v.ShortcutsTaken) : "none")}, gates {string.Join(" ", v.GateLog)}";
                if (nm == "safe") { lapSafe = v.FinishTime; lsS = s; } else { lapShort = v.FinishTime; lsT = s; }
                StopAi();
            }
            Line($"SHORTCUT FULL LAP: the AI driver without boost, safe line {lsS}; both shortcuts {lsT}; difference {lapSafe - lapShort:+0.00;-0.00} s");
        }

        // ---------------- item 6: the soak ----------------
        IEnumerator Soak()
        {
            int laps = ArgInt("-laps", 100); float scale = ArgFloat("-timeScale", 3f), limit = 2f * 150f;
            Line($"SOAK ({game.Courses[ArgInt("-course", 0)].courseName}): {laps} AI laps, four AI cars per race (Abi's car driven by an AI too), time scale {scale:0.0} (physics still at 60 Hz steps), a lap counts when finished within {limit:0} s (twice the 150 s target)");
            var abi = new Rival { name = "Abi (AI)", lane = 0, brakeMargin = .9f, boostMeter = .6f, boostStraight = 9f, shortcut = "Sky Arch slit|Mangrove bypass|Coral Vault tunnel|Lighthouse rocks", shortcutRate = .25f, seed = 51 };
            int done = 0, counted = 0, bad = 0, race = 0; var times = new List<float>(); var resetsAt = new List<(string who, string why, Vector3 at, float prog)>(); var dnfs = new List<string>();
            var takenBy = new Dictionary<string, int>();
            while (done < laps)
            {
                race++; scenario = $"soak{race}"; Clear(); aiDrives = true;
                game.StartCourse(ArgInt("-course", 0), true);   // Round 10: -course 1 soaks Coaster Coast
                var pilot = v.GetComponent<RivalPilot>() ?? v.gameObject.AddComponent<RivalPilot>(); pilot.rival = abi; pilot.AlwaysOn = true;
                Time.timeScale = scale;
                yield return Until(() => !v.Holding, 10f); float go = Time.time;
                var racers = game.Race.Racers.ToList();
                while (Time.time - go < limit + 2 && !racers.All(r => r.Finished)) yield return null;
                Time.timeScale = 1;
                foreach (var r in racers)
                {
                    if (done >= laps) break; done++;
                    bool seqOk = r.GateLog.Count == r.Course.gates.Count && r.GateLog.Select((g, i) => g == i).All(x => x);
                    bool ok = r.Finished && r.FinishTime <= limit;
                    if (ok) { counted++; times.Add(r.FinishTime); if (!seqOk) bad++; }
                    else dnfs.Add($"race {race} {r.RacerName}: {(r.Finished ? $"{r.FinishTime:0} s" : $"not finished, gates {r.GatesPassed}, at {r.Body.position:F0} ({r.Mode}, progress {r.Progress:0})")}");
                    foreach (var l in r.Log.FindAll(x => x.Contains("reset (")))
                    {
                        int i0 = l.IndexOf("reset ("), i1 = l.IndexOf(", at "); string why = i1 > i0 ? l.Substring(i0 + 7, i1 - i0 - 7) : l;
                        var m = System.Text.RegularExpressions.Regex.Match(l, @"at (-?[\d.]+) \(x (-?[\d.]+), y (-?[\d.]+)\)");
                        var at = m.Success ? new Vector3(float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)) : Vector3.zero;
                        resetsAt.Add((r.RacerName, why.Split(',')[0], at, 0));
                    }
                    foreach (var s in r.ShortcutsTaken) takenBy[s] = (takenBy.TryGetValue(s, out var x) ? x : 0) + 1;
                }
                Line($"  race {race}: {string.Join(", ", game.Race.Order().Select(r => $"{r.RacerName} {(r.Finished ? r.FinishTime.ToString("0.0") : "DNF")}{(r.Resets > 0 ? $" ({r.Resets} resets)" : "")}{(r.ShortcutsTaken.Count > 0 ? " [" + string.Join("+", r.ShortcutsTaken) + "]" : "")}"))}");
                if (race % 5 == 0) Save();
            }
            aiDrives = false; StopAi();
            times.Sort();
            bool pass = counted >= laps * .95f && bad == 0;
            Line($"SOAK {(pass ? "PASS" : "FAIL")}: {counted} of {done} laps finished within {limit:0} s (need {Mathf.CeilToInt(laps * .95f)}); {bad} counted finishes with a bad gate sequence; lap times min {(times.Count > 0 ? times[0] : 0):0.0} / median {(times.Count > 0 ? times[times.Count / 2] : 0):0.0} / max {(times.Count > 0 ? times[times.Count - 1] : 0):0.0} s; shortcuts taken: {(takenBy.Count > 0 ? string.Join(", ", takenBy.Select(kv => $"{kv.Key} {kv.Value}")) : "none")}");
            foreach (var d in dnfs) Line("  not counted: " + d);
            Line($"  resets: {resetsAt.Count} in {done} laps; by reason: {string.Join(", ", resetsAt.GroupBy(x => x.why).Select(g => $"{g.Key} {g.Count()}"))}");
            foreach (var g in resetsAt.GroupBy(x => new Vector3Int(Mathf.RoundToInt(x.at.x / 40) * 40, 0, Mathf.RoundToInt(x.at.z / 40) * 40)).OrderByDescending(g => g.Count()).Take(8))
                Line($"  stuck spot near x {g.Key.x}, z {g.Key.z}: {g.Count()} resets ({string.Join(", ", g.GroupBy(x => x.why).Select(h => $"{h.Key} {h.Count()}"))}; {string.Join(", ", g.Select(x => x.who).Distinct())})");
        }
    }
}
