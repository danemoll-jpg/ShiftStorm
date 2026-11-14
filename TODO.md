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
- **Round 6 — split screen and camera views** (2026-11-13, release `build-7` / 0.6.0). Status below; spec stays under Current Objective until Dan's review.

### Round 6 delivery (2026-11-13) — status per item
Evidence: `Docs/Checks/round6/` (reports, pictures of both halves per form, every view per form and mid-change). Every run: built exe,
`-batchmode -mute`, no window, Below Normal, one heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every
run. Sound was never unmuted. No window opened. Controllers in the checks are Input System virtual gamepads (no real pad was used).
1. **Two-player split screen — done.** Ported from Chastain Rush (`Split/SplitScreen.cs`): Menu > 2 Players; player 1 = the device that
   opened it, player 2 joins with A on another controller (Enter on an unused keyboard); each player's input answers only its own device.
   Layout as Chastain Rush: **top / bottom, player 1 on top** (default) or left / right (Screen row, saved). Each half: own camera, HUD,
   arrow, strip, place, speedometer, cues, gate colours (next gold per player), underwater fog, Abi / rival driver animations. Player 2
   picks Rae / Chip / Vivi (their colour); 0-2 AI; Race or Time Trial; results list [P1] / [P2] on both halves. Either Menu pauses both;
   invert per player; a pulled-out pad pauses until it is back. Sound: one mix, both engines non-spatial at 0.7. Split cuts: LOD x0.6,
   shadows x0.6, far clip 1150 m (fog is solid at 1100), Detail layer 500 m, textures one mip down. Check: join, per-device throttle / X /
   invert, pause from P2, pad removed: all PASS; full lap, 2 players + 2 AI: P1 123.2 s, P2 124.9 s, both valid gate sequences, 0 resets,
   HUDs right in 122 of 122 samples, results PASS. **PC frame (both views, 1280 x 800): mean 7.7 ms, 95th pct 11.1 ms, 40 of 11,983 over
   16.7 ms** (one view: ~4.2 ms); peak 6.9 M triangles (Waterfall Return). Player 2's driver has no voice (only Abi speaks; TO voices for
   Rae / Chip / Vivi not wired).
2. **Camera views — done.** Ported from Chastain Rush (`UI/CameraViews.cs`): **X / V** cycles Chase / Far chase / First person (Abi's eyes;
   her head is folded away in her own view because she is one skinned mesh) / Front; per player, saved; right stick looks around (back =
   behind, eases back). X freed from drift / carve (RB / Left Shift only); `CLAUDE.md` Controls updated. Check: a full lap in each view,
   0 resets, pictures per form + mid-change: frames with the camera inside rock / walls or on the water surface: 0 in every view and form.
   Found on the way: the Round 5 Chase camera left the Coral Vault cave in 2 % of the sub's frames (454 of 21,820): now kept inside.
   In first person the eye holds still through the change and the model squashes and twirls under it (following her head looked across her shoulder).
3. **Regression + soak + wrap-up — done.** round6, round5, round4, round3 + settings2, quit / quitresults (exit 0), round2b, round2 + best,
   round1: all PASS (E6, failing in Round 5, passes now). Soak (Normal, 50 AI laps, time scale 3): 50 of 50, 0 bad gate sequences, laps
   129.2 / 132.9 / 161.5 s. Starts: 9 racer touches in 25 starts (Round 5: 6), 0 resets. This file, `CLAUDE.md`, `Docs/REUSE-AUDIT.md`
   section 10, commit + push, release `build-7` (0.6.0), disk cleaned.

#### For Dan to check (Round 6)
- **2 Players on the Deck:** Menu > 2 Players with the Deck's controls, then A on the external pad. Does each pad drive only its own car?
  Does Steam Input number them the way you expect (player 1 is whoever opened the page)?
- **Deck FPS in split screen (R3):** the climb / arch and the Waterfall Return with four cars; PC needs 7.7 ms for both views (one: 4.2).
  If it is under 60, say where: the next cut is render scale.
