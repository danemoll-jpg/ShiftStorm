# ShiftStorm — TODO archive

Older rounds, moved word-for-word from TODO.md. Grep this file; don't read it in full.

## Round 10 spec (delivered as build 12, 2026-11-15): no stranding walls, test tracks out, a roller-coaster track

**Dan's notes on build 11 (0.8.2), 2026-11-15, quoted:**
1. "When you first start this level, before the loop it is very easy to land
   on the other side of the wall and impossible to get back over without
   resetting. Can we remove that wall?"
2. "Let's also clean up all the test tracks. We know the concepts are
   working, no need to have those there cluttering up the space."
Run unattended; don't stop to ask; no visible window. Read-only references
as before.

1. **Remove the stranding wall before the loop.** On the Beach Promenade
   between the start and the loop (around the Creek Jump / Lake Jump
   landings and the bronto neck bridge), racers land on the far side of a
   wall / barrier and can't get back. Remove that wall. Where the road still
   needs an edge, use something you can't land behind: a low kerb / rumble
   strip, open sand that drives back onto the road, or a slope. Then **check
   the whole lap** for any other place where a jump, the loop exit, a
   landing or a wall bounce can leave you on the wrong side of a barrier
   with no way back — fix each (remove, lower, or make the ground there
   lead back to the road). Automatic recovery stays as the last resort,
   not the answer.
   *Check:* a sweep of scripted bad landings off every jump and the loop
   (left / right / long / short, car and fly lines): 0 strandings — every
   case either drives back onto the road or is clearly on it; list each
   barrier changed with before / after pictures.
2. **Remove the test tracks from the game.** The Round 1 strip and the four
   mode lanes (`-course 0..4`, D-pad left / right / F3 / F4, the menu's test
   courses) go: out of the menu, out of the build, the D-pad course switch
   unbound. Paradise Plunge is the only course. **Keep the automated
   checks:** move every check that used the strip or lanes (Round 1 lanes,
   E1–E6 edge cases, slow fps, etc.) onto the real course (or a hidden,
   check-only start point on it), so the regression suite still covers the
   same things. Delete the dead code and assets; update the Controls table
   and Commands in `CLAUDE.md`.
   *Check:* the menu lists no test courses; the full regression suite
   passes on Paradise Plunge alone; build size before / after.
3. **Small fold-in from Round 9's own notes:** on the Scrambler, Abi's hands
   are 10–20 cm short of the handlebars in some forms — put them on the
   grips in every form (IK target / seat position).
   *Check:* pictures of each Scrambler form, hands on the grips.
