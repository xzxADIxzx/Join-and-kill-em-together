namespace Jaket.UI.Fragments;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

using static Rect;

/// <summary> Indicators showing the location of teammates near the cursor. </summary>
public class PlayerIndicators : CanvasSingleton<PlayerIndicators>
{
    /// <summary> List of all indicator targets. </summary>
    private List<Transform> targets = new();
    /// <summary> List of indicators themselves. </summary>
    private List<Image> indicators = new();

    protected override void Awake()
    {
        Events.OnLobbyEntered += () => { if (!Shown) Toggle(); };
        Events.OnTeamChanged += Rebuild;
    }

    private void Update()
    {
        // update all indicators, nothing else to do huh
        for (int i = 0; i < targets.Count; i++) UpdateIndicator(targets[i], indicators[i]);
    }

    /// <summary> Toggles visibility of indicators. </summary>
    public void Toggle()
    {
        gameObject.SetActive(Shown = !Shown);
        if (Shown) Rebuild();
    }

    /// <summary> Rebuilds player indicators to match a new state. </summary>
    public void Rebuild()
    {
        indicators.ForEach(ind => Destroy(ind.gameObject));
        indicators.Clear();
        targets.Clear();

        // create new indicators for each player
        Networking.EachPlayer(AddIndicator);
        Update();
    }

    /// <summary> Adds a new indicator pointing to the player. </summary>
    public void AddIndicator(RemotePlayer player)
    {
        // indicators should only point to teammates, so you can even play hide and seek
        if (!player.Team.Ally()) return;

        targets.Add(player.transform);
        indicators.Add(UIB.Image(player.Header.Name, transform, Size(88f, 88f), player.Team.Color(), UIB.Circle, type: ImageType.Filled));
    }

    /// <summary> Updates the size and rotation of the indicator. </summary>
    public void UpdateIndicator(Transform target, Image indicator)
    {
        // the target can be removed by the game, so just in case, let it be
        if (target == null || indicator == null) return;

        // change indicator size based on distance
        var dst = Vector3.Distance(NewMovement.Instance.transform.position, target.position);
        indicator.fillAmount = Mathf.Clamp(100f - dst, 5f, 100f) * .006f;

        // change indicator color based on distance
        var clr = indicator.color;
        clr.a = 1f - indicator.fillAmount * 1.5f;
        indicator.color = clr;

        // find the direction from the player to the target
        var cam = CameraController.Instance.transform;
        var dir = target.position + new Vector3(0f, 2.5f, 0f) - cam.position;

        // project this direction onto the camera plane, after which find the angle between the camera's up direction and the projected vector
        var projected = Vector3.ProjectOnPlane(dir, cam.forward);
        var angle = Vector3.SignedAngle(projected, cam.up, cam.forward);

        // turn the indicator towards the target
        indicator.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f - angle + indicator.fillAmount * 180f);
    }
}
