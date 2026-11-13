# ShiftStorm — TODO archive

Older rounds, moved word-for-word from TODO.md. Grep this file; don't read it in full.

## Current Objective — Round 1: foundation and the four modes

**Goal:** prove the riskiest thing first — one test vehicle that drives,
flies, boats and dives, and changes form without losing control. No track art,
no driver, no AI this round. **No Tripo models needed from Dan this round.**

Important fact (Dan, 2026-11-12): Chastain Rush has **separate** cars and
planes. No vehicle there changes form. Its driving and flying code is a
starting point; the transformation system, boating and diving are all new.

1. **Repo setup.** In this folder (git already initialised, remote
   `https://github.com/danemoll-jpg/ShiftStorm.git`, branch `main`): create the
   Unity project (6000.6.1f1, URP, Input System only), Unity `.gitignore`,
   `Docs/`. If `Docs/Teed_Off_Racing_Spin_Off_Concept.docx` is there, read it;
   if not, work from this file and note it as missing. Add the `-mute` command-line flag that sets
   master volume / `AudioListener.volume` to 0 from the first frame.
   *Check:* project opens in batch mode with no errors; a `-mute` run reports
   audio output level 0.
2. **Launcher.** Copy the launcher pieces from `chastain_rush` as listed in
   `CLAUDE.md`, rename to `ShiftStormLauncher.exe`, make a new signing key once
   (`python Tools/New-LauncherKey.py`, kept in `Builds/PublisherPrivate/`,
   git-ignored), point the update catalog at
   `https://github.com/danemoll-jpg/ShiftStorm-Releases`, rewrite the starter
   README for ShiftStorm.
   *Check:* launcher builds, finds this round's release and starts the game.
3. **Reuse audit → `Docs/REUSE-AUDIT.md` (read-only).**
   - `chastain_rush`: how its car controller, plane controller, camera,
     collisions and race progress are built; what can be copied as-is, what
     needs changes, what doesn't fit a single shape-changing vehicle.
   - `Tee-dOff`: characters (models, rigs, expressions), voice clips, music
     cues and tropical / water scenery that could be reused. Mark each:
     reuse directly / adapt / build new.
   - End with a **recommended prototype driver and location** for Dan to pick
     (2–3 options each, one line why).
   *Check:* doc exists and lists real file paths for everything it names.
4. **One racing controller, four handling modes.** A single vehicle with one
   shared framework (position, forward intent, boost and race progress kept
   across changes) and four handling modes, per the concept's table:
   - **Driving:** responsive steering, grippy recovery, controllable drift.
     Start from Chastain Rush's car code.
   - **Flying:** forward-moving arcade flight, bank assistance, limited pitch,
     auto-levelling, no stalls. Start from Chastain Rush's plane code.
   - **Boating (new):** broad surface slide, low wave hops, current zones that
     push the boat.
   - **Diving (new):** slower turning, depth control on the vertical stick,
     auto-levelling, no oxygen or ballast.
   - Same inputs in every mode: accelerate, brake, steer, boost, reset, plus one
     secondary button (drift on land, tighter carve on water). **Write the
     exact button mapping (controller + keyboard) into `CLAUDE.md`.** Invert-
     axis setting for air/underwater.
   - One shared rechargeable boost meter (fixed refill rate, carries across
     forms, visible on screen).
   - All tuning values in one place (ScriptableObject per mode) so handling can
     change without touching the track.
   *Check:* each mode driven in its own flat lane; a short clip/screenshots of
   each from the player's view.
5. **Placeholder transforming vehicle.** Built by script in Blender
   (`Tools/Blender/`), simple shapes, but with **separate parts**: shared body
   and cockpit, wheels, wings, hull fins, canopy. Each change animates in
   0.5–0.8 s, steering stays live, no camera cut, no input lock. The visual
   change and the physics handover share one known timing point, so a
   half-open wing never catches a gate.
   *Check:* each of the six changes used on the strip (item 6) times between
   0.5 and 0.8 s, logged.
