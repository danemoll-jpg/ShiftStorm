# ShiftStorm — models and animations needed from Dan

Keep each round's list short; each item is Dan's time.

## Round 5 — Abi's transforming machine, stage 1: three design directions (Tripo, by hand)

**Status (2026-11-13): waiting on Dan.** Please make **only the car form of each direction first** (3 models) so you can compare
them in 3D (Menu > Garage) before spending time on boats, planes and subs. When you pick a direction, make its other three forms.

**What Code does with them:** `python Tools/import_vehicles.py` (one command) cleans each FBX in Blender (scale to 4.0 m long,
origin at the hull centre, nose to +Z, wheels / waterline on the hull's floor, seat point, two LODs), copies it into
`Assets/Resources/Vehicles/<Design>/` and the game shows it in the Garage and can race it. The change between forms is a 0.6 s
swap with an effect (squash, a spin of panels, sparkles / spray / bubbles), so each form is one solid model — **no moving parts,
no separate wheels needed**. Rivals get recoloured copies (the green parts are recoloured), so keep green as the main body colour.

### Shared for all 12 images
- Colours (from Abi's Tee'd Off outfit): **forest / kelly green** main body, **cream white** panels and stripes, **polished silver**
  trim, a **bright orange** accent (one stripe, the intakes or the fin tips), dark charcoal tyres / underside, light-blue tinted glass.
- Style: Tee'd Off toon look: chunky, friendly proportions, slightly oversized cockpit, smooth clean surfaces, soft bevels,
  hand-painted cel-shaded colours, no grime, no realistic decals.
- Every image: **3/4 front view** (from front-left, slightly above), **plain light grey background**, the whole vehicle in frame with
  a little margin, **no driver, no people, no text, no logos, no numbers, no props, no ground shadow clutter**.
- Same machine in all four forms: the **same cockpit / canopy shape**, the same colour layout and the same signature detail
  (named per direction below). Seat one person, cockpit open-topped or under a clear canopy.

### Tripo settings (each model)
- **Image to Model**, one image (the 3/4 front view). If Tripo offers multiview and you also have a side view, use it.
- **Texture: on** (HD texture if offered), **PBR: off** (we want the flat toon colours), style: none / original.
- **Low poly / game mesh: on** (Smart Low Poly or a face limit of about **20,000** faces).
- Export **FBX**, keep the texture (embedded, or the PNG next to the FBX in the same folder).

### Where to save
Images (any name): `C:\Users\danmo\ShiftStorm\Art\Concepts\Vehicles\<Design>\`
Models: `C:\Users\danmo\ShiftStorm\Art\Incoming\Vehicles\<Design>\<Design>_<Form>.fbx` (+ its texture PNG next to it)
`<Design>` = `TeeShot`, `Jetstream` or `Mudlark`; `<Form>` = `Car`, `Boat`, `Plane`, `Sub`.
Example: `Art\Incoming\Vehicles\TeeShot\TeeShot_Car.fbx`. Tell Code when they're there (or just leave them; Code looks).

### Direction 1 — "Tee Shot" (golf-cart roadster)
*Design note:* the friendliest of the three, straight out of a golf course: round bubble canopy, a **dimpled white golf-ball dome**
on the nose (the signature), a little chrome flagpole antenna with an orange pennant behind the seat, rounded fenders. Reads instantly
as Tee'd Off. Risk: may look slow.

1. `TeeShot_Car.fbx` — *"Stylized cartoon toy-like racing car, compact rounded roadster with a clear bubble canopy over a single seat, a dimpled white golf-ball dome on the nose, forest green body with cream white side panels and a bright orange stripe, polished silver bumpers and trim, chunky charcoal tyres with silver hubcaps, a small chrome flagpole antenna with an orange pennant behind the seat, smooth cel-shaded hand-painted style, friendly chunky proportions, 3/4 front view from slightly above, whole vehicle in frame, plain light grey background, no driver, no text, no logos"*
2. `TeeShot_Boat.fbx` — *"Stylized cartoon speedboat, the same compact rounded design as a toy golf-cart roadster turned into a boat: clear bubble canopy over a single seat, a dimpled white golf-ball dome on the bow, forest green hull with cream white upper panels and a bright orange stripe, polished silver trim, small chrome outboard motor at the back, a small chrome flagpole antenna with an orange pennant behind the seat, smooth cel-shaded hand-painted style, chunky proportions, 3/4 front view from slightly above, whole boat in frame, out of the water, plain light grey background, no driver, no text, no logos"*
3. `TeeShot_Plane.fbx` — *"Stylized cartoon single-seat propeller plane, the same compact rounded design as a toy golf-cart roadster turned into a plane: clear bubble canopy, a dimpled white golf-ball dome as the nose spinner with a silver propeller, short rounded forest green wings with cream white tips, bright orange stripe along the body, small rounded tail fin with an orange pennant shape, polished silver trim, smooth cel-shaded hand-painted style, chunky proportions, 3/4 front view from slightly above, whole plane in frame, plain light grey background, no pilot, no text, no logos"*
4. `TeeShot_Sub.fbx` — *"Stylized cartoon single-seat mini submarine, the same compact rounded design as a toy golf-cart roadster turned into a submarine: clear bubble canopy, a dimpled white golf-ball dome on the nose with two small round headlights, forest green rounded hull with cream white panels and a bright orange stripe, small silver dive fins on the sides, a ring-shrouded silver propeller at the back, a small chrome periscope antenna with an orange pennant, smooth cel-shaded hand-painted style, chunky proportions, 3/4 front view from slightly above, whole submarine in frame, plain light grey background, no driver, no text, no logos"*

### Direction 2 — "Jetstream" (1950s jet-age streamliner)
*Design note:* the fastest-looking: long low teardrop body, pointed chrome nose, a **pair of swept tail fins with orange tips** (the
signature) and a clear teardrop canopy. Retro-futuristic like a 1950s toy rocket car. Risk: less "golf".

5. `Jetstream_Car.fbx` — *"Stylized cartoon retro-futuristic racing car, 1950s jet-age streamliner, long low teardrop body with a pointed polished chrome nose, clear teardrop canopy over a single seat, a pair of swept tail fins with bright orange tips at the back, forest green body with a cream white stripe running nose to tail, polished silver trim and round exhaust nozzle, low charcoal tyres half covered by smooth fender skirts, smooth cel-shaded hand-painted style, 3/4 front view from slightly above, whole vehicle in frame, plain light grey background, no driver, no text, no logos"*
6. `Jetstream_Boat.fbx` — *"Stylized cartoon retro-futuristic speedboat, 1950s jet-age streamliner turned into a hydroplane boat: long low teardrop hull with a pointed polished chrome bow, clear teardrop canopy over a single seat, a pair of swept tail fins with bright orange tips at the back, forest green hull with a cream white stripe nose to tail, two small silver side sponsons, round silver jet nozzle at the stern, smooth cel-shaded hand-painted style, 3/4 front view from slightly above, whole boat in frame, out of the water, plain light grey background, no driver, no text, no logos"*
7. `Jetstream_Plane.fbx` — *"Stylized cartoon retro-futuristic single-seat jet plane, 1950s jet-age streamliner turned into a plane: long teardrop body with a pointed polished chrome nose, clear teardrop canopy, swept-back forest green wings with cream white leading edges, a pair of swept tail fins with bright orange tips, cream white stripe nose to tail, round silver jet nozzle at the back, smooth cel-shaded hand-painted style, 3/4 front view from slightly above, whole plane in frame, plain light grey background, no pilot, no text, no logos"*
8. `Jetstream_Sub.fbx` — *"Stylized cartoon retro-futuristic single-seat mini submarine, 1950s jet-age streamliner turned into a submarine: long teardrop hull with a pointed polished chrome nose and a round headlight, clear teardrop canopy, a pair of swept tail fins with bright orange tips, short silver dive planes on the sides, forest green hull with a cream white stripe nose to tail, ring-shrouded silver propeller at the back, smooth cel-shaded hand-painted style, 3/4 front view from slightly above, whole submarine in frame, plain light grey background, no driver, no text, no logos"*

### Direction 3 — "Mudlark" (chunky amphibious rally buggy)
*Design note:* the toughest-looking: a tall rounded buggy with a **silver roll-hoop over the open cockpit and a tall snorkel on the
right side** (the signature), round headlights, big rounded bumpers. Like a toy that really could go anywhere. Risk: looks heavy.

9. `Mudlark_Car.fbx` — *"Stylized cartoon chunky rally buggy, tall rounded single-seat amphibious off-roader, open cockpit under a polished silver roll hoop, a tall silver snorkel rising on the right side, two big round headlights, big rounded cream white bumpers, forest green body with cream white panels and a bright orange stripe across the bonnet, oversized chunky charcoal tyres with silver hubs, smooth cel-shaded hand-painted style, friendly toy-like proportions, 3/4 front view from slightly above, whole vehicle in frame, plain light grey background, no driver, no text, no logos"*
10. `Mudlark_Boat.fbx` — *"Stylized cartoon chunky amphibious boat, tall rounded single-seat rally buggy turned into a boat: open cockpit under a polished silver roll hoop, a tall silver snorkel on the right side, two big round headlights, big rounded cream white bumper as a bow fender, forest green rounded hull with cream white panels and a bright orange stripe, a silver water-jet nozzle at the back, smooth cel-shaded hand-painted style, toy-like proportions, 3/4 front view from slightly above, whole boat in frame, out of the water, plain light grey background, no driver, no text, no logos"*
11. `Mudlark_Plane.fbx` — *"Stylized cartoon chunky single-seat bush plane, tall rounded rally buggy turned into a plane: open cockpit under a polished silver roll hoop with a small windscreen, a tall silver snorkel on the right side, two big round headlights, a silver propeller on the nose, short thick forest green wings with cream white tips, bright orange stripe, chunky rounded tail, smooth cel-shaded hand-painted style, toy-like proportions, 3/4 front view from slightly above, whole plane in frame, plain light grey background, no pilot, no text, no logos"*
12. `Mudlark_Sub.fbx` — *"Stylized cartoon chunky single-seat mini submarine, tall rounded rally buggy turned into a submarine: cockpit sealed under a clear dome inside a polished silver roll hoop, a tall silver snorkel on the right side, two big round headlights, big rounded cream white bumper, forest green rounded hull with cream white panels and a bright orange stripe, short silver dive fins, ring-shrouded silver propeller at the back, smooth cel-shaded hand-painted style, toy-like proportions, 3/4 front view from slightly above, whole submarine in frame, plain light grey background, no driver, no text, no logos"*

**Animations:** none (the vehicles are rigid; Abi keeps her Mixamo clips).

## Round 2 — shared driver animations (Mixamo) — received, in use

Dan saved all five, plus a bonus **Honking Horn** (played at GO), in `Assets/Resources/Drivers/Animations/` as `Driving.fbx`,
`Sitting Idle.fbx`, `Sitting Thumbs Up.fbx`, `Sitting Clap.fbx`, `Sitting Disapproval.fbx`, `Honking Horn.fbx` (Mixamo's own names;
the code loads these names; Y Bot, FBX for Unity, without skin, 30 fps). `Driver.cs` blends them in a playable graph; the upper body
follows the clips, the legs are held forward in the racing seat and the hips pinned to the seat.
