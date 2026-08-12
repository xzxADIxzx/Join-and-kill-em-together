namespace Jaket.UI.Elements;

using UnityEngine;

using Jaket.Net.Types;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Element that displays the state of a player. </summary>
public class Header
{
    /// <summary> Nickname of the player taken from Steam. </summary>
    public string Name;
    /// <summary> Transform to build the header canvas in. </summary>
    public Transform Root;

    /// <summary> Assigns the given player to the header. </summary>
    public void Assign(RemotePlayer player) => Builder.Canvas(Create("Header", Root = player.Doll.Root).transform, Vector3.up * 4.6f, c =>
    {
        var cc = CameraController.Instance;
        var cb = ColorBlindSettings.Instance;

        var name = Name = player.Id.Name;
        var wdth = Name.Length * 141f + 160f;

        RectTransform Bar(Color color) => Builder.Image(Builder.Rect("Bar", c, new(0f, -120f, 1600f, 40f)), Tex.Fill, color, scale: 2f).rectTransform;
        RectTransform
            background = Bar(invi),
            normhealth = Bar(cb.healthBarColor),
            overhealth = Bar(cb.overHealColor);

        var nicknameBg = Builder.Image(Builder.Rect("Nickname", c, new(0f, +120f, wdth, 360f)), Tex.Fill, invi, scale: .5f);
        var ellipsisBg = Builder.Image(Builder.Rect("Ellipsis", c, new(0f, -120f, 400f, 120f)), Tex.Fill, invi, scale: .9f);

        var nickname = Builder.Text(Builder.Rect("Text", nicknameBg, new()), name, 240, white);
        var ellipsis = Builder.Text(Builder.Rect("Text", ellipsisBg, new()), "hi", 240, white);

        ellipsis.horizontalOverflow = HorizontalWrapMode.Overflow;
        ellipsis.verticalOverflow   = VerticalWrapMode  .Overflow;

        c.Component<Bar>(b => b.Update(() =>
        {
            int health = player.Health, dots = (int)(Time.time * 3f) % 4;

            ellipsisBg.gameObject.SetActive(player.Typing);

            nickname.color = health > 0 ? white : red;
            ellipsis.text = $"<b>{new string('.', dots)}<color={Gray}>{new string('.', 3 - dots)}</color></b>";

            normhealth.sizeDelta = new(health <= 000 ? 0f : 16f * Mathf.Clamp(health - 000, 3, 100), 40f);
            overhealth.sizeDelta = new(health <= 100 ? 0f : 16f * Mathf.Clamp(health - 100, 3, 100), 40f);

            c.LookAt(cc?.transform);
            c.Rotate(Vector3.up * 180f, Space.Self);
        }));
    });

    /// <summary> Shows the header. </summary>
    public void Show() => Root.Find("Header").gameObject.SetActive(true);

    /// <summary> Hides the header. </summary>
    public void Hide() => Root.Find("Header").gameObject.SetActive(false);
}