6. **Transition test strip (gray boxes).** One straight-ish test course:
   road → launch ramp → air → water landing corridor → boat section with a
   current → dive portal → short underwater stretch → surfacing pool → water
   ramp → road landing. Rules:
   - Changes happen automatically in marked zones (with car / wing / boat /
     submarine signs before each).
   - A change fires once, then needs a new valid zone before another can
     fire — overlapping water volumes must not flip boat ↔ submarine.
   - Speed is blended into the new mode's sensible range, not reset and not
     carried over unsafely.
   - Gentle, limited alignment help if the vehicle enters at an extreme angle.
   - Landing in water outside the landing corridor → safe reset, not a
     shortcut.
   - Manual reset and automatic recovery (stuck / out of bounds / no progress)
     send the vehicle to the last safe anchor with the right form, a modest
     speed and a short no-collision grace period.
   *Check:* one full run of the strip, start to finish, with no reset needed.
7. **Edge-case checks** (one each, in the built exe, muted, off-screen or
   minimised, with player-view pictures Code actually looks at):
   1. low-speed ramp entry;
   2. boosted water entry;
   3. steep / nose-down water entry;
   4. reset pressed in the middle of a change;
   5. crossing the same trigger twice (back and forth);
   6. surfacing pool → road: dive → boat → drive completes before the wheels
      touch.
   Report pass / fail for each and frame time around each change.
8. **Round wrap-up.** Per-item status in this file, "For Dan to check" list,
   update `CLAUDE.md` (commands, standing facts, "Where things stand"),
   commit, push, **publish a release to ShiftStorm-Releases**, clean the disk.

### Defaults Code should use unless Dan changes them
- **Publish a release every round** (so Dan can test on the Steam Deck).
- **Target hardware: Steam Deck, 60 fps.** Report frame times from the PC
  build for now; Dan checks the Deck.

### Round 1 delivery (2026-11-13) — status per item
Evidence: `Docs/Checks/round1/` (report `check_report.txt` + player-view pictures). Every run: built exe, `-batchmode -mute`, no window,
Below Normal; measured audio output peak 0.000000 (AudioListener.volume 0) in every run.
1. **Repo setup — done.** Unity 6000.6.1f1 URP, Input System only, `.gitignore`, `Docs/` (concept doc moved to `Docs/`, read), `-mute`
   (`QuietRun.cs`). Check: batch setup and build with 0 compile errors; `-mute` runs report output level 0.
2. **Launcher — done.** `ShiftStormLauncher.exe`, new key in `Builds/PublisherPrivate/`, catalog on ShiftStorm-Releases, README rewritten.
   Check: extracted the published starter ZIP, launcher auto-check said "Up to date", started build 1, ready handshake PASS (hidden, muted).
3. **Reuse audit — done.** `Docs/REUSE-AUDIT.md`, every path verified on disk; section 4 lists what was copied and changed.
   Picks for Dan: drivers Rocket Rae / Chip "Showtime" Sterling / Vivi; locations Gull's Point Links / Lost City Links / Jurassic Par.
4. **One controller, four modes — done.** `ShiftVehicle` (+ `.Drive/.Boat/.Fly/.Dive/.Recovery`), tuning in `Resources/Tuning/*.asset`,
   shared boost meter on the HUD, invert setting, mapping in `CLAUDE.md`. Check: each lane driven 16 s (drift and carve seen, fly/dive
   climb-dive weave), pictures `*-lane-b.jpg`. The drive lane's wide scripted weave left the lane once and auto-recovery reset it (correct).
5. **Placeholder transforming vehicle — done.** `Tools/Blender/shiftstorm_vehicle.py`, 14 separate parts; one hull collider for all forms
   (parts are visual only, so nothing half-open can catch a gate); physics hands over at the change's start. Check: every change on the
   strip and in the edge cases timed 0.600 s (in 0.5-0.8). The strip has five changes, not six: D->F, F->B, B->Dv, Dv->B, B->D.
6. **Transition strip — done.** Built at start-up (`WorldBuilder.cs`), signs carry the vehicle in its next form, zones fire once, gates
   G0-G10 separate from zones, anchors and recovery. Check: one full run, finished in 50.9 s, 11/11 gates, 0 resets.
7. **Edge cases — done, all PASS** (frame time worst / mean in the window around each change, PC CPU, off-screen):
   1 low-speed ramp (10 m/s, flight entered at 30.1, 8.3 / 0.5 ms); 2 boosted water entry (boat 30.0 m/s, level in 0.38 s, 18.1 / 0.3 ms);
   3 nose-down -50 deg entry (level in 0.25 s, 12.0 / 0.3 ms); 4 reset 0.25 s into a change (back in car form 3.3 m from the anchor, grace
   on, wing zone fires again); 5 dive zone crossed forward, back and forward: 1 change, no boat/sub flip; 6 surfacing pool -> road: car
   form complete 0.52 s before the wheels touched (10.9 ms worst). Whole run: mean 0.26 ms, 95th pct 0.44 ms, worst 42 ms (not at a change).
   These are PC numbers in batch mode (CPU frame time, GPU not waited for): not Steam Deck numbers.
