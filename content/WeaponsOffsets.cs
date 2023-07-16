namespace Jaket.Content;

using UnityEngine;

/// <summary> List of offsets for all weapons in the game and a useful method. </summary>
public class WeaponsOffsets
{
    /// <summary> List of offsets for all weapons in the game. </summary>
    public static Vector4[] Offsets =
    {
        new(-.21f, -.07f, .03f, .19f),
        new(-.21f, -.07f, .03f, .19f),
        new(-.18f, -.07f, .03f, .19f),
        new(-.21f, -.07f, .03f, .19f),
        new(-.21f, -.07f, .03f, .19f),
        new(-.21f, -.07f, .03f, .19f),
        new(-.41f, -.07f, .03f, .27f),
        new(-.41f, -.07f, .03f, .27f),
        new(-.18f,  .1f,  .13f, .27f),
        new(-.21f,  .2f,  .18f, .27f),
        new(-.18f,  .1f,  .13f, .27f),
        new(-.21f,  .2f,  .18f, .27f),
        new(-.35f, -.07f, .13f, .27f),
        new(-.35f, -.07f, .13f, .27f),
        new(-.35f, -.07f, .13f, .27f),
        new(-.21f,  .1f,  .13f, .3f),
        new(-.21f,  .1f,  .13f, .3f)
    };

    /// <summary> Applies an offset to the given transform. </summary>
    public static void Apply(int index, Transform target)
    {
        target.localPosition = Offsets[index];
        target.localScale = new Vector3(Offsets[index].w, Offsets[index].w, Offsets[index].w);
    }
}