4. **A second track: "Coaster Coast" (working name) — a seaside amusement
   park with a roller coaster, all four forms** (Dan, 2026-11-15: "I have
   been wanting to do a roller coaster so let's go with this one."). Do it
   after items 1–3.
   - **Not Tee'd Off again (Dan):** "this is not Tee'd Off. Yes, it is the
     same universe ... and we certainly use assets we have already built for
     the game (and others) ... I want to have some different environments."
     So: **reuse models and systems** from Tee'd Off, Chastain Rush,
     Hostile Horizon, Gridiron Glory where they help (read-only copies), but
     the **place is new** — not Tee'd Off's Funland layout or look. Setting:
     a seaside **boardwalk amusement park on a pier**, sunset turning to night
     during the lap (neon, string lights, fireworks) — a clear contrast to the
     jungle of Paradise Plunge. Same Tee'd Off toon style and humour.
   - **Route (~2 min safe line, all four forms):**
     - **Car on the roller coaster:** the road *is* a giant coaster track —
       launch, big drops, banked turns, a loop and a corkscrew, airtime hills
       (car rides the rails like the Round 7 loop; never stranded — item 1's
       rule). One plain big jump between two coaster sections.
     - **Boat down the log flume:** a splash-down drop into a winding flume
       through the park, then out onto the sea beside the pier.
     - **Plane over the park:** launch off the coaster's top into flight over
       the midway — through the Ferris wheel, around the helter-skelter,
       through the fireworks (rings / gates), with a **choice jump** (stay on
       the coaster or fly) whose paths really split.
     - **Sub under the pier:** dive between the pier's legs into a giant
       aquarium / sunken carnival (old carousel, rides on the sea floor),
       surface at a water-ride splash pool and land back on the boardwalk
       for the finish.
     - At least one split path in the water / under the pier too.
   - **Life and set-pieces** (Round 8b's rule: big, close, in front, 8 % of
     screen height for 1 s): the Ferris wheel turning right beside you,
     fireworks bursting overhead, the crowd on the boardwalk and in coaster
     cars (Gridiron Glory crowd), a giant mascot balloon, dolphins / whales /
     a giant octopus in the aquarium, gulls. Models: reuse first; anything
     essential missing → **at most 2–3** Tripo prompts in
     `Docs/MODELS-NEEDED.md` for Dan (he's careful with credits), stand-ins
     meanwhile, don't wait.
   - **Everything Paradise Plunge has:** ordered gates, near-miss rule, route
     guides / arrow / route strip, AI on all difficulties (they ride the
     coaster and pick splits), split screen, camera views, recovery, the
     crowd, TeedOff Barker with **lines for this track** (ElevenLabs, his
     voice), driver reactions, steering mechanisms, playtest logs, time
     trial and best times per track.
   - **Music:** **no Tee'd Off music** (Dan). Dan's new song `race_coaster.mp3`
     (prompt in `Docs/AUDIO-NEEDED.md`, song 4); until it arrives, a ShiftStorm
     song stands in. Also: make sure no Tee'd Off song plays anywhere in the
     game any more (e.g. `CourseAudio.cs` falls back to `course_jurassic`):
     fall back to ShiftStorm's own songs instead.
   - **Menu:** pick the course (Paradise Plunge / Coaster Coast) for Race,
     Time Trial and 2 Players; Barker's results line names the course.
   - The name "Coaster Coast" is a working name; Dan may rename it.
   *Check:* check-pilot lap with segment times, every split timed, the
   coaster sections at full / slow / very slow speed (no stranding, no
   fall-through), AI 25 laps per split with 0 stuck, a 50-lap AI soak on
   the new track, set-piece visibility per segment, a split-screen lap,
   frame time (PC) + a Deck check list; player-view pictures of every
   segment — look at them before calling it done.
5. **Regression (both tracks) + 50-lap AI soak (Normal) on each track,
   wrap-up, release build 12 (0.9.0).** Per-item status, "For Dan to check", update `CLAUDE.md`,
   commit, push, publish, clean the disk.


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

## Current Objective — Round 5: speed, then the first art slice

Round 4's spec is in `TODO-ARCHIVE.md`. Dan (2026-11-13): fold Code's
Round 5 speed draft into the next phase (the art slice), as one round. Run
unattended — don't stop to ask; anything needing a visible window or Dan's
OK gets skipped and noted.

### Dan's review of build 5 (0.4.0), 2026-11-13
1. Walls: "the wall bounce seems good now". 2. Keeping up: "still difficult keeping up. Still feels like I am moving slow".
3. Start-line beams: gone. 4. Near miss +3 s: fair ("yes"). 5. Coral Vault tunnel: "not sure maybe". 6. Deck: 60 fps.

**First — vehicle image prompts for Dan (do this before anything else).**
Dan (2026-11-13): "move the image prompts to the front. I can work on them
and it can continue work and then I will let it know when I am done and it
can incorporate them."

**DECIDED (Dan, 2026-11-13): the Jetstream design** (all four forms). Tee Shot and
   Mudlark are parked for later (other designs can be added, e.g. rivals' own machines); the Garage shows Jetstream (and its rival colours) only.
0. **Write the vehicle prompts, tell Dan, keep going.** Write the 12 image
   prompts (3 design directions × 4 forms) and everything else item 5
   lists for them in `Docs/MODELS-NEEDED.md`, commit and push that file,
   then **post the prompts to Dan in the chat right away** (copy-pasteable,
   with the file names and folders to save to and the Tripo settings).
   Then continue with items 1–7 without waiting. When Dan says he's done
   (or his files appear in the named folders), import whatever he made at
   the next sensible point — finish the current item first — and show it in
   the Garage; anything not made yet stays a placeholder.

**Part A — speed (Code's Round 5 draft, approved).** Dan still "feels like
I am moving slow" and can't keep up, so this comes first.

1. **Feel of speed.** The engine sound's pitch tops out at 45 m/s
   (`VehicleSound`), so above that (Drive 65, Fly 80) speed has no sound cue:
   fix it per form's top speed and add a wind layer. Camera: stronger
   field-of-view rise, lower and closer at speed, a bigger boost kick.
   Speed streaks / spray / bubbles per form. Regularly spaced roadside
   things (posts, palms, buoys, stripes) so movement reads against the
   scenery.
   *Check:* before / after pictures at the same spots, frame time on the lap.
2. **Real speed.** Faster acceleration out of corners and form changes
   (Drive 22 m/s² now) and +10–15 % top speeds in every form, AI on the same
   tuning; lap target ~120–125 s (now ~139 s). Re-tune changes, ramps and
   landings for the new speeds.
   *Check:* lap, walls, near-miss, start, waterfall return and soak still pass.
3. **Easy really easy.** Easy AI ~20 % slower than the check pilot (now 9 %),
   Normal ~8 %, Hard as now; still no rubber-banding.
   *Check:* 3 races per level; the check pilot wins on Easy without boost.
4. **Coral Vault tunnel:** leave as it is (Dan: "not sure maybe"). The
   climb-out reshape still needs a window and his OK — later.

**Part B — the first art slice** (concept: "one polished sequence
establishes the visual quality target", match Tee'd Off's look). Gray box
stays everywhere else. Vehicles: Tripo by Dan, not Blender scripts.

5. **Vehicle design, stage 1: better-looking vehicles (Dan, 2026-11-13).**
   Dan: "I want to make sure the vehicles look good. Maybe we can 3d model a
   couple of designs ... I don't think the vehicles I had in Woodstock Rush
   and Chastain Rush were high quality enough." So the real vehicles are
   **not** built by Blender script. They come from Tripo (made by Dan by
   hand) through the art pipeline in `CLAUDE.md`, in stages:
   - **Stage 1 (this round; prompts already written in item 0):** image
     prompts for **3 design directions** for Abi's transforming machine.
     Each direction gets one prompt per form (car, boat, plane, submarine),
     all four clearly the **same machine**: same cockpit/canopy shape, same
     colors (from Abi's Tee'd Off model), same signature details, the Tee'd
     Off toon style. Per image: 3/4 front view, plain light background, whole
     vehicle in frame, no driver, no text. Add a design note per direction,
     the exact file names and folders to save to, and the Tripo settings
     (textured; low-poly / game option; FBX). Ask Dan to make **only the
     car form of each direction in Tripo first** (3 models), so he can
     compare them in 3D before spending time on all four forms.
   - **Transformation with whole models:** Tripo gives one solid mesh per
     form, so the change becomes a **0.6 s swap with an effect** instead of
     folding parts: squash-and-stretch, a spin of panels / a burst of
     sparkles and spray (by form), with the model swapped at the effect's
     midpoint. Build this system now with the current placeholder forms as
     four separate models, so Tripo models drop straight in later. Keep one
     hull collider, Abi seated in every form (a seat point per model).
     Vehicle models come **without glass** (Dan: Tripo gets glass wrong):
     add a separate clear canopy per form in the game, shaped to the
     cockpit rim, closed for the sub, optional elsewhere; and
     the shared timing point for the physics handover.
   - **Textures (Dan, 2026-11-13: the game is played on his 4K PC too, not
     just the Deck):** Dan exports Tripo models with **4K** textures. Keep
     the 4K source; add a **Texture quality** setting (High / Medium) in
     Menu > Settings: High = full 4K on PC, Medium = half resolution
     (Unity's global mipmap limit). Default: Medium on the Steam Deck
     (detect it), High elsewhere. Report memory used by vehicle textures
     with 4 cars on each setting.
   - **Viewer for Dan:** a "Garage" screen in the menu that shows each form
     (and any imported design) turning slowly, with a button to cycle forms
     and designs — so Dan can judge them on the Deck.
   - Drop-in import: when a design's FBX is in the folder named in the doc,
     Code's import step (scale, origin, facing, seat point, LODs, rival
     color variants) is one command. Write it now, test it on the
     placeholders.
   *Check:* MODELS-NEEDED.md has 12 prompts (3 directions × 4 forms) with
   names and folders; the swap change passes every transition check at
   0.6 s; Garage pictures of all four placeholder forms; the import command
   works on a placeholder FBX; any models Dan delivered during the round are
   imported and shown in the Garage (report which).
6. **Polished stretch: Cliffside Climb → Sky Arch → Lagoon Landing.**
   Finished Jurassic Par look for these three segments only: ported
   Tee'd Off scenery (cycads, tree ferns, mossy cliffs and boulders, the
   arch, waterfalls off the cliffs, dinosaurs in the valley, pterodactyls),
   toon water with foam at the lagoon landing, splash and spray effects,
   lighting / sky / fog tuned to Tee'd Off's `ArtStyle_Jurassic`. Copy from
   Tee-dOff, never edit it. No new Tripo models unless something essential
   is missing — then list it in `Docs/MODELS-NEEDED.md` with the image
   prompts (per `CLAUDE.md`) and **leave it for Dan**, don't wait.
   Readability first: gates, rings, the slit, the landing corridor and the
   next change stay clear (concept: "spectacle that remains readable").
   *Check:* player-view pictures every few seconds through the stretch,
   side by side with Tee'd Off's Jurassic Par pictures; Code looks at them
   and says what still doesn't match. Deck budget: frame time on the
   stretch with four cars (PC numbers; Dan checks the Deck).
7. **Regression + 50-lap AI soak (Normal), wrap-up, release build 6
   (0.5.0).** Per-item status for all 8 items (0–7) (done / not done / deferred,
   and why), "For Dan to check", update `CLAUDE.md`, commit, push, publish,
   clean the disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check
- See "For Dan to check (Round 5)" under Completed Tasks.

## Current Objective — Round 6: split screen and camera views

**Dan's review of build 6 (0.5.0), 2026-11-13:** "Looks good. Plays fine."
(Jetstream, speed, the polished stretch: no changes asked.)

**Dan's asks, quoted:** "Is it possible to throw in split screen so I can try
with a friend? Woodstock Rush and Chastain Rush has this. Also they have it
so you change your pov, can you do that here as well."

Reference (read-only, copy and adapt, never edit): `chastain_rush`'s split
screen (`SplitScreen` and the per-device binding in `VehicleInput.cs`) and
`CameraViews.cs` (Chase / Far chase / First person / Front, cycled with X).
Make it work the way Dan already knows from those games. Run unattended —
don't stop to ask; anything needing a visible window or Dan's OK gets skipped
and noted.

1. **Two-player split screen.**
   - Menu: "2 Players" (Race and Time Trial). Player 2 joins by pressing A
     on a second controller (keyboard counts as a device on PC); each player
     is bound to their own device, as in Chastain Rush. On the Deck: the
     Deck's own controls + one external controller.
   - Layout like Chastain Rush (say which: top/bottom or side by side);
     each half has its own camera, HUD, arrow, route strip, place,
     speedometer, cues ("THIS WAY", "MISSED G4 +3 s") and Abi-style driver
     reactions.
   - Player 1 = Abi in Jetstream green; Player 2 picks a driver (Rae, Chip
     or Vivi) and gets that colour; the other two AI racers fill the field
     (option: 0–2 AI). Results show both players clearly.
   - Pause / menu from either controller pauses both. Settings like invert
     are **per player**.
   - Sound: one shared mix, both engines audible but not doubled-loud.
   - Performance: two views is the heaviest case. Add split-screen quality
     cuts (draw distance, shadows, detail layer, texture quality) like
     Chastain Rush's Deck scaling; PC must hold 60 fps; report PC frame times
     and give Dan a short Deck check list.
   *Check:* two scripted players race a full lap with 2 AI: both finish with
   valid gate sequences, each HUD shows its own player's data, pictures of
   both halves in each form.
2. **Camera views (POV), per player.** Cycle with **X** (keyboard **V**), as
   in Woodstock Rush / Chastain Rush: Chase (current), Far chase, First
   person / cockpit (from Abi's eyes, the canopy edge and Jetstream nose in
   view), Front / bumper. Every view works in all four forms and through the
   change; the choice is remembered (per player). Right stick looks around,
   as in Chastain Rush.
   - **Button change:** X is currently a second button for drift / carve
     (RB is the main one). Free X for the view, keep drift / carve on RB
     (and Left Shift). Update the Controls table in `CLAUDE.md`.
   *Check:* pictures of each view in each form; no view clips through
   walls, water surface or the cave ceiling badly; the change effect still
   reads in first person.
3. **Regression + 50-lap AI soak (Normal), wrap-up, release build 7
   (0.6.0).** Per-item status for all 3 items, "For Dan to check", update
   `CLAUDE.md`, commit, push, publish, clean the disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check
- See "For Dan to check (Round 6)" above.

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
   - **Game theme song (added by the planning chat, Dan 2026-11-14):** song 0
     in `AUDIO-NEEDED.md`, `theme.mp3`, instrumental (Dan: no lyrics on a
     looping menu song). When it arrives: title screen (loop from the first chorus) and credits; the
     Tee'd Off `title` placeholder until then.
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
- **Listen:** your four songs (Theme on the menu's main page, the Garage song in Garage / Settings, the race song, Victory on the results);
  the race song muffles under water and lifts out of the waterfall; TeedOff Barker's lines (start, GO, jumps, the wings call, lead
  changes, final stretch, the winner): is any line heard too often? The engines per form (synthesised car, boat outboard, plane, sub).
- **Loop:** does it feel right at full speed, and in First person (at the top the view drifts above Abi's head a little)?
- **Lake Jump:** press LB right off the lip: is 1 s of window enough, and is the fly line fun / about as fast as the car line?
- **Jumps:** about 2 s of airtime; do you land straight? A short jump drops you in the creek (instant reset): fair?
- **Deck frame rate:** the lap now peaks at 5.2 M triangles (+17 %); 27 frames over 50 ms on the PC check (at segment / change moments,
  likely first-use loading): watch for hitches at the start, the loop and the Lake Jump.
- **Settings:** the three volume rows.


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

## Current Objective — Round 8: Jurassic Par for real, louder Barker, driver reactions, steering

**Dan's review of build 8 (0.7.0), 2026-11-14, quoted:**
1. "I don't understand the level created is supposed to be based on Jurassic
   Par or no? I mean I think it is fine if it isn't but if it is there should
   be more dinos."
2. "Can't hear most of Barnaby's lines and he didn't talk as much as I
   thought he should."
3. "Have the driver scream just 'Noooo' or 'oh no!' when they mess up. They
   could say something else based on their specific character. But Barnaby
   should be doing the narration overall."
4. "Should be able to see the steering wheel ... Depending on the vehicle the
   steering mechanism can be different."
5. "Why is the driver clapping when they come in last place?"
6. "Need to be able to control the sounds but engine sounds should be
   quieter, music and Barnaby louder."
7. "Sometimes the jump where you choose jump or fly should be different paths."
Run unattended — don't stop to ask; anything needing a visible window or
Dan's OK gets skipped and noted (exception: item 9 has Dan's OK for a
visible window). Tee-dOff, chastain_rush, Hostile-Horizon read-only.

1. **Make it clearly Jurassic Par — lots more dinosaurs.** Yes, it is meant
   to feel like Jurassic Par. Dinosaurs are the signature, so they should be
   everywhere a player looks, alive and moving, never on the racing line:
   - Herds of brontosaurs wading in the lake and grazing by the road;
     triceratops in the mangrove; a T. rex roaring on a cliff above the
     Cliffside Climb (camera shake, roar); raptors running alongside the road
     for a stretch; pterodactyls swooping past the flight and the loop; a
     plesiosaur / long-neck in the lagoon; something big glimpsed through the
     flooded cave's windows; nests with eggs, footprints, dino skeleton arches
     and fossils in the cave walls.
   - **Animate them:** Tee'd Off's dinos have rigs (`Art/Characters/Dinos/`
     — bronto, trike, trex, ptero; the round-7 port copied static meshes
     only). Copy the rigs and any animations; where none exist, simple
     procedural motion (walk cycles by bone, head bobs, tail sway, wing
     flaps). Sounds: Tee'd Off's dino calls, positional.
   - Also replace the worst "primitive lumps" (Round 7's own note: cliffs,
     statues, the crowd as spheres) with Tee'd Off meshes where they exist.
     If an essential model doesn't exist anywhere (e.g. raptor, plesiosaur),
     write Tripo prompts in `Docs/MODELS-NEEDED.md` for Dan and use a
     stand-in meanwhile — don't wait.
   - Budget: dinos are LOD'd / culled; frame time on the lap reported.
   *Check:* player-view pictures along the lap: count of dinos visible per
   segment (aim: at least one group in every segment); none in the route.
2. **TeedOff Barker: louder and talks more.**
   - Mix: Barker always clearly audible over engines and music — duck the
     music and engines under his lines (about -6 to -9 dB), and raise his
     level. Subtitles stay.
   - More talk: roughly one line every 6–10 s of racing (never over another
     line), more pools and many more lines (ElevenLabs, his voice, as in
     Round 7): every form change, overtakes (by rival name), position
     changes, near misses and penalties, wall hits, the loop, each jump,
     landmarks and dinos ("Look out for the T. rex!"), close racing,
     falling behind / catching up, the last stretch, finish. Varied, funny,
     Tee'd Off-style. At least 120 lines total.
   *Check:* a race log: lines per minute, longest silence, 0 overlaps, 0
   repeats inside a race; his measured level vs engines and music.
3. **Driver reactions when they mess up.** On a hard wall hit, a reset, a
   missed gate / penalty, a failed loop or a short jump, the driver who messed
   up yells a short reaction — "Noooo!", "Oh no!", or something in their own
   character's voice and style (Abi, Rae, Chip, Vivi; their Tee'd Off voice
   clips where they fit, otherwise ElevenLabs in their Tee'd Off voices).
   Short, with cooldowns, never over Barker (Barker has priority; he is the
   narrator). Rivals' reactions only when they're near the player. Split
   screen: each player's own driver.
   *Check:* each driver has at least 6 mess-up lines; triggered in a race,
   none overlaps Barker.
4. **Visible steering mechanism, per form.** Car: a steering wheel that turns
   with your steering; boat: a ship's wheel / helm; plane: a control stick
   (or yoke) that tilts with bank and pitch; sub: twin levers or a joystick.
   It swaps with the form change. Abi's hands on it (hand IK), visible in
   first person and in the chase views. Built so a future vehicle can bring
   its own mechanism (e.g. a motorcycle's handlebars).
   *Check:* pictures of each form in first person and chase, wheel at full
   left / centre / full right; hands follow.
5. **Results reactions by place.** 1st: celebrate (clap / cheer); 2nd–3rd:
   thumbs up / OK; last: disappointed (Sitting Disapproval or a head shake),
   never clapping. Barker's results line matches the player's place.
   *Check:* results for 1st, 2nd and 4th place: the right clip and line.
6. **Sound levels.** Settings volumes: **Music, Voices (Barker + drivers),
   Engines, Effects** (Engines split out of Effects). New defaults: engines
   noticeably quieter, music and voices louder (state the numbers); ducking
   as in item 2. Existing saved settings migrate sensibly.
   *Check:* measured levels per bus in a race with defaults (runs stay
   muted; log the would-be levels).
7. **Choice jumps can lead to different paths.** Rework the Lake Jump so the
   two choices really go different ways: the car line takes its own road
   (e.g. through a jungle canyon or along the beach past the bronto herd), the
   fly line goes out over the lake through the rings — different scenery,
   different hazards, rejoining before the next main gate with similar time.
   If there's room, a second choice jump later in the lap (e.g. on the
   Waterfall Return or the mangrove bank) whose paths also split. Same gate,
   near-miss and AI rules as Round 7; the route strip and HUD arrow follow
   whichever line you took.
   *Check:* both lines on each choice jump timed, 0 resets; AI 25 laps
   through them with 0 stuck; pictures of each line.
8. **Fix the new hitches.** Build 8's lap had 27 frames over 50 ms (worst
   216 ms; build 7: 6–10). Find what causes them (likely new art / audio
   loading, ElevenLabs clips loading on first play, dino or crowd
   instancing) with a profile, fix, and report before / after.
9. **Coral Vault tunnel climb-out reshape — Dan's OK for a visible window
   (Dan, 2026-11-14: "yeah add that").** The tunnel saves ~1.15 s (target
   2–3 s) because every run glances the climb-out walls. **For this item
   only**, you may open a visible game / editor window to look at it — still
   muted, no other item gets this OK. Keep the window small and the session
   short, and say in the report when it was open and for how long. Reshape
   the climb-out so a clean run doesn't touch the walls, keep it demanding.
   *Check:* time saved for a clean run (same entry speed, no boost; target
   2–3 s), 25 AI runs through it with 0 stuck, a deliberate miss / scrape
   loses time but never traps; pictures.
10. **Playtest logging (Dan, 2026-11-14).** For the concept's playtest with
   friends: every race / time trial writes a small log per player (in the
   game's data folder, one file per session): driver, mode, difficulty, lap
   and segment times, resets (where and why), missed gates and penalties,
   shortcut and choice-jump picks, form-change incidents, wall hits, place.
   Menu > Settings: "Save playtest logs" On / Off (default On) and an "Open
   log folder" option on PC. A small script (`Tools/playtest_summary.py`)
   turns a folder of logs into one readable summary (per player and per
   segment).
   *Check:* two scripted races (one split screen) produce logs; the summary
   script reads them.
11. **Regression + 50-lap AI soak (Normal), wrap-up, release build 9
   (0.8.0).** Per-item status for all 11 items, "For Dan to check", update
   `CLAUDE.md` (Settings volumes; reactions; steering; playtest logs), commit, push,
   publish, clean the disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (Round 8)
- **Barker:** can you hear him now over the engines and music, and is a line every ~6 s too much? (Never heard here: all runs muted.)
  Voices / Engines / Music in Menu > Settings if the mix needs moving.
- **Dinosaurs:** does it read as Jurassic Par now? The T. rex on the climb's cliff (and its roar), the raptors beside the promenade, the
  lake herd. Raptor and plesiosaur are stand-ins: make the Tripo models in `Docs/MODELS-NEEDED.md` only if they look wrong.
- **Deck FPS (R3)** on the promenade, the Sky Arch and the Waterfall Return with four cars and the dinosaurs; and whether the **first race
  after installing** stutters (the shader cache fills on the first run) and the second is smooth.
- **Steering in first person (X):** the wheel / helm / yoke / levers big enough (or too big), the hands on them; the slightly lower view.
- **Driver yells** after a crash or reset: "Noooo!" / "Oh no!" — funny or annoying?
- **The Lake Jump's two roads:** the canyon vs the flight over the lake — different enough?
- **The Coral Vault tunnel:** worth taking now (~2 s); does it still feel like it rubs the walls?
- **Engines** at 60 %: quiet enough? An existing settings file was moved to Engines = 60 % of its old Effects.
- **Playtest logs:** Menu > Settings > Open log folder (on the Deck it may not open a file browser: the folder is shown in the menu row).

## Current Objective — Round 8b: dinosaurs you can actually see

**Dan's review of build 9 (0.8.0), 2026-11-14, quoted:** "Not really seeing
dinosaurs, hear them being mentioned but not very visible."

**Why (planning chat, from Round 8's own pictures `dinos-02/04/13`):** the
46 dinos are there but tiny and far away — brontos as small figures out in
the lake, pterodactyls as specks, the T. rex on the climb's cliff about the
height of a palm tree, the trike a small shape on the far mangrove bank. At
140 mph they're gone in a blink. Round 8's check counted a dino as "seen"
if it was in the frame at all, even a few pixels tall — that check passed
when it shouldn't have.

Run unattended; don't stop to ask; no visible window. Tee-dOff read-only.

1. **Big, close, in front of you.** Cartoon-huge and placed where the camera
   looks, not off to the side or on the horizon:
   - **Scale up:** brontos tower over everything (head well above the palms
     — roughly 25–30 m tall), T. rex clearly bigger than the vehicle and
     the palms (about 12–15 m), trikes about 2–3× the car's height,
     pterodactyls with wingspans wider than the road.
   - **Closer:** most dinos 15–60 m from the road edge (outside the
     barriers / banks, never on the route), on the **outside of bends and
     at the end of straights**, so they sit in the forward view for 2–3 s
     at racing speed.
   - **Set-piece moments** (at least one per segment), e.g.: a bronto's
     neck arching **over** the road like a bridge on the promenade; the T.
     rex leaning out from the cliff right above the climb road and roaring
     as you pass; raptors running **alongside** you inside the barrier line
     for a few seconds; a pterodactyl swooping across in front of you
     during the flight; brontos wading right next to the boat line in the
     lagoon; a trike charging along the mangrove bank; a long-neck's head
     rising out of the water beside the sub at the cave entrance (the cave
     has no windows — do it at the Sunken Gateway pool instead); brontos
     watching from beside the waterfall landing.
   - **Barker's dino lines play only when a dino is actually big on screen**
     at that moment.
   - Keep them off the route and out of the gates' view (readability first).
2. **A real visibility check.** Count a dino as "seen" only if it covers at
   least **8 % of the screen height for at least 1 s** from the player's
   chase view at racing speed. Report per segment how many seen and for how
   long (aim: every segment, at least one moment of 2 s+). Player-view
   pictures of every set-piece moment — and **look at them** before calling
   it done.
3. **Performance.** Bigger and closer costs more: keep the lap's 95th
   percentile frame time near Round 8's; LODs and culling as before. Report
   before / after.
4. **Wrap-up, release build 10 (0.8.1).** Per-item status, "For Dan to
   check", update `CLAUDE.md`, commit, push, publish, clean the disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (fill in at end of round)
- Drive the promenade: the two brontos' necks over the road after the Creek Jump; raptors running beside you off the start.
- The T. rex dead ahead at the top of the climb (corner2): big enough, and does its roar / camera shake land right?
- The flight: the pterodactyl flying with you on the right, the second one swooping across in front after the arch. Too distracting?
- Lagoon / Mangrove: brontos right beside the boat line, the long-neck swimming with you, the charging triceratops on the bank.
- Sunken Gateway: the long-neck rising out of the pool beside you before the dive.
- Coral Vault: the long skeletons on the cave walls — readable as skeletons, or just "ribs"?
- Barker's dino lines: do they now come when you can see the dinosaur?
- Frame rate on the Deck (the dinos cost ~1.4 ms on the PC; the Mangrove and the finish are the heaviest).

## Current Objective — Round 9 (delivered 2026-11-15, build-11 / 0.8.2; status under Completed Tasks; waiting for Dan's review)

**Dan's review of build 10 (0.8.1), 2026-11-14, quoted:**
1. "The steering wheel is far too big. It takes up too much of the screen
   when you choose first person pov."
2. "She says 'that's not where I aimed' which doesn't make sense in the
   context of driving." (`Resources/Audio/Voice/abigail/lines.json`: a
   Tee'd Off golf line.)
3. "The finish plays both the finish from Tee'd Off and the new one we
   created for this game."
(The dinosaurs from Round 8b: no complaint this time.)
Run unattended; don't stop to ask; no visible window. Tee-dOff,
chastain_rush, Hostile-Horizon read-only.

1. **Steering mechanisms at a sensible size.** Real proportions to Abi and
   the cockpit: in first person the car's wheel sits low in the view and
   takes no more than about **15–20 % of the screen height**, the road ahead
   clear. Same check for the boat helm, the plane stick and the sub levers.
   Hands still on it (IK). Also check the chase views (not oversized there).
   *Check:* first-person pictures of all four forms, wheel centred and at
   full lock; report the % of screen each mechanism covers.
2. **Every line must fit racing.** Go through **all** driver lines (Abi,
   Rae, Chip, Vivi) and Barker's reused Tee'd Off clips: anything that only
   makes sense in golf (aiming, shots, putts, clubs, holes, the green,
   "fore" used as a golf call, caddies, par…) is removed from the racing
   pools. Replace them with new racing lines in the same voices (ElevenLabs,
   as in Round 8), in each character's style. Keep the short mess-up yells
   ("Noooo!", "Oh no!"). List every line removed and every line added.
   *Check:* a written list of all lines now in use, per character, and
   each one makes sense in a race.
3. **One finish sound.** The finish plays both Tee'd Off's finish jingle and
   our own (Dan's "Chequered Flag" / the new finish). Keep only ShiftStorm's
   own; remove Tee'd Off's jingle from the finish. Then audit every race
   event (GO, gates, form changes, loop, jumps, penalties, finish, results)
   for any other doubled sound (two sounds for one event), and fix them.
   *Check:* an event-by-event list of which sound(s) play; none plays two
   music/jingle cues at once.
3b. **The crowd from Gridiron Glory** (Dan, 2026-11-14: "I didn't even notice a
   crowd. But can we take the crowd work from Gridiron Glory? There was
   already extensive work to make the crowd look more real."). Find Dan's
   Gridiron Glory project (`C:\Users\danmo\footballGame`, confirmed by Dan; read-only —
   copy, never edit) and port its crowd system (models,
   animation, instancing / LOD, colours, cheering) into the grandstands at the
   start / finish and anywhere else a crowd fits (the loop, the waterfall
   landing). Dress them for Jurassic Par where cheap. The crowd should be
   **noticeable** as you pass (same idea as the dinos: big enough, close
   enough, reacting — cheering, waving). Keep frame cost in check (report).
   *Check:* player-view pictures at the grandstands; crowd counted and
   animated; frame time before / after.
3c. **Three new vehicle designs, ready for Dan's models.** Dan drew one form
   each: **Hot Rod** (lavender; his image is the sub), **Scrambler** (a
   motorcycle; his image is the land form) and **Grand Prix** (1930s
   maroon / brass racer; his image is the car). Prompts for the other nine
   forms are in `Docs/MODELS-NEEDED.md`; import whatever arrives
   (`Tools/import_vehicles.py`), show them in the Garage, and give them to the
   rivals (suggested: Hot Rod → Vivi, Grand Prix → Rae, Scrambler → Chip; Dan
   to confirm). For the **Scrambler**: a motorcycle needs its own feel and
   look on land — the rider sits astride (pose / IK on the handlebars),
   the bike leans into turns (visual lean; same handling stats), the
   kickstand is hidden while riding; its **sub form is enclosed in a clear
   glass sphere** (Dan) mounted on the model's chrome ring, made in the
   game like the other canopies. Steering mechanism per design (Round 8):
   handlebars for the Scrambler.
   *Check:* Garage pictures of every form that has arrived; the Scrambler's
   lean and rider pose; the glass sphere on its sub.
4. **Small fold-in: the Round 1 test strip's E6 check** has failed since
   Round 5's speed-up (the strip's wheels-down zone fires 1 m past the ramp).
   Fix the strip so its check passes again (the real course already passes).
5. **Wrap-up, release build 11 (0.8.2).** Regression + 50-lap AI soak
   (Normal); per-item status, "For Dan to check", update `CLAUDE.md`,
   commit, push, publish, clean the disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (fill in at end of round)
- See "For Dan to check (Round 9)" under Completed Tasks.

### Draft, not chosen (2026-11-15): Lantern Reef as the second track
4. **A second track: Lantern Reef, in Tee'd Off's Lost City Links** (Dan,
   2026-11-15: "I want to go ahead and put in a new track this round. I am
   going to bed so I don't really care to have a short round."). Dan didn't
   pick which of the concept's three; the planning chat chose **Lantern
   Reef** because Tee'd Off's Lost City Links (`AtlantisBuilder.cs`,
   `ArtStyle_Atlantis`: sea floor, branching and fan coral, kelp forests,
   white marble temples and columns, city gates, crystals, a galleon wreck,
   the glass bubble, Ollie the Octopus host, `course_atlantis` music) is the
   richest ready-made setting for it. Concept: *"Lantern Reef — boating and
   diving: surface navigation gives way to luminous tunnels with vertical
   splits."* Do it after items 1–3.
   - **Route:** about 2 min on the safe line. Mostly **boat and sub** (the
     concept's idea): a lantern-lit harbour start on the water at dusk, a
     surface run between reef islands and marble ruins, dives into glowing
     tunnels and the sunken city, **vertical splits** (an upper and a lower
     tunnel — real route choices like the Lake Jump), surfacing through a
     temple courtyard, a final surface sprint to the finish. Keep a **short
     car section** (the harbour quay / a causeway with one plain jump) only
     if it makes the start and finish work better — say what you chose.
   - **Everything Paradise Plunge has, on this track too:** ordered gates and
     near-miss rule, route guides / arrow / route strip, AI rivals (all
     difficulties, they handle both splits), split screen, camera views,
     recovery anchors, no stranding spots (item 1's rule), the crowd (on
     the harbour quay / boats), TeedOff Barker with **lines for this track**
     (ElevenLabs, his voice), driver reactions, steering mechanisms,
     playtest logs, time trial and best times per track.
   - **Life instead of dinos:** the reef's equivalent of the dino
     set-pieces, big and close like Round 8b (8 % of screen height for 1 s
     rule): whales or a giant turtle gliding past, manta rays, schools of
     glowing fish, jellyfish lanterns, Ollie the Octopus waving from a
     temple, a sea serpent arching over a tunnel mouth. Tee'd Off models
     first (read-only), stand-ins otherwise; anything essential that's
     missing → Tripo prompts in `Docs/MODELS-NEEDED.md` for Dan (keep it to
     2–3; Dan is careful with credits), don't wait.
   - **Look:** Lost City Links' toon look ported to URP: lantern glow at
     dusk on the surface, luminous coral and crystals in the tunnels, light
     shafts, bubbles; readable first (the next gate and the split choices
     always clear).
   - **Music:** `course_atlantis` as the placeholder; add a **Suno prompt for
     a Lantern Reef race song** to `Docs/AUDIO-NEEDED.md` for Dan, in the
     same style as Paradise Plunge's (instrumental, loops, mouth instruments
     only if they fit — not whistling, not the didgeridoo again).
   - **Menu:** pick the course (Paradise Plunge / Lantern Reef) for Race,
     Time Trial and 2 Players; Barker's results line names the course.
   *Check:* the check pilot's lap (segment times), both splits timed, AI
   25 laps per split with 0 stuck, 50-lap AI soak on the new track,
   set-piece visibility per segment, split-screen lap, frame time (PC) and
   a Deck check list; player-view pictures of every segment — look at them
   before calling it done.

## Current Objective — Round 11: Buck arrives, and the start of story mode

Queued by the planning chat on 2026-11-15 (was `Docs/NEXT-ROUND.md`); started after Round 10 was published. Dan is out of the
house: run unattended, don't stop to ask, no visible window; same testing rules as always.

**Read `Docs/STORY.md` first** — Dan's story direction for the whole game (Dan, 2026-11-15). Key points:
- **Buck "Big Finish" Barlow** (working name) is the tour's organiser: a warm, unhurried, endlessly confident former stunt
  performer whose "perfectly safe" plans terrify everyone. **TeedOff Barker stays the race caller** (during races); Buck
  talks **before and after** races and in story scenes. They never talk over each other.
- His ElevenLabs voice is **"ShiftStorm Buck"** (Dan made it). Use it for every Buck line.
- **Paradise Plunge = the private trial** ("a gentle introductory outing"; he underestimated the wildlife).
  **Coaster Coast = the tour's public opening night** (his comeback; he once did stunt shows at this park).
- The track list in STORY.md is an idea pool, not a plan.

1. **Buck's lines (ElevenLabs, voice "ShiftStorm Buck").** Write and generate in his style — calm about outrageous
   things, exaggerated anecdotes, funny emphasis (see STORY.md's examples; reuse those lines too). At least:
   - a **pre-race briefing** per track, 3–4 variations each (Paradise Plunge: the private trial; Coaster Coast: opening
     night), 10–20 s each;
   - **results remarks** by the player's place (1st / podium / back of the pack), per track, several each;
   - **anecdotes and safety-briefing jokes** to sprinkle in (not every race);
   - the **story-mode scene lines** for item 3.
   Family-friendly, Tee'd Off humour. Keep a transcript (`Docs/Checks/round11/buck_lines.md`, transcribed back with the
   local speech-to-text like Round 9) and check each line makes sense in a race.
   *Check:* every line generated, transcribed, and fits.
2. **Where Buck appears (before the model exists).** A short **pre-race briefing card**: a portrait placeholder (a
   silhouette with his mustache and scarf, or a simple 2D card), his name, subtitles, his voice; **skippable with A /
   Enter**, and shown once per session per track unless story mode needs it. A **results remark** after the race (after
   Barker's results line, never overlapping). Settings: "Buck's briefings: On / Short / Off". Split screen: one Buck for
   both.
   *Check:* a race on each track logs the briefing → Barker during the race → Buck at results, 0 overlaps; skipping works.
3. **Story mode skeleton: "The Tour".** Menu > The Tour (alongside Race, Time Trial, 2 Players):
   - **Stop 1 — The Trial (Paradise Plunge)** and **Stop 2 — Opening Night (Coaster Coast)**, in order; more stops get
     added later as tracks are built.
   - Before each stop a short **scene** (Buck's voice + subtitles over a still or a slow camera fly-through of the track;
     30–60 s, skippable); after it a short reaction based on the result.
   - **Progress by qualifying / championship points, not winning every race** (STORY.md): points for each place; reach a
     set total (or a top-3 finish) to unlock the next stop; you can retry.
   - Progress saved (`tour.json`); a "Tour" screen shows the stops, your best place and points.
   - Difficulty from Settings applies. Accessible to people who never played Tee'd Off.
   *Check:* a scripted run plays both stops start to finish (scene → race → reaction → unlock), saves and reloads
   progress, and a 4th-place finish lets you retry without losing progress.
4. **Choose your racer.** Before Race, Time Trial, 2 Players and each Tour stop: pick a **driver** (Abi, Rae, Chip, Vivi)
   and a **machine** (Jetstream, Grand Prix, Hot Rod, Scrambler), with the Garage's turntable view; each player in split
   screen picks their own; the remaining drivers become the AI rivals with their own machines. Default: Abi + Jetstream.
   Remember the last pick.
   *Check:* every driver × machine combination starts a race (driver seated / astride correctly, steering mechanism,
   colours); split-screen picks; AI fill.
5. **Buck's look — prompts for Dan, don't wait.** STORY.md: barrel-chested older showman, impressive mustache, faded
   stunt jacket, bright scarf, big smile; human, broad bear or stocky dog. In `Docs/MODELS-NEEDED.md` write **3 image
   prompts** (one human, one bear, one dog), full body, A-pose, front view, plain light background, Tee'd Off toon style
   (look at Tee'd Off's character concepts, read-only), for Dan to pick one; then he makes it in Tripo and rigs it in
   Mixamo. List the Mixamo clips Buck will need (talking / gesturing, showman flourish, thumbs up, nervous laugh, clap).
   Until then the placeholder card stays.
   *Check:* the 3 prompts and the clip list are in the doc.
6. **Regression (both tracks) + 50-lap AI soak (Normal) on each, wrap-up, release build 13 (0.10.0).** Per-item status,
   "For Dan to check", update `CLAUDE.md` (Buck, The Tour, choose your racer, Settings), commit, push, publish, clean the
   disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check
See "For Dan to check (Round 11)" under Completed Tasks.

## Current Objective — Round 12: Coaster Coast unlocked, flight-branch fixes (delivered as build 14; waiting for Dan's review)

**Dan's notes on build 13 (0.10.0), 2026-11-15, quoted:** "I wanted to try the new track but it was locked behind the tour.
Let's unlock that as I am building as I don't want to keep racing through the first track. Also there seems to be a couple of
bugs with the first track. The first time you get an option to fly there is not enough space to direct your plane into the
end of that sequence so I always miss it. For some reason the flying shortcut kept screwing up and resetting me back too far
and kept making me repeat the same sequence over and over the one time I am trying to get through it to try out the new
course. Fix all these things."
Run unattended; don't stop to ask; no visible window. Same testing rules. Read-only references as before.

1. **Every track open outside the Tour.** Race, Time Trial and 2 Players always offer **every** track — no locks. Only the
   Tour's own story progress is gated (a stop unlocks the next stop *in the Tour*). Add a Settings / debug option "Unlock all
   Tour stops" (default **On while we're building**; say how to turn it off for release).
   *Check:* a fresh `settings.json` / `tour.json`: Coaster Coast selectable in Race, Time Trial and 2 Players straight away.
2. **First fly choice (the Lake Jump's fly line): room to make the exit.** Dan always misses the end of the flight sequence:
   there isn't enough space to steer the plane into it. **First, read Dan's own playtest logs** (the game's data folder, his
   recent sessions) to see exactly where he missed / reset, and say what they show. Then give the fly line room: longer
   flight, gentler turns, rings and the exit sub-gate **bigger and lined up with the natural flight path** from the lip, the
   wheels-down zone wide and far enough after the last ring, and a light steering assist toward the next ring if needed. A
   normal player (not the check pilot) must make it on the first try.
   *Check:* a "human-like" pilot (late, imprecise inputs, no boost; varied entry speeds and angles) makes the fly line
   ≥ 9 of 10 tries; both lines still about equal in time.
3. **Flight branches must never send you back to repeat them.** Dan was "reset back too far" and made to "repeat the same
   sequence over and over" on a flying shortcut (the Lake flight and/or the Sky Arch slit — check both, plus Coaster Coast's
   fly branch). Find the real cause in his logs and in a reproduction, and fix it:
   - Missing a ring or the branch's exit is a **near miss with a time penalty** or a **forward recovery**: put the racer back
     **on the route ahead** (at the branch's rejoin / next anchor), never behind the jump they already made.
   - Recovery anchors inside every branch; no reset loop (a reset must never put you somewhere that triggers the same reset).
   - General rule for every track: a reset never costs more than a few seconds of progress.
   *Check:* scripted failures on every flight branch (miss each ring, miss the exit, fly off the corridor, too low / too
   high): each ends ahead or at most one anchor back, 0 loops, lap continues; Dan's logged spots reproduced and fixed.
4. **Tidy-up:** Round 11's move of the queued round mangled `TODO.md`'s characters (UTF-8 read as Windows-1252: "â€”" for
   "—"); the planning chat repaired it. Read and write `TODO.md` / `CLAUDE.md` / `Docs/*.md` as UTF-8 (no BOM) from now on.
5. **Regression (both tracks) + 50-lap AI soak (Normal) on each, wrap-up, release build 14 (0.10.1).** Per-item status,
   "For Dan to check", update `CLAUDE.md`, commit, push, publish, clean the disk.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check
- See "For Dan to check (Round 12)" above.

## Round 13 spec (moved from TODO.md, delivered 2026-11-16): known bugs, more route choices, rival challenges

**Dan, 2026-11-16:** "I am busy and can't test — what can we queue up now." So this round needs **no testing or choices
from Dan**: no new tracks (Dan picks those), no new characters / voices (Dan designs those), no Tripo. Run unattended;
don't stop to ask; no visible window. Same testing rules. Read-only references as before. Round 12's "For Dan to check"
list stays open for when he has time.

1. **Fix the known reset / AI bugs from Round 12's report:**
   - Coaster Coast: the AI in Abi's car is reset at the station after the Midway flight (8 in 50 laps).
   - Paradise Plunge: the AI falls off the climb about 1 lap in 8 (x 80, z 1120).
   - Coaster Coast: a car on the car line reset just after the Midway Jump's landing (x 7924, z 408).
   Find each real cause (record, don't guess), fix, and make sure a player can't hit the same thing.
   *Check:* 50-lap soak on each track with **0** resets at those three spots; the player-side reproduction passes.
2. **A second choice jump with split paths on Paradise Plunge** (Dan, 2026-11-14: "Sometimes the jump where you choose
   jump or fly should be different paths"; deferred in Round 8). Where there's room (the Waterfall Return or the mangrove
   bank, or the Cliffside Climb): LB / E in the first second off the lip flies a short branch; staying a car takes its own
   different road. Both about equal in time, with the Round 12 rules from the start (room to make the exit, forward
   recovery, no repeat loops, near misses). Route strip / arrow follow the line taken. AI picks before the lip.
   *Check:* both lines timed, the human-like pilot makes the fly line ≥ 9 of 10, branch failure checks, AI 25 laps, 0 stuck.
3. **The Mangrove submerged bypass** (the concept's "later experiment"): an optional underwater route in Mangrove Run —
   dive before the S-bend, resurface before the Sunken Gateway — as a real choice (boat on the surface vs. sub
   underneath), about equal in time, with its own dive / surface zones, anchors and gates, and some underwater sights
   (roots, glowing fish, a sunken log arch). AI takes it on some laps.
   *Check:* both lines timed, transitions clean (no boat ↔ sub flicker), branch failure checks, AI 25 laps.
4. **Rival challenges in the Tour** (STORY.md: "optional rival challenges and practice events reuse the tracks to develop
   relationships"). After each Tour stop is cleared, unlock a short optional challenge on that track with one rival
   (e.g. "Beat Rae from the waterfall to the finish", "Stay ahead of Chip through the coaster"), with a few lines from the
   rival (their existing voices / ElevenLabs in their own voices, as before) and Buck / Barker. A small reward (a paint
   colour or a Garage badge). Saved in `tour.json`.
   *Check:* each challenge plays start to finish (win and lose), saves, and its lines fit (transcribed like Round 9).
5. **Steam Deck performance presets** (measured on the PC, Dan checks the Deck later): a **Graphics** setting
   (Deck / Low / Medium / High), auto **Deck** on a Steam Deck: render scale, shadows, draw distance, crowd and set-piece
   density, texture quality. Split screen picks a lighter preset automatically. Report frame times per preset on both
   tracks, single and split screen.
5c. **Coaster Coast: lost under the boardwalk / pier** (Dan, 2026-11-16: "it's too easy to get stuck under the boardwalk / pier and
   not know where to go"). Read his playtest logs; make the way through obvious (a clear lane between the pier legs, guide lights / a
   lit channel, the next gate and the HUD arrow visible from inside, the exit light visible early); no dead ends or pockets (close off or
   open up gaps; soft walls / currents back to the route); off the route a few seconds: THIS WAY + the arrow; truly stuck: forward
   recovery (Round 12's rules). AI and the human-like pilot get through without help.
   *Check:* player-view pictures along the under-pier route; scripted runs that wander off in each direction come back or are recovered
   forward; AI 25 laps with 0 stuck there.
6. **Regression (both tracks) + 50-lap AI soak (Normal) on each, wrap-up, release build 15 (0.11.0).** Per-item status,
   "For Dan to check" (add to Round 12's open list, don't replace it), update `CLAUDE.md`, commit, push, publish, clean the
   disk. Keep the .md files UTF-8.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (fill in at end of round)
- …

## Round 14 spec (moved from TODO.md, delivered 2026-11-16): menus, crowd volume, the Scrambler bubble, the Ferris wheel

**Dan's review of build 15 (0.11.0), 2026-11-16, quoted:**
0. (About Round 13's report) "It should list [what I need to check] to begin with." → **standing rule below.**
1. "The crowd noise is far too loud."
2. "Left and right should increase / decrease values."
3. "I don't like the menu in general. Needs clean up to be more readable."
4. "For the cycle sub version, I wanted a bubble that enveloped the whole bike, not just a dome. Pretty sure I mentioned this
   multiple times."
5. "Screen is shaky after race."
6. "The flying through the Ferris wheel was sold as a major thing on the track but the Ferris wheel is out of the main path
   and if you don't hit the fly button quick enough you miss it altogether."
Also from 2026-11-16 (asked for during Round 13 but never built): **the game must start on a title screen, not in a race.**
Run unattended; don't stop to ask; no visible window. Same testing rules. Read-only references as before.

**Standing rule (Dan, 2026-11-16): every round's final report in chat must end with the "For Dan to check" list** — the
actual items, in plain words, not just a pointer to `TODO.md`.

1. **Title screen and a clean main menu** (items 0, 2, 3 above).
   - Start-up (direct or from the launcher) lands on a **title screen**: ShiftStorm logo, the theme song, "Press A / Start".
   - Then a **main menu**: The Tour, Race, Time Trial, 2 Players, Garage, Settings, Quit. Big, readable text with plenty of
     space; one highlighted choice; a short description line for the highlighted item; consistent layout on every screen.
   - **Course select:** a card per track (name, picture, the forms it uses, best time) → **choose your racer** → race.
   - **Settings** grouped (Gameplay / Audio / Video / Controls), one setting per row; **left / right changes the value**
     (sliders for volumes, cycling for options), A also toggles, B goes back; the new value shows immediately.
   - **Pause menu** in a race, short: Resume, Restart, Settings, Quit to menu. Results screen: Race again, Next / Menu.
   - D-pad / stick / keyboard everywhere; readable on the Deck's 1280×800 (check text sizes there); split screen: either
     player can drive the menus.
   - Your checks still start races directly with their command-line flags.
   *Check:* pictures of every menu screen at 1280×800 (look at them for readability); a scripted walk title → menu →
   course → racer → race → pause → quit to menu → settings (left / right changes a volume and an option) → quit game.
2. **Crowd much quieter.** Cut the crowd (murmur, cheers, GO roar, finish) well below the engines and far below Barker and
   music — about −10 to −12 dB from now as a start — and give it its own **Crowd** volume slider in Audio settings.
   *Check:* measured levels per bus in a race and at the finish with defaults (runs stay muted; log the would-be levels).
3. **The Scrambler's sub: a full bubble around the whole bike.** Dan asked for the sub version "surrounded in a glass
   sphere" — a **complete clear sphere enveloping the entire bike and rider** (like a hamster ball / diving bell), with
   the chrome ring as its equator — **not a dome** over the seat. Bubble big enough that nothing pokes through, subtle
   highlights and a little refraction, readable from the chase camera, collider unchanged (or matching the sphere if
   that's better for the water). The change into the sub forms the bubble around the bike (grows / closes over it).
   *Check:* pictures of the Scrambler sub from the side, front and first person; nothing through the glass.
4. **No shaking after the race.** Find what shakes the camera after the finish (boost shake, wall bump, crowd roar shake,
   the T. rex roar, landing bump…), and stop all camera shake at the finish: the results camera is steady.
   *Check:* camera motion logged for 10 s after the finish on both tracks — no shake.
5. **The Ferris wheel is the star, on the main route.** Flying through the Ferris wheel is Coaster Coast's headline
   moment, so **everyone** does it: put a flight through the Ferris wheel **on the main route** (an automatic launch into
   the plane, like Paradise Plunge's cliff launch), with the wheel lined up dead ahead and big; fly through its spokes /
   the centre, then on through the fireworks. Keep a choice elsewhere if it still makes sense (say what you did).
   - **All choice jumps, both tracks:** easier to choose — the "LB: FLY" prompt appears on the **ramp** before the lip, and
     LB can be pressed **any time on the ramp or in the first ~1.5 s** of air (pre-arm counts).
   *Check:* pictures of the approach and the pass through the wheel; the human-like pilot goes through the wheel 10 of 10;
   LB pressed on the ramp works on every choice jump.
6. **Regression (both tracks) + 50-lap AI soak (Normal) on each, wrap-up, release build 16 (0.12.0).** Per-item status,
   "For Dan to check" in `TODO.md` **and at the end of the chat report**, update `CLAUDE.md`, commit, push, publish, clean
   the disk. Keep the .md files UTF-8.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

(For Dan to check: in TODO.md under Round 14's status.)

## Current Objective — Round 15 (not planned yet)
Dan picks the next round after playing build 16 (0.12.0). Open for him: "For Dan to check" for Round 14 (above).

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

## Current Objective — Round 16: not planned yet

Dan reviews build 17 (0.13.0); his notes become Round 16. Round 15's spec stays below for reference.

## Round 15 spec (delivered as build 17, 2026-11-16): the shimmer, Barnaby by name, and Big Top Breakdown

**Dan's review of build 16 (0.12.0), 2026-11-16, quoted:** "Tested new things. The shimmering only happens on the 2nd track
and it is before and after the race. No other issues, so go ahead and fold this into the next round." (Round 14's other
items: no issues.) Then Dan picked the rest (2026-11-16): **"Big Top Breakdown. Also Barnaby should be referred to by name instead of as
Barker."** Run unattended; don't stop to ask; no visible window. Same testing rules. Read-only references as before.

1. **Coaster Coast shimmers before and after the race** (Round 14 item 4 calmed the camera shake after the finish, but on
   Coaster Coast the picture still shimmers on the grid before GO and at the results). Only on Coaster Coast, only while
   stopped. Find the real cause before fixing — record it (camera transform and the car's body every frame while parked,
   the render settings, a sequence of still frames compared pixel by pixel). Likely suspects: the car / camera jittering
   at rest on the pier deck (physics at rest, the rail or the deck colliders), the camera fighting the grandstand / pier,
   temporal anti-aliasing or dynamic resolution, z-fighting on the boardwalk / pier planks / neon signs / water, or the
   night lights / fireworks flickering. Fix the cause, not the symptom.
   *Check:* 10 s parked on the grid and 10 s at the results on Coaster Coast: camera and car motion logged (≈0), frame-to-
   frame image difference near zero apart from things that are meant to move; same on Paradise Plunge (unchanged).

2. **Barnaby by name.** The race caller is **Barnaby** everywhere the player sees or hears it — subtitles ("Barnaby: …"),
   the booth sign, the results ("Race called by Barnaby"), credits, settings labels, the Tour, other characters' lines —
   not "TeedOff Barker" / "Barker". Regenerate any voice line that says "TeedOff Barker" / "Barker" about him (his own
   voice or others') so it uses "Barnaby". Code names / ids can stay. Update `CLAUDE.md`, `Docs/STORY.md`,
   `Docs/AUDIO-NEEDED.md`, `Docs/SOUNDS.md`, `Docs/REUSE-AUDIT.md` to say Barnaby (old check reports can stay as they are).
   *Check:* a search of the built game's text and the voice transcripts finds no "Barker" shown or spoken to the player.
3. **A third track: Big Top Breakdown — a travelling circus** (Dan's pick from `Docs/STORY.md`'s idea pool). STORY.md:
   *"Circus midway roads, aerial hoops, a log flume, and moving wagons. An old show-business acquaintance challenges Buck to
   prove he still has it."* Before building, answer STORY.md's four questions in `Docs/STORY.md` (fun / why Buck is here /
   who cares about this place / what changes). Same rules as Coaster Coast: same universe and reused models / systems, a
   **new environment** (not Tee'd Off's), and **clearly different from Coaster Coast** (that's a seaside fair at night; this
   is a big-top circus camp in **bright, gusty daylight** with striped tents flapping — "Breakdown": the show is a bit
   ramshackle and things keep going wrong).
   - **Route (~2 min safe line, all four forms), suggestions to build on:**
     - **Car:** through the midway between **moving circus wagons** (a wagon train you weave through, timed and readable,
       never a dead stop); **into the big top** and around the ring; a daredevil **Globe of Death** — a steel-mesh sphere
       you loop inside (like the Round 7 loop, on a rail); one plain big jump over the clowns' car pile-up.
     - **Plane:** a **human-cannonball launch** out of a giant circus cannon into flight **through aerial hoops** high under
       the tent, along the trapeze rigging, out through the tent's top flap and over the circus camp.
     - **Boat:** splash into the river beside the camp — past a paddle-steamer show boat — **not a log flume** (Coaster
       Coast has one; if a flume is used, make it clearly different).
     - **Sub:** under the river / into the escape artist's giant water tank or a sunken circus wagon; surface and drive back
       to the ring for the finish.
     - At least one choice with split paths (Round 14's LB rules: prompt on the ramp, LB on the ramp or first ~1.5 s), and
       one in the water. Round 12's rules: room to make every exit, forward recovery only, no loops.
   - **Life and set-pieces** (Round 8b's rule: big, close, in front; 8 % of screen height for 1 s): elephants spraying
     water, seals balancing balls, a tightrope walker overhead, trapeze artists swinging past the hoops, a strongman, clowns
     tumbling out of a tiny car, the crowd under the big top (Gridiron Glory crowd). Family-friendly, no fire hazards on the
     route. Models: reuse first; anything essential missing → **at most 2–3** Tripo prompts in `Docs/MODELS-NEEDED.md` for
     Dan, stand-ins meanwhile, don't wait.
   - **Story:** the Tour's **third stop** ("Growing success" in STORY.md's table). The old show-business acquaintance — a
     **ringmaster** who once shared a bill with Buck — challenges him to prove he still has it. Buck briefing / results
     lines for this stop (voice "ShiftStorm Buck"); Barnaby's lines for this track. The ringmaster's voice and look are
     Dan's call: write a short voice-design prompt and an image prompt for the ringmaster in `Docs/STORY.md` /
     `Docs/MODELS-NEEDED.md` for Dan, and use text-only subtitles (or Buck reading the challenge aloud) until then.
   - **Everything the other tracks have:** gates, near misses, route guides / arrow / strip, AI on all difficulties (they
     handle the wagons, the Globe and the splits), split screen, camera views, recovery, crowd, driver reactions, steering
     mechanisms, playtest logs, time trial / best times, course card in the menu, Tour stop + a rival challenge.
   - **Music:** no Tee'd Off music; Dan's new song `race_bigtop.mp3` (prompt in `Docs/AUDIO-NEEDED.md`, song 5 — added by
     the planning chat); a ShiftStorm song stands in until it arrives.
   *Check:* check-pilot lap with segment times, every split timed, the Globe at full / slow / very slow speed, the wagons
   (0 unavoidable hits), AI 25 laps per split with 0 stuck, a 50-lap AI soak, set-piece visibility per segment, a
   split-screen lap, frame time (PC) + Deck check list; player-view pictures of every segment — look at them.
4. **Regression (all three tracks) + 50-lap AI soak (Normal) on each, wrap-up, release build 17 (0.13.0).** Per-item
   status, update `CLAUDE.md`, commit, push, publish, clean the disk. **End the chat report with the "For Dan to check"
   list itself.** Keep the .md files UTF-8.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check
- See "For Dan to check (build 17)" under Completed Tasks.

## Current Objective
None set: waiting for Dan's notes on build 18 (Round 15b).

## Round 16 spec (delivered as build 19, 2026-11-17): Big Top's first flight, and Dam Shame (a three-lap race where the water rises)

**Dan's review of build 18 (0.13.1), 2026-11-17:** "No more shake. It is a little confusing to me where the first flight
is supposed to go in the circus." Then Dan picked **Dam Shame** (2026-11-17), as a multi-lap race where the course changes each lap (the planning chat's
proposal, Dan: "I meant how you described it"). Run unattended; don't stop to ask; no visible window. Same testing rules.
Read-only references as before.

1. **Big Top's Clown flight (the first flight, LB at the Clown Jump): make it obvious where to go.** Dan couldn't tell
   where it leads. From the moment the wings open, the player must see the destination: a clear chain of big rings / a lit
   path from the lip to the exit, the **exit itself visible and framed** (a banner, a bright arch, a spotlight on the
   wheels-down zone), the HUD arrow pointing at the next ring, the route strip showing the branch, and a camera that looks
   where the flight goes (not at the Globe). Barnaby can call it ("Through the hoops and back to the midway!"). Round 12's
   rules hold (room to make the exit, forward recovery). Check the cannonball flight the same way.
   *Check:* player-view pictures every half-second along the Clown flight and the cannon flight — is the next target always
   in view? the human-like pilot (no prior knowledge of the line, follows only what's on screen) makes it 10 of 10.

2. **A fourth track: Dam Shame — three laps, the water rises every lap.** STORY.md: *"Roads across a dam become spillway
   runs, submerged tunnels, and aerial launches. Buck books a 'controlled water demonstration' and misunderstands its
   scale."* Answer STORY.md's four questions in `Docs/STORY.md` before building. A **new environment**, clearly different
   from the jungle, the pier and the circus: a huge concrete dam across a **mountain canyon** — pine forests, cliffs, a
   reservoir, spillways, the turbine hall — in fresh **morning light** with spray and mist.
   - **The idea:** the **first multi-lap race** (3 laps). The route stays the same place, but **the water rises each lap**,
     so the course and the forms change:
     - **Lap 1, "dry run":** gates shut, mostly **car** — roads across the dam crest and down through dry spillway channels,
       a jump or two.
     - **Lap 2, Buck "opens one gate a smidge":** the spillways run — the same channels become **boat** runs down rushing
       water, with **flights** off the overflow lips.
     - **Lap 3, "the full demonstration":** the valley floods — low roads are under water and you **dive** through flooded
       tunnels and the turbine hall.
   - **How the water works:** one water level for the whole course, raised **when the leader starts lap 2 and lap 3**
     (warning siren, Buck and Barnaby lines, a ~10 s rise you can see). Every change zone decides the form from the **actual
     water at that spot**, so racers on different laps are always right; a car on a road that floods under it changes to
     the boat smoothly — never stranded, never drowned. Time Trial: the water rises at your own lap line.
   - **Multi-lap support** (new for the game): lap counter, gates per lap, positions across laps, lap times + best lap,
     results with each lap, the route strip / arrow / minimap per lap state, the race end after lap 3. Other tracks stay
     single-lap.
   - **Set-pieces** (8 % of screen height for 1 s): the spillway torrents and spray, the gates opening, the turbine hall's
     giant turbines, Buck waving from the control room window, trout leaping, logs and debris riding the flood, a cartoon
     beaver colony with its own little dam (a joke beside the real one), a heron. Models: reuse first; anything essential
     missing → **at most 2–3** Tripo prompts for Dan, stand-ins meanwhile.
   - **Story:** the Tour's **fourth stop** — STORY.md's "Growing success / Going too far": Buck's promises get bigger than
     the venue can safely hold. A local host who cares about the dam (e.g. the dam keeper) — text / subtitles until Dan
     makes a voice; write a short voice-design and image prompt for Dan. Buck briefing / results lines and Barnaby's lines
     for this track and for each rise.
   - **Everything the other tracks have:** near misses, choices with split paths (LB rules from Round 14), forward recovery
     (Round 12), AI on all difficulties (they handle every water state), split screen, camera views, crowd (on the dam
     crest), driver reactions, steering, playtest logs, time trial / best times, course card, Tour stop + a rival challenge.
   - **Music:** Dan's new song (`race_dam.mp3`, prompt in `Docs/AUDIO-NEEDED.md` song 6 — added by the planning chat); a
     ShiftStorm song stands in until it arrives. Optional: a little more energy each lap (filters / mix), not new songs.
   *Check:* check-pilot race (3 laps) with lap and segment times; every water state's route; a straggler a lap down while the
   water rises (no stranding, right forms); AI 25 races with 0 stuck; a 50-race AI soak; set-piece visibility per lap;
   split-screen race; frame time (PC) + Deck check list; player-view pictures of each lap state — look at them.
3. **Regression (all four tracks) + AI soaks, wrap-up, release build 19 (0.14.0).** Per-item status, update `CLAUDE.md`,
   commit, push, publish, clean the disk. **End the chat report with the "For Dan to check" list itself.** UTF-8 docs.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (fill in at end of round)
- …

## Current Objective
None set: waiting for Dan's notes on build 19 (Round 16).