8. **Wrap-up — done.** This file, `CLAUDE.md`, commit + push, release `build-1` published (ShiftStorm-Releases had no commits, so a
   README was added there first: a release needs a commit to tag), disk cleaned.

#### For Dan to check (Round 1)
- On the Deck: install `ShiftStorm-0.1.0-Full.zip` from ShiftStorm-Releases (non-Steam game, Proton Experimental); does it reach 60 fps,
  especially at the five changes?
- Drive the strip: does each change feel in control (0.6 s, steering live)? Is the car fun with the drift (RB)?
- Boat: is the slide too broad / the current (side push near the buoys) readable? Diving: does it feel different from flying?
- The default climb / dive direction (pull back = up) and the invert toggle (View button).
- Pick a prototype driver and location from `Docs/REUSE-AUDIT.md` section 3.
- Placeholder look: the folded wings still show at the sides in car form; the canopy tilts open in car / boat form. Fine for gray box?

## Current Objective — Round 2: the full gray-box lap, Jurassic Par atmosphere

**Dan's Round 1 review (2026-11-13):** tried it on his PC. It "seems fine,
everything felt intuitive." No handling changes requested. Not yet tried on
the Steam Deck.

**Decisions (Dan, 2026-11-13):**
- **Driver: Abi** (Abigail, `Tee-dOff/Assets/_Project/Art/Characters/Abigail/`).
  Dan says the driver doesn't matter much; Abi is the pick (confirmed: Abi = Abigail).
- **Setting: Jurassic Par atmosphere.** Don't reuse a Tee'd Off course
  layout. Build the Paradise Plunge race route (same eight segments, same
  order, same timing budget) as a **new** course that looks and sounds like
  Jurassic Par: cycad palms, tree ferns, mossy boulders and cliffs, waterfalls
  off the cliffs, swamp, hot springs, dinosaurs as scenery. Source:
  `JurassicBuilder.cs` and `ArtStyle_Jurassic.asset` in Tee-dOff
  (read-only), music `course_jurassic`. The course name "Paradise Plunge"
  stays as the working name for now.

**Goal:** one complete lap you can race start to finish against the clock,
in gray boxes but already in the right light, colors and sound, with Abi
in the seat.

1. **Paradise Plunge lap, gray box.** All eight segments in order, each sized
   from Round 1's tuned speeds and the concept's target times (150 s total on
   the safe line). Same transition rules, signs, zones and recovery as the
   Round 1 strip. How each segment reads in Jurassic Par:
   1. Beach Promenade → a road along the jungle lakeshore (start / finish).
   2. Cliffside Climb → two drift corners up mossy cliffs to the launch ramp.
   3. Sky Arch → flight through a huge mossy rock arch over a valley with
      dinosaurs below (scenery only, no hazards yet).
   4. Lagoon Landing → splashdown in a wide jungle lake.
   5. Mangrove Run → a swamp river S-bend with visible current ribbons.
   6. Sunken Gateway → a descending stone opening into a flooded cave under
      the cliffs.
   7. Coral Vault → the flooded cave chamber: mossy rock arches, roots and
      hot-spring vents (or glowing crystals) in place of coral, same vertical
      slalom.
   8. Waterfall Return → surfacing pool behind a waterfall, water ramp
      through the curtain, landing on the finish road.
   Keep the horseshoe layout from the concept: the underwater return runs
   physically **below** the road, so race progress must not confuse the two.
   *Check:* one full lap by the check pilot, all gates in order, 0 resets;
   report each segment's time against its target.
2. **Ordered gates G0–G10 and race position by progress**, per the concept:
   G10 only counts after every gate in order; reverse crossings and skipping
   the underwater part by air are rejected; a missed gate gives a return cue
   or reset. Position = completed gates + progress along the current
   segment (not distance or height).
   *Check:* a scripted run that tries to skip the dive section is rejected.
