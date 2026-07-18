namespace Jaket.UI.Lib;

using UnityEngine;

/// <summary> Structure that represets the position, size, and alignment of an interface element. </summary>
public struct Rect
{
    /// <summary> Position of the element relative to the anchor and its constant size in pixels. </summary>
    public float X, Y, Width, Height;
    /// <summary> Position of the anchor. </summary>
    public Vector2 Min, Max;

    public Rect(float x, float y, float width, float height, Vector2 min, Vector2 max) { X = x; Y = y; Width = width; Height = height; Min = min; Max = max; }

    public Rect(float x, float y, float width, float height, Vector2 anchor) : this(x, y, width, height, anchor, anchor) { }

    public Rect(float x, float y, float width, float height) : this(x, y, width, height, new(.5f, .5f)) { }

    public Rect(float width, float height) : this(0f, 0f, width, height) { }

    public Rect() : this(0f, 0f, 0f, 0f, new(0f, 0f), new(1f, 1f)) { }

    /// <summary> Applies position, size, and alignment. </summary>
    public readonly void Apply(RectTransform rect)
    {
        rect.anchorMin = Min;
        rect.anchorMax = Max;
        rect.anchoredPosition = new(X, Y);
        rect.sizeDelta = new(Width, Height);
    }
}
