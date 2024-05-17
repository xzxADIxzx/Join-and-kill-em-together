namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.World;

using static Rect;

/// <summary> Skateboard stamina and speed. </summary>
public class Skateboard : CanvasSingleton<Skateboard>
{
    static Movement mm => Movement.Instance;
    static NewMovement nm => NewMovement.Instance;
    static ColorBlindSettings cb => ColorBlindSettings.Instance;

    /// <summary> Slider filler showing stamina. </summary>
    private Image fill;
    /// <summary> Text with stamina percentages. </summary>
    private Text stamina;

    /// <summary> Gradient along which the speed color changes. </summary>
    private Gradient gradient = new();
    /// <summary> Text with speedometer and info about dashes. </summary>
    private Text speed;

    private void Start()
    {
        // stamina
        var s = Size(320f, 320f) with { x = -32f };
        var color = cb.staminaChargingColor with { a = .8f };

        var background = UIB.Image("Background", transform, s, color, UIB.Circle, type: ImageType.Filled);
        background.transform.eulerAngles = new(0f, 0f, -30f);
        background.fillAmount = 1f / 3f;

        fill = UIB.Image("Fill", transform, s, color, UIB.Circle, type: ImageType.Filled);
        fill.transform.eulerAngles = new(0f, 0f, -30f);
        stamina = UIB.Text("Stamina", transform, s, align: TextAnchor.MiddleRight);

        // speed
        gradient.SetKeys(new GradientColorKey[]
        {
            new(new(0f, .9f, .4f), 0f),
            new(new(1f, .8f, .3f),.5f),
            new(new(1f, .2f, .1f), 1f),
        }, new GradientAlphaKey[0]);
        UIB.Table("Speed", transform, new(-400f, -100f, 258f, 56f), table => speed = UIB.Text("", table, Size(252f, 56f), align: TextAnchor.MiddleLeft));
    }

    private void Update()
    {
        // stamina
        fill.fillAmount = nm.boostCharge / 900f;
        fill.color = nm.boostCharge >= 100f ? cb.staminaColor : cb.staminaEmptyColor;

        stamina.text = $"{(int)(nm.boostCharge / 3f)}%";
        stamina.color = fill.color;

        float angle = 4f * Mathf.PI / 3f - fill.fillAmount * 2f * Mathf.PI;
        stamina.transform.localPosition = new(Mathf.Cos(angle) * 160f - 210f, Mathf.Sin(angle) * 160f, 0f);

        // speed
        speed.text = Bundle.Format("skateboard", ColorUtility.ToHtmlStringRGB(gradient.Evaluate(mm.SkateboardSpeed / 60f)), ((int)mm.SkateboardSpeed).ToString());
    }
}
