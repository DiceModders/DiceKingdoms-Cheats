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
        Tx.Block(_ui, () => new SettingsBlockSpec
        {
            Id = Plugin.Guid + ".lobby", Page = SettingsPage.Lobby, Title = Tx.T("block.mods"), Order = 20,
            Build = block => Toggle(block, Tx.T("forceReady"), Options.ForceLobbyReady),
        });
        Options.Unlocked.SettingChanged += (_, _) => Gate();
        // the Cheats block in the new language: made again only while it is there
        DiceModders.ModCore.ModCore.Texts.LanguageChanged += () => { if (_cheats == null) return; _cheats.Dispose(); _cheats = null; Gate(); };
        Gate();
    }

    /// <summary>Locked: no Cheats block at all. Unlocked: the block with every cheat.</summary>
    private static void Gate()
    {
        if (Options.Unlocked.Value && _cheats == null)
            _cheats = _ui.Native.Settings.AddBlock(new SettingsBlockSpec
            {
                Id = Plugin.Guid + ".cheats", Page = SettingsPage.Game, Title = Tx.T("block.cheats"), Order = 900,
                Build = block =>
                {
                    Toggle(block, Tx.T("devConsole"), Options.DevConsole);
                    Toggle(block, Tx.T("unlockAll"), Options.UnlockAllBuildings);
                    Toggle(block, Tx.T("buildAnywhere"), Options.BuildAnywhere);
                    block.AddInfo(Tx.T("meteorInfo"));
                    block.AddButton(Tx.T("hideCheats"), Tx.T("hide"), () => Options.Unlocked.Value = false);
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
