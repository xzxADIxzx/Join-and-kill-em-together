namespace Jaket.UI;

using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Class that builds the entire interface of the mod. </summary>
public class UI
{
    /// <summary> Various sprites from which the interface is built. </summary>
    private static Sprite background, shadow, circleShadow, circle;
    /// <summary> Set of colors for buttons. </summary>
    private static ColorBlock colors;

    /// <summary> Loads the resources needed for interface. </summary>
    public static void Load()
    {
        // find all sprites
        var all = Resources.FindObjectsOfTypeAll<Sprite>();
        Func<string, Sprite> find = name => Array.Find(all, s => s.name == name);

        background = find("UISprite");
        shadow = find("horizontalgradientslowfalloff");
        circleShadow = find("weaponwheelbackground");
        circle = find("circle");

        // create a color scheme for buttons
        colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = colors.selectedColor = new(.5f, .5f, .5f, 1f);
        colors.pressedColor = new(1f, 0f, 0f, 1f);
    }

    /// <summary> Creates singleton instances of various UI elements. </summary>
    public static void Build()
    {
        PlayerList.Build();
        PlayerIndicators.Build();

        Chat.Build();
        EmojiWheel.Build();
    }
}
