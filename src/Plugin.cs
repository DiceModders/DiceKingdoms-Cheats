using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DiceModders.ModCore.Tools;

namespace DiceModders.Cheats;

using Core = DiceModders.ModCore.ModCore;   // inside DiceModders.* the bare name would be the namespace
using Ui = DiceModders.UiKit.UiKit;

/// <summary>
/// DiceKingdoms Cheats: the game's dev console, unlock all buildings, build anywhere, meteor castle protection (host lobby rules), force lobby ready.
/// Everything except "force ready" stays hidden and inactive until the cheats are unlocked ([Cheats] Unlocked).
/// </summary>
[BepInPlugin(Guid, Name, Version)]
[BepInDependency("DiceModders.ModCore")]
[BepInDependency("DiceModders.UiKit")]
[BepInProcess("Dice-Kingdoms.exe")]
public sealed class Plugin : BasePlugin
{
    public const string Guid = "org.dicemodders.dicekingdoms.cheats";
    public const string Name = "DiceKingdoms Cheats";
    public const string Version = "4.0.0";

    internal static ManualLogSource L;
    internal static bool On;

    public override void Load()
    {
        L = Log;
        Options.Bind(Config);
        Meteor.Install();

        var patcher = new Patcher(Guid, Log);
        Patches.Install(patcher);
        patcher.Summary();

        var ui = Ui.CreateScope(Guid);
        CheatSettings.Install(ui);

        // gameplay: a change during a match waits until it ends
        Core.Mods.Switch(Config, affectsSimulation: true, description: "Turns every cheat of this mod on or off.")
            .WhenOn(() => { On = true; Patches.Refresh(); }, () => { On = false; Patches.Refresh(); });
    }
}
