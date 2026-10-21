using System;
using DiceModders.ModCore;
using BepInEx.Configuration;
using DiceModders.Shared;
using DiceModders.UiKit;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace DiceModders.Cheats;

// The class ModCore and its namespace share a name; inside DiceModders.* this alias makes "ModCore" the static class.
using ModCore = DiceModders.ModCore.ModCore;

// Inside namespace DiceModders.* the bare name "UiKit" would be the NAMESPACE DiceModders.UiKit; this alias makes it the static class again.
using UiKit = DiceModders.UiKit.UiKit;

/// <summary>
/// Meteorite vs. castle protection, from the decompiled Meteorite.&lt;Execute&gt;d__4.MoveNext:
///
///   tiles = IslandGrid.CountGround(METEORITE)           // meteorite ground tiles already on the target island
///   T     = Meteorite.meteoriteTilesToFullyAllowHitCastle   // +0x38 (game value 128)
///   repeat up to 256 times:
///       impact = random grass tile
///       castle = IslandGrid.MeteoriteNumberCastleTiles(impact, radius)   // castle tiles within radius (+0x30, radius 4)
///       accept the impact if  castle &lt; 1
///                          or ( rollIn[0,T) &lt; tiles   AND   castle &gt;= Meteorite[+0x34] )
///
/// So the chance that a castle hit is allowed is about tiles / T. With 0 tiles (the first fall) the roll can never be
/// below 0, so NO value of T makes the first fall hit the castle - T only sets how fast the chance ramps up as impacts
/// leave tiles behind (each radius-4 impact makes up to 49). "Castle protection OFF" makes MeteoriteNumberCastleTiles
/// report 0, so every random grass tile is accepted and the castle can be hit from the first fall.
/// </summary>
internal static class Meteor
{
    private const string CastleMember = "meteoriteTilesToFullyAllowHitCastle";
    private const int CastleOffset = 0x38;     // Meteorite
    private const int RadiusOffset = 0x30;     // Meteorite: impact radius (4 in game)
    private const string MeteoriteMember = "meteorite";
    private const int MeteoriteOffset = 0x78;  // Disasters

    public const int HardMax = int.MaxValue;   // the game's roll is (uint)(random * T) >> 32 in 64-bit math: any positive int is safe

    /// <summary>The game's own value (128), recorded from the live object before our first write.</summary>
    public static int? GameDefault;

    /// <summary>
    /// The two lobby rules. A meteor is simulated on EVERY client (the game is a deterministic lockstep), so these cannot be local switches:
    /// the host sets them in the lobby settings, UiKit keeps them in the Steam lobby data and freezes them when the match starts, and every client
    /// that has this mod applies the same values. A client without the mod would simulate a different meteor - UiKit warns about that in the lobby.
    /// </summary>
    public static LobbyRule<bool> ProtectionOff;
    public static LobbyRule<int> LimitT;
    public static LobbyRule<bool> NeverHit;

    public static long CastleChecksOverridden;
    public static long ExecutesPatched;

    private static bool _wrote;
    private static object _meteorite;
    private static long _nextLookup;

    internal static void Load()
    {
        ProtectionOff = ModCore.Lobby.Rules.Toggle(
            "cheats.meteor.protectionOff", "Meteor hits castle", false,
            "Any meteor may hit a castle.",
            visible: () => CheatsGate.Unlocked);
        LimitT = ModCore.Lobby.Rules.Number(
            "cheats.meteor.limitT", "Meteor limit T", 0, 0, 4096, 16,
            "0 = game value. Higher = safer.",
            visible: () => CheatsGate.Unlocked);
        NeverHit = ModCore.Lobby.Rules.Toggle(
            "cheats.meteor.neverHit", "Castle never hit", false,
            "Meteors never hit a castle.",
            visible: () => CheatsGate.Unlocked);
    }

    /// <summary>
    /// The T that applies to this match: the host's rule, or the game's own value. "Castle never hit" uses int.MaxValue:
    /// the game's roll is in [0,T), so it is below the tile count about once in 2 billion tries - never in practice.
    /// </summary>
    public static int EffectiveT() =>
        NeverHit != null && NeverHit.Value ? HardMax
        : LimitT != null && LimitT.Value > 0 ? LimitT.Value
        : (GameDefault ?? 128);

    // ---- reading / writing the game's object ---------------------------------------------------------------------

    public static bool TryRead(object meteorite, out int value)
    {
        value = 0;
        if (meteorite == null) return false;
        if (Reflect.TryGetMember(meteorite, CastleMember, out object o) && o is int i) { value = i; return true; }
        IntPtr p = Mem.PtrOf(meteorite);
        if (p == IntPtr.Zero) return false;
        value = Mem.ReadInt(p, CastleOffset);
        return true;
    }

    public static bool TryWrite(object meteorite, int value)
    {
        if (meteorite == null) return false;
        if (!Reflect.TrySetMember(meteorite, CastleMember, value))
        {
            IntPtr p = Mem.PtrOf(meteorite);
            if (p == IntPtr.Zero) return false;
            Mem.WriteInt(p, CastleOffset, value);
        }
        _wrote = true;
        return true;
    }

    /// <summary>Remember the game's own value (only valid before we have written anything).</summary>
    public static void Learn(object meteorite)
    {
        if (GameDefault == null && !_wrote && TryRead(meteorite, out int v))
        {
            GameDefault = v;
            ModPlugin.L.LogInfo($"[meteor] game value of {CastleMember} = {v}");
        }
    }

