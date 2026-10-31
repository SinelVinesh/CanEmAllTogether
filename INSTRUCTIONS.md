# Build and development instructions

Detailed setup for building and extending the Can 'Em All split-screen mod.
For an overview of the mod itself, see [README.md](README.md).

## Target game

- Unity `6000.2.15f1`
- IL2CPP (`GameAssembly.dll` and `Can 'Em All_Data\il2cpp_data\Metadata\global-metadata.dat`)
- 64-bit Windows player
- No official mod loader or managed `Assembly-CSharp.dll` ships with the game

## Setup

1. Make a backup of the game directory.
2. Install the current **BepInEx 6 IL2CPP x64** build into the game directory:
   <https://github.com/BepInEx/BepInEx>
3. Launch the game once and close it. BepInEx should create `BepInEx\interop`.
4. If the loader's assembly layout differs from this project's references,
   install the official BepInEx templates and compare against a generated project:

   ```powershell
   dotnet new install BepInEx.Templates
   dotnet new list BepInEx
   ```

   The official template is authoritative:
   <https://github.com/BepInEx/BepInEx.Templates>
5. Build from this directory:

   ```powershell
   dotnet build -c Release -p:GameDirectory="H:\SteamLibrary\steamapps\common\Can 'Em All"
   ```

   A Release build copies `CanEmAll.SplitScreenMod.dll` to
   `BepInEx\plugins\CanEmAll.SplitScreenMod\`. Close the game first, or the DLL
   is locked and the copy fails.
6. Launch the game and check `BepInEx\LogOutput.log`.
7. Load into a level and press `F1` to toggle split screen.

The project references the BepInEx and Unity assemblies generated in the game
directory, so no BepInEx NuGet package restore is required.

## Configuration

BepInEx writes the config file on first launch to
`BepInEx\config\com.example.canemall.splitscreen.cfg`.

| Section    | Key          | Default            | Purpose                                                      |
|------------|--------------|--------------------|--------------------------------------------------------------|
| Display    | Layout       | `SeparateDisplays` | `Vertical`, `Horizontal` or `SeparateDisplays`               |
| Controls   | ToggleKey    | `F1`               | Toggles split screen and player 2                            |
| Debug      | HudReportKey | `F2`               | Writes a HUD diagnostic report to the BepInEx log            |
| PlayerTwo  | GamepadIndex | `0`                | XInput slot (0-3) of player 2's gamepad                      |
| PlayerTwo  | LookSpeed    | `120`              | Right-stick look speed for player 2                          |

## Source layout

| Path                         | Role                                                              |
|------------------------------|-------------------------------------------------------------------|
| `Plugin.cs`                  | BepInEx entry point: config, patches, controller host object      |
| `SplitScreenController.cs`   | Toggle handling, viewport layout, attach/detach of player 2       |
| `SplitScreenLayout.cs`       | Viewport rectangles for each layout                               |
| `Patches/InputPatches.cs`    | Harmony patches routing `Input` calls to the active player        |
| `Patches/PlayerContextPatches.cs` | Runs player-owned Unity messages as their owner              |
| `PlayerTwo/`                 | Rig cloning, context switching, bindings, sprint, HUD copy        |
| `Gamepad/`                   | XInput P/Invoke wrapper and button/state tracking                 |
| `Diagnostics/HudReport.cs`   | HUD hierarchy dump used while developing the P2 HUD               |

## Development notes

1. Use a decompiler or the generated `BepInEx\interop` assemblies to inspect
   game types.
2. Patch game methods with Harmony rather than cloning arbitrary objects from
   `Update`.
3. Game updates can invalidate interop types and patches.
4. Do not overwrite the original game binaries.

## Known limitations

- Local split screen only; there is no network multiplayer.
- The game has single-player assumptions in scoring, spawning, UI and scene
  transitions that may need further patches.
- Player 2 only has a dedicated HUD on the `SeparateDisplays` layout.
