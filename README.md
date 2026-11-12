# DiceKingdoms Cheats (`DiceModders.Cheats.dll`)

Hidden cheats for private / friendly lobbies. A mod for **Dice Kingdoms** (BepInEx 6 IL2CPP).

Several cheats change the simulation: use them only with everybody's consent.

## What it does

The cheats are **hidden until unlocked**: set `Unlocked = true` under `[Cheats]` in `BepInEx\config\org.dicemodders.dicekingdoms.cheats.cfg` (or with an in-game config editor). A **Cheats** block then appears in **Settings > Game**; its *Hide* button locks them again. While hidden, every cheat is off.

* **Developer console**: lets the game's own developer console work (`saveisland`, `loadisland` ...).
* **Unlock all buildings**: every building can be built without its technology.
* **Build anywhere (RISKY)**: the island placement checks are skipped. Placement is networked, so this can crash everybody in the lobby.
* **Meteor rules** are **lobby rules** under the *Cheats* heading of Settings > Lobby > Rules, shown while the cheats are unlocked: *Meteor hits castle* (any meteor may hit a castle), *Meteor limit T* (a castle hit is allowed with the chance *meteor tiles on the island / T*; 0 = the game's 128) and *Castle never hit*. Only the host edits them, before the match; they are frozen when it starts. Every player with this mod follows the host, because every computer simulates each meteor. A player without the mod would simulate a different meteor and desync; ModCore warns about that in the lobby.
* **Force ready** (host): Settings > Lobby > Mods. Every player counts as ready, so Start unlocks. It is not hidden.

The whole mod can be switched off in the mod list. The building and placement patches are only in the game while their cheat is on, so a locked or switched-off mod costs nothing.

## Code map

| File | Job |
|---|---|
| `Plugin.cs` | the plugin: binds the options, starts the features |
| `Options.cs` | every option (config entries) |
| `CheatSettings.cs` | the Settings blocks (Game > Cheats, Lobby > Mods) |
| `Patches.cs` | developer console, buildings, placement, force ready |
| `Meteor.cs` | the meteor lobby rules |

## Requirements

* Dice Kingdoms with **BepInEx 6 IL2CPP (x64)**: see the wiki's [Mods Installation](https://dicemodders.github.io/DiceKingdoms-Modding-Wiki/guides/mod-installation/) guide. Start the game once so BepInEx creates its folders.
* The DiceModders libraries in `BepInEx\plugins`: [ModCore](https://github.com/DiceModders/DiceKingdoms-ModCore) (`DiceModders.ModCore.dll`), [UiKit](https://github.com/DiceModders/UiKit) (`DiceModders.UiKit.dll`) and [ModBridge](https://github.com/DiceModders/DiceKingdoms-ModBridge) (`DiceModders.ModBridge.dll`, shows ModCore's mod list and lobby rules on the game's screens; without it the host has nothing to edit the meteor rules with). This mod does not contain copies of them.

## Install (players)

Copy `DiceModders.Cheats.dll` into `BepInEx\plugins`, next to the libraries.

## Build (developers)

Needs the .NET SDK (6 or newer) and PowerShell. Game files are never committed: the script links `BepInEx\interop` from your game folder and copies the libraries from `BepInEx\plugins` into `libs`.

```powershell
.\scripts\build.ps1 -GameDir "<your Dice-Kingdoms folder>"   # first time: remembers the folder
.\scripts\build.ps1                                           # build + install (Release, console and log OFF)
.\scripts\build.ps1 -Dev -Run                                 # Debug build, console and log ON, then start the game
```

Close the game before building: it locks the DLLs. If PowerShell refuses to run scripts: `powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1`.

## License

MIT, see [LICENSE](LICENSE).
