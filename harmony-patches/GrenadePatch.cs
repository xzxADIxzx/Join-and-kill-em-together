namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(Grenade), "Start")]
public class GrenadePatch
{
    static void Prefix(Grenade __instance)
    {
        // if the lobby is null or the name is Net, then either the player isn't connected or this bullet was created remotely
        if (LobbyController.Lobby == null || __instance.gameObject.name == "Net") return;

        byte[] data = Bullets.Write(__instance.gameObject);

        if (LobbyController.IsOwner)
            LobbyController.EachMemberExceptOwner(member => Networking.SendEvent(member.Id, data, 0));
        else
            Networking.SendEvent2Host(data, 0);
    }
}
