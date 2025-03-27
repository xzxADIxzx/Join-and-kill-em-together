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
    public static ColorBlock Colors = new() { normalColor = white, pressedColor = red, highlightedColor = gray, selectedColor = gray, disabledColor = dark };

    /// <summary> Creates a rect with the specified position, size and alignment. </summary>
    public static RectTransform Rect(string name, Transform parent, Rect rect) => Component<RectTransform>(Create(name, parent), rect.Apply);

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
    public static Image Image(Transform rect, Sprite sprite, Color color, ImageType type, bool fill) =>
        Component<Image>(rect.gameObject, i =>
        {
            i.sprite = sprite;
            i.color = color;
            i.type = type;
            i.fillCenter = fill;
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
}
