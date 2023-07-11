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
                if (member.Id != Steamworks.SteamClient.SteamId) Networking.SendEvent(member.Id, data);
        }
        else Networking.SendEvent2Host(data);
    }
}
