namespace Jaket.UI.Lib;

using UnityEngine;

using Shape = UnityEngine.UI.Image.Type;

using Jaket.Assets;

/// <summary> List of textures used to build the interface. </summary>
public static class Tex
{
    /// <summary> Backgrounds. </summary>
    public static Sprite Fill, Back;
    /// <summary> Borders. </summary>
    public static Sprite BrdS, BrdL;
    /// <summary> Handles. </summary>
    public static Sprite Hort, Vert;
    /// <summary> Stripes. </summary>
    public static Sprite Dash, Mask;
    /// <summary> Checkmark. </summary>
    public static Sprite Mark;
    /// <summary> Common. </summary>
    public static Sprite Circle, Shadow, Dead, Flash;

    /// <summary> Loads the textures. </summary>
    public static void Load()
    {
        GameAssets.Sprite("Controls/Round_FillLarge.png",           s => Fill   = s);
        GameAssets.Sprite("Controls/Round_VertHandle_Invert 1.png", s => Back   = s);
        GameAssets.Sprite("Controls/Round_BorderSmall.png",         s => BrdS   = s);
        GameAssets.Sprite("Controls/Round_BorderLarge.png",         s => BrdL   = s);
        GameAssets.Sprite("Controls/Round_HorizHandle_Invert.png",  s => Hort   = s);
        GameAssets.Sprite("Controls/Round_VertHandle_Invert.png",   s => Vert   = s);
        GameAssets.Sprite("Controls/StripesMaskSM.png",             s => Dash   = s);
        GameAssets.Sprite("Controls/Round_SliderFill.png",          s => Mask   = s);
        GameAssets.Sprite("Controls/Check.png",                     s => Mark   = s);
        GameAssets.Sprite("circle.png",                             s => Circle = s);
        GameAssets.Sprite("weaponwheelbackground.png",              s => Shadow = s);
        GameAssets.Sprite("ISeeYou.png",                            s => Dead   = s);
        GameAssets.Sprite("s/muzzleflashshotgun 1.png",             s => Flash  = s);
    }

    /// <summary> Executes the task after loading sprites. </summary>
    public static void OnLoad(Runnable task) => Events.Post(() => Fill & Back & BrdS & BrdL & Hort & Vert & Dash & Mask & Mark & Circle & Shadow & Dead & Flash, task);

    /// <summary> Returns the default scale of the sprite. </summary>
    public static float Scale(Sprite sprite) => sprite switch
    {
        _ when sprite == Fill => 4.0f,
        _ when sprite == Back => 4.0f,
        _ when sprite == BrdS => 4.0f,
        _ when sprite == BrdL => 4.0f,
        _ when sprite == Hort => 4.4f,
        _ when sprite == Vert => 4.4f,
        _ when sprite == Dash => 2.0f,
        _ when sprite == Mask => 5.3f,
        _                     => 1.0f,
    };

    /// <summary> Returns the default shape of the sprite. </summary>
    public static Shape Type (Sprite sprite) => sprite switch
    {
        _ when sprite == Fill => Shape.Sliced,
        _ when sprite == Back => Shape.Sliced,
        _ when sprite == BrdS => Shape.Sliced,
        _ when sprite == BrdL => Shape.Sliced,
        _ when sprite == Hort => Shape.Sliced,
        _ when sprite == Vert => Shape.Sliced,
        _ when sprite == Dash => Shape.Tiled,
        _ when sprite == Mask => Shape.Sliced,
        _                     => Shape.Simple,
    };
}
