using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 14 checks (Dan on build 15: the menu "needs clean up to be more readable", left / right should change values, a title screen;
    // the crowd far too loud; the Scrambler's sub in a whole bubble; the screen shaky after the race; the Ferris wheel off the main path).
    //   menus14   a picture of every menu screen at 1280 x 800 (the Deck's screen) and its text, row by row: title, main menu, both course
    //             pages, choose your racer, The Tour, the Garage, 2 Players, the settings list and its four groups, pause, quit, results
    //   menuwalk  the scripted walk with the player's own buttons: title -> menu -> course -> racer -> race -> pause -> quit to menu ->
    //             settings (left / right change a volume and an option) -> quit game (the process must end with exit code 0)
    //   mix       the would-be levels per bus in a race and at the finish with the default volumes (the run stays muted): music, engine,
    //             Barnaby, the crowd (stands' murmur, cheers, the GO roar, the finish's crowd sound), against build 15's crowd gains
    //   bubble    the Scrambler's sub: pictures from the side, the front, first person and the chase camera, the change forming the bubble;
    //             every bone of the rider, the grips and the bike's mesh extents inside the glass
    //   finishcam both courses: the camera for 10 s after the finish (chase and first person): frame-to-frame jitter (no shake)
    //   wheel     Coaster Coast: the human-like pilot from before the Midway Jump through the Ferris wheel (-tries N, default 10), AI runs,
    //             approach / pass pictures; every choice jump: LB pressed on the ramp opens the wings at the lip, LB 1.2 s into the air too
    //   round14 = all six but menuwalk (it ends the process). "-oldFinish", "-oldWheel", "-oldChoice", "-oldBubble": build 15's behaviour.
    public sealed partial class CheckPilot
    {
        IEnumerator Round14(string which)
        {
            bool all = which == "round14";
            if (all || which == "menus14") yield return Menus14();
            if (all || which == "mix") yield return MixCheck();
            if (all || which == "bubble") yield return BubbleCheck();
            if (all || which == "finishcam") yield return FinishCamCheck();
            if (all || which == "wheel") yield return WheelCheck();
        }

        // ---------------- item 1: every menu screen ----------------
        void MenuShot(string name)
        {
            Capture("menu-" + name);
            var h = Hud.Instance; string text = h ? h.MenuScreenText.Replace("\n", "  //  ") : "";
            Line($"  [{name}] page {game.MenuPage}, row {game.MenuIndex} \"{(game.MenuIndex < game.Rows.Count ? game.Rows[game.MenuIndex].Text : "")}\": {text}");
        }
        IEnumerator Menus14()
        {
            scenario = "menus14"; Clear(); coast = true; game.StartCourse(0, true); yield return Wait(.5f);
            float scale = 800f / 720f;
            Line($"MENUS (item 1): pictures at {W} x {H}; the canvas scales its 1280 x 720 reference by x{scale:0.00}: rows {30 * scale:0} px, description {24 * scale:0} px, hint {20 * scale:0} px, titles {46 * scale:0} px (build 15: rows {26 * scale:0} px in one block of text)");
            game.ShowTitle(); yield return WaitReal(.6f); MenuShot("title");
            game.OpenFrontMenu(); yield return WaitReal(.4f); MenuShot("main");
            int Row(string s) => game.MenuItems.FindIndex(x => x.StartsWith(s));
            game.Choose(Row("Race")); yield return WaitReal(.4f); MenuShot("course-race");
            input.sSide = 1; yield return WaitReal(.3f); MenuShot("course-race-right");
            input.sSide = -1; yield return WaitReal(.3f);
            game.Choose(Row("Course")); yield return WaitReal(.8f); MenuShot("pick");
            game.Back(); yield return WaitReal(.2f); game.Back(); yield return WaitReal(.2f);
            game.Choose(Row("Time Trial")); yield return WaitReal(.4f); MenuShot("course-trial"); game.Back(); yield return WaitReal(.2f);
            game.Choose(Row("The Tour")); yield return WaitReal(.4f); MenuShot("tour"); game.Back(); yield return WaitReal(.2f);
            game.Choose(Row("Garage")); yield return WaitReal(.8f); MenuShot("garage"); game.Back(); yield return WaitReal(.2f);
            game.Choose(Row("2 Players")); yield return WaitReal(.4f); MenuShot("2-players"); game.Back(); yield return WaitReal(.2f);
            game.Choose(Row("Settings")); yield return WaitReal(.4f); MenuShot("settings");
            foreach (var g in new[] { "Gameplay", "Audio", "Video", "Controls" }) { game.Choose(Row(g)); yield return WaitReal(.3f); MenuShot("settings-" + g.ToLower()); game.Back(); yield return WaitReal(.2f); }
            game.Back(); yield return WaitReal(.2f);
            game.Choose(Row("Quit game")); yield return WaitReal(.3f); MenuShot("quit"); game.Back(); yield return WaitReal(.2f);
            // the pause menu in a race, and the results
            game.CloseMenu(); game.StartCourse(0, true); yield return Until(() => !v.Holding, 8f); yield return Wait(1f);
            game.OpenMenu(); yield return WaitReal(.3f); MenuShot("pause");
            game.Choose(Row("Settings")); yield return WaitReal(.3f); MenuShot("pause-settings"); game.Back(); yield return WaitReal(.2f); game.CloseMenu();
            coast = false; smart = true; BeginAt(0, AnchorNear(Mk("G9") + 20)); yield return Until(() => game.Trial.HasResults, 40f); yield return Wait(1.5f);
            Capture("menu-results"); Line($"  [results] {string.Join(" | ", game.ResultsItems)}, selected {game.ResultsIndex}");
            Clear(); Save();
        }

        // the scripted walk, with the player's own buttons (the process ends at the end: exit code 0)
        IEnumerator MenuWalk()
        {
            scenario = "menuwalk"; Clear(); coast = true; game.ShowTitle(); yield return WaitReal(.5f);
            var steps = new List<string>(); bool ok = true;
            void Step(string what, bool pass) { steps.Add($"{what}: {(pass ? "ok" : "FAILED")}"); ok &= pass; Line($"  {what}: {(pass ? "ok" : "FAILED")} (page {game.MenuPage}, row \"{(game.MenuIndex < game.Rows.Count ? game.Rows[game.MenuIndex].Text : "")}\")"); }
            IEnumerator P(Action a) { a(); yield return WaitReal(.15f); }
            IEnumerator To(string label) { int guard = 0; while (game.MenuIndex < game.Rows.Count && !game.Rows[game.MenuIndex].Text.StartsWith(label) && guard++ < 20) yield return P(() => input.sMove = 1); }
            Step("title shown", game.MenuPage == Game.Page.Title && game.MenuOpen && Time.timeScale == 0);
            yield return P(() => input.sConfirm = true); Step("A on the title -> main menu", game.MenuPage == Game.Page.Main);
            yield return To("Race"); yield return P(() => input.sConfirm = true); Step("Race -> the course page", game.MenuPage == Game.Page.Course);
            int c0 = game.MenuCourse; yield return P(() => input.sSide = 1); Step($"right: course {game.Courses[c0].courseName} -> {game.Courses[game.MenuCourse].courseName}", game.MenuCourse != c0);
            yield return P(() => input.sSide = -1); Step("left: back to " + game.Courses[game.MenuCourse].courseName, game.MenuCourse == c0);
            yield return P(() => input.sConfirm = true); Step("A -> choose your racer", game.MenuPage == Game.Page.Pick);
            yield return To("Go"); yield return P(() => input.sConfirm = true); yield return Wait(.5f);
            Step($"Go -> the race ({game.Courses[game.CourseIndex].courseName}, race {game.RaceMode})", !game.MenuOpen && game.RaceMode && Time.timeScale == 1);
            yield return Until(() => !v.Holding, 10f); yield return Wait(1f);
            yield return P(() => input.sMenu = true); Step("Menu in the race -> pause", game.MenuPage == Game.Page.Pause && Time.timeScale == 0);
            yield return To("Quit to menu"); yield return P(() => input.sConfirm = true); Step("Quit to menu -> main menu", game.MenuPage == Game.Page.Main && game.InFront);
            yield return To("Settings"); yield return P(() => input.sConfirm = true); yield return To("Audio"); yield return P(() => input.sConfirm = true);
            yield return To("Music"); int m0 = Settings.Volume("music");
            yield return P(() => input.sSide = -1); int m1 = Settings.Volume("music"); string row1 = game.Rows[game.MenuIndex].Text;
            yield return P(() => input.sSide = -1); int m2 = Settings.Volume("music");
            yield return P(() => input.sSide = 1); int m3 = Settings.Volume("music"); string row3 = game.Rows[game.MenuIndex].Text;
            MenuShot("walk-audio");
            Step($"left / left / right on Music: {m0 * 10} % -> {m1 * 10} -> {m2 * 10} -> {m3 * 10} %, the row shows \"{row1}\" then \"{row3}\"", m1 == m0 - 1 && m2 == m0 - 2 && m3 == m0 - 1 && row3.Contains($"{m3 * 10} %"));
            yield return P(() => input.sBack = true); yield return To("Gameplay"); yield return P(() => input.sConfirm = true); yield return To("AI difficulty");
            var d0 = Settings.Difficulty; yield return P(() => input.sSide = 1); var d1 = Settings.Difficulty; yield return P(() => input.sSide = -1); var d2 = Settings.Difficulty;
            MenuShot("walk-gameplay");
            Step($"right / left on AI difficulty: {d0} -> {d1} -> {d2}", d1 != d0 && d2 == d0);
            yield return P(() => input.sBack = true); yield return P(() => input.sBack = true); Step("B, B -> main menu", game.MenuPage == Game.Page.Main);
            yield return To("Quit game"); yield return P(() => input.sConfirm = true); Step("Quit game -> Quit page, No selected", game.MenuPage == Game.Page.Quit && game.Rows[game.MenuIndex].Text.StartsWith("No"));
            Line($"MENU WALK {(ok ? "PASS" : "FAIL")}: {steps.Count} steps");
            yield return P(() => input.sMove = 1);
            Line($"  choosing \"{game.Rows[game.MenuIndex].Text}\" now; the process must end with exit code 0");
            Line($"audio output level (AudioListener.GetOutputData, all frames): peak {meter.MaxPeakSeen:0.000000}, AudioListener.volume {AudioListener.volume}");
            Save();
            input.sConfirm = true; yield return Steps(2);
            float until = Time.realtimeSinceStartup + 10; while (Time.realtimeSinceStartup < until) yield return null;
            Line("QUIT FAIL: still running 10 s after Yes"); Save(); Application.Quit(3);
        }

        // ---------------- item 2: the crowd's level ----------------
        static readonly Dictionary<AudioClip, float> clipRms = new Dictionary<AudioClip, float>();
        static float ClipRms(AudioClip c)
        {
            if (!c) return 0; if (clipRms.TryGetValue(c, out var r)) return r;
            r = .16f; if (c.loadType != AudioClipLoadType.Streaming) { var d = new float[c.samples * c.channels]; if (c.GetData(d, 0)) { double s = 0; foreach (var x in d) s += x * x; r = (float)Math.Sqrt(s / Math.Max(1, d.Length)); } }
            clipRms[c] = r; return r;
        }
        IEnumerator MixCheck()
        {
            scenario = "mix"; Clear(); Settings.Difficulty = Difficulty.Normal;
            float Db(float x) => 20 * Mathf.Log10(Mathf.Max(1e-5f, x));
            var buf = new float[1024];
            float Rms(AudioSource s)
            {
                if (!s || !s.isPlaying || !s.clip) return 0; var c = s.clip; float x = 0;
                if (c.loadType == AudioClipLoadType.Streaming || !c.GetData(buf, Mathf.Clamp(s.timeSamples, 0, Mathf.Max(0, c.samples - buf.Length / c.channels - 1)))) return .16f * s.volume;
                foreach (var b in buf) x += b * b; return Mathf.Sqrt(x / buf.Length) * s.volume;
            }
            float Att(AudioSource s) { if (!s || s.spatialBlend < .5f) return 1; float d = Vector3.Distance(s.transform.position, cam.transform.position); return Mathf.Clamp01(1 - (d - s.minDistance) / Mathf.Max(1, s.maxDistance - s.minDistance)); }
            Line($"MIX (item 2): default volumes: Music {Settings.Music * 100:0} %, Voices {Settings.Voices * 100:0} %, Engines {Settings.Engines * 100:0} %, Effects {Settings.Effects * 100:0} %, Crowd {Settings.Crowd * 100:0} % (x{Settings.CrowdGain:0.00} = {Db(Settings.CrowdGain):0.0} dB under build 15's crowd); the run is muted (would-be levels: each clip's signal x its source volume x the 3D roll-off at the camera)");
            foreach (int course in new[] { 0, 1, 2 })   // Round 15: Big Top Breakdown too
            {
                game.StartCourse(course, true); var caller = Caller.Instance; var snd = v.GetComponent<VehicleSound>();
                var pilot = v.gameObject.AddComponent<RivalPilot>(); pilot.rival = new Rival { name = "Abi (AI)", lane = 0, brakeMargin = .9f, boostMeter = .6f, boostStraight = 9f, seed = 77 }; pilot.AlwaysOn = true; aiDrives = true;
                // the grid (the start stands' murmur), GO (the roar), the lap, the finish
                var music = new List<float>(); var engine = new List<float>(); var barker = new List<float>(); var bedGrid = new List<float>(); var bedLap = new List<float>(); float bufT = 0;
                int logged0 = SoundLog.Entries.Count;
                while (v.Holding) { bedGrid.Add(Crowd.All.Sum(cs => Rms(cs.Bed) * Att(cs.Bed))); music.Add(Rms(CourseAudio.Instance.MusicSource)); yield return null; }
                float go = Time.time; Time.timeScale = 3;
                while (!v.Finished && Time.time - go < 300f)
                {
                    if (Time.time > bufT) { bufT = Time.time + .25f; music.Add(Rms(CourseAudio.Instance.MusicSource)); engine.Add(Rms(snd ? snd.EngineSource : null)); if (caller.BarkerTalking) barker.Add(Rms(caller.Voice)); bedLap.Add(Crowd.All.Sum(cs => Rms(cs.Bed) * Att(cs.Bed))); }
                    yield return null;
                }
                Time.timeScale = 1; yield return Until(() => game.Trial.HasResults, 6f); yield return Wait(2f);
                StopAi();
                // the crowd's one-shots this race (GameSounds' finish / "ooh" / "aww", the stands' cheers and roars), from the sound log
                var crowdShots = SoundLog.Entries.Skip(logged0).Where(e => e.kind == "crowd").ToList();
                float shotLevel = 0; var shotNames = new List<string>();
                foreach (var e in crowdShots)
                {
                    var clip = Resources.Load<AudioClip>("Audio/Sfx/" + e.what.Split(' ').Last());
                    float vol = e.what.Contains("roar") || e.what.Contains("big") ? .9f : .75f; if (e.what.StartsWith("to_")) vol = .6f;
                    float lvl = ClipRms(clip) * vol * Settings.CrowdLevel; shotLevel = Mathf.Max(shotLevel, lvl); shotNames.Add($"{e.what} {Db(lvl):0.0}");
                }
                float mus = music.Count > 0 ? music.Average() : 0, eng = engine.Count > 0 ? engine.Average() : 0, bar = barker.Count > 0 ? barker.Average() : 0, bg = bedGrid.Count > 0 ? bedGrid.Average() : 0, bl = bedLap.Count > 0 ? bedLap.Max() : 0;
                bool pass = Db(shotLevel) < Db(eng) - 3 && Db(bl) < Db(eng) - 6 && Db(shotLevel) < Db(bar) - 10 && Db(shotLevel) < Db(mus) - 6;
                Line($"MIX {game.Courses[course].courseName} {(pass ? "PASS" : "CHECK")}: music {Db(mus):0.0} dBFS, player's engine {Db(eng):0.0}, Barnaby {Db(bar):0.0} ({barker.Count} samples while he talks); crowd: the stands' murmur on the grid {Db(bg):0.0} (build 15: {Db(bg / Settings.CrowdGain):0.0}), loudest passing a stand {Db(bl):0.0} (build 15: {Db(bl / Settings.CrowdGain):0.0}), loudest cheer / finish sound {Db(shotLevel):0.0} (build 15: {Db(shotLevel / Settings.CrowdGain):0.0}); the crowd's loudest is {Db(eng) - Db(shotLevel):0.0} dB under the engine, {Db(bar) - Db(shotLevel):0.0} dB under Barnaby, {Db(mus) - Db(shotLevel):0.0} dB under the music");
                Line($"  crowd sounds this race ({crowdShots.Count}): {string.Join(", ", shotNames)}");
            }
            Line($"  the Crowd row: Settings > Audio > Crowd, {Settings.Crowd * 100:0} % (left / right in steps of 10 %, saved in settings.json \"crowdVol\")");
            Clear(); Save();
        }

        // ---------------- item 3: the Scrambler's bubble ----------------
        IEnumerator BubbleCheck()
        {
            scenario = "bubble"; Clear(); coast = true;
            string design = VehicleDesigns.Names.Contains("Scrambler") ? "Scrambler" : VehicleDesigns.Names.FirstOrDefault(n => VehicleDesigns.Find(n)?.CanopyOf(Mode.Dive) == "sphere");
            if (design == null) { Line("BUBBLE FAIL: no design with a sphere on its sub"); yield break; }
            game.UseDesign(design); Lineup.Apply(game);
            // in the race: underwater (the Gateway pool), chase, first person, a side and a front camera
            BeginForm(Mode.Dive); yield return Wait(2f);
            var sub = v.Forms.ModelOf(Mode.Dive); var bs = sub ? sub.GetComponentInChildren<BubbleSphere>(true) : null;
            if (!bs) { Line($"BUBBLE FAIL: {design}'s sub has no BubbleSphere ({(VehicleDesigns.OldBubble ? "-oldBubble" : "?")})"); yield break; }
            var ctr = bs.transform.position; float R = bs.Radius * bs.transform.parent.lossyScale.x / Mathf.Max(1e-4f, bs.transform.parent.parent ? 1 : 1);
            R = bs.Radius * v.Forms.Pivot.lossyScale.x;
            // inside: every bone of the rider, the grips, the bike's mesh extents (each mesh's bounds: the centres of its six faces)
            var pts = new List<(string what, Vector3 p)>();
            foreach (var sk in v.GetComponentsInChildren<SkinnedMeshRenderer>()) foreach (var b in sk.bones) if (b) pts.Add(("bone " + b.name, b.position));
            foreach (var t in v.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("Grip")) pts.Add((t.name, t.position));
            foreach (var mf in sub.GetComponentsInChildren<MeshFilter>()) { if (!mf.sharedMesh || mf.GetComponentInParent<BubbleSphere>()) continue; var mb = mf.sharedMesh.bounds; foreach (var d in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back }) pts.Add(($"{mf.name} {d}", mf.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, d)))); }
            var worst = pts.OrderByDescending(x => (x.p - ctr).magnitude).First(); float wr = (worst.p - ctr).magnitude / R; int outside = pts.Count(x => (x.p - ctr).magnitude > R);
            Line($"BUBBLE {(outside == 0 ? "PASS" : "FAIL")} ({design}): a whole sphere, radius {R:0.00} m, centre {Vector3.Dot(ctr - v.transform.position, v.transform.up):0.00} m over the vehicle's origin; {pts.Count} points (the rider's bones, the grips, the bike's mesh extents): {outside} outside; the furthest \"{worst.what}\" at {wr * 100:0} % of the radius ({(1 - wr) * R * 100:0} cm clear); chrome ring at its equator {(bs.Ring ? "yes" : "no")}; refraction cameras {BubbleSphere.RefractCameras}");
            var glassR = bs.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "glass");
            Line($"  glass renderer: {(glassR ? $"enabled {glassR.enabled}, active {glassR.gameObject.activeInHierarchy}, world size {glassR.bounds.size.x:0.00} x {glassR.bounds.size.y:0.00} x {glassR.bounds.size.z:0.00} m, grown {bs.Grown:0.00}, sphere lossy {bs.transform.lossyScale.x:0.000}, glass lossy {glassR.transform.lossyScale.x:0.000}, material {glassR.sharedMaterial?.shader?.name}" : "none")}");
            var views = cam.GetComponent<CameraViews>();
            views.SetView(CameraViews.View.Chase); yield return Wait(.6f); Capture("sub-chase-underwater");
            views.SetView(CameraViews.View.FirstPerson); yield return Wait(.6f); Capture("sub-first-person");
            views.SetView(CameraViews.View.Chase); yield return Wait(.3f);
            var side = new GameObject("side cam").AddComponent<Camera>(); side.CopyFrom(cam); side.enabled = false;
            void From(Vector3 local, string name) { var p = v.transform.TransformPoint(local); side.transform.SetPositionAndRotation(p, Quaternion.LookRotation(ctr - p, Vector3.up)); side.fieldOfView = 40; Capture(name, side); }
            From(new Vector3(6.5f, .6f, 0), "sub-side-underwater"); From(new Vector3(0, .8f, 7f), "sub-front-underwater");
            // in the Garage (the sky behind, easier to see): side, front, three-quarter, and the change into the sub
            game.ShowGarage(); yield return WaitReal(.4f); var gar = Garage.Instance; gar.SetDesign(design); gar.SetForm(Mode.Drive, false); yield return WaitReal(.3f);
            gar.SetForm(Mode.Dive, true); float t0 = Time.unscaledTime;
            foreach (float at in new[] { .25f, .36f, .48f, .62f, .9f }) { while (Time.unscaledTime - t0 < at) yield return null; gar.Pose(35); Capture($"garage-change-{at * 1000:0}ms"); }
            foreach (var (yaw, n) in new[] { (90f, "side"), (0f, "front"), (35f, "three-quarter"), (180f, "back") }) { gar.Pose(yaw); yield return WaitReal(.15f); Capture("garage-sub-" + n); }
            Line($"  bubble grows: {BubbleSphere.Grows} times this run (each change into the sub: the ring first, then the glass closes over the bike in 0.45 s)");
            game.CloseMenu(); UnityEngine.Object.Destroy(side.gameObject); game.UseDesign(VehicleDesigns.Default); Lineup.Apply(game);
            Clear(); Save();
        }

        // ---------------- item 4: no shaking after the race ----------------
        IEnumerator FinishCamCheck()
        {
            scenario = "finishcam"; Line($"FINISH CAMERA (item 4){(ChaseCam.OldFinish ? " BUILD 15's (-oldFinish)" : "")}: each course raced to the line from the last anchors (the AI behind), the camera logged for 20 s after the finish (frames next to a hitch, over twice the median frame time, left out); jitter = how far each frame's position / aim is from where the frames either side put it at that frame's time (a shake shows as jitter; a smooth move, at any frame rate, does not)");
            bool allOk = true;
            foreach (int course in new[] { 0, 1, 2 })   // Round 15: Big Top Breakdown too
                foreach (var view in new[] { CameraViews.View.Chase, CameraViews.View.FirstPerson })
                {
                    Clear(); game.StartCourse(course, true); var c = game.Courses[course];
                    float g9 = c.marks != null && c.marks.ContainsKey("G9") ? c.marks["G9"] : Mk("G9");
                    var views = cam.GetComponent<CameraViews>(); views.SetView(view);
                    yield return Until(() => !v.Holding, 8f);
                    v.Begin(c, c.anchors[AnchorNear(g9 + 20)]); smart = true; boost = true;   // boost on the way in: the boost shake must stop at the line too
                    // a rival right behind (a bump at the finish)
                    yield return Until(() => v.Finished, 60f);
                    Clear(); input.Scripted = false;   // after the finish the player's own input path (nothing pressed)
                    var pos = new List<Vector3>(); var fwd = new List<Vector3>(); var dts = new List<float>(); var ts = new List<float>(); float t0 = Time.time; bool shot = false;
                    while (Time.time - t0 < 20f) { yield return new WaitForEndOfFrame(); pos.Add(cam.transform.position); fwd.Add(cam.transform.forward); dts.Add(Time.deltaTime); ts.Add(Time.time - t0); if (!shot && Time.time - t0 > 4f) { shot = true; Capture($"after-finish-{course}-{view}"); } }
                    input.Scripted = true;
                    var jl = new List<float>(); var al = new List<float>(); float jp = 0, ja = 0, jAt = 0, maxSpeed = 0;
                    var sortedDt = new List<float>(); for (int i = 1; i < ts.Count; i++) sortedDt.Add(ts[i] - ts[i - 1]); sortedDt.Sort(); float med = sortedDt.Count > 0 ? sortedDt[sortedDt.Count / 2] : .01f; int over = 0, skipped = 0;
                    for (int i = 1; i < pos.Count - 1; i++)
                    {
                        float span = ts[i + 1] - ts[i - 1]; if (span <= 1e-5f || ts[i] - ts[i - 1] <= 1e-5f) continue;
                        if (ts[i] - ts[i - 1] > 2 * med || ts[i + 1] - ts[i] > 2 * med) { skipped++; continue; } float u = (ts[i] - ts[i - 1]) / span;
                        float e = (pos[i] - Vector3.Lerp(pos[i - 1], pos[i + 1], u)).magnitude, a = Vector3.Angle(fwd[i], Vector3.Slerp(fwd[i - 1], fwd[i + 1], u));
                        jl.Add(e); al.Add(a); if (e > .01f) over++; if (e > jp) { jp = e; jAt = ts[i]; } ja = Mathf.Max(ja, a); maxSpeed = Mathf.Max(maxSpeed, (pos[i] - pos[i - 1]).magnitude / (ts[i] - ts[i - 1]));
                    }
                    jl.Sort(); al.Sort(); float j99 = jl.Count > 0 ? jl[(int)(jl.Count * .99f)] : 0, a99 = al.Count > 0 ? al[(int)(al.Count * .99f)] : 0;
                    bool ok = jp < .02f && ja < .1f; allOk &= ok;   // under 2 cm / 0.1 deg off the smooth path in every frame
                    Line($"  {c.courseName,-16} {CameraViews.Names[(int)view],-12} {(ok ? "STEADY" : "SHAKY")}: {pos.Count} frames, jitter max {jp * 100:0.00} cm (at {jAt:0.00} s) / {ja:0.000} deg, 99th pct {j99 * 100:0.000} cm / {a99:0.000} deg, {over} frames over 1 cm ({skipped} next to a hitch left out); camera speed at most {maxSpeed:0.0} m/s; car {v.Speed:0.0} m/s 20 s after the line; finish camera {(cam.GetComponent<ChaseCam>().FinishCamOn ? "on" : "off")}");
                }
            Line($"FINISH CAMERA {(allOk ? "PASS" : "FAIL")}: no shake after the finish on both courses, chase and first person");
            cam.GetComponent<CameraViews>().SetView(CameraViews.View.Chase); Clear(); Save();
        }

        // ---------------- item 5: the Ferris wheel on the main route, LB on the ramp ----------------
        IEnumerator WheelCheck()
        {
            scenario = "wheel"; int tries = ArgInt("-tries", 10);
            var cc = game.Courses[1]; float lip = CoasterCoast.Marks["jM"]; var w = CoasterCoast.WheelCentre; float hole = CoasterCoast.WheelR - 1.3f;
            Line($"WHEEL (item 5){(CoasterCoast.OldWheel ? " BUILD 15's (-oldWheel)" : "")}: the Midway Jump {(CoasterCoast.OldWheel ? "is a choice (LB)" : "launches every car into the plane")}; the wheel's centre {w.x - CoasterCoast.O.x:0} m from the boardwalk's line, {w.y:0} m up; the line through it {(CoasterCoast.OldWheel ? "-" : $"{cc.PointAt(CoasterCoast.Marks["wheel"], out _).y:0} m up")}; the open hole {hole:0.0} m radius");
            int through = 0, clean = 0; var times = new List<float>();
            for (int k = 0; k < tries; k++)
            {
                Clear(); game.StartCourse(1, false); yield return Until(() => !v.Holding, 6f);
                var h = NewHuman(500 + k); float speed = new[] { 45f, 55f, 65f, 75f }[k % 4], lane = h.R(-5, 5), head = h.R(-5, 5);
                PlaceBefore(lip - 140, speed, lane, head); UseHuman(h);
                if (CoasterCoast.OldWheel) h.wingsAt = .5f;
                int r0 = v.Resets; float t0 = Time.time, passR = -1; bool pressed = false, shotA = false, shotB = false, crossed = false; var prev = v.Body.position;
                while (Time.time - t0 < 40f && v.GatesPassed < 2)
                {
                    var p = v.Body.position;
                    if (CoasterCoast.OldWheel && !pressed && v.WingsOpen && v.GroundedWheels == 0) { input.sWings = true; pressed = true; }
                    if (!crossed && prev.z < w.z && p.z >= w.z) { crossed = true; float u = (w.z - prev.z) / Mathf.Max(1e-4f, p.z - prev.z); var at = Vector3.Lerp(prev, p, u); passR = new Vector2(at.x - w.x, at.y - w.y).magnitude; }
                    if (k == 0 && !shotA && v.Progress > lip - 60) { shotA = true; Capture("approach"); }
                    if (k == 0 && !shotB && p.z > w.z - 20) { shotB = true; Capture("at-the-wheel"); }
                    if (k == 0 && shotB && crossed && p.z > w.z + 8 && p.z < w.z + 12) Capture("through-the-wheel");
                    prev = p; yield return null;
                }
                bool thru = crossed && passR >= 0 && passR < hole; if (thru) through++;
                bool ok = thru && v.GatesPassed >= 2 && v.Resets == r0; if (ok) { clean++; times.Add(Time.time - t0); }
                Line($"  try {k + 1} {(ok ? "THROUGH" : thru ? "through (then a problem)" : "MISSED")}: {(h.keys ? "keys" : "stick")}, {speed:0} m/s, lane {lane:+0.0;-0.0} m, heading {head:+0;-0} deg, late {h.delay:0.00} s: passed the wheel's plane {(passR >= 0 ? $"{passR:0.0} m from its centre" : "never")}, G1 {(v.GatesPassed >= 2 ? $"in {Time.time - t0:0.0} s" : "not reached")}, resets {v.Resets - r0}{(v.Resets > r0 ? $" ({v.LastResetReason})" : "")}, mode at G1 {v.Mode}");
            }
            Line($"WHEEL HUMAN-LIKE {(clean >= tries ? "PASS" : "FAIL")}: through the wheel {through} of {tries}, clean to G1 {clean} of {tries}; mean {(times.Count > 0 ? times.Average() : 0):0.0} s from 140 m before the lip to G1");
            // the AI (Normal) through the wheel
            int aiThrough = 0, aiRuns = 6;
            for (int k = 0; k < aiRuns; k++)
            {
                Clear(); game.StartCourse(1, false); yield return Until(() => !v.Holding, 6f); v.Begin(cc, cc.anchors[cc.anchors.FindIndex(a => a.progress > lip - 200)]);
                DriveWith(""); var prev = v.Body.position; float t0 = Time.time, passR = -1; int r0 = v.Resets;
                while (Time.time - t0 < 40f && v.GatesPassed < 2) { var p = v.Body.position; if (passR < 0 && prev.z < w.z && p.z >= w.z) { float u = (w.z - prev.z) / Mathf.Max(1e-4f, p.z - prev.z); var at = Vector3.Lerp(prev, p, u); passR = new Vector2(at.x - w.x, at.y - w.y).magnitude; } prev = p; yield return null; }
                if (passR >= 0 && passR < hole && v.GatesPassed >= 2 && v.Resets == r0) aiThrough++;
                StopAi();
            }
            Line($"WHEEL AI: through the wheel and on to G1 with no reset {aiThrough} of {aiRuns}");
            // every choice jump: LB on the ramp (pre-armed) and LB late in the air
            int lbOk = 0, lbTotal = 0;
            foreach (var j in ParadisePlunge.Jumps.Where(x => x.choice))
                foreach (var how in new[] { "ramp", "air-1.2s" })
                {
                    lbTotal++; Clear(); game.StartCourse(0, false); yield return Until(() => !v.Holding, 6f);
                    PlaceBefore(j.lip - 80, 60, 0, 0); smart = true;
                    bool pressed = false, rampSeen = false; float leftLip = -1, flewAt = -1, t0 = Time.time; int armed0 = v.WingsArmedPresses;
                    while (Time.time - t0 < 12f && flewAt < 0)
                    {
                        if (v.OnChoiceRamp) rampSeen = true;
                        if (leftLip < 0 && v.Progress > j.lip && v.GroundedWheels == 0) leftLip = Time.time;
                        if (!pressed && how == "ramp" && v.OnChoiceRamp && v.Progress > j.lip - 30) { input.sWings = true; pressed = true; Capture($"lb-ramp-{j.name.Replace(' ', '-')}"); }
                        if (!pressed && how != "ramp" && leftLip > 0 && Time.time - leftLip > 1.2f) { input.sWings = true; pressed = true; }
                        if (v.Mode == Mode.Fly && flewAt < 0) flewAt = Time.time;
                        yield return null;
                    }
                    bool ok = flewAt > 0 && (how != "ramp" || (v.WingsArmedPresses > armed0 && flewAt - leftLip < .4f));
                    if (ok) lbOk++;
                    Line($"  {j.name}, LB {(how == "ramp" ? "on the ramp (30 m before the lip)" : "1.2 s into the air")}: {(ok ? "WINGS" : "NO WINGS")}; prompt on the ramp {rampSeen}; armed {v.WingsArmedPresses - armed0}; wings {(flewAt > 0 && leftLip > 0 ? $"{flewAt - leftLip:0.00} s after leaving the lip" : "never")}; on {string.Join("+", v.ShortcutsTaken)}");
                }
            Line($"CHOICE LB {(lbOk == lbTotal ? "PASS" : "FAIL")}: {lbOk} of {lbTotal} (every choice jump: pressed on the ramp, and late in the 1.5 s air window)");
            Clear(); Save();
        }
    }
}
