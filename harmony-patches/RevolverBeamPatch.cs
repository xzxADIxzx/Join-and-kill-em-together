namespace Jaket.HarmonyPatches;

using HarmonyLib;
using UnityEngine;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(RevolverBeam), "Start")]
public class RevolverBeamPatch
{
    static void Prefix(RevolverBeam __instance)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        byte[] data = Bullets.Write(__instance.gameObject);

        if (LobbyController.IsOwner)
        {
            foreach (var member in LobbyController.Lobby?.Members)
                if (member.Id != Steamworks.SteamClient.SteamId) Networking.SendEvent(member.Id, data, 0);
        }
        else Networking.SendEvent2Host(data, 0);
    }
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
