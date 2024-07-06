namespace Jaket.UI.Fragments;

using System.Collections.Generic;
using UnityEngine.UI;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI.Elements;

using static Rect;

/// <summary> Teammates information displayed in the bottom right corner of the screen. </summary>
public class PlayerInfo : CanvasSingleton<PlayerInfo>
{
    /// <summary> Object containing information about players. </summary>
    private Image root;

    private void Start()
    {
        Events.OnLobbyEntered += () => { if (!Shown) Toggle(); };
        Events.OnTeamChanged += Rebuild;
    }

    private void Update()
    {
        if (root) root.color = new(0f, 0f, 0f, PrefsManager.Instance.GetFloat("hudBackgroundOpacity") / 100f);
    }

    private void UpdateMaterials()
    {
        foreach (var img in root.GetComponentsInChildren<Image>()) img.material = HUDOptions.Instance.hudMaterial;
        foreach (var txt in root.GetComponentsInChildren<Text>()) txt.material = HUDOptions.Instance.hudMaterial;
    }

    /// <summary> Toggles visibility of the information table. </summary>
    public void Toggle()
    {
        Shown = !Shown;
        Rebuild();
    }

    /// <summary> Rebuilds the information table to match a new state. </summary>
    public void Rebuild()
    {
        if (root) Destroy(root.gameObject); // for some reason the operator ? doesn't work here
        if (!Shown || !StyleHUD.Instance) return;

        List<RemotePlayer> teammates = new();
        Networking.EachPlayer(player =>
        {
            // the player should only see information about teammates
            if (player.Team.Ally()) teammates.Add(player);
        });

        float height = teammates.Count == 0 ? 40f : teammates.Count * 48f + 8f;

        root = UIB.Table("Player Info", StyleHUD.Instance.transform, Size(540f, height));
        root.transform.localPosition = new(-75f, -556f + height / 2f, 0f);

        if (teammates.Count == 0)
            UIB.Text("#player-info.alone", root.transform, Size(540f, 40f));
        else
        {
            float y = -20f;
            teammates.ForEach(p => PlayerInfoEntry.Build(p, UIB.Rect(p.Header.Name, root.transform, Btn(y += 48f) with { Width = 540f })));
        }

        Events.Post2(UpdateMaterials);
    }
}
