namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

using Jaket.World;

/// <summary> Skateboard stamina and speed. </summary>
public class Skateboard : CanvasSingleton<Skateboard>
{
    /// <summary> Slider filler showing stamina. </summary>
    private Image fill;
    /// <summary> Text with stamina percentages. </summary>
    private Text stamina;

    /// <summary> Gradient along which the speed changes. </summary>
    private Gradient gradient = new();
    /// <summary> Text with speedometer and info about dashes. </summary>
    private Text speed;

    private void Start()
    {
        // stamina
        var color = ColorBlindSettings.Instance.staminaChargingColor;
        color.a = .8f;

        var background = UI.Image("Background", transform, -32f, 0f, 320f, 320f, color, circle: true);
        background.transform.eulerAngles = new(0f, 0f, -30f);
        background.fillAmount = 1f / 3f;

        fill = UI.Image("Fill", transform, -32f, 0f, 320f, 320f, circle: true);
        fill.transform.eulerAngles = new(0f, 0f, -30f);
        stamina = UI.Text("Stamina", transform, 0f, 0f, align: TextAnchor.MiddleRight);

        // speed
        gradient.SetKeys(new GradientColorKey[]
        {
            new(new(0f, .9f, .4f), 0f),
            new(new(1f, .8f, .3f),.5f),
            new(new(1f, .2f, .1f), 1f),
        }, new GradientAlphaKey[0]);
        UI.Table("Speed", transform, -444f, -102f, 358f, 72f, table => speed = UI.Text("", table, 0f, 2f, 342f, 60f, align: TextAnchor.MiddleLeft));
    }

    private void Update()
    {
        // stamina
        fill.fillAmount = NewMovement.Instance.boostCharge / 900f;
        fill.color = NewMovement.Instance.boostCharge >= 100f ? ColorBlindSettings.Instance.staminaColor : ColorBlindSettings.Instance.staminaEmptyColor;

        stamina.text = $"{(int)(NewMovement.Instance.boostCharge / 3f)}%";
        stamina.color = fill.color;

        float angle = 4f * Mathf.PI / 3f - fill.fillAmount * 2f * Mathf.PI;
        stamina.transform.localPosition = new(Mathf.Cos(angle) * 160f - 210f, Mathf.Sin(angle) * 160f, 0f);

        // speed
        int value = Movement.Instance.SkateboardSpeed();
        var color = ColorUtility.ToHtmlStringRGB(gradient.Evaluate(value / 60f));
        speed.text = $"Speed: <color=#{color}>{value}</color><color=#D8D8D8>U/s</color>\n<color=#cccccccc>[Dash to speed up]</color>";
    }
}
