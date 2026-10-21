using System;
using DiceModders.UiKit;
using UnityEngine;

namespace DiceModders.Cheats;

// Inside namespace DiceModders.* the bare name "UiKit" would be the NAMESPACE DiceModders.UiKit; this alias (declared inside
// our namespace, so it wins) makes it mean the static class again.
using UiKit = DiceModders.UiKit.UiKit;

/// <summary>
/// The Cheats mod's parts inside the game's own screens, all made with UiKit:
///  * lobby match settings: a "Force ready" tick (always visible),
///  * Settings screen: an invisible spot (bottom-left corner of the panel, 5 clicks) that reveals / hides the Cheats block in the Game tab.
/// </summary>
internal static class CheatsUi
{
    private static SettingsRegistration _cheatsBlock;
    private static WindowHandle _hint;
    private static float _hintUntil;

    public static void Init()
    {
        // The lobby settings box is small, so this block has just one row.
        UiKit.Settings.Add(SettingsTab.Lobby, "Mods", s =>
        {
            s.Toggle("Force ready", () => Cheat.ForceLobbyReady.On, v => { if (v != Cheat.ForceLobbyReady.On) Cheat.ForceLobbyReady.Flip(); });
        });

        UiKit.Settings.AddHotspot("cheats.unlock", SettingsCorner.BottomLeft, 5, OnUnlockGesture, 44f, 4f);

        // diagnostics: shows whether the invisible spot was created under the Settings screen and where it is
        UiKit.Settings.Opened += screen =>
        {
            try
            {
                var spot = screen.Find("DM Hotspot cheats.unlock") as RectTransform;
                if (spot == null) { ModPlugin.L.LogWarning("[cheats] Settings screen opened, but the unlock spot does not exist under it."); return; }
                var c = new Vector3[4];
                spot.GetWorldCorners(c);
                ModPlugin.L.LogInfo($"[cheats] Settings opened: unlock spot under '{screen.name}' sibling {spot.GetSiblingIndex()}/{screen.childCount}, screen rect {screen.rect.width:0}x{screen.rect.height:0}, spot world corners {c[0]} {c[2]}");
            }
            catch (Exception e) { ModPlugin.L.LogWarning("[cheats] spot diagnostics failed: " + e.Message); }
        };

        ApplyGate();
        ModPlugin.L.LogInfo("[cheats] secret unlock spot registered; unlocked cheats appear in the Game tab of the Settings screen.");
    }

    public static void Tick()
    {
        if (_hint != null && Time.realtimeSinceStartup > _hintUntil)
        {
            try { _hint.Close(); } catch (Exception) { }
            _hint = null;
        }
    }

    // ---- the secret gesture ------------------------------------------------------------------------------------------------------

    internal static void OnUnlockGesture()
    {
        CheatsGate.Toggle();
        ApplyGate();
        ShowHint(CheatsGate.Unlocked
            ? "Cheats unlocked - reopen the Game tab to see them."
            : "Cheats hidden.");
    }

    /// <summary>The Cheats block exists in the registry only while the cheats are unlocked, so a locked game shows nothing at all.</summary>
    private static void ApplyGate()
    {
        if (CheatsGate.Unlocked && _cheatsBlock == null)
            _cheatsBlock = UiKit.Settings.Add(SettingsTab.Game, "Cheats", BuildCheatRows);
        else if (!CheatsGate.Unlocked && _cheatsBlock != null)
        {
            _cheatsBlock.Remove();
            _cheatsBlock = null;
        }
    }

    private static void ShowHint(string text)
    {
        try
        {
            if (_hint != null) { _hint.Close(); _hint = null; }
            _hint = UiKit.Window(w =>
            {
                w.Size(380, 70);
                w.Body.Label(text).Center();
            });
            _hintUntil = Time.realtimeSinceStartup + 2.5f;
        }
        catch (Exception e) { ModPlugin.L.LogWarning("[cheats] could not show the hint window: " + e.Message); }
    }

    // ---- rows ------------------------------------------------------------------------------------------------------------------------

    private static void BuildCheatRows(SettingsSection s)
    {
        s.Toggle("Unlock all buildings", () => Cheat.UnlockAllBuildings.On, v => Set(Cheat.UnlockAllBuildings, v));
        s.Toggle("Build anywhere (RISKY)", () => Cheat.BuildAnywhere.On, v => Set(Cheat.BuildAnywhere, v));

        s.Header("Meteors");
        s.Info("Meteor rules are set by the host in Settings > Lobby > Rules (Meteor limit T, Meteor hits castle, Castle never hit). They apply to every player of the match.");
        s.Info("Hotkeys (Right Ctrl +): F7 unlock all buildings, PageDown build anywhere, Delete force ready.");
    }

    private static void Set(DiceModders.Shared.Toggle t, bool wanted)
    {
        if (t.On != wanted) t.Flip();
    }
}
