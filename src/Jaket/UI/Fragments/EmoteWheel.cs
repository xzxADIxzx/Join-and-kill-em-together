namespace Jaket.UI.Fragments;

using System.Linq;
using UnityEngine;
using UnityEngine.UI.Extensions;

using Jaket.Assets;
using Jaket.World;

using static Rect;

/// <summary> Wheel for selecting emotes that will be displayed as an animation of the player doll. </summary>
public class EmoteWheel : CanvasSingleton<EmoteWheel>
{
    /// <summary> Array containing the rotations of all segments in degrees. </summary>
    public readonly static float[] SegmentRotations = { -30f, 0f, 30f, -30f, 0f, 30f };
    /// <summary> List of wheel segments. Needed to change the color of the elements and store icons. </summary>
    public readonly WheelSegment[] Segments = new WheelSegment[6];

    /// <summary> Whether the second page of the wheel is open. </summary>
    public bool Second;

    /// <summary> Id of the selected segment, it will be highlighted in red. </summary>
    private int lastSelected, selected;
    /// <summary> Cursor direction relative to wheel center. </summary>
    private Vector2 direction;

    /// <summary> Circle filling the 5th segment. </summary>
    private UICircle fill;
    /// <summary> How long is the current segment selected. </summary>
    private float holdTime;

    private void Start()
    {
        UIB.CircleShadow(transform);
        fill = UIB.CircleImage("Fill", transform, Size(0f, 0f), 1f / 6f, 240, 0f);
        UIB.CircleImage("White", transform, Size(154f, 154f), 1f, 0, 12f);

        for (int i = 0; i < 6; i++)
        {
            float rot = (150f - i * 60f) * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)) * 200f;

            var segment = Segments[i] = new WheelSegment
            {
                segment = UIB.CircleImage("Segment " + i, transform, Size(150f, 150f), 1f / 6f, i * 60, 8f),
                divider = UIB.CircleImage("Divider " + i, transform, Size(640f, 640f), 2f / 360f, i * 60 - 1, 255f),

                iconGlow = UIB.Image("Glow", transform, new(pos.x, pos.y, 284f, 150f)),
                icon = UIB.Image("Icon", transform, new(pos.x, pos.y, 284f, 150f)),
            };

            segment.icon.transform.localEulerAngles = segment.iconGlow.transform.localEulerAngles = new(0f, 0f, SegmentRotations[i]);
            segment.SetActive(false);
        }
    }

    private void Update()
    {
        // some code from the weapon wheel that I don't understand
        direction = Vector2.ClampMagnitude(direction + InputManager.Instance.InputSource.WheelLook.ReadValue<Vector2>() / 12f, 1f);
        float num = Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg + 90f, 360f);
        selected = direction.sqrMagnitude > .9f ? (int)(num / 60f) : selected;

        for (int i = 0; i < Segments.Length; i++) Segments[i].SetActive(i == selected);

        // progress of the transition to the next page
        float progress = holdTime >= 0f && selected == 4 ? holdTime * 2.5f : 0f, size = 150f + progress * 500f;

        fill.Thickness = progress * 250f;
        fill.color = new(1f, 0f, 0f, 1f - progress);
        fill.rectTransform.sizeDelta = new(size, size);

        // play sound
        if (lastSelected != selected)
        {
            lastSelected = selected;
            Instantiate(WeaponWheel.Instance.clickSound);

            holdTime = 0f;
        }
        else holdTime += Time.deltaTime;

        // turn page
        if (holdTime > .4f && selected == 4)
        {
            holdTime = -60f;

            Second = !Second;
            UpdateIcons();
        }
    }

    private void UpdateIcons()
    {
        if (ModAssets.EmoteIcons.All(t => t != null) && ModAssets.EmoteGlows.All(t => t != null))
        {
            for (int i = 0; i < 6; i++)
            {
                int j = Second ? i + 6 : i;
                Segments[i].icon.sprite = ModAssets.EmoteIcons[j];
                Segments[i].iconGlow.sprite = ModAssets.EmoteGlows[j];
            }
        }
        else Invoke("UpdateIcons", 5f);
    }

    /// <summary> Shows the wheel and resets the selected segment. </summary>
    public void Show()
    {
        if (!Shown) UI.HideCentralGroup();

        gameObject.SetActive(Shown = true);
        Movement.UpdateState();

        Second = false;
        Events.Post(UpdateIcons);

        lastSelected = selected = -1;
        direction = Vector2.zero;
    }

    /// <summary> Hides the wheel and starts the selected animation. </summary>
    public void Hide()
    {
        gameObject.SetActive(Shown = false);
        Movement.UpdateState();

        // randomize RPS index if RPS emote is selected
        if (selected == 3) Movement.Instance.Rps = (byte)Random.Range(0, 3);
        // play emote if the selected segment is not a page transition
        if (selected != 4) Movement.Instance.StartEmote((byte)(Second ? selected + 6 : selected));
    }
}
