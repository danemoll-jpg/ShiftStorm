# ShiftStorm — complete download

This ZIP holds the launcher, the game, the cover art, and the icon and poster. The launcher keeps the game up to date: each time you open it, it checks for a newer version and installs it before the game starts, so you only install once.

ShiftStorm is still a prototype: Round 1 is a gray-box test strip and four practice lanes for the one vehicle that turns into a car, boat, plane and submarine.

## Windows

1. Extract the ZIP anywhere you can write to (for example `Documents\Games`). You get a `ShiftStorm` folder.
2. Run `ShiftStorm\ShiftStormLauncher.exe`.
3. Leave it alone: it checks for updates, downloads and installs a newer version if there is one (the window shows the progress), then starts the game. Without an internet connection, or if the check fails, the installed game starts.

B / Esc skips the update and plays the installed game at once. Any other button or key opens the launcher's menu instead: **Check for updates**, **Update game and play**, **Update soundtrack**, **Restore previous version**. D-pad / arrows choose, A / Enter confirms, B / Esc cancels.

## Steam Deck

1. Switch to **Desktop Mode**.
2. Download the ZIP and extract it anywhere in your home folder (for example `/home/deck/Games`), or on the SD card. You get a `ShiftStorm` folder.
3. In Steam: **Games → Add a Non-Steam Game to My Library → Browse**, pick `ShiftStorm/ShiftStormLauncher.exe`, then **Add Selected Programs**.
4. Right-click the new entry → **Properties → Compatibility** → tick **Force the use of a specific Steam Play compatibility tool** → **Proton Experimental**.
5. Optional: in the shortcut's **Properties**, click the icon square and pick `ShiftStorm/ShiftStorm-Icon.png`. For the library artwork, right-click the game in the Library (Desktop Mode) → **Manage → Set custom artwork** and pick `ShiftStorm/ShiftStorm-Poster.png`.
6. Back in Gaming Mode, play it from your library. The launcher updates the game by itself, then starts it. The game opens full screen at the Deck's 1280 x 800.

Keep using this same Steam shortcut from now on. Your settings live in that shortcut's Proton prefix; a new shortcut starts with a new, empty prefix.

## Controls (Round 1)

| | Controller | Keyboard |
|---|---|---|
| Accelerate | RT | W / Up |
| Brake / reverse | LT | S / Down |
| Steer (and bank in the air) | Left stick left / right | A / D, Left / Right |
| Climb / dive (air and underwater) | Left stick: pull back = up | I = up, K = down |
| Boost (one meter for every form) | A | Space |
| Drift (car) / tighter carve (boat) | RB (or X) | Left Shift |
| Reset to the last safe spot | Y | R |
| Invert climb / dive | View (Back) | F2 |
| Next / previous course | D-pad right / left | F4 / F3 |

The vehicle changes form by itself in the marked zones (a car, wing, boat or submarine sign comes before each one).

## Your files

- `versions`: the installed game, plus the previous one so you can go back. An update or a restore never removes your settings.
- `recovery.log`: what the launcher did, if something goes wrong.

Close the game before you install an update.