3. **Time trial, results and restart.** Countdown, lap timer, per-segment
   splits, results screen with best time (saved), restart. Course menu keeps
   the Round 1 strip and lanes for testing.
   *Check:* finish → results show lap + splits → restart works; best time
   survives a restart of the game.
4. **Jurassic Par look and sound, first pass.** Port Tee'd Off's toon look to
   URP for this course only: `ArtStyle_Jurassic` colors, toon bands, outline,
   sky and fog; a toon water surface (from `ToonOcean.shader`) for the lake
   and river; underwater fog and color for the cave. Scenery stays simple:
   a few ported or Blender-scripted cycads, ferns, boulders and one waterfall
   curtain — enough to read the place, not a finished island. Music:
   `course_jurassic` (+ intro); ambience `amb_jungle`, `amb_bubbles`,
   splashes. Shaders and assets are **copied** from Tee-dOff, never edited
   there.
   *Check:* player-view pictures of every segment; Code looks at them and
   confirms each segment is readable (next turn / next change visible early).
5. **Abi in the cockpit.** Import Abigail (Humanoid) into the cockpit,
   sitting at the right scale in all four forms (canopy closes over her for
   diving). Use whatever existing clip gives a usable seated pose; otherwise
   hold a fixed seated pose. In `Docs/MODELS-NEEDED.md`, list the Mixamo
   clips Dan should download for Abigail (real Mixamo search names, what
   each is for: seated driving / steering, a reaction for changes, a
   celebration), and **say in the round report that this is waiting on
   Dan**. Check which Abigail voice clips exist and use a few (with
   cooldowns) for the finish and a reset.
   *Check:* pictures of Abi seated in each form, nothing poking through the
   body or canopy.
6. **Steam Deck performance check.** The real game now has scenery, so
   measure it properly: report PC frame times for the full lap, and give Dan
   a short list of what to watch on the Deck. Any test that needs a visible
   window: **ask Dan first.**
7. **Round wrap-up.** Per-item status, "For Dan to check", update `CLAUDE.md`,
   commit, push, publish release build 2 (0.2.0), clean the disk.

### Round 2 delivery (2026-11-13) — status per item
Evidence: `Docs/Checks/round2/` (`check_report_round2.txt`, `check_report_best.txt`, `check_report_round1_regression.txt`, player-view pictures).
Every run: built exe, `-batchmode -mute`, no window, Below Normal; measured audio output peak 0.000000 (AudioListener.volume 0) in every run.
Sound was never unmuted: the music, ambience, splashes and voice lines were checked as playing / triggered, not listened to.
1. **Paradise Plunge lap — done.** `ParadisePlunge.cs`: eight segments in order, built from one racer line (4.3 km); horseshoe with the flooded
   cave running under the finishing road. Check: one full lap by the check pilot, 11/11 gates in order, 0 resets, **141.8 s** (target 150).
   Segments (time / target): Promenade 18.2/20, Climb 17.5/20, Sky Arch 22.4/25, Lagoon 14.3/15, Mangrove 22.4/20, Gateway 11.3/10,
   Coral Vault 20.9/25, Waterfall Return 14.9/15. The pilot drives the ideal line at full throttle without boost, so a person will be slower.
   Every change took 0.60 s.
2. **Ordered gates and race position — done.** Gates count only in order, forward, and in an allowed form. Going back through a gate shows a
   "wrong way" cue. Going past a gate without earning it shows a "go back" cue, then resets you to an earlier anchor. Progress is searched
   near the last progress in 3D, so the cave under the road is never mistaken for the road. Position = gates passed + progress toward the
   next gate (`Course.RaceDistance`), ready for Round 3's rivals.
   Checks, all rejected as they should be: flying over the cave (no gate counted, the "missed G6" cue, reset to the boat anchor before
   G6); reversing through G0 (cue shown, still 1 gate); crossing the finish line from the pool without G8/G9 (not finished).
3. **Time trial, results, restart — done.** 3-2-1-GO countdown, lap clock, per-segment splits with their targets, a results panel (lap, splits,
   best, NEW BEST), the best lap saved to `besttimes.json`, Reset after the finish restarts, and a course menu (Menu / Esc) that keeps the
   Round 1 strip and lanes. Checks: results and restart PASS; a second game process read the saved best lap (141.82 s), PASS.
