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

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (fill in at end of round)
- …

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
