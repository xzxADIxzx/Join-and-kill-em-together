namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;

[HarmonyPatch(typeof(Coin), "StartCheckingSpeed")] // for some reason, the coin has zero velocity in Start
public class CoinPatch
{
    static void Postfix(Coin __instance) => Bullets.Send(__instance.gameObject, true, false);
}

[HarmonyPatch(typeof(RevolverBeam), "Start")]
public class RevolverBeamPatch
{
    static void Prefix(RevolverBeam __instance)
    {
        if (__instance.sourceWeapon == null) __instance.sourceWeapon = Bullets.synchronizedBullet;
        Bullets.Send(__instance.gameObject);
    }
}

[HarmonyPatch(typeof(Projectile), "Start")]
public class ProjectilePatch
{
    static void Prefix(Projectile __instance)
    {
        if (__instance.sourceWeapon == null) __instance.sourceWeapon = Bullets.synchronizedBullet;
        Bullets.Send(__instance.gameObject);
    }
}

[HarmonyPatch(typeof(Nail), "Start")]
public class NailPatch
{
    static void Prefix(Nail __instance)
    {
        if (__instance.sourceWeapon == null) __instance.sourceWeapon = Bullets.synchronizedBullet;
        Bullets.Send(__instance.gameObject, true, false);
    }
}

[HarmonyPatch(typeof(Harpoon), "Start")]
public class HarpoonPatch
{
    static void Postfix(Harpoon __instance, Rigidbody ___rb)
    {
        // the same as with a coin, so I have to do this
        if (___rb.velocity == Vector3.zero)
            __instance.Invoke("Start", .01f);
        else
            Bullets.Send(__instance.gameObject, true);
    }
}

[HarmonyPatch(typeof(Grenade), "Start")]
public class GrenadePatch
{
    static void Postfix(Grenade __instance, Rigidbody ___rb)
    {
        if (__instance.sourceWeapon == null) __instance.sourceWeapon = Bullets.synchronizedBullet;

        // the same as with a coin, so I have to do this
        if (___rb.velocity == Vector3.zero)
            __instance.Invoke("Start", .01f);
        else
            Bullets.Send(__instance.gameObject, true);
    }
}

[HarmonyPatch(typeof(Cannonball), "Start")]
public class CannonballPatch
{
    static void Prefix(Cannonball __instance)
    {
        if (__instance.sourceWeapon == null) __instance.sourceWeapon = Bullets.synchronizedBullet;
        Bullets.Send(__instance.gameObject, true);
    }
}
