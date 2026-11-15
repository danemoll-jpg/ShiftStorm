# ShiftStorm Reuse Audit

Read-only audit of two reference projects, written before any code was copied.
Paths are relative to each repo root:

- **CR** = `C:\Users\danmo\chastain_rush` (Unity 6000.6.1f1, URP, Input System)
- **TO** = `C:\Users\danmo\Tee-dOff` (Unity 6000.6.1f1, **Built-in pipeline**: no URP package, shaders are `CGPROGRAM`)

Every path named below was checked on disk (2026-11-12).

**Key facts up front**
- Most CR vehicle and race code lives in **namespace `Racer`** (ported from Woodstock Rush). Newer files use `ChastainRush`. Expect to rename on copy.
- CR's vehicle tuning is **C# code** (`VehicleProfile.BuildAll()`), not ScriptableObjects. ShiftStorm's rule says tuning lives in data, so this must change.
- CR's plane **already sits on top of the car**: `PlaneFlight` is added to the same GameObject as `ArcadeVehicle`, which it switches off through `ArcadeVehicle.Flown` and drives itself (in the air) or calls `ArcadeVehicle.Simulate` (on its wheels). This is the closest existing pattern to a shape-changing vehicle.
- CR's vehicle is wired into a big open world (Chastain map, traffic, pipes, stunt park, weather, menus, replays). Most of the coupling is static calls (`GameMenus.Open`, `WorldMap.OwnsInput`, `StuntPark.*`, `WeatherGrip.*`, `RaceController.Instance`) that can be stubbed or removed.
- `CheckRun.cs` has **no `-mute` flag**. It mutes on any `-exe*`, `-batchmode` or `-executeMethod` argument. ShiftStorm needs `-mute` added.

---

## 1. chastain_rush (CR)

### 1.1 Car controller

| File | How it's built | Copy as-is | Needs changes | Doesn't fit one shape-changing vehicle |
|---|---|---|---|---|
| `Assets/Scripts/Vehicle/ArcadeVehicle.cs` (231 lines; one file, no partials) | Rigidbody arcade car: 4 raycast suspension points, spring/damper, drive/brake/coast, lateral grip, yaw torque from a bicycle model, upright torque, air self-levelling, optional air steering. `Simulate(throttle, brake, steer, dt)` is public and input-free. `Flown` flag skips its own step. | Core `Simulate` drive/suspension/grip math; `Flown` hand-off idea; `Live` registry. | Remove `StuntPark` loop/energy carry, `WeatherGrip`, `DrainPipe`/`SurfaceUp`, `HighAssist.Forgiving`, replay `Puppet`, `-r37*` switches. Keep `ShallowWater` immersion only if the Drive mode should slow in shallows. Move tuning fields to a ScriptableObject. | It assumes it is the only motor. In ShiftStorm it becomes the **Drive mode** of one controller, not the owner of the body. |
| `Assets/Scripts/Vehicle/VehicleInput.cs` (108) | Input System actions built in code (no `.inputactions` asset): throttle RT/W, brake LT/S, steer LS/A-D, reset Y/R, view X/V, look RS, air pitch/spin, fist LB/F. Per-device binding for split screen. `Scripted` overrides for checks. | Action-building pattern, deadzones, `Scripted` check hooks, `ConsumeReset/ConsumeView`. | Remove `GameMenus`, `MenuInput`, `TrailerMode`, `WorldMap`, `RiderGestures` calls. Add Fly/Dive axes (pitch, rise/sink) and a boost button. | Car-only action set; ShiftStorm needs one action set that every mode reads. |
| `Assets/Scripts/Vehicle/VehicleConfiguration.cs` (163) + `VehicleProfile.cs` (153) | `Apply(id)` resets the motor from a JSON snapshot, writes profile values into `ArcadeVehicle`, rebuilds the visual (`VehicleVisual.Build`), adds/ends `HelicopterFlight` or `PlaneFlight` by profile class, sets box collider, mass, CCD, registers contacts. Small-vehicle wipeout logic. | The **swap-in-place** sequence (snapshot, re-apply, rebuild visual, re-register colliders, snap camera) is a useful template for a mode change. | Profiles from code to ScriptableObjects; drop paint/police/cup/upgrade/rider code; no visual rebuild per change (the transforming mesh animates instead). | `Apply` resets steering and state; ShiftStorm must **keep** position, forward intent, boost and race progress across a change. |
| `Assets/Scripts/Vehicle/CarRecovery.cs` (286) | Installs `VehicleRespawn.Recover`. In a race: nearest lap point facing race direction, stepped until the box is clear; else last stable spot or nearest road. Separate branches for helicopter and plane. Never into water. | Clear-spot search (overlap box, step along lap and sideways), "short of next checkpoint" rule. | Depends on `RaceController`, `RaceRoute`, `RoadLookup`, `TreeTrunks`, `StuntPark`, `ShallowWater`, `DrainPipe`, `WorldBoot`. Rewrite around ShiftStorm's track spline. | The "never in water" rule is wrong for Boat/Dive segments: the reset spot must match the **mode the segment expects** (surface of the lagoon for Boat, under water for Dive). |
| `Assets/Scripts/Vehicle/VehicleRespawn.cs` (113) | Last stable grounded pose, fall height reset, `Respawned` event, pluggable `Recover` delegate. | Mostly as-is. | Stable-pose test is "wheels down"; needs per-mode "stable" (afloat, flying level, submerged). | — |

### 1.2 Plane controller

