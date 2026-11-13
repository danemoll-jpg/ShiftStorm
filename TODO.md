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

## Current Objective — Round 3: Dan's three notes, then AI rivals and shortcuts

**Dan's notes after playing build 3 (2026-11-13), quoted:**
- "Need to be able to flip flight controls"
- "Need a way to quit game"
- "The traverse from the submarine when you go back to being a car, it hung
  up a tad"
Dan (2026-11-13): do these three first, then the AI rivals and shortcuts
(items 4–7), all in one round. Dan starts it before bed, so it runs
unattended: **don't stop to ask.** Anything that would need a visible window
or Dan's OK gets skipped, noted and left for him.

1. **Flip flight controls, findable.** The invert toggle exists (View / F2)
   but Dan didn't find it, and on the Steam Deck the View button may be
   taken by Steam. Add a **Settings** page in the menu with "Invert
   flight / dive controls: On / Off" (applies to flying and diving, saved),
   alongside the Screen setting and the FPS readout. Keep the button as a
   shortcut. Show a short on-screen note the first time you fly:
   "Invert controls: Menu > Settings". Ask nothing at start-up.
   *Check:* toggle in the menu → climb/dive direction flips in Fly and Dive,
   survives a game restart.
2. **Quit the game.** "Quit game" in the menu (Menu / Esc / the controller's
   Menu button), with a quick "Quit? Yes / No" so it can't be hit by
   accident, and the results screen gets a Quit too. Make sure the game
   closes cleanly when started by the launcher (and from Steam on the Deck:
   it should return to Game Mode, not leave a black screen).
   *Check:* quit from the menu and from results; process exits with code 0;
   from the launcher, the launcher also exits (or returns), as Chastain Rush
   does.
3. **The dive → boat → car change at the Waterfall Return "hung up a tad".**
   Find out what Dan felt before fixing it — it could be:
   - a frame hitch (something loading / building at that moment),
   - the vehicle physically catching (hull or a part touching the water
     ramp, the lip or the road edge; speed dropping suddenly),
   - the change itself pausing (speed blend too strong, steering or
     throttle easing in too slowly, camera lag).
   Record a run through it (player-view pictures every few frames, speed,
   frame time, contacts and form state each step) and say which it was, with
   the numbers. Fix the real cause. The car form must take over smoothly
   with no visible stop, and the landing should feel like springing onto
   the wheels (concept: "the vehicle springs onto its wheels").
   *Check:* before/after speed trace through the surfacing pool, the ramp
   and the landing; no speed drop beyond the planned blend, no frame over
   16.7 ms there, on 5 runs including a boosted one and a slow one.
4. **Three AI rivals.** Same vehicle, same handling limits, boost supply,
   transition zones, gates and recovery as the player — the AI uses the
   player's controls (the Chastain Rush rule: read `RoadDriver.cs` /
   `PlanePilot.cs` read-only for ideas, write ShiftStorm's own).
   - Drivers: three other Tee'd Off characters from the audit (e.g. Rocket
     Rae, Chip "Showtime" Sterling, Vivi), each with their own vehicle color
     — cosmetic only, same stats. Reuse Abi's seated clips on them.
   - Drive and boat along the racer line with side offsets for overtaking;
     3D paths with target height / depth and look-ahead in the air and the
     cave. Slow down **before** tight bends, not after hitting something.
   - Grid start with the countdown; race position (1st–4th) on the HUD and
     the route strip; results show the finishing order and times.
   - **No rubber-banding.** One difficulty for now, tuned so a decent player
     can win; difficulty, when added later, changes braking margins and
     boost timing first.
   - Race (vs AI) and Time Trial (alone) both in the menu.
   *Check:* full races with the check pilot plus 3 AI; AI never stuck, no
   AI passes through another or the player.
5. **The two shortcuts**, per the concept (fixed geometry, discoverable
   from the safe line, never mistaken for the only route):
   - **Sky Arch inside slit:** a narrow opening beside the main arch, saves
     1–2 s; a scrape loses at least the gain; both paths enter the same
     lagoon approach gate.
   - **Coral Vault low tunnel:** a low passage under the main cave chamber,
     saves 2–3 s; needs early depth alignment and a tighter exit; both paths
     rejoin before the surfacing pool.
   - Each gets its own entry / exit sub-gates between the same main gates.
   - AI: safe route by default; one rival tries each shortcut at a set rate,
     choosing before the entry and sticking with it until the rejoin.
   *Check:* measured time saved on each shortcut (same entry speed and
   boost), full-lap comparison included; a deliberate miss uses normal
   recovery and loses time without trapping the racer.
6. **AI soak test.** 100 unattended AI laps (batch mode, muted, Below
   Normal; spread over the night, one heavy job at a time). Pass: at least
   95 finish within twice the target lap time, and **every counted finish
   has a valid gate sequence**. Report stuck spots with positions and fix
   the worst ones.
7. **Round wrap-up.** Per-item status (done / not done / deferred, and why)
   for all 7 items, "For Dan to check", update `CLAUDE.md` (Controls table
   with the Settings page and Quit; Race vs Time Trial), commit, push,
   publish release build 4 (0.3.0), clean the disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (fill in at end of round)
- …

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