- **Layout:** top / bottom (default) or left / right (Menu > 2 Players > Screen) — which reads better on the 1280 x 800 screen? HUD size OK?
- **Camera views (X):** Far chase, First person (Abi's eyes, canopy and nose), Front: right height / FOV? Right stick look-around.
- **Change in first person:** does the squash-twirl still read?
- **Sound with two players:** both engines audible, not too loud (never played here: all runs muted).
- **Unplug a pad mid-race:** pauses with a note; plug it back in to go on.
- **Round 5 — speed, then the first art slice** (2026-11-13, release `build-6` / 0.5.0). Status below; spec stays under Current Objective until Dan's review.

### Round 5 delivery (2026-11-13) — status per item
Evidence: `Docs/Checks/round5/` (reports, before / after and Garage pictures, import previews). Every run: built exe, `-batchmode -mute`,
no window, Below Normal, one heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run. Sound was never
unmuted. No window opened.
0. **Vehicle prompts — done.** 12 prompts (3 directions x 4 forms: Tee Shot, Jetstream, Mudlark) with file names, folders, Tripo settings in
   `Docs/MODELS-NEEDED.md` (later edited for open cockpits, no glass); posted to Dan. **Dan picked Jetstream** and made its 4 images and
   4 Tripo models during the round (Tee Shot and Mudlark parked).
1. **Feel of speed — done.** Engine pitch now runs to each form's top speed (lap peaks: car 1.99, plane 2.31, boat 2.00, sub 1.50; build 5
   stopped at 1.8 at 45 m/s) + a wind layer (to 0.45); camera lower / closer at speed, FOV 65 -> 90 (98 boost) + a boost kick; air streaks,
   boat spray, sub bubbles (80-143 particles); road posts every 20 m + dashed centre line. Before / after pictures at the same spots:
   `before-after-*.jpg`. PC render mean 4.24 -> 4.18 ms on the lap, 0 frames over 50 ms (build 5: 5).
2. **Real speed — done.** +15 % top speeds (Drive 75, Boat 58, Fly 92, Dive 44 m/s), acceleration 22 -> 30 m/s2 (car, boat), AI on the same
   tuning. Lap 138.9 -> **122.7 s** (target 120-125), 0 resets, all 10 changes 0.600-0.607 s. Walls, near misses, scan: as Round 4.
   Waterfall return: car lands at 72 m/s, change done before touch-down (0.87 s airtime), FOV dip 0.2 deg. Round 1 E6 (test strip's
   surfacing ramp) now FAILS: at the new speed the strip's wheels-down zone fires 1 m past the ramp's end and a wheel ray still touches
   it, so "change done before the wheels touch" is not met there; on the real course it is (above). Not changed: a check / strip matter.
3. **Easy really easy — done (close to target).** Easy AI +18 % slower than the check pilot (target ~20 %; pilot won 3 of 3 without boost),
   Normal +6 % (target ~8 %), Hard -3 % (as before). No rubber-banding. The pace cap and mistakes now start 600 m after the grid: at the faster
   launch the held-back Easy cars were rammed at GO (15 touches in 25 starts) -> 6 touches, 0 resets, 0 wall hits (Round 4: 0 touches).
4. **Coral Vault tunnel — left as it is** (Dan: "not sure maybe"); reshaping the climb-out still needs a window and Dan's OK.
5. **Vehicle design stage 1 — done (system); waiting on Dan's models.** Swap-with-effect change (`FormSet`): four whole models, squash +
   quarter twirl, swap at the midpoint with a burst by form; physics handover unchanged; every transition check passes at 0.60 s (lap,
   strip). Garage (Menu > Garage): turntable, Design / Form (with the change) / Race with this design; pictures of the four gray-box forms
   and mid-change (`garage-*.jpg`). One-command import `python Tools/import_vehicles.py` (Blender: scale, origin, facing, floor, Seat, LOD1,
   texture; rival recolour in game; a separate glass dome for the sub) — tested on a Tripo-like stand-in (wrong size, facing, off-centre):
   came in 4.0 m long, the right way round, raced with 3 recoloured rivals, Abi seated. **Imported: Jetstream car, boat, plane, sub**
   (Tripo zips as downloaded; the importer now takes zips, caps meshes at 20,000 faces (from ~1.9 M) + a 6,000-face LOD; Dan's second
   "Boat" download is the sub, mapped in `Art/Incoming/Vehicles/Jetstream/import.json`; facing yaw -90). A clear glass bubble on every
   form over the cockpit rim (Dan's models have no glass); Abi on each model's seat. **Jetstream is the default design** (gray box still
   in the Garage); rivals in Jetstream, green parts recoloured. Garage pictures, 3/4 and side, all four forms: `jetstream-garage.jpg`.
   Lap in Jetstream: 122.76 s, 0 resets, render mean 4.20 ms, peak 4.36 M triangles; 2 races of four Jetstream cars (Easy): Abi 2:03.9,
   AI 2:24.9-2:29.9, never stuck, no pass-through; 12-lap AI soak (Normal) 12 of 12.
6. **Polished stretch — done, first pass.** Cliffside Climb: rock ledges + moss + ferns on the cliff, two waterfalls; Sky Arch: stacked mossy
   rock pillars and span, ferns, a pterodactyl nest with a perched pterodactyl; Lagoon Landing: beaches, boulders, cycads, two waterfalls off
   a cliff, drifting foam; 9 clouds; Jurassic fog now linear like TO (190-1100 m) so the arch reads from the ramp. Looks only (colliders,
   scan unchanged). Frame cost: peak 4.3 M triangles on the climb (build 5: 3.7 M; first try 6.6 M, fixed by per-cell batches and an 800 m
   cull distance), render mean 3.97 ms there. Still not TO: TO's Jurassic Par is far denser and greener (round leafy trees everywhere,
   striped fairways), the cliff face is still a flat slab with rock patches, the road stays tan, clouds rarely in frame. No new models needed.
