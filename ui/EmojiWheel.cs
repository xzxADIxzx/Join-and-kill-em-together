namespace Jaket.UI;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using Jaket.Assets;
using Jaket.World;

using static System.Array;

/// <summary> Wheel for selecting emotions that will be displayed as an animation of the player doll. </summary>
[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class EmojiWheel : MonoSingleton<EmojiWheel>
{
    /// <summary> Whether emoji wheel is visible or hidden. </summary>
    public bool Shown;

    /// <summary> List of all wheel segments. Needed to change the color of elements and store icons. </summary>
    public List<WheelSegment> segments = new();
    /// <summary> Id of the selected segment, it will be highlighted in red. </summary>
    public int lastSelected, selected;
    /// <summary> Cursor direction relative to wheel center. </summary>
    public Vector2 direction;

    /// <summary> Creates a singleton of emoji wheel. </summary>
    public static void Build()
    {
        // initialize the singleton and create a canvas
        Utils.Canvas("Emoji Wheel", Plugin.Instance.transform).AddComponent<EmojiWheel>().gameObject.SetActive(false);

        // hide emoji wheel once loading a scene
        SceneManager.sceneLoaded += (scene, mode) => Instance.gameObject.SetActive(Instance.Shown = false);

        // build emoji wheel
        Utils.CircleShadow("Shadow", Instance.transform, 0f, 0f, 512f, 512f, 181f);

        for (int i = 0; i < 6; i++)
        {
            float deg = 150f - i * 60f, rad = deg * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 160f;

            var segment = new WheelSegment
            {
                segment = Utils.Circle("Segment " + i, Instance.transform, 0f, 0f, 150f, 150f, 1f / 6f, i * 60, 8f, true).GetComponent<UICircle>(),
                divider = Utils.Circle("Divider " + i, Instance.transform, 0f, 0f, 512f, 512f, .005f, i * 60, 181f, false).GetComponent<UICircle>(),

                iconGlow = Utils.Image("Glow", Instance.transform, pos.x, pos.y, 285f, 150f, Color.white).GetComponent<Image>(),
                icon = Utils.Image("Icon", Instance.transform, pos.x, pos.y, 285f, 150f, Color.white).GetComponent<Image>(),
            };

            segment.icon.rectTransform.localEulerAngles = new(0f, 0f, deg);
            segment.iconGlow.rectTransform.localEulerAngles = new(0f, 0f, deg);

            Instance.segments.Add(segment);
            segment.SetActive(false);
        }

        Instance.Invoke("UpdateIcons", 5f);
    }

    public void Update()
    {
        // some code from the weapon wheel that I don't understand
        direction = Vector2.ClampMagnitude(direction + InputManager.Instance.InputSource.WheelLook.ReadValue<Vector2>(), 1f);
        float num = Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * 57.29578f + 90f, 360f);
        selected = direction.sqrMagnitude > 0f ? (int)(num / 60f) : selected;

        // update segments
        for (int i = 0; i < segments.Count; i++) segments[i].SetActive(i == selected);

        // play sound
        if (lastSelected != selected)
        {
            lastSelected = selected;
            Instantiate(WeaponWheel.Instance.clickSound);
        }
    }

    /// <summary> Updates the emoji icons if they are loaded, otherwise repeats the same actions after 5 seconds. </summary>
    public void UpdateIcons()
    {
        if (TrueForAll(DollAssets.EmojiIcons, tex => tex != null) && TrueForAll(DollAssets.EmojiGlows, tex => tex != null))
        {
            for (int i = 0; i < 6; i++)
            {
                segments[i].icon.sprite = DollAssets.EmojiIcons[i];
                segments[i].iconGlow.sprite = DollAssets.EmojiGlows[i];
            }
        }
        else Instance.Invoke("UpdateIcons", 5f);
    }

    /// <summary> Shows emoji selection wheel and resets the selected segment. </summary>
    public void Show()
    {
        gameObject.SetActive(Shown = true);
        CameraController.Instance.enabled = false;

        lastSelected = selected = -1;
        direction = Vector2.zero;
    }

    /// <summary> Hides emoji selection wheel and starts the selected animation. </summary>
    public void Hide()
    {
        gameObject.SetActive(Shown = false);
        CameraController.Instance.enabled = true;

        Movement.Instance.StartEmoji((byte)selected);
    }
}