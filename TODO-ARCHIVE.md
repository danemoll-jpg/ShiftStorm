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
