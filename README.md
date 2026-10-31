# Can 'Em All Split Screen Mod starter

This is a **BepInEx 6 IL2CPP** starter for the installed Unity 6 build of
Can 'Em All. It is intentionally only a first milestone: it creates a second
camera and divides the screen when `F1` is pressed. It does not yet duplicate
the game's player, gameplay state, UI, or input.

## What was detected

- Unity `6000.2.15f1`
- IL2CPP (`GameAssembly.dll` and `Can 'Em All_Data\il2cpp_data\Metadata\global-metadata.dat`)
- 64-bit Windows player
- No official mod loader or managed `Assembly-CSharp.dll` is currently installed

## Setup

1. Make a backup of the game directory.
2. Install the current **BepInEx 6 IL2CPP x64** build into the game directory:
   <https://github.com/BepInEx/BepInEx>
3. Launch the game once and close it. BepInEx should create `BepInEx\interop`.
4. Install the official BepInEx templates and create a test project if the
   loader's assembly layout differs from this starter:

   ```powershell
   dotnet new install BepInEx.Templates
   dotnet new list BepInEx
   ```

   Select the Unity IL2CPP plugin template shown by `dotnet new list`.
   Copy this starter's `Plugin.cs` into that generated project.
5. Build from this directory:

   ```powershell
   dotnet build -c Release -p:GameDirectory="H:\SteamLibrary\steamapps\common\Can 'Em All"
   ```

6. Copy the resulting
   `bin\Release\CanEmAll.SplitScreenMod.dll` to:
   `BepInEx\plugins\CanEmAll.SplitScreenMod\`
7. Launch the game and check `BepInEx\LogOutput.log`.
8. Press `F1` in a scene with a camera.

The project references the BepInEx and Unity assemblies generated/installed in
the game directory, so no BepInEx NuGet package restore is required. If the
loader uses a different assembly layout, the official template is
authoritative:
<https://github.com/BepInEx/BepInEx.Templates>

## Development order

1. Use a decompiler/IL2CPP interop tooling to inspect generated types after
   BepInEx creates `BepInEx\interop`.
2. Identify the scene's player object, movement component, camera-follow logic,
   and game-state manager.
3. Patch the player-spawn or scene-start method rather than cloning arbitrary
   objects from `Update`.
4. Give player 2 an independent input source. If the game uses the legacy
   input API, map a second keyboard/controller profile; if it uses the new
   Input System, create a separate `PlayerInput` user/device pairing.
5. Add split-screen UI and pause/game-over rules after two players can move.

The camera code uses the standard Unity viewport rectangles: player 1 is
`(0, 0, 0.5, 1)` and player 2 is `(0.5, 0, 0.5, 1)`. It is a diagnostic
milestone, not a complete multiplayer implementation.

## Important limitations

- Do not overwrite the original game binaries.
- Updates can invalidate interop types and patches.
- This is local split-screen only; it does not add network multiplayer.
- The game may have single-player assumptions in scoring, spawning, UI, and
  scene transitions that require additional patches.
