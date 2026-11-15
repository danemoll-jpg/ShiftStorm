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
- **Round 10 — no stranding walls, test tracks out, Coaster Coast** (2026-11-15, release `build-12` / 0.9.0).
- **Round 9 — sensible steering, racing lines, one finish sound, the crowd, three new designs** (2026-11-15, release `build-11` / 0.8.2).
- **Round 8b — dinosaurs you can actually see** (2026-11-14, release `build-10` / 0.8.1). Status below.

### Round 10 delivery (2026-11-15) — status per item
Evidence: `Docs/Checks/round10/` (reports, before / after edge pictures, Coaster Coast player-view pictures). Every run: built exe, `-batchmode -mute`,
no window, Below Normal, one heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run but one: the
first 50-lap Coaster Coast soak read a single 0.44 peak with the volume at 0 the whole run (nothing can bypass it: no source ignores the
listener volume, the volume was never set above 0, no "sound ON" in the log). The meter now logs the time and the playing sources of
any non-zero peak; every run since (soaks included) read 0.000000. Sound was never unmuted.
1. **No stranding edges — done.** The promenade's 0.8 m barriers are gone (a flush red / white kerb); the ground beside the promenade and
   the finishing road is flush with the road (build 11: 0.5 m lower behind the barrier); the lakeside beach slopes into the lake (build 11:
   a 1.3 m step). Before the loop: two long angled stone guide walls from 75 / 85 m out (past the out-of-bounds line) to the loop's entry,
   so nothing in bounds is behind them and a car on the grass is glanced into the loop. The climb keeps its barriers (they rise out of the
   road 60 m before it); fall zones under its valley side reset at once; a low guide wall joins the canyon's east wall to the climb's
   barrier (cars down wide from the Lake flight ran off the end of the flush ground there: 36 AI resets in 50 laps, now 3). G1 is 22 m wide
   (15): the ground beside it is drivable now. *Check (`strand`, 36 scripted bad landings: every jump and the loop left / right / wide /
   angled / long / short, car and fly lines, the water ramp):* **0 strandings** (21 on the road, 6 drove back, 9 clearly reset); build
   11's layout (`-oldEdges`) in the same sweep: 2 stranded (round the outside of the loop). Pictures before / after: creek landing,
   promenade, loop approach (front and side), Lake Jump landing, canyon mouth, the climb's valley side, finishing road.
2. **Test tracks removed — done.** The Round 1 strip and four lanes, their menu rows, `-course 2..5`, the D-pad / F3 / F4 course switch and
   15 gray-box materials are gone; `-course 0..1` now picks Paradise Plunge / Coaster Coast. Every check that used them runs on Paradise
   Plunge (`Checks/CheckPilot.Round10.cs`): strip (from the climb's last anchor: 5 changes, PASS), the four lanes (each form at top speed
   on its part of the lap), E1-E6, slow fps: all PASS. Build data folder 141.69 MB (build 11) -> 143.35 MB (with Coaster Coast added).
3. **Scrambler hands on the grips — done.** Abi bends forward from the spine and chest (up to 35 deg), then the shoulders stretch (up to
   12 cm) to reach the bars (`Driver.ReachForward`). *Check (`bike`):* hands 0 / 0 cm off the grips in Car, Boat, Plane and Sub (build 11:
   10-20 cm short); pictures of each form.
