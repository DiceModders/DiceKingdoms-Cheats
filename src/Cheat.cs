using System;
using BepInEx.Configuration;
using DiceModders.Shared;

namespace DiceModders.Cheats;

/// <summary>
/// Things that change what you are allowed to do: the game's own dev console, building unlocks, placement rules and the lobby Start button.
/// Several of these are risky in multiplayer - see the README before enabling them in a shared lobby.
/// </summary>
internal static class Cheat
{
    internal static ConfigEntry<bool> DevConsole;
    internal static Toggle UnlockAllBuildings;
    internal static Toggle BuildAnywhere;
    internal static Toggle ForceLobbyReady;

    /// <summary>Hotkey actions of cheats only work after the cheats were unlocked in the Settings screen.</summary>
    internal static Action Guard(Action a) => () => { if (CheatsGate.Unlocked) a(); };

    internal static void Load(ConfigFile config, Hotkeys keys)
    {
        var L = ModPlugin.L;

        DevConsole = config.Bind("DevConsole", "Unlock", true,
            "Force Cheats.Available / Cheats.CheckAvailable to true so the game's own dev console " +
            "(including saveisland / loadisland) can be used. Works only after the cheats were unlocked.");

        UnlockAllBuildings = new Toggle("Unlock all buildings",
            config.Bind("Startup", "UnlockAllBuildings", false, "Start with every building unlocked.").Value, L);
        BuildAnywhere = new Toggle("Build anywhere (RISKY)",
            config.Bind("Startup", "BuildAnywhere", false,
                "Start with island placement checks bypassed. RISKY: may write outside the island grid " +
                "and, because placement is networked, could crash everyone in the lobby.").Value, L);
        ForceLobbyReady = new Toggle("Force lobby ready (untested)",
            config.Bind("Startup", "ForceLobbyReady", false,
                "Start with every player reported as 'ready' so the lobby Start button unlocks. Untested; host only.").Value, L);

        keys.Bind(config, "UnlockCheats", HotkeyKey.Insert, "Unlock / hide the cheats (the same as the 5 clicks in the Settings screen)", CheatsUi.OnUnlockGesture, "Unlock / hide the cheats", () => CheatsGate.Unlocked);
        keys.Bind(config, "UnlockAllBuildings", HotkeyKey.F7, "Toggle: all buildings unlocked", Guard(UnlockAllBuildings.Flip), "Unlock all buildings", () => UnlockAllBuildings.On);
        keys.Bind(config, "BuildAnywhere", HotkeyKey.PageDown, "Toggle: ignore island placement checks (RISKY)", Guard(BuildAnywhere.Flip), "Build anywhere (RISKY)", () => BuildAnywhere.On);
        keys.Bind(config, "ForceLobbyReady", HotkeyKey.Delete, "Toggle: force lobby 'ready' (untested, host only)", ForceLobbyReady.Flip, "Force lobby ready (host, untested)", () => ForceLobbyReady.On);
    }
}