7. **Regression + soak + wrap-up — done.** Final build: round5 (garage, lap, strip), round1 (E6 above), round2 + best, round2b, round3 +
   settings2, quit / quitresults (exit 0), waterfall, round4: all PASS except E6. Soak (Normal, 50 AI laps, time scale 3): 50 of 50 within 300 s, 0 bad gate sequences, laps 129.2 / 132.9 / 143.5 s (min / median / max; Round 4: 138.5 / 142.1 / 147.8), tunnel 9x, slit 8x.

#### For Dan to check (Round 5)
- **Speed:** does it feel fast now (wind, engine, camera, streaks, posts)? Too much FOV / shake? Can you keep up on Easy, and is Normal a fair race?
- **The change:** the new squash-twirl-swap with the burst: fun, or too busy at speed? (0.6 s as before.)
- **Jetstream:** how do the four forms look in the Garage and at speed? The glass bubble: right size / placement, or none on some forms?
  Is Abi visible enough in the open cockpit? Do the rivals' recoloured copies (red / blue / purple) read well?
- **The polished stretch** (climb, arch, lagoon): the right direction for the Tee'd Off look? What should change first?
- **Deck FPS** on the climb and the arch with four cars (PC: 4 ms; triangles up ~15 %).
- **Sounds** (never played here): the wind layer and the higher engine pitch.

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

## Current Objective — Round 7: music prompts first, loops and big jumps, then course art and sound

**Dan's review of build 7 (0.6.0), 2026-11-14:** "everything good so far."

**Dan's ask, quoted:** "Can we integrate Loops and some big jumps for cars?
all jumps will be a flight or even have a choice a jump if to continue as car
or fly."

Run unattended — don't stop to ask; anything needing a visible window or
Dan's OK gets skipped and noted. Keep the concept's rule: no flying anywhere
you like — wings only where the course offers them.

**Dan (2026-11-14):** "go ahead and do the course art and sounds. But at the
top of the list the music prompt so I can create it and integrate it. We want
the same kind of cartoony songs from the golf game."

0. **Music prompts for Dan — do this before anything else.**
   Write `Docs/AUDIO-NEEDED.md`, commit and push it, then **post the music
   prompts to Dan in the chat right away** (copy-pasteable), and carry on
   with items 1–5 without waiting.
   - **Match Tee'd Off's music.** Read-only, look in `Tee-dOff` for how its
     songs were made (saved Suno prompts, style tags, lyrics files, notes,
     file names and lengths of `Art/Music/*.mp3`, especially
     `course_jurassic`, `course_seaside`, `course_atlantis`, `title`). Base
     the new prompts on what you find, and say what you based them on. The
     songs must sound like they belong in the same cartoony golf game,
     just with racing energy.
   - **Suno songs (long, Dan makes them in Suno):**
     1. **Paradise Plunge race theme** — the main race song, Jurassic Par
        jungle flavour, upbeat and driving, loops cleanly, about 2–3 min.
     2. **Menu / Garage theme** — the same melody, relaxed.
     3. **Results / victory song** — short and bright (or an ElevenLabs
        sting if better).
     For each: a Suno style prompt (genre, instruments, tempo, mood, in
     Suno's tag style), instrumental or not (say which; match Tee'd Off),
     the length to aim for, how to make it loop, and the exact file name
     and folder to save to (e.g. `Art/Incoming/Music/`).
   - **Sound effects are NOT on Dan's list** (Dan, 2026-11-14: "code can
     choose sound effects I am more concerned about picking the music").
     Code picks and makes them itself in item 5. `AUDIO-NEEDED.md` is
     music only.
   - When Dan says he's done (or files appear in the named folders), import
     them at the next sensible point (finish the current item first);
     anything not made yet keeps its placeholder.

