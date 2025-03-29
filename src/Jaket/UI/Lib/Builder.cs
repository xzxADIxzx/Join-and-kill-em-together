namespace Jaket.UI.Lib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using ImageType = UnityEngine.UI.Image.Type;
using ScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode;

using Jaket.Assets;

using static Pal;

/// <summary> Set of different tools for building complex interface elements. </summary>
public static class Builder
{
    #region base

    /// <summary> Set of colors for buttons. </summary>
    public static ColorBlock Colors = new()
    {
        normalColor = white,
        pressedColor = red,
        highlightedColor = gray,
        selectedColor = gray,
        disabledColor = dark,

        colorMultiplier = 1f,
        fadeDuration = .1f
    };

    /// <summary> Creates a rect with the specified position, size and alignment. </summary>
    public static RectTransform Rect(string name, Transform parent, Rect rect) => Component<RectTransform>(Create(name, parent), rect.Apply);

    /// <summary> Creates a mask that cuts off a part of the interface. </summary>
    public static Mask Mask(Transform rect, Sprite sprite) =>
        Component<Mask>(rect.gameObject, m =>
        {
            m.showMaskGraphic = false;
            Image(rect, sprite, white, ImageType.Sliced);
        });

    /// <summary> Creates a label with the given text, size, color and alignment. </summary>
    public static Text Text(Transform rect, string text, int size, Color color, TextAnchor align) =>
        Component<Text>(rect.gameObject, t =>
        {
            t.text = text == string.Empty || text[0] != '#' ? text : Bundle.Get(text[1..]);
            t.font = ModAssets.Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
        });

    #endregion
    #region image

    /// <summary> Creates an image with the given sprite, color and type. </summary>
    public static Image Image(Transform rect, Sprite sprite, Color color, ImageType type) =>
        Component<Image>(rect.gameObject, i =>
        {
            i.sprite = sprite;
            i.color = color;
            i.type = type;
            i.pixelsPerUnitMultiplier = Tex.Scale(sprite);
        });

    /// <summary> Creates a circular image with the given parameters. </summary>
    public static UICircle Circle(Transform rect, float arc, int rotation, float thickness) =>
        Component<UICircle>(rect.gameObject, i =>
        {
            i.Arc = arc;
            i.ArcRotation = rotation;
            i.Thickness = thickness;
            i.Fill = false;
        });

    /// <summary> Adds a diamond-shaped image with the given parameters. </summary>
    public static DiamondGraph Diamond(Transform rect, Color color, float a, float b, float c, float d) =>
        Component<DiamondGraph>(rect.gameObject, i =>
        {
            i.color = color;
            i.A = a; i.B = b; i.C = c; i.D = d;
        });

    #endregion
    #region button

    /// <summary> Creates a button with the given sprite, color and callback. </summary>
    public static Button Button(Transform rect, Sprite sprite, Color color, Runnable callback) =>
        Component<Button>(rect.gameObject, b =>
        {
            b.targetGraphic = Image(rect, sprite, color, ImageType.Sliced);
            b.colors = Colors;
            b.onClick.AddListener(callback.Invoke);
        });

    /// <summary> Creates a button with the given sprite, color, text, size, alignment and callback. </summary>
    public static Button TextButton(Transform rect, Sprite sprite, Color color, string text, int size, TextAnchor align, Runnable callback) =>
        Component<Button>(rect.gameObject, b =>
        {
            b.targetGraphic = Image(rect, sprite, color, ImageType.Sliced);
            b.colors = Colors;
            b.onClick.AddListener(callback.Invoke);

            var subrect = Rect("Text", rect, Lib.Rect.Fill);
            var textclr = sprite == Tex.Fill ? white : color;

            Text(subrect, text, size, textclr, align).alignByGeometry = true;
        });

    #endregion
    #region toggle

    /// <summary> Creates a color block with transparency based on the given toggle state. </summary>
    public static ColorBlock GetColor(bool active) => Colors with { colorMultiplier = active ? 1f : 0f };

    /// <summary> Creates a toggle with the given text, size, color and callback. </summary>
    public static Toggle Toggle(Transform rect, string text, int size, Color color, Cons<bool> callback) =>
        Component<Toggle>(rect.gameObject, t =>
        {
            var subrect1 = Rect("Checkbox", rect, new(-16f, 0f, 32f, 32f, new(1f, .5f)));
            var checkbox = Image(subrect1, Tex.Small, color, ImageType.Sliced);

            var subrect2 = Rect("Checkmark", subrect1, new(16f, 16f));
            var checkmrk = Image(subrect2, Tex.Mark, color, ImageType.Simple);

            t.targetGraphic = checkmrk;
            t.colors = GetColor(false);
            t.onValueChanged.AddListener(value => t.colors = GetColor(value));
            t.onValueChanged.AddListener(callback.Invoke);

            Text(rect, text, size, color, TextAnchor.MiddleLeft).alignByGeometry = true;
        });

    #endregion
    #region slider

    /// <summary> Creates a slider with the given range, color and callback. </summary>
    public static Slider Slider(Transform rect, int min, int max, Color color, Cons<int> callback) =>
        Component<Slider>(rect.gameObject, s =>
        {
            var area = Rect("Area", rect, new(-16f, 0f, -48f, -16f, Vector2.zero, Vector2.one));
            var mask = Rect("Mask", area, Lib.Rect.Fill);
            var fill = Rect("Fill", mask, Lib.Rect.Fill);

            Mask(mask, Tex.Mask);
            Image(fill, Tex.Dash, dark, ImageType.Tiled);
            s.fillRect = mask;

            var zone = Rect("Zone", rect, new(0f, 0f, -48f, -16f, Vector2.zero, Vector2.one));
            var hand = Rect("Hand", zone, Lib.Rect.Fill with { Width = 32f });

            s.targetGraphic = Image(hand, Tex.Hort, color, ImageType.Sliced);
            s.colors = Colors;
            s.handleRect = hand;

            s.wholeNumbers = true;
            s.minValue = min;
            s.maxValue = max;
            s.onValueChanged.AddListener(_ => callback((int)_));

            Image(rect, Tex.Large, color, ImageType.Sliced);
        });

    #endregion
}
