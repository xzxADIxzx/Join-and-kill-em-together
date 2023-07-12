namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Net;

[HarmonyPatch(typeof(Grenade), "Start")]
public class GrenadePatch
{
    static void Prefix(Grenade __instance)
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