| File | How it's built | Copy as-is | Needs changes | Doesn't fit |
|---|---|---|---|---|
| `Assets/Scripts/Vehicle/PlaneFlight.cs` (250) + `.Aerobatics.cs` (128), `.Crash.cs` (121), `.Effects.cs` (141) | Arcade flight on the car's own body: pitch/bank/rudder rates, throttle 0..1, holds height and wings level hands-off, climb costs speed, gentle stall (nose eases down). Take-off past `TakeOffSpeed`; landing under `LandSink` rolls out on wheels; harder is a forgiving crash. Builds its own Input actions. Aerobatics = full roll/loop with a bumper held. Effects = propeller, sound. | Flight feel and constants (`TopSpeed 115`, `Cruise 85`, `StallSpeed 27`, `PitchRate 45`, `RollRate 95`, `MaxBank 70`); hands-off levelling; speed/height trade; `Scripted` hooks. | Strip runway line-up (`RunwayWest/East`, Cobb County data), `WorldMap`, `GameMenus`, `RaceController` air-reset hooks, `HelicopterFlight` checks. Constants into a ScriptableObject. Use the shared input set. | Take-off and landing are **player-driven**; in ShiftStorm the change is triggered by a zone, so Fly must start already airborne with blended speed and land into Drive/Boat by zone, not by touchdown rules. Crash-on-landing rules must not fire during a transform. |
| `Assets/Scripts/Vehicle/GuidedFlight.cs` (107) | Aimed ballistic hop from wherever the car is to a landing point (Hermite curve in plan, gravity in height), let go on any hit or reset. Only needs `ArcadeVehicle`. | As-is (one dependency). | Rename namespace. | Good fit for scripted **transition hops** (car leaps off the cliff into the sky arch, plane drops into the lagoon). |
| `Assets/Scripts/Vehicle/HelicopterFlight*.cs` (4 files) | Hover flight on the same body. | — | — | Reference only, for Dive-mode hover/depth hold. |

### 1.3 Camera

| File | How it's built | Copy as-is | Needs changes | Doesn't fit |
|---|---|---|---|---|
| `Assets/Scripts/Vehicle/ChaseCamera.cs` (98) | Smoothed follow at a profile offset, heading lag, look-ahead, sphere-cast obstruction, flight tilt; keeps its own chase pose so other views can ease back. Deps: `ArcadeVehicle`, `VehicleRespawn`, `DrainPipe`. | Nearly as-is. | Drop `DrainPipe.View`; per-mode offset blended during a change; under-water obstruction mask. | — |
| `Assets/Scripts/Vehicle/CameraViews.cs` (275) + `.Trailer.cs` | Chase / Far chase / First person / Front / Down, cycled with X/V, PlayerPrefs, look-around. Deps: `ChaseCamera`, `VehicleInput`, `VehicleConfiguration`, `HelicopterFlight`, `TrailerMode`, `GameMenus`, `MenuInput`, `WorldMap`, `CheckRun`. | View cycling and look-around. | Strip trailer, helicopter Down view, menus. | — |

### 1.4 Collisions

| File | How it's built | Notes |
|---|---|---|
| `Assets/Scripts/Vehicle/VehicleContact.cs` (37) | `Physics.ContactModifyEvent`: car vs small vehicle mass scaling, zero bounce. Immutable dictionary snapshot for the physics thread. | **Copy as-is** for racer-vs-racer contacts (no deps). |
| `Assets/Scripts/Vehicle/VehicleSurfaceContacts.cs` (292) | Replaces mesh internal-edge normals on registered ramps so ramps never slow the car. Registers surfaces by name (`sc...`). | Copy, change the naming rule to ShiftStorm's ramps. Needed for transition ramps. |
| `Assets/Scripts/Vehicle/WallGlance.cs` (157), `RampHold.cs` (114), `Race/LandingZone.cs` (62) | Glancing wall hits keep speed; ramp landings keep speed; landing zones. | Adapt; Drive mode only. |
| `Assets/Scripts/Vehicle/ShallowWater.cs` (187) | Water areas (box/round/outline) in 64 m cells; `Sample` gives immersion and surface Y; also holds `WaterFeedback` (splash). Deps: `ArcadeVehicle`, `SoundMix`. | **Adapt**: its surface query is the natural basis for Boat buoyancy and Dive depth (where is the water surface here?). |

### 1.5 Race progress (`Assets/Scripts/Race/`)

| File | How it's built | Copy / change / doesn't fit |
|---|---|---|
| `RaceController.cs` (1399) + `.Air.cs`, `.Twin.cs` | One big controller: 3 laps, ordered checkpoints, 5 s miss penalty, official shortcuts, rolling start, AI field, records, ghosts, barriers. `.Air` = ring race; `.Twin` = "Wings & Wheels" (car lap and plane lap of one race). Deps: `MapBuilder`, `RaceBarriers`, `RaceMarkers`, `RoadDriver`, `CleanLapGhost`, `SplitScreen`, `TrailerMode`, `WorldMap`, `GameMenus` and more. | Too entangled to copy. **Read and rewrite** a small ShiftStorm race controller with the same rules. |
| `RaceRoute.cs` (522): `RouteJson`, `RaceGate` | Route data in `Data/routes/*.json`, made by `Tools/mapgen/make_route.py`, heights from the Chastain terrain. `RaceGate.Crossed(from, to, reach, out fraction)` = directed plane test with half width, Below/Above, ring/frame mode, `EitherWay`, `MaxSpeed`. | **Copy `RaceGate`** (it handles ground gates and sky rings, and fits underwater gates too). Drop the route loader (Chastain map data). |
| `RaceEntrant.cs` (58) | Per-racer progress: next gate, missed gates, laps, own clock, teleport flag. | Copy with small changes: progress must not reset when the mode changes. |
| `RaceMarkers.cs` (328), `AirGuideArrow.cs` (101) | Runtime arches/beams; 3D arrow to the next ring drawn over everything. | Adapt (arrow is useful in Fly and Dive). |
| `RaceStuckReset.cs` (51) | Wedged detection then reset prompt. | Adapt per mode. |
| `RoadDriver.cs` (858), `PlanePilot.cs` (266) | AI driver on a road spline; AI pilot flying the ring line through the plane's own `Scripted` controls. | Later rounds. Same "AI uses the player's controls" rule is worth keeping. |
| `CleanLapGhost.cs` (153) | Best clean lap ghost. | Later. |

Transformation **triggers do not exist** in CR; ShiftStorm builds them new (separate from gates, per CLAUDE.md).

