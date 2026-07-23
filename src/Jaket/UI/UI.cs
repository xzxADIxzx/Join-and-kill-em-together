namespace Jaket.UI;

using UnityEngine.EventSystems;
using UnityEngine.UI;

using Jaket.Assets;
using Jaket.Input;
using Jaket.UI.Dialogs;
using Jaket.UI.Fragments;
using Jaket.UI.Lib;

/// <summary> Class responsible for loading and managing the interface. </summary>
public static class UI
{
    /// <summary> Whether the player is focused. </summary>
    public static bool Focused => (EventSystem.current?.currentSelectedGameObject?.TryGetComponent(out InputField f) ?? false) && f.isActiveAndEnabled;
    /// <summary> Whether any dialog is visible. </summary>
    public static bool AnyDialog => (Dialogs?.Any(d => d.Shown) ?? false) || (OptionsManager.Instance?.paused ?? false);

    #region dialogs

    public static Chat Chat;
    public static LobbyTab LobbyTab;
    public static LobbyList LobbyList;
    public static GameConfig GameConfig;
    public static Privileges Privileges;
    public static PlayerList PlayerList;
    public static Settings Settings;
    public static SpraySettings Sprays;

    #endregion
    #region fragments

    public static Debug Debug;
    public static EmoteWheel Emote;
    public static MainMenuAccess Access;
    public static PlayerIndicators PlayerInds;
    public static PlayerInformation PlayerInfo;
    public static Skateboard Skateboard;
    public static Spectator Spectator;
    public static Teleporter Teleporter;

    #endregion
    #region groups

    /// <summary> Group containing all of the dialogs. </summary>
    public static Fragment[] Dialogs;
    /// <summary> Group containing all of the fragments. </summary>
    public static Fragment[] Fragments;
    /// <summary> Group containing elements located on the side of the screen. </summary>
    public static Fragment[] LeftGroup;
    /// <summary> Group containing elements located in the center of the screen. </summary>
    public static Fragment[] MidlGroup;

    #endregion

    /// <summary> Builds the interface. </summary>
    public static void Build() => Tex.OnLoad(() =>
    {
        static void Fix() => Events.Post(() =>
        {
            HudMessageReceiver.Instance.text.font = ModAssets.TmpFont;
            HudMessageReceiver.Instance.Component<UnityEngine.Canvas>(c =>
            {
                c.overrideSorting = true;
                c.sortingOrder = 42 + 01;
            });
        });
        Fix();
        Events.OnLoad += Fix;

        var root = Create("UI", Plugin.Instance.transform).transform;

        Chat = new(root);
        LobbyTab = new(root);
        LobbyList = new(root);
        GameConfig = new(root);
        Privileges = new(root);
        PlayerList = new(root);
        Settings = new(root);
        Sprays = new(root);

        Debug = new(root);
        Emote = new(root);
        Access = new(root);
        PlayerInds = new(root);
        PlayerInfo = new(root);
        Skateboard = new(root);
        Spectator = new(root);
        Teleporter = new(root);

        Dialogs   = [ Chat, LobbyTab, LobbyList, GameConfig, Privileges, PlayerList, Settings, Sprays ];
        Fragments = [ Debug, Emote, Access, PlayerInds, PlayerInfo, Skateboard, Spectator, Teleporter ];
        LeftGroup = [ Chat, LobbyTab, PlayerList, Settings, Debug ];
        MidlGroup = [ LobbyList, GameConfig, Privileges, Sprays ];

        Log.Info($"[FACE] Builded {Dialogs.Length} dialogs and {Fragments.Length} fragments");
    });

    /// <summary> Hides all of the elements in the given group except the fragment and runs the callbacks. </summary>
    public static void Hide(Fragment[] group, Fragment frag, Runnable shown = null, Runnable hidden = null)
    {
        if (frag.Shown)
        {
            if (group == MidlGroup && Scene != "Main Menu") OptionsManager.Instance.UnPause();

            group.Each(f => f.Shown && f != frag, f => f.Toggle());

            shown?.Invoke();
        }
        else hidden?.Invoke();

        Movement.UpdateState();
    }
}
