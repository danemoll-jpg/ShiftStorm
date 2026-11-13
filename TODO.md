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
- **Round 1 — foundation and the four modes** (2026-11-13, release `build-1` / 0.1.0). Delivery below; waiting on Dan's review.

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

### For Dan to check
- On the Deck: install `ShiftStorm-0.1.0-Full.zip` from ShiftStorm-Releases (non-Steam game, Proton Experimental); does it reach 60 fps,
  especially at the five changes?
- Drive the strip: does each change feel in control (0.6 s, steering live)? Is the car fun with the drift (RB)?
- Boat: is the slide too broad / the current (side push near the buoys) readable? Diving: does it feel different from flying?
- The default climb / dive direction (pull back = up) and the invert toggle (View button).
- Pick a prototype driver and location from `Docs/REUSE-AUDIT.md` section 3.
- Placeholder look: the folded wings still show at the sides in car form; the canopy tilts open in car / boat form. Fine for gray box?

## Next Steps (Do Not Start Yet)
- **Round 2 — gray-box Paradise Plunge lap.** All eight segments (Beach
  Promenade → Cliffside Climb → Sky Arch → Lagoon Landing → Mangrove Run →
  Sunken Gateway → Coral Vault → Waterfall Return), sized from Round 1's tuned
  speeds, with ordered gates G0–G10, recovery anchors, time trial and results.
  Waits on Round 1 handling.
- **Round 3 — three AI rivals and the two shortcuts** (Sky Arch inside slit,
  Coral Vault low tunnel). Waits on a reliable lap.
- **Driver and art slice.** Waits on Dan picking a driver and location from
  `Docs/REUSE-AUDIT.md`. Any new character / organic models go through the
  manual Tripo pipeline in `CLAUDE.md`. The real transforming vehicle is built
  by script in Blender (hard-surface), not in Tripo.
- **Later experiment:** optional submerged bypass in Mangrove Run — only after
  the base lap works (concept doc).

## Technical Notes / Blockers
- Unity batch runs need the sandbox off (UPM named pipe). The world is built in code at start-up; there is no hand-made scene content.
- Frame times from batch-mode checks are CPU only; a windowed or on-Deck measurement is needed for real GPU numbers (needs Dan's OK
  for a visible window, or his Deck).
