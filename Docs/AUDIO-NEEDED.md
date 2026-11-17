# ShiftStorm — audio Dan makes (music first)

Sound effects are **not** on this list: Code picks them from Tee'd Off, Chastain Rush and Hostile Horizon
(Dan, 2026-11-14). This page is the Suno music, then (lower priority) any race-caller lines Code can't make itself.

## What these prompts are based on (Tee'd Off, read-only)

- `Tee-dOff/docs/music-suno.md`: every Tee'd Off course song was made in Suno **Custom mode**, the Style text
  pasted in, **Instrumental on**, the course name as the title. Style text in one line of comma-separated tags:
  "cartoon ... game", the instruments, the mood, "major key", a bpm, "seamless loop", "no vocals".
- **Jurassic Par** (`course_jurassic.mp3`, 178.8 s, 104 bpm, a 72-bar loop after a 2.7 s intro): *adventurous
  prehistoric jungle ... tribal hand drums and marimba, plucky pizzicato strings, warm low brass swells, wooden
  flutes and bird-like whistles, playful and a little mysterious, bouncy groove*. This is ShiftStorm's placeholder
  now, so the race theme keeps its instruments and adds racing drive.
- **Title** (`title.mp3`, 86.5 s, 112 bpm: ukulele, pizzicato, glockenspiel, whistling hook) and **Round Complete**
  (`round_end.mp3`, 19 s: brass + xylophone fanfare, then a bouncy outro) set the menu / results pattern.
- Later songs (Lost City Links 180 s, Funland, Bogey Manor) used wordless choirs / sung parts; their loops then had
  to be cut around the voice. Racing music plays under engines and the caller, so these are **instrumental**,
  like Jurassic Par.
- Tee'd Off tip that still applies: pick the take that **keeps one steady feel** (no big breakdown, no fade-out)
  and ends on the chord it starts on. Code cuts the intro + seamless loop itself (a copy of Tee'd Off's
  `make_loops.py`), so the exact length doesn't matter.

## Suno songs

