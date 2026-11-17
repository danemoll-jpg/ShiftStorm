# ShiftStorm sounds (Round 7 item 5)

Code chose every sound (Dan, 2026-11-14). All copied from the three read-only projects into
`Assets/Resources/Audio/`; nothing was changed in them. Prefixes: `to_` Tee'd Off (its ElevenLabs effects),
`hh_` Hostile Horizon (its spaceship sounds). Played through `Core/GameSounds.cs` at the **Effects** volume
(Menu > Settings) unless noted.

## Engines (`Vehicle/VehicleSound.cs`)
| Form | Sound | From |
|---|---|---|
| Car | synthesised "six" engine loop (load), pitch over the car's 0-75 m/s, past it on boost | Chastain Rush `VehicleSoundSynthesis.cs` (copied as `Vehicle/EngineSynth.cs`) |
| Boat | synthesised "flat6" loop (a raspy outboard) | Chastain Rush synthesis |
| Plane | synthesised "v12" loop + `hh_engine_cruise` under it | Chastain Rush + Hostile Horizon `engine_cruise.mp3` |
| Sub | synthesised low rumble (kind 3) + `hh_engine_idle` | Chastain Rush + Hostile Horizon `engine_idle.mp3` |
| all | wind layer (generated noise, rises with speed; muffled underwater) | ShiftStorm (Round 5) |
| rivals | the same engines, 3D (heard from where they are) | as above |

The forms cross-fade over 0.35 s at a change; louder with the throttle.

## Effects (`Core/GameSounds.cs`)
| Sound | When | File (source) |
|---|---|---|
| change whoosh | every form change | `to_swing_whoosh_1/2` (TO) |
| wings out | into the plane | `hh_sting_launch` (HH) |
| canopy seals | into the sub | `hh_canopy_slam` (HH) |
| wheels down | into the car | `hh_gear_down` (HH) |
| hull down | into the boat | `to_sea_splash_1/2` (TO) + the splash below |
| splash | landing in water, the boat through the waterfall | `splash_1/2` (TO, since Round 2) |
| boost | boost pressed | `to_power_whoosh` (TO) + `hh_jump_drive` quietly (HH) |
| gate chime | every main gate (higher at the finish) | `to_fun_ding_1/2` (TO) |
| ring chime | a shortcut's / the Lake flight's sub-gates | `to_amb_chimes_1/2` (TO) |
| near miss | a gate counted with +3 s | `to_crowd_aww` (TO) |
| boost pad | the loop's boost pad | `to_rail_zip_1/2` (TO) |
| loop | into the loop / out of it | `to_slide_whoosh_1/2` / `to_crowd_ooh` (TO) |
| jump | airborne 0.35 s / the landing | `to_cloud_whoosh_1/2` / `to_soccer_thump_1/2` (TO) |
| wall hit | a wall hit (louder when harder) | `wall_hit_1/2` (TO, Round 4) |
| wall scrape | a shallow touch along a wall | `hh_canopy_scrape` (HH) |
| water reset | a car in a creek | `hh_water_splash`, `to_pool_splash_1` |
| reset | any reset | `to_void_bloop_1/2` (TO) |
| countdown | 3, 2, 1 | `to_rocket_beep` (TO) |
| GO | the start | `to_fun_horn_1` (TO) |
| finish | the line | `to_cheer_big` + `to_applause` (TO) |
| win / not first | after the finish in a race | `to_match_win` jingle / `to_crowd_clap_small` (TO) |
| menu | open / move / close | `to_ui_select` / `to_ui_click` / `to_ui_back` (TO) |
| ambience | jungle above water, bubbles below, far dinosaur calls in flight | `amb_jungle`, `amb_bubbles`, `amb_dino_call_*`, `amb_roar_far_1` (TO, Round 2) |

## Music (`Core/CourseAudio.cs`, **Music** volume)
- Race: Dan's `race_paradise` when it is in; until then Tee'd Off's Jurassic Par (`course_jurassic`).
- Menu main page: Dan's `theme` (else the menu song, else Tee'd Off's `title` as `ph_menu`); Garage / Settings: `menu`.
- Results: Dan's `results` (else Tee'd Off's `round_end` as `ph_results`), once.
- Mix by form (filters, not separate songs): flight a little louder; underwater softer and low-passed to 650 Hz;
  a 2.5 s lift at the waterfall exit.
- Dan's Suno files: drop them in `Art/Incoming/Music/` and run `python Tools/Music/make_loops.py` (copied from
  Tee'd Off) to make the intro + seamless loop in `Assets/Resources/Audio/Music/`.

