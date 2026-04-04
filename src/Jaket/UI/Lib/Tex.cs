namespace Jaket.UI.Lib;

using UnityEngine;

using Jaket.Assets;

/// <summary> List of textures used to build the interface. </summary>
public static class Tex
{
    /// <summary> Backgrounds with cut corners and different amount of dashes. </summary>
    public static Sprite Fill, Back, Dash;
    /// <summary> Borders with cut corners and different size. </summary>
    public static Sprite Small, Large;
    /// <summary> Horizontal and vertical slider handle. </summary>
    public static Sprite Hort, Vert;
    /// <summary> Checkmark sprite for the checkbox, and mask sprite for the slider filler. </summary>
    public static Sprite Mark, Mask;
    /// <summary> Hollow circle and circular shadow. </summary>
    public static Sprite Circle, Shadow;
    /// <summary> I see you. </summary>
    public static Sprite Dead;

    /// <summary> Loads the textures from memory. </summary>
    public static void Load()
    {
        GameAssets.Sprite("Controls/Round_FillLarge.png",           s => Fill   = s);
        GameAssets.Sprite("Controls/Round_VertHandle_Invert 1.png", s => Back   = s);
        GameAssets.Sprite("Controls/StripesMaskSM.png",             s => Dash   = s);
        GameAssets.Sprite("Controls/Round_BorderSmall.png",         s => Small  = s);
        GameAssets.Sprite("Controls/Round_BorderLarge.png",         s => Large  = s);
        GameAssets.Sprite("Controls/Round_HorizHandle_Invert.png",  s => Hort   = s);
        GameAssets.Sprite("Controls/Round_VertHandle_Invert.png",   s => Vert   = s);
        GameAssets.Sprite("Controls/Check.png",                     s => Mark   = s);
        GameAssets.Sprite("Controls/Round_SliderFill.png",          s => Mask   = s);
        GameAssets.Sprite("circle.png",                             s => Circle = s);
        GameAssets.Sprite("weaponwheelbackground.png",              s => Shadow = s);
        GameAssets.Sprite("ISeeYou.png",                            s => Dead   = s);
    }

    /// <summary> Executes the task after loading sprites. </summary>
    public static void OnLoad(Runnable task) => Events.Post(() => Fill & Back & Dash & Small & Large & Hort & Vert & Mark & Mask & Circle & Shadow & Dead, task);

    /// <summary> Returns the scale of the given sprite. </summary>
    public static float Scale(Sprite sprite) => sprite == Mask ? 5f : new[] { Fill, Back, Dash, Small, Large, Vert }.Any(s => s == sprite) ? 4f : 1f;
}
