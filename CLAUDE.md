# ShiftStorm — Project Context & Rules

Tee'd Off universe spin-off: one machine changes between car, boat, plane and
submarine during a race. Current target: Paradise Plunge proof of concept.

## Tech Stack
- **Engine:** Unity 6000.6.1f1, URP, Input System only (same as Chastain Rush /
  Woodstock Rush so systems can be ported).
- **Repo:** `C:\Users\danmo\ShiftStorm`, remote
  `https://github.com/danemoll-jpg/ShiftStorm.git`, branch `main`.
- **Releases / update catalog:** `https://github.com/danemoll-jpg/ShiftStorm-Releases`
  (separate from the code repo).
- **Read-only references (copy from, never edit):**
  - `C:\Users\danmo\chastain_rush` — launcher reference implementation; car and
    plane controllers (separate vehicles, no shape-changing vehicle).
  - `C:\Users\danmo\Tee-dOff` — art style, characters, rigs, voices, music,
    environments to reuse.
  - `C:\Users\danmo\Hostile-Horizon` — spaceship game; sound effects to reuse.
- **3D models:** Blender, scripted where possible (`Tools/Blender/*.py`).
  Simple hard-surface things are built by script. **Vehicles are not**
  (Dan, 2026-11-13: script-built vehicles weren't good enough in Woodstock
  Rush / Chastain Rush): they come from Tripo, made by Dan, one model per form. People, creatures and organic props: **Tripo, made by Dan by hand in
  the Tripo web app** — there are no Tripo API credits, so never call a Tripo
  API. Meshy is not used any more.
- **Animation:** Mixamo (Dan rigs and downloads; FBX, Humanoid in Unity).
- **Music:** Suno (long), ElevenLabs (short stings). **SFX:** ElevenLabs.
- All paid licenses; generated assets can be used freely. Don't ask about the
  toolchain.

## Art pipeline for characters / creatures / organic props
1. Code writes the image prompts in `Docs/MODELS-NEEDED.md`: full body, A- or
   T-pose, front view (plus side/back where useful for Tripo multiview), plain
   light background, no cropping, no props; design note; Tripo settings to use
   (low-poly vs detailed, textured, FBX or GLB); exact file name and folder to
   save the download to; animation list. **Stop and tell Dan.**
2. Dan has the images drawn, makes the models in Tripo and saves them where
   the doc says.
3. Code cleans up in Blender (scale, origin, facing, LODs), renders previews,
   **stops** to show Dan. A bad model → say which and what to change for a
   second Tripo try.
4. Dan rigs in Mixamo and downloads the listed animations (real Mixamo search
   names, what each is for).
5. Code imports, wires animations, replaces placeholders, keeps the old model
   as a fallback.
Keep each round's model list short — each one is Dan's time.