4. **Jurassic Par look and sound — done (first pass).** Tee'd Off's toon shaders ported to URP (`Assets/Shaders`: toon bands, outline, toon
   water, sky); `ArtStyle_Jurassic` colours, haze and sun; underwater fog in the cave; cycads, tree ferns, mossy boulders, the rock arch,
   waterfall curtain, glowing crystals and hot-spring vents; Tee'd Off dinosaurs (bronto, trike, T. rex) standing in the valley and
   pterodactyls circling. Sound: `course_jurassic` intro then loop, the jungle and bubbles ambience, splashes, far dino calls. Check: I looked
   at player-view pictures of all 8 segments and fixed what didn't read: the dive entrance is now visible down a straight, the wing sign sits
   before the ramp, the cave arches have glowing edges, and the white mist sheet is gone.
5. **Abi in the cockpit — done.** Abigail (Humanoid), scale 0.95, in an open cockpit tub under a bigger canopy (Blender script updated).
   She uses Dan's 6 shared Mixamo clips (`Resources/Drivers/Animations`): Driving loop, Sitting Idle on the grid and after the finish,
   Thumbs Up at each change, Disapproval after a reset, Clap at the finish, horn at GO. Her legs stay forward in the seat and her hips are
   pinned to it, because the clips sit on a chair. Voice, with cooldowns: finish (win / quipgood), reset (disaster / grunt / trouble),
   waterfall leap (laugh / lucky). Check: in all 4 forms, 0 vertices through the closed canopy and 0 below the hull; pictures `*-abi-*.jpg`.
   `Docs/MODELS-NEEDED.md`: received; nothing is waiting on Dan.
6. **Steam Deck performance — partly done.** PC numbers only: every lap frame was rendered at 1280x800 with the GPU waited for (GTX 1660 Ti):
   mean 3.6 ms, 95th percentile 5.9 ms; 99 of 28,789 frames went over 16.7 ms, with hitches up to 0.7 s. The hitches are not explained:
   an earlier run on the same lap peaked at 96 ms. Peak about 3 million triangles (Mangrove and the return, scenery with outlines and
   shadows). Draw-call counters are not available in a release build. Real Deck numbers need the Deck or a visible window: **not done**,
   as the rules say (ask Dan first).
7. **Wrap-up — done.** This file, `CLAUDE.md`, `Docs/REUSE-AUDIT.md` section 5, commit + push, release `build-2` (0.2.0), disk cleaned.
   Round 1 regression (strip, lanes, 6 edge cases) still passes; the drive lane's wide weave resets once, as in Round 1.

#### For Dan to check (Round 2)
- **On the Deck:** install `ShiftStorm-0.2.0-Full.zip` from ShiftStorm-Releases (or let the launcher update). Drive a full lap. Is it
  steady at 60 fps? Watch for hitches (the PC showed some, unexplained), and for the mangrove, the waterfall return and the valley flight
  (the most triangles).
- Race a lap: is ~2:20-2:40 about right, and do the segments feel the right length? Coral Vault is short, Mangrove a little long.
- Can you see each change coming (wing sign and streamers, foam corridor and boat sign, dive sign and lights, the pool's ramp and waterfall)?
- The look: is the haze too strong? Are the toon water, sky and outlines what you want from Jurassic Par?
- Abi: how she looks in the open cockpit and the canopy; do the clips (thumbs up at every change, clap, horn at GO) feel right or too much?
- Sound: music, jungle/bubbles ambience, splashes, Abi's lines. Never heard during testing (always muted).
- The course menu (Menu / Esc) and restart; best time kept between sessions.

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

## Round 3 spec (delivered 2026-11-13 as build 4 / 0.3.0; status in TODO.md)

### Round 3: Dan's three notes, then AI rivals and shortcuts

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

## Round 4 (archived 2026-11-13): walls, keeping up, the start line, the tunnel, near-miss gates

**Dan's notes after racing build 4 (2026-11-13), quoted:**
1. "Brushing up against the wall shouldn't stop you. Should slow you a tad
   and bounce you off."
2. "Couldn't keep up with the other racers."
3. "There are two beams in the middle of the road at the start/finish line."
4. "Missing a gate should be a time penalty if you just miss it. Still not
   allow you to cheat but just in case you miss it so you don't have to
   circle back."
Dan: fold these into the next round. Run unattended — don't stop to ask;
anything needing a visible window or Dan's OK gets skipped and noted.

1. **Walls: glance off, don't stop.** In every form (road edges, cliff
   walls, arch / buttress, river banks, cave walls), a glancing hit should
   cost a little speed and push you back along the wall, never stop you or
   spin you. Only a near head-on hit costs real speed, and even then you
   keep going.
   - Make the hull slide along walls (low friction against course walls,
     velocity into the wall removed, velocity along it kept minus a small
     scrape loss — about 5–10 % for a glancing hit, scaled by angle), plus
     a short sideways nudge away from the wall and a small camera bump /
     scrape sound so it reads as a hit.
   - No sticking on wall seams, corners or mesh edges; steering stays live.
   - Keep the existing rule that a scrape in the slit costs more than the
     shortcut saves.
   *Check:* scripted hits at 10°, 25° and 45° in each form: speed kept
   after each (report %), no stop, no spin, no reset; and a run that holds
   against a wall for 3 s keeps moving forward.
