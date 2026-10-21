# DiceKingdoms Cheats (`DiceModders.Cheats.dll`)

Hidden cheats for private / friendly Dice Kingdoms lobbies. A mod for **Dice Kingdoms** (BepInEx 6 IL2CPP). It is one self-contained project: it shares no code with other mods.

## What it does

Cheats are **hidden until unlocked**: in the game's Settings screen click the bottom-left corner of the panel **5 times** within 4 seconds. A message says "Cheats unlocked" and a **Cheats** block appears in the **Game** tab (re-open the tab to see it). Right Ctrl + Insert does the same without the gesture. Repeating the gesture hides it again (`[Cheats] Unlocked` in the config). While hidden, the cheats and their hotkeys do nothing.

* **Unlock all buildings**, **Build anywhere (RISKY)** - toggles in the Cheats block. Build anywhere is networked and can crash everybody in the lobby.
* **Meteors** are **lobby rules** (since 3.0.0): *Meteors may hit the castle from the first fall* and the limit **T** (the chance that a meteor may hit the castle is *meteor tiles on the island / T*; 0 = the game's 128). Only the **host** edits them, in Settings > Lobby > Rules (a ModBridge feature) before the match; they are saved in the Steam lobby (ModCore) and frozen when the match starts. Every player who has this mod follows the host (also when their own cheats are still hidden), because every computer simulates each meteor. A player without the mod would simulate a different meteor and desync - ModCore's desync protection warns about it in the lobby.
* **Force ready** (host, untested) is *not* hidden: it is a tick in the lobby's match settings (right side, before the match starts).
* The game's own dev console can be unlocked with `[DevConsole] Unlock` (only after the cheats were unlocked).

## Hotkeys (hold Right Ctrl; configurable in the .cfg)

| Key | Action |
|---|---|
| Insert | unlock / hide the cheats |
| F7 | unlock all buildings |
| PageDown | build anywhere (RISKY) |
| Delete | force lobby ready |
| Home | print the hotkeys to the log |

## Code map

`ModPlugin.cs` (plugin, patches), `Cheat.cs` (building / placement / lobby cheats), `Meteor.cs` (castle protection + T as lobby rules), `Patches.cs`, `CheatsGate.cs` (the unlock), `CheatsUi.cs` (everything drawn, via UiKit), `Common/` (this mod's private copy of the small helper classes).

## Requirements

* Dice Kingdoms with **BepInEx 6 IL2CPP (x64)** - see the wiki's [Mods Installation](https://dicemodders.github.io/DiceKingdoms-Modding-Wiki/guides/mod-installation/) guide. Start the game once so BepInEx creates its folders.
* **[UiKit](https://github.com/DiceModders/UIKit_Another_Version)** (`DiceModders.UiKit.dll`) and **[ModCore](https://github.com/DiceModders/DiceKingdoms-ModCore)** (`DiceModders.ModCore.dll`) in `BepInEx\plugins` - separate plugins (interface; lobby and host rules). This mod does **not** contain a copy of them.
* **[ModBridge](https://github.com/DiceModders/DiceKingdoms-ModBridge)** (`DiceModders.ModBridge.dll`) to get the rows that edit the lobby rules in Settings > Lobby > Rules. Without it the rules still work, but the host has nothing to edit them with.

## Install (players)

Copy `DiceModders.Cheats.dll` into `BepInEx\plugins` (next to `DiceModders.UiKit.dll`). Settings are created in `BepInEx\config` (`org.dicemodders.dicekingdoms.cheats.cfg`).

## Build (developers)

Needs the .NET SDK (6 or newer) and PowerShell. Game files are never committed: the scripts link them from your game folder.

```powershell
.\scripts\link-interop.ps1 "D:\SteamLibrary\steamapps\common\Dice-Kingdoms"   # once: BepInEx\interop -> .\interop
.\scripts\link-uikit.ps1                                                  # once: copies DiceModders.UiKit.dll to .\libs (build UiKit and install it into the game first)
.\scripts\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Dice-Kingdoms"   # normal build: Release, installed, console and log OFF (path is remembered)
.\scripts\build-dev.ps1                                                  # development build: Debug (defines DEVLOG), console and log ON
```

If PowerShell refuses to run scripts: `powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1`.

UiKit may be updated; it is kept backward compatible. Re-run `link-uikit.ps1` to build against a newer one.

## License

MIT - see [LICENSE](LICENSE).
