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

- **Round 2 — the full gray-box lap, Jurassic Par atmosphere** (2026-11-13, release `build-2` / 0.2.0). Status below; the Round 2 spec and Round 1's per-item status are in `TODO-ARCHIVE.md`.

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

## Current Objective — Round 2 review (Dan)
Round 2 is delivered (see Completed Tasks). Next: Dan reviews build 2 on PC and the Steam Deck, then Round 3 starts.

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
- Round 2: frame-time hitches (up to 0.7 s, 99 frames over 16.7 ms in one full lap; 96 ms worst in an earlier run) are not yet explained.
  Next step: a profiled lap (development build), then shader warm-up / GC / scenery LOD as needed. Scenery peaks near 3 M triangles.
