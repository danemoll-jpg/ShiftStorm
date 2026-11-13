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
None yet.

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

### For Dan to check (fill in at end of round)
- …

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
- None yet.
