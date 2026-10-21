using System;

namespace DiceModders.Cheats;

internal static class Patches
{
    // Cheats.get_Available (property getter) and Cheats.CheckAvailable (static) both gate the dev console.
    internal static void ForceTrue(ref bool __result)
    {
        if (Cheat.DevConsole.Value && CheatsGate.Unlocked) __result = true;
    }

    // BuildData.Unlocked(BuildingType)
    internal static bool UnlockedPrefix(ref bool __result)
    {
        if (!CheatsGate.Unlocked || !Cheat.UnlockAllBuildings.On) return true;
        __result = true;
        return false; // skip original
    }

    // IslandGrid.CanPlaceBuilding(Building) - the per-tile placement check.
    internal static bool CanPlaceBuildingPrefix(ref bool __result)
    {
        if (!CheatsGate.Unlocked || !Cheat.BuildAnywhere.On) return true;
        __result = true;
        return false;
    }

    // Player.get_StartGameReady - read by the lobby Start button.
    internal static bool StartGameReadyPrefix(ref bool __result)
    {
        if (!Cheat.ForceLobbyReady.On) return true;
        __result = true;
        return false;
    }

    // Meteorite.Execute(GameState, DisasterOccurrence, Random) runs on every client for every meteorite occurrence. The lobby rule decides T
    // (the game's own value when the rule is 0); this must NOT depend on the local cheat unlock, or a locked client would desync from the host.
    internal static void MeteorExecutePrefix(object __instance)
    {
        try { Meteor.ApplyBeforeExecute(__instance); }
        catch (Exception e) { ModPlugin.L.LogWarning("[meteor] " + e.GetType().Name + ": " + e.Message); }
    }

    // IslandGrid.MeteoriteNumberCastleTiles(int2 impact, int radius): "castle tiles within the impact radius".
    // Reporting 0 makes Meteorite.Execute accept every random grass tile, so the castle is no longer protected. Follows the lobby rule on every client.
    internal static void CastleTilesPostfix(ref int __result)
    {
        if (Meteor.ProtectionOff == null || !Meteor.ProtectionOff.Value) return;
        if (Meteor.NeverHit != null && Meteor.NeverHit.Value) return;   // "never" wins over "always"
        if (__result != 0) Meteor.CastleChecksOverridden++;
        __result = 0;
    }
}
