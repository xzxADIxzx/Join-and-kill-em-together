namespace Jaket.UI.Lib;

using UnityEngine;

using static System.Globalization.NumberStyles;

/// <summary> Color palette that I find appealing. </summary>
public static class Pal
{
    #region palette

    /// <summary> Hex variants of the colors. </summary>
    public static string

    Clear   = "#00000000",
    Invi    = "#00000075",
    Semi    = "#000000C7",
    Black   = "#000000FF",
    Heavy   = "#424242FF",
    Light   = "#A2A2A2FF",
    White   = "#FFFFFFFF",

    Red     = "#FF3223FF",
    Orange  = "#FF8800FF",
    Yellow  = "#FFBB22FF",
    Green   = "#32CD32FF",
    Blue    = "#0096FFFF",
    Pink    = "#FF77CCFF",
    Purple  = "#BF90FBFF",

    Coral   = "#FF7F50FF",
    Charge  = "#2C66CCFF",
    Empty   = "#003366FF",
    Discord = "#5865F2FF";

    /// <summary> Int variants of the colors. </summary>
    public static Color32

    clear   = Hex2Int(Clear  ),
    invi    = Hex2Int(Invi   ),
    semi    = Hex2Int(Semi   ),
    black   = Hex2Int(Black  ),
    heavy   = Hex2Int(Heavy  ),
    light   = Hex2Int(Light  ),
    white   = Hex2Int(White  ),

    red     = Hex2Int(Red    ),
    orange  = Hex2Int(Orange ),
    yellow  = Hex2Int(Yellow ),
    green   = Hex2Int(Green  ),
    blue    = Hex2Int(Blue   ),
    pink    = Hex2Int(Pink   ),
    purple  = Hex2Int(Purple ),

    coral   = Hex2Int(Coral  ),
    charge  = Hex2Int(Charge ),
    empty   = Hex2Int(Empty  ),
    discord = Hex2Int(Discord);

    /// <summary> Fixes namespace collisions. </summary>
    public static string Gray = Light;

    #endregion
    #region strings

    /// <summary> Storage of the basic colors. </summary>
    public static string[] Colors;

    /// <summary> Fills the basic color storage. </summary>
    public static void Load()
    {
        static void Put(string name, string color)
        {
            Colors ??= new string[byte.MaxValue + 1];
            var hash = Hash(name);

            if (Colors[hash] == null)
                Colors[hash] = color;
            else
                Log.Warning($"[PALE] Hash collision has occurred, the hash of {name} is {hash}");
        }

        Put("black",   Black);
        Put("dark",    Heavy);
        Put("gray",    Heavy);
        Put("grey",    Heavy);
        Put("heavy",   Heavy);
        Put("light",   Light);
        Put("white",   White);
        Put("red",     Red);
        Put("orange",  Orange);
        Put("yellow",  Yellow);
        Put("green",   Green);
        Put("blue",    Blue);
        Put("pink",    Pink);
        Put("purple",  Purple);
        Put("coral",   Coral);
        Put("discord", Discord);
    }

    /// <summary> Returns the hash of the given color name. </summary>
    public static byte Hash(string str) => (byte)(str.Length ^ (str[0] - 96 << 1) + (str[1] - 96 << 2) + (str[2] - 96 << 3));

    #endregion
    #region convert

    /// <summary> Converts hex color into int color. </summary>
    public static Color32 Hex2Int(string hex) => new
    (
        byte.Parse(hex[1..3], HexNumber),
        byte.Parse(hex[3..5], HexNumber),
        byte.Parse(hex[5..7], HexNumber),
        byte.Parse(hex[7..9], HexNumber)
    );

    /// <summary> Converts int color into hex color. </summary>
    public static string Int2Hex(Color32 col) => $"#{col.r:X2}{col.g:X2}{col.b:X2}{col.a:X2}";

    extension(Color32 col)
    {
        /// <summary> Darker version of the color. </summary>
        public Color32 Darker => Color32.Lerp(col, black, .4f);
    }

    extension(Color   col)
    {
        /// <summary> Darker version of the color. </summary>
        public Color   Darker => Color  .Lerp(col, black, .4f);
    }

    #endregion
}
