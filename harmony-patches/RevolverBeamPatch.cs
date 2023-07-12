namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Net;

[HarmonyPatch(typeof(RevolverBeam), "Start")]
public class RevolverBeamPatch
{
    static void Prefix(RevolverBeam __instance)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        byte[] data = Weapons.WriteBullet(__instance.gameObject);

        if (LobbyController.IsOwner)
        {
            foreach (var member in LobbyController.Lobby?.Members)
                if (member.Id != Steamworks.SteamClient.SteamId) Networking.SendEvent(member.Id, data, 0);
        }
        else Networking.SendEvent2Host(data, 0);
    }
}

[HarmonyPatch(typeof(RevolverBeam), "PiercingShotCheck")]
public class RevolverBeamPatchPvP
{
    static void Prefix(RevolverBeam __instance, int ___enemiesPierced)
    {
        // there is no point in checking enemy bullets, everyone is responsible for himself
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net" || ___enemiesPierced >= __instance.hitList.Count) return;

        // hit currently being processed
        var hit = __instance.hitList[___enemiesPierced];

        var enemy = hit.transform.gameObject.GetComponentInParent<EnemyIdentifier>();
        if (enemy == null || __instance.hitEids.Contains(enemy)) return;

        if (enemy.gameObject.TryGetComponent<RemotePlayer>(out var player))
        {
            // prevent further processing of the same player
            __instance.hitEids.Add(enemy);

            // send a damage event to the host
            player.Damage(__instance.damage);
        }
    }
}
