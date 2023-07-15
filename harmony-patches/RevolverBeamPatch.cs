namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;
using Jaket.Net.EntityTypes;

[HarmonyPatch(typeof(RevolverBeam), "Start")]
public class RevolverBeamPatch
{
    static void Prefix(RevolverBeam __instance) => Bullets.Send(__instance.gameObject);
}

[HarmonyPatch(typeof(RevolverBeam), "ExecuteHits")]
public class RevolverBeamPatchPvP
{
    static void Prefix(RevolverBeam __instance, RaycastHit currentHit)
    {
        // there is no point in checking enemy bullets, everyone is responsible for himself
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        var enemy = currentHit.transform.gameObject.GetComponentInParent<EnemyIdentifier>();
        if (enemy == null || __instance.hitEids.Contains(enemy)) return;

        // send a damage event to the host
        if (enemy.gameObject.TryGetComponent<RemotePlayer>(out var player)) player.Damage(__instance.damage * 6f);
    }
}
