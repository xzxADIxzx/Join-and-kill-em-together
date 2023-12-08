namespace Jaket.UI;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI.Elements;

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
        if (!Shown || SceneHelper.CurrentScene == "Main Menu") return;

        // find teammates
        List<RemotePlayer> teammates = new();
        Networking.EachPlayer(player =>
        {
            // the player should only see information about teammates
            if (player.team == Networking.LocalPlayer.Team || !LobbyController.PvPAllowed) teammates.Add(player);
        });

        // build new table
        float height = teammates.Count == 0 ? 48f : teammates.Count * 56f + 8f;
        Root = UI.Table("Player Info", transform, 0f, 0f, 540f, height);

        Root.rectTransform.localPosition = new(-75f, -556f + height / 2f, 0f);
        Root.rectTransform.localRotation = Quaternion.identity;
        Root.rectTransform.localScale = Vector3.one;

        // build content
        if (teammates.Count == 0)
            UI.Text("<color=#D8D8D8>[<color=orange>There are no teammates in your team</color>]</color>", Root.rectTransform, 0f, 0f, 540f, size: 24);
        else
        {
            float y = height / 2f - 32f + (56f);
            teammates.ForEach(player =>
                PlayerInfoEntry.Build(player, UI.Rect(player.Header.Name, Root.rectTransform, 0f, y -= 56f, 524f, 48f)));
        }
    }
}
