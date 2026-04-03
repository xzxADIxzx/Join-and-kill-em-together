namespace Jaket.UI.Lib;

using UnityEngine;
using UnityEngine.AddressableAssets;

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
        static Sprite Load(string path) => Addressables.LoadAssetAsync<Sprite>($"Assets/Textures/UI/{path}.png").WaitForCompletion();

        Fill   = Load("Controls/Round_FillLarge"          );
        Back   = Load("Controls/Round_VertHandle_Invert 1");
        Dash   = Load("Controls/StripesMaskSM"            );
        Small  = Load("Controls/Round_BorderSmall"        );
        Large  = Load("Controls/Round_BorderLarge"        );
        Hort   = Load("Controls/Round_HorizHandle_Invert" );
        Vert   = Load("Controls/Round_VertHandle_Invert"  );
        Mark   = Load("Controls/Check"                    );
        Mask   = Load("Controls/Round_SliderFill"         );
        Circle = Load("circle"                            );
        Shadow = Load("weaponwheelbackground"             );
        Dead   = Load("ISeeYou"                           );
    }

    /// <summary> Returns the scale of the given sprite. </summary>
    public static float Scale(Sprite sprite) => sprite == Mask ? 5f : new[] { Fill, Back, Dash, Small, Large, Vert }.Any(s => s == sprite) ? 4f : 1f;
}
