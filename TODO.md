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
  "For Dan to check" list — **in the chat report itself, at the end**, not
  only in `TODO.md` (Dan, 2026-11-16). Per-item status is required: done / not done /
  deferred, and why.

## Completed Tasks
- **Round 16 — Big Top's first flight made obvious, Dam Shame (the first three-lap race, the water rises every lap)** (2026-11-17, release `build-19` / 0.14.0). Status below.
- **Round 15b (hotfix) — the screen shake on Coaster Coast and Big Top** (2026-11-16, release `build-18` / 0.13.1). Status below.
- **Round 15 — the shimmer's cause fixed, Barnaby by name, Big Top Breakdown** (2026-11-16, release `build-17` / 0.13.0). Status below.
- **Round 14 — title screen and clean menus, crowd much quieter, the Scrambler's whole bubble, no shake after the race, the Ferris wheel on the main route** (2026-11-16, release `build-16` / 0.12.0). Status below.
- **Round 13 — known resets fixed, Cliff flight, Mangrove bypass, rival challenges, Graphics presets, Coaster Coast's pier** (2026-11-16, release `build-15` / 0.11.0). Status below.

### Round 16 delivery (2026-11-17) — status per item
Evidence: `Docs/Checks/round16/` (reports, pictures). Every run: built exe, `-batchmode -mute`, no window, Below Normal; measured audio
output peak 0.000000 (AudioListener.volume 0) in every run; sound was never unmuted. One slip: for about 20 minutes two check queues
ran at once (a stopped queue's shell kept going); everything that overlapped was run again on its own afterwards.
1. **Big Top's Clown flight made obvious — DONE (human-like pilot 9 of 10, not 10 of 10).** Four striped circus hoops along the flight
   (the HUD arrow points at the next hoop, then the exit), the Globe of Death moved 12 m east to give them room, a lit arch over the road
   at the wheels-down zone ("TO THE BIG TOP", bulbs, two spotlights on the chevrons), a gentler last bend and a wider, deeper wheels-down
   zone; a plane that gets its wheels high up now comes down within about a second instead of sailing on (all courses). In flight the
   chase camera turns up to 25 deg toward where the route goes (flight branches, and 3 s after any change into the plane). The route strip
   shows every choice jump's flight branch in violet. Cannonball flight: rings bigger and brighter, a "SPLASHDOWN" arch on the river, the
   cannon's wings zone deeper. Barnaby: "Wings out! Through the hoops and on to the big top!" (+ a hoops line).
   *Checks (`bt16`):* pictures every 0.5 s: the next hoop / exit on the screen in **7 of 7** (Clown flight; this round's first try had 5 of 11) and the next hoop / ring / arch in **30 of 30** (cannonball). Human-like pilot (HUD arrow only, late, keys or a noisy
   stick): Clown flight **9 of 10** (first try of the round: 0 of 10, then 3 of 10; the one miss came down 37 m west of the road 30 m up,
   landed beside the door and was put forward, +3 s); cannonball flight **9 of 10** (the miss never got into the cannon's mouth, Round 15's
   entry, and was put forward into the flight). AI: Clown Jump fly 10 / 10, car 10 / 10, river both ways 10 / 10.
2. **Dam Shame — DONE (stand-ins for the beavers, heron and Hattie; Dan's song not in yet).** STORY.md's four questions answered (Buck's
   "controlled water demonstration" at the Lockhart Dam; Hattie Sluice, the dam keeper; the Tour's Stop 4 "The Controlled Demonstration";
   Buck admits it was "a bit much"). A dam across a mountain canyon in morning light (pine forests, cliffs, snowy peaks, the reservoir,
   the spillway, the turbine hall), 9 km south of Paradise Plunge. **The game's first multi-lap race (3 laps of 2.97 km).** Lap 1: crest
   road, the dry spillway chute, the flip bucket (a choice jump: LB = the Canyon flight over the river and the beavers), the valley road,
   the bridge, the turbine hall, the dam road back up. Lap 2 (siren, Buck "a smidge", the valley awash): boat down the torrent, the lip
   throws you into the air, boat over the drowned roads and through the hall, wheels down at the dam road. Lap 3 (the full
   demonstration, 16 m of water): boat down the chute into the flood, sub along the drowned road and through the flooded turbine hall,
   surface and wheels down where the dam road leaves the water. The water rises when the leader starts lap 2 / 3 (time trial: your own lap
   line); every zone and reset decides the form from the water, and a racer the water reaches is changed (car -> boat, boat held under ->
   sub, boat on a dry road -> car). Multi-lap: LAP n / 3 on the HUD (+ a flash, FINAL LAP), gates per lap, positions across laps, one split
   per lap (the lap times), best lap on the results and on file, the course card says "3 laps / Best race / lap". Set-pieces: the dam,
   the spillway gates and torrent, the turbines, Buck waving from the control room, the beaver colony ("DAM GOOD"), a heron, trout, logs
   (and a duck) riding the flood. Crowd on the crest, near misses, AI, split screen, views, reactions, playtest logs, Tour Stop 4 and
   Rae's Flood Run challenge (paint "Spillway Teal"). Voices: Barnaby 38 lines, Buck 24, Rae 3 (ElevenLabs); Hattie speaks in subtitles
   (voice prompt in STORY.md). Music: `race_dam` once Dan's "ShiftStorm - Dam Shame.mp3" arrives (`make_loops.py race_dam`); the
   ShiftStorm theme plays meanwhile. Tripo prompts: beaver, heron, Hattie (`Docs/MODELS-NEEDED.md`).
   *Checks:* `damlap` PASS: 147.3 s (laps 45.8 / 44.6 / 56.9), 16 / 16 gates in order, 0 resets; the water rose at 50 s (-2.5 -> 1.4 m)
   and 94 s (1.4 -> 16 m); forms per lap: car / boat, plane, boat, car / boat, sub, boat, car; a set-piece >= 8 % of the screen for >= 1 s
   in 3 of 3 laps; PC render + GPU mean 3.9 ms, 95th pct 5.3 ms; form changes at most 16 ms. `damrise` (stragglers while the water rises
   round them) **7 of 7** (a car in the valley, a car in the hall, a car through both rises, a boat in the hall, a boat on the drowned
   road, a car at the dam road's foot, a car on the chute): each reached its lap line, no reset. Fixed on the way: the crest's valley-side
   parapet blocked the bend into the spillway (the first lap check looped there), an AI that had planned the Canyon flight kept flying it
   after the water closed it, the Canyon flight's wheels-down turned lap 3's subs into boats. Split screen (Dam Shame, 2 players + 2 AI)
   PASS, both HUDs right 144 / 144. AI soaks (Dam Shame, 4 AI per race, 3 laps each): Normal 199 of 200, Easy 32 of 32, Hard 31 of 32, 0 bad gate sequences;
   the two unfinished were AI stragglers caught in a reset loop at one gate (205 and 136 resets) after the water rose round them.
   Fix: the third reset in a row for the same missed gate now puts the racer past it (+3 s, all courses). Run again: Normal 80 of 80
   (6 resets), Hard 32 of 32 (4 resets); the loop did not come back, so the new rule is in place but was not needed in those runs.
3. **Regression + release — DONE.** `round2` PASS (Paradise lap 124.65 s), `cclap` PASS 106.33 s, `btlap` PASS 94.97 s, `btglobe` 6 / 6,
   `btsplits` 40 / 40, `round11` PASS (Buck on every course, The Tour's four stops, 16 / 16 racers), `challenges` 8 of 8 (each of the four won and lost; Rae's Flood Run included), `menuwalk`
   13 / 13 + exit 0, `unlock` PASS, `menus14` (the course cards as a carousel of four). Release build 19 (0.14.0) published; disk cleaned.

### For Dan to check (build 19)
1. Big Top Breakdown: LB on the Clown Jump's ramp. Can you see where to go now (the four striped hoops, then the lit "TO THE BIG TOP"
   arch over the road)? Did you land on the road before the big top's door?
2. The cannon: the rings over the camp and the "SPLASHDOWN" arch on the river. Clear enough?
3. Dam Shame (Race, Time Trial, or The Tour's Stop 4): three laps. Lap 1 dry, lap 2 the spillway runs (boat down the chute, thrown into
   the air off the flip bucket), lap 3 the valley floods (dive through the turbine hall). Does each lap feel different enough?
4. When the water rises (siren, Buck on the tannoy): does it read well? Was your machine ever caught in the wrong form?
5. Lap 1's flip bucket: LB on the ramp for the Canyon flight over the river and the beavers' little dam. Worth it?
6. The HUD's LAP 2 / 3, the FINAL LAP flash, and the lap times on the results screen.
7. The stand-ins: the beavers, the heron, Buck in the control room window. Make the beaver / heron / Hattie models when you have time
   (prompts in Docs/MODELS-NEEDED.md), and Hattie's voice if you like (prompt in Docs/STORY.md).
8. Your song: save it as `Art/Incoming/Music/ShiftStorm - Dam Shame.mp3`; until then Dam Shame plays the ShiftStorm theme.
9. Rae's Flood Run on The Tour page.

### Round 15b delivery (2026-11-16) — status per item
Evidence: `Docs/Checks/round15b/` (before / after reports and heat maps). Every run: built exe, `-batchmode -mute`, no window, Below Normal,
one heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run; sound was never unmuted.
1. **Reproduced — DONE.** New `shake` check on the Deck graphics preset, the real menu path with the player's buttons (title -> Race -> the
   course cards Paradise -> Coaster -> Big Top -> back), a Coaster Coast race -> results -> Menu -> Paradise selected, a Paradise race and a
   Big Top race; every frame the camera's final position / aim / FOV / projection (CSV), and the picture's flicker on 60 Hz frames (a pixel
   that jumps and jumps back: A -> B -> A) with heat maps, plus pinned-camera windows. Findings: the camera itself is perfectly smooth in
   every menu (0.000 deg off its path at 60 Hz), the projection never changes, and with the camera held still nothing in the world moves,
   so it was not a camera shake, not the shadows (shadows off: the same), not the Deck's render scale (full resolution: the same). The
   flicker was **the menu / HUD itself**: Paradise Plunge 0.002 % of the pixels a frame, Coaster Coast 0.85 % (95th pct 2.9 %), Big Top
   0.26 % (95th pct 3.0 %), and the heat maps light up every letter, card frame and bar.
2. **The cause, fixed — DONE.** The HUD / menu canvas sat 0.22 m in front of the race camera, so it was tiny (1 pixel ~ 0.3 mm) and had world
   positions right next to the camera. Coaster Coast is built 8 km east of the world's origin and Big Top 9 km west, where the computer's
   numbers only hold a position to about 1 mm, which is ~4 pixels on the Deck's screen at 0.22 m: every time the camera moved, the whole
   menu and HUD snapped back and forth by pixels. Paradise Plunge sits near the origin, so it was steady. (Dan's "even Paradise after a
   Coaster Coast race": in the check, Paradise's menu is steady after a Coaster Coast race too, 0.002 %; the menus right after that race
   still show Coaster Coast behind them, which shook.) Fix (`UI/Hud.cs`, `Resources/Shaders/HudUI.shader`): the canvas now stands 100 m
   ahead (1 mm there is ~0.01 pixel) and a HUD shader draws it straight in front of the camera at the old distance, never through a world
   position. Looks the same (same layout, same depth against the cockpit); the text is cleaner far from the origin. Two first tries were
   dropped: a separate UI camera (URP drew no UI in it in the checks, so it could not be verified) and a canvas parked at the origin (not
   drawn 8 km away).
   *Check (`shake`, Deck preset) before (`-oldHud`) -> after:* menu flicker Coaster Coast 0.848 % -> **0.006 %**, Big Top 0.255 % ->
   **0.014 %** (95th pct 2.9 / 3.0 % -> 0.016 / 0.040 %), Paradise 0.002 % -> 0.002 %, Paradise after a Coaster Coast race 0.002 % ->
   0.002 %: **SHAKE PASS**. Big Top race 0.81 % -> 0.44 %: the HUD no longer flickers at all (only the changing digits); what is left is the
   scenery's own edges (the striped tent and road dashes) as the camera moves, the same kind as on every course. The race cameras were
   already smooth against the car. Not verified on the Deck's own screen.
3. **Regression + release — DONE.** `round2` PASS (Paradise lap 124.58 s), `cclap` PASS 106.30 s, `btlap` PASS 94.83 s, `shimmer` PASS,
   `split` PASS (both halves' HUDs right in 122 of 122 samples), `menuwalk` 13 / 13 + exit 0, `menus14` pictures. Release build 18 (0.13.1)
   published; disk cleaned.

### For Dan to check (build 18)
1. Race > the course cards: step to Coaster Coast and Big Top Breakdown. Is the screen steady now?
2. Race Coaster Coast to the end, then the results and the menu. Steady? Then Paradise Plunge: steady?
3. Race Big Top Breakdown. Is the race steady now (the timer, place and speed in the corners should be rock still)?
4. If anything still moves, film it like last time and say which screen. Edges on the striped big top tent and road lines flicker a
   little as you drive (no anti-aliasing on the Deck preset). That is a different thing; say if it bothers you.
5. Still open from build 17: everything in Round 15's list below (Big Top's route, the rapids, its length, the stand-ins, Stop 3's scene).

### Round 15 delivery (2026-11-16) — status per item
Evidence: `Docs/Checks/round15/` (reports, pictures, heat maps). Every run: built exe, `-batchmode -mute`, no window, Below Normal, one heavy
job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run; sound was never unmuted.
1. **Coaster Coast's shimmer — DONE (cause found and fixed).** New `shimmer` check (10 s parked on the grid, after the finish, and on the
   grid after Race again, every course; camera / car / sun / sky every frame, the picture compared frame to frame, heat maps). Build 16:
   camera and car perfectly still, but on Coaster Coast the sun turned 27 deg in 10 s on the grid after Race again (171 steps of the sky's
   evening value): `CoasterSky` eased the sky from night back to sunset at .04 / s in .002 steps, each turning the sun ~0.15 deg, so the
   toon shading's light / shadow bands and every shadow crawled across the picture (the heat map: edges changing all over a still scene);
   only noticeable while parked, only on the course with a moving sun. Fix (`World/CoasterLook.cs`): the evening moves only while a person
   is racing; parked (countdown, after the finish, front menus) it is set once, at once. *Check:* PASS on all three courses: sun 0.0000 deg,
   0 sky changes in all 9 parked windows (build 16: 27 deg, 171 changes). The frame-to-frame picture number is small either way (0.2 / 255;
   a 0.15 deg step is subtle per frame, it is the steady crawl the eye sees) and is now only the sea, rides and fireworks. Paradise Plunge
   unchanged. `-oldSky` keeps build 16's. Not verified on the Deck's screen itself.
2. **Barnaby by name — DONE.** `Caller.Name` = "Barnaby": subtitles ("Barnaby: ..."), the booth's board, the results ("Race called by
   Barnaby"), the Voices volume description; code comments, check reports and the audio tools too; `CLAUDE.md`, `Docs/STORY.md`,
   `Docs/AUDIO-NEEDED.md`, `Docs/SOUNDS.md`, `Docs/REUSE-AUDIT.md` say Barnaby (the ElevenLabs voice keeps its own name "TeedOff Barker";
   the clip folder `Voice/barker` is an id). No voice line ever said "Barker", so nothing needed re-recording for the name. *Check*
   (`Tools/Audio/barnaby_audit.py` -> `Docs/Checks/round15/barnaby.md`): PASS: 0 strings a player can see say "Barker" (the shipped Assembly-CSharp.dll and data files), 0 of 384 voice clips say it (written or heard by local speech-to-text); the 53 new lines all transcribe back at a 0.80+ word match.
3. **Big Top Breakdown — DONE (stand-ins for the animals, performers and the ringmaster).** Course 2 (`World/BigTop*.cs`, `BigTopLook`): a
   circus camp in bright, gusty daylight (flags and the top flap flapping), 9 km west of Paradise Plunge. STORY.md's four questions
   answered (Lionel Lustre, ringmaster of the Lustre Brothers Circus, dares Buck; the Tour's Stop 3 "The Big Top Dare"). Route (line
   5.5 km): the Midway (the wagon parade rolling along both side lanes, the middle always clear; the strongman, the tightrope walker) ->
   the **Clown Jump** over the clowns' car pile-up: **choice** (car: the **Globe of Death**, a loop rail inside a steel-mesh sphere; LB on
   the ramp: the Clown flight past it) -> the big top's door, once round the ring (six stands of fans) -> the **giant cannon** (a rail up
   its barrel, launch to 80 m/s) -> **human cannonball** flight through three hoops beside the trapeze artists and out of the **top flap**
   -> two long arcs over the camp -> the **river** (elephants spraying, the paddle-steamer show boat, the island: main channel or the
   **Island rapids** (current, rocks): the water's choice; sea lions) -> **dive** into the lake past the escape artist's tank and through a
   **sunken circus wagon** -> up into the splash pool, its ramp, the finish. Everything the other tracks have: gates, near misses, route
   guides / arrow / strip, AI, split screen, camera views, recovery, crowd, reactions, playtest logs, best times, a course card, the Tour's
   Stop 3 + **Vivi's Cannonball Run** challenge (paint "Vivi Violet"). Music: Dan's "Big Top Breakdown" song (`race_bigtop`, it was already
   in Art/Incoming). Voices: Barnaby 30 new lines, Buck 20 (+ "Three stops" re-recorded), Vivi 3. Tripo prompts (elephant, sea lion,
   ringmaster) + the ringmaster's voice prompt: `Docs/MODELS-NEEDED.md`, `Docs/STORY.md`.
   *Checks:* `btlap` PASS: 94.8 s (check pilot, safe line; segments 16.0 / 9.4 / 14.6 / 19.1 / 28.4 / 7.4 s), 11 / 11 gates, 0 resets, 0
   wagon hits while racing; set-pieces >= 8 % for >= 1 s in 6 of 6 segments; PC render + GPU mean 4-6 ms per segment, 95th pct <= 7.3 ms.
   `btglobe` PASS: the Globe and the cannon at full / slow / very slow, 6 of 6. AI runs (`btsplits -runs 25`): Clown Jump car 25 / 25 and
   fly 25 / 25 clean, river main channel 25 / 25 and rapids 25 / 25 clean, 0 stuck, 0 resets, 0 wagon hits. Split-screen lap PASS (0
   resets). Soak (Normal, 50 AI laps) 50 / 50, median 104.6 s, 2 resets (an AI arriving off-line missed the cannon's rail, drove under
   the top flap and was put forward onto the flight line). Fixed on the way: the cannon's wings zone reached the ring road (a rival grew
   wings on the ground), a car still in the air at a rail entry was snapped down 6 m in one step (now eased: also smooths Coaster Coast's
   station entry), Vivi's challenge lines were not loaded, the Tour page's rows ran off the screen, G9's frame was too low.
   **Not as asked:** the lap is ~1:35 for the check pilot (~1:45 for the AI), not quite 2 min; the Island rapids are 4.4 s *slower* for the
   AI (it brakes through the rocks; a person may do better, not verified).
