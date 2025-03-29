namespace Jaket.UI.Lib;

using UnityEngine;

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

    /// <summary> Loads the textures from memory. </summary>
    public static void Load()
    {
        var all = ResFind<Sprite>();
        Sprite Find(string name) => all.Find(s => s.name == name);

        Fill = Find("Round_FillLarge");
        Back = Find("Round_VertHandle_Invert 1");
        Dash = Find("StripesMaskSM");
        Small = Find("Round_BorderSmall");
        Large = Find("Round_BorderLarge");
        Hort = Find("Round_HorizHandle_Invert");
        Vert = Find("Round_VertHandle_Invert");
        Mark = Find("Check");
        Mask = Find("Round_SliderFill");
        Circle = Find("circle");
        Shadow = Find("weaponwheelbackground");
    }

    /// <summary> Returns the scale of the given sprite. </summary>
    public static float Scale(Sprite sprite) => sprite == Mask ? 5f : new[] { Fill, Back, Dash, Small, Large }.Any(s => s == sprite) ? 4f : 1f;
}
