namespace Jaket.UI;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI.Extensions;

using Jaket.Assets;
using Jaket.World;

using static System.Array;

/// <summary> Wheel for selecting emotions that will be displayed as an animation of the player doll. </summary>
public class EmojiWheel : CanvasSingleton<EmojiWheel>
{
    /// <summary> Whether the second page of emoji wheel is open. </summary>
    public bool Second;

    /// <summary> An array containing the rotation of all segments in degrees. </summary>
    public static float[] SegmentRotations = { -30f, 0f, 30f, -30f, 0f, 30f };
    /// <summary> List of all wheel segments. Needed to change the color of elements and store icons. </summary>
    public List<WheelSegment> Segments = new();

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
        UI.CircleShadow("Shadow", transform);
        fill = UI.CircleImage("Fill", transform, 0f, 0f, 1f / 6f, 240, 0f);

        for (int i = 0; i < 6; i++)
        {
            float deg = 150f - i * 60f, rad = deg * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 200f;

            var segment = new WheelSegment
            {
                segment = UI.CircleImage("Segment " + i, transform, 150f, 150f, 1f / 6f, i * 60, 8f, true),
                divider = UI.CircleImage("Divider " + i, transform, 640f, 640f, .005f, i * 60, 245f),

                iconGlow = UI.Image("Glow", transform, pos.x, pos.y, 285f, 150f),
                icon = UI.Image("Icon", transform, pos.x, pos.y, 285f, 150f),
            };

            segment.icon.transform.localEulerAngles = segment.iconGlow.transform.localEulerAngles = new(0f, 0f, SegmentRotations[i]);
            segment.SetActive(false);

            Segments.Add(segment);
        }
        UpdateIcons();
    }

    private void Update()
    {
        // the weapon wheel should be unavailable while the emoji wheel is open
        WeaponWheel.Instance.gameObject.SetActive(false);

        // some code from the weapon wheel that I don't understand
        direction = Vector2.ClampMagnitude(direction + InputManager.Instance.InputSource.WheelLook.ReadValue<Vector2>(), 1f);
        float num = Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * 57.29578f + 90f, 360f);
        selected = direction.sqrMagnitude > 0f ? (int)(num / 60f) : selected;

        // update segments
        for (int i = 0; i < Segments.Count; i++) Segments[i].SetActive(i == selected);

        // update fill
        float progress = holdTime >= 0f && selected == 4 ? holdTime * 2f : 0f, size = 150f + progress * 500f;

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
        if (holdTime > .5f && selected == 4)
        {
            holdTime = -60f;

            Second = !Second;
            UpdateIcons();
        }
    }

    /// <summary> Updates the emoji icons if they are loaded, otherwise repeats the same actions after 5 seconds. </summary>
    public void UpdateIcons()
    {
        if (TrueForAll(DollAssets.EmojiIcons, tex => tex != null) && TrueForAll(DollAssets.EmojiGlows, tex => tex != null))
        {
            for (int i = 0; i < 6; i++)
            {
                int j = Second ? i + 6 : i; // if the wheel is on the second page, then need to take other icons
                Segments[i].icon.sprite = DollAssets.EmojiIcons[j];
                Segments[i].iconGlow.sprite = DollAssets.EmojiGlows[j];
            }
        }
        else Invoke("UpdateIcons", 5f);
    }

    /// <summary> Shows emoji selection wheel and resets the selected segment. </summary>
    public void Show()
    {
        // if another menu is open, then nothing needs to be done
        if (UI.AnyJaket()) return;

        // the wheel should be inaccessible in the tunnel between levels
        if (FinalRank.Instance.gameObject.activeInHierarchy || WeaponWheel.Instance.gameObject.activeSelf) return; // TODO remove WeaponWhee.activeSelf after moving controls to Movement

        gameObject.SetActive(Shown = true);
        CameraController.Instance.enabled = false;

        Second = false;
        if (Segments.Count > 0) UpdateIcons();

        lastSelected = selected = -1;
        direction = Vector2.zero;
    }

    /// <summary> Hides emoji selection wheel and starts the selected animation. </summary>
    public void Hide()
    {
        gameObject.SetActive(Shown = false);
        CameraController.Instance.enabled = true;

        // randomize RPS index if RPS emote is selected
        if (selected == 3 && !Second) Movement.Instance.Rps = (byte)Random.Range(0, 3);

        // play emote if the selected segment is not a page transition
        if (selected != 4) Movement.Instance.StartEmoji((byte)(Second ? selected + 6 : selected));
    }
}
