using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DiceModders.Shared;
using UnityEngine;

namespace DiceModders.Cheats;

/// <summary>
/// DiceKingdoms Cheats: unlock all buildings, build anywhere, meteor castle protection, force lobby ready.
/// Everything except "force ready" is hidden until unlocked with a secret gesture in the game's Settings screen (see CheatsUi).
/// The interface is built with the UiKit library (a separate plugin that has to be installed next to this one).
/// </summary>
[BepInPlugin(Guid, Name, Version)]
[BepInDependency("DiceModders.UiKit")]
[BepInDependency("DiceModders.ModCore")]
[BepInProcess("Dice-Kingdoms.exe")]
public sealed class ModPlugin : BasePlugin
{
    public const string Guid = "org.dicemodders.dicekingdoms.cheats";
    public const string Name = "DiceKingdoms Cheats";
    public const string Version = "3.0.0";

    internal static ManualLogSource L;
    internal static Hotkeys Keys;

    public override void Load()
    {
        L = Log;
        CheatsGate.Init(Config);
        Keys = new Hotkeys(Config, L, Name);

        Cheat.Load(Config, Keys);          // dev console unlock, unlock all buildings, build anywhere, force lobby ready
        Meteor.Load();                     // meteor castle protection: lobby rules (host decides, everybody follows)

        var patcher = new Patcher(Guid, L);
        patcher.Patch("Cheats", "get_Available", typeof(Patches), postfix: nameof(Patches.ForceTrue));
        patcher.Patch("Cheats", "CheckAvailable", typeof(Patches), postfix: nameof(Patches.ForceTrue));
        patcher.Patch("BuildData", "Unlocked", typeof(Patches), prefix: nameof(Patches.UnlockedPrefix));
        patcher.Patch("IslandGrid", "CanPlaceBuilding", typeof(Patches), prefix: nameof(Patches.CanPlaceBuildingPrefix));
        patcher.Patch("Player", "get_StartGameReady", typeof(Patches), prefix: nameof(Patches.StartGameReadyPrefix));
        patcher.Patch("Meteorite", "Execute", typeof(Patches), prefix: nameof(Patches.MeteorExecutePrefix));
        patcher.Patch("IslandGrid", "MeteoriteNumberCastleTiles", typeof(Patches), postfix: nameof(Patches.CastleTilesPostfix));
        patcher.Summary();

        L.LogMessage("Meteor rules are lobby rules: the host sets them in the lobby settings and every player with this mod follows them.");

        CheatsUi.Init();                   // settings-screen parts: secret spot, Cheats block, lobby tick
        Keys.Bind(Config, "MeteorShow", HotkeyKey.None, "Meteors: print the values that apply to this match to the log", Meteor.Show, "Print meteor values");
        Keys.LogBindings();
        AddComponent<CheatsTicker>();
    }
}

public sealed class CheatsTicker : MonoBehaviour
{
    public CheatsTicker(IntPtr ptr) : base(ptr) { }

    private void Update()
    {
        ModPlugin.Keys?.Poll();
        CheatsUi.Tick();
    }
}