4. **Regression + soaks + release — DONE.** Regression (built exe): `round2` PASS (Paradise Plunge lap 124.6 s), `cclap` PASS 106.4 s,
   `ccrails` 6 / 6, `round11` (Buck on all three courses, The Tour's three stops, 16 racers) PASS, `challenges` 6 / 6 (Vivi speaks now),
   `menus14` (pictures; The Tour page fits the Deck's screen now), `menuwalk` 13 / 13 + exit 0, `unlock` PASS, `finishcam` STEADY on all
   three courses, `mix` (Big Top within 1 dB of the others), `shimmer` PASS. **Soaks (Normal, 50 AI laps each):** Paradise Plunge 50 / 50,
   median 133.9 s, 4 resets; Coaster Coast 50 / 50, median 116.2 s, 3 resets; Big Top Breakdown 50 / 50, median 104.6 s, 2 resets.
   Release build 17 (0.13.0) published; disk cleaned.

### For Dan to check (build 17)
1. Coaster Coast: race it, then Race again. Is the picture steady on the grid and at the results now? (The cause: the sky and the sun
   were still easing back to sunset while you sat there.)
2. The race caller says / shows "Barnaby" (subtitles, his booth's board at Paradise Plunge's start, the results screen).
3. Big Top Breakdown (Race, Time Trial, or The Tour's Stop 3): the wagon parade on the midway, the Clown Jump (LB on the ramp = fly past
   the Globe; no LB = ride the Globe of Death), once round the ring, into the cannon, through the hoops and out of the top flap.
4. On the river: try the Island rapids (right of the island) against the main channel (left). Worth it, or too rocky?
5. Is it different enough from Coaster Coast, and is ~1:35-1:45 a good length, or should it be longer?
6. The stand-ins: elephants, sea lions, clowns, strongman, tightrope walker, trapeze artists, escape artist. Which ones need real models?
   (Three Tripo prompts are ready in Docs/MODELS-NEEDED.md: elephant, sea lion, the ringmaster Lionel Lustre.)
7. The Tour's Stop 3 scene: Buck reads Lionel's dare. And Vivi's Cannonball Run challenge on The Tour page.

### Round 14 delivery (2026-11-16) — status per item
Evidence: `Docs/Checks/round14/` (reports, pictures). Every run: built exe, `-batchmode -mute`, no window, Below Normal, one heavy job at
a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run; sound was never unmuted.
1. **Title screen + clean menus — DONE.** The game starts on a title screen (logo `Resources/UI/Logo.png` from `Tools/make_logo.py`, the
   theme song, "Press A / Start", the course flying by behind it: `UI/MenuCam.cs`). Main menu: The Tour, Race, Time Trial, 2 Players,
   Garage, Settings, Quit game. Race / Time Trial -> course cards (picture rendered at start-up, name, forms, best lap: `CourseCards`) ->
   choose your racer -> Go. Settings in four groups (Gameplay / Audio / Video / Controls), one setting per row, left / right change it
   (sliders for volumes), A steps it, B back. Pause in a race: Resume, Restart, Settings, Quit to menu. Results: Race again, Next, Menu.
   D-pad, left stick (held = repeat) and keyboard everywhere; either player in split screen. Drawn by `UI/Hud.Menu.cs` (rows 33 px, the
   description 27 px on the Deck's 1280 x 800). *Checks:* `menus14` (18 pictures, every screen's text), `menuwalk` PASS 13 of 13 steps
   (title -> menu -> course -> racer -> race -> pause -> quit to menu -> Music left / left / right 100 -> 90 -> 80 -> 90 %, AI difficulty
   right / left -> quit game, exit code 0). Old menu checks updated (`settings`, `quit`, `quitresults`, `unlock`, `bind`): all PASS.
2. **Crowd much quieter — DONE.** Its own **Crowd** slider (Audio, default 100 %, up to 200 %), and 19 dB under build 15
   (`Settings.CrowdGain` 0.11): murmur beds, cheers, the GO roar, the finish's crowd sound, the near-miss "aww", the loop's "ooh". The
   first try (-11 dB) still left the GO roar / finish cheer over the engine. *Check `mix`* (would-be levels, defaults): Paradise Plunge
   music -26.9 dBFS, engine -32.9, Barker -19.4; crowd murmur on the grid -39.9 (build 15 -20.7), passing a stand -32.6 (-13.4), loudest
   cheer / finish -33.9 (-14.7): 1 dB under the engine, 14.5 under Barker, 7 under the music. Coaster Coast the same within 2 dB.
3. **The Scrambler's sub in a whole glass sphere — DONE.** `Vehicle/BubbleSphere.cs`: a complete ball round the bike and rider (radius
   1.72 m from the bike's mesh extents and the rider's seated box), a chrome ring at its equator, `Resources/Shaders/BubbleGlass.shader`
   (rim, sun and window highlights, sky reflection, a little refraction from the camera's opaque copy, switched on per camera only while
   a bubble is within 80 m). On the change into the sub the ring appears, then the glass grows and closes over the bike (0.45 s). Hull
   collider unchanged. *Check `bubble`* PASS: 49 points (every rider bone, the grips, the bike's mesh extents) all inside, the furthest at
   81 % of the radius (33 cm clear); pictures side / front / three-quarter / back (Garage), chase / side / front / first person underwater,
   five frames of the change.
4. **No shaking after the race — DONE (cause not reproduced by the check).** After the finish no shake of any kind (boost, wall bump, the
   T. rex), a person's car rolls to a stop by itself (results buttons drive nothing), and the camera eases out and slowly circles the car
   looking at a smoothed point, in every view (first person / front included). *Check `finishcam`* (20 s after the line, both courses,
   chase + first person): STEADY in all four, at most 0.04 cm off a smooth path. Build 15's camera (`-oldFinish`) measured nearly steady
   too in the scripted finish (only the boost shake, 1.3 cm, in the first 0.2 s), so the exact shake Dan saw was not reproduced; the
   likely sources (boost shake while A is held, wall / rival bumps, the car still driven by the stick / keys behind the results, a head
   view following Abi's cheering head) are all gone now.
5. **The Ferris wheel on the main route — DONE.** The Midway Jump launches every car into the plane at its lip; the hubless wheel stands
   over the boardwalk dead ahead (70 m tall), the line goes through it 6 m under its centre, on through three firework rings (bursting),
   down to the wheels-down zone before the coaster station. The Midway choice and its shortcut are gone (Coaster Coast's choice is the
   Lighthouse rocks). Choice jumps (Lake Jump, Cliff Gap): "LB: FLY" appears on the ramp, LB there arms the wings ("WINGS READY", out at
   the lip), and the window runs 1.5 s into the air. *Check `wheel`:* human-like pilot through the wheel 10 of 10 (5-17 m from its centre),
   clean to G1 10 of 10; the AI 6 of 6; LB on the ramp and LB 1.2 s into the air work on both choice jumps (4 of 4). `-oldWheel`,
   `-oldChoice` keep build 15's.
6. **Regression + soaks + release — DONE.** Regression (built exe): `round2` (lap 124.6 s, results, restart, skips, Abi) PASS; `round3`
   settings / invert / races PASS (its per-shortcut legs start from the wrong anchor for the Lake / Cliff flights and the bypass: "not
   taken", as before; the full-lap run takes all four); `quit`, `quitresults` exit 0; `cclap` PASS 106.4 s, 0 resets, Drive->Fly at the
   jump and Fly->Drive before the station; `ccrails` 6 of 6; `unlock` PASS; `humanfly` Lake flight 10 of 10, car line 5 of 5; `round6`
   (split, binding, views) PASS; `round11` (Buck, The Tour, racers 16 of 16) PASS; `choice13` PASS; `challenges` 4 of 4 (its Barker-line count fixed: the caller
   clears its list at each start); `steering` PASS. **Soaks (Normal, 50 AI laps each):** Paradise Plunge 50 of 50, median 134.3 s, 5
   resets in 50 laps (water outside the landing corridor 2, fell off the climb 2, out of bounds 1); Coaster Coast 50 of 50, median
   116.6 s, 1 reset (water outside a landing corridor).
   Release build 16 (0.12.0) published; disk cleaned.

### For Dan to check (build 16)
1. Start the game: a title screen (logo, theme song) -> A -> the main menu. Is it readable on the Deck, and the layout clean?
2. Settings: left / right change values (volumes as sliders, options cycle); B goes back. Try Audio > Crowd.
3. Race / Time Trial: the course cards -> choose your racer -> Go. Pause (Start): Resume / Restart / Settings / Quit to menu.
4. The crowd: much quieter (19 dB down). If it is now too quiet, Audio > Crowd goes up to 200 %.
5. The Scrambler in the sub: a whole glass ball round the bike and Abi (chase and first person), and it closing over the bike on the change.
6. After a race: is the screen steady now (chase and first person)? If it still shakes, say what you were pressing and which view.
7. Coaster Coast: everyone flies through the Ferris wheel straight off the Midway Jump, then the firework rings. Does it feel like the
   headline moment?
8. Paradise Plunge's choice jumps: "LB: FLY" now shows on the ramp; pressing LB there should fly you off the lip.

### Round 13 delivery (2026-11-16) — status per item
Evidence: `Docs/Checks/round13/` (reports, pictures, `challenge_lines.md`). Every run: built exe, `-batchmode -mute`, no window, Below Normal,
one heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run; sound was never unmuted.
**Dan's playtest logs:** still not on this PC (they are on the Deck), so item 1's causes were recorded with a new `trace` check and 5c's
stuck places with a wander sweep.
1. **Known resets — DONE (2 of 3 fixed; the third not reproduced).** Recorded with `trace` (a 4 s per-racer buffer dumped at each reset):
   - *Coaster Coast station after the Midway flight:* the car came down at 75 m/s forward, 16 m/s down; the touch on the boardwalk (and the
     plaza box 5 mm under it) took the forward speed and left 5 m/s sideways; the landing's speed hold then scaled that to 74 m/s sideways,
     90 deg off the route, into the sea's out-of-bounds. Fix: the hold keeps the way the car came down unless a wall turned it (jump landings
     too), and a wheels-down car touches down at no more than 8 m/s down. *Soak (50 laps, Normal):* **0 resets there** (Round 12: 8).
   - *Paradise Plunge climb (x 80, z 1120):* off the Lake flight's wheels-down the car came down 15-20 deg right of the road at 70 m/s, bounced
     (17 m/s down), rode up onto the 1.2 m guard wall's top and flew off the ground's end. Fix: after a plane's wheels-down the car is turned
     onto the route (up to 40 deg, 45 deg/s) and touches down softly; the guard wall is 2.6 m. *Soak:* **0 resets in 50 laps** (Round 12: 6).
   - *Coaster Coast car line after the Midway Jump (x 7924, z 408):* not reproduced: 40 forced car-line laps with build 14's landing
     (`-oldLanding`) 0 resets, 40 with the fix 0 (Round 12: once in 50). The same landing hold was the likely cause; it is fixed.
   - New in the final Coaster Coast soak: 3 resets, two AI cars pinned against each other on the south plaza just after G9, pressing Reset
     (63 m off the line). Not reproduced: a 52-lap `trace` afterwards had 0 resets. Open (For Dan: none; watched next round).
2. **Cliff flight (second choice jump) — DONE.** The Cliff Gap on the climb: LB / E off its lip flies over the hillside, round the outside of
   corner2, three rings, into the Sky Arch flight (stands in for G2); the car keeps the climb road and the ramp. Round 12 rules from the
   start. *Checks:* AI lip -> past the rejoin car 13.95 s / fly 12.45 s (fly 1.5 s faster; the Lake flight is 1.1 s faster); human-like pilot
   **10 of 10** on the fly line (no penalty); branch failures (no steering, hard left / right, too high / low, reset button) **6 of 6**; soak
   50 laps: Cliff flight taken 26 times, 0 resets. The
   human-like car line made 3 of 5: that pilot never brakes and leaves the climb at corner2 at full throttle; build 14's climb
   (`-noCliffFlight`) gives it 2 of 5, so not new.
3. **Mangrove bypass — DONE.** A dive bay cut into the left bank before the S-bend (root posts along its river edge, so a boat drifting wide
   never dives by accident: the first try had the lane in the river and plain AI boats dived into it), a root cavern under the river bed
   (hanging roots, glowing fungus, schools of glowing fish, a sunken log arch, a 7 m/s current), up through the bed after bend4 and a
   surfacing zone before G5; its own anchors (a reset in it restarts in it). *Checks:* boat 18.90 s / sub 18.10 s; transitions clean (one
   Boat->Dive, one Dive->Boat) **3 of 3**; failures (no steering, hard left / right, climb / dive hard, reset button, entry missed) **7 of 7**;
   soak: taken 5 times (Vivi, Abi's AI), 0 resets.
4. **Rival challenges — DONE.** The Tour page: Rae's Waterfall Dash (Paradise Plunge: Coral Vault -> G9) and Chip's Coaster Showdown (Coaster
   Coast: station -> top hat), open once the stop is cleared top 3 (or with "Unlock all Tour stops"). Buck's word, the rival's taunt, 3-2-1,
   first through the gate wins; Barker, the rival and Buck react; a first win gives a Garage paint (Rocket Red, Showtime Blue). 13 new
   ElevenLabs lines (Rae 3, Chip 3, Buck 3, Barker 4); speech-to-text: 13 of 13 match >= 0.83 (`challenge_lines.md`). *Check (`challenges`):*
   each won and lost **4 of 4**: result, tour.json saved and read back, all three voices; the Garage paint row recolours the machine.
5. **Graphics presets — DONE (Deck numbers are Dan's).** Menu > Settings > Graphics: Auto / Deck / Low / Medium / High (Auto = Deck when
   Steam reports a Deck). Render scale (FSR below 1), shadows, far clip, detail range, dino range, crowd density, texture mip, LOD bias; two
   players one lighter. *PC (GTX 1660 Ti, batch, CPU-bound: render scale barely shows here), whole frame mean / 95th:* 1 player Paradise
   Plunge Deck 9.9 / 14.5 ms, High 10.7 / 16.1; Coaster Coast Deck 8.0 / 13.2, High 6.7 / 10.5; 2 players Paradise Plunge Deck 17.6 / 25.3,
   High 16.9 / 27.6; Coaster Coast Deck 11.7 / 21.0, High 10.7 / 16.0. Triangles Deck vs High: -14 % (Paradise Plunge), about equal (CC).
5c. **Coaster Coast under the boardwalk / pier — DONE.** Found by sweep: the plazas are decks on legs with open water under them and the
   legs had no colliders, so a boat leaving the flume's channel under the boardwalk drifted in under a 250 m deck; under the fishing pier
   the anchors were up to 280 m apart, so a lost sub was reset 300-440 m back. Now: plank skirts round every plaza edge (nothing goes under
   a deck), lanterns along the channel, a lit channel between the pier legs (guide lights and a seabed stripe), the pool's gold exit frame
   and a light column, clearer water (sight ~100 m, was 73), currents back toward the dive route, anchors every 60-80 m, and lost there
   (6 s without progress) / stuck / out of bounds / the reset button go forward to the next anchor (never past an unearned gate). THIS WAY
   and the arrow were already there. *Check (`underpier`, 90 wander-offs left / right 50 and 90 deg, down, up, then by the HUD):* **89 of
   90** back on the route or recovered close by (one 84 m back, one anchor), 0 stuck (build 14: 60 of 90, 30 reset back up to 340 m);
   human-like pilot through **6 of 6** (build 14: 5 of 6); AI soak 50 laps: 0 resets under the pier.
6. **Regression, soaks, release — DONE.** PASS: round2 (lap 124.58 s), choice, cclap (107.82 s, 8 / 8 set-pieces), ccsplits, round11 (Buck,
   Tour, 16 / 16 racers), humanfly (Lake 10 / 10, Midway 10 / 10), branches (19 / 19), nearmiss, starts (25, 0 resets), settings, quit
   (exit 0), round10 (strand 0 of 39), round6 (bind, split). Soaks 50 laps Normal: **Paradise Plunge 50 / 50, 0 resets**, median 133.9 s;
   **Coaster Coast 50 / 50**, median 117.0 s, 3 resets (the pinned AI cars, item 1). UTF-8 kept (no BOM).

#### For Dan to check (Round 13)
- Paradise Plunge: LB off the Cliff Gap (on the climb): does the hillside flight feel good, and is it a fair choice against the climb road?
- The Mangrove bypass: the violet-buoyed bay on the left after the lagoon: dive, the root cavern, up before G5. Easy to find? Worth it?
- The Tour page: the two rival challenges (lose one, win one): do the lines land? The paint in the Garage (Paint row).
- Menu > Settings > Graphics: on the Deck it should say "Auto (Deck, Steam Deck)". Frame rate on both courses, and with 2 players.
- Coaster Coast: under the boardwalk (after the flume) and under the fishing pier: can you still get lost? Please copy the Deck's
  playtest logs to `Docs/Playtest/` (Round 12's note) so the next round can read where you got stuck.
- For a release: "Unlock all Tour stops" (and so the challenges) is still On by default (`Settings.UnlockTourDefault`).

- **Round 12 — Coaster Coast unlocked, flight-branch fixes** (2026-11-15, release `build-14` / 0.10.1). Status below.

### Round 12 delivery (2026-11-15) — status per item
Evidence: `Docs/Checks/round12/` (reports, pictures). Every run: built exe, `-batchmode -mute`, no window, Below Normal, one heavy job at a
time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run; sound was never unmuted.
**Dan's playtest logs:** none on this PC (`%USERPROFILE%\AppData\LocalLow\ShiftStorm\ShiftStorm` has no `playtest` folder, and no copy
anywhere else). They are on the Deck, inside Proton's prefix (see For Dan). So the causes were found in the code and reproduced with
scripted runs of build 13's behaviour (`-oldFlight`):
- Lake flight: build 13's line came back to the road in an S-bend of ~60 m radius; the plane turns at 150 m (75 m/s) to 200 m (92 m/s) at
  full bank. A human-like pilot on build 13: 7 of 10 made it, only 2 without a +3 s miss of the exit frame.
- "Reset back too far, repeat": on build 13 a reset during the Lake flight (the reset button, or a crash early in it) went to the anchor
  89 m BEFORE the Lake Jump's lip, so you jumped and flew it again (reproduced: the reset button -> 2 resets behind the jump). The Sky Arch
  slit: the reset button on it went 236 m back, behind the branch.
- The HUD arrow followed any shortcut line within its width: it steered cars in the Lake Jump's canyon toward the flight line (the human
  car line took 7.1 s on build 13, 5.4 s now) and planes after the launch toward the Sky Arch slit, 190 m off the route.
- Coaster Coast: a plane that missed the wheels-down zone was told TURN BACK at G1 (a car-only gate) and circled it for good (a zone changes
  the form once, so turning back could never earn it). A car down from the Midway flight crossed the station's rail entry still a few
  metres up, missed the rail and was reset "out of bounds" (Round 10-11's known reset after the Midway flight).
1. **Every course open — DONE.** Race, Time Trial and 2 Players never lock; the choose-your-racer page shows a Course row too. Menu >
   Settings "Unlock all Tour stops" (default On; for the release set `Settings.UnlockTourDefault = false`: a player who never touched the
   setting then plays the Tour in order). *Check (`unlock`, fresh settings.json / tour.json):* Coaster Coast started from Race, Time Trial
   and 2 Players: PASS; the Tour's Stop 2 open with the setting On, "locked: top 3 at Stop 1" with it Off: PASS.
2. **Lake flight with room — DONE.** Out 22 m and 28 m up over the canyon's west wall (not round it), every bend >= 250 m radius, the
   rejoin / wheels-down 30 m further on (G1 - 45), the branch entered as the wings open, 60 x 40 m entry and exit frames (the exit takes a
   car too), rings radius 16, a 70 m wide wheels-down zone, a light steering assist toward the line (people only, up to 14 deg/s yaw and
   10 deg/s pitch, less while steering hard). The same frames / zone / assist on Coaster Coast's Midway flight. *Check (`humanfly`: steers
   only by the HUD arrow, 0.25-0.45 s late, keys or a noisy stick, LB 0.2-0.7 s into the window, no boost, 45-75 m/s, +-5 m, +-5 deg):*
   Lake flight **10 of 10** (9 with no penalty; build 13: 7 / 2), Midway flight **10 of 10** (9; build 13: 8 / 5). Lip -> G1: Lake fly
   4.32 s / car 5.43 s (human), 4.43 / 5.78 s (AI pilot; Rounds 8-11: 4.65 / 5.78): the fly line stays ~1.1-1.3 s faster, as before.
3. **Flight branches never send you back — DONE.** A reset on (or within 8 s of leaving) a flight branch goes forward to the anchor before
   its rejoin, +3 s ("BACK ON COURSE +3 s"; the slit counts G3); in the air elsewhere a reset restarts on the flight line at most 200 m
   back; a gate crossed in a form it does not take resets at once to the anchor before it; the arrow follows a shortcut only once entered;
   new anchors at both rejoins, in the lagoon corridor and in long same-form gaps (climb, Mangrove, Coral Vault); the coaster rail's entry
   catches a car up to 9 m over the track. *Check (`branches`, 19 scripted failures: no steering, hard left / right, too high / low, the
   exit missed, the reset button on each of the Lake / Midway flights; on the slit: into the buttress, veered out, G3 passed wide, the
   reset button, HUD only):* **19 of 19** end ahead or at most one anchor back, 0 behind the jump, 0 loops, the lap goes on (build 13: 17 of
   19, both reset-button cases behind the jump / branch). *Reset audit* (`resetaudit`, every 20 m): Paradise Plunge worst 298 m back (the
   promenade; build 13: 1,149 m in the Sky Arch / lagoon flight); 6 places over ~6 s, all at the end of the Coral Vault (228-268 m).
   Coaster Coast: the long ones are on the coaster rails, where no reset happens; elsewhere 380-434 m (boardwalk, flume; not changed).
4. **UTF-8 — DONE.** TODO.md / CLAUDE.md read and written as UTF-8 without a BOM (checked: no BOM, no mojibake apart from Dan's quoted
   example).
5. **Regression, soaks, release — DONE.** PASS: round2 (lap 124.58 s, skips, Abi), choice, cclap (107.8 s, 8 / 8 set-pieces), ccsplits,
   round11 (Buck, Tour, 16 / 16 racers), nearmiss, starts (25, 0 resets), settings, quit (exit 0), round10 (strand 0 of 36, strip, E1-E6,
   slow fps), round6 (bind, split). Fixed on the way: the finish-out-of-order check's own switch (the new wrong-form reset now honours it);
   E4 failed once on timing and passed on the rerun. Soaks (Normal, 50 laps, time scale 3): **Paradise Plunge 50 / 50**, 0 bad gate
   sequences, median 133.6 s, 6 resets (all "fell off the climb" at x 80, z 1120; Round 11: 3); **Coaster Coast 50 / 50**, 0 bad gate sequences, median 117.0 s, 8 resets, all at the station after the Midway
   flight (x 8080, z 760), all on Abi's car driven by the soak's AI (Round 11: 19; the human-like pilot no longer meets it: reduced, not gone).

#### For Dan to check (Round 12)
- Menu > Race / Time Trial / 2 Players: Coaster Coast straight away (the Course row on the menu and on the choose-your-racer page).
- The Lake Jump's flight: LB off the lip and follow the arrow; does it feel roomy now? Is the steering assist noticeable / too much?
- Miss on purpose (fly wide, too high, press Y on the flight): you should be put on the road just before G1 with "BACK ON COURSE +3 s",
  never back before the jump. The same on Coaster Coast's Midway flight and the Sky Arch slit.
- Please copy the Deck's playtest logs so they can be read next round: Menu > Settings "Open log folder" shows the path (Proton:
  `~/.local/share/Steam/steamapps/compatdata/<id>/pfx/drive_c/users/steamuser/AppData/LocalLow/ShiftStorm/ShiftStorm/playtest`); put the
  `session-*.jsonl` files in `Docs/Playtest/` on the PC.
- For a release: "Unlock all Tour stops" is On by default while we build (`Settings.UnlockTourDefault`).
- Known: the soak's AI in Abi's car is still reset at Coaster Coast's station after the Midway flight (8 in 50 laps; Round 11: 19).
- Known: Paradise Plunge's AI falls off the climb about 1 lap in 8 (x 80, z 1120); on Coaster Coast a car on the car line was once reset just
  after the Midway Jump's landing (x 7924, z 408).
- **Round 11 — Buck arrives, The Tour, choose your racer** (2026-11-15, release `build-13` / 0.10.0).
- **Round 10 — no stranding walls, test tracks out, Coaster Coast** (2026-11-15, release `build-12` / 0.9.0).
- **Round 9 — sensible steering, racing lines, one finish sound, the crowd, three new designs** (2026-11-15, release `build-11` / 0.8.2).
- **Round 8b — dinosaurs you can actually see** (2026-11-14, release `build-10` / 0.8.1). Status below.

### Round 11 delivery (2026-11-15) — status per item
Evidence: `Docs/Checks/round11/` (reports, buck_lines.md, pictures). Every run: built exe, `-batchmode -mute`, no window, Below Normal,
one heavy job at a time; measured audio output peak 0.000000 (AudioListener.volume 0) in every run. No sound check needed unmuting.
1. **Buck's voice — DONE.** 53 lines in the ElevenLabs voice "ShiftStorm Buck" (`Tools/Audio/buck_lines.py`, eleven_v3, 2,602 credits),
   all at -16 LUFS; local speech-to-text in `Docs/Checks/round11/buck_lines.md`: 52 match, 1 flagged (tour_s1_retry_1, 0.73: Whisper
   wrote "alright" / "await" for "all right" / "will wait"; the audio says the line). Barker: 4 new Abi lines (passabi, byabi; 101 credits).
2. **Briefing card, results remark, setting, split screen — DONE.** Card with a placeholder portrait, name and subtitles; time stopped
   under it; A / Enter skips; once per course per session; Settings "Buck's briefings" On / Short / Off; his remark after Barker's results
   line (races only). `buck` check: both courses PASS, 0 frames with two voices at once (45,398 + 61,530 voiced frames), skip PASS (card
   gone at once, Barker's start next), once PASS, Short / Off PASS. Split screen: one Buck, card on both halves.
3. **The Tour skeleton — DONE.** Menu > The Tour: Stop 1 The Trial (Paradise Plunge), Stop 2 Opening Night (Coaster Coast); scene (Buck
   over a camera flight, skippable) -> race -> Buck's reaction; 10 / 6 / 4 / 2 points, top 3 unlocks the next stop, best kept on a retry,
   `tour.json`. `tour` check PASS: both stops, a skipped scene, reload, a 4th-place retry (best 1st kept), Race again.
4. **Choose your racer — DONE.** Driver (Abi, Rae, Chip, Vivi) x machine (Jetstream, Grand Prix, Hot Rod, Scrambler) before Race, Time
   Trial and a Tour stop; the other three race as the AI in their own machines; saved; 2 Players picks per player. `racers` check: 16 of
   16 combinations PASS (hands on the controls), split pick PASS.
5. **Buck's model prompts + Mixamo list — DONE.** `Docs/MODELS-NEEDED.md`, Round 11: Human / Bear / Dog image prompts, Tripo settings,
   file names, 7 Mixamo clips. The placeholder card stays until his model is in.
6. **Regression, soaks, release — DONE.** PASS: round11, round2 (lap 124.6 s, skips rejected, Abi), cclap (107.8 s, 8 / 8 set-pieces),
   ccsplits, settings, places, barker (0 overlaps), round6 (bind, split, views), garage, starts (25, 0 resets), quit (exit 0).
   Soaks (Normal, 50 laps each): Paradise Plunge 50 / 50, 3 resets, median 134.6 s; Coaster Coast 50 / 50, 19 resets, all at the known
   spot after the Midway flight (x 8080, z 760; Round 10's known issue, unchanged).

#### For Dan to check (Round 11)
- Buck's voice and lines: does the "ShiftStorm Buck" voice sound right? (`--redo id` re-records any line.)
- The briefing card: size, placement, the placeholder portrait; is the briefing too long before a race (12-15 s; Short is ~4 s)?
- Buck's briefings On / Short / Off in Settings, and the skip (A / Enter).
- The Tour: both stops, the scenes over the course flight, the points and unlock, the retry message.
- Choose your racer: the page, each driver in each machine, the 2-player picks.
- Pick one of Buck's three looks (Human / Bear / Dog prompts in `Docs/MODELS-NEEDED.md`), then the Tripo model.
- The Tour's line on the results screen is long and runs into the course name at the top right (cosmetic; shorten next round).
- Known: about 1 AI lap in 3 on Coaster Coast is reset just after the Midway flight (from Round 10, not fixed yet).

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

## Current Objective — Round 17: the fun round (power-ups, stunts, big route splits, laps) — Paradise Plunge first

**Why (Dan, 2026-11-17):** the game wasn't clicking — "a great idea on paper" but something missing. Agreed with the planning
chat: the racing needs things to *play with*, and the choices need to matter. Dan's words:
- Power-ups: "I wouldn't mind power ups / weapons Mario Kart style but I don't want them to be golf based ... I would like to be
  a little more creative with them."
- Choices: "when we have the options they seem to be over in a couple of seconds so kind of pointless. I would want them to be
  a complete different experience. This will make the tracks more challenging and varied to keep you wanting to try again."
- Laps: "the laps being one lap doesn't really give you a chance to savor the courses."
- Stunts, not drifting: "I would want to lean more towards tricks. Maybe this can give us the opportunity to build up points so
  a track isn't just about being first but also who can build up stunt points."
- The auto transforms stay.
**Order:** the systems (items, stunts, laps) for every course, but the **full treatment (big route splits, retuned lap) on
Paradise Plunge first** so Dan can feel it; the other three courses' big splits come next round. Run unattended; don't stop to
ask; no visible window. Same testing rules. Read-only references as before.

1. **Power-ups (item boxes), creative, not golf.** Rows of item boxes on every course (in every form: floating on water,
   in the air on flight lines, in the cave). **B** uses the item (free during races; update Controls). One item held at a time
   (Bubble Shield / Slick can be held behind you). Every item works in **all four forms**, adapting to each. The set (Dan
   approved; names can change):
   - **Wrong Form** — zaps the racer ahead into the wrong form for ~2 s (a plane becomes a boat mid-air and belly-flops; a
     car lands on water and bobs). Never strands or resets them; it's comic and costs time.
   - **Rainbow Bridge** — throws a glowing temporary bridge ahead: stay a car across water / a gap, or open a short shortcut
     only you can take (only where the course allows; never skips required gates).
   - **Bubble Trap** — wraps a rival in a giant bubble that floats them up off their line until it pops.
   - **Swap Spot** — trade places with the racer just ahead (confetti puff); never into a wall / wrong form.
   - **Mirage** — three fake copies of you behind you that soak up attacks and confuse the AI.
   - **Rewind** — undo your last 3 s (after a crash or a missed ring).
   - **Pocket Storm** — your own rain cloud drifts ahead and soaks the racers in front: they slip on the road, the water
     chops, the air buffets.
   - **Hype Horn** — the crowd goes wild: your stunts score double for 10 s.
   - **One themed item per course:** Paradise Plunge — a **dino egg** that hatches a baby raptor to chase the racer ahead;
     Coaster Coast — a **firework rocket**; Big Top Breakdown — a **human cannonball**; Dam Shame — a **mini flood wave**.
   - **Buck's Big Finish** (very rare, only for the back of the pack) — Buck's stunt cannon blasts you up the field; the crowd
     roars.
   Fair and fun: items by position (better ones further back), no item stun-locks (a short grace after a hit), clear warnings
   for incoming items, readable effects. The AI picks up and uses items sensibly. Barnaby calls hits; drivers react (their
   mess-up yells). Settings: **Items On / Off** (default On); Time Trial and the Tour's time challenges: off. Simple 2D icons
   for the HUD (no Tripo).
   *Check:* every item used in every form (pictures), hits / blocks / misses logged; AI races with items: 0 stuck, no resets
   caused by items; item odds by position logged over 50 races.
2. **Stunts and stunt points.** In the air in any form, the **right stick** does tricks (it still looks around on the ground):
   car — flips and spins; plane — barrel rolls and loops; boat — wave hops and spins; sub — breaching jumps out of the water
   (new: a sub at speed near the surface can breach). Land clean = **boost + stunt points**; land badly = a wobble and the
   points lost. **Combos** chain tricks; a trick **during a transformation** is a big "Shift Trick" bonus. Score shown on the
   HUD (combo meter, points popping up), Barnaby calls combos, the crowd cheers, **Buck judges** (his lines: he was a stunt
   performer — he loves a good landing).
   - **Every race shows place and stunt score** on the results.
   - **Stunt Show mode** (menu, alongside Race / Time Trial): same courses, won on **stunt points** (position gives a small
     bonus); items On.
   - The AI does tricks too (by difficulty).
   *Check:* every trick in every form lands and fails correctly (pictures); a Shift Trick scored; a Stunt Show race won on
   points; the human-like pilot can land basic tricks.
3. **Multiple laps on every course.** Race lap count chosen per race (**1–5, default 3**) on the course / racer screen; the
   Tour and Stunt Show use 3. Trim **Paradise Plunge's** lap so a 3-lap race is about **4–5 minutes** (now ~2 min a lap); the
   other courses keep their length this round (say their 3-lap times). Dam Shame stays at 3 laps (its water). Lap counter,
   final lap flash, best lap — as Dam Shame already has.
4. **Paradise Plunge: big route splits that are completely different experiences.** One or two **major splits**, each
   lasting a **large part of the lap (roughly a quarter to a third)**, where the other way is a different form, different
   scenery and different hazards, about equal in time, different risk — so you want to try the other way next lap. E.g. the
   Lake Jump's fly line becomes a **long valley flight past the dinosaur herds**, while the car line becomes a **jungle canyon
   run**; a long **underwater** alternative through the cave system vs. a surface river route. Fold the short choices
   (Lake flight, Cliff flight, Mangrove bypass) into these or make them meaningful. Items and stunt spots on both sides
   (big airtime on the jumps, breaching spots for subs). Round 12 / 14 rules (room for every exit, forward recovery, LB on
   the ramp). AI uses both sides.
   *Check:* each split timed both ways, pictures along both, human-like pilot both ways, AI 25 laps, 0 stuck.
5. **Regression (all four courses, items On and Off) + AI soaks, wrap-up, release build 20 (0.15.0).** Per-item status, update
   `CLAUDE.md` (Controls: B item, right stick tricks in the air; modes; settings), commit, push, publish, clean the disk.
   **End the chat report with the "For Dan to check" list itself.** UTF-8 docs.

### Defaults (unchanged)
- Publish a release every round. Target hardware: Steam Deck, 60 fps.

### For Dan to check (fill in at end of round)
- …

## Next Steps (Do Not Start Yet)
- **Round 18 (planned): the big route splits on Coaster Coast, Big Top Breakdown and Dam Shame**, like Round 17 did for
  Paradise Plunge, plus each course's lap length tuned for 3 laps. After Dan's feedback on Round 17.
- **More tracks:** Lantern Reef (boat + sub; draft in `TODO-ARCHIVE.md`),
  Clockwork Cascade (car + boat), Cloud Orchard (car + plane). Dan picks;
  new environments, not Tee'd Off's (Dan, 2026-11-15).
- **Playtest itself** (concept's evaluation plan): a few players of mixed
  skill, first attempt + three practice runs, per-mode questions, using the
  Round 8 logs. Dan runs it when he's ready.

## Technical Notes / Blockers
- Unity batch runs need the sandbox off (UPM named pipe). The world is built in code at start-up; there is no hand-made scene content.
- Frame times from batch-mode checks are CPU only; a windowed or on-Deck measurement is needed for real GPU numbers (needs Dan's OK
  for a visible window, or his Deck).
- Round 2's frame-time hitches: mostly audio clips loading on their first play (preloadAudioData was off on every SFX / voice clip);
  fixed in Round 2b (preload on). PC check runs still show a few hitches that move from run to run (1, 12, 23, 46 frames over 50 ms in four
  laps; worst 0.1-1.1 s), so likely this PC (check pictures being written, other processes); if the Deck shows them too: a profiled lap
  (development build). Scenery peaks near 3.9 M triangles after the 1.7x rescale.
