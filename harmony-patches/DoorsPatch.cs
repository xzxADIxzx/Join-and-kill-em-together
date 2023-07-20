namespace Jaket.HarmonyPatches;

using HarmonyLib;

using Jaket.Content;
using Jaket.Net;

[HarmonyPatch(typeof(Door), nameof(Door.Unlock))]
public class DoorPatch
{
    static void Prefix()
    {
        // doors are unlocked only at the host, because only he has original enemies
        if (LobbyController.Lobby == null || !LobbyController.IsOwner) return;

        // notify each client that the door has opened so they don't get stuck in a room
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, new byte[1], PacketType.UnlockDoors));
    }
}

[HarmonyPatch(typeof(FinalDoor), nameof(FinalDoor.Open))]
public class FinalDoorPatch
{
    static void Prefix()
    {
        // final door can also open on the client, but in the case of boss fight, it only opens on the host
        if (LobbyController.Lobby == null || !LobbyController.IsOwner) return;

        // notify each client that the door has opened so they don't get stuck in a room
        LobbyController.EachMemberExceptOwner(member => Networking.Send(member.Id, new byte[1], PacketType.UnlockFinalDoor));
    }
}
