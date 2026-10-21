using BepInEx.Configuration;

namespace DiceModders.Cheats;

/// <summary>
/// The cheats (and their hotkeys) are hidden until unlocked with the secret gesture in the game's Settings screen:
/// click the bottom-left corner of the settings panel 5 times. The state is saved in [Cheats] Unlocked.
/// </summary>
internal static class CheatsGate
{
    public static ConfigEntry<bool> UnlockedEntry;
    public static bool Unlocked => UnlockedEntry != null && UnlockedEntry.Value;

    public static void Init(ConfigFile cfg)
    {
        UnlockedEntry = cfg.Bind("Cheats", "Unlocked", false,
            "Set by the hidden gesture in the game's Settings screen (click the bottom-left corner of the settings panel 5 times). " +
            "While false every cheat and its hotkey is off and hidden. Cheats can change the simulation: use them only with everybody's consent.");
    }

    public static void Toggle()
    {
        UnlockedEntry.Value = !UnlockedEntry.Value;
        ModPlugin.L.LogMessage("Cheats " + (UnlockedEntry.Value ? "UNLOCKED" : "hidden again"));
    }
}