## Launcher (copy from chastain_rush)
- Copy `Launcher/` (core.cpp/hpp, launcher.cpp, store.cpp, core_cli.cpp,
  vendor/ json + miniz, launcher.rc, icon), `LauncherBridge.cs`, and `Tools/`:
  `Build-Launcher.ps1` (VS 18; SDK from Racer's `Builds/LauncherSDK`),
  `New-LauncherKey.py`, `Prepare-LauncherRelease.py`,
  `Prepare-ComponentUpdate.py`, `Package-LauncherStarter.py`,
  `Publish-LauncherRelease.py`; rewrite `Docs/Starter/README-LAUNCHER.md`.
- Launcher name: `ShiftStormLauncher.exe`. Signing key made once, kept in
  `Builds/PublisherPrivate/` (git-ignored) — never delete, print or replace it.
- **Publish a release every round** (Dan tests on his Steam Deck: Proton
  Experimental, non-Steam game).

## Testing rules (every run)
1. **Always muted:** `-mute` flag sets master volume / `AudioListener.volume`
   to 0 from the first frame. Report the measured audio output level (must be
   0). Sound checks: unmute only for that step, as briefly as possible, and say
   which step and for how long.
2. **Never steal focus:** prefer batch mode / off-screen rendering; otherwise
   minimised, not activated, run-in-background on. **A test that needs a
   visible or focused window: stop and ask Dan first.**
3. One check per fix, in the built exe, played the way Dan plays; look at the
   player-view pictures yourself.
4. One heavy job at a time, Below Normal priority. Clean the disk at round end.
5. Performance target: **Steam Deck, 60 fps (16.7 ms)**; inspect transition
   frame spikes separately.

## Code Style & Architecture
- One racing framework, four handling modes (Drive, Boat, Fly, Dive). Position,
  forward intent, boost state and race progress survive every change; speed is
  blended into the receiving mode's range.
- Transformation triggers are separate from race-progress gates.
- A change fires once per zone; a new valid zone is needed before the next.
- Tuning values live in data (ScriptableObjects), not in the track.
- Code copied from chastain_rush: note in `Docs/REUSE-AUDIT.md` what was copied
  and what was changed.

## Controls
Same inputs in every form (`Assets/Scripts/Vehicle/ShiftInput.cs`, Input System only).

| Action | Controller | Keyboard |
|---|---|---|
| Accelerate | RT | W / Up |
| Brake / reverse | LT | S / Down |
| Steer (banks in the air) | Left stick X | A / D, Left / Right |
| Climb / dive (air, underwater) | Left stick Y: pull back = up | I = up, K = down |
| Boost (one shared meter) | A | Space |
| Secondary: drift (car) / tighter carve (boat) | RB | Left Shift |
| Wings: only in the first 1 s off the Lake Jump's lip (HUD "LB: FLY"); nothing anywhere else | LB | E |
| Camera view: Chase / Far chase / First person / Front (per player, saved) | X | V |
| Look around (pulled back = look behind) | Right stick | - |
| Reset to last safe anchor | Y | R |
| Invert climb / dive (per player, saved in `settings.json`; also Menu > Settings) | View (Back) | F2 |
| Previous / next course (strip, 4 lanes) | D-pad left / right | F3 / F4 |
| FPS + speed readout on / off (saved in `settings.json`; also Menu > Settings) | R3 (right stick click) | F1 |
| Full screen / windowed (saved, `display.json`) | Menu > Settings > Screen | Alt+Enter |
| Menu: Resume, Restart, Race, Time Trial, test courses, 2 Players, Garage, Settings, Quit game | Menu (Start), either player | Esc |
| 2 Players: player 2 joins (on Menu > 2 Players) | A on a second controller | Enter (if player 1 is on a pad) |
| Menu move / choose / back | D-pad up-down / A / B | Up-Down (W-S) / Enter or Space / Backspace |

Menu > Settings: AI difficulty Easy / Normal / Hard (default Easy, saved in `settings.json`), Invert flight / dive controls On / Off, Screen, FPS readout, Music / Effects / Voices volume (A steps 100 -> 80 ... 0 -> 100 %, saved). Quit game asks "No / Yes" (No is preselected) and exits
with code 0 (the launcher then closes too). The results screen offers Race again / Menu / Quit game. After the finish, Reset (Y / R)
starts the same course again the same way (race or time trial). Race = Paradise Plunge against 3 AI rivals from a 2 x 2 grid (Abi at the
back); Time Trial = alone, the only mode that saves a best lap. The game starts in Race; `-trial` starts a time trial (checks always do).

## Commands
Unity 6000.6.1f1 at `C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe`; Blender at `F:\blender\blender.exe`.
Unity batch runs need the sandbox off (its package manager uses a named pipe).
- Project setup (URP, Input System only, layers, materials, tuning assets, scene; safe to repeat):
  `powershell -File Tools/Unity-Batch.ps1 -Log local/logs/setup.log -Method ShiftStorm.EditorTools.ProjectSetup.Run`
- Vehicle model: `F:\blender\blender.exe -b -P Tools/Blender/shiftstorm_vehicle.py -- C:/Users/danmo/ShiftStorm`
- Player build (`Builds/ShiftStorm/ShiftStorm.exe`):
  `powershell -File Tools/Unity-Batch.ps1 -Log local/logs/build.log -Method ShiftStorm.EditorTools.BuildPlayer.Run -Extra '-buildVersion','0.1.0'`
- Muted, windowless check in the built exe (`-batchmode -mute`, Below Normal; report + pictures in the folder):
  `powershell -File Tools/Run-Check.ps1 -Check round2 -Out local/round2/check -Extra '-saveDir','local/round2/save'`
  (`round2` = `lap` + `skip` + `abi`; `round1` = `strip` + `lanes` + `edge`; `round2b` = `screen` + `nav` + `lap` + `skip` + `slowfps`;
  `all` = all three; `best` = a second process reading the best time saved by `lap`, same `-saveDir`). Round 3: `round3` = `settings` +
  `race` + `shortcuts`; `settings2` (a second process: invert survived), `quit` / `quitresults` (exit code 0), `waterfall` (item 3's
  recorder, CSV per run; `-before` = build 3's behaviour), `race` (`-races N`), `shortcuts`, `soak` (`-laps 100 -timeScale 3`, use
  `-TimeoutMinutes 150`). Round 4: `round4` = `scan` (colliders inside the route's width, start-line pictures) + `walls` (10 / 25 / 45 deg
  hits per form, 3 s held against a wall) + `nearmiss` (near / far / wrong-form gate passes, the penalty against the most a near miss can
  save) + `starts` (25 race starts, `-starts N`) + `difficulty` (`-races N` per level); `tunnel` (`-legs N` AI runs through the Coral
  Vault tunnel + scrape runs). Round 5: `round5` = `garage` (every design x form in the Garage, mid-change pictures, Race with this
  design; `-testDesign _Test` adds the import test's design) + `lap` (now also the speed feel per form: engine pitch, wind, FOV,
  particles) + `strip`. Round 6: `round6` = `bind` (two virtual gamepads: join, per-device input, X view, per-player invert, pause from player 2, a pad
  pulled out) + `split` (two scripted players + 2 AI, a full lap, both HUDs sampled, pictures of both halves, split frame times;
  `-leftRight` for the side-by-side layout) + `views` (one lap per camera view, pictures per form and mid-change, look-behind, frames
  inside rock / on the water surface). Round 7: `round7` = `loop` (full / slow / very slow entries, pictures at 4 points in every view, 25 AI
  runs, `-loopRuns N`) + `jumps` (each plain jump with and without boost, a slow one) + `choice` (car line vs fly line timed, LB late, LB
  elsewhere) + `caller` (a race: caller lines, effects, engine and music mix per form); `stuntlaps` (`-races N` x 4 AI, grid to G2, Normal).
  Any check takes `-design <name>` (not saved). Any check takes `-difficulty Easy|Normal|Hard` (not saved) and `-noTunnel`. The launcher's hidden start-up check runs a check named in the `SHIFTSTORM_CHECK` environment variable. `nav`: a guided pilot that steers
  only by the HUD (arrow / next-gate marker) from off the line and facing backwards in the air, plus marker / arrow pictures per form. The lap check also measures render + GPU time per segment (PC numbers).
  Any run with `-mute`, `-check` or `-batchmode` is muted from the first frame and reports the measured output level.
- Game start options: `-course 0..5` (0 Paradise Plunge, 1 transition strip, 2 drive lane, 3 boat lane, 4 fly lane, 5 dive lane);
  `-race` / `-trial`; `-saveDir <dir>` puts the best-times and settings files elsewhere (checks never touch Dan's own files).
- In game: Menu (Start) / Esc opens the menu (see Controls); D-pad left / right still steps courses.
- Vehicle import (Round 5, one command, Tripo models Dan saved in `Art/Incoming/Vehicles/<Design>/<Design>_<Form>.fbx`):
  `python Tools/import_vehicles.py` (or `--design Jetstream`; takes Tripo's zips as downloaded, meshes capped at 20,000 faces;
  `import.json` "files" maps a form saved under another name; `--src local/round5/testfbx --design _Test` for the stand-in made by
  `Tools/Blender/make_test_vehicle.py`). Blender cleans each (4.0 m long, origin, facing: `import.json` "yaw", default 180; floor per
  form, Seat point, LOD1, texture) into `Assets/Resources/Vehicles/<Design>/` + `designs.json`, previews in `Docs/Checks/round5/import/`;
  then build. Never ship `_Test` (names starting `_` are hidden; delete its folder before the release build).
- Music (Round 7): Dan's Suno MP3s in `Art/Incoming/Music/` named `race_paradise`, `menu`, `theme`, `results` (his downloads are
  "ShiftStorm - Paradise Plunge / Garage / Theme / Victory.mp3", found under either name) -> `python Tools/Music/make_loops.py` (copied
  from Tee'd Off) writes the intro + seamless loop to `Assets/Resources/Audio/Music/`. Caller lines: `python Tools/Audio/caller_lines.py`
  (dry run; `--go` records missing ones with ElevenLabs in the TeedOff Barker voice). Every sound and its source: `Docs/SOUNDS.md`.
- Launcher: `powershell -File Tools/Build-Launcher.ps1` (and `-CoreOnly` for `LauncherChecks.exe`) -> `Builds/Launcher/`.
- Release (local draft, then the deliberate publish):
  `python Tools/Release-Round.py --version 0.1.0 --build 1 --notes "..."` then
  `python Tools/Publish-LauncherRelease.py Builds/Release/build-1/draft --publish --confirm-repository danemoll-jpg/ShiftStorm-Releases`
  (version and build go up every round; the starter ZIP is `ShiftStorm-<version>-Full.zip` on the release).
- Round-end disk clean-up: `python Tools/cleanup_round.py`

## Where things stand
**Always check `TODO.md` for the current objective before starting work.**

Round 1 (build 1), Round 2 (build 2, 0.2.0), Round 2b (build 3, 0.2.1), Round 3 (build 4, 0.3.0), Round 4 (build 5, 0.4.0), Round 5 (build 6, 0.5.0), Round 6 (build 7, 0.6.0) and Round 7 (build 8, 0.7.0) are delivered: four-mode vehicle, full gray-box Paradise
Plunge lap in the Jurassic Par look and sound, ordered gates, time trial, Abi in the cockpit; Round 2b: where to go (glowing gate frames,
next gold / after cyan, HUD arrow + marker + route strip, route guides in every form, direction cues from the heading against the route),
much faster, fullscreen by default.
- **Speeds (Round 5, `Resources/Tuning`):** Drive 75 m/s (boost 94, accel 30), Boat 58 (72, accel 30), Fly 92 (115), Dive 44 (55);
  +15 % on Round 2b's. Paradise Plunge is built 1.7x larger on the ground plan than build 2 (`ParadisePlunge.S`; heights unchanged):
  line 7.3 km, pilot lap ~123 s (build 5: 139 s).
- **Frame rate:** physics at a fixed 60 Hz, `Time.maximumDeltaTime` 0.25 s, so movement only slows below 4 fps.
- **Fullscreen:** `Assets/Scripts/Core/ScreenSetting.cs` (from Chastain Rush) sets borderless FullScreenWindow at the desktop's own
  resolution before the first scene, overriding Unity's remembered window; windowed only by the menu / Alt+Enter (saved in
  `display.json` in persistentDataPath). Test runs (`-mute`, `-check`, `-batchmode`, the launcher's start-up check) only log what they
  would set. The launcher passes no screen arguments.
- **Audio clips:** SFX and voice are imported with Preload Audio Data on (off caused first-play hitches); music streams.
- **Round 3 (build 4, 0.3.0):** Settings page + Quit; form changes into a faster form carry the speed's share of the old top speed,
  eased in over the change (`ShiftTuning.changeSpeedCarry`), the car is pulled onto its wheels after a change into Drive and keeps its
  forward speed on the landing, the camera's speed feel eases between forms (item 3's cause). Three AI rivals (`Race/Rival.cs`: Rocket
  Rae, Chip, Vivi; same vehicle and tuning, driven through `ShiftInput`; difficulty knobs = braking margin and boost timing; no
  rubber-banding). Shortcuts: `Race/Shortcut.cs`; the Sky Arch slit (with its updraft) is on; the Coral Vault tunnel is built but off
  (`ParadisePlunge.VaultTunnel = false`) until tuned in a window. Scrapes in the air / underwater cost speed (`ShiftTuning.scrape*`).
- **Round 4 (build 5, 0.4.0):**
  - **Walls glance you off in every form** (`Vehicle/ShiftVehicle.Walls.cs`, `ShiftTuning.wall*`): the speed into the wall is removed, the
    speed along it kept minus 4 % (shallow) to 40 % (head-on) scaled by angle, a 2.5 m/s nudge away, a car's nose turned 80 % along the
    wall, its yaw spin removed; a steep hit always slides you the route's way; pressing along a wall costs 10 %/s. The hull is frictionless.
    Hit sound (`wall_hit_1/2`, from TO) and a camera bump. The Round 3 scrape rule (`ShiftTuning.scrape*`) now applies only to the Sky Arch
    slit's buttress.
  - **Near-miss gates** (`ProgressGate.CrossedNear`, `ShiftTuning.nearMiss*`): through a gate's plane outside its frame but within one
    gate width (8-30 m) of it, in a permitted form, heading along the route: it counts with +3 s ("MISSED G4 +3 s"; in the lap time, the
    results and the race order). Far misses, the wrong form and skipped sections behave as before. Shortcut sub-gates too.
  - **AI difficulty** (`Race/Rival.cs` `AiSkill`; Menu > Settings, default Easy): pace (share of top speed held), braking, line wander,
    boost meter / straightness, small mistakes, shortcut rate; never vehicle stats, no rubber-banding. Easy ~9 % slower than the check
    pilot's no-boost lap (~140 s), Normal ~equal, Hard = Round 3's AI.
  - **Start:** the spline looped back 8 m before G0 and the promenade's barriers followed it across the road (Dan's "two beams"); fixed
    with two even control points; road width and barrier height now change smoothly (no barrier end facing traffic). Grid 11 m x 10 m,
    the AI launches row by row (0.15 s per row) and keeps its grid lane for 200 m.
  - **Coral Vault tunnel back on** (`ParadisePlunge.VaultTunnel`, `-noTunnel` turns it off): wider openings, smooth narrowing, an open
    climb-out, a stronger current; an AI that keeps hitting rock gives up its shortcut. Saves about 1.2 s (not the 2-3 s aimed for).

