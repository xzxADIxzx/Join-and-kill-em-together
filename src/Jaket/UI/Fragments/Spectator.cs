namespace Jaket.UI.Fragments;

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.Input;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that is displayed when the player is dead. </summary>
public class Spectator : Fragment
{
    /// <summary> Information about keybindings that are usable at the moment. </summary>
    private Text info;
    /// <summary> Flash that displays the I See You texture. </summary>
    private Image dead;

    public Spectator(Transform root) : base(root, "Spectator", false)
    {
        info = Builder.Text(Fill("Info"), "", 96, white with { a = semi.a }, TextAnchor.MiddleCenter);
        dead = Builder.Image(Fill("Flash"), Tex.Dead, white, ImageType.Simple);

        Component<HudOpenEffect>(dead.gameObject, e =>
        {
            e.reverse = true;
            e.YFirst = true;
        });
    }

    public override void Toggle()
    {
        Content.gameObject.SetActive(Shown = NewMovement.Instance.dead);
        if (Shown)
        {
            bool
                cg = Scene == "Endless",
                zs = Scene == "Level 0-S";

            info.text = Bundle.Format("spect",
                Keybind.SpectNext.FormatValue(),
                Keybind.SpectPrev.FormatValue(),
                cg || zs
                    ? Bundle.Format("spect.special", cg ? "#spect.cg" : "#spect.zs")
                    : "#spect.default");
        }
    }
}
