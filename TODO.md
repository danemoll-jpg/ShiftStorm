# ShiftStorm — Active Development Plan

Working title: **ShiftStorm**. A Tee'd Off universe spin-off racer where one
machine turns into a car, boat, plane and submarine during one continuous race.
First goal: the **Paradise Plunge** proof of concept (one lap, one vehicle, one
returning Tee'd Off driver, three AI rivals). Full concept: Dan's
`Teed_Off_Racing_Spin_Off_Concept.docx`, to be saved in `Docs/`.

## Standing rules
- Read `CLAUDE.md` first. Process, toolchain, launcher and testing rules there
  apply to every round.
- Every test run muted (`-mute`, measured audio level reported as 0) and never
  steals focus. If a test needs a visible/focused window, stop and ask Dan.
- One heavy job at a time, Below Normal priority. Clean the disk at round end.
- Read-only reference projects: `C:\Users\danmo\chastain_rush` (code, launcher)
  and `C:\Users\danmo\Tee-dOff` (art, characters, voices, music). Copy from
  them; never edit them.
- Results: ~20 lines, one line per item with its one check, plus a
  "For Dan to check" list. Per-item status is required: done / not done /
  deferred, and why.

## Completed Tasks
- **Round 4 — walls, keeping up, the start line, the tunnel, near-miss gates** (2026-11-13, release `build-5` / 0.4.0). Spec moved to `TODO-ARCHIVE.md`.

### Round 4 delivery (2026-11-13) — status per item
Evidence: `Docs/Checks/round4/` (reports per check, pictures). Every run: built exe, `-batchmode -mute`, no window, Below Normal, one
heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run. Sound was never unmuted. No window opened.
1. **Walls: glance off — done.** `ShiftVehicle.Walls.cs`: speed into the wall removed, speed along it kept minus 4 % (shallow) to 40 %
   (head-on) by angle, 2.5 m/s nudge away, a car's nose turned 80 % along the wall and its spin removed, a steep hit always slides the
   route's way; frictionless hull; hit sound + camera bump. Slit buttress keeps the Round 3 scrape rule (a scrape there still costs
   +1.49 s against the safe line, the slit saves 1.57 s). Check (10 / 25 / 45 deg, every form): speed kept 0.25 s later car 98/95/86 %,
   boat 99/95/88 %, plane 98/96/93 %, sub 101/96/92 %; no stop, no spin, 0 resets; 3 s held against a wall: car 106 m, boat 102 m, sub 92 m
   forward. Found and fixed on the way: a reversing sub flipped by a floor glance (Round 1 E5), a corner counted as a hit every step.
   Player lap (check pilot, no boost): 138.91 s (build 4: 137.6-141.0 s); in races 139.4-142.3 s.
2. **Keeping up with the AI — done.** Menu > Settings > AI difficulty Easy / Normal / Hard, **default Easy**, saved in `settings.json`.
   Changes only driving (pace held, braking, line wander, boost timing, small mistakes, shortcut rate), no rubber-banding. 3 races each:
   Easy AI 154.6 s (+9 % on the check pilot's 142.3 s; pilot 1st 3 of 3), Normal 141.9 s (+1 %, pilot 1st 2 of 3), Hard 133.3 s (= Round 3's
   AI with the new walls; pilot 1st 0 of 3).
3. **Beams on the start / finish road — done.** Cause: the course spline looped back 8 m before G0 (uneven control points since the 1.7x
   rescale); the promenade's barrier strips followed the loop diagonally across the road. Fixed (two even points), plus: road width and
   barrier height now change smoothly (build 4 had barrier ends facing traffic at climb0 and the ramp), the climb's cliff wall stood 1 m
   inside the road (moved out), the Fly sign's post stood on the ramp road (moved off), the finishing barriers overlapped the promenade's.
   Scan of the whole lap: no collider inside the route except the take-off lips under the two jumps, the cave exit channel's roof (1.5 m
   over the line) and the cave gates' glow frames (no collider). Pictures: `scan-start-line-*.jpg`.
4. **Start-line shoving — done.** Grid 11 m across x 10 m between rows (was 9 x 7), the AI launches row by row (0.15 s per row) and keeps
   its grid lane for 200 m. 25 starts: 0 resets, 0 back-outs, 0 wall hits, 0 touches in the first 10 s (Easy and Hard).
