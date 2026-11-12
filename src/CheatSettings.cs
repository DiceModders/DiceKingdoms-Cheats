using BepInEx.Configuration;
using DiceModders.UiKit;
using DiceModders.UiKit.Native;

namespace DiceModders.Cheats;

/// <summary>
/// The Cheats mod inside the game's Settings:
///  * Settings > Lobby: "Force ready" (always there, the lobby box is small so it is one row),
///  * Settings > Game: the "Cheats" block, which exists only while the cheats are unlocked ([Cheats] Unlocked, e.g. from an in-game config editor).
/// </summary>
internal static class CheatSettings
{
    private static UiScope _ui;
    private static SettingsBlockHandle _cheats;

    internal static void Install(UiScope ui)
    {
        _ui = ui;
        if (_ui == null) return;
        _ui.Native.Settings.AddBlock(new SettingsBlockSpec
        {
            Id = Plugin.Guid + ".lobby", Page = SettingsPage.Lobby, Title = "Mods", Order = 20,
            Build = block => Toggle(block, "Force ready", Options.ForceLobbyReady),
        });
        Options.Unlocked.SettingChanged += (_, _) => Gate();
        Gate();
    }

    /// <summary>Locked: no Cheats block at all. Unlocked: the block with every cheat.</summary>
    private static void Gate()
    {
        if (Options.Unlocked.Value && _cheats == null)
            _cheats = _ui.Native.Settings.AddBlock(new SettingsBlockSpec
            {
                Id = Plugin.Guid + ".cheats", Page = SettingsPage.Game, Title = "Cheats", Order = 900,
                Build = block =>
                {
                    Toggle(block, "Developer console", Options.DevConsole);
                    Toggle(block, "Unlock all buildings", Options.UnlockAllBuildings);
                    Toggle(block, "Build anywhere (RISKY)", Options.BuildAnywhere);
                    block.AddInfo("Meteor rules are set by the host in Settings > Lobby > Rules, under \"Cheats\". They apply to every player of the match.");
                    block.AddButton("Hide the cheats", "Hide", () => Options.Unlocked.Value = false);
                },
            });
        else if (!Options.Unlocked.Value && _cheats != null) { _cheats.Dispose(); _cheats = null; }
    }

    private static void Toggle(SettingsBlockHandle block, string label, ConfigEntry<bool> entry)
    {
        var row = block.AddToggle(label, entry.Value, v => entry.Value = v);
        if (row != null) entry.SettingChanged += (_, _) => row.SetValue(entry.Value);
    }
}
