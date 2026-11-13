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
| Invert climb / dive (saved, PlayerPrefs `invertVertical`) | View (Back) | F2 |
| Previous / next course (strip, 4 lanes) | D-pad left / right | F3 / F4 |

After the finish, Reset starts the course again.

## Commands
Unity 6000.6.1f1 at `C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe`; Blender at `F:\blender\blender.exe`.
Unity batch runs need the sandbox off (its package manager uses a named pipe).
- Project setup (URP, Input System only, layers, materials, tuning assets, scene; safe to repeat):
  `powershell -File Tools/Unity-Batch.ps1 -Log local/logs/setup.log -Method ShiftStorm.EditorTools.ProjectSetup.Run`
- Vehicle model: `F:\blender\blender.exe -b -P Tools/Blender/shiftstorm_vehicle.py -- C:/Users/danmo/ShiftStorm`
- Player build (`Builds/ShiftStorm/ShiftStorm.exe`):
  `powershell -File Tools/Unity-Batch.ps1 -Log local/logs/build.log -Method ShiftStorm.EditorTools.BuildPlayer.Run -Extra '-buildVersion','0.1.0'`
- Muted, windowless check in the built exe (`-batchmode -mute`, Below Normal; report + pictures in the folder):
  `powershell -File Tools/Run-Check.ps1 -Check all -Out local/round1/check` (`strip`, `lanes`, `edge` or `all`).
  Any run with `-mute`, `-check` or `-batchmode` is muted from the first frame and reports the measured output level.
- Game start options: `-course 0..4` (0 strip, 1 drive lane, 2 boat lane, 3 fly lane, 4 dive lane).
- Launcher: `powershell -File Tools/Build-Launcher.ps1` (and `-CoreOnly` for `LauncherChecks.exe`) -> `Builds/Launcher/`.
- Release (local draft, then the deliberate publish):
  `python Tools/Release-Round.py --version 0.1.0 --build 1 --notes "..."` then
  `python Tools/Publish-LauncherRelease.py Builds/Release/build-1/draft --publish --confirm-repository danemoll-jpg/ShiftStorm-Releases`
  (version and build go up every round; the starter ZIP is `ShiftStorm-<version>-Full.zip` on the release).
- Round-end disk clean-up: `python Tools/cleanup_round.py`

## Where things stand
**Always check `TODO.md` for the current objective before starting work.**

Nothing built yet. Repo is empty apart from these docs. Current objective:
**Round 1 — foundation and the four modes** (repo + Unity project, launcher,
reuse audit, one controller with four handling modes, placeholder
transforming vehicle, transition test strip, edge-case checks, publish).
