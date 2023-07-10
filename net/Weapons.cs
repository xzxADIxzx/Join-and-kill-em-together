namespace Jaket;

using System.Collections.Generic;
using UnityEngine;

public class Weapons
{
    public static List<GameObject> All = new();

    public static void Load()
    {
        All.AddRange(GunSetter.Instance.revolverPierce);
        All.AddRange(GunSetter.Instance.revolverRicochet);
        All.AddRange(GunSetter.Instance.revolverTwirl);

        All.AddRange(GunSetter.Instance.shotgunGrenade);
        All.AddRange(GunSetter.Instance.shotgunPump);
        All.AddRange(GunSetter.Instance.shotgunRed);

        All.AddRange(GunSetter.Instance.nailMagnet);
        All.AddRange(GunSetter.Instance.nailOverheat);
        All.AddRange(GunSetter.Instance.nailRed);

        All.AddRange(GunSetter.Instance.railCannon);
        All.AddRange(GunSetter.Instance.railHarpoon);
        All.AddRange(GunSetter.Instance.railMalicious);

        All.AddRange(GunSetter.Instance.rocketBlue);
        All.AddRange(GunSetter.Instance.rocketGreen);
        All.AddRange(GunSetter.Instance.rocketRed);
    }
}