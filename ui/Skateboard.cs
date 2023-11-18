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

    private void Start()
    {
        var color = ColorBlindSettings.Instance.staminaChargingColor;
        color.a = .8f;

        var background = UI.Image("Background", transform, -32f, 0f, 320f, 320f, color, circle: true);
        background.transform.eulerAngles = new(0f, 0f, -30f);
        background.fillAmount = 1f / 3f;

        fill = UI.Image("Fill", transform, -32f, 0f, 320f, 320f, circle: true);
        fill.transform.eulerAngles = new(0f, 0f, -30f);
        stamina = UI.Text("Stamina", transform, 0f, 0f);
    }

    private void Update()
    {
        fill.fillAmount = NewMovement.Instance.boostCharge / 900f;
        fill.color = NewMovement.Instance.boostCharge >= 100f ? ColorBlindSettings.Instance.staminaColor : ColorBlindSettings.Instance.staminaEmptyColor;

        stamina.text = $"{(int)(NewMovement.Instance.boostCharge / 3f)}%";
        stamina.color = fill.color;

        float angle = 4f * Mathf.PI / 3f - fill.fillAmount * 2f * Mathf.PI;
        stamina.transform.localPosition = new(Mathf.Cos(angle) * 160f - 96f, Mathf.Sin(angle) * 160f, 0f);
    }
}