### 1.6 Check runs, launcher bridge, Steam Deck

| File | How it's built | Copy / change |
|---|---|---|
| `Assets/Scripts/CheckRun.cs` (50) | Static, `SubsystemRegistration`: active on `-exe*`, `-batchmode`, `-executeMethod`; picks an output folder; sets `AudioListener.volume = 0` before the first scene; `runInBackground = true`; `Hear(bool)` for a sound step. No deps. | Copy; **add `-mute`**, rename temp folder to `ShiftStormCheck`, log the measured output level. |
| `Assets/Scripts/LauncherBridge.cs` (76) | Reads `-racerManagedMusic`, `-racerPersonalMusic`, `-racerLauncherReady`, `-racerLauncherNonce`; once `GameShell` runs a few frames, writes the nonce to the ready file. `ReplaceOldLauncher` swaps a known "launcher 1" exe. Deps: `WorldBoot.Defer`, `GameShell`. | Copy; wait on ShiftStorm's own boot object instead of `GameShell`; drop `WorldBoot.Defer` and **drop `ReplaceOldLauncher`** (no old launcher exists). Keep the `-racer*` argument names only if the copied launcher keeps them. |
| `Assets/Scripts/SteamDeck.cs` (40) | Detects 1280x800 or the Deck GPU ("Custom GPU 0405", "Van Gogh", "Sephiroth"); `-steamDeck 0/1` override; scales view/traffic/people to 60 %. Deps: `Racer.GameSettings`, `CheckRun`. | Copy detection and override; replace scales with ShiftStorm's quality knobs. |
| Launcher: `Launcher/core.cpp`, `core.hpp`, `launcher.cpp`, `store.cpp`, `core_cli.cpp`, `launcher.rc`, `ChastainRush.ico`, `vendor/` (json.hpp, miniz.c/.h + licenses) | Native launcher. | Copy per CLAUDE.md. **Do not copy `Launcher/pinned-key.json` or `pinned_key.hpp`**: they pin Chastain Rush's key; ShiftStorm makes its own with `New-LauncherKey.py`. |
| `Tools/Build-Launcher.ps1`, `New-LauncherKey.py`, `Prepare-LauncherRelease.py`, `Prepare-ComponentUpdate.py`, `Package-LauncherStarter.py`, `Publish-LauncherRelease.py`; `Docs/Starter/README-LAUNCHER.md` | Build, sign, package, publish. | Copy and rename. Note: CR has no `Builds/LauncherSDK`; the SDK comes from Racer's, as CLAUDE.md says. |
| `Tools/Blender/plane.py`, `cars.py`, `kit.py`, `render_vehicle.py`, `contact_sheet.py` | Scripted hard-surface vehicle models and preview renders. | Reference for the scripted transforming vehicle and its preview sheets. |

---

## 2. Tee-dOff (TO)

Pipeline note: TO is **Built-in RP**. Every TO shader needs a URP port before use. Models are FBX with a baked base-color PNG, imported as **Humanoid** (`animationType: 3` checked on Chip, Rae, Vivi, Christina, Marcus). Sources were made in **Meshy** (`Art/Characters/incoming/<Name>/<Name>_meshy.fbx/.glb` + task JSON), rigged and animated through **Mixamo** (clip FBXs beside each model).

### 2.1 Characters (golfers and hosts)

Folder pattern: model `Assets/_Project/Art/Characters/<Name>/<Name>.fbx` + `<Name>_basecolor.png`; profile `Assets/_Project/Resources/Characters/<id>/<id>.asset`; definitions in `Assets/_Project/Scripts/Characters/Cast.cs`; stats in `Assets/_Project/Resources/Characters/roster.json`.

| Character (id) | Files | Mixamo clips present | Mark |
|---|---|---|---|
| Chip "Showtime" Sterling (`chip`) | `Art/Characters/Chip/Chip.fbx` | (shared clips) | **Reuse directly** as a driver model; seat pose is new. |
| Rocket Rae (`rae`) | `Art/Characters/Rae/Rae.fbx` | `Mutant Flexing Muscles.fbx` | Reuse directly. |
| Vivi (`vivi`) | `Art/Characters/Vivi/Vivi.fbx` | `Victory (1).fbx`, `Salsa Dancing.fbx`, `Samba Dancing.fbx`, `Dying.fbx` | Reuse directly (podium clips ready). |
| Penny Pickett (`penny`) | `Art/Characters/Penny/Penny.fbx` | `Bashful.fbx`, `Terrified.fbx` | Reuse directly. |
| Marcus "Motormouth" Miles (`marcus`) | `Art/Characters/Marcus/Marcus.fbx` | `Laughing.fbx`, `Standing Taunt Chest Thump.fbx`, `Strut Walking.fbx`, `Throw.fbx` | Reuse directly. |
| Christina (`christina`) | `Art/Characters/Christina/Christina.fbx` | `Hip Hop Dancing.fbx` | Reuse directly. |
| Otis Oakley (`otis`) | `Art/Characters/Otis/Otis.fbx` | — | Reuse directly. |
| Ollie the Octopus (`ollie`, Lost City host) | `Art/Characters/Atlantis/octopus/`; concept `Art/Characters/concepts/atlantis/Octopus.png` | non-Humanoid | Adapt (coral vault cameo / marshal). |
| Professor Pith (`pith`, jungle host) | `Art/Characters/Pith/Pith.fbx` | — | Adapt (mangrove / waterfall cameo). |
| Others: Abigail, Barnaby, Biscuit, Brick, Chester, CountBogey, Dot, Farmer, Groundskeeper, Harold, Moss, Nana, OldManOak, Rexford, Dinos, Lunar, Seasons, Groovy | `Assets/_Project/Art/Characters/<Name>/` | various | Reuse later as rivals/cameos. |

