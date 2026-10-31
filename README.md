# Can 'Em All Together

> **Work in progress.** Player 2 is playable, but the mod is not feature
> complete and the game has single-player assumptions that are still being
> patched.

A local co-op mod for *Can 'Em All* (Unity 6, IL2CPP). It adds a second,
gamepad-controlled player to the single-player game and renders both players on
screen at the same time.

## What it does

- Press `F1` in a level to spawn **player 2** next to player 1 and show both
  views at once.
- Player 2 is a full clone of the player rig: own camera, inventory, weapons,
  vacuum and bullet counter.
- Player 1 keeps keyboard and mouse. Player 2 plays on an XInput gamepad.
- Three display layouts:
  - `Vertical`: left and right halves on one screen.
  - `Horizontal`: top and bottom halves on one screen.
  - `SeparateDisplays` (default): player 2 full screen on a second monitor,
    with their own HUD including a separate vacuum bar. Falls back to
    `Vertical` if only one monitor is detected.
- Press `F1` again to remove player 2 and restore the original single view.

## Player 2 controls (gamepad)

| Input          | Action                |
|----------------|-----------------------|
| Left stick     | Move                  |
| Right stick    | Look                  |
| Left stick click | Sprint              |
| Right trigger  | Primary fire (left mouse)  |
| Left trigger   | Secondary (right mouse)    |
| A              | Jump (`Space`)        |
| X              | Interact (`E`)        |
| Y              | `F` action            |
| Right bumper   | Next weapon           |
| Left bumper    | Previous weapon       |

Toggle key, layout, gamepad slot and look speed are configurable through
BepInEx's config file.

## How it works

The game was never designed for two players, so the mod works by making the
existing single-player code run twice.

1. **BepInEx plugin.** A BepInEx 6 IL2CPP plugin loads with the game and adds a
   persistent controller object that listens for the toggle key.
2. **Rig cloning.** On toggle, the player, camera, hand and inventory objects
   are instantiated together as one inactive copy, so their internal references
   stay intact, then activated beside player 1.
3. **Player context.** Many game components are singletons that assume one
   player. Harmony patches wrap their Unity messages (`Awake`, `Update`, ...) so
   code owned by player 2 runs "as player 2" and the singletons point to the
   right rig for the duration of the call.
4. **Input routing.** Harmony patches on Unity's `Input` calls return keyboard
   and mouse state to player 1 and translate the XInput gamepad into the same
   keys, axes and mouse buttons for player 2.
5. **Screen split.** The controller sets each camera's viewport rectangle (or
   moves player 2's camera to a second display) and re-applies it every frame,
   since game scripts can reset it.
6. **Player 2 HUD.** On a second display, the player 1 HUD canvas is copied and
   bound to player 2's bullet counter and vacuum bar.

Build, install and configuration details are in [INSTRUCTIONS.md](INSTRUCTIONS.md).

## Tech

C# / .NET 6, BepInEx 6 (IL2CPP), Harmony, Il2CppInterop, XInput P/Invoke.

## Known limitations

- Local split screen only; no network multiplayer.
- Scoring, spawning, UI and scene transitions may still assume one player.
- A dedicated player 2 HUD exists only with the `SeparateDisplays` layout.
- Game updates can break the patches.