5. **Coral Vault tunnel — done, but short of the target.** On again. Openings longer, edge past the line, tunnel 20 m wide, exit
   narrowing smooth (to 17 m), the climb-out's right side open, stronger current; an AI that keeps hitting rock gives the shortcut up.
   25 AI runs: 25 through, 0 stuck, 0 resets; deliberate miss +3.3 s (build 4 idea: grinding), forced scrape runs 4-6.5 m off its line
   lose 0-6 s, never trapped. **Saves only 1.15 s** for a clean run (target 2-3 s): every AI run still touches the climb-out's walls
   (2-3 glances, ~10 % each). Reshaping the climb-out wants a look in a window (Dan's OK). Chip takes it on some laps.
6. **Near-miss gates — done.** Through a gate's plane outside the frame but within one gate width (8-30 m), right form, heading along the
   route: counted, +3 s, "MISSED G4 +3 s" on the HUD, in the lap time, results and race order (finishers now ordered by time). Shortcut
   sub-gates too, charged when the shortcut is finished. Check: road (G1), flight (G3, under its frame), water (G4), pool (G8) counted with
   +3 s, no reset; the cave gates G6 / G7 fill the cave, nothing to pass beside. Far miss and wrong form (plane over G4): rejected (TURN
   BACK, reset as before). The most a near miss can save anywhere: 1.32 s (G7) → penalty 3 s, so a near-miss lap is never faster. Same rule for the AI.
7. **Regression + soak — done.** 50 AI laps on Normal (four AI cars, time scale 3): 50 of 50 within 300 s, 0 bad gate sequences, 0 resets
   (Round 3: 27 Reset presses per 100 laps), laps 138.5 / 142.1 / 147.8 s, slit 9x, tunnel 6x. Round 1 (strip, lanes, E1-E6), Round 2 (lap, skip x3, Abi, best), Round 2b (nav x2, slow fps,
   screen), Round 3 (settings, settings2, quit, quitresults exit 0, race x2, shortcuts, waterfall x5): all PASS on the final build.
   Lanes' open-loop full-stick weave: drive 4 / boat 2 resets as in build 3.
8. **Wrap-up — done.** This file, `CLAUDE.md` (Settings: difficulty; walls; near misses; checks), `Docs/REUSE-AUDIT.md` section 8,
   commit + push, release `build-5` (0.4.0), disk cleaned.

#### For Dan to check (Round 4)
- **Walls:** brush the barriers, river banks, arch pillars and cave walls: a tad slower and bounced off, never stopped? Too soft / hard?
- **Difficulty:** Easy is the default; can you stay with them now? Try Normal / Hard (Menu > Settings > AI difficulty).
- **Start line:** no beams, no shoving at GO?
- **Near misses:** clip just outside a frame: "MISSED Gx +3 s", no U-turn. Is 3 s fair? Is the band too wide / narrow?
- **Coral Vault tunnel:** two holes in the cave floor after the first low curtain, back up before G7. Worth taking? It's only ~1 s;
  the climb-out wants reshaping in a window (your OK).
- **Wall hit sound / camera bump:** right amount? (the sound was never played here: all runs muted)
- **Deck FPS** with four cars (R3), as before.

- **Round 3 — Dan's three notes, AI rivals, shortcuts** (2026-11-13, release `build-4` / 0.3.0). Spec moved to `TODO-ARCHIVE.md`.

### Round 3 delivery (2026-11-13) — status per item
Evidence: `Docs/Checks/round3/` (reports per check, waterfall CSV traces before / after, pictures). Every run: built exe, `-batchmode -mute`,
no window, Below Normal, one heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run. Sound was
never unmuted. No window was opened.
1. **Flip flight controls — done.** Menu > Settings: "Invert flight / dive controls: On / Off" (with Screen and the FPS readout), saved
   in `settings.json` (old PlayerPrefs taken over once); View / F2 still toggles it. "Invert controls: Menu > Settings" shows for 5 s on
   the first flight until the setting has been chosen once; nothing asked at start-up. Check: stick pulled back 0.45 s: Fly +18.9 / Dive
   +16.2 deg (nose up) -> after the menu toggle Fly -18.9 / Dive -16.2 deg; a second process read it back (Fly -18.9 deg).
2. **Quit — done.** Menu > Quit game -> "Quit ShiftStorm? No / Yes" (No preselected); the results screen has Race again / Menu / Quit
   game. Exit code 0 from the menu, from the results, and started by the launcher (its hidden start-up check on the 0.3.0 starter ZIP):
   game "quit chosen", launcher "Child exit 0", launcher exit 0. A watchdog ends the process 6 s after Quit if the engine hangs.
   Steam Game Mode on the Deck: not testable here (Dan).
3. **Waterfall return "hung up a tad" — done; cause found.** Recorded every physics step, 5 runs (3 normal, boosted, slow). Not a
   frame hitch (worst 13-18 ms, one frame per run), not the hull catching (no contact from the ramp foot to the wheels; landing loss
   0.2-1.1 m/s). It was the change itself: the car left the lip at 44.5 m/s (68 % of its top speed; the boat's 89 %), floated 1.13 s
   at a flat 44.4 m/s (throttle does nothing in the air), and at that moment the camera re-measured "fast" against the car's 65 m/s
   instead of the boat's 50: field of view -3.4 deg and the camera 1.4 m closer = a visible stop; 1.5 s later only 46 m/s. Fix: into a
   faster form the speed's share of the old top speed carries over, eased in over the change (all changes); the camera's speed
   reference eases between forms; the car is pulled onto its wheels (airtime 1.13 -> 0.82 s) with a short visual spring, and keeps its
   forward speed through the touch. After (5 runs): Boat->Drive 47.3 -> 62.1 m/s at the wheels, 61.8 m/s 1.5 s after (was 46.1), field of
   view -0.1 to -0.5 deg, biggest drop 0.9-1.1 m/s, 0 frames over 16.7 ms.
4. **Three AI rivals — done.** Rocket Rae (red), Chip "Showtime" Sterling (blue), Vivi (purple), TO models with Abi's seated clips; the
   same vehicle, hull, tuning, boost meter, zones, gates and recovery, driven only through ShiftInput; bend speed planned ahead, side
   step to pass, back out when pinned (then Reset, like a player). 2 x 2 grid, countdown, place on the HUD and rival dots on the strip,
   results with order and times. No rubber-banding; knobs: braking margin, boost timing. Menu: Race (default) and Time Trial (alone,
   the only best-lap mode). Check (3 races): Rae 2:11.5 (slit), Vivi 2:16-2:19, check pilot without boost 2:21 (3rd), Chip 2:21-2:28;
   0 AI resets in the last races, 0 pass-throughs (hulls overlapping > 0.4 m).
5. **Shortcuts — Sky Arch slit done; Coral Vault tunnel deferred.** Slit: a buttress from the arch's inner pillar with a 22 m slit
   190 m beside the line, an updraft in it (108 m/s); stands in for G3, violet entry / exit sub-gates; the HUD guides into it. Saves
   1.55 s (23.53 -> 21.98 s, same start, no boost); a scrape there costs +1.10 s against the safe line (more than the gain), not
   trapped; full lap 139.17 -> 137.60 s. Geometry alone only gave ~0.7 s on this line, hence the updraft. Tunnel: built and measured,
   switched off (`ParadisePlunge.VaultTunnel`): the sub still caught the floor edge / chamber wall (-2.5 s), a miss could grind along it.
   Rae tries the slit on half her laps; Chip's tunnel plan is idle until the tunnel is back.
6. **AI soak — done.** 100 AI laps (25 races of four AI cars, time scale 3, physics still 60 Hz steps): 100 of 100 finished within 300 s,
   0 bad gate sequences, laps 131.5 / 138.6 / 153.8 s (min / median / max), slit taken 20 times. Worst stuck spot: the start line
   (cars shoved against the right edge at GO). First soak: 12 resets there, worst lap stuck 21 s; now the AI backs out and then
   presses Reset, worst lap 153.8 s; the shoving itself remains (27 Reset presses in 100 laps). Round 1 / 2 / 2b regression: all PASS
   (E5 fixed after the new scrape rule caught a reversing sub).
7. **Wrap-up — done.** This file, `CLAUDE.md` (controls, Settings / Quit, Race vs Time Trial, checks), `Docs/REUSE-AUDIT.md` section 7,
   commit + push, release `build-4` (0.3.0), disk cleaned.

#### For Dan to check (Round 3)
- **Waterfall return:** does the dive -> boat -> car change still hang? It should spring onto the wheels and keep going at ~60 m/s.
- **Settings:** Menu > Settings > Invert on the Deck; does View still toggle it there, or is it taken by Steam?
- **Quit on the Deck from Steam (Game Mode):** does it return to Steam, no black screen? Also from the launcher.
- **Race:** can you beat Rae (about 2:11 with the slit)? Too easy / hard? Note the start: cars still shove at GO (the soak's worst spot).
- **Sky Arch slit:** do you spot it from the flight (violet frame left of the arch)? Is the updraft fun or too much?
- **Coral Vault tunnel:** off for now; needs a look in a window (your OK) before it comes back.
- **Deck FPS** with four cars and drivers (R3): mangrove, flight, waterfall.

- **Round 2b — Deck fixes** (2026-11-13, release `build-3` / 0.2.1). Spec moved to `TODO-ARCHIVE.md`.

### Round 2b delivery (2026-11-13) — status per item
Evidence: `Docs/Checks/round2b/` (`check_report_all.txt`, `check_report_round2b_final.txt`, `check_report_best.txt`, player-view pictures).
Every run: built exe, `-batchmode -mute`, no window, Below Normal; measured audio output peak 0.000000 (AudioListener.volume 0) in every
run. Sound was never unmuted. No window was opened (the fullscreen look is left to Dan, as agreed).
1. **Where to go — done.** Cause of "wrong way in every direction": build 2's red cue in the air was the missed-gate cue "MISSED G3 - GO
   BACK", driven only by progress (25 m past the next gate's place) and refreshed every step whatever the heading, so a plane that passed
   the Sky Arch beside G3's frame (nothing to aim at; the line curves, a plane flies straight) was told "go back" whichever way it turned,
   then reset 5 s later; 75 m to the side it was also reset "out of bounds" with no warning. Fix (`ShiftVehicle.Route.cs`): WRONG WAY only
   when moving against the route direction at your position (> ~110 deg for 0.6 s) while on the route; off the route: THIS WAY + arrow;
   past a gate (through its plane): TURN BACK until you head back to it (then THIS WAY), reset timer paused while returning; in the air a
   420 m corridor with 4 s of THIS WAY before a reset (a U-turn at 80 m/s is ~330 m wide). Shown: every gate a big glowing frame (Beacon
   shader, no fog) — next gold, pulsing, light column; the one after cyan; HUD arrow (with gate name + distance, CLIMB / DIVE in the air
   and underwater), a diamond marker + distance on the next gate, and a **route strip** (picked over a minimap: simpler) along the top
   showing the lap by form, the gates and you. Route: white edge lines + amber chevrons on the road, rings through the whole flight, a buoy
   lane through lagoon and river, guide lights through the whole cave. Check (guided pilot steers only by the HUD): plane 70 m off the line
   facing away — passed G3 in 8.4 s, WRONG WAY never shown; plane facing backwards on the line — WRONG WAY at 0.6 s, passed G3 in 13.6 s;
   marker + arrow pictures in all 4 forms (`nav-*.jpg`).
2. **Much faster — done.** Speeds ~1.6-1.9x: Drive 65 (boost 82), Boat 50 (62), Fly 80 (100), Dive 38 (48) m/s, accelerations, grip and
   turn rates raised to match (all in `Resources/Tuning/*.asset`); top speed on the lap before -> after: Drive 34.5 -> 64.6, Boat 26.0 ->
   50.2, Fly 57.7 -> 80.6, Dive 28.0 -> 38.0 m/s. Course rescaled 1.7x on the ground plan (`ParadisePlunge.S`): line 4.3 -> 7.3 km, pilot
   lap 141.8 -> **140.7-141.0 s**, 0 resets, all 5 changes 0.60 s. Sense of speed: FOV 65 -> ~85 deg with speed (+6 boost), speed lines, boost
   shake, big speedometer. Frame rate: physics fixed 60 Hz, up to 0.25 s caught up per frame — the slow-fps check gives the same distance at
   968, 24 and 11 fps (100.0 / 100.0 / 100.8 %). **FPS + speed readout: R3 (right stick click) / F1**, or the menu. Round 1 strip and E1-E6
   all PASS; the lanes' open-loop full-stick weave now leaves the lane more often (drive 4 resets, build 2: 1; boat 1, build 2: 0): same
   wiggle, twice the speed. Bonus: build 2's "unexplained" hitches were mostly audio clips loading on first play (preload was off): fixed;
   PC check runs still show hitches that move from run to run (1 to 46 frames over 50 ms per lap, worst 0.1-1.1 s): not explained; likely this PC.
3. **Fullscreen — done (log only, as agreed).** `ScreenSetting.cs` (copied from Chastain Rush's): borderless full screen at the desktop's
   own resolution before the first scene, overriding Unity's remembered window; windowed only if chosen in the menu (Screen) or Alt+Enter
   (saved in `display.json`). The launcher passes no screen arguments. Logged at start-up: started directly, "would set FullScreenWindow
   3840 x 2160" (this PC); from the launcher (its hidden start-up check on the 0.2.1 starter ZIP): "started by the launcher, screen
   arguments: none; Unity started it Windowed 2844 x 1355 [its remembered window]; would set FullScreenWindow 3840 x 2160"
   (`launcher_startup.txt`). That remembered window is what the new start-up setting overrides. The real look: Dan.
4. **Wrap-up — done.** This file, `CLAUDE.md`, `Docs/REUSE-AUDIT.md` section 6, commit + push, release `build-3` (0.2.1), disk cleaned.

#### For Dan to check (Round 2b)
- **Fullscreen:** start the game on the Deck (and from the launcher): does it fill the screen at 1280 x 800? Menu > Screen / Alt+Enter switch.
- **Can you find the way?** Gold frame = next gate, cyan = the one after; the arrow when it's off to the side or behind; the strip at the top.
  Try leaving the route in the air on purpose: you should see THIS WAY and the arrow, never WRONG WAY unless you're really going backwards.
- **Speed:** is it fast enough now (about 145 mph on the road, 180 in the air)? Does each form still handle on the Deck's stick?
- **Deck numbers:** press R3 (right stick) for FPS + speed; tell me the FPS in the mangrove, the flight and the waterfall return, and any hitches.
- Is ~2:20 a lap still about right, and are the edge lines, chevrons, rings, buoys and cave lights too much / not enough?

- **Round 1 — foundation and the four modes** (2026-11-13, release `build-1` / 0.1.0). Dan's review: tried on PC, "seems fine, everything felt intuitive"; Deck not tried yet. Driver / location picked (see Round 2). Round 1 spec moved to `TODO-ARCHIVE.md`.

- **Round 2 — the full gray-box lap, Jurassic Par atmosphere** (2026-11-13, release `build-2` / 0.2.0). Dan's Deck test: runs, but no idea where to go ("wrong way" everywhere in the air), far too slow, not fullscreen. The Round 2 spec and per-item status are in `TODO-ARCHIVE.md`.

- **Round 2b — fixes from Dan's Steam Deck test** (2026-11-13, release `build-3` / 0.2.1). Status in the section below.

## Current Objective
None set: Round 5 is planned with Dan after he plays build 5 (0.4.0). Round 4's spec is in `TODO-ARCHIVE.md`.

## Next Steps (Do Not Start Yet)
- **Art slice beyond Round 2's first pass** (finished Jurassic Par scenery, real
  transforming vehicle). Waits on a reliable lap. New character / organic models
  go through the manual Tripo pipeline in `CLAUDE.md`; the real vehicle is
  built by script in Blender, not in Tripo.
- **Later experiment:** optional submerged bypass in Mangrove Run — only after
  the base lap works (concept doc).

## Technical Notes / Blockers
- Unity batch runs need the sandbox off (UPM named pipe). The world is built in code at start-up; there is no hand-made scene content.
- Frame times from batch-mode checks are CPU only; a windowed or on-Deck measurement is needed for real GPU numbers (needs Dan's OK
  for a visible window, or his Deck).
- Round 2's frame-time hitches: mostly audio clips loading on their first play (preloadAudioData was off on every SFX / voice clip);
  fixed in Round 2b (preload on). PC check runs still show a few hitches that move from run to run (1, 12, 23, 46 frames over 50 ms in four
  laps; worst 0.1-1.1 s), so likely this PC (check pictures being written, other processes); if the Deck shows them too: a profiled lap
  (development build). Scenery peaks near 3.9 M triangles after the 1.7x rescale.