- **Round 5 (build 6, 0.5.0):**
  - **Speed feel:** engine pitch runs to each form's top speed (and past it on boost) + a wind layer (`VehicleSound`); camera lower and
    closer at speed, FOV to 90 (98 on boost), a boost kick (`ChaseCam`); `Vehicle/SpeedFx.cs` air streaks (player, car / plane), boat
    spray, sub bubbles; road posts every 20 m and dashed centre lines. AI on the same tuning. Easy ~20 % slower than the check pilot,
    Normal ~5-8 %, Hard as before; the AI's pace cap and mistakes start 600 m after the grid (clean launches).
  - **Swap change:** `Vehicle/FormSet.cs`: four whole models per vehicle, 0.6 s squash + quarter twirl, swapped at the midpoint with a
    burst (body-coloured panels + sparkles / spray / bubbles by form). Physics handover unchanged (t = 0). `FormAnimator` now only poses
    the gray-box model per form. Abi follows each model's Seat point (`FormSet.SeatOffset`) and squashes with it.
  - **Designs + Garage:** `Vehicle/VehicleDesigns.cs` ("Gray box" + `Resources/Vehicles/designs.json`); rivals' copies have the green
    parts recoloured; every imported form gets the game's own clear bubble over the cockpit rim (Dan's models have no glass). **Jetstream**
    (Dan's pick, Tripo, 4 forms, `Resources/Vehicles/Jetstream/`) is the default design; Tee Shot and Mudlark are parked. Menu > Garage
    (`UI/Garage.cs`): turntable, Design / Form (with the change) / Race with this design (saved, `settings.json` "design").
  - **Polished stretch** (`World/ParadisePlunge.Polish.cs`): TO-style cliff ledges, waterfalls, arch rock stacks + pterodactyl nest,
    lagoon beaches, foam, clouds; Detail layer (11) culled beyond 800 m; looks only (colliders unchanged). Jurassic fog is now linear
    190-1100 m (TO's 140-820 x 1.35).

- **Round 6 (build 7, 0.6.0):**
  - **Split screen** (`Split/SplitScreen.cs`, from Chastain Rush's): Menu > 2 Players; player 1 = the device that opened the page, player 2
    joins with A on another controller (Enter on an unused keyboard); each `ShiftInput` bound to its own device (`ShiftInput.Bind`).
    Player 2 picks Rae / Chip / Vivi (their colour and driver), 0-2 AI racers; Race or Time Trial; top / bottom (player 1 on top, default)
    or left / right (saved, `settings.json` "splitLeftRight"). One `Hud` per half (player 2's shows PAUSED; the menu is on player 1's half,
    either player works it), per-camera underwater fog and gate colours, per-player invert ("invertVertical2"). Split cuts: LOD bias x0.6,
    shadows x0.6, far clip 1150 m, Detail layer 500 m, textures one mip down; engines at 0.7 each (`VehicleSound.Mix`). A pulled-out pad pauses.
  - **Camera views** (`UI/CameraViews.cs`, from Chastain Rush's): X / V cycles Chase / Far chase / First person (Abi's eyes, her head folded
    away) / Front, per player, saved ("view", "view2"); right stick looks around. Chase now keeps out of the cave walls underwater.

- **Round 7 (build 8, 0.7.0):**
  - **Stunts** (`World/ParadisePlunge.Stunts.cs`, `Vehicle/ShiftVehicle.Stunts.cs`): the promenade is a straight stunt strip: Creek Jump,
    the loop (radius 24 m, rail physics, boost pad; too slow = slide back + reset), Lake Jump (LB / E wings in the first 1 s off the lip ->
    the "Lake flight" shortcut, wheels down before G1); Cliff Gap on the climb. Anchors before jumps restart at 40-45 m/s
    (`RecoveryAnchor.speed`). The AI decides fly / car before the lip (`AiSkill.fly`: Easy .35, Normal .5, Hard .6). Safe-line lap 125.0 s.
  - **Course art** (`ParadisePlunge.Polish2.cs`): the rest of the lap dressed; TeedOff Barker's booth at the start line.
  - **Sound** (`Core/GameSounds.cs`, `Vehicle/VehicleSound.cs` + `EngineSynth.cs`, `Core/CourseAudio.cs`; list in `Docs/SOUNDS.md`), Dan's
    four Suno songs, Music / Effects / Voices volumes. **TeedOff Barker** calls the race (`Race/Caller.cs`, clips in `Resources/Audio/Voice/barker`).
  - New sound files must be imported with Preload Audio Data on (the importer rule now sets it).

Dan reviewed build 7: "everything good so far."

Previous objective (delivered, waiting for Dan's review of build 8): **Round 7** — first the music prompts for Dan (`Docs/AUDIO-NEEDED.md`, Tee'd Off-style cartoony songs; Code chooses sound effects itself),
then a loop-the-loop and three big car jumps (one with a fly choice on LB / E), then course art for the rest of the lap, a full
sound pass (Tee'd Off + Chastain Rush + Hostile Horizon sounds) and Barnaby, "TeedOff Barker", as the race caller. Release build 8 (0.7.0). See `TODO.md`.
