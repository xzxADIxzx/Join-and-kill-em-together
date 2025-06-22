namespace Jaket.UI.Fragments;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;
using Jaket.UI.Lib;

/// <summary> Fragment that displays the location of all teammates. </summary>
public class PlayerIndicators : Fragment
{
    /// <summary> List of all indicator targets. </summary>
    private List<RemotePlayer> targets = new(8);
    /// <summary> List of indicators themselves. </summary>
    private List<UICircle> indicators = new(8);

    public PlayerIndicators(Transform root) : base(root, "PlayerIndicators", true, cond: () => Scene == "Main Menu")
    {
        Events.OnLobbyEnter += () => { if (!Shown) Toggle(); };
        Events.OnTeamChange += Rebuild;

        Component<Bar>(Content.gameObject, b => b.Update(() =>
        {
            // update indicators so that they always point to their respective targets
            for (int i = 0; i < targets.Count; i++) Update(targets[i], indicators[i]);
        }));
    }

    public override void Toggle()
    {
        base.Toggle();
        if (Shown) Rebuild();
    }

    public override void Rebuild()
    {
        Content.Each(Dest);
        targets.Clear();
        indicators.Clear();

        if (Scene != "Level 2-S" && Scene != "Intermission1" && Scene != "Intermission2") Networking.Entities.Player(p => p.Team.Ally(), Add);
    }

    /// <summary> Adds a new indicator pointing to the given player. </summary>
    public void Add(RemotePlayer player)
    {
        targets.Add(player);
        indicators.Add(Builder.Circle(Rect("Indicator", new(88f, 88f)), 0f, 0, 4f, color: player.Team.Color()));
    }

    /// <summary> Updates the size and rotation of the given indicator. </summary>
    public void Update(RemotePlayer target, UICircle indicator)
    {
        if (target == null || indicator == null) return;

        var dst = Vector3.Distance(NewMovement.Instance.transform.position, target.Position);
        var cam = CameraController.Instance.transform;
        var dir = target.Position - cam.position;

        indicator.Arc   = Mathf.Clamp(100f - dst, 5f, 100f) * .006f;
        indicator.color = indicator.color with { a = 1f - indicator.Arc * 1.5f };

        var projection  = Vector3.ProjectOnPlane(dir, cam.forward);
        var angle       = Vector3.SignedAngle(projection, cam.up, cam.forward);

        indicator.rectTransform.localEulerAngles = new(0f, 0f, 270f - angle + indicator.Arc * 180f);
    }
}
