namespace Jaket.Patches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net.Types;

[HarmonyPatch]
public class CommonBulletsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Coin), "Start")]
    static void Coin(Coin __instance) => Events.Post2(() => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, true, false));

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
        var ex = __instance.GetComponentInChildren<Explosion>();
        var n1 = __instance.name;
        var n2 = ex.sourceWeapon.name;

        // only shotgun and hammer explosions must be synchronized
        if ((n1 == "SG EXT(Clone)" && n2.StartsWith("Shotgun")) || (n1 == "SH(Clone)" && n2.StartsWith("Hammer")) || n1 == "Net")
            Bullets.Sync(__instance.gameObject, ref ex.sourceWeapon, false, false);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Nail), "Start")]
    static void Nail(Nail __instance) => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, true, false);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Harpoon), "Start")]
    static void Harpoon(Harpoon __instance) => Events.Post2(() => Bullets.Sync(__instance.gameObject, true, true));



    [HarmonyPrefix]
    [HarmonyPatch(typeof(Explosion), "Start")]
    static void Blast(Explosion __instance) => Bullets.SyncBlast(__instance.transform.parent?.gameObject, ref __instance.sourceWeapon);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PhysicalShockwave), "Start")]
    static void Shock(PhysicalShockwave __instance) => Bullets.SyncShock(__instance.gameObject, __instance.force);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Explode))]
    static bool Core(Grenade __instance)
    {
        // if the grenade is a rocket or local, then explode it, otherwise skip the explosion because it will be synced
        if (__instance.rocket || __instance.name != "Net") return true;

        Tools.Destroy(__instance.gameObject);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Nail), "TouchEnemy")]
    static bool Sawblade(Nail __instance, Transform other) =>
        __instance.sawblade && other.TryGetComponent<EnemyIdentifierIdentifier>(out var eid) &&
        eid.eid != null && eid.eid.TryGetComponent<RemotePlayer>(out var player)
            ? !player.Team.Ally()
            : true;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Harpoon), "FixedUpdate")]
    static void HarpoonDamage(Harpoon __instance, ref float ___drillCooldown)
    {
        // this is necessary so that only the one who created the harpoon causes the damage
        if (__instance.name == "Net") ___drillCooldown = 1f;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Harpoon), "OnTriggerEnter")]
    static bool HarpoonLogic(Harpoon __instance, Collider other, ref bool ___hit, ref Rigidbody ___rb)
    {
        if (__instance.name == "Net" && !___hit && other.gameObject == NewMovement.Instance.gameObject)
        {
            ___hit = true;
            ___rb.constraints = RigidbodyConstraints.FreezeAll;
            __instance.transform.SetParent(other.transform, true);

            return false;
        }
        else return true;
    }
}

[HarmonyPatch]
public class EntityBulletsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Grenade), "Start")] // DO NOT USE AWAKE                    __instance?.gameObject doesn't work ._.
    static void GrenadeSpawn(Grenade __instance) => Events.Post2(() => Bullets.Sync(__instance ? __instance.gameObject : null, true, false));

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Explode))]
    static void GrenadeDeath(Grenade __instance) => Bullets.SyncDeath(__instance.gameObject);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Grenade), nameof(Grenade.PlayerRideStart))]
    static void GrenadeRide(Grenade __instance) => __instance.GetComponent<Bullet>()?.TakeOwnage();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Grenade), nameof(Grenade.frozen), MethodType.Getter)]
    static void GrenadeFrozen(Grenade __instance, ref bool __result)
    {
        // pass the network value in the most crutch and optimized way
        if (__instance.rocketSpeed != 100f) __result = __instance.rocketSpeed == 98f;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Cannonball), "Start")]
    static void CannonballSpawn(Cannonball __instance) => Bullets.Sync(__instance.gameObject, true, false);

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