4. **Coaster Coast — done (three stand-in models).** A seaside pier amusement park on its opening night (Docs/STORY.md): sunset turning to
   night over the lap, "GRAND OPENING" bulb marquees, fireworks, string lights, a new place (not Tee'd Off's). Boardwalk -> **Midway Jump**
   (choice: car under / plane through the hubless Ferris wheel) -> **coaster rail A** (launch pad to 72 m/s, airtime hill, banked turn, a
   loop, the **Coaster Gap** jump) -> **rail B** (corkscrew, lift hill, top hat, launch into flight) -> **flight** (round the
   helter-skelter, through fireworks rings, past the mascot balloon) -> **log flume** splash-down and flume under the boardwalk -> **the
   sea** (split: **Lighthouse round / rocks**) -> **under the fishing pier** (sunken carousel, the octopus) -> splash pool ramp -> finish.
   On a rail the car is locked to the track (booster tyres hold a minimum speed; it never stalls, rolls back or falls off). Ordered gates,
   near misses, route strip / arrow, AI on all difficulties (they ride the rails and pick both splits), split screen, camera views, the
   crowd (4 stands), Barker's own lines for it (53 new: the park, the rails, the splits, opening night, results per course; ElevenLabs, his voice, -16 LUFS), driver reactions, playtest
   logs, best time per course. Menu: a Course row (Race / Time Trial / 2 Players). Music: Dan's "Coaster Coast" song (`race_coaster`).
   *Checks:* pilot lap **107.8 s**, 11 / 11 gates, 0 resets, 5 changes; set-pieces **8 of 8 segments** >= 8 % of the screen for >= 1 s;
   rails A and B at full / slow / very slow: **6 of 6 through**, never under the booster minimum; splits: Midway car 11.97 s / fly 10.40 s,
   Lighthouse round 23.92 s / rocks 22.66 s (3 of 3 clean each); split-screen lap PASS (both valid, HUDs right in every sample); PC frame
   mean 4.2 ms, 95th pct 6.7 ms (one view), 8.5 / 12.8 ms split. Pictures of every segment looked at. Stand-ins: mascot balloon, octopus,
   whale (2-3 Tripo prompts in `Docs/MODELS-NEEDED.md`). **No Tee'd Off music anywhere** (`course_jurassic` deleted; the theme is the fallback).
5. **Regression + soaks + release — done.** Paradise Plunge: lap 124.6 s, round2, round2b, round5, round6, round8 (dinos, places after a
   check fix), round9, round10, choice (car 5.78 / fly 4.65 s), crowd (8 stands, only the raced course's cheer), barker (0 overlaps), quit
   (exit 0), difficulty Easy +20 % / Normal +7 % / Hard -3 %: PASS. Not passing, all pre-existing (build 11 too): round7's Cliff Gap
   short / slow case, round3's Lake flight shortcut timing (the check starts from the wrong anchor; the `choice` check times it).
   Soaks (Normal, 50 AI laps each, time scale 3): **Paradise Plunge 50 of 50**, 0 bad gate sequences, laps 131.7 / 134.7 / 145.9 s,
   5 resets; **Coaster Coast 50 of 50**, 0 bad gate sequences, laps 113.4 / 117.6 / 124.6 s, 18 resets (all after the Midway flight: see
   For Dan); 25 race starts: 0 touches, 0 resets; invert (fly and dive both ways) PASS.
   Fixed on the way: a coaster rail's race progress now follows the track (it could jump to the boardwalk 75 m away); a bump between
   racers is capped (6 m/s sideways, 15 % of the speed); the invert check measured the sub with no settle time and too near the floor.

#### For Dan to check (Round 10)
- **The promenade:** land wide off the Creek Jump and the Lake Jump on purpose: can you always drive back onto the road? Do the guide walls
  before the loop read as walls, and does the kerb look right?
- **Coaster Coast, start to finish:** the rails (does riding the coaster feel good, the launch, the loop, the corkscrew, the top hat into
  flight?), the Coaster Gap, the flume, the Lighthouse rocks, under the pier. Is ~108 s the right length?
- **Opening night:** the sunset-to-night sky, fireworks, "GRAND OPENING" marquees, Barker's opening lines; your Coaster Coast song in game.
- **The Midway Jump choice:** through the Ferris wheel vs under it (1.6 s apart): worth the risk?
- **Deck FPS on Coaster Coast** (R3), especially the flight over the park and split screen (PC: 4.2 ms, split 8.5 ms).
- **Scrambler:** Abi's forward lean to the bars: natural or too much?
- **Known:** on Coaster Coast about one AI lap in four is reset after the Midway flight (it lands and is thrown sideways near the coaster
  station); not seen with a single car in any probe; the AI loses ~5 s. Next round.

### Round 9 delivery (2026-11-15) — status per item
Evidence: `Docs/Checks/round9/` (reports, pictures). Every run: built exe, `-batchmode -mute`, no window, Below Normal, one heavy job at a
time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run; sound never unmuted.
1. **Steering at a sensible size — done.** Real proportions to Abi (a 0.26 m wheel = a real 0.38 m one), low where her hands still reach;
   the first-person camera 17 cm over her eyes, 5 deg down (was 12), its FOV 70 -> 80 (was 69 -> 98). Measured by rendering with and
   without the mechanism (what is really seen): wheel 11 / 16 / 18 % of the screen height (rest / top speed / boost), helm 5-13 %, yoke
   12-18 % (19-24 % at full bank on boost), levers 5-13 %; build 10 ~65 %. Chase views 1-6 %. Hands on the grips 4-13 cm (19 at full lock).
2. **Every line fits racing — done.** Out: Abi "That's not where I aimed", Rae "Too much muscle!", "Who planted that?!", "The lake can have
   it!", Vivi "Second place suits no one" (said for last place), plus two unused Abi clips. In (ElevenLabs, their own voices, -16 LUFS):
   Abi "That was NOT the racing line.", Rae "Too much throttle!", "Who put a WALL there?!", "I came here to race, not to swim!", Vivi "Last?
   Darling, that is simply not my colour." Barker's 158 lines all fit (his 7 Tee'd Off clips too). Every clip transcribed back (local
   speech-to-text): `Docs/Checks/round9/voice_lines.md`.
