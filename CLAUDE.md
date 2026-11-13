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
- **3D models:** Blender, scripted where possible (`Tools/Blender/*.py`).
  Hard-surface things (including the transforming vehicle) are built by
  script. People, creatures and organic props: **Tripo, made by Dan by hand in
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
| Secondary: drift (car) / tighter carve (boat) | RB (or X) | Left Shift |
| Reset to last safe anchor | Y | R |
| Invert climb / dive (saved in `settings.json`; also Menu > Settings) | View (Back) | F2 |
| Previous / next course (strip, 4 lanes) | D-pad left / right | F3 / F4 |
| FPS + speed readout on / off (saved in `settings.json`; also Menu > Settings) | R3 (right stick click) | F1 |
| Full screen / windowed (saved, `display.json`) | Menu > Settings > Screen | Alt+Enter |
| Menu: Resume, Restart, Race, Time Trial, test courses, Settings, Quit game | Menu (Start) | Esc |
| Menu move / choose / back | D-pad up-down / A / B | Up-Down (W-S) / Enter or Space / Backspace |

Menu > Settings: Invert flight / dive controls On / Off, Screen, FPS readout. Quit game asks "No / Yes" (No is preselected) and exits
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
  `-TimeoutMinutes 150`). The launcher's hidden start-up check runs a check named in the `SHIFTSTORM_CHECK` environment variable. `nav`: a guided pilot that steers
  only by the HUD (arrow / next-gate marker) from off the line and facing backwards in the air, plus marker / arrow pictures per form. The lap check also measures render + GPU time per segment (PC numbers).
  Any run with `-mute`, `-check` or `-batchmode` is muted from the first frame and reports the measured output level.
- Game start options: `-course 0..5` (0 Paradise Plunge, 1 transition strip, 2 drive lane, 3 boat lane, 4 fly lane, 5 dive lane);
  `-race` / `-trial`; `-saveDir <dir>` puts the best-times and settings files elsewhere (checks never touch Dan's own files).
- In game: Menu (Start) / Esc opens the menu (see Controls); D-pad left / right still steps courses.
- Launcher: `powershell -File Tools/Build-Launcher.ps1` (and `-CoreOnly` for `LauncherChecks.exe`) -> `Builds/Launcher/`.
- Release (local draft, then the deliberate publish):
  `python Tools/Release-Round.py --version 0.1.0 --build 1 --notes "..."` then
  `python Tools/Publish-LauncherRelease.py Builds/Release/build-1/draft --publish --confirm-repository danemoll-jpg/ShiftStorm-Releases`
  (version and build go up every round; the starter ZIP is `ShiftStorm-<version>-Full.zip` on the release).
- Round-end disk clean-up: `python Tools/cleanup_round.py`

## Where things stand
**Always check `TODO.md` for the current objective before starting work.**

Round 1 (build 1), Round 2 (build 2, 0.2.0), Round 2b (build 3, 0.2.1) and Round 3 (build 4, 0.3.0) are delivered: four-mode vehicle, full gray-box Paradise
Plunge lap in the Jurassic Par look and sound, ordered gates, time trial, Abi in the cockpit; Round 2b: where to go (glowing gate frames,
next gold / after cyan, HUD arrow + marker + route strip, route guides in every form, direction cues from the heading against the route),
much faster, fullscreen by default.
- **Speeds (Round 2b, `Resources/Tuning`):** Drive 65 m/s (boost 82), Boat 50 (62), Fly 80 (100), Dive 38 (48). Paradise Plunge is
  built 1.7x larger on the ground plan than build 2 (`ParadisePlunge.S`; heights unchanged): line 7.3 km, pilot lap ~141 s.
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

Current objective: **Round 4** (unattended): walls glance you off instead of stopping you, an AI difficulty setting (default Easy, no
rubber-banding), nothing solid on the road at the start / finish (and anywhere else in the route), a cleaner grid start, the Coral
Vault tunnel back on, and a just-missed gate counts with a time penalty instead of a U-turn. Release build 5 (0.4.0). See `TODO.md`.
