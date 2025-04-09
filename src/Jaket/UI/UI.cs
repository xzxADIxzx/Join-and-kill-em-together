namespace Jaket.UI;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Jaket.UI.Dialogs;
using Jaket.UI.Fragments;
using Jaket.UI.Lib;
using Jaket.World;

/// <summary> Class that loads and manages the interface of the mod. </summary>
public static class UI
{
    /// <summary> Object the player is focused on. </summary>
    public static GameObject Focus => EventSystem.current?.currentSelectedGameObject;
    /// <summary> Whether the player is focused on a input field. </summary>
    public static bool Focused => Focus && Focus.TryGetComponent<InputField>(out var f) && f.isActiveAndEnabled;

    /// <summary> Whether any dialog is visible. </summary>
    public static bool AnyDialog => Dialogs.Any(d => d.Shown) || (OptionsManager.Instance?.paused ?? false);
    /// <summary> Whether any dialog that blocks movement is visible. </summary>
    public static bool AnyMovementBlocking => AnyDialog || NewMovement.Instance.dead || Movement.Instance.Emote != 0xFF;

    #region dialogs

    public static LobbyTab LobbyTab;
    public static PlayerList PlayerList;

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
        var root = Create("UI").transform;

        LobbyTab = new(root);
        PlayerList = new(root);

        Dialogs = new Fragment[] { LobbyTab, PlayerList };
        Fragments = new Fragment[] { };
        LeftGroup = new Fragment[] { LobbyTab, PlayerList };
        MidlGroup = new Fragment[] { };
    }

    /// <summary> Hides all of the elements in the given group except the fragment. </summary>
    public static void Hide(Fragment[] group, Fragment frag)
    {
        group.Each(f => f.Shown && f != frag, f => f.Toggle());
        if (group == MidlGroup)
        {
            OptionsManager.Instance.UnPause();
            WeaponWheel.Instance.gameObject.SetActive(false);
        }
    }
}
