namespace Jaket.UI;

using UnityEngine;

/// <summary> Structure containing the position of an interface element. </summary>
public struct Rect
{
    /// <summary> Huge rectangle used for high quality text. </summary>
    public static readonly Rect Huge = Size(4200f, 4200f);
    /// <summary> Rect at the center of the current with the same size. </summary>
    public readonly Rect Text => new(0f, 1f, Width, Height);

    /// <summary> Position relative to the anchor. </summary>
    public float x, y;
    /// <summary> Constant size in pixels. </summary>
    public float Width, Height;
    /// <summary> Position of the anchor. </summary>
    public Vector2 Min, Max;

    public Rect(float x, float y, float width, float height, Vector2 min, Vector2 max)
    { this.x = x; this.y = y; Width = width; Height = height; Min = min; Max = max; }

    public Rect(float x, float y, float width, float height, Vector2 anchor) : this(x, y, width, height, anchor, anchor) { }

    public Rect(float x, float y, float width, float height) : this(x, y, width, height, new(.5f, .5f), new(.5f, .5f)) { }

    #region common

    public static Rect Tlw(float y, float height) => new(16f + 336f / 2f, -y, 336f, height, new(0f, 1f));

    public static Rect Blw(float y, float height) => new(16f + 336f / 2f, y, 336f, height, new(0f, 0f));

    public static Rect Btn(float y) => new(0f, -y, 320f, 40f, new(.5f, 1f));

    public static Rect Stn(float y, float shift) => new(shift / 2f, -y, 320f - Mathf.Abs(shift), 40f, new(.5f, 1f));

    public static Rect Tgl(float y) => new(0f, -y, 320f, 32f, new(.5f, 1f));

    public static Rect Sld(float y) => new(0f, -y, 320f, 16f, new(.5f, 1f));

    public static Rect Icon(float x, float y) => new(x, -y, 40f, 40f, new(.5f, 1f));

    public static Rect Size(float w, float h) => new(0f, 0f, w, h);

    #endregion
    #region chat & other

    public static Rect Blh(float width) => new(0f, 0f, width, 32f, new(0f, 0f));

    public static Rect Msg(float width) => new(0f, 0f, width, 32f, new(.5f, 0f));

    public static Rect Deb(int x) => new(184f + 352f * x, 296f, 336f, 136f, new(0f, 0f), new(0f, 0f));

    #endregion
}