1. **Loop-the-loop on the road.** One big loop on the Beach Promenade (the
   start / finish road), built into the course line, in the Jurassic Par
   look (mossy stone / wooden track, glowing edge lines).
   - Car-only stick-to-track physics inside the loop (gravity towards the
     track surface while on it), a boost pad before the entry so a normal
     run always makes it, steering limited to keep you on the track.
   - Too slow (e.g. boost-starved after a crash): you don't fall like a
     stone — you slide back down / drop gently and get a quick reset at
     the loop's entry anchor, never stuck.
   - Camera follows smoothly through the loop in every view (Chase, Far,
     First person, Front), in split screen too.
   - Gates / progress: the loop's upside-down part must not confuse race
     progress or "wrong way".
   *Check:* full-speed, slow (no boost) and very slow entries; AI through the
   loop 25 times with 0 stuck; pictures from the player's view at 4 points
   of the loop.
2. **Big car jumps — most stay a car, some give a choice.** Dan's
   correction (2026-11-14): "not all jumps should be flight. some should
   give you a choice." So, three new big jumps on the road parts (say
   where you put them; suggested: two on the Beach Promenade, one on the
   Cliffside Climb before the main launch ramp):
   - **Two plain car jumps:** big, satisfying airtime as a car, wide
     landing ramp, keeps your speed. No wings — LB does nothing here.
   - **One choice jump** (pick the spot with room for a short flight
     branch), as below.
   **On the choice jump:**
   - Off the lip you're a car in the air. A **"Wings" button: LB**
     (keyboard **E**) during the first part of the airtime (HUD prompt
     "LB: FLY") turns you into the plane — otherwise you stay a car and land
     on the landing ramp across the gap.
   - **Car line:** long, satisfying airtime, a wide landing ramp, keeps
     your speed.
   - **Fly line:** a short flight branch with its own rings / sub-gates
     that rejoins the road at the next main gate (auto change back to car
     in a marked zone, like the Waterfall Return). Both lines about equal
     in time; the fly line can be slightly faster but riskier (rings to
     thread). Same near-miss / far-miss rules as other gates.
   - LB does nothing anywhere else (the main Cliffside launch stays an
     automatic flight, as now).
   - AI: picks car or fly before the lip and sticks to it; on Normal
     roughly half fly.
   *Check:* each plain jump (airtime, landing speed kept, no reset); both
   lines on the choice jump: time each, no reset; pressing LB late
   (after the window) does nothing; a short / slow jump still lands safely
   or resets cleanly; AI 25 laps through all three jumps with 0 stuck.
3. **Keep the lap honest.** The loop and jumps add time: keep the safe-line
   lap around 130 s (it's ~120–125 s now). Update the route strip, minimap
   arrow and segment splits for the new parts.
   *Check:* lap time and splits, before / after.
4. **Course art for the rest of the lap**, to the standard of the Round 5
   polished stretch (Cliffside Climb → Sky Arch → Lagoon Landing): Beach
   Promenade (with the new loop and jumps), Mangrove Run, Sunken Gateway,
   Coral Vault (the flooded cave), Waterfall Return, and the finish. Ported
   Tee'd Off Jurassic Par scenery (cycads, tree ferns, mossy cliffs and
   boulders, swamp, hot springs, waterfalls, dinosaurs and pterodactyls as
   scenery), toon water and foam, underwater light shafts / bubbles in the
   cave, splash and spray effects. Copy from Tee-dOff, never edit it.
   Readability first: gates, rings, the shortcuts, the jumps' landing
   ramps and every change stay clear. Missing essential models: Tripo
   prompts in `Docs/MODELS-NEEDED.md` for Dan, don't wait.
   *Check:* player-view pictures through every segment next to Tee'd Off's
   Jurassic Par; Code says what still doesn't match. Frame time on the full
   lap with four cars, single and split screen (PC); Deck check list for Dan.
