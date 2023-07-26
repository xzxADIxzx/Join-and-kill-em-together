namespace Jaket.UI;

using UnityEngine;

/// <summary> Utility class for displaying the interface correctly on wide screens. </summary>
public class WidescreenFix
{
    /// <summary> How many pixels to shift UI elements in height. </summary>
    public static float Offset { get; private set; }

    /// <summary> Makes the necessary calculations in order to move the interface in height by the required number of pixels. </summary>
    public static void Load()
    {
        // cast height and width to float for more accurate calculations
        float width = Screen.width, height = Screen.height;

        // find the aspect ratio of the screen
        float aspect = width / height;

        // save the offset for later use
        Offset = height * (aspect - 16f / 9f) / 3.8f;
    }

    /// <summary> Moves all children of the given transform by Offset pixels up. </summary>
    public static void MoveUp(Transform root)
    {
        foreach (RectTransform child in root) child.anchoredPosition += new Vector2(0f, Offset);
    }

    /// <summary> Moves all children of the given transform by Offset pixels down. </summary>
    public static void MoveDown(Transform root)
    {
        foreach (RectTransform child in root) child.anchoredPosition -= new Vector2(0f, Offset);
    }
}