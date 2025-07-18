namespace Jaket.UI.Elements;

using UnityEngine;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Net.Types;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Element that displays the state of a player. </summary>
public class Header
{
    static CameraController cc => CameraController.Instance;
    static ColorBlindSettings cb => ColorBlindSettings.Instance;

    /// <summary> Nickname of the player taken from Steam. </summary>
    public string Name;
    /// <summary> Width of the label displaying nickname. </summary>
    public float Width => Name.Length * 141f + 160f;

    /// <summary> Player itself whose state is displayed. </summary>
    public RemotePlayer Player;
    /// <summary> Transform to build the header canvas in. </summary>
    public Transform Root;

    public Header(RemotePlayer player)
    {
        Name = Name(player.Id);
        Player = player;
    }

    /// <summary> Assigns the header to the given transform. </summary>
    public void Assign(Transform root) => Builder.WorldCanvas(Create("Header", Root = root).transform, Vector3.up * 4.8f, c =>
    {
        RectTransform Slider(Color color) => Builder.Image(Builder.Rect("Slider", c, new(0f, -120f, 1600f, 40f)), Tex.Fill, color, ImageType.Sliced, 2f).rectTransform;
        RectTransform
            background = Slider(invi),
            normhealth = Slider(cb.healthBarColor),
            overhealth = Slider(cb.overHealColor);

        var nicknameBg = Builder.Image(Builder.Rect("Nickname", c, new(0f, 120f, Width, 360f)), Tex.Fill, invi, ImageType.Sliced, .5f).rectTransform;
        var ellipsisBg = Builder.Image(Builder.Rect("Ellipsis", c, new(0f, -120f, 400f, 120f)), Tex.Fill, invi, ImageType.Sliced, .9f).rectTransform;

        var nickname = Builder.Text(Builder.Rect("Text", nicknameBg, Lib.Rect.Fill), "", 240, white, TextAnchor.MiddleCenter);
        var ellipsis = Builder.Text(Builder.Rect("Text", ellipsisBg, Lib.Rect.Huge), "", 240, white, TextAnchor.MiddleCenter);

        Component<Bar>(c.gameObject, b => b.Update(() =>
        {
            int health = Player.Health, dots = (int)(Time.time * 3f) % 4;

            ellipsisBg.gameObject.SetActive(Player.Typing);

            nickname.text = $"<color={(health > 0 ? White : Red)}>{Name}</color>";
            ellipsis.text = $"<b>{new string('.', dots)}<color={Gray}>{new string('.', 3 - dots)}</color></b>";

            normhealth.sizeDelta = new(health <= 000 ? 0f : 16f * Mathf.Clamp(health - 000, 3, 100), 40f);
            overhealth.sizeDelta = new(health <= 100 ? 0f : 16f * Mathf.Clamp(health - 100, 3, 100), 40f);

            c.LookAt(cc?.transform);
            c.Rotate(Vector3.up * 180f, Space.Self);
        }));
    });

    /// <summary> Hides the header by destroying the canvas. </summary>
    public void Hide() => Dest(Root.Find("Header").gameObject);
}
