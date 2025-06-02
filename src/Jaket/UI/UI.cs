namespace Jaket.UI;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Input;
using Jaket.UI.Dialogs;
using Jaket.UI.Fragments;
using Jaket.UI.Lib;

/// <summary> Class that loads and manages the interface of the mod. </summary>
public static class UI
{
    /// <summary> Object the player is focused on. </summary>
    public static GameObject Focus => EventSystem.current?.currentSelectedGameObject;
    /// <summary> Whether the player is focused on a input field. </summary>
    public static bool Focused => Focus && Focus.TryGetComponent<InputField>(out var f) && f.isActiveAndEnabled;

    /// <summary> Whether any dialog is visible. </summary>
    public static bool AnyDialog => Dialogs.Any(d => d.Shown) || (OptionsManager.Instance?.paused ?? false);

    #region dialogs

    public static LobbyTab LobbyTab;
    public static LobbyList LobbyList;
    public static PlayerList PlayerList;
    public static Settings Settings;

    #endregion
    #region fragments

    public static EmoteWheel Emote;
    public static MainMenuAccess Access;
    public static PlayerIndicators PlayerInds;
    public static Skateboard Skateboard;
    public static Spectator Spectator;
    public static Teleporter Teleporter;

    #endregion
    #region groups

    /// <summary> Group containing all of the dialogs. </summary>
    public static Fragment[] Dialogs;
    /// <summary> Group containing all of the fragments. </summary>
    public static Fragment[] Fragments;
    /// <summary> Group containing elements located on the left side of the screen. </summary>
    public static Fragment[] LeftGroup;
    /// <summary> Group containing elements located in the center of the screen. </summary>
    public static Fragment[] MidlGroup;

    #endregion

    /// <summary> Builds all of the interface elements: fragments, dialogs and so on. </summary>
    public static void Build()
    {
        void Fix() => Events.Post(() =>
        {
            HudMessageReceiver.Instance.text.font = ModAssets.TmpFont;
            Component<Canvas>(HudMessageReceiver.Instance.gameObject, c =>
            {
                c.overrideSorting = true;
                c.sortingOrder = 4200 + 1;
            });
        });
        Fix();
        Events.OnLoad += Fix;

        var root = Create("UI").transform;

        LobbyTab = new(root);
        LobbyList = new(root);
        PlayerList = new(root);
        Settings = new(root);

        Emote = new(root);
        Access = new(root);
        PlayerInds = new(root);
        Skateboard = new(root);
        Spectator = new(root);
        Teleporter = new(root);

        Dialogs = new Fragment[] { LobbyTab, LobbyList, PlayerList, Settings };
        Fragments = new Fragment[] { Access, Skateboard, Spectator, Teleporter };
        LeftGroup = new Fragment[] { LobbyTab, PlayerList, Settings };
        MidlGroup = new Fragment[] { LobbyList };

        Log.Info($"[FACE] Builded {Dialogs.Length} dialogs and {Fragments.Length} fragments");
    }

    /// <summary> Hides all of the elements in the given group except the fragment. </summary>
    public static void Hide(Fragment[] group, Fragment frag)
    {
        group.Each(f => f.Shown && f != frag, f => f.Toggle());
        if (group == MidlGroup && Scene != "Main Menu")
        {
            OptionsManager.Instance.UnPause();
            WeaponWheel.Instance.gameObject.SetActive(false);
        }
    }

    /// <summary> Hides all of the elements in the given group and runs particular callbacks. </summary>
    public static void Hide(Fragment[] group, Fragment frag, Runnable shown)
    {
        if (frag.Shown)
        {
            Hide(group, frag);
            shown();
        }
        Movement.UpdateState();
    }
}
