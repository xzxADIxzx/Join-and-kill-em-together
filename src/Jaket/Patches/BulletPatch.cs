namespace Jaket.Patches;

using System;
using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net.Types;

[HarmonyPatch]
public class CommonBulletsPatch
{
    static void Post2(Action action) => Events.Post(() => Events.Post(action));

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Coin), "Start")]
    static void Coin(Coin __instance) => Post2(() => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, true, false));

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RevolverBeam), "Start")]
    static void Beam(RevolverBeam __instance) => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, false, true);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Projectile), "Start")]
    static void Projectile(Projectile __instance) => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, false, true);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExplosionController), "Start")]
    static void Explosion(ExplosionController __instance)
    {
        var explosion = __instance.GetComponentInChildren<Explosion>();
        if (__instance.name == "SG EXT(Clone)") // only shotgun explosions need to be synchronized
            Bullets.Sync(__instance.gameObject, ref explosion.sourceWeapon, false, false);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Nail), "Start")]
    static void Nail(Nail __instance) => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, true, false);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Harpoon), "Start")]
    static void Harpoon(Harpoon __instance, Rigidbody ___rb) => Post2(() => Bullets.Sync(__instance.gameObject, true, true));



    [HarmonyPrefix]
    [HarmonyPatch(typeof(Explosion), "Start")]
    static void Blast(Explosion __instance) => Bullets.SyncBlast(__instance.transform.parent?.gameObject, ref __instance.sourceWeapon);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PhysicalShockwave), "Start")]
    static void Shock(PhysicalShockwave __instance) => Bullets.SyncShock(__instance.gameObject, __instance.force);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Nail), "TouchEnemy")]
    static bool Sawblade(Nail __instance, Transform other) =>
        __instance.sawblade && other.TryGetComponent<EnemyIdentifierIdentifier>(out var eid) &&
        eid.eid != null && eid.eid.TryGetComponent<RemotePlayer>(out var player)
            ? !player.team.Ally()
            : true;
}

[HarmonyPatch]
public class EntityBulletsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Cannonball), "Start")]
    static void CannonballSpawn(Cannonball __instance) => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, true, false);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Break))]
    static void CannonballDeath(Cannonball __instance) => Bullets.SyncDeath(__instance.gameObject);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Launch))]
    static void CannonballParry(Cannonball __instance) => __instance.GetComponent<Bullet>()?.TakeOwnage();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Cannonball), nameof(Cannonball.Unlaunch))]
    static void CannonballHook(Cannonball __instance) => __instance.GetComponent<Bullet>()?.TakeOwnage();
}

