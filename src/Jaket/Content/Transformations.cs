namespace Jaket.Content;

using UnityEngine;

/// <summary> Set of transformations used to correctly position weapons. </summary>
public static class Transformations
{
    /// <summary> Transformations of different weapon kinds. </summary>
    private static Vector4

    defRewower = new(.0010f, -.0003f, .0012f, .0019f),
    altRewower = new(.0011f, -.0006f, .0015f, .0019f),

    defShotgun = new(.0013f, -.0004f, .0032f, .0024f),
    altShotgun = new(.0020f, +.0008f, .0016f, .0026f),

    defNailgun = new(.0012f, +.0014f, .0013f, .0024f),
    altNailgun = new(.0014f, +.0018f, .0017f, .0021f),

    railcannon = new(.0020f, +.0000f, .0027f, .0024f),
    rocketLnch = new(.0014f, -.0026f, .0018f, .0036f);

    /// <summary> Transformations of different weapon types. </summary>
    private static Vector4[] transforms =
    {
        defRewower, altRewower, defRewower, altRewower, defRewower, altRewower,
        defShotgun, altShotgun, defShotgun, altShotgun, defShotgun, altShotgun,
        defNailgun, altNailgun, defNailgun, altNailgun, defNailgun, altNailgun,
        railcannon, railcannon, railcannon, rocketLnch, rocketLnch, rocketLnch,
    };

    /// <summary> Applies transformation to the given transform. </summary>
    public static void Apply(EntityType type, Transform target)
    {
        target.localPosition = transforms[type - EntityType.RevolverBlue];
        target.localRotation = Quaternion.Euler(270f, 0f, 180f);
        target.localScale    = transforms[type - EntityType.RevolverBlue].w * Vector3.one;
    }
}
