namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.UI;

[HarmonyPatch(typeof(Coin), "StartCheckingSpeed")] // for some reason, the coin has zero velocity in Start
public class CoinPatch
{
    static void Postfix(Coin __instance) => Bullets.Send(__instance.gameObject, true, false);
}

[HarmonyPatch(typeof(RevolverBeam), "Start")]
public class RevolverBeamPatch
{
    static void Prefix(RevolverBeam __instance) => Bullets.Send(__instance.gameObject, ref __instance.sourceWeapon);
}

[HarmonyPatch(typeof(Projectile), "Start")]
public class ProjectilePatch
{
    static void Prefix(Projectile __instance) => Bullets.Send(__instance.gameObject, ref __instance.sourceWeapon);
}

[HarmonyPatch(typeof(ExplosionController), "Start")]
public class ExplosionPatch
{
    static void Prefix(ExplosionController __instance)
    {
        var explosion = __instance.GetComponentInChildren<Explosion>();

        // only shotgun explosions need to be synchronized
        if (explosion?.sourceWeapon != null && explosion.sourceWeapon.name.Contains("Shotgun")) Bullets.Send(__instance.gameObject, false, false);
    }
}

[HarmonyPatch(typeof(Explosion), "Start")]
public class BlastPatch
{
    static void Prefix(Explosion __instance) => Bullets.SendBlast(__instance.transform.parent?.gameObject);
}

[HarmonyPatch(typeof(PhysicalShockwave), "Start")]
public class ShockPatch
{
    static void Prefix(PhysicalShockwave __instance) => Bullets.SendShock(__instance.gameObject, __instance.force);
}

[HarmonyPatch(typeof(Nail), "Start")]
public class NailPatch
{
    static void Prefix(Nail __instance) => Bullets.Send(__instance.gameObject, ref __instance.sourceWeapon, true, false);
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
        if (__instance.sourceWeapon == null) __instance.sourceWeapon = Bullets.SynchronizedBullet;

        // the same as with a coin, so I have to do this
        if (___rb.velocity == Vector3.zero)
            __instance.Invoke("Start", .01f);
        else
            Bullets.Send(__instance.gameObject, true);
    }
}

[HarmonyPatch(typeof(Grenade), "Update")]
public class RidePatch
{
    // disable the ability to get off the rocket during chatting
    static bool Prefix() => !Chat.Instance.Shown;
}

[HarmonyPatch(typeof(Cannonball), "Start")]
public class CannonballPatch
{
    static void Prefix(Cannonball __instance) => Bullets.Send(__instance.gameObject, ref __instance.sourceWeapon, true);
}
