namespace Jaket.UI.Lib;

using UnityEngine;

/// <summary> Color palette that I find appealing. </summary>
public static class Pal
{
    /// <summary> Hex variants of the colors. </summary>
    public static string

    Clear   = "#00000000",
    Invi    = "#00000075",
    Semi    = "#000000C7",
    Black   = "#000000",
    Dark    = "#424242",
    Gray    = "#727272",
    Light   = "#B2B2B2",
    White   = "#FFFFFF",

    Red     = "#FF3223",
    Orange  = "#FF8800",
    Yellow  = "#FFBB22",
    Green   = "#32CD32",
    Blue    = "#0096FF",
    Pink    = "#FF77CC",
    Purple  = "#BF90FB",

    Coral   = "#FF7F50",
    Charge  = "#2C66CC",
    Empty   = "#003366",

    Discord = "#5865F2",
    PayPal  = "#003087",
    Coffee  = "#FFDD00";

    /// <summary> Int variants of the colors. </summary>
    public static Color

    clear   = new(),
    invi    = new(0f, 0f, 0f, .46f),
    semi    = new(0f, 0f, 0f, .78f),
    black   = From(0x000000),
    dark    = From(0x424242),
    gray    = From(0x727272),
    light   = From(0xB2B2B2),
    white   = From(0xFFFFFF),

    red     = From(0xFF3223),
    orange  = From(0xFF8800),
    yellow  = From(0xFFBB22),
    green   = From(0x32CD32),
    blue    = From(0x0096FF),
    pink    = From(0xFF77CC),
    purple  = From(0xBF90FB),

    coral   = From(0xFF7F50),
    charge  = From(0x2C66CC),
    empty   = From(0x003366),

    discord = From(0x5865F2),
    paypal  = From(0x003087),
    coffee  = From(0xFFDD00);

    /// <summary> Hash map containing hex variants of the colors. </summary>
    public static string[] Colors;

    /// <summary> Fills the hash map with basic colors. </summary>
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
        Put("dark",    Dark);
        Put("gray",    Gray);
        Put("grey",    Gray);
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

    /// <summary> Returns int version of the given hex color. </summary>
    public static Color32 From(int hex) => new((byte)(hex >> 16 & 0xFF), (byte)(hex >> 8 & 0xFF), (byte)(hex >> 0 & 0xFF), 0xFF);

    /// <summary> Returns darker version of the given color. </summary>
    public static Color Darker(Color original) => Color.Lerp(original, black, .38f);
}