## The race caller: Barnaby (`Race/Caller.cs`, **Voices** volume)
- 39 new lines made with ElevenLabs in his own Tee'd Off voice ("TeedOff Barker" (the ElevenLabs voice's own name), voice id from Tee'd Off's
  `Tools/Audio/voices.json`, model eleven_v3): `Tools/Audio/caller_lines.py`, 949 credits.
- 7 of his Tee'd Off clips reused: `winner_1`, `winner_3`, `winner_5`, `finale_1` (the winner), `great_2` (a big jump),
  `ohno_1` (a near miss), `ohno_3` (the results when you didn't win).
- Words: `Assets/Resources/Audio/Voice/barker/lines.json` (subtitles on the HUD: "Barnaby: ...").

## Round 8 (2026-11-14)
- **Volumes:** Music, Voices (Barnaby + the drivers), **Engines** (new: engines + wind, default 60 %), Effects. An older
  settings.json gets Engines = 60 % of its old Effects value (never more than 60 %). Music is 1.3x build 8's level (+2.3 dB).
- **Barnaby loud:** build 8's converter faded every new line out from its first sample (`afade=t=out:st=0`): his 39 ElevenLabs
  lines were silent after 20 ms (-43 LUFS). All re-recorded (151 ElevenLabs lines now, 3,875 credits) + 7 Tee'd Off clips = 158;
  every voice clip levelled to -16 LUFS (`Tools/Audio/normalize_voice.py`). Under his lines the music and engines duck 8 dB (`Caller.Duck`).
- **Drivers' mess-ups:** per driver 3 new ElevenLabs lines ("Noooo!", "Oh no!", one in character) + 5 of their Tee'd Off clips (140 credits).
- **Dinosaurs (positional, Effects volume):** T. rex roar (`trex_roar_1/2`), brontosaurus moo (`bronto_moo_1/2`), grunts
  (`dino_grunt_1/2`, the triceratops; the raptors pitched up), pterodactyl calls (`amb_dino_call_2/3`).
- **Finish by place:** 1st the cheer, applause and win jingle; a middle place a small clap; last an "aww" (`to_crowd_aww`), no applause.

## Round 9 (2026-11-15): one sound per event
- **Finish:** Tee'd Off's win jingle (`to_match_win`) is gone: it played on top of Dan's own finish song (Victory, `results`). Now one
  crowd sound by place (1st / time trial the big cheer, a middle place a small clap, last an "aww") + Dan's song; no gate chime on the
  finish gate. Build 10 played cheer + applause + jingle + chime + song.
- **One of a kind at a time** (`Core/SoundLog.cs`): a second music, crowd, splash, whoosh, impact or chime sound within 0.5 s of one of its
  kind is skipped (the change's whoosh on a jump's air whoosh, the landing's double splash, a wall hit under a scrape, both start stands at GO).
- **Event-by-event list:** the `sounds` check (`Docs/Checks/round9/check_report_sounds.txt`): every race event with what it plays.
- **Crowd (Gridiron Glory's, `gg_crowd_*`):** a murmur bed at each grandstand (3D, Effects volume), one roar from the start stands at GO,
  a cheer at the waterfall-landing stand when your racer passes. Every voice line in use: `Docs/Checks/round9/voice_lines.md`.

## Round 10 (2026-11-15): Coaster Coast, no Tee'd Off music
- **Music:** each course has its own song (`Course.music`): Paradise Plunge `race_paradise`, Coaster Coast `race_coaster` (Dan's Suno
  "ShiftStorm - Coaster Coast", looped by `Tools/Music/make_loops.py`: 145.6 bpm, an 86-bar loop after an 18.5 s intro, seam OK). A course
  whose song is missing plays ShiftStorm's `theme`. Tee'd Off's Jurassic Par (`course_jurassic`, the old fallback) is deleted.
- **Ambience:** the jungle bed and the far dinosaur calls only on Paradise Plunge; the bubbles under water on both.
- **Barnaby:** 53 new lines (ElevenLabs, his voice, -16 LUFS; 1,676 credits): Coaster Coast's own start / opening-night (Docs/STORY.md:
  the tour's first public race), fill-ins, wings (through the Ferris wheel), splash, the coaster, the Coaster Gap, the corkscrew, the top hat,
  the helter-skelter, fireworks, the balloon, the flume, the lighthouse, the pier, the carousel, the octopus, the whale / dolphins, the splash
  pool, and a results line naming each course (`ccres_*`, `ppres_*`). On Coaster Coast the Paradise Plunge pools that name the jungle swap
  for these (`Caller.ForCourse`); set-piece lines only while it is big on screen (`SetPiece`).

## Round 11 (2026-11-15): Buck "Big Finish" Barlow (`Race/Buck.cs`, **Voices** volume)
- **53 lines** in his own ElevenLabs voice **"ShiftStorm Buck"** (Dan's, eleven_v3; `Tools/Audio/buck_lines.py`, 2,602 credits), levelled
  to -16 LUFS like every voice: `Resources/Audio/Voice/buck/<id>.ogg` + `lines.json` (subtitles). Pools: briefings per course (4 each),
  short briefings (2 each), results remarks by place per course (9 each), anecdotes (6), The Tour's scenes and reactions (17).
  Transcribed back with local speech-to-text: `Docs/Checks/round11/buck_lines.md` (52 of 53 at a 0.80+ word match; the 53rd is
  "all right" heard as "alright").
- Never over anyone: while he talks Barnaby plays nothing (not even GO or the winner: `Caller.Play`), drivers wait (`Caller.Talking`), and
  his briefing / scene starts by stopping Barnaby and any driver line (`Buck.Hold`). Music and engines duck under him as under Barnaby.
- Barnaby: 4 new lines for Abi as an AI rival (passabi_1-2, byabi_1-2, his voice, 101 credits); overtakes and the winner's line now go
  by the racer's name (any driver can be player 1 or the AI).

## Round 14 (2026-11-16): the crowd's own volume, much quieter
- Dan: "The crowd noise is far too loud." Every crowd sound now plays at the new **Crowd** volume (Menu > Settings > Audio > Crowd,
  default 100 %, up to 200 %, saved as `settings.json` "crowdVol") and 19 dB under build 15 (`Settings.CrowdGain` 0.11; -11 dB first,
  but the mix check still had the GO roar and the finish cheer over the engine): the grandstands' murmur
  beds (`gg_crowd_bed_engaged`), their cheers and the GO roar (`gg_crowd_cheer_*`, `gg_crowd_roar_1`), and GameSounds' crowd kind
  (the finish's `to_cheer_big` / `to_crowd_clap_small` / `to_crowd_aww`, the near miss's "aww", the loop's `to_crowd_ooh`).
  These no longer follow the Effects volume.
- Music: the title screen and the main menu play the theme (`theme`); the other menu pages `menu`. Engines, wind and the jungle are
  silent behind the front menus (the race there is frozen). Left / right on a menu value plays the move click (a little higher).

## Round 15 (2026-11-16): Barnaby by name, Big Top Breakdown
- **Barnaby by name:** the race caller is "Barnaby" in his subtitles ("Barnaby: ..."), on his booth's board, in the results ("Race called
  by Barnaby") and the Voices volume's description. None of his lines (or anyone else's) ever said "Barker" (the line texts and the
  speech-to-text transcripts were searched), so nothing needed re-recording for the name. The clip folder stays `Voice/barker` (an id).
- **Music:** Big Top Breakdown plays Dan's Suno song "ShiftStorm - Big Top Breakdown" as `race_bigtop` (+ `_intro`; 178.6 s,
  74.7 bpm, loop 71.18-167.50 s, `Tools/Music/make_loops.py race_bigtop`, seam checks OK).
- **Barnaby, 30 new lines** (`caller_lines.py` ROUND15, 861 credits): `bt*` pools: start (3), fill-ins (4), wings, splashdown,
  results (2), the Clown Jump, the Globe of Death (2), the cannon (2) and one each for the wagon parade, the strongman, the tightrope
  walker, the clowns, the big top, the trapeze, the elephants, the show boat, the sea lions, the rapids, the escape tank, the sunken
  wagon and the splash pool (set-piece lines only while that thing is big on the screen, Round 8b's rule).
- **Buck, 20 new lines + 1 re-recorded** (`buck_lines.py`, 1,053 credits): Big Top briefings (3 + 2 short), results remarks (6), The
  Tour's Stop 3 scenes (4 intro, 2 pass, 2 retry; he reads Lionel Lustre's dare aloud until the ringmaster has a voice), Vivi's
  challenge; `tour_done_1` now says "Three stops, three shows".
- **Vivi, 3 lines** (`driver_lines.py` ROUND15, 98 credits): her Cannonball Run challenge (taunt, win, lose).
