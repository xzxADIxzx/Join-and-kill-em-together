namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.Types;

[HarmonyPatch(typeof(EnemyIdentifier))]
public class EnemyPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    static bool Start(EnemyIdentifier __instance) => Enemies.Sync(__instance);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EnemyIdentifier.DeliverDamage))]
    static bool Damage(EnemyIdentifier __instance, float multiplier, bool tryForExplode, float critMultiplier, GameObject sourceWeapon) =>
        Enemies.SyncDamage(__instance, multiplier, tryForExplode, critMultiplier, sourceWeapon);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EnemyIdentifier.Death))]
    static void Death(EnemyIdentifier __instance) => Enemies.SyncDeath(__instance);
}

[HarmonyPatch]
public class OtherPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Idol), "SlowUpdate")]
    static bool IdolsLogic() => LobbyController.Lobby == null || LobbyController.IsOwner;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FerrymanFake), "Update")]
    static bool FerryLogic() => LobbyController.Lobby == null || LobbyController.IsOwner;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FerrymanFake), nameof(FerrymanFake.OnLand))]
    static void FerryDeath(FerrymanFake __instance)
    {
        if (LobbyController.IsOwner && __instance.TryGetComponent<Enemy>(out var enemy)) Networking.Send(PacketType.EnemyDied, w => w.Id(enemy.Id), size: 8);
    }
}