    /// <summary>Finds the live Meteorite object through the Disasters asset (cached; retried at most every 2 s).</summary>
    public static object Meteorite
    {
        get
        {
            if (_meteorite != null) return _meteorite;
            long now = Environment.TickCount64;
            if (now < _nextLookup) return null;
            _nextLookup = now + 2000;
            _meteorite = FindMeteorite();
            if (_meteorite != null) Learn(_meteorite);
            return _meteorite;
        }
    }

    private static object FindMeteorite()
    {
        try
        {
            var disastersType = GameTypes.Find("Disasters");
            var meteoriteType = GameTypes.Find("Meteorite");
            if (disastersType == null || meteoriteType == null) return null;

            var found = Resources.FindObjectsOfTypeAll(Il2CppType.From(disastersType));
            if (found == null || found.Length == 0) return null;

            IntPtr disasters = Mem.PtrOf(found[0]);
            object wrapper = Reflect.Wrap(disastersType, disasters);
            if (Reflect.TryGetMember(wrapper, MeteoriteMember, out object m) && m != null && meteoriteType.IsInstanceOfType(m))
                return m;
            return Reflect.Wrap(meteoriteType, Mem.ReadPtr(disasters, MeteoriteOffset));
        }
        catch (Exception e)
        {
            ModPlugin.L.LogWarning("[meteor] lookup failed: " + e.GetType().Name + ": " + e.Message);
            return null;
        }
    }

    // ---- applying the rules ---------------------------------------------------------------------------------------------

    /// <summary>Called before every Meteorite.Execute: writes the rule's T into the game object, or restores the game's own value.</summary>
    public static void ApplyBeforeExecute(object meteorite)
    {
        Learn(meteorite);
        int wanted = EffectiveT();
        if (TryRead(meteorite, out int now) && now != wanted) TryWrite(meteorite, wanted);
        ExecutesPatched++;
    }

    public static void Show()
    {
        ModPlugin.L.LogMessage($"[meteor] T = {EffectiveT()} (game value: {GameDefault?.ToString() ?? "?"}, rule: {LimitT?.Value}), " +
                               $"protection {(ProtectionOff?.Value == true ? "OFF" : "ON")}, executes patched {ExecutesPatched}, castle checks overridden {CastleChecksOverridden}");
    }

    private static long _nextCount;
    private static int _lastCount = -1;

    /// <summary>
    /// IslandGrid.CountGround(METEORITE) for the LOCAL player's island - the same number the meteor code uses.
    /// -1 = not available. Refreshed twice a second at most. Watch it while using the pickaxe to see whether
    /// cleaning a meteorite tile really lowers the count (and with it the castle hit chance).
    /// </summary>
    public static int CountOnLocalIsland()
    {
        long now = Environment.TickCount64;
        if (now < _nextCount) return _lastCount;
        _nextCount = now + 500;
        _lastCount = -1;
        try
        {
            var playerType = GameTypes.Find("Player");
            var islandType = GameTypes.Find("Island");
            var gridType = GameTypes.Find("IslandGrid");
            if (playerType == null || islandType == null || gridType == null) return -1;

            IntPtr local = Mem.PtrOf(Reflect.CallStatic(playerType, "get_LocalPlayer"));
            if (local == IntPtr.Zero) return -1;

            var found = Resources.FindObjectsOfTypeAll(Il2CppType.From(islandType));
            if (found == null) return -1;
            for (int i = 0; i < found.Length; i++)
            {
                try
                {
                    object island = Reflect.Wrap(islandType, Mem.PtrOf(found[i]));
                    if (Mem.PtrOf(Reflect.Call(island, "get_Owner")) != local) continue;
                    object grid = Reflect.Wrap(gridType, Mem.ReadPtr(Mem.PtrOf(island), 0x98));   // Island.grid
                    if (grid == null) return -1;
                    _lastCount = Convert.ToInt32(Reflect.Call(grid, "CountGround", 5));           // GroundTile.METEORITE = 5
                    break;
                }
                catch (Exception) { /* not a spawned island - skip */ }
            }
        }
        catch (Exception) { _lastCount = -1; }
        return _lastCount;
    }

    /// <summary>Explains the numbers in the window.</summary>
    public static string Info()
    {
        int radius = 4;
        var m = Meteorite;
        if (m != null)
        {
            int r = Mem.ReadInt(Mem.PtrOf(m), RadiusOffset);
            if (r >= 1 && r <= 20) radius = r;
        }
        int tilesPerImpact = 0;
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                if (dx * dx + dy * dy <= radius * radius) tilesPerImpact++;

        int t = Math.Max(1, EffectiveT());
        int tiles = CountOnLocalIsland();

        if (NeverHit.Value) return "Castle never hit: meteors always avoid castles.";

        string line1 = ProtectionOff.Value
            ? "Protection OFF: any meteor may hit the castle."
            : $"Castle hit chance = tiles on island / {t}";
        string line2 = $"Full exposure after ~{t / (double)tilesPerImpact:0.0} impacts ({tilesPerImpact} tiles each)";
        string line3 = ProtectionOff.Value ? "(the value above is not used while OFF)" : "0 tiles (first fall): never, unless OFF";
        string line4 = tiles < 0
            ? "Meteorite tiles on YOUR island: not available yet"
            : ProtectionOff.Value
                ? $"Meteorite tiles on YOUR island: {tiles}"
                : $"Tiles on YOUR island: {tiles}  ->  chance now {Math.Min(100.0, 100.0 * tiles / t):0}%";
        return line1 + "\n" + line2 + "\n" + line3 + "\n" + line4;
    }
}
