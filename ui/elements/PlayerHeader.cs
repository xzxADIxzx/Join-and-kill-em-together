namespace Jaket.UI.Elements;

using Steamworks;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Header containing nickname and health. </summary>
public class PlayerHeader
{
    /// <summary> Player name taken from steam. </summary>
    public string Name;
    /// <summary> Component containing the name. </summary>
    public Text Text;

    /// <summary> Canvas containing header content. </summary>
    private Transform canvas;
    /// <summary> Health images that the player directly sees. </summary>
    private RectTransform health, overhealth;

    public PlayerHeader(SteamId id, Transform parent)
    {
        // workaround for getting nickname
        Name = new Friend(id).Name;

        float width = Name.Length * 14f + 16f;
        canvas = UI.WorldCanvas("Header", parent, new(0f, 5f, 0f), action: canvas =>
        {
            UI.Table(Name, canvas, 0f, 0f, width, 40f, table => Text = UI.Text(Name, table, 0f, 0f, width, size: 24));

            UI.Table("Health Background", canvas, 0f, -28f, width - 16f, 4f);
            health = UI.Image("Health", canvas, 0f, -28f, width - 16f, 4f, Color.red).rectTransform;
            overhealth = UI.Image("Overhealth", canvas, 0f, -28f, width - 16f, 4f, Color.green).rectTransform;
        });
    }

    /// <summary> Updates the health and rotates the canvas towards the camera. </summary>
    public void Update(float hp)
    {
        Text.color = hp > 0f ? Color.white : Color.red;

        health.localScale = new(Mathf.Min(hp / 100f, 1f), 1f, 1f);
        overhealth.localScale = new(Mathf.Max((hp - 100f) / 100f, 0f), 1f, 1f);

        canvas.LookAt(Camera.main?.transform);
        canvas.Rotate(new(0f, 180f, 0f), Space.Self);
    }

    /// <summary> Hides the canvas. </summary>
    public void Hide() => canvas.gameObject.SetActive(false);
}
