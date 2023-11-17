namespace Jaket.UI;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.EntityTypes;
using Jaket.World;

/// <summary> Indicators showing the location of teammates near the cursor. </summary>
public class PlayerIndicators : CanvasSingleton<PlayerIndicators>
{
    /// <summary> List of all indicator targets. </summary>
    public List<Transform> targets = new();
    /// <summary> List of indicators themselves. </summary>
    public List<Image> indicators = new();

    private void Start() => Events.OnTeamChanged += Rebuild;

    private void Update()
    {
        // update all indicators, nothing else to do huh
        for (int i = 0; i < targets.Count; i++) UpdateIndicator(targets[i], indicators[i]);
    }

    /// <summary> Toggles visibility of indicators. </summary>
    public void Toggle()
    {
        // if the player is typing, then nothing needs to be done
        if (Chat.Shown) return;

        gameObject.SetActive(Shown = !Shown);

        // no need to update indicators if we hide them
        if (Shown) Rebuild();
    }

    /// <summary> Rebuilds player indicators to match a new state. </summary>
    public void Rebuild()
    {
        // destroy all indicators and clear the lists
        indicators.ForEach(ind => Destroy(ind.gameObject));
        indicators.Clear();
        targets.Clear();

        // create new indicators for each player
        Networking.EachPlayer(AddIndicator);
    }

    /// <summary> Adds a new indicator pointing to the player. </summary>
    public void AddIndicator(RemotePlayer player)
    {
        // indicators should only point to teammates, so you can even play hide and seek
        if (player.team != Networking.LocalPlayer.Team && LobbyController.PvPAllowed) return;

        // save the player's transform in order to rotate an indicator towards it in the future
        targets.Add(player.transform);

        // create a new team color indicator and add it to the list
        var indicator = UI.Image("Indicator", transform, 0f, 0f, 88f, 88f, player.team.Data().Color(), circle: true);
        indicator.type = Image.Type.Filled;
        indicators.Add(indicator);
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
