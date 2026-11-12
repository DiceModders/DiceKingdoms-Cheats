using BepInEx.Configuration;

namespace DiceModders.Cheats;

/// <summary>The cheats' local switches. Several of them are risky in multiplayer (see the README); all need <see cref="Unlocked"/>.</summary>
internal static class Options
{
    internal static ConfigEntry<bool> Unlocked, DevConsole, UnlockAllBuildings, BuildAnywhere, ForceLobbyReady;

    /// <summary>True while the mod is on and the cheats are unlocked.</summary>
    internal static bool CheatsActive => Plugin.On && Unlocked.Value;

    internal static void Bind(ConfigFile config)
    {
        Unlocked = config.Bind("Cheats", "Unlocked", false,
            "While false every cheat is off and hidden. Cheats can change the simulation: use them only with everybody's consent.");
        DevConsole = config.Bind("Cheats", "DevConsole", true, "Let the game's own developer console work (saveisland, loadisland ...).");
        UnlockAllBuildings = config.Bind("Cheats", "UnlockAllBuildings", false, "Every building can be built without its technology.");
        BuildAnywhere = config.Bind("Cheats", "BuildAnywhere", false,
            "Island placement checks are skipped. RISKY: may write outside the island grid and, because placement is networked, crash everyone in the lobby.");
        ForceLobbyReady = config.Bind("Lobby", "ForceReady", false, "Host: every player counts as ready, so Start unlocks.");
    }
}
