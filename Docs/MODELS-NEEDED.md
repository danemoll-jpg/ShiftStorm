# ShiftStorm — models and animations needed from Dan

Keep each round's list short; each item is Dan's time.

**Status (2026-11-13): received and in use.** Dan saved all five, plus a bonus **Honking Horn** (played at GO), in
`Assets/Resources/Drivers/Animations/` as `Driving.fbx`, `Sitting Idle.fbx`, `Sitting Thumbs Up.fbx`, `Sitting Clap.fbx`,
`Sitting Disapproval.fbx`, `Honking Horn.fbx` (Mixamo's own names; the code loads these names). The table below
was the request. Nothing is waiting on Dan right now.

## Round 2 — shared driver animations (Mixamo), first used by Abi

These clips are **shared by every driver**: Abi now, and any other driver later. ShiftStorm imports them as Humanoid
clips, and Unity retargets Humanoid clips onto any Humanoid character. Nothing in these clips belongs to one driver.

**Character to download them on:** Mixamo's default **Y Bot**. This keeps them neutral, and you don't need to upload
anyone. (Abi's own Mixamo character also works, since she's already rigged with `mixamorig:` bones, but it isn't needed.)

**Animations (5).** Use these Mixamo search names. If a name doesn't come up, search `sitting` and pick the closest one.

| # | Mixamo search name | What it's for | Save as |
|---|---|---|---|
| 1 | **Driving** | Main loop: seated, hands on the wheel, steering. Blended with the steering input. | `Driving.fbx` |
| 2 | **Sitting Idle** | Waiting on the grid during the countdown, and on the results screen. | `SittingIdle.fbx` |
| 3 | **Sitting Thumbs Up** | Short reaction at each shape change (wings out, splashdown, dive, surfacing, wheels down). | `SittingThumbsUp.fbx` |
| 4 | **Sitting Clap** | Finish celebration, with the driver's win line. | `SittingClap.fbx` |
| 5 | **Sitting Disapproval** | After a reset, with the driver's grumble line. | `SittingDisapproval.fbx` |

**Download settings (each clip):**
- Format: **FBX for Unity (.fbx)**
- Skin: **Without Skin** (skeleton and animation only, no mesh)
- Frames per second: **30**; Keyframe Reduction: **none**
- Leave the other options at their defaults.

**Save to:** `C:\Users\danmo\ShiftStorm\Assets\Resources\Drivers\Animations\` using the file names above.
(This shared folder sits next to each driver's own folder, e.g. `Resources\Driver\` for Abi's model and texture today.
Create the `Animations` folder if it isn't there.)

**What Code did with them (Round 2):** `Driver.cs` blends them in a playable graph; the upper body follows the clips, the legs are
held forward in the racing seat and the hips pinned to the seat (the sitting clips assume a chair). Originally planned: imports them as Humanoid clips, gives every driver the same animator (Driving
and Sitting Idle loops plus the three reactions as one-shots), and keeps each driver's own voice lines per character.
Until then Abi holds the fixed seated pose Code builds by aiming her Humanoid bones (Round 2): she leans into the
steering, turns her head and throws her arms up at the finish.
