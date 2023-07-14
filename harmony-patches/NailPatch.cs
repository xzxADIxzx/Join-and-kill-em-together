namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(Nail), "Start")]
public class NailPatch
{
    static void Prefix(Nail __instance)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        byte[] data = Bullets.Write(__instance.gameObject, true);

        if (LobbyController.IsOwner)
            LobbyController.EachMemberExceptOwner(member => Networking.SendEvent(member.Id, data, 0));
        else
            Networking.SendEvent2Host(data, 0);
    }
}

[HarmonyPatch(typeof(Nail), "DamageEnemy")]
public class NailPatchPvP
{
    static void Prefix(Nail __instance, EnemyIdentifier eid)
    {
        // there is no point in checking enemy bullets, everyone is responsible for himself
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        // send a damage event to the host
        if (eid.gameObject.TryGetComponent<RemotePlayer>(out var player)) player.Damage(__instance.damage * 6f);
    }
}
