using System;
using DiceModders.ModCore;
using DiceModders.ModCore.Tools;

namespace DiceModders.Cheats;

using Core = DiceModders.ModCore.ModCore;

/// <summary>
/// Meteorite vs. castle protection (decompiled <c>Meteorite.&lt;Execute&gt;d__4.MoveNext</c>):
///   tiles = IslandGrid.CountGround(METEORITE)                       meteorite ground tiles already on the target island
///   T     = Meteorite.meteoriteTilesToFullyAllowHitCastle (+0x38)   game value 128
///   repeat up to 256 times: impact = random grass tile; castle = MeteoriteNumberCastleTiles(impact, radius)
///       accept if castle &lt; 1, or (roll in [0,T) &lt; tiles and castle &gt;= Meteorite[+0x34])
/// So a castle hit is allowed with chance tiles / T; with 0 tiles (the first fall) never. The "Castle hits" choice:
///   Game rule  - the game's logic, with the limit T (0 = the game's 128)
///   Any meteor - MeteoriteNumberCastleTiles reports 0, so every grass tile is accepted
///   Never      - T = int.MaxValue (the roll is 64-bit, any positive int is safe)
/// A meteor is simulated on EVERY client (lock-step), so these are lobby rules: the host sets them, everybody with the mod follows, frozen at match start.
/// They sit under the "Cheats" heading of the lobby rules and are shown only while the cheats are unlocked.
/// </summary>
internal static class Meteor
{
    private const string Member = "meteoriteTilesToFullyAllowHitCastle";
    private const int Offset = 0x38;

    private const int GameRule = 0, AnyMeteor = 1, Never = 2;
    private static LobbyRule<int> _castle, _limit;
    private static int? _gameValue;

    internal static bool CastleCanBeHit => Plugin.On && _castle.Value == AnyMeteor;

    internal static void Install()
    {
        var heading = Core.Lobby.Groups.Get("cheats", "Cheats");
        bool Visible() => Options.Unlocked.Value;
        _castle = Core.Lobby.Rules.Choice("cheats.meteor.castle", "Castle hits", GameRule, new[] { "Game rule", "Any meteor", "Never" }, null, Visible)
            .InGroup(heading);
        _limit = Core.Lobby.Rules.Number("cheats.meteor.limitT", "Meteor limit T", 0, 0, 4096, 16, "0 = game (128). Higher = later hits.",
            () => Visible() && _castle.Live == GameRule).InGroup(heading);
    }

    /// <summary>Before every <c>Meteorite.Execute</c>: the rule's T into the game's object, or the game's own value back.</summary>
    internal static void BeforeExecute(object meteorite)
    {
        int current = Reflect.TryGetMember(meteorite, Member, out object o) && o is int i ? i : Mem.ReadInt(Mem.PtrOf(meteorite), Offset);
        _gameValue ??= current;   // the first read is the game's value: nothing was written before
        int wanted = !Plugin.On ? _gameValue.Value : _castle.Value == Never ? int.MaxValue : _castle.Value == AnyMeteor ? _gameValue.Value : _limit.Value > 0 ? _limit.Value : _gameValue.Value;
        if (current == wanted) return;
        if (!Reflect.TrySetMember(meteorite, Member, wanted)) Mem.WriteInt(Mem.PtrOf(meteorite), Offset, wanted);
    }
}
