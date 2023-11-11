namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;

/// <summary> Teammates information displayed in the bottom right corner of the screen. </summary>
public class PlayerInfo : CanvasSingleton<PlayerInfo>
{
    /// <summary> Object containing information about players. </summary>
    private Image root;
    /// <summary> For some reason the operator ? does not work with destroyed objects. </summary>
    public Image Root { get => root == null ? null : root; set => root = value; }

    private void Update()
    {
        if (Root != null) Root.color = new(0f, 0f, 0f, PrefsManager.Instance.GetFloat("hudBackgroundOpacity") / 100f);
    }

    /// <summary> Toggles visibility of information. </summary>
    public void Toggle()
    {
        // if the player is typing, then nothing needs to be done
        if (Chat.Shown) return;

        Shown = !Shown;
        Rebuild();
    }

    /// <summary> Rebuilds info entries to match a new state. </summary>
    public void Rebuild()
    {
        // destroy old information
        Destroy(Root?.gameObject);
        if (!Shown) return;

        // build new table
        Root = UI.Table("Player Info", transform, 0f, 0f, 540f, 80f);

        float height = LobbyController.Lobby == null
            ? 48f
            : LobbyController.Lobby.Value.MemberCount == 1
                ? 48f
                : (LobbyController.Lobby.Value.MemberCount - 1) * 56f + 8f;

        Root.rectTransform.localPosition = new(-75f, -556f + height / 2f, 0f);
        Root.rectTransform.localRotation = Quaternion.identity;
        Root.rectTransform.localScale = Vector3.one;

        // build content
        if (LobbyController.Lobby == null || LobbyController.Lobby?.MemberCount == 1)
            UI.Text("<color=#D8D8D8>[<color=orange>There are no players in the lobby</color>]</color>", Root.rectTransform, 0f, 0f, 540f, size: 24);
    }
}
