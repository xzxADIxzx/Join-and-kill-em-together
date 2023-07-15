namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
public class NewMovementPatch
{
    static void Prefix()
    {
        // in the sandbox after death, enemies are not destroyed
        if (LobbyController.Lobby == null && SceneHelper.CurrentScene == "uk_construct") return;

        // notify each client that the host has died so that they destroy all enemies
        LobbyController.EachMember(member => Networking.Send(member.Id, new byte[0], PacketType.HostDied));
    }
}