3. **One finish sound — done.** Tee'd Off's win jingle removed; one crowd sound per finish + Dan's song; no chime on the finish gate. Audit
   of every event in a full race (`sounds` check): 0 events with two sounds of a kind (build 10 also doubled: the landing splash, a jump's
   whoosh with the change's, a wall hit under a scrape); a one-of-a-kind rule now prevents it (`Core/SoundLog.cs`). `Docs/SOUNDS.md`.
3b. **The crowd from Gridiron Glory — done.** Its instanced fan sprites, atlas and shader (ported to URP) in four grandstands: both sides
   of the start / finish, the loop, the waterfall landing; 1,182 fans in tropical shirts, x1.35 size; they stand, jump, wave and clap as
   each racer passes (the cheer ripples along the stand at the racer's speed), roar at GO; murmur bed + cheers (never doubling a sound).
   Frame time with / without: 2.36 / 2.52 ms mean (noise), the crowd's CPU 0.014 ms a frame.
3c. **Hot Rod, Scrambler, Grand Prix — done (all 12 models in).** Garage shows all four designs ("Grand Prix", "Hot Rod" titles); Rae races
   the Grand Prix, Chip the Scrambler, Vivi the Hot Rod (in their own colours; recoloured only if you race the same one). Hot Rod: the
   chrome T-handle cut out of every form, the game's steering instead. Scrambler: 2.6 m bikes ridden by a 1.4x Abi astride, leaning into
   turns (up to 35 deg, looks only: same hull and handling), centre stand cut away, her hands on its own bars, a clear glass sphere on the
   sub's chrome ring. Raptor and plesiosaur: Dan's models rigged with Tee'd Off's rigger (copied) replace the stand-ins. Found and fixed
   on the way: every design's texture was shared by name (all designs showed the first one's texture). Note: Dan's first "hotrod" files
   were the Grand Prix; both are now in their own folders.
4. **Strip E6 — done (no change needed).** Passes on build 10 and on this build in three runs (Boat->Drive done 0.63 s before touchdown).
5. **Wrap-up — done.** Regression: round9 (steering, sounds, crowd, bike, designs), dinos, round1 x3 all PASS; soak (Normal, 50 AI laps,
   time scale 3): 50 of 50, 0 bad gate sequences, laps 130.9 / 133.8 / 146.9 s, 6 resets; `CLAUDE.md`, `Docs/REUSE-AUDIT.md`, `Docs/SOUNDS.md`; commit, push, release build-11.

#### For Dan to check (Round 9)
- First person in every form: the wheel / helm / yoke / levers low in the view, the road clear; does the higher eye point feel right?
- The finish: only Dan's finish song and one crowd sound.
- The crowd at the start (GO roar), the loop and the waterfall landing: noticeable enough?
- Garage: Grand Prix, Hot Rod, Scrambler (all forms); confirm the owners Rae / Vivi / Chip.
- Scrambler: the lean, Abi astride (her hands are ~10-20 cm short of the bars in some forms), the glass sphere on the sub.
- The raptors on the promenade and the long-neck in the lagoon (Dan's models).

### Round 8b delivery (2026-11-14) — status per item
Evidence: `Docs/Checks/round8b/` (reports, set-piece pictures from the player's chase view). Every run: built exe, `-batchmode -mute`, no
window, Below Normal, one heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run; sound never unmuted.
1. **Big, close, in front — done.** Brontos 26-28 m (head well over the palms), the T. rex 15 m, trikes 5.5-6 m, pterodactyls 20-30 m
   wingspan, raptors 4.2-5 m. Placed by distance along the route (`Spot`): at the end of a straight or the outside of a bend, 15-60 m from
   the road's edge, facing the racers coming. Set-pieces (16): two brontos' **necks arching over the promenade road** like a bridge (between
   the Creek Jump and the loop); **raptors running alongside** just outside the barriers after the start; a long-neck on the climb's cliff
   top and one dead ahead at corner1; **the T. rex dead ahead on the rock at corner2**, roaring + camera shake (now only at a racer on the
   road, not the plane); **a pterodactyl flying with you** and **one swooping across in front** during the flight; **brontos wading right by
   the boat line** + a long-neck swimming with you in the lagoon; a long-neck on the outside bank of every Mangrove bend + **a triceratops
   charging along the bank**; **a long-neck rising out of the Sunken Gateway pool** beside you + a bronto wading there; 60 m skeletons laid
   along the Coral Vault's outside walls; **brontos either side of the waterfall landing road**. Feet keep >= 5 m from the road's edge
   (check: 5.0 m, the T. rex); the pacers keep 17-27 m to the side (the pterodactyls 20-48 m and 5-14 m up). No colliders.
   **Barker's dinosaur lines** (bronto, T. rex, raptor, trike, pterodactyl, long-neck, skeleton) are no longer said at a place on the lap but
   only while that kind is >= 8 % of the screen for 0.5 s, and only if still big when he speaks: 5-6 lines a lap, **all said while it was big
   (5/5, 6/6)**. Check: `dinos`.
2. **Real visibility check — done, PASS 8 of 8.** A dino counts only while its box covers >= 8 % of the screen height in the chase view (in
   the frame, not behind rock, only the part above water when the camera is above it, not past the underwater fog's 46 m) for >= 1 s; time
   split per segment. Longest moment per segment: Promenade 3.5 s (9 dinos seen), Climb 3.5 s (3), Sky Arch 8.2 s (9), Lagoon 11.8 s (5),
   Mangrove 6.2 s (7), Gateway 2.8 s (2), Coral Vault 2.2 s (5 skeletons), Waterfall 3.4 s (3). Pictures of every set-piece taken when it
   fills a quarter of the screen height; **looked at** (14 kept in `Docs/Checks/round8b/`). Build 9's own check (any pixel in the frame)
   was what passed before; its pictures showed the problem.
3. **Performance — done, costs ~1.4 ms at the 95th percentile (PC).** Same lap, build 9 then build 10 back to back (render + GPU, 1280x800,
   GTX 1660 Ti): 95th pct 9.37 / 12.51, **8.36 / 9.80**, 8.68 / 12.02 ms. The PC was busy (other programs ~60 % CPU): pairs 1 and 3's
   "after" runs had 0.5 s stalls and +3 ms in the Coral Vault where nothing changed, so pair 2 is the fair one (+1.4 ms; Mangrove +2.9,
   Waterfall +2.3, the rest +0.3-1.6). Still well under 16.7 ms. Trims: dino shadows within 100 m (140 before), non-set-piece dinos drawn
   within 500 m (650), 3 filler trikes and a lake bronto dropped (56 dinos + 5 skeletons; build 9: 46), the roar clips loaded at start-up
   (the first roar caused a 130 ms frame). Steam Deck numbers need Dan's Deck.
4. **Wrap-up — done.** This status, "For Dan to check", `CLAUDE.md`, commit, push, release `build-10` (0.8.1), disk cleaned.

- **Round 8 — Jurassic Par for real, louder Barker, driver reactions, steering** (2026-11-14, release `build-9` / 0.8.0). Status below; spec stays under Current Objective until Dan's review.

### Round 8 delivery (2026-11-14) — status per item
Evidence: `Docs/Checks/round8/` (reports, player-view pictures, playtest summary). Every run: built exe, `-batchmode -mute`, no window,
Below Normal, one heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run. Sound was never unmuted.
**No visible window was opened** (item 9's OK was not needed: the tunnel was reshaped and measured with batch-mode runs and pictures).
1. **Dinosaurs — done (two stand-ins).** Tee'd Off's rigged bronto / T. rex / trike / ptero (20.5-20.9k tris each) built at run time and
   animated with Tee'd Off's ModelCreature code (walk, tail, neck, breathing, roar, wing beats): 46 along the lap — a wading herd in the lake,
   4 raptors pacing you beside the promenade, pterodactyls round the loop and past the flight, long-necks grazing beyond the canyon, a T. rex
   on the climb's cliff edge that roars (positional roar, camera shake), herds in the valley, triceratops in the mangroves, a swimming
   long-neck in the lagoon, brontos on the gateway ridge and the finish; nests, footprints, 5 glowing skeletons in the Coral Vault walls;
   positional calls. *Check:* seen per segment (player view, in range, not behind rock): Promenade 30, Climb 12, Sky Arch 37, Lagoon 5,
   Mangrove 18, Gateway 10, **Coral Vault 0** (fossils only: the "something big through the cave's windows" is not done — the cave has no
   windows), Waterfall Return 24 → 7 of 8 segments; none on the route (paths checked at build time; nearest 8 m: the Round 5 pterodactyl
   perched on the arch's nest). Raptor and plesiosaur are stand-ins (small T. rex, swimming bronto): Tripo prompts in `Docs/MODELS-NEEDED.md`.
   Crowd / cliffs / statues: still primitive (Tee'd Off has no meshes for them). Frame cost: lap mean 5.2 ms with them (build 8: 4.25), peak
   5.3 M triangles; drawn within 650 m, shadows within 140 m, animated only when seen.
2. **TeedOff Barker louder and more — done.** Cause found: Round 7's voice converter faded every new line out from its first sample, so his 39
   ElevenLabs lines were silent after 20 ms (-43 LUFS; Dan only heard the 7 Tee'd Off clips). All re-recorded + 112 new: **158 lines in 53
   pools** (form changes, overtakes by rival name, places, close racing, behind / catching up, walls, misses, loop, each jump, boost, 14
   landmarks and dinosaurs, fill-ins, results by place); every voice clip levelled to -16 LUFS; music and engines duck 8 dB under him. *Check
   (a race):* 9.9 lines / min (one every 6.1 s), longest silence 7.5 s, 0 overlaps, 0 repeats; would-be levels: Barker 6.7 dB over the
   ducked music, 15.4 dB over the engine (ElevenLabs about 4,200 credits this round, drivers included).
3. **Driver reactions — done.** Abi, Rae, Chip, Vivi: 7-8 mess-up lines each (3 new in their own Tee'd Off ElevenLabs voices: "Noooo!",
   "Oh no!", one in character; + their Tee'd Off clips), on a hard wall hit, reset, penalty, missed gate, failed loop; rivals only within
   70 m of a person's racer; Barker has priority (a yell waits up to 6 s for his line, he starts nothing new meanwhile). *Check:* a race
   with a forced reset: the yell came after his line, 0 overlaps. Rivals' yells weren't triggered in the checked races (they rarely crash
   near you).
4. **Steering mechanism — done.** Car wheel, boat helm, plane yoke (rolls with bank, slides with climb), sub twin levers; swaps with the model at
   the change's midpoint; Abi's hands on it by two-bone arm IK; data-driven (designs.json "steering" or a "Steering" empty; stick and
   motorcycle handlebars built in). *Check:* every form in first person and chase at full left / centre / full right (24 pictures): hands
   0-11 cm off the grips at centre, up to 21 cm at full lock (her arms are short for the far side). The first-person view tilts 5 deg more down.
5. **Results reactions by place — done.** 1st: Sitting Clap + win line; 2nd-3rd: Sitting Thumbs Up + Barker "podium"; last: Sitting
   Disapproval (no clap), Abi "Rematch. Now.", Barker "last", an "aww" instead of applause. *Check:* races finished 1st / 2nd / 4th: right clip
   and line each time (PASS).
6. **Sound levels — done.** Settings: **Music 100 %, Voices 100 %, Engines 60 % (new), Effects 100 %**; music 1.3x build 8 (+2.3 dB), engines
   at 60 % of build 8's level (-4.4 dB) and -8 dB more under Barker. An old settings.json: Engines = 60 % of its Effects value (Effects 80 % →
   Engines 50 %; checked with a build-8-style file). *Check:* would-be levels in a race (above), the duck -6.4 dB music / -7.4 dB engine.
7. **Choice jumps with different paths — done for the Lake Jump; the second choice jump deferred.** The car line now bends inland through a
   jungle canyon (rock walls, ferns, a fallen log, long-necks beyond it; G1 and the climb's start moved up to 22 m east); the fly line swings
   out west over the beach and the lake's herd through the rings. *Check:* car 5.78 s / fly 4.62 s lip → G1 (Round 7: 5.52 / 4.44), 0 resets;
   from the same anchor without boost the two are within 0.11 s; 28/28 AI stunt runs clean (14 flew, 14 car); pictures of both lines. A second
   choice jump: not built (time; it needs its own lap balance).
8. **Hitches — partly.** Profiled (a development build + `HitchProbe`): no GC, no allocations, no audio loads; the repeatable long frame
   (100-200 ms at the Lake Jump's lip in build 8 and every Round 8 profile) was a first render of scenery not seen before (Direct3D 12 builds
   pipeline states on first draw). Fix: a start-up warm-up renders the course from ~100 views (1.2 s with a warm shader cache): that hitch is
   gone. Remaining: 29-169 frames over 50 ms per lap varying run to run on this PC with no engine or script cause in the profile (outside the
   player loop), and the first run after a fresh install is much slower until the GPU driver's shader cache fills (lap mean 40 ms once, then
   5-8 ms). Needs a Deck measurement (see For Dan).
9. **Coral Vault tunnel — done (touches remain).** Reshaped: a straight chord inside the cave's loop at -27 m, the turns made at depth in a
   wider bowl, a straight 14 deg climb-out, a 17 m/s current (build 8: 9), two rock columns in the chord. *Check:* saves **1.9-2.1 s** (two builds: 1.92 / 2.12 s; build 8:
   1.15 s; target 2-3 s); 25 AI runs: 25 of 25 through, 0 stuck, 0 resets; a deliberate miss / scrape costs time and never traps. Not met: a clean run still glances
   the walls (2.0 touches a run, side walls; build 8: 2.6, first Round 8 shape: 3.5). No window was opened.
10. **Playtest logging — done.** `<data folder>/playtest/session-*.jsonl`: per race per player — driver, mode, difficulty, design, place, lap,
   penalties, segment times, resets (where / why), missed gates and near misses, shortcuts, Lake Jump pick, form-change incidents, wall hits;
   Menu > Settings "Save playtest logs" (On) and "Open log folder"; `python Tools/playtest_summary.py <folder>` → per player / per segment /
   incidents. *Check:* a race and a split-screen race (P1 Abi 2:06.87 2nd, P2 Rae 2:05.00 1st) logged; the summary read 5 session files.
11. **Regression + soak + release — done.** round7 (loop, jumps, choice, caller), round6 (bind, split, views: 0 frames in rock), round4 (walls,
   near misses, starts, difficulty: Easy +18 %, Normal +8 %, Hard -1 %), quit (exit 0): all PASS except one check scenario: a car coasting
   at 28 m/s up the Cliff Gap's kicker rolls back without a quick reset (Round 7's "PASS" there was a false one). Soak (Normal, 50 AI laps, time scale 3): 50 of 50, 0 bad gate sequences, laps 131.0 / 133.9 / 135.8 s; Lake flight 24x, tunnel 9x, slit 3x.
   This file, `CLAUDE.md`, `Docs/REUSE-AUDIT.md`, `Docs/SOUNDS.md`, `Docs/MODELS-NEEDED.md`; commit + push; release `build-9` (0.8.0); disk cleaned.

- **Round 7 — loops and big jumps, course art, sound, TeedOff Barker** (2026-11-14, release `build-8` / 0.7.0). Status below; spec stays under Current Objective until Dan's review.

### Round 7 delivery (2026-11-14) — status per item
Evidence: `Docs/Checks/round7/` (reports, player-view pictures). Every run: built exe, `-batchmode -mute`, no window, Below Normal, one heavy
job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run. Nothing needed a visible window.
0. **Music prompts — done.** `Docs/AUDIO-NEEDED.md` (based on Tee'd Off's `docs/music-suno.md`: Jurassic Par's instruments, instrumental,
   loop-friendly takes); whistling taken out at Dan's word (harmonica / pan flute / jaw harp / ocarina / melodica instead). **Dan's four
   songs are in:** Theme, Paradise Plunge, Garage, Victory, looped by `Tools/Music/make_loops.py` (copied from TO): race 89 bpm, a 36-bar
   loop after a 62 s intro; menu 58 bars; theme 77 bars; results plays once; every seam check OK.
1. **Loop-the-loop — done.** On the Beach Promenade (now a straight stunt strip along the lake), radius 24 m, 52 m tall, exit lane 16 m to
   the right; mossy stone / wooden planks, cyan edge lights, two towers, a boost pad 70 m before it (tops up to 72 m/s). The racer line runs
   round it, so progress / gates / the strip just count along it; a car on it rides a rail (real gravity along the track, grip check);
   too slow: it slides back and is reset at the loop's anchor; no "wrong way" upside down; the camera follows the car's own up (every view).
   *Check:* full speed PASS (68 m/s over the top), slow entry 34 m/s PASS (through, 50 m/s over the top), very slow 15 m/s PASS (slid back,
   reset "too slow for the loop"), 25/25 AI runs, 0 stuck; pictures at 90 / 180 / 270 / 345 deg in Chase, Far, First person, Front.
2. **Big car jumps — done.** Creek Jump (promenade, plain), Lake Jump (promenade, the choice), Cliff Gap (Cliffside Climb, plain; a
   ravine with a stream). Kickers 45 m, about 8 deg at the lip, 36-40 m gaps, long straight landings; a car landing after 0.35 s in the
   air keeps its air speed for 0.25 s. LB / E opens the wings only in the first 1 s off the Lake Jump's lip (HUD "LB: FLY"); the plane takes
   the Lake flight (a climb over the lake-side beach through three gold rings, entry / exit sub-gates) and turns back into a car in the
   marked wheels-down zone before G1 (first try swung 95 m out over the lake: too tight for the plane, reshaped). *Check:* Creek 1.95 s air
   (2.38 s on boost), 59 -> 61 m/s, 0 resets; Cliff Gap 1.95 s, 59 -> 60 m/s; short / slow jumps reset at once (creek water / the ravine's
   reset zone); car line 5.52 s lip -> G1, fly line 4.44 s (from a 45 m/s restart, so the car was slow there; at full speed they are
   close); LB late and LB elsewhere do nothing; the AI decides before the lip: 24 of 50 soak laps flew on Normal. *Stunt runs:* 7 races x 4 AI (Normal) from the grid to G2: 28/28 clean (one loop each, no reset), 0 loop fails, 14 flew / 14 stayed a car.
3. **Lap honest — done.** Safe-line pilot lap 125.0 s (build 7: 122.8): promenade 18.4 s (16.4), the rest unchanged; the AI on Normal
   131-140 s. The route strip shows the jumps (amber; the choice violet) and the loop (cyan).
4. **Course art — done (procedural, Tee'd Off's shapes).** Promenade: beach, boulders, driftwood, a jungle wall, pennants, grandstands with a
   crowd and chequered flags at the line, brontosaurs in the lake, pterodactyls round the loop, a smoking volcano, hot springs by the creeks.
   Mangrove: lily pads, reeds, vines, logs, a triceratops. Sunken Gateway: two giant stone heads with glowing eyes, glyph pillars, bubbles.
   Coral Vault: coral clusters, light shafts from the roof, bubble columns. Waterfall Return: ferns, boulders, mist, a rainbow, foam.
   Still not like TO: everything is primitive lumps (no TO meshes for cliffs / statues), the crowd is spheres, no foam shader at the creeks.
   No essential model missing (no Tripo prompts added). *Perf (PC):* lap mean 4.25 ms, 95th pct 6.7 ms, peak 5.19 M triangles (build 7:
   4.44 M); 27 frames over 50 ms in the lap (worst 216 ms; build 7: 6-10): see For Dan.
5. **Sound pass — done.** Engines per form from Chastain Rush's synthesis (+ Hostile Horizon jet / sub layers), change sounds by form,
   boost, gates and rings, near miss, boost pad, loop, jump whoosh and landing, scrapes, countdown, GO, finish, win jingle, menu clicks;
   music mix by form (flight louder, underwater 650 Hz low-pass, a lift at the waterfall exit); Music / Effects / Voices volumes in
   Settings. Every sound and its source: `Docs/SOUNDS.md`. *Check (muted):* every effect fired in a race; engine pitch Car 0.79-2.02,
   Boat 1.44-2.00, Plane 1.84-2.31, Sub 1.39-1.50; music low-pass about 730 Hz on average underwater.
5b. **TeedOff Barker — done.** `Race/Caller.cs`: 39 new lines in his own voice (ElevenLabs, 949 credits) + 7 of his TO clips; pools with
   cooldowns, a gap after each line, priorities; subtitles "TeedOff Barker: ..."; results "Race called by TeedOff Barker"; his booth with
   his name board and Barnaby himself (TO model, "Standing Arguing" loop) beside the start line; one caller in split screen. *Check:* a race:
   10 lines from 9 pools (trial, go, jump, wings, lead, shortcut, final, win, results), 0 repeats within 20 s.
6. **Regression + soak + release — done.** Round 6 (bind / split / views: one lap per view, 0 resets, camera never inside rock) PASS;
   lap / results / restart PASS; soak 50 AI laps Normal PASS (50/50 within 300 s, 1 reset); release build 8 (0.7.0).
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

## Current Objective — Round 10: no stranding walls, test tracks out, a roller-coaster track

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

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check
See "For Dan to check (Round 10)" under Completed Tasks.

## Next Steps (Do Not Start Yet)
- **More tracks:** Lantern Reef (boat + sub; draft in `TODO-ARCHIVE.md`),
  Clockwork Cascade (car + boat), Cloud Orchard (car + plane). Dan picks;
  new environments, not Tee'd Off's (Dan, 2026-11-15).
- **Playtest itself** (concept's evaluation plan): a few players of mixed
  skill, first attempt + three practice runs, per-mode questions, using the
  Round 8 logs. Dan runs it when he's ready.
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
