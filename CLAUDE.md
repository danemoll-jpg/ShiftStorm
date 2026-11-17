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
- **Music:** Suno (long), ElevenLabs (short stings). **SFX:** ElevenLabs. **No Tee'd Off music in ShiftStorm**
  (Dan, 2026-11-15): every song is Dan's own new Suno song (remixes only if he asks). New environments too: same
  universe and reused models / systems, but not Tee'd Off's places.
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
| Wings: only on a choice jump (Paradise Plunge's Lake Jump and Cliff Gap, Big Top Breakdown's Clown Jump, Dam Shame's flip bucket on lap 1's water): from the ramp (HUD "LB: FLY"; pressed there = "WINGS READY", out at the lip) to 1.5 s into the air; nothing anywhere else | LB | E |
| Camera view: Chase / Far chase / First person / Front (per player, saved) | X | V |
| Look around (pulled back = look behind) | Right stick | - |
| Reset to last safe anchor | Y | R |
| Invert climb / dive (per player, saved in `settings.json`; also Menu > Settings) | View (Back) | F2 |
| FPS + speed readout on / off (saved in `settings.json`; also Menu > Settings) | R3 (right stick click) | F1 |
| Full screen / windowed (saved, `display.json`) | Menu > Settings > Screen | Alt+Enter |
| Title screen (Round 14): any button | A / Start | Enter / Space / Esc |
| Main menu: The Tour, Race, Time Trial, 2 Players, Garage, Settings, Quit game (B: the title); Race / Time Trial -> the course cards -> Choose your racer (driver, machine, form) -> Go | - | - |
| Pause in a race: Resume, Restart, Settings, Quit to menu (+ 2 Players setup / End 2 players while split) | Menu (Start), either player | Esc |
| Skip Buck (a briefing or a Tour scene) | A (or Menu / B) | Enter / Space (or Esc / Backspace) |
| 2 Players: player 2 joins (on Menu > 2 Players) | A on a second controller | Enter (if player 1 is on a pad) |
| Menu move / change a value / choose / back (held = repeat) | D-pad or left stick up-down / left-right / A / B | Up-Down (W-S) / Left-Right (A-D) / Enter or Space / Backspace |

Menu > Settings (Round 14: four groups, one setting per row, left / right change the value, A also steps it, B back):
- Gameplay: AI difficulty Easy / Normal / Hard (default Easy), Buck's briefings On / Short / Off, Unlock all Tour stops On / Off (default On
  while building: `Settings.UnlockTourDefault`), Save playtest logs, Open log folder.
- Audio (sliders, 10 % steps): Music, Voices (Barnaby, Buck, drivers), Engines, Effects, Crowd (Round 14; defaults 100 / 100 / 60 / 100 / 100).
- Video: Graphics Auto / Deck / Low / Medium / High (Auto = Deck on a Steam Deck, High elsewhere; 2 players one lighter), Screen, FPS readout,
  2-player screen. Controls: Invert flight / dive (per player), Camera view, the controls list.