5. **Sound pass.** Wire every sound into the game: engine / motor /
   propeller / sub per form with pitch over the full speed range, the
   change sound, wind, splashes, ring chimes, boost, wall scrapes, loop and
   jump whooshes, countdown, finish. Music: the race theme with the
   concept's touch — more energy in flight, muffled / softer underwater,
   bright again at the waterfall exit (filters / mix, not separate songs).
   **Code chooses the sound effects itself:** Tee'd Off's sound library
   first, then **Chastain Rush's** (`C:\Users\danmo\chastain_rush`: cars,
   planes, engines, skids, crowd) and **Hostile Horizon's**
   (`C:\Users\danmo\Hostile-Horizon`, a spaceship game — jets, whooshes,
   boosts, sci-fi changes) (Dan, 2026-11-14; all read-only copies), Chastain Rush's `VehicleSoundSynthesis.cs`
   for engines if it fits, and ElevenLabs only if an API key is already set
   in the environment (`ELEVENLABS_API_KEY`; never print or commit it) —
   don't ask Dan for sounds. List what each sound is and where it came from.
   Music: Dan's new songs where they exist, Tee'd Off placeholders otherwise.
   A Settings page volume: Music / Effects / Voices. In split screen one
   shared mix (as in Round 6).
   *Check:* sounds are checked as triggered and levels logged (runs stay
   muted); list exactly what Dan should listen for.
5b. **Barnaby is the official race caller** (Dan, 2026-11-14). His name is
   **"TeedOff Barker"** — use that name everywhere the player sees it
   (booth sign, results, credits, subtitles); "Barnaby" is only the
   folder / id in Tee'd Off's cast (`Tee-dOff/Assets/_Project/Art/Characters/Barnaby/`;
   his voice clips under `Resources/Audio/Voice/<id>/`, read-only).
   - Calls with cooldowns and variety: countdown / GO, the start, a lead
     change, a big jump or the loop, the choice jump (flew / stayed a car),
     a shortcut taken, a near-miss penalty, a wall hit, last stretch, photo
     finish, the winner and the results. Split screen: one caller for both.
   - Use his existing Tee'd Off clips wherever a line fits (list which).
   - New racing lines in his voice: find how Tee'd Off made his voice
     (ElevenLabs voice name / ID or notes, read-only). If an
     `ELEVENLABS_API_KEY` is already set, generate them yourself; if not,
     put the line list (short, funny, Tee'd Off-style) with the voice to
     use in `Docs/AUDIO-NEEDED.md` **after the music** as a lower-priority
     list for Dan, and use existing clips meanwhile.
   - If cheap: Barnaby's model in a small booth / on a podium at the start
     line or on the results screen.
   *Check:* lines triggered and logged in a race (runs stay muted); none
   repeats within its cooldown; list what Dan should listen for.
6. **Regression (incl. split screen and camera views) + 50-lap AI soak
   (Normal), wrap-up, release build 8 (0.7.0).** Per-item status for all 8
   items (0–6, 5b), "For Dan to check", update `CLAUDE.md` (Controls: LB / E =
   Wings on the choice jump; Settings: volumes), commit, push, publish, clean
   the disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (fill in at end of round)
- …

## Next Steps (Do Not Start Yet)
- **More vehicle designs** (Dan, 2026-11-13: "we can add different designs
  later"). Tee Shot and Mudlark prompts are parked in `Docs/MODELS-NEEDED.md`;
  the concept wants each driver's machine to fit their personality, so a
  natural use is giving rivals their own designs. Deferred, not decided against.
- **Coral Vault tunnel climb-out reshape** (saves ~1 s, target 2–3 s): needs a
  look in a visible window, with Dan's OK.
- **More tracks** from the concept: Clockwork Cascade (driving + boating),
  Cloud Orchard (driving + flying), Lantern Reef (boating + diving), each in
  a Tee'd Off setting — after Paradise Plunge is finished.
- **Playtest** (concept's evaluation plan): a few players of mixed skill,
  first attempt + three practice runs, per-mode questions; Code can log
  segment times, resets, missed gates and shortcut choices for it.
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