2. **Keeping up with the AI.** The AI drives the ideal line with perfect
   boost timing; Dan, on the Deck, couldn't stay with them.
   - Add a **difficulty setting** in Menu > Settings: Easy / Normal / Hard.
     **Default: Easy.** Difficulty only changes how the AI drives —
     braking margins, line accuracy, boost timing and how often they
     make small mistakes — never their vehicle stats, and **no
     rubber-banding**.
   - Target lap times for the AI pack: Easy about 10 % slower than the
     check pilot's safe lap without boost; Normal about equal to it; Hard =
     today's AI.
   - Item 1 should also help: report the player's lap before / after the
     wall fix.
   *Check:* AI lap times per difficulty (3 races each), next to the check
   pilot's lap; the check pilot finishes 1st on Easy without boost.
3. **The two beams on the start / finish road.** Nothing solid or
   blocking may stand on the racing surface. Move the start / finish gate's
   posts outside the road edges (a banner overhead is fine), and **check
   every gate, sign, ring and route guide on the whole course** for posts
   or beams inside the route's width (road, water lane, flight rings, cave
   lights) — list any found and fix them.
   *Check:* a scan of the course listing every collider inside the route
   width (should be none except intended ones like the slit buttress);
   a player-view picture of the start line.
4. **Start-line shoving.** The Round 3 soak's worst spot: cars pushed
   against the right edge at GO. Space the 2 × 2 grid and stagger the AI's
   launch slightly so nobody gets pinned.
   *Check:* 25 starts, 0 resets in the first 10 s.
5. **Coral Vault low tunnel, back on.** Fix what made it unusable in
   Round 3 (the sub catching the floor edge / chamber wall, a miss
   grinding along it) — item 1's wall behaviour should help. Widen /
   smooth the entry and exit, keep it demanding (early depth alignment,
   tighter exit) and worth 2–3 s for a clean run. Chip uses it on some
   laps.
   *Check:* time saved (same entry speed, no boost), a deliberate miss and
   a scrape run (lose time, never trapped), 25 AI laps through it with 0
   stuck. If it still isn't right without a visible window, leave it off
   and say exactly what's wrong.
6. **A near-miss of a gate = time penalty, not a U-turn.** If you pass
   just beside a gate (through its plane but outside the frame, within
   about one gate-width of it), in the right form, and carry on along the
   route, the gate counts with a **time penalty** added to your lap — no
   need to circle back. Show it clearly ("MISSED G4 +3 s") on the HUD and
   in the results.
   - **Still no cheating:** a far miss (outside that margin), the wrong form
     (e.g. flying over the cave instead of diving through G6), or skipping
     a section keeps today's behaviour (THIS WAY / TURN BACK / reset).
     Shortcut sub-gates follow the same near-miss rule.
   - Penalty: start at 3 s; measure the biggest time a near miss could save
     anywhere on the lap and set the penalty clearly above it (say the
     number used). Same rule for the AI.
   *Check:* a near miss at each kind of gate (road, flight ring, water,
   cave) → counted with the penalty, no reset; a far miss and a
   wrong-form pass → rejected as before; a lap with a near miss is never
   faster than the clean lap.
7. **Regression + short AI soak.** All Round 1 / 2 / 2b / 3 checks again;
   50 AI laps on Normal: all finish within 2× target with valid gate
   sequences.
8. **Round wrap-up.** Per-item status for all 8 items (done / not done /
   deferred, and why), "For Dan to check", update `CLAUDE.md` (Settings:
   difficulty; wall behaviour), commit, push, publish release build 5
   (0.4.0), clean the disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (fill in at end of round)
- …


