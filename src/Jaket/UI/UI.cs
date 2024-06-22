namespace Jaket.UI;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Jaket.UI.Dialogs;
using Jaket.UI.Fragments;
using Jaket.World;

/// <summary> Class that loads and manages the interface of the mod. </summary>
public class UI
{
    /// <summary> Whether the player is focused on a input field. </summary>
    public static bool Focused => Focus != null && Focus.TryGetComponent<InputField>(out var f) && f.isActiveAndEnabled;
    /// <summary> Whether the player is in any of Jaket dialog. </summary>
    public static bool AnyDialog => Chat.Shown || LobbyTab.Shown || LobbyList.Shown || PlayerList.Shown || Settings.Shown || SpraySettings.Shown || (OptionsManager.Instance?.paused ?? false);
    /// <summary> Whether any interface that blocks movement is currently visible. </summary>
    public static bool AnyMovementBlocking => AnyDialog || NewMovement.Instance.dead || Movement.Instance.Emoji != 0xFF;

    /// <summary> Object on which the player is focused. </summary>
    public static GameObject Focus => EventSystem.current?.currentSelectedGameObject;
    /// <summary> Object containing the entire interface. </summary>
    public static Transform Root;

    /// <summary> Creates singleton instances of fragments and dialogs. </summary>
    public static void Load()
    {
        Root = Tools.Create("UI").transform;
        Settings.Load(); // settings must be loaded before building the interface

        Chat.Build("Chat", true, true, hide: () => Chat.Instance.Field?.gameObject.SetActive(Chat.Shown = false));
        LobbyTab.Build("Lobby Tab", false, true);
        LobbyList.Build("Lobby List", false, true);
        PlayerList.Build("Player List", false, true);
        Settings.Build("Settings", false, true);
        SpraySettings.Build("Spray Settings", false, true);
        Debugging.Build("Debugging Menu", false, false);

        PlayerIndicators.Build("Player Indicators", false, false, scene => scene == "Main Menu");
        PlayerInfo.Build("Player Information", false, false, scene => scene == "Main Menu", () => { if (PlayerInfo.Shown) PlayerInfo.Instance.Toggle(); });
        EmojiWheel.Build("Emoji Wheel", false, false);
        Skateboard.Build("Skateboard", false, false);
        MainMenuAccess.Build("Main Menu Access", false, true, hide: () => MainMenuAccess.Instance.Toggle());
        InteractiveGuide.Build("Interactive Guide", false, false, hide: () => InteractiveGuide.Instance.OfferAssistance());
        Teleporter.Build("Teleporter", false, false, hide: () => { });
    }

    /// <summary> Hides the interface of the left group. </summary>
    public static void HideLeftGroup()
    {
        if (LobbyTab.Shown) LobbyTab.Instance.Toggle();
        if (PlayerList.Shown) PlayerList.Instance.Toggle();
        if (Settings.Shown) Settings.Instance.Toggle();
    }

    /// <summary> Hides the interface of the central group. </summary>
    public static void HideCentralGroup()
    {
        if (LobbyList.Shown) LobbyList.Instance.Toggle();
        if (SpraySettings.Shown) SpraySettings.Instance.Toggle();
        if (EmojiWheel.Shown) EmojiWheel.Instance.Hide();
        if (OptionsManager.Instance.paused) OptionsManager.Instance.UnPause();
    }
}
