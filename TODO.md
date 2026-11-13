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
- **Round 1 — foundation and the four modes** (2026-11-13, release `build-1` / 0.1.0). Dan's review: tried on PC, "seems fine, everything felt intuitive"; Deck not tried yet. Driver / location picked (see Round 2). Round 1 spec moved to `TODO-ARCHIVE.md`.

- **Round 2 — the full gray-box lap, Jurassic Par atmosphere** (2026-11-13, release `build-2` / 0.2.0). Dan's Deck test: runs, but no idea where to go ("wrong way" everywhere in the air), far too slow, not fullscreen. The Round 2 spec and per-item status are in `TODO-ARCHIVE.md`.

- **Round 2b — fixes from Dan's Steam Deck test** (2026-11-13, release `build-3` / 0.2.1). Status in the section below.

## Current Objective — Round 2b: fixes from Dan's Steam Deck test (delivered 2026-11-13, waiting on Dan's Deck test)

**Dan's review of build 2 on the Steam Deck (2026-11-13):** "seems to play
fine" (runs OK on the Deck), with three problems, quoted:
- "I have no idea where to go. I am in the air flying in the direction I am
  pointed but everywhere I try to go is telling me it is the wrong way. There
  are no indicators whatsoever telling me where to go."
- "It doesn't move nearly fast enough. It is incredibly slow."
- "The game is not coming up fullscreen."
Dan also downloaded Abigail's Mixamo clips (done, already used in build 2).
Round 3 (AI and shortcuts) waits until these are fixed.

1. **Always show the player where to go.** A first-time player must be able
   to follow the lap with no help, in every form, especially in the air.
   - A **next-gate marker**: every gate is a big, clearly visible ring/arch
     (visible from far away, glowing, with the next one highlighted and the
     one after it dimmer), plus an **on-screen arrow** pointing at the next
     gate when it is off-screen or behind you.
   - A **visible route**: track edges on the road; buoy lanes / foam lines on
     water; a chain of rings or guide lights through the whole flight and
     the whole cave, not just at gates.
   - A small **minimap or route strip** showing your position and the
     route ahead (pick the simpler one, say which).
   - **Fix the "wrong way" cue:** only show it when you are really heading
     backwards along the route (compare your heading with the route
     direction at your position), never just because you are off the line
     or in the air between gates. When you are off the route, show
     "this way" with the arrow instead of "wrong way". Find and fix why Dan
     got "wrong way" in every direction in the air, and say what it was.
   *Check:* a new check-pilot run that starts in the air **off the line,
   facing sideways**, and must find the route using only what's on screen
   (arrow / next gate): no "wrong way" while it turns towards the route;
   pictures from the player's view of the marker and arrow in each form.
2. **Much faster.** Dan: "incredibly slow." Raise speeds a lot — start from
   roughly 1.6× the current tuning (Drive about 65 m/s, Boat about 50,
   Fly about 80, Dive about 38, boosts higher to match) and tune from there
   so each form still handles. Add a stronger **sense of speed**: camera FOV
   widening with speed, speed lines / wind streaks, a little camera shake
   at boost, a speedometer on the HUD. **Rescale the course** (it's built
   from the racer line) so the safe-line lap stays about 150 s at the new
   speeds, and keep every change, ramp and landing working at the new
   speeds (rerun all Round 1 edge cases and the Round 2 lap checks).
   - Also make sure the game isn't slow *because* of the frame rate:
     movement must not slow down when the frame rate drops (check the
     fixed timestep / maximum allowed timestep), and add an **FPS + speed
     readout** Dan can toggle (say which button) so he can report Deck
     numbers.
   *Check:* lap time and top speed per form, before vs after; Round 1 edge
   cases all pass again.
3. **Start fullscreen, on PC and Steam Deck.** Native resolution,
   borderless fullscreen by default, whether started directly or by the
   launcher, ignoring any old saved windowed setting. Do it the way
   Chastain Rush does on the Deck (read-only reference), and check what the
   launcher passes to the game. Add a fullscreen / windowed option to the
   menu and Alt+Enter.
   *Check:* report the screen mode and resolution the game sets at start-up
   (logged), from the launcher and from the exe. Opening a visible window
   to test this needs Dan's OK first — otherwise log it and leave the real
   look to Dan.
4. **Round wrap-up.** Per-item status, "For Dan to check", update
   `CLAUDE.md` (new speeds, the toggle button, how fullscreen is set),
   commit, push, publish release build 3 (0.2.1), clean the disk.

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

### For Dan to check (Round 2b)
- **Fullscreen:** start the game on the Deck (and from the launcher): does it fill the screen at 1280 x 800? Menu > Screen / Alt+Enter switch.
- **Can you find the way?** Gold frame = next gate, cyan = the one after; the arrow when it's off to the side or behind; the strip at the top.
  Try leaving the route in the air on purpose: you should see THIS WAY and the arrow, never WRONG WAY unless you're really going backwards.
- **Speed:** is it fast enough now (about 145 mph on the road, 180 in the air)? Does each form still handle on the Deck's stick?
- **Deck numbers:** press R3 (right stick) for FPS + speed; tell me the FPS in the mangrove, the flight and the waterfall return, and any hitches.
- Is ~2:20 a lap still about right, and are the edge lines, chevrons, rings, buoys and cave lights too much / not enough?

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

## Next Steps (Do Not Start Yet)
- **Round 3 — three AI rivals and the two shortcuts** (Sky Arch inside slit,
  Coral Vault low tunnel). Waits on a reliable lap.
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