**Music rules for ShiftStorm (Dan, 2026-11-15):**
- **No Tee'd Off music in ShiftStorm.** Every song is new, made by Dan in Suno. Remixes of Tee'd Off themes are possible
  when Dan asks for one, but the default is a new song. (Tee'd Off sound effects and voices are still fine to reuse.)
- **Vocals as an instrument** — wordless chants, "whoa-oh" hooks, choir "ooh / aah", vocal chops, scat — is what Dan meant
  by "instruments played with the mouth": some Tee'd Off courses used voices this way. Use it **on some songs, when it fits**,
  not on every one. Real mouth-played instruments (harmonica, kazoo, pan flute, didgeridoo, jaw harp, ocarina, melodica,
  whistling) are fine too when they suit the song — none of them is required, and don't lean on the same one everywhere.
- Current picks: theme harmonica + jaw harp, Paradise Plunge didgeridoo + pan flute, menu ocarina + melodica, victory none;
  the new coaster track uses **wordless vocal chants** as its hook.
If a Suno take still adds whistling, add `no whistling` at the end of the Style and try again.

Suno: Custom mode, paste the **Style**, **Instrumental: on**, use the **Title**. Download the MP3 and save it
under exactly the file name below in **`C:\Users\danmo\ShiftStorm\Art\Incoming\Music\`**. Tell Code (or just leave
it there: Code checks the folder) and it goes in; until then a ShiftStorm song (the theme) stands in — never a Tee'd Off song.

### 0. Game theme song — `theme.mp3` (Dan, 2026-11-14: "a theme song for the game")
The signature ShiftStorm tune for the title screen (and the credits). **Instrumental** — Dan: no lyrics on a song
that loops while someone sits in the menu. A big, catchy, hummable main melody does the job lyrics would.
- **Title:** `ShiftStorm`
- **Instrumental:** on.
- **Style:**
  `catchy cartoon video game main theme, instrumental, upbeat and heroic, memorable hummable lead melody on bright brass and harmonica, surf rock guitar twang, tribal jungle hand drums and punchy kit drums, bouncy marimba and pizzicato strings, slap bass, boingy jaw harp accents, playful Saturday morning cartoon adventure, big anthemic hook, steady feel, major key, 124 bpm, seamless loop, no vocals`
- **Length:** 2-3 min. **Loop:** the title screen loops it; pick a take with no fade-out and no slow breakdown.
  Code cuts the intro + seamless loop.
- Tip: this tune can be the parent of the others — make the **menu / Garage theme** and the **victory** song as
  Suno **Covers** of this one, so the whole game shares one melody.

### 1. Race theme — `race_paradise.mp3` (most important)
- **Title:** `Paradise Plunge`
- **Style:**
  `upbeat cartoon racing game theme, instrumental, prehistoric jungle adventure, driving tribal hand drums and tight snappy kit drums, bouncy marimba and plucky pizzicato strings riff, bold warm brass hook, deep rhythmic didgeridoo grooves driving the beat, breathy pan flute runs answering the brass, slap bass groove, playful and energetic, fast and fun, steady drive all the way, major key, 132 bpm, seamless loop, no whistling, no jaw harp, no vocals`
- **Length:** 2-3 min (Suno's normal 2:30-3:00 take is fine). **Loop:** the whole song loops; choose a take with no
  fade-out, no slow middle section and no big ending. Code cuts a seamless loop on the bar grid.
- In the game it gets more energy in flight, is muffled underwater and brightens at the waterfall exit — that's
  done with filters, so one song is all that's needed.

### 2. Menu / Garage theme — `menu.mp3`
- **Title:** `Paradise Plunge (Pit Stop)`
- **Style:**
  `relaxed cartoon racing game menu music, instrumental, laid-back prehistoric jungle groove, soft hand drums and light shaker, gentle marimba and ukulele carrying a bright brass-style melody, warm upright bass, sweet ocarina and melodica answering the melody, sunny and cheeky, easygoing garage vibe, major key, 104 bpm, seamless loop, no vocals`
- **Length:** 1:30-3:00. **Loop:** whole song loops, steady feel, no fade-out.
- **Same melody as the race theme:** after making song 1, open it in Suno and use **Cover** (or Remix) with this
  Style text, so the melody carries over at the relaxed tempo. If Cover isn't available, a normal take with this
  Style is fine.

### 3. Results / victory — `results.mp3`
- **Title:** `Chequered Flag`
- **Style:**
  `short triumphant cartoon racing victory fanfare, instrumental, bright brass and xylophone, tribal drum roll and cymbal swell, marimba flourish, happy jungle celebration, then a short bouncy outro, major key, 120 bpm, 25 seconds`
- **Length:** 20-40 s (Suno may make a longer take: Code keeps the first fanfare and fades it). **Plays once**, no
  loop. Again a Cover of song 1 keeps the melody if Suno allows it.

### 4. Second track race song — `race_coaster.mp3` (Dan, 2026-11-15; the coaster track, working name "Coaster Coast")
A new song for the seaside amusement-park track (roller coaster, log flume, Ferris wheel, fireworks, an aquarium under the
pier). Not a Tee'd Off song. It uses **wordless vocal chants as an instrument** (not words), so **Instrumental: off** and the
"lyrics" are only chant syllables.
- **Title:** `Coaster Coast`
- **Style:**
  `upbeat cartoon racing game theme, seaside carnival funfair, thrill ride energy, surf rock twang guitar and bright boardwalk calliope organ, punchy brass stabs, snappy kit drums with big tom fills, bouncy bass, glockenspiel and xylophone sparkles, wordless group vocal chants used as an instrument, whoa-oh hooks and hey shouts, no sung words, playful and exciting, steady drive all the way, major key, 140 bpm, seamless loop`
- **Lyrics** (chant syllables only; paste as is):
```
[Intro]
[Instrumental]

[Hook]
Whoa-oh! Whoa-oh-oh!
Whoa-oh! Hey! Hey!

[Instrumental Break]

[Hook]
Whoa-oh! Whoa-oh-oh!
Whoa-oh! Hey! Hey!

[Instrumental]

[Hook]
Whoa-oh! Whoa-oh-oh!
Whoa-oh! Hey!
```
- **Length:** 2-3 min. **Loop:** pick a take with no fade-out and no slow breakdown. If Suno sings real words, try another
  take (or add `no lyrics, vocal chants only` to the Style).
- Save as `Art/Incoming/Music/race_coaster.mp3`.

### 5. Third track race song — `race_bigtop.mp3` (Dan, 2026-11-16; Big Top Breakdown, a travelling circus)
**Status (2026-11-16): received** ("ShiftStorm - Big Top Breakdown.mp3"), in the game as `race_bigtop` (loop made by `Tools/Music/make_loops.py`).
A new song for the circus track (wagons, the big top, a human-cannonball flight through aerial hoops, a river and a water
tank). Not a Tee'd Off song, and **clearly different from Coaster Coast's** (that one is surf guitar + calliope + vocal
chants): this is a zany, slightly ramshackle circus band. Mouth instrument: a cheeky **kazoo** (fits the clowns; not used
in any other song). **Instrumental: on.** Don't imitate any famous circus march.
- **Title:** `Big Top Breakdown`
- **Style:**
  `upbeat cartoon racing game theme, zany travelling circus band, oom-pah tuba and big bass drum, snappy snare rolls, slide trombone smears, honky-tonk piano and accordion, banjo strums, cheeky kazoo lead melody, xylophone runs, cymbal crashes, playful mischievous chaos, a little ramshackle, steady drive all the way, major key, 150 bpm, seamless loop, no vocals`
- **Length:** 2-3 min. **Loop:** pick a take with no fade-out and no slow breakdown.
- Save as `Art/Incoming/Music/race_bigtop.mp3`.

## Race caller: Barnaby — lower priority

Not needed from Dan: an ElevenLabs key is set, so Code makes his new racing lines in his own Tee'd Off voice
(the "Barnaby" voice) and uses his existing clips too. Nothing to do here unless a line sounds wrong.
