namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI.Extensions;

using ImageType = UnityEngine.UI.Image.Type;

using Jaket.Assets;
using Jaket.Input;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that provides the ability to choose emotes. </summary>
public class EmoteWheel : Fragment
{
    /// <summary> Storage of the interface elements and icons. </summary>
    private WheelSegment[] segments = new WheelSegment[6];
    /// <summary> Whether the second page of the wheel is open. </summary>
    private bool second;

    /// <summary> Index of the selected segment, it is painted in red. </summary>
    private int selected, lastSelected;
    /// <summary> Cursor direction relative to the center of the wheel. </summary>
    private Vector2 direction;

    /// <summary> Image filling the 5th segment. </summary>
    private UICircle fill;
    /// <summary> Hold time of the 5th segment. </summary>
    private float holdTime;

    public EmoteWheel(Transform root) : base(root, "EmoteWheel", false)
    {
        Builder.Circle(Rect("Shadow", new(640f, 640f)), 1f, 0, 245f, Tex.Shadow, black);
        Builder.Circle(Rect("Center", new(154f, 154f)), 1f, 0, 12f);

        fill = Builder.Circle(Rect("Fill", new(0f, 0f)), 1f / 6f, 240, 0f);

        for (int i = 0; i < 6; i++)
        {
            var rot = (150f - i * 60f) * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)) * 200f;

            var segment = segments[i] = new WheelSegment
            {
                segment = Builder.Circle(Rect("Segment", new(150f, 150f)), 1f /   6f, i * 60,       8f),
                divider = Builder.Circle(Rect("Divider", new(640f, 640f)), 2f / 360f, i * 60 - 1, 255f),

                iconGlow = Builder.Image(Rect("Glow", new(pos.x, pos.y, 284f, 150f)), null, white, ImageType.Sliced),
                icon     = Builder.Image(Rect("Icon", new(pos.x, pos.y, 284f, 150f)), null, white, ImageType.Sliced),
            };

            segment.icon.transform.localEulerAngles = segment.iconGlow.transform.localEulerAngles = new(0f, 0f, 30f * (i % 3) - 30f);
            segment.SetActive(false);
        }

        Component<Bar>(Content.gameObject, b => b.Update(() =>
        {
            // some code from the weapon wheel that I don't understand
            direction = Vector2.ClampMagnitude(direction + InputManager.Instance.InputSource.WheelLook.ReadValue<Vector2>() / 12f, 1f);
            float num = Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg + 90f, 360f);

            if (direction.sqrMagnitude > .9f) selected = (int)(num / 60f);

            for (int i = 0; i < segments.Length; i++) segments[i].SetActive(i == selected);

            // transition to the next page
            holdTime      += Time.deltaTime;
            float progress = holdTime >= 0f && selected == 4 ? holdTime * 2.5f : 0f;

            fill.Thickness = progress * (640f - 154f) / 2f;
            fill.color = new(1f, 0f, 0f, 1f - progress);
            fill.rectTransform.sizeDelta = Vector2.one * (154f + progress * 486f);

            if (lastSelected != selected)
            {
                lastSelected = selected;

                holdTime = 0f;
                Inst(WeaponWheel.Instance.clickSound);
            }
            if (holdTime >= .4f && selected == 4)
            {
                holdTime = -60f;

                second = !second;
                Rebuild();
            }
        }));
    }

    public override void Rebuild() => Events.Post(() => ModAssets.EmoteIcons.All(t => t != null) && ModAssets.EmoteGlows.All(t => t != null), () =>
    {
        for (int i = 0; i < 6; i++)
        {
            segments[i].icon.sprite     = ModAssets.EmoteIcons[second ? i + 6 : i];
            segments[i].iconGlow.sprite = ModAssets.EmoteGlows[second ? i + 6 : i];
        }
    });

    /// <summary> Shows the wheel and resets the selected segment. </summary>
    public void Show()
    {
        Content.gameObject.SetActive(Shown = true);
        UI.Hide(UI.MidlGroup, this, null);

        second = false;
        Rebuild();

        lastSelected = selected = -1;
        direction = Vector2.zero;
    }

    /// <summary> Hides the wheel and plays the selected emote. </summary>
    public void Hide()
    {
        Content.gameObject.SetActive(Shown = false);
        UI.Hide(UI.MidlGroup, this, null);

        // randomize RPS index if the corresponding emote is selected
        if (selected == 3) Emotes.Rps = (byte)Random.Range(0, 3);
        // play emote if the selected segment is not a page transition
        if (selected != 4) Emotes.Instance.Play((byte)(second ? selected + 6 : selected));
    }
}
