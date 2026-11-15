using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ShiftStorm
{
    // Round 6 checks (built exe, -batchmode -mute, no window, the camera(s) into the 1280 x 800 picture):
    //   bind    item 1: two stand-in controllers (Input System virtual gamepads, no real device): player 1 opens Menu > 2 Players, player 2
    //           joins with A; then each pad's trigger, X (view) and invert drive only its own player
    //   split   item 1: two scripted players (player 1 the check pilot, player 2 a RivalPilot on player 2's own input) race a full lap of
    //           Paradise Plunge with 2 AI: both finish with valid gate sequences, each HUD shows its own player's speed / place / form, a
    //           pause from player 2's Menu pauses both, results on both halves; pictures of both halves at every change of either player;
    //           render + GPU time of the two views per frame (PC)
    //   views   item 2: one lap per view (Chase, Far chase, First person, Front) with pictures in every form and mid-change, look-behind
    //           on the right stick, and how often the camera touched a wall / the cave roof / the water surface, per view and form
    //   round6 = bind + split + views
    public sealed partial class CheckPilot
    {
        Gamepad pad1, pad2;
        void Pads()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;   // batch mode has no focus
            if (pad1 == null) pad1 = InputSystem.AddDevice<Gamepad>("CheckPad1");
            if (pad2 == null) pad2 = InputSystem.AddDevice<Gamepad>("CheckPad2");
        }
        IEnumerator Press(Gamepad pad, GamepadButton b) { InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(b)); yield return null; yield return null; InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null; }

        // ---- item 1: devices ----
        IEnumerator BindCheck()
        {
            scenario = "bind"; Clear(); coast = true; BeginAt(0, 0); yield return Wait(.3f);
            Pads();
            input.Scripted = false;   // the real input path for this check
            game.OpenMenu(); yield return null;
            yield return Press(pad1, GamepadButton.South);   // pad 1 works the menu (A on "Resume" would close it: choose the 2 Players row first)
            if (!game.MenuOpen) game.OpenMenu();
            int row = game.MenuItems.FindIndex(x => x.StartsWith("2 Players")); game.Choose(row); yield return null;
            // pad 1 was the last to press: player 1
            Line($"BIND: menu page {game.MenuPage}, player 1 = {SplitScreen.DeviceName(SplitScreen.P1Device)} (last menu press {SplitScreen.DeviceName(input.LastDevice)}), items: {string.Join(" | ", game.MenuItems)}");
            Capture("split-page-before-join");
            yield return Press(pad2, GamepadButton.South); yield return null;
            bool joined = SplitScreen.P2Device == pad2;
            Line($"BIND join {(joined ? "PASS" : "FAIL")}: A on pad 2 -> player 2 = {SplitScreen.DeviceName(SplitScreen.P2Device)}; note \"{game.MenuNote}\"; page still {game.MenuPage}, row {game.MenuIndex}");
            Capture("split-page-joined");
            if (!joined) { SplitScreen.P1Device = pad1; SplitScreen.P2Device = pad2; }
            int raceRow = game.MenuItems.FindIndex(x => x.StartsWith("Time Trial")); game.Choose(raceRow); yield return Wait(.5f);
            var s = SplitScreen.Session; var p2 = s ? s.P2 : null;
            Line($"BIND split started: {SplitScreen.Active}, player 1 input bound to {SplitScreen.DeviceName(input.Device)}, player 2 ({(p2 ? p2.RacerName : "-")}) bound to {SplitScreen.DeviceName(p2 ? p2.Input.Device : null)}");
            if (!p2) { Save(); yield break; }
            p2.Input.Scripted = false;
            yield return Until(() => !v.Holding, 6f);
            // pad 2's trigger: player 2 only; then pad 1's: player 1 only
            InputSystem.QueueStateEvent(pad2, new GamepadState { rightTrigger = 1 }); yield return null; yield return null;
            float a1 = input.Throttle, a2 = p2.Input.Throttle;
            InputSystem.QueueStateEvent(pad2, new GamepadState()); InputSystem.QueueStateEvent(pad1, new GamepadState { rightTrigger = 1 }); yield return null; yield return null;
            float b1 = input.Throttle, b2 = p2.Input.Throttle;
            InputSystem.QueueStateEvent(pad1, new GamepadState()); yield return null;
            // the keyboard belongs to nobody here (both players on pads): W must drive neither
            bool okThr = a2 > .9f && a1 < .05f && b1 > .9f && b2 < .05f;
            Line($"BIND throttle {(okThr ? "PASS" : "FAIL")}: pad 2 RT -> player 1 {a1:0.00}, player 2 {a2:0.00}; pad 1 RT -> player 1 {b1:0.00}, player 2 {b2:0.00}");
            // X: the view of that player only
            var cv1 = cam.GetComponent<CameraViews>(); var cv2 = s.Views2;
            string v1a = cv1.CurrentName, v2a = cv2.CurrentName;
            yield return Press(pad2, GamepadButton.West);
            string v1b = cv1.CurrentName, v2b = cv2.CurrentName;
            yield return Press(pad1, GamepadButton.West); yield return Press(pad1, GamepadButton.West);
            string v1c = cv1.CurrentName, v2c = cv2.CurrentName;
            bool okView = v1a == v1b && v2a != v2b && v1c != v1a && v2c == v2b;
            Line($"BIND view (X) {(okView ? "PASS" : "FAIL")}: start P1 {v1a} / P2 {v2a}; X on pad 2 -> P1 {v1b} / P2 {v2b}; X x2 on pad 1 -> P1 {v1c} / P2 {v2c}");
            yield return Wait(.5f); Capture("views-after-x");
            // invert: per player (View on pad 2 flips only player 2's)
            bool i1 = input.MyInvert, i2 = p2.Input.MyInvert;
            yield return Press(pad2, GamepadButton.Select);
            bool i1b = input.MyInvert, i2b = p2.Input.MyInvert;
            Line($"BIND invert {(i1 == i1b && i2 != i2b ? "PASS" : "FAIL")}: View on pad 2 -> player 1 {i1} -> {i1b}, player 2 {i2} -> {i2b} (settings: player 1 {ShiftInput.Invert}, player 2 {Settings.InvertVertical2})");
            if (i2b != i2) yield return Press(pad2, GamepadButton.Select);
            // pause from player 2's Start: both paused
            yield return Press(pad2, GamepadButton.Start);
            bool paused = game.MenuOpen && Time.timeScale == 0;
            Line($"BIND pause from player 2 {(paused ? "PASS" : "FAIL")}: menu open {game.MenuOpen}, time scale {Time.timeScale}, menu items: {string.Join(" | ", game.MenuItems)}");
            Capture("paused-by-p2");
            yield return Press(pad1, GamepadButton.Start);
            Line($"BIND resume from player 1: menu open {game.MenuOpen}, time scale {Time.timeScale}");
            // a pad pulled out: the game pauses and says whose; back in: on again
            InputSystem.RemoveDevice(pad2); yield return null;
            bool pausedOut = game.MenuOpen; string miss = s.MissingText;
            Capture("p2-pad-removed");
            InputSystem.AddDevice(pad2); yield return null; yield return null;
            Line($"BIND pad removed {(pausedOut && miss != null && s.MissingPlayer == 0 && p2.Input.Device == pad2 ? "PASS" : "FAIL")}: paused {pausedOut} (\"{miss}\"); after it came back: missing {s.MissingPlayer}, player 2 bound to {SplitScreen.DeviceName(p2.Input.Device)}");
            game.CloseMenu(); yield return null;
            // cameras / views / quality
            Line($"BIND layout: player 1 camera {s.Cam1.rect}, player 2 camera {s.Cam2.rect}; quality while split: {s.QualityNote}; engine mix {VehicleSound.Mix}");
            // back to one player
            game.EndSplit(false); input.Scripted = true; yield return null;
            Line($"BIND end: split {SplitScreen.Active}, player 1 input bound to {(input.Device == null ? "every device" : input.Device.displayName)}, camera {cam.rect}, LOD bias {QualitySettings.lodBias:0.00}, mip limit {QualitySettings.globalTextureMipmapLimit}");
            game.StartCourse(0, false); yield return Wait(.2f);
            Save();
        }

        // ---- item 1: a full split-screen race with two scripted players and 2 AI ----
        IEnumerator SplitCheck()
        {
            scenario = "split"; Clear(); coast = true; BeginAt(ArgInt("-course", 0), 0); yield return Wait(.3f);   // Round 10: -course 1 = Coaster Coast
            Pads();
            SplitScreen.P1Device = pad1; SplitScreen.P2Device = pad2; SplitScreen.P2Pick = 0; SplitScreen.AiCount = 2;
            var lr = Environment.GetCommandLineArgs().Contains("-leftRight"); if (lr) Settings.SplitLeftRight = true;
            game.StartSplit(true); yield return null;
            var s = SplitScreen.Session; var p2 = s.P2; var h1 = Hud.Instance; var h2 = s.Hud2;
            input.Scripted = true;
            var pilot2 = p2.gameObject.AddComponent<RivalPilot>(); pilot2.AlwaysOn = true; pilot2.useDifficulty = false;
            var c = v.Course; var race = game.Race;
            Line($"SPLIT: {(SplitScreen.LeftRight ? "left / right" : "top / bottom")}, player 1 {v.RacerName} (camera {s.Cam1.rect}), player 2 {p2.RacerName} (camera {s.Cam2.rect}), racers {string.Join(", ", race.Racers.Select(r => r.RacerName))}");
            Line($"  quality while split: {s.QualityNote}");
            // pictures: both halves 1 s after any change by either player (the change itself mid-way too)
            int p2Shots = 0;
            Action<string, string> on2 = (kind, text) => { if (kind == "change") { Shot(.25f, $"P2-{p2.ChangeFrom}-{p2.Mode}-mid"); Shot(1.2f, $"P2-in-{p2.Mode}"); p2Shots++; } };
            Action<string, string> on1 = (kind, text) => { if (kind == "changed") Shot(1f, $"P1-in-{v.Mode}"); };
            p2.Event += on2; v.Event += on1;
            Shot(.6f, "grid-countdown");
            yield return Until(() => !v.Holding, 6f);
            coast = false; smart = true; render.Clear(); measure = true;
            int samples = 0, bad1 = 0, bad2 = 0; var badNotes = new List<string>(); bool pausedOk = false; float t0 = Time.time;
            float nextSample = Time.time + 1f; bool didPause = false;
            while ((!v.Finished || !p2.Finished) && Time.time - v.StartTime < 400f)
            {
                segNow = v.Splits.Count < c.segments.Count ? c.segments[v.Splits.Count].name : "finish";
                if (Time.time >= nextSample && !v.Finished && !p2.Finished)
                {
                    nextSample = Time.time + 1f; samples++;
                    // the HUD's speedometer is the speed of its own player (to 2 mph: it is a frame behind), the place its own, the form its own
                    bool ok1 = Mathf.Abs(float.Parse(h1.SpeedText) - v.Speed * 2.237f) < 6 && h1.PlaceText.StartsWith(Race.Ordinal(race.Place(v))) && h1.ModeText.StartsWith(Form(v.Mode));
                    bool ok2 = Mathf.Abs(float.Parse(h2.SpeedText) - p2.Speed * 2.237f) < 6 && h2.PlaceText.StartsWith(Race.Ordinal(race.Place(p2))) && h2.ModeText.StartsWith(Form(p2.Mode));
                    if (!ok1) { bad1++; if (badNotes.Count < 6) badNotes.Add($"P1 HUD {h1.SpeedText} mph {h1.PlaceText} {h1.ModeText} vs {v.Speed * 2.237f:0} {race.Place(v)} {v.Mode}"); }
                    if (!ok2) { bad2++; if (badNotes.Count < 6) badNotes.Add($"P2 HUD {h2.SpeedText} mph {h2.PlaceText} {h2.ModeText} vs {p2.Speed * 2.237f:0} {race.Place(p2)} {p2.Mode}"); }
                }
                // a pause from player 2's Menu, 30 s in: both stop
                if (!didPause && Time.time - v.StartTime > 30f)
                {
                    didPause = true; var x1 = v.Body.position; var x2 = p2.Body.position;
                    p2.Input.sMenu = true; yield return null; yield return null;
                    bool open = game.MenuOpen; Capture("paused-by-p2");
                    yield return WaitReal(.5f);
                    float moved1 = Vector3.Distance(x1, v.Body.position), moved2 = Vector3.Distance(x2, p2.Body.position);
                    pausedOk = open && Time.timeScale == 0 && h2.CenterText == "PAUSED";
                    Line($"SPLIT pause from player 2 {(pausedOk ? "PASS" : "FAIL")}: menu open {open}, time scale {Time.timeScale}, player 2's half shows \"{h2.CenterText}\"; moved in 0.5 s real time: P1 {moved1:0.0} m, P2 {moved2:0.0} m (pause frame included)");
                    p2.Input.sMenu = true; yield return null; yield return null;
                    Line($"  closed again from player 2's Menu: menu open {game.MenuOpen}, time scale {Time.timeScale}");
                }
                yield return null;
            }
            measure = false; segNow = "";
            p2.Event -= on2; v.Event -= on1;
            yield return Wait(1.5f);
            Capture("results-both");
            bool Seq(ShiftVehicle r) => r.GateLog.Count == c.gates.Count && r.GateLog.Select((g, i) => g == i).All(x => x);
            foreach (var r in race.Racers)
                Line($"  {r.RacerName}{(r == v ? " [P1]" : r == p2 ? " [P2]" : "")}: {(r.Finished ? $"{r.FinishTime:0.00} s" : "DNF")}, place {race.Place(r)}, gates {r.GatesPassed}/{c.gates.Count} ({string.Join(" ", r.GateLog)}) {(Seq(r) ? "valid" : "BAD SEQUENCE")}, resets {r.Resets}");
            bool finish = v.Finished && p2.Finished && Seq(v) && Seq(p2);
            Line($"SPLIT race {(finish ? "PASS" : "FAIL")}: both players finished with valid gate sequences: P1 {v.Finished} {Seq(v)}, P2 {p2.Finished} {Seq(p2)}");
            Line($"SPLIT HUDs {(bad1 == 0 && bad2 == 0 ? "PASS" : "FAIL")}: {samples} samples a second apart: player 1's HUD wrong {bad1}x, player 2's {bad2}x{(badNotes.Count > 0 ? ": " + string.Join("; ", badNotes) : "")}");
            bool res = h1.ResultsShown && h2.ResultsShown && h1.ResultsBody.Contains("[P1]") && h1.ResultsBody.Contains("[P2]") && h2.ResultsBody.Contains("[P2]");
            Line($"SPLIT results {(res ? "PASS" : "FAIL")}: shown on player 1's half {h1.ResultsShown}, on player 2's {h2.ResultsShown}; both list [P1] and [P2]; P2's lap on its half: {TimeTrial.Clock(s.Trial2.Lap)}");
            Line($"SPLIT pictures: {shotIndex} so far, player 2 changes pictured {p2Shots}; sound: engine mix {VehicleSound.Mix}, P1 engine non-spatial, P2 engine non-spatial ({(p2.GetComponent<AudioSource>()?.spatialBlend ?? -1):0}), listener on player 1's camera only ({UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length} listener)");
            RenderReport(c);
            Line("  (both views rendered every frame: the cost of a split-screen frame at 1280 x 800 on this PC)");
            game.EndSplit(false); if (lr) Settings.SplitLeftRight = false;
            Clear(); coast = true; game.StartCourse(0, false); yield return Wait(.2f);
            Save();
        }
        static string Form(Mode m) => m == Mode.Drive ? "CAR" : m == Mode.Boat ? "BOAT" : m == Mode.Fly ? "WINGS" : "SUB";

        // ---- item 2: the camera views ----
        IEnumerator ViewsCheck()
        {
            var cv = cam.GetComponent<CameraViews>();
            var marks = new (string name, float p)[] { ("car-promenade", Mk("promMid")), ("car-climb", Mk("climbMid")), ("plane-flight", Mk("lip") + 70), ("plane-arch", Mk("arch") - 25),
                ("boat-lagoon", Mk("splash") + 80), ("boat-mangrove", Mk("bend2") - 30), ("sub-vault", Mk("hi1") - 40), ("sub-curtain", Mk("lo2") - 35), ("car-finish", Mk("G9") + 25) };
            var summary = new List<string>();
            for (int view = 0; view < CameraViews.Names.Length; view++)
            {
                scenario = "view-" + CameraViews.Names[view].Replace(' ', '-').ToLower(); Clear(); coast = true; BeginAt(0, 0); yield return Wait(.2f);
                // X (the check presses it through player 1's own input) until this view is on
                int presses = 0; while ((int)cv.Current != view && presses < 6) { input.sView = true; presses++; yield return null; yield return null; }
                yield return Until(() => !v.Holding, 6f); coast = false; smart = true;
                var hits = new Dictionary<Mode, (int frames, int inside, int surface)>(); int next = 0; bool looked = false; int clips0 = cv.Clips;
                while (!v.Finished && Time.time - v.StartTime < 400f)
                {
                    while (next < marks.Length && v.Progress >= marks[next].p) { Shot(.05f, marks[next].name); next++; }
                    // the camera inside rock or a wall (a 10 cm sphere), or within 5 cm of the water surface
                    var cp = cam.transform.position; bool inside = Physics.CheckSphere(cp, .1f, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore);
                    bool surface = WaterVolume.SurfaceAt(cp, out float sy, 1f) && Mathf.Abs(cp.y - sy) < .05f;
                    hits.TryGetValue(v.Mode, out var h); hits[v.Mode] = (h.frames + 1, h.inside + (inside ? 1 : 0), h.surface + (surface ? 1 : 0));
                    if (inside && h.inside == 0) Shot(0, $"inside-{v.Mode}");
                    if (!looked && v.Progress > Mk("promMid") + 60)
                    {
                        looked = true; input.sLook = new Vector2(0, -1); yield return Wait(.6f); Capture("look-behind"); float yaw = cv.LookYaw;
                        input.sLook = Vector2.zero; yield return Wait(.8f);
                        Line($"VIEW {CameraViews.Names[view]}: right stick pulled back -> looking {yaw:0} deg round; let go -> {cv.LookYaw:0} deg 0.8 s later");
                    }
                    yield return null;
                }
                string perForm = string.Join("; ", hits.Select(kv => $"{Game.FormName(kv.Key)} {kv.Value.inside}/{kv.Value.frames} frames inside rock / walls, {kv.Value.surface} on the water surface"));
                Line($"VIEW {CameraViews.Names[view]} (X pressed {presses}x): lap {(v.Finished ? $"{v.FinishTime:0.00} s" : "DNF")}, resets {v.Resets}; {perForm}; pulled clear {cv.Clips - clips0}x");
                int totalInside = hits.Values.Sum(x => x.inside), totalFrames = hits.Values.Sum(x => x.frames);
                summary.Add($"{CameraViews.Names[view]} {100f * totalInside / Mathf.Max(1, totalFrames):0.0} % inside, {hits.Values.Sum(x => x.surface)} surface frames");
            }
            Line("VIEWS summary: " + string.Join("; ", summary));
            // back to Chase
            while (cv.Current != CameraViews.View.Chase) { input.sView = true; yield return null; yield return null; }
            Clear(); coast = true; game.StartCourse(0, false); yield return Wait(.2f);
            Save();
        }
    }
}