- **Facial expressions:** none as blend shapes. Faces are painted into the base-color texture. Only a procedural wink exists (`CharacterModel.eyeSize`, head-bone eye positions; `Assets/_Project/Scripts/Characters/CharacterModel.cs`). Mark: **build new** if racing needs expressions (texture swaps or simple eye/mouth decals).
- **Drivers in a cockpit:** TO has golf clips only (swings, reactions, dances). Seated driving/steering, piloting and diving poses: **build new** (Mixamo "Driving" / "Sitting" clips, listed in `Docs/MODELS-NEEDED.md` when needed).
- Code for playing a Humanoid model through Playables: `Assets/_Project/Scripts/Characters/ModelGolfer.cs`, `ClipPicker.cs`. Adapt (podium and intro only).

### 2.2 Voice clips

`Assets/_Project/Resources/Audio/Voice/<id>/*.ogg` (32 speakers; about 42 clips per golfer; announcer about 1,000 clips) + `Resources/Audio/subtitles.json`.

| Kind | Examples | Mark |
|---|---|---|
| General reactions | `greet`, `laugh`, `gasp`, `grunt`, `taunt`, `win`, `lose`, `lucky`, `quipgood`, `quipbad`, `disaster`, `trouble`, `wait`, `water` | **Reuse directly** for race banter (hits, overtakes, splash-down, win/lose). |
| Golf-specific | `birdie`, `bogey`, `par`, `eagle`, `ace`, `teeup`, `drive`, `holeout`, `threeputt`, `lipout`, `stiff`, `mishit` | Don't reuse. |
| Announcer | course and hazard lines (`candy_*`, `dino_*`, `exp_*`, `farm_*`, ...) | Build new race-announcer lines (ElevenLabs) in the same voice. |

### 2.3 Music cues

- Source MP3s: `Art/Music/*.mp3` (25 tracks, e.g. `course_seaside.mp3`, `course_atlantis.mp3`, `course_jurassic.mp3`, `course_expedition.mp3`, `title.mp3`, `round_end.mp3`, `credits.mp3`).
- Game copies: `Assets/_Project/Resources/Audio/Music/*.ogg` with matching `*_intro.ogg` stings.
- Jingles: `Assets/_Project/Resources/Audio/Jingles/hole_in_one.wav`, `match_win.wav`.

| Cue | Use in ShiftStorm | Mark |
|---|---|---|
| `course_seaside`, `course_atlantis`, `course_jurassic`, `course_expedition` (+ intros) | Paradise Plunge placeholder soundtrack (beach, sunken, jungle sections) | **Reuse directly** for the prototype; new Suno race track later. |
| `title`, `round_end`, `match_win` | Menu / results | Reuse directly. |

### 2.4 SFX useful for water and air

`Assets/_Project/Resources/Audio/Sfx/`: `amb_waves.wav`, `amb_gull_1..3.wav`, `amb_bubbles.wav`, `amb_atlantis.wav`, `amb_jungle.wav`, `amb_dolphin_1..2.wav`, `amb_wind.wav`, `amb_wind_strong.wav`, `sea_splash_1..2.wav`, `splash_1..2.wav`, `cloud_whoosh_1..2.wav`, `power_whoosh.wav`. Mark: **reuse directly** as ambience/splash/whoosh placeholders. Engine, propeller, boat motor and sub hum: build new (ElevenLabs) or copy CR's `Assets/Scripts/Vehicle/VehicleSoundSynthesis.cs`.

### 2.5 Tropical / water / beach scenery and shaders

TO courses are **built in code at runtime** from hole JSON (`Assets/_Project/Resources/Courses/<Course>/holeN.json`) by builder scripts; there are few environment mesh assets. Reuse means porting builder code (and the shaders) or re-scripting in Blender.

