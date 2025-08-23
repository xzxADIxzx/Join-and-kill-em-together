namespace Jaket.Content;

using UnityEngine;

/// <summary> Set of transformations used to correctly position weapons. </summary>
public static class Transformations
{
    /// <summary> Transformations of different weapon kinds. </summary>
    private static Vector4

    defRewower = new(.0009f, .0020f, .0011f, .0019f),
    altRewower = new(.0009f, .0017f, .0013f, .0018f),

    defShotgun = new(.0012f, .0015f, .0030f, .0024f),
    altShotgun = new(.0022f, .0026f, .0018f, .0028f),

    defNailgun = new(.0011f, .0036f, .0016f, .0024f),
    altNailgun = new(.0012f, .0038f, .0021f, .0021f),

    railcannon = new(.0019f, .0018f, .0025f, .0024f),
    rocketLnch = new(.0015f, .0022f, .0014f, .0036f);

    /// <summary> Transformations of all weapons. </summary>
    private static Vector4[] transforms =
    {
        default, // player
        defRewower, altRewower, defRewower, altRewower, defRewower, altRewower,
        defShotgun, altShotgun, defShotgun, altShotgun, defShotgun, altShotgun,
        defNailgun, altNailgun, defNailgun, altNailgun, defShotgun, altShotgun,
        railcannon, railcannon, railcannon, rocketLnch, rocketLnch, rocketLnch,
    };

    /// <summary> Applies transformation to the given transform. </summary>
    public static void Apply(EntityType type, Transform target)
    {
        target.localPosition = transforms[(int)type];
        target.localRotation = Quaternion.Euler(type >= EntityType.RocketlBlue ? 290f : 280f, 0f, 180f);
        target.localScale    = transforms[(int)type].w * Vector3.one;
    }
}
