/*
namespace Jaket.ObsoletePatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

[HarmonyPatch]
public class CommonBulletsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Projectile), "Start")]
    static void Projectile(Projectile __instance) => Bullets.Sync(__instance.gameObject, ref __instance.sourceWeapon, false, true);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GasolineProjectile), MethodType.Constructor)]
    static void Gasoline(GasolineProjectile __instance) => Events.Post(() => Bullets.Sync(__instance.gameObject, true, false));
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
}
*/