| Source | What it has | Paradise Plunge segment | Mark |
|---|---|---|---|
| `Assets/_Project/Scripts/Course/SeasideBuilder.cs` (Gull's Point Links, `Resources/Courses/GullsPoint`, look `Resources/Tuning/ArtStyle_Seaside.asset`) | Toon ocean to the horizon, shore foam lines, swaying dune grass, dry-stone walls, lighthouse, harbor village, gulls | Beach promenade, cliffs, lagoon surface | **Adapt** (port meshes/placement code; best match) |
| `Assets/_Project/Scripts/Course/JurassicBuilder.cs` (Jurassic Par, climate `tropical`, `ArtStyle_Jurassic.asset`) | Cycad palms, tree ferns, mossy boulders, **waterfalls off mossy cliffs**, swamp, hot springs | Waterfall, cliffs, mangrove stand-in | Adapt |
| `Assets/_Project/Scripts/Course/AtlantisBuilder.cs` + `.Round25.cs` (Lost City Links, climate `undersea`, `ArtStyle_Atlantis.asset`) | Sea floor sand, branching and fan coral, kelp forests, white marble temples, columns, **city gates**, crystals, galleon wreck | Sunken gateway, coral vault (Dive) | Adapt |
| `Assets/_Project/Scripts/Course/ExpeditionBuilder.cs` (The Lost Expedition, climate `jungle`) | Great falls, ruins, stone heads, stepped temple, hanging vines, gateway stone ring | Waterfall, mangroves, sky arch idea | Adapt |
| `Assets/_Project/Resources/Shaders/ToonOcean.shader` (+ `Foam.shader`) | Two-tone rolling waves, crest lines, vertex swell, distance fade; opaque, cheap | Lagoon and sea surface | **Adapt** (URP port; also expose swell height to Boat buoyancy) |
| `Toon.shader`, `ToonTextured.shader`, `ToonOutline.shader`, `ToonCommon.cginc` | 2-3 band toon light + outline; `ToonTextured` is the character shader | Everything (house style) | Adapt (URP port; one shared toon lit + outline) |
| `ToonSway.shader`, `ToonTerrain.shader`, `ToonGlass.shader`, `Sky.shader`, `Cloud.shader`, `Glow.shader` | Grass/palm sway, ground, glass (Atlantis bubble), sky, clouds, crystals | Palms, ground, sub canopy, sky arch | Adapt (URP port) |
| `Assets/_Project/Scripts/Course/ArtStyle.cs` + `Resources/Tuning/ArtStyle*.asset` | One ScriptableObject of toon bands, outline, sky, fog, palettes, water colors | Per-section look | Adapt (good model for ShiftStorm's look data) |
| Mangroves, lagoon shoreline, sky arch, race track surface | — | — | **Build new** (Blender script) |

---

## 3. Recommendations

### Recommended prototype driver
1. **Rocket Rae** (`rae`, `Assets/_Project/Art/Characters/Rae/Rae.fbx`): power and speed personality reads instantly as a racer; Humanoid, flex clip on hand for the intro.
2. **Chip "Showtime" Sterling** (`chip`, `Assets/_Project/Art/Characters/Chip/Chip.fbx`): TO's balanced default lead (all stats 5), the face of the series, full voice set.
3. **Vivi** (`vivi`, `Assets/_Project/Art/Characters/Vivi/Vivi.fbx`): already has Victory and two dance clips, so the finish/podium works with no new Mixamo work.

### Recommended prototype location
1. **Gull's Point Links seaside** (`SeasideBuilder.cs`, `ToonOcean.shader`, `ArtStyle_Seaside.asset`): ocean, beach, cliffs and foam already exist, which covers the Drive to Boat change on the beach promenade and lagoon.
2. **Lost City Links sunken city** (`AtlantisBuilder.cs`, `ArtStyle_Atlantis.asset`): coral, kelp and marble city gates match the sunken gateway and coral vault for the Dive mode.
3. **Jurassic Par / Lost Expedition falls** (`JurassicBuilder.cs`, `ExpeditionBuilder.cs`): waterfalls off cliffs and palms give the waterfall segment and a Fly to Dive drop.

For the Round 1 test strip, start with option 1 (beach promenade, cliff, lagoon): it exercises Drive, Boat and Fly with the least new art.

---

## 4. What ShiftStorm copied in Round 1

| ShiftStorm file | Copied from (chastain_rush) | What changed |
|---|---|---|
| `Launcher/core.cpp`, `core.hpp`, `launcher.cpp`, `store.cpp`, `core_cli.cpp`, `vendor/*` | `Launcher/` (same names) | Renamed ChastainRush -> ShiftStorm (exe, window, cover, save folder `ShiftStorm/ShiftStorm`), catalog URL -> `danemoll-jpg/ShiftStorm-Releases`, `LauncherBuild` reset to 1. Logic unchanged. |
| `Launcher/launcher.rc`, `ShiftStorm.ico` | `Launcher/launcher.rc` | New icon (`Tools/make_icon.py`). |
| `Launcher/pinned_key.hpp`, `pinned-key.json` | not copied | Made fresh by `Tools/New-LauncherKey.py` (ShiftStorm's own key, private half in `Builds/PublisherPrivate/`). |
| `Tools/Build-Launcher.ps1`, `New-LauncherKey.py`, `Prepare-LauncherRelease.py`, `Prepare-ComponentUpdate.py`, `Package-LauncherStarter.py`, `Publish-LauncherRelease.py`, `cleanup_round.py` | `Tools/` (same names) | Names and repository renamed; SDK still read from Racer's `Builds/LauncherSDK`; `gh` still Racer's `Builds/PublisherTools`. New wrapper `Tools/Release-Round.py` stages one draft with the update assets and the starter ZIP. |
| `Assets/Scripts/LauncherBridge.cs` | `Assets/Scripts/LauncherBridge.cs` | Waits for `ShiftVehicle` instead of `GameShell`; `WorldBoot` deferral and `ReplaceOldLauncher` (launcher-1 migration) dropped. |
| `Assets/Scripts/Core/QuietRun.cs` | `Assets/Scripts/CheckRun.cs` (idea) | Adds the explicit `-mute` flag; `-check` / `-checkOut` arguments. |
| `Assets/Scripts/Vehicle/ShiftVehicle.Drive.cs` | `Vehicle/ArcadeVehicle.cs` (`Simulate`) | Suspension, drive, grip, yaw, self-righting ported. Stunt park, weather, shallow water, hold-at-rest, air steering left out. Drift, boost and change authority added. Numbers moved into `DriveTuning`. |
| `Assets/Scripts/Vehicle/ShiftVehicle.Fly.cs` | `Vehicle/PlaneFlight.cs` (`FixedUpdate` air branch) | Attitude flight, bank turn, auto-level and climb/dive speed trade ported. Take-off, landing, crash, aerobatics, map boundary and stall left out; speed floor, ground clearance and ceiling added. Numbers in `FlyTuning`. |
| `Assets/Scripts/Vehicle/ShiftInput.cs` | `Vehicle/VehicleInput.cs` (bindings) | Triggers, dead zones and Y / R reset kept; boost, secondary, vertical axis and invert added; one action set for every form. |
| `Assets/Settings/URP.asset`, `URP_Renderer.asset` | `Assets/Settings/` | Copied as-is (known-good URP 17.6 settings). |
| `Packages/manifest.json` | `Packages/manifest.json` | Same package versions; terrain and particle modules dropped. |

Written new (nothing to copy): boating and diving handling, the form-change framework (zones, speed blending, authority ramp, alignment
assist), the form animator, recovery anchors and course progress, the gray-box strip and lanes, the check pilot, the Blender vehicle script.
Nothing from Tee-dOff was copied in Round 1.

## 5. What ShiftStorm copied in Round 2

All from **TO** (Tee-dOff, read-only; files copied, nothing edited there). Paths: TO relative to `Assets/_Project/`, ShiftStorm relative to `Assets/`.

| From (TO) | To (ShiftStorm) | Changed |
|---|---|---|
| `Resources/Shaders/ToonCommon.cginc`, `Toon.shader`, `ToonOutline.shader` | `Shaders/ShiftToon.hlsl`, `Toon.shader`, `ToonOutline.shader` | Rewritten for URP (HLSL, main light + its shadow map, URP fog, SRP-batcher buffer). The model is the same: 3 flat bands, `albedo x (band x sun x 0.85 + 0.25)`, inverted-hull outline that thins with distance. Added: a texture, a vertex-colour switch, emission, a per-material outline scale, shadow-caster and depth passes. |
| `Resources/Shaders/ToonOcean.shader` | `Shaders/ToonWater.shader` | URP port; drawn from both sides (seen from below in the pool and gateway); `_Scale` added. |
| `Resources/Shaders/Sky.shader` | `Shaders/Sky.shader` | URP port; Groovy Greens swirls dropped. |
| `Resources/Tuning/ArtStyle_Jurassic.asset` (+ `ArtStyle.CreateJurassic`) | `Scripts/World/JurassicLook.cs` | Values copied as constants (sun, ambient, sky, toon bands, outline, water); fog is exponential (one fog mode for the whole build). |
| `Scripts/Course/JurassicBuilder.cs` (tree kinds 8-10, palette) | `Scripts/World/ParadisePlunge.cs` (`Fern`, `Cycad`, `Boulder`), `JurassicLook` palette | The same shapes and colours, rebuilt on ShiftStorm's own `MeshKit.Batch` (no code copied); the course layout is new. |
| `Art/Characters/Abigail/Abigail.fbx` (+ `.meta`, its Humanoid avatar), `Abigail_basecolor.png` | `Resources/Driver/` | Imported Humanoid without animation or materials; painted with the toon outline shader; texture capped at 2048. |
| `Art/Characters/Dinos/{bronto,trike,trex,ptero}/*_mesh.asset`, `*_basecolor.png` | `Resources/Dinos/` | Static scenery (bind pose), toon textured; textures capped at 1024. Eyes, lids and rigs not copied. |
| `Resources/Audio/Music/course_jurassic.ogg`, `course_jurassic_intro.ogg` | `Resources/Audio/Music/` | Streamed; intro then loop. |
| `Resources/Audio/Sfx/amb_jungle, amb_bubbles, splash_1/2, amb_dino_call_1-3, amb_roar_far_1` (.wav) | `Resources/Audio/Sfx/` | Vorbis on import. |
| `Resources/Audio/Voice/abigail/{win_1, quipgood_1-2, disaster_1-2, grunt_1, trouble_1, laugh_1, lucky_1}.ogg` | `Resources/Audio/Voice/abigail/` | Played with cooldowns (finish, reset, waterfall leap). |

Also new in Round 2, not from Tee'd Off: the shared driver animations from Mixamo (Dan, `Resources/Drivers/Animations/`, 6 clips).

Written new in Round 2: the Paradise Plunge course builder and its strips (`MeshKit`), stack-safe progress (`Course.Project` with a
window), the gate rules (forms, wrong way, missed gate), the time trial and best times, the course menu, the driver seat pose, the
waterfall shader.

## 6. What ShiftStorm copied in Round 2b

| From | To (ShiftStorm) | Changed |
|---|---|---|
| CR `Assets/Scripts/ScreenSetting.cs` (Round 24 A3 / Round 27) | `Scripts/Core/ScreenSetting.cs` | Kept: borderless FullScreenWindow at `Display.main.systemWidth x systemHeight` applied at BeforeSceneLoad (wins over Unity's remembered window), windowed at 3/4 size, the choice saved in `display.json`. Dropped: the resolution list and stepping. Changed: skips (and only logs) on any test run (`QuietRun.Muted` / batch mode) instead of `CheckRun.Active`; logs the start-up line (launcher or direct, screen arguments, Unity's starting mode, what it sets); Alt+Enter and the menu row call it. |
| TO audio clips copied in Round 2 (`Resources/Audio/Sfx`, `Voice/abigail`) | same | Import setting only: Preload Audio Data on (first-play loads caused frame hitches). |

Written new in Round 2b: the direction cues and guide target (`ShiftVehicle.Route.cs`), the gate frames and the guide (`Race/RouteGuide.cs`,
`Shaders/Beacon.shader`), the HUD arrow / marker / route strip / speedometer / speed lines / stats (`UI/Hud.Guide.cs`), the camera's speed
FOV and boost shake, the course rescale, and the `nav` / `slowfps` / `screen` checks.

## 7. What ShiftStorm copied in Round 3

| From | To (ShiftStorm) | Changed |
|---|---|---|
| TO `Art/Characters/Chip/Chip.fbx` + `Chip_basecolor.png`, `Rae/Rae.fbx` + `Rae_basecolor.png`, `Vivi/Vivi.fbx` + `Vivi_basecolor.png` | `Resources/Drivers/<Name>/` | Files copied as they are. Their `.fbx.meta` taken from TO (Humanoid, avatar from the model) with a new guid, `isReadable` on (the seat sizing bakes the mesh) and material import off (the game paints them, like Abi). Seated with Abi's shared Mixamo clips (`Driver.Make(v, name)`); no voice for rivals. |
| CR `RoadDriver.cs`, `PlanePilot.cs` (read only, ideas) | — | Nothing copied. Ideas used in ShiftStorm's own `Race/Rival.cs` (`RivalPilot`): a look-ahead target on the racer line, speed planned from the bend ahead (v = sqrt(a r)), a height target in the air. |

Written new in Round 3: the settings file and the menu's Settings / Quit pages (`Core/Settings.cs`, `Game.cs`), the rivals, their pilot
and the race order (`Race/Rival.cs`, `VehicleFactory.MakeRival`), shortcuts (`Race/Shortcut.cs`, `ShiftVehicle.Shortcut.cs`, the Sky Arch
slit and its updraft, the Coral Vault tunnel — switched off for build 4), scrapes in the air and underwater, the speed carried into a
faster form and the wheels-down landing (item 3), the camera's eased speed reference, and the Round 3 checks (`Checks/CheckPilot.Round3.cs`).

## 8. What ShiftStorm copied in Round 4

| From | To (ShiftStorm) | Changed |
|---|---|---|
| TO `Resources/Audio/Sfx/house_hit_1.wav`, `house_hit_2.wav` | `Resources/Audio/Sfx/wall_hit_1.wav`, `wall_hit_2.wav` | Copied as they are, renamed. Imported by ShiftStorm's own audio rule (decompress on load, preloaded). Played on a wall hit, louder for a harder hit (`VehicleSound`). |

Nothing copied from Chastain Rush in Round 4. Written new: walls that glance a racer off in every form (`Vehicle/ShiftVehicle.Walls.cs`,
a frictionless hull, the camera's wall bump), near-miss gates with a time penalty (`ProgressGate.CrossedNear`, `ShiftVehicle.PassGate`),
the AI difficulty setting (`Race/Rival.cs` `AiSkill`, `Core/Settings.cs`, Menu > Settings), the row-by-row launch and the wider grid, the
start-line spline fix and the smooth road width (`World/ParadisePlunge.cs`), the Coral Vault tunnel back on, and the Round 4 checks
(`Checks/CheckPilot.Round4.cs`).

## 9. What ShiftStorm copied in Round 5

| From | To (ShiftStorm) | Changed |
|---|---|---|
| TO `Scripts/Course/JurassicBuilder.cs` decorations "cliff", "boulder", "fern", "ptnest" | `World/ParadisePlunge.Polish.cs` (climb cliff ledges, arch rock stacks, lagoon beaches, the pterodactyl nest on the arch) | Rewritten at ShiftStorm's scale along the race line; same shapes and `ArtStyle_Jurassic` colours (`JurassicLook`), looks only (every collider stays the gray box's). |
| TO `Waterfall` (JurassicBuilder.cs) | `ParadisePlunge.Fall` + `FallStreaks` | Same idea (sheet, mossy lip, foam pool, white streaks running down); uses ShiftStorm's Waterfall shader; optional rock cliff behind. |
| TO `ArtStyle_Jurassic.asset` fog (linear, 140-820 m) and `cloudCount` 9 | `JurassicLook.FogStart / FogEnd` (190-1100 m, 1.35 x for the bigger course), `ChaseCam` keeps linear fog above water; 9 puffy clouds over the stretch | Scaled distances; clouds made of low spheres in TO's cloud / cloudShade colours. |

Nothing copied from Chastain Rush in Round 5. Written new: the speed feel (`VehicleSound` pitch per form + wind, `ChaseCam` lower / closer /
wider at speed + boost kick, `Vehicle/SpeedFx.cs` streaks, spray, bubbles, the road's posts and centre dashes), +15 % speeds (tuning assets),
the AI levels (Easy ~20 % slower, Normal ~8 %), the swap-with-effect change (`Vehicle/FormSet.cs`), vehicle designs (`Vehicle/VehicleDesigns.cs`),
the Garage (`UI/Garage.cs`), the one-command import (`Tools/import_vehicles.py`, `Tools/Blender/vehicle_import.py`, test stand-in
`Tools/Blender/make_test_vehicle.py`) and the Round 5 checks (`Checks/CheckPilot.Round5.cs`).

## 10. What ShiftStorm copied in Round 6

| From (chastain_rush, read-only) | To (ShiftStorm) | Changed |
|---|---|---|
| `Split/SplitScreen.cs` (`SplitScreen`, `SplitSession`) | `Split/SplitScreen.cs` | Same shape: per-player device, second camera made with `CopyFrom` + URP data, top / bottom (player 1 on top) or left / right, per-view cuts (LOD bias x0.6, shadow distance x0.6) restored at the end, pause on a pulled-out controller and rebind when one comes back. Added: far clip 1150 m, Detail layer culled at 500 m, textures one mip down; one shared engine mix (`VehicleSound.Mix` 0.7). Left behind: Free Roam / stunt modes, AI player 2, names, per-player garages, minimap, weather layers. |
| `Menus/GameMenus.Split.cs` (setup page, `UpdateSplitJoin`, `Join`) | `Game.cs` (Page.Split, `OpenSplitPage`, `SplitJoin`, `Join`) | Player 1 = the device that opened the page; A (or Start) on another controller, or Enter on an unused keyboard, joins player 2. Rows trimmed to ShiftStorm's: player 2's driver, AI racers 0-2, screen layout, Race / Time Trial. While the page is up player 1's input is bound to player 1's device only (CR restricts the UI actions). |
| `Racer/VehicleInput.cs` `Bind` / per-device bindings, view and look actions | `Vehicle/ShiftInput.cs` (`Bind`, `Create`, `ConsumeView`, `Look`) | ShiftStorm's own actions kept; X / V view moved off drift / carve (RB / Left Shift stay); invert per player (`MyInvert`, settings.json `invertVertical2`). |
| `Racer/CameraViews.cs` (Chase / Far chase / First person / Front, blend, FreeLook) | `UI/CameraViews.cs` | Same four views and 0.35 s blend, Far chase 1.5 back / 1.55 up, right-stick look (pulled back = behind). Changed: eyes from Abi's Humanoid head bone with the head folded away in her own first person (one skinned mesh), the head views follow the vehicle body not the change's twirl, a clear-of-walls cast for Far chase and look-around, never in the water surface's plane; per player saved in settings.json (`view`, `view2`), not PlayerPrefs. Left behind: mouse look, trailer / winner / helicopter views. |

Written new: per-camera underwater fog (`ChaseCam.BeforeRender`) and gate colours (`RouteGuide`), the HUD once per half (`Hud.Relayout`,
player 2's PAUSED), player 2's vehicle (`VehicleFactory.MakePlayer2`), the split race / trial setup in `Game.StartCourse`, and the Round 6
checks (`Checks/CheckPilot.Round6.cs`: bind with virtual gamepads, split race, views).

## Round 7 copies (2026-11-14)
| Copied | From | Changed |
|---|---|---|
| `Assets/Scripts/Vehicle/EngineSynth.cs` | CR `Assets/Scripts/Vehicle/VehicleSoundSynthesis.cs` | namespace and class name only |
| `Tools/Music/make_loops.py` | TO `Tools/Music/make_loops.py` | paths (Art/Incoming/Music, Assets/Resources/Audio/Music, local/audio_check) and the track list (race_paradise, menu, theme, results) |
| `Tools/Audio/caller_lines.py` | TO `Tools/Audio/elevenlabs_voice.py` (its request and the "TeedOff Barker" voice id from `voices.json`) | rewritten small: one speaker, ffmpeg trim to Ogg, its own line list |
| `Assets/Resources/Audio/Sfx/to_*.wav` (30) | TO `Assets/_Project/Resources/Audio/Sfx` and `Jingles/match_win.wav` | renamed with the `to_` prefix |
| `Assets/Resources/Audio/Sfx/hh_*.mp3` (8) | Hostile Horizon `Assets/Resources/Sounds` | renamed with the `hh_` prefix |
| `Assets/Resources/Audio/Voice/barker/` (7 of 46 clips) | TO `Resources/Audio/Voice/barker` (winner_1/3/5, finale_1, great_2, ohno_1, ohno_3) | renamed to the caller's pools |
| `Assets/Resources/Drivers/Barnaby/` | TO `Assets/_Project/Art/Characters/Barnaby` (model, texture, "Standing Arguing") | none (metas copied with them) |
Docs/SOUNDS.md lists every sound and where it is used.

## Round 8 copies (2026-11-14)
- **Tee'd Off dinosaurs, rigged** (read-only, copied): `Assets/_Project/Art/Characters/Dinos/{bronto,trex,trike,ptero}/<style>_rig.bytes`
  (its Meshy mesh + the skeleton and weights from its `Tools/Characters/rig_dino.py`) -> `Assets/Resources/Dinos/<style>_rig.bytes`.
  ShiftStorm builds the skinned mesh at run time (`World/Dinos.cs` `DinoRig`, the same steps as Tee'd Off's editor `DinoImport.Import`:
  bones from the bone heads, bind poses, UV flip), because ShiftStorm's world is built in code. Changed: no eye lids / blinks (not
  copied), Bone2 skinning, a size by height, ShiftStorm's toon material.
- **Tee'd Off's `ModelCreature` animation** (`Scripts/Characters/ModelCreature.cs`) -> `DinoPose` in `World/Dinos.cs`: walk cycle by speed,
  tail sway, neck look, breathing, roar / chomp, the brontosaurus's neck down, the pterodactyl's wing beats. Changed: no cat / cow /
  blink parts; a flap strength; driven by ShiftStorm's `DinoActor` behaviours (graze, wade, swim, run alongside, roar, swoop).
- **Tee'd Off dinosaur sounds** (`Resources/Audio/Sfx`): `trex_roar_1/2`, `trex_growl_1/2`, `bronto_moo_1/2`, `dino_grunt_1/2`,
  `amb_roar_far_2`, `trex_step_1`, `bronto_step_1` (ElevenLabs-made for Tee'd Off).
- **Tee'd Off voice clips for the drivers' reactions** (`Resources/Audio/Voice/<who>`): Abi, Rae, Chip, Vivi: `disaster_1/2`,
  `mishit_1`, `gasp_1`, `trouble_1/2`, `bogey_1` (Abi), `water_1`, `lose_1`, `win_1`, `quipgood_1/2`, `quipbad_1` (Abi) — levelled to
  -16 LUFS (`Tools/Audio/normalize_voice.py`). New: 3 lines each in their own Tee'd Off ElevenLabs voices (`Tools/Audio/driver_lines.py`).
- Nothing copied from chastain_rush or Hostile-Horizon this round.

## Round 9 copies (2026-11-15)
- **Gridiron Glory (`C:\Users\danmo\footballGame`, read-only)** — the crowd:
  - `Scripts/Gameplay/Field/CrowdRenderer.cs` -> `Assets/Scripts/World/Crowd.cs`. Changed: one Crowd per grandstand with its own material;
    a cheer that ripples along the stand from where a racer passes at its speed (instead of the match mood, boos, towels by side, the wave
    round a bowl, ponchos, empty seats); tropical shirts and sun hats; size x1.35; drawn per camera from URP's `beginCameraRendering` (split
    screen, checks); a murmur bed per stand and a few cheers, never on top of the finish's own crowd sound.
  - `Resources/Shaders/CrowdInstanced.shader` -> `Assets/Resources/Shaders/CrowdInstanced.shader` (+ meta). Changed: URP pass
    ("UniversalForward"), the light / ambient from globals set by Crowd.cs, the per-stand cheer sweep, no boo / towel / wave / poncho / empty-seat logic.
  - `Resources/Materials/CrowdInstanced.mat`, `Resources/Crowd/FanAtlas.png`, `Resources/Crowd/fan_atlas.json` (+ metas): unchanged.
  - Sounds: `crowd_bed_engaged`, `crowd_cheer_big_1/2`, `crowd_cheer_small_1/2/3`, `crowd_roar_1` -> `Assets/Resources/Audio/Sfx/gg_crowd_*.ogg`
    (re-encoded to Vorbis, otherwise unchanged).
- **Tee'd Off (read-only)** — the dinosaur rigger: `Tools/Characters/rig_dino.py`, `glb_load.py`, `rig_biscuit.py` -> `Tools/Characters/`.
  Changed in `rig_dino.py` only: the creature list (Dan's raptor and plesiosaur), the paths (local/dinos, Assets/Resources/Dinos via
  `Tools/Characters/import_dinos.py`), the raptor oriented by its body axis, no bipeds, eyes optional.

## Round 10 copies (2026-11-15)
- **Nothing new copied from the read-only projects.** Reused inside ShiftStorm: Gridiron Glory's crowd (Round 9 copy) in Coaster Coast's
  four stands; Tee'd Off's toon shaders and the Round 7 loop's rail maths (`LoopRail.Pos`) for the coaster rails (`World/CoasterRail.cs`,
  `Vehicle/ShiftVehicle.Rail.cs`); `Tools/Music/make_loops.py` (Round 7 copy) for Dan's `race_coaster` song.
- **Removed:** Tee'd Off's Jurassic Par music (`course_jurassic*.ogg`, the Round 7 fallback): no Tee'd Off music is in the game any more.
