namespace Jaket.Patches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

[HarmonyPatch]
public class CommonBulletsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(RevolverBeam), "Start")]
    static void Beam(RevolverBeam __instance) => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, false, true);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Projectile), "Start")]
    static void Projectile(Projectile __instance) => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, false, true);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GasolineProjectile), MethodType.Constructor)]
    static void Gasoline(GasolineProjectile __instance) => Events.Post(() => Bullets.Sync(__instance.gameObject, true, false));

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExplosionController), "Start")]
    static void Explosion(ExplosionController __instance)
    {
        var ex = __instance.GetComponentInChildren<Explosion>();
        if (ex == null) return;

        var n1 = __instance.name;
        var n2 = ex.sourceWeapon?.name ?? "";

        // only shotgun and hammer explosions must be synchronized
        if ((n1 == "SG EXT(Clone)" && n2.StartsWith("Shotgun")) || (n1 == "SH(Clone)" && n2.StartsWith("Hammer")) || n1 == "Net")
            Bullets.Sync(__instance.gameObject, ref ex.sourceWeapon, false, false);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Nail), "Start")]
    static void Nail(Nail __instance) => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, true, false);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Harpoon), "Start")]
    static void Harpoon(Harpoon __instance) => Events.Post2(() => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, true, true));



    [HarmonyPrefix]
    [HarmonyPatch(typeof(Explosion), "Start")]
    static void Blast(Explosion __instance) => Bullets.SyncBlast(__instance.transform.parent?.gameObject);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PhysicalShockwave), "Start")]
    static void Shock(PhysicalShockwave __instance) => Bullets.SyncShock(__instance.gameObject, __instance.force);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Explode))]
    static bool Core(Grenade __instance)
    {
        // if the grenade is a rocket or local, then explode it, otherwise skip the explosion because it will be synced
        if (LobbyController.Offline || __instance.rocket || __instance.name != "Net") return true;

        Tools.Destroy(__instance.gameObject);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Nail), "TouchEnemy")]
    static bool Sawblade(Nail __instance, Transform other) =>
        LobbyController.Online
        && __instance.sawblade
        && other.TryGetComponent<EnemyIdentifierIdentifier>(out var eid)
        && eid.eid != null
        && eid.eid.TryGetComponent<RemotePlayer>(out var player)
            ? !player.Team.Ally()
            : true;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Harpoon), "OnTriggerEnter")]
    static bool HarpoonLogic(Harpoon __instance, Collider other, ref bool ___hit, ref Rigidbody ___rb)
    {
        if (__instance.sourceWeapon == Bullets.Fake && !___hit && other.gameObject == NewMovement.Instance.gameObject)
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
    [HarmonyPrefix]
    [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.ExecuteHits))]
    static bool CoinFix(RevolverBeam __instance, RaycastHit currentHit) => __instance.name != "Net" || !(currentHit.transform?.CompareTag("Coin") ?? false);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Coin), "Start")]
    static bool CoinSpawn(Coin __instance)
    {
        if (LobbyController.Online)
        {
            Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, default, default);
            return false;
        }
        else return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Coin), "DelayedReflectRevolver")]
    static bool CoinReflect(Coin __instance, GameObject beam)
    {
        if (LobbyController.Online)
        {
            __instance.GetComponent<TeamCoin>()?.Reflect(beam);
            return false;
        }
        else return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Coin), "DelayedPunchflection")]
    static bool CoinPunch(Coin __instance)
    {
        if (LobbyController.Online)
        {
            __instance.GetComponent<TeamCoin>()?.Punch();
            return false;
        }
        else return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Coin), "Bounce")]
    static bool CoinBounce(Coin __instance)
    {
        if (LobbyController.Online)
        {
            __instance.GetComponent<TeamCoin>()?.Bounce();
            return false;
        }
        else return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Coin), "OnCollisionEnter")]
    static bool CoinCollision() => LobbyController.Offline;



    [HarmonyPostfix]
    [HarmonyPatch(typeof(Grenade), "Start")] // DO NOT USE AWAKE
    static void GrenadeSpawn(Grenade __instance) => Bullets.Sync(__instance.gameObject, true, false);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Explode))]
    static void GrenadeDeath(Grenade __instance, bool harmless, float sizeMultiplier) => Bullets.SyncDeath(__instance.gameObject, harmless, sizeMultiplier > 1f);

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
    static void CannonballSpawn(Cannonball __instance) => Bullets.Sync(__instance.gameObject, default, default);

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
