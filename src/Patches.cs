using System;
using DiceModders.ModCore.Tools;

namespace DiceModders.Cheats;

using GameBuilding = DiceKingdoms.Game.Building;
using GameGrid = DiceKingdoms.Game.IslandGrid;

/// <summary>The game methods the cheats change. Each prefix runs the game's code unchanged unless its cheat is active.</summary>
internal static class Patches
{
    // The game calls these two very often (the AI and the automatic start placement try thousands of spots per building), so they are only in the
    // game while their cheat is on: a patch costs a trip into C# on every call even when it does nothing.
    private static PatchSwitch _unlocked, _buildAnywhere;

    internal static void Install(Patcher patcher)
    {
        patcher.Patch("Cheats", "get_Available", typeof(Patches), postfix: nameof(DevConsole));
        patcher.Patch("Cheats", "CheckAvailable", typeof(Patches), postfix: nameof(DevConsole));
        _unlocked = patcher.Switch("BuildData", "Unlocked", typeof(Patches), prefix: nameof(Unlocked));
        _buildAnywhere = patcher.Switch(typeof(GameGrid), nameof(GameGrid.CanPlaceBuilding), typeof(Patches), postfix: nameof(CanPlaceBuilding));
        patcher.Patch("Player", "get_StartGameReady", typeof(Patches), prefix: nameof(StartGameReady));
        patcher.Patch("Meteorite", "Execute", typeof(Patches), prefix: nameof(MeteorExecute));
        patcher.Patch("IslandGrid", "MeteoriteNumberCastleTiles", typeof(Patches), postfix: nameof(CastleTiles));

        Options.Unlocked.SettingChanged += (_, _) => Refresh();
        Options.UnlockAllBuildings.SettingChanged += (_, _) => Refresh();
        Options.BuildAnywhere.SettingChanged += (_, _) => Refresh();
    }

    /// <summary>Puts the two switchable patches in line with the settings and the mod switch (called by those events).</summary>
    internal static void Refresh()
    {
        if (_unlocked != null) _unlocked.On = Options.CheatsActive && Options.UnlockAllBuildings.Value;
        if (_buildAnywhere != null) _buildAnywhere.On = Options.CheatsActive && Options.BuildAnywhere.Value;
    }

    // Cheats.get_Available / Cheats.CheckAvailable gate the game's dev console.
    internal static void DevConsole(ref bool __result) { if (Options.CheatsActive && Options.DevConsole.Value) __result = true; }

    // BuildData.Unlocked(BuildingType): only patched while "unlock all buildings" is on
    internal static bool Unlocked(ref bool __result)
    {
        __result = true;
        return false;
    }

    // IslandGrid.CanPlaceBuilding(Building): the game refuses a tile outside the island grid, under a building or its remains, on the wrong ground
    // (land / water) or under nature. Build anywhere only overrides the ground and nature checks: a tile that already holds a building stays
    // refused, because placing on it breaks the island ("building id used") and stops the game.
    internal static void CanPlaceBuilding(GameGrid __instance, GameBuilding __0, ref bool __result)
    {
        if (__result || __instance == null || __0 == null) return;   // only patched while "build anywhere" is on
        try
        {
            var size = __instance.size;
            var positions = __0.Positions();
            var e = positions.GetEnumerator();
            var walk = e.Cast<Il2CppSystem.Collections.IEnumerator>();
            while (walk.MoveNext())
            {
                var p = e.Current;
                if (p.x < 0 || p.y < 0 || p.x >= size.x || p.y >= size.y) return;
                if (__instance.BuildingAt(p).HasValue || __instance.RemainsAt(p).HasValue) return;
            }
            __result = true;
        }
        catch (Exception ex) { Plugin.L.LogWarning("build anywhere: " + ex.Message); }
    }

    // Player.get_StartGameReady: read by the lobby Start button, but also by every player's ready tick box. Only on the host: on a client the
    // forced value showed its own tick box as always ready and the player could not untick it (and its clicks sent "ready" to the host).
    internal static bool StartGameReady(ref bool __result)
    {
        if (!Plugin.On || !Options.ForceLobbyReady.Value || !DiceModders.ModCore.ModCore.Lobby.IsHost) return true;
        __result = true;
        return false;
    }

    // Meteorite.Execute runs on every client for every meteorite. The lobby rules decide (never the local unlock, or clients would desync).
    internal static void MeteorExecute(object __instance)
    {
        try { Meteor.BeforeExecute(__instance); }
        catch (Exception e) { Plugin.L.LogWarning("meteor: " + e.Message); }
    }

    // IslandGrid.MeteoriteNumberCastleTiles: "castle tiles within the impact radius". 0 makes every grass tile acceptable (castle not protected).
    internal static void CastleTiles(ref int __result) { if (Meteor.CastleCanBeHit) __result = 0; }
}
