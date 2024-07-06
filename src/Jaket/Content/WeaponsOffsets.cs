namespace Jaket.Content;

using UnityEngine;

/// <summary> List of offsets for all weapons in the game and a useful method. </summary>
public class WeaponsOffsets
{
    public static Vector4 Rewower = new(.001f, .0022f, .0012f, .0019f);
    public static Vector4 AltRewower = new(.0011f, .0017f, .0013f, .0019f);
    public static Vector4 Shotgun = new(.0015f, .0014f, .0034f, .0027f);
    public static Vector4 AltShotgun = new(.0022f, .0021f, .0016f, .0028f);
    public static Vector4 Nailgun = new(.0013f, .0038f, .0018f, .0027f);
    public static Vector4 AltNailgun = new(.0015f, .0039f, .0026f, .0027f);
    public static Vector4 Railcannon = new(.0021f, .002f, .0028f, .0027f);
    public static Vector4 Rocket = new(.0015f, .0023f, .0014f, .0036f);

    /// <summary> List of offsets for all weapons in the game. </summary>
    public static Vector4[] Offsets =
    {
        Rewower, AltRewower, Rewower, AltRewower, Rewower, AltRewower,
        Shotgun, AltShotgun, Shotgun, AltShotgun, Shotgun, AltShotgun,
        Nailgun, AltNailgun, Nailgun, AltNailgun, Nailgun, AltNailgun,
        Railcannon, Railcannon, Railcannon,
        Rocket, Rocket, Rocket
    };

    /// <summary> Applies an offset to the given transform. </summary>
    public static void Apply(int index, Transform target)
    {
        target.localPosition = Offsets[index];
        target.localEulerAngles = new(index >= 21 ? 290f : 280f, 0f, 180f); // by default, weapons are very strangely rotated
        target.localScale = new(Offsets[index].w, Offsets[index].w, Offsets[index].w);
    }
}
