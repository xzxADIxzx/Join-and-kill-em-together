namespace Jaket.UI.Lib;

using UnityEngine;

/// <summary> List of textures used to build the interface. </summary>
public static class Tex
{
    /// <summary> Backgrounds with cut corners and dashes. </summary>
    public static Sprite Fill, Back;
    /// <summary> Borders with cut corners and different size. </summary>
    public static Sprite Small, Large;
    /// <summary> Horizontal and vertical slider handle. </summary>
    public static Sprite Hort, Vert;
    /// <summary> Checkmark sprite for the checkbox. </summary>
    public static Sprite Mark;
    /// <summary> Hollow circle and circular shadow. </summary>
    public static Sprite Circle, Shadow;

    /// <summary> Loads the textures from memory. </summary>
    public static void Load()
    {
        var all = ResFind<Sprite>();
        Sprite Find(string name) => all.Find(s => s.name == name);

        Fill = Find("Round_FillLarge");
        Back = Find("Round_VertHandle_Invert 1");
        Small = Find("Round_BorderSmall");
        Large = Find("Round_BorderLarge");
        Hort = Find("Round_HorizHandle_Invert");
        Vert = Find("Round_VertHandle_Invert");
        Mark = Find("Check");
        Circle = Find("circle");
        Shadow = Find("weaponwheelbackground");
    }
}
