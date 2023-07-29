namespace Jaket.Content;

using UnityEngine;

/// <summary> List of offsets for all weapons in the game and a useful method. </summary>
public class WeaponsOffsets
{
    /// <summary> All revolvers have the same offset, which is kinda cool. </summary>
    public static Vector4 RevolverOffset = new(.001f, .0022f, .0012f, .0019f);

    /// <summary> The same situation with alternative revolvers. </summary>
    public static Vector4 AltRevolverOffset = new(.0011f, .0017f, .0013f, .0019f);

    /// <summary> List of offsets for all weapons in the game. </summary>
    public static Vector4[] Offsets =
    {
        RevolverOffset, AltRevolverOffset, RevolverOffset, AltRevolverOffset, RevolverOffset, AltRevolverOffset,
        new(.0015f, .0014f, .0034f, .0027f),
        new(.0015f, .0014f, .0034f, .0027f),
        new(.0013f, .0038f, .0018f, .0027f),
        new(.0015f, .0039f, .0026f, .0027f),
        new(.0013f, .0038f, .0018f, .0027f),
        new(.0015f, .0039f, .0026f, .0027f),
        new(.0021f, .002f,  .0028f, .0027f),
        new(.0021f, .002f,  .0028f, .0027f),
        new(.0021f, .002f,  .0028f, .0027f),
        new(.0015f, .0023f, .0014f, .003f),
        new(.0015f, .0023f, .0014f, .003f)
    };

    /// <summary> Applies an offset to the given transform. </summary>
    public static void Apply(int index, Transform target)
    {
        target.localPosition = Offsets[index];
        target.localEulerAngles = new(index >= 15 ? 290f : 280f, 0f, 180f); // by default, weapons are very strangely rotated
        target.localScale = new(Offsets[index].w, Offsets[index].w, Offsets[index].w);
    }
}
