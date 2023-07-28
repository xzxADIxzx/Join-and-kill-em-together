namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
public class NewMovementPatch
{
    static void Prefix()
    {
        // checkpoint destroys some objects on the level
        World.Instance.Recache();

        // in the sandbox after death, enemies are not destroyed
        if (LobbyController.Lobby == null || !LobbyController.IsOwner || SceneHelper.CurrentScene == "uk_construct") return;

        // notify each client that the host has died so that they destroy all enemies
        LobbyController.EachMemberExceptOwner(member => Networking.SendEmpty(member.Id, PacketType.HostDied));
    }
}