All saved in `settings.json`. The Tour page also lists the rival challenges; Garage: Design / Form / Race with this design / Paint. Quit game
asks "No / Yes" (No is preselected) and exits with code 0 (the launcher then closes too). The results screen offers Race again / Next (the
next course, or the next Tour stop once open) / Menu. After the finish, Reset (Y / R) starts the same course again the same way, the car
rolls to a stop by itself and the camera eases out and slowly circles it (no shake). Race = against 3 AI rivals from a 2 x 2 grid (Abi at
the back); Time Trial = alone, the only mode that saves a best lap. The game starts on the title screen (Round 14); `-race`, `-trial` or
`-course N` start a course straight away (checks always do).

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
  (`round2` = `lap` + `skip` + `abi`; `round1` = `strip` + `lanes` + `edge` (Round 10: all on Paradise Plunge, the test courses are gone); `round2b` = `screen` + `nav` + `lap` + `skip` + `slowfps`;
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
  Round 8: `round8` = `dinos` (Round 8b: the lap counting dinosaurs >= 8 % of the screen height for >= 1 s per segment, set-pieces, Barnaby's dino lines, route clearance, roars; + Lap) + `barker` (a race: lines / min, longest
  silence, overlaps, repeats, would-be levels, mess-ups) + `places` (1st / middle / last: reaction clip + results line) + `steering` (every form,
  first person + chase, full left / centre / right, hands-to-grips) + `volumes` (the four volumes, a migrated old settings.json via `-saveDir`, the
  duck) + `choice8` (Lake Jump car vs fly + canyon / lake pictures); `hitch` = the lap alone (HitchProbe: engine markers per long frame,
  best in the development build: BuildPlayer `-development` -> `Builds/ShiftStormDev`, Run-Check `-Exe`). `-noDinos`, `-noWarmup` for comparisons.
  Round 10: `round10` = `strand` (bad landings off every jump / the loop / the water ramp: on the road, drove back, clearly reset or STRANDED)
  + `edges` (pictures of the changed road edges) + `strip` + `lanes` + `edge` + `slowfps` (the Round 1 checks, now on Paradise Plunge);
  `-oldEdges` builds build 11's promenade / finishing-road edges (before pictures, before sweep). `coaster` = `cclap` (Coaster Coast lap: segments,
  set-piece visibility, rails, perf) + `ccrails` (each rail full / slow / very slow) + `ccsplits` (Midway Jump car / fly, Lighthouse round / rocks);
  `soak` and `split` take `-course 1` for Coaster Coast. `ccprobe`: diagnostics (see Round 10).
  Round 11: `round11` = `buck` (a race per course: briefing -> Barnaby -> Buck's remark, voice overlaps by speaker, skip, once per
  session, Short / Off) + `tour` (fresh tour.json: both stops scene -> race -> reaction -> unlock, reload, a 4th-place retry, Race
  again) + `racers` (16 driver x machine starts, pictures, a split-screen pick).
  Round 12: `round12` = `unlock` (fresh save: Coaster Coast in Race / Time Trial / 2 Players, the Tour setting On / Off) + `resetaudit`
  (how far back a reset goes, every 20 m of both laps) + `humanfly` (a human-like pilot by the HUD arrow, late / noisy inputs, LB late, no
  boost, `-tries N`, on both choice jumps' fly and car lines) + `branches` (scripted failures on the Lake flight, the Sky Arch slit and the
  Midway flight: no steering, hard left / right, too high / low, exit missed, slit missed, reset button). `-oldFlight` = build 13's flight
  branches, arrow and resets (before numbers).
  Round 13: `trace` (item 1's recorder: soak-like AI races, `-course N -laps N`, a 4 s ring buffer per racer dumped at every reset: position,
  velocity, heading, form, wheels, shortcut, inputs, the collider under it, touches and wall hits; `-carLine` / `-flyLine` force the choice
  jumps) + `round13` = `choice13` (Cliff Gap car vs fly, Mangrove bypass boat vs sub, `-runs N`) + `humancliff` (the human-like pilot on the
  Cliff flight / car line) + `branches13` (scripted failures on the Cliff flight and in the bypass) + `challenges` (each rival challenge won
  and lost: result, tour.json, the lines) + `presets` (frame times per Graphics preset, both courses, 1 / 2 players, `-seconds N`);
  `underpier` (Coaster Coast under the boardwalk / pier: pictures, 90 wander-offs, the human-like pilot through). Before switches:
  `-oldLanding` (build 14's landings), `-noCliffFlight`, `-noBypass`, `-oldPier`; `-graphics Deck|Low|Medium|High` for a run.
  Round 14: `round14` = `menus14` (a picture + the text of every menu screen at 1280 x 800) + `mix` (would-be levels per bus, both courses:
  music, engine, Barnaby, the crowd against build 15's gains) + `bubble` (the Scrambler's sub: side / front / first person / chase / the
  change, every rider bone, grip and mesh extent inside the glass) + `finishcam` (10 s of camera after the finish, both courses, chase and
  first person: jitter) + `wheel` (the human-like pilot through the Ferris wheel, `-tries N`; AI runs; LB on the ramp and late on every
  choice jump); `menuwalk` (title -> menu -> course -> racer -> race -> pause -> quit to menu -> settings left / right -> quit game, exit 0).
  Before switches: `-oldFinish`, `-oldWheel` (build 15's Midway choice and wheel), `-oldChoice` (the 1 s air-only window), `-oldBubble`.
  Round 15: `round15` = `shimmer` (every course: 10 s parked on the grid before GO, 10 s after the finish, 10 s on the grid after Race
  again: camera / car / sun / sky every frame + the picture's frame-to-frame change and a heat map; `-oldSky` = build 16's evening) +
  `bigtop` = `btlap` (Big Top Breakdown's check-pilot lap: segments, pictures at every landmark, set-pieces, wagon hits, a probe for
  car speeds over 110 m/s) + `btglobe` (the Globe of Death and the cannon at full / slow / very slow) + `btsplits` (Clown Jump car /
  fly and the river's main channel / Island rapids, AI runs, `-runs N`). `soak`, `split` take `-course 2`. `buck`, `mix`, `finishcam`,
  `presets` cover all three courses. Barnaby's name: `python Tools/Audio/barnaby_audit.py` (the built game's text + every voice clip
  transcribed locally) -> `Docs/Checks/round15/barnaby.md`.
  Round 16: `round16` = `bt16` (`btflight`: Big Top's Clown flight and cannonball flight, a picture every 0.5 s, is the next hoop / ring /
  exit on the screen; `humanclown`: the human-like pilot on both flights, `-tries N`) + `dam` (`damlap`: the check pilot's 3-lap time
  trial: lap times, the water's stages, every form change, set-pieces per lap, pictures of each lap's water; `damrise`: seven stragglers
  while the water rises round them (a rival put on the next lap); `damviews`: fixed viewpoints at every water stage). `soak` and `split`
  take `-course 3` (a 3-lap race: the soak's limit is 720 s). `-oldBtFlight` = build 18's Clown flight, flight camera and HUD arrow.
  Any check takes `-design <name>` (not saved). Any check takes `-difficulty Easy|Normal|Hard` (not saved) and `-noTunnel`. The launcher's hidden start-up check runs a check named in the `SHIFTSTORM_CHECK` environment variable. `nav`: a guided pilot that steers
  only by the HUD (arrow / next-gate marker) from off the line and facing backwards in the air, plus marker / arrow pictures per form. The lap check also measures render + GPU time per segment (PC numbers).
  Any run with `-mute`, `-check` or `-batchmode` is muted from the first frame and reports the measured output level.
- Game start options: `-course 0..3` (0 Paradise Plunge, 1 Coaster Coast, 2 Big Top Breakdown, 3 Dam Shame);
  `-race` / `-trial`; `-saveDir <dir>` puts the best-times and settings files elsewhere (checks never touch Dan's own files).
- In game: Menu (Start) / Esc opens the menu (see Controls). D-pad left / right no longer steps courses (Round 10).
- Vehicle import (Round 5, one command, Tripo models Dan saved in `Art/Incoming/Vehicles/<Design>/<Design>_<Form>.fbx`):
  `python Tools/import_vehicles.py` (or `--design Jetstream`; takes Tripo's zips as downloaded, meshes capped at 20,000 faces;
  `import.json` "files" maps a form saved under another name; `--src local/round5/testfbx --design _Test` for the stand-in made by
  `Tools/Blender/make_test_vehicle.py`). Blender cleans each (4.0 m long, origin, facing: `import.json` "yaw", default 180; floor per
  form, Seat point, LOD1, texture) into `Assets/Resources/Vehicles/<Design>/` + `designs.json`, previews in `Docs/Checks/round5/import/`;
  then build. Never ship `_Test` (names starting `_` are hidden; delete its folder before the release build).
- Music (Round 7): Dan's Suno MP3s in `Art/Incoming/Music/` named `race_paradise`, `menu`, `theme`, `results` (his downloads are
  "ShiftStorm - Paradise Plunge / Garage / Theme / Victory.mp3", found under either name) -> `python Tools/Music/make_loops.py` (copied
  from Tee'd Off) writes the intro + seamless loop to `Assets/Resources/Audio/Music/`. Caller lines: `python Tools/Audio/caller_lines.py`
  (dry run; `--go` records missing ones with ElevenLabs in the Barnaby voice). Every sound and its source: `Docs/SOUNDS.md`.
- Launcher: `powershell -File Tools/Build-Launcher.ps1` (and `-CoreOnly` for `LauncherChecks.exe`) -> `Builds/Launcher/`.
- Release (local draft, then the deliberate publish):
  `python Tools/Release-Round.py --version 0.1.0 --build 1 --notes "..."` then
  `python Tools/Publish-LauncherRelease.py Builds/Release/build-1/draft --publish --confirm-repository danemoll-jpg/ShiftStorm-Releases`
  (version and build go up every round; the starter ZIP is `ShiftStorm-<version>-Full.zip` on the release).
- Round-end disk clean-up: `python Tools/cleanup_round.py`
- Voices (Round 8): `python Tools/Audio/caller_lines.py` (Barnaby; `--go`, `--redo` re-records all), `python Tools/Audio/driver_lines.py` (drivers'
  mess-ups), then `python Tools/Audio/normalize_voice.py` (every voice clip to -16 LUFS). Playtest logs: `python Tools/playtest_summary.py <folder> [--out x.md]`.
- Buck (Round 11): `python Tools/Audio/buck_lines.py` (dry run; `--go` records missing lines in the ElevenLabs voice "ShiftStorm Buck",
  `--redo id ...`), then `python Tools/Audio/buck_audit.py` (local speech-to-text -> `Docs/Checks/round11/buck_lines.md`).

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
  - **Course art** (`ParadisePlunge.Polish2.cs`): the rest of the lap dressed; Barnaby's booth at the start line.
  - **Sound** (`Core/GameSounds.cs`, `Vehicle/VehicleSound.cs` + `EngineSynth.cs`, `Core/CourseAudio.cs`; list in `Docs/SOUNDS.md`), Dan's
    four Suno songs, Music / Effects / Voices volumes. **Barnaby** calls the race (`Race/Caller.cs`, clips in `Resources/Audio/Voice/barker`).
  - New sound files must be imported with Preload Audio Data on (the importer rule now sets it).

- **Round 8 (build 9, 0.8.0):**
  - **Dinosaurs** (`World/Dinos.cs`, `World/ParadisePlunge.Dinos.cs`): Tee'd Off's rigged bronto / T. rex / trike / ptero built at run time from
    `Resources/Dinos/<style>_rig.bytes`, Tee'd Off's ModelCreature animation copied (`DinoPose`), behaviours in `DinoActor` (graze, wade, swim,
    raptors run alongside, the T. rex roars on the climb's cliff with a camera shake, pterodactyls swoop); 46 along the lap + nests, footprints,
    cave fossils; every path kept off the racer line at build time; drawn within 650 m, shadows within 140 m. Stand-ins: raptor (small T. rex),
    plesiosaur (swimming bronto); Tripo prompts in `Docs/MODELS-NEEDED.md`.
  - **Barnaby** (`Race/Caller.cs`): Round 7's ElevenLabs lines were silent (converter fade bug) - all re-recorded; 158 clips in 53 pools,
    every voice at -16 LUFS; a line every ~6 s (landmarks, dinos, overtakes by name, places, form changes, fill-ins), never two at once, no
    repeats in a race; music and engines duck 8 dB under him (`Caller.Duck`).
  - **Drivers** (`Vehicle/Driver.cs`): every driver speaks in their own voice; mess-up yells (wall, reset, penalty, missed gate, loop fail; rivals
    only near a person's racer), never over Barnaby (a yell waits for his line); finish reaction by place (1st clap, middle thumbs up, last
    disappointed). **Steering mechanism** (`Vehicle/Steering.cs`): wheel / helm / yoke / levers per form (+ stick, handlebars; designs.json
    "steering" or a "Steering" empty in a model), hands on it by two-bone IK.
  - **Lake Jump paths:** the car line bends inland through a jungle canyon (`Canyon()`; G1 / climb0 / corner1 moved east), the fly line swings
    out west over the beach and the lake (`LakeFlight`); about equal time. **Coral Vault tunnel** reshaped (straight chord, straight climb-out,
    a bowl at the first turn, a 17 m/s current). **Warm-up** (`Core/Warmup.cs`): the course rendered from ~100 views at start-up (DX12 pipelines).
  - **Playtest logs** (`Core/PlaytestLog.cs`): `<data>/playtest/session-*.jsonl`, one record per race per player.

Dan reviewed build 7: "everything good so far."

Round 7 delivered as build 8 (0.7.0); Dan reviewed it 2026-11-14 and his notes became Round 8.

- **Round 8b (build 10, 0.8.1):** Dan on build 9: "not really seeing dinosaurs". Dinos cartoon-huge (brontos 26-28 m, T. rex 15 m, trikes
  5.5-6 m, pterodactyls 20-30 m wingspan) and placed by distance along the route (`ParadisePlunge.Spot`: end of a straight / outside of a
  bend, 15-60 m from the road's edge, facing the racers); 16 set-pieces (`DinoActor.setPiece`): the promenade's neck bridge (`Kind.Arch`),
  raptors / a pterodactyl / the lagoon long-neck / a triceratops pacing the nearest person's racer (`Kind.Pace`), the T. rex dead ahead at
  corner2, a long-neck rising out of the Sunken Gateway pool (`Kind.Rise`), 60 m skeletons along the Coral Vault walls (`DinoActor.Still`).
  Visibility = `DinoActor.ScreenShare(cam)` (box share of the screen height; above-water part only; underwater fog 46 m); "seen" = 8 % for
  1 s (`dinos` check: per segment, pictures at 25 %). Barnaby's dino pools are said only while that kind is big on screen
  (`Caller.DinoPools`, `DinoActor.Track` / `BigOnScreen`). Dino shadows within 100 m, non-set-pieces drawn within 500 m. ~+1.4 ms 95th pct
  on the PC.

- **Round 9 (build 11, 0.8.2):**
  - **Steering size** (`Vehicle/Steering.cs`): mechanisms at real proportions to the driver, placed low where her hands reach
    (`Steering.Place`, from the Driving clip's hands); first-person camera 17 cm over her eyes, 5 deg down, FOV 70 -> 80
    (`CameraViews.FirstPersonFov`). The `steering` check measures the share of the screen height by rendering with / without it.
    Kind "grips" = only grips on a model's own bars (the Scrambler).
  - **Voices:** golf-only driver clips out, racing lines in (`Tools/Audio/driver_lines.py` ROUND9 / REMOVED); `Tools/Audio/voice_audit.py`
    writes every line in use + what speech-to-text hears (faster-whisper small.en, local; the ElevenLabs key has no STT permission) to
    `Docs/Checks/round9/voice_lines.md`.
  - **Sounds:** Tee'd Off's win jingle gone; one crowd sound per finish; `Core/SoundLog.cs` logs every sound and skips a second one of a kind
    (music, crowd, splash, whoosh, impact, chime) within 0.5 s.
  - **Crowd** (`World/Crowd.cs`, `World/ParadisePlunge.Crowd.cs`, from Gridiron Glory: `Resources/Shaders/CrowdInstanced.shader` (URP),
    `Resources/Crowd/FanAtlas.png`): 4 grandstands (start both sides, loop, waterfall landing), 1,182 fans, a cheer rippling along a stand as a
    racer passes; drawn per camera from `RenderPipelineManager.beginCameraRendering` (Graphics.DrawMesh* queued in LateUpdate misses a
    camera rendered by hand). `-noCrowd` turns it off.
  - **Designs** (designs.json keys from each `import.json`: `title`, `default` (Jetstream), `rival` (GrandPrix -> Rae, Hotrod -> Vivi,
    Scrambler -> Chip, used when all 4 forms are in), `steering`, `canopy` per form (bubble / sphere / none), `astride`, `lean`, `sphere`,
    `sphereY`, `riderScale`; import-only: `length` (Scrambler 2.6 m), `cut` boxes per form (faces deleted: Hot Rod T-handle, Scrambler centre
    stand), `steer` (the steering point), `yaw` (GrandPrix -90, Hotrod 180, Scrambler -90), `skip`). `Vehicle/BikeLean.cs` (visual lean),
    Driver astride pose + `riderScale`. `JurassicLook.Textured` is cached by texture object (by name, all designs shared one texture).
    `Tools/Blender/make_placeholder_scrambler.py` = a test bike design `_Scrambler` (hidden; never ship it).
  - **Dinos:** Dan's raptor + plesiosaur: `python Tools/Characters/import_dinos.py` (Blender `dino_to_glb.py` + Tee'd Off's rigger copied
    to `Tools/Characters/rig_dino.py`) -> `Resources/Dinos/<style>_rig.bytes`; stand-ins only if a rig is missing.
  - Checks: `round9` = `steering` + `sounds` + `crowd` + `bike` (`-testDesign Scrambler`) + `designs` (Garage + who races what).

Round 9 delivered as build 11 (0.8.2), 2026-11-15.

- **Round 10 (build 12, 0.9.0):**
  - **No stranding edges** (`ParadisePlunge.cs`, `ParadisePlunge.Stunts.cs`): the promenade's and the finishing road's barriers are gone (a flush
    red / white kerb instead); the ground beside them is flush with the road (`GroundTop` 1.97 m), the lakeside beach slopes into the lake; two
    long angled guide walls before the loop span the whole in-bounds width (nothing in bounds is behind them); fall zones under the climb's
    valley side. `-oldEdges` builds build 11's layout.
  - **Test courses removed:** the Round 1 strip and lanes, their menu rows, the D-pad / F3 / F4 course switch and 15 gray-box materials;
    their checks run on Paradise Plunge (`Checks/CheckPilot.Round10.cs`). `WorldBuilder` builds the two courses.
  - **Coaster Coast** (`World/CoasterCoast*.cs`, `CoasterLook.cs`, `CoasterRail.cs`, `Vehicle/ShiftVehicle.Rail.cs`): a pier amusement park 8 km
    east of Paradise Plunge (only the raced course is active: `CourseLook`), sunset turning to night over the lap (`CoasterSky`), the tour's
    public opening night (Docs/STORY.md: "GRAND OPENING" banners, fireworks, Barnaby's opening-night lines). Boardwalk (Midway Jump: car under /
    fly through the hubless Ferris wheel) -> coaster rail A (launch, airtime hill, banked turn, loop, the Coaster Gap jump) -> rail B
    (corkscrew, lift hill, top hat) -> flight (helter-skelter, fireworks rings, mascot balloon) -> log flume (under the boardwalk's jump) ->
    the sea (Lighthouse round / rocks) -> under the fishing pier (sunken carousel, octopus) -> splash pool ramp -> finish. Rails: a car is
    locked to the track (booster minimum speeds, never stalls or falls off). Per-course data: `Course.music / marks / stunts` (`CourseStunts`)
    for the AI, the pilot, the route strip and Barnaby (`Caller.ForCourse`, cc* pools). Set-pieces: `SetPiece` (Round 8b's 8 % rule).
    Stand-ins: mascot balloon, octopus, whale (Tripo prompts in `Docs/MODELS-NEEDED.md`). Music: Dan's `race_coaster`.
  - **No Tee'd Off music:** `course_jurassic` deleted; a missing course song falls back to ShiftStorm's theme.
  - Scrambler: Abi bends forward from the spine to reach the bars (`Driver.ReachForward`): hands on the grips in every form.
  - Late fixes: a low guide wall joins the canyon's east wall to the climb's barriers (`ClimbGuard`); on a coaster rail race progress is the
    share of the track ridden and no recovery runs; a bump between racers is capped (6 m/s sideways, 15 % of the speed: `RacerBump`); no rail
    support stands on a road (`OverRoad`); `AudioMeter` logs any non-zero output peak with the sources playing. `ccprobe` = a car driven
    through Coaster Coast's station straight on five lines, logging heading jumps and the colliders round it. Known: ~1 AI lap in 3 on
    Coaster Coast is reset just after the Midway flight (thrown sideways near the station; not reproduced with one car).

Round 10 delivered as build 12 (0.9.0), 2026-11-15.

- **Round 11 (build 13, 0.10.0): Buck arrives, The Tour, choose your racer** (Docs/STORY.md is the story direction):
  - **Buck "Big Finish" Barlow** (`Race/Buck.cs`): the tour's organiser; Barnaby still calls the races, Buck talks before and
    after them and in The Tour's scenes, never at once (Barnaby plays nothing while `Buck.Busy`; `Buck.Hold` stops Barnaby and the
    drivers). 53 lines, ElevenLabs voice "ShiftStorm Buck" (`Resources/Audio/Voice/buck`, pools by prefix: brief_pp/cc, short_pp/cc,
    res_<pp|cc>_<first|podium|back>, yarn, tour_*). Briefing: before a course's first race / time trial of the session, time stopped,
    a card (placeholder portrait `Buck.Portrait`, name, subtitles) on the HUD, A / Enter skips; Settings "Buck's briefings" On / Short /
    Off. Results remark after Barnaby's results line (races, not The Tour). Checks: `Buck.Enabled` (off in check runs unless a check
    turns it on).
  - **The Tour** (`Race/Tour.cs`): Menu > The Tour; Stop 1 The Trial (Paradise Plunge), Stop 2 Opening Night (Coaster Coast): scene
    (Buck's lines over a camera flight along the course, skippable) -> race -> Buck's reaction; points 10 / 6 / 4 / 2, a top-3 finish
    unlocks the next stop, best place / points kept on a retry; `tour.json` beside settings.json; Race again retries the stop (no scene).
  - **Choose your racer** (`Race/Lineup.cs`): before Race, Time Trial and a Tour stop (Menu page "Choose your racer", the Garage
    turntable): a driver (Abi, Rae, Chip, Vivi) and a machine (Jetstream, Grand Prix, Hot Rod, Scrambler); the other three race as the
    AI in their own machines (Abi's: Jetstream; recoloured only when a player races the same one). 2 Players: each player's driver and
    machine on its page. Saved (`settings.json` "driver", "design", "driver2", "design2"). Vehicles are re-seated in place
    (`Driver.Swap`, `FormSet.SetBody` / `SetOwnDesign`), so everything holding a vehicle keeps it. Barnaby's overtake / winner lines go
    by name (4 new "Abi" lines).
  - Buck's look: 3 image prompts (human / bear / dog) and his Mixamo clip list in `Docs/MODELS-NEEDED.md`; the placeholder card stays.
Round 11 delivered as build 13 (0.10.0), 2026-11-15.

- **Round 12 (build 14, 0.10.1): every course open, flight branches fixed:**
  - **Courses:** Race / Time Trial / 2 Players never lock (the choose-your-racer page has a Course row too); The Tour's stops are in order
    only when Menu > Settings "Unlock all Tour stops" is Off (default On: `Settings.UnlockTourDefault`, set it false for a release; `Tour.Earned`
    = what the Tour itself unlocked).
  - **Lake flight** (`ParadisePlunge.LakeFlight`): build 13's return S-bend was ~60 m radius (the plane turns at 150-200 m); now out 22 m and
    28 m up, every bend >= 250 m, rejoin / wheels-down at G1 - 45 (`LakeRejoinBack`; was 75), entry 12 m off the lip, 30 x 20 m half-size
    entry / exit frames, the exit takes a car too, a 70 m wide wheels-down zone, rings radius 16. Midway flight (Coaster Coast): same
    entry / exit / wheels-down treatment.
  - **Flight branches** (`Shortcut.flight`, `ShiftVehicle.Recover` / `.Shortcut`): the wings put you on the choice branch at once; a light
    steering assist on a flight branch (people only, `ShiftTuning.branchAssist*`); a reset on (or within 8 s of leaving) a flight branch goes
    forward to the anchor before its rejoin (a branch standing in for a gate counts it) +3 s (`branchRecoveryPenalty`); in the air elsewhere a
    reset restarts on the flight line at most `flightResetMaxBack` (200 m) back (`FlightPointOk`); crossing / passing a gate in a form it
    does not take resets at once to the anchor before it (turning back could never earn it: a plane circled Coaster Coast's G1 for good);
    the HUD arrow follows a shortcut only once it is entered (it used to steer cars in the Lake Jump's canyon toward the flight line and
    planes toward the Sky Arch slit). New anchors: the Lake / Midway rejoins (G1 - 25 / - 22), the lagoon corridor (splash + 22), and
    `FillAnchors` (same-form gaps over 330 m filled every ~220 m, away from jumps and the loop). `-oldFlight` restores build 13's.
    The coaster rail's entry catches a car up to 9 m over the track (`CoasterRail.Entered`; build 13: 4.5 m: Round 10-11's reset after the
    Midway flight). Dan's playtest logs live on the Deck (Proton prefix), not on this PC.

- **Round 13 (build 15, 0.11.0): known resets fixed, two new route choices, rival challenges, Graphics presets, Coaster Coast's pier:**
  - **Landings** (`ShiftVehicle.HoldLanding` / `WheelsDownLanding`, found with the `trace` check): the landing's speed hold re-scaled whatever
    flat speed was left after the first touch, so a touch that took the forward speed (Coaster Coast's station after the Midway flight)
    became 74 m/s sideways into the sea; now it keeps the way the car came down unless a wall turned it (jump landings too). After a
    plane's wheels-down the car is turned onto the route (up to 40 deg off, 45 deg/s) and touches down at no more than 8 m/s down
    (`ShiftTuning.wheelsDown*`): off the Lake flight it came down 15-20 deg off the road, bounced, rode onto the climb's guard wall and fell
    off the ground's end; the guard wall is 2.6 m now (`ClimbGuard`).
  - **Cliff flight** (`ParadisePlunge.CliffFlight`): the Cliff Gap is a second choice jump (LB / E off its lip): over the hillside, round the
    outside of corner2, three rings, into the Sky Arch flight 230 m past the ramp's lip; stands in for G2; the Round 12 rules (`Shortcut.flight`).
  - **Mangrove bypass** (`ParadisePlunge.Bypass.cs`): a dive bay in the left bank before the S-bend (root posts along its river edge), a root
    cavern under the river bed (`Tunnel(sc, floorY, ...)`), glowing fish, a sunken log arch, a 7 m/s current, its own dive / surfacing zones,
    sub-gates and anchors (`Shortcut.ownAnchors`: a reset in it restarts in it); G5 takes a sub too. Vivi and Abi's AI take it sometimes.
  - **Rival challenges** (`Race/Challenge.cs`): The Tour page lists one per stop (open once the stop is cleared top 3, or with "Unlock all
    Tour stops"): Rae's Waterfall Dash (Paradise Plunge, Coral Vault -> G9) and Chip's Coaster Showdown (Coaster Coast, station -> G3);
    Buck's word, the rival's taunt, 3-2-1 (`ShiftVehicle.HoldFor`), the first through the gate wins; lines from the rival, Barnaby (ch*) and
    Buck (ch_*); a first win gives a Garage paint (`Paints`, settings.json "paint"); tour.json "cWon / cBest / cTries".
  - **Graphics presets** (`Core/GraphicsPreset.cs`): Auto / Deck / Low / Medium / High; render scale (FSR below 1), shadows, far clip, the
    Detail layer, dino draw range (`DinoActor.RangeScale`), crowd density (`Crowd.Density`), texture mip limit, LOD bias; split screen one lighter.
  - **Coaster Coast under the boardwalk / pier** (`CoasterCoast.UnderPier.cs`): plank skirts round every plaza edge (nothing drifts under a
    deck), a lit channel (lanterns, guide lights, a seabed stripe), the pool's exit frame and light column, clearer water (fog .016),
    currents back toward the dive route, more anchors, and `Course.lostRanges`: lost there (6 s without progress), stuck, out of bounds or
    the reset button go forward to the next anchor that is not past an unearned gate.
  - Voices: `Tools/Audio/driver_lines.py` ROUND13, `buck_lines.py` ch_*, `caller_lines.py` ROUND13; `Tools/Audio/challenge_audit.py` ->
    `Docs/Checks/round13/challenge_lines.md`.

Round 13 delivered as build 15 (0.11.0), 2026-11-16.

- **Round 14 (build 16, 0.12.0): title screen and clean menus, crowd much quieter, the Scrambler's whole bubble, no shake after the race,
  the Ferris wheel on the main route:**
  - **Menus** (`Game.cs` rows + `UI/Hud.Menu.cs` drawing): title screen (logo `Resources/UI/Logo.png` from `Tools/make_logo.py`, theme
    song, any button) -> main menu -> course cards (`CourseCards` in `UI/MenuCam.cs`: a picture per course rendered at start-up) -> choose
    your racer -> Go; Settings in Gameplay / Audio / Video / Controls (`Game.MenuRow`: label, value, A action, left / right action, slider,
    description); pause = Resume / Restart / Settings / Quit to menu; results = Race again / Next / Menu. `Game.InFront` = the front menus
    (race behind frozen and hidden, engines / jungle silent, `MenuCam` flies along the course). `ShiftInput.MenuSide` (D-pad / stick / arrows /
    A-D, held = repeat). Checks open the front menus with `game.OpenFrontMenu()`; `game.CloseMenu()` still closes them for a check.
  - **Crowd:** `Settings.Crowd` (Audio > Crowd, 0-200 %, "crowdVol") x `Settings.CrowdGain` 0.11 (-19 dB): the stands (`World/Crowd.cs`) and
    GameSounds' crowd kind.
  - **Scrambler bubble** (`Vehicle/BubbleSphere.cs`, `Resources/Shaders/BubbleGlass.shader`): designs.json canopy "sphere" = a whole ball round
    the bike and rider from their bounds, chrome equator ring, grows over the bike on the change; refraction via the camera's opaque copy,
    switched on per camera only near a bubble. Placed from the FormSet pivot every frame (the model's own scale is not reliable).
    `-oldBubble` = build 15's ball over the seat.
  - **After the finish** (`ChaseCam.FinishCam`): no shake terms, the camera eases out and circles a smoothed point, every view; a person's
    car rolls to a stop (`ShiftInput` ignores its driving inputs once Finished). `-oldFinish`.
  - **Ferris wheel** (`CoasterCoast.LinePoints`, `OldWheel`): the Midway Jump's lip turns every car into the plane ("Wings out (Midway Jump)"),
    the wheel centred over the line (`WheelCentre` (0, 35, 430) from O), the line through it, three firework rings (`WheelFireworks`), the
    wheels-down zone before G1; no Midway choice / shortcut. **Choice jumps:** `ChoiceLip.ramp` (the 45 m kicker): the window opens on the
    ramp, LB there arms the wings (`ShiftVehicle.WingsArmed`), 1.5 s window into the air. `-oldWheel`, `-oldChoice`.

Round 14 delivered as build 16 (0.12.0), 2026-11-16.

- **Round 15 (build 17, 0.13.0): the shimmer's cause fixed, Barnaby by name, Big Top Breakdown:**
  - **Shimmer** (`World/CoasterLook.cs` `CoasterSky`): Coaster Coast's sky eased toward the leader's evening value at .04 / s in .002
    steps, so after a race (night) a restart's grid (sunset) and an unfinished catch-up after the finish turned the sun ~0.15 deg per
    step (27 deg in 10 s): the toon shading's light / shadow bands and the shadows crawled over the picture, seen only while parked.
    Now the evening moves only while a person is racing; parked (countdown, after the finish, front menus) it is set once, at once.
    `-oldSky` = build 16's. Paradise Plunge and Big Top Breakdown have a fixed sun.
  - **Barnaby** (`Caller.Name`): subtitles, the booth's board, the results ("Race called by Barnaby"), the Voices description, the
    docs; the clip folder `Voice/barker` and check names stay (ids). No line ever said "Barker" (`Tools/Audio/barnaby_audit.py`).
  - **Big Top Breakdown** (course 2, `World/BigTop*.cs`, `BigTopLook`; Docs/STORY.md: Lionel Lustre's dare, the Tour's Stop 3 "The Big
    Top Dare"): a circus camp 9 km west of Paradise Plunge in bright gusty daylight. Midway (the wagon parade on the side lanes:
    kinematic `Wagon`s, centre +-7.5 m always clear; strongman, tightrope walker) -> the Clown Jump (choice: car = the Globe of Death,
    a loop rail inside a steel-mesh sphere; LB = the "Clown flight" past it) -> the big top's door, once round the ring (6 stands of
    fans), a 40 m turn into the giant cannon (a rail up its barrel, boosters to 80 m/s) -> human-cannonball flight through three hoops
    (trapeze artists) and out of the top flap (a hole in the solid striped roof), two 300 m arcs over the camp -> the river (x 800-920:
    elephants spraying, the show boat, the island: main channel west, "Island rapids" east with a current and rocks, sea lions) ->
    dive into the lake (escape artist's tank, through a sunken circus wagon: G7) -> the splash pool, its ramp, the finish. Line 5.5 km,
    check-pilot lap ~95 s. Stand-ins for the elephants, sea lions, performers and the ringmaster (`Docs/MODELS-NEEDED.md` Round 15).
    Music `race_bigtop` (Dan's song). Barnaby `bt*` pools, Buck `*_bt` / `tour_s3_*`, Vivi's Cannonball Run (`Challenge` c3, paint
    "Vivi Violet"). `CoasterCoast.RailPath` is shared (`Ramp` piece added).

Round 15 delivered as build 17 (0.13.0), 2026-11-16.

- **Round 15b (build 18, 0.13.1): the screen shake on Coaster Coast / Big Top fixed at its cause:** the HUD / menu canvas was Screen
  Space - Camera 0.22 m in front of the race camera (1 px ~ 0.3 mm), and Unity builds and draws UI vertices through world positions;
  8-10.5 km from the origin a float holds ~1 mm = ~4 px on the Deck's 800 lines, so the whole menu / HUD snapped by pixels as the camera
  moved (Paradise Plunge is near the origin). Now (`UI/Hud.cs`) the canvas stands `Hud.CanvasDistance` (100 m) ahead (1 mm ~ 0.01 px),
  sorting order 32000, and every HUD graphic uses `Resources/Shaders/HudUI.shader` (Unity's UI/Default in URP HLSL): canvas units ->
  the camera's view directly (`_HudLocalToView`, set per HUD material in `beginCameraRendering`), drawn at near clip + 2 cm as before
  (same depth against the cockpit). `Hud.UseMaterial` puts graphics made later on it. Keep UI off world positions far from the origin.
  Tried and dropped: a UI overlay camera at the origin (URP drew no UI in its stacked pass in the checks' renders: unverifiable), a world
  canvas parked at the origin (culled 8 km away).
  Check `shake` (Deck preset unless `-graphics`): the real menu path (title -> Race -> cards Paradise / Coaster / Big Top and back), a
  Coaster Coast race -> results -> Menu -> Paradise, a Paradise race, a Big Top race; the camera every frame (CSV, 60 Hz residuals) and
  the picture's flicker at 60 Hz steps (A -> B -> A pixels, heat maps; shadows off / native variants); `MenuCam.Hold` / `MenuCam.Step`
  for checks. `-oldHud` = build 17's canvas.

Round 15b delivered as build 18 (0.13.1), 2026-11-16.

- **Round 16 (build 19, 0.14.0): Big Top's first flight made obvious, Dam Shame (the first three-lap race):**
  - **Clown flight** (`BigTop.ClownHoops`): four striped circus hoops along it (`Shortcut.rings`: the HUD arrow points at the next hoop,
    then the exit: `Shortcut.GuideAt`), the Globe of Death 12 m east (`BigTop.GlobeX`) to give them room, a lit exit arch "TO THE BIG TOP"
    with two spotlights on the wheels-down chevrons, a later rejoin (door - 32), a wider / deeper wheels-down zone; the cannonball flight's
    rings bigger and brighter, a "SPLASHDOWN" arch on the river, the cannon's wings zone 40 m deep (from 8.5 m up). In flight the chase
    camera turns up to 25 deg toward the arrow's target (on a flight branch, and 3 s after any change into the plane: `ChaseCam`); the route
    strip shows each choice jump's flight branch in violet. `-oldBtFlight` = build 18's.
  - **Dam Shame** (course 3, `World/DamShame*.cs`, `DamLook`; Docs/STORY.md: Buck's "controlled water demonstration" at the Lockhart Dam,
    Hattie Sluice the dam keeper; the Tour's Stop 4 "The Controlled Demonstration"; Rae's Flood Run, paint "Spillway Teal"): 9 km south of
    Paradise Plunge, a dam across a mountain canyon in morning light; the ground is one heightfield with the roads pressed in
    (`DamShame.GroundAt`). **Multi-lap:** the line is the lap three times end to end (`Course.laps`, `lapStarts`, `LapAt`), one segment
    per lap (the splits are the lap times), gates G1-G5 per lap (spillway, bridge, turbine hall, dam road, lap line) + G0 = 16; the HUD
    shows LAP n / 3 and flashes LAP 2 / FINAL LAP; results show the best lap; `besttimes.json` "single" = the best one lap.
    **Water** (`DamWater`): stage 0 / 1 / 2 = the valley at -2.5 / 1.4 / 16 m, raised when the leader (most gates of any racer; a time
    trial's one racer) starts lap 2 / 3: siren (made in code), Buck on the tannoy (`Buck.PA`, `rise1_*` / `rise2_*`), Barnaby (`dmrise1/2`),
    a 10 s rise; a new race puts it back at once. Every zone (`DamShame.dynZones`: its form per stage, or off) and anchor (`dynAnchors`,
    `AnchorFor`) follows the stage, not the racer's lap; the chute's floor is a `WetRamp` (only `enabled` ones count now) with a torrent;
    every physics step a car the water rose over becomes a boat, a boat held under (2.6 m for 0.4 s) a sub, a boat left on a dry road a
    car. Lap 1: car down the dry chute, the flip bucket a choice jump (LB: the "Canyon flight" over the river and the beavers, stands in
    for the bridge gate). Lap 2: boat down the torrent, the lip throws you up (Fly), "Hull down (the valley)", boat through the hall,
    wheels down at the dam road. Lap 3: boat down the chute into the flood, held under / "Canopy seals" -> sub along the drowned road and
    through the flooded turbine hall, "Surface", "Wheels down (the flood line)". Set-pieces: the dam, the spillway gates and torrent, the
    turbines, Buck at the control room window, the beaver colony, a heron, trout, the flood's logs (stand-ins; Tripo prompts for the
    beaver, heron and Hattie in Docs/MODELS-NEEDED.md). Music `race_dam` (Dan's song; the theme until it arrives). Course cards: a
    carousel once there are four.
  - **Reset loops:** the third reset in a row for the same missed gate puts the racer past it, +3 s (`ShiftVehicle.MissedRecover`;
    the Dam Shame soaks had an AI straggler reset 136-205 times at one gate after the water rose round it).

Round 16 delivered as build 19 (0.14.0), 2026-11-17.

Current objective: **Round 17 — the fun round**: creative power-ups (B), stunts with stunt points (right stick in the air;
Stunt Show mode), 1–5 laps on every course (default 3), and Paradise Plunge's big route splits. Release build 20 (0.15.0). See
`TODO.md`.
