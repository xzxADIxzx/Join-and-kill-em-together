namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(Nail), "Start")]
public class NailPatch
{
    static void Prefix(Nail __instance) => Bullets.Send(__instance.gameObject, true);
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
