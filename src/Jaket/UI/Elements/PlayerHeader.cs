namespace Jaket.UI.Elements;

using UnityEngine;
using UnityEngine.UI;

using static Pal;
using static Rect;

/// <summary> Header containing nickname and health. </summary>
public class PlayerHeader
{
    /// <summary> Player name taken from Steam. </summary>
    public string Name;
    /// <summary> Component containing the name. </summary>
    public Text Text;

    /// <summary> Canvas containing header content. </summary>
    private Transform canvas;
    /// <summary> Health images that the player directly sees. </summary>
    private RectTransform health, overhealth;
    /// <summary> Ellipsis indicating that the player is typing a message. </summary>
    private Text ellipsis;

    public PlayerHeader(uint id, Transform parent)
    {
        Name = Tools.Name(id);

        float width = Name.Length * 14f + 16f;
        canvas = UIB.WorldCanvas("Header", parent, new(0f, 5f, 0f), build: canvas =>
        {
            var n = Size(width, 40f);
            UIB.Table("Name", canvas, n, table => Text = UIB.Text(Name, table, n.ToText() * 10f, size: 240));
            Text.transform.localScale /= 10f;

            var h = Size(160f, 4f) with { y = -30f };
            UIB.Image("Background", canvas, h, black);

            health = UIB.Image("Health", canvas, h, red).rectTransform;
            overhealth = UIB.Image("Overhealth", canvas, h, green).rectTransform;

            var e = Size(48f, 24f) with { y = -30f };
            UIB.Table("Ellipsis", canvas, e with { Height = 18f }, table => ellipsis = UIB.Text("...", table, e with { y = 8f }));
        });
    }

    /// <summary> Updates the health and rotates the canvas towards the camera. </summary>
    public void Update(float hp, bool typing)
    {
        Text.color = hp > 0f ? white : red;

        health.localScale = new(Mathf.Min(hp / 100f, 1f), 1f, 1f);
        overhealth.localScale = new(Mathf.Max((hp - 100f) / 100f, 0f), 1f, 1f);

        canvas.LookAt(Camera.main?.transform);
        canvas.Rotate(Vector3.up * 180f, Space.Self);

        ellipsis.transform.parent.gameObject.SetActive(typing);
        if (typing)
        {
            int white = (int)Mathf.Floor(Time.time * 3f % 4);
            ellipsis.text = $"<b>{new string('.', white)}<color=grey>{new string('.', 3 - white)}</color></b>";
        }
    }

    /// <summary> Hides the canvas. </summary>
    public void Hide() => canvas.gameObject.SetActive(false);
}
