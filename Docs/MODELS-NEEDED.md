# ShiftStorm — models and animations needed from Dan

Keep each round's list short; each item is Dan's time.

## Round 10 — Coaster Coast: three set-piece creatures (Tripo, by hand) — optional, stand-ins in the game now

Coaster Coast runs with primitive stand-ins for these three (they already move: the balloon bobs, the whale and the octopus are
placed and animated by code). A model only replaces the stand-in's look. Tee'd Off toon style (chunky shapes, flat bright colours,
soft cartoon proportions, like Tee'd Off's characters), **one full-body image each, front 3/4 view, plain light background, no
cropping, no props, no text**; Tripo: **Image to Model, textured, low-poly (~20k faces), GLB**. No rig needed (Code animates them
by parts: the octopus's arms are separated in Blender by Code).

1. **Mascot balloon** — `Art/Incoming/SetPieces/MascotBalloon/MascotBalloon.glb`. *"A giant parade balloon of a cheerful cartoon
   seagull mascot wearing a navy-and-white sailor's cap and a red neckerchief, wings spread wide as if waving, big friendly eyes,
   yellow beak and feet, inflatable vinyl look with visible seams and a soft shine, Tee'd Off cartoon style, full body, front 3/4
   view, plain light grey background, no strings, no text."* Size in game: 30 m tall over the flight.
2. **Giant octopus** — `Art/Incoming/SetPieces/Octopus/Octopus.glb`. *"A giant friendly cartoon octopus, coral red with pale pink
   suckers, big round curious eyes, a playful smile, its eight arms spread out flat around it in a star shape (so each arm is
   separate), Tee'd Off cartoon style, full body, seen from the front and slightly above, plain light grey background, no props."*
   Size in game: 30 m across, on the sea floor by the dive line.
3. **Whale** — `Art/Incoming/SetPieces/Whale/Whale.glb`. *"A big friendly cartoon humpback whale, slate blue back and a pale
   striped belly, long white flippers, a gentle smile and small kind eyes, tail flukes level, Tee'd Off cartoon style, full body,
   side 3/4 view, plain light grey background, no water, no props."* Size in game: 26 m long, surfacing beside the boat.

After the download: say so and Code cleans them in Blender (scale, origin, facing, LOD), swaps them in and keeps the stand-ins as
the fallback. Animations: none (Mixamo is for people).

## Round 9 — three new vehicle designs (Dan's concepts, Tripo by hand)

**Status (2026-11-15): all 12 models in the game** (Grand Prix -> Rae, Hot Rod -> Vivi, Scrambler -> Chip; Garage pictures in
`Docs/Checks/round9/`). Earlier: **all 12 images done** (Dan's 3 originals + the 9 transformations), saved in
`Art/Concepts/Vehicles/{HotRod,Scrambler,GrandPrix}/<Design>_<Form>.png`. Next: Dan makes the Tripo models. Note for Code: the
Hot Rod has a small chrome T-handle in its cockpit in every form — hide or replace it with the game's own steering mechanism.

Earlier: Dan drew one form of each design. **How Dan uses these prompts:** paste the prompt into the image
generator **together with that design's original image** as the reference; each prompt says the new image is the same vehicle
transformed and must match the original's style as closely as possible. Then the new image goes to Tripo (Image to Model,
textured, FBX for Blender, 4K textures), saved to `Art/Incoming/Vehicles/<Design>/<Design>_<Form>.fbx` (or the Tripo zip).
Dan is conservative with Tripo credits: these 9 + the raptor and long-neck (Round 8) are the list. Open cockpit, **no glass**
(Code adds glass in the game). Suggested owners (Dan to confirm): **Hot Rod → Vivi**, **Grand Prix → Rae**, **Scrambler → Chip**.

### Design A — "Hot Rod" (reference: Dan's lavender **sub** image, saved as `HotRod_Sub`)
1. `HotRod_Car` — *"Transform the vehicle in the attached image into a hot rod car. It is the same vehicle, transformed: match the attached image's art style, shading, line work, colours, materials and details as closely as possible, and keep its signature features (the twin headlights in chrome oval pods, the exposed chrome engine with velocity stacks, the swept purple tail fin with the cream panel, the cream tufted seat and chrome cockpit rim). Give it fat black tyres with chrome hubcaps on all four wheels; remove the propeller and dive fins; keep the open single-seat cockpit. Same 3/4 front view from slightly above as the attached image, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos."*
2. `HotRod_Boat` — *"Transform the vehicle in the attached image into a speedboat. It is the same vehicle, transformed: match the attached image's art style, shading, line work, colours, materials and details as closely as possible, and keep its signature features (the twin headlights in chrome oval pods, the exposed chrome engine with velocity stacks, the swept purple tail fin with the cream panel, the cream tufted seat and chrome cockpit rim). Give it a hull with a V-shaped keel underneath and a chrome propeller and rudder at the stern instead of the shrouded propeller; shown out of the water; keep the open single-seat cockpit. Same 3/4 front view from slightly above as the attached image, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos."*
3. `HotRod_Plane` — *"Transform the vehicle in the attached image into a single-seat plane. It is the same vehicle, transformed: match the attached image's art style, shading, line work, colours, materials and details as closely as possible, and keep its signature features (the twin headlights in chrome oval pods, the exposed chrome engine with velocity stacks, the swept purple tail fin with the cream panel, the cream tufted seat and chrome cockpit rim). Replace the small dive fins with short swept lavender wings with cream tips and put twin chrome jet exhausts at the back instead of the propeller; keep the open single-seat cockpit. Same 3/4 front view from slightly above as the attached image, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos."*

### Design B — "Scrambler" (reference: Dan's motorcycle image, saved as `Scrambler_Car`)
The **sub** is surrounded by a **glass sphere** (Dan) — Tripo can't do glass, so the prompt asks for a chrome ring to mount
it on and **Code adds the clear sphere in the game**.
4. `Scrambler_Boat` — *"Transform the vehicle in the attached image into a jet ski (personal watercraft). It is the same vehicle, transformed: match the attached image's art style, shading, line work, colours, materials and details as closely as possible, and keep its signature features (the cream body panels, orange accents and wheel rims, the round headlight pod, the orange seat, the charcoal finned engine and the handlebars with orange grips). Replace the wheels with a rounded cream hull with orange stripes and a water-jet nozzle at the back; no kickstand; shown out of the water. Same 3/4 front view from slightly above as the attached image, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos."*
5. `Scrambler_Plane` — *"Transform the vehicle in the attached image into a single-seat flying motorbike. It is the same vehicle, transformed: match the attached image's art style, shading, line work, colours, materials and details as closely as possible, and keep its signature features (the cream body panels, orange accents and wheel rims, the round headlight pod, the orange seat, the charcoal finned engine and the handlebars with orange grips). The wheels fold flat sideways into orange-rimmed rotor discs, add short cream wings with orange tips, and turn the rear exhaust pod into a jet with a glowing nozzle; no kickstand. Same 3/4 front view from slightly above as the attached image, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos."*
6. `Scrambler_Sub` — *"Transform the vehicle in the attached image into an underwater sea scooter (a mini submarine scooter). It is the same vehicle, transformed: match the attached image's art style, shading, line work, colours, materials and details as closely as possible, and keep its signature features (the cream body panels, orange accents and wheel rims, the round headlight pod, the orange seat, the charcoal finned engine and the handlebars with orange grips). Give it a rounded cream torpedo body instead of wheels, small orange dive fins, a ring-shrouded propeller at the back, and a wide polished chrome ring around the seat area at mid height (a glass bubble will be added later, so don't draw any glass); no kickstand. Same 3/4 front view from slightly above as the attached image, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos."*

### Design C — "Grand Prix" (reference: Dan's maroon racer image, saved as `GrandPrix_Car`)
7. `GrandPrix_Boat` — *"Transform the vehicle in the attached image into a 1930s racing speedboat. It is the same vehicle, transformed: match the attached image's art style, shading, line work, colours, materials and details as closely as possible, and keep its signature features (the deep maroon and cream body, brass trim lines, brown leather straps, the vertical chrome grille, brass-ringed round headlights, the chrome side exhaust with holes and the tan tufted leather seat). Replace the wheels with a long cigar-shaped hull and put a brass propeller and rudder at the stern; shown out of the water; keep the open single-seat cockpit. Same 3/4 front view from slightly above as the attached image, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos."*
8. `GrandPrix_Plane` — *"Transform the vehicle in the attached image into a stubby 1930s single-seat air racer. It is the same vehicle, transformed: match the attached image's art style, shading, line work, colours, materials and details as closely as possible, and keep its signature features (the deep maroon and cream body, brass trim lines, brown leather straps, the vertical chrome grille, brass-ringed round headlights, the chrome side exhaust with holes and the tan tufted leather seat). Replace the wheels with short maroon wings with cream tips, put a brass propeller on the nose in front of the grille, keep the small swept tail fin and the open single-seat cockpit. Same 3/4 front view from slightly above as the attached image, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos."*
9. `GrandPrix_Sub` — *"Transform the vehicle in the attached image into a 1930s mini submarine. It is the same vehicle, transformed: match the attached image's art style, shading, line work, colours, materials and details as closely as possible, and keep its signature features (the deep maroon and cream body, brass trim lines, brown leather straps, the vertical chrome grille, brass-ringed round headlights, the chrome side exhaust with holes and the tan tufted leather seat). Replace the wheels with small brass dive fins, add rivets and a brass ring-shrouded propeller at the back, and use the side exhaust as a snorkel; keep the open single-seat cockpit. Same 3/4 front view from slightly above as the attached image, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos."*

## Round 8 — two dinosaurs Tee'd Off doesn't have (Tripo, by hand) — optional, stand-ins in the game now

**Status (2026-11-15): both in the game** (Dan's Tripo models, rigged by `Tools/Characters/import_dinos.py`; the stand-ins stay
only as a fallback). Earlier: **images done** — `Art/Concepts/Dinos/Raptor.png` and `Art/Concepts/Dinos/Plesiosaur.png`
(Dan's). Dan is making the Tripo models now. Earlier status: written, not made. Jurassic Par's other dinosaurs (brontosaurus, T. rex, triceratops, pterodactyl) are Tee'd
Off's rigged models, now animated in the race. Two the course wants have no model anywhere: the game uses stand-ins meanwhile (the
raptor is the T. rex at 2.6 m tall; the plesiosaur is a brontosaurus swimming with its body under water). Make these only if the
stand-ins read badly in the race; nothing waits on them.

**What Code does with them:** clean up in Blender (size, origin at the feet / belly, facing +Z, LOD), rig with Tee'd Off's
`rig_dino.py`-style bones (Code writes the bones; no Mixamo: Mixamo's rigs are humanoid only), animate in code like the others
(walk cycle, tail, neck, head), replace the stand-ins, keep the stand-ins as the fallback.

### Shared for both images
- Style: Tee'd Off's Jurassic Par dinosaurs (`Tee-dOff/Art/Characters/concepts/dinos/*.png`): chunky, friendly cartoon proportions,
  big expressive eyes, smooth hand-painted cel-shaded colours, soft rounded shapes, no scars, no gore, no realistic scales.
- **Full body, side view (left side) plus a 3/4 front view**, standing in a neutral pose (legs straight under the body, tail
  straight out behind, mouth closed, eyes open), **plain light background**, the whole animal in frame with a margin, **no props, no
  ground, no shadow, no text**.

### 1. Raptor — `Art/Incoming/Dinos/Raptor/Raptor.glb`
- Prompt: *"Cartoon velociraptor for a family racing game, Tee'd Off toon style: chunky friendly proportions, big round eyes with a
  cheeky grin (mouth closed), short arms with three blunt claws, long straight tail held out level, strong running legs, feathered
  crest on the head and a few feather tufts on the arms, warm orange body with dark brown tiger stripes on the back and a cream
  belly, hand-painted cel-shaded colours, smooth surfaces, full body, standing neutral pose, side view, plain light grey
  background, no props, no ground, no text."*
- Design note: they run alongside the road in a pack of four; they must read at 30-60 m and at speed, so a bold stripe pattern and a
  clear silhouette (crest, tail) matter more than detail. About 2.5 m tall, 4.5 m long.
- Tripo: **Image to Model**, textured, **low-poly / standard** (not detailed: four of them on screen), **GLB**, about 15-20k faces.
- Animations: none from Mixamo (code animates the legs, tail, head).

### 2. Plesiosaur — `Art/Incoming/Dinos/Plesiosaur/Plesiosaur.glb`
- Prompt: *"Cartoon plesiosaur for a family racing game, Tee'd Off toon style: a round friendly body, a very long graceful neck
  with a small smiling head and big gentle eyes, four large paddle flippers, a short tapering tail, teal-blue back fading to a
  pale cream belly with soft darker spots along the back, hand-painted cel-shaded colours, smooth surfaces, full body, neutral pose
  with the neck raised in a gentle S curve and the flippers out to the sides, side view, plain light grey background, no water, no
  props, no text."*
- Design note: it swims in the Lagoon Landing with its body under the surface and its neck up (seen from the boat at 60-120 m),
  so the neck and head carry it. About 14 m long.
- Tripo: **Image to Model**, textured, **standard**, **GLB**, about 20k faces.
- Animations: none from Mixamo (code paddles the flippers and sways the neck).

## Round 5 — Abi's transforming machine, stage 1: three design directions (Tripo, by hand)

**Status (2026-11-13): DECIDED — Dan picked Direction 2, "Jetstream".** He made the four Jetstream images (car, boat, plane,
sub) and is modelling them in Tripo now. Directions 1 and 3 (Tee Shot, Mudlark) are **parked, not dropped**: their prompts stay below for later
(e.g. as rival drivers' own machines); don't build or show them this round. Rivals use Jetstream in their own colours.

**What Code does with them:** `python Tools/import_vehicles.py` (one command) cleans each FBX in Blender (scale to 4.0 m long,
origin at the hull centre, nose to +Z, wheels / waterline on the hull's floor, seat point, two LODs), copies it into
`Assets/Resources/Vehicles/<Design>/` and the game shows it in the Garage and can race it. The change between forms is a 0.6 s
swap with an effect (squash, a spin of panels, sparkles / spray / bubbles), so each form is one solid model — **no moving parts,
no separate wheels needed**. Rivals get recoloured copies (the green parts are recoloured), so keep green as the main body colour.

### Shared for all 12 images
- Colours (from Abi's Tee'd Off outfit): **forest / kelly green** main body, **cream white** panels and stripes, **polished silver**
  trim, a **bright orange** accent (one stripe, the intakes or the fin tips), dark charcoal tyres / underside. (No glass: see below.)
- Style: Tee'd Off toon look: chunky, friendly proportions, slightly oversized cockpit, smooth clean surfaces, soft bevels,
  hand-painted cel-shaded colours, no grime, no realistic decals.
- Every image: **3/4 front view** (from front-left, slightly above), **plain light grey background**, the whole vehicle in frame with
  a little margin, **no driver, no people, no text, no logos, no numbers, no props, no ground shadow clutter**.
- Same machine in all four forms: the **same cockpit shape and rim**, the same colour layout and the same signature detail
  (named per direction below). Seat one person in an **open cockpit: no canopy, no windscreen, no glass anywhere** (Dan, 2026-11-13: Tripo bakes glass in as
  an opaque, often wrong surface). Code adds a separate clear canopy / windscreen in the game where a form needs one (the sub dives
  with it closed), shaped to the cockpit rim.

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
*Design note:* the friendliest of the three, straight out of a golf course: an open cockpit with a rounded rim, a **dimpled white golf-ball dome**
on the nose (the signature), a little chrome flagpole antenna with an orange pennant behind the seat, rounded fenders. Reads instantly
as Tee'd Off. Risk: may look slow.

1. `TeeShot_Car.fbx` — *"Stylized cartoon toy-like racing car, compact rounded roadster with an open single-seat cockpit with a rounded rim, a dimpled white golf-ball dome on the nose, forest green body with cream white side panels and a bright orange stripe, polished silver bumpers and trim, chunky charcoal tyres with silver hubcaps, a small chrome flagpole antenna with an orange pennant behind the seat, smooth cel-shaded hand-painted style, friendly chunky proportions, 3/4 front view from slightly above, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos"*
2. `TeeShot_Boat.fbx` — *"Stylized cartoon speedboat, the same compact rounded design as a toy golf-cart roadster turned into a boat: open single-seat cockpit with a rounded rim, a dimpled white golf-ball dome on the bow, forest green hull with cream white upper panels and a bright orange stripe, polished silver trim, small chrome outboard motor at the back, a small chrome flagpole antenna with an orange pennant behind the seat, smooth cel-shaded hand-painted style, chunky proportions, 3/4 front view from slightly above, whole boat in frame, out of the water, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos"*
3. `TeeShot_Plane.fbx` — *"Stylized cartoon single-seat propeller plane, the same compact rounded design as a toy golf-cart roadster turned into a plane: open single-seat cockpit with a rounded rim, a dimpled white golf-ball dome as the nose spinner with a silver propeller, short rounded forest green wings with cream white tips, bright orange stripe along the body, small rounded tail fin with an orange pennant shape, polished silver trim, smooth cel-shaded hand-painted style, chunky proportions, 3/4 front view from slightly above, whole plane in frame, plain light grey background, no pilot, no canopy, no glass, no windscreen, no text, no logos"*
4. `TeeShot_Sub.fbx` — *"Stylized cartoon single-seat mini submarine, the same compact rounded design as a toy golf-cart roadster turned into a submarine: open single-seat cockpit with a rounded rim, a dimpled white golf-ball dome on the nose with two small round headlights, forest green rounded hull with cream white panels and a bright orange stripe, small silver dive fins on the sides, a ring-shrouded silver propeller at the back, a small chrome periscope antenna with an orange pennant, smooth cel-shaded hand-painted style, chunky proportions, 3/4 front view from slightly above, whole submarine in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos"*

### Direction 2 — "Jetstream" (1950s jet-age streamliner)
*Design note:* the fastest-looking: long low teardrop body, pointed chrome nose, a **pair of swept tail fins with orange tips** (the
signature) and an open cockpit with a low rounded rim. Retro-futuristic like a 1950s toy rocket car. Risk: less "golf".

5. `Jetstream_Car.fbx` — *"Stylized cartoon retro-futuristic racing car, 1950s jet-age streamliner, long low teardrop body with a pointed polished chrome nose, open single-seat cockpit with a low rounded rim, a pair of swept tail fins with bright orange tips at the back, forest green body with a cream white stripe running nose to tail, polished silver trim and round exhaust nozzle, low charcoal tyres half covered by smooth fender skirts, smooth cel-shaded hand-painted style, 3/4 front view from slightly above, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos"*
6. `Jetstream_Boat.fbx` — *"Stylized cartoon retro-futuristic speedboat, 1950s jet-age streamliner turned into a hydroplane boat: long low teardrop hull with a pointed polished chrome bow, open single-seat cockpit with a low rounded rim, a pair of swept tail fins with bright orange tips at the back, forest green hull with a cream white stripe nose to tail, two small silver side sponsons, round silver jet nozzle at the stern, smooth cel-shaded hand-painted style, 3/4 front view from slightly above, whole boat in frame, out of the water, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos"*
7. `Jetstream_Plane.fbx` — *"Stylized cartoon retro-futuristic single-seat jet plane, 1950s jet-age streamliner turned into a plane: long teardrop body with a pointed polished chrome nose, open single-seat cockpit with a low rounded rim, swept-back forest green wings with cream white leading edges, a pair of swept tail fins with bright orange tips, cream white stripe nose to tail, round silver jet nozzle at the back, smooth cel-shaded hand-painted style, 3/4 front view from slightly above, whole plane in frame, plain light grey background, no pilot, no canopy, no glass, no windscreen, no text, no logos"*
8. `Jetstream_Sub.fbx` — *"Stylized cartoon retro-futuristic single-seat mini submarine, 1950s jet-age streamliner turned into a submarine: long teardrop hull with a pointed polished chrome nose and a round headlight, open single-seat cockpit with a low rounded rim, a pair of swept tail fins with bright orange tips, short silver dive planes on the sides, forest green hull with a cream white stripe nose to tail, ring-shrouded silver propeller at the back, smooth cel-shaded hand-painted style, 3/4 front view from slightly above, whole submarine in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos"*

### Direction 3 — "Mudlark" (chunky amphibious rally buggy)
*Design note:* the toughest-looking: a tall rounded buggy with a **silver roll-hoop over the open cockpit and a tall snorkel on the
right side** (the signature), round headlights, big rounded bumpers. Like a toy that really could go anywhere. Risk: looks heavy.

9. `Mudlark_Car.fbx` — *"Stylized cartoon chunky rally buggy, tall rounded single-seat amphibious off-roader, open cockpit under a polished silver roll hoop, a tall silver snorkel rising on the right side, two big round headlights, big rounded cream white bumpers, forest green body with cream white panels and a bright orange stripe across the bonnet, oversized chunky charcoal tyres with silver hubs, smooth cel-shaded hand-painted style, friendly toy-like proportions, 3/4 front view from slightly above, whole vehicle in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos"*
10. `Mudlark_Boat.fbx` — *"Stylized cartoon chunky amphibious boat, tall rounded single-seat rally buggy turned into a boat: open cockpit under a polished silver roll hoop, a tall silver snorkel on the right side, two big round headlights, big rounded cream white bumper as a bow fender, forest green rounded hull with cream white panels and a bright orange stripe, a silver water-jet nozzle at the back, smooth cel-shaded hand-painted style, toy-like proportions, 3/4 front view from slightly above, whole boat in frame, out of the water, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos"*
11. `Mudlark_Plane.fbx` — *"Stylized cartoon chunky single-seat bush plane, tall rounded rally buggy turned into a plane: open cockpit under a polished silver roll hoop, a tall silver snorkel on the right side, two big round headlights, a silver propeller on the nose, short thick forest green wings with cream white tips, bright orange stripe, chunky rounded tail, smooth cel-shaded hand-painted style, toy-like proportions, 3/4 front view from slightly above, whole plane in frame, plain light grey background, no pilot, no canopy, no glass, no windscreen, no text, no logos"*
12. `Mudlark_Sub.fbx` — *"Stylized cartoon chunky single-seat mini submarine, tall rounded rally buggy turned into a submarine: open single-seat cockpit under a polished silver roll hoop, a tall silver snorkel on the right side, two big round headlights, big rounded cream white bumper, forest green rounded hull with cream white panels and a bright orange stripe, short silver dive fins, ring-shrouded silver propeller at the back, smooth cel-shaded hand-painted style, toy-like proportions, 3/4 front view from slightly above, whole submarine in frame, plain light grey background, no driver, no canopy, no glass, no windscreen, no text, no logos"*

**Animations:** none (the vehicles are rigid; Abi keeps her Mixamo clips).

## Round 2 — shared driver animations (Mixamo) — received, in use

Dan saved all five, plus a bonus **Honking Horn** (played at GO), in `Assets/Resources/Drivers/Animations/` as `Driving.fbx`,
`Sitting Idle.fbx`, `Sitting Thumbs Up.fbx`, `Sitting Clap.fbx`, `Sitting Disapproval.fbx`, `Honking Horn.fbx` (Mixamo's own names;
the code loads these names; Y Bot, FBX for Unity, without skin, 30 fps). `Driver.cs` blends them in a playable graph; the upper body
follows the clips, the legs are held forward in the racing seat and the hips pinned to the seat.
